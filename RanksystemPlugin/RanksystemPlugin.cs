using System.Diagnostics.CodeAnalysis;
using DSharpPlus;
using DSharpPlus.Entities;
using PluginBase;
using PluginBase.Events;
using Serilog;

// ReSharper disable once IdentifierTypo
namespace Ranksystem;


[SuppressMessage("ReSharper", "IdentifierTypo")]
public abstract class RanksystemPlugin : IPlugin
{
    public string Name { get => "Ranksystem Plugin"; }
    public string Description { get => "Simple Discord Ranksystem."; }

    public void OnLoad()
    {
        var client = NinjaBot_DC.Worker.GetServiceDiscordClient();

        client.MessageCreated += MessageCreatedEvent.MessageCreated;
        client.MessageReactionAdded += MessageReactionAddedEvent.MessageReactionAdded;

        Log.Information("Hello From Ranksystem Plugin!");

    }


    //ToDo: Make all settings dynamic per guild
    public const int PointsPerMessage = 5;
    public const int PointsPerReaction = 2;
    public const float PointsPerVoiceActivity = 1.2f;
    public const ulong LogChannel = 1041009856175411250;
    public static readonly ulong[] BlacklistedChannels = new ulong[] {1041105089185718334, 1041000929270452274};
    public static readonly ulong[] BlacklistedGroups = new ulong[] {1040990856284479520 }; 
    
    public static bool CheckUserGroupsForBlacklisted(DiscordRole[] userRolesAsArray)
    {
        for (var r = 0; r < userRolesAsArray.Length; r++)
        {
            if (BlacklistedGroups.Contains(userRolesAsArray[r].Id))
                return true;
        }

        return false;
    }

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