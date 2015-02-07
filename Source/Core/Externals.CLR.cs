/*

 Copyright (c) 2004-2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Xml;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;

using PHP.Library;
using PHP.Core;
using PHP.Core.Reflection;
using System.Text;

namespace PHP.Core
{
	#region IExternals

	/// <summary>
	/// This is the main interface between <c>PHP.NET Core</c> and <c>ExtManager</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This interface is implemented by <c>RemoteDispatcher</c> class in <c>php4ts.dll</c> (project ExtSupport).
	/// Most of these methods also have their static counterparts in <see cref="Externals"/> (stubs).
	/// </para>
	/// <para>
	/// For extensions configured as &quot;isolated&quot;, <see cref="IExternals"/> serves as a remote interface
	/// for interprocess communication. For extensions configured as &quot;collocated&quot;, an instance
	/// of <c>RemoteDispatcher</c> implementing <see cref="IExternals"/> is created in the current
	/// <see cref="AppDomain"/>.
	/// </para>
	/// </remarks>
	public interface IExternals
    {
        #region External function/method invocation

        /// <summary>
        /// Get external function proxy object. The object is used for invocation specified function/method.
        /// </summary>
        /// <param name="moduleName"></param>
        /// <param name="className"></param>
        /// <param name="functionName"></param>
        /// <returns></returns>
        IExternalFunction GetFunctionProxy(string moduleName, string className, string functionName);

        /// <summary>
		/// Invokes an external function.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="InvokeFunction"]/*'/>
		/// <param name="workingDir">Current working directory.</param>
		object InvokeFunction(string moduleName, string functionName, ref object[] args, int[] refInfo, string workingDir);

		/// <summary>
		/// Invokes an external method.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="InvokeMethod"]/*'/>
		/// <param name="workingDir">Current working directory.</param>
		object InvokeMethod(string moduleName, string className, string methodName, ref PhpObject self, ref object[] args, int[] refInfo, string workingDir);

		/// <summary>
		/// Returns a proxy of a variable (<c>zval</c>) that lives in <c>ExtManager</c> as one of the last function/method
		/// invocation parameters.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="BindParameter"]/*'/>
		IExternalVariable BindParameter(int paramIndex);

		#endregion

		#region Stream wrapper retrieval

		/// <summary>
		/// Returns a proxy of a stream wrapper that lives in <c>ExtManager</c>.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="GetStreamWrapper"]/*'/>
		IExternalStreamWrapper GetStreamWrapper(string scheme);

		/// <summary>
		/// Returns an <see cref="ICollection"/> of schemes of all available external stream wrappers.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="GetStreamWrapperSchemes"]/*'/>
		ICollection GetStreamWrapperSchemes();

		#endregion

		#region Information retrieval and wrapper generation

		/// <summary>
		/// Returns an <see cref="ICollection"/> of error messages.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="GetStartupErrors"]/*'/>
		ICollection GetStartupErrors();

		/// <summary>
		/// Gathers information about loaded extensions.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="PhpInfo"]/*'/>
		string PhpInfo();

		/// <summary>
		/// Returns an <see cref="ICollection"/> of names of extensions that are currently loaded.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="GetModules"]/*'/>
		ICollection GetModules(bool internalNames);

		/// <summary>
		/// Checks whether a given extension is currently loaded.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="GetModuleVersion"]/*'/>
		string GetModuleVersion(string moduleName, bool internalName, out bool loaded);

		/// <summary>
		/// Returns an <see cref="ICollection"/> of names of functions in a given extension.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="GetFunctionsByModule"]/*'/>
		ICollection GetFunctionsByModule(string moduleName, bool internalName);

		/// <summary>
		/// Returns an <see cref="ICollection"/> of names of classes in a given extension.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="GetClassesByModule"]/*'/>
		ICollection GetClassesByModule(string moduleName, bool internalName);

		/// <summary>
		/// Generates the managed wrapper for a given extension.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="GenerateManagedWrapper"]/*'/>
		string GenerateManagedWrapper(string moduleName);

		#endregion

		#region Request delimitation and lifetime

		/// <summary>
		/// Instructs the <c>ExtManager</c> to load an extension.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="LoadExtensions"]/*'/>
		bool LoadExtension(ExtensionLibraryDescriptor descriptor);

		/// <summary>
		/// Associates calling <see cref="Thread"/> with a new request.
		/// <seealso cref="IRequestTerminator"/>
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="BeginRequest"]/*'/>
		void BeginRequest();

		/// <summary>
		/// Terminates the request currently associated with calling <see cref="Thread"/>.
		/// <seealso cref="IRequestTerminator"/>
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="EndRequest"]/*'/>
		void EndRequest();

		/// <summary>
		/// Returns the private URL of the current <c>ExtManager</c>.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="GetInstanceUrl"]/*'/>
        string GetInstanceUrl(string generalUrl, ApplicationConfiguration appConfig, Dictionary<string, ExtensionLibraryDescriptor> extConfig);

		/// <summary>
		/// Instructs the <c>ExtManager</c> to shut down gracefully.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="GracefulShutdown"]/*'/>
		void GracefulShutdown();

		#endregion

		#region INI entry management

		/// <summary>
		/// Sets an INI value that might have an effect on an extension.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="IniSet"]/*'/>
		bool IniSet(string varName, string newValue, out string oldValue);

		/// <summary>
		/// Gets an INI value related to extensions.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="IniGet"]/*'/>
		bool IniGet(string varName, out string value);

		/// <summary>
		/// Restores an INI value related to extensions.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="IniRestore"]/*'/>
		bool IniRestore(string varName);

		/// <summary>
		/// Gets all INI entry names and values.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="IniGetAll"]/*'/>
		PhpArray IniGetAll(string extension);

		/// <summary>
		/// Determines whether a given extension registered a given INI entry name.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="IniOptionExists"]/*'/>
		bool IniOptionExists(string moduleName, string varName);

		#endregion
	}

	#endregion

    #region IExternalFunction

    /// <summary>
    /// Proxy object calling actual function/method in ExtManager.
    /// </summary>
    public interface IExternalFunction
    {
        /// <summary>
        /// Invoke external function directly.
        /// </summary>
        /// <param name="self">This object. It can be null in case of global function or static method.</param>
        /// <param name="args">List or arguments.
        /// Ref is used to skip marshaling of arguments (by returning null) if nothing is passed by reference (most of the cases).</param>
        /// <param name="refInfo">List of indexes of arguments passed by reference.
        /// -1 means all remaining arguments are passed by reference.
        /// Can be null; in this case no arguments are passed back if they was changed.</param>
        /// <param name="workingDir">Current working directory.</param>
        /// <returns>The external function return value.</returns>
        object Invoke(PhpObject self, ref object[] args, int[] refInfo, string workingDir);

        /// <summary>
        /// Get corresponding extension manager.
        /// </summary>
        IExternals ExtManager { get; }
    }

    #endregion

	#region StatStruct, IExternalStream, IExternalStreamWrapper

	/// <summary>
	/// Managed equivalent of the CRT <c>stat</c> structure.
	/// </summary>
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct StatStruct
	{
		public uint st_dev;
		public ushort st_ino;
		public ushort st_mode;
		public short st_nlink;
		public short st_uid;
		public short st_gid;
		public uint st_rdev;
		public int st_size;
		public long st_atime;
		public long st_mtime;
		public long st_ctime;
	}

	/// <summary>
	/// Interface implemented by a <see cref="MarshalByRefObject"/> that lives in <c>ExtManager</c> and serves
	/// as a proxy for the underlying native PHP stream wrapper.
	/// </summary>
	/// <remarks>
	/// The methods here basically constitute managed versions of operations listed in the <c>php_stream_wrapper_ops</c>
	/// structure in PHP.
	/// </remarks>
	public interface IExternalStreamWrapper
	{
		/// <summary>
		/// Returns an <see cref="IExternalStream"/> proxy of a native PHP stream.
		/// </summary>
		/// <param name="path">The file path passed to <c>fopen</c> PHP function.</param>
		/// <param name="mode">The mode passed to <c>fopen</c> PHP function.</param>
		/// <param name="options">Combination of <c>StreamWrapper.StreamOpenFlags</c>.</param>
		/// <param name="opened_path">The real full path of the file actually opened.</param>
		/// <param name="context">The context provided for the stream wrapper at the call to <c>fopen</c>
		/// PHP function.</param>
		/// <returns>
		/// A new <see cref="MarshalByRefObject"/> implementing the <see cref="IExternalStream"/> interface
		/// or <B>null</B> if there was an error.
		/// </returns>
		IExternalStream Open(string path, string mode, int options, out string opened_path, object context);

		/// <include file='Doc/Wrappers.xml' path='/docs/method[@name="Stat"]/*'/>
		StatStruct Stat(string path, int options, object context, bool streamStat);

		/// <include file='Doc/Wrappers.xml' path='/docs/method[@name="Unlink"]/*'/>
		bool Unlink(string path, int options, object context);

		/// <include file='Doc/Wrappers.xml' path='/docs/method[@name="Listing"]/*'/>
		string[] Listing(string path, int options, object context);

		/// <include file='Doc/Wrappers.xml' path='/docs/method[@name="Rename"]/*'/>
		bool Rename(string fromPath, string toPath, int options, object context);

		/// <include file='Doc/Wrappers.xml' path='/docs/method[@name="MakeDirectory"]/*'/>
		bool MakeDirectory(string path, int accessMode, int options, object context);

		/// <include file='Doc/Wrappers.xml' path='/docs/method[@name="RemoveDirectory"]/*'/>
		bool RemoveDirectory(string path, int options, object context);

		/// <include file='Doc/Wrappers.xml' path='/docs/property[@name="Label"]/*'/>
		string Label
		{
			get;
		}

		/// <include file='Doc/Wrappers.xml' path='/docs/property[@name="IsUrl"]/*'/>
		bool IsUrl
		{
			get;
		}
	}

	/// <summary>
	/// Interface implemented by a <see cref="MarshalByRefObject"/> that lives in <c>ExtManager</c> and serves
	/// as a proxy for the underlying native PHP stream.
	/// </summary>
	/// <remarks>
	/// The methods here basically constitute managed versions of operations listed in the <c>php_stream_ops</c>
	/// structure in PHP.
	/// </remarks>
	public interface IExternalStream
	{
		/// <include file='Doc/Streams.xml' path='/docs/method[@name="RawWrite"]/*'/>
		int Write(byte[] buffer, int offset, int count);

		/// <include file='Doc/Streams.xml' path='/docs/method[@name="RawRead"]/*'/>
		int Read(ref byte[] buffer, int offset, int count);

		/// <summary>
		/// Closes the stream.
		/// </summary>
		/// <returns><B>true</B> on success, <B>false</B> on error.</returns>
		bool Close();

		/// <include file='Doc/Streams.xml' path='/docs/method[@name="RawFlush"]/*'/>
		bool Flush();

		/// <include file='Doc/Streams.xml' path='/docs/method[@name="RawSeek"]/*'/>
		bool Seek(int offset, SeekOrigin whence);

		/// <summary>
		/// Gets the current position in the stream.
		/// </summary>
		/// <returns>The position or <c>-1</c> on error.</returns>
		int Tell();

		/// <include file='Doc/Streams.xml' path='/docs/property[@name="RawEof"]/*'/>
		bool Eof();

		/// <summary>
		/// Returns the status array for the stram.
		/// </summary>
		/// <returns>A stat array describing the stream.</returns>
		StatStruct Stat();
	}

	#endregion

	#region IExternalVariable

	/// <summary>
	/// Interface implemented by a <see cref="MarshalByRefObject"/> that lives in <c>ExtManager</c> and serves
	/// as a proxy for the underlying native PHP variable (a <c>zval</c> structure).
	/// </summary>
	/// <remarks>
	/// After an external function that binds variables (<c>mssql_bind</c> or <c>oci_bind_by_name</c> for example)
	/// is invoked, instances implementing this interface are constructed in <c>ExtManager</c> and remote references
	/// are stored in <see cref="Externals.boundVariables"/>. Later, when an external function that works with bound
	/// variables (<c>mssql_execute</c>, <c>oci_execute</c>) is invoked, underlying native PHP variables are synchronized
	/// with their managed counterparts (in one or both directions).
	/// </remarks>
	public interface IExternalVariable
	{
		/// <summary>
		/// Retrieves the underlying variable's value.
		/// </summary>
		/// <returns>The value.</returns>
		object GetValue();

		/// <summary>
		/// Sets the underlying variable's value.
		/// </summary>
		/// <param name="value">The value.</param>
		void SetValue(object value);

		/// <summary>
		/// Unbinds this variable so that the implementing proxy instance can be discarded and the underlying
		/// <c>zval</c> released.
		/// </summary>
		void Unbind();
	}

	#endregion

	#region Exceptions

	/// <summary>
	/// Extension exception. Thrown when something goes wrong with extension management, wrapper generation,
	/// external function invocation etc.
	/// </summary>
	[Serializable]
	public class ExtensionException : ApplicationException
	{
		/// <summary>
		/// Creates a new <see cref="ExtensionException"/>.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public ExtensionException(string message)
			: base(message)
		{ }

		/// <summary>
		/// Creates a new instance of the <see cref="ExtensionException"/> class with a specified error message
		/// and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="message">The error message that explains why the exception occurred.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public ExtensionException(string message, Exception innerException)
			: base(message, innerException)
		{ }

		/// <include file='Doc/Common.xml' path='/docs/method[@name="serialization.ctor"]/*'/>
		protected ExtensionException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{ }
	}

	/// <summary>
	/// Thrown in <c>Module</c>'s constructor in <c>ExtSupport</c> when something goes wrong about extensions
	/// loading.
	/// </summary>
	[Serializable]
	public class CouldNotLoadExtensionException : ExtensionException
	{
		/// <summary>
		/// Creates a new instance of <see cref="CouldNotLoadExtensionException"/> class with a specified
		/// extension file name and a message that describes the error.
		/// </summary>
		/// <param name="fileName">The file name of the extension.</param>
		/// <param name="message">The message that describes the error.</param>
		public CouldNotLoadExtensionException(string fileName, string message)
			: base(CoreResources.GetString("could_not_load_extension") + fileName + ". " + message)
		{ }

		/// <summary>
		/// Creates a new instance of the <see cref="CouldNotLoadExtensionException"/> class with a specified
		/// extension file name, an error message and a reference to the inner exception that is the cause of
		/// this exception.
		/// </summary>
		/// <param name="fileName">The file name of the extension.</param>
		/// <param name="message">The error message that explains why the exception occurred.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public CouldNotLoadExtensionException(string fileName, string message, Exception innerException)
			: base(CoreResources.GetString("could_not_load_extension") + fileName + ". " + message, innerException)
		{ }

		/// <include file='Doc/Common.xml' path='/docs/method[@name="serialization.ctor"]/*'/>
		protected CouldNotLoadExtensionException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{ }
	}

	#endregion

	#region Extension configuration

	/// <summary>
	/// Decorates managed wrapper assemblies providing information about the wrapped native extension.
	/// </summary>
	[Serializable]
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
	public class ExtensionDescriptorAttribute : PhpLibraryAttribute
	{
		/// <summary>
		/// Creates a new <see cref="ExtensionDescriptorAttribute"/>.
		/// </summary>
		/// <param name="fileName">The extension file name without trailing <c>.DLL</c> (for example php_sockets).</param>
		/// <param name="internalName">Internal extension name (for example sockets).</param>
		/// <param name="earlyInit"><B>True</B> if the extension requires early init semantics, <B>false</B> otherwise.</param>
        public ExtensionDescriptorAttribute(string fileName, string internalName, bool earlyInit)
            : base(typeof(ExtensionLibraryDescriptor), internalName, new string[] { internalName }, false, true)
        {
            this.fileName = fileName;
            this.earlyInit = earlyInit;
        }

		/// <summary>
		/// Creates a new <see cref="ExtensionDescriptorAttribute"/>.
		/// </summary>
		/// <param name="fileName">The extension file name without trailing <c>.DLL</c> (for example php_sockets).</param>
		/// <param name="internalName">Internal extension name (for example sockets).</param>
		public ExtensionDescriptorAttribute(string fileName, string internalName)
			: this(fileName, internalName, false)
		{ }

		/// <summary>
		/// Returns the native extension file name.
		/// </summary>
		public string FileName
		{
			get { return fileName; }
		}

		/// <summary>
		/// Returns the native extension internal name.
		/// </summary>
		public string InternalName
		{
			get { return base.Name; }
		}

		/// <summary>
		/// Returns <B>true</B> if the extension requires early init semantics, <B>false</B> otherwise.
		/// </summary>
		public bool EarlyInit
		{
			get { return earlyInit; }
		}

		private string fileName;
		private bool earlyInit;
	}

	/// <summary>
	/// Describes a class library implemented in a native extension.
	/// </summary>
	[Serializable]
	public sealed class ExtensionLibraryDescriptor : PhpLibraryDescriptor, ISerializable, IDeserializationCallback
	{
        /// <summary>
        /// The version of ExtSupport to use.
        /// </summary>
        public enum ExtSupportVersion
        {
            php4ts = 0,
            php5ts = 1,

            /// <summary>
            /// Amount of versions, indexed from 0.
            /// </summary>
            Count
        }

        ///// <summary>
        ///// If <B>true</B>, the extension should be loaded as isolated, otherwise it should collocate.
        ///// </summary>
        //private bool isolated;

		/// <summary>
		/// The attribute which decorates the managed wrapper.
		/// </summary>
        private ExtensionDescriptorAttribute extensionAttribute { get { return (ExtensionDescriptorAttribute)assemblyAttribute; } }

		/// <summary>
		/// The <c>ExtNatives</c> path effective at the time when this extension is configured to be loaded.
		/// </summary>
		private FullPath extensionPath;

		/// <summary>
		/// If set to <B>true</B>, no extension libraries are loaded. Utilized by ExtManager, which acts
		/// like a server and does not want to have the client side of extension-related architecture loaded.
		/// </summary>
		public static bool ServerMode;

		/// <summary>
		/// Returns the file name of the library (file name of the native DLL without the trailing <c>.DLL</c>).
		/// </summary>
		public string FileName
		{
            get { return extensionAttribute.FileName; }
		}

        /// <summary>
        /// The ExtSupport version to be used with this extension.
        /// </summary>
        /// <remarks>TODO: read it from the module info. Currently it determines version of extension by its file name.</remarks>
        public ExtSupportVersion ExtVersion
        {
            get { return FileName.EndsWith(".php5") ? ExtSupportVersion.php5ts : ExtSupportVersion.php4ts; }
        }

		/// <summary>
		/// Returns the full path of the native DLL directory.
		/// </summary>
		public FullPath ExtensionPath
		{
			get
			{ return extensionPath; }
		}

        ///// <summary>
        ///// Returns <B>true</B>, if the extension should be loaded as isolated, <B>false</B> if it should collocate.
        ///// </summary>
        //public bool Isolated
        //{
        //    get
        //    { return isolated; }
        //}

		/// <summary>
		/// Returns <B>true</B>, if the extension should perform eager request init (currently used only for GTK).
		/// </summary>
		public bool EarlyInit
		{
			get
            { return extensionAttribute.EarlyInit; }
		}

		/// <summary>
		/// Only used after the instance is deserialized.
		/// </summary>
		private ExtensionLocalConfig localConfig;

		/// <summary>
		/// Returns the extension configuration.
		/// </summary>
		public ExtensionLocalConfig LocalConfig
		{
			get
			{
				if (localConfig != null) return localConfig;
				else return (ExtensionLocalConfig)Configuration.Local.GetLibraryConfig(this);
			}
		}

        ///// <summary>
        ///// A collection of isolated extensions. Keys are file names (without the trailing <c>.DLL</c>).
        ///// </summary>
        //internal static Dictionary<string, ExtensionLibraryDescriptor> isolatedExtensions = new Dictionary<string, ExtensionLibraryDescriptor>();

		/// <summary>
		/// A collection of collocated extensions. Keys are file names (without the trailing <c>.DLL</c>).
		/// </summary>
        internal static Dictionary<string, ExtensionLibraryDescriptor> collocatedExtensions = new Dictionary<string, ExtensionLibraryDescriptor>();

		/// <summary>
		/// Returns the list of collocated extensions. Called by collocated ExtManager to access
		/// extension configuration.
		/// </summary>
        public static Dictionary<string, ExtensionLibraryDescriptor> CollocatedExtensions
        {
            get
            { return collocatedExtensions; }
        }

		/// <summary>
		/// A collection of collocated extensions. Keys are internal extension names. Value is ignored.
		/// </summary>
        internal static Dictionary<string, bool> collocatedExtensionsByName = new Dictionary<string, bool>();

		public ExtensionLibraryDescriptor()
		{ }

		/// <summary>
		/// Called when library loading is finished and descriptor is initialized.
		/// </summary>
		/// <exception cref="InvalidOperationException">Extension already loaded.</exception>
        /// <exception cref="NotSupportedException">An attempt to load the extension in isolated mode, which is not supported anymore.</exception>
		internal protected override void Loaded(PhpLibraryAttribute/*!*/ assemblyAttribute,
			LibraryConfigStore configStore)
		{
			base.Loaded(assemblyAttribute, configStore);

			//this.assemblyAttribute = (ExtensionDescriptorAttribute)assemblyAttribute;
			this.extensionPath = Configuration.GetPathsNoLoad().ExtNatives;

            if (configStore != null && configStore.Attributes != null)
            {
                var attr = configStore.Attributes["isolated"];
                if (attr != null && attr.Value == "true")
                {
                    // this.isolated = true;
                    throw new NotSupportedException(CoreResources.isolated_extensions_unsupported);
                }
            }

			if (!ServerMode)
			{
				string file_name = FileName.ToLower();

				try
				{
                    //if (Isolated)
                    //{
                    //    isolatedExtensions.Add(file_name, this);
                    //}
                    //else
					{
						collocatedExtensions.Add(file_name, this);
                        collocatedExtensionsByName.Add(assemblyAttribute.Name, true);
					}
				}
				catch (ArgumentException)
				{
					throw new InvalidOperationException(
						CoreResources.GetString("extension_already_loaded", assemblyAttribute.Name, FileName));
				}
			}
		}

		/// <summary>
		/// Creates an empty library configuration context.
		/// </summary>
		/// <returns>An initialized configuration context. Should not be a <B>null</B> reference.</returns>
		internal protected override ConfigContextBase CreateConfigContext()
		{
			return new ConfigContextBase(new ExtensionLocalConfig(), new ExtensionGlobalConfig());
		}

		/// <summary>
		/// Parses a configuration section belonging to an extension. 
		/// </summary>
		/// <param name="result">A configuration context.</param>
		/// <param name="context">The context of the configuration created by Phalanger Core.</param>
		/// <param name="section">A XML node containing the configuration or its part.</param>
		/// <returns>Updated configuration context.</returns>
		protected internal override ConfigContextBase ParseConfig(ConfigContextBase result, PhpConfigurationContext context,
			XmlNode section)
		{
			// parses XML tree:
			ConfigUtils.ParseNameValueList(section, context, (ExtensionLocalConfig)result.Local, (ExtensionGlobalConfig)result.Global);
			return result;
		}

        ///// <summary>
        ///// Returns a list of names of extensions which are implemented by the library.
        ///// </summary>
        //public override string[] ImplementedExtensions
        //{
        //    get { return new string[] { assemblyAttribute.Name }; }
        //}

		/// <summary>
		/// Validates configuration after it has been completely read and registers the extension.
		/// </summary>
		/// <param name="userContext">The configuration context.</param>
		/// <exception cref="ConfigurationErrorsException">Configuration is invalid.</exception>
		internal protected override void Validate(ConfigContextBase userContext)
		{ }

		#region ISerializable Members

		[NonSerialized]
		private object[] deserializedMembers;

        [System.Security.SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			MemberInfo[] members = FormatterServices.GetSerializableMembers(GetType());
			info.AddValue("_members", FormatterServices.GetObjectData(this, members));

			// we want the instance to deserialize with its configuration
			info.AddValue("_config", LocalConfig);
		}

		private ExtensionLibraryDescriptor(SerializationInfo info, StreamingContext context)
		{
			deserializedMembers = (object[])info.GetValue("_members", typeof(object[]));
			localConfig = (ExtensionLocalConfig)info.GetValue("_config", typeof(ExtensionLocalConfig));
		}

		#endregion

		#region IDeserializationCallback Members

		public void OnDeserialization(object sender)
		{
			MemberInfo[] members = FormatterServices.GetSerializableMembers(GetType());
			if (deserializedMembers != null && deserializedMembers.Length == members.Length)
			{
				FormatterServices.PopulateObjectMembers(this, members, deserializedMembers);
			}
			deserializedMembers = null;
		}

		#endregion
	}

	/// <summary>
	/// Script independent extension configuration.
	/// </summary>
	/// <remarks>
	/// All extension configuration is stored in this class because we do not really care whether it
	/// is local or global. <c>ini_set</c> calls directly ExtSupport and the decision whether to permit
	/// the operation depends on how the extension registered the option.
	/// </remarks>
	[Serializable]
	[NoPhpInfoAttribute]
	public sealed class ExtensionLocalConfig : IPhpConfiguration, IPhpConfigurationSection
	{
		/// <summary>
		/// All configuration options relating to the extension.
		/// </summary>
		/// <remarks>
		/// Keys are option names, values are option values (strings) at the beginning. These strings
		/// might be lazily converted to <c>IniConfig</c> instances if ExtSupport is collocated.
		/// </remarks>
		public Hashtable Options;

		internal ExtensionLocalConfig()
		{
			Options = new Hashtable();
		}

		private ExtensionLocalConfig(Hashtable options)
		{
			this.Options = options;
		}

		/// <summary>
		/// Creates a deep copy of the configuration record.
		/// </summary>
		/// <returns>The copy.</returns>
		public IPhpConfiguration DeepCopy()
		{
			return new ExtensionLocalConfig((Hashtable)Options.Clone());
		}

		/// <summary>
		/// Parses a configuration section node having attributes "name" and "value".
		/// </summary>
		public bool Parse(string name, string value, XmlNode node)
		{
			Options[name] = value;
			return true;
		}
	}

	/// <summary>
	/// Script dependent extension configuration (currently empty, see <see cref="ExtensionLocalConfig"/>).
	/// </summary>
	[Serializable]
	[NoPhpInfoAttribute]
	public sealed class ExtensionGlobalConfig : IPhpConfiguration, IPhpConfigurationSection
	{
		internal ExtensionGlobalConfig() { }

		/// <summary>
		/// Parses a configuration section node having attributes "name" and "value".
		/// </summary>
		public bool Parse(string name, string value, XmlNode node)
		{
			return false;
		}

		/// <summary>
		/// Creates a deep copy of the configuration record.
		/// </summary>
		/// <returns>The copy.</returns>
		public IPhpConfiguration DeepCopy()
		{
			return this;
		}
	}

	#endregion

	#region IRequestTerminator and RequestCookie

	/// <summary>
	/// Defines a callback method to be called when processing of the current request is over.
	/// <seealso cref="RequestCookie"/>
	/// </summary>
	public interface IRequestTerminator
	{
		/// <summary>
		/// The callback method to be called when processing of the current request is over.
		/// </summary>
		void Terminate();
	}

	/// <summary>
	/// A cookie associated with threads that make remote calls.
	/// </summary>
	/// <remarks>
	/// <para>
	/// One instance of <c>RequestCookie</c> is associated with each thread that makes remote
	/// calls to isolated external modules (extensions). Its purpose is to terminate the request as soon
	/// as it is clear that this thread won't make any more remote calls. This happens basically at the
	/// moment of <c>RequestCookie</c> finalization. Terminating <c>ExtManager</c>'s requests as soon
	/// as possible is quite important because external modules might hold scarce unmanaged resources
	/// like database connections.
	/// </para>
	/// <para>
	/// If external functions are used during processing of a page request and nothing goes terribly
	/// wrong, <c>ExtManager</c>'s requests are terminated by calling <see cref="Externals.EndRequest"/>.
	/// However, if external functions are used through managed wrappers by an arbitrary .NET program,
	/// <see cref="Externals.EndRequest"/> will not be called. If this situation occurs, a terminator
	/// 'delegate' is invoked during <see cref="RequestCookie"/>'s finalization (terminator 'delegate'
	/// is not really a <see cref="Delegate"/> but rather the <see cref="IRequestTerminator.Terminate"/>
	/// method, and is set by <c>ExtManager</c> to its method that terminates the request).
	/// </para>
	/// <para>
	/// Nevertheless, GC does not guarantee that <see cref="Object.Finalize"/> is ever called. If this
	/// happens, the request will simply time out. <see cref="RequestCookie"/> is registered as a lifetime
	/// sponsor for the associated <c>Request</c> instance.
	/// </para>
	/// <para>
	/// Last note: if no external function is called during processing a page, there is not overhead
	/// at all. Specifically, no remote calls are made. It is the same for the situation when no 
	/// external function is called from an arbitrary .NET program that references some managed 
	/// wrappers - there is no overhead at all.
	/// </para>
	/// </remarks>
	public static /*sealed*/ class RequestCookie /*: MarshalByRefObject, ILogicalThreadAffinative, ISponsor*/
	{
		/// <summary>
		/// A delegate with <see cref="IExternals.GetInstanceUrl"/> signature. Used for asynchronous calls.
		/// </summary>
        internal delegate string GetInstanceUrlDelegate(string generalUrl, ApplicationConfiguration appConfig, Dictionary<string, ExtensionLibraryDescriptor> extConfig);

		/// <summary>
		/// A delegate with <see cref="IExternals.PhpInfo"/> signature. Used for asynchronous calls.
		/// </summary>
		internal delegate string PhpInfoDelegate();

		/// <summary>
		/// A delegate with <see cref="IExternals.InvokeFunction"/> signature. Used for asynchronous calls.
		/// </summary>
		internal delegate object InvokeFunctionDelegate(string moduleName, string functionName,
			ref object[] args, int[] refInfo, string workingDir);

		/// <summary>
		/// A delegate with <see cref="IExternals.InvokeMethod"/> signature. Used for asynchronous calls.
		/// </summary>
		internal delegate object InvokeMethodDelegate(string moduleName, string className, string methodName,
			ref PhpObject self, ref object[] args, int[] refInfo, string workingDir);

        ///// <summary>
        ///// Creates a new <see cref="RequestCookie"/> and sets the <see cref="extMan"/> field to a proxy
        ///// to <c>ExtManager</c>.
        ///// </summary>
        ///// <remarks>All calls in this request's context should be made through <see cref="extMan"/> and
        ///// not the general proxy (<see cref="Externals.RemoteExtMan"/>).
        ///// </remarks>
        //private RequestCookie()
        //{
            //CallContext.SetData(threadSlotName, this);

            //bool savedSuppressLauncher = LauncherClientChannelSinkProvider.SuppressLauncher;
            //string url = Externals.GetInstanceUrl(this);
            //LauncherClientChannelSinkProvider.SuppressLauncher = savedSuppressLauncher;

            //extMan = (IExternals)RemotingServices.Connect(typeof(IExternals), url);
        //}

        ///// <summary>
        ///// Tries to call the <see cref="Terminator"/>.
        ///// </summary>
        //~RequestCookie()
        //{
        //    try
        //    {
        //        if (Terminator != null)
        //        {
        //            LauncherClientChannelSinkProvider.SuppressLauncher = true;
        //            Terminator.Terminate();
        //        }
        //    }
        //    catch (Exception)
        //    { }
        //}

        ///// <summary>
        ///// Requests a sponsoring client to renew the lease for the associated <c>Request</c> object.
        ///// /// </summary>
        //public TimeSpan Renewal(ILease lease)
        //{
        //    return TimeSpan.FromSeconds(10);
        //}

		public static object ConvertObjectToString(object arg)
		{
            ScriptContext context = ScriptContext.CurrentContext;
            Externals.ParameterTransformation.TransformOutParameter(context, ref arg);
			return Core.Convert.ObjectToString(arg);
		}

		/// <summary>
		/// Invoked from <c>ExtManager</c> in order to call a (user or system) function. See <c>call_user_function</c>,
		/// <c>call_user_function_ex</c>, <c>zend_call_function</c> Zend API.
		/// </summary>
		/// <param name="target">Designation of the function/method to call.</param>
		/// <param name="args">Arguments to be passed to the function.</param>
		/// <returns>Return value of the function.</returns>
		public static object CallFunctionDirect(PhpCallback target, object[] args)
		{
			ScriptContext context = ScriptContext.CurrentContext;

            if (target.TargetInstance != null)
                target.TargetInstance = Externals.ParameterTransformation.PostCallObjectTransform(target.TargetInstance, context) as Reflection.DObject;

			// transform parameters (as if they were 'out' parameters)
			if (args != null)
				for (int i = 0; i < args.Length; i++)
                    Externals.ParameterTransformation.TransformOutParameter(context, ref args[i]);

			object ret_value = target.Invoke(args);

			// transform parameters (as if they were 'in' parameters)
			if (args != null)
				for (int i = 0; i < args.Length; i++)
                    Externals.ParameterTransformation.TransformInParameter(context, ref args[i]);

			// transform the return value (as if it was an 'in' parameter)
            Externals.ParameterTransformation.TransformInParameter(context, ref ret_value);
			
			return ret_value;
		}

        ///// <summary>
        ///// Invoked from <c>ExtManager</c> in order to call a (user or system) function. See <c>call_user_function</c>,
        ///// <c>call_user_function_ex</c>, <c>zend_call_function</c> Zend API.
        ///// </summary>
        ///// <param name="target">Designation of the function/method to call.</param>
        ///// <param name="args">Arguments to be passed to the function.</param>
        ///// <returns>Return value of the function.</returns>
        ///// <remarks>
        ///// <p>
        ///// Obsolete: The reason why self is <c>ref</c> is that we need Remoting to
        ///// serialize and transfer the new state of the instance back to the caller.
        ///// </p>
        ///// <p>
        ///// The state is not transferred at all for user-classes (that are usually used for callbacks).
        ///// </p>
        ///// </remarks>
        //public object CallFunction(PhpCallback target, ref object[] args)
        //{
        //    this.callbackTarget = target;
        //    this.callbackArgs = args;

        //    callbackInvoked.Set();
        //    callbackHandled.WaitOne();

        //    return this.callbackRetValue;
        //}

		/// <summary>
		/// Checks whether the function/method designated by <paramref name="callback"/> is callable.
		/// </summary>
		/// <param name="callback">The callback.</param>
		/// <returns><B>true</B> if the <paramref name="callback"/> is callable, <B>false</B> otherwise.
		/// </returns>
		public static bool CheckCallbackDirect(PhpCallback callback)
		{
			return callback.Bind(true);
		}

        ///// <summary>
        ///// Checks whether the function/method designated by <paramref name="callback"/> is callable.
        ///// </summary>
        ///// <param name="callback">The callback.</param>
        ///// <returns><B>true</B> if the <paramref name="callback"/> is callable, <B>false</B> otherwise.
        ///// </returns>
        ///// <remarks>
        ///// The call is delegated to the static method <see cref="CheckCallbackDirect"/> (Remoting reason).
        ///// </remarks>
        //public bool CheckCallback(PhpCallback callback)
        //{
        //    return CheckCallbackDirect(callback);
        //}

        ///// <summary>
        ///// Associates the calling thread with a new instance of <see cref="RequestCookie"/> if it has not been
        ///// done before.
        ///// </summary>
        ///// <returns>The instance of <see cref="RequestCookie"/> associated with current thread.</returns>
        //internal static RequestCookie EnsureCookieExists()
        //{
        //    RequestCookie cookie = (RequestCookie)CallContext.GetData(threadSlotName);
        //    if (cookie != null)
        //    {
        //        return cookie;
        //    }
        //    else
        //    {
        //        cookie = new RequestCookie();
        //        return cookie;
        //    }
        //}

        ///// <summary>
        ///// Returns true if a <see cref="RequestCookie"/> instance is associated with the calling thread.
        ///// </summary>
        ///// <returns><B>true</B> if a <see cref="RequestCookie"/> instance is associated with the calling
        ///// thread, <B>false</B> otherwise.</returns>
        //internal static bool CookieExists()
        //{
        //    return CallContext.GetData(threadSlotName) != null;
        //}

        ///// <summary>
        ///// Returns the <see cref="RequestCookie"/> that is associated with current thread or <B>null</B>.
        ///// </summary>
        ///// <returns>The <see cref="RequestCookie"/> instance associated with current thread or <B>null</B>.
        ///// </returns>
        //public static RequestCookie GetCurrentThreadCookie()
        //{
        //    return (RequestCookie)CallContext.GetData(threadSlotName);
        //}

        ///// <summary>
        ///// Frees named data slot used to hold the association of calling thread and a
        ///// <see cref="RequestCookie"/> instance.
        ///// </summary>
        //internal static void FreeCookie()
        //{
        //    CallContext.FreeNamedDataSlot(threadSlotName);
        //}

        ///// <summary>
        ///// Gets a delegate to <see cref="extMan"/>'s <see cref="IExternals.InvokeFunction"/> method.
        ///// </summary>
        ///// <returns>The delegate.</returns>
        //internal InvokeFunctionDelegate InvokeFunction
        //{
        //    get
        //    {
        //        return invokeFunctionDelegate ?? (invokeFunctionDelegate = new InvokeFunctionDelegate(extMan.InvokeFunction));
        //    }
        //}

        ///// <summary>
        ///// Gets a delegate to <see cref="extMan"/>'s <see cref="IExternals.InvokeMethod"/> method.
        ///// </summary>
        ///// <returns>The delegate.</returns>
        //internal InvokeMethodDelegate InvokeMethod
        //{
        //    get
        //    {
        //        return invokeMethodDelegate ?? (invokeMethodDelegate = new InvokeMethodDelegate(extMan.InvokeMethod));
        //    }
        //}

        ///// <summary>
        ///// Waits for completion of the asynchronous call associated with <paramref name="asyncResult"/> while
        ///// handling callbacks from ExtManager. PHP exceptions that occured within an external function should be
        ///// rethrown in the same thread that initiated the call in order to be able to walk the call stack
        ///// efficiently.
        ///// </summary>
        ///// <param name="asyncResult">An <see cref="IAsyncResult"/> associated with the remote call.</param>
        //internal void HandleCallbacks(IAsyncResult asyncResult)
        //{
        //    WaitHandle[] wait_handles = new WaitHandle[] { asyncResult.AsyncWaitHandle, exceptionRaised, callbackInvoked };

        //    while (true)
        //    {
        //        switch (WaitHandle.WaitAny(wait_handles))
        //        {
        //            case 1:
        //                {
        //                    // an exception was raised
        //                    try
        //                    {
        //                        PhpException.Throw(error, message);
        //                    }
        //                    finally
        //                    {
        //                        exceptionHandled.Set();
        //                    }
        //                    continue;
        //                }

        //            case 2:
        //                {
        //                    // a callback was invoked
        //                    try
        //                    {
        //                        callbackRetValue = CallFunctionDirect(callbackTarget, callbackArgs);
        //                    }
        //                    finally
        //                    {
        //                        callbackHandled.Set();
        //                    }
        //                    continue;
        //                }
        //        }
        //        break;
        //    }
        //}

        ///// <summary>
        ///// To be called from <c>ExtManager</c> when a PHP exception occurs.
        ///// </summary>
        ///// <param name="error">The error type.</param>
        ///// <param name="message">The error message.</param>
        //public void ExceptionCallback(PhpError error, string message)
        //{
        //    this.error = error;
        //    this.message = message;

        //    exceptionRaised.Set();
        //    exceptionHandled.WaitOne();
        //}

		#region Fields

        ///// <summary>
        ///// A terminator containing the method to be called when this instance of
        ///// <see cref="RequestCookie"/> is finalized.
        ///// </summary>
        //public IRequestTerminator Terminator = null;

        ///// <summary>
        ///// Thread slot name used by <see cref="RequestCookie"/>.
        ///// </summary>
        //private const string threadSlotName = "RequestCookie";

        ///// <summary>
        ///// Delegate to <see cref="extMan"/>'s <see cref="IExternals.InvokeFunction"/>.
        ///// </summary>
        //private InvokeFunctionDelegate invokeFunctionDelegate = null;

        ///// <summary>
        ///// Delegate to <see cref="extMan"/>'s <see cref="IExternals.InvokeMethod"/>.
        ///// </summary>
        //private InvokeMethodDelegate invokeMethodDelegate = null;

        ///// <summary>
        ///// An event that is set when an exception callback occured within an external function.
        ///// <seealso cref="HandleCallbacks"/>
        ///// </summary>
        //private AutoResetEvent exceptionRaised = new AutoResetEvent(false);

        ///// <summary>
        ///// An event that is set when a function callback occured within an external function.
        ///// <seealso cref="HandleCallbacks"/>
        ///// </summary>
        //private AutoResetEvent callbackInvoked = new AutoResetEvent(false);

        ///// <summary>
        ///// An event that is set when an exception callback handling is finished.
        ///// <seealso cref="HandleCallbacks"/>
        ///// </summary>
        //private AutoResetEvent exceptionHandled = new AutoResetEvent(false);

        ///// <summary>
        ///// An event that is set when a function callback handling is finished.
        ///// <seealso cref="HandleCallbacks"/>
        ///// </summary>
        //private AutoResetEvent callbackHandled = new AutoResetEvent(false);

        ///// <summary>
        ///// Type of the error that occured within an external function.
        ///// </summary>
        ///// <remarks>
        ///// Information about the error is passed from the callback thread to the calling thread
        ///// via this field and via <see cref="message"/>.
        ///// </remarks>
        //private volatile PhpError error;

        ///// <summary>
        ///// Error message of the error that occured within an external function.
        ///// </summary>
        ///// <remarks>
        ///// Information about the error is passed from the callback thread to the calling thread
        ///// via this field and via <see cref="error"/>.
        ///// </remarks>
        //private volatile string message;

        ///// <summary>
        ///// The target of a callback invoked from an external function.
        ///// </summary>
        ///// <remarks>
        ///// Information about the callback is passed from the callback thread to the calling thread
        ///// via this field and via <see cref="callbackArgs"/> and in the opposite direction via
        ///// <see cref="callbackRetValue"/>.
        ///// </remarks>
        //private volatile PhpCallback callbackTarget;

        ///// <summary>
        ///// The arguments of a callback invoked from an external function.
        ///// </summary>
        ///// <remarks>
        ///// Information about the callback is passed from the callback thread to the calling thread
        ///// via this field and via <see cref="callbackTarget"/> and in the opposite direction via
        ///// <see cref="callbackRetValue"/>.
        ///// </remarks>
        //private volatile object[] callbackArgs;

        ///// <summary>
        ///// The return value of a callback invoked from an external function.
        ///// </summary>
        ///// <remarks>
        ///// Information about the callback is passed from the callback thread to the calling thread
        ///// via <see cref="callbackTarget"/> and via <see cref="callbackArgs"/> and in the opposite direction
        ///// via this field.
        ///// </remarks>
        //private volatile object callbackRetValue;

        ///// <summary>
        ///// Proxy to the particular instance of <c>ExtManager</c> that is associated with current request.
        ///// </summary>
        //internal readonly IExternals extMan;

		#endregion
	}

	#endregion

    //#region LauncherClientChannelSinkProvider and LauncherClientChannelSink

    ///// <summary>
    ///// Provides a channel sink that launches <c>ExtManager</c> process whenever a remote call 
    ///// is made and <c>ExtManager</c> is not running.
    ///// </summary>
    //internal class LauncherClientChannelSinkProvider : IClientChannelSinkProvider
    //{
    //    /// <summary>
    //    /// If <B>true</B>, <see cref="LauncherClientChannelSink"/> won't try to launch <c>ExtManager</c>.
    //    /// When calling something in a request context, you want the instance that maintains that context,
    //    /// or nothing. Fresh instance of <c>ExtManager</c> would be of no use.
    //    /// </summary>
    //    [ThreadStatic]
    //    public static bool SuppressLauncher;

    //    /// <summary>
    //    /// Next channel sink provider in the chain.
    //    /// </summary>
    //    private IClientChannelSinkProvider next;

    //    /// <summary>
    //    /// Channel sink that launches <c>ExtManager</c> process whenever a remote call is made
    //    /// and <c>ExtManager</c> is not running.
    //    /// </summary>
    //    private class LauncherClientChannelSink : IClientChannelSink
    //    {
    //        #region Fields

    //        /// <summary>
    //        /// Number of milliseconds to wait for the <c>ExtManager</c> to start up.
    //        /// </summary>
    //        private const uint waitTimeout = 15000;

    //        /// <summary>
    //        /// Retry count when trying to launch the ExtManager.
    //        /// </summary>
    //        private const int retryCount = 3;

    //        /// <summary>
    //        /// Next sink in the sink chain.
    //        /// </summary>
    //        private IClientChannelSink nextSink;

    //        #endregion

    //        /// <summary>
    //        /// Creates a new <see cref="LauncherClientChannelSink"/>.
    //        /// </summary>
    //        /// <param name="nextSink">Next sink in the sink chain.</param>
    //        public LauncherClientChannelSink(IClientChannelSink nextSink)
    //        {
    //            this.nextSink = nextSink;
    //        }

    //        #region IChannelSinkBase implementation

    //        /// <summary>
    //        /// Gets a dictionary through which properties on the sink can be accessed.
    //        /// </summary>
    //        /// <remarks>
    //        /// The call is passed-through to the next channel sink.
    //        /// </remarks>
    //        public IDictionary Properties
    //        {
    //            get { return nextSink == null ? null : nextSink.Properties; }
    //        }

    //        #endregion

    //        #region IClientChannelSink implementation

    //        /// <summary>
    //        /// Gets the next client channel sink in the client sink chain.
    //        /// </summary>
    //        public IClientChannelSink NextChannelSink
    //        {
    //            get { return nextSink; }
    //        }

    //        /// <summary>
    //        /// Returns the <see cref="Stream"/> onto which the provided message is to be serialized.
    //        /// </summary>
    //        /// <param name="msg">The <see cref="IMethodCallMessage"/> containing details about the method call.
    //        /// </param>
    //        /// <param name="headers">The headers to add to the outgoing message heading to the server.</param>
    //        /// <returns>The <see cref="Stream"/> onto which the provided message is to be serialized.</returns>
    //        public Stream GetRequestStream(IMessage msg, ITransportHeaders headers)
    //        {
    //            if (nextSink != null)
    //            {
    //                return nextSink.GetRequestStream(msg, headers);
    //            }
    //            else return null;
    //        }

    //        /// <summary>
    //        /// Requests asynchronous processing of a method call on the current sink.
    //        /// </summary>
    //        /// <param name="sinkStack">A stack of channel sinks that called this sink.</param>
    //        /// <param name="msg">The message to process.</param>
    //        /// <param name="headers">The headers to add to the outgoing message heading to the
    //        /// server.</param>
    //        /// <param name="stream">The stream headed to the transport sink.</param>
    //        public void AsyncProcessRequest(IClientChannelSinkStack sinkStack,
    //            IMessage msg, ITransportHeaders headers, Stream stream)
    //        {
    //            // don't try to launch ExtMan when calling in a request's context
    //            if (LauncherClientChannelSinkProvider.SuppressLauncher)
    //            {
    //                try
    //                {
    //                    if (nextSink != null) nextSink.AsyncProcessRequest(sinkStack, msg, headers, stream);
    //                    return;
    //                }
    //                catch (RemotingException ex)
    //                {
    //                    // throw meaningful exceptions instead
    //                    throw new RemotingException(CoreResources.GetString("unable_to_connect_extmanager"), ex);
    //                }
    //                catch (System.Net.Sockets.SocketException ex)
    //                {
    //                    throw new RemotingException(CoreResources.GetString("unable_to_connect_extmanager"), ex);
    //                }
    //            }

    //            int tryCount = retryCount;

    //            if (nextSink != null)
    //            {
    //                while (tryCount-- > 0)
    //                {
    //                    // pass thru
    //                    try
    //                    {
    //                        nextSink.AsyncProcessRequest(sinkStack, msg, headers, stream);
    //                        return;
    //                    }

    //                    catch (RemotingException ex)
    //                    {
    //                        Debug.WriteLine("EXTERNALS", "Exception caught in AsyncProcessMessage: " + ex.Message);
    //                        Debug.WriteLine("EXTERNALS", "Trying to launch ExtManager...");
    //                        LaunchExtManager();

    //                        // reset the request stream!
    //                        stream.Seek(0, SeekOrigin.Begin);
    //                    }
    //                    catch (System.Net.Sockets.SocketException ex)
    //                    {
    //                        throw new RemotingException(CoreResources.GetString("unable_to_connect_extmanager"), ex);
    //                    }
    //                }
    //            }

    //            // unable to launch ExtManager :-(
    //            throw new RemotingTimeoutException(CoreResources.GetString("unable_to_launch_extmanager"));
    //        }

    //        /// <summary>
    //        /// Requests asynchronous processing of a response to a method call on the current sink.
    //        /// </summary>
    //        /// <param name="sinkStack">A stack of sinks that called this sink.</param>
    //        /// <param name="state">Information generated on the request side that is associated with 
    //        /// this sink.</param>
    //        /// <param name="headers">The headers retrieved from the server response stream.</param>
    //        /// <param name="stream">The stream coming back from the transport sink.</param>
    //        public void AsyncProcessResponse(IClientResponseChannelSinkStack sinkStack,
    //            object state, ITransportHeaders headers, Stream stream)
    //        {
    //            // unsupported by the underlying transports sink anyway
    //            if (nextSink != null)
    //            {
    //                nextSink.AsyncProcessResponse(sinkStack, state, headers, stream);
    //            }
    //        }

    //        /// <summary>
    //        /// Requests message processing from the current sink.
    //        /// </summary>
    //        /// <param name="msg">The message to process.</param>
    //        /// <param name="requestHeaders">The headers to add to the outgoing message heading to the
    //        /// server.</param>
    //        /// <param name="requestStream">The stream headed to the transport sink.</param>
    //        /// <param name="responseHeaders">When this method returns, contains an <see cref="ITransportHeaders"/> 
    //        /// interface that holds the headers that the server returned.</param>
    //        /// <param name="responseStream">When this method returns, contains a <see cref="Stream"/> coming back from 
    //        /// the transport sink.</param>
    //        public void ProcessMessage(IMessage msg, ITransportHeaders requestHeaders,
    //            Stream requestStream, out ITransportHeaders responseHeaders, out Stream responseStream)
    //        {
    //            // don't try to launch ExtMan when calling in a request's context
    //            if (LauncherClientChannelSinkProvider.SuppressLauncher)
    //            {
    //                try
    //                {
    //                    if (nextSink != null) nextSink.ProcessMessage(msg, requestHeaders, requestStream,
    //                                              out responseHeaders, out responseStream);
    //                    else
    //                    {
    //                        responseHeaders = null;
    //                        responseStream = null;
    //                    }
    //                    return;
    //                }
    //                catch (RemotingException ex)
    //                {
    //                    // throw meaningful exceptions instead
    //                    throw new RemotingException(CoreResources.GetString("unable_to_connect_extmanager"), ex);
    //                }
    //                catch (System.Net.Sockets.SocketException ex)
    //                {
    //                    throw new RemotingException(CoreResources.GetString("unable_to_connect_extmanager"), ex);
    //                }
    //            }

    //            int tryCount = retryCount;

    //            if (nextSink != null)
    //            {
    //                while (tryCount-- > 0)
    //                {
    //                    // pass thru
    //                    try
    //                    {
    //                        nextSink.ProcessMessage(msg, requestHeaders, requestStream,
    //                            out responseHeaders, out responseStream);
    //                        return;
    //                    }

    //                    catch (RemotingException ex)
    //                    {
    //                        Debug.WriteLine("EXTERNALS", "Exception caught in ProcessMessage: " + ex.Message);
    //                        Debug.WriteLine("EXTERNALS", "Trying to launch ExtManager...");
    //                        LaunchExtManager();

    //                        // reset the request stream!
    //                        requestStream.Seek(0, SeekOrigin.Begin);
    //                    }
    //                    catch (System.Net.Sockets.SocketException ex)
    //                    {
    //                        throw new RemotingException(CoreResources.GetString("unable_to_connect_extmanager"), ex);
    //                    }
    //                }
    //            }

    //            // unable to launch ExtManager :-(
    //            throw new RemotingTimeoutException(CoreResources.GetString("unable_to_launch_extmanager"));
    //        }

    //        #endregion

    //        /// <summary>
    //        /// Launches the <c>ExtManager</c> process and waits until it starts listening.
    //        /// </summary>
    //        /// <returns><B>true</B> if the process was successfully started, <B>false</B> otherwise.</returns>
    //        private bool LaunchExtManager()
    //        {
    //            string eventName = "Global\\PHPNET_ExtManager_launch_event";
    //            IntPtr eventHandle = IntPtr.Zero;

    //            try
    //            {
    //                eventHandle = ShmNative.CreateEvent(ShmNative.securityAttributes, true, false, eventName);
    //                if (eventHandle == IntPtr.Zero)
    //                {
    //                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(),
    //                        CoreResources.GetString("could_not_create_event"));
    //                }

    //                if (Marshal.GetLastWin32Error() != ShmNative.ERROR_ALREADY_EXISTS)
    //                {
    //                    try
    //                    {
    //                        // we created this event -> launch the process
    //                        Process proc = Process.Start(Path.Combine(Configuration.Application.Paths.ExtManager,
    //                            Externals.ExtManagerExe), eventName);
    //                    }
    //                    catch (ArgumentException ex)
    //                    {
    //                        Debug.WriteLine("EXTERNALS", ex.Message);
    //                        return false;
    //                    }
    //                }

    //                // wait until ExtManager starts listening
    //                uint waitResult = ShmNative.WaitForSingleObject(eventHandle, waitTimeout);

    //                if (waitResult != ShmNative.WAIT_OBJECT_0)
    //                {
    //                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(),
    //                        CoreResources.GetString("timeout_waiting_for_extmanager"));
    //                }
    //            }
    //            catch (System.ComponentModel.Win32Exception ex)
    //            {
    //                Debug.WriteLine("EXTERNALS", ex.Message);
    //                return false;
    //            }
    //            finally
    //            {
    //                if (eventHandle != IntPtr.Zero) ShmNative.CloseHandle(eventHandle);
    //            }

    //            return true;
    //        }
    //    }

    //    #region IClientChannelSinkProvider implementation

    //    /// <summary>
    //    /// This method is called by the channel to create a sink chain.
    //    /// </summary>
    //    /// <param name="channel">Channel for which the current sink chain is being constructed.</param>
    //    /// <param name="url">The URL of the object to connect to.</param>
    //    /// <param name="remoteChannelData">A channel data object describing a channel on the remote server.</param>
    //    /// <returns>The first sink of the newly formed channel sink chain.</returns>
    //    public IClientChannelSink CreateSink(IChannelSender channel, string url, object remoteChannelData)
    //    {
    //        IClientChannelSink nextSink = null;
    //        if (Next != null) nextSink = Next.CreateSink(channel, url, remoteChannelData);

    //        return new LauncherClientChannelSink(nextSink);
    //    }

    //    /// <summary>
    //    /// Gets or sets the next sink provider in the channel sink provider chain.
    //    /// </summary>
    //    public IClientChannelSinkProvider Next
    //    {
    //        get { return next; }
    //        set { next = value; }
    //    }

    //    #endregion
    //}

    //#endregion

    #region ObjectWrapper

    /// <summary>
    /// ObjectWrapper prevents serializers to obtain type of wrapped object. By obtaining type of object
    /// defined in missing assembly (or assembly running on another platform, e.g. x64 vs x86),
    /// the de-serialization will throw an exception. Wrapped object must be MarshalByRef.
    /// </summary>
    public sealed class ObjectWrapper : MarshalByRefObject
    {
        private object obj;

        public ObjectWrapper(MarshalByRefObject obj)
        {
            this.obj = obj;
        }

        public object Object { get { return this.obj; } }
    }

    #endregion

    /// <summary>
	/// Provides static methods for extension management and external function and method invocation.
	/// </summary>
	[DebuggerNonUserCode]
	public static class Externals
	{
		#region Fields

        ///// <summary>
        ///// Proxy to the <c>ExtManager</c> that hosts &quot;isolated&quot; extensions.
        ///// </summary>
        //private static IExternals RemoteExtMan
        //{
        //    get
        //    {
        //        if (!extMansInitialized) InitializeExtMans();
        //        return _remoteExtMan;
        //    }
        //}
        //private static IExternals _remoteExtMan = null;

        /// <summary>
		/// Get the <c>ExtManager</c> that hosts &quot;collocated&quot; extensions.
		/// </summary>
        /// <param name="extversion">The version of ExtManager to use.</param>
		private static IExternals LocalExtMan(ExtensionLibraryDescriptor.ExtSupportVersion extversion)
		{
			if (!extMansInitialized)
                InitializeExtMans();

            if (_localExtMan == null)
                return null;

            Debug.Assert((int)extversion >= 0 && (int)extversion < _localExtMan.Length, "Argument 'extversion' out of bounds.");

			return _localExtMan[(int)extversion];
		}

        /// <summary>
        /// Get enumerator of all the <c>ExtManager</c>s that hosts &quot;collocated&quot; extensions.
        /// </summary>
        /// <remarks>All the values are not null.</remarks>
        private static IEnumerable<KeyValuePair<ExtensionLibraryDescriptor.ExtSupportVersion, IExternals>> LocalExtMans
        {
            get
            {
                for (int i = 0; i < (int)ExtensionLibraryDescriptor.ExtSupportVersion.Count; ++i)
                {
                    IExternals extman = LocalExtMan((ExtensionLibraryDescriptor.ExtSupportVersion)i);
                    if (extman != null)
                        yield return new KeyValuePair<ExtensionLibraryDescriptor.ExtSupportVersion, IExternals>((ExtensionLibraryDescriptor.ExtSupportVersion)i, extman);
                }
            }
        }

		private static IExternals[] _localExtMan = null;

		/// <summary>
		/// Whether Extension Managers has been initialized.
		/// </summary>
		private static bool extMansInitialized = false;

        ///// <summary>
        ///// Remoting channel.
        ///// </summary>
        //private static IChannel channel;

        ///// <summary>
        ///// The URL used to connect to ExtManager's well-known section.
        ///// </summary>
        //private const string generalUrl = "shm://phalanger/ExtManager";

        ///// <summary>
        ///// Name of the <c>ExtManager</c> executable.
        ///// </summary>
        //public const string ExtManagerExe = "ExtManager.exe";

		/// <summary>
		/// Suffix of wrapper assembly names.
		/// </summary>
		public const string WrapperAssemblySuffix = ".mng";

		/// <summary>
		/// Name of the &quot;extension&quot; with built-in stream wrappers (http, ftp).
		/// </summary>
		public const string BuiltInStreamWrappersExtensionName = "#stream_wrappers";

		/// <summary>
		/// Prevents more threads calling <see cref="InitCollocation"/> simultaneously.
		/// <seealso cref="GenerateManagedWrapper"/>
		/// </summary>
		private static object initCollocationMutex = new Object();

		/// <summary>
		/// Name of the field that carries object's identity.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Whenever an instance of a user-defined class is about to be passed to <c>ExtManager</c>,
		/// an instance of <see cref="stdClass"/> with a field of this name is passed instead.
		/// The field value contains a unique identification of the original managed instance in form
		/// of a GUID.
		/// </para>
		/// <para>
		/// Whenever an instance of <see cref="stdClass"/> is about to be passed from <c>ExtManager</c>,
		/// it is checked whether it contains this field and if so, this <see cref="stdClass"/> is replaced
		/// with the corresponding managed object.
		/// </para>
		/// </remarks>
		private const string ObjectIdentityFieldName = "__ObjectIdentity";

		/// <summary>
		/// The table that maps object identities (GUIDS) to corresponding object instances.
		/// </summary>
		/// <remarks>
		/// Note that there might be more &quot;identities&quot; of one managed instance - more
		/// entries whose values are identical.
		/// </remarks>
		[ThreadStatic]
		internal static Dictionary<string, DObject> objectIdentities;

		/// <summary>
		/// Cache of <see cref="IExternalStreamWrapper"/>s keyed by scheme.
		/// </summary>
		/// <remarks>
		/// Purged in <see cref="EndRequest"/> (quite aggressive, proxies to wrappers in collocated extensions
		/// could stay here forever).
		/// </remarks>
		[ThreadStatic]
		private static Dictionary<string, IExternalStreamWrapper> streamWrapperCache;

		/// <summary>
		/// List of bound external variables separate for every module.
		/// </summary>
		[ThreadStatic]
		private static Dictionary<string, Dictionary<PhpReference, IExternalVariable>> boundVariables;

		#endregion

		#region Construction and initialization

		/// <summary>
		/// Excludes mutliple threads from initialization procedure.
		/// </summary>
		private static object initializationMutex = new object();

		/// <summary>
		/// Initializes Remote and Local Extension Managers.
		/// </summary>
		private static void InitializeExtMans()
		{
			lock (initializationMutex)
			{
                // make sure another thread which was waiting does not initialize again
                if (extMansInitialized) return;

				// make sure that configuration is loaded
				Configuration.Load(ApplicationContext.Default);

                //if (ExtensionLibraryDescriptor.isolatedExtensions.Count > 0)
                //{
                //    if (!Configuration.Application.Paths.ExtManager.DirectoryExists)
                //        throw new ConfigurationErrorsException(CoreResources.GetString("extmanager_path_not_configured"));

                //    InitSharedMemoryChannel();
                //    // extensions are loaded lazily when GetInstanceUrl is invoked		
                //}

				if (ExtensionLibraryDescriptor.collocatedExtensions.Count > 0)
				{
					InitCollocation();

					// load all the extensions that are configured
					foreach (var pair in ExtensionLibraryDescriptor.collocatedExtensions)
					{
                        _localExtMan[(int)pair.Value.ExtVersion].LoadExtension(pair.Value);
					}
				}

				extMansInitialized = true;
			}
		}

        ///// <summary>
        ///// Initializes Remoting infrastructure for &quot;isolated&quot; extensions.
        ///// </summary>
        ///// <returns>Remote Extension Manager.</returns>
        ///// <remarks>
        ///// Although we're calling here <see cref="RemotingServices.Connect"/>, there is
        ///// no communication taking place, so far. That's why it is correct to perform these
        ///// operations in static constructor though the <c>ExtManager</c> might not be running, yet.
        ///// </remarks>
        //private static void InitSharedMemoryChannel()
        //{
        //    // Binary formatter -> Launcher sink -> Transport channel
        //    BinaryClientFormatterSinkProvider client_formatter_provider = new BinaryClientFormatterSinkProvider();
        //    client_formatter_provider.Next = new LauncherClientChannelSinkProvider();

        //    // set full deserialization level for the server formatter (new to Framework 1.1)
        //    BinaryServerFormatterSinkProvider server_formatter_provider = new BinaryServerFormatterSinkProvider();
        //    server_formatter_provider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;

        //    // create an shm channel and register it
        //    channel = new ShmChannel(null, client_formatter_provider, server_formatter_provider);
        //    ChannelServices.RegisterChannel(channel, false);

        //    // get a proxy to ExtManager object from across the shm channel
        //    _remoteExtMan = (IExternals)RemotingServices.Connect(typeof(IExternals), generalUrl);
        //}

		/// <summary>
		/// Initializes the collocated ExtSupport.
		/// </summary>
		/// <returns>Local Extension Manager.</returns>
		private static void InitCollocation()
        {
            _localExtMan = new IExternals[(int)ExtensionLibraryDescriptor.ExtSupportVersion.Count];

            // load php4ts/php5ts
            for (int i = 0; i < _localExtMan.Length; ++i)
            {
                Assembly ass;

                try
                {
                    switch ((ExtensionLibraryDescriptor.ExtSupportVersion)i)
                    {
                        case ExtensionLibraryDescriptor.ExtSupportVersion.php4ts:
                            ass = Assembly.Load("php4ts, Version=3.0.0.0, Culture=neutral, PublicKeyToken=43b6773fb05dc4f0");
                            break;
                        case ExtensionLibraryDescriptor.ExtSupportVersion.php5ts:
                            ass = Assembly.Load("php5ts, Version=3.0.0.0, Culture=neutral, PublicKeyToken=43b6773fb05dc4f0");
                            break;
                        default:
                            throw new NotImplementedException("Not implemented extension support " + (ExtensionLibraryDescriptor.ExtSupportVersion)i);
                    }
                }
                catch (FileNotFoundException)
                {
                    if (Environment.Is64BitProcess)
                        throw new FileLoadException("Native extension support is not available in 64-bit processes.\n" +
                        ((System.Web.HttpContext.Current != null) ?
                        "Please 'Enable 32-bit Applications' in 'Advanced settings' of your IIS 'Application Pool'." :
                        "Please run this process on 32-bit OS or mark it as X86 using 'corflags.exe'."));
                    else
                        throw;
                }

                Debug.Assert(ass != null);

                // obtain reference to RemoteDispatcher (this is not a proxy!)
                _localExtMan[i] = (IExternals)ass
                    .GetType("PHP.ExtManager.StartupHelper")
                        .InvokeMember(
                            "Collocate",
                            BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, null);
            }
			
			
		}

		#endregion

		#region Request delimitation and lifetime

		/// <summary>
		/// Does nothing.
		/// </summary>
		/// <remarks>
		/// Everything is lazy (especially me).
		/// </remarks>
		public static void BeginRequest()
		{
		}

		/// <summary>
		/// Terminates the request currently associated with calling <see cref="Thread"/>.
		/// <seealso cref="IRequestTerminator"/>
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="EndRequest"]/*'/>
		public static void EndRequest()
		{
			// purge bound variable table (no need to call Unbind - native zvals will be released by EndRequest)
			boundVariables = null;

			// purge stream wrapper cache
			streamWrapperCache = null;

			// purge object identity table
			objectIdentities = null;

			// notify the collocated ExtManager
            if (_localExtMan != null)   // only if extension managers was started... do not initialize the ext managers just to end the request which was not started
                foreach (var extman in LocalExtMans)
                {
                    try
                    {
                        extman.Value.EndRequest();
                    }
                    catch (Exception)
                    { }
                }

            //// notify the isolated ExtManager
            //if (_remoteExtMan != null)
            //{
            //    RequestCookie cookie = RequestCookie.GetCurrentThreadCookie();
            //    if (cookie != null)
            //    {
            //        try
            //        {
            //            LauncherClientChannelSinkProvider.SuppressLauncher = true;
            //            cookie.extMan.EndRequest();
            //        }
            //        catch (Exception)
            //        { }
            //        RequestCookie.FreeCookie();
            //    }
            //}
		}

        ///// <summary>
        ///// Returns the private URL of the current remote <c>ExtManager</c>.
        ///// </summary>
        ///// <returns>The instance (private) URL for the remote <c>ExtManager</c>.</returns>
        //internal static string GetInstanceUrl(RequestCookie cookie)
        //{
        //    LauncherClientChannelSinkProvider.SuppressLauncher = false;

        //    RequestCookie.GetInstanceUrlDelegate remote_delegate =
        //        new RequestCookie.GetInstanceUrlDelegate(RemoteExtMan.GetInstanceUrl);

        //    // invoke the corresponding proxy method asynchronously
        //    IAsyncResult asyncResult = remote_delegate.BeginInvoke(generalUrl, Configuration.Application,
        //        ExtensionLibraryDescriptor.isolatedExtensions, null, null);

        //    string ret_val;
        //    try
        //    {
        //        // wait for completion of the remote call and handle exception callbacks
        //        cookie.HandleCallbacks(asyncResult);
        //    }
        //    finally
        //    {
        //        ret_val = remote_delegate.EndInvoke(asyncResult);
        //    }
        //    return ret_val;
        //}

		#endregion

		#region External function/method invocation

        #region deprecated invocations (backward compatibility)

        /// <summary>
        /// Invokes an external function.
        /// </summary>
        /// <include file='Doc/Externals.xml' path='doc/method[@name="InvokeFunction"]/*'/>
        [Emitted]
        public static object InvokeFunction(string moduleName, string functionName, ref object[] args, int[] refInfo)
        {
            // // //return InvokeExternalFunction(GetFunctionProxy(moduleName, null, functionName), null, ScriptContext.CurrentContext, args, refInfo);
            
            ScriptContext context = ScriptContext.CurrentContext;
            object ret_value;   // the function call return value
            ExtensionLibraryDescriptor descriptor;  // the extension descriptor
            IExternals extman;  // used IExternals

            // extract parameters that should be bound
            PhpReference[] param_binding = PrepareParametersForBinding(args);

            // transform parameters
            ParameterTransformation.TransformInParameters(null, context, args);

            // is the extension collocated?
            if (ExtensionLibraryDescriptor.collocatedExtensions.TryGetValue(moduleName, out descriptor))
            {
                object[] args_copy = args;  // avoid modifying caller's args array if there are no byref parameters
                ret_value = (extman = LocalExtMan(descriptor.ExtVersion)).
                    InvokeFunction(moduleName, functionName, ref args_copy, refInfo, context.WorkingDirectory);
            }
            //else if (RemoteExtMan != null)
            //{
            //    // note: it is not possible to place the call to EnsureCookieExists to the launcher sink's ProcessMessage,
            //    // because the reference to RequestCookie must be in the CallContext before the request stream is created
            //    // by the binary formatter which happens before launcher sink's ProcessMessage is eventually called
            //    LauncherClientChannelSinkProvider.SuppressLauncher = true;

            //    // one instance of InvokeFunctionDelegate pointing to the right InvokeFunction method is cached in RequestCookie
            //    RequestCookie cookie = RequestCookie.EnsureCookieExists();

            //    extman = cookie.extMan;     // used IExternals
            //    object[] args_copy = args;  // avoid modifying caller's args array if there are no byref parameters

            //    // invoke the corresponding proxy method asynchronously
            //    IAsyncResult async_result;
            //    async_result = cookie.InvokeFunction.BeginInvoke(
            //        moduleName, functionName, ref args_copy, refInfo, context.WorkingDirectory, null, null);

            //    try
            //    {
            //        // wait for completion of the remote call and handle exception callbacks
            //        cookie.HandleCallbacks(async_result);
            //    }
            //    finally
            //    {
            //        ret_value = cookie.InvokeFunction.EndInvoke(ref args_copy, async_result);
            //    }
            //}
            else
            {
                PhpException.Throw(PhpError.Error, CoreResources.GetString("undefined_external_module_called", functionName, moduleName));
                return null;
            }

            // transform parameters
            ParameterTransformation.TransformOutParameters(null, context, args);
            ParameterTransformation.TransformOutParameter(context, ref ret_value);

            // bind parameters
            if (param_binding != null)
                BindParameters(moduleName, args, refInfo, extman, param_binding);

            // 
            return ret_value;
        }

        /// <summary>
        /// Invokes an external method.
        /// </summary>
        /// <include file='Doc/Externals.xml' path='doc/method[@name="InvokeMethod"]/*'/>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        [Emitted]
        public static object InvokeMethod(string moduleName, string className, string methodName, PhpObject self,
            ScriptContext context, ref object[] args, int[] refInfo)
        {
            // // //return InvokeExternalFunction(GetFunctionProxy(moduleName, className, methodName), self, context, args, refInfo);

            object ret_value;   // the function call return value
            IExternals extman;  // used IExternals
            ExtensionLibraryDescriptor descriptor;  // the extension descriptor
            PhpObject new_self; // new self object

            // extract parameters that should be bound
            PhpReference[] param_binding = PrepareParametersForBinding(args);

            // transform parameters
            ParameterTransformation.TransformInParameters(self, context, args);

            // is the extension collocated?
            if (ExtensionLibraryDescriptor.collocatedExtensions.TryGetValue(moduleName, out descriptor))
            {
                object[] args_copy = args;  // avoid modifying caller's args array if there are no byref parameters
                new_self = self;

                ret_value = (extman = LocalExtMan(descriptor.ExtVersion)).
                    InvokeMethod(moduleName, className, methodName, ref new_self, ref args_copy, refInfo, context.WorkingDirectory);

            }
            //else if (RemoteExtMan != null)
            //{
            //    // note: it is not possible to place the call to EnsureCookieExists to the launcher sink's ProcessMessage,
            //    // because the reference to RequestCookie  must be in the CallContext before the request stream is created
            //    // by the binary formatter which happens before launcher sink's ProcessMessage is eventually called
            //    LauncherClientChannelSinkProvider.SuppressLauncher = true;

            //    // one instance of InvokeMethodDelegate pointing to the right InvokeMethod method is cached in RequestCookie
            //    RequestCookie cookie = RequestCookie.EnsureCookieExists();

            //    extman = cookie.extMan;
            //    object[] args_copy = args;  // avoid modifying caller's args array if there are no byref parameters

            //    // invoke the corresponding proxy method asynchronously
            //    IAsyncResult async_result;
            //    async_result = cookie.InvokeMethod.BeginInvoke(
            //        moduleName, className, methodName, ref self, ref args_copy, refInfo, context.WorkingDirectory, null, null);

            //    new_self = null;
            //    try
            //    {
            //        // wait for completion of the remote call and handle exception callbacks
            //        cookie.HandleCallbacks(async_result);
            //    }
            //    finally
            //    {
            //        // fetch the return value and the new instance state
            //        ret_value = cookie.InvokeMethod.EndInvoke(ref new_self, ref args_copy, async_result);
            //    }
            //}
            else
            {
                PhpException.Throw(PhpError.Error, CoreResources.GetString("undefined_external_module_called", String.Format("{0}::{1}", className, methodName), moduleName));
                return null;
            }

            // transform parameters
            ParameterTransformation.TransformOutParameters(new_self, context, args);
            ParameterTransformation.TransformOutParameter(context, ref ret_value);

            // incarnate the new state into the original instance:
            // incarnation already done by ExtManager! // if (self != null) self.Incarnate(new_self); // TODO: OR NOT ? as it is in collocated method invoke // compare with PhpMarshaler::IncarnateNativeToManaged

            // bind parameters
            if (param_binding != null)
                BindParameters(moduleName, args, refInfo, extman, param_binding);

            //
            return ret_value;
        }

        /// <summary>
        /// Invokes an external function with parameters on <see cref="ScriptContext.Stack"/>.
        /// </summary>
        /// <param name="moduleName">The name of the extension where the function is defined (without trailing '.DLL').</param>
        /// <param name="functionName">The name of the function.</param>
        /// <param name="stack">The <see cref="PhpStack"/> that parameters have been pushed to.</param>
        /// <param name="refInfo">Indexes of parameters that should be passed by reference, and optionally terminated
        /// with <c>-1</c> which means that from the last marked parameter on, everything should be passed by reference.</param>
        /// <returns>The return value of the function.</returns>
        /// <remarks>
        /// This method is used in situations when arguments are pushed on the calling stack - in &quot;arg-less&quot;
        /// overloads of static functions in wrappers.
        /// <seealso cref="PhpStack"/>
        /// </remarks>
        [Emitted]
        public static object InvokeFunctionDynamic(string moduleName, string functionName, PhpStack stack, int[] refInfo)
        {
            int arg_count = stack.ArgCount;
            object[] args = new object[arg_count];
            PhpReference[] references = new PhpReference[arg_count];

            // convert arguments on calling stack into the args array
            stack.CalleeName = functionName;
            int ref_ptr = (refInfo != null && refInfo.Length > 0 ? 0 : -1);
            for (int i = 0; i < arg_count; i++)
            {
                if (ref_ptr >= 0)
                {
                    if (i == refInfo[ref_ptr])
                    {
                        args[i] = references[i] = stack.PeekReferenceUnchecked(i + 1);
                        if (++ref_ptr >= refInfo.Length) ref_ptr = -1;
                        continue;
                    }
                    else if (refInfo[ref_ptr] == -1)
                    {
                        args[i] = references[i] = stack.PeekReferenceUnchecked(i + 1);
                        continue;
                    }
                }
                args[i] = stack.PeekValueUnchecked(i + 1);
            }
            stack.RemoveFrame();

            if (refInfo == null || refInfo.Length == 0)
            {
                return InvokeFunction(moduleName, functionName, ref args, null);
            }
            else
            {
                object ret_val = InvokeFunction(moduleName, functionName, ref args, refInfo);

                // propagate back byref parameters
                for (int i = 0; i < arg_count; i++)
                {
                    if (references[i] != null) references[i].Value = args[i];
                }

                return ret_val;
            }
        }

        /// <summary>
        /// Invokes an external method with parameters on <see cref="ScriptContext.Stack"/>.
        /// </summary>
        /// <param name="moduleName">The name of the extension where the method is defined (without trailing '.DLL').</param>
        /// <param name="className">The name of the declaring class.</param>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="self">The instance on which the method is to be invoked.</param>
        /// <param name="stack">The <see cref="PhpStack"/> where arguments have been pushed.</param>
        /// <param name="refInfo">Indexes of parameters that should be passed by reference, and optionally terminated
        /// with <c>-1</c> which means that from the last marked parameter on, everything should be passed by reference.</param>
        /// <returns>The return value of the method.</returns>
        /// <remarks>
        /// This method is used in situations when arguments are pushed on the calling stack - in &quot;arg-less&quot;
        /// overloads of methods in wrapping classes.
        /// <seealso cref="PhpStack"/>
        /// </remarks>
        [Emitted]
        public static object InvokeMethodDynamic(string moduleName, string className, string methodName, PhpObject self,
            PhpStack stack, int[] refInfo)
        {
            int arg_count = stack.ArgCount;
            object[] args = new object[arg_count];
            PhpReference[] references = new PhpReference[arg_count];

            // convert arguments on calling stack into the args array
            stack.CalleeName = methodName;
            int ref_ptr = (refInfo != null && refInfo.Length > 0 ? 0 : -1);
            for (int i = 0; i < arg_count; i++)
            {
                if (ref_ptr >= 0)
                {
                    if (i == refInfo[ref_ptr])
                    {
                        args[i] = references[i] = stack.PeekReferenceUnchecked(i + 1);
                        if (++ref_ptr >= refInfo.Length) ref_ptr = -1;
                        continue;
                    }
                    else if (refInfo[ref_ptr] == -1)
                    {
                        args[i] = references[i] = stack.PeekReferenceUnchecked(i + 1);
                        continue;
                    }
                }
                args[i] = stack.PeekValueUnchecked(i + 1);
            }
            stack.RemoveFrame();

            if (refInfo == null || refInfo.Length == 0)
            {
                return InvokeMethod(moduleName, className, methodName, self, stack.Context, ref args, null);
            }
            else
            {
                object ret_val = InvokeMethod(moduleName, className, methodName, self, stack.Context, ref args, refInfo);

                // propagate back byref parameters
                for (int i = 0; i < arg_count; i++)
                {
                    if (references[i] != null) references[i].Value = args[i];
                }

                return ret_val;
            }
        }
        
        #endregion

        #region Invalid function wrapper

        /// <summary>
        /// IExternalFunction implementation, that just throws PHP error.
        /// </summary>
        private class InvalidExternalFunction: MarshalByRefObject, IExternalFunction
        {
            private string message;

            internal InvalidExternalFunction(string message)
            {
                this.message = message;
            }

            #region IExternalFunction Members

            public object Invoke(PhpObject self, ref object[] args, int[] refInfo, string workingDir)
            {
                PhpException.Throw(PhpError.Error, message);

                return null;
            }

            public IExternals ExtManager
            {
                get { return null; }
            }

            #endregion
        }

        #endregion

        #region Collocated function wrapper

        private class CollocatedExternalFunction : MarshalByRefObject, IExternalFunction
        {
            private readonly IExternalFunction externalFunction;
            private readonly IExternals extManager;

            public CollocatedExternalFunction(IExternalFunction/*!*/externalFunction, IExternals extManager)
            {
                Debug.Assert(externalFunction != null);

                this.externalFunction = externalFunction;
                this.extManager = extManager;
            }

            #region IExternalFunction Members

            public object Invoke(PhpObject self, ref object[] args, int[] refInfo, string workingDir)
            {
                Debug.Assert(Externals.extMansInitialized, "Externals.extMansInitialized is false!");
                Debug.Assert(Externals._localExtMan != null, "Externals._localExtMan[] not even initialized!");

                object[] args_copy = args;  // avoid of changing args
                return externalFunction.Invoke(self, ref args_copy, refInfo, workingDir);
            }

            public IExternals ExtManager
            {
                get { return extManager; }
            }

            #endregion
        }

        #endregion

        //#region Isolated function/method wrapper

        //private class IsolatedExternalFunction : MarshalByRefObject, IExternalFunction
        //{
        //    string moduleName, functionName;

        //    public IsolatedExternalFunction(string moduleName, string functionName)
        //    {
        //        this.moduleName = moduleName;
        //        this.functionName = functionName;
        //    }

        //    #region IExternalFunction Members

        //    public object Invoke(PhpObject self, ref object[] args, int[] refInfo, string workingDir)
        //    {
        //        // note: it is not possible to place the call to EnsureCookieExists to the launcher sink's ProcessMessage,
        //        // because the reference to RequestCookie  must be in the CallContext before the request stream is created
        //        // by the binary formatter which happens before launcher sink's ProcessMessage is eventually called
        //        LauncherClientChannelSinkProvider.SuppressLauncher = true;

        //        // one instance of InvokeMethodDelegate pointing to the right InvokeMethod method is cached in RequestCookie
        //        RequestCookie cookie = RequestCookie.EnsureCookieExists();

        //        // invoke the corresponding proxy method asynchronously
        //        object[] args_copy = args;  // keep args unchanged, Invoke
        //        IAsyncResult async_result;
        //        async_result = cookie.InvokeFunction.BeginInvoke(
        //            moduleName, functionName, ref args_copy, refInfo, workingDir, null, null);

        //        object ret;
        //        try
        //        {
        //            // wait for completion of the remote call and handle exception callbacks
        //            cookie.HandleCallbacks(async_result);
        //        }
        //        finally
        //        {
        //            // fetch the return value and the new instance state
        //            ret = cookie.InvokeFunction.EndInvoke(ref args_copy, async_result);
        //        }

        //        return ret;
        //    }

        //    public IExternals ExtManager
        //    {
        //        get
        //        {
        //            RequestCookie cookie = RequestCookie.EnsureCookieExists();
        //            return cookie.extMan;
        //        }
        //    }

        //    #endregion
        //}

        //private class IsolatedExternalMethod : MarshalByRefObject, IExternalFunction
        //{
        //    string moduleName, className, functionName;

        //    public IsolatedExternalMethod(string moduleName, string className, string functionName)
        //    {
        //        this.moduleName = moduleName;
        //        this.className = className;
        //        this.functionName = functionName;
        //    }

        //    #region IExternalFunction Members

        //    public object Invoke(PhpObject self, ref object[] args, int[] refInfo, string workingDir)
        //    {
        //        // note: it is not possible to place the call to EnsureCookieExists to the launcher sink's ProcessMessage,
        //        // because the reference to RequestCookie  must be in the CallContext before the request stream is created
        //        // by the binary formatter which happens before launcher sink's ProcessMessage is eventually called
        //        LauncherClientChannelSinkProvider.SuppressLauncher = true;

        //        // one instance of InvokeMethodDelegate pointing to the right InvokeMethod method is cached in RequestCookie
        //        RequestCookie cookie = RequestCookie.EnsureCookieExists();

        //        // invoke the corresponding proxy method asynchronously
        //        object[] args_copy = args;  // keep args unchanged, Invoke
        //        IAsyncResult async_result;
        //        async_result = cookie.InvokeMethod.BeginInvoke(
        //            moduleName, className, functionName, ref self, ref args_copy, refInfo, workingDir, null, null);

        //        object ret;
        //        try
        //        {
        //            // wait for completion of the remote call and handle exception callbacks
        //            cookie.HandleCallbacks(async_result);
        //        }
        //        finally
        //        {
        //            // fetch the return value and the new instance state
        //            ret = cookie.InvokeMethod.EndInvoke(ref self, ref args_copy, async_result);
        //        }

        //        return ret;
        //    }

        //    public IExternals ExtManager
        //    {
        //        get
        //        {
        //            RequestCookie cookie = RequestCookie.EnsureCookieExists();
        //            return cookie.extMan;
        //        }
        //    }

        //    #endregion
        //}

        //#endregion

        /// <summary>
        /// Get proper external function proxy, that calls the function/method directly.
        /// </summary>
        /// <param name="moduleName">The module name.</param>
        /// <param name="className">Class name, can be null in case of global function.</param>
        /// <param name="functionName">Function/Method name.</param>
        /// <returns>IExternalFunction. Cannot return null. In case of missing module/class/function special InvalidExternalFunction class throwing PHP error is returned.</returns>
        public static IExternalFunction GetFunctionProxy(string moduleName, string className, string functionName)
        {
            ExtensionLibraryDescriptor descriptor;  // the extension descriptor
             
            // is the extension collocated?
            if (ExtensionLibraryDescriptor.collocatedExtensions.TryGetValue(moduleName, out descriptor))
            {
                IExternals extMan = LocalExtMan(descriptor.ExtVersion);
                return new CollocatedExternalFunction(extMan.GetFunctionProxy(moduleName, className, functionName), extMan);
            }
            //else if (RemoteExtMan != null)
            //{
            //    // isolated function proxy, invoking asynchronously
            //    return (className == null) ?
            //        (IExternalFunction)new IsolatedExternalFunction(moduleName, functionName) :
            //        (IExternalFunction)new IsolatedExternalMethod(moduleName, className, functionName);
            //}
            else
            {
                // module was not found
                return new InvalidExternalFunction(CoreResources.GetString("undefined_external_module_called", (className == null) ? functionName : String.Format("{0}::{1}", className, functionName), moduleName));
            }
        }

        // emitted directly by Extension Wrapper Generator
        //public static object InvokeExternalFunction(IExternalFunction/*!*/externalfunc, PhpObject self, ScriptContext context, object[] args, int[] refInfo)
        //{
        //    Debug.Assert(externalfunc != null);

        //    object ret_value;   // the function call return value

        //    // extract parameters that should be bound
        //    PhpReference[] param_binding = PrepareParametersForBinding(args);   // needed only if PhpReference was passed (typeof args[i] = PhpReference|Object)

        //    // transform parameters // needed only for DObject only (typeof args[i] = PhpReference|DObject|object|PhpArray)
        //    ParameterTransformation.TransformInParameters(self, context, args);

        //    // invoke the function
        //    // note: args reference will not be changed
        //    ret_value = externalfunc.Invoke(self, ref args, refInfo, context.WorkingDirectory);

        //    // transform parameters (opposite of TransformInParameters)
        //    ParameterTransformation.TransformOutParameters(self, context, args);
        //    ParameterTransformation.TransformOutParameter(context, ref ret_value)

        //    // incarnate the new state into the original instance:
        //    // incarnation already done by ExtManager! // if (self != null) self.Incarnate(new_self); // TODO: OR NOT ? as it is in collocated method invoke // compare with PhpMarshaler::IncarnateNativeToManaged

        //    // bind parameters
        //    if (param_binding != null)
        //        BindParameters("!!!MODULE NAME!!!", args, refInfo, externalfunc.ExtManager, /*!*/param_binding);

        //    //
        //    return ret_value;
        //}

		#endregion

		#region External variable binding

		/// <summary>
		/// Transfers external bound variables to or from <c>ExtManager</c>.
		/// </summary>
		/// <param name="moduleName">The name of the extension whose bound variables should be transfered.</param>
		/// <param name="nativeToManaged">If <B>true</B> variables should be transfered from <c>ExtManager</c>,
		/// if <B>false</B> variables should be transfered to <c>ExtManager</c>.</param>
		[Emitted]
		public static void MarshalBoundVariables(string moduleName, bool nativeToManaged)
		{
			Dictionary<PhpReference, IExternalVariable> table;
			if (boundVariables != null && boundVariables.TryGetValue(moduleName, out table))
			{
				if (nativeToManaged)
				{
					// overwrite managed by native
					foreach (KeyValuePair<PhpReference, IExternalVariable> pair in table)
					{
						pair.Key.Value = pair.Value.GetValue();
					}
				}
				else
				{
					// overwrite native by managed
					foreach (KeyValuePair<PhpReference, IExternalVariable> pair in table)
					{
						pair.Value.SetValue(pair.Key.Value);
					}
				}
			}
		}

		/// <summary>
		/// Extracts the parameters that should be bound and dereferences <see cref="PhpReference"/>s in <paramref name="args"/>.
		/// </summary>
		/// <param name="args">The parameter array as passed to <see cref="InvokeFunction"/> or <see cref="InvokeMethod"/>.</param>
		/// <returns>Array of the same length as <paramref name="args"/> where non-<B>null</B> means that the parameter
		/// should be bound, or <B>null</B> if no parameters should be bound.</returns>
		public static PhpReference[] PrepareParametersForBinding(object[] args)
		{
			PhpReference[] result = null;
			if (args != null)
			{
				for (int i = 0; i < args.Length; i++)
				{
					PhpReference reference = args[i] as PhpReference;
					if (reference != null)
					{
						// store the reference in result (lazy array init)
						if (result == null) result = new PhpReference[args.Length];
						result[i] = reference;

						// dereference the argument
						args[i] = reference.Value;
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Binds parameters after an external function or method invocation.
		/// </summary>
		/// <param name="moduleName">Name of the calling module - variables are kept separate for every module</param>
		/// <param name="args">The parameter array as passed to <see cref="InvokeFunction"/> or <see cref="InvokeMethod"/>.</param>
		/// <param name="refInfo">The <c>refInfo</c> array as passed to <see cref="InvokeFunction"/> or <see cref="InvokeMethod"/>.
		/// </param>
		/// <param name="extMan">The <c>ExtManager</c> through which the function/method was invoked.</param>
		/// <param name="paramsToBind">Cannot be null. Result of <see cref="PrepareParametersForBinding"/> called prior to the function/method invocation itself.</param>
		public static void BindParameters(string moduleName, object[] args, int[] refInfo, IExternals extMan, PhpReference[]/*!*/paramsToBind)
		{
            Debug.Assert(paramsToBind != null);

		    PhpReference reference;
            Dictionary<PhpReference, IExternalVariable> moduleVars = null;
			bool checkUnbind = (boundVariables != null && boundVariables.TryGetValue(moduleName, out moduleVars));

			for (int i = 0; i < paramsToBind.Length; ++i)
			{
				if ((reference = paramsToBind[i]) != null)
				{
					// release the old binding of this variable (if it exists)
					IExternalVariable ext_var;
					if (checkUnbind)
					{
						Debug.Assert(moduleVars != null);
						if (moduleVars.TryGetValue(reference, out ext_var)) ext_var.Unbind();
					}
					else
					{
						// construct everything as needed
						if (moduleVars == null)
						{
							if (boundVariables == null)
								boundVariables = new Dictionary<string, Dictionary<PhpReference, IExternalVariable>>();
							moduleVars = new Dictionary<PhpReference, IExternalVariable>();
							boundVariables.Add(moduleName, moduleVars);
						}
					}

					// bind
                    ext_var = (extMan != null) ? extMan.BindParameter(i) : null;
					if (ext_var != null) moduleVars[reference] = ext_var;

					// update the variable if it was passed byref
					if (refInfo != null)
					{
						int index, j = 0;
						while (j < refInfo.Length && (index = refInfo[j]) <= i)
						{
							if (index == i || index == -1)
							{
								reference.Value = args[i];
								break;
							}
							++j;
						}
					}
				}
			}
		}

		/// <summary>
		/// Adds one <see cref="PhpReference"/> indirection to a parameter (or parameters) on <see cref="PhpStack"/>.
		/// </summary>
		/// <param name="stack">The stack.</param>
		/// <param name="paramIndex">The index of the parameter if &gt;=0 or the index of the first parameter - 1 if
		/// &lt;0 (in which case all parameters starting with this one are marked).</param>
		/// <remarks>
		/// <para>
		/// Calls to this methods are emitted just before the call to <see cref="InvokeFunctionDynamic"/> or
		/// <see cref="InvokeMethodDynamic"/> in arg-less overloads.
		/// </para>
		/// <para>
		/// Parameter that is marked by the 'bind' attribute in typedef XML should be stored on stack as a
		/// <see cref="PhpReference"/> pointing to another <see cref="PhpReference"/> pointing to the parameter value.
		/// First <see cref="PhpReference"/> will be stripped by <see cref="InvokeFunctionDynamic"/> /
		/// <see cref="InvokeMethodDynamic"/> and will ensure that this parameter is recognized as byref. The second
		/// <see cref="PhpReference"/> will be registered in <see cref="boundVariables"/> by <see cref="InvokeFunction"/> /
		/// <see cref="InvokeMethod"/> and will be stripped before forwarding the call to <c>ExtManager</c>.
		/// </para>
		/// </remarks>
		[Emitted]
		public static void MarkParameterForBinding(PhpStack stack, int paramIndex)
		{
			if (paramIndex < 0)
			{
				paramIndex = -paramIndex - 1;
				while (paramIndex < stack.ArgCount)
				{
					paramIndex++;
					stack.AddIndirection(paramIndex);
				}
			}
			else if (paramIndex < stack.ArgCount)
			{
				stack.AddIndirection(paramIndex + 1);
			}
		}

		#endregion

		#region Parameter transformation

        /// <summary>
        /// Transforming of parameters/self/return value before and after invoking external function.
        /// </summary>
        public static class ParameterTransformation
        {
            /// <summary>
            /// A delegate to <see cref="PreCallObjectTransform"/> static method.
            /// </summary>
            private static PhpWalkCallback InTransformerCallback = new PhpWalkCallback(PreCallObjectTransform);

            /// <summary>
            /// A delegate to <see cref="PostCallObjectTransform"/> static method.
            /// </summary>
            private static PhpWalkCallback OutTransformerCallback = new PhpWalkCallback(PostCallObjectTransform);

            /// <summary>
            /// Walks the object graph using specified callback function.
            /// </summary>
            /// <param name="context"></param>
            /// <param name="callback"></param>
            /// <param name="node">Object graph, can be null.</param>
            public static void TransformParameterGraph(ScriptContext context, PhpWalkCallback callback, IPhpObjectGraphNode node)
            {
                if (node != null) node.Walk(callback, context);
            }

            /// <summary>
            /// Performs in-place transformation of given object using PreCallObjectTransform callback.
            /// </summary>
            /// <param name="context"></param>
            /// <param name="obj"></param>
            /// <returns></returns>
            public static void TransformInParameter(ScriptContext context, ref object obj)
            {
                IPhpObjectGraphNode node;
                if ((node = obj as IPhpObjectGraphNode) != null)
                {
                    obj = PreCallObjectTransform(node, context);
                    TransformParameterGraph(context, InTransformerCallback, obj as IPhpObjectGraphNode);
                }
            }

            /// <summary>
            /// Performs in-place transformation of given object using PostCallObjectTransform callback.
            /// </summary>
            /// <param name="context"></param>
            /// <param name="obj"></param>
            public static void TransformOutParameter(ScriptContext context, ref object obj)
            {
                IPhpObjectGraphNode node;
                if ((node = obj as IPhpObjectGraphNode) != null)
                {
                    obj = PostCallObjectTransform(node, context);
                    TransformParameterGraph(context, OutTransformerCallback, obj as IPhpObjectGraphNode);
                }
            }

            /// <summary>
            /// Invokes <see cref="PreCallObjectTransform"/> on each object implementing the <see cref="IPhpObjectGraphNode"/>
            /// in object graphs rooted in <paramref name="self"/> and in <paramref name="args"/>.
            /// </summary>
            /// <param name="self">The &quot;this&quot; instance (object root).</param>
            /// <param name="context">Current <see cref="ScriptContext"/>.</param>
            /// <param name="args">The parameters (array of object roots).</param>
            public static void TransformInParameters(PhpObject self, ScriptContext context, object[] args)
            {
                TransformParameterGraph(context, InTransformerCallback, self);
                
                if (args != null)
                    for (int i = 0; i < args.Length; ++i)
                        TransformInParameter(context, ref args[i]);
            }

            /// <summary>
            /// Invokes <see cref="PostCallObjectTransform"/> on each object implementing the
            /// <see cref="IPhpObjectGraphNode"/> in object graphs rooted in <paramref name="self"/>
            /// and in <paramref name="args"/>.
            /// </summary>
            /// <param name="self">The &quot;this&quot; instance (object root).</param>
            /// <param name="context">Current <see cref="ScriptContext"/>.</param>
            /// <param name="args">The parameters (array of object roots).</param>
            public static void TransformOutParameters(PhpObject self, ScriptContext context, object[] args)
            {
                TransformParameterGraph(context, OutTransformerCallback, self);

                if (args != null)
                    for (int i = 0; i < args.Length; ++i)
                        TransformOutParameter(context, ref args[i]);
            }

            /// <summary>
            /// Converts instances of user-defined PHP classes to <see cref="stdClass"/>es. Also converts all
            /// strings to the <see cref="System.String"/> representation.
            /// </summary>
            /// <param name="node"></param>
            /// <param name="context">Current <see cref="ScriptContext"/>.</param>
            /// <returns></returns>
            internal static object PreCallObjectTransform(IPhpObjectGraphNode node, ScriptContext context)
            {
                DObject obj = node as DObject;
                if (obj != null && !obj.GetType().FullName.StartsWith(Namespaces.Library))
                {
                    // simulate "marshal-by-ref" semantics for user-defined classes
                    stdClass std = new stdClass(context);

                    string id = Guid.NewGuid().ToString();
                    std.SetProperty(ObjectIdentityFieldName, id, null);

                    (objectIdentities ?? (objectIdentities = new Dictionary<string, DObject>()))[id] = obj;

                    return std;
                }

                if (node is PhpString)
                    return ((IPhpConvertible)node).ToString();
                
                return node;
            }

            /// <summary>
            /// Converts <see cref="stdClass"/>es with identity field present into their corresponding user-defined
            /// class instances.
            /// </summary>
            /// <param name="node"></param>
            /// <param name="context">Current <see cref="ScriptContext"/>.</param>
            /// <returns></returns>
            internal static object PostCallObjectTransform(IPhpObjectGraphNode node, ScriptContext context)
            {
                stdClass std = node as stdClass;
                if (std != null && objectIdentities != null)
                {
                    string id = PhpVariable.AsString(std.GetProperty(ObjectIdentityFieldName, null));
                    if (id != null)
                    {
                        DObject obj;
                        if (objectIdentities.TryGetValue(id, out obj)) return obj;
                    }
                }
                return node;
            }
        }

		#endregion

		#region Stream wrapper retrieval

		/// <summary>
		/// Returns a proxy of a stream wrapper that lives in <c>ExtManager</c>.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="GetStreamWrapper"]/*'/>
		public static IExternalStreamWrapper GetStreamWrapper(string scheme)
		{
			IExternalStreamWrapper wrapper = null;

			// try cache first
			if (streamWrapperCache != null && streamWrapperCache.TryGetValue(scheme, out wrapper))
				return wrapper;
			
            // try isolated extension
            foreach (var extman in LocalExtMans)
                if ((wrapper = extman.Value.GetStreamWrapper(scheme)) != null)
                    break;

            //// try collocated extension
            //if (wrapper == null && RemoteExtMan != null)
            //{
            //    LauncherClientChannelSinkProvider.SuppressLauncher = true;
            //    wrapper = RequestCookie.EnsureCookieExists().extMan.GetStreamWrapper(scheme);
            //}

            // put into the cache
			if (wrapper != null)
			{
				// add to cache (cache is purged on request end)
				if (streamWrapperCache == null) streamWrapperCache = new Dictionary<string, IExternalStreamWrapper>();
				streamWrapperCache[scheme] = wrapper;
			}

            //
			return wrapper;
		}

		/// <summary>
		/// Returns an <see cref="ICollection"/> of schemes of all available external stream wrappers.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="GetStreamWrapperSchemes"]/*'/>
		public static ICollection GetStreamWrapperSchemes()
		{
            ArrayList schemes = new ArrayList();

            //if (RemoteExtMan != null)
            //{
            //    LauncherClientChannelSinkProvider.SuppressLauncher = true;
            //    ICollection remote_schemes = RequestCookie.EnsureCookieExists().extMan.GetStreamWrapperSchemes();
            //    if (remote_schemes != null) schemes.AddRange(remote_schemes);
            //}

            foreach (var extman in LocalExtMans)
            {
                ICollection local_schemes = extman.Value.GetStreamWrapperSchemes();
                if (local_schemes != null) schemes.AddRange(local_schemes);
            }
			
            return schemes;
		}

		#endregion

		#region Information retrieval and wrapper generation

		/// <summary>
		/// Returns an <see cref="ICollection"/> of error messages.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="GetStartupErrors"]/*'/>
		public static ICollection GetStartupErrors()
		{
            //ICollection remote_errors = null;
			ICollection local_errors = null;

            ArrayList errors = new ArrayList();

            //// collect errors from remote ext manager
            //if (RemoteExtMan != null)
            //{
            //    LauncherClientChannelSinkProvider.SuppressLauncher = true;
            //    remote_errors = RequestCookie.EnsureCookieExists().extMan.GetStartupErrors();
            //    if (remote_errors!=null)
            //        errors.AddRange(remote_errors);	
            //}

            // collect errors from local ext managers
            foreach(var extman in LocalExtMans)
            {
                if ((local_errors = extman.Value.GetStartupErrors()) != null)
                    errors.AddRange(local_errors);
            }

            //
            return errors;
		}

		/// <summary>
		/// Gathers information about loaded extensions.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="PhpInfo"]/*'/>
		public static string PhpInfo()
		{
            StringBuilder allinfo = new StringBuilder();

			// get info from collocated extensions
            foreach (var extman in LocalExtMans)
                allinfo.Append(extman.Value.PhpInfo());
			
            //if (RemoteExtMan != null)
            //{
            //    LauncherClientChannelSinkProvider.SuppressLauncher = true;

            //    RequestCookie cookie = RequestCookie.EnsureCookieExists();
            //    RequestCookie.PhpInfoDelegate remoteDelegate =
            //        new PHP.Core.RequestCookie.PhpInfoDelegate(cookie.extMan.PhpInfo);

            //    // invoke the corresponding proxy method asynchronously
            //    IAsyncResult asyncResult = remoteDelegate.BeginInvoke(null, null);

            //    string ret_val;
            //    try
            //    {
            //        // wait for completion of the remote call and handle exception callbacks
            //        cookie.HandleCallbacks(asyncResult);
            //    }
            //    finally
            //    {
            //        ret_val = remoteDelegate.EndInvoke(asyncResult);
            //    }

            //    allinfo.Append(ret_val);
            //}
			
            //
            return allinfo.ToString();
		}

		/// <summary>
		/// Returns an <see cref="ICollection"/> of names of extensions that are currently loaded.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="GetModules"]/*'/>
		public static ICollection GetModules(bool internalNames)
		{
			ArrayList modules = new ArrayList();

            //// isolated modules
            //if (RemoteExtMan != null)
            //{
            //    ICollection remote_modules = null;
                
            //    LauncherClientChannelSinkProvider.SuppressLauncher = true;
            //    remote_modules = RequestCookie.EnsureCookieExists().extMan.GetModules(internalNames);

            //    if (remote_modules != null)
            //        modules.AddRange(remote_modules);
            //}

            // collated modules
            foreach (var extman in LocalExtMans)
            {
                ICollection local_modules = extman.Value.GetModules(internalNames);
                if (local_modules != null)
                    modules.AddRange(local_modules);
            }

            //
			return modules;
		}

		/// <summary>
		/// Checks whether a given extension is currently loaded.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="GetModuleVersion"]/*'/>
		public static string GetModuleVersion(string moduleName, bool internalName, out bool loaded)
		{
			// is the extension collocated?
			if ((!internalName && ExtensionLibraryDescriptor.collocatedExtensions.ContainsKey(moduleName)) ||
				(internalName && ExtensionLibraryDescriptor.collocatedExtensionsByName.ContainsKey(moduleName)))
			{
                foreach (var extman in LocalExtMans)
                {
                    string version = extman.Value.GetModuleVersion(moduleName, internalName, out loaded);

                    if (loaded)
                        return version;
                }
			}

            //if (RemoteExtMan != null)
            //{
            //    LauncherClientChannelSinkProvider.SuppressLauncher = true;
            //    return RequestCookie.EnsureCookieExists().extMan.GetModuleVersion(moduleName, internalName, out loaded);
            //}

			loaded = false;
			return null;
		}

		/// <summary>
		/// Returns an <see cref="ICollection"/> of names of functions in a given extension.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="GetFunctionsByModule"]/*'/>
		public static ICollection GetFunctionsByModule(string moduleName, bool internalName)
		{
			// is the extension collocated?
			if ((!internalName && ExtensionLibraryDescriptor.collocatedExtensions.ContainsKey(moduleName)) ||
				(internalName && ExtensionLibraryDescriptor.collocatedExtensionsByName.ContainsKey(moduleName)))
			{
                foreach (var extman in LocalExtMans)
                {
                    var functions = extman.Value.GetFunctionsByModule(moduleName, internalName);
                    if (functions != null) return functions;
                }
			}

            //if (RemoteExtMan != null)
            //{
            //    LauncherClientChannelSinkProvider.SuppressLauncher = true;
            //    return RequestCookie.EnsureCookieExists().extMan.GetFunctionsByModule(moduleName, internalName);
            //}

			return null;
		}

		/// <summary>
		/// Returns an <see cref="ICollection"/> of names of classes in a given extension.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="GetClassesByModule"]/*'/>
		public static ICollection GetClassesByModule(string moduleName, bool internalName)
		{
			// is the extension collocated?
			if ((!internalName && ExtensionLibraryDescriptor.collocatedExtensions.ContainsKey(moduleName)) ||
				(internalName && ExtensionLibraryDescriptor.collocatedExtensionsByName.ContainsKey(moduleName)))
			{
                foreach (var extman in LocalExtMans)
                {
                    var classes = extman.Value.GetClassesByModule(moduleName, internalName);
                    if (classes != null) return classes;
                }
			}

            //if (RemoteExtMan != null)
            //{
            //    LauncherClientChannelSinkProvider.SuppressLauncher = true;
            //    return RequestCookie.EnsureCookieExists().extMan.GetClassesByModule(moduleName, internalName);
            //}

			return null;
		}

		/// <summary>
		/// Generates the managed wrapper for a given extension.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="GenerateManagedWrapper"]/*'/>
		public static string GenerateManagedWrapper(string moduleName)
		{
            //// is the extension isolated?
            //if (ExtensionLibraryDescriptor.isolatedExtensions.ContainsKey(moduleName) && RemoteExtMan != null)
            //{
            //    LauncherClientChannelSinkProvider.SuppressLauncher = true;
            //    return RequestCookie.EnsureCookieExists().extMan.GenerateManagedWrapper(moduleName);
            //}
            //else
			{
				// let the local ExtManager generate the wrapper
				lock (initCollocationMutex)
				{
					if (_localExtMan == null)
                        InitCollocation();
				}

                IExternals extman = LocalExtMan(    // TODO: read extension version from module info.
                    moduleName.EndsWith(".php5") ?
                    ExtensionLibraryDescriptor.ExtSupportVersion.php5ts :
                    ExtensionLibraryDescriptor.ExtSupportVersion.php4ts
                    );

                if (extman != null)
                    return extman.GenerateManagedWrapper(moduleName);
			}

			PhpException.Throw(PhpError.Error, CoreResources.GetString("extensions_not_installed"));
			return null;
		}

		#endregion

		#region INI entry management

		/// <summary>
		/// Sets an INI value that might have an effect on an extension.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="IniSet"]/*'/>
		public static bool IniSet(string varName, string newValue, out string oldValue)
		{
            foreach (var extman in LocalExtMans)
                if (extman.Value.IniSet(varName, newValue, out oldValue))
                    return true;
            
            //if (RemoteExtMan != null)
            //{
            //    LauncherClientChannelSinkProvider.SuppressLauncher = true;
            //    return RequestCookie.EnsureCookieExists().extMan.IniSet(varName, newValue, out oldValue);
            //}

			oldValue = null;
			return false;
		}

		/// <summary>
		/// Gets an INI value related to extensions.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="IniGet"]/*'/>
		public static bool IniGet(string varName, out string value)
		{
            foreach (var extman in LocalExtMans)
                if (extman.Value.IniGet(varName, out value))
                    return true;
			
            //if (RemoteExtMan != null)
            //{
            //    LauncherClientChannelSinkProvider.SuppressLauncher = true;
            //    return RequestCookie.EnsureCookieExists().extMan.IniGet(varName, out value);
            //}

			value = null;
			return false;
		}

		/// <summary>
		/// Restores an INI value related to extensions.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="IniRestore"]/*'/>
		public static bool IniRestore(string varName)
		{
            foreach (var extman in LocalExtMans)
                if (extman.Value.IniRestore(varName))
                    return true;
			
            //if (RemoteExtMan != null)
            //{
            //    LauncherClientChannelSinkProvider.SuppressLauncher = true;
            //    return RequestCookie.EnsureCookieExists().extMan.IniRestore(varName);
            //}

			return false;
		}

		/// <summary>
		/// Gets all INI entry names and values.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="IniGetAll"]/*'/>
		public static PhpArray IniGetAll(string extension)
		{
            if (extension != null)
            {
                PhpArray result = null;

                // is the extension collocated?
                if (ExtensionLibraryDescriptor.collocatedExtensionsByName.ContainsKey(extension))
                {
                    foreach (var extman in LocalExtMans)
                    {
                        result = extman.Value.IniGetAll(extension);
                        if (result != null) break;
                    }
                }
                //else if (RemoteExtMan != null)
                //{
                //    LauncherClientChannelSinkProvider.SuppressLauncher = true;
                //    result = RequestCookie.EnsureCookieExists().extMan.IniGetAll(extension);
                //}

                if (result == null)
                    PhpException.Throw(PhpError.Warning, CoreResources.GetString("unable_to_find_extension", extension));

                return result;
            }
            else
            {
                // options for all extensions are requested
                PhpArray entries = new PhpArray();

                //if (RemoteExtMan != null)
                //{
                //    LauncherClientChannelSinkProvider.SuppressLauncher = true;
                //    PhpArray remote_entries = RequestCookie.EnsureCookieExists().extMan.IniGetAll(extension);
                //    if (remote_entries != null) foreach (var x in remote_entries) entries.Add(x);
                //}

                foreach (var extman in LocalExtMans)
                {
                    PhpArray local_entries = extman.Value.IniGetAll(extension);
                    if (local_entries != null) foreach (var x in local_entries) entries.Add(x);
                }

                return entries;
            }
		}

		/// <summary>
		/// Determines whether a given extension registered a given INI entry name.
		/// </summary>
		/// <include file='Doc/Externals.xml' path='doc/method[@name="IniOptionExists"]/*'/>
		public static bool IniOptionExists(string moduleName, string varName)
		{
            //// is the extension isolated?
            //if (RemoteExtMan != null && ExtensionLibraryDescriptor.isolatedExtensions.ContainsKey(moduleName))
            //{
            //    LauncherClientChannelSinkProvider.SuppressLauncher = true;
            //    return RequestCookie.EnsureCookieExists().extMan.IniOptionExists(moduleName, varName);
            //}
            //else
			{
				// let the local ExtManager generate the wrapper
                foreach (var extman in LocalExtMans)
                    if (extman.Value.IniOptionExists(moduleName, varName))
                        return true;
			}

            return false;
		}

		#endregion

		#region Utilities

		/// <summary>
		/// Returns full path to the managed wrapper of a given native extension.
		/// </summary>
		/// <param name="nativeFileName">File name of the native extension.</param>
		/// <returns>Full path to the managed wrapper.</returns>
		public static string GetWrapperPath(string nativeFileName)
		{
			return GetWrapperPath(nativeFileName, Configuration.Application.Paths.ExtWrappers);
		}

		internal static string GetWrapperPath(string nativeFileName, string extWrappersPath)
		{
			return Path.Combine(extWrappersPath, Path.ChangeExtension(nativeFileName, WrapperAssemblySuffix + ".dll"));
		}

		#endregion
	}
}
