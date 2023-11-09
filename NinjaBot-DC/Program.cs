using Serilog;

namespace NinjaBot_DC;

public static class Program
{
    private const string OutputTemplate = "[{Timestamp:dd-MM-yyyy HH:mm:ss} {Level}] {Message}{NewLine}{Exception}";
    
    public static string BasePath = Directory.GetCurrentDirectory();
    
    public static void Main(string[] args)
    {
        if (!Environment.UserInteractive)
        {
            BasePath = "C:\\NinjaBot-DC";
        }
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: OutputTemplate )
            .WriteTo.File(Path.Combine(BasePath, "service.log"),outputTemplate: OutputTemplate)
            .CreateLogger();
        
        CreateHostBuilder(args)
            .Build().Run();
        
        
        Log.CloseAndFlush();
        
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureServices((_, services) => 
        {
            services.AddHostedService<Worker>();

        }).UseSerilog();
    }
}
