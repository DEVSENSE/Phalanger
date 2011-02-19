/*

 Copyright (c) 2004-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics.SymbolStore;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using System.Windows.Forms;
using System.Diagnostics;

using PHP.Core;
using PHP.Core.Reflection;
using System.Web;
using System.Web.SessionState;

namespace PHP.Library
{
#if DEBUG

	/// <exclude/>
	public static class PhpDocumentation
	{
		private static bool FunctionsCallback(MethodInfo method, ImplementsFunctionAttribute ifa, object result)
		{
			if ((ifa.Options & FunctionImplOptions.Internal) == 0 && (ifa.Options & FunctionImplOptions.NotSupported) == 0)
			{
				PhpArray array = (PhpArray)result;
				((PhpArray)array["name"]).Add(ifa.Name);
				((PhpArray)array["type"]).Add(method.DeclaringType.FullName);
				((PhpArray)array["method"]).Add(method.Name);
			}
			return true;
		}

		private static bool TypesCallback(Type type, object result)
		{
			PhpArray array = (PhpArray)result;
			((PhpArray)array["name"]).Add(type.FullName);
			((PhpArray)array["interface"]).Add(type.IsInterface);

			return true;
		}

		private static bool ConstantsCallback(FieldInfo field, ImplementsConstantAttribute ica, object result)
		{
			PhpArray array = (PhpArray)result;
			((PhpArray)array["name"]).Add(ica.Name);
			((PhpArray)array["type"]).Add(field.DeclaringType.FullName);
			((PhpArray)array["field"]).Add(field.Name);
			((PhpArray)array["insensitive"]).Add(ica.CaseInsensitive);
			return true;
		}

		/// <summary>
		/// Prints documentation table for classes.
		/// </summary>
		[ImplementsFunction("phpnet_doc_functions", FunctionImplOptions.Internal)]
		public static PhpArray PrintFunctions()
		{
			PhpArray result = new PhpArray();

			result.Add("name", new PhpArray());
			result.Add("type", new PhpArray());
			result.Add("method", new PhpArray());

			Assembly assembly = typeof(PhpDocumentation).Assembly;
			//PhpLibraryModule.EnumerateFunctions(assembly, new PhpLibraryModule.FunctionsEnumCallback(FunctionsCallback), result);
			return result;
		}

		/// <summary>
		/// Prints documentation table for classes.
		/// </summary>
		[ImplementsFunction("phpnet_doc_types", FunctionImplOptions.Internal)]
		public static PhpArray PrintTypes()
		{
			PhpArray result = new PhpArray();
			result.Add("name", new PhpArray());
			result.Add("interface", new PhpArray());

			Assembly assembly = typeof(PhpDocumentation).Assembly;
			//PhpLibraryModule.EnumerateTypes(assembly, new PhpLibraryModule.TypeEnumCallback(TypesCallback), result);
			return result;
		}

		/// <summary>
		/// Prints documentation table for classes.
		/// </summary>
		[ImplementsFunction("phpnet_doc_constants", FunctionImplOptions.Internal)]
		public static PhpArray PrintConstants()
		{
			PhpArray result = new PhpArray();
			result.Add("name", new PhpArray());
			result.Add("type", new PhpArray());
			result.Add("field", new PhpArray());
			result.Add("insensitive", new PhpArray());

			Assembly assembly = typeof(PhpDocumentation).Assembly;
			//PhpLibraryModule.EnumerateConstants(assembly, new PhpLibraryModule.ConstantsEnumCallback(ConstantsCallback), result);
			return result;
		}
	}

	/// <summary>
	/// Functions used for debugging class library.
	/// </summary>
	/// <exclude/>
	public sealed class DebugTests
	{
		private DebugTests() { }

        [ImplementsFunction("__break", FunctionImplOptions.Internal)]
		public static void Break()
		{
			Debugger.Break();
		}

        [ImplementsFunction("__ddump", FunctionImplOptions.Internal)]
		public static void DebugDump(object var)
		{
			StringWriter s = new StringWriter();
			PhpVariable.Dump(s, var);
			Core.Debug.WriteLine("DEBUG", s.ToString());
		}

		[ImplementsFunction("__0", FunctionImplOptions.Internal)]
		public static void f0(PhpReference arg)
		{
			PhpVariable.Dump(arg.value);
			arg.value = "hello";
		}

		[ImplementsFunction("__1", FunctionImplOptions.Internal)]
		public static void f1(params PhpReference[] args)
		{
			foreach (PhpReference arg in args)
			{
				PhpVariable.Dump(arg.value);
				arg.value = "hello";
			}
		}

		[ImplementsFunction("__2", FunctionImplOptions.Internal)]
		public static void f2(params int[] args)
		{
			foreach (int arg in args)
				PhpVariable.Dump(arg);
		}

		[ImplementsFunction("__3", FunctionImplOptions.Internal)]
		public static void f3(params object[] args)
		{
			foreach (object arg in args)
				PhpVariable.Dump(arg);
		}

		[ImplementsFunction("__4", FunctionImplOptions.Internal)]
		public static void f4(params PhpArray[] args)
		{
			foreach (PhpArray arg in args)
				PhpVariable.Dump(arg);
		}

		[ImplementsFunction("__5", FunctionImplOptions.Internal)]
		public static void f5(params double[] args)
		{
			foreach (double arg in args)
				PhpVariable.Dump(arg);
		}

		[ImplementsFunction("__6", FunctionImplOptions.Internal)]
		public static void f6(ref PhpArray arg)
		{
			PhpVariable.Dump(arg);
		}

		[ImplementsFunction("__7", FunctionImplOptions.Internal)]
		public static void f7(params PhpArray[] args)
		{
			foreach (PhpArray arg in args)
				PhpVariable.Dump(arg);
		}

        [ImplementsFunction("__8", FunctionImplOptions.Internal)]
		[return: CastToFalse]
		public static string f8(PhpResource a, PhpResource b, PhpResource c)
		{
			return null;
		}

        [ImplementsFunction("__9", FunctionImplOptions.Internal)]
		[return: CastToFalse]
		public static int f9(PhpResource a)
		{
			return -1;
		}

		[ImplementsFunction("__readline", FunctionImplOptions.Internal)]
		public static string ReadLine()
		{
			return Console.ReadLine();
		}

		[ImplementsFunction("__stacktrace", FunctionImplOptions.Internal)]
		public static string ClrStackTrace()
		{
			StackTrace trace = new StackTrace(true);
			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < trace.FrameCount; i++)
			{
				StackFrame frame = trace.GetFrame(i);
				MethodBase method = frame.GetMethod();

				sb.AppendFormat("{0} {1} {2} {3} {4} {5}\n",
					(method != null) ? method.DeclaringType + "." + method.Name : "NULL",
					frame.GetFileName(),
					frame.GetFileLineNumber(),
					frame.GetFileColumnNumber(),
					frame.GetNativeOffset(),
					frame.GetILOffset());
			}

			return sb.ToString();
		}

		/// <summary>
		/// Gets an array of headers of the current HTTP request.
		/// </summary>
		[ImplementsFunction("__headers", FunctionImplOptions.Internal)]
		public static PhpArray GetHeaders()
		{
			PhpArray result = new PhpArray();

			NameValueCollection headers = HttpContext.Current.Request.Headers;

			string[] keys = headers.AllKeys;
			for (int i = 0; i < keys.Length; i++)
			{
				string[] values = headers.GetValues(keys[i]);

				if (values.Length > 1)
				{
					PhpArray keys_array = new PhpArray();

					for (int j = 0; j < values.Length; j++)
					{
						keys_array.Add(values[j]);
					}
					result.Add(keys[i], keys_array);
				}
				else
				{
					result.Add(keys[i], values[0]);
				}
			}
			return result;
		}

		[ImplementsFunction("__request_enc", FunctionImplOptions.Internal)]
		public static string GetRequestEncoding()
		{
			return HttpContext.Current.Request.ContentEncoding.EncodingName;
		}

		[ImplementsFunction("__response_enc", FunctionImplOptions.Internal)]
		public static string GetResponseEncoding()
		{
			return HttpContext.Current.Response.ContentEncoding.EncodingName;
		}

		[ImplementsFunction("__upper", FunctionImplOptions.Internal)]
		public static PhpBytes GetUpperBytes()
		{
			byte[] result = new byte[30];
			for (int i = 0; i < result.Length; i++)
				result[i] = (byte)(i + 128);
			return new PhpBytes(result);
		}

		[ImplementsFunction("__throw", FunctionImplOptions.Internal)]
		public static PhpBytes __throw()
		{
			throw new ArgumentNullException("XXX", "Fake exception");
		}

		[ImplementsFunction("__dump_transient", FunctionImplOptions.Internal)]
		public static void __dump_transient()
		{
			DynamicCode.Dump(ScriptContext.CurrentContext, ScriptContext.CurrentContext.Output);
		}

		[ImplementsFunction("__evalinfo", FunctionImplOptions.CaptureEvalInfo | FunctionImplOptions.Internal)]
		public static PhpArray __evalinfo()
		{
			ScriptContext context = ScriptContext.CurrentContext;
			return PhpArray.Keyed(
			  "file", context.EvalRelativeSourcePath,
			  "line", context.EvalLine,
			  "column", context.EvalColumn);
		}

        [ImplementsFunction("__dump_session", FunctionImplOptions.Internal)]
		public static void __dump_session()
		{
			TextWriter o = ScriptContext.CurrentContext.Output;

			HttpContext context = HttpContext.Current;
			if (context == null) { o.WriteLine("HTTP CONTEXT NULL"); return; }

			HttpSessionState state = context.Session;
			if (context == null) { o.WriteLine("SESSION NULL"); return; }

			PhpArray a = new PhpArray();
			foreach (string name in state)
			{
				a[name] = state[name];
			}

			PhpVariable.Dump(o, a);
		}

        [ImplementsFunction("__dump_fdecls", FunctionImplOptions.Internal)]
		public static PhpArray __dump_fdecls()
		{
			PhpArray result = new PhpArray();
			foreach (KeyValuePair<string, DRoutineDesc> entry in ScriptContext.CurrentContext.DeclaredFunctions)
			{
				result.Add(entry.Key, entry.Value.MakeFullName());
			}
			return result;
		}

        [ImplementsFunction("__type", FunctionImplOptions.Internal)]
		public static string PhpNetType(object o)
		{
			return o == null ? "null" : o.GetType().FullName;
		}

        [ImplementsFunction("__assemblies", FunctionImplOptions.Internal)]
		public static PhpArray GetAssemblies()
		{
			PhpArray result = new PhpArray();
			foreach (PhpLibraryAssembly a in ScriptContext.CurrentContext.ApplicationContext.GetLoadedLibraries())
				result.Add(a.RealAssembly.FullName);
			return result;
		}

        [ImplementsFunction("__descriptors", FunctionImplOptions.Internal)]
		public static PhpArray GetDescriptors()
		{
			PhpArray result = new PhpArray();
			foreach (PhpLibraryAssembly a in ScriptContext.CurrentContext.ApplicationContext.GetLoadedLibraries())
				result.Add(a.Descriptor.GetType().FullName);
			return result;
		}

		public sealed class Remoter : MarshalByRefObject
		{
			public string[] GetLoadedAssemblies()
			{
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				string[] result = new string[assemblies.Length];
				for (int i = 0; i < assemblies.Length; i++)
					result[i] = assemblies[i].FullName;
				return result;
			}

			public static Remoter CreateRemoteInstance(AppDomain domain)
			{
				Type t = typeof(Remoter);
				return (Remoter)domain.CreateInstanceAndUnwrap(t.Assembly.FullName, t.FullName);
			}
		}

		private static void AppDomainInfo(AppDomain domain, TextWriter output)
		{
			if (domain == null) return;

			output.WriteLine("</PRE><H3>AppDomain info</H3><PRE>");

			output.WriteLine("FriendlyName = {0}", domain.FriendlyName);
			output.WriteLine("ApplicationBase = {0}", domain.SetupInformation.ApplicationBase);
			output.WriteLine("ConfigurationFile = {0}", domain.SetupInformation.ConfigurationFile);
			output.WriteLine("DynamicBase = {0}", domain.SetupInformation.DynamicBase);
			output.WriteLine("PrivateBinPath = {0}", domain.SetupInformation.PrivateBinPath);
			output.WriteLine("CachePath = {0}", domain.SetupInformation.CachePath);
			output.WriteLine("ShadowCopyDirectories = {0}", domain.SetupInformation.ShadowCopyDirectories);
			output.WriteLine("ShadowCopyFiles = {0}", domain.SetupInformation.ShadowCopyFiles);

			if (domain == AppDomain.CurrentDomain)
			{
				foreach (Assembly assembly in domain.GetAssemblies())
					output.WriteLine("  Assembly: {0}", assembly.FullName);
			}
			else
			{
				foreach (string name in Remoter.CreateRemoteInstance(domain).GetLoadedAssemblies())
					output.WriteLine("  Assembly: {0}", name);
			}
		}

        [ImplementsFunction("__info", FunctionImplOptions.Internal)]
		public static void Info()
		{
			Info(ScriptContext.CurrentContext);
		}

		public static void Info(ScriptContext/*!*/ scriptContext)
		{
			TextWriter output = scriptContext.Output;
			HttpContext ctx = HttpContext.Current;

			output.WriteLine("<br><div style='background-color:oldlace'><H3>Phalanger debug info:</H3><PRE>");

			output.WriteLine("</PRE><H3>HttpRuntime</H3><PRE>");

			output.WriteLine("AppDomainAppId = {0}", HttpRuntime.AppDomainAppId);
			output.WriteLine("AppDomainAppPath = {0}", HttpRuntime.AppDomainAppPath);
			output.WriteLine("AppDomainAppVirtualPath = {0}", HttpRuntime.AppDomainAppVirtualPath);
			output.WriteLine("AppDomainId = {0}", HttpRuntime.AppDomainId);
			output.WriteLine("AspInstallDirectory = {0}", HttpRuntime.AspInstallDirectory);
			output.WriteLine("BinDirectory = {0}", HttpRuntime.BinDirectory);
			output.WriteLine("ClrInstallDirectory = {0}", HttpRuntime.ClrInstallDirectory);
			try
			{
				output.WriteLine("CodegenDir = {0}", HttpRuntime.CodegenDir);
			}
			catch (Exception)
			{
				output.WriteLine("CodegenDir = N/A");
			}
			output.WriteLine("MachineConfigurationDirectory = {0}", HttpRuntime.MachineConfigurationDirectory);

			output.WriteLine("</PRE><H3>Worker Process</H3><PRE>");

			output.Write("Worker processes: ");
			if (ctx != null)
			{
				foreach (ProcessInfo pi in ProcessModelInfo.GetHistory(20))
					output.Write(pi.ProcessID + ";");
				output.WriteLine();

				output.WriteLine("Current Worker Process start time: {0}", ProcessModelInfo.GetCurrentProcessInfo().StartTime);
			}
			else
			{
				output.WriteLine("N/A");
			}

			Process proc = Process.GetCurrentProcess();
			output.WriteLine("Current process: Id = {0}", proc.Id);
			output.WriteLine("Current PrivateMemorySize: {0} MB", proc.PrivateMemorySize64 / (1024 * 1024));
			output.WriteLine("Current WorkingSet: {0} MB", proc.WorkingSet64 / (1024 * 1024));
			output.WriteLine("Current VirtualMemorySize: {0} MB", proc.VirtualMemorySize64 / (1024 * 1024));
			output.WriteLine("Current thread: HashCode = {0}", Thread.CurrentThread.GetHashCode());
			output.WriteLine("Current domain: {0}", Thread.GetDomain().FriendlyName);

			AppDomainInfo(AppDomain.CurrentDomain, output);
			if (ctx != null) AppDomainInfo(AppDomain.CurrentDomain, output);

			output.WriteLine("</PRE><H3>Libraries</H3><PRE>");

			foreach (PhpLibraryAssembly a in scriptContext.ApplicationContext.GetLoadedLibraries())
				a.Descriptor.Dump(output);

			//output.WriteLine("</PRE><H3>Invalidated Precompiled Scripts</H3><PRE>");
			//foreach (string item in WebServerManagersDebug.GetInvalidatedScripts())
			//  output.WriteLine(item);

			output.WriteLine("</PRE><H3>Cache</H3><PRE>");
			foreach (DictionaryEntry item in HttpRuntime.Cache)
				if (item.Value is string)
					output.WriteLine("{0} => '{1}'", item.Key, item.Value);
				else
					output.WriteLine("{0} => instance of {1}", item.Key, item.Value.GetType().FullName);

			if (ctx != null)
			{
				output.WriteLine("</PRE><H3>Query Variables</H3><PRE>");
				String[] keys;
				keys = ctx.Request.QueryString.AllKeys;
				for (int i = 0; i < keys.Length; i++)
					output.WriteLine("{0} = \"{1}\"", keys[i], ctx.Request.QueryString.GetValues(keys[i])[0]);

				if (ctx.Session != null)
				{
					output.WriteLine("</PRE><H3>Session Variables</H3><PRE>");

					output.WriteLine("IsCookieless = {0}", ctx.Session.IsCookieless);
					output.WriteLine("IsNewSession = {0}", ctx.Session.IsNewSession);
					output.WriteLine("SessionID = {0}", ctx.Session.SessionID);

					foreach (string name in ctx.Session)
					{
						output.Write("{0} = ", name);
						PhpVariable.Dump(ctx.Session[name]);
					}
				}

				output.WriteLine("</PRE><H3>Cookies</H3><PRE>");
				foreach (string cookie_name in ctx.Request.Cookies)
				{
					HttpCookie cookie = ctx.Request.Cookies[cookie_name];
					Console.WriteLine("{0} = {1}", cookie.Name, cookie.Value);
				}

				output.WriteLine("</PRE><H3>Server Variables</H3><PRE>");

				keys = ctx.Request.ServerVariables.AllKeys;
				for (int i = 0; i < keys.Length; i++)
					output.WriteLine("{0} = \"{1}\"", keys[i], ctx.Request.ServerVariables.GetValues(keys[i])[0]);
			}
			else
			{
				output.WriteLine("</PRE><H3>Missing HttpContext</H3><PRE>");
			}

			output.WriteLine("</PRE></DIV>");
		}
	}

#endif
}
