using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Serilog;

namespace GreeterPlugin.PluginHelpers;

public static class MySqlConnectionHelper
{
    private static MySqlConnection? _connection;

    public static void OpenMySqlConnection()
    {
        
        var configuration = ConfigHelper.Load();
        
        var serverString = configuration.GetValue<string>("greeter-plugin:mysql-server") ?? "127.0.0.1";
        var portString = configuration.GetValue<string>("greeter-plugin:mysql-port") ?? string.Empty;
        
        var userString = configuration.GetValue<string>("greeter-plugin:mysql-user") ?? "root";
        var userPassword = configuration.GetValue<string>("greeter-plugin:mysql-password") ?? string.Empty;
        var databaseString = configuration.GetValue<string>("greeter-plugin:mysql-database") ?? string.Empty;
        
        if (userPassword == string.Empty)
        {
            Log.Fatal("[GreeterPlugin] Error while preparing mysql connection, no password specified");
            return;
        }

        var connectionString = $"Server={serverString}; ";

        if (portString != string.Empty)
            connectionString += $"Port={portString}; ";

        connectionString += $"User ID={userString}; Password={userPassword}; ";
        
        if (databaseString != string.Empty)
            connectionString += $"Database={databaseString};";
        
        _connection = new MySqlConnection(connectionString);
        
        _connection.Open();
        
        Log.Information("[GreeterPlugin] Mysql connection opened");

        if (databaseString == string.Empty)
            _connection.Execute("CREATE DATABASE IF NOT EXISTS GreeterPlugin");

        Log.Information("[GreeterPlugin] Initializing MySql Tables");
        
        InitializeTables();
    }
    
    public static MySqlConnection GetMySqlConnection()
    {
        return _connection!;
    }
    
    public static void CloseMySqlConnection()
    {
        _connection?.Close();
    }

    private static void InitializeTables()
    {
        if (ReferenceEquals(_connection, null))
        {
            Log.Error("[GreeterPlugin] Mysql connection is null, cannot initialize tables");
            return;
        }

        using var mysqlGuildSettingsTableCommand = _connection.CreateCommand();
        {
            mysqlGuildSettingsTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS GuildSettingsIndex (GuildId BIGINT PRIMARY KEY, WelcomeChannelId BIGINT, WelcomeMessage TEXT, WelcomeImageUrl TEXT, WelcomeImageText TEXT, ProfilePictureOffsetX double, ProfilePictureOffsetY double)";
            
            mysqlGuildSettingsTableCommand.ExecuteNonQuery();
        }
        
        using var mysqlUserJoinedDataIndexTableCommand = _connection.CreateCommand();
        {
            mysqlUserJoinedDataIndexTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS UserJoinedDataIndex (EntryId int NOT NULL AUTO_INCREMENT, GuildId BIGINT, UserId BIGINT, UserIndex INT, WasGreeted BOOL, PRIMARY KEY (EntryId))";
            
            mysqlUserJoinedDataIndexTableCommand.ExecuteNonQuery();
        }
        

    }
    
}