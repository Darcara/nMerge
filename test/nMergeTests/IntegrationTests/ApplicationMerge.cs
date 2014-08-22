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
		public static String GetTestMergeResultFileName()
			{
			return Path.Combine(Setup.GetTestTempDir(3), Setup.ApplicationName + ".merged.exe");
			}

		public static void TestIntegration(params String[] args)
			{
			String testTempDir = Setup.GetTestTempDir(2);
			Directory.CreateDirectory(testTempDir);

			var arguments = new List<string>();
			arguments.AddRange(args);
			arguments.Add(@"/in=./" + Setup.ApplicationName);
			arguments.Add("/out=" + GetTestMergeResultFileName());
			arguments.Add("/vv");

			Program.Main(arguments.ToArray());
			Assert.IsTrue(File.Exists(GetTestMergeResultFileName()), String.Format((string) "Merging failed: Assembly '{0}' not created", (object) GetTestMergeResultFileName()));

			Debug.WriteLine("----- Merge completed -----");
			Debug.WriteLine(String.Format("----- Filesize: {0} -----", (new FileInfo(GetTestMergeResultFileName()).Length)));
			}

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
			Setup.TestMergeResult("123", "HelloWorld");
			}
		[Test]
		public void AppFull()
			{
			TestIntegration(@"/m=Application.Program::Main", @"/lib=.\MainLibrary.dll,.\RequiredLibrary.dll,SilentRequiredLibrary.dll");
			Setup.TestMergeResult("123", "HelloWorld");
			}
		[Test]
		public void AppWithLibraries()
			{
			TestIntegration(@"/lib=.\MainLibrary.dll,.\RequiredLibrary.dll");
			Setup.TestMergeResult("123", "HelloWorld");
			}
		[Test]
		public void AppWithEntryPoint()
			{
			TestIntegration(@"/m=Application.Program::AnotherEntryPoint");
			Setup.TestMergeResult("123", "HelloWorld", "MoreStrings");
			}
		[Test]
		public void AppWithParamsEntryPoint()
			{
			TestIntegration(@"/m=Application.Program::ParamsEntryPoint");
			Setup.TestMergeResult("123", "HelloWorld", "MoreStrings", "AndOneMoreString");
			}

		[Test]
		public void AppOnlyCompressed()
			{
			TestIntegration(@"/zip");
			Setup.TestMergeResult("123", "HelloWorld");
			}
		[Test]
		public void AppFullCompressed()
			{
			TestIntegration(@"/m=Application.Program::Main", @"/lib=.\MainLibrary.dll,.\RequiredLibrary.dll,SilentRequiredLibrary.dll,Mono.Cecil.dll", @"/zip");
			Setup.TestMergeResult("123", "HelloWorld");
			}
		[Test]
		public void AppWithEntryPointCompressed()
			{
			TestIntegration(@"/m=Application.Program::AnotherEntryPoint", @"/zip");
			Setup.TestMergeResult("123", "HelloWorld", "MoreStrings");
			}
		[Test]
		public void AppOnlyCompressedNoZipLib()
			{
			TestIntegration(@"/zip", @"/noziplib");
			File.Copy("Ionic.BZip2.dll", Path.Combine(Setup.GetTestTempDir(), "Ionic.BZip2.dll"));
			Setup.TestMergeResult("123", "HelloWorld");
			}
	
		}
	}
