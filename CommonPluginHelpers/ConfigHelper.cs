using Microsoft.Extensions.Configuration;

namespace CommonPluginHelpers;

public static class ConfigHelper
{
    /// <summary>
    /// Loads the configuration based on the presence of environment variables.
    /// If any environment variable with the specified prefix exists, it builds the configuration from the environment variables.
    /// Otherwise, it builds the configuration from a JSON file.
    /// </summary>
    /// <param name="pluginBasePath">The base path of the plugin.</param>
    /// <param name="pluginEnvVarPrefix">The prefix of the environment variables.</param>
    /// <returns>The loaded configuration.</returns>
    public static IConfigurationRoot Load(string pluginBasePath, string pluginEnvVarPrefix)
    {
        // Load all environment variables
        var environmentVariables = Environment.GetEnvironmentVariables();
    
        // Check if any variables with the Prefix are present
        var isAnyEnvVarPresent = environmentVariables.Contains(pluginEnvVarPrefix);

        // If any env-var are present, it returns the configuration from the env-vars, otherwise config from the JSON file.
        return isAnyEnvVarPresent 
            ? BuildConfigFromEnvVar(pluginEnvVarPrefix)
            : BuildConfigFromJsonFile(pluginBasePath);
    }

    /// <summary>
    /// Builds the configuration from a JSON file.
    /// </summary>
    /// <param name="basePath">The base path of the plugin.</param>
    /// <returns>The loaded configuration.</returns>
    private static IConfigurationRoot BuildConfigFromJsonFile(string basePath)
    {
        var builder = new ConfigurationBuilder();
        return builder
            .SetBasePath(basePath)
            .AddJsonFile(Path.Combine(basePath, "config.json"), optional: false, reloadOnChange: true)
            .Build();
    }

    /// <summary>
    /// Builds the configuration from the environment variables with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix of the environment variables.</param>
    /// <returns>The loaded configuration.</returns>
    private static IConfigurationRoot BuildConfigFromEnvVar(string prefix)
    {
        var builder = new ConfigurationBuilder();
        builder.AddEnvironmentVariables(prefix);
        return builder.Build();
    }
}