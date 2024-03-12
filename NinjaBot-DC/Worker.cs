using DSharpPlus;
using DSharpPlus.CommandsNext;
using System.Reflection;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using NinjaBot_DC.PluginLoader;
using PluginBase;
using Serilog;

namespace NinjaBot_DC;

public sealed class Worker : BackgroundService
{
    private static readonly IConfigurationRoot Configuration;

    //private static readonly SQLiteConnection SqLiteConnection;
    
    private static readonly DiscordClient DiscordClient;

    private static readonly SlashCommandsExtension SlashCommandsExtension;

    private static IPlugin[]? _loadedPluginsArray;
    
    private static IConfigurationRoot LoadServiceConfig()
    {
        //Check if there are any env variables set by loading the most mandatory variable
        var testLoad = Environment.GetEnvironmentVariable("ninja-bot:token");
        
        //If none are found try to read from config files
        if (testLoad == null)
        {
            return new ConfigurationBuilder()
                .SetBasePath(Program.BasePath)
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .Build();
        }

        var builder = new ConfigurationBuilder();
        builder.AddEnvironmentVariables();
        
        //If env vars are set load them
        return builder.Build();
    }


    static Worker()
    {
 
            
        Configuration = LoadServiceConfig();
        var token = Configuration.GetValue<string>("ninja-bot:token");
        token ??= "";
        
        var logFactory = new LoggerFactory().AddSerilog();
        
        DiscordClient = new DiscordClient(new DiscordConfiguration()
        {
            Token = token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.Guilds | DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents |
                      DiscordIntents.GuildMembers | DiscordIntents.GuildPresences | DiscordIntents.GuildVoiceStates,
            LoggerFactory = logFactory
        });
        
        SlashCommandsExtension = DiscordClient.UseSlashCommands();
        DiscordClient.UseInteractivity(new InteractivityConfiguration()
        {
        
            Timeout = TimeSpan.FromSeconds(60)
            
        });
    }

    public static IConfigurationRoot GetServiceConfig()
    {
        return Configuration;
    }
    

    public static DiscordClient GetServiceDiscordClient()
    {
        return DiscordClient;
    }
    
    public static SlashCommandsExtension GetServiceSlashCommandsExtension()
    {
        return SlashCommandsExtension;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        var taskList = new List<Task>() {RegisterCommands(), RegisterEvents()};
        await Task.WhenAll(taskList);
        
        await LoadPlugins();
        
        Log.Information("Starting up the Bot");
        await DiscordClient.ConnectAsync();
        

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
        
        Log.Information("Bot is shutting down...");
        
        //If Cancellation was requested dispose (disconnect) the discord-client
        DiscordClient.Dispose();

        var unloadTasks = new List<Task>()
        {
            UnloadPlugins()
        };
        
        await Task.WhenAll(unloadTasks);


    }

    private static Task LoadPlugins()
    {
        
        var pluginFolderConfig = Configuration.GetValue<string>("ninja-bot:plugin-folder");
        if (ReferenceEquals(pluginFolderConfig, null))
        {
            Log.Fatal("Unable to load Plugin Path from Config");
            return Task.CompletedTask;
        }

        var pluginFolder = Path.Combine(Program.BasePath ,pluginFolderConfig);
    
        Directory.CreateDirectory(pluginFolder);

        var pluginPaths = Directory.GetFiles(pluginFolder, "*.dll");
    

        var pluginsArray = pluginPaths.SelectMany(pluginPath =>
        {
            var pluginAssembly = LoadPlugin.LoadPluginFromPath(pluginPath);
            return CreatePlugin.CreateFromAssembly(pluginAssembly);
        }).ToArray();
        
        Log.Information("Loading {PluginCount} Plugins from {FolderPath}", pluginsArray.Length, pluginFolder);
        
        for (var i = 0; i < pluginsArray.Length; i++)
        {
            var plugin = pluginsArray.ElementAt(i);

            Log.Information("Loading Plugin: {PluginName}", plugin.Name);
            
            var pluginAssembly = Assembly.GetAssembly(plugin.GetType());
            
            if (pluginAssembly == null)
                continue;
            
            plugin.OnLoad();
        }

        _loadedPluginsArray = pluginsArray;
        
        return Task.CompletedTask;
    }

    private static Task UnloadPlugins()
    {
        if (_loadedPluginsArray == null) 
            return Task.CompletedTask;

        var pluginsArray = _loadedPluginsArray;

        for (var i = 0; i < pluginsArray.Length; i++)
        {
            pluginsArray[i].OnUnload();
        }
        
        return Task.CompletedTask;
    }

    private static Task RegisterCommands()
    {
        var stringPrefix = Configuration.GetValue<string>("ninja-bot:prefix");

        stringPrefix ??= "1";
        
        Log.Information("Registering Commands");
        var commands = DiscordClient.UseCommandsNext(new CommandsNextConfiguration()
        {
            StringPrefixes  = new []{stringPrefix},
        });

        return Task.CompletedTask;
    }

    private static Task RegisterEvents()
    {
        Log.Information("Registering Events");
        //Lounge System Events
        //DiscordClient.VoiceStateUpdated += LoungeSystem.VoiceStateUpdated_ChanelEnter;
        //DiscordClient.VoiceStateUpdated += LoungeSystem.VoiceStateUpdated_ChanelLeave;
        
        return Task.CompletedTask;
    }
}
