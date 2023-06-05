using System.Data.SQLite;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace StatsPlugin.PluginHelper;

public static class SqLiteConnectionHelper
{
    private static SQLiteConnection? _sqLiteConnection;


    public static void OpenSqLiteConnection(string pluginDirectory)
    {
        var configuration = ConfigHelper.Load();

        var dataSource = configuration.GetValue<string>("stats-plugin:sqlite-source") ?? "database.db";

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
            Log.Error("[Stats Plugin] SQLite connection is null, cannot initialize tables");
            return;
        }
        
        using var sqLiteChannelsIndexTableCommand = _sqLiteConnection.CreateCommand();
        {
            sqLiteChannelsIndexTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS StatsChannelsIndex (GuildId INTEGER, CategoryChannelId INTEGER, MemberCountChannelId INTEGER, TeamCountChannelId INTEGER, BotCountChannelId INTEGER)";
            
            sqLiteChannelsIndexTableCommand.ExecuteNonQuery();
        }

        using var sqliteCustomNamesTableCommand = _sqLiteConnection.CreateCommand();
        {
            sqliteCustomNamesTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS StatsChannelCustomNamesIndex (GuildId INTEGER, ChannelHandle TEXT, CustomName TEXT)";
            
            sqliteCustomNamesTableCommand.ExecuteNonQuery();

        }
    }
    
    
}