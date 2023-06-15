using Dapper;
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
            
            default:
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

        var newName = response.Result.Content;

        var channel = context.Channel;

        void NewEditModel(ChannelEditModel editModel)
        {
            editModel.Name = newName;
        }

        await channel.ModifyAsync(NewEditModel);


        await context.Message.DeleteAsync();

        var referenceMessage = await context.Channel.GetMessageAsync(response.Result.Id);
        await referenceMessage.DeleteAsync();

        await message.DeleteAsync();
    }
}