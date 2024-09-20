namespace LoungeSystemPlugin.Records.Cache;

public record LoungeSetupRecord(ulong ChannelId ,ulong UserId, string NamePattern, string NameDecorator, bool HasInternalInterface);