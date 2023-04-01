using NinjaBot_DC;
using Serilog;

namespace Ranksystem;

public static class InitializeSqLiteTables
{
    public static void Init()
    {
        Log.Information("[RankSystem] Initializing SQLite Tables...");
        var sqLiteConnection = Worker.GetServiceSqLiteConnection();

        using var sqLiteBlackListedChannelTableCommand = sqLiteConnection.CreateCommand();
        {
            sqLiteBlackListedChannelTableCommand.CommandText =
                "CREATE TABLE IF NOT EXISTS BlacklistedChannelsIndex (GuildId INTEGER, ChannelId INTEGER)";

            sqLiteBlackListedChannelTableCommand.ExecuteNonQuery();
        }
        
        using var sqLiteBlackListedRoleTableCommand = sqLiteConnection.CreateCommand();
        {
            sqLiteBlackListedRoleTableCommand.CommandText =
                "CREATE TABLE IF NOT EXISTS BlacklistedRolesIndex (GuildId INTEGER, RoleId INTEGER)";

            sqLiteBlackListedRoleTableCommand.ExecuteNonQuery();
        }
        
        


    }
}