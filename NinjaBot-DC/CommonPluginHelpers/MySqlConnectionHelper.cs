using Dapper;
using MySqlConnector;
using Serilog;

namespace NinjaBot_DC.CommonPluginHelpers;

public class MySqlConnectionHelper
{
    
    private MySqlConnection? _connection;

    public void OpenMySqlConnection(string pluginEnvironmentVariablePrefix, IConfigurationRoot configuration, string pluginName)
    {
        
        var serverString = configuration.GetValue<string>(pluginEnvironmentVariablePrefix +":mysql-server") ?? "127.0.0.1";
        var portString = configuration.GetValue<string>(pluginEnvironmentVariablePrefix+":mysql-port") ?? string.Empty;
        
        var userString = configuration.GetValue<string>(pluginEnvironmentVariablePrefix+":mysql-user") ?? "root";
        var userPassword = configuration.GetValue<string>(pluginEnvironmentVariablePrefix+":mysql-password") ?? string.Empty;
        var databaseString = configuration.GetValue<string>(pluginEnvironmentVariablePrefix+":mysql-database") ?? string.Empty;
        
        if (userPassword == string.Empty)
        {
            Log.Fatal("[{PluginName}]Error while preparing mysql connection, no password specified", pluginName);
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
        Log.Information("[{PluginName}] Mysql connection opened", pluginName);
    }
    
    public MySqlConnection GetMySqlConnection()
    {
        return _connection!;
    }
    
    public void CloseMySqlConnection()
    {
        _connection?.Close();
    }

    public void InitializeTables(IEnumerable<string> tableCommands, string pluginName)
    {
        Log.Information("[{PluginName}] Initializing MySql Tables", pluginName);
        
        if (ReferenceEquals(_connection, null))
        {
            Log.Error("[{PluginName}] Mysql connection is null, cannot initialize tables", pluginName);
            return;
        }

        foreach (var commandString in tableCommands)
        {
            using var command = _connection.CreateCommand();

            command.CommandText = commandString;

            command.ExecuteNonQuery();
        }
    }
    
}