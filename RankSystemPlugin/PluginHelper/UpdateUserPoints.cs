using Dapper;
using DSharpPlus;
using DSharpPlus.Entities;
using RankSystem;
using RankSystem.Models;
using Serilog;

namespace RankSystem.PluginHelper;

public static class UpdateUserPoints
{
    public static async Task Add(DiscordClient client,ulong guildId,DiscordUser user, RankSystemPlugin.ERankSystemReason reason)
    {
        if (ReferenceEquals(RankSystemPlugin.GetMySqlConnectionHelper(), null))
            return;
        
        var sqlConnection = RankSystemPlugin.GetMySqlConnectionHelper()!.GetMySqlConnection();
        
        var config = await sqlConnection.QueryFirstOrDefaultAsync<RankSystemConfigurationModel>("SELECT * FROM RankSystemConfigurationIndex WHERE GuildId = @GuildId", new {GuildId = guildId});
        
        if (config == null)
        {
            Log.Error("RankSystem configuration not found for Guild {GuildId}!", guildId);
            return;
        }
        
        var pointsToAdd = reason switch
        {
            RankSystemPlugin.ERankSystemReason.ChannelMessageAdded => config.PointsPerMessage,
            RankSystemPlugin.ERankSystemReason.MessageReactionAdded => config.PointsPerReaction,
            RankSystemPlugin.ERankSystemReason.ChannelVoiceActivity => config.PointsPerVoiceActivity,
            _ => 0
        };
        

        
        var addUserPoints = await sqlConnection.ExecuteAsync("UPDATE RankSystemUserPointsIndex SET Points = Points + @PointsToAdd WHERE GuildId = @GuildId AND UserId = @UserId", new {PointsToAdd = pointsToAdd, GuildId = guildId, UserId = user.Id});

        if (addUserPoints == 0)
        {
            await sqlConnection.ExecuteAsync("INSERT INTO RankSystemUserPointsIndex (GuildId, UserId, Points) VALUES (@GuildId, @UserId, @PointsToAdd)", new {PointsToAdd = pointsToAdd, GuildId = guildId, UserId = user.Id});
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