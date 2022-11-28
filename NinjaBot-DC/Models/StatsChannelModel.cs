using Dapper.Contrib.Extensions;

namespace NinjaBot_DC.Models;

[Table("StatsChannelIndex")]
public record StatsChannelModel
{
    [ExplicitKey]
    public ulong GuildId { get; set; }
    
    public ulong CategoryChannelId { get; set; }
    
    public ulong MemberCountChannelId { get; set; }
    
    public ulong TeamCountChannelId { get; set; }
    
    public ulong BotCountChannelId { get; set; }
}