using DSharpPlus;
using DSharpPlus.CommandsNext;
using NinjaBot_DC.Commands;
using NinjaBot_DC.Extensions;
using System.Data.SQLite;
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
        
        var startupTasks = new List<Task>() {LoungeSystem.StartupCleanup(_discord)};
        await Task.WhenAll(startupTasks);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private Task RegisterCommands()
    {
        Log.Information("Registering Commands");
        var commands = _discord.UseCommandsNext(new CommandsNextConfiguration()
        {
            StringPrefixes  = new []{"!"},
        });

        commands.RegisterCommands<LoungeCommandModule>();
        
        return Task.CompletedTask;
    }

    private Task RegisterEvents()
    {
        Log.Information("Registering Events");
        _discord.VoiceStateUpdated += LoungeSystem.VoiceStateUpdated_ChanelEnter;
        _discord.VoiceStateUpdated += LoungeSystem.VoiceStateUpdated_ChanelLeave;

        return Task.CompletedTask;
    }

    private static async Task InitializeDatabase()
    {
        Log.Information("Initializing Database");
        try
        {
            SqLiteConnection.Open();

            var sqLiteCommand = SqLiteConnection.CreateCommand();

            sqLiteCommand.CommandText = "CREATE TABLE IF NOT EXISTS LoungeIndex (ChannelId INTEGER, OwnerId INTEGER, GuildId INTEGER)";

            await sqLiteCommand.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Unable to open the sqlite database connection");
        }
    }
}
