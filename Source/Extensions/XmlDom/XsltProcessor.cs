/*

 Copyright (c) 2006 Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Library.Xml
{
	/// <summary>
	/// Enumerates the clone behavior. (Where is this supposed to be used?)
	/// </summary>
	public enum CloneType
	{
		[ImplementsConstant("XSL_CLONE_AUTO")]
		Auto = 0,

		[ImplementsConstant("XSL_CLONE_NEVER")]
		Never = 1,

		[ImplementsConstant("XSL_CLONE_ALWAYS")]
		Always = -1
	}

	/// <summary>
	/// Implements the XSLT processor.
	/// </summary>
	[ImplementsType]
	public partial class XSLTProcessor
	{
		#region Delegates

		private delegate void LoadDelegate(IXPathNavigable stylesheet);
		private delegate XmlWriterSettings GetOutputSettingsDelegate();
		private delegate void TransformToWriterDelegate(IXPathNavigable input, XsltArgumentList arguments, XmlWriter results);
		private delegate void TransformToStreamDelegate(IXPathNavigable input, XsltArgumentList arguments, Stream results);

		#endregion

		#region Fields and Properties

		private LoadDelegate Load;
		private GetOutputSettingsDelegate GetOutputSettings;
		private TransformToWriterDelegate TransformToWriter;
		private TransformToStreamDelegate TransformToStream;

		private XsltArgumentList xsltArgumentList;
		private XsltUserFunctionHandler xsltUserFunctionHandler;

		private const string PhpNameSpaceUri = "http://php.net/xsl";

		private static bool mvpXmlAvailable;
		private static Type mvpXmlType;

		private static MethodInfo getOutputSettingsMethodFW;
		private static MethodInfo loadMethodMvp;
		private static MethodInfo getOutputSettingsMethodMvp;
		private static MethodInfo transformToWriterMethodMvp;
		private static MethodInfo transformToStreamMethodMvp;

		#endregion

		#region Construction

		/// <summary>
		/// Determines whether Mvp.Xml is available and reflects the MvpXslTransform type.
		/// </summary>
		static XSLTProcessor()
		{
			getOutputSettingsMethodFW = typeof(XslCompiledTransform).GetProperty("OutputSettings").GetGetMethod();

			// try to load the Mvp.Xml assembly
			try
			{
				Assembly mvp_xml_assembly = Assembly.Load("Mvp.Xml, Version=2.0.2158.1055, Culture=neutral, PublicKeyToken=dd92544dc05f5671");
				mvpXmlType = mvp_xml_assembly.GetType("Mvp.Xml.Exslt.ExsltTransform");

				if (mvpXmlType != null)
				{
					loadMethodMvp = mvpXmlType.GetMethod("Load", new Type[] { typeof(IXPathNavigable) });
					getOutputSettingsMethodMvp = mvpXmlType.GetProperty("OutputSettings").GetGetMethod();
					transformToWriterMethodMvp = mvpXmlType.GetMethod("Transform", new Type[] { typeof(IXPathNavigable), typeof(XsltArgumentList), typeof(XmlWriter) });
					transformToStreamMethodMvp = mvpXmlType.GetMethod("Transform", new Type[] { typeof(IXPathNavigable), typeof(XsltArgumentList), typeof(Stream) });

					mvpXmlAvailable =
						(loadMethodMvp != null &&
						getOutputSettingsMethodMvp != null &&
						transformToWriterMethodMvp != null &&
						transformToStreamMethodMvp != null);
				}
			}
			catch (Exception)
			{
				return;
			}
		}

		public XSLTProcessor()
			: base(ScriptContext.CurrentContext, true)
		{
			if (mvpXmlAvailable)
			{
				object transform = Activator.CreateInstance(mvpXmlType);

				Load = (LoadDelegate)Delegate.CreateDelegate(typeof(LoadDelegate), transform, loadMethodMvp);
				GetOutputSettings = (GetOutputSettingsDelegate)Delegate.CreateDelegate(typeof(GetOutputSettingsDelegate),
					transform, getOutputSettingsMethodMvp);

				TransformToWriter = (TransformToWriterDelegate)Delegate.CreateDelegate(typeof(TransformToWriterDelegate),
					transform, transformToWriterMethodMvp);
				TransformToStream = (TransformToStreamDelegate)Delegate.CreateDelegate(typeof(TransformToStreamDelegate),
					transform, transformToStreamMethodMvp);
			}
			else
			{
				// Mvp.Xml not available -> falling back to XslCompiledTransform
				XslCompiledTransform transform = new XslCompiledTransform();

				Load = new LoadDelegate(transform.Load);
				GetOutputSettings = (GetOutputSettingsDelegate)
					Delegate.CreateDelegate(typeof(GetOutputSettingsDelegate), transform, getOutputSettingsMethodFW);

				TransformToWriter = new TransformToWriterDelegate(transform.Transform);
				TransformToStream = new TransformToStreamDelegate(transform.Transform);
			}

			this.xsltArgumentList = new XsltArgumentList();
		}

		public override bool ToBoolean()
		{
			return true;
		}

		#endregion

		#region Transformation

		/// <summary>
		/// Import a stylesheet.
		/// </summary>
		/// <param name="doc">The imported style sheet passed as a <see cref="DOMDocument"/> object.</param>
		/// <returns><B>True</B> or <B>false</B>.</returns>
		[PhpVisible]
		public object importStylesheet(DOMDocument doc)
		{
			try
			{
				Load(doc.XmlDocument);
			}
			catch (XsltException e)
			{
				PhpException.Throw(PhpError.Warning, e.Message);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Transforms the source node to a <see cref="DOMDocument"/> applying the stylesheet given by the
		/// <see cref="importStylesheet(DOMDocument)"/> method.
		/// </summary>
		/// <param name="node">The node to be transformed.</param>
		/// <returns>The resulting <see cref="DOMDocument"/> or <B>false</B> on error.</returns>
		[PhpVisible]
		public object transformToDoc(IXmlDomNode node)
		{
			XmlDocument doc = new XmlDocument();
			doc.PreserveWhitespace = true;

			using (MemoryStream stream = new MemoryStream())
			{
				XmlWriterSettings settings = GetOutputSettings();
				if (settings.Encoding is UTF8Encoding)
				{
					// no BOM in UTF-8 please!
					settings = settings.Clone();
					settings.Encoding = new UTF8Encoding(false);
				}

				using (XmlWriter writer = XmlWriter.Create(stream, settings))
				{
					// transform the document
					try
					{
						TransformToWriter(node.UnderlyingObject, xsltArgumentList, writer);
					}
					catch (XsltException e)
					{
						if (e.InnerException != null)
						{
							// ScriptDiedException etc.
							throw e.InnerException;
						}

						PhpException.Throw(PhpError.Warning, e.Message);
						return false;
					}
					catch (InvalidOperationException e)
					{
						PhpException.Throw(PhpError.Warning, e.Message);
						return false;
					}
				}

				stream.Seek(0, SeekOrigin.Begin);

				// build the resulting XML document
				try
				{
					doc.Load(stream);
				}
				catch (XmlException e)
				{
					PhpException.Throw(PhpError.Warning, e.Message);
					return false;
				}
			}

			return new DOMDocument(doc);
		}

		/// <summary>
		/// Transforms the source node to an URI applying the stylesheet given by the
		/// <see cref="importStylesheet(DOMDocument)"/> method.
		/// </summary>
		/// <param name="doc">The document to transform.</param>
		/// <param name="uri">The destination URI.</param>
		/// <returns>Returns the number of bytes written or <B>false</B> if an error occurred.</returns>
		[PhpVisible]
		public object transformToUri(DOMDocument doc, string uri)
		{
			using (PhpStream stream = PhpStream.Open(uri, "wt"))
			{
				if (stream == null) return false;

				// transform the document
				try
				{
					TransformToStream(doc.XmlNode, xsltArgumentList, stream.RawStream);
				}
				catch (XsltException e)
				{
					if (e.InnerException != null)
					{
						// ScriptDiedException etc.
						throw e.InnerException;
					}

					PhpException.Throw(PhpError.Warning, e.Message);
					return false;
				}
				catch (InvalidOperationException e)
				{
					PhpException.Throw(PhpError.Warning, e.Message);
					return false;
				}

				// TODO:
				return (stream.RawStream.CanSeek ? stream.RawStream.Position : 1);
			}
		}

		/// <summary>
		/// Transforms the source node to a string applying the stylesheet given by the
		/// <see cref="importStylesheet(DOMDocument)"/> method.
		/// </summary>
		/// <param name="doc">The document to transform.</param>
		/// <returns>The result of the transformation as a string or FALSE on error.</returns>
		[PhpVisible]
		public object transformToXml(DOMDocument doc)
		{
			// writing to a StringWriter would result in forcing UTF-16 encoding
			using (MemoryStream stream = new MemoryStream())
			{
				XmlWriterSettings settings = GetOutputSettings();
				if (settings.Encoding is UTF8Encoding)
				{
					// no BOM in UTF-8 please!
					settings = settings.Clone();
					settings.Encoding = new UTF8Encoding(false);
				}

				using (XmlWriter writer = XmlWriter.Create(stream, settings))
				{
					// transform the document
					try
					{
						TransformToWriter(doc.XmlNode, xsltArgumentList, writer);
					}
					catch (XsltException e)
					{
						if (e.InnerException != null)
						{
							// ScriptDiedException etc.
							throw e.InnerException;
						}

						PhpException.Throw(PhpError.Warning, e.Message);
						return false;
					}
					catch (InvalidOperationException e)
					{
						PhpException.Throw(PhpError.Warning, e.Message);
						return false;
					}
				}

				return new PhpBytes(stream.ToArray());
			}
		}

		/// <summary>
		///  Sets value for a parameter.
		/// </summary>
		/// <param name="ns">The namespace URI of the XSLT parameter.</param>
		/// <param name="name">The local name of the XSLT parameter or an array of name =&gt; option pairs.</param>
		/// <param name="value">The new value of the XSLT parameter.</param>
		/// <returns><B>True</B> or <B>false</B>.</returns>
		[PhpVisible]
		public object setParameter(string ns, object name, [Optional] string value)
		{
			PhpArray array = name as PhpArray;
			if (array != null)
			{
				// set all name => value pairs contained in the array
				foreach (KeyValuePair<IntStringKey, object> pair in array)
				{
					if (!pair.Key.IsString)
					{
						PhpException.Throw(PhpError.Warning, Resources.InvalidParameterKey);
						return false;
					}

					if (xsltArgumentList.GetParam(pair.Key.String, ns) != null)
					{
						xsltArgumentList.RemoveParam(pair.Key.String, ns);
					}
					xsltArgumentList.AddParam(pair.Key.String, ns, XsltConvertor.PhpToDotNet(pair.Value));
				}
			}
			else
			{
				string name_str = PHP.Core.Convert.ObjectToString(name);

				if (xsltArgumentList.GetParam(name_str, ns) != null) xsltArgumentList.RemoveParam(name_str, ns);
				xsltArgumentList.AddParam(name_str, ns, XsltConvertor.PhpToDotNet(value));
			}

			return true;
		}

		/// <summary>
		/// Gets value of a parameter.
		/// </summary>
		/// <param name="ns">The namespace URI of the XSLT parameter.</param>
		/// <param name="name">The local name of the XSLT parameter.</param>
		/// <returns>The value of the parameter or NULL if it's not set.</returns>
		[PhpVisible]
		public object getParameter(string ns, string name)
		{
			return XsltConvertor.DotNetToPhp(xsltArgumentList.GetParam(name, ns));
		}

		/// <summary>
		/// Removes a parameter.
		/// </summary>
		/// <param name="ns">The namespace URI of the XSLT parameter.</param>
		/// <param name="name">The local name of the XSLT parameter.</param>
		/// <returns><B>True</B> or <B>false</B>.</returns>
		[PhpVisible]
		public object removeParameter(string ns, string name)
		{
			return (xsltArgumentList.RemoveParam(name, ns) != null);
		}

		/// <summary>
		/// Determine if this extension has EXSLT support.
		/// </summary>
		/// <returns><B>False</B>.</returns>
		/// <remarks>
		/// A EXSLT implementation for the .NET XSL can be found here
		/// <A href="http://mvp-xml.sourceforge.net/exslt/">http://mvp-xml.sourceforge.net/exslt/</A>.</remarks>
		[PhpVisible]
		public object hasExsltSupport()
		{
			if (!mvpXmlAvailable)
			{
				PhpException.Throw(PhpError.Notice, Resources.ExsltSupportMissing);
				return false;
			}
			else return true;
		}

		/// <summary>
		/// Enables the ability to use PHP functions as XSLT functions.
		/// </summary>
		/// <param name="restrict">A string or array denoting function(s) to be made callable.</param>
		[PhpVisible]
		public void registerPHPFunctions([Optional] object restrict)
		{
			if (xsltUserFunctionHandler == null)
			{
				xsltUserFunctionHandler = new XsltUserFunctionHandler();
				xsltArgumentList.AddExtensionObject(PhpNameSpaceUri, xsltUserFunctionHandler);
			}

			if (restrict == null) xsltUserFunctionHandler.RegisterAllFunctions();
			else
			{
				// check for string argument
				string func_name = PhpVariable.AsString(restrict);
				if (func_name != null) xsltUserFunctionHandler.RegisterFunction(func_name);
				else
				{
					// check for array argument
					PhpArray func_names = restrict as PhpArray;
					if (func_names != null)
					{
						foreach (KeyValuePair<IntStringKey, object> pair in func_names)
						{
							xsltUserFunctionHandler.RegisterFunction(PHP.Core.Convert.ObjectToString(pair.Key.Object));
						}
					}
					else xsltUserFunctionHandler.RegisterAllFunctions();
				}
			}
		}

		#endregion
	}

	/// <summary>
	/// Provides conversion routines between .NET and PHP representation of W3C data types.
	/// </summary>
	internal static class XsltConvertor
	{
		#region Conversions

		/// <summary>
		/// Converts a W3C .NET object to the corresponding W3C PHP object.
		/// </summary>
		public static object DotNetToPhp(object arg)
		{
			// Result Tree Fragment (XSLT) / Node (XPath)
			XPathNavigator nav = arg as XPathNavigator;
			if (nav != null) return DOMNode.Create(nav.UnderlyingObject as XmlNode);

			// Node Set (XPath) - XPathNavigator[]
			XPathNavigator[] navs = arg as XPathNavigator[];
			if (navs != null)
			{
				PhpArray array = new PhpArray(navs.Length, 0);

				for (int i = 0; i < navs.Length; i++)
				{
					IXmlDomNode node = DOMNode.Create(navs[i].UnderlyingObject as XmlNode);
					if (node != null) array.Add(node);
				}

				return array;
			}

			// Node Set (XPath) - XPathNodeIterator
			XPathNodeIterator iter = arg as XPathNodeIterator;
			if (iter != null)
			{
				PhpArray array = new PhpArray();

				foreach (XPathNavigator navigator in iter)
				{
					IXmlDomNode node = DOMNode.Create(navigator.UnderlyingObject as XmlNode);
					if (node != null) array.Add(node);
				}

				return array;
			}

			// Number (XPath), Boolean (XPath), String (XPath)
			return arg;
		}

		/// <summary>
		/// Converts a W3C PHP object to the corresponding W3C .NET object.
		/// </summary>
		public static object/*!*/ PhpToDotNet(object arg)
		{
			if (arg == null) return String.Empty;

			// Node* (XPath)
			IXmlDomNode node = arg as IXmlDomNode;
			if (node != null) return node.UnderlyingObject.CreateNavigator();

			// Node Set (XPath), Result Tree Fragment (XSLT)
			DOMNodeList list = arg as DOMNodeList;
			if (list != null)
			{
				XPathNavigator[] navs = new XPathNavigator[list.length];

				int i = 0;
                foreach (var pair in (IEnumerable<KeyValuePair<object, object>>)list)
				{
					navs[i++] = ((IXmlDomNode)pair.Value).UnderlyingObject.CreateNavigator();
				}

				return navs;
			}

			// any other object
			IPhpVariable var = arg as IPhpVariable;
			if (var != null) return var.ToString();

			// String (XPath), Boolean (XPath), Number (XPath)
			return arg;
		}

		/// <summary>
		/// Converts a W3C PHP object to a corresponding string.
		/// </summary>
		public static string/*!*/ PhpToString(object arg)
		{
			// Node* (XPath)
			IXmlDomNode node = arg as IXmlDomNode;
			if (node != null) return node.UnderlyingObject.Value;

			// Node Set (XPath), Result Tree Fragment (XSLT)
			DOMNodeList list = arg as DOMNodeList;
			if (list != null)
			{
				if (list.length == 0) return String.Empty;
				return list.item(0).UnderlyingObject.Value;
			}

			// any other object
			return PHP.Core.Convert.ObjectToString(arg);
		}

		#endregion
	}

	/// <summary>
	/// Handles PHP function invocations via <code>php:function</code> and <code>php:functionString</code>.
	/// </summary>
	internal sealed class XsltUserFunctionHandler
	{
		#region Fields

		private bool allFunctionsRegistered;
		private Dictionary<string, PhpCallback> registeredFunctions = new Dictionary<string, PhpCallback>();

		#endregion

		#region Function registration

		internal void RegisterAllFunctions()
		{
			allFunctionsRegistered = true;
		}

		internal void RegisterFunction(string functionName)
		{
			if (!registeredFunctions.ContainsKey(functionName))
			{
				registeredFunctions.Add(functionName, null);
			}
		}

		#endregion

		#region Function invocation

		private object InvokeFunction(string name, params object[] args)
		{
			return XsltConvertor.PhpToDotNet(InvokeFunctionCore(name, args));
		}

		private string InvokeFunctionString(string name, params object[] args)
		{
			return XsltConvertor.PhpToString(InvokeFunctionCore(name, args));
		}

		private object InvokeFunctionCore(string name, params object[] args)
		{
			// check whether this function is allowed to be called
			PhpCallback callback;
			if (allFunctionsRegistered)
			{
				registeredFunctions.TryGetValue(name, out callback);
			}
			else
			{
				if (registeredFunctions.TryGetValue(name, out callback))
				{
					PhpException.Throw(PhpError.Warning, String.Format(Resources.HandlerNotAllowed, name));
					return null;
				}
			}

			// if the callback does not already exists, create it
			if (callback == null)
			{
				// parse name
				int index = name.IndexOf("::");
				switch (index)
				{
					case -1: callback = new PhpCallback(name); break;
					case 0: callback = new PhpCallback(name.Substring(2)); break;
					default: callback = new PhpCallback(name.Substring(0, index), name.Substring(index + 2)); break;
				}

				if (!callback.Bind()) return null;

				registeredFunctions[name] = callback;
			}

			// convert arguments
			for (int i = 0; i < args.Length; i++) args[i] = XsltConvertor.DotNetToPhp(args[i]);

			// invoke!
			return callback.Invoke(args);
		}

		#endregion

		#region function (exposed to XSL)

		public object function(string name)
		{
			return InvokeFunction(name);
		}

		public object function(string name, object arg1)
		{
			return InvokeFunction(name, arg1);
		}

		public object function(string name, object arg1, object arg2)
		{
			return InvokeFunction(name, arg1, arg2);
		}

		public object function(string name, object arg1, object arg2, object arg3)
		{
			return InvokeFunction(name, arg1, arg2, arg3);
		}

		public object function(string name, object arg1, object arg2, object arg3, object arg4)
		{
			return InvokeFunction(name, arg1, arg2, arg3, arg4);
		}

		public object function(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5)
		{
			return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5);
		}

		public object function(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6)
		{
			return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6);
		}

		public object function(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7)
		{
			return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
		}

		public object function(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8)
		{
			return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
		}

		public object function(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9)
		{
			return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
		}

		public object function(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
		{
			return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
		}

		public object function(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
		{
			return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
		}

		public object function(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
			object arg12)
		{
			return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
				arg12);
		}

		public object function(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
			object arg12, object arg13)
		{
			return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
				arg12, arg13);
		}

		public object function(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
			object arg12, object arg13, object arg14)
		{
			return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
				arg12, arg13, arg14);
		}

		public object function(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
			object arg12, object arg13, object arg14, object arg15)
		{
			return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
				arg12, arg13, arg14, arg15);
		}

		public object function(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
			object arg12, object arg13, object arg14, object arg15, object arg16)
		{
			return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
				arg12, arg13, arg14, arg15, arg16);
		}

		public object function(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
			object arg12, object arg13, object arg14, object arg15, object arg16, object arg17)
		{
			return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
				arg12, arg13, arg14, arg15, arg16, arg17);
		}

		public object function(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
			object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18)
		{
			return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
				arg12, arg13, arg14, arg15, arg16, arg17, arg18);
		}

		public object function(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
			object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18,
			object arg19)
		{
			return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
				arg12, arg13, arg14, arg15, arg16, arg17, arg18, arg19);
		}

		public object function(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
			object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18,
			object arg19, object arg20)
		{
			return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
				arg12, arg13, arg14, arg15, arg16, arg17, arg18, arg19, arg20);
		}

		#endregion

		#region functionString (exposed to XSL)

		public object functionString(string name)
		{
			return InvokeFunctionString(name);
		}

		public object functionString(string name, object arg1)
		{
			return InvokeFunctionString(name, arg1);
		}

		public object functionString(string name, object arg1, object arg2)
		{
			return InvokeFunctionString(name, arg1, arg2);
		}

		public object functionString(string name, object arg1, object arg2, object arg3)
		{
			return InvokeFunctionString(name, arg1, arg2, arg3);
		}

		public object functionString(string name, object arg1, object arg2, object arg3, object arg4)
		{
			return InvokeFunctionString(name, arg1, arg2, arg3, arg4);
		}

		public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5)
		{
			return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5);
		}

		public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6)
		{
			return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6);
		}

		public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7)
		{
			return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
		}

		public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8)
		{
			return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
		}

		public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9)
		{
			return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
		}

		public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
		{
			return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
		}

		public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
		{
			return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
		}

		public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
			object arg12)
		{
			return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
				arg12);
		}

		public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
			object arg12, object arg13)
		{
			return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
				arg12, arg13);
		}

		public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
			object arg12, object arg13, object arg14)
		{
			return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
				arg12, arg13, arg14);
		}

		public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
			object arg12, object arg13, object arg14, object arg15)
		{
			return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
				arg12, arg13, arg14, arg15);
		}

		public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
			object arg12, object arg13, object arg14, object arg15, object arg16)
		{
			return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
				arg12, arg13, arg14, arg15, arg16);
		}

		public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
			object arg12, object arg13, object arg14, object arg15, object arg16, object arg17)
		{
			return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
				arg12, arg13, arg14, arg15, arg16, arg17);
		}

		public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
			object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18)
		{
			return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
				arg12, arg13, arg14, arg15, arg16, arg17, arg18);
		}

		public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
			object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18,
			object arg19)
		{
			return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
				arg12, arg13, arg14, arg15, arg16, arg17, arg18, arg19);
		}

		public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
			object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
			object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18,
			object arg19, object arg20)
		{
			return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
				arg12, arg13, arg14, arg15, arg16, arg17, arg18, arg19, arg20);
		}

		#endregion
	}
}
