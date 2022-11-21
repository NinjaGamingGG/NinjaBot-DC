using Dapper.Contrib.Extensions;

namespace NinjaBot_DC.Models;

[Table("ReactionMessagesIndex")]
public class ReactionMessageDbModel
{
    [ExplicitKey]
    public ulong GuildId { get; set; }
    
    public ulong MessageId { get; set; }
    
    public string MessageTag { get; set; }
}