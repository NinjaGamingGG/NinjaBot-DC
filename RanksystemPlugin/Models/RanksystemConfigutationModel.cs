using Dapper.Contrib.Extensions;

namespace RankSystem.Models;

[Table(("RanksystemConfiguration"))]
public record RanksystemConfigurationModel
{
    [ExplicitKey]
    public ulong GuildId { get; set; }
    public int PointsPerMessage { get; set; }
    public int PointsPerReaction { get; set; }
    public float PointsPerVoiceActivity { get; set; }
    public ulong LogChannel { get; set; }
}