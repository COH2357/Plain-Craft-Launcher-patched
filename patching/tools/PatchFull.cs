using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Reconstructs the parts of PCL 2.99.0.0 that the RestrictMcDownload=True build erased:
//  1) ModpackInstall() (no-arg: file dialog + background install) -> rebuilt from source logic
//  2) resource-page install button (Install_Click)                -> rebuild ToListItem call
internal static class PatchFull
{
    static ModuleDefinition M;
    static List<MethodReference> Refs;

    static void Main(string[] a)
    {
        string src = a[0], dst = a[1];
        string exeDir = Path.GetDirectoryName(Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location));
        var res = new ToolResolver(exeDir,
            Path.Combine(Path.GetDirectoryName(exeDir), "extracted"),
            Path.Combine(exeDir, "net40"));
        M = ModuleDefinition.ReadModule(src, new ReaderParameters { AssemblyResolver = res });
        Refs = new List<MethodReference>();
        Collect(M.Types);

        PatchDialog();
        PatchCompDetail();

        M.Write(dst);
        Console.WriteLine("written " + dst);
    }

    static void Collect(IEnumerable<TypeDefinition> types)
    {
        foreach (var t in types)
        {
            foreach (var m in t.Methods)
            {
                if (!m.HasBody) continue;
                foreach (var i in m.Body.Instructions)
                {
                    var mr = i.Operand as MethodReference;
                    if (mr != null) Refs.Add(mr);
                }
            }
            Collect(t.NestedTypes);
        }
    }

    static MethodReference Ref(Func<MethodReference, bool> p, string what)
    {
        var r = Refs.FirstOrDefault(p);
        if (r == null)
        {
            Console.WriteLine("-- candidates for " + what + " (name filter):");
            foreach (var c in Refs.Where(x => x.Name.Contains("Implicit") || x.Name == "SelectFile").Distinct())
                Console.WriteLine("   " + c.FullName + " | decl=" + c.DeclaringType.FullName + " declType=" + c.DeclaringType.GetType().Name);
            throw new Exception("reference not found: " + what);
        }
        return r;
    }

    static TypeDefinition Type(string fullName)
    {
        return Walk(M.Types).FirstOrDefault(t => t.FullName == fullName);
    }

    static IEnumerable<TypeDefinition> Walk(IEnumerable<TypeDefinition> ts)
    {
        foreach (var t in ts)
        {
            yield return t;
            foreach (var n in Walk(t.NestedTypes)) yield return n;
        }
    }

    static bool IsGen(TypeReference t, string elem)
    {
        var g = t as GenericInstanceType;
        return g != null && g.ElementType.Name == elem;
    }

    static void PatchDialog()
    {
        var exceptionType = M.ImportReference(typeof(System.Exception));
        MethodDefinition target = Type("PCL.ModModpack").Methods.First(m => m.Name == "ModpackInstall" && m.Parameters.Count == 0);
        MethodDefinition install3 = Type("PCL.ModModpack").Methods.First(m => m.Name == "ModpackInstall" && m.Parameters.Count == 3);

        var selectFile = Ref(r => r.Name == "SelectFile" && r.DeclaringType.Name == "Dialogs" && r.Parameters.Count == 4, "Dialogs.SelectFile");
        var oneOf = Ref(r => r.Name == "op_Implicit" && r.Parameters.Count == 1
            && r.DeclaringType is GenericInstanceType
            && ((GenericInstanceType)r.DeclaringType).ElementType.Name == "OneOf`2"
            && IsGen(((GenericInstanceType)r.DeclaringType).GenericArguments[0], "IEnumerable`1")
            && IsGen(((GenericInstanceType)((GenericInstanceType)r.DeclaringType).GenericArguments[0]).GenericArguments[0], "ValueTuple`2")
            && r.Parameters[0].ParameterType is GenericParameter
            && ((GenericParameter)r.Parameters[0].ParameterType).Position == 0, "OneOf.op_Implicit(tuple filter)");
        var nullableCtor = Ref(r => r.Name == ".ctor" && r.Parameters.Count == 1
            && r.DeclaringType is GenericInstanceType
            && ((GenericInstanceType)r.DeclaringType).ElementType.Name == "Nullable`1"
            && IsGen(((GenericInstanceType)r.DeclaringType).GenericArguments[0], "OneOf`2"), "Nullable<OneOf>.ctor");
        var tupleCtor = Ref(r => r.Name == ".ctor" && r.Parameters.Count == 2
            && r.DeclaringType is GenericInstanceType
            && ((GenericInstanceType)r.DeclaringType).ElementType.Name == "ValueTuple`2"
            && ((GenericInstanceType)r.DeclaringType).GenericArguments[0] is ArrayType
            && ((ArrayType)((GenericInstanceType)r.DeclaringType).GenericArguments[0]).ElementType.FullName == "System.String"
            && ((GenericInstanceType)r.DeclaringType).GenericArguments[1].FullName == "System.String", "ValueTuple<string[],string>.ctor");
        var firstOrDefault = Ref(r => r.Name == "FirstOrDefault" && r.DeclaringType.Name == "Enumerable" && r.Parameters.Count == 1
            && (r is GenericInstanceMethod || r.GenericParameters.Count == 1), "Enumerable.FirstOrDefault<T>");
        var isNullOrEmpty = Ref(r => r.Name == "IsNullOrEmpty" && r.DeclaringType.FullName == "System.String", "String.IsNullOrEmpty");
        var actionCtor = Ref(r => r.Name == ".ctor" && r.DeclaringType.FullName == "System.Action" && r.Parameters.Count == 2, "Action.ctor");
        var runInThread = Ref(r => r.Name == "RunInThread" && r.Parameters.Count == 1 && r.Parameters[0].ParameterType.FullName == "System.Action", "ModBase.RunInThread");
        var throwIfCanceled = Ref(r => r.Name == "ThrowIfCanceled" && r.Parameters.Count == 1, "ThrowIfCanceled");
        var loggerError = Ref(r => r.DeclaringType.Name == "Logger" && r.Name == "Error" && r.Parameters.Count == 4, "Logger.Error");

        var tupleType = tupleCtor.DeclaringType;                          // ValueTuple<string[],string>
        var stringArray = ((GenericInstanceType)tupleType).GenericArguments[0]; // string[]
        var nullableType = nullableCtor.DeclaringType;                    // Nullable<OneOf<...>>
        var oneOfType = ((GenericInstanceType)nullableType).GenericArguments[0]; // OneOf<...>
        var oneOfEnumerable = ((GenericInstanceType)oneOf.DeclaringType).GenericArguments[0]; // IEnumerable<ValueTuple<...>>
        var actionType = runInThread.Parameters[0].ParameterType;

        // --- field carrying the chosen path ---
        var field = new FieldDefinition("$PCLPatch$PendingModpack", FieldAttributes.Public | FieldAttributes.Static, M.TypeSystem.String);
        Type("PCL.ModModpack").Fields.Add(field);

        // --- worker method: ModpackInstall(file, Nothing, Nothing) inside try/catch ---
        var worker = new MethodDefinition("$PCLPatch$InstallPendingModpack", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig, M.TypeSystem.Void);
        Type("PCL.ModModpack").Methods.Add(worker);
        var wb = new MethodBody(worker);
        worker.Body = wb;
        wb.InitLocals = true;
        wb.MaxStackSize = 4;
        var exVar = new VariableDefinition(exceptionType);
        wb.Variables.Add(exVar);

        var wRet = Instruction.Create(OpCodes.Ret);
        var wTry = Instruction.Create(OpCodes.Ldsfld, field);
        var wCatch = Instruction.Create(OpCodes.Stloc, exVar);
        wb.Instructions.Add(wTry);
        wb.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
        wb.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
        wb.Instructions.Add(Instruction.Create(OpCodes.Call, install3));
        wb.Instructions.Add(Instruction.Create(OpCodes.Pop));
        wb.Instructions.Add(Instruction.Create(OpCodes.Leave, wRet));
        wb.Instructions.Add(wCatch);
        wb.Instructions.Add(Instruction.Create(OpCodes.Ldloc, exVar));
        wb.Instructions.Add(Instruction.Create(OpCodes.Call, throwIfCanceled));
        wb.Instructions.Add(Instruction.Create(OpCodes.Ldloc, exVar));
        wb.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "手动安装整合包失败"));
        wb.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_3)); // LogBehavior.Alert
        wb.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "ModModpack.vb"));
        wb.Instructions.Add(Instruction.Create(OpCodes.Call, loggerError));
        wb.Instructions.Add(Instruction.Create(OpCodes.Leave, wRet));
        wb.Instructions.Add(wRet);
        wb.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
        {
            CatchType = exceptionType,
            TryStart = wTry,
            TryEnd = wCatch,
            HandlerStart = wCatch,
            HandlerEnd = wRet
        });

        // --- new ModpackInstall() body ---
        var body = new MethodBody(target);
        target.Body = body;
        body.InitLocals = true;
        body.MaxStackSize = 8;
        var fileVar = new VariableDefinition(M.TypeSystem.String);
        body.Variables.Add(fileVar);

        var ret = Instruction.Create(OpCodes.Ret);
        var l = new List<Instruction>();
        l.Add(Instruction.Create(OpCodes.Ldstr, "选择整合包压缩文件"));
        l.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        l.Add(Instruction.Create(OpCodes.Ldnull));
        l.Add(Instruction.Create(OpCodes.Ldc_I4_1));
        l.Add(Instruction.Create(OpCodes.Newarr, tupleType));
        l.Add(Instruction.Create(OpCodes.Dup));
        l.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        l.Add(Instruction.Create(OpCodes.Ldc_I4_3));
        l.Add(Instruction.Create(OpCodes.Newarr, M.TypeSystem.String));
        int n = 0;
        foreach (var ext in new[] { "rar", "zip", "mrpack" })
        {
            l.Add(Instruction.Create(OpCodes.Dup));
            l.Add(Instruction.Create(OpCodes.Ldc_I4, n++));
            l.Add(Instruction.Create(OpCodes.Ldstr, ext));
            l.Add(Instruction.Create(OpCodes.Stelem_Ref));
        }
        l.Add(Instruction.Create(OpCodes.Ldstr, "整合包文件"));
        l.Add(Instruction.Create(OpCodes.Newobj, tupleCtor));
        l.Add(Instruction.Create(OpCodes.Stelem_Any, tupleType));
        l.Add(Instruction.Create(OpCodes.Call, oneOf));
        l.Add(Instruction.Create(OpCodes.Newobj, nullableCtor));
        l.Add(Instruction.Create(OpCodes.Call, selectFile));
        l.Add(Instruction.Create(OpCodes.Call, firstOrDefault));
        l.Add(Instruction.Create(OpCodes.Stloc, fileVar));
        l.Add(Instruction.Create(OpCodes.Ldloc, fileVar));
        l.Add(Instruction.Create(OpCodes.Call, isNullOrEmpty));
        l.Add(Instruction.Create(OpCodes.Brtrue, ret));
        l.Add(Instruction.Create(OpCodes.Ldloc, fileVar));
        l.Add(Instruction.Create(OpCodes.Stsfld, field));
        l.Add(Instruction.Create(OpCodes.Ldnull));
        l.Add(Instruction.Create(OpCodes.Ldftn, worker));
        l.Add(Instruction.Create(OpCodes.Newobj, actionCtor));
        l.Add(Instruction.Create(OpCodes.Call, runInThread));
        l.Add(ret);
        foreach (var i in l) body.Instructions.Add(i);
        Console.WriteLine("patched ModpackInstall(): " + body.Instructions.Count + " instrs, refs ok (" + oneOfEnumerable.Name + ", " + oneOfType.Name + ", " + nullableType.Name + ")");
    }

    static void PatchCompDetail()
    {
        MethodDefinition init = Type("PCL.PageDownloadResourceDetail").Methods.First(m => m.Name == "InitializeStackResourceVersions");
        MethodDefinition installClick = Type("PCL.PageDownloadResourceDetail").Methods.First(m => m.Name == "Install_Click");
        MethodDefinition saveClick = Type("PCL.PageDownloadResourceDetail").Methods.First(m => m.Name == "Save_Click");

        var il = init.Body.Instructions;
        int k = -1;
        for (int i = 0; i < il.Count; i++)
        {
            var mr = il[i].Operand as MethodReference;
            if (il[i].OpCode == OpCodes.Ldftn && mr != null && mr.Name == "Save_Click") { k = i; break; }
        }
        if (k < 0) throw new Exception("Save_Click ldftn not found");
        if (il[k + 1].OpCode != OpCodes.Newobj || il[k + 2].OpCode != OpCodes.Ldnull) throw new Exception("unexpected sequence after ldftn");
        var listCtor = il[k + 1].Operand as MethodReference;
        var saveDef = il[k].Operand as MethodReference;

        var iconHandler = Walk(new[] { Type("PCL.MyIconButton") }).FirstOrDefault(t => t.Name == "ClickEventHandler");
        if (iconHandler == null) throw new Exception("MyIconButton.ClickEventHandler not found");
        var iconCtor = iconHandler.Methods.First(m => m.Name == ".ctor");
        var clickCtor = il[k + 1].Operand as MethodReference;
        var saveRef = il[k].Operand as MethodReference;
        var resourceField = Type("PCL.PageDownloadResourceDetail").Fields.First(f => f.Name == "TargetResourceType");

        // Original (erased) shape:
        //   If TargetResourceType = ResourceTypes.ModPack Then
        //       Version.ToListItem(AddressOf Install_Click, AddressOf Save_Click, IsBadDisplay)
        //   Else
        //       Version.ToListItem(AddressOf Save_Click, Nothing, IsBadDisplay)
        var after = il[k + 3];                                  // ldloc.1 (IsBadDisplay)
        var lSave = Instruction.Create(OpCodes.Ldarg_0);
        var lCall = after;
        var brCall = Instruction.Create(OpCodes.Br, lCall);
        var seq = new List<Instruction>
        {
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldfld, resourceField),
            Instruction.Create(OpCodes.Ldc_I4_2),              // ResourceTypes.ModPack
            Instruction.Create(OpCodes.Bne_Un, lSave),
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldftn, installClick),
            Instruction.Create(OpCodes.Newobj, clickCtor),
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldftn, saveRef),
            Instruction.Create(OpCodes.Newobj, iconCtor),
            brCall,
            lSave,
            Instruction.Create(OpCodes.Ldftn, saveRef),
            Instruction.Create(OpCodes.Newobj, clickCtor),
            Instruction.Create(OpCodes.Ldnull),
        };
        for (int i = 0; i < 4; i++) il.RemoveAt(k - 1);          // ldarg.0, ldftn, newobj, ldnull
        for (int i = seq.Count - 1; i >= 0; i--) il.Insert(k - 1, seq[i]);
        Console.WriteLine("patched InitializeStackResourceVersions: conditional install button for ModPack rows (" + iconCtor.FullName + ")");
    }
}
