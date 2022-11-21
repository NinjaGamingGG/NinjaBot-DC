using Dapper.Contrib.Extensions;
using DSharpPlus;
using DSharpPlus.EventArgs;
using NinjaBot_DC.Models;

namespace NinjaBot_DC.Extensions;

public static class ReactionRoles
{
    public static async Task MessageReactionAdded(DiscordClient client, MessageReactionAddEventArgs eventArgs)
    {
        var reactionMessages = await Worker.SqLiteConnection.GetAllAsync<ReactionMessageDbModel>();
        
        foreach (var reactionMessageDbModel in reactionMessages)
        {
            await HandleAddedReactionAsync(reactionMessageDbModel, eventArgs.Message.Id);
        }

        //eventArgs.Emoji.Id
        
    }

    public static async Task HandleAddedReactionAsync(ReactionMessageDbModel reactionMessageDbModel, ulong eventMessageId)
    {
        if (reactionMessageDbModel.MessageId != eventMessageId)
            return;
        
        switch (reactionMessageDbModel.MessageTag)
        {
            case ("ttv-yt"):
            {
                    
            }
                break;
        }
    }

    public static async Task HandleTwitchYoutubeReaction()
    {
        
    }
    
    

    public static async Task MessageReactionRemoved(DiscordClient client, MessageReactionRemoveEventArgs eventArgs)
    {
        
    }
}