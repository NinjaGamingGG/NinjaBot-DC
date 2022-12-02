using Dapper.Contrib.Extensions;

namespace NinjaBot_DC.Models;

[Table("TwitchCreatorSocialMediaChannelIndex")]
public record TwitchCreatorSocialMediaChannelModel
{
    public ulong GuildId { get; set; }  //The id of the discord guild
    
    public ulong UserId { get; set; }   //The id of the discord user
    
    public string RoleTag { get; set; } //The id of the discord role
    
    public string SocialMediaChannel { get; set; }  //The name of the Social Media Channel
    
    public string Platform { get; set; }    //The name of the Social Media Platform
    
}