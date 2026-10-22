using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class PatchT
{
    private static void Main(string[] args)
    {
        string src = args[0], dst = args[1];
        var dirs = new List<string> { System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(src)), System.IO.Path.GetFullPath("extracted") };
        var resolver = new ToolResolver(dirs.ToArray());
        var asm = AssemblyDefinition.ReadAssembly(src, new ReaderParameters { AssemblyResolver = resolver });
        TypeDefinition t = asm.MainModule.GetType("PCL.ModModpack");
        MethodDefinition target = t.Methods.First(m => m.Name == "ModpackInstall" && m.Parameters.Count == 3);
        TypeReference archiveType = target.Body.Variables[1].VariableType;

        var disp = new MethodDefinition("$PCLPatch$DispatchModpackInstall",
            MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig, target.ReturnType);
        disp.Parameters.Add(new ParameterDefinition("file", ParameterAttributes.None, asm.MainModule.TypeSystem.String));
        disp.Parameters.Add(new ParameterDefinition("archive", ParameterAttributes.None, archiveType));
        disp.Parameters.Add(new ParameterDefinition("baseFolder", ParameterAttributes.None, asm.MainModule.TypeSystem.String));
        disp.Parameters.Add(new ParameterDefinition("instanceName", ParameterAttributes.None, asm.MainModule.TypeSystem.String));
        disp.Parameters.Add(new ParameterDefinition("logo", ParameterAttributes.None, asm.MainModule.TypeSystem.String));
        disp.Parameters.Add(new ParameterDefinition("packType", ParameterAttributes.None, asm.MainModule.TypeSystem.Int32));
        t.Methods.Add(disp);
        var dil = disp.Body.GetILProcessor();
        dil.Append(Instruction.Create(OpCodes.Ldnull));
        dil.Append(Instruction.Create(OpCodes.Ret));

        var test = new MethodDefinition("$PCLPatch$Test", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig, asm.MainModule.TypeSystem.Void);
        t.Methods.Add(test);
        var til = test.Body.GetILProcessor();
        til.Append(Instruction.Create(OpCodes.Ldnull));
        til.Append(Instruction.Create(OpCodes.Ldnull));
        til.Append(Instruction.Create(OpCodes.Ldnull));
        til.Append(Instruction.Create(OpCodes.Ldnull));
        til.Append(Instruction.Create(OpCodes.Ldnull));
        til.Append(Instruction.Create(OpCodes.Ldc_I4_0));
        til.Append(Instruction.Create(OpCodes.Call, disp));
        til.Append(Instruction.Create(OpCodes.Pop));
        til.Append(Instruction.Create(OpCodes.Ret));

        asm.Write(dst);
        Console.WriteLine("written " + dst);
    }
}
