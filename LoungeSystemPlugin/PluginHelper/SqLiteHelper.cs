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
        if (_sqLiteConnection == null)
        {
            Log.Error("[LoungeSystem Plugin] SQLite connection is null, cannot initialize tables");
            return;
        }

    }
}