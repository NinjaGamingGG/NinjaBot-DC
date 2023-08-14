using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace GreeterPlugin.PluginHelpers;

public static class MySqlConnectionHelper
{
    private static MySqlConnection? _connection;

    public static void OpenMySqlConnection()
    {
        
        var configuration = ConfigHelper.Load();
        
        var serverString = configuration.GetValue<string>("greeter-plugin:mysql-server") ?? "127.0.0.1:3306";
        var userString = configuration.GetValue<string>("greeter-plugin:mysql-user") ?? "root";
        var userPassword = configuration.GetValue<string>("greeter-plugin:mysql-password") ?? "";
        var databaseString = configuration.GetValue<string>("greeter-plugin:mysql-database") ?? "GreeterPlugin";
        
        var connectionString = $"Server={serverString}; User ID={userString}; Password={userPassword}; Database={databaseString};";
        
        _connection = new MySqlConnection(connectionString);
        
        _connection.Open();

    }
    
}