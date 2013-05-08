using System;
using RequiredLibrary;
using SilentRequiredLibrary;

namespace MainLibrary
	{
	public class DerivedClass : BaseClass
		{
		private readonly string _someString;

		public DerivedClass(Int32 someNumber, String someString) : base(someNumber)
			{
			_someString = someString;
			}

		public String GetTheString()
			{
			return _someString;
			}

		public String GetStringFromSilentLibrary(String s)
			{
			var sl = new SilentClass(s, _someString);
			return sl.ComputeOutput();
			}

		public static void UnusedMethod()
			{
			Console.WriteLine("This 'MainLibrary.DerivedClass::UnusedMethod' should never be displayed.");
			}
		}
	}
