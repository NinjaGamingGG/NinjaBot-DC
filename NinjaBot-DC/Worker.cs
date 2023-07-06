using DSharpPlus;
using DSharpPlus.CommandsNext;
using NinjaBot_DC.Extensions;
using System.Data.SQLite;
using System.Reflection;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using NinjaBot_DC.CommandModules;
using NinjaBot_DC.PluginLoader;
using PluginBase;
using Serilog;

namespace NinjaBot_DC;

public sealed class Worker : BackgroundService
{
    private static readonly IConfigurationRoot Configuration;

    private static readonly SQLiteConnection SqLiteConnection;
    
    private static readonly DiscordClient DiscordClient;

    private static readonly SlashCommandsExtension SlashCommandsExtension;

    private static IPlugin[]? _loadedPluginsArray; 

    static Worker()
    {
        Configuration = LoadServiceConfig();

        var sqliteSource = Configuration.GetValue<string>("ninja-bot:sqlite-source");
        SqLiteConnection = new SQLiteConnection($"Data Source={sqliteSource};Version=3;New=True;Compress=True;");

        var token = Configuration.GetValue<string>("ninja-bot:token");

        var logFactory = new LoggerFactory().AddSerilog();

        token ??= "";

        DiscordClient = new DiscordClient(new DiscordConfiguration()
        {
            Token = token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.Guilds | DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents | DiscordIntents.GuildMembers | DiscordIntents.GuildPresences | DiscordIntents.GuildVoiceStates,
            LoggerFactory = logFactory
        });

        SlashCommandsExtension = DiscordClient.UseSlashCommands();
        
        DiscordClient.UseInteractivity(new InteractivityConfiguration()
        {
            Timeout = TimeSpan.FromSeconds(30)
        });
        {
            
        }
    }

    private static IConfigurationRoot LoadServiceConfig()
    {
        //Check if there are any env variables set by loading the most mandatory variable
        var testLoad = Environment.GetEnvironmentVariable("ninja-bot:token");
        
        //If none are found try to read from config files
        if (testLoad == null)
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .Build();
        }

        var builder = new ConfigurationBuilder();
        builder.AddEnvironmentVariables();
        
        //If env vars are set load them
        return builder.Build();
    }

    public static IConfigurationRoot GetServiceConfig()
    {
        return Configuration;
    }
    

    public static DiscordClient GetServiceDiscordClient()
    {
        return DiscordClient;
    }

    [Obsolete]
    public static SQLiteConnection GetServiceSqLiteConnection()
    {
        return SqLiteConnection;
    }
    
    public static SlashCommandsExtension GetServiceSlashCommandsExtension()
    {
        return SlashCommandsExtension;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var taskList = new List<Task>() {RegisterCommands(), RegisterEvents(), InitializeDatabase()};
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
            UnloadDatabase(),
            UnloadPlugins()
        };
        
        await Task.WhenAll(unloadTasks);


    }

    private static Task LoadPlugins()
    {
        var pluginFolder = Path.Combine(Directory.GetCurrentDirectory() ,"Plugins");
    
        Directory.CreateDirectory(pluginFolder);
    
        var pluginPaths = Directory.GetFiles(pluginFolder, "*.dll");
    

        var pluginsArray = pluginPaths.SelectMany(pluginPath =>
        {
            var pluginAssembly = LoadPlugin.LoadPluginFromPath(pluginPath);
            return CreatePlugin.CreateFromAssembly(pluginAssembly);
        }).ToArray();
        
        for (var i = 0; i < pluginsArray.Length; i++)
        {
            var plugin = pluginsArray.ElementAt(i);

            Log.Information("Loading Plugin: {PluginName}", plugin.Name);
            
            var pluginAssembly = Assembly.GetAssembly(plugin.GetType());
            
            if (pluginAssembly == null)
                continue;
            
            var pluginDirectory = Path.Combine(Path.GetDirectoryName(pluginAssembly.Location)!, pluginAssembly.GetName().Name!);
            Directory.CreateDirectory(pluginDirectory);
            plugin.PluginDirectory = pluginDirectory;
            
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

        commands.RegisterCommands<ReactionRolesCommandModule>();
        commands.RegisterCommands<TwitchAlertsCommandModule>();
        
        return Task.CompletedTask;
    }

    private static Task RegisterEvents()
    {
        Log.Information("Registering Events");
        //Lounge System Events
        //DiscordClient.VoiceStateUpdated += LoungeSystem.VoiceStateUpdated_ChanelEnter;
        //DiscordClient.VoiceStateUpdated += LoungeSystem.VoiceStateUpdated_ChanelLeave;
        
        //Reaction Role Events
        DiscordClient.MessageReactionAdded += ReactionRoles.MessageReactionAdded; 
        //DiscordClient.MessageReactionRemoved += ReactionRoles.MessageReactionRemoved;

            
        return Task.CompletedTask;
    }

    private static async Task InitializeDatabase()
    {
        Log.Information("Initializing Database");
        try
        {
            SqLiteConnection.Open();

            await using var sqliteTwitchAlertRoleTableCommand = SqLiteConnection.CreateCommand();
            {
                sqliteTwitchAlertRoleTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS TwitchAlertRoleIndex(GuildId INTEGER, RoleId INTEGER, RoleTag VARCHAR(50))";

                await sqliteTwitchAlertRoleTableCommand.ExecuteNonQueryAsync();
            }
            
            await using var sqliteTwitchCreatorTableCommand = SqLiteConnection.CreateCommand();
            {
                sqliteTwitchCreatorTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS TwitchCreatorIndex(GuildId INTEGER, UserId INTEGER, RoleTag VARCHAR(50))";

                await sqliteTwitchCreatorTableCommand.ExecuteNonQueryAsync();
            }
    
            await using var sqliteTwitchCreatorSocialChannelTableCommand = SqLiteConnection.CreateCommand();
            {
                sqliteTwitchCreatorSocialChannelTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS TwitchCreatorSocialMediaChannelIndex(GuildId INTEGER, UserId INTEGER, RoleTag VARCHAR(50), SocialMediaChannel VARCHAR(50),Platform VARCHAR(50) )";

                await sqliteTwitchCreatorSocialChannelTableCommand.ExecuteNonQueryAsync();
            }
            
            await using var sqliteTwitchDiscordChannelTableCommand = SqLiteConnection.CreateCommand();
            {
                sqliteTwitchDiscordChannelTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS TwitchDiscordChannelIndex(GuildId INTEGER, ChannelId INTEGER, RoleTag VARCHAR(50))";

                await sqliteTwitchDiscordChannelTableCommand.ExecuteNonQueryAsync();
            }
                
            await using var sqliteTwitchStreamIndexTableCommand = SqLiteConnection.CreateCommand();
            {
                sqliteTwitchStreamIndexTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS TwitchStreamCacheIndex(Id VARCHAR(50), ChannelName VARCHAR(50), ChannelId VARCHAR(50))";

                await sqliteTwitchStreamIndexTableCommand.ExecuteNonQueryAsync();
            }    

        }
        catch (Exception e)
        {
            Log.Error(e, "Unable to open the sqlite database connection");
        }
    }
    
    private static Task UnloadDatabase()
    {
        SqLiteConnection.Close();
        return Task.CompletedTask;
    }
}
