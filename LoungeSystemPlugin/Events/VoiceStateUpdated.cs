﻿using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.Records;

namespace LoungeSystemPlugin.Events;

public static class VoiceStateUpdated
{
    public static async Task ChannelEnter(DiscordClient client, VoiceStateUpdateEventArgs eventArgs)
    {
        if (ReferenceEquals(eventArgs.Channel, null))
            return;
        
        var mySqlConnection = LoungeSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();

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
        if (loungeMessageReplacementsAsArray.Any())
        {
            foreach (var replacement in loungeMessageReplacementsAsArray)
            {
                switch (replacement.ReplacementHandle)
                {
                    case"Separator":
                        separatorPattern = replacement.ReplacementValue;
                        break;
                    
                    case"CustomName":
                        customNamePattern = replacement.ReplacementValue;
                        break;
                    
                    case"DecoratorDecal":
                        decoratorDecal = replacement.ReplacementValue;
                        break;
                    
                    case"DecoratorEmoji":
                        decoratorEmoji = replacement.ReplacementValue;
                        break;
                    
                    case"DecoratorPrefix":
                        decoratorPrefix = replacement.ReplacementValue;
                        break;
                }

            }
        }
        
        if (!ReferenceEquals(separatorPattern, null) && separatorPattern.Contains("{decoratordecal}"))
            separatorPattern = separatorPattern.Replace("{decoratordecal}", decoratorDecal);
        
        if (!ReferenceEquals(separatorPattern, null) && separatorPattern.Contains("{decoratoremoji}"))
            separatorPattern = separatorPattern.Replace("{decoratoremoji}", decoratorEmoji);
        if (!ReferenceEquals(separatorPattern, null) && separatorPattern.Contains("{decoratorprefix}"))
            separatorPattern = separatorPattern.Replace("{decoratorprefix}", decoratorPrefix);
        
        if (!ReferenceEquals(customNamePattern, null) && customNamePattern.Contains("{username}"))
            customNamePattern = customNamePattern.Replace("{username}", eventArgs.User.Username);

        
        ulong interfaceChannel = 0;

        foreach (var channelConfig in channelsList.Where(channelConfig => eventArgs.Channel.Id == channelConfig.TargetChannelId))
        {
            channelExists = true;
            channelNamePattern = channelConfig.LoungeNamePattern;
            interfaceChannel = channelConfig.InterfaceChannelId;

            

            //if (channelNamePattern != null && channelNamePattern.Contains("{username}"))
            //    channelNamePattern = channelNamePattern.Replace("{username}", eventArgs.User.Username);
            if (!ReferenceEquals(channelNamePattern, null) && channelNamePattern.Contains("{separator}"))
                channelNamePattern = channelNamePattern.Replace("{separator}", separatorPattern);
        
            if (!ReferenceEquals(channelNamePattern, null) && channelNamePattern.Contains("{customname}"))
                channelNamePattern = channelNamePattern.Replace("{customname}", customNamePattern);
            
            break;
        }

        if (channelExists == false)
            return;
        
        if (ReferenceEquals(channelNamePattern, null))
            return;
        
        eventArgs.Channel.Guild.Members.TryGetValue(eventArgs.User.Id, out var discordMember);
        if (ReferenceEquals(discordMember, null))
            return;

        var overWriteBuildersList = new List<DiscordOverwriteBuilder>();
        
        overWriteBuildersList.Add(      
            new DiscordOverwriteBuilder(discordMember)
            .Allow(Permissions.AccessChannels)
            .Allow(Permissions.UseVoice)
            .Allow(Permissions.Speak)
            .Allow(Permissions.SendMessages)
            .Allow(Permissions.Stream)
            .Allow(Permissions.PrioritySpeaker));
        
        var requiredRoles = await mySqlConnection.QueryAsync<RequiredRoleRecord>("SELECT * FROM RequiredRoleIndex WHERE GuildId = @GuildId AND ChannelId = @ChannelId", new { GuildId = eventArgs.Guild.Id, ChannelId = eventArgs.Channel.Id});
        var requiredRolesList = requiredRoles.ToList();


        if (!requiredRolesList.Any())
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

        var inserted = await mySqlConnection.InsertAsync(newModel);
        
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
                .AddComponents(new DiscordComponent[]
                {
                    new DiscordButtonComponent(ButtonStyle.Secondary, "lounge_rename_button",
                          "Rename",false, new DiscordComponentEmoji( DiscordEmoji.FromName(client, ":black_nib:"))),
                    new DiscordButtonComponent(ButtonStyle.Secondary, "lounge_resize_button",
                        "Resize", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":busts_in_silhouette:"))),
                    new DiscordButtonComponent(ButtonStyle.Secondary, "lounge_trust_button",
                        "Trust", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":people_hugging:")))
                })
                .AddComponents(new DiscordComponent[]
                {
                    new DiscordButtonComponent(ButtonStyle.Secondary, "lounge_claim_button",
                        "Claim", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":triangular_flag_on_post:"))),
                    new DiscordButtonComponent(ButtonStyle.Secondary, "lounge_kick_button",
                        "Kick", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":athletic_shoe:"))),
                    new DiscordButtonComponent(ButtonStyle.Secondary, "lounge_un-trust_button",
                        // ReSharper disable once StringLiteralTypo
                        "Untrust", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":bust_in_silhouette:")))
                })
                .AddComponents(new DiscordComponent[]
                {
                    new DiscordButtonComponent(ButtonStyle.Danger, "lounge_lock_button",
                        "Un/Lock", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":lock:"))),
                    new DiscordButtonComponent(ButtonStyle.Danger, "lounge_ban_button",
                        "Ban", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":judge:"))),
                    new DiscordButtonComponent(ButtonStyle.Danger, "lounge_delete_button",
                        "Delete", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":put_litter_in_its_place:"))),
                });
            
            var interfaceMessage = await newChannel.SendMessageAsync(builder);

        }



    }

    public static async Task ChannelLeave(DiscordClient client, VoiceStateUpdateEventArgs eventArgs)
    {
        if ( ReferenceEquals(eventArgs.Before, null))
            return;

        var mySqlConnection = LoungeSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();

        var loungeList = await mySqlConnection.GetAllAsync<LoungeDbRecord>();

        foreach (var loungeDbModel in loungeList)
        {
            if (loungeDbModel.ChannelId != eventArgs.Before.Channel.Id)
                continue;
            
            if (eventArgs.Before.Channel.Users.Count != 0)
                return;
            
            await CleanupLounge.Execute(loungeDbModel);

        }
    }
    
}