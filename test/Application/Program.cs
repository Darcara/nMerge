using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MainLibrary;

namespace Application
	{
	public class Program
		{
		public static void Main(string[] args)
			{
			if(args.Length != 2)
				{
				Console.WriteLine("Usage: application.exe <Int32> <String>\nGot: " + String.Join(", ", args));
				return;
				}

			var dc = new DerivedClass(Int32.Parse(args[0]), args[1]);

			Console.WriteLine(dc.GetTheString() + ":" + dc.GetNumber());
			}

		public static void AnotherEntryPoint(String[] args)
			{
			if(args.Length != 3)
				{
				Console.WriteLine("Usage: application.exe <Int32> <String> <String>\nGot: " + String.Join(", ", args));
				return;
				}

			var dc = new DerivedClass(Int32.Parse(args[0]), args[1] + args[2]);

			Console.WriteLine(dc.GetTheString() + ":" + dc.GetNumber());
			}

		public static void ParamsEntryPoint(params String[] args)
			{
			if(args.Length != 4)
				{
				Console.WriteLine("Usage: application.exe <Int32> <String> <String> <String>\nGot: " + String.Join(", ", args));
				return;
				}

			var dc = new DerivedClass(Int32.Parse(args[0]), args[1] + args[2] + args[3]);

			Console.WriteLine(dc.GetTheString() + ":" + dc.GetNumber());
			}
		}
	}
