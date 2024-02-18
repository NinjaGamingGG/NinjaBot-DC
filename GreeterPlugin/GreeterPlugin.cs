using GreeterPlugin.CommandsModules;
using GreeterPlugin.Events;
using GreeterPlugin.PluginHelpers;
using NinjaBot_DC;
using CommonPluginHelpers;
using PluginBase;
using Serilog;

namespace GreeterPlugin;

public class GreeterPlugin : IPlugin
{
    public string Name => "GreeterPlugin";
    public string EnvironmentVariablePrefix => "greeter-plugin";
    public string Description => "Greets new users with an welcome image";
    public string? PluginDirectory { get; set; }

    public static string StaticPluginDirectory = string.Empty;

    private static MySqlConnectionHelper? _mySqlConnectionHelper;
    
    public static MySqlConnectionHelper? GetMySqlConnectionHelper()
    {
        return _mySqlConnectionHelper;
    }
    
    public void OnLoad()
    {
        var client = Worker.GetServiceDiscordClient();
        
        client.GuildMemberAdded += GuildMemberAdded.GuildMemberAddedEvent;

        if (ReferenceEquals(PluginDirectory, null))
        {
            OnUnload();
            return;
        }
        StaticPluginDirectory = PluginDirectory;
        
        Directory.CreateDirectory(Path.Combine(StaticPluginDirectory, "temp"));

        var config = ConfigHelper.Load(PluginDirectory, EnvironmentVariablePrefix);

        var tableStrings = new[]
        {
            "CREATE TABLE IF NOT EXISTS GuildSettingsIndex (GuildId BIGINT PRIMARY KEY, WelcomeChannelId BIGINT, WelcomeMessage TEXT, WelcomeImageUrl TEXT, WelcomeImageText TEXT, ProfilePictureOffsetX double, ProfilePictureOffsetY double)",
            "CREATE TABLE IF NOT EXISTS UserJoinedDataIndex (EntryId int NOT NULL AUTO_INCREMENT, GuildId BIGINT, UserId BIGINT, UserIndex INT, WasGreeted BOOL, PRIMARY KEY (EntryId))"
        };

        _mySqlConnectionHelper = new MySqlConnectionHelper(EnvironmentVariablePrefix, config, Name);

        try
        {
            var connection = _mySqlConnectionHelper.GetMySqlConnection();
            _mySqlConnectionHelper.InitializeTables(tableStrings, connection);
            connection.Close();
        }
        catch (Exception)
        {
            Log.Fatal("Canceling the Startup of {PluginName} Plugin!", Name);
            return;
        }
        

        var slashCommands = Worker.GetServiceSlashCommandsExtension();
        slashCommands.RegisterCommands<SlashCommandModule>();

        Task.Run(async () => await StartupIndex.StartupTask(client));

        Log.Debug("[Greeter Plugin] Init Finished");
    }

    public void OnUnload()
    {

    }
}