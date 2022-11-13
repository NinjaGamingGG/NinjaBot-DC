using DSharpPlus;
using NinjaBot_DC.Extensions;
using Serilog;

namespace NinjaBot_DC;

public class Worker : BackgroundService
{
    private readonly IConfigurationRoot _configuration;

    private readonly DiscordClient _discord;

    public Worker()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("config.json", optional: false, reloadOnChange: true)
            .Build();

        var token = _configuration.GetValue<string>("ninja-bot:token");

        var logFactory = new LoggerFactory().AddSerilog();
        
        _discord = new DiscordClient(new DiscordConfiguration()
        {
            Token = token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged,
            LoggerFactory = logFactory
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("Registering Events");
        _discord.VoiceStateUpdated += LoungeSystem.VoiceStateUpdated_ChanelEnter;
        _discord.VoiceStateUpdated += LoungeSystem.VoiceStateUpdated_ChanelLeave;

        Log.Information("Starting up the Bot");
        await _discord.ConnectAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}
