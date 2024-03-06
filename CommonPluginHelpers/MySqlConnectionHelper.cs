using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Serilog;

namespace CommonPluginHelpers;

/// <summary>
/// Helper class for managing MySQL database connections and table initialization.
/// </summary>
public class MySqlConnectionHelper(string envVarPrefix, IConfiguration configuration, string pluginName)
{
    /// <summary>
    /// Get a MySqlConnection object for connecting to a MySQL database.
    /// </summary>
    /// <returns>A MySqlConnection object.</returns>
    
    [Obsolete]
    public MySqlConnection GetMySqlConnection()
    {
        
        var serverString = configuration.GetValue<string>(envVarPrefix +":mysql-server") ?? "127.0.0.1";
        var portString = configuration.GetValue<string>(envVarPrefix+":mysql-port") ?? string.Empty;
        
        var userString = configuration.GetValue<string>(envVarPrefix+":mysql-user") ?? "root";
        var userPassword = configuration.GetValue<string>(envVarPrefix+":mysql-password") ?? string.Empty;
        var databaseString = configuration.GetValue<string>(envVarPrefix+":mysql-database") ?? string.Empty;
        
        if (userPassword == string.Empty)
        {
            Log.Fatal("[{PluginName}]Error while preparing mysql connection, no password specified", pluginName);
        }

        var connectionString = $"Server={serverString}; ";

        if (portString != string.Empty)
            connectionString += $"Port={portString}; ";

        connectionString += $"User ID={userString}; Password={userPassword}; ";
        
        if (databaseString != string.Empty)
            connectionString += $"Database={databaseString};";
        
        var connection = new MySqlConnection(connectionString);


        try
        {
            connection.Open();
            Log.Information("[{PluginName}] Mysql connection opened", pluginName);
        }
        catch (Exception e)
        {
            connection.Dispose();
            Log.Fatal(e, "An exception occured while trying to connect to your specified mysql database:");
            throw;
        }
        
        return connection;

    }

    /// <summary>
    /// Retrieves the MySQL connection string based on the provided configuration.
    /// </summary>
    /// <returns>The MySQL connection string.</returns>
    public string GetMySqlConnectionString()
    {
        var serverString = configuration.GetValue<string>(envVarPrefix +":mysql-server") ?? "127.0.0.1";
        var portString = configuration.GetValue<uint>(envVarPrefix+":mysql-port");
        
        var userString = configuration.GetValue<string>(envVarPrefix+":mysql-user") ?? "root";
        var userPassword = configuration.GetValue<string>(envVarPrefix+":mysql-password") ?? string.Empty;
        var databaseString = configuration.GetValue<string>(envVarPrefix+":mysql-database") ?? string.Empty;
        
        if (userPassword == string.Empty)
        {
            Log.Error("[{PluginName}]Error while preparing mysql connection, no password specified", pluginName);
        }

        var builder = new MySqlConnectionStringBuilder
        {
            Server = serverString,
            Port = portString,
            UserID = userString,
            Password = userPassword,
            Database = databaseString
        };

        return builder.ConnectionString;

    }

    /// <summary>
    /// Initializes MySQL database tables.
    /// </summary>
    /// <param name="tableCommands">The SQL commands used to create the tables.</param>
    /// <param name="connection">The MySqlConnection object used to connect to the database.</param>
    public void InitializeTables(IEnumerable<string> tableCommands, MySqlConnection connection)
    {
        Log.Information("[{PluginName}] Initializing MySql Tables", pluginName);
        
        if (ReferenceEquals(connection, null))
        {
            Log.Error("[{PluginName}] Mysql connection is null, cannot initialize tables", pluginName);
            return;
        }

        foreach (var commandString in tableCommands)
        {
            using var command = connection.CreateCommand();

            command.CommandText = commandString;

            command.ExecuteNonQuery();
        }
    }
    
}