using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Net.Models;

namespace NinjaBot_DC.Commands;



// ReSharper disable once ClassNeverInstantiated.Global
public class LoungeCommandModule : BaseCommandModule
{
    [Command("lounge")]
    // ReSharper disable once UnusedMember.Global
#pragma warning disable CA1822
    public async Task RenameCommand(CommandContext ctx, params string[] arguments)
#pragma warning restore CA1822
    {
        if (arguments[0].ToLower() != "rename")
            return;

        if (ctx.Member == null)
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

        void NewEditModel(ChannelEditModel editModel)
        {
            editModel.Name = newName;
        }

        await channel.ModifyAsync(NewEditModel);
    }

}