using Dapper.Contrib.Extensions;

namespace NinjaBot_DC.Models.LoungeSystemModels;

[Table(("LoungeIndex"))]
public record LoungeDbModel
{
    [ExplicitKey]
    public ulong ChannelId { get; set; }
    public ulong OwnerId { get; set; }
    public ulong GuildId { get; set; }
}