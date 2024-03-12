using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using LoungeSystemPlugin.PluginHelper;
using NinjaBot_DC;
using Serilog;

namespace LoungeSystemPlugin.Events.ComponentInteractions;

public static class RenameButton
{
    internal static async Task ButtonInteracted(ComponentInteractionCreateEventArgs eventArgs, DiscordMember member)
    {
        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(member, eventArgs.Channel, eventArgs.Guild);
        
        if (existsAsOwner == false)
            return;

        var modal = new DiscordInteractionResponseBuilder();
        
        modal.WithTitle("Rename your Lounge").WithCustomId("lounge_rename_modal").
            AddComponents(new TextInputComponent("New Lounge Name", "lounge_new_name", required: true, min_length: 4, max_length: 12));

        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);
    }
}