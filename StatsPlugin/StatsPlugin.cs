using NinjaBot_DC;
using PluginBase;
using Serilog;
using StatsPlugin.PluginHelper;

namespace StatsPlugin;

public class StatsPlugin : DefaultPlugin
{
    public override void OnLoad()
    {
        var client = Worker.GetServiceDiscordClient();

        if (ReferenceEquals(PluginDirectory, null))
        {
            OnUnload();
            return;
        }
        SqLiteConnectionHelper.OpenSqLiteConnection(PluginDirectory);

        SqLiteConnectionHelper.InitializeSqliteTables();

        var slashCommands = Worker.GetServiceSlashCommandsExtension();
        
        slashCommands.RegisterCommands<SlashCommandModule>();

        Task.Run(async () =>
        {
            await RefreshServerStats.Execute(client);
        });
        
        Log.Information("[Stats Plugin] Plugin Loaded!");
    }

    public override void OnUnload()
    {
        SqLiteConnectionHelper.CloseSqLiteConnection();
        Log.Information("[Stats Plugin] Plugin Unloaded!");
    }
}