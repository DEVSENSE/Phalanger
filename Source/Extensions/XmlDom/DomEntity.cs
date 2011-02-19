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
	/// DOM entity.
	/// </summary>
	[ImplementsType]
	public partial class DOMEntity : DOMNode
	{
		#region Fields and Properties

		protected internal XmlEntity XmlEntity
		{
			get
			{ return (XmlEntity)XmlNode; }
			set
			{ XmlNode = value; }
		}

		/// <summary>
		/// Returns the type of the node (<see cref="NodeType.Entity"/>).
		/// </summary>
		[PhpVisible]
		public override object nodeType
		{
			get
			{ return (int)NodeType.Entity; }
		}

		/// <summary>
		/// Returns the public identifier of this entity.
		/// </summary>
		[PhpVisible]
		public string publicId
		{
			get
			{ return XmlEntity.PublicId; }
		}

		/// <summary>
		/// Returns the system identifier of this entity.
		/// </summary>
		[PhpVisible]
		public string systemId
		{
			get
			{ return XmlEntity.SystemId; }
		}

		/// <summary>
		/// Returns the name of the optional NDATA attribute.
		/// </summary>
		[PhpVisible]
		public string notationName
		{
			get
			{ return XmlEntity.NotationName; }
		}

		/// <summary>
		/// Always returns <B>null</B> as in PHP 5.1.6.
		/// </summary>
		[PhpVisible]
		public string actualEncoding
		{
			get
			{ return null; }
			set
			{ }
		}

		/// <summary>
		/// Always returns <B>null</B> as in PHP 5.1.6.
		/// </summary>
		[PhpVisible]
		public string encoding
		{
			get
			{ return null; }
			set
			{ }
		}

		/// <summary>
		/// Always returns <B>null</B> as in PHP 5.1.6.
		/// </summary>
		[PhpVisible]
		public string version
		{
			get
			{ return null; }
			set
			{ }
		}
		
		#endregion

		#region Construction

		public DOMEntity()
			: base(ScriptContext.CurrentContext, true)
		{ }

		internal DOMEntity(XmlEntity/*!*/ xmlEntity)
			: base(ScriptContext.CurrentContext, true)
		{
			this.XmlEntity = xmlEntity;
		}

		protected override PHP.Core.Reflection.DObject CloneObjectInternal(PHP.Core.Reflection.DTypeDesc caller, ScriptContext context, bool deepCopyFields)
		{
			if (IsAssociated) return new DOMEntity(XmlEntity);
			else return new DOMEntity();
		}

		#endregion
	}

	/// <summary>
	/// DOM entity reference.
	/// </summary>
	[ImplementsType]
	public partial class DOMEntityReference : DOMNode
	{
		#region Fields and Properties

		protected internal XmlEntityReference XmlEntityReference
		{
			get
			{ return (XmlEntityReference)XmlNode; }
			set
			{ XmlNode = value; }
		}

		private string _name;

		/// <summary>
		/// Returns the name of the entity reference.
		/// </summary>
		[PhpVisible]
		public override string nodeName
		{
			get
			{ return (IsAssociated ? base.nodeName : _name); }
		}

		/// <summary>
		/// Returns <B>null</B>.
		/// </summary>
		[PhpVisible]
		public override object nodeValue
		{
			get
			{ return null; }
			set
			{ }
		}

		/// <summary>
		/// Returns <B>null</B>.
		/// </summary>
		[PhpVisible]
		public override string namespaceURI
		{
			get
			{ return null; }
		}

		/// <summary>
		/// Returns the type of the node (<see cref="NodeType.EntityReference"/>).
		/// </summary>
		[PhpVisible]
		public override object nodeType
		{
			get
			{ return (int)NodeType.EntityReference; }
		}

		#endregion

		#region Construction

		public DOMEntityReference()
			: base(ScriptContext.CurrentContext, true)
		{ }

		internal DOMEntityReference(XmlEntityReference/*!*/ xmlEntityReference)
			: base(ScriptContext.CurrentContext, true)
		{
			this.XmlEntityReference = xmlEntityReference;
		}

		protected override PHP.Core.Reflection.DObject CloneObjectInternal(PHP.Core.Reflection.DTypeDesc caller, ScriptContext context, bool deepCopyFields)
		{
			if (IsAssociated) return new DOMEntityReference(XmlEntityReference);
			else
			{
				DOMEntityReference copy = new DOMEntityReference();
				copy.__construct(this._name);
				return copy;
			}
		}

		[PhpVisible]
		public void __construct(string name)
		{
			this._name = name;
		}

		#endregion

		#region Hierarchy

		protected internal override void Associate(XmlDocument/*!*/ document)
		{
			if (!IsAssociated)
			{
				XmlEntityReference = document.CreateEntityReference(_name);
			}
		}

		#endregion
	}
}
