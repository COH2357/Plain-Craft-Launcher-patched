using System;using Mono.Cecil;using System.Linq;
class ResList{static void Main(string[] a){var asm=AssemblyDefinition.ReadAssembly(a[0],new ReaderParameters{AssemblyResolver=new ToolResolver(System.IO.Path.GetDirectoryName(a[0]),a[1])});foreach(var r in asm.MainModule.Resources)Console.WriteLine(r.Name+" "+r.ResourceType);Console.WriteLine("total="+asm.MainModule.Resources.Count);}}
