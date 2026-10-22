using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

internal static class CtorJit
{
    static void Main(string[] a)
    {
        var asm = Assembly.Load(File.ReadAllBytes(a[0]));
        var t = asm.GetType(a[1], true);
        var c = t.GetConstructor(Type.EmptyTypes);
        if (c == null) { Console.WriteLine("ctor not found"); return; }
        RuntimeHelpers.PrepareMethod(c.MethodHandle);
        Console.WriteLine("JIT OK: " + c);
    }
}
