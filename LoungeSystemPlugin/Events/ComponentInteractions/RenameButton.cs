using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;

namespace LoungeSystemPlugin.Events.ComponentInteractions;

public static class RenameButton
{
    internal static async Task ButtonInteracted(ComponentInteractionCreateEventArgs eventArgs, DiscordMember member)
    {
        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(member, eventArgs.Channel, eventArgs.Guild);
        
        if (existsAsOwner == false)
            return;
        
        var builder = new DiscordFollowupMessageBuilder().WithContent("Please use *!l rename* to rename your channel");
        
        await ThrowAwayFollowupMessage.HandleAsync(builder, eventArgs.Interaction);
    }
}