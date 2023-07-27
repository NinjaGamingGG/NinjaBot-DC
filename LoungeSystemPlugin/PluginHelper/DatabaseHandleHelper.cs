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
                return "CustomName";

            case CommandModules.ReplacementHandleEnum.DecoratorDecal:
                return "DecoratorDecal";
            
            case CommandModules.ReplacementHandleEnum.DecoratorEmoji:
                return "DecoratorEmoji";
            
            case CommandModules.ReplacementHandleEnum.DecoratorPrefix:
                return "DecoratorPrefix";
            
            default:
                return "NoHandle";

        }
    }
}