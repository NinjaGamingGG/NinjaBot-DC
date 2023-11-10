using Dapper;
using DSharpPlus.Entities;

namespace LoungeSystemPlugin.PluginHelper;

public class LoungeOwnerCheck
{
    public static async Task<bool> IsLoungeOwnerAsync(DiscordMember member, DiscordChannel channel, DiscordGuild guild)
    {
        var mySqlConnection = LoungeSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();
        
        if (ReferenceEquals(member, null))
            return false;
        
        var existsAsOwner = await mySqlConnection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM LoungeIndex WHERE ChannelId = @ChannelId AND OwnerId = @OwnerId AND GuildId = @GuildId",
            new {ChannelId = channel.Id ,OwnerId = member.Id, GuildId = guild.Id});
        
        return existsAsOwner != 0;
    }
    
    public static async Task<ulong> GetOwnerIdAsync(DiscordChannel channel)
    {
        var mySqlConnection = LoungeSystemPlugin.GetMySqlConnectionHelper().GetMySqlConnection();;
        
        var ownerId = await mySqlConnection.ExecuteScalarAsync<ulong>(
            "SELECT OwnerId FROM LoungeIndex WHERE ChannelId = @ChannelId",
            new {ChannelId = channel.Id});
        
        return ownerId;
    }
}