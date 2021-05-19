using CppAst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DLLHeaderParser
{
    class XmlMaker
    {
        /// <summary>
        /// generate result for Rohitab Api Monitor
        /// </summary>
        /// <param name="parserResult">parser result from CppAst</param>
        /// <param name="matchingFunctions">matching functions from DLL</param>
        public static void genResults(CppCompilation parserResult, List<CppFunction> matchingFunctions) {
            XElement x = new XElement("ApiMonitor");
            x.Add(
                new XElement("Include", new XAttribute("FileName", @"Headers\common.h.xml"))
                );


            var funcs = XmlMaker.makeFunctions(matchingFunctions);


            var variables = XmlMaker.makeVariables(parserResult);

            Console.WriteLine(x);
        }

        public static List<XElement> makeFunctions(List<CppFunction> funcs) {
            /** 
             * each api has this format:
             * 
             * <Api Name="CredFindBestCredential" BothCharset="True">
             * <Param Type="LPCTSTR" Name="TargetName" />
             * <Param Type="DWORD" Name="Type" />
             * <Param Type="DWORD" Name="Flags" />
             * <Return Type="BOOL" />
             * </Api>
             */
            List<XElement> x = new List<XElement>();

            /*iterate over each api and make the xml of it*/
            foreach (var func in funcs) {
                XElement apiElement = new XElement("Api");
                apiElement.SetAttributeValue("Name", func.Name); /* set it's name in attribute */
                foreach (var param in func.Parameters) {  /* add it's parameters */
                    XElement parameter = new XElement("Param");
                    parameter.SetAttributeValue("Type",param.Type);
                    parameter.SetAttributeValue("Name", param.Name);
                    apiElement.Add(parameter);
                }
                /* add the return type */
                XElement returnType = new XElement("Return",new XAttribute("Type",func.ReturnType));
                apiElement.Add(returnType);
                /* add it to list */
                x.Add(apiElement);
            }
            
            //@todo find all attributes of the function
            //@todo iterate back to basic types and make them            
            return x;
        }

        public static XElement makeModule() {
            XElement x = null;
            /**
             * header is before module
             * 
             * each module has a 
             * 
             * 1- variable
             * 2- apis
             */
            return x;
        }

        public static List<XElement> makeVariables(CppCompilation comp) {
            /** 
             * we don't need interface , only module. declare variables that are used in each dll and import them 
             * to other modules if they cross reference
            */

            //classes , enums , Fields , typedefs with non-null source file
            List<XElement> classes = new List<XElement>();
            List<XElement> typedefs = new List<XElement>();
            List<XElement> enums = new List<XElement>();
            List<XElement> fields = new List<XElement>();

            List<XElement> result = new List<XElement>();


            /* process classes */
            foreach (var clazz in comp.Classes.Where(x=> x.SourceFile != null)) {
                /* clazz.ClassKind is either union or struct */
                //@Todo continue from here
            }


            return result;
        }
    }
}
