using System.Data.SQLite;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace LoungeSystemPlugin.PluginHelper;

public static class SqLiteHelper
{
    private static SQLiteConnection? _sqLiteConnection;


    public static void OpenSqLiteConnection(string pluginDirectory)
    {
        var configuration = ConfigHelper.Load();

        var dataSource = configuration.GetValue<string>("lounge_system-plugin:sqlite-source") ?? "database.db";

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
        if (ReferenceEquals(_sqLiteConnection, null))
        {
            Log.Error("[LoungeSystem Plugin] SQLite connection is null, cannot initialize tables");
            return;
        }
        
        using var sqLiteLoungeSystemConfigurationTableCommand = _sqLiteConnection.CreateCommand();
        {
            sqLiteLoungeSystemConfigurationTableCommand.CommandText =
                "CREATE TABLE IF NOT EXISTS LoungeSystemConfigurationIndex (Id INTEGER PRIMARY KEY AUTOINCREMENT, GuildId INTEGER, TargetChannelId INTEGER, InterfaceChannelId INTEGER, LoungeNamePattern TEXT)";

            sqLiteLoungeSystemConfigurationTableCommand.ExecuteNonQuery();
        }
        
        using var sqLiteLoungeSystemLoungeTableCommand = _sqLiteConnection.CreateCommand();
        {
            sqLiteLoungeSystemLoungeTableCommand.CommandText =
                "CREATE TABLE IF NOT EXISTS LoungeIndex (ChannelId INTEGER, GuildId INTEGER, OwnerId INTEGER, IsPublic BOOLEAN, OriginChannel INTEGER)";

            sqLiteLoungeSystemLoungeTableCommand.ExecuteNonQuery();
        }
        
        using var sqliteLoungeSystemRequiredRolesTableCommand  = _sqLiteConnection.CreateCommand();
        {
            sqliteLoungeSystemRequiredRolesTableCommand.CommandText =
                "CREATE TABLE IF NOT EXISTS RequiredRoleIndex (Id INTEGER PRIMARY KEY AUTOINCREMENT, GuildId INTEGER, ChannelId INTEGER, RoleId INTEGER)";
            
            sqliteLoungeSystemRequiredRolesTableCommand.ExecuteNonQuery();
        }
        
        using var sqliteLoungeMessageReplacementTableCommand  = _sqLiteConnection.CreateCommand();
        {
            sqliteLoungeMessageReplacementTableCommand.CommandText =
                "CREATE TABLE IF NOT EXISTS LoungeMessageReplacementIndex (Id INTEGER PRIMARY KEY AUTOINCREMENT, GuildId INTEGER, ChannelId INTEGER, ReplacementHandle TEXT,ReplacementValue TEXT)";
            
            sqliteLoungeMessageReplacementTableCommand.ExecuteNonQuery();
        }


    }
}