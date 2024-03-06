using Dapper.Contrib.Extensions;

namespace LoungeSystemPlugin.Records;

[Table("LoungeMessageReplacementIndex")]
public record LoungeMessageReplacement
{
    [ExplicitKey]
    public int Id { get; init; }
    public ulong GuildId { get; init; }
    public ulong TargetChannelId { get; init; }
    public string? ReplacementHandle { get; init; }
    public string? ReplacementValue { get; init; } 
    
}