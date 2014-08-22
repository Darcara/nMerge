using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
	using Omega.App.nMerge.Application;
	using Omega.App.nMerge.Library;

	public class Program
		{
		public static void Main(params string[] args)
			{
			CommandLine cl;
			try
				{
				cl = new CommandLine(args);
				}
			catch (Exception e)
				{
				Log.Error("Invalid Command line parameters: " + e.Message);
				Log.Error(String.Empty);
				Log.Error(CommandLine.Usage);
				return;
				}

			Log.LogLevel = cl.LogLevel;
			MethodReference method = null;
			
			
			Log.Info("Preparations complete.");

			try
				{
				if(cl.MergeType == MergeType.Application)
					{
					var merger = new ApplicationMerger(cl);
					merger.Merge();
					}
				else
					{
					var merger = new LibraryMerger(cl);
					merger.Merge();

					}

				}
			catch(Exception e)
				{
				Log.Error("Failed to merge assemblies: " + e);
				Environment.Exit(3);
				}

			Log.Info("All Done!");
			}

		//private static List<MethodDefinition> CreateResolveMixedMethod(AssemblyDefinition assemblyDef)
		//	{
		//	var loadAssemblyFromStream = new MethodDefinition("LoadAssemblyFromStream", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static, ImportType<Assembly>(assemblyDef));
		//	loadAssemblyFromStream.Parameters.Add(new ParameterDefinition(ImportType<Stream>(assemblyDef)));
		//	loadAssemblyFromStream.Body.Variables.Add(new VariableDefinition("buf", ImportType<byte[]>(assemblyDef)));
		//	loadAssemblyFromStream.Body.Variables.Add(new VariableDefinition("outStream", ImportType<Stream>(assemblyDef)));
		//	loadAssemblyFromStream.Body.Variables.Add(new VariableDefinition("bytesRead", ImportType<Int32>(assemblyDef)));
		//	loadAssemblyFromStream.Body.InitLocals = true;

		//	var il = loadAssemblyFromStream.Body.GetILProcessor();
		//	var loopBodyStart = il.Create(OpCodes.Ldloc_1);
		//	var loopHeadStart = il.Create(OpCodes.Ldarg_0);
			
		//	il.Emit(OpCodes.Ldc_I4, 1024);
		//	il.Emit(OpCodes.Newarr, ImportType<byte>(assemblyDef));
		//	il.Emit(OpCodes.Stloc_0);
		//	il.Emit(OpCodes.Newobj, ImportCtor<MemoryStream>(assemblyDef));
		//	il.Emit(OpCodes.Stloc_1);
		//	il.Emit(OpCodes.Br_S, loopHeadStart);	//Jump to head start
		//	//loop body start
		//	il.Append(loopBodyStart);
		//	il.Emit(OpCodes.Ldloc_0);
		//	il.Emit(OpCodes.Ldc_I4_0);
		//	il.Emit(OpCodes.Ldloc_2);
		//	il.Emit(OpCodes.Callvirt, ImportMethod<Stream>(assemblyDef, "Write", typeof(byte[]), typeof(Int32), typeof(Int32)));
		//	//loop head start
		//	il.Append(loopHeadStart);
		//	il.Emit(OpCodes.Ldloc_0);
		//	il.Emit(OpCodes.Ldc_I4_0);
		//	il.Emit(OpCodes.Ldc_I4, 1024);
		//	il.Emit(OpCodes.Callvirt, ImportMethod<Stream>(assemblyDef, "Read", typeof(byte[]), typeof(Int32), typeof(Int32)));
		//	il.Emit(OpCodes.Dup);
		//	il.Emit(OpCodes.Stloc_2);
		//	il.Emit(OpCodes.Ldc_I4_0);
		//	il.Emit(OpCodes.Bgt_S, loopBodyStart);	//Conditional jump to body start
		//	//loop end

		//	il.Emit(OpCodes.Ldloc_1);
		//	il.Emit(OpCodes.Callvirt, ImportMethod<MemoryStream>(assemblyDef, "ToArray"));
		//	il.Emit(OpCodes.Call, ImportMethod<Assembly>(assemblyDef, "Load", typeof(byte[])));
		//	il.Emit(OpCodes.Ret);




		//	var loadAssemblyFromCompressedStream = new MethodDefinition("LoadAssemblyFromCompressedStream", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static, ImportType<Assembly>(assemblyDef));
		//	loadAssemblyFromCompressedStream.Parameters.Add(new ParameterDefinition(ImportType<Stream>(assemblyDef)));
		//	loadAssemblyFromCompressedStream.Body.InitLocals = true;

		//	il = loadAssemblyFromCompressedStream.Body.GetILProcessor();
		//	il.Emit(OpCodes.Ldarg_0);
		//	il.Emit(OpCodes.Newobj, ImportCtor<BZip2InputStream>(assemblyDef, typeof(Stream)));
		//	il.Emit(OpCodes.Call, loadAssemblyFromStream);
		//	il.Emit(OpCodes.Ret);
			
		

		//	var method = new MethodDefinition("OnAssemblyResolve", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static, ImportType<Assembly>(assemblyDef));
		//	method.Parameters.Add(new ParameterDefinition(ImportType<object>(assemblyDef)));
		//	method.Parameters.Add(new ParameterDefinition(ImportType<ResolveEventArgs>(assemblyDef)));
		//	method.Body.Variables.Add(new VariableDefinition("assemblyName", ImportType<String>(assemblyDef)));
		//	method.Body.Variables.Add(new VariableDefinition("inStream", ImportType<Stream>(assemblyDef)));
		//	method.Body.Variables.Add(new VariableDefinition("buf", ImportType<byte[]>(assemblyDef)));
		//	method.Body.Variables.Add(new VariableDefinition("outStream", ImportType<Stream>(assemblyDef)));
		//	method.Body.Variables.Add(new VariableDefinition("bytesRead", ImportType<Int32>(assemblyDef)));
		//	method.Body.InitLocals = true;

		//	il = method.Body.GetILProcessor();
		//	var callLoadAssemblyFromStream = il.Create(OpCodes.Ldloc_1);

		//	il.Emit(OpCodes.Ldstr, "Resolving(Mixed): ");
		//	il.Emit(OpCodes.Ldarg_1);
		//	il.Emit(OpCodes.Callvirt, ImportMethod<ResolveEventArgs>(assemblyDef, "get_Name"));
		//	il.Emit(OpCodes.Call, ImportMethod<String>(assemblyDef, "Concat", typeof(String), typeof(String)));
		//	il.Emit(OpCodes.Call, ImportMethod(assemblyDef, typeof(Console), "WriteLine", typeof(String)));

		//	il.Emit(OpCodes.Ldarg_1);
		//	il.Emit(OpCodes.Callvirt, ImportMethod<ResolveEventArgs>(assemblyDef, "get_Name"));
		//	il.Emit(OpCodes.Newobj, ImportCtor<AssemblyName>(assemblyDef, typeof(String)));
		//	il.Emit(OpCodes.Call, ImportMethod<AssemblyName>(assemblyDef, "get_Name"));
		//	il.Emit(OpCodes.Ldstr, ".dll.bz2");
		//	il.Emit(OpCodes.Call, ImportMethod<String>(assemblyDef, "Concat", typeof(String), typeof(String)));
		//	il.Emit(OpCodes.Stloc_0);
		//	il.Emit(OpCodes.Call, ImportMethod<Assembly>(assemblyDef, "GetExecutingAssembly"));
		//	il.Emit(OpCodes.Ldloc_0);
		//	il.Emit(OpCodes.Callvirt, ImportMethod<Assembly>(assemblyDef, "GetManifestResourceStream", typeof(String)));
		//	il.Emit(OpCodes.Stloc_1);
		//	il.Emit(OpCodes.Ldloc_0);
		//	il.Emit(OpCodes.Ldstr, "Ionic.BZip2.dll.bz2");
		//	il.Emit(OpCodes.Call, ImportMethod<String>(assemblyDef, "op_Equality", typeof(String), typeof(String)));
		//	il.Emit(OpCodes.Brtrue_S, callLoadAssemblyFromStream);

		//	il.Emit(OpCodes.Ldloc_1);
		//	il.Emit(OpCodes.Call, loadAssemblyFromCompressedStream);
		//	il.Emit(OpCodes.Ret);
			
		//	il.Append(callLoadAssemblyFromStream);
		//	il.Emit(OpCodes.Call, loadAssemblyFromStream);
		//	il.Emit(OpCodes.Ret);


		//	return new List<MethodDefinition>() { method, loadAssemblyFromCompressedStream, loadAssemblyFromStream };
		//	}
		}
	}
