using Dapper.Contrib.Extensions;

namespace NinjaBot_DC.Models.ReactionRoleModels;

[Table("ReactionRoleIndex")]
public record ReactionRoleLinkDbModel
{
    [ExplicitKey]
    public ulong GuildId { get; set; }
    public string MessageTag { get; set; } = string.Empty;
    public string ReactionEmojiTag { get; set; } = string.Empty;
    public ulong LinkedRoleId { get; set; }
}