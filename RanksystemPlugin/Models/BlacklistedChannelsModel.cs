using Dapper.Contrib.Extensions;

namespace RankSystem.Models;

[Table("BlacklistedChannelsIndex")]
public record BlacklistedChannelsModel
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
}