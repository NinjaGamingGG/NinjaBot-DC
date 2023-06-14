using System.Text;
using Dapper;
using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.Records;

namespace LoungeSystemPlugin;


[SlashCommandGroup("lounge", "Lounge System Commands")]
public class LoungeSystemSubGroupContainer : ApplicationCommandModule
{
    [RequirePermissions(Permissions.Administrator)]    
    [SlashCommandGroup("admin", "Server Admin Commands")]
    public class AdminCommandsSubGroup : ApplicationCommandModule
    {
        [SlashCommand("setup", "Setup a new LoungeSystem Configuration")]
        public async Task SetupCommand(InteractionContext context, 
            [Option("target-channel", "The channel which users will join to create an lounge. CANT be an Category!")] DiscordChannel channel, 
            [Option("name-pattern", "The pattern for the lounge name. Use {username} for the username. Example: ╠🥳» {username}'s Lounge")] string namePattern,
            [Option("interface", "Should we create the Interface in the lounge channels?")] bool createInterface = true, 
            [Option("interface-channel", "Specify an interface channel here if you selected *false* earlier")] DiscordChannel? interfaceChannel = null)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            
            if (createInterface == false && ReferenceEquals(interfaceChannel, null))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You need to specify an interface channel if you selected *false* for the interface option!"));
                return;
            }

            var sqLiteConnection = SqLiteHelper.GetSqLiteConnection();

            if (ReferenceEquals(sqLiteConnection, null))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Unable to connect with Database!"));
            }

            var newConfigRecord = new LoungeSystemConfigurationRecord()
            {
                GuildId = context.Guild.Id,
                TargetChannelId = channel.Id,
                InterfaceChannelId = interfaceChannel?.Id ?? 0,
                LoungeNamePattern = namePattern
            };
            
            var alreadyExists = await sqLiteConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM LoungeSystemConfigurationIndex WHERE GuildId = @GuildId AND TargetChannelId = @TargetChannelId", new { GuildId = context.Guild.Id, TargetChannelId = channel.Id });

            if (alreadyExists != 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Configuration already exists!"));
                return;
            }

            var insertSuccess = await sqLiteConnection.ExecuteAsync("INSERT INTO LoungeSystemConfigurationIndex (GuildId, TargetChannelId, InterfaceChannelId, LoungeNamePattern) VALUES (@GuildId, @TargetChannelId, @InterfaceChannelId, @LoungeNamePattern)", newConfigRecord);

            if (insertSuccess == 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Unable to insert new configuration record!"));
                return;
            }
            
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Successfully created new configuration record!"));
        }
        
        [SlashCommand("remove", "Remove an existing LoungeSystem Configuration")]
        public async Task RemoveConfigurationCommand(InteractionContext context, [Option("channel","Channel to remove from Config")] DiscordChannel channel)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            
            var sqLiteConnection = SqLiteHelper.GetSqLiteConnection();

            if (ReferenceEquals(sqLiteConnection, null))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Unable to connect with Database!"));
            }

            var alreadyExists = await sqLiteConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM LoungeSystemConfigurationIndex WHERE GuildId = @GuildId AND TargetChannelId = @TargetChannelId", new { GuildId = context.Guild.Id, TargetChannelId = channel.Id });

            if (alreadyExists == 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Configuration does not exists!"));
                return;
            }

            var deleteSuccess = await sqLiteConnection.ExecuteAsync("DELETE FROM LoungeSystemConfigurationIndex WHERE GuildId = @GuildId AND TargetChannelId = @TargetChannelId", new { GuildId = context.Guild.Id, TargetChannelId = channel.Id });

            if (deleteSuccess == 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Unable to delete configuration record!"));
                return;
            }
            
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Successfully deleted configuration record!"));
        }

        [SlashCommand("list", "List all existing LoungeSystem Configurations")]
        public async Task ListConfigurationsCommand(InteractionContext context)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            
            var sqLiteConnection = SqLiteHelper.GetSqLiteConnection();
            
            if (ReferenceEquals(sqLiteConnection, null))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Unable to connect with Database!"));
            }
            
            var configurationRecords = await sqLiteConnection.QueryAsync<LoungeSystemConfigurationRecord>("SELECT * FROM LoungeSystemConfigurationIndex WHERE GuildId = @GuildId", new { GuildId = context.Guild.Id });
            
            var configurationRecordsList = configurationRecords.ToList();
            
            if (!configurationRecordsList.Any())
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("No configurations found!"));
                return;
            }

            var configStringBuilder = new StringBuilder();
            
            configStringBuilder.AppendLine("Found the following configurations:\n");

            foreach (var configurationRecord in configurationRecordsList)
            {
                var targetChannel = context.Guild.GetChannel(configurationRecord.TargetChannelId);
                configStringBuilder.AppendLine("Target Channel: " + targetChannel.Mention);
                configStringBuilder.AppendLine("Lounge Name Pattern: " + configurationRecord.LoungeNamePattern);
                if (configurationRecord.InterfaceChannelId != 0)
                {
                    var interfaceChannel = context.Guild.GetChannel(configurationRecord.InterfaceChannelId);
                    configStringBuilder.AppendLine("Interface Channel: " + interfaceChannel.Mention);
                }
                else
                {
                    configStringBuilder.AppendLine("Interface Channel: None");
                }
                
                if(configurationRecordsList.Last() != configurationRecord)
                    configStringBuilder.AppendLine("-------------------------------------------------");

            }
            
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(configStringBuilder.ToString()));
            
            

        }
        
    }
}