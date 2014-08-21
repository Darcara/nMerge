using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Ionic.BZip2;
using Microsoft.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using Omega.App.nMerge.Options;
using Omega.App.nMerge.Properties;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace Omega.App.nMerge
	{
	using System.IO.Compression;

	public class Program
		{
		public static void Main(params string[] args)
			{
			String outputFile = null;
			String inputFile = null;
			String methodRaw = null;
			String librariesRaw = null;
			Boolean compress = false;
			Boolean includeZipLib = true;

			List<String> libraries = null;
			MethodReference method = null;
			Boolean isExecuteable = true;
			AssemblyDefinition assemblyDef = null;

			var cli = new OptionSet()
									{
										{"h|help|?", v => PrintUsage()},
										{"out=", v => outputFile = v},
										{"in=", v => inputFile = v},
										{"m=", v => methodRaw = v},
										{"lib=", v => librariesRaw = v},
										{"zip", v => compress = true},
										{"noziplib", v => includeZipLib = false},
									};

			
			try
				{
				cli.Parse(args);

				if(String.IsNullOrWhiteSpace(outputFile))
					throw new Exception("out must be set");
				if(String.IsNullOrWhiteSpace(inputFile))
					throw new Exception("in must be set");
				if(!File.Exists(inputFile))
					throw new FileNotFoundException("input file does not exist: " + Path.GetFullPath(inputFile), inputFile);

				if(Path.GetExtension(inputFile) == ".dll")
					{
					isExecuteable = false;
					}
				else if(Path.GetExtension(inputFile) == ".exe")
					isExecuteable = true;
				else
					throw new Exception("Unable to recognize type of input file. Must be 'dll' or 'exe'");

				var applicationPath =  Path.GetDirectoryName(inputFile);
				if(librariesRaw == null && applicationPath != null)
					librariesRaw = Path.Combine(applicationPath, "*.dll");
				
				libraries = ParseLibraries(librariesRaw, inputFile);
				assemblyDef = ReadAssembly(inputFile);
				}
			catch(Exception e)
				{
				Console.WriteLine("Invalid Command line parameters: " + e.Message);
				Console.WriteLine();
				PrintUsage();
				Environment.Exit(2);
				}

			Console.WriteLine("Preparations complete.");

			try
				{
				if(isExecuteable)
					{
					method = GetCalleeMethod(methodRaw, assemblyDef);

					if(method == null)
						throw new Exception("Entry method could not be determined for " + (String.IsNullOrEmpty(methodRaw) ? "<DefaultEntryPoint>" : methodRaw));

					BuildWrapper(outputFile, inputFile, libraries, method.DeclaringType.FullName + "," + assemblyDef.FullName, method.Name, compress, includeZipLib);
					InjectWrapper();
					}
				else
					{
					foreach (var library in libraries)
						{
						if(compress)
							{
							String compressedLib = Path.GetFileName(library) + ".bz2";
							CopyFile(library, compressedLib, true);
							assemblyDef.MainModule.Resources.Add(new EmbeddedResource(Path.GetFileName(compressedLib), ManifestResourceAttributes.Public, File.ReadAllBytes(compressedLib)));
							}
						else
							assemblyDef.MainModule.Resources.Add(new EmbeddedResource(Path.GetFileName(library) + ".bz2", ManifestResourceAttributes.Public, File.ReadAllBytes(library)));
						}
					

					//var unusedMethod = FindMethod("MainLibrary.DerivedClass::UnusedMethod", assemblyDef);
					//var voidRef = assemblyDef.MainModule.Import(unusedMethod.ReturnType);
					const MethodAttributes attributes = MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
					var cctor = new MethodDefinition(".cctor", attributes, ImportType(assemblyDef, typeof(void)));

					List<MethodDefinition> resolveMethod;

					if(compress)
						{
						if(includeZipLib)
							{
							assemblyDef.MainModule.Resources.Add(new EmbeddedResource("Ionic.BZip2.dll.bz2", ManifestResourceAttributes.Public, File.ReadAllBytes(GetIonicBzipPath())));
							resolveMethod = CreateResolveMixedMethod(assemblyDef);
							}
						else
							resolveMethod = CreateResolveCompressedMethod(assemblyDef);
						}
					else
						resolveMethod = CreateResolveMinimalMethod(assemblyDef);


					ILProcessor il = cctor.Body.GetILProcessor();


					//il.Append(il.Create(OpCodes.Call, unusedMethod));
					il.Emit(OpCodes.Ldstr, "Initializing Assembly Resolver");
					il.Emit(OpCodes.Call, ImportMethod(assemblyDef, typeof(Console), "WriteLine", typeof(String)));

					il.Append(il.Create(OpCodes.Call, ImportMethod<AppDomain>(assemblyDef, "get_CurrentDomain")));
					il.Emit(OpCodes.Ldnull);
					il.Emit(OpCodes.Ldftn, resolveMethod.First(m => m.Name == "OnAssemblyResolve"));
					il.Emit(OpCodes.Newobj, ImportCtor<ResolveEventHandler>(assemblyDef, typeof(object), typeof(IntPtr)));
					il.Emit(OpCodes.Callvirt, ImportMethod<AppDomain>(assemblyDef, "add_AssemblyResolve"));
					//il.Append(il.Create(OpCodes.Call, unusedMethod));
					il.Append(il.Create(OpCodes.Ret));

					


					TypeDefinition moduleClass = assemblyDef.MainModule.Types.First(t => t.Name == "<Module>");
					moduleClass.Methods.Add(cctor);


					//var myAssembly = AssemblyDefinition.ReadAssembly("nMerge.exe", new ReaderParameters(ReadingMode.Immediate));
					//var setupMethod = FindMethod("MinimalWrapper::MinimalWrapperMain", myAssembly);
					//var wrapperType = FindType("MinimalWrapper", myAssembly);
					//var mDef = new MethodDefinition("Test", MethodAttributes.Public | MethodAttributes.Static, voidRef);
					//var testil = mDef.Body.GetILProcessor();
					//var oldinst = setupMethod.Body.Instructions;
					//assemblyDef.MainModule.Types[1].Methods.Add(mDef);


					var moduleType = assemblyDef.MainModule.Types.Single(x => x.Name == "<Module>");
					foreach(var m in resolveMethod)
					moduleType.Methods.Add(m);
					assemblyDef.Write(outputFile);
					}

				}
			catch(Exception e)
				{
				Console.WriteLine("Failed to merge assemblies: " + e);
				Environment.Exit(3);
				}

			Console.WriteLine("All Done!");
			}

		private static String GetIonicBzipPath()
			{
			String zip = "Ionic.BZip2.dll";
			if(File.Exists(zip))
				return Path.GetFullPath(zip);

			String loc = Assembly.GetExecutingAssembly().Location;
			if(!String.IsNullOrEmpty(loc) && File.Exists(Path.Combine(Path.GetDirectoryName(loc), zip)))
				return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(loc), zip));
			
			loc = Assembly.GetEntryAssembly().Location;
			if(!String.IsNullOrEmpty(loc) && File.Exists(Path.Combine(Path.GetDirectoryName(loc), zip)))
				return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(loc), zip));

			return null;
			}

		private static TypeReference ImportType<T>(AssemblyDefinition assemblyDef)
			{
			return assemblyDef.MainModule.Import(typeof(T));
			}
		private static TypeReference ImportType(AssemblyDefinition assemblyDef, Type t)
			{
			return assemblyDef.MainModule.Import(t);
			}
		private static MethodReference ImportMethod<T>(AssemblyDefinition assemblyDef, string methodName)
			{
			return assemblyDef.MainModule.Import(typeof(T).GetMethod(methodName));
			}
		private static MethodReference ImportMethod<T>(AssemblyDefinition assemblyDef, string methodName, params Type[] types)
			{
			return assemblyDef.MainModule.Import(typeof(T).GetMethod(methodName, types));
			}
		private static MethodReference ImportMethod(AssemblyDefinition assemblyDef, Type t, string methodName, params Type[] types)
			{
			return assemblyDef.MainModule.Import(t.GetMethod(methodName, types));
			}
		private static MethodReference ImportCtor<T>(AssemblyDefinition assemblyDef, params Type[] types)
			{
			return assemblyDef.MainModule.Import(typeof(T).GetConstructor(types));
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
			il.Emit(OpCodes.Newobj, ImportCtor<BZip2InputStream>(assemblyDef, typeof(Stream)));
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
		private static List<MethodDefinition> CreateResolveMixedMethod(AssemblyDefinition assemblyDef)
			{
			var loadAssemblyFromStream = new MethodDefinition("LoadAssemblyFromStream", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static, ImportType<Assembly>(assemblyDef));
			loadAssemblyFromStream.Parameters.Add(new ParameterDefinition(ImportType<Stream>(assemblyDef)));
			loadAssemblyFromStream.Body.Variables.Add(new VariableDefinition("buf", ImportType<byte[]>(assemblyDef)));
			loadAssemblyFromStream.Body.Variables.Add(new VariableDefinition("outStream", ImportType<Stream>(assemblyDef)));
			loadAssemblyFromStream.Body.Variables.Add(new VariableDefinition("bytesRead", ImportType<Int32>(assemblyDef)));
			loadAssemblyFromStream.Body.InitLocals = true;

			var il = loadAssemblyFromStream.Body.GetILProcessor();
			var loopBodyStart = il.Create(OpCodes.Ldloc_1);
			var loopHeadStart = il.Create(OpCodes.Ldarg_0);
			
			il.Emit(OpCodes.Ldc_I4, 1024);
			il.Emit(OpCodes.Newarr, ImportType<byte>(assemblyDef));
			il.Emit(OpCodes.Stloc_0);
			il.Emit(OpCodes.Newobj, ImportCtor<MemoryStream>(assemblyDef));
			il.Emit(OpCodes.Stloc_1);
			il.Emit(OpCodes.Br_S, loopHeadStart);	//Jump to head start
			//loop body start
			il.Append(loopBodyStart);
			il.Emit(OpCodes.Ldloc_0);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Ldloc_2);
			il.Emit(OpCodes.Callvirt, ImportMethod<Stream>(assemblyDef, "Write", typeof(byte[]), typeof(Int32), typeof(Int32)));
			//loop head start
			il.Append(loopHeadStart);
			il.Emit(OpCodes.Ldloc_0);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Ldc_I4, 1024);
			il.Emit(OpCodes.Callvirt, ImportMethod<Stream>(assemblyDef, "Read", typeof(byte[]), typeof(Int32), typeof(Int32)));
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Stloc_2);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Bgt_S, loopBodyStart);	//Conditional jump to body start
			//loop end

			il.Emit(OpCodes.Ldloc_1);
			il.Emit(OpCodes.Callvirt, ImportMethod<MemoryStream>(assemblyDef, "ToArray"));
			il.Emit(OpCodes.Call, ImportMethod<Assembly>(assemblyDef, "Load", typeof(byte[])));
			il.Emit(OpCodes.Ret);




			var loadAssemblyFromCompressedStream = new MethodDefinition("LoadAssemblyFromCompressedStream", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static, ImportType<Assembly>(assemblyDef));
			loadAssemblyFromCompressedStream.Parameters.Add(new ParameterDefinition(ImportType<Stream>(assemblyDef)));
			loadAssemblyFromCompressedStream.Body.InitLocals = true;

			il = loadAssemblyFromCompressedStream.Body.GetILProcessor();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Newobj, ImportCtor<BZip2InputStream>(assemblyDef, typeof(Stream)));
			il.Emit(OpCodes.Call, loadAssemblyFromStream);
			il.Emit(OpCodes.Ret);
			
		

			var method = new MethodDefinition("OnAssemblyResolve", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static, ImportType<Assembly>(assemblyDef));
			method.Parameters.Add(new ParameterDefinition(ImportType<object>(assemblyDef)));
			method.Parameters.Add(new ParameterDefinition(ImportType<ResolveEventArgs>(assemblyDef)));
			method.Body.Variables.Add(new VariableDefinition("assemblyName", ImportType<String>(assemblyDef)));
			method.Body.Variables.Add(new VariableDefinition("inStream", ImportType<Stream>(assemblyDef)));
			method.Body.Variables.Add(new VariableDefinition("buf", ImportType<byte[]>(assemblyDef)));
			method.Body.Variables.Add(new VariableDefinition("outStream", ImportType<Stream>(assemblyDef)));
			method.Body.Variables.Add(new VariableDefinition("bytesRead", ImportType<Int32>(assemblyDef)));
			method.Body.InitLocals = true;

			il = method.Body.GetILProcessor();
			var callLoadAssemblyFromStream = il.Create(OpCodes.Ldloc_1);

			il.Emit(OpCodes.Ldstr, "Resolving(Mixed): ");
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Callvirt, ImportMethod<ResolveEventArgs>(assemblyDef, "get_Name"));
			il.Emit(OpCodes.Call, ImportMethod<String>(assemblyDef, "Concat", typeof(String), typeof(String)));
			il.Emit(OpCodes.Call, ImportMethod(assemblyDef, typeof(Console), "WriteLine", typeof(String)));

			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Callvirt, ImportMethod<ResolveEventArgs>(assemblyDef, "get_Name"));
			il.Emit(OpCodes.Newobj, ImportCtor<AssemblyName>(assemblyDef, typeof(String)));
			il.Emit(OpCodes.Call, ImportMethod<AssemblyName>(assemblyDef, "get_Name"));
			il.Emit(OpCodes.Ldstr, ".dll.bz2");
			il.Emit(OpCodes.Call, ImportMethod<String>(assemblyDef, "Concat", typeof(String), typeof(String)));
			il.Emit(OpCodes.Stloc_0);
			il.Emit(OpCodes.Call, ImportMethod<Assembly>(assemblyDef, "GetExecutingAssembly"));
			il.Emit(OpCodes.Ldloc_0);
			il.Emit(OpCodes.Callvirt, ImportMethod<Assembly>(assemblyDef, "GetManifestResourceStream", typeof(String)));
			il.Emit(OpCodes.Stloc_1);
			il.Emit(OpCodes.Ldloc_0);
			il.Emit(OpCodes.Ldstr, "Ionic.BZip2.dll.bz2");
			il.Emit(OpCodes.Call, ImportMethod<String>(assemblyDef, "op_Equality", typeof(String), typeof(String)));
			il.Emit(OpCodes.Brtrue_S, callLoadAssemblyFromStream);

			il.Emit(OpCodes.Ldloc_1);
			il.Emit(OpCodes.Call, loadAssemblyFromCompressedStream);
			il.Emit(OpCodes.Ret);
			
			il.Append(callLoadAssemblyFromStream);
			il.Emit(OpCodes.Call, loadAssemblyFromStream);
			il.Emit(OpCodes.Ret);


			return new List<MethodDefinition>() { method, loadAssemblyFromCompressedStream, loadAssemblyFromStream };
			}

		private static void BuildWrapper(String outputFile, String mainAssembly, List<String> libraries, String mainClassTypeName, String mainMethod, Boolean compress, Boolean includeZipLib)
			{
			var resourcesRaw = libraries == null ? new List<string>() : libraries.ToList();
			resourcesRaw.Add(mainAssembly);
			var resourcesLocal = CreateResources("Resources.resources", resourcesRaw, compress);
			var provider = new CSharpCodeProvider();
			var compilerparams = new CompilerParameters
														{
															GenerateExecutable = true,
															GenerateInMemory = false,
															OutputAssembly = outputFile
														};
			compilerparams.ReferencedAssemblies.Add(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.dll");
			compilerparams.ReferencedAssemblies.Add(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Data.dll");
			compilerparams.ReferencedAssemblies.Add(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Core.dll");
			compilerparams.ReferencedAssemblies.Add(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Xml.dll");
			compilerparams.ReferencedAssemblies.Add(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Xml.Linq.dll");
			if(compress)
				compilerparams.ReferencedAssemblies.Add(GetIonicBzipPath());

#if DEBUG
			compilerparams.CompilerOptions += " /debug /define:DEBUG";
#endif
			foreach (var resource in resourcesLocal)
				compilerparams.EmbeddedResources.Add(resource);
			
			if(compress)
				{
				if(includeZipLib)
					compilerparams.EmbeddedResources.Add(GetIonicBzipPath());
				compilerparams.CompilerOptions += " /define:COMPRESS";
				}

			String code = Resources.Wrapper
				.Replace("***TYPE***", mainClassTypeName)
				.Replace("***METHOD***", mainMethod);
			CompilerResults results = provider.CompileAssemblyFromSource(compilerparams, code);
			if(results.Errors.HasErrors)
				{
				var errors = new StringBuilder("Compiler Errors :\r\n");
				foreach(CompilerError error in results.Errors)
					{
					errors.AppendFormat("Line {0},{1}\t: {2}\n",
								 error.Line, error.Column, error.ErrorText);
					}

				throw new Exception(errors.ToString());
				}
			


			}

		private static IEnumerable<string> CreateResources(String filename, IEnumerable<String> resources, Boolean compress)
			{
			var loclaResources = new List<String>();
			foreach (var resource in resources)
				{
				String localFile = Path.GetFileName(resource) + ".bz2";
				CopyFile(resource, localFile, compress);

				loclaResources.Add(localFile);
				}

			return loclaResources;
			}

		private static void CopyFile(String inFile, String outFile, Boolean compress)
			{
			using(FileStream inStream = File.OpenRead(inFile))
			using(FileStream outStream = File.Create(outFile))
				{
				if(compress)
					{
					using(var compressionStream = new BZip2OutputStream(outStream))
						inStream.CopyTo(compressionStream);
					}
				else
					inStream.CopyTo(outStream);
				}

			using (FileStream inStream = File.OpenRead(inFile))
			using (FileStream outStream = File.Create(outFile + ".zip"))
				{
					using (var compressionStream = new GZipStream(outStream, CompressionMode.Compress))
						inStream.CopyTo(compressionStream);
				}
			using (FileStream inStream = File.OpenRead(inFile))
			using (FileStream outStream = File.Create(outFile + ".deflate"))
				{
				using (var compressionStream = new DeflateStream(outStream, CompressionMode.Compress))
					inStream.CopyTo(compressionStream);
				}

			}

		private static void InjectWrapper()
			{
			
			}

		private static List<String> ParseLibraries(String raw, String mainAssembly)
			{
			if(String.IsNullOrWhiteSpace(raw))
				return null;

			var rawSplit = raw.Split(',');

			var l = new List<String>();
			foreach(var libraryFile in rawSplit.Where(libraryFile => !String.IsNullOrWhiteSpace(libraryFile)))
				{
				var dir = Path.GetDirectoryName(libraryFile);
				var searchPattern = Path.GetFileName(libraryFile);
				var files = Directory.GetFiles(String.IsNullOrWhiteSpace(dir) ? "./" : dir, searchPattern ?? "*.dll");

				foreach (var file in files.Where(file => file != mainAssembly && Path.GetFileName(file) != "Ionic.BZip2.dll"))
					{
					if(!File.Exists(file))
						throw new FileNotFoundException("Library not found: " + file, file);

					Console.WriteLine("Found library " + file);
					l.Add(file);
					}

				if(files.Length == 0)
					Console.WriteLine("No files found for: " + libraryFile);
				}
			return l;
			} 

		private static MethodReference GetCalleeMethod(String methodRaw, AssemblyDefinition assemblyDef)
			{
			if(String.IsNullOrWhiteSpace(methodRaw))
				{
				if(assemblyDef.EntryPoint == null)
					throw new Exception("No method supplied and target assembly has no entry point.");

				methodRaw = String.Format("{0}::{1}", assemblyDef.EntryPoint.DeclaringType.FullName, assemblyDef.EntryPoint.Name);
				}

			ModuleDefinition module = assemblyDef.MainModule;
			if(!methodRaw.Contains("::"))
				throw new Exception("Invalid format for m(ethod) parameter. Expected: Some.Namespace.Class::MethodName");

			String typeName = methodRaw.Substring(0, methodRaw.IndexOf("::"));
			String methodName = methodRaw.Substring(typeName.Length + 2);
			TypeDefinition moduleInitializerClass = module.Types.FirstOrDefault(t => t.FullName == typeName);
			
			if(moduleInitializerClass == null)
				throw new Exception(String.Format("No type named '{0}' exists in assembly '{1}'!", typeName, assemblyDef.FullName));

			MethodDefinition callee = moduleInitializerClass.Methods.FirstOrDefault(m => m.Name == methodName);
			
			if(callee == null)
				throw new Exception(string.Format("No method named '{0}' exists in the type '{1}'", methodName, typeName));
			if(callee.Parameters.Count != 1 || callee.Parameters[0].Name.Equals(typeof(String[]).Name))
				throw new Exception("Method must have exactly one parameter with type 'System.String[]'");
			if(callee.IsPrivate || callee.IsFamily)
				throw new Exception("Method must be public.");
			if(!callee.ReturnType.FullName.Equals(typeof(void).FullName))
				throw new Exception("Method return type must be void.");
			if(!callee.IsStatic)
				throw new Exception("Method must be static.");

			return callee;
			}

		private static MethodDefinition FindMethod(String methodRaw, AssemblyDefinition assemblyDef)
			{
			if(String.IsNullOrWhiteSpace(methodRaw))
				throw new ArgumentNullException("methodRaw", "No method supplied.");

			ModuleDefinition module = assemblyDef.MainModule;
			if(!methodRaw.Contains("::"))
				throw new Exception("Invalid format for m(ethod) parameter. Expected: Some.Namespace.Class::MethodName");

			String typeName = methodRaw.Substring(0, methodRaw.IndexOf("::"));
			String methodName = methodRaw.Substring(typeName.Length + 2);
			TypeDefinition moduleInitializerClass = module.Types.FirstOrDefault(t => t.FullName == typeName);

			if(moduleInitializerClass == null)
				throw new Exception(String.Format("No type named '{0}' exists in assembly '{1}'!", typeName, assemblyDef.FullName));

			MethodDefinition callee = moduleInitializerClass.Methods.FirstOrDefault(m => m.Name == methodName);

			if(callee == null)
				throw new Exception(string.Format("No method named '{0}' exists in the type '{1}'", methodName, typeName));

			return callee;
			}

		private static TypeDefinition FindType(String typeName, AssemblyDefinition assemblyDef)
			{
			if(String.IsNullOrWhiteSpace(typeName))
				throw new ArgumentNullException("typeName", "No type supplied.");

			ModuleDefinition module = assemblyDef.MainModule;
			return module.Types.FirstOrDefault(t => t.FullName == typeName);

			
			}

		private static AssemblyDefinition ReadAssembly(String assemblyFile)
			{
			var readParams = new ReaderParameters(ReadingMode.Immediate);
			if(File.Exists(Path.ChangeExtension(assemblyFile, ".pdb")))
				{
				readParams.ReadSymbols = true;
				readParams.SymbolReaderProvider = new PdbReaderProvider();
				}
			return AssemblyDefinition.ReadAssembly(assemblyFile, readParams);
			}

		private static void PrintUsage()
			{
			
			Console.WriteLine(@"nmerge.exe /out=<outputFile> /in=<application> [/lib=<library>,<library>] [/zip] [/m=<method>]

/out=<outputFile>   The output file

/in=<application>   The application assembly

/m=<method>         Optional.
                    The fully qualified method name
                    Method must be public static void(String[] args)
                    Defaults to the entry point for executeables
                    e.g: Some.Namespace.Program::Main

/lib=<library>      Optional.
                    A ','-separated list of assemblies to include
                    Wildcards are permitted
                    If not specified, all *.dll files in the same directory
                    as the application are assumed

/zip                Optional.
                    If specified all assemblies will be compressed.
                    This has a slight performance cost, but may drastically
                    reduce filesize

/noziplib           Optional.
                    Requires: /zip
                    If specified the Ionic.BZip2.dll will not be merged into
                    the assembly. Use this if the library will be available or
                    the assembly will be nMerged again with /zip enabled.

/?, /h, /help       Prints this helpful screen

Additional information on this website:
	https://github.com/Darcara/nMerge");
			Environment.Exit(1);
			}
		}
	}
