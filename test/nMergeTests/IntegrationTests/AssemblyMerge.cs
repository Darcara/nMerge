using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Omega.App.nMerge;

namespace nMergeTests.IntegrationTests
	{
	[TestFixture]
	public class AssemblyMerge
		{

		public static String GetTestMergeResultFileName()
			{
			return Path.Combine(Setup.GetTestTempDir(3), Setup.AssemblyName);
			}

		public static String TestIntegration(params String[] args)
			{
			String testTempDir = Setup.GetTestTempDir(2);
			Directory.CreateDirectory(testTempDir);

			var arguments = new List<String>();
			arguments.AddRange(args);
			arguments.Add(@"/in=./" + Setup.AssemblyName);
			arguments.Add("/out=" + GetTestMergeResultFileName());

			Program.Main(arguments.ToArray());
			Assert.IsTrue(File.Exists(GetTestMergeResultFileName()), String.Format("Merging failed: Assembly '{0}' not created", GetTestMergeResultFileName()));

			Debug.WriteLine("----- Merge completed -----");
			Debug.WriteLine(String.Format("----- Filesize: {0} -----", (new FileInfo(GetTestMergeResultFileName()).Length)));

			return GetTestMergeResultFileName();
			}

		[TestFixtureSetUp]
		public void Cleanup()
			{
			if(Directory.Exists(Setup.TempDir))
				Directory.Delete(Setup.TempDir, true);
			}


		[Test]
		public void Assembly()
			{
			TestIntegration(@"/lib=SilentRequiredLibrary.dll");
			Program.Main("/in=" + Setup.ApplicationName, "/out=" + Path.Combine(Setup.GetTestTempDir(), Setup.ApplicationName) + ".merged.exe", "/lib=", "/m=Application.Program::SilentEntryPoint");
			File.Copy("./RequiredLibrary.dll", Path.Combine(Setup.GetTestTempDir(), "RequiredLibrary.dll"), true);

			Setup.TestMergeResult("123", "HelloWorld", "MoreStrings");
			}
		[Test]
		public void AssemblyZip()
			{
			TestIntegration(@"/lib=SilentRequiredLibrary.dll", @"/zip");
			Program.Main("/in=" + Setup.ApplicationName, "/out=" + Path.Combine(Setup.GetTestTempDir(), Setup.ApplicationName) + ".merged.exe", "/lib=", "/m=Application.Program::SilentEntryPoint");
			File.Copy("./RequiredLibrary.dll", Path.Combine(Setup.GetTestTempDir(), "RequiredLibrary.dll"), true);

			Setup.TestMergeResult("123", "HelloWorld", "MoreStrings");
			}

		[Test]
		public void AppWithAssembly()
			{
			String result = TestIntegration(@"/lib=SilentRequiredLibrary.dll");
			Program.Main("/in=" + Setup.ApplicationName, "/out=" + Path.Combine(Setup.GetTestTempDir(), Setup.ApplicationName) + ".merged.exe", "/lib=RequiredLibrary.dll," + result, "/m=Application.Program::SilentEntryPoint");

			File.Delete(result);

			Setup.TestMergeResult("123", "HelloWorld", "MoreStrings");
			}
		[Test]
		public void AppWithAssemblyZip()
			{
			String result = TestIntegration(@"/lib=SilentRequiredLibrary.dll", @"/zip");
			Program.Main("/in=" + Setup.ApplicationName, "/out=" + Path.Combine(Setup.GetTestTempDir(), Setup.ApplicationName) + ".merged.exe", "/lib=RequiredLibrary.dll," + result, "/m=Application.Program::SilentEntryPoint");

			File.Delete(result);

			Setup.TestMergeResult("123", "HelloWorld", "MoreStrings");
			}
		[Test]
		public void AppZipWithAssemblyZip()
			{
			String result = TestIntegration(@"/lib=SilentRequiredLibrary.dll", @"/zip");
			Program.Main("/in=" + Setup.ApplicationName, "/out=" + Path.Combine(Setup.GetTestTempDir(), Setup.ApplicationName) + ".merged.exe", "/lib=RequiredLibrary.dll," + result, "/m=Application.Program::SilentEntryPoint", @"/zip");

			File.Delete(result);

			Setup.TestMergeResult("123", "HelloWorld", "MoreStrings");
			}
		[Test]
		public void AppZipWithAssemblyZipNoZipLib()
			{
			String result = TestIntegration(@"/lib=SilentRequiredLibrary.dll", @"/zip", @"/noziplib");
			Program.Main("/in=" + Setup.ApplicationName, "/out=" + Path.Combine(Setup.GetTestTempDir(), Setup.ApplicationName) + ".merged.exe", "/lib=RequiredLibrary.dll," + result, "/m=Application.Program::SilentEntryPoint", @"/zip");

			File.Delete(result);

			Setup.TestMergeResult("123", "HelloWorld", "MoreStrings");
			}

		}
	}
