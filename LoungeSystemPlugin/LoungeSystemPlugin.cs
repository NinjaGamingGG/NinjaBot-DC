using DSharpPlus.CommandsNext;
using LoungeSystemPlugin.CommandModules;
using LoungeSystemPlugin.Events;
using LoungeSystemPlugin.PluginHelper;
using NinjaBot_DC;
using PluginBase;
using Serilog;

namespace LoungeSystemPlugin;

public class LoungeSystemPlugin : IPlugin
{
    public string Name => "LoungeSystem Plugin";
    public string Description => "Simple Discord LoungeSystem Plugin.";
    public string? PluginDirectory { get; set; }
    public void OnLoad()
    {
        if(ReferenceEquals(PluginDirectory, null))
            OnUnload();

        SqLiteHelper.OpenSqLiteConnection(PluginDirectory!);
        SqLiteHelper.InitializeSqliteTables();

        var slashCommands = Worker.GetServiceSlashCommandsExtension();
        slashCommands.RegisterCommands<LoungeSystemSubGroupContainer>();

        var client = Worker.GetServiceDiscordClient();

        var commandsNext = client.GetCommandsNext();
        commandsNext.RegisterCommands<CommandNextModule>();
        
        client.VoiceStateUpdated += VoiceStateUpdated.ChannelEnter;
        client.VoiceStateUpdated += VoiceStateUpdated.ChannelLeave;

        client.ComponentInteractionCreated += ComponentInteractionCreated.InterfaceButtonPressed;
        
        
        Directory.CreateDirectory(PluginDirectory!);

        Task.Run(async () =>
        {
            await StartupCleanup.Execute();
        });

        Log.Information("[{Name}] Plugin Loaded", Name);
    }

    public void OnUnload()
    {
        var client = Worker.GetServiceDiscordClient();
        
        client.VoiceStateUpdated -= VoiceStateUpdated.ChannelEnter;
        client.VoiceStateUpdated -= VoiceStateUpdated.ChannelLeave;
        
        SqLiteHelper.CloseSqLiteConnection();
        
        Log.Information("[{Name}] Plugin Unloaded", Name);
    }
}