using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeSetupUi;

public static class LoungeSetupNamePatternButton
{
    internal static async Task ButtonPressed(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember member)
    {
        await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal,
            LoungeSetupUiHelper.ChannelNamePatternModalBuilder);
    }
}