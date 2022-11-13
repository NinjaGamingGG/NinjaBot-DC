using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.EventArgs;

namespace NinjaBot_DC.Extensions;

public static class LoungeSystem
{
    public static async Task VoiceStateUpdated_ChanelEnter(DiscordClient client, VoiceStateUpdateEventArgs eventArgs)
    {
        await Task.Delay(1);
    }
    
    public static async Task VoiceStateUpdated_ChanelLeave(DiscordClient client, VoiceStateUpdateEventArgs eventArgs)
    {
        await Task.Delay(1);
    }
}