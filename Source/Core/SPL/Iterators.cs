/*

 Copyright (c) 2005-2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using PHP.Core;

namespace PHP.Library.SPL
{
	/// <summary>
	/// Built-in marker interface.
	/// </summary>
	[ImplementsType]
	public interface Traversable
	{ }

	/// <summary>
	/// Interface for external iterators or objects that can iterate themselves internally.
	/// </summary>
	/// <remarks>
	/// Note that contrary to the .NET framework enumerating interfaces,
	/// calling <c>rewind</c> positions the iterator on the first element, so <c>next</c>
	/// shall not be called until the first element is retrieved.
	/// </remarks>
	[ImplementsType]
	public interface Iterator : Traversable
	{
		/// <summary>
		/// Rewinds the iterator to the first element.
		/// </summary>
		[ImplementsMethod]
		object rewind(ScriptContext context);

		/// <summary>
		/// Moves forward to next element.
		/// </summary>
		[ImplementsMethod]
		object next(ScriptContext context);

		/// <summary>
		/// Checks if there is a current element after calls to <see cref="rewind"/> or <see cref="next"/>.
		/// </summary>
		[ImplementsMethod]
		object valid(ScriptContext context);

		/// <summary>
		/// Returns the key of the current element.
		/// </summary>
		[ImplementsMethod]
		object key(ScriptContext context);

		/// <summary>
		/// Returns the current element (value).
		/// </summary>
		[ImplementsMethod]
		[AllowReturnValueOverride]
		object current(ScriptContext context);
	}

    /// <summary>
    /// The Seekable iterator.
    /// </summary>
    [ImplementsType]
    public interface SeekableIterator : Iterator
    {
        /// <summary>
        /// Seeks to a given position in the iterator.
        /// </summary>
        [ImplementsMethod]
        [AllowReturnValueOverride]
        object seek(ScriptContext context, object position);
    }

	/// <summary>
	/// Interface to create an external iterator.
	/// </summary>
	/// <remarks>
	/// This interface contains only arg-less stubs as signatures should not be restricted.
	/// </remarks>
	[ImplementsType]
	public interface IteratorAggregate : Traversable
	{
		/// <summary>
		/// Returns an <see cref="Iterator"/> or another <see cref="IteratorAggregate"/> for
		/// the implementing object.
		/// </summary>
		[ImplementsMethod]
		object getIterator(ScriptContext context);
	}
}
