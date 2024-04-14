﻿using Dapper;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using MySqlConnector;
using Serilog;

namespace RankSystem.CommandModules;

public class RankSlashCommandModule: ApplicationCommandModule
{
    /// <summary>
    /// Adds a channel to the blacklist.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="targetUser">The user to check. If null, the executing user will be the target user.</param>
    /// <returns></returns>
    [SlashCommand( "Rank","Check the RankSystem Points of a user")]
    // ReSharper disable once UnusedMember.Global
    public async Task AddChannelToBlacklist(InteractionContext context, [Option("user","User to check")] DiscordUser? targetUser = null)
    {
        await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        
        //If the Target User is null (wasn't specified in command) the executing user is the target user
        targetUser ??= context.User;

        var mysqlConnectionString = RankSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        int userPoints;
        try
        {
            var mysqlConnection = new MySqlConnection(mysqlConnectionString);
            await mysqlConnection.OpenAsync();

            userPoints = await mysqlConnection.ExecuteScalarAsync<int>(
                    "SELECT Points FROM RankSystemUserPointsIndex WHERE GuildId= @GuildId AND UserId= @UserId",
                    new { GuildId = context.Guild.Id, UserId = targetUser.Id });
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Error while retrieving user points from database in RankSystem RankSlashCommandModule");
            return;
        }

        if (targetUser.Id == context.User.Id)
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Your Points are: {userPoints}"));

        else
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"The Points of {targetUser.Username} are: {userPoints}"));
    }
    
}