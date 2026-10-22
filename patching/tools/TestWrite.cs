using System;
using Mono.Cecil;
internal static class TestWrite
{
    private static void Main(string[] args)
    {
        var asm = AssemblyDefinition.ReadAssembly(args[0]);
        Console.WriteLine("read ok, types=" + asm.MainModule.Types.Count);
        asm.Write(args[1]);
        Console.WriteLine("write ok");
    }
}
