using DSharpPlus;
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
    private static readonly ulong[] BlacklistedChannels = new ulong[] {1041105089185718334};
    
    
    public static async Task MessageCreatedEvent(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        //Check if it was own message
        if (eventArgs.Message.Author.Id == client.CurrentUser.Id)
            return;
        
        //Check if message is valid (no spam, long enough etc)
        var messageContent = eventArgs.Message.Content;
        if (messageContent.Length < 5)
            return;

        //Check if message was send in blacklisted channel
        if (BlacklistedChannels.Contains(eventArgs.Channel.Id))
            return;
        //Apply exp rewards
        var logChannel = eventArgs.Guild.GetChannel(LogChannel);
        await logChannel.SendMessageAsync($"User {eventArgs.Author.Mention} earned {PointsPerMessage}xp for message {eventArgs.Message.Id}");

    }
    
    public static async Task ReactionAddedEvent(DiscordClient client, MessageReactionAddEventArgs eventArgs)
    {
        //Check if reaction is valid (no reaction to bot message, etc)
        if (eventArgs.User.Id == client.CurrentUser.Id)
            return;

        //Check if message was send in blacklisted channel
        if (BlacklistedChannels.Contains(eventArgs.Channel.Id))
            return;
        
        //Apply exp rewards
        var logChannel = eventArgs.Guild.GetChannel(LogChannel);
        await logChannel.SendMessageAsync($"User {eventArgs.User.Mention} earned {PointsPerReaction}xp for Reaction on Message {eventArgs.Message.Id}");
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

                var userChannel = membersAsArray[i].VoiceState.Channel;
                if (BlacklistedChannels.Contains(userChannel.Id))
                    continue;

                var user = membersAsArray[i];
                await AddUserPoints(client, PointsPerVoiceActivity, $"User {user.Mention} earned {PointsPerVoiceActivity}xp for being active in voiceChannel {userChannel.Mention}");
            }
        }

    }

    private static async Task AddUserPoints(DiscordClient client, float pointsToAdd, string reason)
    {
        var guild = await client.GetGuildAsync(1039518370015490169);
            
        var logChannel = guild.GetChannel(LogChannel);
        await logChannel.SendMessageAsync(reason);
    }
}
