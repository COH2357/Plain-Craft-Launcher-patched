using System;using Mono.Cecil;using System.Linq;
class ResCount{static void Main(string[] a){var asm=AssemblyDefinition.ReadAssembly(a[0],new ReaderParameters{AssemblyResolver=new ToolResolver(System.IO.Path.GetDirectoryName(a[0]),a[1])});Console.WriteLine("resources="+asm.MainModule.Resources.Count);Console.WriteLine("types="+asm.MainModule.Types.Count);Console.WriteLine("asm="+asm.FullName);}}
