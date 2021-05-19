# DLLHeaderParser

This simple tool helps building the files required for Monitoring of custom DLLs in WinApiOverride and Rohitabs WinApi Monitor.

you need to have the DLLs and the header files used to build them. DLLHeaderParser uses CppAst internally to parse the Header files as a compilation
and produces the required output for both tools.

# How To use:
read info.txt
<br>
see the VS project debug arguments. for each DLL there's a separate debug profile that parses that DLL.

# Known Limitations:
read info.txt

Rohitab Api Monitor XML generation is not complete and only the function XML is generated. no output yet
