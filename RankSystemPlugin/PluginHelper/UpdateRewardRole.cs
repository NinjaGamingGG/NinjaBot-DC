﻿using Dapper;
using DSharpPlus;
using RankSystem;
using RankSystem.Models;
using Serilog;

namespace RankSystem.PluginHelper;

public static class UpdateRewardRole
{
    public static async Task ForUserAsync(DiscordClient client, ulong  guildId, ulong userId)
    {
        var sqlConnection = RankSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();
        
        var userPointsForGuild = await sqlConnection.QueryFirstOrDefaultAsync<int>("SELECT Points FROM RankSystemUserPointsIndex WHERE GuildId = @GuildId AND UserId = @UserId", new {GuildId = guildId, UserId = userId});

        var rankSystemConfiguration = await sqlConnection.QueryFirstOrDefaultAsync<RankSystemConfigurationModel>("SELECT * FROM RankSystemConfigurationIndex WHERE GuildId = @GuildId", new {GuildId = guildId});
        
        var rewardRoles = await sqlConnection.QueryAsync<RankSystemRewardRoleModel>("SELECT * FROM RankSystemRewardRolesIndex WHERE GuildId = @GuildId", new {GuildId = guildId});
        
        var rewardRolesAsList = rewardRoles.ToList();
        
        if (!rewardRolesAsList.Any())
        {
            Log.Error("No reward role found for Guild {GuildId}!", guildId);
            return;
        }
        
        var rewardRolesOrdered = rewardRolesAsList.OrderBy(x => x.RequiredPoints);

        var highestRewardRole = rewardRolesOrdered.First(rewardRole => userPointsForGuild >= rewardRole.RequiredPoints);

        if (ReferenceEquals(highestRewardRole, null ))
            return;
        
        var guild = await client.GetGuildAsync(guildId);
                
        var role = guild.GetRole(highestRewardRole.RoleId);
                
        if (ReferenceEquals(role, null))
        {
            Log.Error("Role {RoleId} not found for Guild {GuildId}!", highestRewardRole, guildId);
            return;
        }
                
        var user = await guild.GetMemberAsync(userId);
                
        if (ReferenceEquals(user, null))
        {
            Log.Error("User {UserId} not found for Guild {GuildId}!", userId, guildId);
            return;
        }
                
        if (user.Roles.Contains(role))
            return;
                
        await user.GrantRoleAsync(role);

        if (rankSystemConfiguration != null)
        {
            var notifyChannel = guild.GetChannel(rankSystemConfiguration.NotifyChannelId);
            var logChannel = guild.GetChannel(rankSystemConfiguration.LogChannelId);
            await logChannel.SendMessageAsync($"[Rank-system] {user.Username} earned the role {role.Name} for {highestRewardRole.RequiredPoints} xp");
            await notifyChannel.SendMessageAsync(
                $"Yay {user.Mention}, you leveled up in the Rank-System! Your new Rank is: {role.Mention}");
        }

        
                
        
        
    }
    
}