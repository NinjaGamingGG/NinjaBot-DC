using DSharpPlus;
using DSharpPlus.EventArgs;
using RankSystem;
using Ranksystem.PluginHelper;

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
        if (Blacklist.CheckUserChannel(eventArgs.Channel))
            return;

        //Check if parent channel is blacklisted (most likely a category)
        if (Blacklist.CheckUserChannel(eventArgs.Channel.Parent))
            return;

        //Apply exp rewards
        await UpdateUserPoints.Add(client,eventArgs.Guild.Id, user,
            RankSystemPlugin.ERankSystemReason.MessageReactionAdded);
        
    }
    
}