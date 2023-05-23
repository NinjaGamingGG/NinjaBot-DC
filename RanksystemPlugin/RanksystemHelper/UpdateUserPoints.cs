﻿using Dapper;
using DSharpPlus;
using DSharpPlus.Entities;
using NinjaBot_DC;
using RankSystem;
using RankSystem.Models;
using Serilog;

namespace Ranksystem.RanksystemHelper;

public static class UpdateUserPoints
{
    public static async Task Add(DiscordClient client,ulong guildId,DiscordUser user, RankSystemPlugin.ERankSystemReason reason)
    {
        var sqlite = Worker.GetServiceSqLiteConnection();
        
        var config = await sqlite.QueryFirstOrDefaultAsync<RanksystemConfigurationModel>("SELECT * FROM RanksystemConfigurationIndex WHERE GuildId = @GuildId", new {GuildId = guildId});
        
        if (config == null)
        {
            Log.Error("Ranksystem configuration not found for Guild {GuildId}!", guildId);
            return;
        }
        
        var pointsToAdd = reason switch
        {
            RankSystemPlugin.ERankSystemReason.ChannelMessageAdded => config.PointsPerMessage,
            RankSystemPlugin.ERankSystemReason.MessageReactionAdded => config.PointsPerReaction,
            RankSystemPlugin.ERankSystemReason.ChannelVoiceActivity => config.PointsPerVoiceActivity,
            _ => 0
        };
        

        
        var addUserPoints = await sqlite.ExecuteAsync("UPDATE RankSystemUserPointsIndex SET Points = Points + @PointsToAdd WHERE GuildId = @GuildId AND UserId = @UserId", new {PointsToAdd = pointsToAdd, GuildId = guildId, UserId = user.Id});

        if (addUserPoints == 0)
        {
            await sqlite.ExecuteAsync("INSERT INTO RankSystemUserPointsIndex (GuildId, UserId, Points) VALUES (@GuildId, @UserId, @PointsToAdd)", new {PointsToAdd = pointsToAdd, GuildId = guildId, UserId = user.Id});
        }
        
        var guild = await client.GetGuildAsync(guildId);
        
        var logChannel = guild.GetChannel(config.LogChannelId);

        var reasonMessage = reason switch
        {
            RankSystemPlugin.ERankSystemReason.ChannelMessageAdded => "Message added",
            RankSystemPlugin.ERankSystemReason.MessageReactionAdded => "Reaction added",
            RankSystemPlugin.ERankSystemReason.ChannelVoiceActivity => "Voice activity",
            _ => "Unknown reason"
        };

        await logChannel.SendMessageAsync($"[Rank-system] {user.Username} earned {pointsToAdd} xp for {reasonMessage}");
    }
}