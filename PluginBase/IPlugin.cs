namespace PluginBase;

public interface IPlugin
{
    string Name { get; }
    string EnvironmentVariablePrefix { get;}
    string Description { get; }
    
    public string? PluginDirectory { get; }
    
    void OnLoad();
    void OnUnload();
}