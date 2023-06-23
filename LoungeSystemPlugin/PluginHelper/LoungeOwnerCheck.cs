using Dapper;
using DSharpPlus.Entities;

namespace LoungeSystemPlugin.PluginHelper;

public class LoungeOwnerCheck
{
    public static async Task<bool> IsLoungeOwnerAsync(DiscordMember member, DiscordChannel channel, DiscordGuild guild)
    {
        var sqLiteConnection = SqLiteHelper.GetSqLiteConnection();
        
        if (ReferenceEquals(member, null))
            return false;
        
        var existsAsOwner = await sqLiteConnection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM LoungeIndex WHERE ChannelId = @ChannelId AND OwnerId = @OwnerId AND GuildId = @GuildId",
            new {ChannelId = channel.Id ,OwnerId = member.Id, GuildId = guild.Id});
        
        return existsAsOwner != 0;
    }
    
    public static async Task<ulong> GetOwnerIdAsync(DiscordChannel channel)
    {
        var sqLiteConnection = SqLiteHelper.GetSqLiteConnection();
        
        var ownerId = await sqLiteConnection.ExecuteScalarAsync<ulong>(
            "SELECT OwnerId FROM LoungeIndex WHERE ChannelId = @ChannelId",
            new {ChannelId = channel.Id});
        
        return ownerId;
    }
}