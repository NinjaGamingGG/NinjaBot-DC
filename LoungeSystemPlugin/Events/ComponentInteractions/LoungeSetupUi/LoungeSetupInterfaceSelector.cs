using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeSetupUi;

public static class LoungeSetupInterfaceSelector
{
    internal static async Task SelectionMade(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember member)
    {
        var selection = eventArgs.Interaction.Data.Values[0];

        switch (selection)
        {
            case ("separate_interface"):
                await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,
                    LoungeSetupUiHelper.InterfaceSelectedResponseBuilder);
                break;
            
            case ("internal_interface"):
                await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,
                    LoungeSetupUiHelper.LoungeSetupComplete);
                break;
            
            default:
                await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                    LoungeSetupUiHelper.InteractionFailedResponseBuilder("The selection made was Invalid, please try again"));
                break;
            
        }

    }
}