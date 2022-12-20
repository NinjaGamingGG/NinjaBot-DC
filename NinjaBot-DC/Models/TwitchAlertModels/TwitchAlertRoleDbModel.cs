using Dapper.Contrib.Extensions;

namespace NinjaBot_DC.Models.TwitchAlertModels;

[Table("TwitchAlertRoleIndex")]
public record TwitchAlertRoleDbModel
{
    [ExplicitKey]
    public ulong GuildId { get; set; }  //The Id of the Discord Guild
    
    public ulong RoleId { get; set; }   //The Id of the Discord Role
    
    public string RoleTag { get; set; } = string.Empty;     //The tag of the role
}