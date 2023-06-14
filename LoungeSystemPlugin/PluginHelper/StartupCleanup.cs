using Dapper.Contrib.Extensions;
using LoungeSystemPlugin.Records;

namespace LoungeSystemPlugin.PluginHelper;

public class StartupCleanup
{
    public static async Task Execute()
    {
        var sqLiteConnection = SqLiteHelper.GetSqLiteConnection();
        var loungeDbModels = await sqLiteConnection.GetAllAsync<LoungeDbRecord>();

        foreach (var loungeDbModel in loungeDbModels)
        {
            await CleanupLounge.Execute(loungeDbModel);
        }
    }

}