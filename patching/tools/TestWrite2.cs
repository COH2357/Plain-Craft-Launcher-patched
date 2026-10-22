using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
internal static class TestWrite2
{
    private static void Main(string[] args)
    {
        var dirs = new List<string> { Path.GetDirectoryName(Path.GetFullPath(args[0])) };
        if (args.Length > 2) dirs.Add(Path.GetFullPath(args[2]));
        var resolver = new ToolResolver(dirs.ToArray());
        var asm = AssemblyDefinition.ReadAssembly(args[0], new ReaderParameters { AssemblyResolver = resolver });
        Console.WriteLine("read ok");
        asm.Write(args[1]);
        Console.WriteLine("write ok");
    }
}
