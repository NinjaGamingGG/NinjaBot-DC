using Dapper.Contrib.Extensions;

namespace RankSystem.Models;

[Table("RankSystemUserPointsIndex")]
public record UserPointModel
{
    [ExplicitKey]
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public int Points { get; set; }
}