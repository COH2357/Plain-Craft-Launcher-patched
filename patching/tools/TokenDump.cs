using System;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class TokenDump
{
    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var asm = AssemblyDefinition.ReadAssembly(args[0]);
        foreach (ModuleDefinition mod in asm.Modules)
        {
            foreach (TypeDefinition t in mod.Types)
            {
                if (t.FullName == "PCL.ModDownload")
                {
                    Console.WriteLine("TYPE " + t.FullName + " token=0x" + t.MetadataToken.ToUInt32().ToString("X8"));
                    foreach (FieldDefinition f in t.Fields)
                    {
                        if (f.Name.Contains("DlClientList"))
                            Console.WriteLine("FIELD " + f.Name + " type=" + f.FieldType.FullName + " static=" + f.IsStatic + " token=0x" + f.MetadataToken.ToUInt32().ToString("X8"));
                    }
                }
                if (t.FullName == "PCL.PageDownloadInstall")
                {
                    foreach (MethodDefinition m in t.Methods)
                    {
                        if (m.Name == ".cctor")
                        {
                            Console.WriteLine("CCTOR IL:");
                            int n = 0;
                            foreach (Instruction i in m.Body.Instructions)
                            {
                                Console.WriteLine("  IL_" + i.Offset.ToString("X4") + ": " + i.OpCode.Name + " " + (i.Operand == null ? "" : i.Operand.ToString()));
                                if (++n > 14) break;
                            }
                        }
                        if (m.Name == "LoaderInit")
                            Console.WriteLine("METHOD " + m.FullName + " RVA=0x" + m.RVA.ToString("X") + " token=0x" + m.MetadataToken.ToUInt32().ToString("X8"));
                    }
                }
            }
        }
    }
}
