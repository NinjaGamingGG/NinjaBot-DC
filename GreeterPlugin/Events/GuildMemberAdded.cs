using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using GreeterPlugin.DatabaseRecords;
using GreeterPlugin.PluginHelpers;
using Serilog;

namespace GreeterPlugin.Events;

public static class GuildMemberAdded
{
    
    public static async Task GuildMemberAddedEvent(DiscordClient client, GuildMemberAddEventArgs args)
    {
        var connection = MySqlConnectionHelper.GetMySqlConnection();
            
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

            var welcomeCard = Path.Combine(GreeterPlugin.StaticPluginDirectory,"temp", $"welcomeCard{args.Member.Id}.png");

            await GenerateWelcomeImage.Generator(args.Member.Username,
                args.Member.AvatarUrl, 
                guildSettingsRecord.WelcomeImageText,
                userJoinedDataRecord.UserIndex,
                guildSettingsRecord.WelcomeImageUrl, 
                true, 
                guildSettingsRecord.ProfilePictureOffsetX, 
                guildSettingsRecord.ProfilePictureOffsetY, 
                welcomeCard);
            
            var messageBuilder = new DiscordMessageBuilder();

            var filestream = File.Open(welcomeCard,FileMode.Open);
            
            messageBuilder.AddFile(filestream);
            
            if (guildSettingsRecord.WelcomeMessage.Contains("{usermention}"))
            {
                guildSettingsRecord.WelcomeMessage = guildSettingsRecord.WelcomeMessage.Replace("{usermention}", args.Member.Mention);
            }
            
            messageBuilder.WithContent(guildSettingsRecord.WelcomeMessage);
                
            await client.SendMessageAsync(welcomeChannel, messageBuilder);
            
            filestream.Close();
            await filestream.DisposeAsync();

            if (!IsFileLocked.Check(welcomeCard, 10))
            {
                File.Delete(welcomeCard);

            }
            else
            {
                Log.Error("[Greeter Plugin] Failed to delete welcome card, file appears to be locked! Filepath: {FilePath}", welcomeCard);
                
            } 
            
            await connection.ExecuteAsync("UPDATE UserJoinedDataIndex SET WasGreeted = @WasGreeted WHERE GuildId = @GuildId AND UserId = @UserId", new {WasGreeted = true, GuildId = args.Guild.Id, UserId = args.Member.Id});
            
    }
}