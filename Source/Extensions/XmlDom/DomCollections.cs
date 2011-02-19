/*

 Copyright (c) 2006 Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using PHP.Core;

namespace PHP.Library.Xml
{
	/// <summary>
	/// DOM node list.
	/// </summary>
	[ImplementsType]
	public partial class DOMNodeList : IEnumerable<KeyValuePair<object, object>>
	{
		#region Fields and Properties

		private List<IXmlDomNode>/*!*/ list;

		/// <summary>
		/// Returns the number of nodes in the list.
		/// </summary>
		[PhpVisible]
		public int length
		{
			get
			{ return list.Count; }
		}

		#endregion

		#region Construction

		public DOMNodeList()
			: base(ScriptContext.CurrentContext, true)
		{
			list = new List<IXmlDomNode>();
		}

		#endregion

		#region Item access

		internal void AppendNode(IXmlDomNode/*!*/ node)
		{
			Debug.Assert(node != null);
			list.Add(node);
		}

		/// <summary>
		/// Retrieves a node specified by an index.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns>The node or <B>NULL</B> if the <paramref name="index"/> is invalid.</returns>
		[PhpVisible]
		public IXmlDomNode item(int index)
		{
			if (index < 0 || index >= list.Count) return null;
			return list[index];
		}

		#endregion

		#region IEnumerable<KeyValuePair<object,object>> Members

		IEnumerator<KeyValuePair<object, object>> IEnumerable<KeyValuePair<object, object>>.GetEnumerator()
		{
			int count = list.Count;
			for (int i = 0; i < count; i++)
			{
				yield return new KeyValuePair<object, object>(i, list[i]);
			}
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			int count = list.Count;
			for (int i = 0; i < count; i++)
			{
				yield return new DictionaryEntry(i, list[i]);
			}
		}

		#endregion
	}

	/// <summary>
	/// DOM named node map.
	/// </summary>
	[ImplementsType]
	public partial class DOMNamedNodeMap : IEnumerable<KeyValuePair<object, object>>
	{
		#region MapKey

		private struct MapKey : IEquatable<MapKey>
		{
			public readonly string NamespaceUri;
			public readonly string/*!*/ LocalName;

			public MapKey(string namespaceUri, string/*!*/ localName)
			{
				Debug.Assert(localName != null);

				this.NamespaceUri = namespaceUri;
				this.LocalName = localName;
			}

			public override int GetHashCode()
			{
				int code = LocalName.GetHashCode();
				if (NamespaceUri != null) code ^= NamespaceUri.GetHashCode();
				return code;
			}

			#region IEquatable<MapKey> Members

			public bool Equals(MapKey other)
			{
				return (NamespaceUri == other.NamespaceUri && LocalName == other.LocalName);
			}

			#endregion
		}

		#endregion

		#region Fields and Properties

		private OrderedHashtable<MapKey>/*!*/ map;

		/// <summary>
		/// Returns the number of nodes in the map.
		/// </summary>
		[PhpVisible]
		public object length
		{
			get { return map.Count; }
		}

		#endregion

		#region Construction

		public DOMNamedNodeMap()
			: base(ScriptContext.CurrentContext, true)
		{
			map = new OrderedHashtable<MapKey>();
		}

		#endregion

		#region Item access

		internal void AddNode(IXmlDomNode/*!*/ node)
		{
			Debug.Assert(node != null);

			DOMNode domNode = node as DOMNode;
			if (domNode != null)
			{
				map.Add(new MapKey(domNode.namespaceURI, domNode.localName), domNode);
			}
			else
			{
				map.Add(new MapKey(node.UnderlyingObject.NamespaceURI, node.UnderlyingObject.LocalName), node);
			}
		}

		/// <summary>
		/// Retrieves a node specified by name.
		/// </summary>
		/// <param name="name">The (local) name of the node to retrieve.</param>
		/// <returns>A node with the specified (local) node name or <B>null</B> if no node is found.</returns>
		[PhpVisible]
		public object getNamedItem(string name)
		{
			if (name == null) return null;

			// try null namespace first
			object item;
			if (map.TryGetValue(new MapKey(null, name), out item)) return item;
			
			// iterate and take the first that fits
			foreach (KeyValuePair<MapKey, object> pair in map)
			{
				if (pair.Key.LocalName == name) return pair.Value;
			}

			return null;
		}

		/// <summary>
		/// Not implemented in PHP 5.1.6.
		/// </summary>
		[PhpVisible]
		public void setNamedItem(DOMNode item)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		/// <summary>
		/// Not implemented in PHP 5.1.6.
		/// </summary>
		[PhpVisible]
		public void removeNamedItem(string name)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		/// <summary>
		/// Retrieves a node specified by an index.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns>The node or <B>null</B> if <paramref name="index"/> is invalid.</returns>
		[PhpVisible]
		public object item(int index)
		{
			if (index < 0 || index >= map.Count) return null;

			OrderedHashtable<MapKey>.Enumerator enumerator = map.GetEnumerator();
			for (int i = 0; i <= index; i++)
			{
				enumerator.MoveNext();
			}
			return enumerator.Current.Value;
		}

		/// <summary>
		/// Retrieves a node specified by local name and namespace URI.
		/// </summary>
		/// <param name="namespaceUri">The namespace URI.</param>
		/// <param name="localName">The local name.</param>
		/// <returns>A node with the specified local name and namespace URI, or <B>null</B> if no node is found.</returns>
		[PhpVisible]
		public object getNamedItemNS(string namespaceUri, string localName)
		{
			if (localName == null) return null;

			object item;
			if (map.TryGetValue(new MapKey(namespaceUri, localName), out item)) return item;
			else return null;
		}

		/// <summary>
		/// Not implemented in PHP 5.1.6.
		/// </summary>
		[PhpVisible]
		public void setNamedItemNS(DOMNode item)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		/// <summary>
		/// Not implemented in PHP 5.1.6.
		/// </summary>
		[PhpVisible]
		public void removeNamedItemNS(string namespaceUri, string localName)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		#endregion

		#region IEnumerable<KeyValuePair<object,object>> Members

		IEnumerator<KeyValuePair<object, object>> IEnumerable<KeyValuePair<object, object>>.GetEnumerator()
		{
			foreach (KeyValuePair<MapKey, object> pair in map)
			{
				yield return new KeyValuePair<object, object>(pair.Key.LocalName, pair.Value);
			}
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			foreach (KeyValuePair<MapKey, object> pair in map)
			{
				yield return new DictionaryEntry(pair.Key.LocalName, pair.Value);
			}
		}

		#endregion
	}

	/// <summary>
	/// DOM string list. Not implemented in PHP 5.1.6.
	/// </summary>
	[ImplementsType]
	public partial class DOMStringList
	{
		#region Fields and Properies

		[PhpVisible]
		public object length
		{
			get
			{
				PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
				return null;
			}
		}

		#endregion

		#region Methods

		[PhpVisible]
		public void item(int index)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		#endregion
	}

	/// <summary>
	/// DOM name list. Not implemented in PHP 5.1.6.
	/// </summary>
	[ImplementsType]
	public partial class DOMNameList
	{
		#region Fields and Properies

		[PhpVisible]
		public object length
		{
			get
			{
				PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
				return null;
			}
		}

		#endregion

		#region Methods

		[PhpVisible]
		public void getName(int index)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		[PhpVisible]
		public void getNamespaceURI(int index)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		#endregion
	}

	/// <summary>
	/// DOM implementation list. Not implemented in PHP 5.1.6.
	/// </summary>
	[ImplementsType]
	public partial class DOMImplementationList
	{
		#region Fields and Properies

		[PhpVisible]
		public object length
		{
			get
			{
				PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
				return null;
			}
		}

		#endregion

		#region Methods

		[PhpVisible]
		public void item(int index)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		#endregion
	}

	/// <summary>
	/// DOM string extend. Not implemented in PHP 5.1.6.
	/// </summary>
	[ImplementsType]
	public partial class DOMStringExtend
	{
		#region Methods

		[PhpVisible]
		public void findOffset16(int offset16)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		[PhpVisible]
		public void findOffset32(int offset32)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		#endregion
	}
}
