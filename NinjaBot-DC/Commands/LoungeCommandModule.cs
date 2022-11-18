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
    public async Task LoungeCommand(CommandContext ctx, params string[] arguments)
#pragma warning restore CA1822
    {
        if (arguments[0].ToLower() == "rename")
        {
            arguments[0] = $"╠🥳» ";

            var newName = string.Join(' ', arguments);
            
            await RenameLounge(ctx, newName);
        }

        if (arguments[0].ToLower() == "resize")
        {
            var parseSuccess = Int32.TryParse(arguments[1], out var newSize);

            if (parseSuccess)
                await ResizeLounge(ctx, newSize);
        }



    }

    private static async Task RenameLounge(CommandContext ctx, string newName)
    {
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

        void NewEditModel(ChannelEditModel editModel)
        {
            editModel.Name = newName;
        }

        await channel.ModifyAsync(NewEditModel);
    }

    private static async Task ResizeLounge(CommandContext context, int newSize)
    {
        if (context.Member == null)
            return;
        
        var channel = context.Member.VoiceState.Channel;
        
        if (channel == null)
            return;
        
        void NewEditModel(ChannelEditModel editModel)
        {
            editModel.Userlimit = newSize;
        }

        await channel.ModifyAsync(NewEditModel);
    }

}