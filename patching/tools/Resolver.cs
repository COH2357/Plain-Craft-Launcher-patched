using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mono.Cecil;

internal sealed class ToolResolver : IAssemblyResolver
{
    private readonly Dictionary<string, AssemblyDefinition> cache = new Dictionary<string, AssemblyDefinition>(StringComparer.OrdinalIgnoreCase);
    private readonly string[] dirs;

    public ToolResolver(params string[] dirs)
    {
        this.dirs = dirs;
    }

    public AssemblyDefinition Resolve(AssemblyNameReference name)
    {
        return Resolve(name, new ReaderParameters { AssemblyResolver = this });
    }

    public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
    {
        AssemblyDefinition ad;
        if (cache.TryGetValue(name.FullName, out ad)) return ad;
        string path = Find(name.Name + ".dll");
        if (path == null)
        {
            try
            {
                var a = Assembly.Load(name.FullName);
                if (!string.IsNullOrEmpty(a.Location)) path = a.Location;
            }
            catch
            {
            }
        }
        if (path == null) throw new AssemblyResolutionException(name);
        ad = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { AssemblyResolver = this });
        cache[name.FullName] = ad;
        return ad;
    }

    private string Find(string file)
    {
        foreach (string d in dirs)
        {
            string p = Path.Combine(d, file);
            if (File.Exists(p)) return p;
        }
        return null;
    }

    public void Dispose()
    {
        foreach (var a in cache.Values) a.Dispose();
        cache.Clear();
    }
}
