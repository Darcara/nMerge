using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

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
		return LoadAssemblyFromStream(executingAssembly.GetManifestResourceStream(args.Name + ".dll"));
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
		Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream((new AssemblyName(args.Name)).Name + ".dll");

		var buf = new byte[s.Length];
		s.Read(buf, 0, (int)s.Length);
		return Assembly.Load(buf);
		}
	}


// ReSharper restore CheckNamespace
