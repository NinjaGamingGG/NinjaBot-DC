using Microsoft.Extensions.Configuration;
using NinjaBot_DC;

namespace Ranksystem.PluginHelper;

public static class ConfigHelper
{
    public static IConfigurationRoot Load()
    {
        
        //Check if there are any env variables set by loading the most mandatory variable
        var testLoad = Environment.GetEnvironmentVariable("stats-plugin:sqlite-source");
        
        //If none are found try to read from config files
        if (testLoad == null)
        {
            var directory = Program.BasePath;
            
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(Path.Combine(directory,"config.json"), optional: false, reloadOnChange: true)
                .Build();
        }

        var builder = new ConfigurationBuilder();
        builder.AddEnvironmentVariables();
        
        //If env vars are set load them
        return builder.Build();
    }
    
}