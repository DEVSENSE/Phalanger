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
using System.Collections.Generic;
using System.Runtime.InteropServices;

using PHP.Core;

namespace PHP.Library.Xml
{
	/// <summary>
	/// DOM attribute.
	/// </summary>
	[ImplementsType]
	public partial class DOMAttr : DOMNode
	{
		#region Fields and Properties

		protected internal XmlAttribute XmlAttribute
		{
			get
			{ return (XmlAttribute)XmlNode; }
			set
			{ XmlNode = value; }
		}

		private string _name;
		private string _value;

		/// <summary>
		/// Returns the name of the attribute.
		/// </summary>
		[PhpVisible]
		public override string nodeName
		{
			get
			{ return (IsAssociated ? base.nodeName : _name); }
		}

		/// <summary>
		/// Returns or sets the value of the attribute.
		/// </summary>
		[PhpVisible]
		public override object nodeValue
		{
			get
			{ return (IsAssociated ? base.nodeValue : _value); }
			set
			{
				this._value = PHP.Core.Convert.ObjectToString(value);
				if (IsAssociated) base.nodeValue = this._value;
			}
		}

		/// <summary>
		/// Returns the namespace URI of the attribute.
		/// </summary>
		[PhpVisible]
		public override string namespaceURI
		{
			get
			{ return (IsAssociated ? base.namespaceURI : null); }
		}

		/// <summary>
		/// Returns the type of the node (<see cref="NodeType.Attribute"/>).
		/// </summary>
		[PhpVisible]
		public override object nodeType
		{
			get
			{ return (int)NodeType.Attribute; }
		}

		/// <summary>
		/// Returns the name of the attribute.
		/// </summary>
		[PhpVisible]
		public string name
		{
			get
			{ return this.nodeName; }
		}

		/// <summary>
		/// Returns or sets the value of this attribute
		/// </summary>
		[PhpVisible]
		public object value
		{
			get
			{ return (string)this.nodeValue; }
			set
			{ this.nodeValue = value; }
		}

		/// <summary>
		/// Always returns <B>true</B> as in PHP 5.1.6.
		/// </summary>
		public bool specified
		{
			get
			{ return true; }
		}

		/// <summary>
		/// Returns the <see cref="DOMElement"/> to which this attribute belongs.
		/// </summary>
		[PhpVisible]
		public object ownerElement
		{
			get
			{ return (IsAssociated ? DOMNode.Create(XmlAttribute.OwnerElement) : null); }
		}

		/// <summary>
		/// Not implemented in PHP 5.1.6.
		/// </summary>
		[PhpVisible]
		public object schemaTypeInfo
		{
			get
			{
				PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
				return null;
			}
		}

		#endregion

		#region Construction

		public DOMAttr()
			: base(ScriptContext.CurrentContext, true)
		{ }

		internal DOMAttr(XmlAttribute/*!*/ xmlAttribute)
			: base(ScriptContext.CurrentContext, true)
		{
			this.XmlAttribute = xmlAttribute;
		}

		protected override PHP.Core.Reflection.DObject CloneObjectInternal(PHP.Core.Reflection.DTypeDesc caller, ScriptContext context, bool deepCopyFields)
		{
			if (IsAssociated) return new DOMAttr(XmlAttribute);
			else
			{
				DOMAttr copy = new DOMAttr();
				copy.__construct(this._name, this._value);
				return copy;
			}
		}

		/// <summary>
		/// Initializes a new <see cref="DOMAttr"/> object.
		/// </summary>
		[PhpVisible]
		public void __construct(string name, [Optional] string value)
		{
			// just save up the name and value for later XmlAttribute construction
			this._name = name;
			this._value = value;
		}

		#endregion

		#region Hierarchy

		protected internal override void Associate(XmlDocument/*!*/ document)
		{
			if (!IsAssociated)
			{
				XmlAttribute attr = document.CreateAttribute(_name);
				if (_value != null) attr.Value = _value;

				XmlAttribute = attr;
			}
		}

		#endregion

		#region Validation

		/// <summary>
		/// Checks if attribute is a defined ID.
		/// </summary>
		/// <returns><B>True</B> or <B>false</B>.</returns>
		[PhpVisible]
		public object isId()
		{
			IXmlSchemaInfo schema_info = XmlNode.SchemaInfo;
			if (schema_info != null)
			{
				return (schema_info.SchemaType.TypeCode == XmlTypeCode.Id);
			}
			else return false;
		}

		#endregion
	}
}
