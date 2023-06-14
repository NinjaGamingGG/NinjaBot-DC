using DSharpPlus;
using DSharpPlus.EventArgs;
using RankSystem;
using Ranksystem.PluginHelper;

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
        if (Blacklist.CheckUserChannel(eventArgs.Channel))
            return;
        
        //Check if parent channel is blacklisted (most likely a category)
        if (Blacklist.CheckUserChannel(eventArgs.Channel.Parent))
            return;
        
        //Apply exp rewards
        await UpdateUserPoints.Add(client,eventArgs.Guild.Id, user, RankSystemPlugin.ERankSystemReason.ChannelMessageAdded);
    }
}