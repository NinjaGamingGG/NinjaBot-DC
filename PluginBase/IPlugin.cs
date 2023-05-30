namespace PluginBase;

public interface IPlugin
{
    string Name { get; }
    string Description { get; }
    
    public string? PluginDirectory { set; }
    
    void OnLoad();
    void OnUnload();
}