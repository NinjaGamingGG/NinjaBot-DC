using PluginBase;

namespace StatsPlugin;

public class StatsPlugin : IPlugin
{
    public string Name => "Stats Plugin";
    public string Description => "Simple Discord Server Stats Plugin.";

    public void OnLoad()
    {
        Console.WriteLine("Hello World!");
    }

    public void OnUnload()
    {
        Console.WriteLine("Goodbye World!");
    }
}