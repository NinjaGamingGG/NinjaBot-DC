﻿using Dapper;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Net.Models;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.Records;

namespace LoungeSystemPlugin.CommandModules;

public class CommandNextModule : BaseCommandModule
{
    [Command("l")]
    public async Task LoungeCommand(CommandContext context, string argument)
    {
        if (ReferenceEquals(context.Member, null))
            return;
        
        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(context.Member, context.Channel, context.Guild);
        
        if (existsAsOwner == false)
            return;
        
        switch (argument)
        {
            case "rename":
                await RenameChannelCommand(context);
                break;
            case "resize":
                break;
        }
    }

    private static async Task RenameChannelCommand(CommandContext context)
    {
        var builder = new DiscordMessageBuilder().WithContent("Please respond with new Channel name");

        var message = await context.RespondAsync(builder);

        var response = await context.Message.GetNextMessageAsync();

        if (response.TimedOut)
        {
            var errorBuilder = new DiscordMessageBuilder().WithContent("Error. Interaction Timed out");
            await message.RespondAsync(errorBuilder);
        }

        var sqliteConnection = SqLiteHelper.GetSqLiteConnection();
        
        var channelConfigurations = await sqliteConnection.QueryAsync<LoungeSystemConfigurationRecord>("SELECT * FROM LoungeSystemConfigurationIndex WHERE GuildId = @GuildId ", new { GuildId = context.Guild.Id});
        
        var channelConfigurationList = channelConfigurations.ToList();

        var channelRecords = await sqliteConnection.QueryAsync<LoungeDbRecord>("SELECT * FROM LoungeIndex WHERE GuildId = @GuildId AND ChannelId = @ChannelId", new {GuildId = context.Guild.Id, ChannelId = context.Channel.Id});

        var channelRecordsAsList = channelRecords.ToList();

        var channelRecord = channelRecordsAsList.First();
        
        var channelNamePattern = string.Empty;

        var customNamePattern = response.Result.Content;
        var separatorPattern = string.Empty;
        var decoratorPrefix = string.Empty;
        var decoratorEmoji = string.Empty;
        var decoratorDecal = string.Empty;

        var nameReplacementRecord = await sqliteConnection.QueryAsync<LoungeMessageReplacement>("SELECT * FROM LoungeMessageReplacementIndex WHERE GuildId= @GuildId AND ChannelId = @ChannelId", new {GuildId = context.Guild.Id, ChannelId = channelRecord.OriginChannel});
        var loungeMessageReplacementsAsArray = nameReplacementRecord as LoungeMessageReplacement[] ?? nameReplacementRecord.ToArray();
        if (loungeMessageReplacementsAsArray.Any())
        {
            foreach (var replacement in loungeMessageReplacementsAsArray)
            {
                switch (replacement.ReplacementHandle)
                {
                    case"Separator":
                        separatorPattern = replacement.ReplacementValue;
                        break;

                    case"DecoratorDecal":
                        decoratorDecal = replacement.ReplacementValue;
                        break;
                    
                    case"DecoratorEmoji":
                        decoratorEmoji = replacement.ReplacementValue;
                        break;
                    
                    case"DecoratorPrefix":
                        decoratorPrefix = replacement.ReplacementValue;
                        break;
                }

            }
        }
        
        if (!ReferenceEquals(separatorPattern, null) && separatorPattern.Contains("{decoratordecal}"))
            separatorPattern = separatorPattern.Replace("{decoratordecal}", decoratorDecal);
        
        if (!ReferenceEquals(separatorPattern, null) && separatorPattern.Contains("{decoratoremoji}"))
            separatorPattern = separatorPattern.Replace("{decoratoremoji}", decoratorEmoji);
        if (!ReferenceEquals(separatorPattern, null) && separatorPattern.Contains("{decoratorprefix}"))
            separatorPattern = separatorPattern.Replace("{decoratorprefix}", decoratorPrefix);

        foreach (var channelConfig in channelConfigurationList.Where(channelConfig => channelRecord.OriginChannel == channelConfig.TargetChannelId))
        {
            channelNamePattern = channelConfig.LoungeNamePattern;

            
            //if (channelNamePattern != null && channelNamePattern.Contains("{username}"))
            //    channelNamePattern = channelNamePattern.Replace("{username}", eventArgs.User.Username);
            if (!ReferenceEquals(channelNamePattern, null) && channelNamePattern.Contains("{separator}"))
                channelNamePattern = channelNamePattern.Replace("{separator}", separatorPattern);
        
            if (!ReferenceEquals(channelNamePattern, null) && channelNamePattern.Contains("{customname}"))
                channelNamePattern = channelNamePattern.Replace("{customname}", customNamePattern);
            
            break;
        }

        var channel = context.Channel;

        void NewEditModel(ChannelEditModel editModel)
        {
            if (channelNamePattern != null) editModel.Name = channelNamePattern;
            editModel.Topic = "Test";
        }

        await channel.ModifyAsync(NewEditModel);


        await context.Message.DeleteAsync();

        var referenceMessage = await context.Channel.GetMessageAsync(response.Result.Id);
        await referenceMessage.DeleteAsync();

        await message.DeleteAsync();
    }
}