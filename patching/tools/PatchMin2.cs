using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class PatchMin2
{
    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        string src = args[0], dst = args[1];
        Console.WriteLine("STEP read");
        var dirs = new List<string> { System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(src)) };
        dirs.Add(args.Length > 2 ? System.IO.Path.GetFullPath(args[2]) : System.IO.Path.GetFullPath("extracted"));
        var resolver = new ToolResolver(dirs.ToArray());
        var asm = AssemblyDefinition.ReadAssembly(src, new ReaderParameters { AssemblyResolver = resolver });
        var mod = asm.MainModule;
        TypeDefinition t = mod.GetType("PCL.ModModpack");
        if (t == null) throw new Exception("type not found");

        Console.WriteLine("STEP find methods");
        MethodDefinition target = t.Methods.First(m => m.Name == "ModpackInstall" && m.Parameters.Count == 3);
        MethodDefinition curse = t.Methods.First(m => m.Name == "InstallPackCurseForge");
        MethodDefinition hmcl = t.Methods.First(m => m.Name == "InstallPackHMCL");
        MethodDefinition mmc = t.Methods.First(m => m.Name == "InstallPackMMC");
        MethodDefinition mcbbs = t.Methods.First(m => m.Name == "InstallPackMCBBS");
        MethodDefinition modrinth = t.Methods.First(m => m.Name == "InstallPackModrinth");
        MethodDefinition launcher = t.Methods.First(m => m.Name == "InstallPackLauncherPack");
        MethodDefinition compress = t.Methods.First(m => m.Name == "InstallPackCompress");

        TypeReference archiveType = target.Body.Variables[1].VariableType;
        TypeReference stringType = mod.TypeSystem.String;
        TypeReference intType = mod.TypeSystem.Int32;

        // ---- new dispatcher method ----
        var disp = new MethodDefinition("$PCLPatch$DispatchModpackInstall",
            MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig,
            target.ReturnType);
        disp.Parameters.Add(new ParameterDefinition("file", ParameterAttributes.None, stringType));
        disp.Parameters.Add(new ParameterDefinition("archive", ParameterAttributes.None, archiveType));
        disp.Parameters.Add(new ParameterDefinition("baseFolder", ParameterAttributes.None, stringType));
        disp.Parameters.Add(new ParameterDefinition("instanceName", ParameterAttributes.None, stringType));
        disp.Parameters.Add(new ParameterDefinition("logo", ParameterAttributes.None, stringType));
        disp.Parameters.Add(new ParameterDefinition("packType", ParameterAttributes.None, intType));
        Console.WriteLine("STEP add method");
        t.Methods.Add(disp);
        disp.Body.InitLocals = false;
        ILProcessor dil = disp.Body.GetILProcessor();

        var L0 = Instruction.Create(OpCodes.Nop);
        var L1 = Instruction.Create(OpCodes.Nop);
        var L2 = Instruction.Create(OpCodes.Nop);
        var L3 = Instruction.Create(OpCodes.Nop);
        var L4 = Instruction.Create(OpCodes.Nop);
        var L9 = Instruction.Create(OpCodes.Nop);
        var LDEF = Instruction.Create(OpCodes.Nop);

        dil.Append(Instruction.Create(OpCodes.Ldarg, disp.Parameters[5]));
        dil.Append(Instruction.Create(OpCodes.Switch, new[] { L0, L1, L2, L3, L4, LDEF, LDEF, LDEF, LDEF, L9 }));
        dil.Append(Instruction.Create(OpCodes.Br, LDEF));

        // case 0: CurseForge(file, archive, baseFolder, instance, logo)
        dil.Append(L0);
        dil.Append(Instruction.Create(OpCodes.Ldarg_0));
        dil.Append(Instruction.Create(OpCodes.Ldarg_1));
        dil.Append(Instruction.Create(OpCodes.Ldarg_2));
        dil.Append(Instruction.Create(OpCodes.Ldarg_3));
        dil.Append(Instruction.Create(OpCodes.Ldarg, disp.Parameters[4]));
        dil.Append(Instruction.Create(OpCodes.Call, curse));
        dil.Append(Instruction.Create(OpCodes.Ret));

        // case 1: HMCL(file, archive, baseFolder)
        dil.Append(L1);
        dil.Append(Instruction.Create(OpCodes.Ldarg_0));
        dil.Append(Instruction.Create(OpCodes.Ldarg_1));
        dil.Append(Instruction.Create(OpCodes.Ldarg_2));
        dil.Append(Instruction.Create(OpCodes.Call, hmcl));
        dil.Append(Instruction.Create(OpCodes.Ret));

        // case 2: MMC
        dil.Append(L2);
        dil.Append(Instruction.Create(OpCodes.Ldarg_0));
        dil.Append(Instruction.Create(OpCodes.Ldarg_1));
        dil.Append(Instruction.Create(OpCodes.Ldarg_2));
        dil.Append(Instruction.Create(OpCodes.Call, mmc));
        dil.Append(Instruction.Create(OpCodes.Ret));

        // case 3: MCBBS(file, archive, baseFolder, instance)
        dil.Append(L3);
        dil.Append(Instruction.Create(OpCodes.Ldarg_0));
        dil.Append(Instruction.Create(OpCodes.Ldarg_1));
        dil.Append(Instruction.Create(OpCodes.Ldarg_2));
        dil.Append(Instruction.Create(OpCodes.Ldarg_3));
        dil.Append(Instruction.Create(OpCodes.Call, mcbbs));
        dil.Append(Instruction.Create(OpCodes.Ret));

        // case 4: Modrinth(file, archive, baseFolder, instance, logo)
        dil.Append(L4);
        dil.Append(Instruction.Create(OpCodes.Ldarg_0));
        dil.Append(Instruction.Create(OpCodes.Ldarg_1));
        dil.Append(Instruction.Create(OpCodes.Ldarg_2));
        dil.Append(Instruction.Create(OpCodes.Ldarg_3));
        dil.Append(Instruction.Create(OpCodes.Ldarg, disp.Parameters[4]));
        dil.Append(Instruction.Create(OpCodes.Call, modrinth));
        dil.Append(Instruction.Create(OpCodes.Ret));

        // case 9: LauncherPack
        dil.Append(L9);
        dil.Append(Instruction.Create(OpCodes.Ldarg_0));
        dil.Append(Instruction.Create(OpCodes.Ldarg_1));
        dil.Append(Instruction.Create(OpCodes.Ldarg_2));
        dil.Append(Instruction.Create(OpCodes.Call, launcher));
        dil.Append(Instruction.Create(OpCodes.Ret));

        // default: Compress
        dil.Append(LDEF);
        dil.Append(Instruction.Create(OpCodes.Ldarg_0));
        dil.Append(Instruction.Create(OpCodes.Ldarg_1));
        dil.Append(Instruction.Create(OpCodes.Call, compress));
        dil.Append(Instruction.Create(OpCodes.Ret));

        if (Environment.GetEnvironmentVariable("PCLMIN") == "1")
        {
            disp.Body.Instructions.Clear();
            disp.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            disp.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        // ---- rewrite ModpackInstall(File,Instance,Logo): replace erased 'throw' with dispatch ----
        Console.WriteLine("STEP rewrite target");
        ILProcessor il = target.Body.GetILProcessor();
        Instruction a = target.Body.Instructions.First(i =>
            i.OpCode == OpCodes.Ldstr && i.Operand != null && i.Operand.ToString().Contains("为遵守相关法律法规"));
        Instruction b = a.Next;
        Instruction c = b.Next;
        if (b.OpCode != OpCodes.Newobj || c.OpCode != OpCodes.Throw) throw new Exception("unexpected throw site");

        var retLdloc = Instruction.Create(OpCodes.Ldloc_0);
        var ret = Instruction.Create(OpCodes.Ret);
        il.Append(retLdloc);
        il.Append(ret);

        // leave target must be outside handlers
        ExceptionHandler outer = target.Body.ExceptionHandlers.First(h => h.HandlerType == ExceptionHandlerType.Finally
            && h.HandlerStart.Offset == 0x2E5);
        outer.HandlerEnd = retLdloc;

        a.OpCode = OpCodes.Ldarg_0;
        a.Operand = null;

        Instruction cur = a;
        foreach (Instruction ins in new[]
        {
            Instruction.Create(OpCodes.Ldloc_1),
            Instruction.Create(OpCodes.Ldloc_2),
            Instruction.Create(OpCodes.Ldarg_1),
            Instruction.Create(OpCodes.Ldarg_2),
            Instruction.Create(OpCodes.Ldloc, target.Body.Variables[4]),
            Instruction.Create(OpCodes.Call, disp),
            Instruction.Create(OpCodes.Stloc_0)
        })
        {
            il.InsertAfter(cur, ins);
            cur = ins;
        }
        b.OpCode = OpCodes.Leave;
        b.Operand = retLdloc;
        c.OpCode = OpCodes.Nop;
        c.Operand = null;

        Console.WriteLine("STEP write");
        asm.Write(dst);
        Console.WriteLine("written " + dst);
    }
}
