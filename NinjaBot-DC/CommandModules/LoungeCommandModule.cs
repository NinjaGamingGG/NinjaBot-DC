using Dapper.Contrib.Extensions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Net.Models;
using NinjaBot_DC.Models;
using NinjaBot_DC.Models.LoungeSystemModels;

namespace NinjaBot_DC.CommandModules;



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
            
            if (newSize is > 8 or < 0)
                return;

            if (parseSuccess)
                await ResizeLounge(ctx, newSize);
        }

        if (arguments[0].ToLower() == "claim")
        {
            await ClaimLounge(ctx);
        }



    }

    private static async Task RenameLounge(CommandContext context, string newName)
    {
        if (context.Member == null)
            return;
        
        var channel = context.Member.VoiceState.Channel;
        
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
    
    private static async Task ClaimLounge(CommandContext context)
    {
        if (context.Member == null)
            return;
        
        var channel = context.Member.VoiceState.Channel;
        
        if (channel == null)
            return;

        var sqlite = Worker.GetServiceSqLiteConnection();
        var loungeModel = await sqlite.GetAsync<LoungeDbModel>(channel.Id);

        if (loungeModel == null)
            return;

        var channelMembers = channel.Users;

        var containsOwner = false;
        
        foreach (var member in channelMembers)
        {
            if (member.Id == loungeModel.OwnerId)
                containsOwner = true;
        }
        
        if (containsOwner == true)
            return;

        loungeModel.OwnerId = context.Member.Id;

        var sqlSuccess = await sqlite.UpdateAsync(loungeModel);

        if (sqlSuccess)
            await context.Channel.SendMessageAsync($"{context.Member.DisplayName} ist nun Lounge Owner");
    }

}