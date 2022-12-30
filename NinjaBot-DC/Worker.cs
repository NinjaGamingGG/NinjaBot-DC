using DSharpPlus;
using DSharpPlus.CommandsNext;
using NinjaBot_DC.Extensions;
using System.Data.SQLite;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using NinjaBot_DC.CommandModules;
using Serilog;

namespace NinjaBot_DC;

public class Worker : BackgroundService
{
    private static readonly IConfigurationRoot Configuration;

    private static readonly SQLiteConnection SqLiteConnection;
    
    private static readonly DiscordClient DiscordClient;

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

    public static SQLiteConnection GetServiceSqLiteConnection()
    {
        return SqLiteConnection;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        var taskList = new List<Task>() {RegisterCommands(), RegisterEvents(), InitializeDatabase()};
        await Task.WhenAll(taskList);
        
        Log.Information("Starting up the Bot");
        await DiscordClient.ConnectAsync();
        
        var startupTasks = new List<Task>() {
                LoungeSystem.StartupCleanup(DiscordClient), 
                ServerStats.RefreshServerStats(DiscordClient), 
                TwitchAlerts.InitExtensionAsync(),
                RankSystem.UpdateVoiceActivity()
        };
                
        
        await Task.WhenAll(startupTasks);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
        
        //If Cancellation was requested dispose (disconnect) the discord-client
        DiscordClient.Dispose();
        Log.Information("Bot is shutting down...");

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
        commands.RegisterCommands<ServerStatsCommandModule>();
        commands.RegisterCommands<ReactionRolesCommandModule>();
        commands.RegisterCommands<TwitchAlertsCommandModule>();
        
        return Task.CompletedTask;
    }

    private static Task RegisterEvents()
    {
        Log.Information("Registering Events");
        //Lounge System Events
        DiscordClient.VoiceStateUpdated += LoungeSystem.VoiceStateUpdated_ChanelEnter;
        DiscordClient.VoiceStateUpdated += LoungeSystem.VoiceStateUpdated_ChanelLeave;
        
        //Reaction Role Events
        DiscordClient.MessageReactionAdded += ReactionRoles.MessageReactionAdded; 
        //DiscordClient.MessageReactionRemoved += ReactionRoles.MessageReactionRemoved;
        
        //Rank-system Events
        DiscordClient.MessageCreated += RankSystem.MessageCreatedEvent;
        DiscordClient.MessageReactionAdded += RankSystem.ReactionAddedEvent;
            
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

            await using var sqLiteStatsTableCommand = SqLiteConnection.CreateCommand();
            {
                sqLiteStatsTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS StatsChannelIndex (GuildId INTEGER, CategoryChannelId INTEGER, MemberCountChannelId INTEGER, TeamCountChannelId INTEGER, BotCountChannelId INTEGER)";
            
                await sqLiteStatsTableCommand.ExecuteNonQueryAsync();
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
}
