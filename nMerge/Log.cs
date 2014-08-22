namespace Omega.App.nMerge
	{
	using System;
	using System.IO;

	internal class Log
		{
		public const Int32 LevelNone = -1;
		public const Int32 LevelError = 0;
		public const Int32 LevelInfo = 1;
		public const Int32 LevelVerbose = 2;

		public static Int32 LogLevel { get; set; }

		private static void Write(Int32 lvl, TextWriter stream, String msg)
			{
			if(lvl < LogLevel)
				return;
			
			stream.WriteLine(msg);
			}

		public static void Error(String msg) { Write(LevelError, Console.Error, msg); }
		public static void Info(String msg) { Write(LevelInfo, Console.Out, msg); }
		public static void Verbose(String msg) { Write(LevelVerbose, Console.Out, msg); }
		}
	}
