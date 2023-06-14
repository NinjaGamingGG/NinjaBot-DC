using Dapper.Contrib.Extensions;

namespace LoungeSystemPlugin.Records;

[Table("LoungeIndex")]
public record LoungeDbRecord
{
    [ExplicitKey]
    public ulong ChannelId { get; set; }
    public ulong GuildId { get; set; }
    public ulong OwnerId { get; set; }

}