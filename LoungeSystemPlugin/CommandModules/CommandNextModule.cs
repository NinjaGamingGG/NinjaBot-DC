using Dapper;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Net.Models;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.Records;
using MySqlConnector;

namespace LoungeSystemPlugin.CommandModules;

[Obsolete]
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

        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        var mySqlConnection = new MySqlConnection(connectionString);
        await mySqlConnection.OpenAsync();
        
        var channelConfigurations = await mySqlConnection.QueryAsync<LoungeSystemConfigurationRecord>("SELECT * FROM LoungeSystemConfigurationIndex WHERE GuildId = @GuildId ", new { GuildId = context.Guild.Id});
        
        var channelConfigurationList = channelConfigurations.ToList();

        var channelRecords = await mySqlConnection.QueryAsync<LoungeDbRecord>("SELECT * FROM LoungeIndex WHERE GuildId = @GuildId AND ChannelId = @ChannelId", new {GuildId = context.Guild.Id, ChannelId = context.Channel.Id});

        var channelRecordsAsList = channelRecords.ToList();

        var channelRecord = channelRecordsAsList.First();
        
        var channelNamePattern = string.Empty;

        var customNamePattern = response.Result.Content;
        var separatorPattern = string.Empty;
        var decoratorPrefix = string.Empty;
        var decoratorEmoji = string.Empty;
        var decoratorDecal = string.Empty;

        var nameReplacementRecord = await mySqlConnection.QueryAsync<LoungeMessageReplacement>("SELECT * FROM LoungeMessageReplacementIndex WHERE GuildId= @GuildId AND ChannelId = @ChannelId", new {GuildId = context.Guild.Id, ChannelId = channelRecord.OriginChannel});

        await mySqlConnection.CloseAsync();
        
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

                    case"Decorator_Decal":
                        decoratorDecal = replacement.ReplacementValue;
                        break;
                    
                    case"Decorator_Emoji":
                        decoratorEmoji = replacement.ReplacementValue;
                        break;
                    
                    case"Decorator_Prefix":
                        decoratorPrefix = replacement.ReplacementValue;
                        break;
                }

            }
        }
        
        if (!ReferenceEquals(separatorPattern, null) && separatorPattern.Contains("{Decorator_Decal}"))
            separatorPattern = separatorPattern.Replace("{Decorator_Decal}", decoratorDecal);
        
        if (!ReferenceEquals(separatorPattern, null) && separatorPattern.Contains("{Decorator_Emoji}"))
            separatorPattern = separatorPattern.Replace("{Decorator_Emoji}", decoratorEmoji);
        if (!ReferenceEquals(separatorPattern, null) && separatorPattern.Contains("{Decorator_Prefix}"))
            separatorPattern = separatorPattern.Replace("{Decorator_Prefix}", decoratorPrefix);

        foreach (var channelConfig in channelConfigurationList.Where(channelConfig => channelRecord.OriginChannel == channelConfig.TargetChannelId))
        {
            channelNamePattern = channelConfig.LoungeNamePattern;

            
            //if (channelNamePattern != null && channelNamePattern.Contains("{username}"))
            //    channelNamePattern = channelNamePattern.Replace("{username}", eventArgs.User.Username);
            if (!ReferenceEquals(channelNamePattern, null) && channelNamePattern.Contains("{Separator}"))
                channelNamePattern = channelNamePattern.Replace("{Separator}", separatorPattern);
        
            if (!ReferenceEquals(channelNamePattern, null) && channelNamePattern.Contains("{Custom_Name}"))
                channelNamePattern = channelNamePattern.Replace("{Custom_Name}", customNamePattern);
            
            break;
        }

        var channel = context.Channel;

        await channel.ModifyAsync(NewEditModel);
        
        await context.Message.DeleteAsync();

        var referenceMessage = await context.Channel.GetMessageAsync(response.Result.Id);
        await referenceMessage.DeleteAsync();

        await message.DeleteAsync();
        return;

        void NewEditModel(ChannelEditModel editModel)
        {
            if (channelNamePattern != null) editModel.Name = channelNamePattern;
            editModel.Topic = "Test";
        }
    }
}