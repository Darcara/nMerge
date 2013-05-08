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

		public static void TestIntegration(params String[] args)
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

			Program.Main("/in=" + Setup.ApplicationName, "/out=" + Path.Combine(Setup.GetTestTempDir(2), Setup.ApplicationName) + ".merged.exe", "/lib=", "/m=Application.Program::SilentEntryPoint");
			//File.Copy(Setup.ApplicationName, Path.Combine(Path.GetDirectoryName(GetTestMergeResultFileName()), Setup.ApplicationName));
			File.Copy("./RequiredLibrary.dll", Path.Combine(Path.GetDirectoryName(GetTestMergeResultFileName()), "RequiredLibrary.dll"),true);
			//File.Copy("./SilentRequiredLibrary.dll", Path.Combine(Path.GetDirectoryName(GetTestMergeResultFileName()), "SilentRequiredLibrary.dll"), true);
			}

		//[TestFixtureSetUp]
		public void Cleanup()
			{
			if(Directory.Exists(Setup.TempDir))
				Directory.Delete(Setup.TempDir, true);
			}


		[Test]
		public void MergeAssembly()
			{
			TestIntegration(@"/lib=SilentRequiredLibrary.dll");

			Setup.TestMergeResult("123", "HelloWorld", "MoreStrings");
			}
		}
	}
