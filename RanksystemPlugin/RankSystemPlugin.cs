using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DSharpPlus.CommandsNext;
using NinjaBot_DC;
using PluginBase;
using Ranksystem;
using RankSystem.Commands;
using Ranksystem.Events;
using Ranksystem.RanksystemHelper;
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

        var commands = client.GetCommandsNext();
        
        commands.RegisterCommands<RankSystemCommandModule>();

        InitializeSqLiteTables.Init();

        client.MessageCreated += MessageCreatedEvent.MessageCreated;
        client.MessageReactionAdded += MessageReactionAddedEvent.MessageReactionAdded;

        Log.Information("Hello From Ranksystem Plugin!");

        Task.Run(async () => await UpdateVoiceActivity.Update(client));

    }
    
    public enum ERankSystemReason {ChannelVoiceActivity, ChannelMessageAdded, MessageReactionAdded}

    public void OnUnload()
    {
        Console.WriteLine("Goodbye World!");
    }
}