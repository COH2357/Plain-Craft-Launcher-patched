using System;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DumpMethod
{
    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var asm = AssemblyDefinition.ReadAssembly(args[0]);
        foreach (ModuleDefinition mod in asm.Modules)
        {
            foreach (TypeDefinition t in mod.Types)
            {
                if (t.FullName != args[1]) continue;
                foreach (MethodDefinition m in t.Methods)
                {
                    if (m.Name != args[2] || !m.HasBody) continue;
                    Console.WriteLine(m.FullName + "  IL size=" + m.Body.CodeSize);
                    int limit = args.Length > 3 ? int.Parse(args[3]) : 40;
                    int n = 0;
                    foreach (Instruction i in m.Body.Instructions)
                    {
                        Console.WriteLine("  IL_" + i.Offset.ToString("X4") + ": " + i.OpCode.Name + " " + (i.Operand == null ? "" : i.Operand.ToString()));
                        if (++n >= limit) break;
                    }
                }
            }
        }
    }
}
