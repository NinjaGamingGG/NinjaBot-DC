using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.Records;
using MySqlConnector;
using Serilog;

namespace LoungeSystemPlugin.Events;

public static class VoiceStateUpdated
{
    public static async Task ChannelEnter(DiscordClient client, VoiceStateUpdateEventArgs eventArgs)
    {
        if (ReferenceEquals(eventArgs.Channel, null))
            return;

        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        var mySqlConnection = new MySqlConnection(connectionString);
        await mySqlConnection.OpenAsync();

        var channels = await mySqlConnection.QueryAsync<LoungeSystemConfigurationRecord>("SELECT * FROM LoungeSystemConfigurationIndex WHERE GuildId = @GuildId", new { GuildId = eventArgs.Guild.Id});
        
        var channelsList = channels.ToList();
        
        var channelExists = false;
        var channelNamePattern = string.Empty;

        var customNamePattern = string.Empty;
        var separatorPattern = string.Empty;
        var decoratorPrefix = string.Empty;
        var decoratorEmoji = string.Empty;
        var decoratorDecal = string.Empty;

        var nameReplacementRecord = await mySqlConnection.QueryAsync<LoungeMessageReplacement>("SELECT * FROM LoungeMessageReplacementIndex WHERE GuildId= @GuildId AND ChannelId = @ChannelId", new {GuildId = eventArgs.Guild.Id, ChannelId = eventArgs.Channel.Id});

        var loungeMessageReplacementsAsArray = nameReplacementRecord as LoungeMessageReplacement[] ?? nameReplacementRecord.ToArray();
        if (loungeMessageReplacementsAsArray.Length != 0)
        {
            foreach (var replacement in loungeMessageReplacementsAsArray)
            {
                switch (replacement.ReplacementHandle)
                {
                    case"Separator":
                        separatorPattern = replacement.ReplacementValue;
                        break;
                    
                    case"Custom_Name":
                        customNamePattern = replacement.ReplacementValue;
                        break;
                    
                    case"Decorator_Decal":
                        decoratorDecal = replacement.ReplacementValue;
                        break;
                    
                    case"Decorator_Emoji":
                        decoratorEmoji = replacement.ReplacementValue;
                        break;
                    
                    case"Decorator_Prefix":
                        decoratorPrefix = replacement.ReplacementValue;
                        break;
                }

            }
        }
        
        if (!ReferenceEquals(separatorPattern, null) && separatorPattern.Contains("{Decorator_Decal}"))
            separatorPattern = separatorPattern.Replace("{Decorator_Decal}", decoratorDecal);
        
        if (!ReferenceEquals(separatorPattern, null) && separatorPattern.Contains("{Decorator_Emoji}"))
            separatorPattern = separatorPattern.Replace("{Decorator_Emoji}", decoratorEmoji);
        if (!ReferenceEquals(separatorPattern, null) && separatorPattern.Contains("{Decorator_Prefix}"))
            separatorPattern = separatorPattern.Replace("{Decorator_Prefix}", decoratorPrefix);
        
        if (!ReferenceEquals(customNamePattern, null) && customNamePattern.Contains("{username}"))
            customNamePattern = customNamePattern.Replace("{username}", eventArgs.User.Username);

        
        ulong interfaceChannel = 0;

        foreach (var channelConfig in channelsList.Where(channelConfig => eventArgs.Channel.Id == channelConfig.TargetChannelId))
        {
            channelExists = true;
            channelNamePattern = channelConfig.LoungeNamePattern;
            interfaceChannel = channelConfig.InterfaceChannelId;

            if (!ReferenceEquals(channelNamePattern, null) && channelNamePattern.Contains("{Separator}"))
                channelNamePattern = channelNamePattern.Replace("{Separator}", separatorPattern);
        
            if (!ReferenceEquals(channelNamePattern, null) && channelNamePattern.Contains("{Custom_Name}"))
                channelNamePattern = channelNamePattern.Replace("{Custom_Name}", customNamePattern);
            
            break;
        }

        if (channelExists == false)
            return;
        
        if (ReferenceEquals(channelNamePattern, null))
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

        var requiredRoles = await mySqlConnection.QueryAsync<RequiredRoleRecord>("SELECT * FROM RequiredRoleIndex WHERE GuildId = @GuildId AND ChannelId = @ChannelId", new { GuildId = eventArgs.Guild.Id, ChannelId = eventArgs.Channel.Id});
        var requiredRolesList = requiredRoles.ToList();


        if (requiredRolesList.Count == 0)
            requiredRolesList.Add(new RequiredRoleRecord{RoleId = eventArgs.Guild.EveryoneRole.Id});
        else
            overWriteBuildersList.Add(new DiscordOverwriteBuilder(eventArgs.Guild.EveryoneRole)
                .Deny(Permissions.AccessChannels)
                .Deny(Permissions.SendMessages)
                .Deny(Permissions.UseVoice)
                .Deny(Permissions.Speak)
                .Deny(Permissions.Stream));


        overWriteBuildersList.AddRange(requiredRolesList.Select(requiredRole => eventArgs.Guild.GetRole(requiredRole.RoleId))
            .Select(discordRole => new DiscordOverwriteBuilder(discordRole)
                .Allow(Permissions.AccessChannels)
                .Allow(Permissions.UseVoice)
                .Allow(Permissions.Speak)
                .Allow(Permissions.SendMessages)
                .Allow(Permissions.Stream)));

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

        var inserted = await mySqlConnection.ExecuteAsync("INSERT INTO LoungeIndex (ChannelId, GuildId, OwnerId, IsPublic, OriginChannel) VALUES (@ChannelId,@GuildId,@OwnerId,@IsPublic,@OriginChannel)",newModel);
        await mySqlConnection.CloseAsync();
        
        if (inserted == 0)
        {
            await newChannel.DeleteAsync();
            return;
        }

        await discordMember.PlaceInAsync(newChannel);
        
        await newChannel.SendMessageAsync("Welcome to your Lounge!");

        if (interfaceChannel == 0)
        {

            var builder = new DiscordMessageBuilder()
                .WithContent(discordMember.Mention + " this is your lounge Interface")
                .AddComponents([
                    new DiscordButtonComponent(ButtonStyle.Secondary, "lounge_rename_button",
                          "Rename",false, new DiscordComponentEmoji( DiscordEmoji.FromName(client, ":black_nib:"))),
                    new DiscordButtonComponent(ButtonStyle.Secondary, "lounge_resize_button",
                        "Resize", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":busts_in_silhouette:"))),
                    new DiscordButtonComponent(ButtonStyle.Secondary, "lounge_trust_button",
                        "Trust", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":people_hugging:")))
                ])
                .AddComponents([
                    new DiscordButtonComponent(ButtonStyle.Secondary, "lounge_claim_button",
                        "Claim", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":triangular_flag_on_post:"))),
                    new DiscordButtonComponent(ButtonStyle.Secondary, "lounge_kick_button",
                        "Kick", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":athletic_shoe:"))),
                    new DiscordButtonComponent(ButtonStyle.Secondary, "lounge_un-trust_button",
                        // ReSharper disable once StringLiteralTypo
                        "Untrust", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":bust_in_silhouette:")))
                ])
                .AddComponents([
                    new DiscordButtonComponent(ButtonStyle.Danger, "lounge_lock_button",
                        "Un/Lock", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":lock:"))),
                    new DiscordButtonComponent(ButtonStyle.Danger, "lounge_ban_button",
                        "Ban", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":judge:"))),
                    new DiscordButtonComponent(ButtonStyle.Danger, "lounge_delete_button",
                        "Delete", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":put_litter_in_its_place:")))
                ]);
            
            await newChannel.SendMessageAsync(builder);

        }



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