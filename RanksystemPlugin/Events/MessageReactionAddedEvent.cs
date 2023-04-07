using DSharpPlus;
using DSharpPlus.EventArgs;
using RankSystem;
using Ranksystem.RanksystemHelper;

namespace Ranksystem.Events;

public static class MessageReactionAddedEvent
{
    public static async Task MessageReactionAdded(DiscordClient client, MessageReactionAddEventArgs eventArgs)
    {
        //Check if reaction is valid (no reaction to bot message, etc)
        if (eventArgs.User.Id == client.CurrentUser.Id)
            return;

        //Get the member that added the reaction
        var guild = await client.GetGuildAsync(eventArgs.Guild.Id);
        var user = await guild.GetMemberAsync(eventArgs.User.Id);

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
            $"User {user.Mention} earned {RankSystemPlugin.PointsPerReaction}xp for reacting to message {eventArgs.Message.Id} in Channel {eventArgs.Channel.Mention}",
            RankSystemPlugin.ERankSystemReason.MessageReactionAdded);
        
    }
    
}