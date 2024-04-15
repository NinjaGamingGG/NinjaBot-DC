using System.Text;
using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using MySqlConnector;
using RankSystem.Models;
using Serilog;


namespace RankSystem.CommandModules;

[SlashCommandGroup("RankSystem-Admin", "RankSystem Plugin Admin Commands",false)]
// ReSharper disable once ClassNeverInstantiated.Global
public class AdminCommandSubGroupContainer : ApplicationCommandModule
{
    [SlashCommandGroup("blacklist", "Blacklist Commands")]
    [SlashRequirePermissions(Permissions.Administrator)]
    public class BlacklistSubGroup : ApplicationCommandModule
    {
        [SlashCommand("add-channel", "Add a channel to the blacklist")]
        public async Task AddChannelToBlacklist(InteractionContext context,
            [Option("channel", "Channel to Blacklist")] DiscordChannel channel, [Option("Blacklist-Parent", "Should the Parent (Category) be blacklisted too?")] bool blacklistParent = false)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            
            var channelId = channel.Id;

            if (blacklistParent)
                channelId = channel.Parent.Id;
        
            var blackListedChannel = new BlacklistedChannelsModel()
            {
                GuildId = context.Guild.Id,
                ChannelId = channelId
            };
            
            
            var sqlString = RankSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
            
            int insertSuccess;
            try
            {
                await using var sqlConnection = new MySqlConnection(sqlString);
                await sqlConnection.OpenAsync();

                insertSuccess =
                    await sqlConnection.ExecuteAsync("INSERT INTO RankSystem.RankSystemBlacklistedChannelsIndex (GuildId, ChannelId) VALUES (@GuildId, @ChannelId)", blackListedChannel);
                await sqlConnection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Unable to connect to specified Mysql Database in RankSystemPlugin on AddChannelToBlacklist Command");
                return;
            }


            if (insertSuccess == 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Unable to Blacklist Discord Channel!"));
                return;
            }
        
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
        }

        [SlashCommand("add-role", "Add a role to the blacklist")]
        public async Task AddRoleToBlacklist(InteractionContext context, [Option("Role","Role to Blacklist")] DiscordRole role)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            

        
            var blackListedRole = new BlacklistedRolesModel()
            {
                GuildId = context.Guild.Id,
                RoleId = role.Id
            };

            int insertSuccess;
            var connectionString = RankSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
            
            try
            {
                await using var sqlConnection = new MySqlConnection(connectionString);
                await sqlConnection.OpenAsync();
                
                insertSuccess = await sqlConnection.ExecuteAsync("INSERT INTO RankSystem.RankSystemBlacklistedRolesIndex (GuildId, RoleId) VALUES (@GuildId, @RoleId)", blackListedRole);
                await sqlConnection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Unable to connect to specified Mysql Database in RankSystemPlugin on AddRoleToBlacklist Command");
                return;
            }
            
            if (insertSuccess == 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Unable to Blacklist Discord Role!"));
                return;
            }
        
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
        }
        
        [SlashCommand("remove-channel", "Remove a channel from the blacklist")]
        public async Task RemoveChannelFromBlacklist (InteractionContext context, [Option("channel", "Channel to remove from the blacklist")] DiscordChannel channel)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        
            var channelId = channel.Id;

            int deleteSuccess;
            var connectionString = RankSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();

            try
            {
                await using var sqlConnection = new MySqlConnection(connectionString);
                await sqlConnection.OpenAsync();
                
                deleteSuccess = await sqlConnection.ExecuteAsync(
                    $"DELETE FROM RankSystem.RankSystemBlacklistedChannelsIndex WHERE GuildId = {context.Guild.Id} AND ChannelId = {channelId}");
                await sqlConnection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Unable to connect to specified Mysql Database in RankSystemPlugin on RemoveChannelFromBlacklist Command");
                return;
            }
            
            if (deleteSuccess == 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Unable to remove channel from the blacklist!"));
                return;
            }
        
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
        }
        
        [SlashCommand("remove-role", "Remove a role from the blacklist")]
        public async Task RemoveRoleFromBlacklist (InteractionContext context, [Option("Role", "Role to remove from the blacklist")] DiscordRole role)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            int deleteSuccess;
            var connectionString = RankSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();

            try
            {
                await using var sqlConnection = new MySqlConnection(connectionString);
                await sqlConnection.OpenAsync();
        
                deleteSuccess = await sqlConnection.ExecuteAsync(
                    $"DELETE FROM RankSystem.RankSystemBlacklistedRolesIndex WHERE GuildId = {context.Guild.Id} AND RoleId = {role.Id}");
                await sqlConnection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Unable to connect to specified Mysql Database in RankSystemPlugin on RemoveRoleFromBlacklist Command");
                return;
            }

        
            if (deleteSuccess == 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Unable to remove role from the blacklist!"));
                return;
            }
        
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
        }
    }
    
    
    [SlashCommandGroup("reward-role", "Reward Commands")]
    [SlashRequirePermissions(Permissions.Administrator)]
    public class RewardSubGroup : ApplicationCommandModule
    {
        [SlashCommand("add", "Add a reward role")]
        public async Task AddRewardRole(InteractionContext context, [Option("Role", "Role to add as a reward")] DiscordRole role, [Option("Points", "Required Points")] long requiredPoints)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (ReferenceEquals(role, null))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Role is invalid!"));
                return;
            }

            var connectionString = RankSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
            int insertSuccess;

            try
            {
                await using var sqlConnection = new MySqlConnection(connectionString);
                await sqlConnection.OpenAsync();
                
                var alreadyExists = await sqlConnection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM RankSystem.RankSystemRewardRolesIndex WHERE GuildId = {context.Guild.Id} AND RoleId = {role.Id}");
                
                if (alreadyExists != 0)
                {
                    await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Reward Role already Exists!"));
                    return;
                }
            
                var rewardRole = new RankSystemRewardRoleModel()
                {
                    GuildId = context.Guild.Id,
                    RoleId = role.Id,
                    RequiredPoints = (int)requiredPoints
                };
                
                insertSuccess = await sqlConnection.InsertAsync(rewardRole);
                await sqlConnection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Unable to connect to specified Mysql Database in RankSystemPlugin on AddRewardRole Command");
                return;
            }
            
            if (insertSuccess == 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Unable to add RewardRole!"));
                return;
            }
        
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
        }
        
        [SlashCommand("remove", "Remove a reward role")]
        public async Task RemoveRewardRole(InteractionContext context, [Option("Role", "Role to remove from rewards")] DiscordRole role)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (ReferenceEquals(role, null))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Role is invalid!"));
                return;
            }

            int deleteSuccess;
            var connectionString = RankSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
            
            try
            {
                await using var sqlConnection = new MySqlConnection(connectionString);

                var alreadyExists = await sqlConnection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM RankSystem.RankSystemRewardRolesIndex WHERE GuildId = {context.Guild.Id} AND RoleId = {role.Id}");

                if (alreadyExists == 0)
                {
                    await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Reward Role doesn't exist!"));
                    return;
                }
            
                deleteSuccess = await sqlConnection.ExecuteAsync(
                    $"DELETE FROM RankSystem.RankSystemRewardRolesIndex WHERE GuildId = {context.Guild.Id} AND RoleId = {role.Id}");

                await sqlConnection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Unable to connect to specified Mysql Database in RankSystemPlugin on RemoveRewardRole Command");
                return;
            }
            
        
            if (deleteSuccess == 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Unable to remove RewardRole!"));
                return;
            }
        
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
        }

        [SlashCommand("list", "List all reward roles")]
        public async Task ListRewardRoles(InteractionContext context)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var connectionString = RankSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
            List<RankSystemRewardRoleModel> rewardRoleModels;
            
            try
            {
              await using var sqlConnection = new MySqlConnection(connectionString);
              var rewardRoles = await sqlConnection.GetAllAsync<RankSystemRewardRoleModel>();
              rewardRoleModels = rewardRoles.ToList();

              await sqlConnection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Unable to connect to specified Mysql Database in RankSystemPlugin on ListRewardRoles Command");
                return;
            }

            
            if (rewardRoleModels.Count == 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. No Reward Roles Found!"));
                return;
            }
        
            var rewardRolesString = new StringBuilder();

            rewardRolesString.AppendLine("There are the following Reward Roles:");
        
            foreach (var rewardRole in rewardRoleModels)
            {
                var role = context.Guild.GetRole(rewardRole.RoleId);
                rewardRolesString.AppendLine($"Role: {role.Mention} | Required Points: {rewardRole.RequiredPoints}");
            }
        
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(rewardRolesString.ToString()));
        }
        
        [SlashCommand("edit", "Edit a reward role")]
        public async Task EditRewardRole(InteractionContext context, [Option("Role", "Role to edit")] DiscordRole role, [Option("Points", "Required Points")] long requiredPoints)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (ReferenceEquals(role, null))
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Role is invalid!"));
                return;
            }

            int updateSuccess;
            var connectionString = RankSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();

            try
            {
                await using var sqlConnection = new MySqlConnection(connectionString);

                var alreadyExists = await sqlConnection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM RankSystem.RankSystemRewardRolesIndex WHERE GuildId = {context.Guild.Id} AND RoleId = {role.Id}");

                if (alreadyExists == 0)
                {
                    await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Reward Role doesn't exist!"));
                    return;
                }
            
                updateSuccess = await sqlConnection.ExecuteAsync(
                    $"UPDATE RankSystem.RankSystemRewardRolesIndex SET RequiredPoints = {requiredPoints} WHERE GuildId = {context.Guild.Id} AND RoleId = {role.Id}");
                await sqlConnection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Unable to connect to specified Mysql Database in RankSystemPlugin on EditRewardRole Command");
                throw;
            }

            
            if (updateSuccess == 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Unable to edit RewardRole!"));
                return;
            }
        
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
        }
        
        
    }
    
    [SlashCommandGroup("config", "Config Commands")]
    [SlashRequirePermissions(Permissions.Administrator)]
    public class ConfigSubGroup : ApplicationCommandModule
    {
        [SlashCommand("Setup", "Setup reward configuration")]
        public async Task SetupRewardConfig(InteractionContext context,
            [Option("log-channel", "Channel to log to")] DiscordChannel logChannel,
            [Option("Points-per-Message", "How much reward points each send message should generate")]
            long pointsPerMessage,
            [Option("Points-per-reaction", "How much reward points each created reaction should generate")]
            long pointsPerReaction,
            [Option("points-per-voice-minute", "How much reward points each minute in a voice channel should generate")]
            long pointsPerVoiceMinute,
            [Option("notify-channel", "Channel where Users get messaged about new Ranks gained")] 
            DiscordChannel notifyChannel)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            int insertSuccess;
            var connectionString = RankSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
            
            try
            {
                await using var sqlConnection = new MySqlConnection(connectionString);
        
                var alreadyExists = await sqlConnection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM RankSystem.RankSystemConfigurationIndex WHERE GuildId = {context.Guild.Id}");
                
                if (alreadyExists != 0)
                {
                    await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Reward config already exist!"));
                    return;
                }
        
                var rewardConfig = new RankSystemConfigurationModel()
                {
                    GuildId = context.Guild.Id,
                    PointsPerMessage = (int)pointsPerMessage,
                    PointsPerReaction = (int)pointsPerReaction,
                    PointsPerVoiceActivity = (int)pointsPerVoiceMinute,
                    LogChannelId = logChannel.Id,
                    NotifyChannelId = notifyChannel.Id
                };
        
                insertSuccess = await sqlConnection.InsertAsync(rewardConfig);
            
                await sqlConnection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Unable to connect to specified Mysql Database in RankSystemPlugin on SetupRewardConfig Command");
                return;
            }
            
            if (insertSuccess != 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Unable to add Reward Config!"));
                return;
            }
        
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
        }

        [SlashCommand("update", "Update reward configuration")]
        public async Task UpdateRewardConfig(InteractionContext context,
            [Option("log-channel", "Channel to log to")]
            DiscordChannel logChannel,
            [Option("Points-per-Message", "How much reward points each send message should generate")]
            long pointsPerMessage,
            [Option("Points-per-reaction", "How much reward points each created reaction should generate")]
            long pointsPerReaction,
            [Option("points-per-voice-minute", "How much reward points each minute in a voice channel should generate")]
            long pointsPerVoiceMinute,
            [Option("notify-channel", "Channel where Users get messaged about new Ranks gained")] 
            DiscordChannel notifyChannel)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var connectionString = RankSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
            int updateSuccess;
            
            try
            {
                await using var sqlConnection = new MySqlConnection(connectionString);
        
                updateSuccess = await sqlConnection.ExecuteAsync(
                    $"UPDATE RankSystem.RankSystemConfigurationIndex SET PointsPerMessage = {pointsPerMessage}, PointsPerReaction = {pointsPerReaction}, PointsPerVoiceActivity = {pointsPerVoiceMinute}, LogChannelId = {logChannel.Id}, NotifyChannelId = {notifyChannel.Id} WHERE GuildId = {context.Guild.Id}");

                await sqlConnection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Unable to connect to specified Mysql Database in RankSystemPlugin on UpdateRewardConfig Command");
                return;
            }

            
            if (updateSuccess == 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Unable to update Reward Config!"));
                return;
            }
        
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done!"));
        }
        
        [SlashCommand("List", "List reward configuration")]
        public async Task ListRewardConfig(InteractionContext context)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var connectionString = RankSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
            List<RankSystemConfigurationModel> rewardConfigModels;

            try
            {
                await using var sqlConnection = new MySqlConnection(connectionString);
                await sqlConnection.OpenAsync();
        
                var rewardConfig = await sqlConnection.GetAllAsync<RankSystemConfigurationModel>();

                rewardConfigModels = rewardConfig.ToList();
                await sqlConnection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Unable to connect to specified Mysql Database in RankSystemPlugin on ListRewardConfig Command");
                return;
            }


            if (rewardConfigModels.Count == 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. No Reward Config Found!"));
                return;
            }
        
            var rewardConfigString = new StringBuilder();

            rewardConfigString.AppendLine("There are the following Reward Configs:");
        
            foreach (var config in rewardConfigModels)
            {
                var logChannel = context.Guild.GetChannel(config.LogChannelId);
                var notifyChannel = context.Guild.GetChannel(config.NotifyChannelId);
                rewardConfigString.AppendLine($"Points Per Message: {config.PointsPerMessage}");
                rewardConfigString.AppendLine($"Points Per Reaction: {config.PointsPerMessage}");
                rewardConfigString.AppendLine($"Points Per Voice Activity: {config.PointsPerVoiceActivity}");
                rewardConfigString.AppendLine($"Log Channel: {logChannel.Mention}");
                rewardConfigString.AppendLine($"Notify Channel: {notifyChannel.Mention}");
                if(rewardConfigModels.Last() != config)
                    rewardConfigString.AppendLine("-------------------------------------------------");
            }
        
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(rewardConfigString.ToString()));
        }

        [SlashCommand("Delete", "Delete reward configuration")]
        public async Task DeleteRewardConfig(InteractionContext context)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var connectionString = RankSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
            int deleteSuccess;
            
            try
            {
                var sqlConnection = new MySqlConnection(connectionString);
        
                deleteSuccess = await sqlConnection.ExecuteAsync(
                    $"DELETE FROM RankSystem.RankSystemConfigurationIndex WHERE GuildId = {context.Guild.Id}");
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Unable to connect to specified Mysql Database in RankSystemPlugin on DeleteRewardConfig Command");
                return;
            }
            
        
            if (deleteSuccess == 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Unable to remove Reward Config!"));
                return;
            }
        
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Done!"));
        }
        
    }
    
}