using Dapper.Contrib.Extensions;

namespace RankSystem.Models;

[Table("BlacklistedRolesIndex")]
public record BlacklistedRolesModel
{
    [ExplicitKey]
    public ulong GuildId { get; set; }
    public ulong RoleId { get; set; }
}