using Dapper.Contrib.Extensions;

namespace LoungeSystemPlugin.Records;

[Table("RequiredRoleIndex")]
public record RequiredRoleRecord
{
   [ExplicitKey]
   int Id { get; set; }
   
   public ulong GuildId { get; set; }
   
   public ulong ChannelId { get; set; }
   
   public ulong RoleId { get; set; }
}