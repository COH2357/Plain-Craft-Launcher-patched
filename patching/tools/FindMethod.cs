using System;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class FindMethod
{
    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var asm = AssemblyDefinition.ReadAssembly(args[0]);
        string needle = args[1];
        int dump = args.Length > 2 ? int.Parse(args[2]) : 0;
        foreach (ModuleDefinition mod in asm.Modules)
        {
            foreach (TypeDefinition t in mod.Types)
            {
                foreach (MethodDefinition m in t.Methods)
                {
                    if (!m.Name.Contains(needle) || !m.HasBody) continue;
                    Console.WriteLine("=== " + t.FullName + "::" + m.Name + "  RVA=0x" + m.RVA.ToString("X") + " IL=" + m.Body.CodeSize);
                    if (dump > 0)
                    {
                        int n = 0;
                        foreach (Instruction i in m.Body.Instructions)
                        {
                            Console.WriteLine("  IL_" + i.Offset.ToString("X4") + ": " + i.OpCode.Name + " " + (i.Operand == null ? "" : i.Operand.ToString()));
                            if (++n >= dump) break;
                        }
                    }
                }
            }
        }
    }
}
