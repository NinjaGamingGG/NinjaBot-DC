using Dapper.Contrib.Extensions;
using LoungeSystemPlugin.Records;
using MySqlConnector;
using NinjaBot_DC;
using Serilog;

namespace LoungeSystemPlugin.PluginHelper;

public static class StartupCleanup
{
    public static async Task Execute()
    {
        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        
        List<LoungeDbRecord> loungeDbRecordList;

        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
            var loungeDbModels = await mySqlConnection.GetAllAsync<LoungeDbRecord>();
            loungeDbRecordList = loungeDbModels.ToList();
            await mySqlConnection.CloseAsync();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex, "Error while reading lounge db records on StartupCleanup");
            return;
        }
        
        var client = Worker.GetServiceDiscordClient();

        while (!client.IsConnected)
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
        
        foreach (var loungeDbModel in loungeDbRecordList)
        {
            await CleanupLounge.Execute(loungeDbModel);
        }
    }

} 