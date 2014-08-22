using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Omega.App.nMerge
	{
	using System.IO;
	using System.IO.Compression;
	using Mono.Cecil;
	using Mono.Cecil.Pdb;

	internal abstract class Merger
		{
		protected AssemblyDefinition AssemblyDef;
		protected CommandLine ClParams;

		protected Merger(CommandLine cl)
			{
			ClParams = cl;
			AssemblyDef = ReadAssembly(ClParams.InputFile);
			}

		protected AssemblyDefinition ReadAssembly(String assemblyFile)
			{
			var readParams = new ReaderParameters(ReadingMode.Immediate);
			if (File.Exists(Path.ChangeExtension(assemblyFile, ".pdb")))
				{
				readParams.ReadSymbols = true;
				readParams.SymbolReaderProvider = new PdbReaderProvider();
				}
			return AssemblyDefinition.ReadAssembly(assemblyFile, readParams);
			}

		protected static TypeReference ImportType<T>(AssemblyDefinition assemblyDef)
			{
			return assemblyDef.MainModule.Import(typeof(T));
			}

		protected static TypeReference ImportType(AssemblyDefinition assemblyDef, Type t)
			{
			return assemblyDef.MainModule.Import(t);
			}

		protected static MethodReference ImportMethod<T>(AssemblyDefinition assemblyDef, string methodName)
			{
			return assemblyDef.MainModule.Import(typeof(T).GetMethod(methodName));
			}

		protected static MethodReference ImportMethod<T>(AssemblyDefinition assemblyDef, string methodName, params Type[] types)
			{
			return assemblyDef.MainModule.Import(typeof(T).GetMethod(methodName, types));
			}

		protected static MethodReference ImportMethod(AssemblyDefinition assemblyDef, Type t, string methodName, params Type[] types)
			{
			return assemblyDef.MainModule.Import(t.GetMethod(methodName, types));
			}

		protected static MethodReference ImportCtor<T>(AssemblyDefinition assemblyDef, params Type[] types)
			{
			return assemblyDef.MainModule.Import(typeof(T).GetConstructor(types));
			}

		protected static void CopyFile(String inFile, String outFile, Boolean compress)
			{
			using (FileStream inStream = File.OpenRead(inFile))
			using (FileStream outStream = File.Create(outFile))
				{
				if (compress)
					{
					using (var compressionStream = new DeflateStream(outStream, CompressionMode.Compress))
						inStream.CopyTo(compressionStream);
					}
				else
					inStream.CopyTo(outStream);
				}
			}

		private static MethodDefinition FindMethod(String methodRaw, AssemblyDefinition assemblyDef)
			{
			if (String.IsNullOrWhiteSpace(methodRaw))
				throw new ArgumentNullException("methodRaw", "No method supplied.");

			ModuleDefinition module = assemblyDef.MainModule;
			if (!methodRaw.Contains("::"))
				throw new Exception("Invalid format for m(ethod) parameter. Expected: Some.Namespace.Class::MethodName");

			String typeName = methodRaw.Substring(0, methodRaw.IndexOf("::"));
			String methodName = methodRaw.Substring(typeName.Length + 2);
			TypeDefinition moduleInitializerClass = module.Types.FirstOrDefault(t => t.FullName == typeName);

			if (moduleInitializerClass == null)
				throw new Exception(String.Format("No type named '{0}' exists in assembly '{1}'!", typeName, assemblyDef.FullName));

			MethodDefinition callee = moduleInitializerClass.Methods.FirstOrDefault(m => m.Name == methodName);

			if (callee == null)
				throw new Exception(String.Format("No method named '{0}' exists in the type '{1}'", methodName, typeName));

			return callee;
			}

		private static TypeDefinition FindType(String typeName, AssemblyDefinition assemblyDef)
			{
			if (String.IsNullOrWhiteSpace(typeName))
				throw new ArgumentNullException("typeName", "No type supplied.");

			ModuleDefinition module = assemblyDef.MainModule;
			return module.Types.FirstOrDefault(t => t.FullName == typeName);


			}
		}
	}
