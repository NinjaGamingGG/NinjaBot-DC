using Dapper.Contrib.Extensions;

namespace NinjaBot_DC.Models;

[Table(("LoungeIndex"))]
public class LoungeDbModel
{
    [ExplicitKey]
    public ulong ChannelId { get; set; }
    public ulong OwnerId { get; set; }
    public ulong GuildId { get; set; }
}