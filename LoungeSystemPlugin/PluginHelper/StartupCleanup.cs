using Dapper.Contrib.Extensions;
using LoungeSystemPlugin.Records;
using MySqlConnector;

namespace LoungeSystemPlugin.PluginHelper;

public static class StartupCleanup
{
    public static async Task Execute()
    {
        var connectionString = LoungeSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnectionString();
        var mySqlConnection = new MySqlConnection(connectionString);
        
        var loungeDbModels = await mySqlConnection.GetAllAsync<LoungeDbRecord>();
        
        await mySqlConnection.CloseAsync();

        //If this is run at startup this codes sometimes executes before GuildVoiceStates Intents are registered and will always display an member count of 0 on channels
        //This is a 3am workaround to wait 5 seconds before executing the code
        await Task.Delay(TimeSpan.FromSeconds(5));
        
        foreach (var loungeDbModel in loungeDbModels)
        {
            await CleanupLounge.Execute(loungeDbModel);
        }
    }

} 