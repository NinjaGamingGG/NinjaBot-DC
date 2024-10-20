using DSharpPlus;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.Events.ModalsSubmitted;


namespace LoungeSystemPlugin.Events;

internal static class ModalSubmitted
{
    internal static async Task ModalSubmittedHandler(DiscordClient sender, ModalSubmittedEventArgs eventArgs)
    {
        switch (eventArgs.Interaction.Data.CustomId)
        {
                case "lounge_rename_modal":
                    await LoungeRenameModal.WasSubmitted(sender, eventArgs);
                    break;
                
                case "lounge_setup_name-pattern_modal":
                    await LoungeSetupUiModal.ModalSubmitted(sender, eventArgs);
                    break;
        }
        

    }
}