using Dapper;
using DSharpPlus.Entities;
using MySqlConnector;
using RankSystem;
using Serilog;

namespace RankSystem.PluginHelper;

public static class Blacklist
{
    /// <summary>
    /// Checks if the user belongs to any blacklisted groups.
    /// </summary>
    /// <param name="userRolesAsArray">The array of DiscordRole objects representing the user's roles.</param>
    /// <param name="guild">The DiscordGuild object representing the guild.</param>
    /// <returns>True if the user belongs to any blacklisted roles, otherwise false.</returns>
    public static bool CheckUserGroups(DiscordRole[] userRolesAsArray, DiscordGuild guild)
    {
        ulong[] blacklistedRolesIds;
        var connectionString = RankSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        
        try
        {
            using var sqlConnection = new MySqlConnection(connectionString);

            var blacklistedRoles = sqlConnection.Query($"SELECT RoleId FROM RanksystemBlacklistedRolesIndex WHERE GuildId = {guild.Id} ").ToArray();
        
            blacklistedRolesIds = blacklistedRoles.Select(t => (ulong) t.RoleId).ToArray();
            sqlConnection.Close();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex,"Error while Querying Blacklisted roles from Database for Guild {GuildId}/{GuildName}",guild.Id,guild.Name);
            return true;
        }

        return userRolesAsArray.Any(t => blacklistedRolesIds.Contains(t.Id));
    }

    /// <summary>
    /// Checks if a user's channel is blacklisted.
    /// </summary>
    /// <param name="userChannel">The channel associated with the user.</param>
    /// <returns>
    /// <c>true</c> if the user's channel is blacklisted;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool CheckUserChannel(DiscordChannel userChannel)
    {
        ulong[] blacklistedChannelsIds;
        var connectionString = RankSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();

        try
        {
            using var sqlConnection = new MySqlConnection(connectionString);

            var blacklistedChannels = sqlConnection.Query($"SELECT ChannelId FROM RanksystemBlacklistedChannelsIndex WHERE GuildId = {userChannel.GuildId} ").ToArray();
        
            blacklistedChannelsIds = blacklistedChannels.Select(t => (ulong) t.ChannelId).ToArray();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex,"Error while Querying Blacklisted roles from Database for Channel {ChannelId}/{ChannelName} on Guild {GuildId}/{GuildName}",userChannel.Id,userChannel.Name,userChannel.Guild.Id,userChannel.Guild.Name);
            return true;
        }

        
        
        return blacklistedChannelsIds.Contains(userChannel.Id);
    }
}