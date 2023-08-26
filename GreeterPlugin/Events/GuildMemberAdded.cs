using Dapper;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using GreeterPlugin.DatabaseRecords;
using GreeterPlugin.PluginHelpers;
using Serilog;

namespace GreeterPlugin.Events;

public static class GuildMemberAdded
{
    
    public static async Task GuildMemberAddedEvent(DiscordClient sender, GuildMemberAddEventArgs args)
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
            var rowsInTable = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM UserJoinedDataIndex WHERE GuildId = @GuildId", new {GuildId = args.Guild.Id});
            
            userJoinedDataRecord = new UserJoinedDataRecord()
            {
                UserId = args.Member.Id,
                GuildId = args.Guild.Id,
                UserIndex = rowsInTable + 1,
                WasGreeted = false
            };
        }
        
        var welcomeChannel = args.Guild.GetChannel(guildSettingsRecord.WelcomeChannelId);

        var welcomeCard = GreeterPlugin.StaticPluginDirectory + $@"\welcomeCard{args.Member.Id}.png";
        
        GenerateWelcomeImage.Generator(args.Member.Username,
            args.Member.AvatarUrl, 
            guildSettingsRecord.WelcomeImageText,
            userJoinedDataRecord.UserIndex,
            guildSettingsRecord.WelcomeImageUrl, 
            true, 
            guildSettingsRecord.ProfilePictureOffsetX, 
            guildSettingsRecord.ProfilePictureOffsetY, 
            welcomeCard);
        
        var messageBuilder = new DiscordMessageBuilder();
            
        var filestream = File.Open(welcomeCard, FileMode.Open);
            
        messageBuilder.AddFile(filestream);
                
        await sender.SendMessageAsync(welcomeChannel, messageBuilder);

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