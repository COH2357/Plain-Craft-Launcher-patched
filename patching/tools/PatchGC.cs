using System;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class PatchGC
{
    private static void Main(string[] args)
    {
        try { Run(args); } catch (Exception ex) { Console.WriteLine("ERR " + ex.GetType().FullName + ": " + ex.Message); Environment.Exit(3); }
    }

    private static void Run(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var asm = AssemblyDefinition.ReadAssembly(args[0], new ReaderParameters { AssemblyResolver = new ToolResolver(System.IO.Path.GetDirectoryName(args[0]), args[2]) });
        var mod = asm.MainModule;
        TypeDefinition t = mod.GetType("PCL.ModModpack");
        MethodDefinition target = t.Methods.First(m => m.Name == "ModpackInstall" && m.Parameters.Count == 3);

        MethodReference hint = mod.ImportReference(typeof(System.GC).GetMethod("Collect", Type.EmptyTypes));

        ILProcessor il = target.Body.GetILProcessor();
        Instruction a = target.Body.Instructions.First(i =>
            i.OpCode == OpCodes.Ldstr && i.Operand != null && i.Operand.ToString().Contains("为遵守相关法律法规"));
        Instruction b = a.Next;
        Instruction c = b.Next;

        var retLd = Instruction.Create(OpCodes.Ldnull);
        var ret = Instruction.Create(OpCodes.Ret);
        il.Append(retLd);
        il.Append(ret);

        ExceptionHandler outer = target.Body.ExceptionHandlers.First(h => h.HandlerType == ExceptionHandlerType.Finally
            && h.HandlerStart.Offset == 0x2E5);
        outer.HandlerEnd = retLd;

        // keep a as ldstr, then call Hint(msg, 2)
        il.InsertAfter(a, Instruction.Create(OpCodes.Call, hint));
        b.OpCode = OpCodes.Leave;
        b.Operand = retLd;
        c.OpCode = OpCodes.Nop;
        c.Operand = null;

        asm.Write(args[1]);
        Console.WriteLine("written " + args[1]);
    }
}
