using Dapper.Contrib.Extensions;

namespace RankSystem.Models;

[Table(("RanksystemConfigurationIndex"))]
public record RanksystemConfigurationModel
{
    [ExplicitKey]
    public ulong GuildId { get; set; }
    public int PointsPerMessage { get; set; }
    public int PointsPerReaction { get; set; }
    public float PointsPerVoiceActivity { get; set; }
    public ulong LogChannelId { get; set; }
    public ulong NotifyChannelId { get; set; }
}