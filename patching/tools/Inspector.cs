using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class Inspector
{
    private const string Msg = "为遵守相关法律法规";

    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        string path = args[0];
        var asm = AssemblyDefinition.ReadAssembly(path);
        Console.WriteLine("Assembly: " + asm.Name.FullName);
        Console.WriteLine("PublicKey: " + (asm.Name.PublicKey.Length == 0 ? "<none>" : asm.Name.PublicKey.Length + " bytes"));

        List<string> filters = new List<string>();
        for (int i = 1; i < args.Length; i++) filters.Add(args[i]);

        foreach (ModuleDefinition mod in asm.Modules)
        {
            foreach (TypeDefinition type in mod.Types)
            {
                ScanType(type, filters);
            }
        }
    }

    private static void ScanType(TypeDefinition type, List<string> filters)
    {
        foreach (MethodDefinition m in type.Methods)
        {
            if (!m.HasBody) continue;
            bool hit = false;
            foreach (Instruction ins in m.Body.Instructions)
            {
                if (ins.OpCode == OpCodes.Ldstr && ins.Operand is string)
                {
                    if (((string)ins.Operand).Contains(Msg)) { hit = true; break; }
                }
            }
            bool named = false;
            foreach (string f in filters)
            {
                if (m.FullName.Contains(f)) { named = true; break; }
            }
            if (hit || named)
            {
                Console.WriteLine();
                Console.WriteLine("==== " + m.FullName);
                Console.WriteLine("RVA=" + m.RVA + "  InitLocals=" + m.Body.InitLocals + "  MaxStack=" + m.Body.MaxStackSize + "  Locals=" + m.Body.Variables.Count);
                foreach (Instruction ins in m.Body.Instructions)
                {
                    string operand = "";
                    if (ins.Operand != null)
                    {
                        if (ins.Operand is string) operand = "\"" + ins.Operand + "\"";
                        else operand = ins.Operand.ToString();
                    }
                    Console.WriteLine(string.Format("  IL_{0:X4}: {1,-12} {2}", ins.Offset, ins.OpCode.Name, operand));
                }
            }
        }
        foreach (TypeDefinition nested in type.NestedTypes) ScanType(nested, filters);
    }
}
