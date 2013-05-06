using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using System.Linq;
using Omega.App.nMerge;

namespace nMergeTests.IntegrationTests
	{
	[TestFixture]
	public class ApplicationMerge
		{

		private static String ExecuteHelper(String commandline, params String[] args)
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

		private static String GetTestMergeResultFileName()
			{
			return Path.Combine(Setup.GetTestTempDir(3), Setup.ApplicationName + ".merged.exe");
			}

		private static void TestIntegration(params String[] args)
			{
			String testTempDir = Setup.GetTestTempDir(2);
			Directory.CreateDirectory(testTempDir);

			var arguments = new List<String>();
			arguments.AddRange(args);
			arguments.Add(@"/in=./" + Setup.ApplicationName);
			arguments.Add("/out=" + GetTestMergeResultFileName());

			Program.Main(arguments.ToArray());
			Assert.IsTrue(File.Exists(GetTestMergeResultFileName()), String.Format("Merging failed: Assembly '{0}' not created", GetTestMergeResultFileName()));

			Debug.WriteLine("----- Merge completed -----");
			Debug.WriteLine(String.Format("----- Filesize: {0} -----", (new FileInfo(GetTestMergeResultFileName()).Length)));
			}

		private static void TestMergeResult(params String[] args)
			{
			String result = args.Skip(1).Aggregate("", (current, s) => current + s);
			result += ":" + args[0];

			String stdout = ExecuteHelper(GetTestMergeResultFileName(), args);
			Debug.WriteLine(stdout);
			Assert.That(stdout.TrimEnd(), Is.StringEnding(result));

			}

		//[TestFixtureTearDown]
		[TestFixtureSetUp]
		public void Cleanup()
			{
			if(Directory.Exists(Setup.TempDir))
				Directory.Delete(Setup.TempDir, true);
			}

		[Test]
		public void AppOnly()
			{
			TestIntegration();
			TestMergeResult("123", "HelloWorld");
			}
		[Test]
		public void AppFull()
			{
			TestIntegration(@"/m=Application.Program::Main", @"/lib=.\MainLibrary.dll,.\RequiredLibrary.dll");
			TestMergeResult("123", "HelloWorld");
			}
		[Test]
		public void AppWithLibraries()
			{
			TestIntegration(@"/lib=.\MainLibrary.dll,.\RequiredLibrary.dll");
			TestMergeResult("123", "HelloWorld");
			}
		[Test]
		public void AppWithEntryPoint()
			{
			TestIntegration(@"/m=Application.Program::AnotherEntryPoint");
			TestMergeResult("123", "HelloWorld", "MoreStrings");
			}
		[Test]
		public void AppWithParamsEntryPoint()
			{
			TestIntegration(@"/m=Application.Program::ParamsEntryPoint");
			TestMergeResult("123", "HelloWorld", "MoreStrings", "AndOneMoreString");
			}

		[Test]
		public void AppOnlyCompressed()
			{
			TestIntegration(@"/zip");
			TestMergeResult("123", "HelloWorld");
			}
		[Test]
		public void AppFullCompressed()
			{
			TestIntegration(@"/m=Application.Program::Main", @"/lib=.\MainLibrary.dll,.\RequiredLibrary.dll", @"/zip");
			TestMergeResult("123", "HelloWorld");
			}
		[Test]
		public void AppWithEntryPointCompressed()
			{
			TestIntegration(@"/m=Application.Program::AnotherEntryPoint", @"/zip");
			TestMergeResult("123", "HelloWorld", "MoreStrings");

			}
		}
	}
