using System.Reflection;
using System.Xml.Serialization;
using Serilog;

namespace PluginBase;

public abstract class DefaultPlugin : IPlugin
{
    public required string Name { get; init; }
    public required string EnvironmentVariablePrefix { get; init; }
    public required string Description { get; init; }
    public required string PluginDirectory { get; init; }
    
    public required CancellationTokenSource CancellationTokenSource;

    protected DefaultPlugin()
    {
        var assembly = Assembly.GetAssembly(GetType());

        if (assembly == null)
            return;

        PluginDirectory = Path.Combine(Path.GetDirectoryName(assembly.Location)!, assembly.GetName().Name!);

        if (!Directory.Exists(PluginDirectory))
            Directory.CreateDirectory(PluginDirectory);

        var serializer = new XmlSerializer(typeof(PluginInfo));

        PluginInfo? loadedPluginInfos;
        CancellationTokenSource = new CancellationTokenSource();
        
        

        try
        {
            using var reader = new StreamReader(Path.Combine(PluginDirectory, "plugin.xml"));

            loadedPluginInfos = (PluginInfo)serializer.Deserialize(reader)!;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex,"Unable to Load plugin.xml of Assembly {AssemblyName}", assembly.FullName);
            Name = "InvalidPlugin";
            EnvironmentVariablePrefix = "";
            Description = "Unable to Load plugin.xml";
            return;
        }


        Name = loadedPluginInfos.PluginName;
        EnvironmentVariablePrefix = loadedPluginInfos.EnvironmentVariablePrefix;
        Description = loadedPluginInfos.Description;
    }

    public abstract void OnLoad();

    public abstract void OnUnload();

}
