using Serilog;

namespace NinjaBot_DC;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder ConfigureSerilog(this ILoggingBuilder loggingBuilder, IConfiguration configuration)
    {
        
#if DEBUG
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(Directory.GetCurrentDirectory(), "service.log"))
            .CreateLogger();

        return loggingBuilder;
#else
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(Directory.GetCurrentDirectory(), "service.log"))
            .CreateLogger();
        
                return loggingBuilder;
#endif


    }
}