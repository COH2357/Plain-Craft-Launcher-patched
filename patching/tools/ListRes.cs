using System;
using Mono.Cecil;
internal static class ListRes
{
    private static void Main(string[] args)
    {
        var asm = AssemblyDefinition.ReadAssembly(args[0]);
        foreach (var r in asm.MainModule.Resources)
            Console.WriteLine(r.ResourceType + " " + r.Name + " (" + r.GetType().Name + ")");
        foreach (var a in asm.MainModule.AssemblyReferences)
            Console.WriteLine("REF " + a.FullName);
    }
}
