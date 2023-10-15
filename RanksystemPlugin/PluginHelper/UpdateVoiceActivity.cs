using DSharpPlus;
using DSharpPlus.Entities;
using RankSystem;

namespace Ranksystem.PluginHelper;

public static class UpdateVoiceActivity
{
    private static readonly PeriodicTimer ChannelActivityUpdateTimer = new(TimeSpan.FromMinutes(1));
    

    
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

        foreach (var member in members)
        {
            if (ReferenceEquals(member.VoiceState,null))
                continue;
                
            //Check if member is in any blacklisted groups
            if(Blacklist.CheckUserGroups(member.Roles.ToArray(), guild.Value))
                continue;

            var userChannel = member.VoiceState.Channel;
            if (Blacklist.CheckUserChannel(userChannel))
                continue;
                
            //Check if parent channel is blacklisted (most likely a category)
            if (Blacklist.CheckUserChannel(userChannel.Parent))
                continue;


            await UpdateUserPoints.Add(client,guild.Key ,member, 
                RankSystemPlugin.ERankSystemReason.ChannelVoiceActivity);

            await UpdateRewardRole.ForUserAsync(client, guild.Key, member.Id);
        }
    }
    

}