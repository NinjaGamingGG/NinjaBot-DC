using System.Data.SQLite;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Ranksystem.PluginHelper;

public static class SqLiteHelper
{
    private static SQLiteConnection? _sqLiteConnection;


    public static void OpenSqLiteConnection(string pluginDirectory)
    {
        var configuration = ConfigHelper.Load();

        var dataSource = configuration.GetValue<string>("ranksystem-plugin:sqlite-source") ?? "database.db";

        var sqliteSource = Path.Combine(pluginDirectory, dataSource);

        _sqLiteConnection = new SQLiteConnection($"Data Source={sqliteSource};Version=3;New=True;Compress=True;");

        _sqLiteConnection.Open();
    }
    
    public static SQLiteConnection? GetSqLiteConnection()
    {
        return _sqLiteConnection;
    }
    
    public static void CloseSqLiteConnection()
    {
        _sqLiteConnection?.Close();
    }

    public static void InitializeSqliteTables()
    {
        if (_sqLiteConnection == null)
        {
            Log.Error("[Ranksystem Plugin] SQLite connection is null, cannot initialize tables");
            return;
        }
        
        using var sqLiteBlackListedChannelTableCommand = _sqLiteConnection.CreateCommand();
        {
            sqLiteBlackListedChannelTableCommand.CommandText =
                "CREATE TABLE IF NOT EXISTS RanksystemBlacklistedChannelsIndex (GuildId INTEGER, ChannelId INTEGER)";

            sqLiteBlackListedChannelTableCommand.ExecuteNonQuery();
        }
        
        using var sqLiteBlackListedRoleTableCommand = _sqLiteConnection.CreateCommand();
        {
            sqLiteBlackListedRoleTableCommand.CommandText =
                "CREATE TABLE IF NOT EXISTS RanksystemBlacklistedRolesIndex (GuildId INTEGER, RoleId INTEGER)";

            sqLiteBlackListedRoleTableCommand.ExecuteNonQuery();
        }
        
        using var sqLiteRewardRoleTableCommand = _sqLiteConnection.CreateCommand();
        {
            sqLiteRewardRoleTableCommand.CommandText =
                "CREATE TABLE IF NOT EXISTS RanksystemRewardRolesIndex (GuildId INTEGER, RoleId INTEGER, RequiredPoints INTEGER)";

            sqLiteRewardRoleTableCommand.ExecuteNonQuery();
        }
        
        using var sqliteRanksystemConfigurationTableCommand = _sqLiteConnection.CreateCommand();
        {
            sqliteRanksystemConfigurationTableCommand.CommandText =
                "CREATE TABLE IF NOT EXISTS RanksystemConfigurationIndex (GuildId INTEGER, PointsPerMessage INTEGER, PointsPerReaction INTEGER, PointsPerVoiceActivity INTEGER, LogChannelId INTEGER)";

            sqliteRanksystemConfigurationTableCommand.ExecuteNonQuery();
        }
        
        using var sqLiteUserPointTableCommand = _sqLiteConnection.CreateCommand();
        {
            sqLiteUserPointTableCommand.CommandText =
                "CREATE TABLE IF NOT EXISTS RankSystemUserPointsIndex (Id INTEGER ,GuildId INTEGER, UserId INTEGER, Points INTEGER)";

            sqLiteUserPointTableCommand.ExecuteNonQuery();
        }
        
    }
}