using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus;
using DSharpPlus.EventArgs;
using GreeterPlugin.DatabaseRecords;
using GreeterPlugin.PluginHelpers;

namespace GreeterPlugin.Events;

public static class GuildMemberAdded
{
    
    public static async Task GuildMemberAddedEvent(DiscordClient client, GuildMemberAddEventArgs args)
    {
        var connection = GreeterPlugin.GetMySqlConnectionHelper().GetMySqlConnection();
            
            var guildSettingsRecord = await connection.QueryFirstOrDefaultAsync<GuildSettingsRecord>("SELECT * FROM GuildSettingsIndex WHERE GuildId = @GuildId", new {GuildId = args.Guild.Id});
            
            if (guildSettingsRecord == null)
            {
                return;
            }
            
            var userJoinedDataRecord = await connection.QueryFirstOrDefaultAsync<UserJoinedDataRecord>("SELECT * FROM UserJoinedDataIndex WHERE GuildId = @GuildId AND UserId = @UserId", new {GuildId = args.Guild.Id, UserId = args.Member.Id});
        
            if (userJoinedDataRecord == null)
            {
                var highest = await connection.QueryAsync<int>("SELECT MAX(UserIndex) FROM UserJoinedDataIndex WHERE GuildId = @GuildId", new {GuildId = args.Guild.Id});
                
                var highestIndex = highest.FirstOrDefault();
                
                userJoinedDataRecord = new UserJoinedDataRecord()
                {
                    GuildId = args.Guild.Id,
                    UserId = args.Member.Id,
                    UserIndex = highestIndex + 1,
                    WasGreeted = false
                };
                
                await connection.InsertAsync(userJoinedDataRecord);
            }
            
            var welcomeChannel = args.Guild.GetChannel(guildSettingsRecord.WelcomeChannelId);

        await GenerateWelcomeMessageWithImage.Generate(client, args.Member, guildSettingsRecord, userJoinedDataRecord, welcomeChannel, connection, args.Guild);
    }
}