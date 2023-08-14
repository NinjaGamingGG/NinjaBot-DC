using Microsoft.Extensions.Configuration;

namespace GreeterPlugin.PluginHelpers;

public static class ConfigHelper
{
    private static string _basePath = Directory.GetCurrentDirectory();
    
    public static IConfigurationRoot Load()
    {
        
        //Check if there are any env variables set by loading the most mandatory variable
        var testLoad = Environment.GetEnvironmentVariable("greeter-plugin:sqlite-source");
        
        //If none are found try to read from config files
        if (testLoad == null)
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(Path.Combine(_basePath,"config.json"), optional: false, reloadOnChange: true)
                .Build();
        }

        var builder = new ConfigurationBuilder();
        builder.AddEnvironmentVariables();
        
        //If env vars are set load them
        return builder.Build();
    }
    
    public static void SetBasePath(string path)
    {
        _basePath = path;
    }
}