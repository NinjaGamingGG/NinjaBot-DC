using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;

namespace LoungeSystemPlugin.CommandModules;

public class CommandNextModule : BaseCommandModule
{
    [Command("l")]
    public async Task LoungeCommand(CommandContext context, string argument)
    {
        if (argument == "rename")
        {
            var builder = new DiscordMessageBuilder().WithContent("Please respond with new Channel name");
        
            var message = await context.RespondAsync(builder);
            
            var response = await context.Message.GetNextMessageAsync();

            if (response.TimedOut)
            { 
                var errorBuilder = new DiscordMessageBuilder().WithContent("Error. Interaction Timed out");
                await message.RespondAsync(errorBuilder);
            }
        }

        if (argument == "resize")
        {

        }
    }
    
}