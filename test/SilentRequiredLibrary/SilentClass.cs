using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilentRequiredLibrary
	{
	public class SilentClass
		{
		private readonly string _s1;
		private readonly string _s2;

		public SilentClass(String s1, String s2)
			{
			_s1 = s1;
			_s2 = s2;
			}

		public String ComputeOutput()
			{
			return String.Format("{1}{0}", _s1, _s2);
			}

		private static void UnusedMethod()
			{
			Console.WriteLine("This SILENTCLASS-METHOD should never be displayed.");
			}
		}
	}