using Dapper.Contrib.Extensions;

namespace NinjaBot_DC.Models.TwitchAlertModels;

[Table("TwitchCreatorSocialMediaChannelIndex")]
public record TwitchCreatorSocialMediaChannelDbModel
{
    public ulong GuildId { get; set; }  //The id of the discord guild
    
    public ulong UserId { get; set; }   //The id of the discord user
    
    public string RoleTag { get; set; } = string.Empty;//The id of the discord role
    
    public string SocialMediaChannel { get; set; }  = string.Empty;//The name of the Social Media Channel
    
    public string Platform { get; set; } = string.Empty;   //The name of the Social Media Platform
    
}