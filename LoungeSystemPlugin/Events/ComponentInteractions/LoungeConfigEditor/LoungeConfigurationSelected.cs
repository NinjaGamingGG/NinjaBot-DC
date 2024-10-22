using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper.UserInterface;
using Serilog;
using StackExchange.Redis;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeConfigEditor;

public static class LoungeConfigurationSelected
{
    internal static async Task ChannelSelectionMade(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember invokingMember)
    {
        if (!invokingMember.Permissions.HasPermission(DiscordPermissions.Administrator))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,new DiscordInteractionResponseBuilder().WithContent("You do not have permission to do this."));
            return;
        }
        
        var parseSuccess = ulong.TryParse(eventArgs.Interaction.Data.Values[0], out var selectedChannelId);

        if (parseSuccess == false)
        {
            await eventArgs.Interaction.DeferAsync();
            return;
        }

        var selectedChannel = await eventArgs.Guild.GetChannelAsync(selectedChannelId);

        await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
            UIMessageBuilders.LoungeConfigSelectedResponseBuilder(selectedChannel.Mention, true));

        var responseMessage = await eventArgs.Interaction.GetOriginalResponseAsync();
        
        try
        {
            var redisConnection = await ConnectionMultiplexer.ConnectAsync(LoungeSystemPlugin.RedisConnectionString);
            var db = redisConnection.GetDatabase();
            
            var hash = new HashEntry[]
            {
                new("TargetChannelId", selectedChannel.Id),
            };
            var redisKey = $"InteractionMessageId:{responseMessage.Id}";

            db.HashSet(redisKey, hash);
            db.KeyExpire(redisKey, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{PluginName}] Error while adding Interaction Message to Cache", LoungeSystemPlugin.RedisConnectionString);
        }
    }

    internal static async Task DeleteButton(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember invokingMember)
    {
        if (!invokingMember.Permissions.HasPermission(DiscordPermissions.Administrator))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,new DiscordInteractionResponseBuilder().WithContent("You do not have permission to do this."));
            return;
        }
    }
    
    internal static async Task ResetNamePatternButton(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember invokingMember)
    {
        if (!invokingMember.Permissions.HasPermission(DiscordPermissions.Administrator))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,new DiscordInteractionResponseBuilder().WithContent("You do not have permission to do this."));
            return;
        }
        
        await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal,
            UIMessageBuilders.ChannelNamePatternRenameModalBuilder);
    }
    
    internal static async Task ResetInterfaceButton(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember invokingMember)
    {
        if (!invokingMember.Permissions.HasPermission(DiscordPermissions.Administrator))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,new DiscordInteractionResponseBuilder().WithContent("You do not have permission to do this."));
            return;
        }
    }
}