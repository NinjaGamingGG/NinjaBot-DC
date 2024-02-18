using MySqlConnector;
using Serilog;

namespace NinjaBot_DC.CommonPluginHelpers;

public class MySqlConnectionHelper
{
    
    private MySqlConnection? _connection;

    public void OpenMySqlConnection(string envVarPrefix, IConfigurationRoot configuration, string pluginName)
    {
        
        var serverString = configuration.GetValue<string>(envVarPrefix +":mysql-server") ?? "127.0.0.1";
        var portString = configuration.GetValue<string>(envVarPrefix+":mysql-port") ?? string.Empty;
        
        var userString = configuration.GetValue<string>(envVarPrefix+":mysql-user") ?? "root";
        var userPassword = configuration.GetValue<string>(envVarPrefix+":mysql-password") ?? string.Empty;
        var databaseString = configuration.GetValue<string>(envVarPrefix+":mysql-database") ?? string.Empty;
        
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


        try
        {
            _connection.Open();
            Log.Information("[{PluginName}] Mysql connection opened", pluginName);
        }
        catch (Exception e)
        {
            _connection = null;
            Log.Fatal(e, "An exception occured while trying to connect to your specified mysql database:");
            throw;
        }

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