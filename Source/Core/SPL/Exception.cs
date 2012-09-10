/*

 Copyright (c) 2005-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Text;
using System.Collections;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using PHP.Core;
using PHP.Core.Reflection;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Library.SPL
{
	/// <summary>
	/// Base class for PHP user exceptions.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The class implements PHP5 class Exception which PHP "header" declaration follows:
	/// <code>
	/// class Exception
	/// {
	///   protected $message = 'Unknown exception';   // exception message
	///   protected $code = 0;                        // user defined exception code
	///   protected $file;                            // source filename of exception
	///   protected $line;                            // source line of exception
	///   protected $column;                          // source column of exception
	///   private $trace;                             // an array containing the trace
	///
	///   function __construct($message = null, $code = 0);
	///
	///   final function getMessage();                // message of exception 
	///   final function getCode();                   // code of exception
	///   final function getFile();                   // source file name
	///   final function getLine();                   // source file line
	///   final function getColumn();                 // source file column
	///   final function getTrace();                  // the PhpArray representation of the trace 
	///   final function getTraceAsString();          // formated string of trace
	///
	///   function __toString();                      // formated string for display
	/// } 
	/// </code>
	/// </para>
	/// <para>
	/// The stack trace is captured in the constructor (as in Java) not by throw statement (as in C#).
	/// </para>
	/// </remarks>
	[ImplementsType]
#if !SILVERLIGHT
	[Serializable]
#endif
	public class Exception : PhpObject
	{
		/// <summary>
		/// Contains a trace formatted to the string or a <B>null</B> reference.
		/// Needn't to be serialized.
		/// </summary>
		private string stringTraceCache;
        private object previous;

		/// <summary>
		/// Invoked when the instance is created (not called when unserialized).
		/// </summary>
		protected override void InstanceCreated(ScriptContext context)
		{
			base.InstanceCreated(context);

			PhpStackTrace trace = new PhpStackTrace(context, 1);
			PhpStackFrame frame = trace.GetFrame(0);
			Debug.Assert(frame != null);

			this.file.Value = frame.File;
			this.line.Value = frame.Line;
			this.column.Value = frame.Column;
			this.trace.Value = trace.GetUserTrace();
		}

		/// <summary>
		/// Gets the default string representation of the exception.
		/// </summary>
		internal string BaseToString()
		{
			string type_name = DTypeDesc.GetFullName(this.GetType(), new StringBuilder()).ToString();
			int int_line = Core.Convert.ObjectToInteger(line.Value);
			int int_column = Core.Convert.ObjectToInteger(column.Value);
			string str_file = Core.Convert.ObjectToString(file.Value);

			if (int_line > 0 && int_column > 0 && str_file != String.Empty)
			{
				return CoreResources.GetString("stringified_exception_debug",
				  type_name,
				  Core.Convert.ObjectToString(message.Value),
				  str_file, int_line, int_column,
				  getTraceAsString(null));
			}
			else
			{
				return CoreResources.GetString("stringified_exception",
				  type_name,
				  Core.Convert.ObjectToString(message.Value),
				  getTraceAsString(null));
			}
		}

        #region Throw helpers

        /// <summary>
        /// Throws <see cref="PhpUserException"/>. Internal <see cref="Exception"/> is created using given <paramref name="factory"/>.
        /// </summary>
        /// <param name="factory">Factory to create new instance of <see cref="Exception"/>.</param>
        /// <param name="context">Current <see cref="ScriptContext"/> provided to factory and <see cref="Exception.__construct"/>.</param>
        /// <param name="message">First parameter to be passed to <see cref="Exception.__construct"/>.</param>
        /// <param name="code">Second parameter to be passed to <see cref="Exception.__construct"/>.</param>
        /// <param name="previous">Thhird parameter to be passed to <see cref="Exception.__construct"/>.</param>
        public static void ThrowSplException(Func<ScriptContext, Exception>/*!*/factory, ScriptContext/*!*/context, object message, object code, object previous)
        {
            Debug.Assert(context != null);

            var e = factory(context);
            e.__construct(context, message, code, previous);

            //
            throw new PhpUserException(e);
        }

        #endregion

		#region PHP Fields

		/// <summary>
		/// A message.
		/// </summary>
		protected PhpReference message = new PhpSmartReference();

		/// <summary>
		/// A code.
		/// </summary>
		protected PhpReference code = new PhpSmartReference();

		/// <summary>
		/// A source file where the exception has been thrown.
		/// </summary>
		protected PhpReference file = new PhpSmartReference();

		/// <summary>
		/// A line in the source file where the exception has been thrown.
		/// </summary>
		protected PhpReference line = new PhpSmartReference();

		/// <summary>
		/// A column in the source file where the exception has been thrown.
		/// </summary>
		protected PhpReference column = new PhpSmartReference();

		/// <summary>
		/// A user stack trace in form of <see cref="PhpArray"/>.
		/// </summary>
		private PhpReference trace = new PhpSmartReference();

		#endregion

		#region PHP Methods

		/// <summary>
		/// Creates an instance of user exception.
		/// </summary>
		/// <param name="context">Current <see cref="ScriptContext"/>.</param>
		/// <param name="message">A message to be associated with the exception.</param>
		/// <param name="code">A code to be associated with the exception.</param>
        /// <param name="previous">The previous exception used for the exception chaining.</param>
		/// <returns>A <b>null</b> reference (void in PHP).</returns>
		[ImplementsMethod]
		public virtual object __construct(ScriptContext context, [Optional] object message, [Optional] object code, [Optional] object previous)
		{
            this.message.Value = (message == Arg.Default || message == Type.Missing) ? CoreResources.GetString("default_exception_message") : message;
			this.code.Value = (code == Arg.Default || code == Type.Missing) ? 0 : code;
            this.previous = (previous == Arg.Default || previous == Type.Missing) ? null : previous;

            Debug.Assert(this.previous == null || (this.previous is DObject && ((DObject)this.previous).RealObject is Exception));

            // stack is already captured by CLR ctor //

			return null;
		}

		/// <summary>
		/// Converts the instance to a string.
		/// </summary>
		/// <returns>The string containing formatted trace.</returns>
		[ImplementsMethod]
		public object __toString(ScriptContext context)
		{
			return BaseToString();
		}

		/// <summary>
		/// Gets a source file where the exception has been thrown.
		/// </summary>
		/// <returns>The source file.</returns>
		[ImplementsMethod]
		public object getFile(ScriptContext context)
		{
			return file.Value;
		}

		/// <summary>
		/// Gets a line in the source file where the exception has been thrown. 
		/// </summary>
		/// <returns>The line.</returns>
		[ImplementsMethod]
		public object getLine(ScriptContext context)
		{
			return line.Value;
		}

		/// <summary>
		/// Gets a column in the source file where the exception has been thrown.
		/// </summary>
		/// <returns>The column.</returns>
		[ImplementsMethod]
		public object getColumn(ScriptContext context)
		{
			return column.Value;
		}

		/// <summary>
		/// Gets the code specified in the constructor.
		/// </summary>
		/// <returns>The code.</returns>
		[ImplementsMethod]
		public object getCode(ScriptContext context)
		{
			return code.Value;
		}

		/// <summary>
		/// Gets a message.
		/// </summary>
		/// <returns>The message set by the constructor.</returns>
		[ImplementsMethod]
		public object getMessage(ScriptContext context)
		{
			return message.Value;
		}

        /// <summary>
        /// Returns previous <see cref="Exception"/> (the third parameter of <see cref="__construct"/>).
        /// </summary>
        /// <returns></returns>
        [ImplementsMethod]
        public object getPrevious(ScriptContext context)
        {
            return previous;
        }

		/// <summary>
		/// Returns a trace of the stack in the moment the exception was thrown.
		/// </summary>
		/// <returns>The trace.</returns>
		[ImplementsMethod]
		public object getTrace(ScriptContext context)
		{
			return trace.Value;
		}

		/// <summary>
		/// Returns a trace formatted in a form of a string.
		/// </summary>
		/// <returns>The formatted trace.</returns>
		[ImplementsMethod]
		public object getTraceAsString(ScriptContext context)
		{
			if (stringTraceCache == null)
			{
				PhpArray array = trace.Value as PhpArray;
				stringTraceCache = (array != null) ? PhpStackTrace.FormatUserTrace(array) : String.Empty;
			}

			return stringTraceCache;
		}

		#endregion

		#region Implementation Details

		/// <summary>
		/// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
		/// </summary>
		/// <param name="typeDesc">The type desc to populate.</param>
		internal static void __PopulateTypeDesc(PhpTypeDesc typeDesc)
		{
			typeDesc.AddMethod("__construct", PhpMemberAttributes.Public, __construct);
			typeDesc.AddMethod("__toString", PhpMemberAttributes.Public, __toString);
			typeDesc.AddMethod("getMessage", PhpMemberAttributes.Public, getMessage);
			typeDesc.AddMethod("getCode", PhpMemberAttributes.Public, getCode);
			typeDesc.AddMethod("getFile", PhpMemberAttributes.Public, getFile);
			typeDesc.AddMethod("getTrace", PhpMemberAttributes.Public, getTrace);
            typeDesc.AddMethod("getPrevious", PhpMemberAttributes.Public, getPrevious);
			typeDesc.AddMethod("getTraceAsString", PhpMemberAttributes.Public, getTraceAsString);
			typeDesc.AddMethod("getLine", PhpMemberAttributes.Public, getLine);
			typeDesc.AddMethod("getColumn", PhpMemberAttributes.Public, getColumn);

			typeDesc.AddProperty("message", PhpMemberAttributes.Protected, __get_message, __set_message);
			typeDesc.AddProperty("code", PhpMemberAttributes.Protected, __get_code, __set_code);
			typeDesc.AddProperty("file", PhpMemberAttributes.Protected, __get_file, __set_file);
			typeDesc.AddProperty("line", PhpMemberAttributes.Protected, __get_line, __set_line);
			typeDesc.AddProperty("column", PhpMemberAttributes.Protected, __get_column, __set_column);
		}

		/// <summary>
		/// For internal purposes only.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public Exception(ScriptContext context, bool newInstance)
			: base(context, newInstance)
		{
		}

		/// <summary>
		/// For internal purposes only.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public Exception(ScriptContext context, DTypeDesc caller)
			: base(context, caller)
		{
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object __construct(object instance, PhpStack stack)
		{
			object message = stack.PeekValueOptional(1);
			object code = stack.PeekValueOptional(2);
            object previous = stack.PeekValueOptional(3);
			stack.RemoveFrame();
			return ((Exception)instance).__construct(stack.Context, message, code, previous);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object __toString(object instance, PhpStack stack)
		{
			stack.RemoveFrame();
			return ((Exception)instance).__toString(stack.Context);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object getMessage(object instance, PhpStack stack)
		{
			stack.RemoveFrame();
			return ((Exception)instance).getMessage(stack.Context);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
        public static object getPrevious(object instance, PhpStack stack)
		{
			stack.RemoveFrame();
            return ((Exception)instance).getPrevious(stack.Context);
		}

        [EditorBrowsable(EditorBrowsableState.Never)]
		public static object getTrace(object instance, PhpStack stack)
		{
			stack.RemoveFrame();
			return ((Exception)instance).getTrace(stack.Context);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object getCode(object instance, PhpStack stack)
		{
			stack.RemoveFrame();
			return ((Exception)instance).getCode(stack.Context);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object getFile(object instance, PhpStack stack)
		{
			stack.RemoveFrame();
			return ((Exception)instance).getFile(stack.Context);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object getLine(object instance, PhpStack stack)
		{
			stack.RemoveFrame();
			return ((Exception)instance).getLine(stack.Context);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object getColumn(object instance, PhpStack stack)
		{
			stack.RemoveFrame();
			return ((Exception)instance).getColumn(stack.Context);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object getTraceAsString(object instance, PhpStack stack)
		{
			stack.RemoveFrame();
			return ((Exception)instance).getTraceAsString(stack.Context);
		}

		private static object __get_message(object instance) { return ((Exception)instance).message; }
		private static void __set_message(object instance, object value) { ((Exception)instance).message = (PhpReference)value; }

		private static object __get_code(object instance) { return ((Exception)instance).code; }
		private static void __set_code(object instance, object value) { ((Exception)instance).code = (PhpReference)value; }

		private static object __get_file(object instance) { return ((Exception)instance).file; }
		private static void __set_file(object instance, object value) { ((Exception)instance).file = (PhpReference)value; }

		private static object __get_line(object instance) { return ((Exception)instance).line; }
		private static void __set_line(object instance, object value) { ((Exception)instance).line = (PhpReference)value; }

		private static object __get_column(object instance) { return ((Exception)instance).column; }
		private static void __set_column(object instance, object value) { ((Exception)instance).column = (PhpReference)value; }

		#endregion

		#region Serialization (CLR only)
#if !SILVERLIGHT

		/// <summary>
		/// Deserializing constructor.
		/// </summary>
		protected Exception(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

#endif
		#endregion
	}

    /// <summary>
    /// Exception thrown if an error which can only be found on runtime occurs.
    /// </summary>
    [ImplementsType]
#if !SILVERLIGHT
    [Serializable]
#endif
    public class RuntimeException : Exception
    {
        #region Implementation Details

        /// <summary>
        /// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
        /// </summary>
        /// <param name="typeDesc">The type desc to populate.</param>
        internal static new void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RuntimeException(ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RuntimeException(ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected RuntimeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion
    }

    /// <summary>
    /// An Error Exception.
    /// </summary>
    [ImplementsType]
#if !SILVERLIGHT
    [Serializable]
#endif
    public class ErrorException : Exception
    {
        private int severity;
        
        #region __construct, getSeverity

        [ImplementsMethod]
        public object __construct(ScriptContext/*!*/context,
            [Optional]object message /*""*/, [Optional]object code /*0*/, [Optional]object severity /*1*/,
            [Optional]object filename /*__FILE__*/, [Optional]object lineno /*__LINE__*/,
            [Optional]object previous /*NULL*/ )
        {
            base.__construct(context, message, code, previous);

            this.severity = (severity == Arg.Default) ? 1 : PHP.Core.Convert.ObjectToInteger(severity);
            if (filename != Arg.Default) this.file.Value = PHP.Core.Convert.ObjectToString(filename);
            if (lineno != Arg.Default) this.line.Value = PHP.Core.Convert.ObjectToInteger(filename);

            return null;
        }

        /// <summary>
        /// Returns the severity of the exception.
        /// </summary>
        [ImplementsMethod]
        public object getSeverity(ScriptContext/*!*/context)
        {
            return this.severity;
        }

        #endregion

        #region Implementation Details
        
        /// <summary>
        /// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
        /// </summary>
        /// <param name="typeDesc">The type desc to populate.</param>
        internal static new void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ErrorException (ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ErrorException (ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static new object __construct(object instance, PhpStack stack)
        {
            object message = stack.PeekValueOptional(1);
            object code = stack.PeekValueOptional(2);
            object severity = stack.PeekValueOptional(3);
            object filename = stack.PeekValueOptional(4);
            object lineno = stack.PeekValueOptional(5);
            object previous = stack.PeekValueOptional(6);
            stack.RemoveFrame();
            return ((ErrorException)instance).__construct(stack.Context, message, code, severity, filename, lineno, previous);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getSeverity(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ErrorException)instance).getSeverity(stack.Context);
        }

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected ErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion
    }

    /// <summary>
    /// Exception that represents error in the program logic.
    /// This kind of exceptions should directly lead to a fix in your code.
    /// </summary>
    [ImplementsType]
#if !SILVERLIGHT
    [Serializable]
#endif
    public class LogicException : Exception
    {
        #region Implementation Details

		/// <summary>
		/// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
		/// </summary>
		/// <param name="typeDesc">The type desc to populate.</param>
        internal static new void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        { }

        		/// <summary>
		/// For internal purposes only.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public LogicException(ScriptContext context, bool newInstance)
			: base(context, newInstance)
		{
		}

		/// <summary>
		/// For internal purposes only.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
        public LogicException(ScriptContext context, DTypeDesc caller)
			: base(context, caller)
		{
		}

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
		/// Deserializing constructor.
		/// </summary>
		protected LogicException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

#endif
		#endregion
    }

    /// <summary>
    /// Exception thrown if an argument does not match with the expected value.
    /// </summary>
    [ImplementsType]
#if !SILVERLIGHT
    [Serializable]
#endif
    public class InvalidArgumentException : LogicException
    {
        #region Implementation Details

        /// <summary>
        /// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
        /// </summary>
        /// <param name="typeDesc">The type desc to populate.</param>
        internal static new void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public InvalidArgumentException (ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public InvalidArgumentException (ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected InvalidArgumentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion
    }

    /// <summary>
    /// Exception thrown when an illegal index was requested.
    /// </summary>
    [ImplementsType]
#if !SILVERLIGHT
    [Serializable]
#endif
    public class OutOfRangeException  : LogicException
    {
        #region Implementation Details

        /// <summary>
        /// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
        /// </summary>
        /// <param name="typeDesc">The type desc to populate.</param>
        internal static new void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public OutOfRangeException(ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public OutOfRangeException(ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #endregion
    }

    /// <summary>
    /// Exception thrown if a callback refers to an undefined function or if some arguments are missing.
    /// </summary>
    [ImplementsType]
#if !SILVERLIGHT
    [Serializable]
#endif
    public class BadFunctionCallException : LogicException
    {
        #region Implementation Details

        /// <summary>
        /// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
        /// </summary>
        /// <param name="typeDesc">The type desc to populate.</param>
        internal static new void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public BadFunctionCallException(ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public BadFunctionCallException(ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected BadFunctionCallException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion
    }

    /// <summary>
    /// Exception thrown if a callback refers to an undefined method or if some arguments are missing.
    /// </summary>
    [ImplementsType]
#if !SILVERLIGHT
    [Serializable]
#endif
    public class BadMethodCallException : BadFunctionCallException
    {
        #region Implementation Details

        /// <summary>
        /// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
        /// </summary>
        /// <param name="typeDesc">The type desc to populate.</param>
        internal static new void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public BadMethodCallException(ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public BadMethodCallException(ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected BadMethodCallException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion
    }

    /// <summary>
    /// Exception thrown if a length is invalid.
    /// </summary>
    [ImplementsType]
#if !SILVERLIGHT
    [Serializable]
#endif
    public class LengthException : LogicException
    {
        #region Implementation Details

        /// <summary>
        /// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
        /// </summary>
        /// <param name="typeDesc">The type desc to populate.</param>
        internal static new void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public LengthException(ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public LengthException(ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected LengthException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion
    }

    /// <summary>
    /// Exception thrown to indicate range errors during program execution.
    /// Normally this means there was an arithmetic error other than under/overflow.
    /// </summary>
    [ImplementsType]
#if !SILVERLIGHT
    [Serializable]
#endif
    public class RangeException : RuntimeException
    {
        #region Implementation Details

        /// <summary>
        /// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
        /// </summary>
        /// <param name="typeDesc">The type desc to populate.</param>
        internal static new void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RangeException(ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RangeException(ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected RangeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion
    }

    /// <summary>
    /// Exception thrown if a value is not a valid key.
    /// </summary>
    [ImplementsType]
#if !SILVERLIGHT
    [Serializable]
#endif
    public class OutOfBoundsException : RuntimeException
    {
        #region Implementation Details

        /// <summary>
        /// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
        /// </summary>
        /// <param name="typeDesc">The type desc to populate.</param>
        internal static new void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public OutOfBoundsException(ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public OutOfBoundsException(ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #endregion
    }

    /// <summary>
    /// Exception thrown when adding an element to a full container.
    /// </summary>
    [ImplementsType]
#if !SILVERLIGHT
    [Serializable]
#endif
    public class OverflowException : RuntimeException
    {
        #region Implementation Details

        /// <summary>
        /// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
        /// </summary>
        /// <param name="typeDesc">The type desc to populate.</param>
        internal static new void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public OverflowException(ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public OverflowException(ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected OverflowException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion
    }
    
    /// <summary>
    /// Exception thrown when performing an invalid operation on an empty container,
    /// such as removing an element.
    /// </summary>
    [ImplementsType]
#if !SILVERLIGHT
    [Serializable]
#endif
    public class UnderflowException : RuntimeException
    {
        #region Implementation Details

        /// <summary>
        /// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
        /// </summary>
        /// <param name="typeDesc">The type desc to populate.</param>
        internal static new void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public UnderflowException(ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public UnderflowException(ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected UnderflowException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion
    }

    /// <summary>
    /// Exception thrown if a value does not match with a set of values.
    /// Typically this happens when a function calls another function and expects the return value
    /// to be of a certain type or value not including arithmetic or buffer related errors.
    /// </summary>
    [ImplementsType]
#if !SILVERLIGHT
    [Serializable]
#endif
    public class UnexpectedValueException  : RuntimeException
    {
        #region Implementation Details

        /// <summary>
        /// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
        /// </summary>
        /// <param name="typeDesc">The type desc to populate.</param>
        internal static new void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        { throw new NotImplementedException(); }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public UnexpectedValueException (ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public UnexpectedValueException (ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected UnexpectedValueException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion
    }

    /// <summary>
    /// Exception thrown if a value does not adhere to a defined valid data domain.
    /// </summary>
    [ImplementsType]
#if !SILVERLIGHT
    [Serializable]
#endif
    public class DomainException : LogicException
    {
        #region Implementation Details

        /// <summary>
        /// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
        /// </summary>
        /// <param name="typeDesc">The type desc to populate.</param>
        internal static new void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public DomainException(ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public DomainException(ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected DomainException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion
    }
}
