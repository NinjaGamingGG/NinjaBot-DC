using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using NinjaBot_DC;

namespace LoungeSystemPlugin.Events.ComponentInteractions;

public static class LoungeTrustUserButton
{
    internal static async Task ButtonInteracted(ComponentInteractionCreateEventArgs eventArgs, DiscordMember member)
    {
        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(member, eventArgs.Channel, eventArgs.Guild);

        if (existsAsOwner == false)
            return;
        
        var optionsList = new List<DiscordSelectComponentOption>();
        
        var client = Worker.GetServiceDiscordClient();
        
        foreach (var guildMember in eventArgs.Guild.Members.Values)
        {
            if (guildMember.IsBot)
                continue;
            
            //Check if User is Owner / command sender
            if (guildMember.Id == eventArgs.User.Id)
                continue;

            var voiceStateString = string.Empty;


            
            if (ReferenceEquals(guildMember.VoiceState, null) || ReferenceEquals(guildMember.VoiceState.Channel, null))
                voiceStateString = DiscordEmoji.FromName(client, ":red_circle:") + " Not in Server VC";

            else if (guildMember.VoiceState.Channel.Id != eventArgs.Channel.Id)
                voiceStateString = DiscordEmoji.FromName(client, ":green_circle:") + "Currently connected to Server VC";

            optionsList.Add(new DiscordSelectComponentOption("@" + guildMember.DisplayName, guildMember.Id.ToString(), voiceStateString));
        }
        
        var sortedList = optionsList.OrderBy(x => x.Label);

        var dropdown = new DiscordSelectComponent("lounge_trust_dropdown", "Please select an user", sortedList);

        var followUpMessageBuilder = new DiscordFollowupMessageBuilder().WithContent("Please select an user below").AddComponents(dropdown);
        
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

        var selectedUserIds = eventArgs.Interaction.Data.Values.ToList();

        foreach (var selectedUserId in selectedUserIds)
        {
            var selectedUser = await eventArgs.Guild.GetMemberAsync(ulong.Parse(selectedUserId));

            var overwriteBuilderList = new List<DiscordOverwriteBuilder>
            {
                new DiscordOverwriteBuilder(selectedUser)
                    .Allow(Permissions.AccessChannels)
                    .Allow(Permissions.SendMessages)
                    .Allow(Permissions.UseVoice)
                    .Allow(Permissions.Speak)
                    .Allow(Permissions.Stream)
            };

            var existingOverwrites = eventArgs.Channel.PermissionOverwrites;

            foreach (var overwrite in existingOverwrites)
            {
                overwriteBuilderList.Add(await new DiscordOverwriteBuilder(selectedUser).FromAsync(overwrite));
            }
            
            
            await eventArgs.Channel.ModifyAsync(x => x.PermissionOverwrites = overwriteBuilderList);
        }
        
    }
}