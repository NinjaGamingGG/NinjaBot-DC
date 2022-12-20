using Dapper.Contrib.Extensions;

namespace NinjaBot_DC.Models.ReactionRoleModels;

[Table("ReactionRoleIndex")]
public record ReactionRoleLinkDbModel
{
    [ExplicitKey]
    public ulong GuildId { get; set; }
    public string MessageTag { get; set; }
    public string ReactionEmojiTag { get; set; }
    public ulong LinkedRoleId { get; set; }
}