using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.Events.ComponentInteractions.LoungeConfigEditor;
using LoungeSystemPlugin.Events.ComponentInteractions.LoungeInterface;
using LoungeSystemPlugin.Events.ComponentInteractions.LoungeSetupUi;


namespace LoungeSystemPlugin.Events;

public static class ComponentInteractionCreated
{
    public static async Task InterfaceButtonPressed(DiscordClient sender, ComponentInteractionCreatedEventArgs eventArgs)
    {
        if (ReferenceEquals(eventArgs.User, null))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
            return;
        }


        var member = await eventArgs.Guild.GetMemberAsync(eventArgs.User.Id);
        
        switch (eventArgs.Interaction.Data.CustomId)
        {
            case "lounge_rename_button":
                await RenameButton.ButtonInteracted(eventArgs, member);
                break;
            
            case "lounge_resize_button":
                await LoungeResizeButton.ButtonInteracted(eventArgs, member);
                break;
            
            case "lounge_trust_button":
                await LoungeTrustUserButton.ButtonInteracted(eventArgs, member);
                break;
            
            case "lounge_un-trust_button":
                await LoungeUnTrustUserButton.ButtonInteracted(eventArgs, member);
                break;
            
            case "lounge_claim_button":
                await LoungeClaimButton.ButtonInteracted(eventArgs, member);
                break;
            
            case "lounge_kick_button":
                await LoungeKickButton.ButtonInteraction(eventArgs, member);
                break;
            
            case "lounge_lock_button":
                await LoungeLockButton.ButtonInteracted(eventArgs, member);
                break;
            
            case "lounge_ban_button":
                await LoungeBanButton.ButtonInteracted(eventArgs, member);
                break;
            
            case "lounge_delete_button":
                await LoungeDeleteButtonLogic.ButtonInteracted(eventArgs, member);
                break;
            
            case "lounge_ban_dropdown":
                await LoungeBanButton.DropdownInteracted (eventArgs, member);
                break;
            
            case "lounge_kick_dropdown":
                await LoungeKickButton.DropdownInteraction(eventArgs, member);
                break;
            
            case "lounge_resize_dropdown":
                await LoungeResizeButton.DropdownInteracted(eventArgs, member);
                break;
            
            case "lounge_un-trust_dropdown":
                await LoungeUnTrustUserButton.DropdownInteracted(eventArgs, member);
                break;
            
            case "lounge_trust_user-selection":
                await LoungeTrustUserButton.UserSelected(eventArgs, member);
                break;
            
            case "lounge_setup_channel_select":
                await LoungeSetupChannelSelect.ChannelSelected(eventArgs, member);
                break;
            
            case "lounge_setup_name-pattern_button":
                await LoungeSetupNamePatternButton.ButtonPressed(eventArgs, member);
                break;
            
            case "lounge_setup_interface_selector":
                await LoungeSetupInterfaceSelector.SelectionMade(eventArgs, member);
                break;
            
            case "lounge_setup_interface_channel_select":
                await LoungeSetupInterfaceChannelSelection.SelectionMade(eventArgs, member);
                break;
            
            case "lounge_config_selector":
                await LoungeConfigurationSelected.ChannelSelectionMade(eventArgs, member);
                break;
            
            case "lounge_config_reset":
                await LoungeConfigurationSelected.ResetInterfaceButton(eventArgs, member);
                break;
            
            case "lounge_config_update_name_pattern":
                await LoungeConfigurationSelected.ResetNamePatternButton(eventArgs, member);
                break;
            
            case "lounge_config_delete":
                await LoungeConfigurationSelected.DeleteButton(eventArgs, member);
                break;
            
            default:
                await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                return;
        }
    }
}