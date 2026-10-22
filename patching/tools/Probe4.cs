using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

internal static class Probe4
{
    static IEnumerable<TypeDefinition> Walk(IEnumerable<TypeDefinition> ts)
    {
        foreach (var t in ts)
        {
            yield return t;
            foreach (var n in Walk(t.NestedTypes)) yield return n;
        }
    }

    static void Main(string[] a)
    {
        var res = new ToolResolver(Path.GetDirectoryName(Path.GetFullPath(a[0])), "extracted", "tools/net40");
        var m = ModuleDefinition.ReadModule(a[0], new ReaderParameters { AssemblyResolver = res });
        string tn = a[1];
        foreach (var t in Walk(m.Types))
        {
            if (t.FullName != tn) continue;
            Console.WriteLine("TYPE " + t.FullName + " base=" + (t.BaseType == null ? "-" : t.BaseType.FullName));
            foreach (var f in t.Fields)
                if (f.Name.IndexOf(a[2], StringComparison.OrdinalIgnoreCase) >= 0)
                    Console.WriteLine("  F " + f.Name + " : " + f.FieldType.FullName);
        }
    }
}
