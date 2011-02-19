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
	public abstract partial class PhpLibraryDescriptor
	{
		#region Methods to be implemented by subclasses



		/// <summary>
		/// Called when library loading is finished and descriptor is initialized.
		/// </summary>
		/// <param name="assemblyAttribute">
		/// A metadata attribute defined on library assembly or a <B>null</B> reference.
		/// </param>
		/// <param name="configStore">
		/// A collection of XML attributes used in configuration file to add the assembly to the Class Library
		/// or a <B>null</B> reference.
		/// </param>
		/// <remarks>
		/// Library is load when configuration reader finds out a node defining the library.
		/// After library is loaded the reader continues with configuration reading and calls <see cref="ParseConfig"/>
		/// when it reaches the section belonging to the library.
		/// </remarks>
		internal protected virtual void Loaded(PhpLibraryAttribute assemblyAttribute, LibraryConfigStore configStore)
		{
			Debug.WriteLine("CONFIG", "Library loaded: idx={0}, assembly={1}", UniqueIndex, RealAssembly.FullName);

            this.assemblyAttribute = assemblyAttribute;
		}

		
		/// <summary>
		/// Parses a configuration section belonging to the library. 
		/// </summary>
		/// <param name="userContext">
		/// User specified configuration context. Contains a <B>null</B> reference if the method is called for the first time
		/// or an instance of the user configuration type partially filled with configuration values which has
		/// been already processed.
		/// </param>
		/// <param name="context">The Core configuration context.</param>
		/// <param name="section">XML node containing the configuration or its part.</param>
		/// <returns>
		/// The library configuration context which is is passed to the next iteration of the method if any.
		/// </returns>
		/// <remarks>
		/// The method is called for each configuration file and each XML node containing configuration of the library
		/// as they are processed by .NET configuration loader. Note that the method may not be called at all. 
		/// </remarks>
		internal protected abstract ConfigContextBase ParseConfig(ConfigContextBase userContext,
			PhpConfigurationContext context, XmlNode section);

		/// <summary>
		/// Creates empty library configuration context.
		/// </summary>
		/// <returns>
		/// An initialized configuration context. Should not be a <B>null</B> reference.
		/// Creates an empty context for libraries that doesn't use configuration.
		/// </returns>
		internal protected virtual ConfigContextBase CreateConfigContext()
		{
			// GENERICS: not needed, since factories can be written via generics
			return new ConfigContextBase(null, null);
		}

		/// <summary>
		/// Validates configuration after it has been completely read.
		/// </summary>
		/// <param name="userContext">The configuration context.</param>
		/// <exception cref="ConfigurationErrorsException">Configuration is invalid.</exception>
		internal protected virtual void Validate(ConfigContextBase userContext)
		{
		}

		#endregion
	}

	internal sealed partial class DefaultLibraryDescriptor : PhpLibraryDescriptor
	{
		internal protected override ConfigContextBase ParseConfig(ConfigContextBase userContext, PhpConfigurationContext context, System.Xml.XmlNode section)
		{
			return null;
		}

		internal protected override ConfigContextBase CreateConfigContext()
		{
			return new ConfigContextBase(null, null);
		}
	}

}