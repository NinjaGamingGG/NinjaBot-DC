﻿using System.Runtime.InteropServices;
using Dapper;
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
    public async Task ReactionMessageEvent(CommandContext ctx, string argument, string messageTag)
#pragma warning restore CA1822
    {
    
    if (argument.ToLower()!= "setup")
        return;

    await HandleSetupArgument(ctx, messageTag.ToLower());
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

    [Command("ReactionMessage")]
    public async Task LinkRoleCommand(CommandContext context, string argument,string messageTag, DiscordRole discordRole, DiscordEmoji discordEmoji)
    {
        switch (argument)
        {
            case ("link-role"):
            {
                if (await CheckIfRoleClear(context, discordRole) == false)
                    return;

                var emojiTag = discordEmoji.GetDiscordName();

                var reactionRoleLinkDbModel = new ReactionRoleLinkDbModel()
                    {GuildId = context.Guild.Id, MessageTag = messageTag, LinkedRoleId = discordRole.Id, ReactionEmojiTag = emojiTag};

                await Worker.SqLiteConnection.InsertAsync(reactionRoleLinkDbModel);
            }
                break;

            case ("unlink-role"):
            {
                await UnLinkRole(context, messageTag, discordRole);
            }
                break;
        }

        


        


    }
    private static async Task UnLinkRole(CommandContext context, string messageTag, DiscordRole discordRole)
    {
        await Worker.SqLiteConnection.ExecuteAsync("DELETE FROM ReactionRoleIndex WHERE " +
                                                   $"(GuildId = {context.Guild.Id} " +
                                                   $"AND LinkedRoleId = {discordRole.Id} " +
                                                   $"AND MessageTag = '{messageTag}')");
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


        await CreateRoleBasedReaction(ctx, messageTag, msg);

        var reactionMessageDbModel = new ReactionMessageDbModel()
            {GuildId = ctx.Guild.Id, MessageId = msg.Id, MessageTag = messageTag};


        var updates = await Worker.SqLiteConnection.ExecuteAsync($"UPDATE ReactionMessagesIndex SET MessageId = {msg.Id} WHERE (GuildId = {ctx.Guild.Id} AND MessageTag = '{messageTag}')");
        
        if (updates == 0)
            await Worker.SqLiteConnection.InsertAsync(reactionMessageDbModel);
    }

    private static async Task<bool> CheckIfRoleClear(CommandContext context, DiscordRole discordRole)
    {
        var roleCount = await Worker.SqLiteConnection.QueryAsync(
                $"SELECT * FROM ReactionRoleIndex WHERE (GuildId = {context.Guild.Id} AND LinkedRoleId = {discordRole.Id})");

        if (roleCount.Count() == 1)
            return false;

        return true;
    }

    private static async Task CreateRoleBasedReaction(CommandContext context, string messageTag, DiscordMessage message)
    {
        //var roles = await Worker.SqLiteConnection.GetAllAsync<ReactionRoleLinkDbModel>();
        var roles = await Worker.SqLiteConnection.QueryAsync<ReactionRoleLinkDbModel>(
            $"SELECT * FROM ReactionRoleIndex WHERE (GuildId = {context.Guild.Id} AND MessageTag = MessageTag)");
        var rolesAsArray = roles.ToArray();

        for (var i = 0; i < rolesAsArray.Length; i++)
        {
            await message.CreateReactionAsync(DiscordEmoji.FromName(context.Client,rolesAsArray[i].ReactionEmojiTag));
        }
    }

}