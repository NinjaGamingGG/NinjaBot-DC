using DSharpPlus;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.Events.ComponentInteractions;


namespace LoungeSystemPlugin.Events;

public static class ComponentInteractionCreated
{
    public static async Task InterfaceButtonPressed(DiscordClient sender, ComponentInteractionCreateEventArgs eventArgs)
    {
        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        
        if (ReferenceEquals(eventArgs.User, null))
            return;

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
            
            case "lounge_trust_dropdown":
                await LoungeTrustUserButton.DropdownInteracted(eventArgs, member);
                break;
            
            case "lounge_un-trust_dropdown":
                await LoungeUnTrustUserButton.DropdownInteracted(eventArgs, member);
                break;
            
            default:
                await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                return;
        }
    }
}