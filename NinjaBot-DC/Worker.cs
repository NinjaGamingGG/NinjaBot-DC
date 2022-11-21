using DSharpPlus;
using DSharpPlus.CommandsNext;
using NinjaBot_DC.Commands;
using NinjaBot_DC.Extensions;
using System.Data.SQLite;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Serilog;

namespace NinjaBot_DC;

public class Worker : BackgroundService
{
    //private readonly IConfigurationRoot _configuration;

    public static SQLiteConnection SqLiteConnection = null!;
    
    private readonly DiscordClient _discord;

    public Worker()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("config.json", optional: false, reloadOnChange: true)
            .Build();

        var sqliteSource = configuration.GetValue<string>("ninja-bot:sqlite-source");
        SqLiteConnection = new SQLiteConnection($"Data Source={sqliteSource};Version=3;New=True;Compress=True;");

        var token = configuration.GetValue<string>("ninja-bot:token");

        var logFactory = new LoggerFactory().AddSerilog();
        
        _discord = new DiscordClient(new DiscordConfiguration()
        {
            Token = token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.Guilds | DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents,
            LoggerFactory = logFactory
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var taskList = new List<Task>() {RegisterCommands(), RegisterEvents(), InitializeDatabase()};
        await Task.WhenAll(taskList);
        
        Log.Information("Starting up the Bot");
        await _discord.ConnectAsync();
        
        var startupTasks = new List<Task>() {LoungeSystem.StartupCleanup(_discord), ServerStats.RefreshServerStats(_discord)};
        await Task.WhenAll(startupTasks);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private Task SetupInteractivity()
    {
        _discord.UseInteractivity(new InteractivityConfiguration()
        {
            PollBehaviour = PollBehaviour.KeepEmojis,
            Timeout = TimeSpan.FromSeconds(30)
        });
        return Task.CompletedTask;
    }

    private Task RegisterCommands()
    {
        Log.Information("Registering Commands");
        var commands = _discord.UseCommandsNext(new CommandsNextConfiguration()
        {
            StringPrefixes  = new []{"!"},
        });

        commands.RegisterCommands<LoungeCommandModule>();
        commands.RegisterCommands<ServerStatsCommandModule>();
        commands.RegisterCommands<ReactionRolesCommandModule>();
        
        return Task.CompletedTask;
    }

    private Task RegisterEvents()
    {
        Log.Information("Registering Events");
        _discord.VoiceStateUpdated += LoungeSystem.VoiceStateUpdated_ChanelEnter;
        _discord.VoiceStateUpdated += LoungeSystem.VoiceStateUpdated_ChanelLeave;
        _discord.MessageReactionAdded += ReactionRoles.MessageReactionAdded; 
        _discord.MessageReactionRemoved += ReactionRoles.MessageReactionRemoved; 

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
            
            await using var sqLiteReactionTableCommand = SqLiteConnection.CreateCommand();
            {
                sqLiteReactionTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS ReactionMessagesIndex (GuildId INTEGER, MessageId INTEGER, MessageTag VARCHAR(20))";
            
                await sqLiteReactionTableCommand.ExecuteNonQueryAsync();
            }
            
            


        }
        catch (Exception e)
        {
            Log.Error(e, "Unable to open the sqlite database connection");
        }
    }
}
