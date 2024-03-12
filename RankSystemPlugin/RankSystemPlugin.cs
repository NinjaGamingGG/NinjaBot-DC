using System.Diagnostics.CodeAnalysis;

using NinjaBot_DC;
using CommonPluginHelpers;
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
    private static MySqlConnectionHelper? _mySqlConnectionHelper;
    
    public static MySqlConnectionHelper? GetMySqlConnectionHelper()
    {
        return _mySqlConnectionHelper;
    }
    

    public override void OnLoad()
    {
        var client = Worker.GetServiceDiscordClient();

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

        _mySqlConnectionHelper = new MySqlConnectionHelper(EnvironmentVariablePrefix, config, Name);
        
        try
        {
            var connection = _mySqlConnectionHelper.GetMySqlConnection();
            _mySqlConnectionHelper.InitializeTables(tableStrings,connection);
            connection.Close();
        }
        catch (Exception)
        {
            Log.Fatal("Canceling the Startup of {PluginName} Plugin! Please check you MySql configuration", Name);
            return;
        }

        var slashCommands = Worker.GetServiceSlashCommandsExtension();
        slashCommands.RegisterCommands<AdminCommandSubGroupContainer>();
        
        client.MessageCreated += MessageCreatedEvent.MessageCreated;
        client.MessageReactionAdded += MessageReactionAddedEvent.MessageReactionAdded;

        Log.Information("[{Name}] Plugin Loaded", Name);

        Task.Run(async () => await UpdateVoiceActivity.Update(client));

    }
    
    public enum ERankSystemReason {ChannelVoiceActivity, ChannelMessageAdded, MessageReactionAdded}

    public override void OnUnload()
    {
        Log.Information("[{Name}] Plugin Unloaded", Name);
    }
}