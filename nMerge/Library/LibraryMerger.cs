using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Omega.App.nMerge.Library
	{
	using System.IO;
	using System.IO.Compression;
	using System.Reflection;
	using Mono.Cecil;
	using Mono.Cecil.Cil;
	using MethodAttributes = Mono.Cecil.MethodAttributes;

	internal class LibraryMerger : Merger
		{
		public LibraryMerger(CommandLine cl) : base(cl) {}

		public void Merge()
			{
			foreach (var library in ClParams.Libraries)
				{

				if (ClParams.Compress)
					{
					String compressedLib = Path.GetFileName(library) + ".bz2";
					CopyFile(library, compressedLib, true);
					AssemblyDef.MainModule.Resources.Add(new EmbeddedResource(Path.GetFileName(compressedLib), ManifestResourceAttributes.Public, File.ReadAllBytes(compressedLib)));
					}
				else
					AssemblyDef.MainModule.Resources.Add(new EmbeddedResource(Path.GetFileName(library) + ".bz2", ManifestResourceAttributes.Public, File.ReadAllBytes(library)));
				}


			//var unusedMethod = FindMethod("MainLibrary.DerivedClass::UnusedMethod", AssemblyDef);
			//var voidRef = AssemblyDef.MainModule.Import(unusedMethod.ReturnType);
			const MethodAttributes attributes = MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
			var cctor = new MethodDefinition(".cctor", attributes, ImportType(AssemblyDef, typeof(void)));

			List<MethodDefinition> resolveMethod = ClParams.Compress ? CreateResolveCompressedMethod(AssemblyDef) : CreateResolveMinimalMethod(AssemblyDef);


			ILProcessor il = cctor.Body.GetILProcessor();


			//il.Append(il.Create(OpCodes.Call, unusedMethod));
			il.Emit(OpCodes.Ldstr, "Initializing Assembly Resolver");
			il.Emit(OpCodes.Call, ImportMethod(AssemblyDef, typeof(Console), "WriteLine", typeof(String)));

			il.Append(il.Create(OpCodes.Call, ImportMethod<AppDomain>(AssemblyDef, "get_CurrentDomain")));
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ldftn, resolveMethod.First(m => m.Name == "OnAssemblyResolve"));
			il.Emit(OpCodes.Newobj, ImportCtor<ResolveEventHandler>(AssemblyDef, typeof(object), typeof(IntPtr)));
			il.Emit(OpCodes.Callvirt, ImportMethod<AppDomain>(AssemblyDef, "add_AssemblyResolve"));
			//il.Append(il.Create(OpCodes.Call, unusedMethod));
			il.Append(il.Create(OpCodes.Ret));




			TypeDefinition moduleClass = AssemblyDef.MainModule.Types.First(t => t.Name == "<Module>");
			moduleClass.Methods.Add(cctor);


			//var myAssembly = AssemblyDefinition.ReadAssembly("nMerge.exe", new ReaderParameters(ReadingMode.Immediate));
			//var setupMethod = FindMethod("MinimalWrapper::MinimalWrapperMain", myAssembly);
			//var wrapperType = FindType("MinimalWrapper", myAssembly);
			//var mDef = new MethodDefinition("Test", MethodAttributes.Public | MethodAttributes.Static, voidRef);
			//var testil = mDef.Body.GetILProcessor();
			//var oldinst = setupMethod.Body.Instructions;
			//AssemblyDef.MainModule.Types[1].Methods.Add(mDef);


			var moduleType = AssemblyDef.MainModule.Types.Single(x => x.Name == "<Module>");
			foreach (var m in resolveMethod)
				moduleType.Methods.Add(m);
			AssemblyDef.Write(ClParams.OutputFile);
			}

		private static List<MethodDefinition> CreateResolveMinimalMethod(AssemblyDefinition assemblyDef)
			{
			var method = new MethodDefinition("OnAssemblyResolve", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static, ImportType<Assembly>(assemblyDef));
			method.Parameters.Add(new ParameterDefinition(ImportType<object>(assemblyDef)));
			method.Parameters.Add(new ParameterDefinition(ImportType<ResolveEventArgs>(assemblyDef)));
			method.Body.Variables.Add(new VariableDefinition(ImportType<Stream>(assemblyDef)));
			method.Body.Variables.Add(new VariableDefinition(ImportType<byte[]>(assemblyDef)));
			method.Body.InitLocals = true;

			var il = method.Body.GetILProcessor();
			il.Emit(OpCodes.Ldstr, "Resolving(Minimal): ");
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Callvirt, ImportMethod<ResolveEventArgs>(assemblyDef, "get_Name"));
			il.Emit(OpCodes.Call, ImportMethod<String>(assemblyDef, "Concat", typeof(String), typeof(String)));
			il.Emit(OpCodes.Call, ImportMethod(assemblyDef, typeof(Console), "WriteLine", typeof(String)));

			il.Emit(OpCodes.Call, ImportMethod<Assembly>(assemblyDef, "GetExecutingAssembly"));
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Callvirt, ImportMethod<ResolveEventArgs>(assemblyDef, "get_Name"));
			il.Emit(OpCodes.Newobj, ImportCtor<AssemblyName>(assemblyDef, typeof(String)));
			il.Emit(OpCodes.Call, ImportMethod<AssemblyName>(assemblyDef, "get_Name"));
			il.Emit(OpCodes.Ldstr, ".dll.bz2");
			il.Emit(OpCodes.Call, ImportMethod<String>(assemblyDef, "Concat", typeof(string), typeof(string)));
			il.Emit(OpCodes.Callvirt, ImportMethod<Assembly>(assemblyDef, "GetManifestResourceStream", typeof(String)));
			il.Emit(OpCodes.Stloc_0);
			il.Emit(OpCodes.Ldloc_0);
			il.Emit(OpCodes.Callvirt, ImportMethod<Stream>(assemblyDef, "get_Length"));
			il.Emit(OpCodes.Conv_Ovf_I);
			il.Emit(OpCodes.Newarr, ImportType<byte>(assemblyDef));
			il.Emit(OpCodes.Stloc_1);
			il.Emit(OpCodes.Ldloc_0);
			il.Emit(OpCodes.Ldloc_1);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Ldloc_0);
			il.Emit(OpCodes.Callvirt, ImportMethod<Stream>(assemblyDef, "get_Length"));
			il.Emit(OpCodes.Conv_I4);
			il.Emit(OpCodes.Callvirt, ImportMethod<Stream>(assemblyDef, "Read", typeof(byte[]), typeof(int), typeof(int)));
			il.Emit(OpCodes.Pop);
			il.Emit(OpCodes.Ldloc_1);
			il.Emit(OpCodes.Call, ImportMethod<Assembly>(assemblyDef, "Load", typeof(byte[])));
			il.Emit(OpCodes.Ret);

			return new List<MethodDefinition>() { method };
			}
		private static List<MethodDefinition> CreateResolveCompressedMethod(AssemblyDefinition assemblyDef)
			{
			var method = new MethodDefinition("OnAssemblyResolve", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static, ImportType<Assembly>(assemblyDef));
			method.Parameters.Add(new ParameterDefinition(ImportType<object>(assemblyDef)));
			method.Parameters.Add(new ParameterDefinition(ImportType<ResolveEventArgs>(assemblyDef)));
			method.Body.Variables.Add(new VariableDefinition("inStream", ImportType<Stream>(assemblyDef)));
			method.Body.Variables.Add(new VariableDefinition("buf", ImportType<byte[]>(assemblyDef)));
			method.Body.Variables.Add(new VariableDefinition("outStream", ImportType<Stream>(assemblyDef)));
			method.Body.Variables.Add(new VariableDefinition("bytesRead", ImportType<Int32>(assemblyDef)));
			method.Body.InitLocals = true;

			var il = method.Body.GetILProcessor();
			var loopBodyStart = il.Create(OpCodes.Ldloc_2);
			var loopHeadStart = il.Create(OpCodes.Ldloc_0);


			il.Emit(OpCodes.Ldstr, "Resolving(Compressed): ");
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Callvirt, ImportMethod<ResolveEventArgs>(assemblyDef, "get_Name"));
			il.Emit(OpCodes.Call, ImportMethod<String>(assemblyDef, "Concat", typeof(String), typeof(String)));
			il.Emit(OpCodes.Call, ImportMethod(assemblyDef, typeof(Console), "WriteLine", typeof(String)));

			il.Emit(OpCodes.Call, ImportMethod<Assembly>(assemblyDef, "GetExecutingAssembly"));
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Callvirt, ImportMethod<ResolveEventArgs>(assemblyDef, "get_Name"));
			il.Emit(OpCodes.Newobj, ImportCtor<AssemblyName>(assemblyDef, typeof(String)));
			il.Emit(OpCodes.Call, ImportMethod<AssemblyName>(assemblyDef, "get_Name"));
			il.Emit(OpCodes.Ldstr, ".dll.bz2");
			il.Emit(OpCodes.Call, ImportMethod<String>(assemblyDef, "Concat", typeof(String), typeof(String)));

			il.Emit(OpCodes.Callvirt, ImportMethod<Assembly>(assemblyDef, "GetManifestResourceStream", typeof(String)));
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Newobj, ImportCtor<DeflateStream>(assemblyDef, typeof(Stream), typeof(CompressionMode)));
			il.Emit(OpCodes.Stloc_0);
			il.Emit(OpCodes.Ldc_I4, 1024);
			il.Emit(OpCodes.Newarr, ImportType<byte>(assemblyDef));
			il.Emit(OpCodes.Stloc_1);
			il.Emit(OpCodes.Newobj, ImportCtor<MemoryStream>(assemblyDef));
			il.Emit(OpCodes.Stloc_2);
			il.Emit(OpCodes.Br_S, loopHeadStart);	//Jump to head start
			//loop body start
			il.Append(loopBodyStart);
			il.Emit(OpCodes.Ldloc_1);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Ldloc_3);
			il.Emit(OpCodes.Callvirt, ImportMethod<Stream>(assemblyDef, "Write", typeof(byte[]), typeof(Int32), typeof(Int32)));
			//loop head start
			il.Append(loopHeadStart);
			il.Emit(OpCodes.Ldloc_1);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Ldc_I4, 1024);
			il.Emit(OpCodes.Callvirt, ImportMethod<Stream>(assemblyDef, "Read", typeof(byte[]), typeof(Int32), typeof(Int32)));
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Stloc_3);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Bgt_S, loopBodyStart);	//Conditional jump to body start
			//loop end

			il.Emit(OpCodes.Ldloc_2);
			il.Emit(OpCodes.Callvirt, ImportMethod<MemoryStream>(assemblyDef, "ToArray"));
			il.Emit(OpCodes.Call, ImportMethod<Assembly>(assemblyDef, "Load", typeof(byte[])));
			il.Emit(OpCodes.Ret);

			return new List<MethodDefinition>() { method };
			}

		}
	}
