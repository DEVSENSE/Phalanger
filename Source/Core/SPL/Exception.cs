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
		/// <returns>A <b>null</b> reference (void in PHP).</returns>
		[ImplementsMethod]
		public virtual object __construct(ScriptContext context, [Optional] object message, [Optional] object code)
		{
			this.message.Value = (message == Arg.Default) ? CoreResources.GetString("default_exception_message") : message;
			this.code.Value = (code == Arg.Default) ? 0 : code;

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
			return file;
		}

		/// <summary>
		/// Gets a line in the source file where the exception has been thrown. 
		/// </summary>
		/// <returns>The line.</returns>
		[ImplementsMethod]
		public object getLine(ScriptContext context)
		{
			return line;
		}

		/// <summary>
		/// Gets a column in the source file where the exception has been thrown.
		/// </summary>
		/// <returns>The column.</returns>
		[ImplementsMethod]
		public object getColumn(ScriptContext context)
		{
			return column;
		}

		/// <summary>
		/// Gets the code specified in the constructor.
		/// </summary>
		/// <returns>The code.</returns>
		[ImplementsMethod]
		public object getCode(ScriptContext context)
		{
			return code;
		}

		/// <summary>
		/// Gets a message.
		/// </summary>
		/// <returns>The message set by the constructor.</returns>
		[ImplementsMethod]
		public object getMessage(ScriptContext context)
		{
			return message;
		}

		/// <summary>
		/// Returns a trace of the stack in the moment the exception was thrown.
		/// </summary>
		/// <returns>The trace.</returns>
		[ImplementsMethod]
		public object getTrace(ScriptContext context)
		{
			return trace;
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
			stack.RemoveFrame();
			return ((Exception)instance).__construct(stack.Context, message, code);
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
}
