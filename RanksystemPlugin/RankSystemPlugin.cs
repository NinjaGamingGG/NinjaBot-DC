using System.Diagnostics.CodeAnalysis;
using Dapper;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
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


    //ToDo: Make all settings dynamic per guild
    public const int PointsPerMessage = 5;
    public const int PointsPerReaction = 2;
    public const float PointsPerVoiceActivity = 1.2f;
    public const ulong LogChannel = 1041009856175411250;
    public static readonly ulong[] BlacklistedChannels = new ulong[] {1041105089185718334, 1041000929270452274};



    public static async Task AddUserPoints(DiscordClient client, float pointsToAdd, string reasonMessage, ERankSystemReason reason)
    {
        if (reason == ERankSystemReason.ChannelVoiceActivity)
            return;
        
        var guild = await client.GetGuildAsync(1039518370015490169);
            
        var logChannel = guild.GetChannel(LogChannel);
        
        await logChannel.SendMessageAsync($"[Rank-system] {reasonMessage}>");
    }

    public enum ERankSystemReason {ChannelVoiceActivity, ChannelMessageAdded, MessageReactionAdded}

    public void OnUnload()
    {
        Console.WriteLine("Goodbye World!");
    }
}