﻿/*

 Copyright (c) 2012 DEVSENSE
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace PHP.Core
{
    /// <summary>
    /// Dictionary preserving order of entries.
    /// Defines additional methods for ordering and for PHP library functions support.
    /// </summary>
#if !SILVERLIGHT
    [Serializable]
#endif
    public sealed class OrderedDictionary : IDictionary<IntStringKey, object>, IDictionary, ISerializable, ICloneable
    {
        #region Fields

        private int tableMask;			// Mask = (tableSize - 1)
        private int tableSize;			// Table size = (1 << n)
        private int count,              // Used entries (0..count)
                    freeCount,	        // Amount of free entries within (0..count)
                    freeList;           // first free Entry
        //private int nextNewIndex;	    // Index for next new element   // TODO: to be moved to PhpArray
        private int listHead;			// first Entry
        private int listTail;			// last Entry
        private int[]/*!*/buckets;		// indexes to Entries (buckets[ hash & tableMask ])
        private Entry[] entries;        // initialized lazily

        /// <summary>
        /// Used as intial value for <see cref="buckets"/> if array is empty.
        /// With this as buckets, all the operators work and they do not have to check whether the collection is empty.
        /// </summary>
        private readonly static int[] emptyBuckets = new int[] { -1 };

        // TODO: int flags = 0; // heuristics
        /* e.g.:
         * DeletionPerformed (whether nextNewIndex has to be recomputed when DeepCopied)
         * HasDeepCopiableObjects (whether DeepCopy of values is necessary when cloned)
         * IsSorted (only if all the items were added by [] operator or as a collection in ctor)
         */

        /// <summary>
        /// Keep track of additional references. Increased when a copy is made, decreased is a copy is released.
        /// </summary>
        private int copiesCount = 0;

        /// <summary>
        /// Additional information about this instance creator.
        /// </summary>
        internal readonly object owner;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize new instance of <see cref="OrderedDictionary"/> as a duplicate of given <paramref name="copyfrom"/>.
        /// </summary>
        /// <param name="owner">Instance creator.</param>
        /// <param name="copyfrom">Instance of an existing <see cref="OrderedDictionary"/>.</param>
        internal OrderedDictionary(object owner, OrderedDictionary/*!*/copyfrom)
        {
            Debug.Assert(copyfrom != null);

            // duplicate internal structure as it is,
            // there are no references, so walk through the array is not necessary,
            // also rehashing is not necessary.

            this.tableSize = copyfrom.tableSize;
            this.tableMask = copyfrom.tableMask;
            if (copyfrom.buckets != emptyBuckets)
            {
                this.buckets = new int[this.tableSize];
                Buffer.BlockCopy(copyfrom.buckets, 0, this.buckets, 0, this.tableSize * sizeof(int));
                //Array.Copy(copyfrom.buckets, 0, this.buckets, 0, this.tableSize);
                // TODO: check whether Array.Copy is faster
            }
            else
            {
                this.buckets = emptyBuckets;
            }
            this.listHead = copyfrom.listHead;
            this.listTail = copyfrom.listTail;
            this.count = copyfrom.count;
            this.freeCount = copyfrom.freeCount;
            this.freeList = copyfrom.freeList;
            //this.nextNewIndex = copyfrom.nextNewIndex;
            if (copyfrom.entries != null)
            {
                this.entries = new Entry[this.tableSize];
                Array.Copy(copyfrom.entries, 0, this.entries, 0, this.count);
            }
            else
            {
                this.entries = null;
            }

            //
            this.owner = owner;

            //
            this._debug_check_consistency();
        }

        public OrderedDictionary(object owner, int size)
        {
            this.tableSize = CalculatetableSize(size);
            this.tableMask = 0;	/* 0 means that this.buckets is uninitialized */
            this.buckets = emptyBuckets;  // proper instance initialized lazily
            this.listHead = -1;
            this.listTail = -1;
            this.count = this.freeCount = 0;
            this.freeList = -1;
            //this.nextNewIndex = 0;
            this.entries = null;    // initialized lazily
            this.owner = owner;     // instance creator

            //
            this._debug_check_consistency();
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CalculatetableSize(int size)
        {
            if (size < (1 << 30))
            {
                int i = (1 << 2);   // how big is our smallest possible array? "1" is min, do not put "0" here! Smaller number makes initialization faster, but slows down expanding. However the size is known mostly ...
                while (i < size)
                    i <<= 1;

                return i;
            }

            /* prevent overflow */
            return (1 << 30);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureInitialized()
        {
            if (this.tableMask == 0)
                InitializeBuckets();
        }
        private void InitializeBuckets()
        {
            Debug.Assert(this.entries == null, "Initialized already!");

            int[] _buckets;

            this.buckets = _buckets = new int[this.tableSize];
		    this.tableMask = this.tableSize - 1;
            this.entries = new Entry[this.tableSize];

            for (int i = 0; i < _buckets.Length; i++)
                _buckets[i] = -1;
        }

        #endregion

        #region Inner class: Entry

        /// <summary>
        /// An element stored in the table.
        /// </summary>
        [Serializable]
        private struct Entry
        {
            /// <summary>
            /// Key associated with the element.
            /// </summary>
            public IntStringKey Key { get { return _key; } set { _key = value; } }
            internal IntStringKey _key;
            
            // linked list of entries:
            internal int
                next, last,             // within bucket
                listNext, listLast;     // within the whole ordered dictionary list
            
            /// <summary>
            /// Value associated with the element.
            /// </summary>
            public object Value { get { return _value; } set { _value = value; } }
            private object _value;

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool KeyEquals(ref IntStringKey other)
            {
                return _key.Equals(ref other);
            }

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool KeyEquals(int ikey)
            {
                return _key.Equals(ikey);
            }

            public KeyValuePair<IntStringKey, object> KeyValuePair { get { return new KeyValuePair<IntStringKey, object>(_key, _value); } }
        }

        #endregion
                
        #region Inner class: Enumerator

        [Serializable]
        public class Enumerator : IEnumerator<KeyValuePair<IntStringKey, object>>, IDictionaryEnumerator, IDisposable, IPhpEnumerator
        {
            /// <summary>
            /// Enumerated table.
            /// </summary>
            internal OrderedDictionary/*!*/table;

            /// <summary>
            /// Reference to associated <see cref="PhpHashtable"/>. Used to unregister enumerator.
            /// </summary>
            internal readonly PhpHashtable hashtable;

            /// <summary>
            /// Current element index.
            /// </summary>
            private int element;

            /// <summary>
            /// Fetched element data.
            /// </summary>
            private KeyValuePair<IntStringKey, object> current;

            /// <summary>
            /// Whether enumeration is on the start.
            /// </summary>
            bool start;

            /// <summary>
            /// Whether the enumerator should return <c>KeyValuePair{K, object}</c> when used as <see cref="IEnumerator"/>.
            /// If <B>false</B> it will return <see cref="DictionaryEntry"/>.
            /// </summary>
            private readonly bool isGeneric;

            /// <summary>
            /// A reference to another <see cref="Enumerator"/>, allows to link existing enumerators into a linked list.
            /// </summary>
            internal Enumerator next;

            public Enumerator(OrderedDictionary/*!*/table, bool isGeneric)
            {
                Debug.Assert(table != null);

                this.table = table;
                this.element = -1;
                this.current = new KeyValuePair<IntStringKey, object>();
                this.start = true;
                this.isGeneric = isGeneric;
            }

            public Enumerator(PhpHashtable/*!*/hashtable, bool isGeneric)
                : this(hashtable.table, isGeneric)
            {
                this.hashtable = hashtable;
                hashtable.RegisterEnumerator(this);
            }

            public object CurrentValue { get { return current.Value; } }
            public IntStringKey CurrentKey { get { return current.Key; } }
            private bool FetchCurrent()
            {
                if (element >= 0)
                {
                    current = new KeyValuePair<IntStringKey, object>(table.entries[element]._key, table.entries[element].Value);
                    return true;
                }

                current = new KeyValuePair<IntStringKey, object>();
                return false;
            }

            /// <summary>
            /// Callback method caled by <see cref="_del_key_or_index"/> when an entry has been deleted.
            /// </summary>
            /// <param name="entry_index">Deleted entry index.</param>
            /// <param name="next_entry_index">Next entry index as a replacement.</param>
            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void EntryDeleted(int entry_index, int next_entry_index)
            {
                if (entry_index == this.element)
                {
                    this.element = next_entry_index;
                    FetchCurrent();
                }
            }

            /// <summary>
            /// Called when underlaying table has been changed (Unshare() called).
            /// </summary>
            internal void TableChanged()
            {
                Debug.Assert(this.hashtable != null, "Enumerator was not registered!");
                Debug.Assert(this.table != this.hashtable.table, "Table was not changed!");

                this.table = this.hashtable.table;
            }

            #region IEnumerator<KeyValuePair<IntStringKey, object>>

            public KeyValuePair<IntStringKey, object> Current
            {
                get
                {
                    return current;
                }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return isGeneric ? current : (object)((IDictionaryEnumerator)this).Entry; }
            }

            public bool MoveNext()
            {
                Debug.Assert(this.hashtable == null || this.hashtable.table == this.table, "Underlaying table has been changed without updating Enumerator!");

                if (element >= 0)
                {
                    element = table.entries[element].listNext;
                }
                else if (start)
                {
                    element = table.listHead;
                    start = false;
                }

                return FetchCurrent();
            }

            public void Reset()
            {
                element = -1;
                start = true;
            }

            #endregion
            
            #region IDisposable

            public void Dispose()
            {
                element = -1;
                current = new KeyValuePair<IntStringKey, object>();

                if (this.hashtable != null)
                {
                    this.hashtable.UnregisterEnumerator(this);
                    //this.hashtable = null;
                }
            }

            #endregion

            #region IDictionaryEnumerator Members

            DictionaryEntry IDictionaryEnumerator.Entry { get { return new DictionaryEntry(current.Key, current.Value); } }
            object IDictionaryEnumerator.Key { get { return current.Key; } }
            object IDictionaryEnumerator.Value { get { return current.Value; } }

            #endregion

            #region IPhpEnumerator

            public bool MoveLast()
            {
                start = false;
                element = table.listTail;
                return FetchCurrent();
            }

            public bool MoveFirst()
            {
                start = false;
                element = table.listHead;
                return FetchCurrent();
            }

            public bool MovePrevious()
            {
                if (element >= 0)
                {
                    element = table.entries[element].listLast;
                }
                else if (start)
                {
                    element = table.listTail;
                    start = false;
                }

                return FetchCurrent();
            }

            public bool AtEnd
            {
                get
                {
                    // if the enumerator is in starting state, it's not considered to be at the end:
                    if (start) return false;

                    return (element < 0);
                }
            }

            #endregion
        }

        #endregion

        #region Inner class: FastEnumerator

        internal FastEnumerator GetFastEnumerator()
        {
            return new FastEnumerator(this);
        }

        public struct FastEnumerator : IDisposable
        {
            private readonly OrderedDictionary/*!*/table;
            private int currentEntry;
            private KeyValuePair<IntStringKey, object> current;

            public FastEnumerator(OrderedDictionary/*!*/table)
            {
                Debug.Assert(table != null);

                this.table = table;
                this.currentEntry = -1;
                this.current = new KeyValuePair<IntStringKey, object>();
            }

            public bool MoveNext()
            {
                int next;
                if (currentEntry >= 0)
                    currentEntry = next = table.entries[currentEntry].listNext;
                else
                    currentEntry = next = table.listHead;  // start // note after unsuccessful MoveNext() enumerator is restarted

                if (next >= 0)
                {
                    this.current = table.entries[next].KeyValuePair;
                    return true;
                }
                
                this.current = new KeyValuePair<IntStringKey, object>();
                return false;
            }

            public IntStringKey CurrentKey { get { return current.Key; } }
            public object CurrentValue
            {
                get
                {
                    return current.Value;
                }
                set
                {
                    ModifyCurrentValue(value);
                    // current.Value is not updated for performance reasons
                }
            }
            public KeyValuePair<IntStringKey, object> Current { get { return current; } }

            public void Reset()
            {
                this.currentEntry = -1;
                this.current = new KeyValuePair<IntStringKey, object>();
            }

            #region IDisposable

            public void Dispose()
            {
                currentEntry = -1;
                current = new KeyValuePair<IntStringKey, object>();
            }

            #endregion

            #region internal: Helper methods

            /// <summary>
            /// Checks whether enumerator points to an entry.
            /// </summary>
            internal bool IsValid { get { return this.currentEntry >= 0; } }

            /// <summary>
            /// Gets or sets current entry's <see cref="Entry.listLast"/> field.
            /// </summary>
            internal int CurrentEntryListLast
            {
                get { Debug.Assert(this.IsValid); return this.table.entries[this.currentEntry].listLast; }
                set
                {
                    Debug.Assert(this.IsValid);
                    this.table.entries[this.currentEntry].listLast = value;
                }
            }

            /// <summary>
            /// Gets or sets current entry's <see cref="Entry.listNext"/> field or <see cref="OrderedDictionary.listHead"/> if enumerator is not started yet.
            /// </summary>
            internal int CurrentEntryListNext
            {
                get
                {
                    if (this.IsValid)
                        return this.table.entries[this.currentEntry].listNext;
                    else
                        return this.table.listHead;
                }
                set
                {
                    if (this.IsValid)
                        this.table.entries[this.currentEntry].listNext = value;
                    else
                        this.table.listHead = value;
                }
            }

            /// <summary>
            /// Modifies key of current entry in the table.
            /// </summary>
            /// <param name="newkey">New key for the current entry.</param>
            /// <remarks>This function does not change the <see cref="CurrentKey"/> and <see cref="Current"/>, since both there values are already fetched.
            /// Note the table must be rehashed manually after this operation.</remarks>
            internal void ModifyCurrentEntryKey(IntStringKey newkey)
            {
                Debug.Assert(this.IsValid);
                this.table.entries[this.currentEntry]._key = newkey;
            }

            internal void ModifyCurrentValue(object newvalue)
            {
                Debug.Assert(IsValid);
                this.table.entries[this.currentEntry].Value = newvalue;
            }

            /// <summary>
            /// Delete current entry from the table and advances enumerator to the next entry.
            /// </summary>
            /// <param name="activeEnumerators">List of active enumerators so they can be updated.</param>
            /// <returns>Whether there is another entry in the table.</returns>
            internal bool DeleteCurrentEntryAndMove(OrderedDictionary.Enumerator activeEnumerators)
            {
                Debug.Assert(this.IsValid);
                int p = this.currentEntry;
                int nIndex = this.CurrentKey.Integer & this.table.tableMask;
                bool hasMore = this.MoveNext();

                this.table._remove_entry(ref this.table.entries[p], p, nIndex, activeEnumerators);

                return hasMore;
            }

            /// <summary>
            /// Insert new entry before current entry.
            /// </summary>
            /// <param name="key">New item key.</param>
            /// <param name="value">New item value.</param>
            internal void InsertBeforeCurrentEntry(IntStringKey key, object value)
            {
                this.table._add_before(ref key, value, this.currentEntry);  // is not this.IsValid, new entry is added at the end.
            }

            /// <summary>
            /// Gets current entry index within the <see cref="OrderedDictionary.entries"/> array.
            /// </summary>
            internal int CurrentEntryIndex
            {
                get { return this.currentEntry; }
                set
                {
                    this.currentEntry = value;

                    // fetch current entry:
                    if (value >= 0)
                        this.current = table.entries[value].KeyValuePair;
                    else
                        this.current = new KeyValuePair<IntStringKey, object>();
                }
            }

            #endregion
        }

        #endregion
        
        #region table operations

        #region _enlist_*, _debug_check_consistency

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _enlist_to_bucket(ref Entry entry, int entry_index, int list_head)
        {
            entry.last = -1;
            entry.next = list_head;
            if (list_head >= 0)
                this.entries[list_head].last = entry_index;
        }
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _enlist_to_global(ref Entry entry, int entry_index)
        {
            entry.listNext = -1;
            entry.listLast = this.listTail;

            if (this.listTail >= 0)
                this.entries[this.listTail].listNext = entry_index;

            this.listTail = entry_index; 
            if (this.listHead < 0)
                this.listHead = entry_index;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _enlist(ref Entry entry, int entry_index, int list_head)
        {
            //this._enlist_to_bucket(ref entry, entry_index, list_head);
            //this._enlist_to_global(ref entry, entry_index);

            entry.next = list_head;
            entry.last = -1;
            entry.listNext = -1;
            entry.listLast = this.listTail;
            
            if (list_head >= 0)
                this.entries[list_head].last = entry_index;
            
            if (this.listTail >= 0)
                this.entries[this.listTail].listNext = entry_index;

            this.listTail = entry_index;
            if (this.listHead < 0)
                this.listHead = entry_index;                        
        }

        /// <summary>
        /// Enlists <paramref name="element"/> before given <paramref name="p"/>.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="elementIndex"></param>
        /// <param name="p"></param>
        /// <param name="pIndex"></param>
        private void _enlist_to_global_before(ref Entry element, int elementIndex, ref Entry p, int pIndex)
        {
            element.listLast = p.listLast;
            element.listNext = pIndex;

            if (p.listLast >= 0)
                this.entries[p.listLast].listNext = elementIndex;
            else
                this.listHead = elementIndex;
            p.listLast = elementIndex;
        }

        [Conditional("DEBUG")]
        internal void _debug_check_consistency()
        {
            Debug.Assert((this.listHead >= 0 && this.listTail >= 0) || (this.listHead < 0 && this.listTail < 0), "listHead, listTail");
            Debug.Assert(this.entries == null || this.entries.Length == this.tableSize, "this.entries.Length != this.tableSize");

            var _entries = this.entries;
            var _buckets = this.buckets;

            // check global list
            int count = 0;
            int last = -1;
            for (int p = this.listHead; p >= 0; p = _entries[p].listNext)
            {
                Debug.Assert(last != p, "global list cycled!");
                Debug.Assert(_entries[p].listLast == last, "_entries[p].listLast != last");
                last = p;
                ++count;
            }
            Debug.Assert(last == this.listTail, "last == this.listTail");
            Debug.Assert(count == this.Count, "count != this.Count");

            // check bucket lists
            count = 0;
            for (int i = 0; i < _buckets.Length; i++)
            {
                last = -1;
                for (int p = _buckets[i]; p >= 0; p = _entries[p].next)
                {
                    Debug.Assert(last != p, "bucket list cycled!");
                    Debug.Assert(_entries[p].last == last, "_entries[p].last != last");
                    last = p;
                    ++count;
                }
            }
            Debug.Assert(count == this.Count, "count != this.Count (sum of bucket lists)");

            // check free list
            if (this.freeCount > 0)
            {
                last = -1;
                Debug.Assert(this.freeList >= 0, "this.freeCount > 0 && this.freeList < 0");
                for (int p = this.freeList; p >= 0; p = _entries[p].next)
                {
                    Debug.Assert(last != p, "freeList cycled!");
                    Debug.Assert(_entries[p].Value == null, "free entry has not disposed value");
                    last = p;
                }
            }
            else
            {
                Debug.Assert(this.freeList < 0, "this.freeCount == 0 && this.freeList >= 0");
            }
        }

        #endregion

        #region _do_resize, _rehash

        /// <summary>
        /// Double the size of internal structures. Rehash entries.
        /// </summary>
        /// <remarks><see cref="entries"/> has to be initialized already.</remarks>
        /// <exception cref="OverflowException">Table size cannot be doubled more.</exception>
        private void _do_resize()
        {
            Debug.Assert(this.entries != null);

            // double the table size:
            int new_size = checked(this.tableSize << 1);
            int new_mask = new_size - 1;
            
            var new_buckets = new int[new_size];
            for (int i = 0; i < new_buckets.Length; i++)    // JIT optimization
                new_buckets[i] = -1;

            var new_entries = new Entry[new_size];
            Array.Copy(this.entries, 0, new_entries, 0, this.count);

            // 
            this.tableSize = new_size;
            this.tableMask = new_mask;
            this.buckets = new_buckets;
            this.entries = new_entries;

            // _rehash():
            int nIndex;
            for (var p = this.listHead; p >= 0; p = new_entries[p].listNext)
            {
                nIndex = new_entries[p]._key.Integer & new_mask;
                this._enlist_to_bucket(ref new_entries[p], p, new_buckets[nIndex]);
                new_buckets[nIndex] = p;
            }

            // check
            this._debug_check_consistency();
        }

        /// <summary>
        /// Rehashes all the entries according to their current key. Preserves the order.
        /// </summary>
        internal void _rehash()
        {
            // use locals instead of fields:
            var _buckets = this.buckets;
            var _mask = this.tableMask;
            var _entries = this.entries;

            // empty buckets
            for (int i = 0; i < _buckets.Length; i++)
                _buckets[i] = -1;

            // rehash all the entries:
            int nIndex;
            for (var p = this.listHead; p >= 0; p = _entries[p].listNext)
            {
                nIndex = _entries[p]._key.Integer & _mask;
                this._enlist_to_bucket(ref _entries[p], p, _buckets[nIndex]);
                _buckets[nIndex] = p;
            }

            // check
            this._debug_check_consistency();
        }

        #endregion

        #region _add_or_update, _add_first, _add_last

        /// <summary>
        /// Set <paramref name="value"/> onto given <paramref name="key"/> position.
        /// </summary>
        /// <param name="key">Key of the item to be added or upudated.</param>
        /// <param name="value">Value of the item.</param>
        /// <remarks>If <paramref name="key"/> is not contained in the table yet, newly added entry is added at the end.</remarks>
        public void _add_or_update(IntStringKey key, object value/*, bool add*/)
        {
            //ulong h;// = key.Integer
            
            EnsureInitialized();

            int nIndex = key.Integer & this.tableMask;// index(ref key);
            var _entries = this.entries;

            // find
            int p;
            for (p = this.buckets[nIndex]; p >= 0; p = _entries[p].next) // TODO: unsafe
            {
                if (_entries[p].KeyEquals(ref key))
                {
                    //if (add)
                    //    return;// false;

                    _entries[p].Value = value;
                    return;// true;
                }
            }

            // not found, _add_last:

            // find an empty Entry to be used
            if (this.freeCount > 0)
            {
                p = this.freeList;
                this.freeList = _entries[p].next;
                --this.freeCount;
            }
            else
            {
                if (this.count == _entries.Length)
                {
                    this._do_resize();  // double the capacity
                    
                    // update locals affected by resize:
                    nIndex = key.Integer & this.tableMask;// index(ref key);    // new index
                    _entries = this.entries;
                }
                p = this.count++;
            }

            //
            _entries[p]._key = key;
            this._enlist(ref _entries[p], p, this.buckets[nIndex]);
            _entries[p].Value = value;
            this.buckets[nIndex] = p;
            
            //// update nextNewIndex: // moved to PhpArray
            //if (key.IsInteger && key.Integer >= this.nextNewIndex)
            //    this.nextNewIndex = key.Integer + 1;

            return;// true;
        }

        //private void _add_no_check(int intKey, object value)
        //{
        //    
        //}

        /// <summary>
        /// Add new entry at the begining of the array.
        /// </summary>
        /// <param name="key">Entry key.</param>
        /// <param name="value">Entry value.</param>
        /// <exception cref="ArgumentException">An element with the same key already exists.</exception>
        internal void _add_first(IntStringKey key, object value)
        {
            if (_findEntry(ref key) >= 0)
                throw new ArgumentException();

            EnsureInitialized();

            int nIndex = key.Integer & this.tableMask;// index(ref key);
            var _entries = this.entries;

            // add:
            int p; // index of entry to be used for new item

            // find an empty Entry to be used
            if (this.freeCount > 0)
            {
                p = this.freeList;
                this.freeList = _entries[p].next;
                --this.freeCount;
            }
            else
            {
                if (this.count == _entries.Length)
                {
                    this._do_resize();  // double the capacity

                    // update locals affected by resize:
                    nIndex = key.Integer & this.tableMask;// index(ref key);    // new index
                    _entries = this.entries;
                }
                p = this.count++;
            }

            //
            _entries[p]._key = key;

            // enlist into bucket
            this._enlist_to_bucket(ref _entries[p], p, this.buckets[nIndex]);
            
            // enlist into global
            _entries[p].listLast = -1;
            _entries[p].listNext = this.listHead;
            if (this.listHead >= 0)
                _entries[this.listHead].listLast = p;
            if (this.listTail < 0)
                this.listTail = p;
            this.listHead = p;

            this.buckets[nIndex] = p;
            _entries[p].Value = value;
            
            //// update nextNewIndex: // moved to PhpArray
            //if (key.IsInteger && key.Integer >= this.nextNewIndex)
            //    this.nextNewIndex = key.Integer + 1;

            return;// true;
        }

        /// <summary>
        /// Add specified item at the end of the array.
        /// </summary>
        /// <param name="key">New item key.</param>
        /// <param name="value">New item value.</param>
        /// <remarks>The function does not check if the item already exists.</remarks>
        private void _add_last(ref IntStringKey key, object value)
        {
            Debug.Assert(!this._contains(ref key), "Item with given key already exists!");

            var _entries = this.entries;
            int p;
            
            // find an empty Entry to be used
            if (this.freeCount > 0)
            {
                p = this.freeList;
                this.freeList = _entries[p].next;
                --this.freeCount;
            }
            else
            {
                if (this.count == _entries.Length)
                {
                    this._do_resize();  // double the capacity

                    // update locals affected by resize:
                    _entries = this.entries;
                }
                p = this.count++;
            }

            //
            var nIndex = key.Integer & this.tableMask;// index(ref key);

            _entries[p]._key = key;
            this._enlist(ref _entries[p], p, this.buckets[nIndex]);
            _entries[p].Value = value;
            this.buckets[nIndex] = p;

            //// update nextNewIndex: // moved to PhpArray
            //if (key.IsInteger && key.Integer >= this.nextNewIndex)
            //    this.nextNewIndex = key.Integer + 1;

            return;// true;
        }

        /// <summary>
        /// Add specified item at the end of the array.
        /// </summary>
        /// <param name="ikey">New item key.</param>
        /// <param name="value">New item value.</param>
        /// <remarks>The function does not check if the item already exists.</remarks>
        internal void _add_last(int ikey, object value)
        {
            Debug.Assert(!this.ContainsKey(new IntStringKey(ikey)), "Item with given key already exists!");

            this.EnsureInitialized();

            var _entries = this.entries;
            int p;

            // find an empty Entry to be used
            if (this.freeCount > 0)
            {
                p = this.freeList;
                this.freeList = _entries[p].next;
                --this.freeCount;
            }
            else
            {
                if (this.count == _entries.Length)
                {
                    this._do_resize();  // double the capacity

                    // update locals affected by resize:
                    _entries = this.entries;
                }
                p = this.count++;
            }

            //
            var nIndex = ikey & this.tableMask;// index(ref key);

            _entries[p]._key = new IntStringKey(ikey);
            this._enlist(ref _entries[p], p, this.buckets[nIndex]);
            _entries[p].Value = value;
            this.buckets[nIndex] = p;

            //// update nextNewIndex: // moved to PhpArray
            //if (key.IsInteger && key.Integer >= this.nextNewIndex)
            //    this.nextNewIndex = key.Integer + 1;

            return;// true;
        }

        /// <summary>
        /// Add new entry before given <paramref name="entry_index"/>. If <paramref name="entry_index"/> is invalid, new item is added at the end of the table.
        /// Note given <paramref name="key"/> must not exist in the table yet.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="entry_index"></param>
        private void _add_before(ref IntStringKey key, object value, int entry_index)
        {
            this.EnsureInitialized();

            if (entry_index < 0)
            {
                _add_last(ref key, value);
                return;
            }

            Debug.Assert(!this._contains(ref key), "Item with given key already exists!");
            
            var _entries = this.entries;
            int p;

            // find an empty Entry to be used
            if (this.freeCount > 0)
            {
                p = this.freeList;
                this.freeList = _entries[p].next;
                --this.freeCount;
            }
            else
            {
                if (this.count == _entries.Length)
                {
                    this._do_resize();  // double the capacity

                    // update locals affected by resize:
                    _entries = this.entries;
                }
                p = this.count++;
            }

            //
            var nIndex = key.Integer & this.tableMask;// index(ref key);

            _entries[p]._key = key;
            // enlist to bucket:
            this._enlist_to_bucket(ref _entries[p], p, this.buckets[nIndex]);

            // enlist to global
            this._enlist_to_global_before(ref _entries[p], p, ref _entries[entry_index], entry_index);

            _entries[p].Value = value;
            this.buckets[nIndex] = p;
        }

        #endregion

        #region _del_key_or_index, _remove_first, _remove_last

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _remove_entry(ref Entry entry, int entry_index, int bucket_index, Enumerator active_enumerators)
        {
#if DEBUG
            Debug.Assert(entry._key.Equals(ref this.entries[entry_index]._key), "entry != entries[entry_index");
            int p;
            for (p = this.buckets[bucket_index]; p >= 0; p = this.entries[p].next)
                if (p == entry_index)
                    break;
            Debug.Assert(p >= 0, "entry_index not found");
            Debug.Assert(freeCount > 0 || freeList < 0, "freeCount == 0 && freeList >= 0,");
#endif

            // update active enumerators, so they won't point to the item being deleted:
            for (; active_enumerators != null; active_enumerators = active_enumerators.next)
                active_enumerators.EntryDeleted(entry_index, entry.listNext);

            // unlink entry from the bucket list:
            if (entry.last >= 0)
                this.entries[entry.last].next = entry.next;
            else
                this.buckets[bucket_index] = entry.next;                

            if (entry.next >= 0)
                this.entries[entry.next].last = entry.last;

            // unlink entry from global list:
            if (entry.listLast >= 0)
                this.entries[entry.listLast].listNext = entry.listNext;
            else // Deleting the head of the list
                this.listHead = entry.listNext;

            if (entry.listNext >= 0)
                this.entries[entry.listNext].listLast = entry.listLast;
            else
                this.listTail = entry.listLast;

            // link entry to freeList:
            entry.next = this.freeList;
            //ignoring: entry.last, entry.listNext, entry.listLast
            entry.Value = null;
            this.freeList = entry_index;
            ++this.freeCount;
        }

        /// <summary>
        /// Removes given <paramref name="key"/> from the collection.
        /// </summary>
        /// <param name="key">Key to be removed from the collection.</param>
        /// <param name="active_enumerators">List of active enumerators so they can be updated if they point to the item being deleted.</param>
        /// <returns><c>True</c> if specified key was found and the item removed.</returns>
        /// <remarks>This operation can invalidate an existing enumerator. You can prevent this
        /// by passing <paramref name="active_enumerators"/>List of active enumerators do they can be updated.</remarks>
        public bool _del_key_or_index(ref IntStringKey key, Enumerator active_enumerators)
        {
            var nIndex = key.Integer & this.tableMask;// index(ref key);
            for (var p = this.buckets[nIndex]; p >= 0; p = this.entries[p].next)
                if (this.entries[p].KeyEquals(ref key))
                {
                    this._remove_entry(ref this.entries[p], p, nIndex, active_enumerators);
                    return true;
                }

            return false;
        }

        /// <summary>
        /// Removes specified entry from the array.
        /// </summary>
        /// <param name="p">Index of the entry within the <see cref="entries"/> array.</param>
        /// <param name="active_enumerators">List of active enumerators so they can be updated if they point to the item being deleted.</param>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _remove_entry(int p, Enumerator active_enumerators)
        {
            _remove_entry(ref this.entries[p], p, this.entries[p].Key.Integer & this.tableMask, active_enumerators);   // remove the entry
        }

        /// <summary>
        /// Removes the last entry of the array and returns it.
		/// </summary>
        /// <param name="active_enumerators">List of active enumerators so they can be updated if they point to deleted item.</param>
		/// <returns>The last entry of the array.</returns>
		/// <exception cref="InvalidOperationException">The table is empty.</exception>
        public KeyValuePair<IntStringKey, object> _remove_last(Enumerator active_enumerators)
        {
            var p = this.listTail;  // entry to be removed from the collection
            if (p < 0)
                throw new InvalidOperationException();

            var result = this.entries[p].KeyValuePair;
            _remove_entry(p, active_enumerators);

            //
            return result;
        }

        /// <summary>
        /// Removes the first entry of the array and returns it.
        /// </summary>
        /// <param name="active_enumerators">List of active enumerators so they can be updated if they point to deleted item.</param>
        /// <returns>The first entry of the array.</returns>
        /// <exception cref="InvalidOperationException">The table is empty.</exception>
        public KeyValuePair<IntStringKey, object> _remove_first(Enumerator active_enumerators)
        {
            var p = this.listHead;  // entry to be removed from the collection
            if (p < 0)
                throw new InvalidOperationException();

            var result = this.entries[p].KeyValuePair;
            _remove_entry(p, active_enumerators);

            //
            return result;
        }

        #endregion

        #region _findEntry, _tryGetValue, _get, _contains

        private int _findEntry(ref IntStringKey key)
        {
            var nIndex = key.Integer & this.tableMask;  // index(ref key);// h & ht->nTableMask;
            for (var p = this.buckets[nIndex]; p >= 0; p = entries[p].next)
                if (entries[p].KeyEquals(ref key))
                    return p;

            return -1;
        }

        private bool _tryGetValue(IntStringKey key, out object value)
        {
            var nIndex = key.Integer & this.tableMask;// index(ref key);// h & ht->nTableMask;
            var/*!*/_entries = this.entries;
            for (var p = this.buckets[nIndex]; p >= 0; p = _entries[p].next)
                if (_entries[p].KeyEquals(ref key))
                {
                    value = _entries[p].Value;
                    return true;
                }

            value = default(object);
            return false;
        }

        private bool _tryGetValue(int ikey, out object value)
        {
            var nIndex = ikey & this.tableMask;// index(ref key);// h & ht->nTableMask;
            var/*!*/_entries = this.entries;
            for (var p = this.buckets[nIndex]; p >= 0; p = _entries[p].next)
                if (_entries[p].KeyEquals(ikey))
                {
                    value = _entries[p].Value;
                    return true;
                }

            value = default(object);
            return false;
        }

        private object _get(ref IntStringKey key)
        {
            var _entries = this.entries;
            var nIndex = key.Integer & this.tableMask;// index(ref key);// h & ht->nTableMask;
            for (var p = this.buckets[nIndex]; p >= 0; p = _entries[p].next)
                if (_entries[p].KeyEquals(ref key))
                    return _entries[p].Value;

            // not found:
            return null;// throw new KeyNotFoundException();
        }
        private bool _contains(ref IntStringKey key)
        {
            return _findEntry(ref key) >= 0;
        }

        #endregion

        #region _clear, _shuffle_data, _merge_sort, _sort, _reverse, _find_max_int_key, _merge_sort

        /// <summary>
        /// Reset internal data structure (fast).
        /// </summary>
        private void _clear()
        {
            // nullify entries, so their values can be disposed:
            if (this.count > 0)
                Array.Clear(this.entries, 0, this.count);

            // destroy lists, reset counts:
            this.listHead = -1;
            this.listTail = -1;
            this.count = this.freeCount = 0;
            //this.nextNewIndex = 0;
        }
        
        /// <summary>
        /// Shuffles entries order, while keys and data are preserved.
        /// </summary>
        /// <param name="generator">Random number generator used to randomize the order.</param>
        public void _shuffle_data(Random/*!*/generator)
        {
            if (generator == null)
                throw new ArgumentNullException("generator");

            var n_elems = this.Count;
            if (n_elems <= 1)
                return;

            var elems = new int[n_elems];
            var n_left = n_elems;
            int j, p;
            var _entries = this.entries;

            // store indices of active entries
            for (j = 0, p = this.listHead; p >= 0; p = _entries[p].listNext)
                elems[j++] = p;

            // shuffle indices randomly
            while ((--n_left) > 0)
            {
                // swap elems[n_left] randomly with another entry:
                int rnd = generator.Next(0, n_left + 1);
                if (rnd < n_left)
                {
                    p = elems[n_left];
                    elems[n_left] = elems[rnd];
                    elems[rnd] = p;
                }
            }

            // reconnect the global list
            this.listHead = elems[0];
            this.listTail = -1;
            // TODO: reset instrict enumerators within the shuffle() operation

            for (j = 0; j < elems.Length; j++)  // JIT optimization
            {
                var elems_j = elems[j];
                this._enlist_to_global(ref _entries[elems_j], elems_j);

                //if (this.listTail >= 0)
                //    _entries[this.listTail].listNext = elems_j;

                //entries[elems_j].listLast = this.listTail;
                //entries[elems_j].listNext = -1;
                //this.listTail = elems_j;
            }

            // check
            this._debug_check_consistency();
        }

        /// <summary>
        /// Reverses entries order.
        /// </summary>
        public void _reverse()
        {
            var _entries = this.entries;

            int tmp;
            
            for (var p = this.listHead; p >= 0; p = _entries[p].listLast)
            {
                // swap prev/next
                tmp = _entries[p].listNext;
                _entries[p].listNext = _entries[p].listLast;
                _entries[p].listLast = tmp;
            }

            // swap head/tail
            tmp = this.listHead;
            this.listHead = this.listTail;
            this.listTail = tmp;

            //
            this._debug_check_consistency();
        }

        /// <summary>
        /// Iterate through the array and find the max integer key.
        /// </summary>
        /// <returns>Max integer key or <c>-1</c> if no positive integer key is found.</returns>
        public int _find_max_int_key()
        {
            var _entries = this.entries;

            // TODO: check flags, whether it is a simple sorted array (0..N)

            int max_key = -1;
            // iterate backwards, find the max faster
            for (int p = this.listTail; p >= 0; p = _entries[p].listLast)
            {
                if (_entries[p]._key.Integer > max_key && _entries[p]._key.IsInteger)
                    max_key = _entries[p]._key.Integer;
            }

            //
            return max_key;
        }

        /// <summary>
        /// Sort sequence of entries using merge sort. Only changes <see cref="Entry.listNext"/> fields, <see cref="Entry.listLast"/> are not modified at all.
        /// </summary>
        /// <param name="comparer">Comparer.</param>
        /// <param name="entries"><see cref="OrderedDictionary.entries"/> of table being sorted.</param>
        /// <param name="first">Index of an entry to start sorting from.</param>
        /// <param name="count">Amount if entries to sort.</param>
        /// <param name="after">Index of the entry after the sorted sequence.</param>
        /// <returns>New first entry index.</returns>
        private static int _merge_sort(IComparer<KeyValuePair<IntStringKey, object>>/*!*/ comparer, Entry[] entries, int first, int count, out int after)
        {
            Debug.Assert(first >= 0 && count > 0);

            // recursion end:
            if (count == 1)
            {
                after = entries[first].listNext;
                entries[first].listNext = -1;
                return first;
            }

            // sort recursively:
            int alen = count >> 1;
            int blen = count - alen;
            Debug.Assert(alen <= blen && alen > 0);

            // divides the portion into two lists (a and b) and sorts them:
            int result;
            var a = _merge_sort(comparer, entries, first, alen, out result);
            var b = _merge_sort(comparer, entries, result, blen, out after);

            // initializes merging - sets the first element of the result list:
            if (comparer.Compare(entries[a].KeyValuePair, entries[b].KeyValuePair) <= 0)
            {
                // if there is exactly one element in the "a" list returns (a,b) list:
                if (--alen == 0) { entries[a].listNext = b; return a; }
                result = a;
                a = entries[a].listNext;
            }
            else
            {
                // if there is exactly one element in the "b" list returns (b,a) list:
                if (--blen == 0) { entries[b].listNext = a; return b; }
                result = b;
                b = entries[b].listNext;
            }

            // merges "a" and "b" lists into the "result";
            // "iterator" points to the last element added to the "result", 
            // "a" and "b" references moves along the respective lists:
            var iterator = result;
            Debug.Assert(alen > 0 && blen > 0);
            for (; ; )
            {
                if (comparer.Compare(entries[a].KeyValuePair, entries[b].KeyValuePair) <= 0)
                {
                    // adds element from list "a" to the "result":
                    iterator = entries[iterator].listNext = a;

                    if (--alen == 0)
                    {
                        // adds remaining elements to the result: 
                        if (blen > 0) entries[iterator].listNext = b;
                        break;
                    }

                    // advances "a" pointer:
                    a = entries[a].listNext;
                }
                else
                {
                    // adds element from list "b" to the "result":
                    iterator = entries[iterator].listNext = b;

                    if (--blen == 0)
                    {
                        // adds remaining elements to the result: 
                        if (alen > 0) entries[iterator].listNext = a;
                        break;
                    }

                    // advances "a" pointer:
                    b = entries[b].listNext;
                }
            }

            return result;
        }
        
        #endregion

        #region sortops: _sort, _multisort

        internal struct sortops
        {
            /// <summary>
            /// Sorts items according to given <paramref name="comparer"/>. This changes only the order of items.
            /// </summary>
            /// <param name="table"><see cref="OrderedDictionary"/> instance to be sorted.</param>
            /// <param name="comparer">Comparer used to sort items.</param>
            internal static void _sort(OrderedDictionary/*!*/table, IComparer<KeyValuePair<IntStringKey, object>>/*!*/ comparer)
            {
                Debug.Assert(table != null);
                Debug.Assert(comparer != null);

                var count = table.Count;
                if (count <= 1) return;

                int after;
                table.listHead = _merge_sort(comparer, table.entries, table.listHead, count, out after);
                Debug.Assert(after < 0);

                // update double-linked list (prev):
                table._link_prevs_by_nexts();

                // check
                table._debug_check_consistency();
            }

            /// <summary>
            /// Sorts multiple lists given comparer for each hashtable.
            /// </summary>
            /// <param name="count">The number of items in each and every list.</param>
            /// <param name="hashtables">The lists.</param>
            /// <param name="comparers">Comparers to be used for lexicographical comparison.</param>
            internal static void _multisort(int count, PhpHashtable[]/*!!*/ hashtables, IComparer<KeyValuePair<IntStringKey, object>>[]/*!!*/ comparers)
            {
                int next;
                int length = hashtables.Length;
                int last = length - 1;

                OrderedDictionary table;

                // nothing to do:
                if (count == 0 || hashtables.Length <= 1) return;

                // interconnects all lists into a grid, heads are unchanged:
                InterconnectGrid(count, hashtables);

                // lists are only single-linked cyclic and "heads" are unchanged from here on:
                for (int i = last; i > 0; i--)
                {
                    table = hashtables[i].table;
                    // sorts i-th list (doesn't modify Prev and keeps the list cyclic):
                    table.listHead = _merge_sort(comparers[i], table.entries, table.listHead, count, out next);
                    Debug.Assert(next < 0);

                    // reorders the (i-1)-the list according to the the i-th one:
                    ReorderList(count, hashtables[i - 1].table, hashtables[i].table);
                }

                // sorts the 0-th list (its order will determine the order of whole grid rows):
                table = hashtables[0].table;
                table.listHead = _merge_sort(comparers[0], table.entries, table.listHead, count, out next);
                Debug.Assert(next < 0);

                // reorders the last list according to the 0-th one:
                ReorderList(count, hashtables[last].table, hashtables[0].table);

                // reorders remaining lists (if any):
                for (int i = last; i >= 2; i--)
                    ReorderList(count, hashtables[i - 1].table, hashtables[i].table);

                // disconnects lists from each other and reconstructs their double-linked structure:
                DisconnectGrid(count, hashtables);

                //
#if DEBUG
                for (int i = 0; i < hashtables.Length; i++)
                    hashtables[i].table._debug_check_consistency();
#endif
            }

            /// <summary>
            /// Interconnects elements of given lists into a grid using their <see cref="Entry.listLast"/> fields. <see cref="OrderedDictionary.listHead"/> is preserved.
            /// </summary>
            /// <param name="count">The number of elements in each and every list.</param>
            /// <param name="hashtables">Lists to be interconnected.</param>
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
            private static void InterconnectGrid(int count, PhpHashtable[]/*!!*/ hashtables)
            {
                int last = hashtables.Length - 1;

                var enumerators = new FastEnumerator[hashtables.Length];

                // initialize enumerators and moves them to the respective first elements:
                for (int i = 0; i < enumerators.Length; i++)
                {
                    enumerators[i] = hashtables[i].GetFastEnumerator();
                    enumerators[i].MoveNext();  // advance enumerator to first entry
                }

                while (count-- > 0)
                {
                    // sets Prev field of the first iterator:
                    enumerators[0].CurrentEntryListLast = enumerators[last].CurrentEntryIndex;

                    // all iterators except for the last one:
                    for (int i = 0; i < last; i++)
                    {
                        enumerators[i + 1].CurrentEntryListLast = enumerators[i].CurrentEntryIndex;
                        enumerators[i].MoveNext();
                    }

                    // advances the last iterator:
                    enumerators[last].MoveNext();
                }
            }

            /// <summary>
            /// Disconnects elements of lists each from other.
            /// </summary>
            /// <param name="count">The number of elements in each and every list.</param>
            /// <param name="hashtables">The lists.</param>
            private static void DisconnectGrid(int count, PhpHashtable[]/*!!*/hashtables)
            {
                for (int i = 0; i < hashtables.Length; i++)
                {
                    // restores Prev references in all elements of the i-th list except for the head:
                    hashtables[i].table._link_prevs_by_nexts();
                }
            }

            /// <summary>
            /// Reorders a minor list according to the major one. "Straightens" horizontal interconnection.
            /// </summary>
            /// <param name="count">The number of elements in each and every list.</param>
            /// <param name="minorHead">The head of a minor list (i).</param>
            /// <param name="majorHead">The head of a major list (i + 1).</param>
            /// <remarks><paramref name="minorHead"/> is the array before <paramref name="majorHead"/>.</remarks>
            private static void ReorderList(int count, OrderedDictionary minorHead, OrderedDictionary majorHead)
            {
                var major = majorHead.GetFastEnumerator(); major.MoveNext();
                var minor = minorHead.GetFastEnumerator();

                while (count-- > 0)
                {
                    minor.CurrentEntryListNext = major.CurrentEntryListLast;    // major.listLast points to minor, so we can set these links
                    minor.MoveNext();
                    major.MoveNext();
                }

                minor.CurrentEntryListNext = -1;
            }

        }

        #endregion

        #region private: _link_prevs_by_nexts, _link_nexts_by_prevs, _reverse_prev_links

        /// <summary>
        /// Update <see cref="Entry.listLast"/> and <see cref="listTail"/> according to <see cref="Entry.listNext"/>s.
        /// </summary>
        /// <remarks>Makes global list valid.</remarks>
        private void _link_prevs_by_nexts()
        {
            var _entries = this.entries;
            int last = -1;
            for (int p = this.listHead; p >= 0; p = _entries[p].listNext)
            {
                _entries[p].listLast = last;
                last = p;
            }
            this.listTail = last;

            // global list is valid now
        }

        /// <summary>
        /// Update <see cref="Entry.listNext"/> and <see cref="listHead"/> according to <see cref="Entry.listLast"/>s.
        /// </summary>
        /// <remarks>Makes global list valid.</remarks>
        private void _link_nexts_by_prevs()
        {
            var _entries = this.entries;
            int next = -1;
            for (int p = this.listTail; p >= 0; p = _entries[p].listLast)
            {
                _entries[p].listNext = next;
                next = p;
            }
            this.listHead = next;

            // global list is valid now
        }

        /// <summary>
        /// Reverses <see cref="Entry.listLast"/> links.
        /// </summary>
        /// <remarks>Global list won't be valid after this operation. However this operation reverts itself when called twice.</remarks>
        private void _reverse_prev_links()
        {
            var _entries = this.entries;
            int p, next = -1, prev;

            // reverse the listLast links
            for (p = this.listTail; p >= 0; p = prev)
            {
                prev = _entries[p].listLast;
                _entries[p].listLast = next;
                next = p;
            }

            // listTail now points to previously first entry
            this.listTail = next;            
        }

        #endregion

        #region _deep_copy_*

        /// <summary>
        /// Perform inplace deep copy of all values.
        /// </summary>
        public void _deep_copy_inplace()
        {
            var _entries = this.entries;
            for (var p = this.listHead; p >= 0; p = _entries[p].listNext)
                _deep_copy_entry_value(ref _entries[p]);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _deep_copy_entry_value(ref Entry entry)
        {
            entry.Value = PhpVariable.DeepCopy(entry.Value);
        }

        /// <summary>
        /// Perform inplace deep copy of all values.
        /// This overload replaces <paramref name="oldref"/> with <paramref name="newref"/>
        /// within <see cref="PhpReference"/> values; only of <paramref name="oldref"/> os not <c>null</c>.
        /// </summary>
        public void _deep_copy_inplace(object oldref, object newref)
        {
            if (oldref == null)
            {
                _deep_copy_inplace();
            }
            else
            {
                var _entries = this.entries;
                for (var p = this.listHead; p >= 0; p = _entries[p].listNext)
                    _deep_copy_entry_value(oldref, newref, ref _entries[p]);
            }
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _deep_copy_entry_value(object oldref, object newref, ref Entry entry)
        {
            if (entry.Value is PhpReference && ((PhpReference)entry.Value).Value == oldref)
                entry.Value = new PhpReference(newref);
            else
                entry.Value = PhpVariable.DeepCopy(entry.Value);
        }

        #endregion

        #region _ensure_item_ref, _add_or_update_preserve_ref, _ensure_item_array

        /// <summary>
        /// Checks if the updated entry contains <see cref="PhpReference"/>
        /// and if so, it updates its value instead of entry's value.
        /// 
        /// Otherwise new item is added at the end of the array.
        /// </summary>
        /// <param name="array">Caller array.</param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void _add_or_update_preserve_ref(PhpArray/*!*/array, ref IntStringKey key, object value)
        {
            Debug.Assert(!(value is PhpReference));

            this.ThrowIfShared();
            EnsureInitialized();

            int nIndex = key.Integer & this.tableMask;// index(ref key);
            var _entries = this.entries;

            // find
            int p;
            for (p = this.buckets[nIndex]; p >= 0; p = _entries[p].next) // TODO: unsafe
            {
                if (_entries[p].KeyEquals(ref key))
                {
                    PhpReference valueref;
                    if ((valueref = _entries[p].Value as PhpReference) != null) // TODO: x != null && x.GetType() == typeof(PhpReference)
                        valueref.Value = value;
                    else
                        _entries[p].Value = value;

                    return;// true;
                }
            }

            // not found, _add_last:

            // find an empty Entry to be used
            if (this.freeCount > 0)
            {
                p = this.freeList;
                this.freeList = _entries[p].next;
                --this.freeCount;
            }
            else
            {
                if (this.count == _entries.Length)
                {
                    this._do_resize();  // double the capacity

                    // update locals affected by resize:
                    nIndex = key.Integer & this.tableMask;// index(ref key);    // new index
                    _entries = this.entries;
                }
                p = this.count++;
            }

            //
            _entries[p]._key = key;
            this._enlist(ref _entries[p], p, this.buckets[nIndex]);
            _entries[p].Value = value;
            this.buckets[nIndex] = p;

            array.KeyAdded(ref key);

            return;// true;
        }

        /// <summary>
        /// Checks if the updated entry contains <see cref="PhpReference"/>
        /// and if so, it updates its value instead of entry's value.
        /// 
        /// Otherwise new item is added at the end of the array.
        /// </summary>
        /// <param name="array">Caller array.</param>
        /// <param name="ikey"></param>
        /// <param name="value"></param>
        public void _add_or_update_preserve_ref(PhpArray/*!*/array, int ikey, object value)
        {
            Debug.Assert(!(value is PhpReference));

            this.ThrowIfShared();
            EnsureInitialized();

            int nIndex = ikey & this.tableMask;// index(ref key);
            var _entries = this.entries;

            // find
            int p;
            for (p = this.buckets[nIndex]; p >= 0; p = _entries[p].next) // TODO: unsafe
            {
                if (_entries[p].KeyEquals(ikey))
                {
                    PhpReference valueref;
                    if ((valueref = _entries[p].Value as PhpReference) != null) // TODO: x != null && x.GetType() == typeof(PhpReference)
                        valueref.Value = value;
                    else
                        _entries[p].Value = value;

                    return;// true;
                }
            }

            // not found, _add_last:

            // find an empty Entry to be used
            if (this.freeCount > 0)
            {
                p = this.freeList;
                this.freeList = _entries[p].next;
                --this.freeCount;
            }
            else
            {
                if (this.count == _entries.Length)
                {
                    this._do_resize();  // double the capacity

                    // update locals affected by resize:
                    nIndex = ikey & this.tableMask;// index(ref key);    // new index
                    _entries = this.entries;
                }
                p = this.count++;
            }

            //
            _entries[p]._key = new IntStringKey(ikey);
            this._enlist(ref _entries[p], p, this.buckets[nIndex]);
            _entries[p].Value = value;
            this.buckets[nIndex] = p;

            array.KeyAdded(ikey);

            return;// true;
        }

        /// <summary>
        /// Wrap specified item into <see cref="PhpReference"/> if not yet.
        /// If there is no such item, new is created.
        /// </summary>
        /// <param name="key">Index of item to be checked.</param>
        /// <param name="array">Caller. Used to lazy copy if necessary.</param>
        /// <returns><see cref="PhpReference"/> of specified item.</returns>
        public PhpReference/*!*/_ensure_item_ref(ref IntStringKey key, PhpArray/*!*/array)
        {
            Debug.Assert(array != null, "array == null");
            Debug.Assert(array.table == this, "array.table != this");

            PhpReference valueref;
            
            var _entries = this.entries;
            var nIndex = key.Integer & this.tableMask;// index(ref key);// h & ht->nTableMask;
            for (var p = this.buckets[nIndex]; p >= 0; p = _entries[p].next)
                if (_entries[p].KeyEquals(ref key))
                {
                    var value = _entries[p].Value;
                    valueref = value as PhpReference;
                    if (valueref != null)   // value != null && value.GetType() == typeof(PhpReference)) valueref = (PhpReference)value
                    {
                        // if valueref references the array itself, array must be lazily copied:
                        if (this.IsShared && valueref.value == this.owner)
                        {
                            // shared table references itself, must be deepcopied:
                            array.EnsureWritable();
                            // "this" is not "array.table" anymore!
                            Debug.Assert(!array.table.IsShared, "array.table.IsShared");
                            Debug.Assert(array.table.entries[p].Value is PhpReference, "array.table.entries[p].Value is not PhpReference");
                            Debug.Assert(array.table != this, "array.table == this; but it shouldn't after deep copying!");
                            valueref = (PhpReference)array.table.entries[p].Value;
                        }
                    }
                    else
                    {
                        // value is not PhpReference,
                        // make the value PhpReference:
                        if (this.IsShared)
                        {
                            // we have to unshare this, so we can modify the content:
                            array.EnsureWritable();
                            // "this" is not "array.table" anymore!
                            _entries = array.table.entries;
                            _entries[p].Value = (valueref = new PhpReference(_entries[p].Value));
                        }
                        else
                        {
                            valueref = new PhpReference(_entries[p].Value);
                            _entries[p].Value = valueref;
                        }
                    }

                    //
                    return valueref;
                }

            // not found, create new item:
            valueref = new PhpReference();
            array.Add(key, valueref);    // we have to adjust maxIntKey and make the array writable; do not call _add_last directly
            return valueref;
        }

        /// <summary>
        /// Ensures specified item is <see cref="PhpArray"/>.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="array">Caler.</param>
        /// <returns></returns>
        public PhpArray/*!*/_ensure_item_array(ref IntStringKey key, PhpArray/*!*/array)
        {
            Debug.Assert(array != null, "array == null");
            Debug.Assert(array.table == this, "array.table != this");

            PhpArray valuearray;

            var _entries = this.entries;
            var nIndex = key.Integer & this.tableMask;// index(ref key);// h & ht->nTableMask;
            for (var p = this.buckets[nIndex]; p >= 0; p = _entries[p].next)
                if (_entries[p].KeyEquals(ref key))
                {
                    var value = _entries[p].Value;
                    valuearray = value as PhpArray;

                    if (valuearray == null)
                    {
                        valuearray = new PhpArray();

                        // value is not PhpReference,
                        // make the value PhpReference:
                        if (this.IsShared)
                        {
                            // we have to unshare this, so we can modify the content:
                            array.EnsureWritable();
                            // "this" is not "array.table" anymore!
                            array.table.entries[p].Value = valuearray;
                        }
                        else
                        {
                            _entries[p].Value = valuearray;
                        }
                    }

                    //
                    return valuearray;
                }

            // not found, create new item:
            valuearray = new PhpArray();
            array.Add(key, valuearray);    // we have to adjust maxIntKey and make the array writable; do not call _add_last directly
            return valuearray;
        }

        #endregion

        #region _set_operation

        /// <summary>
        /// Performs diff operation on the list of this instance and the other list.
        /// </summary>
        /// <param name="op">The operation.</param>
        /// <param name="other">The other list.</param>
        /// <param name="comparer">A comparer.</param>
        /// <param name="deleted_dummy_next">Value to be assigned to <see cref="Entry.listNext"/> to be marked as deleted.</param>
        /// <remarks>Updates only <see cref="Entry.listNext"/> links. <see cref="Entry.listLast"/>s are preserved so the operation can be reverted eventually.</remarks>
        private void _set_operation(SetOperations op, OrderedDictionary/*!*/ other, IComparer<KeyValuePair<IntStringKey, object>>/*!*/comparer, int deleted_dummy_next)
        {
            Debug.Assert(other != null && comparer != null);
            Debug.Assert(deleted_dummy_next < -1, "deleted_dummy_next has to be different than end-of-list value!");

            var _entries = this.entries;

            var other_iterator = other.GetFastEnumerator(); 
            var iterator = this.GetFastEnumerator();
            int iterator_prev_entry = -1;
            
            // advances iterators onto the first element:
            iterator.MoveNext();
            other_iterator.MoveNext();
            
            while (iterator.IsValid && other_iterator.IsValid)
            {
                int cmp = comparer.Compare(iterator.Current, other_iterator.Current);
                if (cmp > 0)
                {
                    // advance the other list iterator:
                    other_iterator.MoveNext();
                }
                else if (cmp < 0 ^ op == SetOperations.Difference)
                {
                    var next = iterator.CurrentEntryListNext;

                    // marks and skips the current element in the instance list, advances iterator:
                    if (iterator_prev_entry < 0) this.listHead = next;
                    else _entries[iterator_prev_entry].listNext = next;

                    iterator.CurrentEntryListNext = deleted_dummy_next;
                    iterator.CurrentEntryIndex = next;
                }
                else
                {
                    // advance this instance list iterator:
                    iterator_prev_entry = iterator.CurrentEntryIndex;
                    iterator.MoveNext();
                }
            }

            // marks the remaining elements:
            if (op == SetOperations.Intersection)
            {
                while (iterator.IsValid)
                {
                    var next = _entries[iterator.CurrentEntryIndex].listNext;

                    // marks and skips the current element in the instance list, advances iterator:
                    if (iterator_prev_entry < 0) this.listHead = next;
                    else _entries[iterator_prev_entry].listNext = next;

                    iterator.CurrentEntryListNext = deleted_dummy_next;
                    iterator.CurrentEntryIndex = next;
                }
            }

            //// dispose enumerators:
            //iterator.Dispose();
            //other_iterator.Dispose();
        }

        /// <summary>
        /// Retrieves the difference of this instance elemens and elements of the specified lists.
        /// </summary>
        /// <param name="op">The operation.</param>
        /// <param name="arrays">Array of arrays take away from this instance.</param>
        /// <param name="comparer">The comparer of entries.</param>
        /// <param name="result">The <see cref="IDictionary"/> where to add remaining items.</param>
        internal void _set_operation(SetOperations op, PhpHashtable[]/*!*/ arrays,
            IComparer<KeyValuePair<IntStringKey, object>>/*!*/ comparer, /*IDictionary<IntStringKey, object>*/PhpArray/*!*/ result)
        {
            Debug.Assert(arrays != null && comparer != null && result != null);

            int next;
            int count = this.Count;

            // nothing to do:
            if (count == 0) return;

            var _entries = this.entries;
            const int deleted_dummy_next = -3;
            
            // sorts this instance list (doesn't modify Prevs and keeps list cyclic):
            this.listHead = _merge_sort(comparer, _entries, this.listHead, count, out next);
            Debug.Assert(next < 0);

            foreach (var other_array in arrays)
            {
                // total number of elements in diff list:
                count = (other_array != null) ? other_array.Count : 0;

                var other_table = other_array.table;

                // result is empty - either the list is differentiated with itself or intersected with an empty set:
                if (other_table == this && op == SetOperations.Difference || count == 0 && op == SetOperations.Intersection)
                {
                    // reconstructs double linked list skipping elements marked as deleted:
                    this._link_nexts_by_prevs();

                    // the result is empty:
                    return;
                }

                // skip operation (nothing new can be added):
                if (other_table == this && op == SetOperations.Intersection || count == 0 && op == SetOperations.Difference)
                    continue;

                // sorts other_head's list (doesn't modify Prevs and keeps list cyclic):
                other_table.listHead = _merge_sort(comparer, other_table.entries, other_table.listHead, count, out next);
                Debug.Assert(next < 0);

                // applies operation on the instance list and the other list:
                this._set_operation(op, other_table, comparer, deleted_dummy_next);

                // rolls mergesort back:
                other_table._link_nexts_by_prevs();

                // instance list is empty:
                if (this.listHead < 0) break;
            }

            this._reverse_prev_links();

            // adds remaining elements to a dictionary:
            for (var iterator = this.listTail; iterator >= 0; iterator = _entries[iterator].listLast)
            {
                if (_entries[iterator].listNext != deleted_dummy_next)
                    result.Add(_entries[iterator].Key, _entries[iterator].Value);
            }

            this._reverse_prev_links();

            // reconstructs double linked list skipping elements marked as deleted:
            this._link_nexts_by_prevs();

            //
            this._debug_check_consistency();
        }

        #endregion

        #endregion

        #region Public: IsShared, Share, Unshare, ThrowIfShared, InplaceCopyOnReturn

        /// <summary>
        /// True iff the data structure is shared by more PhpHashtable instances and must not be modified.
        /// </summary>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsShared { get { return this.copiesCount > 0; } }

        /// <summary>
        /// Remember whether this instance and its owner (<see cref="PhpArray"/>) can be recycled upon returning by value from a function.
        /// </summary>
        internal bool InplaceCopyOnReturn { get { return this.copiesCount < 0; } set { this.copiesCount = value ? -1 : 0; } }

        /// <summary>
        /// Marks this instance as shared (<see cref="IsShared"/>) and returns itself.
        /// </summary>
        /// <returns></returns>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrderedDictionary/*!*/Share()
        {
            ++copiesCount;
            return this;
        }

        /// <summary>
        /// Release shared instance of internal data.
        /// </summary>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unshare()
        {
            Debug.Assert(copiesCount >= 0, "Too many Unshare() calls!");    // 0 is allowed, so noone needs this table anymore
            --copiesCount;
        }

        /// <summary>
        /// Helper method that throws if current instance is marked as shared.
        /// </summary>
        /// <exception cref="InvalidOperationException">If this instance is marked as shared.</exception>
        [Conditional("DEBUG")]
        private void ThrowIfShared()
        {
            if (this.IsShared)
                throw new InvalidOperationException("The instance is not modifiable.");
        }

        #endregion

        #region IDictionary<IntStringKey, object>

        public void Add(IntStringKey key, object value)
        {
            _add_or_update(key, value/*, false*/);
        }

        public bool ContainsKey(IntStringKey key)
        {
            return _contains(ref key);
        }

        public ICollection<IntStringKey> Keys
        {
            get
            {
                if (this._keys == null)
                    this._keys = new KeyCollection(this);

                return this._keys;
            }
        }
        private KeyCollection _keys;

        public bool Remove(IntStringKey key)
        {
            return _del_key_or_index(ref key, null);
        }

        public bool TryGetValue(IntStringKey key, out object value)
        {
            return _tryGetValue(key, out value);
        }

        public bool TryGetValue(int ikey, out object value)
        {
            return _tryGetValue(ikey, out value);
        }
        
        /// <summary>
        /// Gets a collection of values. 
        /// </summary>
        public ICollection<object>/*!*/ Values
        {
            get
            {
                if (this._values == null)
                    this._values = new ValueCollection(this);

                return this._values;
            }
        }
        [NonSerialized]
        private ValueCollection _values;

        #region Inner class: ValueCollection

        /// <summary>
        /// Auxiliary collection used for manipulating keys or values of PhpHashtable.
        /// </summary>
        [Serializable]
        public sealed class ValueCollection : ICollection<object>, ICollection
        {
            private readonly OrderedDictionary/*!*/ hashtable;

            internal ValueCollection(OrderedDictionary/*!*/ hashtable)
            {
                this.hashtable = hashtable;
            }

            #region ICollection<object> Members

            public bool Contains(object item)
            {
                using(var enumerator = hashtable.GetFastEnumerator())
                    while (enumerator.MoveNext())
                        if (enumerator.CurrentValue == item)
                            return true;

                return false;
            }

            public void CopyTo(object[]/*!*/ array, int index)
            {
                using (var enumerator = hashtable.GetFastEnumerator())
                    while (enumerator.MoveNext())
                        array[index ++] = enumerator.CurrentValue;
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
                var enumerator = hashtable.GetFastEnumerator();
                while (enumerator.MoveNext())
                    yield return enumerator.CurrentValue;
                enumerator.Dispose();
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

        #region Inner class: KeyCollection

        /// <summary>
        /// Auxiliary collection used for manipulating keys or values of PhpHashtable.
        /// </summary>
        [Serializable]
        public sealed class KeyCollection : ICollection<IntStringKey>, ICollection
        {
            private readonly OrderedDictionary/*!*/ hashtable;

            internal KeyCollection(OrderedDictionary/*!*/ hashtable)
            {
                this.hashtable = hashtable;
            }

            #region ICollection<object> Members

            public bool Contains(IntStringKey item)
            {
                using (var enumerator = hashtable.GetFastEnumerator())
                    while (enumerator.MoveNext())
                        if (enumerator.CurrentKey.Equals(ref item))
                            return true;

                return false;
            }

            public void CopyTo(IntStringKey[]/*!*/ array, int index)
            {
                using (var enumerator = hashtable.GetFastEnumerator())
                    while (enumerator.MoveNext())
                        array[index ++] = enumerator.CurrentKey;
            }

            public bool IsReadOnly { get { return true; } }

            public void Add(IntStringKey item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Remove(IntStringKey item)
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
                CopyTo((IntStringKey[])array, index);
            }

            #endregion

            #region IEnumerable<object> Members

            public IEnumerator<IntStringKey> GetEnumerator()
            {
                var enumerator = hashtable.GetFastEnumerator();
                while (enumerator.MoveNext())
                    yield return enumerator.CurrentKey;
                enumerator.Dispose();
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

        public object this[IntStringKey key]
        {
            get
            {
                return _get(ref key);
            }
            set
            {
                _add_or_update(key, value/*, false*/);
            }
        }

        public void Add(KeyValuePair<IntStringKey, object> item)
        {
            var key = item.Key;
            _add_or_update(key, item.Value/*, true*/);
        }

        public void Clear()
        {
            _clear();
        }

        public bool Contains(KeyValuePair<IntStringKey, object> item)
        {
            object value;
            return _tryGetValue(item.Key, out value) && (value == item.Value);
        }

        public void CopyTo(KeyValuePair<IntStringKey, object>[] array, int arrayIndex)
        {
            if (array == null || arrayIndex < 0 || (arrayIndex + this.Count) > array.Length)
                throw new ArgumentException();

            using (var enumerator = GetFastEnumerator())
                while (enumerator.MoveNext())
                    array[arrayIndex++] = enumerator.Current;
        }

        public int Count
        {
            get { return this.count - this.freeCount; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<IntStringKey, object> item)
        {
            var key = item.Key;
            return _del_key_or_index(ref key, null);
        }

        public IEnumerator<KeyValuePair<IntStringKey, object>>/*!*/GetEnumerator()
        {
            return new Enumerator(this, true);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this, true);
        }

        #endregion

        #region ISerializable (CLR only)
#if !SILVERLIGHT

        [System.Security.SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
            var array = new KeyValuePair<IntStringKey, object>[this.Count];
            this.CopyTo(array, 0);
            info.AddValue("KeyValuePairs", array);
		}

		/// <include file='Doc/Common.xml' path='/docs/method[@name="serialization.ctor"]/*'/>
        private OrderedDictionary(SerializationInfo/*!*/info, StreamingContext context)
            :this(null, 0)
		{
            if (info == null)
                throw new ArgumentNullException();

            var array = (KeyValuePair<IntStringKey, object>[])info.GetValue("KeyValuePairs", typeof(KeyValuePair<IntStringKey, object>[]));
            if (array != null)
                foreach (var pair in array)
                    this.Add(pair);
		}

#endif
		#endregion

        #region ICloneable

        /// <summary>
        /// Perform fast clone.
        /// </summary>
        /// <returns>Clone of <c>this</c>.</returns>
        public object Clone()
        {
            return new OrderedDictionary(this.owner, this);
        }

        #endregion

        #region IDictionary

        public void Add(object key, object value) { this.Add((IntStringKey)key, value); }
        public bool Contains(object key) { return this.Contains((IntStringKey)key); }
        IDictionaryEnumerator IDictionary.GetEnumerator() { return new Enumerator(this, false); }
        public bool IsFixedSize { get { return false; } }
        ICollection IDictionary.Keys { get { return (ICollection)this.Keys; } }
        public void Remove(object key){this.Remove((IntStringKey)key);}
        ICollection IDictionary.Values{get { return (ICollection)this.Values; }        }
        public object this[object key]
        {
            get { return this[(IntStringKey)key]; }
            set { this[(IntStringKey)key] = value; }
        }
        public void CopyTo(Array array, int index)
        {
            // KeyValuePair<IntStringKey, object>[]
            var pairs = array as KeyValuePair<IntStringKey, object>[];
            if (pairs != null)
            {
                CopyTo(pairs, index);
                return;
            }

            // DictionaryEntry[];
            var entries = array as DictionaryEntry[];
            if (entries != null)
            {
                using (var enumerator = GetFastEnumerator())
                    while (enumerator.MoveNext())
                        entries[index++] = new DictionaryEntry(enumerator.CurrentKey, enumerator.CurrentValue);
                return;
            }

            // otherwise
            throw new ArgumentException();
        }
        public bool IsSynchronized { get { return false; } }
        public object SyncRoot { get { return this; } }

        #endregion
    }
}
