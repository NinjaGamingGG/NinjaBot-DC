using GreeterPlugin.Events;
using GreeterPlugin.PluginHelpers;
using NinjaBot_DC;
using PluginBase;

namespace GreeterPlugin;

public class GreeterPlugin : IPlugin
{
    public string Name => "GreeterPlugin";
    public string Description => "Greets new users with an welcome image";
    public string? PluginDirectory { get; set; }
    
    public void OnLoad()
    {
        var client = Worker.GetServiceDiscordClient();
        
        client.GuildMemberAdded += GuildMemberAdded.GreetUser;

        if (PluginDirectory != null) ConfigHelper.SetBasePath(PluginDirectory);

        MySqlConnectionHelper.OpenMySqlConnection();
    }

    public void OnUnload()
    {
    }
}