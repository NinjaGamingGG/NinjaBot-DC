using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus;
using DSharpPlus.EventArgs;
using NinjaBot_DC.Models;
// ReSharper disable All

namespace NinjaBot_DC.Extensions;

public static class ReactionRoles
{
    public static async Task MessageReactionAdded(DiscordClient client, MessageReactionAddEventArgs eventArgs)
    {
        var sqlite = Worker.GetServiceSqLiteConnection();
        var reactionMessages = await sqlite.GetAllAsync<ReactionMessageDbModel>();

        var reactionMessageAsList = reactionMessages.ToList();

        for (var i = 0; i < reactionMessageAsList.Count; i++)
        {
            var reactionMessage = reactionMessageAsList[i];
            
            await HandleAddedReactionAsync(reactionMessage,client, eventArgs);
        }
    }

    private static async Task HandleAddedReactionAsync(ReactionMessageDbModel reactionMessageDbModel,DiscordClient client,  MessageReactionAddEventArgs eventArgs)
    {
        if (reactionMessageDbModel.MessageId != eventArgs.Message.Id)
            return;

        var sqlite = Worker.GetServiceSqLiteConnection();
        
        var roles = await sqlite.QueryAsync<ReactionRoleLinkDbModel>
            ($"SELECT * FROM ReactionRoleIndex WHERE (GuildId = {eventArgs.Guild.Id} " +
             $"AND ReactionEmojiTag = '{eventArgs.Emoji.GetDiscordName()}'" +
             $"AND MessageTag = '{reactionMessageDbModel.MessageTag}')");

        var rolesAsList = roles.ToList();
        
        for (var i = 0; i < rolesAsList.Count; i++)
        {
            var role = rolesAsList[i];
            var discordGuild = await client.GetGuildAsync(role.GuildId);

            var member = await discordGuild.GetMemberAsync(eventArgs.User.Id);

            var newRole = discordGuild.GetRole(role.LinkedRoleId);

            await member.GrantRoleAsync(newRole);

        }
    }
    
    public static async Task MessageReactionRemoved(DiscordClient client, MessageReactionRemoveEventArgs eventArgs)
    {
        
    }
}