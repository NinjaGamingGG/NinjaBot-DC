using System.Diagnostics.CodeAnalysis;

using NinjaBot_DC;
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
    public string Description => "Simple Discord Ranksystem.";
    public string? PluginDirectory { get; set; }
    

    public void OnLoad()
    {
        var client = Worker.GetServiceDiscordClient();
        
        SqLiteHelper.OpenSqLiteConnection(PluginDirectory!);
        SqLiteHelper.InitializeSqliteTables();

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
        SqLiteHelper.CloseSqLiteConnection();
        Log.Information("[{Name}] Plugin Unloaded", Name);
    }
}