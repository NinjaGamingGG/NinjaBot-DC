using Dapper.Contrib.Extensions;

namespace LoungeSystemPlugin.Records;

[Table("LoungeMessageReplacementIndex")]
public record LoungeMessageReplacement
{
    [ExplicitKey]
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong TargetChannelId { get; set; }
    public string? ReplacementHandle { get; set; }
    public string? ReplacementValue { get; set; } 
    
}