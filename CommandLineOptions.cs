using System.Collections.Generic;
using CommandLine;

namespace DLLHeaderParser
{
    class CommandLineOptions
    {
        [Option("dll", Required = true, HelpText = "DLL file to generate the results for")]
        public string InputDLL { get; set; }

        [Option("files", Group = "inputfiles" , HelpText = "Main files to be parsed")]
        public IEnumerable<string> MainFiles { get; set; }

        [Option("folders", Group = "inputfiles", HelpText = "Folders with files to be parsed")]
        public IEnumerable<string> MainFolders { get; set; }

        [Option("filesEx", HelpText = "files to be excluded from compilation")]
        public IEnumerable<string> MainfilesEx { get; set; }

        [Option('i', "include-folders", Required = false, HelpText = "Dll Header files to be parsed")]
        public IEnumerable<string> IncludeFolders { get; set; }

        [Option('s', "system-include-folders", Required = false, HelpText = "Compiler Header files to be parsed")]
        public IEnumerable<string> SystemIncludeFolders { get; set; }

        [Option('D', "defines", Required = false, HelpText = "Compiler Defines passed to the parser")]
        public IEnumerable<string> CompilerDefines { get; set; }

        [Option("additional-args", Required = false, HelpText = "additional arguments passed to the parser")]
        public IEnumerable<string> AdditionalArguments { get; set; }

        [Option("cpp", Default = true, HelpText = "Parse files as C++ (false is C)")]
        public bool? ParseAsCpp { get; set; }

        [Option("macros", Default = true, HelpText = "Parse macros inside files")]
        public bool ParseMacros { get; set; }

        [Option("attributes", Default = true, HelpText = "Parse attributes")]
        public bool ParseAttributes { get; set; }

        [Option('x', "xml",Default = @"./OutputApiMonitor/", HelpText = "Output XML file for ApiMonitor")]
        public string XmlFile { get; set; }
        
        [Option("winapifolder", Default = @"./OutputWinApi/", HelpText = "Output monitoring folder for WinApiOverride")]
        public string WinApiOverrideFolder { get; set; }

        [Option("apimonitor", HelpText = "generate output for Rohitab ApiMonitor")]
        public bool genApiMonitor { get; set; }

        [Option("winapioverride", HelpText = "generate output for WinApiOverride")]
        public bool genWinApiOverride { get; set; }

        [Option("log", HelpText = "write the logs to a file instead of the Console")]
        public string writeLog { get; set; }

        // Omitting long name, defaults to name of property, ie "--verbose"
        [Option(
          Default = false,
          HelpText = "Prints all warnings")]
        public bool Verbose { get; set; }

    }
}
