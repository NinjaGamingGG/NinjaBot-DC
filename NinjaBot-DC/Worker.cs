using DSharpPlus;
using System.Reflection;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using NinjaBot_DC.PluginLoader;
using PluginBase;
using Serilog;

namespace NinjaBot_DC;

public sealed class Worker : BackgroundService
{
    private static readonly IConfigurationRoot Configuration;
    
    private static readonly DiscordClient DiscordClient;

    private static readonly DiscordClientBuilder ClientBuilder;
    
    private static readonly CommandsExtension CommandsExtension;

    private static IPlugin[]? _loadedPluginsArray;
    
    private static IConfigurationRoot LoadServiceConfig()
    {
        var configurationBuilder = new ConfigurationBuilder();

        configurationBuilder.AddJsonFile("config.json", true);
        
        configurationBuilder.AddEnvironmentVariables("ninja-bot");

        if (!Program.IsDebugEnabled) 
            return configurationBuilder.Build();
        
        var assembly = AppDomain.CurrentDomain.GetAssemblies().
            SingleOrDefault(assembly => assembly.GetName().Name == "NinjaBot-DC");

        if (assembly != null) configurationBuilder.AddUserSecrets(assembly);
        
        return configurationBuilder.Build(); 
    }
    
    public static IConfigurationRoot LoadAssemblyConfig(string path, Assembly assembly, string envVarPrefix)
    {
        var configurationBuilder = new ConfigurationBuilder();

        configurationBuilder.AddJsonFile(path, true);
        
        configurationBuilder.AddEnvironmentVariables(envVarPrefix);

        if (!Program.IsDebugEnabled) 
            return configurationBuilder.Build();

        configurationBuilder.AddUserSecrets(assembly);
        
        return configurationBuilder.Build(); 
    }


    static Worker()
    {
        Configuration = LoadServiceConfig();
        var token = Configuration.GetValue<string>("ninja-bot:token");
        token ??= "";

        ClientBuilder = DiscordClientBuilder.CreateDefault(token,
            DiscordIntents.Guilds | DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents |
            DiscordIntents.GuildMembers | DiscordIntents.GuildPresences | DiscordIntents.GuildVoiceStates)
            .ConfigureLogging(builder => builder.ClearProviders().AddSerilog() );
        
        LoadPlugins();
        
        DiscordClient = ClientBuilder.Build();
        

        CommandsExtension = DiscordClient.UseCommands(new CommandsConfiguration()
        {
            DebugGuildId = Configuration.GetValue<ulong>("ninja-bot:debug-guild"),
            RegisterDefaultCommandProcessors = true
        });
        
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

    public static DiscordClientBuilder GetDiscordClientBuilder()
    {
        return ClientBuilder;
    }
    
    // ReSharper disable once MemberCanBePrivate.Global
    public static CommandsExtension GetServiceCommandsExtension()
    {
        return CommandsExtension;
    }
    
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public static CancellationToken? BotCancellationToken { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        BotCancellationToken = stoppingToken;

        var slashCommandProcessor = new SlashCommandProcessor();

        await CommandsExtension.AddProcessorAsync(slashCommandProcessor);
        
        DiscordActivity status = new("/help", DiscordActivityType.Watching);

        await RegisterPluginCommands();
        
        Log.Information("Starting up the Bot");
        await DiscordClient.ConnectAsync(status, DiscordUserStatus.Online);
        

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
        
        Log.Information("Bot is shutting down...");
        
        
        var unloadTasks = new List<Task>()
        {
            UnloadPlugins()
        };
        
        //If Cancellation was requested dispose (disconnect) the discord-client
        DiscordClient.Dispose();
        
        await Task.WhenAll(unloadTasks);


    }

    /// <summary>
    /// Loads plugins from the specified plugin folder.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static void LoadPlugins()
    {
        
        var pluginFolderConfig = Configuration.GetValue<string>("ninja-bot:plugin-folder");
        if (ReferenceEquals(pluginFolderConfig, null))
        {
            Log.Fatal("Unable to load Plugin Path from Config");
            return;
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
    }

    /// <summary>
    /// Unloads loaded plugins by calling their OnUnload method.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    private static Task RegisterPluginCommands()
    {
        if (_loadedPluginsArray == null) 
            return Task.CompletedTask;

        var pluginsArray = _loadedPluginsArray;
        var commandsExtension = GetServiceCommandsExtension();

        for (var i = 0; i < pluginsArray.Length; i++)
        {
            var pluginType = pluginsArray[i].GetType();
            commandsExtension.AddCommands(pluginType.Assembly);


        }
        
        return Task.CompletedTask;
    }
    
}
