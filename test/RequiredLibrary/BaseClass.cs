using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RequiredLibrary
	{
	public class BaseClass
		{
		private readonly int _someNumber;

		public BaseClass(Int32 someNumber)
			{
			_someNumber = someNumber;
			}

		public Int32 GetNumber()
			{
			return _someNumber;
			}
		}
	}
