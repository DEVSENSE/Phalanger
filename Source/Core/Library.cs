/*

 Copyright (c) 2005-2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Xml;
using System.Diagnostics;
using System.Reflection;
using System.Configuration;
using System.Collections;
using System.IO;
using System.Threading;

using PHP.Core.Emit;
using PHP.Core.Reflection;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core
{
	/// <summary>
	/// Base class for Phalanger library descriptors.
	/// </summary>
	[Serializable]
	public abstract partial class PhpLibraryDescriptor
	{
		#region Properties

		public Assembly RealAssembly { get { return module.Assembly.RealAssembly; } }

		/// <summary>
		/// Module representing the library. 
		/// Written up when the library is loaded.
		/// </summary>
		public DModule Module { get { return module; } }
		[NonSerialized] // TODO? MarshalByRef?
		private DModule module;

		/// <summary>
		/// Unique index assigned to the library. 
		/// Written up when the library is loaded.
		/// </summary>
		internal int UniqueIndex { get { return uniqueIndex; } }
		private int uniqueIndex;

		/// <summary>
		/// Name of the section in the configuration file or a <B>null</B> reference if no section is used or 
		/// the section name has not been set yet (e.g. during <see cref="Loaded"/> callback).
		/// Written up when the confgiuration is loaded.
		/// </summary>
		public string ConfigurationSectionName { get { return configurationSectionName; } }
		private string configurationSectionName;

        ///// <summary>
        ///// Returns a list of names of extensions which are implemented by the library.
        ///// </summary>
        ///// <returns>An array of names.</returns>
        ///// <remarks>The first item (if any) is considered to be default extension for the library.</remarks>
        //public /*virtual*/ string[] ImplementedExtensions
        //{
        //    get
        //    {
        //        Debug.Assert(assemblyAttribute != null);

        //        return assemblyAttribute.ImplementsExtensions;
        //    }
        //}

        protected PhpLibraryAttribute assemblyAttribute;

        ///// <summary>
        ///// Returns a name of default extension which is implemented by the library.
        ///// </summary>
        ///// <remarks>The first item (if any) is considered to be default extension for the library.</remarks>
        //public string DefaultExtension
        //{
        //    get { 

        //        string[] extensions = this.ImplementedExtensions;

        //        if (extensions.Length > 0)
        //            return extensions[0];
        //        else
        //            return null;
        //    }
        //}

		#endregion

		#region Construction

		/// <summary>
		/// Subclasses should have a parameter-less constructor.
		/// </summary>
		protected PhpLibraryDescriptor()
		{
			this.module = null;
			this.uniqueIndex = -1;
			this.configurationSectionName = null;
		}

		internal void WriteUp(DModule/*!*/ module, int uniqueIndex)
		{
			Debug.Assert(this.module == null, "Already written up");
			Debug.Assert(module != null);

			this.module = module;
			this.uniqueIndex = uniqueIndex;
			this.configurationSectionName = null; // to be written up by configuration
		}

		internal void WriteConfigurationUp(string sectionName)
		{
			// TODO (TP): Consider whther this is correct behavior?
			//       This occures under stress test, because ASP.NET calls 
			//       ConfigurationSectionHandler.Create even though we already loaded assemblies
			// Debug.Assert(this.configurationSectionName == null, "Already written up");

			Debug.Assert(sectionName != null);

			this.configurationSectionName = sectionName;
		}

		internal void Invalidate()
		{
			this.configurationSectionName = null;
			this.module = null;
			this.uniqueIndex = -1;
		}

		#endregion

		#region Factory

		/// <summary>
		/// Creates a new instance of descriptor given its type.
		/// </summary>
		/// <param name="type">The type of the descriptor to create.</param>
		/// <returns>The new instance.</returns>
		/// <exception cref="LibraryLoadFailedException"><paramref name="type"/> is not valid descriptor type.</exception>
		internal static PhpLibraryDescriptor CreateInstance(Type/*!*/ type)
		{
			PhpLibraryDescriptor result;
			try
			{
				result = (PhpLibraryDescriptor)Activator.CreateInstance(type);
			}
			catch (Exception e)
			{
				throw new LibraryLoadFailedException(type.Assembly.FullName, e);
			}

			return result;
		}

		#endregion

		#region Debug

		[Conditional("DEBUG")]
		public void Dump(TextWriter output)
		{
			output.WriteLine("{0}: assembly = {1}", UniqueIndex, RealAssembly.FullName);
			output.WriteLine("   section = {0}, config = {1}", configurationSectionName,
				Configuration.Local.GetLibraryConfig(this));
		}

		#endregion
	}

	internal sealed partial class DefaultLibraryDescriptor : PhpLibraryDescriptor
	{
	}

	internal sealed class LibraryLoadFailedException : ApplicationException
	{
		public LibraryLoadFailedException(string assemblyName, string message)
			: base(CoreResources.GetString("library_load_failed", assemblyName, message))
		{
		}

		public LibraryLoadFailedException(string assemblyName, Exception/*!*/ inner)
			: base(CoreResources.GetString("library_load_failed", assemblyName, inner.Message))
		{
		}
	}
}
