using Dapper;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using GreeterPlugin.DatabaseRecords;
using GreeterPlugin.PluginHelpers;
using Serilog;

namespace GreeterPlugin.CommandsModules;

[SlashCommandGroup("greeter", "Greeter Plugin Commands")]
// ReSharper disable once ClassNeverInstantiated.Global
public class SlashCommandModule : ApplicationCommandModule
{
    [SlashCommandGroup("config", "Greeter Plugin Config Commands")]
    public class ConfigSubGroup : ApplicationCommandModule
    {
        [SlashCommand("add", "Add a new config entry")]
        public async Task AddConfigCommand(InteractionContext context, [Option("WelcomeChannel", "Your Welcome Channel")] DiscordChannel channel, [Option("WelcomeMessage", "Your Welcome Message")] string message, [Option("WelcomeImageUrl", "Your Welcome Image Url")] string imageUrl, [Option("WelcomeImageText", "Your Welcome Image Text")] string imageText, [Option("ProfilePictureOffsetX", "Your Profile Picture Offset X")] double offsetX, [Option("ProfilePictureOffsetY", "Your Profile Picture Offset Y")] double offsetY)
        {
            var guildId = context.Guild.Id;
            var welcomeChannelId = channel.Id;

            var guildSettingsRecord = new GuildSettingsRecord()
            {
                GuildId = guildId,
                WelcomeChannelId = welcomeChannelId,
                WelcomeMessage = message,
                WelcomeImageUrl = imageUrl,
                WelcomeImageText = imageText,
                ProfilePictureOffsetX = offsetX,
                ProfilePictureOffsetY = offsetY
            };
            
            var connection = MySqlConnectionHelper.GetMySqlConnection();
            
            var inserted = await connection.ExecuteAsync("INSERT INTO GuildSettingsIndex (GuildId, WelcomeChannelId, WelcomeMessage, WelcomeImageUrl, WelcomeImageText, ProfilePictureOffsetX, ProfilePictureOffsetY) VALUES (@GuildId, @WelcomeChannelId, @WelcomeMessage, @WelcomeImageUrl, @WelcomeImageText, @ProfilePictureOffsetX, @ProfilePictureOffsetY)", guildSettingsRecord);

            if (inserted == 0)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Failed to add config!"));
                return;
            }
            
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Config added!"));
        }
        
        
    }
    /*
    [SlashCommandGroup("debug", "asd")]
    public class DebugSubGroup : ApplicationCommandModule
    {
        [SlashCommand("generate", "asd")]
        public async Task GenerateCommand(InteractionContext context, [Option("User", "asd")] DiscordUser user)
        {
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Generating Image..."));
            
            var connection = MySqlConnectionHelper.GetMySqlConnection();
            
            var guildSettingsRecord = await connection.QueryFirstOrDefaultAsync<GuildSettingsRecord>("SELECT * FROM GuildSettingsIndex WHERE GuildId = @GuildId", new {GuildId = context.Guild.Id});
            
            if (guildSettingsRecord == null)
            {
                return;
            }
            
            var userJoinedDataRecord = await connection.QueryFirstOrDefaultAsync<UserJoinedDataRecord>("SELECT * FROM UserJoinedDataIndex WHERE GuildId = @GuildId AND UserId = @UserId", new {GuildId = context.Guild.Id, UserId = context.Member.Id});
        
            if (userJoinedDataRecord == null)
            {
                return;
            }
            
            var welcomeChannel = context.Guild.GetChannel(guildSettingsRecord.WelcomeChannelId);

            var welcomeCard = Path.Combine(GreeterPlugin.StaticPluginDirectory,"temp", $"welcomeCard{user.Id}.png");

            await GenerateWelcomeImage.Generator(user.Username,
                user.AvatarUrl, 
                guildSettingsRecord.WelcomeImageText,
                userJoinedDataRecord.UserIndex,
                guildSettingsRecord.WelcomeImageUrl, 
                true, 
                guildSettingsRecord.ProfilePictureOffsetX, 
                guildSettingsRecord.ProfilePictureOffsetY, 
                welcomeCard);


            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Image Generated, releasing now"));
            
            var messageBuilder = new DiscordMessageBuilder();

            var filestream = File.Open(welcomeCard,FileMode.Open);
            
            messageBuilder.AddFile(filestream);
            
            if (guildSettingsRecord.WelcomeMessage.Contains("{usermention}"))
            {
                guildSettingsRecord.WelcomeMessage = guildSettingsRecord.WelcomeMessage.Replace("{usermention}", user.Mention);
            }
            
            messageBuilder.WithContent(guildSettingsRecord.WelcomeMessage);
                
            await context.Client.SendMessageAsync(welcomeChannel, messageBuilder);
            
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
            
            await connection.ExecuteAsync("UPDATE UserJoinedDataIndex SET WasGreeted = @WasGreeted WHERE GuildId = @GuildId AND UserId = @UserId", new {WasGreeted = true, GuildId = context.Guild.Id, UserId = context.Member.Id});
            
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Operation Complete"));
        }
    }*/

    
}