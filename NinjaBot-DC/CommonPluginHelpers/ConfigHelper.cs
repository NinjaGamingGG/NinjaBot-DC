namespace NinjaBot_DC.CommonPluginHelpers;

public static class ConfigHelper
{
    public static IConfigurationRoot Load(string pluginBasePath, string pluginEnvironmentVariablePrefix)
    {
        
        //Check if there are any env variables set by loading the most mandatory variable
        var testLoad = Environment.GetEnvironmentVariables();
        
        //If none are found try to read from config files
        if (!testLoad.Contains(pluginEnvironmentVariablePrefix))
        {

            
            return new ConfigurationBuilder()
                .SetBasePath(pluginBasePath)
                .AddJsonFile(Path.Combine(pluginBasePath,"config.json"), optional: false, reloadOnChange: true)
                .Build();
        }

        var builder = new ConfigurationBuilder();
        builder.AddEnvironmentVariables();
        
        //If env vars are set load them
        return builder.Build();
    }

}