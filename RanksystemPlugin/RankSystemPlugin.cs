using System.Diagnostics.CodeAnalysis;
using Dapper;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using NinjaBot_DC;
using PluginBase;
using PluginBase.Events;
using RankSystem.Commands;
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

        InitializeSqLiteTables();

        client.MessageCreated += MessageCreatedEvent.MessageCreated;
        client.MessageReactionAdded += MessageReactionAddedEvent.MessageReactionAdded;

        Log.Information("Hello From Ranksystem Plugin!");

    }

    private static void InitializeSqLiteTables()
    {
        Log.Information("[RankSystem] Initializing SQLite Tables...");
        var sqLiteConnection = Worker.GetServiceSqLiteConnection();

        using var sqLiteBlaclListedChannelTableCommand = sqLiteConnection.CreateCommand();
        {
            sqLiteBlaclListedChannelTableCommand.CommandText =
                "CREATE TABLE IF NOT EXISTS BlacklistedChannelsIndex (GuildId INTEGER, ChannelId INTEGER)";

            sqLiteBlaclListedChannelTableCommand.ExecuteNonQuery();
        }
        
        using var sqLiteBlaclListedRoleTableCommand = sqLiteConnection.CreateCommand();
        {
            sqLiteBlaclListedRoleTableCommand.CommandText =
                "CREATE TABLE IF NOT EXISTS BlacklistedRolesIndex (GuildId INTEGER, RoleId INTEGER)";

            sqLiteBlaclListedRoleTableCommand.ExecuteNonQuery();
        }
        
        


    }


    //ToDo: Make all settings dynamic per guild
    public const int PointsPerMessage = 5;
    public const int PointsPerReaction = 2;
    public const float PointsPerVoiceActivity = 1.2f;
    public const ulong LogChannel = 1041009856175411250;
    public static readonly ulong[] BlacklistedChannels = new ulong[] {1041105089185718334, 1041000929270452274};

    public static bool CheckUserGroupsForBlacklisted(DiscordRole[] userRolesAsArray, DiscordGuild guild)
    {
        var sqliteConnection = Worker.GetServiceSqLiteConnection();

        var blacklistedRoles = sqliteConnection.Query($"SELECT RoleId FROM BlacklistedRolesIndex WHERE GuildId = {guild.Id} ").ToArray();
        
        var bloacklistedRolesIds = new List<ulong>();

        for (var i = 0; i < blacklistedRoles.Length; i++)
        {
            bloacklistedRolesIds.Add((ulong)blacklistedRoles[i].RoleId); 
        }
        
        //ToDo: Add some caching here

        for (var r = 0; r < userRolesAsArray.Length; r++)
        {

            if (bloacklistedRolesIds.Contains(userRolesAsArray[r].Id))
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