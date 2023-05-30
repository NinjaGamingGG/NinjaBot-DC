using DSharpPlus;
using DSharpPlus.Net.Models;

namespace NinjaBot_DC.Extensions;

public static class ServerStats
{
    private static readonly PeriodicTimer ChannelInfoRefreshTimer = new(TimeSpan.FromSeconds(60));

    public static async Task RefreshServerStats(DiscordClient discordClient)
    {
        var sqlite = Worker.GetServiceSqLiteConnection();
        
        while (await ChannelInfoRefreshTimer.WaitForNextTickAsync())
        {
            /*
            var serverStatsModels = await sqlite.GetAllAsync<StatsChannelModel>();

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

                var editTaskList = new List<Task>()
                {
                    EditMemberCountChannelName(discordClient, memberCount, serverStatsModel.MemberCountChannelId),
                    EditBotCountChannelName(discordClient,botCount,serverStatsModel.BotCountChannelId),
                    EditTeamCountChannelName(discordClient,teamCount, serverStatsModel.TeamCountChannelId)
                };
                
                await Task.WhenAll(editTaskList);
            }
            */
        }
        
    }

    private static async Task EditMemberCountChannelName(DiscordClient discordClient, int count, ulong channelId)
    {
        var channel = await discordClient.GetChannelAsync(channelId);

        void NewEditModel(ChannelEditModel editModel)
        {
            editModel.Name = $"╔😎～Mitglieder: {count}";
        }

        await channel.ModifyAsync(NewEditModel);
    }
    
    private static async Task EditBotCountChannelName(DiscordClient discordClient, int count, ulong channelId)
    {
        var channel = await discordClient.GetChannelAsync(channelId);

        void NewEditModel(ChannelEditModel editModel)
        {
            editModel.Name = $"╠🤖～Bot Count: {count}";
        }

        await channel.ModifyAsync(NewEditModel);
    }
    
    private static async Task EditTeamCountChannelName(DiscordClient discordClient, int count, ulong channelId)
    {
        var channel = await discordClient.GetChannelAsync(channelId);

        void NewEditModel(ChannelEditModel editModel)
        {
            editModel.Name = $"╚🥷～Teammitglieder: {count}";
        }

        await channel.ModifyAsync(NewEditModel);
    }
}