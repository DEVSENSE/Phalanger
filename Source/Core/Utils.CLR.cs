/*

 Copyright (c) 2004-2006 Tomas Matousek and Ladislav Prosek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Xml;
using System.Threading;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Configuration;
using System.Web;
using System.Security.Permissions;
using System.Web.Configuration;

namespace PHP.Core
{
	#region Debug

	/// <summary>
	/// Support for debugging (replaces <see cref="System.Diagnostics.Debug"/>).
	/// </summary>
	public static partial class Debug
	{
		/// <summary>
		/// Initializes log logging in the context of web server.
		/// The log file is created in <see cref="HttpRuntime.CodegenDir"/> directory.
		/// </summary>
		[Conditional("DEBUG")]
		internal static void WebInitialize()
		{
            try
            {
                string debug_dir = Path.Combine(HttpRuntime.CodegenDir, "Debug");
                string debug_file = DateTime.Now.ToString("HH-mm-ss-ffff") + ".log";
                Directory.CreateDirectory(debug_dir);

                StreamWriter writer = new StreamWriter(Path.Combine(debug_dir, debug_file));

                writer.AutoFlush = true;
                TextWriterTraceListener listener = new TextWriterTraceListener(writer);
                listener.IndentSize = 2;

                System.Diagnostics.Debug.Listeners.Clear();
                System.Diagnostics.Debug.Listeners.Add(listener);
            }
            catch
            { }
		}

		[Conditional("DEBUG")]
		public static void ConsoleInitialize(string dir)
		{
			Directory.CreateDirectory(dir);

			StreamWriter writer = new StreamWriter(Path.Combine(dir, DateTime.Now.ToString("HH-mm-ss-ffff") + ".log"));

			writer.AutoFlush = true;
			TextWriterTraceListener listener = new TextWriterTraceListener(writer);
			listener.IndentSize = 2;

			System.Diagnostics.Debug.Listeners.Clear();
			System.Diagnostics.Debug.Listeners.Add(listener);
		}
	}

	#endregion

	#region Config Utils

	/// <summary>
	/// Utils for parsing Phalanger XML configuration file.
	/// </summary>
	public static class ConfigUtils
	{
		/// <summary>
		/// Exception thrown if a node is invalid.
		/// </summary>
		public class InvalidNodeException : ConfigurationErrorsException
		{
			public InvalidNodeException(XmlNode node)
				: base(CoreResources.GetString("invalid_node"), node)
			{ }
		}

		/// <summary>
		/// Exception thrown if a value of an attribute is not valid.
		/// </summary>
		public class InvalidAttributeValueException : ConfigurationErrorsException
		{
			public InvalidAttributeValueException(XmlNode node, string attributeName)
				: base(CoreResources.GetString("invalid_attribute_value", attributeName), node) { }
		}

		/// <summary>
		/// Gets a string value contained in the mandatory attribute of specified name.
		/// </summary>
		/// <param name="node">The node which attribute get.</param>
		/// <param name="name">The name of attribute.</param>
		/// <returns>The string value contained in the attribute.</returns>
		/// <exception cref="ConfigurationErrorsException">The attribute is missing.</exception>
		public static string MandatoryAttribute(XmlNode/*!*/ node, string/*!*/ name)
		{
			if (node == null)
				throw new ArgumentNullException("node");
			if (name == null)
				throw new ArgumentNullException("name");

			XmlAttribute a = node.Attributes[name];
			if (a == null)
			{
				throw new ConfigurationErrorsException(CoreResources.GetString("missing_attribute", name), node);
			}
			return a.Value;
		}

		/// <summary>
		/// Gets a string value contained in the optional attribute of specified name.
		/// </summary>
		/// <param name="node">The node which attribute get.</param>
		/// <param name="name">The name of attribute.</param>
		/// <returns>The string value contained in the attribute or a <B>null</B> reference if the attribute is missing.</returns>
		public static string OptionalAttribute(XmlNode/*!*/ node, string/*!*/ name)
		{
			if (node == null)
				throw new ArgumentNullException("node");
			if (name == null)
				throw new ArgumentNullException("name");

			XmlAttribute a = node.Attributes[name];
			return (a != null) ? a.Value : null;
		}

		/// <summary>
		/// Parses a configuration contained in the specified node.
		/// The section is expected to follow Phalanger configuration schema.
		/// </summary>
		/// <param name="node">The node to parse.</param>
		/// <param name="context">The configuration context or a <B>null</B> reference.</param>
		/// <param name="section1">The section to fill in.</param>
		public static void ParseNameValueList(XmlNode/*!*/ node, PhpConfigurationContext context,
			IPhpConfigurationSection/*!*/ section1)
		{
			ParseNameValueList(node, context, section1, null, null);
		}

		/// <summary>
		/// Parses a configuration contained in the specified node.
		/// The section is expected to follow Phalanger configuration schema.
		/// </summary>
		/// <param name="node">The node to parse.</param>
		/// <param name="context">The configuration context or a <B>null</B> reference.</param>
		/// <param name="section1">The first section to fill in.</param>
		/// <param name="section2">The second section to fill in if the first doesn't contain the option.</param>
		public static void ParseNameValueList(XmlNode/*!*/ node, PhpConfigurationContext context,
			IPhpConfigurationSection/*!*/ section1, IPhpConfigurationSection section2)
		{
			ParseNameValueList(node, context, section1, section2, null);
		}

		/// <summary>
		/// Parses a configuration contained in the specified node.
		/// The section is expected to follow Phalanger configuration schema.
		/// </summary>
		/// <param name="node">The node to parse.</param>
		/// <param name="context">The configuration context or a <B>null</B> reference.</param>
		/// <param name="section1">The first section to fill in.</param>
		/// <param name="section2">The second section to fill in if the first doesn't contain the option. Can be <B>null</B>.</param>
		/// <param name="section3">The third section to fill in if neither the first not the second contain the option. Can be <B>null</B>.</param>
		/// <remarks>
		/// The following node type is allowed to be contained in the <paramref name="node"/>:
		/// <code>
		///   &lt;set name="{string}" value="{string}" [allowOverride="{bool}"] /&gt;
		///   &lt;set name="{string}" [allowOverride="{bool}"] &gt; ... &lt;/set&gt;
		/// </code>                                          
		/// </remarks>
		public static void ParseNameValueList(XmlNode/*!*/ node, PhpConfigurationContext context,
			IPhpConfigurationSection/*!*/ section1, IPhpConfigurationSection section2, IPhpConfigurationSection section3)
		{
			if (node == null)
				throw new ArgumentNullException("node");
			if (section1 == null)
				throw new ArgumentNullException("section1");

			foreach (XmlNode child in node.ChildNodes)
			{
				if (child.Name == "set")
				{
					string name = MandatoryAttribute(child, "name");
					string allow_override = OptionalAttribute(child, "allowOverride");
					string/*!*/value = OptionalAttribute(child, "value") ?? string.Empty;

					// checks for sealed nodes:
					if (context != null)
					{
						if (context.IsOptionSealed(name))
							throw new ConfigurationErrorsException(CoreResources.GetString("cannot_modify_option", context.GetSealingLocation(name)), child);

						if (allow_override != null && context.IsSubApplicationConfig())
							throw new ConfigurationErrorsException(CoreResources.GetString("invalid_attribute_location", context.VirtualPath, "allowOverride"), node);

						if (allow_override == "false")
							context.SealOption(name);
					}

					// processes the option:                             					
					if ((/*section1 == null ||*/ !section1.Parse(name, value, child)) &&
						(section2 == null || !section2.Parse(name, value, child)) &&
						(section3 == null || !section3.Parse(name, value, child)))
						throw new InvalidAttributeValueException(child, "name");
				}
				else if (child.NodeType == XmlNodeType.Element)
				{
					throw new InvalidNodeException(child);
				}
			}
		}

		/// <summary>
		/// Parses a configuration contained in the specified node and its children in a form of flags.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="value">An initial value of the flags.</param>
		/// <param name="type">The type of flag enumeration.</param>
		/// <returns>An updated value after all flags stated in the sub-nodes are added/removed/cleared.</returns>
		/// <remarks>
		/// The following node types are allowed to be contained in the <paramref name="node"/>:
		/// <code>
		///  (&lt;add value="{enum field list}" /&gt; |
		///   &lt;remove value="{enum field list}" /&gt; |
		///   &lt;clear/&gt;)*
		/// </code>
		/// </remarks>
		public static int ParseFlags(XmlNode/*!*/ node, int value, Type/*!*/ type)
		{
			if (node == null)
				throw new ArgumentNullException("node");
			if (type == null)
				throw new ArgumentNullException("type");
			if (!type.IsEnum)
				throw new ArgumentException("Type must be an enumeration.");

			foreach (XmlNode child in node.ChildNodes)
			{
				switch (child.Name)
				{
					case "add":
					case "remove":
						{
							string s = ConfigUtils.MandatoryAttribute(child, "value");
							try
							{
								int v = (s != "") ? (int)Enum.Parse(type, s, true) : 0;

								if (child.Name == "remove")
									value &= ~v;
								else
									value |= v;
							}
							catch (Exception)
							{
								throw new ConfigUtils.InvalidAttributeValueException(child, "value");
							}
							break;
						}

					case "clear":
						value = 0;
						break;

					default:
						if (child.NodeType == XmlNodeType.Element)
							throw new InvalidNodeException(child);
						break;
				}
			}

			return value;
		}

        /// <summary>
        /// Get the file name of given <see cref="XmlDocument"/>.
        /// </summary>
        /// <param name="document">Xml config file.</param>
        /// <returns>File name of the xml document or null.</returns>
        public static string GetConfigXmlPath(XmlDocument/*!*/document)
        {
            Debug.Assert(document != null);

            var errorInfo = document as System.Configuration.Internal.IConfigErrorInfo;
            var configXml = document as System.Configuration.ConfigXmlDocument;

            if (document.BaseURI != "")
                return document.BaseURI;
            else if (errorInfo != null)
                return errorInfo.Filename;
            else if (configXml != null)
                return configXml.Filename;
            else
                return AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
        }

        /// <summary>
        /// Determine the configuration file for given <see cref="XmlNode"/> and its last modification time.
        /// </summary>
        /// <param name="node"><see cref="XmlNode"/> from a configuration file.</param>
        /// <param name="maxTime">Currently latest modification time. The returned value cannot be lower.</param>
        /// <returns>Time of the configuration file modification or <see cref="DateTime.MinValue"/>.</returns>
        public static DateTime GetConfigModificationTime(XmlNode/*!*/node, DateTime maxTime)
        {
            Debug.Assert(node != null);

            try
            {
                var d = File.GetLastWriteTime(GetConfigXmlPath(node.OwnerDocument));
                return (d > maxTime) ? d : maxTime;
            }
            catch
            {
                return maxTime;
            }
        }

        /// <summary>
        /// Get the full URI of the specified <c>url</c>. Uses path of the configuration file to resolve URI.
        /// If the path is not available, uses current directory.
        /// </summary>
        /// <param name="node">The XML node used to obtain the uri.</param>
        /// <param name="url">Relative or absolute URL of the file.</param>
        /// <returns>Resolved URI from <c>url</c>. To resolve relative path it use the file name of the XML.
        /// It filename cannot be resolved from the <c>node</c> it uses current configuration path.</returns>
        public static Uri GetUri(XmlNode node, string url)
        {
            return new Uri(new Uri(GetConfigXmlPath(node.OwnerDocument)), url);
        }

		/// <summary>
		/// Parses a list of string items inserting parsed values in a specified list.
		/// </summary>
		/// <param name="node">XML node containing a list.</param>
		/// <param name="result">The list where to add items.</param>
		/// <remarks>
		/// The following node types are allowed to be contained in the <paramref name="node"/>:
		/// <code>
		///   &lt;add value="{string}" /&gt;
		///   &lt;remove value="{string}" /&gt;
		///   &lt;clear/&gt;
		/// </code>
		/// </remarks>
		public static void ParseStringList(XmlNode/*!*/ node, IList<string>/*!*/ result)
		{
			if (node == null)
				throw new ArgumentNullException("node");
			if (result == null)
				throw new ArgumentNullException("result");

			foreach (XmlNode child in node.ChildNodes)
			{
				switch (child.Name)
				{
					case "add":
						{
							string s = ConfigUtils.MandatoryAttribute(child, "value");
							result.Add(s);
							break;
						}

					case "remove":
						{
							string s = ConfigUtils.MandatoryAttribute(child, "value");
							result.Remove(s);
							break;
						}

					case "clear":
						result.Clear();
						break;

					default:
						if (child.NodeType == XmlNodeType.Element)
							throw new ConfigUtils.InvalidNodeException(child);
						break;
				}
			}
		}

		/// <summary>
		/// Callback used by <see cref="ParseLibraryAssemblyList"/>.
		/// Returning <B>false</B> will stop parsing.
		/// </summary>
		/// <param name="assemblyName">Parsed long assembly name or a <B>null</B> reference.</param>
		/// <param name="assemblyUrl">Parsed assembly file URL or a <B>null</B> reference.</param>
		/// <param name="sectionName">Configuration section name or a <B>null</B> reference.</param>
		/// <param name="node">XML node being parsed.</param>
		/// <remarks>
		/// Either <paramref name="assemblyName"/> or <paramref name="assemblyUrl"/> is always non-null.
		/// </remarks>
		public delegate bool ParseLibraryAssemblyCallback(string assemblyName, Uri assemblyUrl, string sectionName, XmlNode/*!*/ node);

        /// <summary>
        /// Callback used by <see cref="ParseScriptLibraryAssemblyList" />. 
        /// Returning <b>false</b> will stop parsing.
        /// </summary>
        /// <param name="assemblyName">Parsed long assembly name or a <B>null</B> reference.</param>
        /// <param name="assemblyUrl">Parsed assembly file URL or a <B>null</B> reference.</param>
        /// <param name="libraryRootPath">Library root which will be used.</param>
        /// <returns></returns>
        public delegate bool ParseScriptLibraryAssemblyCallback(string assemblyName, Uri assemblyUrl, string libraryRootPath);

		/// <summary>
		/// Parses list of library assemblies.
		/// </summary>
		/// <param name="node">Node containing the list.</param>
		/// <param name="libraries">List of libraries to be modified by given <paramref name="node"/>.</param>
		/// <param name="extensionsPath">Full path to the extensions directory.</param>
		/// <param name="librariesPath">Full path to the libraries directory.</param>
		/// <remarks>
		/// The following node type is allowed to be contained in the <paramref name="node"/>:
		/// <code>
		///   &lt;add assembly="{string}" [section="{string}"] {additional attributes specific to library} /&gt;
		/// </code>
		/// </remarks>
		public static void ParseLibraryAssemblyList(XmlNode/*!*/ node,
            LibrariesConfigurationList/*!*/ libraries,
			FullPath extensionsPath, FullPath librariesPath)
		{
			if (node == null)
				throw new ArgumentNullException("node");
            if (libraries == null)
                throw new ArgumentNullException("libraries");

			foreach (XmlNode child in node.ChildNodes)
			{
                if (child.Name == "add" || child.Name == "remove")
				{
					if (!Configuration.IsValidInCurrentScope(child)) continue;

					string assembly_name = ConfigUtils.OptionalAttribute(child, "assembly");
					string library_name = ConfigUtils.OptionalAttribute(child, "library");
					string extension_name = ConfigUtils.OptionalAttribute(child, "extension");
					string url = ConfigUtils.OptionalAttribute(child, "url");
					string section_name = ConfigUtils.OptionalAttribute(child, "section");
					Uri uri = null;

					if (assembly_name == null && url == null && extension_name == null && library_name == null)
						throw new ConfigurationErrorsException(CoreResources.GetString("missing_attribute", "assembly"), child);

					if (library_name != null)
					{
						try
						{
							uri = new Uri("file:///" + Path.Combine(librariesPath.IsEmpty ? "" : librariesPath, library_name + ".dll"));
						}
						catch (UriFormatException)
						{
							throw new InvalidAttributeValueException(child, "library");
						}
					}

					if (extension_name != null)
					{
						try
						{
							uri = new Uri("file:///" + Externals.GetWrapperPath(extension_name, extensionsPath.IsEmpty ? "" : extensionsPath));
						}
						catch (UriFormatException)
						{
							throw new InvalidAttributeValueException(child, "extension");
						}
					}

					if (url != null)
					{
						try
						{
                            uri = GetUri(node, url);
						}
						catch (UriFormatException)
						{
							throw new InvalidAttributeValueException(child, "url");
						}
					}

                    if (child.Name == "add")
                        libraries.AddLibrary(assembly_name, uri, section_name, child);
                    else if (child.Name == "remove")
                        libraries.RemoveLibrary(assembly_name, uri);
                    else
                        Debug.Fail();
				}
                else if (child.Name == "clear")
                {
                    libraries.ClearLibraries();
                }
                else if (child.NodeType == XmlNodeType.Element)
                {
                    throw new ConfigUtils.InvalidNodeException(child);
                }
			}
		}

        internal static void ParseScriptLibraryAssemblyList(XmlNode/*!*/ node, ScriptLibraryDatabase/*!*/librares)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            if (librares == null)
                throw new ArgumentNullException("librares");

            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name == "add" || child.Name == "remove")
                {
                    if (!Configuration.IsValidInCurrentScope(child)) continue;

                    string assemblyName = ConfigUtils.OptionalAttribute(child, "assembly");
                    //purposefully disabled (not needed and there are some integrity problems regarding library root)
                    string libraryRoot = ConfigUtils.OptionalAttribute(child, "root");
                    string assemblyUrl = ConfigUtils.OptionalAttribute(child, "url");
                    Uri uri = null;

                    if (assemblyName == null && assemblyUrl == null)
                        throw new ConfigurationErrorsException(string.Format(CoreResources.missing_attribute, "assembly"), child);

                    if (assemblyUrl != null)
                    {
                        try
                        {
                            uri = GetUri(node, assemblyUrl);
                        }
                        catch (UriFormatException)
                        {
                            throw new InvalidAttributeValueException(child, "url");
                        }
                    }

                    if (child.Name == "add")
                        librares.AddLibrary(assemblyName, uri, assemblyUrl, libraryRoot);
                    else
                        librares.RemoveLibrary(assemblyName, uri, assemblyUrl, libraryRoot);
                }
                else if (child.Name == "clear")
                {
                    librares.ClearLibraries();
                }
                else if (child.NodeType == XmlNodeType.Element)
                {
                    throw new ConfigUtils.InvalidNodeException(child);
                }
            }
        }

        public static void ParseTypesList(XmlNode/*!*/ node,
            Action<string>/*!*/ addCallback,
            Action<string>/*!*/ removeCallback,
            Action<object>/*!*/ clearCallback)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            if (addCallback == null)
                throw new ArgumentNullException("addCallback");
            if (removeCallback == null)
                throw new ArgumentNullException("removeCallback");
            if (clearCallback == null)
                throw new ArgumentNullException("clearCallback");

            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name == "add" || child.Name == "remove")
                {
                    if (!Configuration.IsValidInCurrentScope(child)) continue;

                    string typeQualifiedName = ConfigUtils.MandatoryAttribute(child, "type");
                    
                    if (child.Name == "add")
                        addCallback(typeQualifiedName);
                    else
                        removeCallback(typeQualifiedName);
                }
                else if (child.Name == "clear")
                {
                    clearCallback(null);
                }
                else if (child.NodeType == XmlNodeType.Element)
                {
                    throw new ConfigUtils.InvalidNodeException(child);
                }
            }
        }

        /// <summary>
		/// Parses an integer from a string.
		/// </summary>
		/// <param name="value">The string.</param>
		/// <param name="min">The minimal possible value for the resulting integer.</param>
		/// <param name="max">The maximal possible value for the resulting integer.</param>
		/// <param name="node">The configuration node being parsed.</param>
		/// <returns>The value.</returns>
		/// <exception cref="ConfigurationErrorsException">Invalid format or out of range.</exception>
		public static int ParseInteger(string value, int min, int max, XmlNode node)
		{
			int result;
			try
			{
				result = Int32.Parse(value);
			}
			catch (System.Exception e)
			{
				throw new ConfigurationErrorsException(e.Message, node);
			}

			if (result < min || result > max)
				throw new ConfigurationErrorsException(CoreResources.GetString("out_of_range", min, max), node);

			return result;
		}

		/// <summary>
		/// Parses a double from a string.
		/// </summary>
		/// <param name="value">The string.</param>
		/// <param name="node">The configuration node being parsed.</param>
		/// <returns>The value.</returns>
		/// <exception cref="ConfigurationErrorsException">Invalid format.</exception>
		public static double ParseDouble(string value, XmlNode node)
		{
			double result;
			try
			{
				result = Double.Parse(value);
			}
			catch (System.Exception e)
			{
				throw new ConfigurationErrorsException(e.Message, node);
			}

			return result;
		}

		public static int[]/*!*/ ParseIntegerList(string/*!*/ value, char separator, int min, int max, XmlNode node)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			string[] components = value.Split(',');
			int[] result = new int[components.Length];

			for (int i = 0; i < components.Length; i++)
				result[i] = ParseInteger(components[i], min, max, node);

			return result;
		}
	}

	#endregion

	#region File System

	/// <summary>
	/// File system utilities.
	/// </summary>
	public static partial class FileSystemUtils
	{
		/// <summary>
		/// Retrieves information about the amount of space available on a disk volume.
		/// </summary>
		/// <param name="directoryName">A directory on the disk of interest (can be a network UNC path). 
		/// A <b>null</b> reference means the root of the current disk.</param>
		/// <param name="freeBytesAvailable">Receives the total number of free bytes on the disk that are 
		/// available to the user associated with the calling thread.</param>
		/// <param name="totalNumberOfBytes">Receives the total number of bytes on the disk that are 
		/// available to the user associated with the calling thread.</param>
		/// <param name="totalNumberOfFreeBytes">Receives the total number of free bytes on the disk.</param>
		/// <returns>Whether a call of method was successful.</returns>
		[DllImport("kernel32.dll", EntryPoint = "GetDiskFreeSpaceEx")]
		public static extern bool GetDiskFreeSpace(
			string directoryName,
			out long freeBytesAvailable,
			out long totalNumberOfBytes,
			out long totalNumberOfFreeBytes);

		public static string CanonizePath(string path, string root)
		{
			return Path.GetFullPath(Path.Combine(root, path));
		}


        public static string[] GetFiles(string path, string searchPattern)
        {
            return GetFileSystemEntries(path, searchPattern, true, false);
        }

        public static string[] GetDirectories(string path, string searchPattern)
        {
            return GetFileSystemEntries(path, searchPattern, false, true);
        }

        public static string[] GetFileSystemEntries(string path, string searchPattern)
        {
            return GetFileSystemEntries(path, searchPattern, true, true);
        }

        public static string[] GetFileSystemEntries(string path, string searchPattern, bool includeFiles, bool includeDirectories)
        {
            if (includeFiles && includeDirectories)
            {
                return System.IO.Directory.GetFileSystemEntries(path, searchPattern);
            }
            if (includeFiles)
            {
                return System.IO.Directory.GetFiles(path, searchPattern);
            }
            if (includeDirectories)
            {
                return System.IO.Directory.GetDirectories(path, searchPattern);
            }
            return ArrayUtils.EmptyStrings;
        }


		/// <summary>
		/// Gets a list of file full paths contained in a specified directories.
		/// </summary>
		/// <param name="paths">List of paths to files and/or directories.</param>
		/// <returns>
		/// List of all files contained in <paramref name="paths"/> and all files contained
		/// in directories whose paths are contained in the <paramref name="paths"/> list.
		/// </returns>
		public static List<FullPath>/*!*/ GetAllFiles(IEnumerable<FullPath>/*!*/ paths)
		{
			if (paths == null) throw new ArgumentNullException("paths");

			List<FullPath> result = new List<FullPath>();

			foreach (FullPath path in paths)
			{
				if (path.FileExists)
					result.Add(path);
				else if (path.DirectoryExists)
					GetAllFiles(path, result);
			}

			return result;
		}

        /// <summary>
        /// Gets a list of files contained in a specified directories.
        /// </summary>
        /// <param name="paths">List of paths to files and/or directories.</param>
        /// <returns>
        /// List of all files contained in <paramref name="paths"/> and all files contained
        /// in directories whose paths are contained in the <paramref name="paths"/> list.
        /// </returns>
        /// <remarks>It is safe to pass <see cref="ResourceFileReference"/> here</remarks>
        public static List<FileReference>/*!*/ GetAllFiles(IEnumerable<FileReference>/*!*/ paths) {
            if(paths == null) throw new ArgumentNullException("paths");

            List<FileReference> result = new List<FileReference>();

            foreach (FileReference path in paths)
            {
                if(path.Path.FileExists || path is ResourceFileReference)
                    result.Add(path);
                else if(path.Path.DirectoryExists)
                    foreach (FullPath file in GetAllFiles(new FullPath[] { path.Path }))
                        result.Add(new FileReference(file));
            }

            return result;
        }

		private static void GetAllFiles(string/*!*/ dir, ICollection<FullPath>/*!*/ result)
		{
			foreach (string file in Directory.GetFiles(dir))
				result.Add(new FullPath(file, false));

			foreach (string subdir in Directory.GetDirectories(dir))
				GetAllFiles(new FullPath(subdir, false), result);
		}

		/// <summary>
		/// Seeks a specified line in a file and reads its content.
		/// </summary>
		/// <exception cref="Exception">Any exception the <see cref="File.OpenText"/> or <see cref="StreamReader.ReadLine"/> may throw.</exception>
		public static string ReadFileLine(string/*!*/ filePath, int line)
		{
			using (StreamReader reader = File.OpenText(filePath))
			{
				for (int i = 0; i < line - 1; i++) reader.ReadLine();
				return reader.ReadLine();
			}
		}

		public static int GetByteOrderMarkLength(Stream/*!*/ stream)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			if (stream.Position != 0)
				return 0;

			byte[] b = new byte[4];
			int count = stream.Read(b, 0, 4);

			stream.Seek(0, SeekOrigin.Begin);

			if (count >= 2)
			{
				if (b[0] == 0xfe && b[1] == 0xff)
				{
					return 2;
				}
				else if (b[0] == 0xff && b[1] == 0xfe)
				{
					return (count > 4 && b[2] == 0 && b[3] == 0) ? 4 : 2;
				}
				else if (count > 3 && b[0] == 0xef && b[1] == 0xbb && b[2] == 0xbf)
				{
					return 3;
				}
			}
			return 0;
		}
	}

	#endregion

	#region Network

	/// <summary>
	/// Network utilities.
	/// </summary>
	public sealed class NetworkUtils
	{
		/// <summary>
		/// A singleton whose finalizer shuts down Winsock.
		/// </summary>
		private static NetworkUtils singleton = new NetworkUtils();

		/// <summary>
		/// Creates a new <see cref="NetworkUtils"/> singleton whose purpose is to initialize and shut down Winsock.
		/// </summary>
		private NetworkUtils()
		{
            if (Environment.Is64BitProcess)
            {
                var wsa_data = new WsaData64();
                if (WSAStartup64(WORD_VERSION, ref wsa_data) != 0)
                    throw new NotSupportedException(CoreResources.GetString("networkutils_unsupported"));
            }
            else
            {
                var wsa_data = new WsaData32();
                if (WSAStartup32(WORD_VERSION, ref wsa_data) != 0)
                    throw new NotSupportedException(CoreResources.GetString("networkutils_unsupported"));
            }
		}

		/// <summary>
		/// Shuts down Winsock.
		/// </summary>
		~NetworkUtils()
		{
            WSACleanup();
		}

        const int WSADESCRIPTION_LEN = 256;
        const int WSASYS_STATUS_LEN = 128;

        public const ushort HIGH_VERSION = 2;
        public const ushort LOW_VERSION = 2;
        public const short WORD_VERSION = (ushort)(HIGH_VERSION << 8) + LOW_VERSION;
        
        /// <summary>
		/// Managed representation of the <c>WSADATA</c> structure.
		/// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WsaData32
        {
            public ushort wVersion;
            public ushort wHighVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WSADESCRIPTION_LEN + 1)]public String szDescription;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WSASYS_STATUS_LEN + 1)]public String szSystemStatus;
            public ushort iMaxSockets;
            public ushort iMaxUdpDg;
            public IntPtr lpVendorInfo;
        }
        /// <summary>
        /// Managed representation of the <c>WSADATA</c> structure.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WsaData64
        {
            public ushort wVersion;
            public ushort wHighVersion;
            public ushort iMaxSockets;
            public ushort iMaxUdpDg;
            public IntPtr lpVendorInfo;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WSADESCRIPTION_LEN + 1)]
            public String szDescription;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WSASYS_STATUS_LEN + 1)]
            public String szSystemStatus;
        }
        
		/// <summary>
		/// Initializes Winsock for the current process.
		/// </summary>
        /// <param name="wVersionRequested">The Winsock version requested by caller.</param>
		/// <param name="wsaData">Receives information about Winsock implementation.</param>
		/// <returns>Zero if successfull, a non-zero error code otherwise.</returns>
        [DllImport("ws2_32.dll", EntryPoint = "WSAStartup", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int WSAStartup32(Int16 wVersionRequested, ref WsaData32 wsaData);

        /// <summary>
        /// Initializes Winsock for the current process.
        /// </summary>
        /// <param name="wVersionRequested">The Winsock version requested by caller.</param>
        /// <param name="wsaData">Receives information about Winsock implementation.</param>
        /// <returns>Zero if successfull, a non-zero error code otherwise.</returns>
        [DllImport("ws2_32.dll", EntryPoint = "WSAStartup", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int WSAStartup64(Int16 wVersionRequested, ref WsaData64 wsaData);

		/// <summary>
		/// Shuts down Winsock for the current process.
		/// </summary>
		/// <returns>Zero if successfull, a non-zero error code otherwise.</returns>
		[DllImport("ws2_32.dll")]
        private static extern int WSACleanup();

		/// <summary>
		/// Managed representation of the <c>protoent</c> structure.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public class ProtoEnt
		{
            [MarshalAs(UnmanagedType.LPStr)]
            public string p_name;
			public IntPtr p_aliases;
			public short p_proto;

            /// <summary>
            /// Marshales native pointer to <see cref="ProtoEnt"/> instance.
            /// </summary>
            /// <param name="ptr">Pointer returned by <see cref="getprotobyname"/> or <see cref="getprotobynumber"/>.</param>
            /// <remarks>The wrapper avoids freeing of pointer returned from winsoc native library. The returned pointer is managed by winsoc library and must not be freed by CLI.</remarks>
            internal static ProtoEnt FromIntPtr(IntPtr ptr)
            {
                if (ptr == IntPtr.Zero)
                return null;

                // marshall returned object to ProtoEnt class:
                ProtoEnt result = new ProtoEnt();
                Marshal.PtrToStructure(ptr, result);
                return result;
            }
		}

		/// <summary>
		/// Managed representation of the <c>servent</c> structure.
		/// </summary>
		public class ServEnt
		{
            public string s_name;
			public short s_port;
            public string s_proto;

            //struct  servent
            //{
            //    char    FAR * s_name;           /* official service name */
            //    char    FAR * FAR * s_aliases;  /* alias list */
            //#ifdef _WIN64
            //    char    FAR * s_proto;          /* protocol to use */
            //    short   s_port;                 /* port # */
            //#else
            //    short   s_port;                 /* port # */
            //    char    FAR * s_proto;          /* protocol to use */
            //#endif
            //};

            [StructLayout(LayoutKind.Sequential)]
		    private class x86
            {
                [MarshalAs(UnmanagedType.LPStr)]
                public string s_name;           // official service name
                public IntPtr s_aliases;        // alias list
                public short   s_port;          // port #
                [MarshalAs(UnmanagedType.LPStr)]
                public string s_proto;          // protocol to use
            }

            [StructLayout(LayoutKind.Sequential)]
		    private class x64
            {
                [MarshalAs(UnmanagedType.LPStr)]
                public string s_name;           // official service name
                public IntPtr s_aliases;        // alias list
                [MarshalAs(UnmanagedType.LPStr)]
                public string s_proto;          // protocol to use
                public short   s_port;          // port #
            }

            /// <summary>
            /// Marshales native pointer to <see cref="ServEnt"/> instance.
            /// </summary>
            /// <param name="ptr">Pointer returned by <see cref="getservbyname"/> or <see cref="getservbyport"/>.</param>
            /// <remarks>The wrapper avoids freeing of pointer returned from winsoc native library. The returned pointer is managed by winsoc library and must not be freed by CLI.</remarks>
            internal static ServEnt FromIntPtr(IntPtr ptr)
            {
                if (ptr == IntPtr.Zero)
                return null;

                // marshall returned object to ProtoEnt class:
                if (Environment.Is64BitProcess)
                {
                    var result = new ServEnt.x64();
                    Marshal.PtrToStructure(ptr, result);
                    return new ServEnt()
                    {
                        s_name = result.s_name,
                        s_port = result.s_port,
                        s_proto = result.s_proto
                    };
                }
                else
                {
                    var result = new ServEnt.x86();
                    Marshal.PtrToStructure(ptr, result);
                    return new ServEnt()
                    {
                        s_name = result.s_name,
                        s_port = result.s_port,
                        s_proto = result.s_proto
                    };
                }
            }
		}

		/// <summary>
		/// Retrieves the protocol information corresponding to a protocol name.
		/// </summary>
		/// <param name="name">The protocol name.</param>
		/// <returns>Protocol information or <B>null</B> if an error occurs.</returns>
        [DllImport("ws2_32.dll")]
        private static extern IntPtr getprotobyname([MarshalAs(UnmanagedType.LPStr)]string name);

        /// <summary>
        /// Safe wrapper for <see cref="getprotobyname"/> function call.
        /// </summary>
        /// <param name="name">The protocol name.</param>
		/// <returns>Protocol information or <B>null</B> if an error occurs.</returns>
        /// <remarks>The wrapper avoids freeing of pointer returned from <see cref="getprotobyname"/>. The returned pointer is managed by winsoc library and must not be freed by CLI.</remarks>
        public static ProtoEnt GetProtocolByName(string name)
        {
            return ProtoEnt.FromIntPtr(getprotobyname(name));
        }

		/// <summary>
		/// Retrieves protocol information corresponding to a protocol number.
		/// </summary>
		/// <param name="number">The protocol number.</param>
		/// <returns>Protocol information or <B>null</B> if an error occurs.</returns>
        [DllImport("ws2_32.dll")]
        private static extern IntPtr getprotobynumber(int number);

        /// <summary>
        /// Safe wrapper for <see cref="getprotobynumber"/> function call.
        /// </summary>
        /// <param name="number">The protocol number.</param>
		/// <returns>Protocol information or <B>null</B> if an error occurs.</returns>
        public static ProtoEnt GetProtocolByNumber(int number)
        {
            return ProtoEnt.FromIntPtr(getprotobynumber(number));
        }

		/// <summary>
		/// Retrieves service information corresponding to a service name and protocol.
		/// </summary>
		/// <param name="name">The service name.</param>
		/// <param name="proto">The protocol name or <B>null</B> if only <paramref name="name"/> should be matched.
		/// </param>
		/// <returns>Service information or <B>null</B> if an error occurs.</returns>
        [DllImport("ws2_32.dll")]
		private static extern IntPtr getservbyname([MarshalAs(UnmanagedType.LPStr)]string name, [MarshalAs(UnmanagedType.LPStr)]string proto);

        /// <summary>
        /// Safe wrapper for <see cref="getservbyname"/> function call.
        /// </summary>
		/// <param name="name">The service name.</param>
		/// <param name="proto">The protocol name or <B>null</B> if only <paramref name="name"/> should be matched.
		/// </param>
		/// <returns>Service information or <B>null</B> if an error occurs.</returns>
        public static ServEnt GetServiceByName(string name, string proto)
        {
            return ServEnt.FromIntPtr(getservbyname(name, proto));
        }

		/// <summary>
		/// Retrieves service information corresponding to a port and protocol.
		/// </summary>
		/// <param name="port">The port number (network order).</param>
		/// <param name="proto">The protocol name or <B>null</B> if only <paramref name="port"/> should be matched.
		/// </param>
		/// <returns>Service information or <B>null</B> if an error occurs.</returns>
        [DllImport("ws2_32.dll")]
		public static extern IntPtr getservbyport(int port, [MarshalAs(UnmanagedType.LPStr)]string proto);

        /// <summary>
		/// Safe wrapper for <see cref="getservbyport"/> function call.
		/// </summary>
		/// <param name="port">The port number (network order).</param>
		/// <param name="proto">The protocol name or <B>null</B> if only <paramref name="port"/> should be matched.
		/// </param>
		/// <returns>Service information or <B>null</B> if an error occurs.</returns>
        public static ServEnt GetServiceByPort(int port, string proto)
        {
            return ServEnt.FromIntPtr(getservbyport(port, proto));
        }
	}

	#endregion

	#region Performance Counters

	internal static class Performance
	{
		const string CategoryName = "Phalanger";
		const string CompiledEvalCountName = "Compiled eval count";
		const string DynamicCacheHitsName = "Dynamic cache hits";
		const string ArrayDCsName = "Array DCs";

#pragma warning disable 649
		public static PerformanceCounter CompiledEvalCount;
		public static PerformanceCounter DynamicCacheHits;
		public static PerformanceCounter ArrayDCs;
#pragma warning restore 649

		public static void Increment(PerformanceCounter counter)
		{
			if (counter != null)
				counter.Increment();
		}

		static Performance()
		{
#if PERFORMANCE_COUNTERS
			if (!PerformanceCounterCategory.Exists(CategoryName) ||
				  !PerformanceCounterCategory.CounterExists(CompiledEvalCountName,CategoryName) || 
			    !PerformanceCounterCategory.CounterExists(DynamicCacheHitsName,CategoryName) || 
			    !PerformanceCounterCategory.CounterExists(ArrayDCsName,CategoryName))
			{    
				try
				{
					if (PerformanceCounterCategory.Exists(CategoryName))
					  PerformanceCounterCategory.Delete(CategoryName);
				  
				  PerformanceCounterCategory.Create(CategoryName,"Phalanger performance counters",
						PerformanceCounterCategoryType.SingleInstance,
						new CounterCreationDataCollection(
						  new CounterCreationData[] 
						  {
							  new CounterCreationData(CompiledEvalCountName,"Number of compiled evals",PerformanceCounterType.NumberOfItems32),
						    new CounterCreationData(DynamicCacheHitsName,"Number of dynamic cache hits",PerformanceCounterType.NumberOfItems32),
						    new CounterCreationData(ArrayDCsName,"Number of array deep copies",PerformanceCounterType.NumberOfItems32),
						  }
						)
				  );
				}
				catch(System.Security.SecurityException)
				{
					return;  
				}  
			} 
			CompiledEvalCount = new PerformanceCounter(CategoryName,CompiledEvalCountName,false);
			DynamicCacheHits = new PerformanceCounter(CategoryName,DynamicCacheHitsName,false);
			ArrayDCs = new PerformanceCounter(CategoryName,ArrayDCsName,false);
#endif
		}

		public static void Initialize()
		{
			if (CompiledEvalCount != null)
			{
				CompiledEvalCount.RawValue = 0;
				DynamicCacheHits.RawValue = 0;
				ArrayDCs.RawValue = 0;
			}
		}
	}

	#endregion

	#region Icons Resources

	/// <summary>
	/// Represents a Win32 icon resource.
	/// </summary>
	/// <remarks>
	/// Supports creation from an <B>.ICO</B> file and conversion to a <B>.RES</B> file. Contains a group of
	/// Win32 icons read from one <B>.ICO</B> file.
	/// </remarks>
	[Serializable]
	public sealed class Win32IconResource
	{
		/// <summary>
		/// Represents one Win32 icon.
		/// </summary>
		[Serializable]
        private class Win32Icon
		{
			public byte bWidth;
			public byte bHeight;
			public byte bColorCount;
			public byte bReserved;
			public ushort wPlanes;
			public ushort wBitCount;
			public ushort id;
			public byte[] image;
		}

		/// <summary>
		/// The icons in the group.
		/// </summary>
		private Win32Icon[] icons;

		/// <summary>
		/// Creates a new <see cref="Win32IconResource"/> given an <B>.ICO</B> file stream.
		/// </summary>
		/// <param name="stream">The <B>.ICO</B> file stream.</param>
		public Win32IconResource(Stream/*!*/ stream)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			ReadFromIcoFile(stream);
		}

		/// <summary>
		/// Creates a new <see cref="Win32IconResource"/> given an <B>.ICO</B> file path.
		/// </summary>
		/// <param name="icoFilePath">The <B>.ICO</B> file path.</param>
		public Win32IconResource(string/*!*/ icoFilePath)
		{
			if (icoFilePath == null)
				throw new ArgumentNullException("icoFilePath");

			using (FileStream stream = new FileStream(icoFilePath, FileMode.Open))
				ReadFromIcoFile(stream);
		}

		public void DefineIconResource(AssemblyBuilder/*!*/ builder, string/*!*/ tempFile)
		{
			if (builder == null)
				throw new ArgumentNullException("builder");
			if (tempFile == null)
				throw new ArgumentNullException("tempFile");

			MemoryStream mem = new MemoryStream();
			WriteToResFile(mem);
			byte[] b = mem.ToArray();
			//builder.DefineUnmanagedResource(b);

			using (FileStream fs = new FileStream(tempFile, FileMode.Create))
			{
				WriteToResFile(fs);
			}

			builder.DefineUnmanagedResource(tempFile);

			//			fs = new FileStream("sample2.res", FileMode.Open);
			//			byte[] q = new byte[fs.Length];
			//			fs.Read(q, 0, q.Length);
			//			builder.DefineUnmanagedResource(q);
		}

		/// <summary>
		/// Writes one resource header to a provided <see cref="BinaryWriter"/>.
		/// </summary>
		/// <param name="w">The <see cref="BinaryWriter"/> to write the header to.</param>
		/// <param name="dataLength">The length of the data that comprise this resource.</param>
		/// <param name="type">The resource type code.</param>
		/// <param name="id">The resource ID.</param>
		/// <param name="memoryFlags">The resource memory flags.</param>
		private void WriteResourceHeader(BinaryWriter w, int dataLength, ushort type, ushort id, ushort memoryFlags)
		{
			w.Write((int)dataLength);
			w.Write((int)32); // header length

			w.Write((ushort)0xFFFF);
			w.Write((ushort)type); // resource type

			w.Write((ushort)0xFFFF);
			w.Write((ushort)id); // resource ID

			w.Write((int)0); // data version (reserved?)
			w.Write((ushort)memoryFlags); // memory flags
			w.Write((ushort)0x0409); // language ID (English)

			w.Write((int)0); // version
			w.Write((int)0); // characteristics
		}

		/// <summary>
		/// Writes the icon resource to a provided <B>.RES</B> file stream.
		/// </summary>
		/// <param name="stream">The output stream.</param>
		private void WriteToResFile(Stream stream)
		{
			using (BinaryWriter w = new BinaryWriter(stream))
			{
				// "illegal" resource to indicate 32-bit resources
				w.Write((ulong)0x0000002000000000);
				w.Write((ulong)0x0000FFFF0000FFFF);
				w.Write((ulong)0x0000000000000000);
				w.Write((ulong)0x0000000000000000);

				// write RT_ICON resources
				for (int i = 0; i < icons.Length; i++)
				{
					Win32Icon icon = icons[i];

					// RT_ICON resource, ID icon.id, DISCARDABLE | MOVABLE
					WriteResourceHeader(w, icons[i].image.Length, 3, icons[i].id, 0x1010);

					w.Write(icons[i].image);
				}

				// RT_GROUP_ICON resource, ID 100, DISCARDABLE | PURE | MOVABLE
				WriteResourceHeader(w, 6 + 14 * icons.Length, 14, 100, 0x1030);

				w.Write((short)0);
				w.Write((short)1);
				w.Write((short)icons.Length);
				for (int i = 0; i < icons.Length; i++)
				{
					Win32Icon icon = icons[i];

					w.Write(icon.bWidth);
					w.Write(icon.bHeight);
					w.Write(icon.bColorCount);
					w.Write((byte)0);
					w.Write(icon.wPlanes);
					w.Write(icon.wBitCount);
					w.Write((int)icon.image.Length);
					w.Write(icon.id);
				}
			}
		}

		/// <summary>
		/// Reads the icon resource from a provided <B>.ICO</B> file stream.
		/// </summary>
		/// <param name="stream">The input stream.</param>
		/// <exception cref="InvalidDataException">The icon has an invalid format.</exception>
		private void ReadFromIcoFile(Stream/*!*/ stream)
		{
			icons = null;

			long max_length = (stream.CanSeek) ? stream.Length : Int32.MaxValue;

			using (BinaryReader r = new BinaryReader(stream))
			{
				int idReserved = r.ReadInt16();
				int idType = r.ReadInt16();
				if (idReserved != 0 || idType != 1)
				{
					throw new ArgumentException("Invalid .ICO file format", "stream");
				}
				long count = r.ReadInt16();

				icons = new Win32Icon[count];

				for (int i = 0; i < count; i++)
				{
					Win32Icon icon = new Win32Icon();

					icon.bWidth = r.ReadByte();
					icon.bHeight = r.ReadByte();
					icon.bColorCount = r.ReadByte();
					icon.bReserved = r.ReadByte();
					icon.wPlanes = r.ReadUInt16();
					icon.wBitCount = r.ReadUInt16();
					icon.id = (ushort)(i + 1);

					int length = r.ReadInt32();
					int offset = r.ReadInt32();

					// prevents allocation boom when the length or possions are invalid:
					if (length > max_length || offset > max_length)
						throw new InvalidDataException(CoreResources.GetString("invalid_icon_format"));

					icon.image = new byte[length];
					long pos = stream.Position;
					stream.Position = offset;
					stream.Read(icon.image, 0, length);
					stream.Position = pos;

					// The wPlanes and wBitCount members in the ICONDIRENTRY structure can be 0,
					// so we set them from the BITMAPINFOHEADER structure that follows
					if (icon.wPlanes == 0) icon.wPlanes = (ushort)(icon.image[12] | (icon.image[13] << 8));
					if (icon.wBitCount == 0) icon.wBitCount = (ushort)(icon.image[14] | (icon.image[15] << 8));

					icons[i] = icon;
				}
			}
		}
	}

	#endregion
}
