using System.ComponentModel.DataAnnotations.Schema;

namespace NinjaBot_DC.Models;

[Table("TwitchDiscordChannelIndex")]
public record TwitchDiscordChannelDbModel
{
    public ulong GuildId { get; set; }  //The Id of the Discord Guild
    
    public ulong ChannelId { get; set; }    //The Id of the Linked Discord Channel
    
    public string RoleTag { get; set; } //The id of the Linked Role
}