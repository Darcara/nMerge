using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Ionic.BZip2;

// ToDo: This should be dynamically read from the input file and substituded before compilation, using AssemblyDefinition.CustomAttributes
[assembly: AssemblyTitle("nMerge - MergedAssembly")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("nMerge - MergedAssembly")]
[assembly: AssemblyCopyright("Copyright © Omega 2013")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("8911ed25-0ad1-42c0-aecf-d70ef059bfbf")]
[assembly: AssemblyVersion("1.0.*")]


// ReSharper disable CheckNamespace
class Wrapper
	{
	private static String _type = "***TYPE***";
	private static String _method = "***METHOD***";

	public static void Main(String[] args)
		{
		try
			{
			AppDomain.CurrentDomain.AssemblyResolve += ResolveEmbeddedAssembly;
			var execAssembly = Assembly.GetExecutingAssembly();
			Console.WriteLine("Hello World! This is " + execAssembly);

			var resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
			Console.WriteLine("Listing embedded resources(" + resourceNames.Length + "):");
			foreach(var name in resourceNames)
				Console.WriteLine("  " + name);

			var type = Type.GetType(_type, false);
			if(type == null)
				{
				Console.WriteLine("Main type not found: " + _type);
				return;
				}
			Console.WriteLine("Found main class");

			var method = type.GetMethod(_method);
			if(method == null)
				{
				Console.WriteLine("Main method not found: " + _type + "::" + _method);
				return;
				}
			Console.WriteLine("Found main method");

			Console.WriteLine("Calling main method");
			method.Invoke(null, new Object[] {args});
			}
		catch(Exception e)
			{
			Console.WriteLine("An unhandled Exception occured: " + e);
			}
		finally
			{
#if DEBUG
			Console.ReadKey(false);
#endif
			}
		}

	private static Assembly ResolveEmbeddedAssembly(object sender, ResolveEventArgs args)
		{
		AssemblyName assName = new AssemblyName(args.Name);
		Console.WriteLine("Resolving assembly ("+assName.Name+"): " + args.Name);

		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		Stream _s = null;
		_s = executingAssembly.GetManifestResourceStream(assName.Name + ".dll" + ".bz2");
		if(_s == null)
			_s = executingAssembly.GetManifestResourceStream(assName.Name + ".exe" + ".bz2");

		if(_s == null)
			{
			Console.WriteLine("Assembly not found in resources");
			return null;
			}

		byte[] block = null;
		using(var inStream = new BZip2InputStream(_s))
			{
			var buf = new byte[1024];
			using(var outStream = new MemoryStream())
				{
				int bytesRead = 0;
				while((bytesRead = inStream.Read(buf, 0, 1024)) > 0)
					{
					outStream.Write(buf, 0, bytesRead);
					}
				block = outStream.ToArray();
				}
			}

		Assembly a2 = Assembly.Load(block);
		return a2;
		}
	}
// ReSharper restore CheckNamespace
