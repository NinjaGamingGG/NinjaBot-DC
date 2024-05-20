using LoungeSystemPlugin.CommandModules;
using LoungeSystemPlugin.Events;
using LoungeSystemPlugin.PluginHelper;
using NinjaBot_DC;
using CommonPluginHelpers;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using PluginBase;
using Serilog;

namespace LoungeSystemPlugin;

// ReSharper disable once ClassNeverInstantiated.Global
public class LoungeSystemPlugin : DefaultPlugin
{
    public static MySqlConnectionHelper MySqlConnectionHelper { get; private set; } = null!;

    public override void OnLoad()
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
        
        MySqlConnectionHelper = new MySqlConnectionHelper(EnvironmentVariablePrefix, config, Name);
        
        try
        {
            var connectionString = MySqlConnectionHelper.GetMySqlConnectionString();
            var connection = new MySqlConnection(connectionString);
            connection.Open();
            MySqlConnectionHelper.InitializeTables(tableStrings,connection);
            connection.Close();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex,"Canceling the Startup of {PluginName} Plugin!", Name);
            return;
        }

        var slashCommands = Worker.GetServiceSlashCommandsExtension();
        
        if (Program.IsDebugEnabled)
        {

            var debugGuildId = Worker.GetServiceConfig().GetValue<ulong>("ninja-bot:debug-guild");
            Log.Debug("Registering {PluginName} Commands in debug mode for Guild {GuildId}", Name,debugGuildId);
            slashCommands.RegisterCommands<LoungeSystemSubGroupContainer>(debugGuildId);
        }
        else
            slashCommands.RegisterCommands<LoungeSystemSubGroupContainer>();
  


        var client = Worker.GetServiceDiscordClient();
        
        client.VoiceStateUpdated += VoiceStateUpdated.ChannelEnter;
        client.VoiceStateUpdated += VoiceStateUpdated.ChannelLeave;

        client.ModalSubmitted += ModalSubmitted.OnModalSubmitted;
        client.ComponentInteractionCreated += ComponentInteractionCreated.InterfaceButtonPressed;
        
        
        Directory.CreateDirectory(PluginDirectory);

        Task.Run(async () =>
        {
            await StartupCleanup.Execute();
        });

        Log.Information("[{Name}] Plugin Loaded", Name);
    }


    public override void OnUnload()
    {
        var client = Worker.GetServiceDiscordClient();
        
        client.VoiceStateUpdated -= VoiceStateUpdated.ChannelEnter;
        client.VoiceStateUpdated -= VoiceStateUpdated.ChannelLeave;
        
        Log.Information("[{Name}] Plugin Unloaded", Name);
    }
}