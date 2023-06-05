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
                break;
            
            case SlashCommandModule.ChannelHandleEnum.MemberChannel:
                return "MemberCountChannelId";
                break;
            
            case SlashCommandModule.ChannelHandleEnum.BotChannel:
                return "BotCountChannelId";
                break;
            
            case SlashCommandModule.ChannelHandleEnum.TeamChannel:
                return "TeamCountChannelId";
                break;
            
            case SlashCommandModule.ChannelHandleEnum.NoChannel:
            default:
                return "NoChannel";

        }
    }
    
}