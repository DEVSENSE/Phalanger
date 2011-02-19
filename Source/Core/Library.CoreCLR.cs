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
		/// <param name="configAttributes">
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
		}

		#endregion
	}
}