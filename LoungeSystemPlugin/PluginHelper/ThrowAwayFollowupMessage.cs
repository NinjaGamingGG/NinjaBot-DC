using DSharpPlus.Entities;

namespace LoungeSystemPlugin.PluginHelper;

public static class ThrowAwayFollowupMessage
{
    public static async Task HandleAsync(DiscordFollowupMessageBuilder builder, DiscordInteraction interaction, int waitDelay = 20)
    {
        var followupMessage = await interaction.CreateFollowupMessageAsync(builder);
        
        await Task.Delay(TimeSpan.FromSeconds(waitDelay));

        try
        {
            await followupMessage.DeleteAsync();
        }
        catch (DSharpPlus.Exceptions.NotFoundException)
        {
            //Do nothing
        }
    }

    public static async Task HandleAsync(DiscordMessage followupMessage)
    {
        await Task.Delay(TimeSpan.FromSeconds(15));
        
        try
        {
            await followupMessage.DeleteAsync();
        }
        catch (DSharpPlus.Exceptions.NotFoundException)
        {
            //Do nothing
        }
    }
}