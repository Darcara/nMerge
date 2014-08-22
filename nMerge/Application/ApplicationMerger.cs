namespace Omega.App.nMerge.Application
	{
	using System;
	using System.CodeDom.Compiler;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using Microsoft.CSharp;
	using Mono.Cecil;
	using Omega.App.nMerge.Properties;

	internal class ApplicationMerger : Merger
		{
		private MethodReference _applicationEntryMethod;

		public ApplicationMerger(CommandLine clParams) : base(clParams)
			{
			}

		public void Merge()
			{
			_applicationEntryMethod = GetCalleeMethod(ClParams.MethodRaw, AssemblyDef);

			if (_applicationEntryMethod == null)
				throw new Exception("Entry method could not be determined for " + (String.IsNullOrEmpty(ClParams.MethodRaw) ? "<DefaultEntryPoint>" : ClParams.MethodRaw));

			BuildWrapper(ClParams.OutputFile, ClParams.InputFile, ClParams.Libraries, _applicationEntryMethod.DeclaringType.FullName + "," + AssemblyDef.FullName, _applicationEntryMethod.Name, ClParams.Compress);
			InjectWrapper();

			}

		protected static MethodReference GetCalleeMethod(String methodRaw, AssemblyDefinition assemblyDef)
			{
			if (String.IsNullOrWhiteSpace(methodRaw))
				{
				if (assemblyDef.EntryPoint == null)
					throw new Exception("No method supplied and target assembly has no entry point.");

				methodRaw = String.Format("{0}::{1}", assemblyDef.EntryPoint.DeclaringType.FullName, assemblyDef.EntryPoint.Name);
				}

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
				throw new Exception(string.Format("No method named '{0}' exists in the type '{1}'", methodName, typeName));
			if (callee.Parameters.Count != 1 || callee.Parameters[0].Name.Equals(typeof(String[]).Name))
				throw new Exception("Method must have exactly one parameter with type 'System.String[]'");
			if (callee.IsPrivate || callee.IsFamily)
				throw new Exception("Method must be public.");
			if (!callee.ReturnType.FullName.Equals(typeof(void).FullName))
				throw new Exception("Method return type must be void.");
			if (!callee.IsStatic)
				throw new Exception("Method must be static.");

			return callee;
			}
		private static void BuildWrapper(String outputFile, String mainAssembly, List<String> libraries, String mainClassTypeName, String mainMethod, Boolean compress)
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

#if DEBUG
			compilerparams.CompilerOptions += " /debug /define:DEBUG";
#endif
			foreach (var resource in resourcesLocal)
				compilerparams.EmbeddedResources.Add(resource);

			if (compress)
				{
				compilerparams.CompilerOptions += " /define:COMPRESS";
				}

			String code = Resources.Wrapper
				.Replace("***TYPE***", mainClassTypeName)
				.Replace("***METHOD***", mainMethod);
			CompilerResults results = provider.CompileAssemblyFromSource(compilerparams, code);
			if (results.Errors.HasErrors)
				{
				var errors = new StringBuilder("Compiler Errors :\r\n");
				foreach (CompilerError error in results.Errors)
					{
					errors.AppendFormat("Line {0},{1}\t: {2}\n",
								 error.Line, error.Column, error.ErrorText);
					}

				throw new Exception(errors.ToString());
				}



			}
		private static void InjectWrapper()
			{

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



		}
	}
