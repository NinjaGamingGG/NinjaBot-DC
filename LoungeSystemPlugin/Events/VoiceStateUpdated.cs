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
        
        var sqliteConnection = SqLiteHelper.GetSqLiteConnection();

        var channels = await sqliteConnection.QueryAsync<LoungeSystemConfigurationRecord>("SELECT * FROM LoungeSystemConfigurationIndex WHERE GuildId = @GuildId", new { GuildId = eventArgs.Guild.Id});
        
        var channelsList = channels.ToList();
        
        var channelExists = false;
        string? channelNamePattern = null;
        ulong interfaceChannel = 0;

        foreach (var channelConfig in channelsList.Where(channelConfig => eventArgs.Channel.Id == channelConfig.TargetChannelId))
        {
            channelExists = true;
            channelNamePattern = channelConfig.LoungeNamePattern;
            interfaceChannel = channelConfig.InterfaceChannelId;
            
            if (channelNamePattern != null && channelNamePattern.Contains("{username}"))
                channelNamePattern = channelNamePattern.Replace("{username}", eventArgs.User.Username);
            
            break;
        }

        if (channelExists == false)
            return;
        
        if (ReferenceEquals(channelNamePattern, null))
            return;
        
        var newChannel = await eventArgs.Channel.Guild.CreateVoiceChannelAsync(channelNamePattern, eventArgs.Channel.Parent, 96000,4, position: eventArgs.Channel.Position + 1);
        
        if (ReferenceEquals(newChannel, null))
            return;
        
        var newModel = new LoungeDbRecord()
        {
            GuildId = eventArgs.Guild.Id,
            ChannelId = newChannel.Id,
            OwnerId = eventArgs.User.Id,
        };

        var inserted = await sqliteConnection.InsertAsync(newModel);
        
        if (inserted == 0)
        {
            await newChannel.DeleteAsync();
            return;
        }
        
        eventArgs.Channel.Guild.Members.TryGetValue(eventArgs.User.Id, out var discordMember);
        if (ReferenceEquals(discordMember, null))
            return;
        
        await discordMember.PlaceInAsync(newChannel);
        
        await  Task.Run(async () =>
        {
            await newChannel.AddOverwriteAsync(discordMember, Permissions.ManageChannels);
        });
        
        await  Task.Run(async () =>
        {
            await newChannel.AddOverwriteAsync(discordMember, Permissions.AccessChannels);
        });
        
        await  Task.Run(async () =>
        {
            await newChannel.AddOverwriteAsync(discordMember, Permissions.UseVoice);
        });
        
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
                        "Trust", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":people_hugging:"))),
                    new DiscordButtonComponent(ButtonStyle.Secondary, "lounge_claim_button",
                        "Claim", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":triangular_flag_on_post:"))),
                    new DiscordButtonComponent(ButtonStyle.Secondary, "lounge_kick_button",
                        "Kick", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":athletic_shoe:")))
                })
                .AddComponents(new DiscordComponent[]
                {
                    new DiscordButtonComponent(ButtonStyle.Secondary, "lounge_un-trust_button",
                        // ReSharper disable once StringLiteralTypo
                        "Untrust", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":bust_in_silhouette:"))),
                    new DiscordButtonComponent(ButtonStyle.Danger, "lounge_lock_button",
                        "Un/Lock", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":lock:"))),
                    new DiscordButtonComponent(ButtonStyle.Danger, "lounge_ban_button",
                        "Ban", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":judge:"))),
                    new DiscordButtonComponent(ButtonStyle.Danger, "lounge_delete_button",
                        "Delete", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":put_litter_in_its_place:"))),
                });
            
            await newChannel.SendMessageAsync(builder);

        }



    }

    public static async Task ChannelLeave(DiscordClient client, VoiceStateUpdateEventArgs eventArgs)
    {
        if ( ReferenceEquals(eventArgs.Before, null))
            return;

        var sqlite = SqLiteHelper.GetSqLiteConnection();

        var loungeList = await sqlite.GetAllAsync<LoungeDbRecord>();

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