using Dapper;
using DSharpPlus;
using NinjaBot_DC;
using RankSystem.Models;
using Serilog;

namespace Ranksystem.PluginHelper;

public static class UpdateRewardRole
{
    public static async Task ForUserAsync(DiscordClient client, ulong  guildId, ulong userId)
    {
        var sqlite = Worker.GetServiceSqLiteConnection();
        
        var userPointsForGuild = await sqlite.QueryFirstOrDefaultAsync<int>("SELECT Points FROM RankSystemUserPointsIndex WHERE GuildId = @GuildId AND UserId = @UserId", new {GuildId = guildId, UserId = userId});

        var rankSystemConfiguration = await sqlite.QueryFirstOrDefaultAsync<RanksystemConfigurationModel>("SELECT * FROM RanksystemConfigurationIndex WHERE GuildId = @GuildId", new {GuildId = guildId});
        
        var rewardRoles = await sqlite.QueryAsync<RanksystemRewardRoleModel>("SELECT * FROM RanksystemRewardRolesIndex WHERE GuildId = @GuildId", new {GuildId = guildId});
        
        
        foreach (var rewardRole in rewardRoles)
        {
            if (userPointsForGuild >= rewardRole.RequiredPoints)
            {
                var guild = await client.GetGuildAsync(guildId);
                
                var role = guild.GetRole(rewardRole.RoleId);
                
                if (role == null)
                {
                    Log.Error("Role {RoleId} not found for Guild {GuildId}!", rewardRole.RoleId, guildId);
                    continue;
                }
                
                var user = await guild.GetMemberAsync(userId);
                
                if (user == null)
                {
                    Log.Error("User {UserId} not found for Guild {GuildId}!", userId, guildId);
                    continue;
                }
                
                if (user.Roles.Contains(role))
                    continue;
                
                await user.GrantRoleAsync(role);
                
                var logChannel = guild.GetChannel(rankSystemConfiguration.LogChannelId);
                
                await logChannel.SendMessageAsync($"[Rank-system] {user.Username}#{user.Discriminator} earned the role {role.Name} for {rewardRole.RequiredPoints} xp");
            }
        }
        
    }
    
}