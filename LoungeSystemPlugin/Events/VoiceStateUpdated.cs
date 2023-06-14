using Dapper;
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
                .AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "lounge_test",
                    DiscordEmoji.FromName(client, ":black_nib:") + " Edit Name"));

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