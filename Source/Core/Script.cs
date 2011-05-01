/*

 Copyright (c) 2004-2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Text.RegularExpressions;

using PHP.Core.Emit;
using PHP.Core.Reflection;
using System.Collections.Generic;

namespace PHP.Core
{
    #region InclusionResolutionContext

    /// <summary>
    /// Contains information needed during inclusion resolution.
    /// </summary>
    public class InclusionResolutionContext
    {
        /// <summary>
        /// Application context.
        /// </summary>
        public ApplicationContext ApplicationContext { get { return applicationContext; } }
        private ApplicationContext applicationContext;

        /// <summary>
        /// Directory, where the including script is present.
        /// </summary>
        public string ScriptDirectory { get { return scriptDirectory; } }
        private string scriptDirectory;

        /// <summary>
        /// Working directory.
        /// </summary>
        public string WorkingDirectory { get { return workingDirectory; } }
        private string workingDirectory;

        /// <summary>
        /// Semicolon-separated list of paths where included file is searched before the local directory is checked.
        /// </summary>
        public string SearchPaths { get { return searchPaths; } }
        private string searchPaths;

        /*
        /// <summary>
        /// Severity of inclusion-related errors. This is determined by type of inclusion being made and is needed by subsequent functions to report errors.
        /// </summary>
        public PhpError ErrorSeverity { get { return errorSeverity; } }
        private PhpError errorSeverity;
        */

        public InclusionResolutionContext(ApplicationContext applicationContext, string scriptDirectory, string workingDirectory, string searchPaths)
        {
            Debug.Assert(applicationContext != null && scriptDirectory != null && workingDirectory != null && searchPaths != null);

            this.applicationContext = applicationContext;
            this.scriptDirectory = scriptDirectory;
            this.workingDirectory = workingDirectory;
            this.searchPaths = searchPaths;
        }
    }

    #endregion

    /// <summary>
	/// Interface marking a class containing script implementation. 
	/// </summary>
	public interface IPhpScript
	{
	}

	/// <summary>
	/// Provides functionality related to PHP scripts.
	/// </summary>
	public sealed class PhpScript
	{
		/// <summary>
		/// A name of an assembly where all web pages are compiled in.
		/// </summary>
        /// <remarks>
        /// This has to be unified with the script library concept in the future.
        /// </remarks>
		public const string CompiledWebAppAssemblyName = "WebPages.dll";


		#region Main Helper

		/// <summary>
		/// Determines whether a specified method whose declaring type is a script type is a Main helper.
		/// </summary>
		/// <param name="method">The method.</param>
		/// <param name="parameters">GetUserEntryPoint parameters (optimization). Can be <B>null</B> reference.</param>
		/// <returns>Whether a specified method is an arg-less stub.</returns>
		internal static bool IsMainHelper(MethodInfo/*!*/ method, ParameterInfo[] parameters)
		{
			Debug.Assert(method != null && PhpScript.IsScriptType(method.DeclaringType));

			if (method.Name != ScriptModule.MainHelperName) return false;
			if (parameters == null) parameters = method.GetParameters();
			return parameters.Length == 5 && parameters[4].ParameterType == Emit.Types.Bool[0];
		}

		/// <summary>
		/// Checks whether a specified <see cref="Type"/> is a script type.
		/// </summary>
		/// <param name="type">The type to be checked.</param>
		/// <returns><B>true</B> iff <paramref name="type"/> is a script type.</returns>
		public static bool IsScriptType(Type type)
		{
			return typeof(IPhpScript).IsAssignableFrom(type);
		}

        ///// <summary>
        ///// Invokes a main method of a specified script.
        ///// </summary>
        ///// <param name="script">The script type to be dynamically included.</param>
        ///// <param name="context">A script context.</param>
        ///// <param name="variables">A table of defined variables.</param>
        ///// <param name="self">PHP object context.</param>
        ///// <param name="includer">PHP class context.</param>
        ///// <param name="isMain">Whether the target script is the main script.</param>
        ///// <returns>The return value of the helper method.</returns>
        ///// <exception cref="MissingMethodException">If the helper method is not found.</exception>
        ///// <exception cref="PhpException">Fatal error.</exception>
        ///// <exception cref="PhpUserException">Uncaught user exception.</exception>
        ///// <exception cref="ScriptDiedException">Script died or exit.</exception>
        ///// <exception cref="TargetInvocationException">An internal error thrown by the target.</exception>
        //internal static object InvokeMainHelper(
        //    Type script,
        //    ScriptContext context,
        //    Dictionary<string, object> variables,
        //    DObject self,
        //    DTypeDesc includer,
        //    bool isMain)
        //{
        //    MethodInfo mi = script.GetMethod(ScriptModule.MainHelperName);
        //    if (mi == null)
        //        throw new MissingMethodException(ScriptModule.MainHelperName);

        //    return PhpFunctionUtils.Invoke(mi, null, context, variables, self, includer, isMain);
        //}

		#endregion

		#region Names

		/// <summary>
		/// String added to identifiers of m-decl functions/classes.
		/// </summary>
		internal const char MDeclMark = '#';

		/// <summary>
		/// Splits a specified identifier and into a function/class name and an m-decl index (if applicable).
		/// </summary>
		/// <param name="fullClrName">An identifier.</param>
		/// <param name="name">The name of the function/class.</param>
		/// <param name="index">The index of the function/class if identifier has m-decl format or -1 if not.</param>
		public static void ParseMDeclName(string/*!*/ fullClrName, out string name, out int index)
		{
			Debug.Assert(fullClrName != null);

			int idx = fullClrName.LastIndexOf(MDeclMark);

			if (idx > 0)
			{
				name = fullClrName.Substring(0, idx);
				index = (int)UInt32.Parse(fullClrName.Substring(idx + 1));
			}
			else
			{
				name = fullClrName;
				index = -1;
			}
		}

		public static string/*!*/ ParseMDeclName(string/*!*/ fullClrName)
		{
			Debug.Assert(fullClrName != null);

			int idx = fullClrName.LastIndexOf(MDeclMark);
			return (idx > 0) ? fullClrName.Substring(0, idx) : fullClrName;
		}

		/// <summary>
		/// Decides whehter a specified name has m-decl name format.
		/// </summary>
		/// <param name="name">The name of the function.</param>
		/// <param name="index">The m-decl index of the function. Should be positive.</param>
		/// <returns>Whether the name has m-decl name format.</returns>
		public static string FormatMDeclName(string name, int index)
		{
			Debug.Assert(index > 0);
			return String.Concat(name, MDeclMark, index.ToString());
		}



		#endregion

		#region FindInclusionTargetPath, IsXxxInclusion

#if !SILVERLIGHT
        ///// <summary>
        ///// Tests whether path can be used for script inclusion.
        ///// </summary>
        ///// <param name="context">Inclusion context containing information about include which is being evaluated.</param>
        ///// <param name="fullPath">FullPath value.</param>
        ///// <param name="pathIsValid">Function deciding about file existence.</param>
        ///// <param name="errorMessage">Error message containing description of occured error. If no error occured, null value is returned.</param>
        ///// <returns>True is path is valid for inclusion, otherwise false.</returns>
        //internal static bool IsPathValidForInclusion(InclusionResolutionContext context, FullPath fullPath, Predicate<FullPath>/*!*/pathIsValid, out string errorMessage)
        //{
        //    errorMessage = null;

        //    //return
        //    //    (context.ApplicationContext.ScriptLibraryDatabase != null && context.ApplicationContext.ScriptLibraryDatabase.ContainsScript(fullPath)) ||
        //    //    (fileExists != null && fileExists(fullPath)) ||
        //    //    (fullPath.FileExists);

        //    Debug.Assert(pathIsValid != null);

        //    return pathIsValid(fullPath);
        //}

        /// <summary>
        /// Searches for an existing file among files which names are combinations of a relative path and one of the 
        /// paths specified in a list.
        /// </summary>
        /// <param name="context">Inclusion context containing information about include which is being evaluated.</param>
        /// <param name="relativePath">The relative path.</param>
        /// <param name="pathIsValid">Function deciding file existence.</param>
        /// <returns>Full path to a first existing file or an empty path.</returns>
        private static FullPath SearchInSearchPaths(InclusionResolutionContext context, string relativePath, Predicate<FullPath>/*!*/pathIsValid)
        {
            // TODO: review this when script libraries are united with precompiled web
            if (context.SearchPaths == String.Empty)
                return FullPath.Empty;
            
            Debug.Assert(pathIsValid != null);

            string path;

            for (int i = 0, j = 0; j >= 0; i = j + 1)
            {
                j = context.SearchPaths.IndexOf(Path.PathSeparator, i);
                path = (j >= 0) ? context.SearchPaths.Substring(i, j - i) : context.SearchPaths.Substring(i);

                FullPath result = FullPath.Empty;

                // TODO: exceptions should be handled better, not as part of algorithm's logic
                try
                {
                    string path_root = Path.GetPathRoot(path);

                    // makes the path complete and absolute:
                    if (path_root == "\\")
                    {
                        path = Path.Combine(Path.GetPathRoot(context.WorkingDirectory), path.Substring(1));
                    }
                    else if (path_root == "")
                    {
                        path = Path.Combine(context.WorkingDirectory, path);
                    }

                    // combines the search path with the relative path:
                    path = Path.GetFullPath(Path.Combine(path, relativePath));

                    // prepare the FullPath version
                    result = new FullPath(path, false);
                }
                catch (SystemException)
                {
                    continue;
                }

                // this function might throw an exception in case of ambiguity
                if (pathIsValid(result)/*IsPathValidForInclusion(context, result, pathIsValid, out errorMessage)*/)
                    return result;
                
                //if (errorMessage != null)
                //    return FullPath.Empty;
            }

            //errorMessage = null;
            return FullPath.Empty;
        }

		/// <summary>
		/// Searches for a specified inclusion target.
		/// </summary>
        /// <param name="context">Inclustion resolution context.</param>
        /// <param name="path">Path to the file to search.</param>
        /// <param name="pathIsValid">Function deciding about file existence. Only path that passes this function is returned.</param>
        /// <param name="errorMessage">Warning which should be reported by the compiler or a <B>null</B> reference. The error message can be set iff the returned path is empty.</param>
		/// <returns>
		/// A canonical path to the target file or a <B>null</B> reference if the file path is not valid or the file not exists.
		/// </returns>
		internal static FullPath FindInclusionTargetPath(InclusionResolutionContext context, string path, Predicate<FullPath>/*!*/pathIsValid, out string errorMessage)
		{
            Debug.Assert(context != null && path != null);
            Debug.Assert(pathIsValid != null);

            try
            {
                string root = Path.GetPathRoot(path);

                if (root == "\\")
                {
                    // incomplete absolute path //

                    // the path is at least one character long - the first character is slash that should be trimmed out: 
                    path = Path.Combine(Path.GetPathRoot(context.WorkingDirectory), path.Substring(1));
                }
                else if (root == "")
                {
                    // relative path //

                    // search in search paths at first (accepts empty path list as well):
                    FullPath result = SearchInSearchPaths(context, path, pathIsValid/*, out errorMessage*/);

                    // if an error message occurred, immediately return
                    //if (errorMessage != null)
                    //    return FullPath.Empty;
                    
                    // if the file is found then it exists so we can return immediately:
                    if (!result.IsEmpty)
                    {
                        errorMessage = null;
                        return result;
                    }

                    // not found => the path is combined with the directory where the script being compiled is stored:
                    path = Path.Combine(context.ScriptDirectory, path);
                }

                // canonizes the complete absolute path:
                path = Path.GetFullPath(path);
            }
            catch (SystemException e)
            {
                errorMessage = e.Message + "\n" + e.StackTrace;
                return FullPath.Empty;
            }

			FullPath full_path = new FullPath(path, false);

			// file does not exists:
            if (!pathIsValid(full_path)/*IsPathValidForInclusion(context, full_path, pathIsValid, out errorMessage)*/)
            {
                errorMessage = "Script cannot be included with current configuration.";
                return FullPath.Empty;
            }

            errorMessage = null;
            return full_path;
		}
#endif

		#endregion

		#region Unit Testing
#if DEBUG && !SILVERLIGHT

		public static void Test_FindInclusionTargetPath()
		{
			ApplicationConfiguration app_config = Configuration.Application;
			string result, message;

			string[,] s = new string[,] 
      { 
        // source script                    // included script    // working dir       // include_path 
        { @"C:\Web\phpBB2\includes\db.php", "./db/mssql.php",     @"C:\Web\phpBB2",    "."},
        // -> path='C:\Web\phpBB2\db\mssql.php' message=""

        { @"C:\Web\phpBB2\includes\db.php", "/db/mssql.php",      @"D:\Video",         ""},
        // -> path='' message="File 'D:\db\mssql.php' does not exist."
        
        { @"C:\Web\phpBB2\includes\db.php", "./mssql.php",        @"C:\Web\phpBB2",    "db"},
        // -> path='C:\Web\phpBB2\db\mssql.php' message=""
        
        { @"C:\Web\phpBB2\includes\db.php", "./mssql.php",        @"C:\Web\phpBB2",    "x"},
        // -> path='' message="File 'C:\Web\phpBB2\includes\mssql.php' does not exist."
        
        { @"C:\Web\phpBB2\includes\db.php", "mssql.php",          @"C:\Web\phpBB2",    "/Web/phpBB2/db"},
        // -> path='C:\Web\phpBB2\db\mssql.php' message=""

        { @"C:\Web\phpBB2\includes\db.php", "mssql.php",          @"C:\Web\phpBB2",    "/Web/php*B2/db"},
        // -> path='' message="File 'C:\Web\phpBB2\includes\mssql.php' does not exist."
        
        { @"C:\Web\phpBB2\includes\db.php", "mssql.php",          @"C:\W*b\phpBB2",    "/Web/phpBB2/db"},
        // -> path='C:\Web\phpBB2\db\mssql.php' message=""
        
        { @"C:\Web\phpBB2\includes\db.php", "*/mssql.php",        @"C:\W*b\phpBB2",    "/Web/phpBB2/db"},
        // -> path='' message="Illegal characters in path."
      };

			Console.WriteLine("{0}; {1}; {2}; {3}\n", "source script", "included script", "working dir", "include_path");
			for (int i = 0; i < s.GetLength(0); i++)
			{
                result = FindInclusionTargetPath(new InclusionResolutionContext(ApplicationContext.Default, s[i, 0], s[i, 2], s[i, 3]), s[i, 1], (path) => path.FileExists, out message);
				Console.WriteLine("'{0}'; '{1}'; '{2}'; '{3}'\npath='{4}' message=\"{5}\"\n", s[i, 0], s[i, 1], s[i, 2], s[i, 3], result, message);
			}
		}

		public static void Test_TranslateIncludeExpression()
		{
			ApplicationConfiguration app = Configuration.Application;

			string[,] s = new string[,]
      {
        // pattern                                  // replacement              // expression
        {@"LIB_PATH\s*\.\s*""([^""$]+)""",          @"/Library/$1",             "LIB_PATH . \"file1.php\""},
        // result='/Library/file1.php'
        
        {@"LIB_PATH\s*\.\s*""([^""$]+)""",          @"/Library/$1",             "  \t LIB_PATH . \"file2.php\""},
        // result='/Library/file2.php'
        
        {@"LIB_PATH\s*\.\s*""([^""$]+)""",          @"/Library/$1",             "lib_path.\"file3.php\""},
        // result='/Library/file3.php'
        
        {@"LIB_PATH\s*\.\s*""([^""$]+)""",          @"/Library/$1",             "LIB_PATH.\"file$i.php\""},
        // result=''
        
        {@"LIB_PATH\s*\.\s*""([^""$]+)""",          @"/Library/$1",             "'file3.php'"},
        // result=''
        
        {@"LIB_PATH\s*\.\s*""([^""$]+)""",          @"/Library/$1",             "$x.'file3.php'"},
        // result=''      
        
      };

			List<InclusionMapping> mappings = new List<InclusionMapping>(1);

			Console.WriteLine("{0}; {1}; {2};\n", "pattern", "replacement", "expression");
			for (int i = 0; i < s.GetLength(0); i++)
			{
				mappings[0] = new InclusionMapping(s[i, 0], s[i, 1], null);
				string result = InclusionMapping.TranslateExpression(mappings, s[i, 2], @"C:\inetpub\wwwroot");
				Console.WriteLine("#{0}# '{1}' '{2}'\nresult='{3}'\n", s[i, 0], s[i, 1], s[i, 2], result);
			}
		}

#endif
		#endregion
	}
}
