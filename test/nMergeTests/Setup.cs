using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace nMergeTests
	{
	public class Setup
		{
		public static readonly String ApplicationName = @"Application.exe";
		public static readonly String TempDir = @".\Temp";

		public static String GetTestTempDir(int lvl = 1)
			{
				var st = new StackTrace();
				StackFrame sf = st.GetFrame(lvl);

				return Path.Combine(TempDir, sf.GetMethod().Name);
			}
		}
	}
