using Dapper;
using DSharpPlus;
using DSharpPlus.Entities;
using NinjaBot_DC;

namespace Ranksystem.RanksystemHelper;

public static class Blacklist
{
    public static bool CheckUserGroups(DiscordRole[] userRolesAsArray, DiscordGuild guild)
    {
        var sqliteConnection = Worker.GetServiceSqLiteConnection();

        var blacklistedRoles = sqliteConnection.Query($"SELECT RoleId FROM RanksystemBlacklistedRolesIndex WHERE GuildId = {guild.Id} ").ToArray();
        
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
        
        
        var sqliteConnection = Worker.GetServiceSqLiteConnection();

        var blacklistedChannels = sqliteConnection.Query($"SELECT ChannelId FROM RanksystemBlacklistedChannelsIndex WHERE GuildId = {userChannel.GuildId} ").ToArray();
        
        var blacklistedChannelsIds = blacklistedChannels.Select(t => (ulong) t.ChannelId).ToArray();
        
        
        if (blacklistedChannelsIds.Contains(userChannel.Id))
            return true;

        return false;
    }
}