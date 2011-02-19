/*

 Copyright (c) 2006 Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Xml;
using System.Xml.Schema;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using PHP.Core;

namespace PHP.Library.Xml
{
	/// <summary>
	/// DOM implementation.
	/// </summary>
	[ImplementsType]
	public partial class DOMImplementation
	{
		#region Fields and Properties

		protected internal XmlImplementation XmlImplementation;

		#endregion

		#region Construction

		public DOMImplementation()
			: base(ScriptContext.CurrentContext, true)
		{
			XmlImplementation = new XmlImplementation();
		}

		#endregion

		#region Operations

		/// <summary>
		/// Not implemented in PHP 5.1.6.
		/// </summary>
		[PhpVisible]
		public static void getFeature([This] DOMImplementation instance, string feature, string version)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		/// <summary>
		/// Tests if this DOM implementation implements a specific feature.
		/// </summary>
		/// <param name="instance">The <see cref="DOMImplementation"/> instance or <B>null</B>.</param>
		/// <param name="feature">The feature.</param>
		/// <param name="version">The feature version.</param>
		/// <returns><B>True</B> or <B>false</B>.</returns>
		[PhpVisible]
		public static object hasFeature([This] DOMImplementation instance, string feature, string version)
		{
			XmlImplementation impl = (instance != null ? instance.XmlImplementation : new XmlImplementation());
			return impl.HasFeature(feature, version);
		}

		/// <summary>
		/// Creates a new <see cref="DOMDocumentType"/>.
		/// </summary>
		/// <param name="instance">The <see cref="DOMImplementation"/> instance or <B>null</B>.</param>
		/// <param name="qualifiedName">Name of the document type.</param>
		/// <param name="publicId">The public identifier of the document type.</param>
		/// <param name="systemId">The system identifier of the document type.</param>
		/// <returns>The <see cref="DOMDocumentType"/>.</returns>
		[PhpVisible]
		public static object createDocumentType([This] DOMImplementation instance, string qualifiedName,
			string publicId, string systemId)
		{
			return new DOMDocumentType(qualifiedName, publicId, systemId);
		}

		/// <summary>
		/// Creates a new <see cref="DOMDocument"/>.
		/// </summary>
		/// <param name="instance">The <see cref="DOMImplementation"/> instance or <B>null</B>.</param>
		/// <param name="namespaceUri">The namespace URI of the root element to create.</param>
		/// <param name="qualifiedName">The qualified name of the document element.</param>
		/// <param name="docType">The type of document to be created.</param>
		/// <returns>The <see cref="DOMDocument"/>.</returns>
		[PhpVisible]
		public static object createDocument([This] DOMImplementation instance, string namespaceUri,
			string qualifiedName, [Nullable] DOMDocumentType docType)
		{
			XmlImplementation impl = (instance != null ? instance.XmlImplementation : new XmlImplementation());
			XmlDocument doc = impl.CreateDocument();

			if (docType != null)
			{
				if (!docType.IsAssociated) docType.Associate(doc);
				else
				{
					DOMException.Throw(ExceptionCode.WrongDocument);
					return false;
				}
			}

			doc.AppendChild(docType.XmlNode);
			doc.AppendChild(doc.CreateElement(qualifiedName, namespaceUri));

			return new DOMDocument(doc);
		}

		#endregion
	}

	/// <summary>
	/// DOM implementation source.
	/// </summary>
	[ImplementsType]
	public partial class DOMImplementationSource
	{
		#region Construction

		public DOMImplementationSource()
			: base(ScriptContext.CurrentContext, true)
		{ }

		#endregion

		#region Operations

		/// <summary>
		/// Not implemented in PHP 5.1.6.
		/// </summary>
		[PhpVisible]
		public void getDomimplementation(string features)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		/// <summary>
		/// Not implemented in PHP 5.1.6.
		/// </summary>
		[PhpVisible]
		public void getDomimplementations(string features)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		#endregion
	}

}
