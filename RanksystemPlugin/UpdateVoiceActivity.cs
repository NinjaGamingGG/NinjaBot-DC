using DSharpPlus;
using DSharpPlus.Entities;
using Ranksystem;

namespace PluginBase;

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
                if(RanksystemPlugin.CheckUserGroupsForBlacklisted(membersAsArray[i].Roles.ToArray()))
                    continue;

                var userChannel = membersAsArray[i].VoiceState.Channel;
                if (RanksystemPlugin.BlacklistedChannels.Contains(userChannel.Id))
                    continue;
                
                //Check if parent channel is blacklisted (most likely a category)
                if(RanksystemPlugin.BlacklistedChannels.Contains(userChannel.Parent.Id))
                    continue;

                var user = membersAsArray[i];
                await RanksystemPlugin.AddUserPoints(client, RanksystemPlugin.PointsPerVoiceActivity, 
                    $"User {user.Mention} earned {RanksystemPlugin.PointsPerVoiceActivity}xp for being active in voiceChannel {userChannel.Mention}", 
                    RanksystemPlugin.ERankSystemReason.ChannelVoiceActivity);
            }
        }
    }
    

}