using Dapper.Contrib.Extensions;

namespace LoungeSystemPlugin.Records;

[Table("LoungeSystemConfigurationIndex")]
public record LoungeSystemConfigurationRecord
{
    [ExplicitKey]
    public int Id { get; init; }
    public ulong GuildId { get; init; }
    public ulong TargetChannelId { get; init; }
    public ulong InterfaceChannelId { get; init; }
    public string? LoungeNamePattern { get; init; }
}