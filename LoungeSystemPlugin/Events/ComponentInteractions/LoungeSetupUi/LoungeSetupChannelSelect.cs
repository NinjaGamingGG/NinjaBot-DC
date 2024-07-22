using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using NinjaBot_DC;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeSetupUi;

public static class LoungeSetupChannelSelect
{
    internal static async Task ChannelSelected(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember member)
    {
        await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,LoungeSetupUiHelper.ChannelSelectedMessageBuilder);
    }
}