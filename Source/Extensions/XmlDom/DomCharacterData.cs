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
	/// DOM character data.
	/// </summary>
	[ImplementsType]
	public partial class DOMCharacterData : DOMNode
	{
		#region Fields and Properties

		protected internal XmlCharacterData XmlCharacterData
		{
			get
			{ return (XmlCharacterData)XmlNode; }
			set
			{ XmlNode = value; }
		}

		/// <summary>
		/// Returns or sets the data of the node.
		/// </summary>
		[PhpVisible]
		public object data
		{
			get
			{ return XmlCharacterData.Data; }
			set
			{ XmlCharacterData.Data = PHP.Core.Convert.ObjectToString(value); }
		}

		/// <summary>
		/// Returns the length of the data in characters.
		/// </summary>
		[PhpVisible]
		public int length
		{
			get
			{ return XmlCharacterData.Length; }
		}

		#endregion

		#region Construction

		public DOMCharacterData()
			: base(ScriptContext.CurrentContext, true)
		{ }

		protected override PHP.Core.Reflection.DObject CloneObjectInternal(PHP.Core.Reflection.DTypeDesc caller, ScriptContext context, bool deepCopyFields)
		{
			return new DOMCharacterData();
		}

		#endregion

		#region String operations

		/// <summary>
		/// Retrieves a substring of the full string from the specified range.
		/// </summary>
		/// <param name="offset">The position within the string to start retrieving.</param>
		/// <param name="count">The number of characters to retrieve.</param>
		/// <returns>The substring corresponding to the specified range or <B>false</B>.</returns>
		[PhpVisible]
		public object substringData(int offset, int count)
		{
			if (offset < 0 || count < 0 || offset > XmlCharacterData.Length)
			{
				DOMException.Throw(ExceptionCode.IndexOutOfBounds);
				return false;
			}

			return XmlCharacterData.Substring(offset, count);
		}

		/// <summary>
		/// Appends the specified string to the end of the character data of the node.
		/// </summary>
		/// <param name="arg">The string to insert into the existing string.</param>
		/// <returns><B>True</B>.</returns>
		[PhpVisible]
		public bool appendData(string arg)
		{
			XmlCharacterData.AppendData(arg);
			return true;
		}

		/// <summary>
		/// Inserts the specified string at the specified character offset. 
		/// </summary>
		/// <param name="offset">The position within the string to insert the supplied string data.</param>
		/// <param name="arg">The string data that is to be inserted into the existing string.</param>
		/// <returns><B>True</B> or <B>false</B>.</returns>
		[PhpVisible]
		public bool insertData(int offset, string arg)
		{
			if (offset < 0 || offset > XmlCharacterData.Length)
			{
				DOMException.Throw(ExceptionCode.IndexOutOfBounds);
				return false;
			}

			XmlCharacterData.InsertData(offset, arg);
			return true;
		}

		/// <summary>
		/// Removes a range of characters from the node.
		/// </summary>
		/// <param name="offset">The position within the string to start deleting.</param>
		/// <param name="count">The number of characters to delete.</param>
		/// <returns><B>True</B> or <B>false</B>.</returns>
		[PhpVisible]
		public bool deleteData(int offset, int count)
		{
			if (offset < 0 || count < 0 || offset > XmlCharacterData.Length)
			{
				DOMException.Throw(ExceptionCode.IndexOutOfBounds);
				return false;
			}

			XmlCharacterData.DeleteData(offset, count);
			return true;
		}

		/// <summary>
		/// Replaces the specified number of characters starting at the specified offset with the specified string.
		/// </summary>
		/// <param name="offset">The position within the string to start replacing.</param>
		/// <param name="count">The number of characters to replace.</param>
		/// <param name="arg">The new data that replaces the old string data.</param>
		/// <returns><B>True</B> or <B>false</B>.</returns>
		[PhpVisible]
		public bool replaceData(int offset, int count, string arg)
		{
			if (offset < 0 || count < 0 || offset > length)
			{
				DOMException.Throw(ExceptionCode.IndexOutOfBounds);
				return false;
			}

			XmlCharacterData.ReplaceData(offset, count, arg);
			return true;
		}

		#endregion
	}

	/// <summary>
	/// DOM text.
	/// </summary>
	[ImplementsType]
	public partial class DOMText : DOMCharacterData
	{
		#region Fields and Properties

		protected internal XmlText XmlText
		{
			get
			{ return (XmlText)XmlNode; }
			set
			{ XmlNode = value; }
		}

		protected string _value;

		/// <summary>
		/// Returns &quot;#text&quot;.
		/// </summary>
		[PhpVisible]
		public override string nodeName
		{
			get
			{ return "#text"; }
		}

		/// <summary>
		/// Returns or sets the text.
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
		/// Returns <B>null</B>.
		/// </summary>
		[PhpVisible]
		public override string namespaceURI
		{
			get
			{ return null; }
		}

		/// <summary>
		/// Returns the type of the node (<see cref="NodeType.Text"/>).
		/// </summary>
		[PhpVisible]
		public override object nodeType
		{
			get
			{ return (int)NodeType.Text; }
		}

		/// <summary>
		/// Gets the concatenated values of the node and all its child nodes.
		/// </summary>
		[PhpVisible]
		public string wholeText
		{
			get
			{ return (IsAssociated ? XmlText.InnerText : _value); }
		}

		#endregion

		#region Construction

		public DOMText()
		{ }

		internal DOMText(XmlText/*!*/ xmlText)
		{
			this.XmlText = xmlText;
		}

		protected override PHP.Core.Reflection.DObject CloneObjectInternal(PHP.Core.Reflection.DTypeDesc caller, ScriptContext context, bool deepCopyFields)
		{
			if (IsAssociated) return new DOMText(XmlText);
			else
			{
				DOMText copy = new DOMText();
				copy.__construct(this._value);
				return copy;
			}
		}

		[PhpVisible]
		public virtual void __construct([Optional] string value)
		{
			this._value = value;
		}

		#endregion

		#region Hierarchy

		protected internal override void Associate(XmlDocument/*!*/ document)
		{
			if (!IsAssociated)
			{
				XmlText = document.CreateTextNode(_value);
			}
		}

		#endregion

		#region String operations

		/// <summary>
		/// Splits the node into two nodes at the specified offset, keeping both in the tree as siblings.
		/// </summary>
		/// <param name="offset">The offset at which to split the node.</param>
		/// <returns>The new node.</returns>
		[PhpVisible]
		public object splitText(int offset)
		{
			if (offset < 0 || offset > XmlText.Length) return false;

			return XmlText.SplitText(offset);
		}

		/// <summary>
		/// Determines whether this text node is empty / whitespace only.
		/// </summary>
		/// <returns><B>True</B> or <B>false</B>.</returns>
		[PhpVisible]
		public bool isWhitespaceInElementContent()
		{
			return IsBlankNode();
		}

		/// <summary>
		/// Determines whether this text node is empty / whitespace only.
		/// </summary>
		/// <returns><B>True</B> or <B>false</B>.</returns>
		[PhpVisible]
		public bool isElementContentWhitespace()
		{
			return IsBlankNode();
		}

		/// <summary>
		/// Not implemented in PHP 5.1.6.
		/// </summary>
		[PhpVisible]
		public void replaceWholeText(string context)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		private bool IsBlankNode()
		{
			string text = nodeValue as string;
			if (text == null) return false;

			for (int i = 0; i < text.Length; i++)
			{
				if (!Char.IsWhiteSpace(text, i)) return false;
			}
			return true;
		}

		#endregion
	}

	/// <summary>
	/// DOM character data section.
	/// </summary>
	[ImplementsType]
	public partial class DOMCdataSection : DOMText
	{
		#region Fields and Properties

		protected internal XmlCDataSection XmlCDataSection
		{
			get
			{ return (XmlCDataSection)XmlNode; }
			set
			{ XmlNode = value; }
		}

		/// <summary>
		/// Returns &quot;#cdata-section&quot;.
		/// </summary>
		[PhpVisible]
		public override string nodeName
		{
			get
			{ return "#cdata-section"; }
		}

		/// <summary>
		/// Returns the type of the node (<see cref="NodeType.CharacterDataSection"/>).
		/// </summary>
		[PhpVisible]
		public override object nodeType
		{
			get
			{ return (int)NodeType.CharacterDataSection; }
		}

		#endregion

		#region Construction

		public DOMCdataSection()
		{ }

		internal DOMCdataSection(XmlCDataSection/*!*/ xmlCDataSection)
		{
			this.XmlCDataSection = xmlCDataSection;
		}

		protected override PHP.Core.Reflection.DObject CloneObjectInternal(PHP.Core.Reflection.DTypeDesc caller, ScriptContext context, bool deepCopyFields)
		{
			if (IsAssociated) return new DOMCdataSection(XmlCDataSection);
			else
			{
				DOMCdataSection copy = new DOMCdataSection();
				copy.__construct(this._value);
				return copy;
			}
		}

		[PhpVisible]
		public override void __construct(string value)
		{
			base.__construct(value);
		}

		#endregion

		#region Hierarchy

		protected internal override void Associate(XmlDocument/*!*/ document)
		{
			if (!IsAssociated)
			{
				XmlCDataSection = document.CreateCDataSection(_value);
			}
		}

		#endregion
	}

	/// <summary>
	/// DOM comment.
	/// </summary>
	[ImplementsType]
	public partial class DOMComment : DOMCharacterData
	{
		#region Fields and Properties

		protected internal XmlComment XmlComment
		{
			get
			{ return (XmlComment)XmlNode; }
			set
			{ XmlNode = value; }
		}

		private string _value;

		/// <summary>
		/// Returns &quot;#comment&quot;.
		/// </summary>
		[PhpVisible]
		public override string nodeName
		{
			get
			{ return "#comment"; }
		}

		/// <summary>
		/// Returns or sets the text.
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
		/// Returns <B>null</B>.
		/// </summary>
		[PhpVisible]
		public override string namespaceURI
		{
			get
			{ return null; }
		}

		/// <summary>
		/// Returns the type of the node (<see cref="NodeType.Comment"/>).
		/// </summary>
		[PhpVisible]
		public override object nodeType
		{
			get
			{ return (int)NodeType.Comment; }
		}

		#endregion

		#region Construction

		public DOMComment()
		{ }

		internal DOMComment(XmlComment/*!*/ xmlComment)
		{
			this.XmlComment = xmlComment;
		}

		protected override PHP.Core.Reflection.DObject CloneObjectInternal(PHP.Core.Reflection.DTypeDesc caller, ScriptContext context, bool deepCopyFields)
		{
			if (IsAssociated) return new DOMComment(XmlComment);
			else
			{
				DOMComment copy = new DOMComment();
				copy.__construct(this._value);
				return copy;
			}
		}

		[PhpVisible]
		public void __construct([Optional] string value)
		{
			this._value = value;
		}

		#endregion

		#region Hierarchy

		protected internal override void Associate(XmlDocument/*!*/ document)
		{
			if (!IsAssociated)
			{
				XmlComment = document.CreateComment(_value);
			}
		}

		#endregion
	}
}
