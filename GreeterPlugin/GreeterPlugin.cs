using System.Net.Mime;
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

    public static string StaticPluginDirectory = string.Empty;
    
    public void OnLoad()
    {
        var client = Worker.GetServiceDiscordClient();
        
        client.GuildMemberAdded += GuildMemberAdded.GuildMemberAddedEvent;

        if (PluginDirectory != null) ConfigHelper.SetBasePath(PluginDirectory);
        if (PluginDirectory != null) StaticPluginDirectory = PluginDirectory;
        
        Directory.CreateDirectory(Path.Combine(StaticPluginDirectory, "temp"));

        MySqlConnectionHelper.OpenMySqlConnection();
        
        var slashCommands = Worker.GetServiceSlashCommandsExtension();
        slashCommands.RegisterCommands<GreeterPluginSubGroupContainer>();
        

        Task.Run(async () => await StartupIndex.StartupTask(client));
    }

    public void OnUnload()
    {
        MySqlConnectionHelper.CloseMySqlConnection();
    }
}