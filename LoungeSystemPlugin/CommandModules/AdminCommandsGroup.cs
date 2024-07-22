using Dapper;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.Records;
using MySqlConnector;
using Serilog;

namespace LoungeSystemPlugin.CommandModules;

[Command("lounge")]
public static class AdminCommandsGroup
{
    
    [Command("setup-noui")]
    public static async Task LoungeSetup_NoUICommand(CommandContext context, DiscordChannel targetChannel, string namePattern, bool createInterfaceChannel = false, DiscordChannel? interfaceChannel = null)
    {
        await context.DeferResponseAsync();
        
        
        if (createInterfaceChannel == false && ReferenceEquals(interfaceChannel, null))
        {
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                "You need to specify an interface channel if you selected *false* for the interface option!"));
            return;
        }
        
        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        int insertSuccess;
        try
        {
            var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
        
            if (ReferenceEquals(mySqlConnection, null))
            {
                Log.Error("[LoungeSystem] Unable to connect to database!");
                return;
            }

            var guildId = context.Channel.Guild.Id;

            var newConfigRecord = new LoungeSystemConfigurationRecord()
            {
                GuildId = guildId,
                TargetChannelId = targetChannel.Id,
                InterfaceChannelId = interfaceChannel?.Id ?? 0,
                LoungeNamePattern = namePattern
            };
        
            var alreadyExists = await mySqlConnection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM LoungeSystemConfigurationIndex WHERE GuildId = @GuildId AND TargetChannelId = @TargetChannelId",
                new { GuildId = guildId, TargetChannelId = targetChannel.Id });
        
            if (alreadyExists != 0)
            {
                await context.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent("Error. Configuration already exists!"));
                return;
            }
        
            insertSuccess = await mySqlConnection.ExecuteAsync(
                "INSERT INTO LoungeSystemConfigurationIndex (GuildId, TargetChannelId, InterfaceChannelId, LoungeNamePattern) VALUES (@GuildId, @TargetChannelId, @InterfaceChannelId, @LoungeNamePattern)",
                newConfigRecord);
        
            await mySqlConnection.CloseAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, $"[{LoungeSystemPlugin.GetStaticPluginName()}] Error while Executing Mysql Operations on Lounge System Admin Command Module Setup Command");
            return;
        }
        

        if (insertSuccess == 0)
        {
            await context.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent("Error. Unable to insert new configuration record!"));
            return;
        }
        
        await context.EditResponseAsync(
            new DiscordWebhookBuilder().WithContent("Successfully created new configuration record!"));
        
    }

    [Command("setup")]
    public static async Task LoungeSetupCommand(CommandContext context)
    {
        await context.RespondAsync(LoungeSetupUiHelper.InitialMessageBuilder);
    }
    
    
}