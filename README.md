nMerge
======

A small command line tool to merge and compress C# assemblies

Usage
=====

	nmerge.exe /out=<outputFile> /in=<application> [/lib=<library>,<library>] [/zip] [/m=<method>]
	
	/out=<outputFile>   The output file
	
	/in=<application>   The application assembly
	
	/m=<method>         Optional.
	                    The fully qualified method name
	                    Method must be public static void(String[] args)
	                    Defaults to the entry point for executeables
	                    e.g: Some.Namespace.Program::Main
	
	/lib=<library>      Optional.
	                    A ','-separated list of assemblies to include
	                    Wildcards are permitted
	                    If not specified, all *.dll files in the same directory
	                    as the application are assumed
	
	/zip                Optional.
	                    If specified all assemblies will be compressed.
	                    This has a slight performance cost, but may drastically
	                    reduce filesize. Zip-merged assembly will always be > 56k
	
	/?, /h, /help       Prints this helpful screen
	
	Additional information on this website:
		https://github.com/Darcara/nMerge
  
Caveats
=======

* If merging applications, the output file name must be different from the input assembly.
* Assembly information and icons are not transferred to the merged assembly, yet.
* If using compression, an uncompressed version of Ionic.Bzip2.dll(roughly 56k) must be merged into the target. For very small assemblies that can mean a net increase in file size. Merged assembly will always be bigger than 56k.  
 
