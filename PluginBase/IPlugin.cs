namespace PluginBase;

public interface IPlugin
{
    string Name { get; }
    string Description { get; }
    
    void OnLoad();
    void OnUnload();
}