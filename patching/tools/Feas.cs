using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class Feas
{
    static string S(object o)
    {
        if (o == null) return "null";
        try { return o.ToString(); } catch { return "<" + o.GetType().Name + ">"; }
    }

    static void Main(string[] a)
    {
        string exe = a[0];
        string typeName = a.Length > 1 ? a[1] : "PCL.PageInstanceOverall";
        string methName = a.Length > 2 ? a[2] : "ComboDisplayLogo_SelectionChanged";
        var res = new ToolResolver(Path.GetDirectoryName(Path.GetFullPath(exe)), "extracted", "tools/net40");
        var m = ModuleDefinition.ReadModule(exe, new ReaderParameters { AssemblyResolver = res });

        Console.WriteLine("MODULE " + m.Name + " asm=" + m.Assembly.Name.FullName);
        foreach (var t in m.Types)
        {
            foreach (var mm in t.Methods)
            {
                if (mm.Name == "SelectFile") Console.WriteLine("DEF " + mm.FullName + " params=" + mm.Parameters.Count);
                if (mm.HasBody)
                {
                    foreach (var i in mm.Body.Instructions)
                    {
                        var mr = i.Operand as MethodReference;
                        if (mr != null && mr.Name == "SelectFile")
                            Console.WriteLine("CALLER " + mm.DeclaringType.FullName + "::" + mm.Name + " IL_" + i.Offset.ToString("x4"));
                    }
                }
                if ((mm.Name == "RunInThread" && t.Name == "ModBase") || (t.Name == "Logger" && mm.Name == "Error") ||
                    mm.Name == "ThrowIfCanceled" || (t.Name == "ModModpack" && mm.Name == "ModpackInstall"))
                    Console.WriteLine("SIG " + mm.FullName + " params=" + mm.Parameters.Count + (mm.IsStatic ? " static" : ""));
            }
        }

        foreach (var t in m.Types)
        {
            if (t.Name != typeName && t.FullName != typeName) continue;
            foreach (var mm in t.Methods)
            {
                if (mm.Name != methName && !mm.FullName.EndsWith("::" + methName, StringComparison.Ordinal)) continue;
                if (!mm.HasBody) continue;
                Console.WriteLine("== " + mm.FullName + " code=" + mm.Body.CodeSize + " max=" + mm.Body.MaxStackSize + " vars=" + mm.Body.Variables.Count);
                foreach (var v in mm.Body.Variables) Console.WriteLine("   V_" + v.Index + " " + S(v.VariableType));
                foreach (var i in mm.Body.Instructions) Console.WriteLine("   IL_" + i.Offset.ToString("x4") + ": " + i.OpCode + " " + S(i.Operand));
                foreach (var h in mm.Body.ExceptionHandlers) Console.WriteLine("   H " + h.HandlerType + " try " + h.TryStart.Offset.ToString("x4") + ".." + h.TryEnd.Offset.ToString("x4") + " h " + h.HandlerStart.Offset.ToString("x4") + ".." + (h.HandlerEnd == null ? "end" : h.HandlerEnd.Offset.ToString("x4")));
            }
        }
    }
}
