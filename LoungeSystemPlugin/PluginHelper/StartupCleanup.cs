using Dapper.Contrib.Extensions;
using LoungeSystemPlugin.Records;

namespace LoungeSystemPlugin.PluginHelper;

public class StartupCleanup
{
    public static async Task Execute()
    {
        var mySqlConnection = LoungeSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();
        var loungeDbModels = await mySqlConnection.GetAllAsync<LoungeDbRecord>();

        //If this is run at startup this codes sometimes executes before GuildVoiceStates Intents are registered and will always display an member count of 0 on channels
        //This is a 3am workaround to wait 5 seconds before executing the code
        await Task.Delay(TimeSpan.FromSeconds(5));
        
        foreach (var loungeDbModel in loungeDbModels)
        {
            await CleanupLounge.Execute(loungeDbModel);
        }
    }

} 