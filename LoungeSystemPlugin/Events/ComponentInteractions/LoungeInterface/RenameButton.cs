﻿using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeInterface;

public static class RenameButton
{
    internal static async Task ButtonInteracted(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember member)
    {
        var targetChannel = await InterfaceTargetHelper.GetTargetDiscordChannelAsync(eventArgs.Channel, member);

        if (ReferenceEquals(targetChannel, null))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                LoungeSetupUiHelper.Messages.NotInChannelResponseBuilder);
            return;
        }
        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(member, targetChannel, eventArgs.Guild);
        
        if (existsAsOwner == false)
            return;

        var modal = new DiscordInteractionResponseBuilder();
        
        modal.WithTitle("Rename your Lounge").WithCustomId("lounge_rename_modal").AddComponents(new DiscordTextInputComponent("New Lounge Name", "lounge_new_name", required: true, min_length: 4, max_length: 12));

        await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
    }
}