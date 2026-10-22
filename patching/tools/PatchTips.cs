using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Removes the two modpack-related tooltips that stand out in PCL's otherwise tooltip-free UI:
//  - PageSelectLeft "导入整合包" list item   (.ToolTip = "在当前选择的 Minecraft 文件夹下安装整合包")
//  - PageResource  "安装已有整合包" button    (ToolTip set from BAML -> cleared after InitializeComponent)
internal static class PatchTips
{
    const string Tip = "在当前选择的 Minecraft 文件夹下安装整合包";

    static IEnumerable<TypeDefinition> Walk(IEnumerable<TypeDefinition> ts)
    {
        foreach (var t in ts)
        {
            yield return t;
            foreach (var n in Walk(t.NestedTypes)) yield return n;
        }
    }

    static void Main(string[] a)
    {
        string src = a[0], dst = a[1];
        string exeDir = Path.GetDirectoryName(Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location));
        var res = new ToolResolver(exeDir,
            Path.Combine(Path.GetDirectoryName(exeDir), "extracted"),
            Path.Combine(exeDir, "net40"));
        var m = ModuleDefinition.ReadModule(src, new ReaderParameters { AssemblyResolver = res });

        // --- 1) PageSelectLeft: drop .ToolTip = Tip on the "导入整合包" item ---
        int removed = 0;
        foreach (var t in Walk(m.Types))
        {
            foreach (var mm in t.Methods)
            {
                if (!mm.HasBody) continue;
                var il = mm.Body.Instructions;
                for (int i = 0; i < il.Count; i++)
                {
                    if (il[i].OpCode == OpCodes.Ldstr && (string)il[i].Operand == Tip)
                    {
                        var next = il[i + 1].Operand as MethodReference;
                        if (next != null && next.Name == "set_ToolTip")
                        {
                            il[i].OpCode = OpCodes.Ldnull;
                            il[i].Operand = null;
                            removed++;
                            Console.WriteLine("cleared tooltip in " + t.FullName + "::" + mm.Name + " IL_" + il[i].Offset.ToString("x4"));
                        }
                        else
                        {
                            Console.WriteLine("WARN: tooltip string not followed by set_ToolTip in " + t.FullName + "::" + mm.Name);
                        }
                    }
                }
            }
        }
        if (removed != 1) throw new Exception("expected exactly 1 tooltip assignment, removed " + removed);

        // --- 2) PageResource ctor: BtnSearchInstallModPack.ToolTip = Nothing after InitializeComponent ---
        var page = Walk(m.Types).First(t => t.FullName == "PCL.PageResource");
        var field = page.Fields.FirstOrDefault(f => f.Name == "_BtnSearchInstallModPack" || f.Name == "BtnSearchInstallModPack");
        if (field == null) throw new Exception("BtnSearchInstallModPack field not found");

        MethodReference feSetTag = null;
        foreach (var t in Walk(m.Types))
        {
            foreach (var mm in t.Methods)
            {
                if (!mm.HasBody) continue;
                foreach (var i in mm.Body.Instructions)
                {
                    var mr = i.Operand as MethodReference;
                    if (mr != null && mr.Name == "set_Tag" && mr.DeclaringType.Name == "FrameworkElement" && mr.Parameters.Count == 1)
                    { feSetTag = mr; break; }
                }
                if (feSetTag != null) break;
            }
            if (feSetTag != null) break;
        }
        if (feSetTag == null) throw new Exception("FrameworkElement::set_Tag reference not found");

        var setToolTip = new MethodReference("set_ToolTip", m.TypeSystem.Void, feSetTag.DeclaringType);
        setToolTip.HasThis = true;
        setToolTip.Parameters.Add(new ParameterDefinition(m.TypeSystem.Object));

        var ctor = page.Methods.First(x => x.Name == ".ctor" && x.Parameters.Count == 0);
        var il2 = ctor.Body.Instructions;
        int ic = -1;
        for (int i = 0; i < il2.Count; i++)
        {
            var mr = il2[i].Operand as MethodReference;
            if (il2[i].OpCode == OpCodes.Call && mr != null && mr.Name == "InitializeComponent" && mr.DeclaringType.FullName == "PCL.PageResource") { ic = i; break; }
        }
        if (ic < 0) throw new Exception("InitializeComponent call not found in PageResource ctor");

        var clear = new List<Instruction>
        {
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldfld, field),
            Instruction.Create(OpCodes.Ldnull),
            Instruction.Create(OpCodes.Callvirt, setToolTip),
        };
        for (int i = clear.Count - 1; i >= 0; i--) il2.Insert(ic + 1, clear[i]);
        Console.WriteLine("cleared BtnSearchInstallModPack.ToolTip in PCL.PageResource::.ctor");

        m.Write(dst);
        Console.WriteLine("written " + dst);
    }
}
