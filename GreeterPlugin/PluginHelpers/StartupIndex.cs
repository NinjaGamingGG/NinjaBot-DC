using Dapper;
using DSharpPlus;
using DSharpPlus.Entities;
using GreeterPlugin.DatabaseRecords;
using Serilog;

namespace GreeterPlugin.PluginHelpers;

public static class StartupIndex 
{
    public static async Task StartupTask(DiscordClient client)
    {
        var guildConfigRecords = await MySqlConnectionHelper.GetMySqlConnection().QueryAsync<GuildSettingsRecord>("SELECT * FROM GuildSettingsIndex");

        foreach (var guildConfig in guildConfigRecords)
        {
            var guild = await client.GetGuildAsync(guildConfig.GuildId);
            await IndexAllGuildMembers(client, guild);
        }
    }

    private static async Task IndexAllGuildMembers(DiscordClient client, DiscordGuild guild)
    {
        var guildMembers = await guild.GetAllMembersAsync();
        
        var guildMembersSorted = guildMembers.OrderBy(x => x.JoinedAt);
        
        var currentGuildMemberIndex = 1;
        
        var connection = MySqlConnectionHelper.GetMySqlConnection();
        
        foreach (var guildMember in guildMembersSorted)
        {
            var recordExists = await connection.ExecuteScalarAsync<int>("SELECT * FROM UserJoinedDataIndex WHERE GuildId = @GuildId AND UserId = @UserId", new {GuildId = guild.Id, UserId = guildMember.Id});

            if (recordExists != 0)
            {
                currentGuildMemberIndex++;
                continue;
            }
            
            var userJoinedDataRecord = new UserJoinedDataRecord()
            {
                UserId = guildMember.Id,
                GuildId = guild.Id,
                UserIndex = currentGuildMemberIndex,
                WasGreeted = false
            };
            
            await connection.ExecuteAsync("INSERT INTO UserJoinedDataIndex (UserId, GuildId, UserIndex, WasGreeted) VALUES (@UserId, @GuildId, @UserIndex, @WasGreeted)", userJoinedDataRecord);
            
            Log.Debug("Indexed User {UserId} in Guild {GuildId}", guildMember.Id, guild.Id);
            
            currentGuildMemberIndex++;
                
        }
    }
}