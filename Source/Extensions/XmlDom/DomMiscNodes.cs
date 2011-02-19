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
	/// DOM document type.
	/// </summary>
	[ImplementsType]
	public partial class DOMDocumentType : DOMNode
	{
		#region Fields and Properties

		protected internal XmlDocumentType XmlDocumentType
		{
			get
			{ return (XmlDocumentType)XmlNode; }
			set
			{ XmlNode = value; }
		}

		private string _qualifiedName;
		private string _publicId;
		private string _systemId;

		/// <summary>
		/// Returns the type of the node (<see cref="NodeType.DocumentType"/>).
		/// </summary>
		[PhpVisible]
		public override object nodeType
		{
			get
			{ return (int)NodeType.DocumentType; }
		}

		/// <summary>
		/// Returns the name of this document type.
		/// </summary>
		[PhpVisible]
		public object name
		{
			get
			{ return this.nodeName; }
		}

		/// <summary>
		/// Returns a map of the entities declared by this document type.
		/// </summary>
		[PhpVisible]
		public object entities
		{
			get
			{
				DOMNamedNodeMap map = new DOMNamedNodeMap();

				foreach (XmlNode entity in XmlDocumentType.Entities)
				{
					IXmlDomNode node = DOMNode.Create(entity);
					if (node != null) map.AddNode(node);
				}

				return map;
			}
		}

		/// <summary>
		/// Returns a map of the entities declared by this document type.
		/// </summary>
		[PhpVisible]
		public object notations
		{
			get
			{
				DOMNamedNodeMap map = new DOMNamedNodeMap();

				foreach (XmlNode notation in XmlDocumentType.Notations)
				{
					IXmlDomNode node = DOMNode.Create(notation);
					if (node != null) map.AddNode(node);
				}

				return map;
			}
		}

		/// <summary>
		/// Returns the value of the public identifier of this document type.
		/// </summary>
		[PhpVisible]
		public string publicId
		{
			get
			{ return XmlDocumentType.PublicId; }
		}

		/// <summary>
		/// Gets the value of the system identifier on this document type.
		/// </summary>
		[PhpVisible]
		public string systemId
		{
			get
			{ return XmlDocumentType.SystemId; }
		}

		/// <summary>
		/// Gets the value of the DTD internal subset on this document type.
		/// </summary>
		[PhpVisible]
		public string internalSubset
		{
			get
			{ return XmlDocumentType.InternalSubset; }
		}

		#endregion

		#region Construction

		public DOMDocumentType()
			: base(ScriptContext.CurrentContext, true)
		{ }

		internal DOMDocumentType(XmlDocumentType/*!*/ xmlDocumentType)
			: base(ScriptContext.CurrentContext, true)
		{
			this.XmlDocumentType = xmlDocumentType;
		}

		internal DOMDocumentType(string qualifiedName, string publicId, string systemId)
			: base(ScriptContext.CurrentContext, true)
		{
			this._qualifiedName = qualifiedName;
			this._publicId = publicId;
			this._systemId = systemId;
		}

		protected override PHP.Core.Reflection.DObject CloneObjectInternal(PHP.Core.Reflection.DTypeDesc caller, ScriptContext context, bool deepCopyFields)
		{
			if (IsAssociated) return new DOMDocumentType(XmlDocumentType);
			else return new DOMDocumentType(this._qualifiedName, this._publicId, this._systemId);
		}

		#endregion

		#region Hierarchy

		protected internal override void Associate(XmlDocument document)
		{
			if (!IsAssociated)
			{
				XmlDocumentType = document.CreateDocumentType(_qualifiedName, _publicId, _systemId, null);
			}
		}

		#endregion
	}

	/// <summary>
	/// DOM notation.
	/// </summary>
	[ImplementsType]
	public partial class DOMNotation : IXmlDomNode
	{
		#region IXmlDomNode Members

		XmlNode IXmlDomNode.UnderlyingObject
		{
			get { return XmlNotation; }
		}

		#endregion

		#region Fields and Properties

		private XmlNotation _xmlNotation;
		protected internal XmlNotation XmlNotation
		{
			get
			{
				if (_xmlNotation == null) DOMException.Throw(ExceptionCode.InvalidState);
				return _xmlNotation;
			}
			set
			{
				_xmlNotation = value;
			}
		}

		protected internal bool IsAssociated
		{
			get
			{ return (_xmlNotation != null); }
		}

		/// <summary>
		/// Returns the value of the public identifier on the notation declaration.
		/// </summary>
		[PhpVisible]
		public string publicId
		{
			get
			{ return XmlNotation.PublicId; }
		}

		/// <summary>
		/// Returns the value of the system identifier on the notation declaration.
		/// </summary>
		[PhpVisible]
		public string systemId
		{
			get
			{ return XmlNotation.SystemId; }
		}

		/// <summary>
		/// Returns the name of the notation node.
		/// </summary>
		[PhpVisible]
		public string nodeName
		{
			get
			{ return XmlNotation.Name; }
		}

		/// <summary>
		/// Returns or sets the value of the notation node.
		/// </summary>
		[PhpVisible]
		public object nodeValue
		{
			get
			{ return XmlNotation.Value; }
			set
			{ XmlNotation.Value = PHP.Core.Convert.ObjectToString(value); }
		}

		/// <summary>
		/// Returns the attributes of this notation node.
		/// </summary>
		[PhpVisible]
		public object attributes
		{
			get
			{
				DOMNamedNodeMap map = new DOMNamedNodeMap();
				
				foreach (XmlAttribute attr in XmlNotation.Attributes)
				{
					IXmlDomNode node = DOMNode.Create(attr);
					if (node != null) map.AddNode(node);
				}

				return map;
			}
		}

		#endregion

		#region Construction

		public DOMNotation()
			: base(ScriptContext.CurrentContext, true)
		{ }

		internal DOMNotation(XmlNotation/*!*/ xmlNotation)
			: base(ScriptContext.CurrentContext, true)
		{
			this.XmlNotation = xmlNotation;
		}

		protected override PHP.Core.Reflection.DObject CloneObjectInternal(PHP.Core.Reflection.DTypeDesc caller, ScriptContext context, bool deepCopyFields)
		{
			if (IsAssociated) return new DOMNotation(XmlNotation);
			else return new DOMNotation();
		}

		#endregion
	}

	/// <summary>
	/// DOM processing instruction.
	/// </summary>
	[ImplementsType]
	public partial class DOMProcessingInstruction : DOMNode
	{
		#region Fields and Properties

		protected internal XmlProcessingInstruction XmlProcessingInstruction
		{
			get
			{ return (XmlProcessingInstruction)XmlNode; }
			set
			{ XmlNode = value; }
		}

		private string _name;
		private string _value;

		/// <summary>
		/// Returns the name of the processing instruction.
		/// </summary>
		[PhpVisible]
		public override string nodeName
		{
			get
			{ return (IsAssociated ? base.nodeName : _name); }
		}

		/// <summary>
		/// Returns or sets the value of the processing instruction.
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
		/// Returns the namespace URI of the processing instruction.
		/// </summary>
		[PhpVisible]
		public override string namespaceURI
		{
			get
			{ return (IsAssociated ? base.namespaceURI : null); }
		}

		/// <summary>
		/// Returns the type of the node (<see cref="NodeType.ProcessingInstruction"/>).
		/// </summary>
		[PhpVisible]
		public override object nodeType
		{
			get
			{ return (int)NodeType.ProcessingInstruction; }
		}

		/// <summary>
		/// Returns the target (name) of the processing instruction.
		/// </summary>
		[PhpVisible]
		public object target
		{
			get
			{ return this.nodeName; }
		}

		/// <summary>
		/// Returns or sets the data (value) of the processing instruction.
		/// </summary>
		[PhpVisible]
		public object data
		{
			get
			{ return this.nodeValue; }
			set
			{ this.nodeValue = value; }
		}

		#endregion

		#region Construction

		public DOMProcessingInstruction()
			: base(ScriptContext.CurrentContext, true)
		{ }

		internal DOMProcessingInstruction(XmlProcessingInstruction/*!*/ xmlProcessingInstruction)
			: base(ScriptContext.CurrentContext, true)
		{
			this.XmlProcessingInstruction = xmlProcessingInstruction;
		}

		protected override PHP.Core.Reflection.DObject CloneObjectInternal(PHP.Core.Reflection.DTypeDesc caller, ScriptContext context, bool deepCopyFields)
		{
			if (IsAssociated) return new DOMProcessingInstruction(XmlProcessingInstruction);
			else
			{
				DOMProcessingInstruction copy = new DOMProcessingInstruction();
				copy.__construct(this._name, this._value);
				return copy;
			}
		}

		[PhpVisible]
		public void __construct(string name, [Optional] string value)
		{
			this._name = name;
			this._value = value;
		}

		#endregion
	}

	/// <summary>
	/// DOM namespace node (unsupported).
	/// </summary>
	[ImplementsType]
	public partial class DOMNameSpaceNode
	{
		#region Properties

		[PhpVisible]
		public string nodeName
		{
			get
			{
				DOMException.Throw(ExceptionCode.InvalidState);
				return null;
			}
		}

		[PhpVisible]
		public string nodeValue
		{
			get
			{
				DOMException.Throw(ExceptionCode.InvalidState);
				return null;
			}
		}

		[PhpVisible]
		public object nodeType
		{
			get
			{ return (int)NodeType.NamespaceDecl; }
		}

		[PhpVisible]
		public string prefix
		{
			get
			{
				DOMException.Throw(ExceptionCode.InvalidState);
				return null;
			}
		}

		[PhpVisible]
		public string namespaceURI
		{
			get
			{
				DOMException.Throw(ExceptionCode.InvalidState);
				return null;
			}
		}

		[PhpVisible]
		public object ownerDocument
		{
			get
			{
				DOMException.Throw(ExceptionCode.InvalidState);
				return null;
			}
		}

		[PhpVisible]
		public object parentNode
		{
			get
			{
				DOMException.Throw(ExceptionCode.InvalidState);
				return null;
			}
		}

		#endregion

		#region Construction

		public DOMNameSpaceNode()
			: base(ScriptContext.CurrentContext, true)
		{ }

		protected override PHP.Core.Reflection.DObject CloneObjectInternal(PHP.Core.Reflection.DTypeDesc caller, ScriptContext context, bool deepCopyFields)
		{
			return new DOMNameSpaceNode();
		}

		#endregion
	}
}
