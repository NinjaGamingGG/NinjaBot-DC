using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using RankSystem;
using RankSystem.PluginHelper;
using Serilog;

namespace RankSystem.Events;



public static class MessageCreatedEvent
{
    public static async Task MessageCreated(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        if (ReferenceEquals(eventArgs.Message.Author, null))
            return;
        
        
        //Check if it was own message
        if (eventArgs.Message.Author.Id == client.CurrentUser.Id)
            return;
        
        //Check if message is valid (no spam, long enough etc)
        var messageContent = eventArgs.Message.Content;
        if (messageContent.Length < 5)
            return;
        
        //Get the member that wrote the message
        var guild = await client.GetGuildAsync(eventArgs.Guild.Id);

        DiscordMember user;
        try
        {
            user = await guild.GetMemberAsync(eventArgs.Author.Id);
        }
        catch (DSharpPlus.Exceptions.NotFoundException exception)
        {
            Log.Error(exception, "[Ranksystem] Error could not find the User {MemberId}", eventArgs.Author.Id);
            return;
        }


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