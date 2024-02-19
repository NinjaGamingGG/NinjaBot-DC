using Dapper;
using DSharpPlus.Entities;
using RankSystem;

namespace RankSystem.PluginHelper;

public static class Blacklist
{
    public static bool CheckUserGroups(DiscordRole[] userRolesAsArray, DiscordGuild guild)
    {
        var sqlConnection = RankSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();

        var blacklistedRoles = sqlConnection.Query($"SELECT RoleId FROM RanksystemBlacklistedRolesIndex WHERE GuildId = {guild.Id} ").ToArray();
        
        var blacklistedRolesIds = blacklistedRoles.Select(t => (ulong) t.RoleId).ToArray();
        
        for (var r = 0; r < userRolesAsArray.Length; r++)
        {

            if (blacklistedRolesIds.Contains(userRolesAsArray[r].Id))
                return true;
        }

        return false;
    }
    
    public static bool CheckUserChannel(DiscordChannel userChannel)
    {
        
        
        var sqlConnection = RankSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();

        var blacklistedChannels = sqlConnection.Query($"SELECT ChannelId FROM RanksystemBlacklistedChannelsIndex WHERE GuildId = {userChannel.GuildId} ").ToArray();
        
        var blacklistedChannelsIds = blacklistedChannels.Select(t => (ulong) t.ChannelId).ToArray();
        
        
        if (blacklistedChannelsIds.Contains(userChannel.Id))
            return true;

        return false;
    }
}