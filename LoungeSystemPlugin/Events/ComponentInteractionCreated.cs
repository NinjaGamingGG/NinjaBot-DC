using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Net.Models;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.Records;
using NinjaBot_DC;
using Serilog;

namespace LoungeSystemPlugin.Events;

public static class ComponentInteractionCreated
{
    public static async Task InterfaceButtonPressed(DiscordClient sender, ComponentInteractionCreateEventArgs eventArgs)
    {
        switch (eventArgs.Interaction.Data.CustomId)
        {
            case "lounge_rename_button":
                await RenameButton(eventArgs);
                break;
            
            case "lounge_resize_button":
                await ResizeButtonLogic(eventArgs);
                break;
            
            case "lounge_trust_button":
                await TrustUserButtonLogic(eventArgs);
                break;
            
            case "lounge_un-trust_button":
                await UnTrustUserButtonLogic(eventArgs);
                break;
            
            case "lounge_claim_button":
                await LoungeClaimButtonLogic(eventArgs);
                break;
            
            case "lounge_kick_button":
                await LoungeKickButtonLogic(eventArgs);
                break;
            
            case "lounge_lock_button":
                await LoungeLockButtonLogic(eventArgs);
                break;
            
            case "lounge_ban_button":
                await LoungeBanButtonLogic(eventArgs);
                break;
            
            case "lounge_delete_button":
                await LoungeDeleteButtonLogic(eventArgs);
                break;
            
            case "lounge_ban_dropdown":
                await BanDropdownLogic(eventArgs);
                break;
            
            case "lounge_kick_dropdown":
                await KickDropdownLogin(eventArgs);
                break;
            
            case "lounge_resize_dropdown":
                await ResizeDropdownLogic(eventArgs);
                break;
            
            case "lounge_trust_dropdown":
                await TrustDropdownLogic(eventArgs);
                break;
            
            case "lounge_un-trust_dropdown":
                await UnTrustDropdownLogic(eventArgs);
                break;
            
            default:
                await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                return;
        }
    }

    private static async Task LoungeDeleteButtonLogic(ComponentInteractionCreateEventArgs eventArgs)
    {
        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        
        if (ReferenceEquals(eventArgs.User, null))
            return;

        var owningMember = await eventArgs.Guild.GetMemberAsync(eventArgs.User.Id);
        
        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(owningMember, eventArgs.Channel, eventArgs.Guild);
        
        //Only non owners can delete
        if (!existsAsOwner)
            return;

        var sqliteConnection = SqLiteHelper.GetSqLiteConnection();

        var loungeDbRecordEnumerable = await sqliteConnection.QueryAsync<LoungeDbRecord>("SELECT * FROM LoungeIndex WHERE GuildId = @GuildId AND ChannelId= @ChannelId", new {GuildId = eventArgs.Guild.Id, ChannelId = eventArgs.Channel.Id});

        var loungeDbRecordList = loungeDbRecordEnumerable.ToList();

        if (!loungeDbRecordList.Any())
        {
            Log.Error("Unable to Load the LoungeDbRecord from Lounge Index on Guild {GuildId} at Channel {ChannelId}", eventArgs.Guild.Id, eventArgs.Channel.Id);
            return;
        }

        var loungeChannel = eventArgs.Channel;

        await loungeChannel.DeleteAsync();
        
        var sqlSuccess = await sqliteConnection.DeleteAsync(loungeDbRecordList.First());
                
        if (sqlSuccess == false)
            Log.Error("Unable to delete the Sql Record for Lounge {LoungeName} with the Id {LoungeId} in Guild {GuildId}",loungeChannel.Name, eventArgs.Channel.Id, eventArgs.Guild.Id);

    }

    private static async Task BanDropdownLogic(ComponentInteractionCreateEventArgs eventArgs)
    {
        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        
        if (ReferenceEquals(eventArgs.User, null))
            return;

        var owningMember = await eventArgs.Guild.GetMemberAsync(eventArgs.User.Id);
        
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

    private static async Task LoungeBanButtonLogic(ComponentInteractionCreateEventArgs eventArgs)
    {
        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        
        if (ReferenceEquals(eventArgs.User, null))
            return;

        var owningMember = await eventArgs.Guild.GetMemberAsync(eventArgs.User.Id);
        
        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(owningMember, eventArgs.Channel, eventArgs.Guild);
        
        //Only non owners can ban
        if (!existsAsOwner)
            return;

        var membersInChannel = eventArgs.Channel.Users;

        var optionsList = membersInChannel.Select(channelMember => new DiscordSelectComponentOption("@" + channelMember.DisplayName, channelMember.Id.ToString())).ToList();

        var sortedList = optionsList.OrderBy(x => x.Label);
        
        var dropdown = new DiscordSelectComponent("lounge_ban_dropdown", "Please select an user to ban (from channel)", sortedList);

        var followUpMessageBuilder = new DiscordFollowupMessageBuilder().WithContent("Please select an user below").AddComponents(dropdown);
        
        var followupMessage = await eventArgs.Interaction.CreateFollowupMessageAsync(followUpMessageBuilder);
        
        await Task.Delay(TimeSpan.FromSeconds(20));

        try
        {
            await followupMessage.DeleteAsync();
        }
        catch (DSharpPlus.Exceptions.NotFoundException)
        {
            //Do nothing
        }
    }

    private static async Task LoungeLockButtonLogic(ComponentInteractionCreateEventArgs eventArgs)
    {
        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        
        if (ReferenceEquals(eventArgs.User, null))
            return;

        var owningMember = await eventArgs.Guild.GetMemberAsync(eventArgs.User.Id);
        
        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(owningMember, eventArgs.Channel, eventArgs.Guild);
        
        //Only non owners can kick
        if (!existsAsOwner)
            return;

        var sqliteConnection = SqLiteHelper.GetSqLiteConnection();

        var isPublic =
            await sqliteConnection.QueryAsync<bool>(
                "SELECT isPublic FROM LoungeIndex where GuildId=@GuildId and ChannelId=@ChannelId", new {GuildId = eventArgs.Guild.Id, ChannelId= eventArgs.Channel.Id});

        var isPublicAsArray = isPublic as bool[] ?? isPublic.ToArray();
        if (isPublicAsArray.Any() == false)
            Log.Error("[Ranksystem] Unable to load isPublic variable for Channel {ChannelId} on Guild {GuildId}", eventArgs.Channel.Id, eventArgs.Guild.Id);

        if (isPublicAsArray[0] == false)
        {
            await UnLockLoungeLogic(eventArgs);
            return;
        }

        await LockLoungeLogic(eventArgs);
    }

    private static async Task LockLoungeLogic(ComponentInteractionCreateEventArgs eventArgs)
    {
        var sqliteConnection = SqLiteHelper.GetSqLiteConnection();
        var requiredRolesQueryResult =
           await sqliteConnection.QueryAsync<ulong>(
                "SELECT RoleId FROM RequiredRoleIndex WHERE GuildId=@GuildId AND ChannelId=@ChannelId", new {GuildId = eventArgs.Guild.Id,ChannelId = eventArgs.Channel.Id});

        var requiresRolesList = requiredRolesQueryResult.ToList();

        var lounge = eventArgs.Channel;

        var existingOverwrites = lounge.PermissionOverwrites;
        var overwriteBuilderList = new List<DiscordOverwriteBuilder>();
        
        foreach (var overwrite in existingOverwrites)
        {
            if (overwrite.Type == OverwriteType.Role)
                continue;
            
            var overwriteMember = await overwrite.GetMemberAsync();
            overwriteBuilderList.Add(await new DiscordOverwriteBuilder(overwriteMember).FromAsync(overwrite));
        }
        
        if (!requiresRolesList.Any())
            requiresRolesList.Add(eventArgs.Guild.EveryoneRole.Id);

        overwriteBuilderList.AddRange(requiresRolesList.Select(requiredRole => eventArgs.Guild.GetRole(requiredRole))
            .Select(role => new DiscordOverwriteBuilder(role).Allow(Permissions.AccessChannels)
                .Deny(Permissions.SendMessages)
                .Deny(Permissions.UseVoice)
                .Deny(Permissions.Speak)
                .Deny(Permissions.Stream)));
        
        await eventArgs.Channel.ModifyAsync(x => x.PermissionOverwrites = overwriteBuilderList);

        await sqliteConnection.ExecuteAsync("UPDATE LoungeIndex SET isPublic = FALSE WHERE GuildId=@GuildId AND ChannelId=@ChannelId", new {GuildId = eventArgs.Guild.Id,ChannelId = eventArgs.Channel.Id});
    }
    
    
    
    private static async Task UnLockLoungeLogic(ComponentInteractionCreateEventArgs eventArgs)
    {
        var sqliteConnection = SqLiteHelper.GetSqLiteConnection();
        var requiredRolesQueryResult =
            await sqliteConnection.QueryAsync<ulong>(
                "SELECT RoleId FROM RequiredRoleIndex WHERE GuildId=@GuildId AND ChannelId=@ChannelId", new {GuildId = eventArgs.Guild.Id,ChannelId = eventArgs.Channel.Id});

        var requiresRolesList = requiredRolesQueryResult.ToList();

        var lounge = eventArgs.Channel;

        var existingOverwrites = lounge.PermissionOverwrites;
        var overwriteBuilderList = new List<DiscordOverwriteBuilder>();
        
        foreach (var overwrite in existingOverwrites)
        {
            if (overwrite.Type == OverwriteType.Role)
                continue;
            
            var overwriteMember = await overwrite.GetMemberAsync();
            overwriteBuilderList.Add(await new DiscordOverwriteBuilder(overwriteMember).FromAsync(overwrite));
        }
        
        if (!requiresRolesList.Any())
            requiresRolesList.Add(eventArgs.Guild.EveryoneRole.Id);
        else
            overwriteBuilderList.Add(new DiscordOverwriteBuilder(eventArgs.Guild.EveryoneRole)
                .Deny(Permissions.AccessChannels)
                .Deny(Permissions.SendMessages)
                .Deny(Permissions.UseVoice)
                .Deny(Permissions.Speak)
                .Deny(Permissions.Stream));

        overwriteBuilderList.AddRange(requiresRolesList.Select(requiredRole => eventArgs.Guild.GetRole(requiredRole))
            .Select(role => new DiscordOverwriteBuilder(role).Allow(Permissions.AccessChannels)
                .Allow(Permissions.SendMessages)
                .Allow(Permissions.UseVoice)
                .Allow(Permissions.Speak)
                .Allow(Permissions.Stream)));

        await eventArgs.Channel.ModifyAsync(x => x.PermissionOverwrites = overwriteBuilderList);

        await sqliteConnection.ExecuteAsync("UPDATE LoungeIndex SET isPublic = TRUE WHERE GuildId=@GuildId AND ChannelId=@ChannelId", new {GuildId = eventArgs.Guild.Id,ChannelId = eventArgs.Channel.Id});

    }

    private static async Task KickDropdownLogin(ComponentInteractionCreateEventArgs eventArgs)
    {
        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        
        if (ReferenceEquals(eventArgs.User, null))
            return;

        var owningMember = await eventArgs.Guild.GetMemberAsync(eventArgs.User.Id);
        
        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(owningMember, eventArgs.Channel, eventArgs.Guild);
        
        //Owner cant be kicked
        if (!existsAsOwner)
            return;

        await eventArgs.Message.DeleteAsync();
        
        var selectedUserIds = eventArgs.Interaction.Data.Values.ToList();

        foreach (var userId in selectedUserIds)
        {
            var user = await eventArgs.Guild.GetMemberAsync(ulong.Parse(userId));

            await user.ModifyAsync(delegate (MemberEditModel kick)
            {
                kick.VoiceChannel = eventArgs.Guild.AfkChannel;
            });
        }
    }

    private static async Task LoungeKickButtonLogic(ComponentInteractionCreateEventArgs eventArgs)
    {
        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        
        if (ReferenceEquals(eventArgs.User, null))
            return;

        var owningMember = await eventArgs.Guild.GetMemberAsync(eventArgs.User.Id);
        
        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(owningMember, eventArgs.Channel, eventArgs.Guild);
        
        //Only non owners can kick
        if (!existsAsOwner)
            return;

        var membersInChannel = eventArgs.Channel.Users;

        var optionsList = new List<DiscordSelectComponentOption>();

        foreach (var channelMember in membersInChannel)
        {
            optionsList.Add(new DiscordSelectComponentOption("@"+channelMember.DisplayName, channelMember.Id.ToString()));
        }
        
        var sortedList = optionsList.OrderBy(x => x.Label);
        
        var dropdown = new DiscordSelectComponent("lounge_kick_dropdown", "Please select an user", sortedList);

        var followUpMessageBuilder = new DiscordFollowupMessageBuilder().WithContent("Please select an user below").AddComponents(dropdown);
        
        var followupMessage = await eventArgs.Interaction.CreateFollowupMessageAsync(followUpMessageBuilder);
        
        await Task.Delay(TimeSpan.FromSeconds(20));

        try
        {
            await followupMessage.DeleteAsync();
        }
        catch (DSharpPlus.Exceptions.NotFoundException)
        {
            //Do nothing
        }
    }

    private static async Task LoungeClaimButtonLogic(ComponentInteractionCreateEventArgs eventArgs)
    {
        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        
        if (ReferenceEquals(eventArgs.User, null))
            return;

        var member = await eventArgs.Guild.GetMemberAsync(eventArgs.User.Id);
        
        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(member, eventArgs.Channel, eventArgs.Guild);
        
        //Only non owners can claim
        if (existsAsOwner)
            return;

        var ownerId = await LoungeOwnerCheck.GetOwnerIdAsync(eventArgs.Channel);
        
        var membersInChannel = eventArgs.Channel.Users;

        var isOwnerPresent = false;
        
        foreach (var discordMember in membersInChannel)
        {
            if (discordMember.Id == ownerId)
                isOwnerPresent = true;
        }

        if (isOwnerPresent)
            return;
        
        var builder = new DiscordFollowupMessageBuilder().WithContent(eventArgs.User.Mention + " please wait, claiming channel for you");
        
        var followupMessage = await eventArgs.Interaction.CreateFollowupMessageAsync(builder);

        var sqLiteConnection = SqLiteHelper.GetSqLiteConnection();
        
        var updateCount = await sqLiteConnection.ExecuteAsync(
            "UPDATE LoungeIndex SET OwnerId = @OwnerId WHERE ChannelId = @ChannelId",
            new {OwnerId = eventArgs.User.Id, ChannelId = eventArgs.Channel.Id});
        
        if (updateCount == 0)
        {
            await followupMessage.ModifyAsync("Failed to claim Lounge, please try again later");
            return;
        }
        
        await followupMessage.ModifyAsync("Lounge claimed successfully, you are now the owner");

        await Task.Delay(TimeSpan.FromSeconds(15));
        
        try
        {
            await followupMessage.DeleteAsync();
        }
        catch (DSharpPlus.Exceptions.NotFoundException)
        {
            //Do nothing
        }

    }

    private static async Task RenameButton(ComponentInteractionCreateEventArgs eventArgs)
    {
        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        
        if (ReferenceEquals(eventArgs.User, null))
            return;

        var member = await eventArgs.Guild.GetMemberAsync(eventArgs.User.Id);
        
        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(member, eventArgs.Channel, eventArgs.Guild);
        
        if (existsAsOwner == false)
            return;
        
        var builder = new DiscordFollowupMessageBuilder().WithContent("Please use *!l rename* to rename your channel");
        
        var followupMessage = await eventArgs.Interaction.CreateFollowupMessageAsync(builder);

        await Task.Delay(TimeSpan.FromSeconds(15));

        await followupMessage.DeleteAsync();
    }
    
    private static async Task TrustUserButtonLogic(ComponentInteractionCreateEventArgs eventArgs)
    {
        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        
        if (ReferenceEquals(eventArgs.User, null))
            return;
        
        var member = await eventArgs.Guild.GetMemberAsync(eventArgs.User.Id);
        
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
        
        var followupMessage = await eventArgs.Interaction.CreateFollowupMessageAsync(followUpMessageBuilder);

        await Task.Delay(TimeSpan.FromSeconds(20));

        try
        {
            await followupMessage.DeleteAsync();
        }
        catch (DSharpPlus.Exceptions.NotFoundException)
        {
            //Do nothing
        }
    }
    
    private static async Task UnTrustUserButtonLogic(ComponentInteractionCreateEventArgs eventArgs)
    {
        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        
        if (ReferenceEquals(eventArgs.User, null))
            return;
        
        var owningMember = await eventArgs.Guild.GetMemberAsync(eventArgs.User.Id);
        
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
        
        var followupMessage = await eventArgs.Interaction.CreateFollowupMessageAsync(followUpMessageBuilder);
        
        await Task.Delay(TimeSpan.FromSeconds(20));

        try
        {
            await followupMessage.DeleteAsync();
        }
        catch (DSharpPlus.Exceptions.NotFoundException)
        {
            //Do nothing
        }
        
    }

    private static async Task ResizeButtonLogic(ComponentInteractionCreateEventArgs eventArgs)
    {
        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        
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
                optionsList.Add(new DiscordSelectComponentOption(i.ToString(),"lounge_resize_label_"+ i,isDefault: true));
                continue;
            }
            
            optionsList.Add(new DiscordSelectComponentOption(i.ToString(),"lounge_resize_label_"+ i));
        }
        
        var dropdown = new DiscordSelectComponent("lounge_resize_dropdown", "Select a new Size Below", optionsList);

        var followUpMessage = new DiscordFollowupMessageBuilder().WithContent("Select a new Size Below").AddComponents(dropdown);

        await eventArgs.Interaction.CreateFollowupMessageAsync( followUpMessage);

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

    private static async Task TrustDropdownLogic(ComponentInteractionCreateEventArgs eventArgs)
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
    
    private static async Task UnTrustDropdownLogic(ComponentInteractionCreateEventArgs eventArgs)
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