using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using NUnit.Framework;
using Omega.App.nMerge;
using nMergeTests.IntegrationTests;

namespace nMergeTests
	{
	public class Setup
		{
		public static readonly String ApplicationName = @"Application.exe";
		public static readonly String AssemblyName = @"MainLibrary.dll";
		public static readonly String TempDir = @".\Temp";

		public static String GetTestTempDir(int lvl = 1)
			{
				var st = new StackTrace();
				StackFrame sf = st.GetFrame(lvl);

				return Path.Combine(TempDir, sf.GetMethod().Name);
			}

		public static String ExecuteHelper(String commandline, params String[] args)
			{
			var psi = new ProcessStartInfo(Path.GetFullPath(commandline), args == null ? null : String.Join(" ", args))
			          	{
			          		RedirectStandardOutput = true,
			          		UseShellExecute = false,
			          		CreateNoWindow = true

			          	};
			Process p = Process.Start(psi);
			Assert.NotNull(p);

			return p.StandardOutput.ReadToEnd();
			}





		public static void TestMergeResult(params String[] args)
			{
			String result = args.Skip(1).Aggregate("", (current, s) => current + s);
			result += ":" + args[0];

			String stdout = ExecuteHelper(ApplicationMerge.GetTestMergeResultFileName(), args);
			Debug.WriteLine(stdout);
			Assert.That(stdout.TrimEnd(), Is.StringEnding(result));

			}
		}
	}

