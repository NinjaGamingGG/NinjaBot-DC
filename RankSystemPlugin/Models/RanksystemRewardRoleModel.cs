using Dapper.Contrib.Extensions;

namespace RankSystem.Models;

[Table("RanksystemRewardRolesIndex")]
public record RanksystemRewardRoleModel
{
    [ExplicitKey]
    public ulong GuildId { get; set; }
    public ulong RoleId { get; set; }
    public int RequiredPoints { get; set; }
}