/*

 Copyright (c) 2004-2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using PHP.Core.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection.Emit;
using System.IO;

namespace PHP.Core
{
	/// <summary>
	/// Kinds of a stack frame.
	/// </summary>
	internal enum FrameKinds
	{
		Invisible,
		Visible,
		ClassLibraryFunction
	}

	/// <summary>
	/// Represents a PHP stack frame.
	/// </summary>
    [DebuggerNonUserCode]
    public sealed class PhpStackFrame
	{
		#region Fields and Properties

		/// <summary>
		/// Gets a name of the frame (name of the PHP function, PHP method or class library function).
		/// </summary>
		public string Name { get { return name; } }
		private string name;

		/// <summary>
		/// Gets a source line where the thred has left the function/method.
		/// </summary>
		public int Line { get { return line; } }
		private int line;

		/// <summary>
		/// Gets a source column where the thred has left the function/method.
		/// </summary>
		public int Column { get { return column; } }
		private int column;

		/// <summary>
		/// Gets a source file where the thred has left the function/method.
		/// </summary>
		public string File { get { return file; } }
		private string file;

		/// <summary>
		/// Gets CLR frame.
		/// </summary>
		public StackFrame Frame { get { return frame; } }
		private StackFrame frame;

		/// <summary>
		/// Checks whether a frame belongs to a class library function.
		/// </summary>
		public bool IsLibraryFunction { get { return kind == FrameKinds.ClassLibraryFunction; } }

		private FrameKinds kind;

		/// <summary>
		/// Checks whether the frame represents a PHP method.
		/// </summary>
		public bool IsMethod
		{
			get
			{
				Type rt;
				return kind != FrameKinds.ClassLibraryFunction && 
					(rt = frame.GetMethod().DeclaringType) != null &&
					PhpType.IsPhpRealType(rt);
			}
		}

		/// <summary>
		/// Checks whether debug information (line, column, file) is known for the frame.
		/// </summary>
		public bool HasDebugInfo { get { return line > 0; } }

		/// <summary>
		/// Gets a PHP operator (either "::" or "->") used for accessing a PHP method of the frame.
		/// </summary>
		/// <remarks>
		/// If the frame is representing a function "::" is returned.
		/// </remarks>
		public string Operator
		{
			get
			{
				return frame.GetMethod().IsStatic ? "::" : "->";
			}
		}

		/// <summary>
		/// Gets a declaring type of the method/function associated with the frame. Non-null.
		/// </summary>
		public Type DeclaringType
		{
			get
			{
				return frame.GetMethod().DeclaringType;
			}
		}

		/// <summary>
		/// Gets a name of the declaring type of the PHP method (or function) represented by the frame. 
		/// </summary>
		/// <remarks>
		/// Returns only valid part of m-decl types.
		/// </remarks>
		public string DeclaringTypeName
		{
			get
			{
				if (_declaringTypeName == null)
				{
					Type type = DeclaringType;
					_declaringTypeName = (type != null) ? DTypeDesc.GetFullName(type, new StringBuilder()).ToString() : null;
				}

				return _declaringTypeName;
			}
		}
		private string _declaringTypeName = null;

		#endregion

		#region Construction (stack trace)

		/// <summary>
		/// Creates a new PHP stack frame.
		/// </summary>
		/// <param name="context">A script context.</param>
		/// <param name="frame">The respective CLR frame.</param>
		/// <param name="kind">A kind of the frame.</param>
		internal PhpStackFrame(ScriptContext/*!*/ context, StackFrame/*!*/ frame, FrameKinds kind)
		{
			Debug.Assert(context != null && frame != null && kind != FrameKinds.Invisible);

			this.frame = frame;
			this.kind = kind;

			MethodBase method = frame.GetMethod();

			if (kind == FrameKinds.ClassLibraryFunction)
			{
				this.name = ImplementsFunctionAttribute.Reflect(method).Name;

				SetDebugInfo(frame);
			}
			else
			{
				Type type = method.DeclaringType;

				int eval_id = TransientAssembly.InvalidEvalId;

				if (type != null && context.ApplicationContext.IsTransientRealType(type))
				{
					// gets [PhpEvalId] attribute defined on the type:
					object[] attrs = type.GetCustomAttributes(typeof(PhpEvalIdAttribute), false);
					eval_id = ((PhpEvalIdAttribute)attrs[0]).Id;

					ErrorStackInfo info = new ErrorStackInfo();

					PhpStackTrace.FillEvalStackInfo(context, eval_id, ref info, false);

					this.line = info.Line;
					this.column = info.Column;
					this.file = info.File;
					this.name = info.Caller;
				}
				else
				{
					SetDebugInfo(frame);
				}

				// the caller has already been set by FillEvalStackInfo 
				// if it is not an eval main:
				if (!(eval_id != TransientAssembly.InvalidEvalId && PhpScript.IsScriptType(type) && method.Name == ScriptModule.MainHelperName))
				{
					int j;
					PhpScript.ParseMDeclName(method.Name, out this.name, out j);
				}
			}
		}

		#endregion

		internal void SetDebugInfo(PhpStackFrame/*!*/ frame)
		{
			this.line = frame.line;
			this.column = frame.column;
			this.file = frame.file;
		}

		internal void SetDebugInfo(StackFrame/*!*/ frame)
		{
			this.line = frame.GetFileLineNumber();
			this.column = frame.GetFileColumnNumber();
			this.file = frame.GetFileName();
		}
	}

	/// <summary>
	/// Represents a stack trace containing only those frames visible from PHP.
	/// </summary>
    [DebuggerNonUserCode]
    public sealed class PhpStackTrace
	{
		private readonly List<PhpStackFrame>/*!*/ frames;

		/// <summary>
		/// Get the <paramref name="i"/>-th frame of the trace.
		/// </summary>
		/// <param name="i">An index of the frame to get.</param>
		/// <returns>The frame or a <B>null</B> reference if <paramref name="i"/> is out of bounds.</returns>
		public PhpStackFrame GetFrame(int i)
		{
			return (i >= 0 || i < frames.Count) ? frames[i] : null;
		}

		/// <summary>
		/// Gets the number of frames in the trace.
		/// </summary>
		/// <returns>The number of frames.</returns>
		public int GetFrameCount()
		{
			return frames.Count;
		}

		#region GetFrameKind

		/// <summary>
		/// Finds out a kind of a CLI frame from the PHP point of view.
		/// </summary>
		/// <param name="frame">The CLI frame.</param>
		/// <returns>The kind of the frame.</returns>
		private static FrameKinds GetFrameKind(StackFrame/*!*/ frame)
		{
			Debug.Assert(frame != null);

			MethodBase method_base = frame.GetMethod();

			// skip CLR ctors and generic methods (we don't emit any):
			if (method_base.IsConstructor || method_base.IsGenericMethod)
				return FrameKinds.Invisible;

			// skip various stubs (special-name) except for Main helper:
			if (method_base.IsSpecialName)
			{
				// main helper in PHP module (script module):
				if (DRoutineDesc.GetSpecialName(method_base) == ScriptModule.MainHelperName &&
					method_base.Module.Assembly.IsDefined(typeof(ScriptAssemblyAttribute), false))
				{
					return FrameKinds.Visible;
				}

				return FrameKinds.Invisible;
			}

			MethodInfo method = (MethodInfo)method_base;

			Type type = method.DeclaringType;

			if (type != null)
			{
				// methods //

				string ns = type.Namespace;

				if (ns != null)
				{
					// skip Core and Extension Manger methods and Dynamic Wrapper Stubs:
					if (ns.StartsWith(Namespaces.Core) || ns == Namespaces.ExtManager || ns == Namespaces.LibraryStubs)
						return FrameKinds.Invisible;

					// skip Class Library methods including PHP functions and PHP methods (remembering the last function):
					if (ns.StartsWith(Namespaces.Library))
					{
						// find out [ImplementsFunction] attributes assigned to the method:
						if (method.IsDefined(Emit.Types.ImplementsFunctionAttribute, false))
							return FrameKinds.ClassLibraryFunction;
						else
							return FrameKinds.Invisible;
					}

					return FrameKinds.Visible;
				}
				else
				{
					// skip export stubs (and other debugger hidden functions):
					if (method.IsDefined(Emit.Types.DebuggerHiddenAttribute, false))
						return FrameKinds.Invisible;

					return FrameKinds.Visible;
				}
			}
			else
			{
				// global functions //

				// skip functions of ExtSupport (and other global non-PHP functions):
				if (method.Module.Assembly != DynamicCode.DynamicMethodType.Assembly &&
					!method.Module.Assembly.IsDefined(typeof(DAssemblyAttribute), false))
				{
					return FrameKinds.Invisible;
				}

				// transient special names:
				if (TransientModule.IsSpecialName(method.Name))
				{
					// main helper is visible as it contains user code:
					if (DRoutineDesc.GetSpecialName(method) == ScriptModule.MainHelperName)
						return FrameKinds.Visible;

					return FrameKinds.Invisible;
				}

				// skip export stubs (and other debugger hidden functions):
				if (method.IsDefined(Emit.Types.DebuggerHiddenAttribute, false))
					return FrameKinds.Invisible;

				return FrameKinds.Visible;
			}

			//// global functions (in extensions) are not included in the PHP stack trace:
			//if (type==null) return FrameKinds.Invisible;

			//string ns = type.Namespace;

			//// non-PHP user code:
			//if (ns == null)
			//  return FrameKinds.Visible;

			//// Core, System, and Extension Manger methods are skipped:
			//if (ns.StartsWith(Namespaces.Core) || ns.StartsWith(Namespaces.System) || ns == Namespaces.ExtManager) 
			//  return FrameKinds.Invisible;

			//// skips library stubs:
			//if (ns == Namespaces.LibraryStubs) 
			//  return FrameKinds.Invisible;

			//// methods in user namespace (either generated by Phalanger or written in other .NET language):
			//if (ns.StartsWith(Namespaces.User))
			//{
			//  // skips arg-less stubs (method is not a constructor => it has MethodInfo):
			//  if (PhpFunctionUtils.IsArglessStub((MethodInfo)method,null))
			//    return FrameKinds.Invisible;

			//  return FrameKinds.UserRoutine;
			//}

			//// skip Class Library methods including PHP functions and PHP methods (remembering the last function):
			//if (ns.StartsWith(Namespaces.Library))
			//{
			//  // find out [ImplementsFunction] attributes assigned to the method:
			//  if (method.IsDefined(Emit.Types.ImplementsFunctionAttribute,false))
			//    return FrameKinds.ClassLibraryFunction; else
			//    return FrameKinds.Invisible;
			//}

			//// non-PHP user code:
			//return FrameKinds.Visible;
		}

		#endregion

		#region Construction

		/// <summary>
		/// Creates a stack trace containing only those frames visible to PHP code.
		/// </summary>
		/// <param name="context">A script context.</param>
		/// <param name="skipFrames">The number of frames which will be skipped.</param>
		/// <exception cref="ArgumentOutOfRangeException">The <paramref name="skipFrames"/> parameter is negative.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="context"/> is a <B>null</B> reference.</exception>
		public PhpStackTrace(ScriptContext/*!*/ context, int skipFrames)
			: this(context, new StackTrace(skipFrames + 1, true))
		{
		}

		/// <summary>
		/// Creates a stack trace containing only those frames visible to PHP code.
		/// </summary>
		/// <param name="context">A script context.</param>
		/// <param name="clrTrace">CLR stack trace.</param>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="clrTrace"/> is a <B>null</B> reference.</exception>
		public PhpStackTrace(ScriptContext/*!*/ context, StackTrace/*!*/ clrTrace)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			if (clrTrace == null)
				throw new ArgumentNullException("trace");

			this.frames = new List<PhpStackFrame>(clrTrace.FrameCount);

			for (int i = 0; i < clrTrace.FrameCount; i++)
			{
				StackFrame frame = clrTrace.GetFrame(i);
				FrameKinds kind = GetFrameKind(frame);

				if (kind != FrameKinds.Invisible)
					frames.Add(new PhpStackFrame(context, frame, kind));
			}
		}

		#endregion

		#region TraceErrorFrame

		/// <summary>
		/// Traces up the stack frame containing the method call that has caused an error.
		/// </summary>
		/// <returns>Found stack info.</returns>
		/// <remarks>
		/// Starts with a frame of a calling method and ends with the first frame belonging to user routine.
		/// If there was <see cref="ImplementsFunctionAttribute"/> found during the walk the last one's value
		/// is considered as the caller.
		/// If there was not such attribute found (error occured in an operator, directly in the code etc.) 
		/// the last inspected method's debug info is returned.
		/// If the trace ends up with a function or method inside transient assembly an eval hierarchy is inspected
		/// and added to the resulting source position information.
		/// </remarks>
		internal static ErrorStackInfo TraceErrorFrame(ScriptContext/*!*/ context)
		{
			Debug.Assert(context != null);

			ErrorStackInfo result = new ErrorStackInfo();
			int cl_function_idx = -1;
			string cl_function_name = null;
			StackFrame frame;
			int eval_id = TransientAssembly.InvalidEvalId;

			// stack trace without debug info is constructed:
#if !SILVERLIGHT
			StackTrace trace = new StackTrace(1, false);

			// note: method stack frame contains a debug info about the call to the callee
			// hence if we find a method that reported the error we should look the next frame 
			// to obtain a debug info

			int i = 0;
			for (; ; )
			{
				// gets frame:
				frame = trace.GetFrame(i++);

				// error has been thrown directly by Core without intermediary user code (all frames are invisible):
				if (frame == null)
				{
					// cl_function_idx can be non-minus-one here because a callback can be called directly from Core 
					// (e.g. output buffer filter targeting class library function):
					if (cl_function_idx != -1)
					{
						result.Caller = cl_function_name;
						result.LibraryCaller = true;
					}

					return result;
				}

				FrameKinds frame_kind = GetFrameKind(frame);

				if (frame_kind == FrameKinds.Visible)
				{
					MethodBase method = frame.GetMethod();

					int eid = TransientModule.GetEvalId(context.ApplicationContext, method);

					if (eval_id == TransientAssembly.InvalidEvalId)
						eval_id = eid;

					if (eid == TransientAssembly.InvalidEvalId)
						break;
				}
				else if (frame_kind == FrameKinds.ClassLibraryFunction)
				{
					MethodBase method = frame.GetMethod();

					cl_function_idx = i;
					cl_function_name = ImplementsFunctionAttribute.Reflect(method).Name;
				}
			}

			// skips i frames (the very first frame has been skipped in the previous 
			// trace construction and we want to skip i-1 frames from that trace => i frames totally):
			frame = new StackFrame(1 + i - 1, true);

			// extracts a source info (file & position):
			if (eval_id != TransientAssembly.InvalidEvalId)
			{
				FillEvalStackInfo(context, eval_id, ref result, false);
			}
			else
			{
				result.Line = frame.GetFileLineNumber();
				result.Column = frame.GetFileColumnNumber();
				result.File = frame.GetFileName();
			}

			// determines a caller (either a library function or a user function/method):
			if (cl_function_idx >= 0)
			{
				result.Caller = cl_function_name;
				result.LibraryCaller = true;
			}
			//else
			//{
			//  MethodBase method = frame.GetMethod();
			//  Type type = method.DeclaringType;

			//  // the caller has already been set by FillEvalStackInfo 
			//  // if we are in eval and the function is Main helper of the script type:
			//  if (eval_id == TransientAssembly.InvalidEvalId)
			//  {
			//    result.LibraryCaller = false;

			//    if (type != null)
			//    {
			//      result.Caller = String.Concat(DTypeDesc.MakeFullName(type), "::", DRoutineDesc.MakeFullName(method));
			//    }
			//    else
			//    {
			//      result.Caller = DRoutineDesc.MakeFullName(method);
			//    }  
			//  } 
			//}
#endif

            // add missing info about file and line
            context.LastErrorLine = result.Line;
            context.LastErrorFile = result.File;

            //
			return result;
		}

		#endregion

		#region Eval related

		/// <summary>
		/// Fills an instance of <see cref="ErrorStackInfo"/> with information gathered from eval transient debug info.
		/// </summary>
		/// <param name="context">Script context.</param>
		/// <param name="evalId">An id of the inner-most eval where an error occured.</param>
		/// <param name="result">The resulting error stack info.</param>
		/// <param name="html">Whether the message is used in HTML.</param>
		internal static void FillEvalStackInfo(ScriptContext/*!*/ context, int evalId, ref ErrorStackInfo result, bool html)
		{
			Debug.Assert(context != null);

			FullPath source_root = Configuration.Application.Compiler.SourceRoot;

			// stack info about the error position (with respect to inner most eval):
			result.Line = context.EvalLine;
			result.Column = context.EvalColumn;
			result.Caller = "<error>";
			result.File = null;

			List<ErrorStackInfo> infos = new List<ErrorStackInfo>();
			infos.Add(result);

			// fills "infos" with full eval error trace:
			context.ApplicationContext.TransientAssemblyBuilder.TransientAssembly.GetEvalFullTrace(evalId, infos);

			Debug.WriteLine("EVAL ERROR", "");
			foreach (ErrorStackInfo info in infos)
			{
				Debug.WriteLine("EVAL ERROR", "info: {0}({1})", info.File, info.Line, info.Caller);
			}

			// hides transparent evals and modifies the others accordingly:
			HideTransparentEvals(infos);

			// refresh inner most error info:
			result = infos[0];
			result.File = EvalTraceToFileName(infos, source_root, html);
		}


		/// <summary>
		/// Modifies a specified eval trace such that all transparent evals get hidden.
		/// The others will have updated line numbers.
		/// </summary>
		private static void HideTransparentEvals(List<ErrorStackInfo>/*!*/ trace)
		{
			int add_line = 0;

			for (int i = trace.Count - 1; i >= 0; i--)
			{
				ErrorStackInfo info = trace[i];

				if (info.Caller == null)
				{
					// skips the frame if there is no debug info:
					if (info.Line > 0)
						add_line += info.Line - 1;
				}
				else if (add_line > 0)
				{
					// skips the frame if there is no debug info:
					if (info.Line > 0)
					{
						info.Line += add_line;
						// replace struct:
						trace[i] = info;
					}

					add_line = 0;
				}
			}

			Debug.Assert(add_line == 0);
		}

		/// <summary>
		/// Extracts debug information from an eval trace and returns it in a form of extended file name:
		/// {full canonical file name of the inner most eval source file} 
		/// { inside {eval|assert|...|run-time funcion} (on line #, column #) }*
		/// </summary>
		private static string EvalTraceToFileName(List<ErrorStackInfo>/*!*/ trace, string sourceRoot, bool html)
		{
			StringBuilder sb = new StringBuilder();

			for (int i = trace.Count - 1; i > 0; i--)
			{
				ErrorStackInfo info = trace[i];

				if (sb.Length == 0)
				{
					sb.Append(Path.GetFullPath(Path.Combine(sourceRoot, info.File)));
				}

				if (info.Caller != null)
				{
					sb.Append(' ');

					if (info.Line >= 0 && info.Column >= 0)
					{
						sb.Append(CoreResources.GetString(html ? "error_message_html_eval_debug" : "error_message_plain_eval_debug",
							info.Caller, info.Line, info.Column));
					}
					else
					{
						sb.Append(CoreResources.GetString(html ? "error_message_html_eval" : "error_message_plain_eval",
							info.Caller));
					}
				}
			}

			return sb.ToString();
		}

		#endregion

		#region GetClassContext

		/// <summary>
		/// Traces the calling stack to discover current PHP class context.
		/// </summary>
		/// <returns><see cref="Type"/> of the PHP class that represents current class context for this thread or
		/// <B>null</B> if this thread is executing in a function or startup Main context.</returns>
		public static DTypeDesc GetClassContext()
		{
			// SILVERLIGHT: Todo Todo .. ? what to do here ?
#if !SILVERLIGHT
			StackTrace stack_trace = new StackTrace(1);
			int frame_count = stack_trace.FrameCount;

			for (int i = 0; i < frame_count; i++)
			{
				StackFrame stack_frame = stack_trace.GetFrame(i);

				MethodBase method = stack_frame.GetMethod();
				Type type = method.DeclaringType;
				if (type != null)
				{
					if (PhpType.IsPhpRealType(type)) return DTypeDesc.Create(type);

					MethodInfo minfo = method as MethodInfo;
					if (minfo != null)
					{
						ParameterInfo[] parameters = minfo.GetParameters();
						if (!PhpFunctionUtils.IsArglessStub(minfo, parameters) &&
							PhpScript.IsScriptType(minfo.DeclaringType) && !PhpScript.IsMainHelper(minfo, parameters))
						{
							return null;
						}
						// if the method is a helper method (Main, an arg-less overload, a constructor, etc.),
						// continue with the trace
					}
				}
			}
#endif
			return null;
		}

		#endregion

		#region User Trace Formatting

		/// <summary>
		/// Returns array containing current stack state. Each item is an array representing one stack frame.
		/// </summary>
		/// <returns>The stack trace.</returns>
		/// <remarks>
		/// The resulting array contains the following items (their keys are stated):
		/// <list type="bullet">
		/// <item><c>"file"</c> - a source file where the function/method has been called</item>
		/// <item><c>"line"</c> - a line in a source code where the function/method has been called</item>
		/// <item><c>"column"</c> - a column in a source code where the function/method has been called</item>
		/// <item><c>"function"</c> - a name of the function/method</item> 
		/// <item><c>"class"</c> - a name of a class where the method is declared (if any)</item>
		/// <item><c>"type"</c> - either "::" for static methods or "->" for instance methods</item>
		/// </list>
		/// Unsupported items:
		/// <list type="bullet">
		/// <item><c>"args"</c> - routine arguments</item>
		/// <item><c>"object"</c> - target instance of the method invocation</item>
		/// </list>
		/// </remarks>
		public PhpArray GetUserTrace()
		{
			int i = GetFrameCount() - 1;
			PhpArray result = new PhpArray();

			if (i >= 1)
			{
				PhpStackFrame info_frame = GetFrame(i--);

				while (i >= 0)
				{
					PhpStackFrame frame = GetFrame(i);
					PhpArray item = new PhpArray();

					// debug info may be unknown in the case of transient code:
					if (info_frame.Line > 0)
					{
						item["line"] = info_frame.Line;
						item["column"] = info_frame.Column;
					}
					item["file"] = info_frame.File;

					item["function"] = frame.Name;
					if (frame.IsMethod)
					{
						item["class"] = frame.DeclaringTypeName;
						item["type"] = frame.Operator;
					}

					result.Prepend(i, item);

					if (frame.HasDebugInfo)
						info_frame = frame;

					i--;
				}
			}

			return result;
		}

		/// <summary>
		/// Formats a trace to user string.
		/// </summary>
		/// <param name="trace">An array containing the user trace.</param>
		/// <returns>The formatted trace.</returns>
		public static string FormatUserTrace(PhpArray/*!*/ trace)
		{
			if (trace == null)
				throw new ArgumentNullException("trace");

			StringBuilder result = new StringBuilder();

			foreach (KeyValuePair<IntStringKey, object> entry in trace)
			{
				PhpArray frame = entry.Value as PhpArray;
				if (frame != null)
				{
					int line = Convert.ObjectToInteger(frame["line"]);
					int column = Convert.ObjectToInteger(frame["column"]);

					result.Insert(0, String.Format("#{0} {1}{2}: {3}{4}\n",
					  entry.Key.Object,
					  Convert.ObjectToString(frame["file"]),
					  (line > 0 && column > 0) ? String.Format("({0},{1})", line, column) : null,
					  Convert.ObjectToString(frame["class"]) + Convert.ObjectToString(frame["type"]),
					  frame["function"]));
				}
			}
			return result.AppendFormat("#{0} {{main}}", trace.Count).ToString();
		}

		/// <summary>
		/// Formats a trace to user string.
		/// </summary>
		/// <returns>The formatted trace.</returns>
		public string FormatUserTrace()
		{
			int i = GetFrameCount() - 1;
			StringBuilder result = new StringBuilder(String.Format("#{0} {{main}}", i));

			if (i >= 1)
			{
				PhpStackFrame info_frame = GetFrame(i--);

				while (i >= 0)
				{
					PhpStackFrame frame = GetFrame(i);

					result.Insert(0, String.Format("#{0} {1}{2}: {3}{4}\n",
					  i,
					  info_frame.File,
					  (info_frame.Line > 0) ? String.Format("({0},{1})", info_frame.Line, info_frame.Column) : null,
					  (frame.IsMethod) ? frame.DeclaringTypeName + frame.Operator : null,
					  frame.Name));

					if (frame.HasDebugInfo)
						info_frame = frame;

					i--;
				}
			}
			return result.ToString();
		}

		#endregion
	}
}