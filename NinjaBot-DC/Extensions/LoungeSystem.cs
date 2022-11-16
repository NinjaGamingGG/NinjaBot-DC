using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace NinjaBot_DC.Extensions;

public static class LoungeSystem
{
    public static async Task VoiceStateUpdated_ChanelEnter(DiscordClient client, VoiceStateUpdateEventArgs eventArgs)
    {
        if ( eventArgs.Channel == null || eventArgs.Channel.Id != 1040993809368109066)
            return;

        await  Task.Run(async () =>
        {
            DiscordChannel? newChannel = null;
            
            newChannel = await eventArgs.Channel.Guild.CreateVoiceChannelAsync($"╠🥳» {eventArgs.User.Username}'s Lounge",
                eventArgs.Channel.Parent, 128000, position: 9999, user_limit: 4);
            
            if (newChannel == null)
                return;
            
            eventArgs.Channel.Guild.Members.TryGetValue(eventArgs.User.Id, out var discordMember);
            if (discordMember == null)
                return;
            
            await discordMember.PlaceInAsync(newChannel);

            await newChannel.AddOverwriteAsync(discordMember, Permissions.ManageChannels);
            await newChannel.AddOverwriteAsync(discordMember, Permissions.AccessChannels);
            await newChannel.AddOverwriteAsync(discordMember, Permissions.UseVoice);
        });
    }
    
    public static async Task VoiceStateUpdated_ChanelLeave(DiscordClient client, VoiceStateUpdateEventArgs eventArgs)
    {
        if ( eventArgs.Before == null || !eventArgs.Before.Channel.Name.Contains("🥳"))
            return;
            
        if (eventArgs.Before.Channel.Users.Count != 0)
            return;

        await Task.Run(async () =>
        {
            await eventArgs.Before.Channel.DeleteAsync();
        });
    }
}