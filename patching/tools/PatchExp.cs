using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class PatchExp
{
    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        string src = args[0], dst = args[1];
        var dirs = new List<string> { System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(src)) };
        dirs.Add(System.IO.Path.GetFullPath("extracted"));
        var resolver = new ToolResolver(dirs.ToArray());
        var asm = AssemblyDefinition.ReadAssembly(src, new ReaderParameters { AssemblyResolver = resolver });
        TypeDefinition t = asm.MainModule.GetType("PCL.ModModpack");
        MethodDefinition target = t.Methods.First(m => m.Name == "ModpackInstall" && m.Parameters.Count == 3);
        ILProcessor il = target.Body.GetILProcessor();

        Instruction a = target.Body.Instructions.First(i => i.OpCode == OpCodes.Ldstr && i.Operand != null && i.Operand.ToString().Contains("为遵守相关法律法规"));
        VariableDefinition v4 = target.Body.Variables[4];
        Instruction b = a.Next;
        Instruction c = b.Next;
        if (b.OpCode != OpCodes.Newobj || c.OpCode != OpCodes.Throw) throw new Exception("unexpected");

        var retLd = Instruction.Create(OpCodes.Ldnull);
        var ret = Instruction.Create(OpCodes.Ret);
        il.Append(retLd);
        il.Append(ret);

        var outer = target.Body.ExceptionHandlers.First(h => h.HandlerType == ExceptionHandlerType.Finally && h.HandlerStart.Offset == 0x2E5);
        outer.HandlerEnd = retLd;

        a.OpCode = OpCodes.Ldarg_0;
        a.Operand = null;
        Instruction cur = a;
        foreach (Instruction ins in new[] {
            Instruction.Create(OpCodes.Ldloc_1),
            Instruction.Create(OpCodes.Ldloc_2),
            Instruction.Create(OpCodes.Ldarg_1),
            Instruction.Create(OpCodes.Ldarg_2),
            Instruction.Create(OpCodes.Ldloc, v4),
            Instruction.Create(OpCodes.Pop),
            Instruction.Create(OpCodes.Pop),
            Instruction.Create(OpCodes.Pop),
            Instruction.Create(OpCodes.Pop),
            Instruction.Create(OpCodes.Pop),
            Instruction.Create(OpCodes.Pop),
            Instruction.Create(OpCodes.Ldnull) })
        { il.InsertAfter(cur, ins); cur = ins; }
        foreach (Instruction ins in new[] { Instruction.Create(OpCodes.Nop), Instruction.Create(OpCodes.Nop), Instruction.Create(OpCodes.Nop), Instruction.Create(OpCodes.Nop) }) { il.InsertAfter(cur, ins); cur = ins; }
        b.OpCode = OpCodes.Stloc_0;
        b.Operand = null;
        cur = b;
        for (int i = 0; i < 4; i++) { var n = Instruction.Create(OpCodes.Nop); il.InsertAfter(cur, n); cur = n; }
        c.OpCode = OpCodes.Leave;
        c.Operand = retLd;

        asm.Write(dst);
        Console.WriteLine("written " + dst);
    }
}
