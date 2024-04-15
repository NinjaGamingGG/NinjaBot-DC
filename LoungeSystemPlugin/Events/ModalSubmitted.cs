using DSharpPlus;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.Events.ModalsSubmitted;


namespace LoungeSystemPlugin.Events;

internal static class ModalSubmitted
{
    internal static async Task OnModalSubmitted(DiscordClient sender, ModalSubmitEventArgs eventArgs)
    {
        switch (eventArgs.Interaction.Data.CustomId)
        {
                case "lounge_rename_modal":
                    await LoungeRenameModal.WasSubmitted(sender, eventArgs);
                    break;
        }
        
        await eventArgs.Interaction.DeleteOriginalResponseAsync();
    }
}