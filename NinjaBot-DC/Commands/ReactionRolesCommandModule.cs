using Dapper.Contrib.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using NinjaBot_DC.Models;

namespace NinjaBot_DC.Commands;

public class ReactionRolesCommandModule : BaseCommandModule
{
    [Command("ReactionMessage")]
    // ReSharper disable once UnusedMember.Global
#pragma warning disable CA1822
    public async Task ReactionMessageEvent(CommandContext ctx, params string[] arguments)
#pragma warning restore CA1822
    {
        if (arguments[0].ToLower() != "setup")
            return;
        switch (arguments[0].ToLower())
        {
            case ("setup"):
            {
                await HandleSetupArgument(ctx, arguments[1]);
            }
                break;

            case ("link-role"):
            {
                
            }
                break;
        }

        

        
    }

    private static async Task HandleSetupArgument(CommandContext context, string messageTag)
    {
        if (context.Member == null)
            return;
        
        switch (messageTag)
        {

            case ("ttv-yt"):
            {
                if ((context.Member.Permissions & Permissions.Administrator) == 0)
                    return;

                await SetupStreamMessage(context, messageTag);
            }
                break;
        }
    }

    public async Task LinkRoleArgument(CommandContext context, string roleName)
    {
        
    }

    private static async Task SetupStreamMessage(CommandContext ctx, string messageTag)
    {
        var ttvSuccess = ctx.Guild.Roles.TryGetValue(1041099026717745222, out var ttvRole);
        var ytSuccess = ctx.Guild.Roles.TryGetValue(1041099366687064184, out var ytRole);
        
        if (ttvSuccess == false || ytSuccess == false)
            return;
        
        var embed = new DiscordEmbedBuilder()
        {
            Title = "Bleibe up to date",
            Description = $"Wähle eine der untenstehenden Rollen und werde Automatisch über die neuesten Streams & Videos benachrichtigt.\n\n🔴 {ttvRole.Mention}\n📺 {ytRole.Mention}"
            
        };


        var msg = await new DiscordMessageBuilder()
            .WithEmbed(embed.Build())
            .SendAsync(ctx.Channel);

        await msg.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client,":red_circle:"));
        await msg.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":tv:"));

        var reactionMessageDbModel = new ReactionMessageDbModel()
            {GuildId = ctx.Guild.Id, MessageId = msg.Id, MessageTag = messageTag};

        await Worker.SqLiteConnection.InsertAsync(reactionMessageDbModel);
    }

}