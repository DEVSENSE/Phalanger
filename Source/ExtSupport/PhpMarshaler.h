//
// ExtSupport - substitute for php4ts.dll/php5ts.dll
//
// PhpMarshaler.h
// - contains declaration of PhpMarshaler class
//

#pragma once

#include "stdafx.h"
#include <stdio.h>

using namespace System;
using namespace System::Collections;
using namespace System::Collections::Specialized;
using namespace System::Runtime::CompilerServices;

/*

  Designed and implemented by Ladislav Prosek.
  
*/

namespace PHP
{
	namespace ExtManager
	{
		ref class Module;

		/// <summary>
		/// This is the marshaler for transforming managed objects into PHP variables (the <c>zval</c>
		/// structure) and vice versa. Implementing the <see cref="ICustomMarshaler"/> was not necessary
		/// here, because we have nothing to do with COM, but at least methods have standard names.
		/// </summary>
		private ref class PhpMarshaler : public System::Runtime::InteropServices::ICustomMarshaler
		{
		private:
			/// <summary>
			/// Private constructor. Use <see cref="GetInstance"/> to obtain an instance.
			/// </summary>
			PhpMarshaler(Module ^module)
			{
				this->module = module;
				this->nativeCache = gcnew System::Collections::Generic::Dictionary<IntPtr, Object^>();
				this->managedCache = gcnew System::Collections::Generic::Dictionary<Object^, IntPtr>();
			}

			/// <summary>The extension to which this marshaler instance is bound.</summary>
			Module ^module;

			/// <summary>Caches object (both managed and unmanaged) marshaled by this instance.</summary>
			/// <remarks>
			/// Always create a new instance of <see cref="PhpMarshaler"/> for each object graph to be marshaled.
			/// </remarks>
			System::Collections::Generic::Dictionary<IntPtr, Object^> ^nativeCache;
			System::Collections::Generic::Dictionary<Object^, IntPtr> ^managedCache;

			/// <summary>
			/// Converts a <c>PHP.Core.PhpArray</c> instance to Zend HashTable.
			/// </summary>
			/// <param name="enumerator">Managed enumerator of the source array.</param>
			/// <param name="ht">The destination Zend HashTable.</param>
			/// <param name="probeStringKeys"><B>True</B> if string keys should be converted to int keys when possible.
			/// </param>
			void MarshalManagedArrayToNativeArray(IDictionaryEnumerator ^enumerator, HashTable *ht, bool probeStringKeys);

			/// <summary>
			/// Fill zval struct with initialized native array of given bytes.
			/// </summary>
			/// <param name="var">zval ti be initialized with data.</param>
			/// <param name="bytes">Bytes to be copied into the zval.</param>
			void MarshalManagedBytesToNativeString(OUT zval*var, array<unsigned char> ^bytes);

		public:
			/// <summary>
			/// Returns a <see cref="PhpMarshaler"/> instance.
			/// </summary>
			/// <param name="module">The extension to which the marshaler instance should be bound.</param>
			/// <returns>The <see cref="PhpMarshaler"/> instance.</returns>
			static PhpMarshaler ^GetInstance(Module ^module)
			{
				return gcnew PhpMarshaler(module);
			}

			//
			// ICustomMarshaler implementation
			//

			/// <summary>
			/// Performs necessary cleanup of the managed data when it is no longer needed.
			/// </summary>
			/// <param name="ManagedObj">The managed object to be destroyed.</param>
			virtual void CleanUpManagedData(Object ^ManagedObj);
			
			/// <summary>
			/// Performs necessary cleanup of the unmanaged data when it is no longer needed.
			/// </summary>
			/// <param name="pNativeData">A pointer to the unmanaged data to be destroyed.</param>
			virtual void CleanUpNativeData(IntPtr pNativeData);

			/// <summary>
			/// Returns the size of the native data to be marshaled.
			/// </summary>
			/// <returns>The size in bytes of the native data.</returns>
			virtual int GetNativeDataSize();

			/// <summary>
			/// Converts the managed data to unmanaged data.
			/// </summary>
			/// <param name="ManagedObj">The managed object to be converted.</param>
			/// <returns>Returns the unmanaged view of the managed object.</returns>
			/// <remarks>
			/// Currently supported are managed variables of types: <see cref="Boolean"/>, <see cref="Int32"/>,
			/// <see cref="Double"/>, <see cref="String"/>, <c>PhpArray</c>, <c>PhpExternalResource</c>,
			/// <c>PhpObject</c> and null references.
			/// </remarks>
			virtual IntPtr MarshalManagedToNative(Object ^ManagedObj);

			/// <summary>
			/// Converts the unmanaged data to managed data.
			/// </summary>
			/// <param name="pNativeData">A pointer to the unmanaged data to be wrapped.</param>
			/// <returns>Returns the managed view of the unmanaged data.</returns>
			/// <remarks>
			/// Currently supported are unmanaged Zend variables of types: <c>IS_BOOL</c>, <c>IS_LONG</c>,
			/// <c>IS_DOUBLE</c>, <c>IS_STRING</c>, <c>IS_ARRAY</c>, <c>IS_RESOURCE</c>, <c>IS_OBJECT</c>
			/// and <c>IS_NULL</c>.
			/// </remarks>
			virtual Object ^MarshalNativeToManaged(IntPtr pNativeData);

			/// <summary>
			/// Incarnates a given <see cref="PhpObject"/> using a native representation.
			/// </summary>
			/// <param name="pNativeData"></param>
			///
			/// <param name="ManagedObj"></param>
			/// <remarks>
			/// This method is used when marshaling <c>$this</c>. <paramref name="pNativeData"/>
			/// must be of the <c>IS_OBJECT</c> type and its class must correspond to that of
			/// <paramref name="ManagedObj"/>.
			/// </remarks>
			void IncarnateNativeToManaged(IntPtr pNativeData, PHP::Core::PhpObject ^ManagedObj);

			// Static members

		public:
			/// <summary>
			/// Converts a managed to string to unmanaged string (<c>char *</c>).
			/// </summary>
			/// <param name="str">The managed string.</param>
			/// <returns>Pointer to the memory block allocated by <c>emalloc</c> where the native string is stored.</returns>
			static char *MarshalManagedStringToNativeString(String ^str);

			/// <summary>
			/// Converts a managed to string to unmanaged string (<c>char *</c>).
			/// </summary>
			/// <param name="str">The managed string.</param>
			/// <returns>Pointer to the memory block allocated by <c>malloc</c> where the native string is stored.</returns>
			static char *MarshalManagedStringToNativeStringPersistent(String ^str);
		};
	}
}
