using System;
using System.IO;
using System.Reflection;

// ReSharper disable CheckNamespace
public class MinimalWrapper
	{
	public static void MinimalWrapperMain()
		{
		AppDomain.CurrentDomain.AssemblyResolve += ResolveEmbeddedAssembly;
		}
	private static Assembly ResolveEmbeddedAssembly(object sender, ResolveEventArgs args)
		{
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		return LoadAssemblyFromStream(executingAssembly.GetManifestResourceStream(args.Name + ".dll.bz2"));
		}
	private static Assembly LoadAssemblyFromStream(Stream _s)
		{
		byte[] block = null;
		var buf = new byte[1024];
		using(var outStream = new MemoryStream())
			{
			int bytesRead = 0;
			while((bytesRead = _s.Read(buf, 0, 1024)) > 0)
				{
				outStream.Write(buf, 0, bytesRead);
				}
			block = outStream.ToArray();
			}

		return Assembly.Load(block);
		}


	public static void SuperMinimal()
		{
		AppDomain.CurrentDomain.AssemblyResolve += MinmalResolve;
		}
	private static Assembly MinmalResolve(object sender, ResolveEventArgs args)
		{
		Console.WriteLine("Resolving(Minimal): " + args.Name);
		Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream((new AssemblyName(args.Name)).Name + ".dll.bz2");

		var buf = new byte[s.Length];
		s.Read(buf, 0, (int)s.Length);
		return Assembly.Load(buf);
		}


	public static void CompressedMinimal()
		{
		AppDomain.CurrentDomain.AssemblyResolve += CompressedMinmalResolve;
		}
	private static Assembly CompressedMinmalResolve(object sender, ResolveEventArgs args)
		{
		var assemblyName = (new AssemblyName(args.Name)).Name + ".dll.bz2";
		Console.WriteLine("Resolving: " + assemblyName);
		var inStream = new Ionic.BZip2.BZip2InputStream(Assembly.GetExecutingAssembly().GetManifestResourceStream(assemblyName));
		var buf = new byte[1024];
		var outStream = new MemoryStream();
		int bytesRead;
		while((bytesRead = inStream.Read(buf, 0, 1024)) > 0)
			outStream.Write(buf, 0, bytesRead);
		return Assembly.Load(outStream.ToArray());
		}

	public static void MixedMinimal()
		{
		AppDomain.CurrentDomain.AssemblyResolve += MixedMinmalResolve;
		}
	private static Assembly MixedMinmalResolve(object sender, ResolveEventArgs args)
		{
		Console.WriteLine("Resolving(Mixed): " + args.Name);

		var assemblyName = (new AssemblyName(args.Name)).Name + ".dll.bz2";
		Stream inStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(assemblyName);
		return assemblyName == "Ionic.BZip2.dll.bz2" ? MixedMinimalLoadAssembly(inStream) : MixedMinimalLoadCompressedAssembly(inStream);
		}
	private static Assembly MixedMinimalLoadCompressedAssembly(Stream inStream)
		{
		return MixedMinimalLoadAssembly(new Ionic.BZip2.BZip2InputStream(inStream));
		}
	private static Assembly MixedMinimalLoadAssembly(Stream inStream)
		{
		var buf = new byte[1024];
		var outStream = new MemoryStream();
		int bytesRead;
		while((bytesRead = inStream.Read(buf, 0, 1024)) > 0)
			outStream.Write(buf, 0, bytesRead);
		return Assembly.Load(outStream.ToArray());		
		}

	}


// ReSharper restore CheckNamespace
