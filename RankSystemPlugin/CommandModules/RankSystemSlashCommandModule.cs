using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using RankSystem.PluginHelper;

namespace RankSystem.CommandModules;

[SlashCommandGroup("RankSystem", "RankSystem Plugin Commands",true)]
public class RankSystemSlashCommandModule : ApplicationCommandModule
{
    [SlashCommand("Rank", "Check the RankSystem Points of a user")]

    // ReSharper disable once UnusedMember.Global
    public async Task RankCommand(InteractionContext context, [Option("user", "User to check")] DiscordUser? targetUser = null)
    {
        await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        
        //If the Target User is null (wasn't specified in command) the executing user is the target user
        targetUser ??= context.User;

        //true if user is the target user
        var isUserTarget = targetUser.Id == context.User.Id;

        await context.EditResponseAsync(await UserRankDisplayHelper.BuildDisplayAsync(context.Guild, targetUser, isUserTarget));
    }
}