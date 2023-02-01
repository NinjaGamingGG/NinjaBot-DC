using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using NinjaBot_DC;
using RankSystem.Models;

namespace RankSystem.Commands;

public class RankSystemCommandModule: BaseCommandModule
{
    [Command("ranksystem")]
    public async Task BlackListedChannelCommand(CommandContext context, string action, DiscordChannel channel, bool blacklistParent = false)
    {
        switch (action)
        {
            case ("blacklist-channels-add"):
                await AddBlackListedChannel(context, channel, blacklistParent);
                break;
            
            case ("blacklist-channels-remove"):
                await RemoveBlackListedChannel(context, channel, blacklistParent);
                break;
        }

    }
    
    public async Task AddBlackListedChannel(CommandContext context, DiscordChannel channel, bool blacklistParent = false)
    {
        var sqLiteConnection = Worker.GetServiceSqLiteConnection();
        
        var channelId = channel.Id;

        if (blacklistParent == true)
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
    
    public async Task RemoveBlackListedChannel(CommandContext context, DiscordChannel channel, bool blacklistParent = false)
    {
        var sqLiteConnection = Worker.GetServiceSqLiteConnection();
        
        var channelId = channel.Id;

        if (blacklistParent == true)
            channelId = channel.Parent.Id;
        
        var deleteSuccess = await sqLiteConnection.ExecuteAsync(
            $"DELETE FROM BlacklistedChannelsIndex WHERE GuildId = {context.Guild.Id} AND ChannelId = {channelId}");
        
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
        switch (action)
        {
            case ("blacklist-roles-add"):
                await AddBlackListedRole(context, role);
                break;
            
            case ("blacklist-roles-remove"):
                await RemoveBlackListedRole(context, role);
                break;
        }
    }

    private static async Task RemoveBlackListedRole(CommandContext context, DiscordRole role)
    {
        var sqLiteConnection = Worker.GetServiceSqLiteConnection();
        
        var deleteSuccess = await sqLiteConnection.ExecuteAsync(
            $"DELETE FROM BlacklistedRolesIndex WHERE GuildId = {context.Guild.Id} AND RoleId = {role.Id}");

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
}