using DSharpPlus.Commands;
using DSharpPlus.Entities;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.Records.Cache;
using NRedisStack.RedisStackCommands;
using Serilog;
using StackExchange.Redis;

namespace LoungeSystemPlugin.CommandModules;

[Command("lounge")]
public static class AdminCommandsGroup
{
    [Command("setup")]
    public static async Task LoungeSetupCommand(CommandContext context)
    {
        if (ReferenceEquals(context.Member, null))
        {
            await context.DeferResponseAsync();
            return;
        }

        if (!context.Member.Permissions.HasPermission(DiscordPermissions.Administrator))
        {
            await context.DeferResponseAsync();
            Log.Debug("User {userName} doesnt hast Permission for '/lounge setup' command", context.Member.Username);
            await context.RespondAsync(LoungeSetupUiHelper.Messages.NoPermissionMessageBuilder);
            return;
        }
        
        await context.RespondAsync(LoungeSetupUiHelper.Messages.InitialMessageBuilder);
        
        var responseMessage = await context.GetResponseAsync();

        if (ReferenceEquals(responseMessage, null))
        {
            Log.Error("[{PluginName}] Unable to get response from Discord!", LoungeSystemPlugin.GetStaticPluginName());
        }

        try
        {
            var redisConnection = await ConnectionMultiplexer.ConnectAsync(LoungeSystemPlugin.RedisConnectionString);
            var redisDatabase = redisConnection.GetDatabase(LoungeSystemPlugin.RedisDatabase);
            var json = redisDatabase.JSON();
            
            var newLoungeSetupRecord = new LoungeSetupRecord("", context.User.Id.ToString(), "", "");
            json.Set(responseMessage!.Id.ToString(), "$", newLoungeSetupRecord);
            redisDatabase.KeyExpire(responseMessage.Id.ToString(), TimeSpan.FromMinutes(15));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{PluginName}] Unable to insert new configuration record!",LoungeSystemPlugin.GetStaticPluginName());
        }
    }
}