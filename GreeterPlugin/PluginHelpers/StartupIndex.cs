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
        var guildConfigRecords = await GreeterPlugin.GetMySqlConnectionHelper().GetMySqlConnection().QueryAsync<GuildSettingsRecord>("SELECT * FROM GuildSettingsIndex");

        foreach (var guildConfig in guildConfigRecords)
        {
            var guild = await client.GetGuildAsync(guildConfig.GuildId);
            await IndexAllGuildMembers(guild);
        }
    }

    private static async Task IndexAllGuildMembers(DiscordGuild guild)
    {
        var guildMembers = guild.GetAllMembersAsync();

        var guildMembersAsList = new List<DiscordMember>();

        await foreach (var member in guildMembers)
        {
            guildMembersAsList.Add(member);
        }
        
        var guildMembersSorted = guildMembersAsList.OrderBy(x => x.JoinedAt);
        
        var currentGuildMemberIndex = 1;
        
        var connection = GreeterPlugin.GetMySqlConnectionHelper().GetMySqlConnection();
        
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