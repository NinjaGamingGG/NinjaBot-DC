using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Net.Models;

namespace NinjaBot_DC.Commands;



public class LoungeCommandModule : BaseCommandModule
{
    [Command("lounge")]
    public async Task RenameCommand(CommandContext ctx, params string[] arguments)
    {
        if (arguments[0].ToLower() != "rename")
            return;

        var channel = ctx.Member.VoiceState.Channel;
        
        if (channel == null)
            return;

        var channelName = channel.Name;
        
        if (channelName == null )
            return;
        
        
        if (!channelName.Contains("🥳"))
            return;

        arguments[0] = $"╠🥳» ";

        var newName = string.Join(' ', arguments);

        var newEditModel = delegate(ChannelEditModel editModel)
        {
            editModel.Name = newName;
        };

        await channel.ModifyAsync(newEditModel);
    }

}