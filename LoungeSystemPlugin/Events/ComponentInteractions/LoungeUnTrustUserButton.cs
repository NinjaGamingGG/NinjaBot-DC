using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;

namespace LoungeSystemPlugin.Events.ComponentInteractions;

public static class LoungeUnTrustUserButton
{
    internal static async Task ButtonInteracted(ComponentInteractionCreateEventArgs eventArgs, DiscordMember owningMember)
    {
        await eventArgs.Interaction.DeferAsync();

        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(owningMember, eventArgs.Channel, eventArgs.Guild);

        if (existsAsOwner == false)
            return;
        
        var optionsList = new List<DiscordSelectComponentOption>();

        var channelOverwrites = eventArgs.Channel.PermissionOverwrites;

        foreach (var overwriteEntry in channelOverwrites)
        {
            if (overwriteEntry.Type == OverwriteType.Role)
                continue;
            
            //Check if User is Owner / command sender
            if (overwriteEntry.Id == owningMember.Id)
                continue;
            
            var memberInChannel = await eventArgs.Guild.GetMemberAsync(overwriteEntry.Id);
            
            optionsList.Add(new DiscordSelectComponentOption("@"+memberInChannel.DisplayName, memberInChannel.Id.ToString()));
        }
        
        var sortedList = optionsList.OrderBy(x => x.Label);
        
        var dropdown = new DiscordSelectComponent("lounge_un-trust_dropdown", "Please select an user", sortedList);

        var followUpMessageBuilder = new DiscordFollowupMessageBuilder().WithContent("Please select an user below").AddComponents(dropdown);

        await ThrowAwayFollowupMessage.HandleAsync(followUpMessageBuilder, eventArgs.Interaction);
    }
    
    internal static async Task DropdownInteracted(ComponentInteractionCreateEventArgs eventArgs, DiscordMember member)
    {
        await eventArgs.Interaction.DeferAsync();

        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(member, eventArgs.Channel, eventArgs.Guild);

        if (existsAsOwner == false)
            return;
        
        var interactionId = eventArgs.Message.Id;
        var message = await eventArgs.Channel.GetMessageAsync(interactionId);
        await message.DeleteAsync();
        
        var selectedUserIds = eventArgs.Interaction.Data.Values.ToList();
        
        var existingOverwrites = eventArgs.Channel.PermissionOverwrites;

        var overwriteBuilderList = new List<DiscordOverwriteBuilder>();
        
        foreach (var existingOverwrite in existingOverwrites)
        {
            if (selectedUserIds.Contains(existingOverwrite.Id.ToString()))
                continue;


            if (existingOverwrite.Type == OverwriteType.Role)
            {
                var role = eventArgs.Guild.GetRole(existingOverwrite.Id);
                
                overwriteBuilderList.Add(await new DiscordOverwriteBuilder(role).FromAsync(existingOverwrite));
            }
            else
            {
                var user = await eventArgs.Guild.GetMemberAsync(existingOverwrite.Id);
                
                overwriteBuilderList.Add(await new DiscordOverwriteBuilder(user).FromAsync(existingOverwrite));
            }
        }
        
        await eventArgs.Channel.ModifyAsync(x => x.PermissionOverwrites = overwriteBuilderList);
    }
}