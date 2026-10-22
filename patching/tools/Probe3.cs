using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class Probe3
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
        string exe = a[0];
        string mode = a[1];
        var res = new ToolResolver(Path.GetDirectoryName(Path.GetFullPath(exe)), "extracted", "tools/net40");
        var m = ModuleDefinition.ReadModule(exe, new ReaderParameters { AssemblyResolver = res });

        if (mode == "find")
        {
            string needle = a[2];
            foreach (var t in Walk(m.Types))
            {
                foreach (var mm in t.Methods)
                {
                    if (!mm.HasBody) continue;
                    foreach (var i in mm.Body.Instructions)
                    {
                        if (i.OpCode == OpCodes.Ldstr && (string)i.Operand == needle)
                            Console.WriteLine(t.FullName + "::" + mm.Name + " IL_" + i.Offset.ToString("x4"));
                    }
                }
            }
            return;
        }
    }
}
