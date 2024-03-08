using Dapper.Contrib.Extensions;
using DSharpPlus;
using LoungeSystemPlugin.Records;
using MySqlConnector;
using NinjaBot_DC;
using Serilog;

namespace LoungeSystemPlugin.PluginHelper;

public static class CleanupLounge
{
    public static async Task Execute(LoungeDbRecord loungeDbRecord)
    {
        var connectionString = LoungeSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnectionString();
        var discordClient = Worker.GetServiceDiscordClient();       
        var loungeChannel = await discordClient.GetChannelAsync(loungeDbRecord.ChannelId);
        var guild = await discordClient.GetGuildAsync(loungeDbRecord.GuildId);
        
        if (guild.Channels[loungeChannel.Id].Users.Count != 0)
            return;

        var channelExits  = await IsChannelInGuildAsync(discordClient, loungeDbRecord.ChannelId, loungeDbRecord.GuildId);

        if (channelExits)
        {
            await loungeChannel.DeleteAsync();
        }
        
        bool deleteSuccess;
        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
            
            deleteSuccess = await mySqlConnection.DeleteAsync(loungeDbRecord);
            await mySqlConnection.CloseAsync();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex, "Unable to delete Lounge Record on Initial Cleanup of LoungeSystem");
            return;
        }
        
        if (deleteSuccess == false)
            Log.Error("Unable to delete the Sql Record for Lounge {LoungeName} with the Id {LoungeId} in Guild {GuildId}",loungeChannel.Name, loungeDbRecord.ChannelId, loungeDbRecord.GuildId);
    }
    
    private static async Task<bool> IsChannelInGuildAsync(DiscordClient discordClient,ulong channelId, ulong guildId)
    {
        var guild = await discordClient.GetGuildAsync(guildId);

        var guildChannels = await guild.GetChannelsAsync();

        foreach (var guildChannel in guildChannels)
        {
            if (guildChannel.Id == channelId)
                return true;
        }
        
        return false;
    }
    
}