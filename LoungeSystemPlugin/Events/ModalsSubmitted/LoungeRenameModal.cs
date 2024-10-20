﻿using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Net.Models;
using LoungeSystemPlugin.PluginHelper;

namespace LoungeSystemPlugin.Events.ModalsSubmitted;

public static class LoungeRenameModal
{
    public static async Task WasSubmitted(DiscordClient sender, ModalSubmittedEventArgs eventArgs)
    {
        await eventArgs.Interaction.DeferAsync();
        
        var isValuePresent = eventArgs.Values.TryGetValue("lounge_new_name", out var userValue);

        if (isValuePresent == false || string.IsNullOrEmpty(userValue) )
            return;
        
        if (ReferenceEquals(eventArgs.Interaction.Guild, null))
            return;

        var newChannelName = await ChannelNameBuilder.BuildAsync(eventArgs.Interaction.Guild.Id, eventArgs.Interaction.Channel.Id,
            userValue);
        
        var channel = await sender.GetChannelAsync(eventArgs.Interaction.Channel.Id);
        await channel.ModifyAsync(NewEditModel);
        
        
        await eventArgs.Interaction.DeleteOriginalResponseAsync();
        return;

        void NewEditModel(ChannelEditModel editModel)
        {
            editModel.Name = newChannelName;
        }
        

    }
    

    
}