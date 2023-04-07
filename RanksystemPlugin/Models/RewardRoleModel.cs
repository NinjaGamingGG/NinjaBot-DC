using Dapper.Contrib.Extensions;

namespace RankSystem.Models;

[Table("RewardRolesIndex")]
public record RewardRoleModel
{
    [ExplicitKey]
    public ulong GuildId { get; set; }
    public ulong RoleId { get; set; }
    public int RequiredPoints { get; set; }
}