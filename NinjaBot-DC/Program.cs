using Serilog;

namespace NinjaBot_DC;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            var host = CreateHostBuilder(args)
                .Build();
            
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Worker service failed initiation. See exception for more details");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args).ConfigureServices((_, services) => 
        {
            services.AddHostedService<Worker>();

        }).ConfigureLogging((hostContext, builder) =>
        {
            builder.ConfigureSerilog(hostContext.Configuration);
        }).UseSerilog();
    }
}
