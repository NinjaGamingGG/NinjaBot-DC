using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using NinjaBot_DC;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeSetupUi;

public static class LoungeSetupChannelSelect
{
    internal static async Task ChannelSelected(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember member)
    {
        //Check if User has Admin Permissions
        if (member.Permissions.HasPermission(DiscordPermissions.Administrator))
        {
            await eventArgs.Interaction.DeferAsync();
            return;
        }
        
        await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,LoungeSetupUiHelper.ChannelSelectedMessageBuilder);
    }
}