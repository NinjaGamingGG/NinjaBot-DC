using Dapper;
using DSharpPlus.Entities;
using MySqlConnector;
using Serilog;

namespace RankSystem.PluginHelper;

public static class UserRankDisplayHelper
{
    public static async Task<DiscordWebhookBuilder> BuildDisplayAsync(DiscordGuild guild, DiscordUser targetUser, bool isUserTarget)
    {
        var mysqlConnectionString = RankSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        int userPoints;
        try
        {
            var mysqlConnection = new MySqlConnection(mysqlConnectionString);
            await mysqlConnection.OpenAsync();

            userPoints = await mysqlConnection.ExecuteScalarAsync<int>(
                "SELECT Points FROM RankSystem.RankSystemUserPointsIndex WHERE GuildId= @GuildId AND UserId= @UserId",
                new { GuildId =guild.Id, UserId = targetUser.Id });
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Error while retrieving user points from database in RankSystem RankSlashCommandModule");
            return new DiscordWebhookBuilder();
        }

        if (isUserTarget)
            return new DiscordWebhookBuilder().WithContent($"Your Points are: {userPoints}");
        
        return new DiscordWebhookBuilder().WithContent($"The Points of {targetUser.Username} are: {userPoints}");
    }
}