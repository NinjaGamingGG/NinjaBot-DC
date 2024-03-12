using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Net.Models;
using LoungeSystemPlugin.PluginHelper;
using Serilog;

namespace LoungeSystemPlugin.Events.ComponentInteractions;

public static class LoungeResizeButton
{
    internal static async Task ButtonInteracted(ComponentInteractionCreateEventArgs eventArgs, DiscordMember member)
    {
        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(member, eventArgs.Channel, eventArgs.Guild);
        
        if (existsAsOwner == false)
            return;
        
        const int maxChannelSize = 25;
        
        var optionsList = new List<DiscordSelectComponentOption>();
        
        for (var i = 1; i < maxChannelSize; i++)
        {
            if (i == 4)
            {
                optionsList.Add(new DiscordSelectComponentOption(i.ToString(),"lounge_resize_label_"+ i,isDefault: true));
                continue;
            }
            
            optionsList.Add(new DiscordSelectComponentOption(i.ToString(),"lounge_resize_label_"+ i));
        }
        
        var dropdown = new DiscordSelectComponent("lounge_resize_dropdown", "Select a new Size Below", optionsList);

        var followUpMessageBuilder = new DiscordFollowupMessageBuilder().WithContent("Select a new Size Below").AddComponents(dropdown);

        await ThrowAwayFollowupMessage.HandleAsync(followUpMessageBuilder, eventArgs.Interaction);

    }

    internal static async Task DropdownInteracted(ComponentInteractionCreateEventArgs eventArgs, DiscordMember member)
    {
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