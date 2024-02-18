using System.Diagnostics.CodeAnalysis;

using NinjaBot_DC;
using CommonPluginHelpers;
using PluginBase;
using Ranksystem;
using Ranksystem.Events;
using Ranksystem.PluginHelper;
using Serilog;

// ReSharper disable once IdentifierTypo
namespace RankSystem;


[SuppressMessage("ReSharper", "IdentifierTypo")]
// ReSharper disable once ClassNeverInstantiated.Global
public class RankSystemPlugin : IPlugin
{
    public string Name => "Ranksystem Plugin";
    public string EnvironmentVariablePrefix => "rank_system-plugin";
    public string Description => "Simple Discord Ranksystem.";
    public string? PluginDirectory { get; set; }

    private static MySqlConnectionHelper _mySqlConnectionHelper;
    
    public static MySqlConnectionHelper GetMySqlConnectionHelper()
    {
        return _mySqlConnectionHelper;
    }
    

    public void OnLoad()
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
            "CREATE TABLE IF NOT EXISTS RanksystemBlacklistedChannelsIndex (GuildId BIGINT, ChannelId INTEGER)",
            "CREATE TABLE IF NOT EXISTS RanksystemBlacklistedRolesIndex (GuildId BIGINT, RoleId INTEGER)",
            "CREATE TABLE IF NOT EXISTS RanksystemRewardRolesIndex (GuildId BIGINT, RoleId INTEGER, RequiredPoints INTEGER)",
            "CREATE TABLE IF NOT EXISTS RanksystemConfigurationIndex (GuildId BIGINT, PointsPerMessage INTEGER, PointsPerReaction INTEGER, PointsPerVoiceActivity INTEGER, LogChannelId INTEGER)",
            "CREATE TABLE IF NOT EXISTS RankSystemUserPointsIndex (Id INTEGER ,GuildId INTEGER, UserId INTEGER, Points INTEGER)"
            
            
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
        slashCommands.RegisterCommands<RanksystemSubGroupContainer>();
        
        client.MessageCreated += MessageCreatedEvent.MessageCreated;
        client.MessageReactionAdded += MessageReactionAddedEvent.MessageReactionAdded;

        Log.Information("[{Name}] Plugin Loaded", Name);

        Task.Run(async () => await UpdateVoiceActivity.Update(client));

    }
    
    public enum ERankSystemReason {ChannelVoiceActivity, ChannelMessageAdded, MessageReactionAdded}

    public void OnUnload()
    {
        Log.Information("[{Name}] Plugin Unloaded", Name);
    }
}