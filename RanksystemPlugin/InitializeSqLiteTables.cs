using NinjaBot_DC;
using Serilog;

namespace Ranksystem;

public static class InitializeSqLiteTables
{
    public static void Init()
    {
        Log.Information("[RankSystem] Initializing SQLite Tables...");
        var sqLiteConnection = Worker.GetServiceSqLiteConnection();

        using var sqLiteBlackListedChannelTableCommand = sqLiteConnection.CreateCommand();
        {
            sqLiteBlackListedChannelTableCommand.CommandText =
                "CREATE TABLE IF NOT EXISTS RanksystemBlacklistedChannelsIndex (GuildId INTEGER, ChannelId INTEGER)";

            sqLiteBlackListedChannelTableCommand.ExecuteNonQuery();
        }
        
        using var sqLiteBlackListedRoleTableCommand = sqLiteConnection.CreateCommand();
        {
            sqLiteBlackListedRoleTableCommand.CommandText =
                "CREATE TABLE IF NOT EXISTS RanksystemBlacklistedRolesIndex (GuildId INTEGER, RoleId INTEGER)";

            sqLiteBlackListedRoleTableCommand.ExecuteNonQuery();
        }
        
        using var sqLiteRewardRoleTableCommand = sqLiteConnection.CreateCommand();
        {
            sqLiteRewardRoleTableCommand.CommandText =
                "CREATE TABLE IF NOT EXISTS RanksystemRewardRolesIndex (GuildId INTEGER, RoleId INTEGER, RequiredPoints INTEGER)";

            sqLiteRewardRoleTableCommand.ExecuteNonQuery();
        }
        
        using var sqliteRanksystemConfigurationTableCommand = sqLiteConnection.CreateCommand();
        {
            sqliteRanksystemConfigurationTableCommand.CommandText =
                "CREATE TABLE IF NOT EXISTS RanksystemConfigurationIndex (GuildId INTEGER, PointsPerMessage INTEGER, PointsPerReaction INTEGER, PointsPerVoiceActivity INTEGER, LogChannelId INTEGER)";

            sqliteRanksystemConfigurationTableCommand.ExecuteNonQuery();
        }
        
        using var sqLiteUserPointTableCommand = sqLiteConnection.CreateCommand();
        {
            sqLiteUserPointTableCommand.CommandText =
                "CREATE TABLE IF NOT EXISTS RankSystemUserPointsIndex (Id INTEGER ,GuildId INTEGER, UserId INTEGER, Points INTEGER)";

            sqLiteUserPointTableCommand.ExecuteNonQuery();
        }
        


    }
}