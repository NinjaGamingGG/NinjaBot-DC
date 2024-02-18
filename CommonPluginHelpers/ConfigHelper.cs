using Microsoft.Extensions.Configuration;

namespace CommonPluginHelpers;

public static class ConfigHelper
{
    public static IConfigurationRoot Load(string pluginBasePath, string pluginEnvironmentVariablePrefix)
    {
        
        //Check if there are any env variables set by loading the most mandatory variable
        var testLoad = Environment.GetEnvironmentVariables();
        
        var builder = new ConfigurationBuilder();
        
        //If none are found try to read from config files
        if (!testLoad.Contains(pluginEnvironmentVariablePrefix))
        {

            
            return builder
                .SetBasePath(pluginBasePath)
                .AddJsonFile(Path.Combine(pluginBasePath,"config.json"), optional: false, reloadOnChange: true)
                .Build();
        }
        
        builder.AddEnvironmentVariables();
        
        //If env vars are set load them
        return builder.Build();
    }

}