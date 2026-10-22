using System;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DumpFull
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
                    Console.WriteLine("=== " + m.FullName + "  IL=" + m.Body.CodeSize + "  InitLocals=" + m.Body.InitLocals);
                    Console.WriteLine("LOCALS:");
                    foreach (VariableDefinition v in m.Body.Variables)
                        Console.WriteLine("  V_" + v.Index + " : " + v.VariableType.FullName);
                    Console.WriteLine("HANDLERS:");
                    foreach (ExceptionHandler h in m.Body.ExceptionHandlers)
                        Console.WriteLine("  " + h.HandlerType + " try=[IL_" + h.TryStart.Offset.ToString("X4") + "..IL_" + h.TryEnd.Offset.ToString("X4") + ") handler=IL_" + h.HandlerStart.Offset.ToString("X4") + " filter=IL_" + (h.FilterStart == null ? "-" : h.FilterStart.Offset.ToString("X4")));
                    Console.WriteLine("IL:");
                    foreach (Instruction i in m.Body.Instructions)
                        Console.WriteLine("  IL_" + i.Offset.ToString("X4") + ": " + i.OpCode.Name + " " + (i.Operand == null ? "" : i.Operand.ToString()));
                }
            }
        }
    }
}
