using GreeterPlugin.CommandsModules;
using GreeterPlugin.Events;
using GreeterPlugin.PluginHelpers;
using NinjaBot_DC;
using CommonPluginHelpers;
using PluginBase;
using Serilog;

namespace GreeterPlugin;

public class GreeterPlugin : DefaultPlugin
{
    private static string _staticPluginDirectory = string.Empty;

    private static MySqlConnectionHelper? _mySqlConnectionHelper;
    
    public static MySqlConnectionHelper? GetMySqlConnectionHelper()
    {
        return _mySqlConnectionHelper;
    }

    public static string GetStaticPluginDirectory()
    {
        return _staticPluginDirectory;
    }
    
    public override void OnLoad()
    {
        var client = Worker.GetServiceDiscordClient();
        
        client.GuildMemberAdded += GuildMemberAdded.GuildMemberAddedEvent;

        if (ReferenceEquals(PluginDirectory, null))
        {
            OnUnload();
            return;
        }
        _staticPluginDirectory = PluginDirectory;
        
        Directory.CreateDirectory(Path.Combine(_staticPluginDirectory, "temp"));

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

    public override void OnUnload()
    {

    }
}