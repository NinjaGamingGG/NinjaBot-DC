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

        DiscordClient = new DiscordClient(new DiscordConfiguration()
        {
            Token = token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.Guilds | DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents | DiscordIntents.GuildMembers | DiscordIntents.GuildPresences,
            LoggerFactory = logFactory
        });

        SlashCommandsExtension = DiscordClient.UseSlashCommands();
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
        
        var startupTasks = new List<Task>() {
                LoungeSystem.StartupCleanup(DiscordClient), 
                TwitchAlerts.InitExtensionAsync(),
        };
                
        
        await Task.WhenAll(startupTasks);
        
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

    private static Task SetupInteractivity()
    {
        DiscordClient.UseInteractivity(new InteractivityConfiguration()
        {
            PollBehaviour = PollBehaviour.KeepEmojis,
            Timeout = TimeSpan.FromSeconds(30)
        });
        return Task.CompletedTask;
    }

    private static Task RegisterCommands()
    {
        var stringPrefix = Configuration.GetValue<string>("ninja-bot:prefix");
        
        Log.Information("Registering Commands");
        var commands = DiscordClient.UseCommandsNext(new CommandsNextConfiguration()
        {
            StringPrefixes  = new []{stringPrefix},
        });

        commands.RegisterCommands<LoungeCommandModule>(); 
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

            await using var sqLiteLoungeTableCommand = SqLiteConnection.CreateCommand();
            {
                sqLiteLoungeTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS LoungeIndex (ChannelId INTEGER, OwnerId INTEGER, GuildId INTEGER)";
            
                await sqLiteLoungeTableCommand.ExecuteNonQueryAsync();
            }

            await using var sqLiteReactionMessageTableCommand = SqLiteConnection.CreateCommand();
            {
                sqLiteReactionMessageTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS ReactionMessagesIndex (GuildId INTEGER, MessageId INTEGER, MessageTag VARCHAR(20))";
            
                await sqLiteReactionMessageTableCommand.ExecuteNonQueryAsync();
            }

            await using var sqLiteReactionRoleCommand = SqLiteConnection.CreateCommand();
            {
                sqLiteReactionRoleCommand.CommandText = "CREATE TABLE IF NOT EXISTS ReactionRoleIndex(GuildId INTEGER, MessageTag VARCHAR(50),ReactionEmojiTag VARCHAR(50),LinkedRoleId INTEGER)";

                await sqLiteReactionRoleCommand.ExecuteNonQueryAsync();
            }
            
            await using var sqliteTwitchAlertRoleTableCommand = SqLiteConnection.CreateCommand();
            {
                sqLiteReactionRoleCommand.CommandText = "CREATE TABLE IF NOT EXISTS TwitchAlertRoleIndex(GuildId INTEGER, RoleId INTEGER, RoleTag VARCHAR(50))";

                await sqLiteReactionRoleCommand.ExecuteNonQueryAsync();
            }
            
            await using var sqliteTwitchCreatorTableCommand = SqLiteConnection.CreateCommand();
            {
                sqLiteReactionRoleCommand.CommandText = "CREATE TABLE IF NOT EXISTS TwitchCreatorIndex(GuildId INTEGER, UserId INTEGER, RoleTag VARCHAR(50))";

                await sqLiteReactionRoleCommand.ExecuteNonQueryAsync();
            }
    
            await using var sqliteTwitchCreatorSocialChannelTableCommand = SqLiteConnection.CreateCommand();
            {
                sqLiteReactionRoleCommand.CommandText = "CREATE TABLE IF NOT EXISTS TwitchCreatorSocialMediaChannelIndex(GuildId INTEGER, UserId INTEGER, RoleTag VARCHAR(50), SocialMediaChannel VARCHAR(50),Platform VARCHAR(50) )";

                await sqLiteReactionRoleCommand.ExecuteNonQueryAsync();
            }
            
            await using var sqliteTwitchDiscordChannelTableCommand = SqLiteConnection.CreateCommand();
            {
                sqLiteReactionRoleCommand.CommandText = "CREATE TABLE IF NOT EXISTS TwitchDiscordChannelIndex(GuildId INTEGER, ChannelId INTEGER, RoleTag VARCHAR(50))";

                await sqLiteReactionRoleCommand.ExecuteNonQueryAsync();
            }
                
            await using var sqliteTwitchStreamIndexTableCommand = SqLiteConnection.CreateCommand();
            {
                sqLiteReactionRoleCommand.CommandText = "CREATE TABLE IF NOT EXISTS TwitchStreamCacheIndex(Id VARCHAR(50), ChannelName VARCHAR(50), ChannelId VARCHAR(50))";

                await sqLiteReactionRoleCommand.ExecuteNonQueryAsync();
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
