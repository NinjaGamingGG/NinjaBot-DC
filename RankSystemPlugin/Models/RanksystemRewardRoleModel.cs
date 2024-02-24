using Dapper.Contrib.Extensions;

namespace RankSystem.Models;

[Table("RankSystemRewardRolesIndex")]
public record RankSystemRewardRoleModel
{
    [ExplicitKey]
    public ulong GuildId { get; set; }
    public ulong RoleId { get; set; }
    public int RequiredPoints { get; set; }
}