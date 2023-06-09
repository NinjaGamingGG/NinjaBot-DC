using DSharpPlus.Entities;

namespace StatsPlugin.PluginHelper;

public static class DatabaseHandleHelper
{
    public static string GetHandleFromEnum(SlashCommandModule.ChannelHandleEnum handle)
    {

        switch (handle)
        {
            case SlashCommandModule.ChannelHandleEnum.CategoryChannel:
                return "CategoryChannelId";
            
            case SlashCommandModule.ChannelHandleEnum.MemberChannel:
                return "MemberCountChannelId";

            case SlashCommandModule.ChannelHandleEnum.BotChannel:
                return "BotCountChannelId";
            
            case SlashCommandModule.ChannelHandleEnum.TeamChannel:
                return "TeamCountChannelId";
            
            case SlashCommandModule.ChannelHandleEnum.NoChannel:
            default:
                return "NoChannel";

        }
    }
    
}