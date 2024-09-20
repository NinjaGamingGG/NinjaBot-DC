using DSharpPlus.Entities;

namespace LoungeSystemPlugin.PluginHelper;

public static class LoungeSetupUiHelper
{
    private const string UiBaseMessageContent =
        ":desktop: **Lounge Setup UI.**\n*This UI will guide you trough the Lounge Setup Process.*\n*This UI will get invalid after 15 Minutes!*\n\n";
    
    public static readonly DiscordMessageBuilder InitialMessageBuilder = new DiscordMessageBuilder()
        .WithContent(UiBaseMessageContent +
                     "Please select a Channel Users will enter in order to create a Lounge / Temporary VC")
        .AddComponents([
            new DiscordChannelSelectComponent("lounge_setup_channel_select", "Target Channel",[DiscordChannelType.Voice])]);
    
    public static readonly DiscordInteractionResponseBuilder ChannelSelectedMessageBuilder = new DiscordInteractionResponseBuilder()
        .WithContent(UiBaseMessageContent +
                     ":white_check_mark: Channel Selected\n:point_down: Please click the Button Below to Enter the new Channel Name Pattern that will be used for new Channels")
        .AddComponents([
            new DiscordButtonComponent(DiscordButtonStyle.Primary,"lounge_setup_name-pattern_button","Set Name Pattern")]);

    public static readonly DiscordInteractionResponseBuilder ChannelNamePatternModalBuilder =
        new DiscordInteractionResponseBuilder().WithTitle("Set your Channel Name Pattern")
            .WithCustomId("lounge_setup_name-pattern_modal")
            .AddComponents(new DiscordTextInputComponent("Name Pattern","lounge_setup_name-pattern_modal_name","For example use {username}'s Lounge"))
            .AddComponents(new DiscordTextInputComponent("Decorator","lounge_setup_name-pattern_modal_decorator","Displayed before the Name. For Example use: ~🗿»"));
    
    
    public static readonly DiscordInteractionResponseBuilder ModalSubmittedResponseBuilder = 
        new DiscordInteractionResponseBuilder()
            .WithContent(UiBaseMessageContent +
                         ":white_check_mark: Name Pattern Set\n:point_down: Please select below whether you want the Lounge Interface in a Separate Channel or in the Lounge's Chat")
            .AddComponents(
                new DiscordSelectComponent("lounge_setup_interface_selector", "Click to select" , new List<DiscordSelectComponentOption>()
                {
                    new("Separate", "separate_interface"),
                    new("Internal", "internal_interface")
                }));
    
    public static readonly DiscordInteractionResponseBuilder InterfaceSelectedResponseBuilder = 
        new DiscordInteractionResponseBuilder()
            .WithContent(UiBaseMessageContent +
                         ":white_check_mark: Separate Interface Selected\n:point_down: Please select below which Channel should be used for the interface")
            .AddComponents([
                new DiscordChannelSelectComponent("lounge_setup_interface_channel_select", "Interface Channel",[DiscordChannelType.Text])]);

    public static readonly DiscordInteractionResponseBuilder LoungeSetupComplete =
        new DiscordInteractionResponseBuilder()
            .WithContent(UiBaseMessageContent +
                         ":trophy: Setup Completed Successfully!\nNow you can use your new Lounge Channel");

    public static DiscordInteractionResponseBuilder InteractionFailedResponseBuilder(string errorMessage)
    {
        return new DiscordInteractionResponseBuilder()
            .WithContent($"A error occured during the interaction: {errorMessage}");
    }


}