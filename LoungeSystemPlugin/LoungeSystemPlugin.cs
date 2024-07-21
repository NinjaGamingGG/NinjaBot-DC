using LoungeSystemPlugin.Events;
using LoungeSystemPlugin.PluginHelper;
using NinjaBot_DC;
using CommonPluginHelpers;
using DSharpPlus;
using MySqlConnector;
using PluginBase;
using Serilog;

namespace LoungeSystemPlugin;

// ReSharper disable once ClassNeverInstantiated.Global
public class LoungeSystemPlugin : DefaultPlugin
{
    public static MySqlConnectionHelper MySqlConnectionHelper { get; private set; } = null!;

    private static string? _staticPluginName;
    public static string GetStaticPluginName()
    {
        return _staticPluginName ?? "Not Initialized";
    }

    private static void SetStaticPluginName(string pluginName)
    {
        _staticPluginName = pluginName;
    }
    
    public override void OnLoad()
    {
        if (ReferenceEquals(PluginDirectory, null))
        {
            OnUnload();
            return;
        }
        
        SetStaticPluginName(Name);
        
        Directory.CreateDirectory(PluginDirectory);

        var config = Worker.LoadAssemblyConfig(Path.Combine(PluginDirectory,"config.json"), GetType().Assembly, EnvironmentVariablePrefix);
        
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
            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            MySqlConnectionHelper.InitializeTables(tableStrings,connection);
            connection.Close();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex,"[{PluginName}] Canceling the Startup of Plugin!", Name);
            return;
        }
        
        var clientBuilder = Worker.GetDiscordClientBuilder();

        clientBuilder.ConfigureEventHandlers(
            builder => builder.HandleModalSubmitted(ModalSubmitted.ModalSubmittedHandler)
                .HandleVoiceStateUpdated(VoiceStateUpdated.ChannelEnter)
                .HandleVoiceStateUpdated(VoiceStateUpdated.ChannelLeave)
                .HandleComponentInteractionCreated(ComponentInteractionCreated.InterfaceButtonPressed));

        Task.Run(async () =>
        {
            await StartupCleanup.Execute();
        });

        Log.Information("[{PluginName}] Plugin Loaded", Name);
    }


    public override void OnUnload()
    {
        Log.Information("[{PluginName}] Plugin Unloaded", Name);
    }
}