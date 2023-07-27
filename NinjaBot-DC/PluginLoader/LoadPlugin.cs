using System.Reflection;

namespace NinjaBot_DC.PluginLoader;

public static class LoadPlugin
{
    public static Assembly LoadPluginFromPath(string relativePath)
    {
        var loadContext = new PluginLoadContext(relativePath);
        return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(relativePath)));
    }
}