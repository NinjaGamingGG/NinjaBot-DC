﻿using System.Text;
using CommonPluginHelpers;
using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using MySqlConnector;
using RankSystem;
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

            var sqlConnection = RankSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();
        
            var channelId = channel.Id;

            if (blacklistParent)
                channelId = channel.Parent.Id;
        
            var blackListedChannel = new BlacklistedChannelsModel()
            {
                GuildId = context.Guild.Id,
                ChannelId = channelId
            };
        
            var insertSuccess = await sqlConnection.InsertAsync(blackListedChannel);

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
            
            var sqlConnection = RankSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();
        
            var blackListedRole = new BlacklistedRolesModel()
            {
                GuildId = context.Guild.Id,
                RoleId = role.Id
            };
        
            var insertSuccess = await sqlConnection.InsertAsync(blackListedRole);
        
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
            
            var sqlConnection = RankSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();
        
            var channelId = channel.Id;
        
            var deleteSuccess = await sqlConnection.ExecuteAsync(
                $"DELETE FROM RanksystemBlacklistedChannelsIndex WHERE GuildId = {context.Guild.Id} AND ChannelId = {channelId}");
        
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
            
            var sqlConnection = RankSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();
        
            var deleteSuccess = await sqlConnection.ExecuteAsync(
                $"DELETE FROM RanksystemBlacklistedRolesIndex WHERE GuildId = {context.Guild.Id} AND RoleId = {role.Id}");
        
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

            var sqlConnection = RankSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();

            var alreadyExists = await sqlConnection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM RanksystemRewardRolesIndex WHERE GuildId = {context.Guild.Id} AND RoleId = {role.Id}");

            if (alreadyExists != 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Reward Role already Exists!"));
                return;
            }
            
            var rewardRole = new RanksystemRewardRoleModel()
            {
                GuildId = context.Guild.Id,
                RoleId = role.Id,
                RequiredPoints = (int)requiredPoints
            };
        
            var insertSuccess = await sqlConnection.InsertAsync(rewardRole);
        
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

            var sqlConnection = RankSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();

            var alreadyExists = await sqlConnection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM RanksystemRewardRolesIndex WHERE GuildId = {context.Guild.Id} AND RoleId = {role.Id}");

            if (alreadyExists == 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Reward Role doesn't exist!"));
                return;
            }
            
            var deleteSuccess = await sqlConnection.ExecuteAsync(
                $"DELETE FROM RanksystemRewardRolesIndex WHERE GuildId = {context.Guild.Id} AND RoleId = {role.Id}");
        
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

            var sqlConnection = RankSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();
        
            var rewardRoles = await sqlConnection.GetAllAsync<RanksystemRewardRoleModel>();

            var rewardRoleModels = rewardRoles.ToList();

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

            var sqlConnection = RankSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();

            var alreadyExists = await sqlConnection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM RanksystemRewardRolesIndex WHERE GuildId = {context.Guild.Id} AND RoleId = {role.Id}");

            if (alreadyExists == 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Reward Role doesn't exist!"));
                return;
            }
            
            var updateSuccess = await sqlConnection.ExecuteAsync(
                $"UPDATE RanksystemRewardRolesIndex SET RequiredPoints = {requiredPoints} WHERE GuildId = {context.Guild.Id} AND RoleId = {role.Id}");
        
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

            
            var sqlConnection = RankSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();
        
            var alreadyExists = await sqlConnection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM RanksystemConfigurationIndex WHERE GuildId = {context.Guild.Id}");

            if (alreadyExists != 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Reward config already exist!"));
                return;
            }
        
            var rewardConfig = new RanksystemConfigurationModel()
            {
                GuildId = context.Guild.Id,
                PointsPerMessage = (int)pointsPerMessage,
                PointsPerReaction = (int)pointsPerReaction,
                PointsPerVoiceActivity = (int)pointsPerVoiceMinute,
                LogChannelId = logChannel.Id,
                NotifyChannelId = notifyChannel.Id
            };
        
            var insertSuccess = await sqlConnection.InsertAsync(rewardConfig);
            
            await sqlConnection.CloseAsync();
        
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

            var sqlConnection = RankSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();
        
            var updateSuccess = await sqlConnection.ExecuteAsync(
                $"UPDATE RanksystemConfigurationIndex SET PointsPerMessage = {pointsPerMessage}, PointsPerReaction = {pointsPerReaction}, PointsPerVoiceActivity = {pointsPerVoiceMinute}, LogChannelId = {logChannel.Id}, NotifyChannelId = {notifyChannel.Id} WHERE GuildId = {context.Guild.Id}");

            await sqlConnection.CloseAsync();
            
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

            var sqlConnection = RankSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();
        
            var rewardConfig = await sqlConnection.GetAllAsync<RanksystemConfigurationModel>();

            var rewardConfigModels = rewardConfig.ToList();

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
                rewardConfigString.AppendLine($"Points Per Reaction: {config.PointsPerReaction}");
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

            
            var sqlConnection = RankSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();
        
            var deleteSuccess = await sqlConnection.ExecuteAsync(
                $"DELETE FROM RanksystemConfigurationIndex WHERE GuildId = {context.Guild.Id}");
        
            if (deleteSuccess == 0)
            {
                await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Unable to remove Reward Config!"));
                return;
            }
        
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error. Done!"));
        }
        
    }
    
}