using Dapper;
using DSharpPlus;
using DSharpPlus.Entities;
using MySqlConnector;
using RankSystem.Models;
using Serilog;

namespace RankSystem.PluginHelper;

public static class UpdateUserPoints
{
    public static async Task Add(DiscordClient client,ulong guildId,DiscordUser user, RankSystemPlugin.ERankSystemReason reason)
    {
        var connectionString = RankSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        RankSystemConfigurationModel? config;
        
        try
        {
            await using var sqlConnection = new MySqlConnection(connectionString);
            await sqlConnection.OpenAsync();
            config = await sqlConnection.QueryFirstOrDefaultAsync<RankSystemConfigurationModel>("SELECT * FROM RankSystem.RankSystemConfigurationIndex WHERE GuildId = @GuildId", new {GuildId = guildId});
            await sqlConnection.CloseAsync();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex,"Unable to get the RankSystemConfigurationModel from the Database in the RankSystem");
            throw;
        }


        
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


        try
        {
            var sqlConnection = new MySqlConnection(connectionString);
            await sqlConnection.OpenAsync();
            var userPointsAdded = await sqlConnection.ExecuteAsync("UPDATE RankSystem.RankSystemUserPointsIndex SET Points = Points + @PointsToAdd WHERE GuildId = @GuildId AND UserId = @UserId", new {PointsToAdd = pointsToAdd, GuildId = guildId, UserId = user.Id});

            if (userPointsAdded == 0)
                await sqlConnection.ExecuteAsync("INSERT INTO RankSystem.RankSystemUserPointsIndex (GuildId, UserId, Points) VALUES (@GuildId, @UserId, @PointsToAdd)", new {PointsToAdd = pointsToAdd, GuildId = guildId, UserId = user.Id});

            await sqlConnection.CloseAsync();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex,"Unable to add User Points in the RankSystem");
            return;
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