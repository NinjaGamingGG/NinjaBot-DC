using DSharpPlus.CommandsNext;
using LoungeSystemPlugin.CommandModules;
using LoungeSystemPlugin.Events;
using LoungeSystemPlugin.PluginHelper;
using NinjaBot_DC;
using CommonPluginHelpers;
using MySqlConnector;
using PluginBase;
using Serilog;

namespace LoungeSystemPlugin;

public class LoungeSystemPlugin : IPlugin
{
    public string Name => "LoungeSystem Plugin";
    public string EnvironmentVariablePrefix => "lounge_system-plugin";
    public string Description => "Simple Discord LoungeSystem Plugin.";
    public string? PluginDirectory { get; set; }

    private static MySqlConnectionHelper _mySqlConnectionHelper;
    
    public static MySqlConnectionHelper GetMySqlConnectionHelper()
    {
        return _mySqlConnectionHelper;
    }
    
    public void OnLoad()
    {
        if (ReferenceEquals(PluginDirectory, null))
        {
            OnUnload();
            return;
        }


        var config = ConfigHelper.Load(PluginDirectory, EnvironmentVariablePrefix);
        
        var tableStrings = new[]
        {
            "CREATE TABLE IF NOT EXISTS LoungeSystemConfigurationIndex (Id INTEGER PRIMARY KEY AUTO_INCREMENT, GuildId BIGINT, TargetChannelId BIGINT, InterfaceChannelId BIGINT, LoungeNamePattern TEXT)",
            "CREATE TABLE IF NOT EXISTS LoungeIndex (ChannelId BIGINT, GuildId BIGINT, OwnerId BIGINT, IsPublic BOOLEAN, OriginChannel BIGINT)",
            "CREATE TABLE IF NOT EXISTS RequiredRoleIndex (Id INTEGER PRIMARY KEY AUTO_INCREMENT, GuildId INTEGER, ChannelId BIGINT, RoleId INTEGER)",
            "CREATE TABLE IF NOT EXISTS LoungeMessageReplacementIndex (Id INTEGER PRIMARY KEY AUTO_INCREMENT, GuildId BIGINT, ChannelId BIGINT, ReplacementHandle TEXT,ReplacementValue TEXT)"
        };
        
        _mySqlConnectionHelper = new MySqlConnectionHelper(EnvironmentVariablePrefix, config, Name);
        
        try
        {
            var connectionString = _mySqlConnectionHelper.GetMySqlConnectionString();
            var connection = new MySqlConnection(connectionString);
            _mySqlConnectionHelper.InitializeTables(tableStrings,connection);
            connection.Close();
        }
        catch (Exception)
        {
            Log.Fatal("Canceling the Startup of {PluginName} Plugin!", Name);
            return;
        }

        var slashCommands = Worker.GetServiceSlashCommandsExtension();
        slashCommands.RegisterCommands<LoungeSystemSubGroupContainer>();

        var client = Worker.GetServiceDiscordClient();

        var commandsNext = client.GetCommandsNext();
        commandsNext.RegisterCommands<CommandNextModule>();
        
        client.VoiceStateUpdated += VoiceStateUpdated.ChannelEnter;
        client.VoiceStateUpdated += VoiceStateUpdated.ChannelLeave;

        client.ComponentInteractionCreated += ComponentInteractionCreated.InterfaceButtonPressed;
        
        
        Directory.CreateDirectory(PluginDirectory!);

        Task.Run(async () =>
        {
            await StartupCleanup.Execute();
        });

        Log.Information("[{Name}] Plugin Loaded", Name);
    }

    public void OnUnload()
    {
        var client = Worker.GetServiceDiscordClient();
        
        client.VoiceStateUpdated -= VoiceStateUpdated.ChannelEnter;
        client.VoiceStateUpdated -= VoiceStateUpdated.ChannelLeave;
        
        Log.Information("[{Name}] Plugin Unloaded", Name);
    }
}