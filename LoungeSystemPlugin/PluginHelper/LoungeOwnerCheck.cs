using Dapper;
using DSharpPlus.Entities;
using MySqlConnector;

namespace LoungeSystemPlugin.PluginHelper;

public static class LoungeOwnerCheck
{
    public static async Task<bool> IsLoungeOwnerAsync(DiscordMember member, DiscordChannel channel, DiscordGuild guild)
    {
        var connectionString = LoungeSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnectionString();
        var mySqlConnection = new MySqlConnection(connectionString);
        
        if (ReferenceEquals(member, null))
            return false;
        
        var existsAsOwner = await mySqlConnection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM LoungeIndex WHERE ChannelId = @ChannelId AND OwnerId = @OwnerId AND GuildId = @GuildId",
            new {ChannelId = channel.Id ,OwnerId = member.Id, GuildId = guild.Id});

        await mySqlConnection.CloseAsync();
        
        return existsAsOwner != 0;
    }
    
    public static async Task<ulong> GetOwnerIdAsync(DiscordChannel channel)
    {
        var connectionString = LoungeSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnectionString();
        var mySqlConnection = new MySqlConnection(connectionString);
        
        var ownerId = await mySqlConnection.ExecuteScalarAsync<ulong>(
            "SELECT OwnerId FROM LoungeIndex WHERE ChannelId = @ChannelId",
            new {ChannelId = channel.Id});

        await mySqlConnection.CloseAsync();
        
        return ownerId;
    }
}