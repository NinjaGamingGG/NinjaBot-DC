using Serilog;

namespace NinjaBot_DC;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder ConfigureSerilog(this ILoggingBuilder loggingBuilder, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        return loggingBuilder;
    }
}