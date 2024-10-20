using Dapper;
using DSharpPlus.Entities;
using LoungeSystemPlugin.Records.Cache;
using LoungeSystemPlugin.Records.MySQL;
using MySqlConnector;
using Serilog;

namespace LoungeSystemPlugin.PluginHelper;

public static class LoungeSetupUiHelper
{
    private const string UiBaseMessageContent =
        ":desktop: **Lounge Setup UI.**\n*This UI will guide you trough the Lounge Setup Process.*\n*This UI will get invalid after 15 Minutes!*\n\n";
    
    private const string NoPermissionsMessage = ":x: You do not have permission to do this.";
    
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
            .WithContent("<:Aheto:1286757430381903973> A error occured during the interaction:\n\n"+errorMessage);
    }


    
    public static readonly DiscordInteractionResponseBuilder NoPermissionsResponseBuilder = new DiscordInteractionResponseBuilder()
        .WithContent(NoPermissionsMessage);
    
    public static readonly DiscordMessageBuilder NoPermissionMessageBuilder = new DiscordMessageBuilder().WithContent(NoPermissionsMessage);

    public static async void CompleteSetup(LoungeSetupRecord setupRecord,ulong guildId, ulong interfaceChannelId = 0)
    {
        var parseSuccess = ulong.TryParse(setupRecord.ChannelId, out var targetChannelId);

        if (!parseSuccess)
        {
            Log.Error("[{PluginName}] Unable to parse channelId for {targetChannelId}", LoungeSystemPlugin.GetStaticPluginName(), setupRecord.ChannelId);    
        }
        
        var newConfigRecord = new LoungeSystemConfigurationRecord()
        {
            GuildId = guildId,
            TargetChannelId = targetChannelId,
            InterfaceChannelId = interfaceChannelId,
            LoungeNamePattern = setupRecord.NameDecorator + " " +  setupRecord.NamePattern,
        };
        
        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        try
        {
            var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
        
            if (ReferenceEquals(mySqlConnection, null))
            {
                Log.Error("[{PluginName}] Unable to connect to database!",LoungeSystemPlugin.GetStaticPluginName());
                return;
            }
        
            var alreadyExists = await mySqlConnection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM LoungeSystemConfigurationIndex WHERE GuildId = @GuildId AND TargetChannelId = @TargetChannelId",
                new { GuildId = guildId, TargetChannelId = targetChannelId });
        
            if (alreadyExists != 0)
            {
                Log.Error("[{PluginName}] Error while Executing Setup Completion in LoungeSetupUiHelper. Configuration already exists!", LoungeSystemPlugin.GetStaticPluginName());
                return;
            }
        
            await mySqlConnection.ExecuteAsync(
                "INSERT INTO LoungeSystemConfigurationIndex (GuildId, TargetChannelId, InterfaceChannelId, LoungeNamePattern, DecoratorPattern) VALUES (@GuildId, @TargetChannelId, @InterfaceChannelId, @LoungeNamePattern, @DecoratorPattern)",
                newConfigRecord);
        
            await mySqlConnection.CloseAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "[{PluginName}] Error while Executing Mysql Operations on LoungeSetupUiHelper Setup Completion", LoungeSystemPlugin.GetStaticPluginName());
        }
        
    }


}