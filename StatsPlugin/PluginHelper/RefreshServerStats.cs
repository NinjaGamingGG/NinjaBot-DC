using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus;
using DSharpPlus.Net.Models;
using Serilog;
using StatsPlugin.Models;

namespace StatsPlugin.PluginHelper;

public static class RefreshServerStats
{
    private static readonly PeriodicTimer RefreshTimer = new(TimeSpan.FromMinutes(10));
    
    public static async Task Execute(DiscordClient discordClient)
    {
        var sqlite = SqLiteConnectionHelper.GetSqLiteConnection();

        do
        {
            Log.Debug("[Server Stats] Refreshing Server Stats");

            var serverStatsModels = await sqlite.GetAllAsync<StatsChannelIndexModel>();

            foreach (var serverStatsModel in serverStatsModels)
            {
                var guild = await discordClient.GetGuildAsync(serverStatsModel.GuildId);

                var allGuildMembers = await guild.GetAllMembersAsync();

                var memberCount = allGuildMembers.Count;

                var botCount = 0;
                var teamCount = 0;

                var teamHandle = DatabaseHandleHelper.GetRoleHandleFromEnum(SlashCommandModule.RoleHandleEnum.TeamRole);
                var botHandle = DatabaseHandleHelper.GetRoleHandleFromEnum(SlashCommandModule.RoleHandleEnum.BotRole);

                var teamRoles = await sqlite.QueryAsync<StatsChannelLinkedRoleIndex>(
                    "SELECT * FROM StatsChannelLinkedRolesIndex WHERE GuildId = @GuildId AND RoleHandle = @RoleHandle",
                    new { serverStatsModel.GuildId, RoleHandle = teamHandle });
                var botRoles = await sqlite.QueryAsync<StatsChannelLinkedRoleIndex>(
                    "SELECT * FROM StatsChannelLinkedRolesIndex WHERE GuildId = @GuildId AND RoleHandle = @RoleHandle",
                    new { serverStatsModel.GuildId, RoleHandle = botHandle });

                var teamRoleIdsAsList = teamRoles.Select(role => role.RoleId).ToArray();
                var botRoleIdsAsList = botRoles.Select(role => role.RoleId).ToArray();


                foreach (var member in allGuildMembers)
                {
                    var roles = member.Roles;

                    foreach (var role in roles)
                    {
                        if (teamRoleIdsAsList.Contains(role.Id))
                            teamCount++;

                        if (botRoleIdsAsList.Contains(role.Id))
                            botCount++;

                    }
                }

                var taskList = new List<Task>()
                {
                    GetChannelNameFromEnum(discordClient, SlashCommandModule.ChannelHandleEnum.CategoryChannel,
                        serverStatsModel, memberCount, botCount, teamCount, guild.Id),
                    GetChannelNameFromEnum(discordClient, SlashCommandModule.ChannelHandleEnum.MemberChannel,
                        serverStatsModel, memberCount, botCount, teamCount, guild.Id),
                    GetChannelNameFromEnum(discordClient, SlashCommandModule.ChannelHandleEnum.BotChannel,
                        serverStatsModel, memberCount, botCount, teamCount, guild.Id),
                    GetChannelNameFromEnum(discordClient, SlashCommandModule.ChannelHandleEnum.TeamChannel,
                        serverStatsModel, memberCount, botCount, teamCount, guild.Id),
                };

                await Task.WhenAll(taskList);
            }

        } while (await RefreshTimer.WaitForNextTickAsync());

    }

    private static async Task GetChannelNameFromEnum(DiscordClient discordClient, SlashCommandModule.ChannelHandleEnum channelEnum, StatsChannelIndexModel serverStatsModel, int memberCount, int botCount, int teamCount, ulong guildId)
    {
        var sqlite = SqLiteConnectionHelper.GetSqLiteConnection();
        
         var channelHandleAsString = DatabaseHandleHelper.GetChannelHandleFromEnum(channelEnum);
            var customNameRecord = await sqlite.QueryAsync<StatsChannelCustomNamesIndex>(
                "SELECT CustomName FROM StatsChannelCustomNamesIndex WHERE GuildId = @GuildId and ChannelHandle = @ChannelHandle",
                new {serverStatsModel.GuildId, ChannelHandle = channelHandleAsString});

            switch (channelEnum)
            {
                case SlashCommandModule.ChannelHandleEnum.MemberChannel:
                    await HandleCustomNameRecord(discordClient, serverStatsModel, memberCount, customNameRecord, "Members {count}", serverStatsModel.MemberCountChannelId, guildId, SlashCommandModule.ChannelHandleEnum.MemberChannel);
                    break;

                case SlashCommandModule.ChannelHandleEnum.BotChannel:
                    await HandleCustomNameRecord(discordClient, serverStatsModel, botCount, customNameRecord, "Bots {count}", serverStatsModel.BotCountChannelId, guildId, SlashCommandModule.ChannelHandleEnum.BotChannel);
                    break;

                case SlashCommandModule.ChannelHandleEnum.TeamChannel:
                    await HandleCustomNameRecord(discordClient, serverStatsModel, teamCount, customNameRecord, "Team {count}", serverStatsModel.TeamCountChannelId, guildId, SlashCommandModule.ChannelHandleEnum.TeamChannel);
                    break;

                case SlashCommandModule.ChannelHandleEnum.CategoryChannel:
                    await HandleCustomNameRecord(discordClient, serverStatsModel, 0, customNameRecord, "-Stats-", serverStatsModel.CategoryChannelId, guildId, SlashCommandModule.ChannelHandleEnum.CategoryChannel);
                    break;
                
                case SlashCommandModule.ChannelHandleEnum.NoChannel:
                default:
                    return;
            }
        
    }

    private static async Task HandleCustomNameRecord(DiscordClient discordClient, StatsChannelIndexModel serverStatsModel,
        int memberCount, IEnumerable<StatsChannelCustomNamesIndex> customNameRecord, string defaultName, ulong channelId, ulong guildId, SlashCommandModule.ChannelHandleEnum handleEnum)
     {
        var recordAsList = customNameRecord.ToList();

        string channelName;

        if (!recordAsList.Any())
        {
            channelName = defaultName;

            await EditChannelName(discordClient, memberCount, channelId,
                channelName, guildId);

            return;
        }


        var channelRecord = recordAsList.First();

        channelName = string.IsNullOrEmpty(channelRecord.CustomName) ? defaultName : channelRecord.CustomName;

        switch (handleEnum)
        {
            case SlashCommandModule.ChannelHandleEnum.MemberChannel:
                await EditChannelName(discordClient, memberCount, serverStatsModel.MemberCountChannelId,
                    channelName, guildId);
                break;
            
            case SlashCommandModule.ChannelHandleEnum.TeamChannel:
                await EditChannelName(discordClient, memberCount, serverStatsModel.TeamCountChannelId,
                    channelName, guildId);
                break;
            
            case SlashCommandModule.ChannelHandleEnum.BotChannel:
                await EditChannelName(discordClient, memberCount, serverStatsModel.BotCountChannelId,
                    channelName, guildId);
                break;
            
            case SlashCommandModule.ChannelHandleEnum.CategoryChannel:
                await EditChannelName(discordClient, memberCount, serverStatsModel.CategoryChannelId,
                    channelName, guildId);
                break;

        }
     }

    private static async Task EditChannelName(DiscordClient discordClient, int count, ulong channelId, string newChanelName, ulong guildId)
    {
        try
        {
            var channel = await discordClient.GetChannelAsync(channelId);
            
            if (newChanelName.Contains("{count}"))
                newChanelName = newChanelName.Replace("{count}", count.ToString());
        
        
            if (channel.Name == newChanelName)
            {
                return;
            }

            void NewEditModel(ChannelEditModel editModel)
            {
                editModel.Name = newChanelName;
            }

            await channel.ModifyAsync(NewEditModel);
        }
        catch (DSharpPlus.Exceptions.NotFoundException e)
        {
            Log.Fatal(e,"[Server Stats] Channel not found while trying to edit channel name: {ChannelName} ChannelId: {ChannelId} on Guild {GuildId}",newChanelName, channelId , guildId);
        }
    }
}