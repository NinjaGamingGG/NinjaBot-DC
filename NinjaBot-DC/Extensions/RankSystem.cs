using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace NinjaBot_DC.Extensions;

public static class RankSystem
{
    private static readonly PeriodicTimer ChannelActivityUpdateTimer = new(TimeSpan.FromSeconds(5));

    //ToDo: Make all settings dynamic per guild
    private const int PointsPerMessage = 5;
    private const int PointsPerReaction = 2;
    private const float PointsPerVoiceActivity = 1.2f;
    private const ulong LogChannel = 1041009856175411250;
    private static readonly ulong[] BlacklistedChannels = new ulong[] {1041105089185718334, 1041000929270452274};
    private static readonly ulong[] BlacklistedGroups = new ulong[] {1040990856284479520 }; 


    public static async Task MessageCreatedEvent(DiscordClient client, MessageCreateEventArgs eventArgs)
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
        if(CheckUserGroupsForBlacklisted(user.Roles.ToArray()))
            return;

        //Check if message was send in blacklisted channel
        if (BlacklistedChannels.Contains(eventArgs.Channel.Id))
            return;
        //Check if parent channel is blacklisted (most likely a category)
        if(BlacklistedChannels.Contains(eventArgs.Channel.Parent.Id))
            return;
        
        //Apply exp rewards
        var logChannel = eventArgs.Guild.GetChannel(LogChannel);
        await AddUserPoints(client, PointsPerVoiceActivity, 
            $"User {user.Mention} earned {PointsPerVoiceActivity}xp for creating message {eventArgs.Message.Id} in Channel {eventArgs.Channel.Mention}",
            ERankSystemReason.ChannelMessageAdded);

    }
    
    public static async Task ReactionAddedEvent(DiscordClient client, MessageReactionAddEventArgs eventArgs)
    {
        //Check if reaction is valid (no reaction to bot message, etc)
        if (eventArgs.User.Id == client.CurrentUser.Id)
            return;

        //Get the member that added the reaction
        var guild = await client.GetGuildAsync(eventArgs.Guild.Id);
        var user = await guild.GetMemberAsync(eventArgs.User.Id);

        //Check if member is in any blacklisted groups
        if(CheckUserGroupsForBlacklisted(user.Roles.ToArray()))
            return;
        
        //Check if message was send in blacklisted channel
        if (BlacklistedChannels.Contains(eventArgs.Channel.Id))
            return;

        //Check if parent channel is blacklisted (most likely a category)
        if(BlacklistedChannels.Contains(eventArgs.Channel.Parent.Id))
            return;
        
        //Apply exp rewards
        var logChannel = eventArgs.Guild.GetChannel(LogChannel);
        await AddUserPoints(client, PointsPerVoiceActivity, 
            $"User {user.Mention} earned {PointsPerReaction}xp for reacting to message {eventArgs.Message.Id} in Channel {eventArgs.Channel.Mention}",
            ERankSystemReason.MessageReactionAdded);
    }

    public static async Task UpdateVoiceActivity()
    {
        var client = Worker.GetServiceDiscordClient();
        var guild = await client.GetGuildAsync(1039518370015490169);

        while (await ChannelActivityUpdateTimer.WaitForNextTickAsync())
        {
            var members = await guild.GetAllMembersAsync();
            var membersAsArray = members.ToArray();

            for (var i = 0; i < membersAsArray.Length; i++)
            {
                if (membersAsArray[i].VoiceState == null)
                    continue;
                
                //Check if member is in any blacklisted groups
                if(CheckUserGroupsForBlacklisted(membersAsArray[i].Roles.ToArray()))
                    continue;

                var userChannel = membersAsArray[i].VoiceState.Channel;
                if (BlacklistedChannels.Contains(userChannel.Id))
                    continue;
                
                //Check if parent channel is blacklisted (most likely a category)
                if(BlacklistedChannels.Contains(userChannel.Parent.Id))
                    continue;

                var user = membersAsArray[i];
                await AddUserPoints(client, PointsPerVoiceActivity, 
                    $"User {user.Mention} earned {PointsPerVoiceActivity}xp for being active in voiceChannel {userChannel.Mention}", 
                    ERankSystemReason.ChannelVoiceActivity);
            }
        }

    }

    private static bool CheckUserGroupsForBlacklisted(DiscordRole[] userRolesAsArray)
    {
        for (var r = 0; r < userRolesAsArray.Length; r++)
        {
            if (BlacklistedGroups.Contains(userRolesAsArray[r].Id))
                return true;
        }

        return false;
    }

    private static async Task AddUserPoints(DiscordClient client, float pointsToAdd, string reasonMessage, ERankSystemReason reason)
    {
        if (reason == ERankSystemReason.ChannelVoiceActivity)
            return;
        
        var guild = await client.GetGuildAsync(1039518370015490169);
            
        var logChannel = guild.GetChannel(LogChannel);
        
        await logChannel.SendMessageAsync($"[Rank-system] {reasonMessage}>");
    }

    private enum ERankSystemReason {ChannelVoiceActivity, ChannelMessageAdded, MessageReactionAdded}
}
