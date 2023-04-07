using Dapper;
using DSharpPlus.Entities;
using NinjaBot_DC;

namespace Ranksystem.RanksystemHelper;

public static class Blacklist
{
    public static bool CheckUserGroups(DiscordRole[] userRolesAsArray, DiscordGuild guild)
    {
        var sqliteConnection = Worker.GetServiceSqLiteConnection();

        var blacklistedRoles = sqliteConnection.Query($"SELECT RoleId FROM BlacklistedRolesIndex WHERE GuildId = {guild.Id} ").ToArray();
        
        var blacklistedRolesIds = blacklistedRoles.Select(t => (ulong) t.RoleId).ToArray();
        
        for (var r = 0; r < userRolesAsArray.Length; r++)
        {

            if (blacklistedRolesIds.Contains(userRolesAsArray[r].Id))
                return true;
        }

        return false;
    }
}