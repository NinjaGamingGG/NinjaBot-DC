using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.Records;
using MySqlConnector;
using NinjaBot_DC;
using Serilog;

namespace LoungeSystemPlugin.Events;

public static class VoiceStateUpdated
{
    public static async Task ChannelEnter(DiscordClient client, VoiceStateUpdateEventArgs eventArgs)
    {
        if (ReferenceEquals(eventArgs.Channel, null))
            return;

        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();


        var channelExists = false;

        var customNamePattern = string.Empty;

        List<LoungeSystemConfigurationRecord> channelsList;
        LoungeMessageReplacement[]? loungeMessageReplacementsAsArray;

        try
        {
            var mySqlConnection = new MySqlConnection(connectionString);
            
            var channels = await mySqlConnection.QueryAsync<LoungeSystemConfigurationRecord>("SELECT * FROM LoungeSystemConfigurationIndex WHERE GuildId = @GuildId", new { GuildId = eventArgs.Guild.Id});
        
            var nameReplacementRecord = await mySqlConnection.QueryAsync<LoungeMessageReplacement>("SELECT * FROM LoungeMessageReplacementIndex WHERE GuildId= @GuildId AND ChannelId = @ChannelId", new {GuildId = eventArgs.Guild.Id, ChannelId = eventArgs.Channel.Id});
            await mySqlConnection.CloseAsync();
            
            channelsList = channels.ToList();
            loungeMessageReplacementsAsArray = nameReplacementRecord as LoungeMessageReplacement[] ?? nameReplacementRecord.ToArray();

        }
        catch (Exception ex)
        {
            Log.Error(ex,"Unable to Retrieve Lounge System Config & NameReplacement Records from Database");
            return;
        }
        
    
        if (loungeMessageReplacementsAsArray.Length == 0)
         return;
        
        foreach (var replacement in loungeMessageReplacementsAsArray)
        {
            if (replacement.ReplacementHandle == "Custom_Name")
                customNamePattern = replacement.ReplacementValue;
        }
        
        
        if (!ReferenceEquals(customNamePattern, null) && customNamePattern.Contains("{username}"))
            customNamePattern = customNamePattern.Replace("{username}", eventArgs.User.Username);

        if (ReferenceEquals(customNamePattern, null))
            return;
        
        ulong interfaceChannel = 0;

        foreach (var channelConfig in channelsList.Where(channelConfig => eventArgs.Channel.Id == channelConfig.TargetChannelId))
        {
            channelExists = true;
            interfaceChannel = channelConfig.InterfaceChannelId;
            
            break;
        }

        if (channelExists == false)
            return;
        
        eventArgs.Channel.Guild.Members.TryGetValue(eventArgs.User.Id, out var discordMember);
        if (ReferenceEquals(discordMember, null))
            return;

        var overWriteBuildersList = new List<DiscordOverwriteBuilder>
        {
            new DiscordOverwriteBuilder(discordMember)
                .Allow(Permissions.AccessChannels)
                .Allow(Permissions.UseVoice)
                .Allow(Permissions.Speak)
                .Allow(Permissions.SendMessages)
                .Allow(Permissions.Stream)
                .Allow(Permissions.PrioritySpeaker)
        };

        var roleSpecificOverrides = await BuildOverwritesForRequiredRoles(eventArgs.Guild.Id, eventArgs.Channel.Id);
        overWriteBuildersList.AddRange(roleSpecificOverrides);

        
        var channelNamePattern =
            await ChannelNameBuilder.BuildAsync(eventArgs.Guild.Id, eventArgs.Channel.Id,
                customNamePattern);
        
        var newChannel = await eventArgs.Channel.Guild.CreateVoiceChannelAsync(channelNamePattern, eventArgs.Channel.Parent, 96000,4, position: eventArgs.Channel.Position + 1, overwrites:overWriteBuildersList);
        
        if (ReferenceEquals(newChannel, null))
            return;
        
        var newModel = new LoungeDbRecord()
        {
            GuildId = eventArgs.Guild.Id,
            ChannelId = newChannel.Id,
            OwnerId = eventArgs.User.Id,
            IsPublic = true,
            OriginChannel = eventArgs.Channel.Id
        };

        int inserted;
        
        try
        {
            var mySqlConnection = new MySqlConnection(connectionString);
            inserted = await mySqlConnection.ExecuteAsync("INSERT INTO LoungeIndex (ChannelId, GuildId, OwnerId, IsPublic, OriginChannel) VALUES (@ChannelId,@GuildId,@OwnerId,@IsPublic,@OriginChannel)",newModel);
            await mySqlConnection.CloseAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex,"Unable to insert into Mysql LoungeIndex");
            return;
        }

        
        if (inserted == 0)
        {
            await newChannel.DeleteAsync();
            return;
        }

        await discordMember.PlaceInAsync(newChannel);
        
        await newChannel.SendMessageAsync("Welcome to your Lounge!");

        if (interfaceChannel != 0)
            return;
        
            
        var builder = InterfaceMessageBuilder.GetBuilder(client,discordMember.Mention + " this is your lounge Interface");
        await newChannel.SendMessageAsync(builder);
    }

    private static async Task<List<DiscordOverwriteBuilder>> BuildOverwritesForRequiredRoles(ulong guildId, ulong channelId)
    {
        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        var mySqlConnection = new MySqlConnection(connectionString);
        
        var requiredRoles = await mySqlConnection.QueryAsync<RequiredRoleRecord>("SELECT * FROM RequiredRoleIndex WHERE GuildId = @GuildId AND ChannelId = @ChannelId", new { GuildId = guildId, ChannelId = channelId});
        var requiredRolesList = requiredRoles.ToList();
        
        var discordClient = Worker.GetServiceDiscordClient();

        var guild = await discordClient.GetGuildAsync(guildId);

        var overWriteBuildersList = new List<DiscordOverwriteBuilder>();

        if (requiredRolesList.Count == 0)
            requiredRolesList.Add(new RequiredRoleRecord{RoleId = guild.EveryoneRole.Id});
        else
            overWriteBuildersList.Add(new DiscordOverwriteBuilder(guild.EveryoneRole)
                .Deny(Permissions.AccessChannels)
                .Deny(Permissions.SendMessages)
                .Deny(Permissions.UseVoice)
                .Deny(Permissions.Speak)
                .Deny(Permissions.Stream));

        overWriteBuildersList.AddRange(requiredRolesList.Select(requiredRole => guild.GetRole(requiredRole.RoleId))
            .Select(discordRole => new DiscordOverwriteBuilder(discordRole)
                .Allow(Permissions.AccessChannels)
                .Allow(Permissions.UseVoice)
                .Allow(Permissions.Speak)
                .Allow(Permissions.SendMessages)
                .Allow(Permissions.Stream)));

        return overWriteBuildersList;
    }

    public static async Task ChannelLeave(DiscordClient client, VoiceStateUpdateEventArgs eventArgs)
    {
        if (ReferenceEquals(eventArgs.Before, null))
            return;

        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        List<LoungeDbRecord> loungeList;

        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();

            var loungeRecords = await mySqlConnection.GetAllAsync<LoungeDbRecord>();
            await mySqlConnection.CloseAsync();

            loungeList = loungeRecords.ToList();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex,"Error while Operating on the MySql Database in ChannelLeave Task on VoiceStateUpdated Event in LoungeSystem");
            return;
        }
        
        foreach (var loungeDbModel in loungeList)
        {
            if (ReferenceEquals(eventArgs.Before.Channel,null))
                return;
            
            if (loungeDbModel.ChannelId != eventArgs.Before.Channel.Id)
                continue;
            
            if (eventArgs.Before.Channel.Users.Count != 0)
                return;
            
            await CleanupLounge.Execute(loungeDbModel);

        }
    }
    
}