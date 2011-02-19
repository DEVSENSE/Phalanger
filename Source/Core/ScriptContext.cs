/*

 Copyright (c) 2004-2006 Tomas Matousek and Ladislav Prosek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.ComponentModel;
using PHP.Core.Emit;
using PHP.Core.Reflection;
using System.Configuration;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core
{
	#region ScriptInfo

	/// <summary>
	/// Holds information about an included script. Caches the MainHelper and allows to call the Main of the Script.
	/// </summary>
	public class ScriptInfo
	{
        /// <summary>
		/// The script type.
		/// </summary>
		public readonly Type Script;

		internal ScriptInfo(Type/*!*/script)
		{
            if (!PhpScript.IsScriptType(script))
                throw new ArgumentException("Given script type is not IPhpScript.", "script");

			Script = script;
		}

        internal ScriptInfo(Type/*!*/script, MethodInfo/*!*/mainHelper)
        {
            // no check, for internal ScriptModule use only
            this.Script = script;
            this.mainHelper = mainHelper;
        }

        internal MethodInfo MainHelper
        {
            get { return mainHelper ?? (mainHelper = this.mainHelper = Script.GetMethod(ScriptModule.MainHelperName, ScriptModule.MainHelperArgTypes)); }
            set { mainHelper = value; }
        }
        private MethodInfo mainHelper = null;

        /// <summary>
        /// Get delegate that Invokes the Main helper method of the script.
        /// Unwraps any thrown <c>InnerException</c> of <c>PhpException</c>, <c>PhpUserException</c>, <c>ScriptDiedException</c> and <c>ThreadAbortException</c>.
        /// </summary>
        internal MainRoutineDelegate Main
        {
            get
            {
                return (ScriptContext context, Dictionary<string, object> variables, DObject self, DTypeDesc includer, bool isMain) =>
                    {
                        try
                        {
                            return MainRoutine(context, variables, self, includer, isMain);
                        }
                        catch (TargetInvocationException e)
                        {
                            if (e.InnerException is PhpException ||
                                e.InnerException is PhpUserException ||
                                e.InnerException is ScriptDiedException ||
                                e.InnerException is System.Threading.ThreadAbortException)
                                throw e.InnerException;

                            throw;
                        }
                    };                   
            }
        }

        /// <summary>
        /// The delegate to the Main method of the Script. Delegate is lazily 
        /// </summary>
        private MainRoutineDelegate MainRoutine
        {
            get
            {
                return mainRoutine ?? (mainRoutine = (MainRoutineDelegate)Delegate.CreateDelegate(typeof(MainRoutineDelegate), MainHelper));
            }
        }
        private MainRoutineDelegate mainRoutine = null;
	}

	#endregion

    #region ResolveTypeFlags

    /// <summary>
	/// Flags passed to <see cref="ScriptContext.ResolveType"/>.
	/// </summary>
	[Flags]
	public enum ResolveTypeFlags
	{
		None = 0,

		/// <summary>Tries to execute autoload when the class is not found.</summary>
		UseAutoload = 1,

		/// <summary>Throw an error if the class is not found.</summary>
		ThrowErrors = 2,

		/// <summary>Stack frame is preserved if autoload is called.</summary>
		PreserveFrame = 4,

		/// <summary><see cref="PhpStack.RemoveFrame"/> is called before throwing an error.</summary>
		RemoveFrame = 8,

		/// <summary>
		/// Whether not to interpret the full name as a generic name if it cannot be resolved otherwise.
		/// </summary>
		SkipGenericNameParsing = 16
	}

	#endregion

	/// <summary>
	/// The context of an executing script. Contains data associated with a request.
	/// </summary>
	[DebuggerNonUserCode]
    public sealed partial class ScriptContext : MarshalByRefObject
	{
		#region DebugView

		internal class DebugView
		{
			private readonly ScriptContext/*!*/ context;

			public DebugView(ScriptContext/*!*/ context)
			{
				if (context == null)
					throw new ArgumentNullException("context");

				this.context = context;
			}

            [DebuggerDisplay("array({GlobalVariables.Count})", Name = "$GLOBALS", Type = "array")]
			public PhpArray/*!*/ GlobalVariables
			{
				get { return context.AutoGlobals.Globals.Value as PhpArray; }
			}

            [DebuggerDisplay("array({DeclaredFunctions.Length})", Name = "DeclaredFunctions", Type = "array")]
			public string[]/*!*/ DeclaredFunctions
			{
				get
				{
					string[] result = new string[context.DeclaredFunctions.Count];
					context.DeclaredFunctions.Keys.CopyTo(result, 0);
					Array.Sort(result);
					return result;
				}
			}

            [DebuggerDisplay("array({DeclaredTypes.Length})", Name = "DeclaredTypes", Type = "array")]
			public string[]/*!*/ DeclaredTypes
			{
				get
				{
					string[] result = new string[context.DeclaredTypes.Count];
					context.DeclaredTypes.Keys.CopyTo(result, 0);
					Array.Sort(result);
					return result;
				}
			}

#if DEBUG

			public int OwningThread
			{
				get
				{
					return context.Owner.ManagedThreadId;
				}
			}

#endif
		}

		#endregion

		#region Instance Fields & Properties

		public ApplicationContext/*!*/ ApplicationContext { get { return applicationContext; } }
		private readonly ApplicationContext/*!*/ applicationContext;

		/// <summary>
		/// List of <see cref="ScriptInfo"/>s included by the current script. Contains also the script itself.
		/// Used for resolving inclusions.
        /// </summary>
        private readonly Dictionary<PhpSourceFile, ScriptInfo> scripts = new Dictionary<PhpSourceFile, ScriptInfo>();

		public PhpSourceFile[] GetIncludedScripts()
		{
			PhpSourceFile[] result = new PhpSourceFile[scripts.Count];
			scripts.Keys.CopyTo(result, 0);
			return result;
		}

		/// <summary>
		/// A path to the source file of main script.
		/// </summary>
		public PhpSourceFile MainScriptFile { get { return mainScriptFile; } }
		private PhpSourceFile mainScriptFile;

		/// <summary>
		/// The configuration used by the class library and script functions and by objects which 
		/// has this instance of <see cref="ScriptContext"/> associated with itself.
		/// </summary>
		public LocalConfiguration Config
		{
			get
			{
				return config;
			}
			set
			{
				config = (value == null) ? Configuration.DefaultLocal : value;
			}
		}
		private LocalConfiguration config;

		/// <summary>
		/// User functions declarators - delegates pointing on declared functions.
		/// </summary>
		public Dictionary<string, DRoutineDesc>/*!*/ DeclaredFunctions
		{
			get
			{
				if (_declaredFunctions == null) _declaredFunctions = new Dictionary<string, DRoutineDesc>(StringComparer.OrdinalIgnoreCase);
				return _declaredFunctions;
			}
		}
		private Dictionary<string, DRoutineDesc> _declaredFunctions;

		/// <summary>
		/// Declarators of user classes.
		/// </summary>
		public Dictionary<string, DTypeDesc>/*!*/ DeclaredTypes
		{
			get
			{
				if (_declaredTypes == null) _declaredTypes = new Dictionary<string, DTypeDesc>(StringComparer.OrdinalIgnoreCase);
				return _declaredTypes;
			}
		}
		private Dictionary<string, DTypeDesc> _declaredTypes;

		/// <summary>
		/// User defined static locals.
		/// </summary>
		private Dictionary<string, PhpReference> staticLocals;

		/// <summary>
		/// The stack for performing indirect calls and calls to argument-aware functions.
		/// </summary>
		public readonly PhpStack Stack;

		/// <summary>
		/// Registered user stream wrappers per request. Initialized in a lazy manner.
		/// </summary>
        public Dictionary<string, StreamWrapper> UserStreamWrappers { get; set; }

		/// <summary>
		/// The current directory used as working one for PHP file system functions and for including scripts.
		/// </summary>
		public string WorkingDirectory { get { return workingDirectory; } set { workingDirectory = value; } }
		private string workingDirectory;

        /// <summary>
        /// Get the list of SPL autoload functions. This cannot be null. First call to this property will enable SPL autoload functions.
        /// </summary>
        public LinkedList<PhpCallback> SplAutoloadFunctions
        {
            get
            {
                return splAutoloadFunctions ?? (splAutoloadFunctions = new LinkedList<PhpCallback>());
            }
        }
        private LinkedList<PhpCallback> splAutoloadFunctions;

        /// <summary>
        /// Get the value indicating if SPL autload functions are enabled. (If spl_autoload_register was used.)
        /// </summary>
        public bool IsSplAutoloadEnabled { get { return splAutoloadFunctions != null; } }

        /// <summary>
        /// List of SPL extensions used by spl_autoload() function.
        /// </summary>
        public string[] SplAutoloadExtensions
        {
            get { return splAutoloadExtensions ?? new string[] { ".php" }; }
            set { splAutoloadExtensions = value; }
        }
        private string[] splAutoloadExtensions;

        /// <summary>
        /// Lazily resolved and initialized __autoload() function.
        /// Initialized when needed in the first time in <c>ResolveTypeByAutoload</c>.
        /// </summary>
        private DRoutineDesc autoloadFunction;

        /// <summary>
        /// Lazily created stack of types name (in lower case) being auto-loaded.
        /// Used as a recursion prevention of <b>autoload</b>.
        /// </summary>
        private Stack<string> pendingAutoloads;

#if DEBUG
		
		public Thread/*!*/ Owner { get { return owner; } }
		private readonly Thread/*!*/ owner;

#endif

		#endregion

		#region Construction

		/// <summary>
		/// Creates an instance of <see cref="ScriptContext"/> initialized with dummy streams and 
		/// a copy of the default local configuration.
		/// </summary>
		public ScriptContext(ApplicationContext/*!*/ appContext)
			: this(appContext, (LocalConfiguration)Configuration.DefaultLocal.DeepCopy(), TextWriter.Null, Stream.Null)
		{
		}

		/// <summary>
		/// Creates instance of <see cref="ScriptContext"/>.
		/// </summary>
		public ScriptContext(ApplicationContext/*!*/ appContext, LocalConfiguration/*!*/ config, 
			TextWriter/*!*/ textSink, Stream/*!*/ streamSink)
		{
			if (textSink == null)
				throw new ArgumentNullException("textSink");
			if (streamSink == null)
				throw new ArgumentNullException("streamSink");
			if (config == null)
				throw new ArgumentNullException("config");

			Debug.WriteLine("SC", "Created by thread #{0}", Thread.CurrentThread.ManagedThreadId);

#if DEBUG
			this.owner = Thread.CurrentThread;
#endif

			this.textSink = textSink;
			this.streamSink = streamSink;
			this.config = config;

			InitPlatformSpecific();

			// creates a new buffered output object which sends its data to given sinks:
			this.bufferedOutput = new BufferedOutput(false, this.textSink, this.streamSink,
				Configuration.Application.Globalization.PageEncoding);

			// stack:
			this.Stack = new PhpStack(this);

			// initializes output redirecting fields:
			this.IsOutputBuffered = false;
#if !SILVERLIGHT
			this.workingDirectory = Directory.GetCurrentDirectory();
#else
			this.workingDirectory = "";
#endif

			this.applicationContext = appContext;
		}

		#endregion

		#region Current Context

		public ScriptContext/*!*/ Fork()
		{
			LocalConfiguration new_config = (LocalConfiguration)this.config.DeepCopy();
			ScriptContext new_context = new ScriptContext(this.ApplicationContext, new_config, this.textSink, this.streamSink);

			new_context.WorkingDirectory = this.workingDirectory;

			// copy function declarators:
			Dictionary<string, DRoutineDesc> new_declared_functions = new_context.DeclaredFunctions;
			foreach (KeyValuePair<string, DRoutineDesc> entry in DeclaredFunctions)
				new_declared_functions[entry.Key] = entry.Value;

			// copy type declarators:
			Dictionary<string, DTypeDesc> new_declared_types = new_context.DeclaredTypes;
			foreach (KeyValuePair<string, DTypeDesc> entry in DeclaredTypes)
				new_declared_types[entry.Key] = entry.Value;

			// deep copy global variables:
			PhpArray new_globals = (PhpArray)this.GlobalVariables.DeepCopy();
			new_context.AutoGlobals.Globals = new PhpReference(new_globals);

			// TODO: deep copy other super-globals (move copying to AutoGlobals already)
			// TODO: staticLocals, wrappers, scripts

			return CurrentContext = new_context;
		}

		#region Hokus pokus

		//private HttpContext lastContext;
		//private bool httpAttachPending;

		//private ScriptContext AttachToHttpApplication()
		//{
		//    ScriptContext script_context = this;

		//    HttpContext context = HttpContext.Current;
		//    if (context != null && context != lastContext)
		//    {
		//        Debug.WriteLine("ASP.NET", "Initializing request context");

		//        script_context = RequestContext.Initialize(context).ScriptContext;
		//        script_context.lastContext = context;
		//        script_context.httpAttachPending = true;
		//    }

		//    if (script_context.httpAttachPending)
		//    {
		//        if (context == null)
		//        {
		//            // we are not running in a web request context -> no attaching takes place
		//            script_context.httpAttachPending = false;
		//        }
		//        else
		//        {
		//            HttpApplication app = context.ApplicationInstance;
		//            if (app != null)
		//            {
		//                Debug.WriteLine("ASP.NET", "HttpApplication is non-null");

		//                // a HTTP application has already been initialized -> attach to it
		//                try
		//                {
		//                    // do we already have the session state?
		//                    HttpSessionState session_state = app.Session;

		//                    Debug.WriteLine("ASP.NET", "Session state already acquired");
		//                    ApplicationInstance_PostAcquireRequestState(script_context, EventArgs.Empty);
		//                }
		//                catch (HttpException)
		//                {
		//                    // session state not yet acquired
		//                    app.PostAcquireRequestState += new EventHandler(script_context.ApplicationInstance_PostAcquireRequestState);
		//                }

		//                app.PostRequestHandlerExecute += new EventHandler(script_context.ApplicationInstance_PostRequestHandlerExecute);

		//                script_context.httpAttachPending = false;
		//            }
		//        }
		//    }

		//    return script_context;
		//}

		//private void ApplicationInstance_PostAcquireRequestState(object sender, EventArgs e)
		//{
		//    Debug.WriteLine("ASP.NET", "PostAcquireRequestState");
		//    RequestContext request_context = RequestContext.CurrentContext;

		//    Debug.Assert(request_context != null);

		//    if (Config.Session.AutoStart) request_context.StartSession();
		//}

		//private void ApplicationInstance_PostRequestHandlerExecute(object sender, EventArgs e)
		//{
		//    Debug.WriteLine("ASP.NET", "PostRequestHandlerExecute");
		//    RequestContext.CurrentContext.Dispose();

		//    this.httpAttachPending = true;
		//}

		#endregion

		#endregion

		#region Http header handling



		#endregion

		#region Constants

		/// <summary>
		/// User defined constants.
		/// </summary>
		public DualDictionary<string, object>/*!*/ Constants
		{
			get
			{
				if (_constants == null)
				{
					_constants = new DualDictionary<string, object>(null, StringComparer.OrdinalIgnoreCase);

					// predefined run-time constants:
					InitConstants(_constants);

                    // Used just to differentiate what is core constant and what is user constant
                    _coreConstants = new DualDictionary<string, object>(null, StringComparer.OrdinalIgnoreCase);
                    InitConstants(_coreConstants);
                   
				}
				return _constants;
			}
		}

		private DualDictionary<string, object> _constants;
        
        /// <summary>
        /// Contain constants defined by runtime in ScriptContext (all of them are ignoreCase)
        /// </summary>
        /// <remarks>Actaully it is here just because of GetDefinedConstants(bool) library function</remarks>
        private DualDictionary<string, object> _coreConstants; 


		/// <summary>
		/// Defines a user constant.
		/// </summary>
		/// <param name="name">The constant name. Compiler converts constant name to string before passing it to this method.</param>
		/// <param name="value">The constant value (should be either scalar or a <B>null</B> reference).</param>
		/// <returns>Whether the constant has been defined. Returns <B>false</B> if the constant is already defined.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="context"/> is a <B>null</B> reference.</exception>
		public bool DefineConstant(string name, object value)
		{
			return DefineConstant(name, value, false);
		}

		/// <summary>
		/// Defines a user constant.
		/// </summary>
		/// <param name="name">The constant name. Compiler converts constant name to string before passing it to this method.</param>
		/// <param name="value">The constant value (should be either scalar or a <B>null</B> reference).</param>
		/// <param name="ignoreCase">Whether the constant is case insensitive.</param>
		/// <returns>Whether the new constant has been defined.</returns>
		/// <exception cref="PhpException">Constant has already been defined (Notice).</exception>
		/// <exception cref="PhpException">Value is neither scalar not <B>null</B> (Warning).</exception>
		/// <exception cref="ArgumentNullException"><paramref name="context"/> is a <B>null</B> reference.</exception>
		public bool DefineConstant(string name, object value, bool ignoreCase)
		{
			if (!PhpVariable.IsScalar(value) && value != null)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("constant_value_neither_scalar_nor_null"));
				return false;
			}

			if (name == null) name = String.Empty;

			if (Constants.ContainsKey(name))
			{
				PhpException.Throw(PhpError.Notice, CoreResources.GetString("constant_redefined", name));
				return false;
			}

			Constants.Add(name, value, ignoreCase);
			return true;
		}

		[Emitted]
		public void DeclareConstant(string name, object value)
		{
			Constants[name, false] = value;
		}

		/// <summary>
		/// Retrieves a value of a constant (either user or library).
		/// </summary>
		/// <param name="name">The name of the constant.</param>
		/// <returns>Returns the value of the constant or its name it it is not defined.</returns>
		/// <exception cref="PhpException">Constant is not defined (Notice).</exception>
		[Emitted]
		public object GetConstantValue(string name)
		{
			return GetConstantValue(name, false);
		}

		/// <summary>
		/// Retrieves a value of a constant (either user or library).
		/// </summary>
		/// <param name="name">The name of the constant.</param>
		/// <param name="quiet">Whether to report a notice if the constant is not defined.</param>
		/// <returns>Returns the value of the constant or its name it it is not defined.</returns>
		/// <exception cref="PhpException">Constant is not defined (Notice).</exception>
		public object GetConstantValue(string name, bool quiet)
		{
			if (name == null) name = String.Empty;

			// gets user constant first:
			object result;
			if (Constants.TryGetValue(name, out result))
				return result;

			// gets system constant if user one is not defined:
			DConstantDesc desc;
			if (applicationContext.Constants.TryGetValue(name, out desc))
				return desc.LiteralValue;

			// constant is not defined:
			if (!quiet)
				PhpException.Throw(PhpError.Notice, CoreResources.GetString("undefined_constant", name));

			return name;
		}

		/// <summary>
		/// Checks whether a constant of a specified name is defined.
		/// </summary>
		/// <param name="name">The name of the constant.</param>
		/// <returns>Whether user or library constant with <paramref name="name"/> name is defined.</returns>
		[Emitted]
		public bool IsConstantDefined(string name)
		{
			if (name == null) name = String.Empty;
			return Constants.ContainsKey(name) || applicationContext.Constants.ContainsKey(name);
		}

		/// <summary>
		/// Retrieves all defined user and library constants.
		/// </summary>
		/// <exception cref="ArgumentNullException"><paramref name="result"/> is a <B>null</B> reference.</exception>
		public void GetDefinedConstants(IDictionary/*!*/ result)
		{
			if (result == null)
				throw new ArgumentNullException("result");

			foreach (KeyValuePair<string, object> entry in Constants)
				result[entry.Key] = entry.Value;

			foreach (KeyValuePair<string, DConstantDesc> entry in applicationContext.Constants)
				result[entry.Key] = entry.Value.LiteralValue;
		}

		/// <summary>
		/// Retrieves the number of all defined user and library constants.
		/// </summary>
		/// <returns>The number of constants.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="context"/> is a <B>null</B> reference.</exception>
		public int GetDefinedConstantCount()
		{
			return applicationContext.Constants.Count + Constants.Count;
		}

        /// <summary>
        /// Retrieves the number of user defined constants.
        /// </summary>
        /// <returns>The number of constants.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is a <B>null</B> reference.</exception>
        public int GetDefinedUserConstantCount()
        {
            return Constants.Count - _coreConstants.Count;
        }

        /// <summary>
        /// Retrieves all user defined constants.
        /// </summary>
        public void GetDefinedUserConstants(IDictionary/*!*/ result)
        {
            if (result == null)
                throw new ArgumentNullException("result");

            foreach (KeyValuePair<string, object> entry in Constants)
            {
                if (!_coreConstants.ContainsKey(entry.Key)) // it is user constant
                    result[entry.Key] = entry.Value;

            }
        }

        /// <summary>
        /// Retrieves all defined extension constants.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> is a <B>null</B> reference.</exception>
        public void GetDefinedExtensionConstants(IDictionary/*!*/ result, string extensionName)
        {
            string actualLibraryName = null;
            GlobalConstant gConst;

            if (result == null)
                throw new ArgumentNullException("result");

            if (extensionName == null)
                extensionName = "Core";

            if (extensionName == "Core")
            {
                foreach (KeyValuePair<string, object> entry in _coreConstants)
                {
                    result[entry.Key] = entry.Value;
                }
            }

            foreach (KeyValuePair<string, DConstantDesc> entry in applicationContext.Constants)
            {
                gConst = entry.Value.GlobalConstant;
                
                if (gConst == null)
                    actualLibraryName = "Core";
                else
                    actualLibraryName = gConst.Extension;

                if (actualLibraryName == null)
                {
                    actualLibraryName = "Core";
                }

                if (actualLibraryName == extensionName)
                    result[entry.Key] = entry.Value.LiteralValue;
            }
        }

		#endregion

		#region Functions

		/// <summary>
		/// Declares a PHP function.
		/// Emitted.
		/// </summary>
		/// <param name="function">The <see cref="RoutineDelegate"/> of the function.</param>
		/// <param name="fullName">The name of the function.</param>
		/// <param name="memberAttributes">Modifiers.</param>
		/// <exception cref="PhpException">A function of the given name has already been declared. (Error)</exception>
		[Emitted]
		public void DeclareFunction(RoutineDelegate/*!*/ function, string/*!*/ fullName, PhpMemberAttributes memberAttributes)
		{
			Debug.Assert(function != null && fullName != null);
			DRoutineDesc existing;

			if (!DeclaredFunctions.TryGetValue(fullName, out existing))
			{
				DeclaredFunctions.Add(fullName, new PhpRoutineDesc(memberAttributes, function));
				return;
			}

			PhpException.Throw(PhpError.Error, CoreResources.GetString("function_redeclared", existing.MakeFullName()));
		}

		/// <summary>
		/// Declares a PHP lambda function.
		/// Operator.
		/// </summary>
		/// <param name="function">The <see cref="DRoutineDesc"/> of the function.</param>
		/// <return>The generated name for the function.</return>
		[Emitted]
		public string/*!*/ DeclareLambda(RoutineDelegate/*!*/ function)
		{
			Debug.Assert(function != null);

			string name = DynamicCode.GenerateLambdaName();
			DeclaredFunctions[name] = new PhpRoutineDesc(PhpMemberAttributes.Public | PhpMemberAttributes.Static,
				function);

			return name;
		}

		/// <summary>
		/// Calls a function which is unknown at compile time.
		/// </summary>
		/// <param name="localVariables">Table of local variables if available.</param>
		/// <param name="namingContext">Naming context.</param>
		/// <param name="name">The name of the function. Case insensitive.</param>
		/// <param name="context">The script context in which to do the call.</param>
		/// <returns>The return value of the function called.</returns>
		/// <remarks>
		/// If a compile time unknown function is called all variables are expected to be of 
		/// type <see cref="PhpReference"/>. If the result is passed to non reference target 
		/// it is dereferenced.
		/// </remarks>
		[Emitted]
		public static PhpReference/*!*/ Call(Dictionary<string, object> localVariables, NamingContext namingContext, object name,
			ScriptContext/*!*/ context)
		{
			object result = null;

			string function_name = Convert.ObjectToString(name);
			if (String.IsNullOrEmpty(function_name))
			{
				context.Stack.RemoveFrame();
				PhpException.Throw(PhpError.Error, CoreResources.GetString("invalid_function_name"));
			}
			else
			{
				DRoutineDesc desc = context.ResolveFunction(function_name, null, true);

				if (desc != null)
				{
					// the callee may need table of local variables and/or naming context:
					context.Stack.Variables = localVariables;
					context.Stack.NamingContext = namingContext;

					result = desc.Invoke(null, context.Stack);
				}
				else
				{
					context.Stack.RemoveFrame();
					PhpException.Throw(PhpError.Error, CoreResources.GetString("undefined_function_called", name));
				}
			}

			return PhpVariable.MakeReference(PhpVariable.Copy(result, CopyReason.PassedByCopy));
		}

		/// <summary>
		/// Populates given list with names of user and library functions. 
		/// </summary>
		public void GetDeclaredFunctions(IList/*!*/ userFunctions, IList/*!*/ libraryFunctions)
		{
			if (userFunctions == null)
				throw new ArgumentNullException("userFunctions");
			if (libraryFunctions == null)
				throw new ArgumentNullException("libraryFunctions");

			// all user functions have declarators:
			foreach (KeyValuePair<string, DRoutineDesc> entry in DeclaredFunctions)
			{
				if (entry.Value.DeclaringModule.Assembly is PhpLibraryAssembly)
					libraryFunctions.Add(entry.Key);
				else
					userFunctions.Add(entry.Key);
			}

			// all user functions have declarators:
			foreach (KeyValuePair<string, DRoutineDesc> entry in applicationContext.Functions)
			{
				if (entry.Value.DeclaringModule.Assembly is PhpLibraryAssembly)
					libraryFunctions.Add(entry.Key);
				else
					userFunctions.Add(entry.Key);
			}
		}

		#endregion

		#region Types

		/// <summary>
		/// Declares a PHP class or PHP interface.
		/// </summary>
		/// <param name="typeDesc">The <see cref="DTypeDesc"/> of the class/interface.</param>
		/// <param name="fullName">The name of the class.</param>
		/// <exception cref="PhpException">A class or interface of the given name has already been declared. (Error)</exception>
		[Emitted]
		public void DeclareType(PhpTypeDesc/*!*/ typeDesc, string/*!*/ fullName)
		{
			Debug.Assert(typeDesc != null && !typeDesc.IsGeneric && fullName != null);
			DTypeDesc existing;

			if (!DeclaredTypes.TryGetValue(fullName, out existing))
			{
				// the completion type needn't to be created for non-generic types
				DeclaredTypes.Add(fullName, typeDesc);
				return;
			}

			// a class of this name has already been declared
			PhpException.Throw(PhpError.Error, CoreResources.GetString("type_redeclared", existing.MakeFullName()));
		}

		public void DeclareGenericType(PhpTypeDesc/*!*/ typeDesc, string/*!*/ fullName)
		{
			Debug.Assert(typeDesc != null && typeDesc.IsGeneric && fullName != null);
			DTypeDesc existing;

			if (!DeclaredTypes.TryGetValue(fullName, out existing))
			{
				bool old_throw = ThrowExceptionOnError;
				throwExceptionOnError = true;

				try
				{
					// TODO: optimize - we don't need completion if there are no resolved unknown types in the spec;

					// referring type is useless as the type/method parameters are not visible to the 
					// conditional declaration:
					GenericParameterDesc[] gps = typeDesc.ReflectGenericParameters(null, null, new ResolverDelegate(ResolveType));

					PhpTypeCompletionDesc completion = new PhpTypeCompletionDesc(typeDesc, gps);

					DeclaredTypes.Add(fullName, completion);
				}
				catch (PhpException)
				{
					if (old_throw) throw;
				}
				finally
				{
					throwExceptionOnError = old_throw;
				}

				return;
			}

			// a class of this name has already been declared
			PhpException.Throw(PhpError.Error, CoreResources.GetString("type_redeclared", existing.MakeFullName()));
		}

		/// <summary>
		/// Declares a generic PHP class or PHP interface.
		/// </summary>
		/// <param name="typeHandle">The <see cref="DTypeDesc"/> of the class/interface.</param>
		/// <param name="fullName">The name of the class.</param>
		/// <exception cref="PhpException">A class or interface of the given name has already been declared. (Error)</exception>
		[Emitted]
		public void DeclareType(RuntimeTypeHandle/*!*/ typeHandle, string/*!*/ fullName)
		{
			DeclareGenericType(PhpTypeDesc.Create(typeHandle), fullName);
		}

		public IList/*!*/ GetDeclaredClasses(IList/*!*/ result)
		{
			if (result == null)
				throw new ArgumentNullException("result");

			// global interfaces:
			foreach (KeyValuePair<string, DTypeDesc> entry in applicationContext.Types)
			{
				if (!entry.Value.IsInterface)
					result.Add(entry.Key);
			}

			// local interfaces:
			foreach (KeyValuePair<string, DTypeDesc> entry in this.DeclaredTypes)
			{
				if (!entry.Value.IsInterface)
					result.Add(entry.Key);
			}

			return result;
		}

		public IList/*!*/ GetDeclaredInterfaces(IList/*!*/ result)
		{
			if (result == null)
				throw new ArgumentNullException("result");

			// global interfaces:
			foreach (KeyValuePair<string, DTypeDesc> entry in applicationContext.Types)
			{
				if (entry.Value.IsInterface)
					result.Add(entry.Key);
			}

			// local interfaces:
			foreach (KeyValuePair<string, DTypeDesc> entry in this.DeclaredTypes)
			{
				if (entry.Value.IsInterface)
					result.Add(entry.Key);
			}

			return result;
		}

		#endregion

		#region Variables

		public PhpArray/*!*/ GlobalVariables
		{
			get
			{
                return (AutoGlobals.Globals.Value as PhpArray) ?? (PhpArray)(AutoGlobals.Globals.Value = new PhpArray());
			}
		}

		/// <summary>
		/// Auto-global arrays.
		/// </summary>
		public readonly AutoGlobals AutoGlobals = new AutoGlobals();

		#endregion

		#region Run-time Resolving

		public DRoutineDesc ResolveFunction(string/*!*/ fullName, NamingContext nameContext, bool quiet)
		{
			DRoutineDesc result = SearchForName(DeclaredFunctions, applicationContext.Functions, fullName, nameContext);

			if (!quiet && result == null)
				PhpException.Throw(PhpError.Error, CoreResources.GetString("undefined_function_called", fullName));

			return result;
		}

		public DRoutineDesc ResolveFunction(string/*!*/ fullName, NamingContext nameContext)
		{
			return ResolveFunction(fullName, nameContext, false);
		}

		/// <summary>
		/// Finds a PHP class or PHP interface (either user or system) of a given name. Resolves <c>self</c> and
		/// <c>parent</c> reserved class names.
		/// </summary>
		/// <param name="fullName">Name of the class to search for, without namespace prefix.</param>
		/// <returns>The <see cref="System.Type"/> or <B>null</B> if not found.</returns>
		public DTypeDesc ResolveType(string/*!*/ fullName)
		{
			return ResolveType(fullName, null, UnknownTypeDesc.Singleton, null, ResolveTypeFlags.UseAutoload);
		}

		private DTypeDesc ResolveType(string/*!*/ fullName, NamingContext nameContext, DTypeDesc caller)
		{
            return ResolveType(fullName, nameContext, caller, null, ResolveTypeFlags.ThrowErrors | ResolveTypeFlags.SkipGenericNameParsing | ResolveTypeFlags.UseAutoload);
		}

		/// <summary>
		/// Finds a type (either user or system) of a given name. Resolves <c>self</c> and
		/// <c>parent</c> reserved class names.
		/// </summary>
		/// <param name="fullName">Name of the class/interface to search for, without namespace prefix, any case.</param>
		/// <param name="nameContext">Current naming context.</param>
		/// <param name="caller">Current class context.</param>
        /// <param name="genericArgs">Array of function type params. Stored in pairs in a form of [(string)name1,(DTypeDescs)type1, .., ..]. Can be null.</param>
		/// <param name="flags">Resolve type flags.</param>
        /// <returns>The <see cref="DTypeDesc"/> or <B>null</B> if not found.</returns>
		/// <exception cref="PhpException">The <paramref name="fullName"/> is <c>self</c> or <c>parent</c> but there is
		/// no class context. (Error)</exception>
		/// <exception cref="PhpException">The <paramref name="fullName"/> is <c>parent</c> but the current class has no parent.
		/// (Error)</exception>
		/// <exception cref="PhpException">The class is not found. (Error)</exception>
		public DTypeDesc ResolveType(string/*!*/ fullName, NamingContext nameContext, DTypeDesc caller, object[] genericArgs, ResolveTypeFlags flags)
		{
			if (fullName == null)
				throw new ArgumentNullException("fullName");

			string lowercase_full_name = fullName.ToLower();

			DTypeDesc type_desc;

			// self, parent:
			type_desc = ResolveSpecialTypeNames(lowercase_full_name, caller, flags);
			if (type_desc != null) return type_desc;

			// type parameters (the requirement for exclamation mark prevents from misinterpreting 
			// regular type name as a generic parameter name):
			if (lowercase_full_name.Length > 0 && lowercase_full_name[0] == '!')
                return ResolveGenericParameterType(lowercase_full_name.Substring(1), caller, genericArgs);

			// regular types:
			type_desc = SearchForName(DeclaredTypes, applicationContext.Types, fullName, nameContext);
			if (type_desc != null) return type_desc;

			// try parse generic type name:
			if ((flags & ResolveTypeFlags.SkipGenericNameParsing) == 0)
			{
                type_desc = TryResolveGenericTypeName(fullName, nameContext, caller, genericArgs, flags);
				if (type_desc != null) return type_desc;
			}

			// try to invoke __autoload
			if ((flags & ResolveTypeFlags.UseAutoload) != 0)
			{
                if ((type_desc = ResolveTypeByAutoload(fullName, lowercase_full_name, nameContext, caller, flags)) != null)
                        return type_desc;
			}

            // Specified type could not be found
			if ((flags & ResolveTypeFlags.RemoveFrame) != 0) Stack.RemoveFrame();
			if ((flags & ResolveTypeFlags.ThrowErrors) != 0)
			{
				PhpException.Throw(PhpError.Error, CoreResources.GetString("class_not_found", fullName));
			}
			return null;
		}

        /// <summary>
        /// In case SPL autoload is enabled, invoke registered SPL autoload functions subsequently until
        /// specified type is declared. Otherwise try to invoke __autoload function to declare specified type.
        /// </summary>
        /// <param name="fullName">Name of the class/interface to search for, without namespace prefix, any case.</param>
        /// <param name="fullNameLower">Name of the class (<c>fullName</c>) in lower case used in recursion prevention.</param>
        /// <param name="nameContext">Current naming context.</param>
        /// <param name="caller">Current class context.</param>
        /// <param name="flags">Resolve type flags.</param>
        /// <returns>The <see cref="DTypeDesc"/> or <B>null</B> if the class was not autoloaded.</returns>
        private DTypeDesc ResolveTypeByAutoload(string fullName, string fullNameLower, NamingContext nameContext, DTypeDesc caller, ResolveTypeFlags flags)
        {
            DTypeDesc resolved_type_desc = null;    // result of the autoload

            if (pendingAutoloads == null)
                pendingAutoloads = new Stack<string>();
            else
                if (pendingAutoloads.Contains(fullNameLower))
                    return null;    // "Class '{0}' not found"

            // recursion prevention
            pendingAutoloads.Push(fullNameLower);

            // try to invoke autoload function and check fullName again
            if (IsSplAutoloadEnabled)
            {
                foreach (var callback in SplAutoloadFunctions)
                {
                    if ((flags & ResolveTypeFlags.PreserveFrame) != 0)
                    {
                        PhpStack.CallState call_state = Stack.SaveCallState();

                        callback.Invoke(caller, new object[] { fullName });

                        Stack.RestoreCallState(call_state);
                    }
                    else
                    {
                        callback.Invoke(caller, new object[] { fullName });
                    }

                    // search again

                    if ((resolved_type_desc = SearchForName(DeclaredTypes, applicationContext.Types, fullName, nameContext)) != null)
                        break;
                }
            }
            else
            {
                if (autoloadFunction == null)
                    autoloadFunction = ResolveFunction(Name.AutoloadName.Value, null, true);

                if (autoloadFunction != null)
                {
                    if ((flags & ResolveTypeFlags.PreserveFrame) != 0)
                    {
                        PhpStack.CallState call_state = Stack.SaveCallState();

                        Stack.AddFrame(fullName);
                        autoloadFunction.Invoke(null, Stack);

                        Stack.RestoreCallState(call_state);
                    }
                    else
                    {
                        Stack.AddFrame(fullName);
                        autoloadFunction.Invoke(null, Stack);
                    }

                    // search again
                    resolved_type_desc = SearchForName(DeclaredTypes, applicationContext.Types, fullName, nameContext);
                }
            }

            // recursion prevention end
            pendingAutoloads.Pop();

            // return found class description
            return resolved_type_desc;
        }

        /// <summary>
        /// Resolve generic parameter type. The type specified as a generic parameter.
        /// </summary>
        /// <param name="lowercaseFullName">The name of the generic parameter without the leading exclamation mark. The name is in lowercase.</param>
        /// <param name="caller">Current class context.</param>
        /// <param name="genericArgs">Array of function type params. Stored in pairs in a form of [(string)name1,(DTypeDescs)type1, .., ..]. Can be null.</param>
        /// <returns>The <see cref="DTypeDesc"/> or <B>null</B> if not found.</returns>
        private DTypeDesc ResolveGenericParameterType(string lowercaseFullName, DTypeDesc caller, object[] genericArgs)
        {
            // function/method params:
            if (genericArgs != null)
            {
                // get DTypeDesc from the function/method generic argument
                // it is faster to lookup in linear array in case of only few elements then to construct Dictionary for this
                for (int i = 0; i < genericArgs.Length; i += 2)
                {
                    Debug.Assert(genericArgs[i] is string);
                    Debug.Assert(genericArgs[i + 1] is DTypeDesc);

                    if ((string)genericArgs[i] == lowercaseFullName)
                        return (DTypeDesc)genericArgs[i + 1];
                }
            }

            // type params:
            if (caller != null && caller.IsUnknown) caller = PhpStackTrace.GetClassContext();   // determine the caller if it was not known in compile time
            if (caller != null)
                return caller.GetGenericParameter(lowercaseFullName);
            
            // type could not be found
            return null;
        }

        /// <summary>
        /// Resolve <c>base</c> or <c>parent</c> class names in current class context.
        /// </summary>
        /// <param name="lowercaseFullName">The name of class.</param>
        /// <param name="caller">Class context.</param>
        /// <param name="flags">Resolve type flags.</param>
        /// <returns>The <see cref="DTypeDesc"/> or <B>null</B> if the class was not autoloaded.</returns>
        /// <exception cref="PhpException">The <paramref name="lowercaseFullName"/> is <c>self</c> or <c>parent</c> but there is
        /// no class context. (Error)</exception>
        /// <exception cref="PhpException">The <paramref name="lowercaseFullName"/> is <c>parent</c> but the current class has no parent.
        /// (Error)</exception>
		private DTypeDesc ResolveSpecialTypeNames(string/*!*/ lowercaseFullName, DTypeDesc caller, ResolveTypeFlags flags)
		{
			// check for 'self' reserved class name
			if (Name.SelfClassName.EqualsLowercase(lowercaseFullName))
			{
                if (caller != null && caller.IsUnknown) caller = PhpStackTrace.GetClassContext();   // determine the caller if it was not known in compile time
                if (caller != null) return caller;

                // otherwise, caller is global code
				if ((flags & ResolveTypeFlags.RemoveFrame) != 0) Stack.RemoveFrame();
				if ((flags & ResolveTypeFlags.ThrowErrors) != 0)
				{
					PhpException.Throw(PhpError.Error, CoreResources.GetString("self_accessed_out_of_class"));
				}
				return null;
			}

			// check for 'parent' reserved class name
			if (Name.ParentClassName.EqualsLowercase(lowercaseFullName))
			{
                if (caller != null && caller.IsUnknown) caller = PhpStackTrace.GetClassContext();   // determine the caller if it was not known in compile time
                if (caller != null)
                {
                    if (caller.Base != null)
                        return caller.Base;

                    // otherwise parent class accessed in parent-less class
                    if ((flags & ResolveTypeFlags.RemoveFrame) != 0) Stack.RemoveFrame();
                    if ((flags & ResolveTypeFlags.ThrowErrors) != 0)
                    {
                        PhpException.Throw(PhpError.Error, CoreResources.GetString("parent_accessed_in_parentless_class"));
                    }
                    return null;
                }

                // otherwise, caller is global code
				if ((flags & ResolveTypeFlags.RemoveFrame) != 0) Stack.RemoveFrame();
				if ((flags & ResolveTypeFlags.ThrowErrors) != 0)
				{
					PhpException.Throw(PhpError.Error, CoreResources.GetString("parent_accessed_out_of_class"));
				}
				return null;
			}

			return null;
		}

		/// <summary>
		/// Searches for a type in a given naming context.
		/// </summary>
		/// <param name="localTable"></param>
		/// <param name="globalTable"></param>
		/// <param name="fullName">Name of the type to search for.</param>
		/// <param name="nameContext">The naming context, in which the search should be performed.</param>
		/// <returns>The corresponding <see cref="DTypeDesc"/> or <B>null</B> if not found.</returns>
		private Desc SearchForName<Desc>(
			Dictionary<string, Desc>/*!*/ localTable,
			Dictionary<string, Desc>/*!*/ globalTable,
			string/*!*/ fullName,
			NamingContext nameContext)
			where Desc : DMemberDesc
		{
			Desc desc;

			// search in local and global tables:
			if (localTable.TryGetValue(fullName, out desc) || globalTable.TryGetValue(fullName, out desc))
				return desc;

			// if we have a naming context, use it
			if (nameContext != null)
			{
				bool debug_mode = Configuration.Application.Compiler.Debug;
				Desc candidate;

				string[] prefixes = nameContext.Prefixes;
				for (int i = 0; i < prefixes.Length; i++)
				{
					string candidate_name = prefixes[i] + fullName;

					// search in ClassDeclarators and application context
					if (localTable.TryGetValue(candidate_name, out candidate) ||
						globalTable.TryGetValue(candidate_name, out candidate))
					{
						if (debug_mode)
						{
							if (desc != null)
							{
								// ambiguity
								PhpException.Throw(PhpError.Error, CoreResources.GetString("ambiguous_name_match",
									fullName, desc.MakeFullName(), candidate_name));

								return null;
							}
							else desc = candidate;
						}
						else
						{
							// release mode: return the first candidate found
							return candidate;
						}
					}
				}
			}

			return desc;
		}

        public DTypeDesc TryResolveGenericTypeName(string/*!*/ fullName, NamingContext nameContext, DTypeDesc caller, object[] genericArgs, ResolveTypeFlags flags)
		{
			if (!CheckGenericNameStructure(fullName)) return null;

			int i = 0;
            return ResolveGenericTypeName(ref i, fullName, nameContext, caller, genericArgs, flags);
		}

		/// <summary>
		/// <c>start</c> points to the first character of the substring to parse.
		/// <c>end</c> points to the one character behind the substring to parse.
		/// </summary>
        private DTypeDesc ResolveGenericTypeName(ref int i, string/*!*/ fullName, NamingContext nameContext, DTypeDesc caller, object[] genericArgs, ResolveTypeFlags flags)
		{
			int start = i;

			// find end of the type name:
			while (fullName[i] != '<' && fullName[i] != ',' && fullName[i] != '>') i++;
			if (fullName[i] == '>' && fullName[i - 1] == ':') i--;

			// resolve type name:
            DTypeDesc type_desc = ResolveType(fullName.Substring(start, i - start), nameContext, caller, genericArgs, flags | ResolveTypeFlags.SkipGenericNameParsing);
			if (type_desc == null || fullName[i] != '<')
			{
				if (type_desc != null && type_desc.IsGenericDefinition)
					return Operators.MakeGenericTypeInstantiation(type_desc, DTypeDesc.EmptyArray, 0);
				else
					return type_desc;
			}

			// skip '<' or '<:'
			i++;
			if (fullName[i] == ':') i++;

			// arguments:
			DTypeDesc[] args = new DTypeDesc[type_desc.GenericParameters.Length];
			int arg_idx = 0;
			do
			{
				// a warning will be reported later, expand the array and go on:
				if (arg_idx == args.Length) Array.Resize(ref args, args.Length * 2 + 1);

				args[arg_idx] = ResolveGenericTypeName(ref i, fullName, nameContext, caller, genericArgs, flags);
				if (args[arg_idx] == null) return null;
				arg_idx++;
			}
			while (fullName[i++] == ',');

			Debug.Assert(fullName[i - 1] == ':' || fullName[i - 1] == '>');

			// skip '>' or ':>'
			if (fullName[i - 1] == ':') i++;

			return Operators.MakeGenericTypeInstantiation(type_desc, args, arg_idx);
		}

		private bool CheckGenericNameStructure(string/*!*/ fullName)
		{
			int opens = 0;

			if (fullName.Length == 0 || fullName[fullName.Length - 1] != '>') return false;

			int i = 0;
			while (i < fullName.Length - 1)
			{
				if (fullName[i] == '<')
				{
					// invalid character preceding '<'
					if (i == 0 || (fullName[i - 1] == '<' || fullName[i - 1] == ':')) return false;
					opens++;
				}
				else if (fullName[i] == '>')
				{
					// invalid pairing:
					if (opens == 0) return false;

					// invalid character following the '>'
					if (fullName[i + 1] != '>' && fullName[i + 1] != ':' && fullName[i + 1] != ',') return false;

					opens--;
				}
				else if (fullName[i] == ':')
				{
					// namespace separator ':::'
					if (fullName[i + 1] == ':' && i + 2 < fullName.Length && fullName[i + 2] == ':')
					{
						i += 2;
					}
					else
					{
						// colon before first '<'
						if (opens == 0) return false;

						// either colon can be preceded by '<', or followed by '>'
						if (!((fullName[i - 1] == '<') ^ (fullName[i + 1] == '>'))) return false;
					}
				}
				else if (fullName[i] == ',')
				{
					// comma before first '<' or behind last '>'
					if (opens == 0) return false;
				}

				i++;
			}

			return opens == 1;
		}

		#endregion

		#region Error Reporting

		/// <summary>
		/// Whether to throw an exception on soft error (Notice, Warning, Strict).
		/// </summary>
		public bool ThrowExceptionOnError { get { return throwExceptionOnError; } set { throwExceptionOnError = value; } }
		private bool throwExceptionOnError = true;

		/// <summary>
		/// Gets whether error reporting is disabled or enabled.
		/// </summary>
		public bool ErrorReportingDisabled
		{
			get
			{
				return (errorReportingDisabled > 0) && !config.ErrorControl.IgnoreAtOperator;
			}
		}
		private int errorReportingDisabled = 0;

		/// <summary>
		/// Gets a value indicating a level of error reporting presented to user.
		/// </summary>
		public int ErrorReportingLevel
		{
			get
			{
				return ErrorReportingDisabled ? 0 : (int)config.ErrorControl.ReportErrors;
			}
		}

		/// <summary>
		/// Disables error reporting. Can be called for multiple times. To enable reporting again 
		/// <see cref="EnableErrorReporting"/> should be called as many times as <see cref="DisableErrorReporting"/> was.
		/// </summary>
		[Emitted]
		public void DisableErrorReporting()
		{
			errorReportingDisabled++;
		}

		/// <summary>
		/// Enables error reporting disabled by a single call to <see cref="DisableErrorReporting"/>.
		/// </summary>
		[Emitted]
		public void EnableErrorReporting()
		{
			if (errorReportingDisabled > 0)
				errorReportingDisabled--;
		}

		#endregion

		#region Output Control, Echo

		// sinks where buffered output is flushed:
		private TextWriter textSink;
		private Stream streamSink;

		/// <summary>
		/// Buffered output associated with the request.
		/// </summary>
		public BufferedOutput BufferedOutput
		{
			get
			{
				return bufferedOutput;
			}
		}
		private BufferedOutput bufferedOutput;

		/// <summary>
		/// Stream where text output will be sent.
		/// </summary>
		public TextWriter Output
		{
			get
			{
				return output;
			}
			set
			{
				this.textSink = value;
				bufferedOutput.CharSink = value;
				if (output != bufferedOutput)        // if output is not buffered
					output = value;
			}
		}
		internal TextWriter output;

		/// <summary>
		/// Stream where binary output will be sent.
		/// </summary>
		public Stream OutputStream
		{
			get
			{
				return binaryOutput;
			}
			set
			{
				this.streamSink = value;
				bufferedOutput.ByteSink = value;
				if (binaryOutput != bufferedOutput.Stream)        // if output is not buffered
					binaryOutput = value;
			}
		}
		internal Stream binaryOutput;

		/// <summary>
		/// Specifies whether script output is passed through <see cref="BufferedOutput"/>.
		/// </summary>
		public bool IsOutputBuffered
		{
			get
			{
				return output == bufferedOutput;
			}
			set
			{
				if (value)
				{
					output = bufferedOutput;
					binaryOutput = bufferedOutput.Stream;
				}
				else
				{
					output = textSink;
					binaryOutput = streamSink;
				}
			}
        }

        #region Echo

        /// <summary>
		/// Writes data to the current output.
		/// </summary>
		/// <param name="value">Data to be written.</param>
        /// <param name="scriptcontext">Current script context.</param>
		[Emitted]
        public static void Echo(object value, ScriptContext scriptcontext)
		{
            if (value == null)
                return;

			PhpBytes bytes;

            if ((bytes = value as PhpBytes) != null)
            {
                if (bytes.Length > 0)
                    scriptcontext.binaryOutput.Write(bytes.Data, 0, bytes.Length);
            }
            else
            {
                scriptcontext.output.Write(Convert.ObjectToString(value));
            }
		}

        ///// <summary>
        ///// Writes data to the current output.
        ///// </summary>
        ///// <param name="values">An array of object to be written to output.</param>
        ///// <exception cref="ArgumentNullException"><paramref name="values"/> is a <B>null</B> reference.</exception>
        //[Emitted]
        //public static void Echo(params object[] values, ScriptContext scriptcontext)
        //{
        //    if (values == null)
        //        throw new ArgumentNullException("values");

        //    for (int i = 0; i < values.Length; i++)
        //        Echo(values[i]);
        //}

        /// <summary>
        /// Writes <see cref="PhpBytes" /> data to the current output.
        /// </summary>
        /// <param name="value">Data to be written.</param>
        /// <param name="scriptcontext">Current script context.</param>
        [Emitted]
        public static void Echo(PhpBytes value, ScriptContext/*!*/scriptcontext)
        {
            if (value != null && value.Length > 0)
                scriptcontext.binaryOutput.Write(value.Data, 0, value.Length);
        }

		/// <summary>
		/// Writes <see cref="string" /> to the current output.
		/// </summary>
		/// <param name="value">The string to be written.</param>
        /// <param name="scriptcontext">Current script context.</param>
        [Emitted]
        public static void Echo(string value, ScriptContext/*!*/scriptcontext)
		{
            scriptcontext.output.Write(value);
		}

		/// <summary>
		/// Writes <see cref="bool" /> to the current output.
		/// </summary>
		/// <param name="value">The boolean to be written.</param>
        /// <param name="scriptcontext">Current script context.</param>
        [Emitted]
        public static void Echo(bool value, ScriptContext/*!*/scriptcontext)
		{
            if (value) scriptcontext.output.Write('1');
		}

		/// <summary>
		/// Writes <see cref="int" /> to the current output.
		/// </summary>
		/// <param name="value">The integer to be written.</param>
        /// <param name="scriptcontext">Current script context.</param>
        [Emitted]
        public static void Echo(int value, ScriptContext/*!*/scriptcontext)
		{
            scriptcontext.output.Write(value.ToString());
		}

		/// <summary>
		/// Writes <see cref="long"/> to the current output.
		/// </summary>
		/// <param name="value">The long integer to be written.</param>
        /// <param name="scriptcontext">Current script context.</param>
        [Emitted]
        public static void Echo(long value, ScriptContext/*!*/scriptcontext)
		{
            scriptcontext.output.Write(value.ToString());
		}

		/// <summary>
		/// Writes <see cref="double"/> to the current output.
		/// </summary>
        /// <param name="value">The double to be written.</param>
        /// <param name="scriptcontext">Current script context.</param>
        [Emitted]
        public static void Echo(double value, ScriptContext/*!*/scriptcontext)
		{
            scriptcontext.output.Write(Convert.DoubleToString(value));
        }

        #endregion

        /*#region Echo (static wrappers)

        [Emitted]
        public static void Echo(object value, ScriptContext scriptcontext)
        {
            scriptcontext.Echo(value);
        }

        [Emitted]
        public static void Echo(PhpBytes value, ScriptContext scriptcontext)
        {
            scriptcontext.Echo(value);
        }

        /// <summary>
        /// Writes <see cref="string"/> to the current output.
        /// </summary>
        /// <param name="value">The string to be written.</param>
        /// <param name="scriptcontext">Current ScriptContext to be used.</param>
        [Emitted]
        public static void Echo(string value, ScriptContext scriptcontext)
        {
            scriptcontext.Echo(value);
        }

        /// <summary>
        /// Writes <see cref="bool"/> to the current output.
        /// </summary>
        /// <param name="value">The boolean to be written.</param>
        /// <param name="scriptcontext">Current ScriptContext to be used.</param>
        [Emitted]
        public static void Echo(bool value, ScriptContext scriptcontext)
        {
            scriptcontext.Echo(value);
        }

        /// <summary>
        /// Writes <see cref="int"/> to the current output.
        /// </summary>
        /// <param name="value">The integer to be written.</param>
        /// <param name="scriptcontext">Current ScriptContext to be used.</param>
        [Emitted]
        public static void Echo(int value, ScriptContext scriptcontext)
        {
            scriptcontext.Echo(value);
        }

        /// <summary>
        /// Writes <see cref="long"/> to the current output.
        /// </summary>
        /// <param name="value">The long integer to be written.</param>
        /// <param name="scriptcontext">Current ScriptContext to be used.</param>
        [Emitted]
        public static void Echo(long value, ScriptContext scriptcontext)
        {
            scriptcontext.Echo(value);
        }

        /// <summary>
        /// Writes <see cref="double"/> to the current output.
        /// </summary>
        /// <param name="value">The double to be written.</param>
        /// <param name="scriptcontext">Current ScriptContext to be used.</param>
        [Emitted]
        public static void Echo(double value, ScriptContext scriptcontext)
        {
            scriptcontext.Echo(value);
        }

        #endregion*/

        #endregion

        #region Script Termination, Timeouts, GuardedCall

        /// <summary>
		/// A timer used for timeouting the request in the manner of PHP.
		/// </summary>
		private Timer timer;

		/// <summary>
		/// Returns <B>true</B> if the main script code has been timed out.
		/// </summary>
		public bool ExecutionTimedOut { get { return executionTimedOut; } }
		private bool executionTimedOut = false;

		/// <summary>
		/// Updates a value of <see cref="LocalConfiguration.RequestControlSection.ExecutionTimeout"/>
		/// in the current configuration record and adjusts the active timer (if any).
		/// </summary>
		/// <param name="seconds">Timeout in seconds, non-positive values mean no timeout.</param>
		public void ApplyExecutionTimeout(int seconds)
		{
			config.RequestControl.ExecutionTimeout = seconds;
			if (timer != null)
			{
				timer.Change(config.RequestControl.ExecutionTimeoutForTimer, Timeout.Infinite);
			}
		}

		/// <summary>
		/// Calls a specified routine in limited time and termination exceptions being caught
		/// (i.e. <see cref="ScriptDiedException"/> and <see cref="ThreadAbortException"/>).
		/// </summary>
		/// <param name="routine">A routine to be called.</param>
		/// <param name="data">Data passed to the routine.</param>
		/// <param name="allowUserExceptions">Whether user exceptions are allowed to be thrown by the target.</param>
		/// <exception cref="PhpNetInternalException">Internal error.</exception>
        internal TResult GuardedCall<TData, TResult>(Converter<TData, TResult> routine, TData data, bool allowUserExceptions)
		{
			Library.SPL.Exception user_exception = null;

			try
			{
				// do not timeout script which is being debugged:
				if (Debugger.IsAttached) // TODO: long compilation time
				{
					return routine(data);
				}
				else
				{
					using (timer = new Timer(new TimerCallback(TimedOut), Thread.CurrentThread,
						config.RequestControl.ExecutionTimeoutForTimer, Timeout.Infinite))
					{
						return routine(data);
					}
				}
			}
			catch (ScriptDiedException)
			{
				// die or exit has been called or connections has been aborted
			}
			catch (ThreadAbortException)
			{
				if (!executionTimedOut) throw;
				ThreadAbortedDueToTimeout();
			}
			catch (PhpException)
			{
				// an error occurred -> we shell continue with execution
			}
			catch (PhpUserException e)
			{
				user_exception = e.UserException;
			}
			catch (Exception e)
			{
				throw new PhpNetInternalException("Guarded call", e);
			}
			finally
			{
				timer = null;
			}

			if (user_exception != null)
			{
				string str_exception = null;
				string error_id = null;

				// try execute user exception handler (if allowed and set):
				if (allowUserExceptions)
				{
					if (config.ErrorControl.UserExceptionHandler != null)
					{
						// calls uncaught-exception user handler:
						GuardedCall<Library.SPL.Exception,object>(CallUserExceptionHandler, user_exception, false);
					}
					else
					{
						error_id = "uncaught_exception";

						// gets exception string representation:
                        str_exception = GuardedCall<Library.SPL.Exception, string>(CallUserExceptionToString, user_exception, false);

						// null can be returned on time out or if there is an error during guarded call:
						if (str_exception == null) str_exception = user_exception.BaseToString();
					}
				}
				else
				{
					error_id = "exception_cannot_be_thrown";

					// does not call __toString to prevent infinite recursion:
					str_exception = user_exception.BaseToString();
				}

				// reports an error (doesn't throw an exception since execution should continue):
				if (error_id != null)
				{
					bool old_throw = ThrowExceptionOnError;
					ThrowExceptionOnError = false;
					PhpException.Throw(PhpError.Error, CoreResources.GetString(error_id, str_exception));
					ThrowExceptionOnError = old_throw;
				}
			}

			return default(TResult);
		}

		/// <summary>
		/// Calls user eception handler.
		/// </summary>
        private object CallUserExceptionHandler(PHP.Library.SPL.Exception/*!*/ e)
		{
			Debug.Assert(config.ErrorControl.UserExceptionHandler != null);
			config.ErrorControl.UserExceptionHandler.Invoke(e);
			return null;
		}

		/// <summary>
		/// Get user exception string representation.
		/// </summary>
        private string CallUserExceptionToString(Library.SPL.Exception/*!*/ e)
		{
			//Debug.Assert(e is Library.SPL.Exception);
			return (string)e.__toString(this);
		}

		// GENERICS: lambda
		private object GuardedMain(object/*!*/ mainRoutine)
		{
			RoutineDelegate user_main = mainRoutine as RoutineDelegate;
			if (user_main != null)
			{
				return user_main(null, this.Stack);
			}
			else
			{
				return ((MainRoutineDelegate)mainRoutine)(
					this,
					null,  // no local variables
					null,  // no object context (makes $this use illegal)
					null,  // no class context (makes protected and private members of all classes inaccessible)
					true);
			}
		}

		/// <summary>
		/// Flushes all remaining data from output buffers.
		/// </summary>
		internal object FinalizeBufferedOutput(object _)
		{
			// flushes output, applies user defined output filter, and disables buffering:
			bufferedOutput.FlushAll();

			// redirects sinks:
			IsOutputBuffered = false;

			return null;
		}

		/// <summary>
		/// Called when the execution has been timed out. 
		/// </summary>
		private void ThreadAbortedDueToTimeout()
		{
			Debug.Assert(executionTimedOut);

#if !SILVERLIGHT
			Thread.ResetAbort();
#endif

			bool old_throw = ThrowExceptionOnError;
			ThrowExceptionOnError = false;

			PhpException.Throw(PhpError.Error, CoreResources.GetString("execution_timed_out",
				config.RequestControl.ExecutionTimeout));

			ThrowExceptionOnError = old_throw;
		}

		// GENERICS: Lambda
		private void TimedOut(object/*!*/ thread)
		{
			executionTimedOut = true;
			((Thread)thread).Abort();
		}

		/// <summary>
		/// Implements language construct exit/die. 
		/// </summary>
		/// <param name="status">The status returned by script's Main() method. Can be a <B>null</B> reference.</param>
		/// <remarks>
		/// Prints the <paramref name="status"/> if it is of <see cref="string"/> type.
		/// </remarks>
		[Emitted]
		public void Die(object status)
		{
			// prints status:
			string s = status as string;
			if (s != null) output.Write(s);

			// terminates script execution:
			throw new ScriptDiedException(status);
		}

		#endregion

		#region Shutdown Callbacks

		/// <summary>
		/// User defined post-request callbacks.
		/// Can be a <B>null</B> reference which means no shutdown callback has ever been registered.
		/// </summary>
		private Queue shutdownCallbacks; // <PhpCallbackParameterized>

		/// <summary>
		/// Adds new user callback to the list of callbacks called on script termination.
		/// </summary>
		/// <param name="callback">The callback to register.</param>
		/// <param name="parameters">Parameters for the callback.</param>
		/// <exception cref="ArgumentNullException"><paramref name="callback"/> is a <B>null</B> reference.</exception>
		public void RegisterShutdownCallback(PhpCallback/*!*/ callback, params object[] parameters)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			if (shutdownCallbacks == null) shutdownCallbacks = new Queue();
			shutdownCallbacks.Enqueue(new PhpCallbackParameterized(callback, parameters));
		}

		/// <summary>
		/// Processes shutdown callbacks registered by a user.
		/// </summary>
		/// <exception cref="PhpException">An error occured during execution of the callbacks.</exception>
		/// <exception cref="PhpUserException">Uncaught exception has been raised by some callback.</exception>
		/// <exception cref="ScriptDiedException">Script died or exited.</exception>
		/// <exception cref="PhpNetInternalException">Internal error.</exception>
		public object ProcessShutdownCallbacks(object _)
		{
			if (shutdownCallbacks != null)
			{
				while (shutdownCallbacks.Count > 0)
				{
					((PhpCallbackParameterized)shutdownCallbacks.Dequeue()).Invoke();
				}
			}
			return null;
		}

		#endregion

		#region PhpObject Finalization

		/// <summary>
		/// List of weak references to <see cref="DObject"/> instances that should be finalized when current request is over.
		/// Lazily constructed.
		/// </summary>
		/// <remarks>
		/// Objects are finalized as their memory is about to be reclaimed by the GC during the request/app
		/// processing. This list ensures that all instantiated objects are finalized before the request is
		/// finished.
		/// </remarks>
		private List<WeakReference> objectsToBeFinalized;

		private int finalizationCheckCounter;

		/// <summary>
		/// Registers a <see cref="DObject"/> instance for finalization.
		/// </summary>
		/// <param name="obj">The object.</param>
		[Emitted]
		public void RegisterDObjectForFinalization(DObject/*!*/ obj)
		{
			Debug.Assert(obj != null);

#if SILVERLIGHT
			bool registerFinalizer = true;
#else
			bool registerFinalizer = !System.Runtime.Remoting.RemotingServices.IsTransparentProxy(this);
#endif

			if (registerFinalizer)
			{
				if (objectsToBeFinalized == null) objectsToBeFinalized = new List<WeakReference>();
				objectsToBeFinalized.Add(new WeakReference(obj, true));

				if (++finalizationCheckCounter > 65536)
				{
					finalizationCheckCounter = 0;

					int count = objectsToBeFinalized.Count;
					long list_memory = count * 3 * Marshal.SizeOf(typeof(IntPtr));

					// don't let the list occupy more than 10% of allocated memory
					if (list_memory > GC.GetTotalMemory(false) / 10)
					{
						List<WeakReference> new_list = new List<WeakReference>(count / 4);
						for (int i = 0; i < count; i++)
						{
							WeakReference reference = objectsToBeFinalized[i];
							if (reference.IsAlive) new_list.Add(reference);
						}

						objectsToBeFinalized = new_list;
					}
				}
			}
		}

		/// <summary>
		/// Calls <c>__destruct</c> on <see cref="DObject"/> instances that have been created in this context.
		/// </summary>
		/// <param name="_">Dummy.</param>
		internal object FinalizePhpObjects(object _)
		{
			if (objectsToBeFinalized != null)
			{
				for (int i = 0; i < objectsToBeFinalized.Count; i++)
				{
					WeakReference reference = objectsToBeFinalized[i];

					// remove object from finalization list to prevent its repeated finalization
					// (for the case the finalization is interrupted by exception and resumed later):
					objectsToBeFinalized[i] = null;

					DObject obj = null;
					if (reference.IsAlive)
					{
						try
						{
							obj = (DObject)reference.Target;
						}
						catch (InvalidOperationException)
						{ }
					}

					if (obj != null) obj.Dispose();
				}
				objectsToBeFinalized = null;
			}
			return null;
		}

		#endregion

		#region Graph Walking Callbacks

		/// <summary>
		/// Cached class context used when acquiring objects.
		/// </summary>
		private DTypeDesc classContext;

		/// <summary>
		/// If <see cref="classContext"/> has not been set, this field is <B>true</B>
		/// (<B>null</B> is a valid value for <see cref="classContext"/>).
		/// </summary>
		private bool classContextIsValid;

		/// <summary>
		/// Called for each object that implements the <see cref="IPhpObjectGraphNode"/> interface when
		/// releasing an object graph (e.g. when storing variables to InProc session).
		/// </summary>
		/// <param name="node">The object that should be released.</param>
		/// <param name="context">Current <see cref="ScriptContext"/>.</param>
		/// <returns>The released object (might be different from <paramref name="node"/>).</returns>
		internal object ReleaseObject(IPhpObjectGraphNode node, ScriptContext context)
		{
			DObject obj = node as DObject;
			if (obj != null)
			{
				bool sleep_called;
				if (!classContextIsValid)
				{
					classContext = PhpStackTrace.GetClassContext();
					classContextIsValid = true;
				}

				PhpArray sleep_result = obj.Sleep(classContext, context, out sleep_called);

				// if __sleep does not return a valid array, the object dies completely
				if (sleep_called && sleep_result == null) return null;

				if (sleep_result != null)
				{
					// create a new instance
					DObject new_instance = (DObject)obj.TypeDesc.New(this);

					// copy values of the properties whose names have been returned by __sleep
					foreach (KeyValuePair<IntStringKey, object> pair in sleep_result)
					{
						string name = Convert.ObjectToString(pair.Value);
						string type_name;
						PhpMemberAttributes visibility;
						string field_name = Serialization.ParsePropertyName(name, out type_name, out visibility);

						DTypeDesc declarer;
						if (type_name == null)
						{
							declarer = obj.TypeDesc;
						}
						else
						{
							declarer = ResolveType(type_name);
							if (declarer == null) declarer = obj.TypeDesc;
						}

						// copy the field value
						object val = obj.GetProperty(field_name, declarer);
						new_instance.SetProperty(field_name, val, declarer);
					}

					return new_instance;
				}

				return obj;
			}

			// nullify resources
			PhpResource res = node as PhpResource;
			if (res != null) return 0;

			return node;
		}

		/// <summary>
		/// Called for each object that implements the <see cref="IPhpObjectGraphNode"/> interface when
		/// acquiring an object graph (e.g. when retrieving variables from InProc session).
		/// </summary>
		/// <param name="node">The object that should be acquired.</param>
		/// <param name="context">Current <see cref="ScriptContext"/>.</param>
		/// <returns>The acquired object (always <paramref name="node"/>).</returns>
		internal object AcquireObject(IPhpObjectGraphNode node, ScriptContext context)
		{
			DObject obj = node as DObject;
			if (obj != null)
			{
				if (!classContextIsValid)
				{
					classContext = PhpStackTrace.GetClassContext();
					classContextIsValid = true;
				}

				obj.Wakeup(classContext, context);
			}
			return node;
		}

		/// <summary>
		/// Call <c>__wakeup</c> recursively on <see cref="PhpObject"/>s contained in the array.
		/// </summary>
		internal void AcquireArray(PhpArray array)
		{
			Debug.Assert(array != null);

			classContextIsValid = false;
			array.Walk(new PhpWalkCallback(AcquireObject), this);
		}

		/// <summary>
		/// Call <c>__sleep</c> recursively on <see cref="PhpObject"/>s contained in the array.
		/// </summary>
		internal void ReleaseArray(PhpArray array)
		{
			Debug.Assert(array != null);

			classContextIsValid = false;
			array.Walk(new PhpWalkCallback(ReleaseObject), this);
		}

		#endregion

		#region Static Locals

		/// <summary>
		/// Gets a value of a static local. For internal use only.
		/// </summary>
		/// <param name="id">Compound name of a static local (in format {simple function name}${local name index}).</param>
		/// <returns>Value of the local or <B>null</B>.</returns>
		[Emitted, EditorBrowsable(EditorBrowsableState.Never)]
		public PhpReference GetStaticLocal(string/*!*/ id)
		{
			if (staticLocals == null) staticLocals = new Dictionary<string, PhpReference>();
			PhpReference result = null;
			staticLocals.TryGetValue(id, out result);
			return result;
		}

		/// <summary>
		/// Adds a local into the static local table. For internal use only.
		/// </summary>
		/// <param name="id">Compound name of a static local (in format {simple function name}${local name index}).</param>
		/// <param name="value">A value of local (not a reference).</param>
		/// <returns>A reference containing the <paramref name="value"/>.</returns>
		[Emitted, EditorBrowsable(EditorBrowsableState.Never)]
		public PhpReference/*!*/ AddStaticLocal(string/*!*/ id, object value)
		{
			Debug.Assert(staticLocals != null && !(value is PhpReference));

			PhpReference result = new PhpReference(value);
			staticLocals.Add(id, result);
			return result;
		}

		#endregion

		#region Initialization of requests and applications

		/// <summary>
		/// Sets a main script of the application.
		/// </summary>
        /// <param name="script">The script related to the <c>sourceFile</c>.</param>
        /// <param name="sourceFile">The file path of the <c>script</c>.</param>
		internal void DefineMainScript(ScriptInfo/*!*/ script, PhpSourceFile/*!*/ sourceFile)
		{
			Debug.Assert(mainScriptFile == null, "Main script redefined.");

			scripts.Add(sourceFile, script);
			mainScriptFile = sourceFile;
		}

		#endregion

		#region Eval

		/// <summary>
		/// Contains captured line number.
		/// </summary>
		[Emitted]
		public int EvalLine = -1;

		/// <summary>
		/// Contains captured column number.
		/// </summary>
		[Emitted]
		public int EvalColumn = -1;

		/// <summary>
		/// Contains captured eval id.
		/// </summary>
		[Emitted]
		public int EvalId = -1;

		/// <summary>
		/// Contains captured relative source file packed path.
		/// </summary>
		[Emitted]
		public string EvalRelativeSourcePath;

		/// <summary>
		/// Gets captured eval info. Eval info on a script context is updated by 
		/// a code injected to generated code by the compiler.
		/// </summary>
		/// <returns>The captured eval info.</returns>
		/// <remarks>
		/// Eval info is captured before a class library function call if it is implemented with option 
		/// <see cref="FunctionImplOptions.CaptureEvalInfo"/> and a part of it (line, column) is updated
		/// in every sequence point in debug mode. Should be called on the beginning of the function implementetion.
		/// Can be called even if the info will not be needed eventually.
		/// </remarks>
		public SourceCodeDescriptor GetCapturedSourceCodeDescriptor()
		{
			return new SourceCodeDescriptor(EvalRelativeSourcePath, EvalId, EvalLine, EvalColumn);
		}

		/// <summary>
		/// Clears the info.
		/// </summary>
		internal void ClearCapturedSourceCodeDescriptor()
		{
			EvalLine = EvalColumn = EvalId = -1;
			EvalRelativeSourcePath = null;
		}

		#endregion

		#region Setter Chains

		/// <summary>
		/// A stack of <see cref="PhpRuntimeChain"/>s that are currently being processed.
		/// </summary>
		/// <remarks>
		/// <para>
		/// <see cref="PhpRuntimeChain"/>s are maintained in a stack in order to support nested operator chains.
		/// </para>
		/// <para>
		/// Every time the <see cref="Operators.EnsurePropertyIsArray"/> or <see cref="Operators.EnsurePropertyIsObject"/>
		/// is called on a system class with overloaded member access (<B>__set</B> and/or <B>__get</B>), a new
		/// <see cref="PhpRuntimeChain"/> is pushed to the stack (<see cref="BeginSetterChain"/>).
		/// </para>
		/// <para>
		/// If a setter chain has been created, the consequent chaining operators just record field and item names
		/// to it (<see cref="ExtendSetterChain"/>). The last operator calls <see cref="FinishSetterChain"/> which
		/// causes the recorded data to be passed to the <B>__set</B> handler and the <see cref="PhpRuntimeChain"/>
		/// popped from the stack.
		/// </para>
		/// </remarks>
		private Stack setterChainStack; // GENERICS: <SetterChain>

		/// <summary>
		/// Singleton instance of <see cref="PhpObject"/> passed among chaining operators to indicate that
		/// they are part of a setter chain.
		/// </summary>
		internal static PhpObject SetterChainSingletonObject = new Library.stdClass(null);

		/// <summary>
		/// Singleton instance of <see cref="PhpArray"/> passed among chaining operators to indicate that
		/// they are part of a setter chain.
		/// </summary>
		internal static PhpSetterChainArray SetterChainSingletonArray = new PhpSetterChainArray();

		/// <summary>
		/// Creates a new setter chain. Called from <see cref="Operators.EnsurePropertyIsArray"/> and
		/// <see cref="Operators.EnsurePropertyIsObject"/> when a field of a system class with overloaded member
		/// access should be ensured.
		/// </summary>
		/// <param name="obj">The instance on which the setter chain is applied.</param>
		internal void BeginSetterChain(DObject obj)
		{
			PhpRuntimeChain chain = new PhpRuntimeChain(obj);

			if (setterChainStack == null) setterChainStack = new Stack();
			setterChainStack.Push(chain);
		}

		/// <summary>
		/// Adds a new element to the current setter chain. Called from chaining operators that can follow
		/// <see cref="Operators.EnsurePropertyIsArray"/> or <see cref="Operators.EnsurePropertyIsObject"/> when
		/// they detect they are part of a setter chain.
		/// </summary>
		/// <param name="elem">The setter chain element to add.</param>
		internal void ExtendSetterChain(RuntimeChainElement elem)
		{
			PhpRuntimeChain chain = (PhpRuntimeChain)setterChainStack.Peek();
			chain.Add(elem);
		}

		/// <summary>
		/// Pops the current setter chain without invoking the setter.
		/// </summary>
		/// <param name="quiet">Whether not to report an error.</param>
		internal void AbortSetterChain(bool quiet)
		{
			setterChainStack.Pop();
			if (!quiet)
				PhpException.Throw(PhpError.Error, CoreResources.GetString("undefined_property_when_access_overloaded"));
		}

		/// <summary>
		/// Passes the current setter chain to the setter and pops the setter chain. Called from chaining
		/// operators that conclude the chain that might contain <see cref="Operators.EnsurePropertyIsArray"/> or
		/// <see cref="Operators.EnsurePropertyIsObject"/> when they detect they are part of a setter chain.
		/// </summary>
		/// <param name="value">The value that should be assigned to the field/item represented by the last
		/// setter chain element.</param>
		internal void FinishSetterChain(object value)
		{
			Debug.Assert(!(value is PhpReference));

			PhpRuntimeChain chain = (PhpRuntimeChain)setterChainStack.Pop();

			Debug.Assert(chain.Variable is PhpObject, "SetterChain must begin with a PhpObject");
			PhpObject instance = (PhpObject)chain.Variable;

			if (!instance.InvokeSetter(chain.Chain, value))
			{
				// if the setter could not be invoked, pretend that we didn't even try to arrange
				// this silly meat cube O:-]
				PhpException.Throw(PhpError.Error, CoreResources.GetString("undefined_property_when_access_overloaded"));
			}
		}

		#endregion
		
		#region Interop

		/// <summary>
		/// Calls a PHP function. Intended for use from other languages.
		/// </summary>
		/// <param name="functionName">Name of the function.</param>
		/// <param name="arguments">Arguments.</param>
		/// <returns>Function call result.</returns>
		public PhpReference/*!*/ Call(string/*!*/ functionName, params object[] arguments)
		{
			return Call(functionName, null, null, arguments);
		}

		/// <summary>
		/// Calls a PHP function. Intended for use from other languages.
		/// </summary>
		/// <param name="functionName">Name of the function.</param>
		/// <param name="namingContext">Naming context within which the name is resolved (namespaces and aliases). Can be <B>null</B>.</param>
		/// <param name="callerLocalVariables">Table of the caller's variables. Can be <B>null</B>.</param>
		/// <param name="arguments">Arguments.</param>
		/// <returns>Function call result.</returns>
		public PhpReference/*!*/ Call(string/*!*/ functionName, NamingContext namingContext,
			Dictionary<string, object> callerLocalVariables, params object[] arguments)
		{
			if (functionName == null)
				throw new ArgumentNullException("functionName");
			if (arguments == null)
				throw new ArgumentNullException("arguments");

			Stack.AddFrame(arguments);
			return Call(callerLocalVariables, namingContext, functionName, this);
		}

		/// <summary>
		/// Creates and instance of a PHP class. Intended for use from other languages.
		/// </summary>
		/// <param name="className">Name of the class to instantiate.</param>
		/// <param name="ctorArguments">Constructor arguments.</param>
		/// <returns>The new instance or <B>null</B> on error.</returns>
		public object NewObject(string/*!*/ className, params object[] ctorArguments)
		{
			return NewObject(className, null, ctorArguments);
		}
		
		/// <summary>
		/// Creates and instance of a PHP class. Intended for use from other languages.
		/// </summary>
		/// <param name="className">Name of the class to instantiate.</param>
		/// <param name="namingContext">Naming context within which the name is resolved (namespaces and aliases). Can be <B>null</B>.</param>
		/// <param name="ctorArguments">Constructor arguments.</param>
		/// <returns>The new instance or <B>null</B> on error.</returns>
		public object NewObject(string/*!*/ className, NamingContext namingContext, params object[] ctorArguments)
		{
			if (className == null)
				throw new ArgumentNullException("className");
			if (ctorArguments == null)
				throw new ArgumentNullException("ctorArguments");

			Stack.AddFrame(ctorArguments);
			return Operators.New(ResolveType(className), null, this, namingContext);
		}

		public T Call<T>(string/*!*/ functionName, NamingContext namingContext,
			Dictionary<string, object> callerLocalVariables, params object[] arguments)
			where T : class
		{
			PhpReference rf = Call(functionName, namingContext, callerLocalVariables, arguments);
			if (rf.Value == null)
				return null;
			else
				return DuckTyping.Instance.ImplementDuckType<T>(rf.Value);
		}

		public T Call<T>(string/*!*/ functionName, params object[] arguments)
			where T : class
		{
			return Call<T>(functionName, null, null, arguments);
		}

		/// <summary>
		/// To be used with DuckType(GlobalFunctions=true) only!
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T NewObject<T>()
		{
			return DuckTyping.Instance.ImplementGlobalDuckType<T>();
		}

        /// <summary>
        /// Creates new object with given class name and arguments and then wraps it into
        /// a duck type specified in generic type arguments.
        /// </summary>
        /// <typeparam name="T">Duck type interface to be used for wrapping.</typeparam>
        /// <param name="className">Class name which will be used for new object creation.</param>
        /// <param name="ctorArguments">Constructor arguments to be used.</param>
        /// <returns>Dynamic object wrapped into static wrapper.</returns>
		public T NewObject<T>(string/*!*/ className, params object[] ctorArguments)
		{
			return NewObject<T>(className, null, ctorArguments);
		}

        /// <summary>
        /// Creates new object with given class name, naming context and arguments and then wraps it into
        /// a duck type specified in generic type arguments.
        /// </summary>
        /// <typeparam name="T">Duck type interface to be used for wrapping.</typeparam>
        /// <param name="className">Class name which will be used for new object creation.</param>
        /// <param name="namingContext">Naming context.</param>
        /// <param name="ctorArguments">Constructor arguments to be used.</param>
        /// <returns>Dynamic object wrapped into static wrapper.</returns>
		public T NewObject<T>(string/*!*/ className, NamingContext namingContext, params object[] ctorArguments)
		{
            //create new argument array and dig wrapped values out of it
            object[] newCtorArgs = new object[ctorArguments.Length];

            for (int i = 0; i < newCtorArgs.Length; i++)
            {
                IDuckType duck = ctorArguments[i] as IDuckType;
                if (duck != null)
                    newCtorArgs[i] = duck.OriginalObject;
                else
                    newCtorArgs[i] = ctorArguments[i];                    
            }

            object o = NewObject(className, namingContext, newCtorArgs);
			return DuckTyping.Instance.ImplementDuckType<T>(o);
		}

        public T WrapObject<T>(object o)
        {
            return DuckTyping.Instance.ImplementDuckType<T>(o);
        }

		#endregion
	}
}
