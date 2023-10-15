using Serilog;

namespace NinjaBot_DC;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder ConfigureSerilog(this ILoggingBuilder loggingBuilder, IConfiguration configuration)
    {
        const string outputTemplate = "[{Timestamp:dd-MM-yyyy HH:mm:ss} {Level}] {Message}{NewLine}{Exception}";
        
#if DEBUG
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: outputTemplate )
            .WriteTo.File(Path.Combine(Directory.GetCurrentDirectory(), "service.log"),outputTemplate: outputTemplate)
            .CreateLogger();

        return loggingBuilder;
#else
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate: outputTemplate)
            .WriteTo.File(Path.Combine(Directory.GetCurrentDirectory(), "service.log"),outputTemplate: outputTemplate)
            .CreateLogger();
        
                return loggingBuilder;
#endif


    }
}