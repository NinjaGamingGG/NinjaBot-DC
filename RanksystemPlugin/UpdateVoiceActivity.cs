using DSharpPlus;
using RankSystem;
using Ranksystem.Ranksystem;

namespace Ranksystem;

public static class UpdateVoiceActivity
{
    private static readonly PeriodicTimer ChannelActivityUpdateTimer = new(TimeSpan.FromSeconds(5));
    

    
    public static async Task Update(DiscordClient client)
    {
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
                if(Blacklist.CheckUserGroups(membersAsArray[i].Roles.ToArray(), guild))
                    continue;

                var userChannel = membersAsArray[i].VoiceState.Channel;
                if (RankSystemPlugin.BlacklistedChannels.Contains(userChannel.Id))
                    continue;
                
                //Check if parent channel is blacklisted (most likely a category)
                if(RankSystemPlugin.BlacklistedChannels.Contains(userChannel.Parent.Id))
                    continue;

                var user = membersAsArray[i];
                await RankSystemPlugin.AddUserPoints(client, RankSystemPlugin.PointsPerVoiceActivity, 
                    $"User {user.Mention} earned {RankSystemPlugin.PointsPerVoiceActivity}xp for being active in voiceChannel {userChannel.Mention}", 
                    RankSystemPlugin.ERankSystemReason.ChannelVoiceActivity);
            }
        }
    }
    

}