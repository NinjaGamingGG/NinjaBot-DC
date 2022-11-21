namespace NinjaBot_DC.Models;

public class ReactionRoleLinkDbModel
{
    public ulong GuildId { get; set; }
    public string MessageTag { get; set; }
    public ulong ReactionEmojiId { get; set; }
    public ulong LinkedRoleId { get; set; }
}