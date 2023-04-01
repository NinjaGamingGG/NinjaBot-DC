using Dapper;
using DSharpPlus.Entities;
using NinjaBot_DC;

namespace Ranksystem.Ranksystem;

public class Blacklist
{
    public static bool CheckUserGroups(DiscordRole[] userRolesAsArray, DiscordGuild guild)
    {
        var sqliteConnection = Worker.GetServiceSqLiteConnection();

        var blacklistedRoles = sqliteConnection.Query($"SELECT RoleId FROM BlacklistedRolesIndex WHERE GuildId = {guild.Id} ").ToArray();
        
        var blacklistedRolesIds = new List<ulong>();

        for (var i = 0; i < blacklistedRoles.Length; i++)
        {
            blacklistedRolesIds.Add((ulong)blacklistedRoles[i].RoleId); 
        }
        

        for (var r = 0; r < userRolesAsArray.Length; r++)
        {

            if (blacklistedRolesIds.Contains(userRolesAsArray[r].Id))
                return true;
        }

        return false;
    }
}