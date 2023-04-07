using DSharpPlus;
using DSharpPlus.EventArgs;
using RankSystem;
using Ranksystem.RanksystemHelper;

namespace Ranksystem.Events;



public static class MessageCreatedEvent
{
    public static async Task MessageCreated(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        //Check if it was own message
        if (eventArgs.Message.Author.Id == client.CurrentUser.Id)
            return;
        
        //Check if message is valid (no spam, long enough etc)
        var messageContent = eventArgs.Message.Content;
        if (messageContent.Length < 5)
            return;
        
        //Get the member that wrote the message
        var guild = await client.GetGuildAsync(eventArgs.Guild.Id);
        var user = await guild.GetMemberAsync(eventArgs.Author.Id);

        //Check if member is in any blacklisted groups
        if(Blacklist.CheckUserGroups(user.Roles.ToArray(), eventArgs.Guild))
            return;

        //Check if message was send in blacklisted channel
        if (RankSystemPlugin.BlacklistedChannels.Contains(eventArgs.Channel.Id))
            return;
        //Check if parent channel is blacklisted (most likely a category)
        if(RankSystemPlugin.BlacklistedChannels.Contains(eventArgs.Channel.Parent.Id))
            return;
        
        //Apply exp rewards
        var logChannel = eventArgs.Guild.GetChannel(RankSystemPlugin.LogChannel);
        await RankSystemPlugin.AddUserPoints(client, RankSystemPlugin.PointsPerVoiceActivity, 
            $"User {user.Mention} earned {RankSystemPlugin.PointsPerVoiceActivity}xp for creating message {eventArgs.Message.Id} in Channel {eventArgs.Channel.Mention}",
            RankSystemPlugin.ERankSystemReason.ChannelMessageAdded);
    }
}