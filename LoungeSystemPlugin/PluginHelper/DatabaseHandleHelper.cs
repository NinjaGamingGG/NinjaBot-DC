namespace LoungeSystemPlugin.PluginHelper;

public static class DatabaseHandleHelper
{
    public static string GetChannelHandleFromEnum(CommandModules.ReplacementHandleEnum handle)
    {

        switch (handle)
        {
            case CommandModules.ReplacementHandleEnum.Separator:
                return "Separator";
            
            case CommandModules.ReplacementHandleEnum.CustomName:
                return "Custom_Name";

            case CommandModules.ReplacementHandleEnum.DecoratorDecal:
                return "Decorator_Decal";
            
            case CommandModules.ReplacementHandleEnum.DecoratorEmoji:
                return "Decorator_Emoji";
            
            case CommandModules.ReplacementHandleEnum.DecoratorPrefix:
                return "Decorator_Prefix";
            
            default:
                return "NoHandle";

        }
    }
}