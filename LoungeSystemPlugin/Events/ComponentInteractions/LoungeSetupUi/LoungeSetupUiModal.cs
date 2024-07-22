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
        //Check if Guild is null
        if (ReferenceEquals(eventArgs.Interaction.Guild, null))
        {
            await eventArgs.Interaction.DeferAsync();
            return;
        }
        
        //Get the DiscordMember that Submitted the Modal
        var member = await eventArgs.Interaction.Guild.GetMemberAsync(eventArgs.Interaction.User.Id);
            
        //Check if User has Admin Permissions
        if (member.Permissions.HasPermission(DiscordPermissions.Administrator))
        {
            await eventArgs.Interaction.DeferAsync();
            return;
        }
        
        //Update the Message this Modal was attached to
        await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,
            LoungeSetupUiHelper.ModalSubmittedResponseBuilder);

    }
}