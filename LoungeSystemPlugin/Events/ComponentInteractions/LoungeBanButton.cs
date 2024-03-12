using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;

namespace LoungeSystemPlugin.Events.ComponentInteractions;

public static class LoungeBanButton
{
    internal static async Task ButtonInteracted(ComponentInteractionCreateEventArgs eventArgs, DiscordMember owningMember)
    {
        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(owningMember, eventArgs.Channel, eventArgs.Guild);
        
        //Only non owners can ban
        if (!existsAsOwner)
            return;

        var membersInChannel = eventArgs.Channel.Users;

        var optionsList = membersInChannel.Select(channelMember => new DiscordSelectComponentOption("@" + channelMember.DisplayName, channelMember.Id.ToString())).ToList();

        var sortedList = optionsList.OrderBy(x => x.Label);
        
        var dropdown = new DiscordSelectComponent("lounge_ban_dropdown", "Please select an user to ban (from channel)", sortedList);

        var followUpMessageBuilder = new DiscordFollowupMessageBuilder().WithContent("Please select an user below").AddComponents(dropdown);
        
        await ThrowAwayFollowupMessage.HandleAsync(followUpMessageBuilder, eventArgs.Interaction);
    }
    
    internal static async Task DropdownInteracted(ComponentInteractionCreateEventArgs eventArgs, DiscordMember owningMember)
    {
        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(owningMember, eventArgs.Channel, eventArgs.Guild);
        
        //Only owner can ban
        if (!existsAsOwner)
            return;

        await eventArgs.Message.DeleteAsync();
        
        var selectedUserIds = eventArgs.Interaction.Data.Values.ToList();

        var selectedUsersAsDiscordMember = new List<DiscordMember>();

        foreach (var userIdAsUlong in selectedUserIds.Select(ulong.Parse))
        {
            selectedUsersAsDiscordMember.Add(await eventArgs.Guild.GetMemberAsync(userIdAsUlong));
        }

        var existingOverwrites = eventArgs.Channel.PermissionOverwrites.ToList();

        var overwriteBuilderList = new List<DiscordOverwriteBuilder>();
        
        foreach (var overwrite in existingOverwrites)
        {
            if (overwrite.Type == OverwriteType.Member && selectedUsersAsDiscordMember.Contains(await overwrite.GetMemberAsync()))
                continue;
            
            if (overwrite.Type == OverwriteType.Member)
                overwriteBuilderList.Add(await new DiscordOverwriteBuilder(await overwrite.GetMemberAsync()).FromAsync(overwrite));
            
            if (overwrite.Type == OverwriteType.Role)
                overwriteBuilderList.Add(await new DiscordOverwriteBuilder(await overwrite.GetRoleAsync()).FromAsync(overwrite));
        }

        overwriteBuilderList.AddRange(selectedUsersAsDiscordMember.Select(member => new DiscordOverwriteBuilder(member).Allow(Permissions.AccessChannels)
            .Deny(Permissions.SendMessages)
            .Deny(Permissions.UseVoice)
            .Deny(Permissions.Speak)
            .Deny(Permissions.Stream)));
        
        await eventArgs.Channel.ModifyAsync(x => x.PermissionOverwrites = overwriteBuilderList);
    }
    
}