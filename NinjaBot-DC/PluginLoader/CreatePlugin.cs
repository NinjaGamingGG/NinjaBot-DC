﻿using System.Reflection;
using PluginBase;

namespace NinjaBot_DC.PluginLoader;

public static class CreatePlugin
{
    public static IEnumerable<IPlugin> CreateFromAssembly(Assembly assembly)
    {
        var count = 0;

        foreach (var type in assembly.GetTypes())
        {
            if (typeof(IPlugin).IsAssignableFrom(type))
            {
                var result = Activator.CreateInstance(type) as IPlugin;
                if (result != null)
                {
                    count++;
                    yield return result;
                }
            }
        }

        if (count == 0)
        {
            string availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
            throw new ApplicationException(
                $"Can't find any type which implements ICommand in {assembly} from {assembly.Location}.\n" +
                $"Available types: {availableTypes}");
        }
    }
}