using Dapper.Contrib.Extensions;

namespace NinjaBot_DC.Models.ReactionRoleModels;

[Table("ReactionMessagesIndex")]
public record ReactionMessageDbModel
{
    [ExplicitKey]
    public ulong GuildId { get; set; }
    
    public ulong MessageId { get; set; }
    
    public string MessageTag { get; init; }
}