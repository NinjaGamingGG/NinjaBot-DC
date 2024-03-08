using System.Text;
using Dapper;
using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.Records;
using MySqlConnector;
using Serilog;

namespace LoungeSystemPlugin.CommandModules;


[SlashCommandGroup("lounge", "Lounge System Commands")]
// ReSharper disable once ClassNeverInstantiated.Global
public class LoungeSystemSubGroupContainer : ApplicationCommandModule
{
    [RequirePermissions(Permissions.Administrator)]
    [SlashCommandGroup("admin", "Server Admin Commands")]
    public class AdminCommandsSubGroup : ApplicationCommandModule
    {
        [SlashCommand("setup", "Setup a new LoungeSystem Configuration")]
        public async Task SetupCommand(InteractionContext context,
            [Option("target-channel", "The channel which users will join to create an lounge. CANT be an Category!")]
            DiscordChannel channel,
            [Option("name-pattern",
                "The pattern for the lounge name. Use {username} for the username. Example: ╠🥳» {username}'s Lounge")]
            string namePattern,
            [Option("interface", "Should we create the Interface in the lounge channels?")]
            bool createInterface = true,
            [Option("interface-channel", "Specify an interface channel here if you selected *false* earlier")]
            DiscordChannel? interfaceChannel = null)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (createInterface == false && ReferenceEquals(interfaceChannel, null))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                    "You need to specify an interface channel if you selected *false* for the interface option!"));
                return;
            }

            var connectionString = LoungeSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnectionString();
            var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();

            if (ReferenceEquals(mySqlConnection, null))
            {
                Log.Error("[LoungeSystem] Unable to connect to database!");
                return;
            }

            var newConfigRecord = new LoungeSystemConfigurationRecord()
            {
                GuildId = context.Guild.Id,
                TargetChannelId = channel.Id,
                InterfaceChannelId = interfaceChannel?.Id ?? 0,
                LoungeNamePattern = namePattern
            };

            var alreadyExists = await mySqlConnection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM LoungeSystemConfigurationIndex WHERE GuildId = @GuildId AND TargetChannelId = @TargetChannelId",
                new { GuildId = context.Guild.Id, TargetChannelId = channel.Id });

            if (alreadyExists != 0)
            {
                await context.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent("Error. Configuration already exists!"));
                return;
            }

            var insertSuccess = await mySqlConnection.ExecuteAsync(
                "INSERT INTO LoungeSystemConfigurationIndex (GuildId, TargetChannelId, InterfaceChannelId, LoungeNamePattern) VALUES (@GuildId, @TargetChannelId, @InterfaceChannelId, @LoungeNamePattern)",
                newConfigRecord);

            await mySqlConnection.CloseAsync();

            if (insertSuccess == 0)
            {
                await context.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent("Error. Unable to insert new configuration record!"));
                return;
            }

            await context.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent("Successfully created new configuration record!"));
        }

        [SlashCommand("remove", "Remove an existing LoungeSystem Configuration")]
        public async Task RemoveConfigurationCommand(InteractionContext context,
            [Option("channel", "Channel to remove from Config")] DiscordChannel channel)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var connectionString = LoungeSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnectionString();
            var deleteSuccess = 0;

            try
            {
                await using var mySqlConnection = new MySqlConnection(connectionString);
                await mySqlConnection.OpenAsync();
                
                var alreadyExists = await mySqlConnection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM LoungeSystemConfigurationIndex WHERE GuildId = @GuildId AND TargetChannelId = @TargetChannelId",
                    new { GuildId = context.Guild.Id, TargetChannelId = channel.Id });

                if (alreadyExists == 0)
                {
                    await context.EditResponseAsync(
                        new DiscordWebhookBuilder().WithContent("Error. Configuration does not exists!"));
                    return;
                }

                deleteSuccess = await mySqlConnection.ExecuteAsync(
                    "DELETE FROM LoungeSystemConfigurationIndex WHERE GuildId = @GuildId AND TargetChannelId = @TargetChannelId",
                    new { GuildId = context.Guild.Id, TargetChannelId = channel.Id });

                await mySqlConnection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex, "Error while Executing Lounge-System Remove-Config-Command");
            }
            

            if (deleteSuccess == 0)
            {
                await context.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent("Error. Unable to delete configuration record!"));
                return;
            }

            await context.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent("Successfully deleted configuration record!"));
        }

        [SlashCommand("list", "List all existing LoungeSystem Configurations")]
        public async Task ListConfigurationsCommand(InteractionContext context)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var connectionString = LoungeSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnectionString();
            var configurationRecordsList = new List<LoungeSystemConfigurationRecord>();
            try
            {
                await using var mySqlConnection = new MySqlConnection(connectionString);
                await mySqlConnection.OpenAsync();

                var configurationRecords = await mySqlConnection.QueryAsync<LoungeSystemConfigurationRecord>(
                    "SELECT * FROM LoungeSystemConfigurationIndex WHERE GuildId = @GuildId",
                    new { GuildId = context.Guild.Id });

                await mySqlConnection.CloseAsync();

                configurationRecordsList = configurationRecords.ToList();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Error while Executing Lounge-System List-Config-Command");
            }

            
            if (configurationRecordsList.Count == 0)
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

                if (configurationRecordsList.Last() != configurationRecord)
                    configStringBuilder.AppendLine("-------------------------------------------------");

            }

            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(configStringBuilder.ToString()));

        }

        [SlashCommand("add-required-role",
            "If Channel requires i.e. reaction Role for access. Otherwise access level will be @everyone role))")]
        public async Task AddRequiredRole(InteractionContext context,
            [Option("Channel", "Channel which this Role is Required for")] DiscordChannel channel,
            [Option("Role", "Role which is Required for this Channel")] DiscordRole role)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var connectionString = LoungeSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnectionString();
            var insertSuccess = 0;
            try
            {
                await using var mySqlConnection = new MySqlConnection(connectionString);
                await mySqlConnection.OpenAsync();
                
                var alreadyExists = await mySqlConnection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM RequiredRoleIndex WHERE GuildId = @GuildId AND ChannelId = @ChannelId AND RoleId = @RoleId",
                    new { GuildId = context.Guild.Id, ChannelId = channel.Id, RoleId = role.Id });

                if (alreadyExists != 0)
                {
                    await context.EditResponseAsync(
                        new DiscordWebhookBuilder().WithContent(
                            "Error. Role is already listed as required for this channel!"));
                    return;
                }

                insertSuccess = await mySqlConnection.ExecuteAsync(
                    "INSERT INTO RequiredRoleIndex (GuildId, ChannelId, RoleId) VALUES (@GuildId, @ChannelId, @RoleId)",
                    new { GuildId = context.Guild.Id, ChannelId = channel.Id, RoleId = role.Id });
                await mySqlConnection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Error while Executing Lounge-System Add-Required-Role-Command");
            }

            if (insertSuccess == 0)
            {
                await context.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent("Error. Unable to insert new required role record!"));
                return;
            }

            await context.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent("Successfully created new required role record!"));
        }

        [SlashCommand("remove-required-role", "Remove a required role from a channel")]
        public async Task RemoveRequiredRole(InteractionContext context,
            [Option("Channel", "Channel which this Role is Required for")] DiscordChannel channel,
            [Option("Role", "Role which is Required for this Channel")] DiscordRole role)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            var connectionString = LoungeSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnectionString();
            var deleteSuccess = 0;

            try
            {
                var mySqlConnection = new MySqlConnection(connectionString);
                await mySqlConnection.OpenAsync();

                var alreadyExists = await mySqlConnection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM RequiredRoleIndex WHERE GuildId = @GuildId AND ChannelId = @ChannelId AND RoleId = @RoleId",
                    new { GuildId = context.Guild.Id, ChannelId = channel.Id, RoleId = role.Id });

                if (alreadyExists == 0)
                {
                    await context.EditResponseAsync(
                        new DiscordWebhookBuilder().WithContent(
                            "Error. Role is not listed as required for this channel!"));
                    return;
                }

                deleteSuccess = await mySqlConnection.ExecuteAsync(
                    "DELETE FROM RequiredRoleIndex WHERE GuildId = @GuildId AND ChannelId = @ChannelId AND RoleId = @RoleId",
                    new { GuildId = context.Guild.Id, ChannelId = channel.Id, RoleId = role.Id });

                await mySqlConnection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Error while Executing Lounge-System Remove-Required-Role-Command");
            }


            if (deleteSuccess == 0)
            {
                await context.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent("Error. Unable to delete required role record!"));
                return;
            }

            await context.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent("Successfully deleted required role record!"));
        }

        [SlashCommand("list-required-roles", "List all required roles for a channel")]
        public async Task ListRequiredRoles(InteractionContext context,
            [Option("Channel", "Channel which this Role is Required for")] DiscordChannel channel)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var connectionString = LoungeSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnectionString();
            var requiredRoleRecordsList = new List<RequiredRoleRecord>();
                
            try
            {
                var mySqlConnection = new MySqlConnection(connectionString);
                await mySqlConnection.OpenAsync();

                var requiredRoleRecords = await mySqlConnection.QueryAsync<RequiredRoleRecord>(
                    "SELECT * FROM RequiredRoleIndex WHERE GuildId = @GuildId AND ChannelId = @ChannelId",
                    new { GuildId = context.Guild.Id, ChannelId = channel.Id });

                await mySqlConnection.CloseAsync();

                requiredRoleRecordsList = requiredRoleRecords.ToList();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Error while Executing Lounge-System List-Required-Role-Command");
            }

            if (requiredRoleRecordsList.Count == 0)
            {
                await context.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent("No required roles found for this channel!"));
                return;
            }

            var requiredRoleStringBuilder = new StringBuilder();

            requiredRoleStringBuilder.AppendLine("Found the following required roles:\n");

            foreach (var requiredRoleRecord in requiredRoleRecordsList)
            {
                var requiredRole = context.Guild.GetRole(requiredRoleRecord.RoleId);
                requiredRoleStringBuilder.AppendLine("Required Role: " + requiredRole.Mention);

                if (requiredRoleRecordsList.Last() != requiredRoleRecord)
                    requiredRoleStringBuilder.AppendLine("-------------------------------------------------");

            }

            await context.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent(requiredRoleStringBuilder.ToString()));

        }

        [SlashCommand("add-name-replacement", "Adds a name Replacement for lounge (eg {prefix})")]
        public async Task AddNameReplacement(InteractionContext context,
            [Option("Target-Channel", "Targeted Channel Configuration")]
            DiscordChannel targetChannel,
            [Option("Replacement-Handle", "Handle of the Replacement you want so set")]
            ReplacementHandleEnum replacementHandle,
            [Option("Replacement-Value", "Value of the decorator you want to add")]
            string replacementValue)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var connectionString = LoungeSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnectionString();
            int insertSuccess;

            try
            {
                var replacementHandleString = DatabaseHandleHelper.GetChannelHandleFromEnum(replacementHandle);
                await using var mySqlConnection = new MySqlConnection(connectionString);
                await mySqlConnection.OpenAsync();
                
                var existingRecordsCount = await mySqlConnection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM LoungeMessageReplacementIndex WHERE GuildId= @GuildId AND ChannelId= @ChannelId AND ReplacementHandle= @ReplacementHandle",
                    new
                    {
                        GuildId = context.Guild.Id, ChannelId = targetChannel.Id,
                        ReplacementHandle = replacementHandleString
                    });

                if (existingRecordsCount != 0)
                {
                    await context.EditResponseAsync(
                        new DiscordWebhookBuilder().WithContent("Record for the name replacement already exists!"));
                    return;
                }

                insertSuccess = await mySqlConnection.ExecuteAsync(
                    "INSERT INTO LoungeMessageReplacementIndex (GuildId, ChannelId, ReplacementHandle, ReplacementValue) VALUES (@GuildId, @ChannelId, @ReplacementHandle, @ReplacementValue)",
                    new
                    {
                        GuildId = context.Guild.Id, ChannelId = targetChannel.Id,
                        ReplacementHandle = replacementHandleString, ReplacementValue = replacementValue
                    });

                await mySqlConnection.CloseAsync();

            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Error while Executing Lounge-System Add-name-replacement-Command");
                throw;
            }
            
            if (insertSuccess == 0)
            {
                await context.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent(
                        "Error. Unable to insert new name replacement record!"));
                return;
            }

            await context.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent("Successfully created new name replacement record!"));
            

            await context.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent("Successfully updated the name replacement record!"));
        }
        
                [SlashCommand("update-name-replacement", "Updates an Existing name Replacement for lounge (eg {prefix})")]
        public async Task UpdateNameReplacement(InteractionContext context,
            [Option("Target-Channel", "Targeted Channel Configuration")]
            DiscordChannel targetChannel,
            [Option("Replacement-Handle", "Handle of the Replacement you want so update")]
            ReplacementHandleEnum replacementHandle,
            [Option("Replacement-Value", "Value of the decorator you want to update")]
            string replacementValue)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var connectionString = LoungeSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnectionString();
            int updateSuccess;
            try
            {
                await using var mySqlConnection = new MySqlConnection(connectionString);
                await mySqlConnection.OpenAsync();
                
                var replacementHandleString = DatabaseHandleHelper.GetChannelHandleFromEnum(replacementHandle);

                var existingRecordsCount = await mySqlConnection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM LoungeMessageReplacementIndex WHERE GuildId= @GuildId AND ChannelId= @ChannelId AND ReplacementHandle= @ReplacementHandle",
                    new
                    {
                        GuildId = context.Guild.Id, ChannelId = targetChannel.Id,
                        ReplacementHandle = replacementHandleString
                    });

                if (existingRecordsCount == 0)
                {
                    await context.EditResponseAsync(
                        new DiscordWebhookBuilder().WithContent("Error. Unable to find the specified record!"));
                    return;
                }

                updateSuccess = await mySqlConnection.ExecuteAsync(
                    "UPDATE LoungeMessageReplacementIndex SET ReplacementValue=@ReplacementValue WHERE GuildId=@GuildId AND ChannelId= @ChannelId AND ReplacementHandle= @ReplacementHandle",
                    new
                    {
                        GuildId = context.Guild.Id, ChannelId = targetChannel.Id,
                        ReplacementHandle = replacementHandleString, ReplacementValue = @replacementValue
                    });

                await mySqlConnection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex, "Error while Executing Lounge-System Update-Required-name-replacement-Command");
                return;
            }
            
            if (updateSuccess == 0)
            {
                await context.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent("Error. Unable to update the name replacement record!"));
                return;
            }

            await context.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent("Successfully updated the name replacement record!"));
        }

        [SlashCommand("remove-name-replacement", "Removes an existing name Replacement record")]
        public async Task RemoveNameReplacement(InteractionContext context,
            [Option("Target-Channel", "Targeted Channel Configuration")]
            DiscordChannel targetChannel,
            [Option("Replacement-Handle", "Handle of the Replacement you want so remove")]
            ReplacementHandleEnum replacementHandle)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var connectionString = LoungeSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnectionString();
            
            int deleteSuccess;
            try
            {
                await using var mySqlConnection = new MySqlConnection(connectionString);
                await mySqlConnection.OpenAsync();
                
                var replacementHandleString = DatabaseHandleHelper.GetChannelHandleFromEnum(replacementHandle);

                deleteSuccess = await mySqlConnection.ExecuteAsync(
                    "DELETE FROM LoungeMessageReplacementIndex WHERE GuildId= @GuildId AND ChannelId= @ChannelId and ReplacementHandle=@ReplacementHandle",
                    new
                    {
                        GuildId = context.Guild.Id, ChannelId = targetChannel.Id,
                        ReplacementHandle = replacementHandleString
                    });

                await mySqlConnection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex, "Error while Executing Lounge-System remove-Required-name-replacement-Command");
                return;
            }

            
            
            if (deleteSuccess == 0)
            {
                await context.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent("Error. Unable to delete name replacement record OR it is not existing!"));
                return;
            }

            await context.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent("Successfully deleted name replacement record!"));

        }
        
                [SlashCommand("list-name-replacement", "List all message replacements for  a channel")]
        public async Task ListNameReplacement(InteractionContext context,
            [Option("Channel", "Channel where the replacement is set for")] DiscordChannel channel)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var connectionString = LoungeSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnectionString();
            List<LoungeMessageReplacement> loungeMessageReplacementsList;

            try
            {
                await using var mySqlConnection = new MySqlConnection(connectionString);
                await mySqlConnection.OpenAsync();
                
                var loungeMessageReplacements = await mySqlConnection.QueryAsync<LoungeMessageReplacement>(
                    "SELECT * FROM LoungeMessageReplacementIndex WHERE GuildId = @GuildId AND ChannelId = @ChannelId",
                    new { GuildId = context.Guild.Id, ChannelId = channel.Id });

                await mySqlConnection.CloseAsync();
            
                loungeMessageReplacementsList = loungeMessageReplacements.ToList();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex, "Error while Executing Lounge-System list-Required-name-replacement-Command");
                return;
            }
            


            

            if (loungeMessageReplacementsList.Count == 0)
            {
                await context.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent("No records found for this channel!"));
                return;
            }

            var requiredRoleStringBuilder = new StringBuilder();

            requiredRoleStringBuilder.AppendLine("Found the following name replacements:\n");

            foreach (var requiredRoleRecord in loungeMessageReplacementsList)
            {
                requiredRoleStringBuilder.AppendLine($"Replacement Handle= {requiredRoleRecord.ReplacementHandle}");
                requiredRoleStringBuilder.AppendLine($"Replacement Handle= {requiredRoleRecord.ReplacementValue}");

                if (loungeMessageReplacementsList.Last() != requiredRoleRecord)
                    requiredRoleStringBuilder.AppendLine("-------------------------------------------------");

            }

            await context.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent(requiredRoleStringBuilder.ToString()));

        }




}

}

/// <summary>
/// Represents the handle options for lounge system configurations.
/// </summary>
public enum ReplacementHandleEnum
    {
     [ChoiceName("Custom Name")] 
     CustomName,
     [ChoiceName("separator")]
     Separator,
     [ChoiceName("Decorator Prefix")]
     DecoratorPrefix,
     [ChoiceName("Decorator Emoji")]
     DecoratorEmoji,
     [ChoiceName("Decorator Decal")]
     DecoratorDecal
    }
    
    
