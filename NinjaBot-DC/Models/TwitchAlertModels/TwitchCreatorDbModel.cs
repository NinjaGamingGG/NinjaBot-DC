using Dapper.Contrib.Extensions;

namespace NinjaBot_DC.Models;

[Table("TwitchCreatorIndex")]
public record TwitchCreatorDbModel
{
    [ExplicitKey]
    public ulong GuildId { get; set; }//The id of the Discord guild
    
    public ulong UserId { get; set; }//The Id of the Discord user
    
    public string RoleTag { get; set; }//The tag of the Role
}