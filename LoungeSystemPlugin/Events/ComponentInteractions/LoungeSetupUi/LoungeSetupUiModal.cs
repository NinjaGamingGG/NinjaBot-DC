using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using Serilog;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeSetupUi;

public static class LoungeSetupUiModal
{
    internal static async Task ModalSubmitted(DiscordClient sender, ModalSubmittedEventArgs eventArgs)
    {
        var message = eventArgs.Interaction.Message;
        
        if (message == null)
            return;

        await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,
            LoungeSetupUiHelper.ModalSubmittedResponseBuilder);

    }
}