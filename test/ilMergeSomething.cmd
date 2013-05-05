REM ILMERGE
REM ILMerge\ILMerge.exe /log:log.txt /out:assembly.exe /target:exe source.exe Q:\path\to\dlls\*.dll /targetplatform:'v4,C:\Windows\Microsoft.NET\Framework64\v4.0.30319' /wildcards

REM ildasm --> ilasm makes a target.exe
REM can specify /FOLD for slightly smaller bytecode --> BUGs ?
REM ildasm /out=target.il /all /utf8 source.exe
REM ilasm /EXE /NOLOGO /RESOURCE=target.res /x64 /OPTIMIZE target.il