using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus;
using DSharpPlus.Net.Models;
using Serilog;
using StatsPlugin.Models;

namespace StatsPlugin.PluginHelper;

public static class RefreshServerStats
{
    private static readonly PeriodicTimer RefreshTimer = new(TimeSpan.FromSeconds(10));
    
    public static async Task Execute(DiscordClient discordClient)
    {
        var sqlite = SqLiteConnectionHelper.GetSqLiteConnection();
        
        while (await RefreshTimer.WaitForNextTickAsync())
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
                
                
                foreach (var member in allGuildMembers)
                {
                    var roles = member.Roles;

                    foreach (var role in roles)
                    {
                        switch (role.Name)
                        {
                            case "🥷​ Team":
                                teamCount++;
                                break;
                            case "🤖​ Bot":
                                botCount++;
                                break;
                        }
                    }
                }
                
                var taskList = new List<Task>()
                {
                    GetChannelNameFromEnum(discordClient, SlashCommandModule.ChannelHandleEnum.MemberChannel, serverStatsModel, memberCount, botCount, teamCount),
                    GetChannelNameFromEnum(discordClient, SlashCommandModule.ChannelHandleEnum.BotChannel, serverStatsModel, memberCount, botCount, teamCount),
                    GetChannelNameFromEnum(discordClient, SlashCommandModule.ChannelHandleEnum.TeamChannel, serverStatsModel, memberCount, botCount, teamCount),
                };

                await Task.WhenAll(taskList);
            }
            
        }
        
    }

    private static async Task GetChannelNameFromEnum(DiscordClient discordClient, SlashCommandModule.ChannelHandleEnum channelEnum, StatsChannelIndexModel serverStatsModel, int memberCount, int botCount, int teamCount)
    {
        var sqlite = SqLiteConnectionHelper.GetSqLiteConnection();
        
         var channelHandleAsString = DatabaseHandleHelper.GetHandleFromEnum(channelEnum);
            IEnumerable<StatsChannelCustomNamesIndex> customNameRecord;

            switch (channelEnum)
            {
                case SlashCommandModule.ChannelHandleEnum.MemberChannel:
                    customNameRecord = await sqlite.QueryAsync<StatsChannelCustomNamesIndex>(
                        "SELECT CustomName FROM StatsChannelCustomNamesIndex WHERE GuildId = @GuildId and ChannelHandle = @ChannelHandle",
                        new {serverStatsModel.GuildId, ChannelHandle = channelHandleAsString});
                    
                    await HandleCustomNameRecord(discordClient, serverStatsModel, memberCount, customNameRecord, "Members {count}", serverStatsModel.MemberCountChannelId);
                    break;

                case SlashCommandModule.ChannelHandleEnum.BotChannel:
                    customNameRecord = await sqlite.QueryAsync<StatsChannelCustomNamesIndex>(
                        "SELECT CustomName FROM StatsChannelCustomNamesIndex WHERE GuildId = @GuildId and ChannelHandle = @ChannelHandle",
                        new {serverStatsModel.GuildId, ChannelHandle = channelHandleAsString});
                    await HandleCustomNameRecord(discordClient, serverStatsModel, botCount, customNameRecord, "Bots {count}", serverStatsModel.BotCountChannelId);
                    break;

                case SlashCommandModule.ChannelHandleEnum.TeamChannel:
                    customNameRecord = await sqlite.QueryAsync<StatsChannelCustomNamesIndex>(
                        "SELECT CustomName FROM StatsChannelCustomNamesIndex WHERE GuildId = @GuildId and ChannelHandle = @ChannelHandle",
                        new {serverStatsModel.GuildId, ChannelHandle = channelHandleAsString});
                    await HandleCustomNameRecord(discordClient, serverStatsModel, teamCount, customNameRecord, "Team {count}", serverStatsModel.TeamCountChannelId);
                    break;

                case SlashCommandModule.ChannelHandleEnum.CategoryChannel:
                case SlashCommandModule.ChannelHandleEnum.NoChannel:
                default:
                    return;
            }
        
    }

    private static async Task HandleCustomNameRecord(DiscordClient discordClient, StatsChannelIndexModel serverStatsModel,
        int memberCount, IEnumerable<StatsChannelCustomNamesIndex> customNameRecord, string defaultName, ulong channelId)
     {
        var recordAsList = customNameRecord.ToList();

        string channelName;

        if (!recordAsList.Any())
        {
            channelName = defaultName;

            await EditChannelName(discordClient, memberCount, channelId,
                channelName);

            return;
        }


        var channelRecord = recordAsList.First();

        channelName = string.IsNullOrEmpty(channelRecord.CustomName) ? defaultName : channelRecord.CustomName;


        await EditChannelName(discordClient, memberCount, serverStatsModel.MemberCountChannelId,
            channelName);
    }

    private static async Task EditChannelName(DiscordClient discordClient, int count, ulong channelId, string newChanelName)
    {
        var channel = await discordClient.GetChannelAsync(channelId);
        
        if (newChanelName.Contains($"{count}"))
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
    
    
    
}