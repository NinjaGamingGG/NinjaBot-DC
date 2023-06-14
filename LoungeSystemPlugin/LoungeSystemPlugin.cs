using NinjaBot_DC;
using PluginBase;
using Serilog;

namespace LoungeSystemPlugin;

public class LoungeSystemPlugin : IPlugin
{
    public string Name => "LoungeSystem Plugin";
    public string Description => "Simple Discord LoungeSystem Plugin.";
    public string? PluginDirectory { get; set; }
    public void OnLoad()
    {
        var client = Worker.GetServiceDiscordClient();
        
        if(ReferenceEquals(PluginDirectory, null))
            OnUnload();

        Directory.CreateDirectory(PluginDirectory!);
        
        Log.Information("[{Name}] Plugin Loaded", Name);
    }

    public void OnUnload()
    {
        Log.Information("[{Name}] Plugin Unloaded", Name);
    }
}