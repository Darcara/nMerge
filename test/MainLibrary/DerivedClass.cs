using System;
using RequiredLibrary;

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
		}
	}
