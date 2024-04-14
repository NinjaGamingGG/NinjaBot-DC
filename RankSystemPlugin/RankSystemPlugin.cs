using System.Diagnostics.CodeAnalysis;

using NinjaBot_DC;
using CommonPluginHelpers;
using MySqlConnector;
using PluginBase;
using RankSystem.CommandModules;
using RankSystem.Events;
using RankSystem.PluginHelper;
using Serilog;

// ReSharper disable once IdentifierTypo
namespace RankSystem;


[SuppressMessage("ReSharper", "IdentifierTypo")]
// ReSharper disable once ClassNeverInstantiated.Global
public class RankSystemPlugin : DefaultPlugin
{
    public static MySqlConnectionHelper MySqlConnectionHelper { get; private set; } = null!;
    

    public override void OnLoad()
    {
        var client = Worker.GetServiceDiscordClient();
        try
        {
            var botToken = Worker.BotCancellationToken;

            //If Bot Cancellation Token is not null link plugins cancellation token to it.
            if (botToken != null)
                CancellationTokenSource.CreateLinkedTokenSource((CancellationToken)botToken);
        }
        catch (Exception ex)
        {
            Log.Warning(ex,"Unable to link plugin cancellation token to bot's cancellation token");
        }


        if (ReferenceEquals(PluginDirectory, null))
        {
            OnUnload();
            return;
        }
        
        var config = ConfigHelper.Load(PluginDirectory, EnvironmentVariablePrefix);
        
        var tableStrings = new[]
        {
            "CREATE TABLE IF NOT EXISTS RankSystemBlacklistedChannelsIndex (GuildId MEDIUMTEXT, ChannelId MEDIUMTEXT)",
            "CREATE TABLE IF NOT EXISTS RankSystemBlacklistedRolesIndex (GuildId MEDIUMTEXT, RoleId MEDIUMTEXT)",
            "CREATE TABLE IF NOT EXISTS RankSystemRewardRolesIndex (GuildId MEDIUMTEXT, RoleId MEDIUMTEXT, RequiredPoints INTEGER)",
            "CREATE TABLE IF NOT EXISTS RankSystemConfigurationIndex (GuildId MEDIUMTEXT, PointsPerMessage INTEGER, PointsPerReaction INTEGER, PointsPerVoiceActivity INTEGER, LogChannelId MEDIUMTEXT, NotifyChannelId MEDIUMTEXT)",
            "CREATE TABLE IF NOT EXISTS RankSystemUserPointsIndex (Id INTEGER ,GuildId MEDIUMTEXT, UserId MEDIUMTEXT, Points MEDIUMTEXT)"
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
        catch (Exception)
        {
            Log.Fatal("Canceling the Startup of {PluginName} Plugin! Please check you MySql configuration", Name);
            return;
        }

        var slashCommands = Worker.GetServiceSlashCommandsExtension();
        slashCommands.RegisterCommands<AdminCommandSubGroupContainer>();
        slashCommands.RegisterCommands<RankSlashCommandModule>();
        
        client.MessageCreated += MessageCreatedEvent.MessageCreated;
        client.MessageReactionAdded += MessageReactionAddedEvent.MessageReactionAdded;

        Log.Information("[{Name}] Plugin Loaded", Name);

        Task.Run(async () => await UpdateVoiceActivity.Update(client),CancellationTokenSource.Token);

    }
    
    public enum ERankSystemReason {ChannelVoiceActivity, ChannelMessageAdded, MessageReactionAdded}

    public override void OnUnload()
    {
        var client = Worker.GetServiceDiscordClient();
        CancellationTokenSource.Cancel();
        
        client.MessageCreated -= MessageCreatedEvent.MessageCreated;
        client.MessageReactionAdded -= MessageReactionAddedEvent.MessageReactionAdded;
        
        Log.Information("[{Name}] Plugin Unloaded", Name);
    }
}