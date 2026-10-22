using System;
using System.Reflection;
using System.Runtime.CompilerServices;

internal static class VerifyJit
{
    private static void Main(string[] args)
    {
        try
        {
            Assembly asm = Assembly.LoadFrom(args[0]);
            Console.WriteLine("loaded: " + asm.FullName);
            Type t = asm.GetType("PCL.PageDownloadInstall", true);
            foreach (string name in new[] { "LoaderInit", "LoadInstanceFinished" })
            {
                MethodInfo m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                if (m == null) { Console.WriteLine(name + ": NOT FOUND"); continue; }
                RuntimeHelpers.PrepareMethod(m.MethodHandle);
                Console.WriteLine(name + ": JIT OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("EX " + ex.GetType().FullName);
            Console.WriteLine("MSG " + ex.Message);
            if (ex.InnerException != null)
            {
                Console.WriteLine("INNER " + ex.InnerException.GetType().FullName);
                Console.WriteLine("INNERMSG " + ex.InnerException.Message);
            }
        }
    }
}
