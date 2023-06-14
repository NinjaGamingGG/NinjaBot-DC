using Dapper.Contrib.Extensions;

namespace LoungeSystemPlugin.Records;

[Table("LoungeSystemConfigurationIndex")]
public record LoungeSystemConfigurationRecord
{
    [ExplicitKey]
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong TargetChannelId { get; set; }
    public ulong InterfaceChannelId { get; set; }
    public string? LoungeNamePattern { get; set; }
}