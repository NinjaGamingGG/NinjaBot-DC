using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using Serilog;

namespace LoungeSystemPlugin.Events;

public static class ComponentInteractionCreated
{
    public static async Task RenameButtonPressed(DiscordClient sender, ComponentInteractionCreateEventArgs eventArgs)
    {
        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

        var builder = new DiscordFollowupMessageBuilder().WithContent("Please use *!l rename* to rename your channel");
        
        var message = await eventArgs.Interaction.CreateFollowupMessageAsync(builder);

        


    }
}