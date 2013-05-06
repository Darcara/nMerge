using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ionic.BZip2;
using Microsoft.CSharp;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using Omega.App.nMerge.Options;
using Omega.App.nMerge.Properties;

namespace Omega.App.nMerge
	{
	public class Program
		{
		public static void Main(params string[] args)
			{
			String outputFile = null;
			String inputFile = null;
			String methodRaw = null;
			String librariesRaw = null;
			Boolean compress = false;

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
			          	};

			
			try
				{
				cli.Parse(args);

				if(String.IsNullOrWhiteSpace(outputFile))
					throw new Exception("out must be set");
				if(String.IsNullOrWhiteSpace(inputFile))
					throw new Exception("in must be set");
				if(!File.Exists(inputFile))
					throw new FileNotFoundException("input file does not exist", inputFile);

				if(Path.GetExtension(inputFile) == ".dll")
					{
					isExecuteable = false;
					throw new NotSupportedException("Merging libraries currently not supported");
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
				method = GetCalleeMethod(methodRaw, assemblyDef);

				if(method == null)
					throw new Exception("Entry method could not be determined for " + (String.IsNullOrEmpty(methodRaw) ? "<DefaultEntryPoint>" : methodRaw));
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
				BuildWrapper(outputFile, inputFile, libraries, method.DeclaringType.FullName + "," + assemblyDef.FullName, method.Name, compress);
				InjectWrapper();
				}
			catch(Exception e)
				{
				Console.WriteLine("Failed to merge assemblies: " + e);
				Environment.Exit(3);
				}

			Console.WriteLine("All Done!");
			}

		private static void BuildWrapper(String outputFile, String mainAssembly, List<String> libraries, String mainClassTypeName, String mainMethod, Boolean compress)
			{
			var resourcesRaw = libraries.ToList();
			resourcesRaw.Add(mainAssembly);
			var resourcesLocal = CreateResources("Resources.resources", resourcesRaw, compress);
			var provider = new CSharpCodeProvider();
			var compilerparams = new CompilerParameters
			                     	{
															GenerateExecutable = true,
															GenerateInMemory = false,
															OutputAssembly = outputFile,
															
			                     	};
			compilerparams.ReferencedAssemblies.Add(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.dll");
			compilerparams.ReferencedAssemblies.Add(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Data.dll");
			compilerparams.ReferencedAssemblies.Add(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Core.dll");
			compilerparams.ReferencedAssemblies.Add(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Xml.dll");
			compilerparams.ReferencedAssemblies.Add(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Xml.Linq.dll");
			if(compress)
				compilerparams.ReferencedAssemblies.Add(@"Ionic.BZip2.dll");
#if DEBUG
			compilerparams.CompilerOptions += " /debug /define:DEBUG";
#endif
			foreach (var resource in resourcesLocal)
				compilerparams.EmbeddedResources.Add(resource);
			
			if(compress)
				{
				compilerparams.EmbeddedResources.Add(@"Ionic.BZip2.dll");
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
			foreach (var libraryFile in rawSplit)
				{
				var dir = Path.GetDirectoryName(libraryFile);
				var searchPattern = Path.GetFileName(libraryFile);
				var files = Directory.GetFiles(dir ?? "./", searchPattern ?? "*.dll");

				foreach (var file in files.Where(file => file != mainAssembly && Path.GetFileName(file) != "Ionic.BZip2.dll"))
					{
					if(!File.Exists(file))
						throw new FileNotFoundException("Library not found", file);

					Console.WriteLine("Found library " + file);
					l.Add(file);
					}
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

/?, /h, /help       Prints this helpful screen

Additional information on this website:
	https://github.com/Darcara/nMerge");
			Environment.Exit(1);
			}
		}
	}
