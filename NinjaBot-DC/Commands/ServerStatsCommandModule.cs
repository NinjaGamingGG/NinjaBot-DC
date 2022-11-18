using Dapper.Contrib.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using NinjaBot_DC.Models;

namespace NinjaBot_DC.Commands;

// ReSharper disable once ClassNeverInstantiated.Global
public class ServerStatsCommandModule : BaseCommandModule
{
    [Command("stat-channels")]
    // ReSharper disable once UnusedMember.Global
#pragma warning disable CA1822
    public async Task StatChannelsCommand(CommandContext ctx, string argument)
#pragma warning restore CA1822
    {
        if (argument.ToLower() == "setup")
        {
            await SetupChannels(ctx);
        }
        
    }
    
    private static async Task SetupChannels(CommandContext context)
    {
        var guild = context.Guild;

        var newCategory = await guild.CreateChannelCategoryAsync(@"· • ●  📊 Stats 📊 ● • ·");
        
        if (newCategory == null)
            return;

        var memberCountChannel = await guild.CreateChannelAsync("╔😎～Mitglieder:", ChannelType.Voice, newCategory);
        var botCountChannel = await guild.CreateChannelAsync("╠🤖～Bot Count:", ChannelType.Voice, newCategory);
        var teamCountChannel = await guild.CreateChannelAsync("╚🥷～Teammitglieder:", ChannelType.Voice, newCategory);

        var statsChannelModel = new StatsChannelModel()
        {
            GuildId = guild.Id, 
            CategoryChannelId = newCategory.Id, 
            MemberCountChannelId = memberCountChannel.Id, 
            BotCountChannelId = botCountChannel.Id, 
            TeamCountChannelId = teamCountChannel.Id
        };

        var hasUpdated = await Worker.SqLiteConnection.UpdateAsync(statsChannelModel);

        if (hasUpdated == false)
            await Worker.SqLiteConnection.InsertAsync(statsChannelModel);
    }
}