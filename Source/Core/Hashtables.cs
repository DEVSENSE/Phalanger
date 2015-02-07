/*

 Copyright (c) 2004-2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Collections.Generic;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

/*

  NOTES:
   - operations on intrinsic operator forces it to be unbreakable
   
  FUTURE VERSION:
   - OrderdHashtable.SortToList
     - sorts hashtable and stores result into a list 
     - usefull for some methods which needs to iterate over sorted table

  COPY ON WRITE:
   - problem: enumerators
     - read/only enums  - can operate on a single version, version changes => enum unusable
     - read/write enums - operates on different versions, creation == write operation => copy made, version++
     - enumerator wrapper for PhpHT - cannot use OHT enumerator directly (must have reference to PhpHT + version)
*/


namespace PHP.Core
{
	/// <summary>
	/// Implemented operations.
	/// </summary>
	public enum SetOperations
	{
		Difference,
		Intersection
	}

	#region DualDictionary<K,V>

    /// <summary>
    /// DualDictionary contains two dictionaries that each one has its own comparer, but behaves as one dictionary
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    /// <remarks>
    /// It is used for example to store constants, because some constants ignores case and others don't
    /// </remarks>
	[DebuggerNonUserCode]
	public sealed class DualDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
	{
		private Dictionary<K, V>/*!*/ primary;
		private Dictionary<K, V>/*!*/ secondary;

		public DualDictionary(DualDictionary<K, V>/*!*/ dictionary)
		{
			if (dictionary == null)
				throw new ArgumentNullException("dictionary");

			this.primary = new Dictionary<K, V>(dictionary.primary, dictionary.primary.Comparer);
			this.secondary = new Dictionary<K, V>(dictionary.secondary, dictionary.secondary.Comparer);
		}

		public DualDictionary(IEqualityComparer<K> primaryComparer, IEqualityComparer<K> secondaryComparer)
		{
			this.primary = new Dictionary<K, V>(primaryComparer);
			this.secondary = new Dictionary<K, V>(secondaryComparer);
		}

		public int Count
		{
			get
			{
				return primary.Count + secondary.Count;
			}
		}

		public V this[K/*!*/ key]
		{
			get
			{
				V result;
				if (this.TryGetValue(key, out result))
					return result;
				else
					throw new KeyNotFoundException();
			}
		}

		public V this[K/*!*/ key, bool isPrimary]
		{
			set
			{
				(isPrimary ? primary : secondary)[key] = value;
			}
		}


		public bool TryGetValue(K/*!*/ key, out V result)
		{
			return primary.TryGetValue(key, out result) || secondary.TryGetValue(key, out result);
		}

		public bool TryGetValue(K key, out V result, out bool isSensitive)
		{
			return (isSensitive = primary.TryGetValue(key, out result)) || secondary.TryGetValue(key, out result);
		}

		public bool ContainsKey(K/*!*/ key)
		{
			return primary.ContainsKey(key) || secondary.ContainsKey(key);
		}

		public void Add(K/*!*/ key, V value, bool ignoreCase)
		{
			(ignoreCase ? secondary : primary).Add(key, value);
		}

		public bool Remove(K/*!*/ key)
		{
			return primary.Remove(key) || secondary.Remove(key);
		}

		public IEnumerator<KeyValuePair<K, V>>/*!*/ GetEnumerator()
		{
			foreach (KeyValuePair<K, V> entry in primary)
				yield return entry;

			foreach (KeyValuePair<K, V> entry in secondary)
				yield return entry;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			foreach (KeyValuePair<K, V> entry in primary)
				yield return new DictionaryEntry(entry.Key, entry.Value);

			foreach (KeyValuePair<K, V> entry in secondary)
				yield return new DictionaryEntry(entry.Key, entry.Value);
		}
	}

	#endregion

    #region OrderedHashtable<K>

    /// <summary>The hash table with an additional memory of an order in which elements have been added.</summary>
	/// <remarks>
	/// <para>
	/// The Enumerator enumerates through items in that order. 
	/// Unlike <see cref="System.Collections.Hashtable"/>'s enumerator this one doesn't get broken by changing 
	/// the underlying hashtable.
	/// </para>
	/// <para>The order of entries is maintained by a bidirectional circular list with a head.</para>
	/// </remarks>
	[DebuggerNonUserCode]
#if !SILVERLIGHT
	[Serializable]
#endif
	public sealed class OrderedHashtable<K> : IDictionary<K, object>, IDictionary, ICloneable, ISerializable
	{
		#region Fields & Specific Properties

		/// <summary>
		/// Expose the dictionary to item getters on <see cref="PhpArray"/> to make them a little bit faster.
		/// </summary>
        internal readonly Dictionary<K, Element>/*!*/dict;

		/// <summary>The head of the cyclic list.</summary>
        internal readonly Element/*!*/head;

        #endregion

		#region Constructors

        public OrderedHashtable()
            : this(null, 0)
        {
        }

        public OrderedHashtable(int capacity)
            :this(null, capacity)
        {
        }

        public OrderedHashtable(PhpHashtable owner)
            :this(owner,0)
		{
		}

        public OrderedHashtable(PhpHashtable owner, int capacity)
            : this(owner, capacity, /*(typeof(K) == typeof(IntStringKey)) ? (IEqualityComparer<K>)IntStringKey.EqualityComparer.Default :*/ null)
        {
        }

        public OrderedHashtable(PhpHashtable owner, int capacity, IEqualityComparer<K> comparer)
        {
            this.dict = new Dictionary<K, Element>(capacity, comparer);
            this.head = new Element(this);
            //this.owner = owner;
        }

		#endregion

        //#region Share, Unshare, CheckNotShared

        ///// <summary>
        ///// Marks this instance as shared (<see cref="IsShared"/>) and returns itself.
        ///// </summary>
        ///// <returns></returns>
        //public OrderedHashtable<K>/*!*/Share()
        //{
        //    ++_refCount;
        //    return this;
        //}

        ///// <summary>
        ///// Get back shared instance of internal data.
        ///// </summary>
        //public void Unshare()
        //{
        //    --_refCount;

        //    Debug.Assert(_refCount >= 0, "Too many Unshare() calls!");
        //}

        [Conditional("DEBUG")]
        private void CheckNotShared()
        {
            ////Debug.Assert(!this.IsShared, "Cannot modify shared OrderedHashtable.");
            //if (this.IsShared)
            //    throw new AccessViolationException("Cannot modify shared OrderedHashtable.");
        }

        //#endregion

        #region Inner class: Element

        /// <summary>
		/// An element stored in the table.
		/// </summary>
		[Serializable]
        [DebuggerNonUserCode]
        public sealed class Element
		{
			internal OrderedHashtable<K> Table;
			internal Element Next;
			internal Element Prev;

            ///// <summary>
            ///// The unique ID of Element within the <see cref="Table"/>.
            ///// </summary>
            //internal ulong ID;

			/// <summary>
			/// Key associated with the element. <see cref="InvalidItem"/> in head.
			/// </summary>
			public K Key
			{
				get { return _key; }
				internal set { _key = value; }  // ReindexIntegers
			}
			private K _key;

			/// <summary>
			/// Value associated with the element.
			/// </summary>
			public object Value
            {
                get
                {
                    return this._value;
                }
                set
                {
                    Table.CheckNotShared();
                    this._value = value;
                }
            }
			private object _value;

			public KeyValuePair<K, object> Entry { get { return new KeyValuePair<K, object>(_key, _value); } }

			internal Element(OrderedHashtable<K> table)
			{
				Table = table;

				Next = this;
				Prev = this;
                //ID = table.elementsId ++;

				this._value = InvalidItem.Default;                
			}

            internal Element(OrderedHashtable<K> table, K key, object value, Element next, Element prev)
            {
                // reverse order of arguments so they are served on top of the stack ?
                Prev = prev;
                Next = next;

                this._value = value;
                this._key = key;

                Table = table;

                //ID = table.elementsId++;
            }

			/// <summary>
			/// Gets whether the element is a head of the list.
			/// </summary>
            /// <exception cref="NullReferenceException">When called on a deleted element.</exception>
            internal bool IsHead { get { return this.Table.head == this; } }

            /// <summary>
            /// Gets whether the element was deleted from the array.
            /// </summary>
            internal bool IsDeleted { get { return this.Table == null; } }

			/// <summary>
			/// Boxes the value to a reference if it is not already a reference.
			/// </summary>
			/// <returns>The boxed value.</returns>
			public PhpReference/*!*/ MakeValueReference()
			{
				PhpReference result = _value as PhpReference;
				if (result == null)
				{
                    Table.CheckNotShared();

					// it is correct to box the Value without making a deep copy since there was a single pointer on Value
					// before this operation (by invariant) and there will be a single one after the operation as well:
					result = new PhpReference(_value);
					_value = result;
				}
				return result;
			}
		}

		#endregion

		#region Inner class: Enumerator

		/// <summary>
		/// Unbreakable enumerator which iterates through a hashtable in the order in which elements were added.
		/// </summary>
		/// <remarks>
		/// <para>
		/// If the enumerator reaches a head of a list it stops enumeration (<see cref="MoveNext"/> 
		/// returns <b>false</b>) and 
		/// has to be restarted in order to enumerate the list again from the beginning.
		/// Before it is done so both <c>Value</c> and <c>Key</c> properties return a reference to 
		/// <see cref="InvalidItem"/> internal singleton which cannot be accessed except for checinkg its type.
		/// Note, a key of an entry can never be a <B>null</B> reference. 
		/// </para>
		/// <para>
		/// Items can be deleted from an underlying Ordered Hashtable during an enumeration.
		/// When an item is removed is is immediately deleted from the underlying 
		/// hashtable and remains only connected to the list to allow enumeration continuation.
		/// Since deleted items are skipped by all enumerator's operations (<see cref="Current"/>, 
		/// <see cref="MoveNext"/>, ...) such an item cannot be accessed once it is deleted.
		/// </para>
		/// <para>
		/// Thanks to above two properties there is no exception thrown by this enumerator.
		/// </para>
		/// </remarks>
		[Serializable]
		public sealed class Enumerator : IEnumerator<KeyValuePair<K, object>>, IDictionaryEnumerator, IPhpEnumerator, IDisposable
		{
			#region Fields

            ///// <summary>
            ///// Reference to the hashtable that owns this enumerator. Can be null.
            ///// If <see cref="OrderedHashtable&lt;K&gt;"/> is changed (due to lazy copy),
            ///// the <see cref="Enumerator"/> can relink <see cref="head"/> and <see cref="current"/> accordingly.
            ///// </summary>
            //private readonly PhpHashtable hashtable;

			/// <summary>
			/// Reference to head of the list.
			/// </summary>
			internal Element/*!*/ head;

			/// <summary>
			/// Reference to the current element in the list.
			/// </summary>
			internal Element/*!*/ current;

			/// <summary>
			/// Whether the enumertor is starting a new enumeration of the list.
			/// </summary>
			/// <remarks>
			/// Set on construction and by <see cref="Reset"/> and unset by each <see cref="MoveNext"/>.
			/// Allows to distinguish two different states of enumerator in both of which the <c>current</c>
			/// element is a head of a list.
			/// </remarks>
			private bool starting;

			/// <summary>
			/// Whether the enumerator should return <c>KeyValuePair{K, object}</c> when used as <see cref="IEnumerator"/>.
			/// If <B>false</B> it will return <see cref="DictionaryEntry"/>.
			/// </summary>
			private readonly bool isGeneric;

			#endregion

			#region Constructors

			internal Enumerator(OrderedHashtable<K>/*!*/ hashtable, bool isGeneric)
			{
				Debug.Assert(hashtable != null, "hashtable cannot be null!");

                this.head = hashtable.head;
				this.current = head;
				this.starting = true;
				this.isGeneric = isGeneric;

				Debug.Assert(head.Value == InvalidItem.Default && !head.IsDeleted, "Unexpected head state!");
			}

            ///// <summary>
            ///// Create the enumerator aware of PhpHashtable container. Enumerator checks if the internal table
            ///// has been lazily copied and then updates links to the new one if necessary.
            ///// </summary>
            ///// <param name="hashtable">The <see cref="PhpHashtable"/> container. Cannot be null.</param>
            ///// <param name="isGeneric">True iff <see cref="IEnumerator"/> if being used. False in case of <see cref="IDictionaryEnumerator"/>.</param>
            //internal Enumerator(PhpHashtable/*!*/hashtable, bool isGeneric)
            //    : this(hashtable.table as OrderedHashtable<K>, isGeneric)
            //{
            //    // store the table container to allow internal table update check:
            //    this.hashtable = hashtable;

            //    // register the enumerator object:
            //    hashtable.RegisterEnumerator(this as OrderedHashtable<IntStringKey>.Enumerator);
            //}

			#endregion

            #region Misc: SkipDeletedForward, SkipDeletedBackward, EnsureCurrentTable

            /// <summary>
            /// Move <see cref="current"/> iterator over the deleted element (follows Next reference).
            /// </summary>
            /// <returns>The <see cref="current"/> iterator.</returns>
            internal Element SkipDeletedForward()
            {
                if (current.Table != null)
                    return current;

                return SkipDeletedForwardInternal();
            }

            /// <summary>
            /// Second part of <see cref="SkipDeletedForward"/>. The method is separated to enhance
            /// the call to <see cref="SkipDeletedForward"/>, the code contained in this method is rarely called.
            /// </summary>
            /// <returns>The <see cref="current"/> iterator.</returns>
            private Element SkipDeletedForwardInternal()
            {
                while (current.IsDeleted) current = current.Next;
                return current;
            }

            /// <summary>
            /// Move <see cref="current"/> iterator over the deleted element backwards (follows Prev reference).
            /// </summary>
            /// <returns>The <see cref="current"/> iterator.</returns>
            private Element SkipDeletedBackward()
            {
                while (current.IsDeleted) current = current.Prev;
                return current;
            }

            #endregion

            #region IPhpEnumerator Members

            /// <summary>
			/// Moves enumerator to the last element in the list if exists. 
			/// </summary>
			/// <returns>Whether there is any item in the list.</returns>
			public bool MoveLast()
			{
                current = head.Prev;
				return SkipDeletedBackward() != head;
			}

			/// <summary>
			/// Moves enumerator to the first element in the list if exists. 
			/// </summary>
			/// <returns>Whether there is any item in the list.</returns>
			public bool MoveFirst()
			{
                current = head.Next;
                starting = false;
				return SkipDeletedForward() != head;
			}

			/// <summary>
			/// Moves the enumerator to the previous element of the hashtable's list.
			/// </summary>
			/// <return>
			/// <B>true</B> if the enumerator was successfully moved to the previous element; 
			/// <B>false</B> if the enumerator has passed the beginning or end of the list.
			/// </return>       
			public bool MovePrevious()
			{
                // ensures the table first and skips deleted items:
                SkipDeletedForward();   // note: forward really  // note (J): this really have to be called first
                
                // we are at the end of the list and not ready to start iteration:
				if (current == head && !starting) return false;
				starting = false;

                // move prev, and skips deleted elements backward:
                current = current.Prev;
                return SkipDeletedBackward() != head;
			}

			public bool AtEnd
			{
				get
				{
					// if the enumerator is in starting state, it's not considered to be at the end:
					if (starting) return false;

                    // iterate while pointing to a deleted element:
					return SkipDeletedForward() == head;
				}
			}

			#endregion

			#region IEnumerator<KeyValuePair<K, object>> Members

			/// <summary>
			/// Gets current pair.
			/// </summary>
			public KeyValuePair<K, object> Current
			{
				get
				{
                    // skip deleted elements (forward) and return current:
					return SkipDeletedForward().Entry;
				}
			}

			#endregion

			#region IDisposable Members

			public void Dispose()
			{
                //// remove from the list of active enumerators:
                //if (hashtable != null)
                //    hashtable.UnregisterEnumerator(this as OrderedHashtable<IntStringKey>.Enumerator);
			}

			#endregion

			#region IEnumerator Members

			/// <summary>
			/// Gets current key-value pair or dictionary entry.
			/// </summary>
			object IEnumerator.Current
			{
				get
				{
                    return isGeneric ? Current : (object)((IDictionaryEnumerator)this).Entry;
				}
			}

			/// <summary>
			/// Advances the enumerator to the next element of the hashtable's list.
			/// </summary>
			/// <return>
			/// <B>true</B> if the enumerator was successfully advanced to the next element; 
			/// <B>false</B> if the enumerator has passed the end or the beginning of the list.
			/// </return>  
			public bool MoveNext()
			{
				// we are at the end of the list and not ready to start iteration:
                if (!starting)
                {
                    if (current == head)
                        return false;
                }
                else
                {
                    starting = false;
                }

                // skips deleted items:
                SkipDeletedForward(); // note (J): this really have to be called first
                
                // current ++, SkipDeletedForward();
                do { current = current.Next; } while (current.IsDeleted);

                // iterates while the "current" refereces a deleted element:
                return current != head;
			}

			/// <summary>
			/// Moves the enumerator to the head of the list (i.e. before the first element) and enables new iteration.
			/// </summary>
			public void Reset()
			{
                // no need to ensure the table, head is head

				current = head;
				starting = true;
			}

			#endregion

			#region IDictionaryEnumerator Members

			DictionaryEntry IDictionaryEnumerator.Entry
			{
				get
				{
					KeyValuePair<K, object> pair = Current;
					return new DictionaryEntry(pair.Key, pair.Value);
				}
			}

			object IDictionaryEnumerator.Key
			{
				get
				{
                    return SkipDeletedForward().Key;
				}
			}

			object IDictionaryEnumerator.Value
			{
				get
				{
                    return SkipDeletedForward().Value;
				}
			}

			#endregion
		}

		#endregion

		#region List operations: LinkNextsByPrevs, LinkPrevsByNexts, ReversePrevLinks, ReverseNextLinks

		/// <summary>
		/// Links <see cref="Element.Next"/> links according to <see cref="Element.Prev"/>.
		/// </summary>
		/// <param name="head">The head of the list.</param>
		internal static void LinkNextsByPrevs(Element/*!*/ head)
		{
			Element iterator_next = head;
			Element iterator = head.Prev;

			do
			{
				iterator.Next = iterator_next;
				iterator_next = iterator;
				iterator = iterator.Prev;
			}
			while (iterator_next != head);
		}

		/// <summary>
		/// Links <see cref="Element.Prev"/> links according to <see cref="Element.Next"/>.
		/// </summary>
		/// <param name="head">The head of the list.</param>
		internal static void LinkPrevsByNexts(Element/*!*/ head)
		{
			Element iterator_prev = head;
			Element iterator = head.Next;

			do
			{
				iterator.Prev = iterator_prev;
				iterator_prev = iterator;
				iterator = iterator.Next;
			}
			while (iterator_prev != head);
		}

		/// <summary>
		/// Reverses <see cref="Element.Prev"/> links.
		/// </summary>
		/// <param name="head">The head of the list.</param>
		internal static void ReversePrevLinks(Element head)
		{
            Element iterator_next = head;
			Element iterator = head.Prev;
			Element prev;

			do
			{
				prev = iterator.Prev;
				iterator.Prev = iterator_next;
				iterator_next = iterator;
				iterator = prev;
			}
			while (iterator_next != head);
		}

		#endregion

		#region Special Operations: AddBefore, AddAfter, Delete, Prepend, GetElement, SetElement, RemoveFirst, RemoveLast

		/// <summary>
		/// Adds an entry pair into the table before a specified element. 
		/// </summary>
		/// <param name="element">The element before which to add new entry.</param>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is a null reference.</exception>
		/// <exception cref="ArgumentException">An element with the same key already exists in this instance.</exception>
		internal Element/*!*/AddBefore(Element element, K key, object value)
		{
            var new_element = new Element(this, key, value, element, element.Prev);
            dict.Add(key, new_element);
            new_element.Prev.Next = new_element;
            new_element.Next.Prev = new_element;

            return new_element;
		}

        ///// <summary>
        ///// Adds deleted entry into the linked list only. There is no value and the element is marked as deleted immediately.
        ///// </summary>
        ///// <param name="element">Element to add before.</param>
        ///// <param name="key">The key to be added.</param>
        ///// <returns>Added element.</returns>
        //internal Element/*!*/AddDeletedBefore(Element element, K key)
        //{
        //    var new_element = new Element(this, key, null, element, element.Prev);
        //    new_element.Table = null;
        //    new_element.Prev.Next = new_element;
        //    new_element.Next.Prev = new_element;
            
        //    return new_element;
        //}

        /// <summary>
		/// Adds an entry pair into the table after a specified element. 
		/// </summary>
		/// <param name="element">The element before which to add new entry.</param>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is a null reference.</exception>
		/// <exception cref="ArgumentException">An element with the same key already exists in this instance.</exception>
		private void AddAfter(Element element, K key, object value)
		{
			Element new_element = new Element(this, key, value, element.Next, element);
			dict.Add(key, new_element);
			new_element.Prev.Next = new_element;
			new_element.Next.Prev = new_element;
		}

		/// <summary>
		/// Disconnects an element from the list an marks it as deleted.
		/// </summary>
		/// <param name="element">The element to be deleted.</param>
		internal void Delete(Element element)
		{
            // disconnects (unilaterally):
            element.Prev.Next = element.Next;
            element.Next.Prev = element.Prev;

            // drop the value:
            element.Value = null;

			// marks item as deleted:
			element.Table = null;
		}

		/// <summary>
		/// Adds a key-value pair into the table at its logical beginning.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is a null reference.</exception>
		/// <exception cref="ArgumentException">An element with the same key already exists in this instance.</exception>
		public void Prepend(K key, object value)
		{
            this.CheckNotShared();
			AddAfter(head, key, value);
		}

		/// <summary>
		/// Gets an element representing the key-value pair in the table.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>The element.</returns>
		public Element GetElement(K key)
		{
			Element element;
			return dict.TryGetValue(key, out element) ? element : null;
		}

		/// <summary>
		/// Sets an element representing the key-value pair in the table.
		/// </summary>
		/// <param name="element">The element.</param>
		internal void RehashElement(Element/*!*/ element)
		{
			dict[element.Key] = element;
		}

		/// <summary>
		/// Removes the last entry of the array and returns it.
		/// </summary>
		/// <returns>The last entry of the array.</returns>
		/// <exception cref="InvalidOperationException">The table is empty.</exception>
		public KeyValuePair<K, object> RemoveLast()
		{
			if (this.Count == 0)
				throw new InvalidOperationException(CoreResources.GetString("item_removed_from_empty_array"));

            this.CheckNotShared();

            var entry = head.Prev;
            //// skip deleted
            //while (entry.IsDeleted) entry = entry.Prev;

            KeyValuePair<K, object> last_entry = entry.Entry;
            Remove(entry.Key);
			return last_entry;
		}

		/// <summary>
		/// Removes the first entry of the array and returns it.
		/// </summary>
		/// <returns>The first entry of the array.</returns>
		/// <exception cref="InvalidOperationException">The table is empty.</exception>
		public KeyValuePair<K, object> RemoveFirst()
		{
			if (this.Count == 0)
				throw new InvalidOperationException(CoreResources.GetString("item_removed_from_empty_array"));

            this.CheckNotShared();

            var entry = head.Next;
            //// skip deleted
            //while (entry.IsDeleted) entry = entry.Next;

            //
			KeyValuePair<K, object> first_entry = entry.Entry;
			Remove(entry.Key);
			return first_entry;
		}

		#endregion

		#region ICloneable

		/// <summary>
		/// Creates a shallow copy of this instance.
		/// </summary>
		/// <returns>The new hashtable.</returns>
		/// <remarks>This instance should not be interconnected with the other.</remarks>
		/// <exception cref="InvalidOperationException">This table is interconnected with the other.</exception>
		public object Clone()
		{
			OrderedHashtable<K> result = new OrderedHashtable<K>(null, this.Count);

			for (var iterator = head.Next; iterator != head; iterator = iterator.Next)
            {
                result.Add(iterator.Key, iterator.Value);

            }
			
			return result;
		}

		#endregion

		#region IDictionary<K,object> Members

		public bool TryGetValue(K key, out object value)
		{
			Element element;
			if (dict.TryGetValue(key, out element))
			{
				value = element.Value;
				return true;
			}
			else
			{
				value = null;
				return false;
			}
		}

		/// <summary>
		/// Gets or sets a value associated with a key.
		/// </summary>
		/// <param name="key">The key whose value to get or set.</param>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is a null reference.</exception>
		public object this[K key]
		{
			get
			{
				Element element;
				return dict.TryGetValue(key, out element) ? element.Value : null;
			}
			set
			{
				Element element;

				// adds new element or changes an existing one:
				if (dict.TryGetValue(key, out element))
					element.Value = value;
				else
					Add(key, value);
			}
		}

		public bool ContainsKey(K key)
		{
			return dict.ContainsKey(key);
		}

		/// <summary>
		/// Adds an entry into the table at its logical end. 
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is a null reference.</exception>
		/// <exception cref="ArgumentException">An element with the same key already exists in this instance.</exception>
		public void Add(K key, object value)
		{
            this.CheckNotShared();

			AddBefore(head, key, value);
		}

		/// <summary>
		/// Removes an entry pair from the table.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>Whether the key was contained in the dictionary prior removal.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is a <B>null</B> reference.</exception>
		public bool Remove(K key)
		{
			Element element;
			if (dict.TryGetValue(key, out element))
			{
                this.CheckNotShared();

				// removes from hashtable:
				dict.Remove(key);

				// removes from list:
				Delete(element);

				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Gets a collection of keys.
		/// </summary>
		public ICollection<K>/*!*/ Keys
		{
			get
			{
                return _keys ?? (_keys = new KeyCollection(this));
			}
		}
		[NonSerialized]
		private KeyCollection _keys;

		/// <summary>
		/// Gets a collection of values. 
		/// </summary>
		public ICollection<object>/*!*/ Values
		{
			get
			{
				return _values ?? (_values = new ValueCollection(this));
			}
		}
		[NonSerialized]
		private ValueCollection _values;

		#region Inner class: KeyCollection

		[Serializable]
		public sealed class KeyCollection : ICollection<K>, ICollection
		{
			private readonly OrderedHashtable<K>/*!*/ hashtable;

			internal KeyCollection(OrderedHashtable<K>/*!*/ hashtable)
			{
				this.hashtable = hashtable;
			}

			#region ICollection<K> Members

			public bool Contains(K item)
			{
				return hashtable.ContainsKey(item);
			}

			public void CopyTo(K[]/*!*/ array, int index)
			{
				ArrayUtils.CheckCopyTo(array, index, hashtable.Count);

				//foreach (KeyValuePair<K, object> entry in hashtable)
                for (var p = hashtable.head.Next; p != hashtable.head; p = p.Next)
                    //if (!p.IsDeleted)
					    array[index++] = p.Key;
			}

			public bool IsReadOnly { get { return true; } }

			public void Add(K item)
			{
				throw new NotSupportedException();
			}

			public void Clear()
			{
				throw new NotSupportedException();
			}

			public bool Remove(K item)
			{
				throw new NotSupportedException();
			}

			#endregion

			#region ICollection Members

			public int Count { get { return hashtable.Count; } }

			public bool IsSynchronized { get { return false; } }

			public object SyncRoot { get { return this; } }

            void ICollection.CopyTo(Array/*!*/ array, int index)
            {
                CopyTo((K[])array, index);
            }

			#endregion

			#region IEnumerable<K> Members

			public IEnumerator<K> GetEnumerator()
			{
				//foreach (KeyValuePair<K, object> pair in hashtable)
                for (var p = hashtable.head.Next; p != hashtable.head; p = p.Next)
                    //if (!p.IsDeleted)
					    yield return p.Key;
			}

			#endregion

			#region IEnumerable Members

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			#endregion
		}

		#endregion

		#region Inner class: ValueCollection

		/// <summary>
		/// Auxiliary collection used for manipulating keys or values of PhpHashtable.
		/// </summary>
		[Serializable]
		public sealed class ValueCollection : ICollection<object>, ICollection
		{
			private readonly OrderedHashtable<K>/*!*/ hashtable;

			internal ValueCollection(OrderedHashtable<K>/*!*/ hashtable)
			{
				this.hashtable = hashtable;
			}

			#region ICollection<object> Members

			public bool Contains(object item)
			{
                for (var p = hashtable.head.Next; p != hashtable.head; p = p.Next)
                    //if (!p.IsDeleted)
                        if (p.Value == item)
                            return true;

                return false;
			}

			public void CopyTo(object[]/*!*/ array, int index)
			{
				ArrayUtils.CheckCopyTo(array, index, hashtable.Count);

				for (var p = hashtable.head.Next; p != hashtable.head; p = p.Next)
                    //if (!p.IsDeleted)
                        array[index++] = p.Value;
			}

			public bool IsReadOnly { get { return true; } }

			public void Add(object item)
			{
				throw new NotSupportedException();
			}

			public void Clear()
			{
				throw new NotSupportedException();
			}

			public bool Remove(object item)
			{
				throw new NotSupportedException();
			}

			#endregion

			#region ICollection Members

			public int Count { get { return hashtable.Count; } }

			public bool IsSynchronized { get { return false; } }

			public object SyncRoot { get { return this; } }

			public void CopyTo(Array/*!*/ array, int index)
			{
				CopyTo((object[])array, index);
			}

			#endregion

			#region IEnumerable<object> Members

			public IEnumerator<object> GetEnumerator()
			{
                //foreach (KeyValuePair<K, object> pair in hashtable)
				for (var p = hashtable.head.Next; p != hashtable.head; p = p.Next)
                    //if (!p.IsDeleted)
                        yield return p.Value;
			}

			#endregion

			#region IEnumerable Members

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			#endregion
		}

		#endregion

		#endregion

		#region ICollection<KeyValuePair<K,object>> Members

		public void CopyTo(KeyValuePair<K, object>[]/*!*/ array, int index)
		{
			ArrayUtils.CheckCopyTo(array, index, this.Count);

			Element iterator = head.Next;
			while (iterator != head)
			{
				array[index] = iterator.Entry;
				iterator = iterator.Next;
				index++;
			}
		}

		void ICollection<KeyValuePair<K, object>>.Add(KeyValuePair<K, object> item)
		{
			Add(item.Key, item.Value);
		}

		bool ICollection<KeyValuePair<K, object>>.Contains(KeyValuePair<K, object> item)
		{
			object value;
			return TryGetValue(item.Key, out value) && EqualityComparer<object>.Default.Equals(value, item.Value);
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		bool ICollection<KeyValuePair<K, object>>.Remove(KeyValuePair<K, object> item)
		{
			object value;
			if (TryGetValue(item.Key, out value) && EqualityComparer<object>.Default.Equals(value, item.Value))
			{
				Remove(item.Key);
				return true;
			}
			return false;
		}

		#endregion

		#region IEnumerable<KeyValuePair<K,object>> Members

		IEnumerator<KeyValuePair<K, object>>/*!*/ IEnumerable<KeyValuePair<K, object>>.GetEnumerator()
		{
			return new Enumerator(this, true);
		}

		#endregion

		#region IEnumerable Members

		IEnumerator/*!*/ IEnumerable.GetEnumerator()
		{
			return new Enumerator(this, true);
		}

		#endregion

		#region IDictionary Members

		/// <summary>
		/// Removes all elements from the table.
		/// </summary>
		public void Clear()
		{
            this.CheckNotShared();

			dict.Clear();
			head.Next = head;
			head.Prev = head;
		}

		public bool IsFixedSize { get { return false; } }

		ICollection IDictionary.Keys { get { return (ICollection)dict.Keys; } }

		ICollection IDictionary.Values { get { return (ICollection)this.Values; } }

		void IDictionary.Add(object key, object value)
		{
			this.Add((K)key, value);
		}

		bool IDictionary.Contains(object key)
		{
			return dict.ContainsKey((K)key);
		}

		IDictionaryEnumerator IDictionary.GetEnumerator()
		{
			return new Enumerator(this, false);
		}

		void IDictionary.Remove(object key)
		{
			this.Remove((K)key);
		}

		object IDictionary.this[object key]
		{
			get
			{
				return this[(K)key];
			}
			set
			{
				this[(K)key] = value;
			}
		}

		#endregion

		#region ICollection Members

		public int Count { get { return dict.Count; } }

		public bool IsSynchronized { get { return false; } }

		public object SyncRoot { get { return ((ICollection)dict).SyncRoot; } }

		/// <summary>
		/// Copies values stored in this table and the interconnected table to a given array.
		/// </summary>
		/// <param name="array">The array where to copy values. The array is expected to be already allocated.</param>
		/// <param name="index">The index to the <paramref name="array"/> where the first value stored in the hashtable will be copied to.</param>
		void ICollection.CopyTo(Array/*!*/ array, int index)
		{
			ArrayUtils.CheckCopyTo(array, index, this.Count);

			KeyValuePair<K, object>[] pairs = array as KeyValuePair<K, object>[];
			if (pairs != null)
			{
				CopyTo(pairs, index);
				return;
			}

			DictionaryEntry[] entries = array as DictionaryEntry[];
			if (entries != null)
			{
				CopyTo(entries, index);
			}

			throw new InvalidOperationException();
		}

		private void CopyTo(DictionaryEntry[]/*!*/ array, int index)
		{
			Element iterator = head.Next;
			while (iterator != head)
			{
				array[index] = new DictionaryEntry(iterator.Key, iterator.Value);
				iterator = iterator.Next;
				index++;
			}
		}

		#endregion

		#region Enumerator

		public Enumerator/*!*/ GetEnumerator()
		{
			return new Enumerator(this, true);
		}

		#endregion

		// CLR only //

		#region ISerializable (CLR only)
#if !SILVERLIGHT

        [System.Security.SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("dict", dict);
			info.AddValue("head", head);
		}

		/// <include file='Doc/Common.xml' path='/docs/method[@name="serialization.ctor"]/*'/>
		private OrderedHashtable(SerializationInfo info, StreamingContext context)
		{
			dict = (Dictionary<K, Element>)info.GetValue("dict", typeof(Dictionary<K, Element>));
			head = (Element)info.GetValue("head", typeof(Element));
		}

#endif
		#endregion

		// Advanced Operations //

		#region Sorting

		/// <remarks>
		/// Stably sorts a portion of a list of <see cref="Element"/>s.
		/// </remarks>
		/// <param name="comparer">The comparer used to sort elements.</param>
		/// <param name="count">The length of the portion of elements to sort.</param>
		/// <param name="first">The first element in the portion.</param>
		/// <param name="next">The element following the last element of the portion before a call is made.</param>
		/// <param name="successor">The element which will be set as successor of the last element of the list.</param>
		/// <returns>The sorted portion. Reference to the first element of unidirectional list.</returns>
		/// <remarks>
		/// <para>The list's first element is referenced by the <paramref name="first"/> parameter.</para>
		/// <para>The portion is <paramref name="count"/> elements long. A reference to an element which has immediately 
		/// followed the last element of the sorted portion before the method had been called will be returned 
		/// in the <paramref name="next"/>.</para>
		/// <para>The portion is sorted and the resulting unidirectional list is returned. Elements
		/// are joined only in one direction - <see cref="Element.Next"/> fields of elements are connected. 
		/// <see cref="Element.Prev"/> fields have to be connected subsequently as well as the head of the list.</para>
		/// <para>The sort is stable, key-value associations are preserved as well as <see cref="Element.Prev"/> references.</para>
		/// </remarks>
		private static Element MergeSortRecursive(IComparer<KeyValuePair<K, object>>/*!*/ comparer, Element/*!*/ successor,
			int count, Element/*!*/ first, out Element/*!*/ next)
		{
			Debug.Assert(comparer != null && successor != null && first != null);

			// only one element remains:
			if (count == 1)
			{
				next = first.Next;

				// store the successor here - all references to the successor will be 
				// overwritten by sorting expcept for the one which will be the very 
				// last in the sorted list (that's what we want):
				first.Next = successor;

				return first;
			}

			int alen = count >> 1;
			int blen = count - alen;
			Debug.Assert(alen <= blen && alen > 0);

			// divides the portion into two lists (a and b) and sorts them:
			Element result;
			Element a = MergeSortRecursive(comparer, successor, alen, first, out result);
			Element b = MergeSortRecursive(comparer, successor, blen, result, out next);

			// initializes merging - sets the first element of the result list:
			if (comparer.Compare(a.Entry, b.Entry) <= 0)
			{
				// if there is exactly one element in the a list returns (a,b) list:
				if (--alen == 0) { a.Next = b; return a; }
				result = a;
				a = a.Next;
			}
			else
			{
				// if there is exactly one element in the b list returns (a,b) list:
				if (--blen == 0) { b.Next = a; return b; }
				result = b;
				b = b.Next;
			}

			// merges "a" and "b" lists into the "result";
			// "iterator" points to the last element added to the "result", 
			// "a" and "b" references moves along the respective lists:
			Element iterator = result;
			Debug.Assert(alen > 0 && blen > 0);
			for (; ; )
			{
				if (comparer.Compare(a.Entry, b.Entry) <= 0)
				{
					// adds element from list "a" to the "result":
					iterator = iterator.Next = a;

					if (--alen == 0)
					{
						// adds remaining elements to the result: 
						if (blen > 0) iterator.Next = b;
						break;
					}

					// advances "a" pointer:
					a = a.Next;
				}
				else
				{
					// adds element from list "b" to the "result":
					iterator = iterator.Next = b;

					if (--blen == 0)
					{
						// adds remaining elements to the result: 
						if (alen > 0) iterator.Next = a;
						break;
					}

					// advances "a" pointer:
					b = b.Next;
				}
			}

			return result;
		}

		/// <summary>
		/// Interconnects elements of given lists into a grid using their <see cref="Element.Prev"/> fields.
		/// </summary>
		/// <param name="iterators">Elements referencing heads of the lists at the beginning and at the end as well.</param>
		/// <param name="count">The number of elements in each and every list.</param>
		/// <remarks>
		/// The grid: <BR/>
		/// <PRE>
		///  H H H
		///  | | |
		/// ~o~o~o~
		///  | | |   ~ = Prev (right to left), cyclic without a head (necessary)
		/// ~o~o~o~  - = Next (top to bottom), cyclic with a head (not necessary)
		///  | | |
		/// </PRE>
		/// </remarks>
		internal static void InterconnectGrid(int count, Element[]/*!!*/ iterators)
		{
			int last = iterators.Length - 1;

			// moves all iterators to the respective first elements:
			for (int i = 0; i <= last; i++)
				iterators[i] = iterators[i].Next;

			while (count-- > 0)
			{
				// sets Prev field of the first iterator:
				iterators[0].Prev = iterators[last];

				// all iterators except for the last one:
				for (int i = 0; i < last; i++)
				{
					iterators[i + 1].Prev = iterators[i];
					iterators[i] = iterators[i].Next;
				}

				// advances the last iterator:
				iterators[last] = iterators[last].Next;
			}

			// all iterators are on the respective heads now:
			for (int i = 0; i <= last; i++)
				Debug.Assert(iterators[i].IsHead);
		}

		/// <summary>
		/// Disconnects elements of lists each from other.
		/// </summary>
		/// <param name="count">The number of elements in each and every list.</param>
		/// <param name="iterators">Elements referencing heads of the lists at the beginning and at the end as well.</param>
		internal static void DisconnectGrid(int count, Element[] iterators)
		{
			for (int i = 0; i < iterators.Length; i++)
			{
				// restores Prev references in all elements of the i-th list except for the head:
				Element iterator = iterators[i];
				for (int j = 0; j <= count; j++)
				{
					iterator.Next.Prev = iterator;
					iterator = iterator.Next;
				}
			}
		}

		/// <summary>
		/// Reorders a minor list according to the major one. "Straightens" horizontal interconnection.
		/// </summary>
		/// <param name="count">The number of elements in each and every list.</param>
		/// <param name="minorHead">The head of a minor list.</param>
		/// <param name="majorHead">The head of a major list.</param>
		internal static void ReorderList(int count, Element minorHead, Element majorHead)
		{
			Element major = majorHead.Next;
			Element minor = minorHead;

			while (count-- > 0)
			{
				minor.Next = major.Prev;
				minor = minor.Next;
				major = major.Next;
			}

			minor.Next = minorHead;
		}

		/// <summary>
		/// Sorts multiple lists given comparer for each hashtable.
		/// </summary>
		/// <param name="count">The number of items in each and every list.</param>
		/// <param name="heads">Heads of lists.</param>
		/// <param name="comparers">Comperers to be used for lexicographical comparison.</param>
		internal static void Sort(int count, Element[]/*!!*/ heads, IComparer<KeyValuePair<K, object>>[]/*!!*/ comparers)
		// TODO: IComparer<KeyValuePair<K, object>>
		{
			Element next;
			int length = heads.Length;
			int last = length - 1;

            // nothing to do:
            if (count == 0) return;

			// interconnects all lists into a grid, heads are unchanged:
			InterconnectGrid(count, heads);

			// lists are only single-linked cyclic and "heads" are unchanged from here on:
			for (int i = last; i > 0; i--)
			{
				// sorts i-th list (doesn't modify Prev and keeps the list cyclic):
				heads[i].Next = MergeSortRecursive(comparers[i], heads[i], count, heads[i].Next, out next);
				Debug.Assert(next == heads[i]);

				// reorders the (i-1)-the list according to the the i-th one:
				ReorderList(count, heads[i - 1], heads[i]);
			}

			// sorts the 0-th list (its order will determine the order of whole grid rows):
			heads[0].Next = MergeSortRecursive(comparers[0], heads[0], count, heads[0].Next, out next);
			Debug.Assert(next == heads[0]);

			// reorders the last list according to the 0-th one:
			ReorderList(count, heads[last], heads[0]);

			// reorders remaining lists (if any):
			for (int i = last; i >= 2; i--)
				ReorderList(count, heads[i - 1], heads[i]);

			// disconnects lists from each other and reconstructs their double-linked structure:
			DisconnectGrid(count, heads);
		}

		/// <summary>
		/// Sorts elements of the hashtable.
		/// </summary>
		/// <param name="comparer">The comparer used for sorting.</param>
		/// <remarks>
		/// Changes only the prev/next references of elements contained. 
		/// Entries are neither copied nor modified.
		/// Affects interconnected table's items as well.
		/// </remarks>
		public void Sort(IComparer<KeyValuePair<K, object>>/*!*/ comparer)
		{
			// total number of elements (interconnected table has to be taken into consideration): 
			int count = this.Count;
			if (count <= 1) return;

            this.CheckNotShared();

			// sort whole list of elements:
			Element next;
			head.Next = MergeSortRecursive(comparer, head, count, head.Next, out next);
			Debug.Assert(next == head);

			// links Prevs according to Nexts:
			LinkPrevsByNexts(head);
		}

        ///// <summary>
        ///// Sorts multiple hashtables given comparer for each hashtable.
        ///// </summary>
        ///// <param name="hashtables">Collection of Ordered Hashtables. All these tables has to be of the same length.</param> 
        ///// <param name="comparers">
        ///// Array of comparers.
        ///// The number of comparers has to be the same as the number of <paramref name="hashtables"/>.
        ///// </param>
        ///// <remarks>
        ///// Sorts lexicographically all <paramref name="hashtables"/> from the first to the last one using 
        ///// <paramref name="comparers"/> successively. Changes only order of entries in <paramref name="hashtables"/>.
        ///// </remarks>
        ///// <exception cref="ArgumentNullException"><paramref name="hashtables"/> or <paramref name="comparers"/> is a <B>null</B> reference.</exception>
        //public static void Sort(ICollection<OrderedHashtable<K>>/*!*/ hashtables,
        //    IComparer<KeyValuePair<K, object>>[]/*!!*/ comparers)
        //{
        //    #region requires (hashtables && comparer && comparers.Length==hashtables.Length)

        //    if (hashtables == null)
        //        throw new ArgumentNullException("hashtables");
        //    if (comparers == null)
        //        throw new ArgumentNullException("comparers");
        //    if (hashtables.Count != comparers.Length)
        //        throw new ArgumentException(CoreResources.GetString("lengths_are_different", "hashtables", "comparers"));

        //    #endregion

        //    if (comparers.Length == 0) return;

        //    IEnumerator<OrderedHashtable<K>> iterator = hashtables.GetEnumerator();
        //    iterator.MoveNext();

        //    int count = iterator.Current.Count;
        //    Element[] heads = new Element[hashtables.Count];
        //    for (int i = 0; i < hashtables.Count; i++)
        //    {
        //        if (count != iterator.Current.Count)
        //            throw new ArgumentException(CoreResources.GetString("lengths_are_different", "hashtables[0]", String.Format("hashtables[{0}]", i)), "hashtables");

        //        heads[i] = iterator.Current.head;
        //        iterator.MoveNext();
        //    }

        //    Sort(count, heads, comparers);
        //}

		#endregion

		#region Set Operations: Difference and Intersection

        ///// <summary>
        ///// Enumerates over a collection of ordered hashtables using specified enumerator 
        ///// returning heads of visited hashtables.
        ///// </summary>
        //internal class HeadsProvider : IEnumerator                                             // GENERICS: IEnumerator<Element>
        //{
        //    private IEnumerator hashtables;                                                      // GENERICS: IEnumerator<OrderedHashtable>

        //    public HeadsProvider(IEnumerator hashtables) { this.hashtables = hashtables; }
        //    public void Reset() { hashtables.Reset(); }
        //    public bool MoveNext() { return hashtables.MoveNext(); }

        //    public object Current
        //    {
        //        get
        //        {
        //            return (hashtables.Current != null) ? ((OrderedHashtable<IntStringKey>)hashtables.Current).head : null;
        //        }
        //    }
        //}

		/// <summary>
		/// Performs diff operation on the list of this instance and the other list.
		/// </summary>
		/// <param name="op">The operation.</param>
		/// <param name="otherHead">A head of the other list.</param>
		/// <param name="comparer">A comparer.</param>
        /// <remarks>Updates only <see cref="Element.Next"/> link. <see cref="Element.Prev"/> is preserved so the operation can be eventually reverted.</remarks>
		private void SetOperation(SetOperations op, Element/*!*/ otherHead, IComparer<KeyValuePair<K, object>>/*!*/ comparer)
		{
			Debug.Assert(otherHead != null && comparer != null);

			Element other_iterator = otherHead.Next;
			Element iterator = head.Next;
			Element iterator_prev = head;

			while (iterator != head && other_iterator != otherHead)
			{
				KeyValuePair<K, object> entry = iterator.Entry;
				KeyValuePair<K, object> other_entry = other_iterator.Entry;

				int cmp = comparer.Compare(entry, other_entry);
				if (cmp > 0)
				{
					// advance the other list iterator:
					other_iterator = other_iterator.Next;
				}
				else if (cmp < 0 ^ op == SetOperations.Difference)
				{
					// marks and skips the current element in the instance list, advances iterator:
					iterator_prev.Next = iterator.Next;
					iterator.Next = null;
					iterator = iterator_prev.Next;
				}
				else
				{
					// advance this instance list iterator:
					iterator_prev = iterator;
					iterator = iterator.Next;
				}
			}

			// marks the remaining elements:
			if (op == SetOperations.Intersection)
			{
				while (iterator != head)
				{
					// marks and skips the current element in the instance list, advances iterator:
					iterator_prev.Next = iterator.Next;
					iterator.Next = null;
					iterator = iterator_prev.Next;
				}
			}
		}
		/// <summary>
		/// Retrieves the difference of this instance elemens and elements of the specified lists.
		/// </summary>
		/// <param name="op">The operation.</param>
		/// <param name="heads">The <see cref="IEnumerator"/> of heads of lists to take away from this instance.</param>
		/// <param name="comparer">The comparer of entries.</param>
		/// <param name="result">The <see cref="IDictionary"/> where to add remaining items.</param>
		internal void SetOperation(SetOperations op, IEnumerable<Element>/*!*/ heads,
			IComparer<KeyValuePair<K, object>>/*!*/ comparer, IDictionary<K, object>/*!*/ result)
		{
			Debug.Assert(heads != null && comparer != null && result != null);

			Element next, iterator;
			int count = this.Count;

			// nothing to do:
			if (count == 0) return;

			// sorts this instance list (doesn't modify Prevs and keeps list cyclic):
			head.Next = MergeSortRecursive(comparer, head, count, head.Next, out next);
			Debug.Assert(next == head);

			foreach (Element other_head in heads)
			{
				// total number of elements in diff list:
				count = (other_head != null) ? other_head.Table.Count : 0;

				// result is empty - either the list is differentiated with itself or intersected with an empty set:
				if (other_head == head && op == SetOperations.Difference || count == 0 && op == SetOperations.Intersection)
				{
					// reconstructs double linked list skipping elements marked as deleted:
					LinkNextsByPrevs(head);

					// the result is empty:
					return;
				}

				// skip operation (nothing new can be added):
				if (other_head == head && op == SetOperations.Intersection || count == 0 && op == SetOperations.Difference)
					continue;

				// sorts other_head's list (doesn't modify Prevs and keeps list cyclic):
				other_head.Next = MergeSortRecursive(comparer, other_head, count, other_head.Next, out next);
				Debug.Assert(next == other_head);

				// applies operation on the instance list and the other list:
				SetOperation(op, other_head, comparer);

				// rolls mergesort back:
				LinkNextsByPrevs(other_head);

				// instance list is empty:
				if (head.Next == head) break;
			}

			ReversePrevLinks(head);

			// adds remaining elements to a dictionary:
			iterator = head.Prev;
			while (iterator != head)
			{
				if (iterator.Next != null)
					result.Add(iterator.Key, iterator.Value);
				iterator = iterator.Prev;
			}

			ReversePrevLinks(head);

			// reconstructs double linked list skipping elements marked as deleted:
			LinkNextsByPrevs(head);
		}

        ///// <summary>
        ///// Computes the difference or intersection of specified Ordered Hashtables.
        ///// </summary>
        ///// <param name="op">The operation to be performed.</param>
        ///// <param name="hashtables">The <see cref="ICollection"/> of <see cref="PhpHashtable"/>s.</param>
        ///// <param name="comparer">The comparer used to compare entries of <paramref name="hashtables"/>.</param>
        ///// <param name="result">The dictionary where to add remaining elements.</param>
        ///// <remarks>
        ///// Preserves order of the entries in this instance. 
        ///// </remarks>
        ///// <exception cref="ArgumentNullException"><paramref name="hashtables"/> or <paramref name="comparer"/> or <paramref name="result"/> is a <B>null</B> reference.</exception>
        ///// <exception cref="ArgumentException"><paramref name="result"/> references this instance.</exception>
        //public void SetOperation(SetOperations op, ICollection<OrderedHashtable<K>>/*!*/ hashtables,
        //        IComparer<KeyValuePair<K, object>>/*!*/ comparer, IDictionary<K, object>/*!*/ result)
        //{
        //    #region Requires (hashtables && comparer && result && result!=this)

        //    if (hashtables == null)
        //        throw new ArgumentNullException("hashtables");
        //    if (comparer == null)
        //        throw new ArgumentNullException("comparers");
        //    if (result == null)
        //        throw new ArgumentNullException("result");
        //    if (result == this)
        //        throw new ArgumentException(CoreResources.GetString("argument_equals", "result", "this"));

        //    #endregion

        //    if (hashtables.Count == 0) return;

        //    SetOperation(op, EnumerateHeads(hashtables), comparer, result);
        //}

        //private IEnumerable<Element>/*!*/ EnumerateHeads(ICollection<OrderedHashtable<K>>/*!*/ hashtables)
        //{
        //    foreach (OrderedHashtable<K> hashtable in hashtables)
        //    {
        //        yield return (hashtable != null) ? hashtable.head : null;
        //    }
        //}

		#endregion

		#region Reverse, Shuffle

        ///// <summary>
        ///// Reverses an order of items in the hashtable.
        ///// </summary>
        ///// <remarks>
        ///// Changes only the prev/next references of elements contained. 
        ///// Entries are neither copied nor modified.
        ///// Affects interconnected table's items as well.
        ///// </remarks>
        //public void Reverse()
        //{
        //    this.CheckNotShared();

        //    Element iterator, next;

        //    // exchanges prev and next references in head:
        //    iterator = head.Next;
        //    head.Next = head.Prev;
        //    head.Prev = iterator;

        //    while (iterator != head)
        //    {
        //        // exchanges prev and next references:
        //        next = iterator.Next;
        //        iterator.Next = iterator.Prev;
        //        iterator.Prev = next;

        //        iterator = next;
        //    }
        //}

		/// <summary>
		/// Shuffles order of elements in the hashtable at random.
		/// </summary>
		/// <param name="generator">Some initialized random number generator.</param>
		/// <remarks>
		/// Changes only the prev/next references of elements contained. 
		/// Entries are neither copied nor modified.
		/// Affects interconnected table's items as well.
		/// </remarks>
		/// <exception cref="ArgumentNullException"><paramref name="generator"/> is a <b>null</b> reference.</exception>
		public void Shuffle(Random generator)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

            // total number of elements (interconnected table has to be taken into consideration): 
			int count = this.Count;
			if (count <= 1) return;

            this.CheckNotShared(); 
            
            int i, p, n;
			Element element;
			Element[] elements = new Element[count + 2];

			// stores references to elements into an array (0 is head, 1..count are elements, count+1 is head):
			element = head;
			for (i = 0; i <= count + 1; i++)
			{
				elements[i] = element;
				element = element.Next;
			}

			// Takes n-th element from the array at random with probability 1/i
			// and exchanges it with the one on the i-th position.
			// Thus a random permutation is formed in the second part of an array (from i to count)
			// and the set of remaining elements is stored in the first part.
			for (i = count; i > 1; i--)
			{
				n = generator.Next(i) + 1;

				element = elements[i];
				elements[i] = elements[n];
				elements[n] = element;
			}

			// connects elements into cyclic bidirectional list:
			for (p = 0, i = 1, n = 2; i <= count; i++)
			{
				elements[i].Next = elements[n++];
				elements[i].Prev = elements[p++];
			}
			head.Next = elements[1];
			head.Prev = elements[count];
		}

		#endregion

		#region Rehashing

		internal void BaseClear()
		{
			dict.Clear();
		}

        //internal void BaseRemove(K key)
        //{
        //    dict.Remove(key);
        //}

        ///// <summary>
        ///// Rehashes elements of the list associated with this table to the underlying hashtable  
        ///// using keys stored in the list for hashing.
        ///// </summary>
        ///// <param name="clear">Whether to clear the underlying hashtable before rehashing.</param>
        ///// <remarks>
        ///// <para>
        ///// Items which belongs to interconnected table as well as deleted items are skipped.
        ///// </para>
        ///// <para>
        ///// Used on tables which are in inconsistent state - keys in elements of the list 
        ///// don't correspond those in hashtable. That's why this method should not be public.
        ///// </para>    
        ///// </remarks>
        //internal void Rehash(bool clear)
        //{
        //    // clears items in the underlying hashtable, no changes to the list are made: 
        //    if (clear) Clear();

        //    // adds items stored in the list to the underlying hashtable; 
        //    // overwrites items already contained in the table:
        //    for (Element element = head.Next; element != head; element = element.Next)
        //    {
        //        // skips deleted items and items belonging to the other table:
        //        RehashElement(element);
        //    }
        //}

		///// <summary>
		///// Rehashes all elements of the list to the underlying hashtable (this or the interconnected one) 
		///// using keys stored in the list for hashing.
		///// </summary>
		///// <param name="clear">Whether to clear this hashtables before rehashing.</param>
		///// <param name="clearInterconnected">Whether to clear the interconnected hashtable before rehashing.</param>
		///// <remarks>
		///// <para>
		///// Used on tables which are in inconsistent state - keys in elements of the list 
		///// don't correspond those in hashtable. That's why this method should not be public.
		///// </para>    
		///// </remarks>
		//internal void RehashAll(bool clear,bool clearInterconnected)
		//{
		//  // clears items in underlying hashtables, no changes to the list are made: 
		//  if (clear) dict.Clear();
		//  if (clearInterconnected && interconnectedWith!=null) interconnectedWith.BaseClear();

		//  // adds items stored in the list to the underlying hashtables: 
		//  for (Element element = head.Next;element!=head;element = element.Next)
		//  {
		//    element.Table.SetElement(element.Key,element);
		//  }    
		//}

		#endregion
	}

	#endregion

	#region IntStringKey

	[Serializable]
    [DebuggerNonUserCode]
    public struct IntStringKey : IEquatable<IntStringKey>, IComparable<IntStringKey>
	{
        /// <summary>
        /// <pre>new IntStringKey( "" )</pre>
        /// </summary>
        internal readonly static IntStringKey EmptyStringKey = new IntStringKey(string.Empty);
        
        [Serializable]
        [DebuggerNonUserCode]
        public class EqualityComparer : IEqualityComparer<IntStringKey>
		{
			public static readonly EqualityComparer/*!*/ Default = new EqualityComparer();
			
			public bool Equals(IntStringKey x, IntStringKey y)
			{
				return x.ikey == y.ikey && x.skey == y.skey;
			}

			public int GetHashCode(IntStringKey x)
			{
				return x.ikey;
			}
		}

        /// <summary>
        /// Computes the hash code of the given string. The function returns the same result on all platforms.
        /// Resulting value is different using different string keys as it is in case of string.GetHashCode().
        /// </summary>
        /// <param name="s">The string key to be used to compute the hash.</param>
        /// <returns>The unique integer value corresponsing to the given string key.</returns>
        /// <remarks>Unsafe x64 implementation of String.GetHashCode(). But it returns the same results on all platforms.
        /// Phalanger needs the same results in case of compiling on one platform (and .NET version) and running on another platform.
        /// Phalanger computes the hashes during compilation time to speedup the runtime.</remarks>
        public static unsafe int StringKeyToArrayIndex(string s)
        {
            unchecked
            {
                fixed (char* str = s)
                {
                    int* numPtr = (int*)str;
                    int num = 0x15051505;
                    int num2 = num;
                    for (int i = s.Length; i > 0; i -= 4)
                    {
                        num = (((num << 5) + num) + (num >> 0x1b)) ^ numPtr[0];
                        if (i <= 2)
                        {
                            break;
                        }
                        num2 = (((num2 << 5) + num2) + (num2 >> 0x1b)) ^ numPtr[1];
                        numPtr += 2;
                    }
                    return (num + (num2 * 0x5d588b65));
                }
            }
        }

		public object Object { get { return skey ?? (object)ikey; } }

		/// <summary>
		/// Integer value iff <see cref="IsString"/> return <B>false</B>.
		/// </summary>
		public int Integer { get { return ikey; } }
        private int ikey; // Holds string hashcode if skey != null, computed using Convert.StringKeyToArrayIndex(skey).

		/// <summary>
		/// String value iff <see cref="IsString"/> return <B>true</B>.
		/// </summary>
		public string String { get { return skey; } }
		private string skey;

        public IntStringKey(object key)
        {
            ikey = ((skey = key as string) == null) ? (int)key : StringKeyToArrayIndex(skey);
        }

		public IntStringKey(int key)
		{
			ikey = key;
			skey = null;
		}

		public IntStringKey(string/*!*/ key)
		{
			Debug.Assert(key != null);

            ikey = StringKeyToArrayIndex((skey = key));// key.GetHashCode();
		}
		
        /// <summary>
        /// Initialize the IntStringKey with precomputed hashCode.
        /// </summary>
        /// <param name="key">The string key.</param>
        /// <param name="hashcode">The hashCode computed using Convert.StringKeyToArrayIndex(key) method !</param>
		internal IntStringKey(string/*!*/ key, int hashcode)
		{
			//Debug.Assert(key != null && key.GetHashCode() == hashcode);
            Debug.Assert(key != null && StringKeyToArrayIndex(key) == hashcode);
			
			this.skey = key;
			this.ikey = hashcode;
		}

		public bool IsString
		{
			get
			{
				return skey != null;
			}
		}

		public bool IsInteger
		{
			get
			{
                return skey == null;
			}
		}

		public override int GetHashCode()
		{
			return ikey;
		}

		public bool Equals(IntStringKey other)
		{
			return ikey == other.ikey && skey == other.skey;
		}

        public bool Equals(ref IntStringKey other)
        {
            return ikey == other.ikey && skey == other.skey;
        }

        public bool Equals(int ikey)
        {
            return this.ikey == ikey && this.skey == null;
        }

		public override string ToString()
		{
			return (skey == null) ? ikey.ToString() : skey;
		}

		public int CompareTo(IntStringKey other)
		{
			if (this.IsString)
			{
				if (other.IsString)
					return String.CompareOrdinal(this.skey, other.skey);
				else
					return String.CompareOrdinal(this.skey, other.ikey.ToString());
			}
			else
			{
				if (other.IsString)
					return String.CompareOrdinal(this.ikey.ToString(), other.skey);
				else
					return (this.ikey == other.ikey) ? 0 : (this.ikey < other.ikey ? -1 : +1);
			}
		}
	}
	
	#endregion

    #region IntStringKeyDictionary : IDictionary<IntStringKey>

    #endregion

    #region PhpHashtable

    /// <summary>
	/// The hashtable storing entries with <see cref="string"/> and <see cref="int"/> keys in a manner of PHP.
	/// </summary>
	[Serializable]
    [DebuggerNonUserCode]
    public class PhpHashtable : IDictionary<IntStringKey, object>, IList, IDictionary, ICloneable
	{
		#region Fields and Properties

		/// <summary>
		/// Whether this instance has been visited during recursive pass of some structure containing <see cref="PhpArray"/>s.
		/// </summary>
		/// <remarks>
		/// Must be set to <B>false</B> immediately after the pass.
		/// </remarks>
		public bool Visited { get { return visited; } set { visited = value; } }
        [NonSerialized]
        private bool visited = false;

		/// <summary>
		/// A field used by <see cref="RecursiveEnumerator"/> to store an enumerator of respective recursion level.
		/// </summary>
        [NonSerialized]
        private OrderedDictionary.Enumerator recursiveEnumerator;

		/// <summary>
		/// Ordered hashtable where integers are stored.
		/// </summary>
		/// <remarks>
		/// Expose the table to item getters on <see cref="PhpArray"/> to make them a little bit faster.
		/// </remarks>
        internal OrderedDictionary/*!*/ table;

        /// <summary>
        /// Max integer key in the array.
        /// Returns <c>-1</c> if there are no integer keys.
        /// </summary>
        public int MaxIntegerKey
        {
            get
            {
                if (nextNewIndex < 0)
                    RefreshMaxIntegerKeyInternal();

                return nextNewIndex - 1;
            }
        }
        /// <summary>
        /// Index for next new element when key is not specified.
        /// </summary>
        private int nextNewIndex = 0;

        /// <summary>
		/// Retrieves the number of items with integer keys in this instance.
		/// </summary>
		public int IntegerCount { get { return intCount; } }
		private int intCount = 0;

		/// <summary>
		/// Retrieves the number of items with string keys in this instance.
		/// </summary>
		public int StringCount { get { return stringCount; } }
		private int stringCount = 0;

        #region active enumerators

        /// <summary>
        /// Callback methods for entry deletion event.
        /// </summary>
        [NonSerialized]
        internal OrderedDictionary.Enumerator activeEnumerators = null;

        /// <summary>
        /// Add given <paramref name="enumerator"/> into <see cref="activeEnumerators"/> list.
        /// </summary>
        /// <param name="enumerator">New enumerator.</param>
        internal void RegisterEnumerator(OrderedDictionary.Enumerator/*!*/enumerator)
        {
            Debug.Assert(enumerator != null, "Argument null!");
            Debug.Assert(enumerator.next == null, "Enumerator already enlisted somewhere!");
            Debug.Assert(enumerator.table == this.table, "Enumerator was not associated with this PhpHashtable!");

            enumerator.next = this.activeEnumerators;
            this.activeEnumerators = enumerator;
        }

        /// <summary>
        /// Remove given <paramref name="enumerator"/> from <see cref="activeEnumerators"/> list.
        /// </summary>
        /// <param name="enumerator"><see cref="OrderedDictionary.Enumerator"/> to be removed from the list of active enumerators.</param>
        internal void UnregisterEnumerator(OrderedDictionary.Enumerator/*!*/enumerator)
        {
            Debug.Assert(enumerator != null, "Argument null!");
            Debug.Assert(enumerator.table == this.table, "Enumerator was not associated with this PhpHashtable!");

            if (this.activeEnumerators == enumerator)
            {
                this.activeEnumerators = enumerator.next; // remove the first item from the list, most recent case
            }
            else
            {
                for (var e = this.activeEnumerators; e != null; e = e.next)
                    if (e.next == enumerator)
                    {
                        e.next = enumerator.next;
                        break;
                    }
            }
        }

        #endregion

        #endregion

        #region EnsureWritable

        /// <summary>
        /// Ensures the internal <see cref="OrderedDictionary"/> will be writable (not shared).
        /// </summary>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EnsureWritable()
        {
            if (table.IsShared)
                Unshare();
        }

        private void Unshare()
        {
            Debug.Assert(table.IsShared);

            this.table.Unshare();

            var oldowner = this.table.owner;

            this.table = new OrderedDictionary(this, table);
            this.table._deep_copy_inplace(oldowner, this);   // deep copy values, replace references to original array into this

            for (var e = this.activeEnumerators; e != null; e = e.next)
                e.TableChanged();
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <c>PhpHashtable</c> class.
        /// </summary>
        public PhpHashtable() : this(0) { }

		/// <summary>
		/// Initializes a new instance of the <c>PhpHashtable</c> class.
		/// </summary>
		/// <param name="capacity">Initial capacity.</param>
		public PhpHashtable(int capacity)
		{
            table = new OrderedDictionary(this, capacity);
		}

		/// <summary>
		/// Initializes a new instance of the <c>PhpHashtable</c> class filled by values from specified array. 
		/// </summary>
		/// <param name="values">Values to be added.</param>
		/// <remarks>
		/// Adds all pairs key-value where the value is an item of <v>values</v> array 
		/// and the key is its index in the array.
		/// </remarks>
		public PhpHashtable(Array values) : this(values, 0, values.Length) { }

		/// <summary>
		/// Initializes a new instance of the <c>PhpHashtable</c> class filled by values from specified array. 
		/// </summary>
		/// <param name="values">Values to be added.</param>
		/// <param name="index">The starting index.</param>
		/// <param name="length">The number of items to add.</param>
		/// <remarks>
		/// Adds at most <c>length</c> pairs key-value where the value is an item of <v>values</v> array 
		/// and the key is its index in the array starting from the <c>index</c>.
		/// </remarks>
		public PhpHashtable(Array values, int index, int length)
			: this(length)
		{
			int end = index + length;
			int max = values.Length;
			if (end > max) end = max;

			for (int i = index; i < end; i++)
				Add(i, values.GetValue(i));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PhpHashtable"/> class filled by values from specified array.
		/// </summary>
		/// <param name="values">An array of values to be added to the table.</param>
		/// <param name="start">An index of the first item from <paramref name="values"/> to add.</param>
		/// <param name="length">A number of items to add.</param>
		/// <param name="value">A value to be filtered.</param>
		/// <param name="doFilter">Wheter to add all items but <paramref name="value"/> (<b>true</b>) or 
		/// all items with the value <paramref name="value"/> (<b>false</b>).</param>
		public PhpHashtable(int[] values, int start, int length, int value, bool doFilter)
			: this(length)
		{
			int end = start + length;
			int max = values.Length;
			if (end > max) end = max;

			if (doFilter)
			{
				for (int i = start; i < end; i++) if (values[i] != value) Add(i, values[i]);
			}
			else
			{
				for (int i = start; i < end; i++) if (values[i] == value) Add(i, value);
			}
		}

        /// <summary>
        /// Creates PhpHashtable that shares internal <see cref="table"/> with another array.
        /// </summary>
        /// <param name="array">The table to be shared.</param>
        /// <param name="preserveMaxInt">True to copy the <see cref="PhpHashtable.MaxIntegerKey"/> from <paramref name="array"/>.
        /// Otherwise the value will be recomputed when needed.</param>
        public PhpHashtable(PhpHashtable/*!*/array, bool preserveMaxInt)
        {
            this.table = array.table.Share();
            this.nextNewIndex = preserveMaxInt ? (array.nextNewIndex) : (-1); // TODO: (preserveMaxInt || array.table.DOES_NOT_HAVE_ANY_DELETIONS)
            this.intCount = array.IntegerCount;
            this.stringCount = array.StringCount;
        }

		#endregion

		#region Inner class: RecursiveEnumerator

		/// <summary>
		/// Recursively enumerates <see cref="PhpHashtable"/>.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Enumerator starts enumeration with a <see cref="PhpHashtable"/> specified in its constructor and 
		/// enumerates its items by instance of <see cref="IDictionaryEnumerator"/> retrieved via 
		/// <see cref="PhpHashtable.GetEnumerator"/>. This enumerator is supposed to be unbreakable.
		/// If an enumerated item value is <see cref="PhpHashtable"/>  (or <see cref="PhpReference"/> and its 
		/// <see cref="PhpReference.value"/> is <see cref="PhpHashtable"/> and <see cref="FollowReferences"/>
		/// property is <B>true</B>) then this item is returned by Current and Entry
		/// like any other item but the enumerator continues with enumeration of that item when it is moved by
		/// <see cref="MoveNext"/>. The <see cref="Level"/> of recursion is increased and the previous hashtable
		/// is pushed in the internal stack. When enumerator finishes the enumeration of the current level hashtable
		/// and the level of recursion is not zero it pops hashtable stored in the stack and continues with
		/// enumeration on the item immediately following the item which caused the recursion.
		/// </para>
		/// <para>
		/// Before the level of recursion is raised enumerator checks whether the next level hashtable
		/// was not already visited by any recursive enumerator. If that is the case such hashtable is skipped to
		/// prevent infinite recursion. Note, that you should not use more than one <see cref="RecursiveEnumerator"/>
		/// on the same <see cref="PhpHashtable"/>. This is not checked automatically but it is left to the user
		/// to avoid such usage. One can check whether the current item will cause a recursion by inspecting
		/// <see cref="InfiniteRecursion"/> property.
		/// </para>
		/// <para>
		/// <B>Warning</B>: Enumerator should be disposed!
		/// It temporarily stores information to each hashtable pushed 
		/// on the stack. This information is needed to prevent the recursion and it is cleared immediately after
		/// the return from the respective level of recursion (when popping a hashtable).
		/// Hence, if enumeration ends when the level of recursion is greater than zero (i.e. stack is non-empty),  
		/// some information may remain in visited arrays and the next enumeration will skip them.
		/// That's why it is recommanded to call <see cref="Dispose"/> method whenever an enumeration ends using
		/// the following pattern:
		/// <code>
		///   using(PhpHashtable.RecursiveEnumerator e = ht.GetRecursiveEnumerator())
		///   {
		///     while (e.MoveNext()) 
		///     { 
		///       /* do something useful */
		///     }
		///   }
		/// </code>
		/// </para>
		/// <para>
		/// Enumerator is unbreakable (i.e. enumerated hashtables may be changed while enumerating them).
		/// </para>
		/// </remarks>
		public sealed class RecursiveEnumerator : IEnumerator<KeyValuePair<IntStringKey, object>>, IDictionaryEnumerator, IDisposable
		{
			#region Fields and Properties

			/// <summary>
			/// A stack for visited arrays. The currently enumerated array is not there.
			/// </summary>
			private Stack<PhpHashtable>/*!*/ stack;

			/// <summary>
			/// The currently enumerated array.
			/// </summary>
			public PhpHashtable/*!*/ CurrentTable { get { return currentTable; } }
			private PhpHashtable/*!*/ currentTable;

			/// <summary>
			/// The current level hashtable enumerator.
			/// </summary>
            private OrderedDictionary.Enumerator/*!*/ current;

			/// <summary>
			/// The level of recursion starting from zero (the top level).
			/// </summary>
			public int Level { get { return stack.Count; } }

			/// <summary>
			/// Whether to follow <see cref="PhpReference"/>s when resolving next level of recursion.
			/// </summary>
			public bool FollowReferences { get { return followReferences; } set { followReferences = value; } }
			private bool followReferences = false;

            /// <summary>
            /// Wheter the enumerator is used to read the array items only.
            /// </summary>
            private readonly bool readsOnly;

			/// <summary>
			/// Whether the current value causes infinite recursion.
			/// </summary>
			/// <exception cref="NullReferenceException">If enumerator has been disposed.</exception>
			public bool InfiniteRecursion
			{
				get
				{
					object val = current.Current.Value;

					// dereferences PHP reference if required:
					if (followReferences)
						val = PhpVariable.Dereference(val);

					// checks whether the value is visited array: 
					PhpHashtable array = val as PhpHashtable;
					return array != null && array.recursiveEnumerator != null;
				}
			}

			#endregion

			#region Constructors

			/// <summary>
			/// Creates an instance of <see cref="RecursiveEnumerator"/>.
			/// </summary>
            internal RecursiveEnumerator(PhpHashtable/*!*/ array, bool followReferences, bool readsOnly)
			{
				Debug.Assert(array != null);

                this.stack = new Stack<PhpHashtable>();
				this.followReferences = followReferences;
                this.readsOnly = readsOnly;

                // the array may be accessed for writing, ensure its child items/arrays are ready:
                if (!readsOnly)
                    array.EnsureWritable();

				// store the current array and the current enumerator:
				this.currentTable = array;
				this.current = currentTable.GetPhpEnumerator();
			}

			#endregion

			#region ReturnFromRecursion, ReturnFromRecursionAtEnd

			/// <summary>
			/// Returns from recursion on a specified level.
			/// </summary>
			/// <param name="targetLevel">The level where to continue with enumeration.</param>
			/// <exception cref="NullReferenceException">If enumerator has been disposed.</exception>
			private void ReturnFromRecursion(int targetLevel)
			{
				Debug.Assert(targetLevel >= 0);

				while (stack.Count > targetLevel)
				{
                    // leave and Dispose the current array (visited = false):
                    current.Dispose();
					currentTable.recursiveEnumerator = null;

                    // returns back:
					currentTable = stack.Pop();
					current = currentTable.recursiveEnumerator;
				}
			}

			/// <summary>
			/// Returns from recursion while the current enumerator is at the end of the list it enumerates.
			/// </summary>
			/// <returns>Whether we are not at the definite end of enumeration.</returns>
			private bool ReturnFromRecursionAtEnd()
			{
				while (current.AtEnd)
				{
					// leave and Dispose the current array (visited = false):
                    current.Dispose();
                    currentTable.recursiveEnumerator = null;

					// the top list (real end):
					if (stack.Count == 0) return false;

					// returns back:
					currentTable = stack.Pop();
					current = currentTable.recursiveEnumerator;
				}
				return true;
			}

			#endregion

			#region IDictionaryEnumerator Members

			/// <summary>
			/// The current key.
			/// </summary>
			/// <exception cref="NullReferenceException">If enumerator has been disposed.</exception>
			object IDictionaryEnumerator.Key
			{
				get
				{
					// skips deleted items (if any) with possible return from recursion:
					ReturnFromRecursionAtEnd();
					return current.Current.Key.Object;
				}
			}

			/// <summary>
			/// The current value.
			/// </summary>
			/// <exception cref="NullReferenceException">If enumerator has been disposed.</exception>
			object IDictionaryEnumerator.Value
			{
				get
				{
					// skips deleted items (if any) with possible return from recursion:
					ReturnFromRecursionAtEnd();
					return current.Current.Value;
				}
			}

			/// <summary>
			/// The current entry.
			/// </summary>
			/// <exception cref="NullReferenceException">If enumerator has been disposed.</exception>
			DictionaryEntry IDictionaryEnumerator.Entry
			{
				get
				{
					// skips deleted items (if any) with possible return from recursion:
					ReturnFromRecursionAtEnd();
					return new DictionaryEntry(current.Current.Key, current.Current.Value);
				}
			}

			#endregion

			#region IEnumerator Members

			/// <summary>
			/// Returns the current entry.
			/// </summary>
			/// <exception cref="NullReferenceException">If enumerator has been disposed.</exception>
			object IEnumerator.Current
			{
				get
				{
					return ((IDictionaryEnumerator)this).Entry;
				}
			}

			/// <summary>
			/// Resets enumerator i.e. returns from recursion to the top level and resets top level enumerator.
			/// </summary>
			/// <exception cref="NullReferenceException">If enumerator has been disposed.</exception>
			public void Reset()
			{
				ReturnFromRecursion(0);
				current.Reset();
			}

			/// <summary>
			/// Moves to the next element recursively.
			/// </summary>
			/// <returns>Whether an enumeration has ended.</returns>
			/// <exception cref="NullReferenceException">If enumerator has been disposed.</exception>
			public bool MoveNext()
			{
				PhpHashtable array;

				object value = current.Current.Value;

				// moves to the next item in the current level:
				current.MoveNext();

				// dereferences the value if following references:
				if (followReferences)
					value = PhpVariable.Dereference(value);

				if ((array = value as PhpHashtable) != null)
				{
                    // the array may be accessed for writing, ensure its child items/arrays are ready:
                    if (!readsOnly)
                        array.EnsureWritable();

					// mark the current table as visited and store there the current enumerator:
					currentTable.recursiveEnumerator = current;

					// skips arrays which are already on the stack (prevents infinite recursion)  
					// and those which doesn't contain any item (optimization):
					if (array.recursiveEnumerator == null && array.Count > 0)
					{
						// stores the current level:
						stack.Push(currentTable);

						// next level of recursion:
                        currentTable = array;

						// creates a new enumerator (visited = true):
						current = currentTable.GetPhpEnumerator();

						// starts enumerating next level:
						current.MoveNext();
					}
				}

				// check whether we are at the definite end:
				return ReturnFromRecursionAtEnd();
			}

			#endregion

			#region IDisposable Members

			/// <summary>
			/// Clears information stored in each array on the stack.
			/// </summary>
			public void Dispose()
			{
				// if not disposed yet:
				if (stack != null)
				{
					ReturnFromRecursion(0);

                    // cleanup the last enumerator:
                    if (currentTable != null) currentTable.recursiveEnumerator = null;
                    currentTable = null;

                    if (current != null) current.Dispose();
                    current = null;
					
                    stack = null;
				}
			}

			#endregion

			#region IEnumerator<KeyValuePair<IntStringKey,object>> Members

			public KeyValuePair<IntStringKey, object> Current
			{
				get
				{
					// skips deleted items (if any) with possible return from recursion:
					ReturnFromRecursionAtEnd();
					return current.Current;
				}
			}

			#endregion
		}

		#endregion

		#region PHP Enumeration

        /// <summary>
        /// Throw an exception if this instance is not <see cref="PhpArray"/> or <see cref="PhpHashtable"/>.
        /// This should avoid using features that are not available in special derived arrays yet.
        /// </summary>
        /// <exception cref="NotImplementedException">This instance does not support the operation yet. Method has to be marked as virtual, and functionality has to be implemented in derived type.</exception>
        [Conditional("DEBUG")]
        protected void ThrowIfNotPhpArrayHelper()
        {
            if (this.GetType() == typeof(PhpHashtable) || this.GetType() == typeof(PhpArray))
                return;

            throw new NotImplementedException();
        }

		/// <summary>
		/// Retrieves a recursive enumerator of this instance.
		/// </summary>
		/// <param name="followReferences">Whether <see cref="PhpReference"/>s are followed by recursion.</param>
        /// <param name="readOnly">True if the array items will be read only. Lazy copy is not necessary.</param>
		/// <returns>The <see cref="RecursiveEnumerator"/>.</returns>
		public RecursiveEnumerator/*!*/ GetRecursiveEnumerator(bool followReferences, bool readOnly)
		{
            ThrowIfNotPhpArrayHelper();
            return new RecursiveEnumerator(this, followReferences, readOnly);
		}

        ///// <summary>
        ///// Retrieves a recursive enumerator of this instance.
        ///// </summary>
        ///// <returns>The <see cref="RecursiveEnumerator"/> not following PHP references.</returns>
        //public RecursiveEnumerator/*!*/ GetRecursiveEnumerator()
        //{
        //    return new RecursiveEnumerator(this, false, false);
        //}

        public OrderedDictionary.Enumerator/*!*/ GetPhpEnumerator()
		{
            ThrowIfNotPhpArrayHelper();
            return new OrderedDictionary.Enumerator(this, true); //(IPhpEnumerator)table.GetEnumerator();
		}

        public OrderedDictionary.Enumerator/*!*/ GetBaseEnumerator()
		{
            ThrowIfNotPhpArrayHelper();
            return new OrderedDictionary.Enumerator(this, true); //table.GetEnumerator();
		}

        /// <summary>
        /// Get fast enumerator structure to be used internally.
        /// </summary>
        /// <returns></returns>
        public OrderedDictionary.FastEnumerator GetFastEnumerator()
        {
            ThrowIfNotPhpArrayHelper();
            return table.GetFastEnumerator();
        }

		#endregion

		#region IEnumerable<KeyValuePair<IntStringKey, object>> Members

		public IEnumerator<KeyValuePair<IntStringKey, object>>/*!*/ GetEnumerator()
		{
            if (this.Count == 0)
                return OrderedDictionary.EmptyEnumerator.SingletonInstance;

            return new OrderedDictionary.Enumerator(this, true); //table.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
            if (this.Count == 0)
                return OrderedDictionary.EmptyEnumerator.SingletonInstance;

            return new OrderedDictionary.Enumerator(this, true); //(IEnumerator)table.GetEnumerator();
		}

		#endregion

		#region ICollection Members

		/// <summary>Retrieves the number of items in this instance.</summary>
		public virtual int Count { get { return table.Count; } }

		/// <summary>This property is always false.</summary>
		public bool IsSynchronized { get { return false; } }

		/// <summary>This property always refers to this instance.</summary>
		public object SyncRoot { get { return table.SyncRoot; } }

		/// <summary>
		/// Copies the <see cref="PhpHashtable"/> or a portion of it to a one-dimensional array.
		/// </summary>
		/// <param name="array">The one-dimensional array.</param>
		/// <param name="index">The zero-based index in array at which copying begins.</param>
		public void CopyTo(Array/*!*/ array, int index)
		{
			((ICollection)table).CopyTo(array, index);
		}

		#endregion

		#region IDictionary Members

        #region IDictionaryAdapter

        [Serializable]
        public class IDictionaryAdapter : IDictionaryEnumerator
        {
            #region Fields

            /// <summary>
            /// Currently pointed element.
            /// </summary>
            private OrderedDictionary.FastEnumerator enumerator;

            #endregion

            #region Construction

            public IDictionaryAdapter(PhpHashtable/*!*/table)
            {
                Debug.Assert(table != null);
                this.enumerator = table.GetFastEnumerator();
            }

            #endregion

            #region IDictionaryEnumerator Members

            public DictionaryEntry Entry
            {
                get { return new DictionaryEntry(Key, Value); }
            }

            public object Key
            {
                get
                {
                    return this.enumerator.CurrentKey.Object;
                }
            }

            public object Value
            {
                get
                {
                    return this.enumerator.CurrentValue;
                }
            }

            #endregion

            #region IEnumerator Members

            public object Current
            {
                get { return this.Entry; }
            }

            public bool MoveNext()
            {
                return this.enumerator.MoveNext();
            }

            public void Reset()
            {
                this.enumerator.Reset();
            }

            #endregion
        }

        #endregion

        /// <summary>This property is always false.</summary>
		public bool IsFixedSize { get { return false; } }

        /// <summary>This property is always false.</summary>
		public bool IsReadOnly { get { return false; } }

		/// <summary>
		/// Returns an enumerator which iterates through values in this instance in order as they were added in it.
		/// </summary>
		/// <returns>The enumerator.</returns>
		IDictionaryEnumerator/*!*/ IDictionary.GetEnumerator()
		{
            if (this.Count == 0)
                return OrderedDictionary.EmptyEnumerator.SingletonInstance;

            return new IDictionaryAdapter(this); // new GenericDictionaryAdapter<object, object>(GetDictionaryEnumerator(), false);
		}

        //private IEnumerator<KeyValuePair<object, object>>/*!*/ GetDictionaryEnumerator()
        //{
        //    foreach (KeyValuePair<IntStringKey, object> entry in table)
        //    {
        //        yield return new KeyValuePair<object, object>(entry.Key.Object, entry.Value);
        //    }
        //}

		/// <summary>
		/// Removes all elements from this instance.
		/// </summary>
		public virtual void Clear()
		{
            this.EnsureWritable();

			table.Clear();
		}

		/// <summary>
		/// Determines whether an element with the specified key is in this instance.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>Whether an element with the <paramref name="key"/> key is in the table.</returns>
		/// <exception cref="InvalidCastException">The <paramref name="key"/> is neither <see cref="int"/> nor <see cref="string"/>.</exception>
		public bool Contains(object key)
		{
            return this.ContainsKey(new IntStringKey(key));
		}

		/// <summary>
		/// Adds an entry into the table at its logical end. 
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <exception cref="ArgumentException">An element with the same key already exists in this instance.</exception>
		/// <exception cref="InvalidCastException">The <paramref name="key"/> is neither <see cref="int"/> nor not null <see cref="string"/>.</exception>
		public void Add(object key, object value)
		{
            ThrowIfNotPhpArrayHelper();
			this.Add(new IntStringKey(key), value);
		}

		/// <summary>
		/// Gets or sets a value associated with a key.
		/// </summary>
		/// <remarks>If the key doesn't exist in table the new entry is added.</remarks>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is a null reference.</exception>
		/// <exception cref="InvalidCastException">The <paramref name="key"/> is neither <see cref="int"/> nor not null <see cref="string"/>.</exception>
		public object this[object key]
		{
			get
			{
				return this[new IntStringKey(key)];
			}
			set
			{
				this[new IntStringKey(key)] = value;
			}
		}

		/// <summary>
		/// Removes an entry having the specified key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <exception cref="InvalidCastException">The <paramref name="key"/> is neither <see cref="int"/> nor not null <see cref="string"/>.</exception>
		public void Remove(object key)
		{
			Remove(new IntStringKey(key));
		}

		ICollection/*!*/ IDictionary.Keys
		{
			get
			{
				if (_keys == null) _keys = new KeyCollection(this);
				return _keys;
			}
		}
		[NonSerialized]
		private KeyCollection _keys;

		ICollection/*!*/ IDictionary.Values
		{
			get
			{
				return (ICollection)table.Values;
			}
		}

		#region Inner class: KeyCollection

		[Serializable]
		public class KeyCollection : ICollection
		{
			private readonly PhpHashtable/*!*/ hashtable;

			internal KeyCollection(PhpHashtable/*!*/ hashtable)
			{
				this.hashtable = hashtable;
			}

			#region ICollection Members

			public int Count { get { return hashtable.Count; } }

			public bool IsSynchronized { get { return false; } }

			public object SyncRoot { get { return this; } }

			void ICollection.CopyTo(Array/*!*/ array, int index)
			{
				ArrayUtils.CheckCopyTo(array, index, hashtable.Count);

				foreach (KeyValuePair<IntStringKey, object> entry in hashtable)
					array.SetValue(entry.Key.Object, index++);
			}

			#endregion

			#region IEnumerable Members

			IEnumerator IEnumerable.GetEnumerator()
			{
				foreach (KeyValuePair<IntStringKey, object> pair in hashtable)
					yield return pair.Key.Object;
			}

			#endregion
		}

		#endregion
        
		#endregion

		#region IList Members

		/// <summary>
		/// Adds an entry into the table at its logical end. The key is generated automatically.
		/// </summary>
		/// <param name="value">The value to be added.</param>
		/// <return>
		/// 1 if the entry has been added, 0 otherwise. Note, this differs from <see cref="IList.Add"/>
		/// because <see cref="PhpHashtable"/> doesn't support fast retrieval of the element's index.
		/// </return>
		/// <remarks>
		/// The key will be the maximal value of an integer key ever added into this instance plus one
		/// provided the result of addition fits into an 32-bit integer. Otherwise, the entry is not added
		/// and <b>false</b> is returned.
		/// </remarks>
		[Emitted]
		public int Add(object value)
		{
			//if (MaxIntegerKey < int.MaxValue)
			{
                this.EnsureWritable();

                if (nextNewIndex < 0) RefreshMaxIntegerKeyInternal();
                AddToEnd(value);
				return 1;
			}
			//return 0;
		}

		public void RemoveAt(int index)
		{
			throw new NotImplementedException();
		}

		public void Insert(int index, object value)
		{
			throw new NotImplementedException();
		}

		public int IndexOf(object value)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IDictionary<IntStringKey,object> Members

		public void Add(IntStringKey key, object value)
		{
            this.EnsureWritable();

			table.Add(key, value);
			KeyAdded(ref key);
		}

		public bool ContainsKey(IntStringKey key)
		{
            ThrowIfNotPhpArrayHelper();
			return table.ContainsKey(key);
		}

		public virtual bool Remove(IntStringKey key)
		{
            //if (key.Integer == this.nextNewIndex - 1)
            //{
            //    // copy of this array has to find new max int
            //}

            this.EnsureWritable();
            return this.table._del_key_or_index(ref key, this.activeEnumerators);
		}

		public bool TryGetValue(IntStringKey key, out object value)
		{
            ThrowIfNotPhpArrayHelper();
			return table.TryGetValue(key, out value);
		}

		public ICollection<IntStringKey>/*!*/ Keys
		{
            get { ThrowIfNotPhpArrayHelper(); return table.Keys; }
		}

		public ICollection<object>/*!*/ Values
		{
            get { ThrowIfNotPhpArrayHelper(); return table.Values; }
		}

		#endregion

		#region ICollection<KeyValuePair<IntStringKey,object>> Members

		public void Add(KeyValuePair<IntStringKey, object> item)
		{
            ThrowIfNotPhpArrayHelper();

            this.EnsureWritable();

			table.Add(item.Key, item.Value);
			KeyAdded(item.Key);
		}

		public bool Contains(KeyValuePair<IntStringKey, object> item)
		{
			return ((ICollection<KeyValuePair<IntStringKey, object>>)table).Contains(item);
		}

		public void CopyTo(KeyValuePair<IntStringKey, object>[] array, int arrayIndex)
		{
            table.CopyTo(array, arrayIndex);
		}

		public bool Remove(KeyValuePair<IntStringKey, object> item)
		{
            ThrowIfNotPhpArrayHelper();

            this.EnsureWritable();

			return ((ICollection<KeyValuePair<IntStringKey, object>>)table).Remove(item);
		}

		#endregion

		#region Specific Members: Add, Prepend, this[], Remove, RemoveLast, RemoveFirst, AddRange

        /// <summary>
        /// Simple wrapper to allow call KeyAdded without ref.
        /// </summary>
        /// <param name="key"></param>
        private void KeyAdded(IntStringKey key)
        {
            KeyAdded(ref key);
        }

        /// <summary>
        /// Called when new item is added into the collection. It just updates the <see cref="stringCount"/> or <see cref=" intCount"/> and <see cref="nextNewIndex"/>.
        /// </summary>
        /// <param name="key"></param>
		internal void KeyAdded(ref IntStringKey key)
		{
			if (key.IsInteger)
			    KeyAdded(key.Integer);
			else
                KeyAdded(key.String);
		}

		internal void KeyAdded(int key)
		{
            if (nextNewIndex < 0) RefreshMaxIntegerKeyInternal();
            if (key >= nextNewIndex) nextNewIndex = key + 1;
			++intCount;
		}

		private void KeyAdded(string key)
		{
			++stringCount;
		}

		#region Contains

		/// <summary>
		/// Determines whether an element with the specified key is in this instance.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>Whether an element with the <paramref name="key"/> key is in the table.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="key"/> is a <B>null</B> reference.</exception>
		public bool ContainsKey(string key)
		{
			return table.ContainsKey(new IntStringKey(key));
		}

		/// <summary>
		/// Determines whether an element with the specified key is in this instance.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>Whether an element with the <paramref name="key"/> key is in the table.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="key"/> is a <B>null</B> reference.</exception>
		public bool ContainsKey(int key)
		{
			return table.ContainsKey(new IntStringKey(key));
		}

		#endregion

        #region Add, AddToEnd

        /// <summary>
        /// Add an item onto the end of this array.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        /// <remarks>This method is supposed to be called on newly created arrays. Several checks are not performed to enhance performance of arrays initialization.</remarks>
        [Emitted]
        public void AddToEnd(object value)
        {
            Debug.Assert(nextNewIndex >= 0, "This method is supposed to be called on newly created arrays which have [nextNewIndex] field initialized!");
            Debug.Assert(!this.table.IsShared, "This method is supposed to be called on newly created arrays which cannot be shared!");
            Debug.Assert(this.GetType() == typeof(PhpArray), "This method is not supposed to be called on PHpArray's inherited class!");

            table._add_last(nextNewIndex++, value);
            ++intCount;
        }

		/// <summary>
		/// Adds an entry into the table at its logical end. 
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <exception cref="ArgumentException">An element with the same key already exists in this instance.</exception>
		public void Add(int key, object value)
		{
            this.EnsureWritable();

			table.Add(new IntStringKey(key), value);
			KeyAdded(key);
		}

		/// <summary>
		/// Adds an entry into the table at its logical end. 
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is a null reference.</exception>
		/// <exception cref="ArgumentException">An element with the same key already exists in this instance.</exception>
		public void Add(string key, object value)
		{
            this.EnsureWritable();

			table.Add(new IntStringKey(key), value);
			KeyAdded(key);
		}

		#endregion

		#region Prepend

		/// <summary>
		/// Adds an entry into the table at its logical beginning. 
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <exception cref="ArgumentException">An element with the same key already exists in this instance.</exception>
		public void Prepend(string key, object value)
		{
            this.EnsureWritable();

            this.table._add_first(new IntStringKey(key), value);
			KeyAdded(key);
		}

		/// <summary>
		/// Adds an entry into the table at its logical beginning. 
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <exception cref="ArgumentException">An element with the same key already exists in this instance.</exception>
		public void Prepend(int key, object value)
		{
            this.EnsureWritable();

            this.table._add_first(new IntStringKey(key), value);
			KeyAdded(key);
		}

		/// <summary>
		/// Adds an entry into the table at its logical beginning. 
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <exception cref="ArgumentException">An element with the same key already exists in this instance.</exception>
		public void Prepend(IntStringKey key, object value)
		{
            this.EnsureWritable();

            this.table._add_first(key, value);
			KeyAdded(ref key);
		}

		/// <summary>
		/// Adds an entry into the table at its logical beginning. 
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <exception cref="ArgumentException">An element with the same key already exists in this instance.</exception>
		/// <exception cref="InvalidCastException">The <paramref name="key"/> is neither <see cref="int"/> nor <see cref="string"/>.</exception>
        public void Prepend(object key, object value)
        {
            this.EnsureWritable();

            IntStringKey iskey;
            this.table._add_first((iskey = new IntStringKey(key)), value);
            KeyAdded(ref iskey);
        }

		#endregion

		#region Remove, RemoveFirst, RemoveLast

		//  NOTE:
		//  - RemoveLast/RemoveFirst returns removed entry while Remove does not.
		//   This is because a caller of RemoveLast/RemoveFirst knows neither a key nor a value while
		//   a caller of Remove knows at least a key.

        ///// <summary>
        ///// Removes an entry having the specified <see cref="string"/> key.
        ///// </summary>
        ///// <param name="key">The key.</param>
        //public virtual void Remove(int key)
        //{
        //    this.EnsureWritable();
        //    var iskey = new IntStringKey(key);
        //    table._del_key_or_index(ref iskey, this.onDeleteEntry);
        //}

        ///// <summary>
        ///// Removes an entry having the specified <see cref="int"/> key.
        ///// </summary>
        ///// <param name="key">The key.</param>
        ///// <exception cref="ArgumentNullException"><paramref name="key"/> is a null reference.</exception>
        //public virtual void Remove(string key)
        //{
        //    this.EnsureWritable();
        //    var iskey = new IntStringKey(key);
        //    table._del_key_or_index(ref iskey, this.onDeleteEntry);
        //}

		/// <summary>
		/// Removes the last entry of the array and returns it.
		/// </summary>
		/// <returns>The last entry of the array.</returns>
		/// <exception cref="InvalidOperationException">The table is empty.</exception>
		public KeyValuePair<IntStringKey, object> RemoveLast()
		{
            this.EnsureWritable();
            return table._remove_last(this.activeEnumerators);
		}

		/// <summary>
		/// Removes the first entry of the array and returns it.
		/// </summary>
		/// <returns>The first entry of the array.</returns>
		/// <exception cref="InvalidOperationException">The table is empty.</exception>
		public KeyValuePair<IntStringKey, object> RemoveFirst()
		{
            this.EnsureWritable();
            return table._remove_first(this.activeEnumerators);
		}

		#endregion

		#region this[], TryGetValue

		/// <summary>
		/// Gets or sets a value associated with a key.
		/// </summary>
		/// <param name="key">The <see cref="String"/> key.</param>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is a null reference.</exception>
		/// <remarks>If the key doesn't exist in table the new entry is added.</remarks>
		public object this[string key]
		{
			get
			{
				return table[new IntStringKey(key)];
			}
			set
			{
                this.EnsureWritable();

				table[new IntStringKey(key)] = value;
				KeyAdded(key);
			}
		}

		/// <summary>
		/// Gets or sets a value associated with a key.
		/// </summary>
		/// <param name="key">The <see cref="Int32"/> key.</param>
		/// <remarks>If the key doesn't exist in table the new entry is added.</remarks>
		public object this[int key]
		{
			get
			{
				return table[new IntStringKey(key)];
			}
			set
			{
                this.EnsureWritable();

				table[new IntStringKey(key)] = value;
				KeyAdded(key);
			}
		}

		/// <summary>
		/// Gets or sets a value associated with a key.
		/// </summary>
		/// <param name="key">The <see cref="Int32"/> key.</param>
		/// <remarks>If the key doesn't exist in table the new entry is added.</remarks>
		public object this[IntStringKey key]
		{
			get
			{
				return table[key];
			}
			set
			{
                this.EnsureWritable();

				table[key] = value;
				KeyAdded(ref key);
			}
		}

		public bool TryGetValue(string key, out object value)
		{
			return table.TryGetValue(new IntStringKey(key), out value);
		}

		public bool TryGetValue(int key, out object value)
		{
			return table.TryGetValue(new IntStringKey(key), out value);
		}

		public bool TryGetValue(object key, out object value)
		{
			return table.TryGetValue(new IntStringKey(key), out value);
		}

        #endregion

		#endregion

        #region Clone, InplaceDeepCopy, AddTo, CopyValuesTo

        /// <summary>
		/// Creates a shallow copy of the hashtable.
		/// </summary>
		/// <returns>A copy of the hashtable.</returns>
		public virtual object Clone()
		{
            var clone = new PhpHashtable(this, true);
            clone.EnsureWritable();
            return clone;
		}

        /// <summary>
		/// Replaces values in the table with their deep copies.
		/// </summary>
        public void InplaceDeepCopy()
		{
            ThrowIfNotPhpArrayHelper();
            Debug.Assert(!this.table.IsShared);
            this.table._deep_copy_inplace();
		}

		/// <summary>
		/// Adds items of this instance to a specified instance resetting integer keys.
		/// </summary>
		/// <param name="dst">Destination table.</param>
		/// <param name="deepCopy">Whether to make deep copies of added items.</param>
		/// <exception cref="ArgumentNullException"><paramref name="dst"/> is a <B>null</B> reference.</exception>
		public void AddTo(PhpHashtable/*!*/dst, bool deepCopy)
		{
            ThrowIfNotPhpArrayHelper();

			if (dst == null)
				throw new ArgumentNullException("dst");

            using (var enumerator = this.GetFastEnumerator())
                while (enumerator.MoveNext())
                {
                    object val = enumerator.CurrentValue;
                    if (deepCopy) val = PhpVariable.DeepCopy(val);

                    if (enumerator.CurrentKey.IsInteger)
                        dst.Add(val);
                    else
                        dst.Add(enumerator.CurrentKey, val);
                }
		}

        /// <summary>
        /// Copy values of this array into 
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="offset"></param>
        public void CopyValuesTo(object[]/*!*/dst, int offset)
        {
            Debug.Assert(dst != null);
            Debug.Assert(dst.Length - offset >= this.Count);
            Debug.Assert(offset >= 0);

            using (var enumerator = this.GetFastEnumerator())
                while (enumerator.MoveNext())
                {
                    dst[offset++] = enumerator.CurrentValue;
                }
        }

		#endregion

		#region Misc methods: Sort, Diff, Reverse, Shuffle, Unite

		/// <summary>
		/// Sorts this instance using specified comparer.
		/// </summary>
		/// <param name="comparer">The comparer to be used to compare array items.</param>
		public void Sort(IComparer<KeyValuePair<IntStringKey, object>>/*!*/ comparer)
		{
			if (comparer == null)
				throw new ArgumentNullException("comparer");

            this.EnsureWritable();
            OrderedDictionary.sortops._sort(this.table, comparer);
		}

		/// <summary>
		/// Sorts multiple hashtables given comparer for each hashtable.
		/// </summary>
		/// <param name="hashtables">
		/// The <see cref="ICollection"/> of <see cref="PhpHashtable"/>s. 
		/// All these tables has to be of the same length which has to be .
		/// </param> 
		/// <param name="comparers">
		/// An array of <see cref="IDictionaryComparer"/>s.
		/// The number of comparers has to be the same as the number of <paramref name="hashtables"/>.
		/// </param>
		/// <remarks>
		/// Sorts lexicographically all <paramref name="hashtables"/> from the first to the last one using 
		/// <paramref name="comparers"/> successively. Changes only order of entries in <paramref name="hashtables"/>.
		/// </remarks>
		/// <exception cref="ArgumentNullException"><paramref name="hashtables"/> or <paramref name="comparers"/> is a <B>null</B>reference.</exception>
		public static void Sort(PhpHashtable[]/*!*/ hashtables,
			IComparer<KeyValuePair<IntStringKey, object>>[]/*!*/ comparers)
		{
			#region requires (hashtables && comparer && comparers.Length==hashtables.Length)

			if (hashtables == null)
				throw new ArgumentNullException("hashtables");
			if (comparers == null)
				throw new ArgumentNullException("comparers");
			if (hashtables.Length != comparers.Length)
				throw new ArgumentException(CoreResources.GetString("lengths_are_different", "hashtables", "comparers"));

			#endregion

			if (comparers.Length == 0) return;

            // prepare tables (check they are the same length and make them writable):
            int count = -1;
            for (int i = 0; i < hashtables.Length; i++)
            {
                var table = hashtables[i];

                if (i == 0) count = table.Count;
                else if (table.Count != count)
                {
                    throw new ArgumentException(CoreResources.GetString("lengths_are_different", "hashtables[0]",
                        String.Format("hashtables[{0}]", i)), "hashtables");
                }

                table.EnsureWritable();
            }

            OrderedDictionary.sortops._multisort(count, hashtables, comparers);
		}

		/// <summary>
		/// Performs a set operation <see cref="PhpHashtable"/>s.
		/// </summary>
		/// <param name="op">The operation.</param>
		/// <param name="hashtables">The <see cref="ICollection"/> of <see cref="PhpHashtable"/>s.</param>
		/// <param name="comparer">The <see cref="IDictionaryComparer"/> used to compare entries of <paramref name="hashtables"/>.</param>
		/// <param name="result">The <see cref="IDictionary"/> where to add remaining elements.</param>
		/// <remarks>
		/// Entries that will remain in this instance if a difference was made are stored into 
		/// the <paramref name="result"/> in the same order they are stored in this instance. 
		/// </remarks>
		/// <exception cref="ArgumentNullException"><paramref name="hashtables"/> or <paramref name="comparer"/> or <paramref name="result"/> is a <B>null</B> reference.</exception>
		/// <exception cref="ArgumentException"><paramref name="result"/> references this instance.</exception>
		public void SetOperation(SetOperations op, PhpHashtable[]/*!*/ hashtables,
			IComparer<KeyValuePair<IntStringKey, object>>/*!*/ comparer, /*IDictionary<IntStringKey, object>*/PhpArray/*!*/ result)
		{
			#region Requires (hashtables && comparer && result && result!=this)

			if (hashtables == null)
				throw new ArgumentNullException("hashtables");
			if (comparer == null)
				throw new ArgumentNullException("comparers");
			if (result == null)
				throw new ArgumentNullException("result");
			if (result == this)
				throw new ArgumentException(CoreResources.GetString("argument_equals", "result", "this"));

			#endregion

            if (hashtables.Length == 0) return;

            this.EnsureWritable();
            this.table._set_operation(op, hashtables, comparer, result);
		}

        //private IEnumerable<OrderedHashtable<IntStringKey>.Element>/*!*/ EnumerateHeads(IEnumerable<PhpHashtable>/*!*/ hashtables)
        //{
        //    foreach (PhpHashtable hashtable in hashtables)
        //        yield return hashtable.table.head;
        //}

        /// <summary>
        /// Reverses order of entries in this instance.
        /// </summary>
        public void Reverse()
        {
            this.EnsureWritable();
            this.table._reverse();
        }

		/// <summary>
		/// Shuffles order of elements in the hashtable at random.
		/// </summary>
		/// <exception cref="ArgumentNullException"><paramref name="generator"/> is a <b>null</b> reference.</exception>
		public void Shuffle(Random generator)
		{
            this.EnsureWritable();
			table._shuffle_data(generator);
		}

		/// <summary>
		/// Unites an <paramref name="array"/> with this instance.
		/// </summary>
		/// <param name="array">An <see cref="PhpArray"/> of items to be united with this instance.</param>
		/// <returns>Reference to this instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is null reference.</exception>
		/// <remarks>
		/// All keys are preserved. Values associated with existing string keys will not be overwritten.
		/// </remarks>
		public PhpHashtable Unite(PhpHashtable array)
		{
			if (array == null) throw new ArgumentNullException("array");
			if (array.table == this.table) return this;

            using (var enumerator = array.GetFastEnumerator())
                while (enumerator.MoveNext())
                {
                    if (!this.table.ContainsKey(enumerator.CurrentKey))
                        this[enumerator.CurrentKey] = enumerator.CurrentValue;  // TODO: DeepCopy value ?
                }
			
			return this;
		}

		#endregion

		#region RefreshMaxIntegerKey, ReindexAll, ReindexIntegers, ReindexAndReplace

        /// <summary>
        /// Ensure the internal maximal key value will be updated.
        /// </summary>
		public void RefreshMaxIntegerKey()
		{
            this.nextNewIndex = -1;
		}

        /// <summary>
        /// Recalculates <see cref="nextNewIndex"/> value.
        /// </summary>
        private void RefreshMaxIntegerKeyInternal()
        {
            this.nextNewIndex = (this.intCount == 0) ? 0 : this.table._find_max_int_key() + 1;
        }

        /// <summary>
		/// Sets all keys to increasing integers according to their respective order in the list.
		/// </summary>
        public void ReindexAll()
        {
            this.EnsureWritable();

            // updates the list:
            int i = 0;

            using (var enumerator = this.table.GetFastEnumerator())
                while (enumerator.MoveNext())
                {
                    enumerator.ModifyCurrentEntryKey(new IntStringKey(i++));
                }

            //
            this.nextNewIndex = i;

            //
            this.table._rehash();
        }

		/// <summary>
		/// Sets all keys to increasing integers according to their respective order in the list.
		/// </summary>
		/// <param name="startIndex">An index from which to start indexing.</param>
		/// <remarks>If indexing overflows a capacity of integer type it continues with <see cref="int.MinValue"/>.</remarks>
        public void ReindexIntegers(int startIndex)
        {
            this.EnsureWritable();

            // updates the list:
            int i = startIndex;
            
            using (var enumerator = this.table.GetFastEnumerator())
                while (enumerator.MoveNext())
                {
                    if (enumerator.CurrentKey.IsInteger)
                    {
                        enumerator.ModifyCurrentEntryKey(new IntStringKey(i++));
                    }
                }
            
            //
            this.nextNewIndex = i;
            
            //
            this.table._rehash();
        }

		/// <summary>
		/// Replaces a part of the hashtable with specified item(s) and reindexes all integer keys in result.
		/// </summary>
		/// <param name="offset">
		/// The ordinary number of the first item to be replaced. 
		/// <paramref name="offset"/> should be at least zero and at most equal as the number of items in the array.
		/// </param>
		/// <param name="length">
		/// The number of items to be replaced. Should be at least zero and at most equal 
		/// to the number of items in the array.
		/// </param>
		/// <param name="replacementValues">
		/// The enumerable collection of values by which items in the range specified by
		/// <paramref name="offset"/> and <paramref name="length"/> is replaced.
		/// </param>
		/// <param name="replaced">
		/// The hashtable where removed values will be placed. Keys are successive integers starting from zero.
		/// </param>
		/// <exception cref="ArgumentOutOfRangeException"><pararef name="offset"/> or <paramref name="length"/> has invalid value.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="replaced"/> is a <b>null</b> reference.</exception>
		public void ReindexAndReplace(int offset, int length, IEnumerable replacementValues, PhpHashtable/*!*/ replaced)
		{
            int count = this.Count;

            if (offset < 0 || offset > count)
                throw new ArgumentOutOfRangeException("first");
            if (length < 0 || offset + length > count)
                throw new ArgumentOutOfRangeException("length");
            if (replaced == null)
                throw new ArgumentNullException("replaced");

            this.EnsureWritable();  // ensure values are deeply copied
            
            int ikey = 0;

            // reindexes integer keys of elements before the first replaced item:
            int i = 0;
            using (var enumerator = this.GetFastEnumerator())
            {
                // reindex first [offset] entries (whose key is integer):
                while (i++ < offset && enumerator.MoveNext())
                {
                    if (enumerator.CurrentKey.IsInteger)
                        enumerator.ModifyCurrentEntryKey(new IntStringKey(ikey++));
                }

                // [enumerator] points to last reindexed entry, have to be advanced to the next
                enumerator.MoveNext();

                // removes items with ordinal number in interval [first,last]:
                int jkey = 0;
                i = 0;
                while (i++ < length/* && enumerator.MoveNext()*/)
                {
                    Debug.Assert(enumerator.IsValid);

                    if (enumerator.CurrentKey.IsInteger)
                    {
                        replaced.Add(jkey++, enumerator.CurrentValue);
                    }
                    else
                    {
                        replaced.Add(enumerator.CurrentKey, enumerator.CurrentValue);
                    }

                    // remove item from the list:
                    enumerator.DeleteCurrentEntryAndMove(this.activeEnumerators);
                }

                // adds new elements before "enumerator" element:
                if (replacementValues != null)
                {
                    foreach (object value in replacementValues)
                        enumerator.InsertBeforeCurrentEntry(new IntStringKey(ikey++), value);
                }

                // reindexes integer keys of the rest elements:
                if (enumerator.IsValid)
                {
                    do
                    {
                        if (enumerator.CurrentKey.IsInteger)
                            enumerator.ModifyCurrentEntryKey(new IntStringKey(ikey++));

                    } while (enumerator.MoveNext());
                }
            }

            // rehashes the table (updates bucket lists)
            this.table._rehash();

            // updates max integer value in table:
            this.nextNewIndex = ikey;
		}

		#endregion

		#region Static PhpHashtable/Dictionary Switching (useful for local/global variables dictionaries)

		public static bool TryGetValue(PhpHashtable hashtable, Dictionary<string, object> dictionary, string key, out object value)
		{
			if (hashtable != null)
				return hashtable.TryGetValue(key, out value);
			else if (dictionary != null)
				return dictionary.TryGetValue(key, out value);
            else
                throw new ArgumentNullException("hashtable");
		}

		public static bool ContainsKey(PhpHashtable hashtable, Dictionary<string, object> dictionary, string key)
		{
			if (hashtable != null)
				return hashtable.ContainsKey(key);
			else if (dictionary != null)
				return dictionary.ContainsKey(key);
            else
                throw new ArgumentNullException("hashtable");
		}

		public static void Add(PhpHashtable hashtable, Dictionary<string, object> dictionary, string key, object value)
		{
			if (hashtable != null)
				hashtable.Add(key, value);
            else if (dictionary != null)
				dictionary.Add(key, value);
            else
                throw new ArgumentNullException("hashtable");
		}

		public static void Set(PhpHashtable hashtable, Dictionary<string, object> dictionary, string key, object value)
		{
			if (hashtable != null)
				hashtable[key] = value;
			else if (dictionary != null)
				dictionary[key] = value;
            else
                throw new ArgumentNullException("hashtable");
		}

		public static void Remove(PhpHashtable hashtable, Dictionary<string, object> dictionary, string key)
		{
			if (hashtable != null)
				hashtable.Remove(key);
			else if (dictionary != null)
				dictionary.Remove(key);
            else
                throw new ArgumentNullException("hashtable");
		}

		public static IEnumerable<KeyValuePair<string, object>>/*!*/ GetEnumerator(PhpArray hashtable,
			Dictionary<string, object> dictionary)
		{
			if (hashtable != null)
				return hashtable.GetStringKeyEnumerable();
			else if (dictionary != null)
				return (IEnumerable<KeyValuePair<string, object>>)dictionary;
			else
                throw new ArgumentNullException("hashtable");
		}

		private IEnumerable<KeyValuePair<string, object>>/*!*/ GetStringKeyEnumerable()
		{
			foreach (KeyValuePair<IntStringKey, object> entry in this)
				yield return new KeyValuePair<string, object>(entry.Key.ToString(), entry.Value);
		}

		#endregion
	}

	#endregion
}
