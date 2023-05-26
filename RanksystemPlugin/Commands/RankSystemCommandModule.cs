using System.Text;
using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using NinjaBot_DC;
using RankSystem.Models;
using Serilog;

namespace RankSystem.Commands;
// ReSharper disable once ClassNeverInstantiated.Global

public class RankSystemCommandModule: BaseCommandModule
{
    [Command("ranksystem")]
    public async Task BlackListedChannelCommand(CommandContext context, string action, DiscordChannel channel, bool blacklistParent = false)
    {
        if(context.Member == null)
           return;
        
        if (!context.Member.Permissions.HasPermission(Permissions.Administrator))
            return;
        
        switch (action)
        {
            case ("blacklist-channels-add"):
                await AddBlackListedChannel(context, channel, blacklistParent);
                break;
            
            case ("blacklist-channels-remove"):
                await RemoveBlackListedChannel(context, channel, blacklistParent);
                break;
            //ToDo: Add Blacklist Channels List
        }

    }

    private static async Task AddBlackListedChannel(CommandContext context, DiscordChannel channel, bool blacklistParent = false)
    {
        var sqLiteConnection = Worker.GetServiceSqLiteConnection();
        
        var channelId = channel.Id;

        if (blacklistParent)
            channelId = channel.Parent.Id;
        
        var blackListedChannel = new BlacklistedChannelsModel()
        {
            GuildId = context.Guild.Id,
            ChannelId = channelId
        };
        
        var insertSuccess = await sqLiteConnection.InsertAsync(blackListedChannel);

        if (insertSuccess == 0)
        {
            await context.Message.RespondAsync($"❌ Error | Unable to Blacklist Discord Channel");
            return;
        }
        
        await context.Message.CreateReactionAsync(DiscordEmoji.FromName(context.Client,":white_check_mark:"));
    }

    private static async Task RemoveBlackListedChannel(CommandContext context, DiscordChannel channel, bool blacklistParent = false)
    {
        var sqLiteConnection = Worker.GetServiceSqLiteConnection();
        
        var channelId = channel.Id;

        if (blacklistParent)
            channelId = channel.Parent.Id;
        
        var deleteSuccess = await sqLiteConnection.ExecuteAsync(
            $"DELETE FROM RanksystemBlacklistedChannelsIndex WHERE GuildId = {context.Guild.Id} AND ChannelId = {channelId}");
        
        if (deleteSuccess == 0)
        {
            await context.Message.RespondAsync($"❌ Error | Unable to Remove Discord Channel from Blacklist");
            return;
        }

        await context.Message.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":white_check_mark:"));



    }

    [Command("ranksystem")]
    public async Task BlackListedRoleCommand(CommandContext context, string action, DiscordRole role)
    {
        if(context.Member == null)
            return;
        
        if (!context.Member.Permissions.HasPermission(Permissions.Administrator))
            return;
        
        switch (action)
        {
            case ("blacklist-roles-add"):
                await AddBlackListedRole(context, role);
                break;
            
            case ("blacklist-roles-remove"):
                await RemoveBlackListedRole(context, role);
                break;
            //ToDo: Add Blacklist Roles List
        }
    }

    private static async Task RemoveBlackListedRole(CommandContext context, DiscordRole role)
    {
        var sqLiteConnection = Worker.GetServiceSqLiteConnection();
        
        var deleteSuccess = await sqLiteConnection.ExecuteAsync(
            $"DELETE FROM RanksystemBlacklistedRolesIndex WHERE GuildId = {context.Guild.Id} AND RoleId = {role.Id}");

        if (deleteSuccess == 0)
        {
            await context.Message.RespondAsync($"❌ Error | Unable to Remove Discord Role from Blacklist");
            return;
        }
        
        await context.Message.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":white_check_mark:"));
    }

    private static async Task AddBlackListedRole(CommandContext context, DiscordRole role)
    {
        var sqLiteConnection = Worker.GetServiceSqLiteConnection();
        
        var blackListedRole = new BlacklistedRolesModel()
        {
            GuildId = context.Guild.Id,
            RoleId = role.Id
        };
        
        var insertSuccess = await sqLiteConnection.InsertAsync(blackListedRole);
        
        if (insertSuccess == 0)
        {
            await context.Message.RespondAsync($"❌ Error | Unable to Blacklist Discord Role");
            return;
        }
        
        await context.Message.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":white_check_mark:"));
    }
    
    [Command("ranksystem")]
    public async Task RewardRoleCommand(CommandContext context, string action, DiscordRole role, int requiredPoints)
    {
        if(context.Member == null)
            return;
        
        if (!context.Member.Permissions.HasPermission(Permissions.Administrator))
            return;
        
        switch (action)
        {
            case ("reward-roles-add"):
                await AddRewardRole(context, role, requiredPoints);
                break;
            
            case ("reward-roles-remove"):
                await RemoveRewardRole(context, role);
                break;
            
            case ("reward-roles-update"):
                await UpdateRewardRole(context, role, requiredPoints);
                break;
            
            case ("reward-roles-list"):
                await ListRewardRoles(context);
                break;
        }
    }
    
    private static async Task AddRewardRole(CommandContext context, DiscordRole? role, int requiredPoints)
    {
        var sqLiteConnection = Worker.GetServiceSqLiteConnection();

        if (role == null)
        {
            await context.Message.RespondAsync($"❌ Error | Reward Role Invalid");
            return;
        }
        
        var alreadyExists = await sqLiteConnection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM RanksystemRewardRolesIndex WHERE GuildId = {context.Guild.Id} AND RoleId = {role.Id}");

        if (alreadyExists != 0)
        {
            await context.Message.RespondAsync($"❌ Error | Reward Role Already Exists");
            return;
        }
        
        var rewardRole = new RanksystemRewardRoleModel()
        {
            GuildId = context.Guild.Id,
            RoleId = role.Id,
            RequiredPoints = requiredPoints
        };
        
        var insertSuccess = await sqLiteConnection.InsertAsync(rewardRole);
        
        if (insertSuccess == 0)
        {
            await context.Message.RespondAsync($"❌ Error | Unable to Add Reward Role");
            return;
        }
        
        await context.Message.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":white_check_mark:"));
    }
    
    private static async Task RemoveRewardRole(CommandContext context, DiscordRole? role)
    {
        if (role == null)
        {
            await context.Message.RespondAsync($"❌ Error | Reward Role Invalid");
            return;
        }
        
        var sqLiteConnection = Worker.GetServiceSqLiteConnection();
        
        var deleteSuccess = await sqLiteConnection.ExecuteAsync(
            $"DELETE FROM RanksystemRewardRolesIndex WHERE GuildId = {context.Guild.Id} AND RoleId = {role.Id}");

        if (deleteSuccess == 0)
        {
            await context.Message.RespondAsync($"❌ Error | Unable to Remove Reward Role");
            return;
        }
        
        await context.Message.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":white_check_mark:"));
    }
    
    private static async Task UpdateRewardRole(CommandContext context, DiscordRole? role, int requiredPoints)
    {
        if (role == null)
        {
            await context.Message.RespondAsync($"❌ Error | Reward Role Invalid");
            return;
        }
        
        var sqLiteConnection = Worker.GetServiceSqLiteConnection();
        
        var updateSuccess = await sqLiteConnection.ExecuteAsync(
            $"UPDATE RanksystemRewardRolesIndex SET RequiredPoints = {requiredPoints} WHERE GuildId = {context.Guild.Id} AND RoleId = {role.Id}");

        if (updateSuccess == 0)
        {
            await context.Message.RespondAsync($"❌ Error | Unable to Update Reward Role");
            return;
        }
        
        await context.Message.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":white_check_mark:"));
    }
    
    private static async Task ListRewardRoles(CommandContext context)
    {
        var sqLiteConnection = Worker.GetServiceSqLiteConnection();
        
        var rewardRoles = await sqLiteConnection.GetAllAsync<RanksystemRewardRoleModel>();

        var rewardRoleModels = rewardRoles.ToList();

        if (rewardRoleModels.Count == 0)
        {
            await context.Message.RespondAsync($"❌ Error | No Reward Roles Found");
            return;
        }
        
        var rewardRolesString = new StringBuilder();

        rewardRolesString.AppendLine("There are the following Reward Roles:");
        
        foreach (var rewardRole in rewardRoleModels)
        {
            var role = context.Guild.GetRole(rewardRole.RoleId);
            rewardRolesString.AppendLine($"Role: {role.Mention} | Required Points: {rewardRole.RequiredPoints}");
        }
        
        await context.Message.RespondAsync(rewardRolesString.ToString());
    }
    
    [Command("ranksystem")]
    public async Task RanksystemConfigCommand(CommandContext context,string action, int pointsPerMessage, int pointsPerReaction, int pointsPerVoiceActivity, DiscordChannel? logChannel)
    {
        if(context.Member == null)
            return;
        
        if (!context.Member.Permissions.HasPermission(Permissions.Administrator))
            return;
        
        switch (action)
        {
            case ("config-add"):
                if (logChannel != null)
                    await AddRewardConfig(context, pointsPerMessage, pointsPerReaction, pointsPerVoiceActivity,
                        logChannel);
                break;
            
            case ("config-remove"):
                await RemoveRewardConfig(context);
                break;
            
            case ("config-update"):
                if (logChannel != null)
                    await UpdateRewardConfig(context, pointsPerMessage, pointsPerReaction, pointsPerVoiceActivity,
                        logChannel);
                break;
            
            case ("config-list"):
                await ListRewardConfig(context);
                break;
        }
    }

    private async Task ListRewardConfig(CommandContext context)
    {
        var sqLiteConnection = Worker.GetServiceSqLiteConnection();

        RanksystemConfigurationModel rewardConfig;
        
        try
        { 
            rewardConfig = await sqLiteConnection.QueryFirstAsync<RanksystemConfigurationModel>("SELECT * FROM RanksystemConfigurationIndex WHERE GuildId = @GuildId", new { GuildId = context.Guild.Id });
        }
        catch (System.InvalidOperationException e)
        {
            Log.Error(e, "Error while getting reward config");
            
            await context.Message.RespondAsync($"❌ Error | No Reward Config Found");
            return;
        }

        var rewardConfigString = new StringBuilder();
        
        rewardConfigString.AppendLine("There are the following Reward Config:");
        rewardConfigString.AppendLine($"Points Per Message: {rewardConfig.PointsPerMessage}");
        rewardConfigString.AppendLine($"Points Per Reaction: {rewardConfig.PointsPerReaction}");
        rewardConfigString.AppendLine($"Points Per Voice Activity: {rewardConfig.PointsPerVoiceActivity}");
        rewardConfigString.AppendLine($"Log Channel: {context.Guild.GetChannel(rewardConfig.LogChannelId).Mention}");
        
        await context.Message.RespondAsync(rewardConfigString.ToString());

    }

    private async Task UpdateRewardConfig(CommandContext context, int pointsPerMessage, int pointsPerReaction, int pointsPerVoiceActivity, DiscordChannel logChannel)
    {
        var sqLiteConnection = Worker.GetServiceSqLiteConnection();
        
        var updateSuccess = await sqLiteConnection.ExecuteAsync(
            $"UPDATE RanksystemConfigurationIndex SET PointsPerMessage = {pointsPerMessage}, PointsPerReaction = {pointsPerReaction}, PointsPerVoiceActivity = {pointsPerVoiceActivity}, LogChannelId = {logChannel.Id} WHERE GuildId = {context.Guild.Id}");

        if (updateSuccess == 0)
        {
            await context.Message.RespondAsync($"❌ Error | Unable to Update Reward Role");
            return;
        }
        
        await context.Message.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":white_check_mark:"));
    }

    private static async Task RemoveRewardConfig(CommandContext context)
    {
        var sqLiteConnection = Worker.GetServiceSqLiteConnection();
        
        var deleteSuccess = await sqLiteConnection.ExecuteAsync(
            $"DELETE FROM RanksystemConfigurationIndex WHERE GuildId = {context.Guild.Id}");
        
        if (deleteSuccess == 0)
        {
            await context.Message.RespondAsync($"❌ Error | Unable to Remove Reward Role");
            return;
        }
        
        await context.Message.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":white_check_mark:"));
    }

    private static async Task AddRewardConfig(CommandContext context, int pointsPerMessage, int pointsPerReaction, int pointsPerVoiceActivity, DiscordChannel logChannel)
    {
        var sqLiteConnection = Worker.GetServiceSqLiteConnection();
        
        var alreadyExists = await sqLiteConnection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM RanksystemConfigurationIndex WHERE GuildId = {context.Guild.Id}");

        if (alreadyExists != 0)
        {
            await context.Message.RespondAsync($"❌ Error | Reward Config Already Exists");
            return;
        }
        
        var rewardConfig = new RanksystemConfigurationModel()
        {
            GuildId = context.Guild.Id,
            PointsPerMessage = pointsPerMessage,
            PointsPerReaction = pointsPerReaction,
            PointsPerVoiceActivity = pointsPerVoiceActivity,
            LogChannelId = logChannel.Id
        };
        
        var insertSuccess = await sqLiteConnection.InsertAsync(rewardConfig);
        
        if (insertSuccess == 0)
        {
            await context.Message.RespondAsync($"❌ Error | Unable to Add Reward Config");
            return;
        }
        
        await context.Message.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":white_check_mark:"));
    }
}