
namespace Omega.App.nMerge
	{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Omega.App.nMerge.Options;

	internal class CommandLine
		{
		public String OutputFile { get; private set; }
		public String InputFile { get; private set; }
		public String MethodRaw { get; private set; }
		public String LibrariesRaw { get; private set; }
		public Boolean Compress { get; private set; }
		public List<String> Libraries { get; private set; }
		public MergeType MergeType { get; private set; }
		public Int32 LogLevel { get; set; }

		/// <summary>
		/// Parses the command line arguments into a proper format.
		/// </summary>
		/// <param name="args">The command line arguments.</param>
		/// <returns>A new instance of <see cref="CommandLine"/>.</returns>
		public CommandLine(IEnumerable<string> args)
			{
			LogLevel = 0;

			var cli = new OptionSet
									{
										{"h|help|?", v => { Console.WriteLine(Usage); Environment.Exit(0); }},
										{"out=", v => OutputFile = v},
										{"in=", v => InputFile = v},
										{"m=", v => MethodRaw = v},
										{"lib=", v => LibrariesRaw = v},
										{"zip", v => Compress = true},
										{"v", v => LogLevel++},
									};


			cli.Parse(args);

			if (String.IsNullOrWhiteSpace(OutputFile))
				throw new Exception("out must be set");
			if (String.IsNullOrWhiteSpace(InputFile))
				throw new Exception("in must be set");
			if (!File.Exists(InputFile))
				throw new FileNotFoundException("input file does not exist: " + Path.GetFullPath(InputFile), InputFile);

			if (String.Equals(Path.GetExtension(InputFile), ".dll", StringComparison.InvariantCultureIgnoreCase))
				MergeType = MergeType.Library;
			else if (String.Equals(Path.GetExtension(InputFile), ".exe", StringComparison.InvariantCultureIgnoreCase))
				MergeType = MergeType.Application;
			else
				throw new Exception("Unable to recognize type of input file. Must end in '.dll' or '.exe'");

			var applicationPath = Path.GetDirectoryName(InputFile);
			if (LibrariesRaw == null && applicationPath != null)
				LibrariesRaw = Path.Combine(applicationPath, "*.dll");

			Libraries = ParseLibraries(LibrariesRaw, InputFile);
			}

		/// <summary>
		/// <para>Parses the raw library string from the command line into a list of paths-to-libraries.</para>
		/// <para>The main assembly will not be added to the resulting list.</para>
		/// <para>No check is being done to verify that library files are proper C# assemblies.</para>
		/// </summary>
		/// <param name="rawLibraryString">The raw string of the command line 'lib' argument.</param>
		/// <param name="pathToMainAssembly">The main assembly to be merged. It will not be included in the result.</param>
		/// <returns>A list of paths to existing library files.</returns>
		private static List<String> ParseLibraries(String rawLibraryString, String pathToMainAssembly)
			{
			if (String.IsNullOrWhiteSpace(rawLibraryString))
				return null;

			var rawSplit = rawLibraryString.Split(',');

			var libraryFiles = new List<String>();
			foreach (var rawLibraryFile in rawSplit.Where(rawLibraryFile => !String.IsNullOrWhiteSpace(rawLibraryFile)))
				{
				var rawLibraryDirectory = Path.GetDirectoryName(rawLibraryFile);
				var searchPattern = Path.GetFileName(rawLibraryFile);
				var files = Directory.GetFiles(String.IsNullOrWhiteSpace(rawLibraryDirectory) ? "./" : rawLibraryDirectory, searchPattern ?? "*.dll");

				if (files.Length == 0)
					{
					Console.WriteLine("No files found for: " + rawLibraryFile);
					continue;
					}

				foreach (var file in files.Where(file => file != pathToMainAssembly))
					{
					if (!File.Exists(file))
						throw new FileNotFoundException("Library not found: " + file, file);

					Console.WriteLine("Found library " + file);
					libraryFiles.Add(file);
					}

				}
			return libraryFiles;
			}

		public const String Usage = @"nmerge.exe /out=<outputFile> /in=<application> [/lib=<library>,<library>] [/zip] [/m=<method>]

/out=<outputFile>   The output file

/in=<application>   The application assembly

/m=<method>         Optional.
                    The fully qualified method name
                    Method must be public static void(String[] args)
                    Defaults to the entry point for executeable
                    e.g: Some.Namespace.Class::Method

/lib=<library>      Optional.
                    A ',' (comma) - separated list of assemblies to include
                    Wildcards are permitted
                    If not specified, all *.dll files in the same directory
                    as the application are assumed

/zip                Optional.
                    If specified all assemblies will be compressed.
                    This has a slight performance cost, but may drastically
                    reduce filesize

/?, /h, /help       Prints this helpful screen

Additional information on the github repository:
	https://github.com/Darcara/nMerge";
		}
	}
