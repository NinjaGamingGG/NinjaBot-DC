using Dapper;
using DSharpPlus;
using DSharpPlus.Entities;
using LoungeSystemPlugin.Records;
using MySqlConnector;
using NinjaBot_DC;
using Serilog;

namespace LoungeSystemPlugin.PluginHelper;

public static class NewLoungeHelper
{
    public static async Task CreateNewLounge(DiscordGuild guild, DiscordChannel originalChannel, DiscordUser owningUser)
    {
        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        
        var channelExists = false;

        var customNamePattern = string.Empty;

        List<LoungeSystemConfigurationRecord> channelsList;
        LoungeMessageReplacement[]? loungeMessageReplacementsAsArray;

        try
        {
            var mySqlConnection = new MySqlConnection(connectionString);
            
            var channels = await mySqlConnection.QueryAsync<LoungeSystemConfigurationRecord>("SELECT * FROM LoungeSystem.LoungeSystemConfigurationIndex WHERE GuildId = @GuildId", new { GuildId = guild.Id});
        
            var nameReplacementRecord = await mySqlConnection.QueryAsync<LoungeMessageReplacement>("SELECT * FROM LoungeSystem.LoungeMessageReplacementIndex WHERE GuildId= @GuildId AND ChannelId = @ChannelId", new {GuildId = guild.Id, ChannelId = originalChannel.Id});
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
            customNamePattern = customNamePattern.Replace("{username}", owningUser.Username);

        if (!ReferenceEquals(owningUser.Presence.Activity.Name,null))
        {
            if (!ReferenceEquals(customNamePattern, null) && customNamePattern.Contains("{activity}"))
                customNamePattern = customNamePattern.Replace("{activity}", owningUser.Presence.Activity.Name);
        }
        


        if (ReferenceEquals(customNamePattern, null))
            return;
        
        ulong interfaceChannel = 0;

        foreach (var channelConfig in channelsList.Where(channelConfig => originalChannel.Id == channelConfig.TargetChannelId))
        {
            channelExists = true;
            interfaceChannel = channelConfig.InterfaceChannelId;
            
            break;
        }

        if (channelExists == false)
            return;
        
        originalChannel.Guild.Members.TryGetValue(owningUser.Id, out var discordMember);
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

        var roleSpecificOverrides = await NewLoungeHelper.BuildOverwritesForRequiredRoles(guild.Id, originalChannel.Id);
        overWriteBuildersList.AddRange(roleSpecificOverrides);

        
        var channelNamePattern =
            await ChannelNameBuilder.BuildAsync(guild.Id, originalChannel.Id,
                customNamePattern);
        
        var newChannel = await originalChannel.Guild.CreateVoiceChannelAsync(channelNamePattern, originalChannel.Parent, originalChannel.Bitrate,4, position: originalChannel.Position + 1, overwrites:overWriteBuildersList);
        
        if (ReferenceEquals(newChannel, null))
            return;
        
        var newModel = new LoungeDbRecord()
        {
            GuildId = guild.Id,
            ChannelId = newChannel.Id,
            OwnerId = owningUser.Id,
            IsPublic = true,
            OriginChannel = originalChannel.Id
        };

        int inserted;
        
        try
        {
            var mySqlConnection = new MySqlConnection(connectionString);
            inserted = await mySqlConnection.ExecuteAsync("INSERT INTO LoungeSystem.LoungeIndex (ChannelId, GuildId, OwnerId, IsPublic, OriginChannel) VALUES (@ChannelId,@GuildId,@OwnerId,@IsPublic,@OriginChannel)",newModel);
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

        var discordClient = Worker.GetServiceDiscordClient();
            
        var builder = InterfaceMessageBuilder.GetBuilder(discordClient,discordMember.Mention + " this is your lounge Interface");
        await newChannel.SendMessageAsync(builder);
    }
    
    private static async Task<List<DiscordOverwriteBuilder>> BuildOverwritesForRequiredRoles(ulong guildId, ulong channelId)
    {
        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        var mySqlConnection = new MySqlConnection(connectionString);
        
        var requiredRoles = await mySqlConnection.QueryAsync<RequiredRoleRecord>("SELECT * FROM LoungeSystem.RequiredRoleIndex WHERE GuildId = @GuildId AND ChannelId = @ChannelId", new { GuildId = guildId, ChannelId = channelId});
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
}