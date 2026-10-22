using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class HandlerDump
{
    private static void Main(string[] args)
    {
        var resolver = new ToolResolver(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(args[0])));
        var asm = AssemblyDefinition.ReadAssembly(args[0], new ReaderParameters { AssemblyResolver = resolver });
        TypeDefinition t = asm.MainModule.GetType(args[1]);
        MethodDefinition m = t.Methods.First(x => x.Name == args[2] && x.Parameters.Count == int.Parse(args[3]));
        Console.WriteLine("IL=" + m.Body.Instructions.Count + " size=" + m.Body.Instructions.Sum(i => i.GetSize()) + " maxstack=" + m.Body.MaxStackSize + " initlocals=" + m.Body.InitLocals);
        foreach (var h in m.Body.ExceptionHandlers)
        {
            Console.WriteLine(h.HandlerType + " try=[" + Off(h.TryStart) + ".." + Off(h.TryEnd) + ") handler=[" + Off(h.HandlerStart) + ".." + Off(h.HandlerEnd) + ")");
        }
        foreach (var i in m.Body.Instructions)
            Console.WriteLine("  IL_" + i.Offset.ToString("X4") + " " + i.OpCode.Name + " " + (i.Operand == null ? "" : i.Operand.ToString()));
    }

    private static string Off(Instruction i)
    {
        return i == null ? "(end)" : "IL_" + i.Offset.ToString("X4");
    }
}
