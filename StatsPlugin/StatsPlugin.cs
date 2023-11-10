using NinjaBot_DC;
using PluginBase;
using Serilog;
using StatsPlugin.PluginHelper;
using ConfigHelper = NinjaBot_DC.CommonPluginHelpers.ConfigHelper;

namespace StatsPlugin;

public class StatsPlugin : IPlugin
{

    public string Name => "Stats Plugin";
    public string EnvironmentVariablePrefix => "stats-plugin";
    public string Description => "Simple Discord Server Stats Plugin.";
    public string? PluginDirectory { get; set; }


    public void OnLoad()
    {
        var client = Worker.GetServiceDiscordClient();

        if (ReferenceEquals(PluginDirectory, null))
        {
            OnUnload();
            return;
        }


        //Nullable warning suppressed, check for null is not needed, because it is checked above.
        Directory.CreateDirectory(PluginDirectory!); 
        SqLiteConnectionHelper.OpenSqLiteConnection(PluginDirectory!);

        SqLiteConnectionHelper.InitializeSqliteTables();

        var slashCommands = Worker.GetServiceSlashCommandsExtension();
        
        slashCommands.RegisterCommands<SlashCommandModule>();

        Task.Run(async () =>
        {
            await RefreshServerStats.Execute(client);
        });
        
        Log.Information("[Stats Plugin] Plugin Loaded!");
    }

    public void OnUnload()
    {
        SqLiteConnectionHelper.CloseSqLiteConnection();
        Log.Information("[Stats Plugin] Plugin Unloaded!");
    }
}