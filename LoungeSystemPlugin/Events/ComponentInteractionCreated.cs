using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Net.Models;
using LoungeSystemPlugin.PluginHelper;
using Serilog;

namespace LoungeSystemPlugin.Events;

public static class ComponentInteractionCreated
{
    public static async Task InterfaceButtonPressed(DiscordClient sender, ComponentInteractionCreateEventArgs eventArgs)
    {
        switch (eventArgs.Interaction.Data.CustomId)
        {
            case "lounge_rename_button":
                await RenameLogic(eventArgs);
                break;
            
            case "lounge_resize_button":
                await ResizeLogic(eventArgs);
                break;
            
            case "lounge_trust_button":
                break;
            
            case "lounge_claim_button":
                break;
            
            case "lounge_kick_button":
                break;
            
            case "lounge_lock_button":
                break;
            
            case "lounge_delete_button":
                break;
            
            case "lounge_resize_dropdown":
                await ResizeDropdownLogic(eventArgs);
                break;
            
            default:
                await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                return;
        }
    }

    private static async Task RenameLogic(ComponentInteractionCreateEventArgs eventArgs)
    {
        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        
        if (ReferenceEquals(eventArgs.User, null))
            return;

        var member = await eventArgs.Guild.GetMemberAsync(eventArgs.User.Id);
        
        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(member, eventArgs.Channel, eventArgs.Guild);
        
        if (existsAsOwner == false)
            return;
        
        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

        var builder = new DiscordFollowupMessageBuilder().WithContent("Please use *!l rename* to rename your channel");
        
        var followupMessage = await eventArgs.Interaction.CreateFollowupMessageAsync(builder);

        await Task.Delay(TimeSpan.FromSeconds(15));

        await followupMessage.DeleteAsync();
    }

    private static async Task ResizeLogic(ComponentInteractionCreateEventArgs eventArgs)
    {
        if (ReferenceEquals(eventArgs.User, null))
            return;

        var member = await eventArgs.Guild.GetMemberAsync(eventArgs.User.Id);
        
        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(member, eventArgs.Channel, eventArgs.Guild);
        
        if (existsAsOwner == false)
            return;
        
        const int maxChannelSize = 25;
        
        var optionsList = new List<DiscordSelectComponentOption>();
        
        for (var i = 1; i < maxChannelSize; i++)
        {
            if (i == 4)
            {
                optionsList.Add(new DiscordSelectComponentOption(i.ToString(),"lounge_resize_label_"+ i.ToString(),isDefault: true));
                continue;
            }
            
            optionsList.Add(new DiscordSelectComponentOption(i.ToString(),"lounge_resize_label_"+ i.ToString()));
        }
        
        var dropdown = new DiscordSelectComponent("lounge_resize_dropdown", "Select a new Size Below", optionsList);

        var followUpMessage = new DiscordInteractionResponseBuilder().WithContent("Select a new Size Below").AddComponents(dropdown);

        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder(followUpMessage));

    }

    private static async Task ResizeDropdownLogic(ComponentInteractionCreateEventArgs eventArgs)
    {
        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        
        if (ReferenceEquals(eventArgs.User, null))
            return;

        var member = await eventArgs.Guild.GetMemberAsync(eventArgs.User.Id);

        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(member, eventArgs.Channel, eventArgs.Guild);

        if (existsAsOwner == false)
            return;
        
        var interactionId = eventArgs.Message.Id;
        var message = await eventArgs.Channel.GetMessageAsync(interactionId);
        await message.DeleteAsync();

        var newSizeString = eventArgs.Interaction.Data.Values[0].Replace("lounge_resize_label_", "");
        
        var parseSuccess = int.TryParse(newSizeString, out var parseResult);
        
        if (parseSuccess == false)
        {
            Log.Error("Failed to parse new size for lounge");
            return;
        }
        
        var channel = eventArgs.Channel;
        
        if (ReferenceEquals(channel, null))
            return;
        
        void NewEditModel(ChannelEditModel editModel)
        {
            editModel.Userlimit = parseResult;
        }

        await channel.ModifyAsync(NewEditModel);

    }
}