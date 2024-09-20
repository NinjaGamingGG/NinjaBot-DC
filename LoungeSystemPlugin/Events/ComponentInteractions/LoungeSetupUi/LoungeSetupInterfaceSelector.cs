using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeSetupUi;

public static class LoungeSetupInterfaceSelector
{
    internal static async Task SelectionMade(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember member)
    {
        //Check if User has Admin Permissions
        if (!member.Permissions.HasPermission(DiscordPermissions.Administrator))
        {
            await eventArgs.Interaction.DeferAsync();
            return;
        }
        
        //The Selection that was made. The Component is set to allow only one option to be selected, so we just get the element at the first position
        var selection = eventArgs.Interaction.Data.Values[0];

        //Handle the value of the Selection
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