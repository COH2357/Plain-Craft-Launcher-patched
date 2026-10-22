using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Resources;

internal static class ExtractRes
{
    private static void Main(string[] args)
    {
        string exe = Path.GetFullPath(args[0]);
        string dir = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(dir);
        Assembly asm = Assembly.Load(File.ReadAllBytes(exe));
        var stream = asm.GetManifestResourceStream("PCL.Resources.resources");
        Console.WriteLine("stream=" + (stream != null));
        if (stream == null) return;
        using (var rr = new ResourceReader(stream))
        {
            foreach (DictionaryEntry e in rr)
            {
                string name = (string)e.Key;
                object val = e.Value;
                Console.WriteLine(name + " : " + (val == null ? "null" : val.GetType().FullName));
                var bytes = val as byte[];
                if (bytes != null)
                {
                    string p = Path.Combine(dir, name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? name : name + ".dll");
                    File.WriteAllBytes(p, bytes);
                }
            }
        }
    }
}
