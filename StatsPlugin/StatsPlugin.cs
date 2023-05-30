using DSharpPlus.SlashCommands;
using NinjaBot_DC;
using PluginBase;
using StatsPlugin.PluginHelper;

namespace StatsPlugin;

public class StatsPlugin : IPlugin
{

    public string Name => "Stats Plugin";
    public string Description => "Simple Discord Server Stats Plugin.";
    public string? PluginDirectory { get; set; }


    public void OnLoad()
    {
        var client = Worker.GetServiceDiscordClient();

        if (PluginDirectory == null)
            OnUnload();
            
        //Nullable warning suppressed, check for null is not needed, because it is checked above.
        Directory.CreateDirectory(PluginDirectory!); 
        SqLiteConnectionHelper.OpenSqLiteConnection(PluginDirectory!);

        SqLiteConnectionHelper.InitializeSqliteTables();
        

        var slashCommands = client.UseSlashCommands();
        
        slashCommands.RegisterCommands<SlashCommandModule>();
        
        Console.WriteLine("[Stats Plugin] Plugin Loaded!");
    }

    public void OnUnload()
    {
        Console.WriteLine("Goodbye World!");
    }
}