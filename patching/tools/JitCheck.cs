using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

internal static class JitCheck
{
    private static void Main(string[] args)
    {
        string file = args[0], typeName = args[1], methodName = args[2];
        int paramCount = args.Length > 3 ? int.Parse(args[3]) : 3;
        Assembly asm = Assembly.LoadFrom(file);
        Console.WriteLine("loaded " + asm.FullName);
        Type t = asm.GetType(typeName, false);
        if (t == null)
        {
            Console.WriteLine("TYPE NOT FOUND");
            return;
        }
        MethodInfo m = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
            .FirstOrDefault(x => x.Name == methodName && x.GetParameters().Length == paramCount);
        if (m == null)
        {
            Console.WriteLine("METHOD NOT FOUND");
            return;
        }
        try
        {
            RuntimeHelpers.PrepareMethod(m.MethodHandle);
            Console.WriteLine("JIT OK: " + m);
        }
        catch (Exception ex)
        {
            Console.WriteLine("JIT FAIL: " + ex.GetType().FullName + ": " + ex.Message);
        }
    }
}
