using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using CommandLine;
using CppAst;
using PeNet;

namespace DLLHeaderParser
{
    class Program
    {
        static void RunOptions(CommandLineOptions opts)
        {
            //handle options
            if (opts.Verbose)
            {
                Console.WriteLine("parsing with the following arguments:");
                /* iterate through the CommandLineOptions fields*/
                foreach (var argument in typeof(CommandLineOptions).GetProperties())
                {
                    if (argument.PropertyType.IsArray || argument.PropertyType.IsEquivalentTo(typeof(IEnumerable<string>)))
                    {
                        var array = ((IEnumerable)argument.GetValue(opts)).Cast<object>().ToArray();
                        foreach (object ob in array)
                        {
                            Console.WriteLine("{0} = {1}", argument.Name, ob.ToString());
                        }
                    }
                    else
                    {
                        Console.WriteLine("{0} = {1}", argument.Name, argument.GetValue(opts));
                    }
                }
            }
            
        }
        static void HandleParseError(IEnumerable<Error> errs)
        {
            //handle errors
            Console.WriteLine("Error in parsing arguments!");
            Environment.Exit(-1);
        }

        static void Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(RunOptions)
                .WithNotParsed(HandleParseError);
            Parsed<CommandLineOptions> cmdoptions = (Parsed<CommandLineOptions>)result;

            //redirect Console.Out to log file if instructed so
            if (cmdoptions.Value.writeLog != null) {
                FileStream filestream = new FileStream(cmdoptions.Value.writeLog , FileMode.Create);
                var streamwriter = new StreamWriter(filestream);
                streamwriter.AutoFlush = true;
                Console.SetOut(streamwriter);
            }
            /* ================== 1- PARSE INPUT ARGUMENTS ===================== */
            /* ===== Forward cmdoptions to parserOptions ==== */
            var parserOptions = new CppParserOptions();

            /* Compiler defines */
            parserOptions.Defines.AddRange(cmdoptions.Value.CompilerDefines);

            /* additinal arguments */
            parserOptions.AdditionalArguments.AddRange(cmdoptions.Value.AdditionalArguments);

            /* DLL include folders */
            parserOptions.IncludeFolders.AddRange(cmdoptions.Value.IncludeFolders);

            /* Compiler include folders */
            parserOptions.SystemIncludeFolders.AddRange(cmdoptions.Value.SystemIncludeFolders);

            /* disable parsing system includes */
            parserOptions.ParseSystemIncludes = false;

            /* enable parsing macros? */
            parserOptions.ParseMacros = cmdoptions.Value.ParseMacros;

            /* enable parsing attributes? */
            parserOptions.ParseAttributes = cmdoptions.Value.ParseAttributes;

            /* parse files as c++? (usually false does not work for C)*/
            parserOptions.ParseAsCpp = (bool)cmdoptions.Value.ParseAsCpp;
            
            /* disable autosquashing typedefs as we need the typedefs intact*/
            parserOptions.AutoSquashTypedef = false;

            /* add all input files and folders */
            List<string> mainfiles = new List<string>();
            //add files from folders
            foreach (var dir in cmdoptions.Value.MainFolders.ToList()) {
                mainfiles.AddRange(Directory.EnumerateFiles(dir,"*.h"));
            }
            //add direct files
            mainfiles.AddRange(cmdoptions.Value.MainFiles.ToList());
            
            //remove files from the exclusion list if any
            foreach(var fileex in cmdoptions.Value.MainfilesEx)
            {
                if (!mainfiles.Remove(fileex))
                {   
                    Console.WriteLine(fileex + " was not found/excluded from the compilation");
                }
                else {
                    Console.WriteLine(fileex + " excluded from the compilation!");
                }
            }

            /* ================== 1.1- PARSE THE FILES ===================== */
            CppCompilation parserResult = CppParser.ParseFiles(mainfiles, parserOptions);

            /*print input text to parser in case of verbose */
            if (cmdoptions.Value.Verbose) {
                Console.WriteLine();
                Console.WriteLine("List of input text to parser:");
                Console.WriteLine(parserResult.InputText);
            }

            if (parserResult.HasErrors) {
                Console.WriteLine("List of Errors:");
                foreach ( var msg in parserResult.Diagnostics.Messages) {
                    if (msg.Type == CppLogMessageType.Error) {
                        Console.WriteLine("Error:{0}:{1}",msg.Location,msg.Text);
                    } 
                }
                Console.WriteLine();
            }

            /*print parser warnings in case of verbose */
            if (cmdoptions.Value.Verbose)
            {
                Console.WriteLine("List of Warnings:");
                foreach (var msg in parserResult.Diagnostics.Messages)
                {
                    if (msg.Type == CppLogMessageType.Warning)
                    {
                        Console.WriteLine("Warning:{0}:{1}", msg.Location, msg.Text);
                    }
                }
                Console.WriteLine();
            }

            /* ================== 2- READ INPUT DLL TO GET FUNCTIONS ===================== */
            /* get dll functions */
            PeFile inputdll = null;
            try
            {
                inputdll = new PeNet.PeFile(cmdoptions.Value.InputDLL);
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine("Error in opening the DLL file");
                Environment.Exit(-1);
            }

            if (parserResult.HasErrors) {
                Console.WriteLine("Parser reported errors. Aborting...");
                Environment.Exit(-1);
            }

            /* Extract Dll functions from file and save them for mapping later */
            List<string> dllfunctions = inputdll.ExportedFunctions.ToList().ConvertAll(x => x.Name);

            if (cmdoptions.Value.Verbose) {
                Console.WriteLine(Environment.NewLine + "Found following functions in DLL:");
                dllfunctions.ForEach(Console.WriteLine);
            }
            
            /* ================== 2.1- Match DLL functions with parser output ===================== */
            //filter , intersect the dllfunctions with parser funtions -> list of functions we need with their type
            var matchingFunctions = parserResult.Functions.Where(x => dllfunctions.Contains(x.Name)).ToList();
            if (cmdoptions.Value.Verbose)
            {
                Console.WriteLine(Environment.NewLine + "Matching items:");
                matchingFunctions.ForEach(x => Console.WriteLine(x.Name));
            }
            /* issue a warning for the functions that are not found in header file */
            Console.WriteLine(Environment.NewLine + "Warning ! Missing Items:");
            var nonMatchingItems = dllfunctions.Where(function => !matchingFunctions.Exists(matchingItem => function.Equals(matchingItem.Name)));
            nonMatchingItems.ToList().ForEach(Console.WriteLine);
            Console.WriteLine();

            /* ================== 3- Build results ===================== */
            /* generate results for WinApiOverride */
            if (cmdoptions.Value.genWinApiOverride) {
                WinApiOverride.genWinApiOverride(parserResult, matchingFunctions, cmdoptions.Value.WinApiOverrideFolder, cmdoptions.Value.InputDLL);
            }
            /* generate results for ApiMonitor */
            if (cmdoptions.Value.genApiMonitor)
            {
                /* now we need to build the xml for the matching functions */
                XmlMaker.genResults(parserResult, matchingFunctions);
            }

            Console.WriteLine("Parsing Finished successfully!");

        }
    }
}
