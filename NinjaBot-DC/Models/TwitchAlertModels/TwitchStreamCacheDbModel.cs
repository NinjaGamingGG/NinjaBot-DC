using Dapper.Contrib.Extensions;

namespace NinjaBot_DC.Models.TwitchAlertModels;

[System.ComponentModel.DataAnnotations.Schema.Table("TwitchStreamCacheIndex")]
public record TwitchStreamCacheDbModel
{
    [ExplicitKey]
    public string Id { get; init; } = string.Empty; //Id of the Twitch Stream or vod
    public string ChannelName { get; init; } = string.Empty; //Name of the Channel
    public string ChannelId { get; init; } = string.Empty; //ChannelId
    
}