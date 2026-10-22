using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class Probe2
{
    static string S(object o)
    {
        if (o == null) return "null";
        try { return o.ToString(); } catch { return "<" + o.GetType().Name + ">"; }
    }

    static void Main(string[] a)
    {
        string exe = a[0];
        string mode = a[1];
        var res = new ToolResolver(Path.GetDirectoryName(Path.GetFullPath(exe)), "extracted", "tools/net40");
        var m = ModuleDefinition.ReadModule(exe, new ReaderParameters { AssemblyResolver = res });

        if (mode == "list")
        {
            string needle = a[2];
            foreach (var t in m.Types)
                foreach (var mm in t.Methods)
                    if (mm.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                        Console.WriteLine("M " + mm.FullName + " params=" + mm.Parameters.Count + " body=" + (mm.HasBody ? mm.Body.CodeSize.ToString() : "-"));
            return;
        }

        // dump Type::Method paramCount
        string spec = a[2];
        int slash = spec.IndexOf("::");
        string tn = spec.Substring(0, slash);
        string mn = spec.Substring(slash + 2);
        int pc = a.Length > 3 ? int.Parse(a[3]) : -1;
        foreach (var t in m.Types)
        {
            if (t.FullName != tn) continue;
            foreach (var mm in t.Methods)
            {
                if (mm.Name != mn) continue;
                if (pc >= 0 && mm.Parameters.Count != pc) continue;
                if (!mm.HasBody) continue;
                Console.WriteLine("== " + mm.FullName + " code=" + mm.Body.CodeSize + " max=" + mm.Body.MaxStackSize + " vars=" + mm.Body.Variables.Count);
                foreach (var v in mm.Body.Variables) Console.WriteLine("   V_" + v.Index + " " + S(v.VariableType));
                foreach (var i in mm.Body.Instructions) Console.WriteLine("   IL_" + i.Offset.ToString("x4") + ": " + i.OpCode + " " + S(i.Operand));
                foreach (var h in mm.Body.ExceptionHandlers)
                    Console.WriteLine("   H " + h.HandlerType + " try " + h.TryStart.Offset.ToString("x4") + ".." + h.TryEnd.Offset.ToString("x4")
                        + " h " + h.HandlerStart.Offset.ToString("x4") + ".." + (h.HandlerEnd == null ? "end" : h.HandlerEnd.Offset.ToString("x4")));
            }
        }
    }
}
