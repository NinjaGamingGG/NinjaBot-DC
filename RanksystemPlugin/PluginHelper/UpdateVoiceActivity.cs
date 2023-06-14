using DSharpPlus;
using DSharpPlus.Entities;
using RankSystem;

namespace Ranksystem.PluginHelper;

public static class UpdateVoiceActivity
{
    private static readonly PeriodicTimer ChannelActivityUpdateTimer = new(TimeSpan.FromSeconds(5));
    

    
    public static async Task Update(DiscordClient client)
    {

        while (await ChannelActivityUpdateTimer.WaitForNextTickAsync())
        {
            var guilds = client.Guilds;

            foreach (var guild in guilds)
            {
                
                await UpdateForGuild(guild, client);

            }
        }
    }

    private static async Task UpdateForGuild(KeyValuePair<ulong, DiscordGuild> guild, DiscordClient client)
    {
        var members = await guild.Value.GetAllMembersAsync();
        var membersAsArray = members.ToArray();

        for (var i = 0; i < membersAsArray.Length; i++)
        {
            if (membersAsArray[i].VoiceState == null)
                continue;
                
            //Check if member is in any blacklisted groups
            if(Blacklist.CheckUserGroups(membersAsArray[i].Roles.ToArray(), guild.Value))
                continue;

            var userChannel = membersAsArray[i].VoiceState.Channel;
            if (Blacklist.CheckUserChannel(userChannel))
                continue;
                
            //Check if parent channel is blacklisted (most likely a category)
            if (Blacklist.CheckUserChannel(userChannel.Parent))
                continue;

            var user = membersAsArray[i];
            await UpdateUserPoints.Add(client,guild.Key ,user, 
                RankSystemPlugin.ERankSystemReason.ChannelVoiceActivity);

            await UpdateRewardRole.ForUserAsync(client, guild.Key, user.Id);
        }
    }
    

}