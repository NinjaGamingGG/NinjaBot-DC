using Dapper.Contrib.Extensions;

namespace NinjaBot_DC.Models;

[System.ComponentModel.DataAnnotations.Schema.Table("TwitchStreamCacheIndex")]
public record TwitchStreamCacheDbModel
{
    [ExplicitKey]
    public string Id { get; set; }//Id of the Twitch Stream or vod
    public string ChannelName { get; set; }//Name of the Channel
    public string ChannelId { get; set; }//ChannelId
    
}