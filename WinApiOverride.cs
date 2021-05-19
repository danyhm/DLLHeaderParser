using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CppAst;

namespace DLLHeaderParser
{
    class WinApiOverride
    {
        /// <summary>
        /// generate results for WinApiOverride
        /// </summary>
        /// <param name="parserResult">parser result from CppAst</param>
        /// <param name="matchingFunctions">matching functions list</param>
        /// <param name="resultFolder">result folder where to store the results</param>
        /// <param name="libraryname">name of the DLL file being parsed</param>
        public static void genWinApiOverride(CppCompilation parserResult, List<CppFunction> matchingFunctions, string resultFolder, string libraryname) {
            /* make functions */
            if (!Directory.Exists(resultFolder)) {
                Directory.CreateDirectory(resultFolder);
            }
            //library name is without the dll extension and with txt extension.
            using (TextWriter writer = File.CreateText(resultFolder + Path.GetFileNameWithoutExtension(libraryname)+ ".txt"))
            {
                Console.WriteLine("=============== Processing Functions ====================");
                Console.WriteLine();
                makeFunctions(writer, matchingFunctions, libraryname);
            }
            /* make user Types*/
            makeUserTypes(parserResult, resultFolder, libraryname);
            
            /* make user Defines*/ 
            //making user defines is not easy so we'll skip it and let the user do it manually.
        }



        static string mapFunctionExpand(CppFunction func) {
            var res = new StringBuilder();

            res.Append(String.Format("{0} {1}(", mapTypeToString(func.ReturnType),func.Name) );            

            //write parameters
            int paramCount = func.Parameters.Count;
            foreach (var param in func.Parameters)
            {  /* add it's parameters */
                res.Append(String.Format("{0} {1}", mapTypeToString(param.Type), param.Name) );
                if (param.Type.TypeKind.Equals(CppTypeKind.Array))
                {
                    res.Append(String.Format("[{0}]" ,((CppArrayType)param.Type).Size) );
                }

                if (paramCount > 1)
                {
                    res.Append(",");
                    paramCount--;
                }
            }

            res.Append(')');

            return res.ToString();
        }


        static string mapClassExpand(CppClass clazz) {
            var res = new StringBuilder();

            //warn user if more parameters are there that are not processed
            if (clazz.Classes.Count > 0 | clazz.Attributes.Count > 0 | clazz.Enums.Count > 0 | clazz.Functions.Count > 0)
            {
                Console.WriteLine("Warning: {0} from {1} has more parameters not processed!", clazz.Name, clazz.Span.Start.File);
            }

            //if the class is empty , then don't process it as it's futile 
            if (clazz.Fields.Count == 0)
            {
                return String.Format("{0} {1} {1}",clazz.ClassKind.ToString().ToLower(), clazz.Name);
            }

            //Classkind is either struct, enum or union
            res.AppendLine(String.Format("{0} {1}{{", clazz.ClassKind.ToString().ToLower(),clazz.Name));
            
            //now iterate each member
            foreach (var member in clazz.Fields)
            {
                res.AppendLine(String.Format("\t{0} {1};" , mapTypeToString(member.Type),member.Name) );
            }
            res.Append(String.Format("}}{0};", clazz.Name) );

            return res.ToString();
        }


        static string mapEnumExpand(CppEnum enumzz) {
            var res = new StringBuilder();

            res.AppendLine(String.Format("enum {0}{{",enumzz.Name));
            //now iterate each member
            foreach (var item in enumzz.Items)
            {
                res.AppendLine(String.Format("\t{0}={1};",item.Name , item.Value) );
            }
            res.AppendLine(String.Format("}}{0};",enumzz.Name) );

            return res.ToString();
        }


        static void makeFunctions(TextWriter writer, List<CppFunction> funcs, string libraryname) {
            /**
             * for WinApiOverride the format is the following
             * LibraryName| [ReturnType] FuncName( ParamType [paramName] ) [;]
             * where LibraryName is the name of the DLL
             * we only need the prototype of the function 
             */
            writer.WriteLine(";Monitoring file generated for exports table of " + Path.GetFileName(libraryname) + " by DLLHeaderParser");
            writer.WriteLine();

            foreach (var func in funcs) {
                //write comment ???

                writer.Write(Path.GetFileName(libraryname) + "|");

                //resolve function
                writer.Write(mapFunctionExpand(func));

                //finish the line
                writer.WriteLine(";");
            }

        }

        static void makeUserTypes(CppCompilation parserResult, string resultFolder, string libraryname) {
            //for each type we need a new txt file
            #region classes
            //====================== CLASSES ==============================
            Console.WriteLine("=============== Processing classStructEnum ====================");
            Console.WriteLine();

            string path;
            path = resultFolder + @"/classStructEnum/";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            foreach (var clazz in parserResult.Classes) {
                int postnum = 0;
                string currentpath = String.Format("{0}{1}.txt", path, clazz.Name);
                while (File.Exists(currentpath)) {
                    postnum++;
                    currentpath = String.Format("{0}{1}{2}.txt", path, clazz.Name, postnum);
                    //warn if a file is being overwriten!
                    Console.WriteLine(String.Format("Warning {0}.txt already exists, renaming to {1}.txt", clazz.Name, clazz.Name + postnum));
                }
                using (TextWriter writer = File.CreateText(currentpath))
                {
                    string res = mapClassExpand(clazz);

                    writer.Write(res);
                }
            }
            #endregion classes

            #region enums
            //====================== ENUMS ==============================
            Console.WriteLine("=============== Processing Enums ====================");
            Console.WriteLine();

            path = resultFolder + @"/enums/";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            foreach (var enumzz in parserResult.Enums) {
                string name = enumzz.Name;
                if (String.IsNullOrEmpty(enumzz.Name)) {
                    Console.WriteLine("Warning unnamed enum being written as {0}.txt", enumzz.Items[0].Name);
                    name = enumzz.Items[0].Name;
                }
                int postnum = 0;
                string currentpath = String.Format("{0}{1}.txt", path, name);
                while (File.Exists(currentpath))
                {
                    postnum++;
                    currentpath = String.Format("{0}{1}{2}.txt", path, name, postnum);
                    //warn if a file is being overwriten!
                    Console.WriteLine(String.Format("Warning {0}.txt already exists, renaming to {1}.txt", name, name + postnum));
                }
                using (TextWriter writer = File.CreateText(currentpath))
                {
                    string res = mapEnumExpand(enumzz);

                    writer.Write(res);
                }
            }
            #endregion enums

            #region typedefs
            //====================== Typedefs ==============================
            Console.WriteLine("=============== Processing Typedefs ====================");
            Console.WriteLine();

            path = resultFolder + @"/typedefs/";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            foreach (var typdeffs in parserResult.Typedefs) {
                int postnum = 0;
                string currentpath = String.Format("{0}{1}.txt", path, typdeffs.Name);
                while (File.Exists(currentpath))
                {
                    postnum++;
                    currentpath = String.Format("{0}{1}{2}.txt", path, typdeffs.Name, postnum);
                    //warn if a file is being overwriten!
                    Console.WriteLine(String.Format("Warning {0}.txt already exists, renaming to {1}.txt", typdeffs.Name, typdeffs.Name + postnum));
                }
                
                using (TextWriter writer = File.CreateText(currentpath))
                {
                    var res = new StringBuilder();
                    //if typedef is enum or struct or union the format is different @todo
                    if (typdeffs.ElementType is CppPrimitiveType)
                    {
                        //write directly
                        res.Append(typdeffs.ToString());
                    }
                    else if (typdeffs.ElementType is CppClass)
                    {
                        res.Append("typedef ");
                        res.Append(mapClassExpand((CppClass)typdeffs.ElementType));
                    }
                    else if (typdeffs.ElementType is CppEnum) {
                        res.Append("typedef ");
                        res.Append(mapEnumExpand((CppEnum)typdeffs.ElementType));
                    }
                    else if (typdeffs.ElementType is CppPointerType)
                    {
                        res.Append(typdeffs.ToString());
                    }
                    else if (typdeffs.ElementType is CppTypedef) 
                    {
                        res.Append(typdeffs.ToString());
                    }
                    else
                    {
                        Console.WriteLine("Warning: " + typdeffs.ToString() + " ignored because the type is not implemented");
                    }
                    writer.Write(res);
                }
            }

            #endregion typedefs

            #region fields
            //====================== FIELDS ==============================
            Console.WriteLine("=============== Processing Fields ====================");
            Console.WriteLine();

            path = resultFolder + @"/fields/";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            foreach (var fields in parserResult.Fields) {
                int postnum = 0;
                string currentpath = String.Format("{0}{1}.txt", path, fields.Name);
                while (File.Exists(currentpath))
                {
                    postnum++;
                    currentpath = String.Format("{0}{1}{2}.txt", path, fields.Name, postnum);
                    //warn if a file is being overwriten!
                    Console.WriteLine(String.Format("Warning {0}.txt already exists, renaming to {1}.txt", fields.Name, fields.Name + postnum));
                }

                using (TextWriter writer = File.CreateText(currentpath))
                {
                    var res = new StringBuilder();

                    res.Append(fields.ToString());

                    writer.Write(res);
                }
            }

            #endregion fields

        }

        /// <summary>
        /// the CppType.ToString of the compilation is not compatible with WinApiOverride
        /// so we have to map the results to something the WinApiOverride understands
        /// </summary>
        /// <param name="basetype"></param>
        /// <returns>WinApiOverride understandtable type format</returns>
        static string mapTypeToString(CppType basetype) {
            string result = ""; 
            StringBuilder res = new StringBuilder();

            switch (basetype.TypeKind)
            {
                case CppTypeKind.Primitive:
                    var primitive = (CppPrimitiveType)basetype;
                    //if the type is primitive convert it according to what type it is
                    //result = primitive.ToString();
                    
                    switch (primitive.Kind)
                    {
                        case CppPrimitiveKind.Void: result = "VOID"; break;
                        case CppPrimitiveKind.Bool: result = "BOOL"; break;
                        case CppPrimitiveKind.WChar: result = "wchar_t"; break;
                        case CppPrimitiveKind.Char: result = "CHAR"; break;
                        case CppPrimitiveKind.Short: result = "SHORT"; break;
                        case CppPrimitiveKind.Int: result = "INT"; break;
                        case CppPrimitiveKind.LongLong: result = "LONGLONG"; break;
                        case CppPrimitiveKind.UnsignedChar: result = "UCHAR"; break;
                        case CppPrimitiveKind.UnsignedShort: result = "USHORT"; break;
                        case CppPrimitiveKind.UnsignedInt: result = "UINT"; break;
                        case CppPrimitiveKind.UnsignedLongLong: result = "ULONGLONG"; break;
                        case CppPrimitiveKind.Float: result = "float"; break;
                        case CppPrimitiveKind.Double: result = "double"; break;
                        case CppPrimitiveKind.LongDouble: result = "long double"; break;
                        default:
                            throw new Exception("Primitive type not present");
                            //break;
                    }
                    
                    break;
                case CppTypeKind.Pointer:
                    var pointer = (CppPointerType)basetype;
                    var element = pointer.ElementType;
                    res.Append(mapTypeToString(element));
                    //only add a * if the element is not a function pointer
                    if(!element.TypeKind.Equals(CppTypeKind.Function))
                        res.Append("*");
                    result = res.ToString();
                    break;
                case CppTypeKind.Reference:
                    throw new Exception("Type not implemented!");
                    //break;
                case CppTypeKind.Array:
                    var array = (CppArrayType)basetype;
                    result = mapTypeToString(array.ElementType);
                    break;
                case CppTypeKind.Qualified: //it's const or volatile , ignore
                    var qual = (CppQualifiedType)basetype;
                    result = mapTypeToString(qual.ElementType);
                    break;
                case CppTypeKind.Function:
                    var functype = (CppFunctionType)basetype;
                    //for a function we want to start from the return type and go all the way to end
                    
                    res.Append(String.Format("{0} (", mapTypeToString(functype.ReturnType)) );
                    
                    int paramCount = functype.Parameters.Count;
                    foreach(var parameter in functype.Parameters)
                    {
                        res.Append( String.Format("{0} {1}", mapTypeToString(parameter.Type), parameter.Name) );
                        if (parameter.Type.TypeKind.Equals(CppTypeKind.Array))
                        {
                            res.Append(String.Format("[{0}]", ((CppArrayType)parameter.Type).Size));
                        }

                        if (paramCount > 1)
                        {
                            res.Append(",");
                            paramCount--;
                        }
                    }
                    res.Append(");");
                    result = res.ToString();
                    break;
                case CppTypeKind.Typedef: //just ignore typedef and continue with the rest
                    var typedeff = (CppTypedef)basetype;
                    result = mapTypeToString(typedeff.ElementType);
                    if (String.IsNullOrEmpty(result))
                        result = typedeff.Name;
                    break;
                case CppTypeKind.StructOrClass:
                    var structOrClass = (CppClass)basetype;
                    result = structOrClass.Name;
                    break;
                case CppTypeKind.Enum:
                    var typenum = (CppEnum)basetype;
                    result = typenum.Name;
                    break;
                case CppTypeKind.TemplateParameterType:
                    throw new Exception("Type not implemented!");
                    //break;
                case CppTypeKind.Unexposed:
                    throw new Exception("Type not implemented!");
                    //break;
                default:
                    break;
            }

            return result;
        }


        /*
         * builtin types of winapiOverride are the following
         * 
"P" means "pointer"

"W" means "wide"

"STR" means "string"

"C" means "const"

PSTR, PCSTR, LPSTR, LPCSTR, CHAR*, SEC_CHAR*, PCHAR, LPCHAR,
PWSTR, PCWSTR, LPWSTR, LPCWSTR, LPCWSTR, WCHAR*, PWCHAR, LPWCHAR, wchar_t*, SEC_WCHAR*, BSTR, OLECHAR, LPOLESTR
VOID*, PVOID, LPVOID, LPCVOID, VOID**, PVOID*, LPVOID*, LPCVOID*,
CHAR, SEC_CHAR, UCHAR, BOOLEAN, BYTE, BYTE*, PBYTE, LPBYTE, BOOLEAN*, PBOOLEAN, LPBOOLEAN, UCHAR*, PUCHAR, LPUCHAR,
WCHAR, SEC_WCHAR, wchar_t, wctrans_t, WORD, short, SHORT, USHORT, u_short, wint_t, wctype_t, WPARAM,
short*, SHORT*, PSHORT, LPSHORT, USHORT*, PUSHORT, LPUSHORT, WORD*, PWORD, LPWORD, u_short*,
BOOL, BOOL*, PBOOL, LPBOOL,
SIZE_T, _fsize_t, SIZE_T*, PSIZE_T, LPSIZE_T,
INT, INT*, PINT, LPINT, UINT, UINT*, PUINT, LPUINT,
LONG, NTSTATUS, LONG_PTR, LPARAM, LONG*, PLONG, LPLONG, LONG_PTR*, PLONG_PTR, LPLONG_PTR,
ULONG_PTR, UINT_PTR, SOCKET, ULONG, u_long, DWORD, ULONG*, PULONG, LPULONG, SOCKET*, u_long*,
DWORD*, PDWORD, LPDWORD, ULONG_PTR*, PULONG_PTR, LPULONG_PTR, LSA_HANDLE, LSA_HANDLE*,
PLSA_HANDLE, LPLSA_HANDLE,
HKEY, PHKEY, HKEY*, COLORREF, COLORREF*, LPCOLORREF, PFNCALLBACK, LCID,
SYSTEMTIME, SYSTEMTIME*, PSYSTEMTIME, LPSYSTEMTIME, FILETIME, FILETIME*, PFILETIME, LPFILETIME,
ACCESS_MASK, ACCESS_MASK*, PACCESS_MASK,
HANDLE, HINSTANCE, HWND, HMODULE, HMODULE*, PHMODULE, LPHMODULE, HANDLE*, PHANDLE, LPHANDLE,
HDESK, HBRUSH, HRGN, HDPA, HDSA, HDC, HICON, HICON*, WNDPROC, HMENU, HIMAGELIST,
DLGPROC, FARPROC, LPWSAOVERLAPPED_COMPLETION_ROUTINE,
HPALETTE, HFONT, HMETAFILE, HGDIOBJ, HCOLORSPACE, HBITMAP, HCONV, HSZ, HDDEDATA, SC_HANDLE,
HCERTSTORE, HGLOBAL, PSID, PSID*, PSECURITY_DESCRIPTOR, PSECURITY_DESCRIPTOR*, SECURITY_INFORMATION,
REGSAM, SECURITY_ATTRIBUTES, SECURITY_ATTRIBUTES*, PSECURITY_ATTRIBUTES, LPSECURITY_ATTRIBUTES,
ACL, ACL*, PACL, LPCDLGTEMPLATE,
WNDCLASS, WNDCLASS*, PWNDCLASS, LPWNDCLASS, WNDCLASSEX, WNDCLASSEX*, PWNDCLASSEX, LPWNDCLASSEX,
POINT, POINT*, PPOINT, LPPOINT, POINTL, POINTL*, PPOINTL,
SIZE, SIZE*, PSIZE, LPSIZE, RECT, RECT*, PRECT, LPRECT, RECTL, RECTL, PRECTL, LPRECTL,
CRITICAL_SECTION, CRITICAL_SECTION*, PCRITICAL_SECTION, LPCRITICAL_SECTION,
sockaddr, sockaddr*, PSOCKADDR, LPSOCKADDR, sockaddr_in, sockaddr_in*, hostent, hostent*,
timeval*, FILE*, LPSTARTUPINFO, LPSTARTUPINFOW, LPSHELLEXECUTEINFO, LPSHELLEXECUTEINFOW,
LARGE_INTEGER, LARGE_INTEGER*, PLARGE_INTEGER, ULARGE_INTEGER, ULARGE_INTEGER*, PULARGE_INTEGER,
GUID, GUID*, PGUID, LPGUID, REFGUID, IID, IID*, PIID, LPIID, REFIID, CLSID, CLSID*, PCLSID, LPCLSID, REFCLSID,
FMTID, FMTID*, PFMTID, LPFMTID, REFFMTID, MSG*, PMSG, LPMSG,
HCRYPTPROV, HCRYPTKEY, HCRYPTHASH,
PUNICODE_STRING, UNICODE_STRING*, PANSI_STRING, ANSI_STRING*,
PSecHandle, PCtxtHandle, PCredHandle,
MEMORY_BASIC_INFORMATION, MEMORY_BASIC_INFORMATION*, PMEMORY_BASIC_INFORMATION,
PROCESSENTRY32*, LPPROCESSENTRY32, PPROCESSENTRY32, PROCESSENTRY32W*, LPPROCESSENTRY32W, PPROCESSENTRY32W,
MODULEENTRY32*, LPMODULEENTRY32, PMODULEENTRY32, MODULEENTRY32W*, PMODULEENTRY32W, LPMODULEENTRY32W,
HEAPENTRY32*, PHEAPENTRY32, LPHEAPENTRY32, THREADENTRY32*, PTHREADENTRY32, LPTHREADENTRY32,
PROCESS_HEAP_ENTRY*, PPROCESS_HEAP_ENTRY, LPPROCESS_HEAP_ENTRY,
WIN32_FIND_DATA*, PWIN32_FIND_DATA, LPWIN32_FIND_DATA, WIN32_FIND_DATAW*, PWIN32_FIND_DATAW, LPWIN32_FIND_DATAW,
IO_STATUS_BLOCK*, PIO_STATUS_BLOCK,
PRINTDLG*, LPPRINTDLG, LPPRINTDLGA, LPPRINTDLGW, PRINTDLGEX*, LPPRINTDLGEX, LPPRINTDLGEXA, LPPRINTDLGEXW,
PAGESETUPDLG*, LPPAGESETUPDLG, LPPAGESETUPDLGA, LPPAGESETUPDLGW,
OPENFILENAME*, LPOPENFILENAME, LPOPENFILENAMEA, LPOPENFILENAMEW,
CHOOSEFONT*, LPCHOOSEFONT, LPCHOOSEFONTA, LPCHOOSEFONTW,
FINDREPLACE*, LPFINDREPLACE, LPFINDREPLACEA, LPFINDREPLACEW,
BROWSEINFO*, PBROWSEINFO, LPBROWSEINFO, LPBROWSEINFOA, LPBROWSEINFOW,
SHFILEINFOA*, PSHFILEINFOA, SHFILEINFOW*, PSHFILEINFOW,
NOTIFYICONDATA*, PNOTIFYICONDATA, NOTIFYICONDATAA*, PNOTIFYICONDATAA, NOTIFYICONDATAW*, PNOTIFYICONDATAW,
fd_set*, WSABUF*, PWSABUF, LPWSABUF,
ADDRINFO*, PADDRINFO, LPADDRINFO,
WSADATA*, PWSADATA, LPWSADATA,
LPWSAPROTOCOL_INFOA, LPWSAPROTOCOL_INFOW,
OVERLAPPED*, POVERLAPPED, LPOVERLAPPED, WSAOVERLAPPED*, PWSAOVERLAPPED, LPWSAOVERLAPPED,
float, float*, PFLOAT, LPFLOAT, double, double*, PDOUBLE, LPDOUBLE,
__int64, INT64, INT64*, PINT64, LPINT64, ULONG64, ULONG64*, PULONG64, LPULONG64,
DWORD64, DWORD64*, PDWORD64, LPDWORD64, ULONGLONG, ULONGLONG*, PULONGLONG, DWORDLONG, DWORDLONG*, PDWORDLONG,
TRACEHANDLE, TRACEHANDLE*, PTRACEHANDLE, DCB*, PDCB, LPDCB, COMMTIMEOUTS*, PCOMMTIMEOUTS, LPCOMMTIMEOUTS,
COMMCONFIG*, PCOMMCONFIG, LPCOMMCONFIG
SAFEARRAY, SAFEARRAY*, LPSAFEARRAY, SAFEARRAYBOUND, SAFEARRAYBOUND*, LPSAFEARRAYBOUND
VARIANTARG, LPVARIANT, VARIANTARG*, VARIANT*
DECIMAL, DECIMAL*, LPDECIMAL
MULTI_QI, MUTLI_QI*, LPMUTLI_QI
EXCEPINFO, EXCEPINFO*, LPEXCEPINFO
DISPPARAMS, DISPPARAMS*, PDISPPARAMS, LPDISPPARAMS
LOGFONTA, LOGFONTA*, PLOGFONTA, LPLOGFONTA, LOGFONTW, LOGFONTW*, PLOGFONTW, LPLOGFONTW 
         */
    }
}
