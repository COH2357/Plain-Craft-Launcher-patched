using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

internal static class Smoke
{
    private static string dir;
    private static Assembly app;

    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        dir = Path.GetDirectoryName(Path.GetFullPath(args[0]));
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            string n = new AssemblyName(e.Name).Name;
            foreach (string d in new[] { dir, Path.Combine(dir, "extracted") })
            {
                string p = Path.Combine(d, n + ".dll");
                if (File.Exists(p)) return Assembly.Load(File.ReadAllBytes(p));
            }
            return null;
        };
        app = Assembly.Load(File.ReadAllBytes(args[0]));
        Type t = app.GetType("PCL.ModModpack", true);
        MethodInfo m = t.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(x => x.Name == "ModpackInstall" && x.GetParameters().Length == 3);
        foreach (string file in args.Skip(1))
        {
            Console.WriteLine("--- invoke with: " + file);
            try
            {
                object r = m.Invoke(null, new object[] { file, null, null });
                Console.WriteLine("RETURNED: " + (r == null ? "null" : r.GetType().FullName));
            }
            catch (TargetInvocationException ex)
            {
                for (Exception ie = ex.InnerException; ie != null; ie = ie.InnerException) Console.WriteLine("THREW: " + ie.GetType().FullName + " | " + ie.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("OUTER: " + ex.GetType().FullName + " | " + ex.Message);
            }
        }
    }
}
