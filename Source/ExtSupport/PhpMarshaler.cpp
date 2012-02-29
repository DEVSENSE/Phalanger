/*

 Copyright (c) 2004-2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

//
// ExtSupport - substitute for php4ts.dll/php5ts.dll
//
// PhpMarshaler.cpp
// - contains definition of PhpMarshaler class
//

#include "stdafx.h"

#include "PhpMarshaler.h"
#include "Module.h"
#include "Memory.h"
#include "Variables.h"
#include "Hash.h"
#include "Request.h"
#include "Resources.h"
#include "Objects.h"

#undef GetClassName

using namespace System;
using namespace System::IO;
using namespace System::Threading;
using namespace System::Collections;
using namespace System::Collections::Specialized;

using namespace PHP::ExtManager;

namespace PHP
{
	namespace ExtManager
	{
		// Helper methods:

		// Converts a <c>PHP.Core.PhpArray</c> instance to Zend HashTable.
		void PhpMarshaler::MarshalManagedArrayToNativeArray(IDictionaryEnumerator ^enumerator, HashTable *ht, bool probeStringKeys)
		{
			while (enumerator->MoveNext() == true)
			{
				Object ^key = enumerator->Key;
				zval *elem = (zval *)MarshalManagedToNative(enumerator->Value).ToPointer();
				
				Int32 ^p_int = dynamic_cast<System::Int32 ^>(key);
				int int_key;

				if (p_int != nullptr)
				{
					// key is an integer
					int_key = *p_int;
					
					if (zend_hash_index_update(ht, int_key, &elem, sizeof(zval *), NULL) != SUCCESS)
					{
						throw gcnew InvalidOperationException(ExtResources::GetString("native_ht_marshal_error"));
					}
				}
				else
				{
					// key is a string
					System::String ^str_key = static_cast<System::String ^>(key);

					IntStringKey iskey;
					if (probeStringKeys && (iskey = PHP::Core::Convert::StringToArrayKey(str_key)).IsInteger)
					{
						if (zend_hash_index_update(ht, iskey.Integer, &elem, sizeof(zval *), NULL) != SUCCESS)
						{
							throw gcnew InvalidOperationException(ExtResources::GetString("native_ht_marshal_error"));
						}
					}
					else
					{
						array<unsigned char> ^bytes = Request::AppConf->Globalization->PageEncoding->GetBytes(str_key);
						int length = bytes->Length;

						// stack alloc
						char *str_ptr = (char *)_alloca(length + 1);

						Marshal::Copy(bytes, 0, IntPtr(str_ptr), length);
						str_ptr[length] = 0;

						if (zend_hash_add(ht, str_ptr, length + 1, &elem, sizeof(zval *), NULL) != SUCCESS)
						{
							throw gcnew InvalidOperationException(ExtResources::GetString("native_ht_marshal_error"));
						}
					}
				}
			}
		}


		void PhpMarshaler::MarshalManagedBytesToNativeString(OUT zval*var, array<unsigned char> ^bytes)
		{
			int length = bytes->Length;

			var->type = IS_STRING;
			var->value.str.len = length;
			var->value.str.val = (char *)_emalloc(length + 1);
			if (var->value.str.val == NULL) throw gcnew OutOfMemoryException();

			if (length > 0)
			{
				// the following two lines are faster than Marshal::Copy
				pin_ptr<unsigned char> p = &(bytes[0]);
				memcpy(var->value.str.val, p, length);
			}

			var->value.str.val[length] = 0;
		}
		//
		// ICustomMarshaler implementation
		//

		// Performs necessary cleanup of the managed data when it is no longer needed.
		void PhpMarshaler::CleanUpManagedData(Object ^ManagedObj)
		{
			//managedCache = gcnew System::Collections::Generic::Dictionary<Object^, IntPtr>();
			managedCache->Remove(ManagedObj);
		}

		// Performs necessary cleanup of the unmanaged data when it is no longer needed.
		void PhpMarshaler::CleanUpNativeData(IntPtr pNativeData)
		{
			zval *var = (zval *)pNativeData.ToPointer();
			zval_ptr_dtor(&var);

			//nativeCache = gcnew System::Collections::Generic::Dictionary<IntPtr, Object^>();
			nativeCache->Remove(pNativeData);
		}

		// Returns the size of the native data to be marshaled.
		int PhpMarshaler::GetNativeDataSize()
		{
			return sizeof(zval);
		}

		// Converts the managed data to unmanaged data.
		IntPtr PhpMarshaler::MarshalManagedToNative(Object ^ManagedObj)
		{
			// have we already marshaled this object?
			IntPtr ret_val;
			if (ManagedObj != nullptr && managedCache->TryGetValue(ManagedObj, ret_val))
			{
				((zval *)ret_val.ToPointer())->refcount++;

				return ret_val;
			}

			//
			// create new zval
			//

			zval *var = (zval *)_emalloc(sizeof(zval));
			if (var == NULL) throw gcnew OutOfMemoryException();
			
			// PhpReference must be dereferenced
			PhpReference ^ref = dynamic_cast<PhpReference ^>(ManagedObj);
			if (ref != nullptr) ManagedObj = ref->value;

			var->refcount = 1;
			var->is_ref = 0;

			// IS_NULL
			if (ManagedObj == nullptr)
			{
				var->type = IS_NULL;
			}
			else switch (Type::GetTypeCode(ManagedObj->GetType()))
			{
				// Boolean
				case TypeCode::Boolean:
				{
					var->type = IS_BOOL;
					var->value.lval = *(static_cast<Boolean ^>(ManagedObj)) ? 1 : 0;
				}
				break;
			
				// Int32
				case TypeCode::Int32:
				{
					var->type = IS_LONG;
					var->value.lval = *(static_cast<Int32 ^>(ManagedObj));
				}
				break;

				// Int64
				case TypeCode::Int64:
				{
					Int64 l64 = *(static_cast<Int64 ^>(ManagedObj));
					if (sizeof(long) == sizeof(Int64))
					{	// long is int64
						var->type = IS_LONG;
						var->value.lval = (long)l64;
					}
					else
					{	// long is int32 only
						var->type = IS_DOUBLE;
						var->value.dval = (double)l64;
					}
				}
				break;

				// Double
				case TypeCode::Double:
				{
					var->type = IS_DOUBLE;
					var->value.dval = *(static_cast<Double ^>(ManagedObj));
				}
				break;
			
				// String
				case TypeCode::String:
				{
					MarshalManagedBytesToNativeString(var,Request::AppConf->Globalization->PageEncoding->GetBytes(static_cast<System::String ^>(ManagedObj)));
				}
				break;
			
				// PhpArray, PhpResource, PhpObject
				case TypeCode::Object:
				{
					PHP::Core::PhpBytes ^phpbytes;
					PHP::Core::PhpArray ^array;
					PHP::Core::PhpObject ^object;
					PHP::Core::PhpResource ^resource;

					// PhpBytes
					if ((phpbytes = dynamic_cast<PHP::Core::PhpBytes^>(ManagedObj)) != nullptr)
					{
						MarshalManagedBytesToNativeString(var,phpbytes->ReadonlyData);
					}

					// PhpArray
					else if ((array = dynamic_cast<PHP::Core::PhpArray ^>(ManagedObj)) != nullptr)
					{
						var->type = IS_ARRAY;
						ALLOC_HASHTABLE_REL(var->value.ht);
						zend_hash_init(var->value.ht, array->Count, NULL, ZVAL_PTR_DTOR, 0);
						
						IDictionaryEnumerator ^enumerator = (static_cast<IDictionary ^>(array))->GetEnumerator();
						MarshalManagedArrayToNativeArray(enumerator, var->value.ht, false);
					}

					// PhpResource
					else if ((resource = dynamic_cast<PHP::Core::PhpResource ^>(ManagedObj)) != nullptr)
					{
						PhpExternalResource ^ext_res = dynamic_cast<PhpExternalResource ^>(resource);
						if (ext_res != nullptr)
						{
							var->type = IS_RESOURCE;
							var->value.lval = ext_res->GetId();
						}
						else
						{
							//_efree(var);
							//throw gcnew ArgumentException(ExtResources::GetString("non_external_resource_marshaled"),
							//	"ManagedObj");

							// treat this problem rather "softly", note that this limitation
							// makes functions such as ftp_fget useless :(
							// possible workaround: implement a reverse (native->managed) stream wrapper
							var->type = IS_NULL;
						}
					}

					// PhpObject
					else if ((object = dynamic_cast<PHP::Core::PhpObject ^>(ManagedObj)) != nullptr)
					{
						zend_class_entry *unmng_type;

						if (object->GetType() == PHP::Library::stdClass::typeid)
						{
							// treat stdClass specially - solely for performance purposes
							unmng_type = zend_stdClass_ptr;
						}
						else
						{
							// lookup the corresponding Zend class entry
							Class ^mng_type = module->GetClassByName(object->GetType()->Name);
							if (mng_type == nullptr)
							{
								_efree(var);
								throw gcnew ArgumentException(ExtResources::GetString("unknown_class_marshaled",
									object->GetType()->FullName, module->GetFileName()), "ManagedObj");
							}

							unmng_type = mng_type->GetClassEntry();
						}
						
						// init properties array
						HashTable *properties;
						ALLOC_HASHTABLE_REL(properties);
						zend_hash_init(properties, 0, NULL, ZVAL_PTR_DTOR, 0);

						// fill properties array (only public fields are enumerated)
						IDictionaryEnumerator ^enumerator = object->GetEnumerator(nullptr);
						MarshalManagedArrayToNativeArray(enumerator, properties, true);

						// create the object
						TSRMLS_FETCH();

#ifdef PHP4TS
						_object_and_properties_init(var, unmng_type, properties ZEND_FILE_LINE_RELAY_CC TSRMLS_CC);
#elif defined(PHP5TS)
						Request ^request = Request::GetCurrentRequest(false, false);

						IntPtr tmp_var;

						if (request->ZendObjectHandles->TryGetValue(object, tmp_var))
						{
							_efree(var);
							var = (zval *)tmp_var.ToPointer();
							zend_object * zo = (zend_object *)zend_object_store_get_object(var TSRMLS_CC);
							FREE_HASHTABLE_REL(zo->properties);
							zo->properties = properties;
						}
						else
						{
							_object_and_properties_init(var, unmng_type, properties ZEND_FILE_LINE_RELAY_CC TSRMLS_CC);
							request->ZendObjectHandles->Add(object, IntPtr(var));
						}
						var->refcount++;
#endif
					}

					// unsupported type
					else
					{
						_efree(var);
						throw gcnew ArgumentException(ExtResources::GetString("unsupported_variable_marshaled",
							ManagedObj->GetType()->Name), "ManagedObj");
					}
				}
				break;

				default:
				{
					_efree(var);
					throw gcnew ArgumentException(ExtResources::GetString("unsupported_variable_marshaled",
						ManagedObj->GetType()->Name), "ManagedObj");
				}
			}

			ret_val = IntPtr(var);
			if (ManagedObj != nullptr)
				managedCache->Add(ManagedObj, ret_val);

			return ret_val;
		}

		// Converts the unmanaged data to managed data.
		Object ^PhpMarshaler::MarshalNativeToManaged(IntPtr pNativeData)
		{
			// have we already marshaled this zval?
			Object ^ret_val;
			if (pNativeData != IntPtr::Zero && nativeCache->TryGetValue(pNativeData, ret_val) )
			{
				return ret_val;
			}

			zval *var = (zval *)pNativeData.ToPointer();
			if (var == NULL) throw gcnew NullReferenceException(ExtResources::GetString("null_zval_marshaled"));

			switch (var->type)
			{
				case IS_NULL:
					ret_val = nullptr;
					break;

				case IS_BOOL:
					ret_val = (bool)(var->value.lval != 0);
					break;

				case IS_LONG:
					ret_val = var->value.lval;
					break;

				case IS_DOUBLE:
					ret_val = var->value.dval;
					break;

				case IS_STRING:
					{
						int length = var->value.str.len;
						array<unsigned char>^buffer = gcnew array<unsigned char>(length);
						if (length > 0)
						{
							pin_ptr<unsigned char> dst = &(buffer[0]);
							memcpy((void*)dst, var->value.str.val, length);
						}
						ret_val = gcnew PHP::Core::PhpBytes(buffer);
					}
					//ret_val = gcnew String(var->value.str.val, 0, var->value.str.len, Request::AppConf->Globalization->PageEncoding);
					break;

				case IS_ARRAY:	
					{
						PHP::Core::PhpArray ^array = gcnew PHP::Core::PhpArray();
						HashPosition pos;
						zval **elem;
						char *string_key;
						unsigned string_key_len;
						unsigned long num_key;

						zend_hash_internal_pointer_reset_ex(var->value.ht, &pos);
						while (zend_hash_get_current_data_ex(var->value.ht, (void **)&elem, &pos) == SUCCESS)
						{
							if (zend_hash_get_current_key_ex(var->value.ht, &string_key, &string_key_len, 
								&num_key, 0, &pos) == HASH_KEY_IS_LONG)
							{
								// key is an integer
								array->Add(num_key, MarshalNativeToManaged(IntPtr(*elem)));
							}
							else
							{
								// key is a string
								array->Add(gcnew System::String(string_key, 0, string_key_len - 1,
									Request::AppConf->Globalization->PageEncoding),
									MarshalNativeToManaged(IntPtr(*elem)));
							}

							zend_hash_move_forward_ex(var->value.ht, &pos);
						}

						ret_val = array;
						break;
					}

				case IS_RESOURCE:
					{
						char *name = zend_rsrc_list_get_rsrc_type(var->value.lval, NULL);
						ret_val = gcnew PhpExternalResource(var->value.lval, gcnew String(name ? name : ""));
						break;
					}

				case IS_OBJECT:
					{
						TSRMLS_FETCH();

						PhpObject ^object;
						Type ^wrapping_type;

						// create an instance of the corresponding managed wrapping class
						zend_class_entry *class_entry = Z_OBJCE_P(var);
						if (class_entry == zend_stdClass_ptr)
						{
							// treat stdClass specially
							object = gcnew PHP::Library::stdClass(ScriptContext::CurrentContext);
							wrapping_type = PHP::Library::stdClass::typeid;
						}
						else
						{
							// perform Class lookup in the current Module
							Class ^cls = module->GetClassByName(Class::GetZendClassName(Z_OBJCE_P(var)));
							if (cls == nullptr)
							{
								throw gcnew ArgumentException(ExtResources::GetString("unknown_class_marshaled",
									Class::GetZendClassName(class_entry), module->GetFileName()), "pNativeData");
							}
							cls->OnMarshalNativeToManaged(class_entry);

							// obtain a wrapping type
							wrapping_type = cls->GetWrappingType();
							if (wrapping_type == nullptr)
							{
								throw gcnew ArgumentException(ExtResources::GetString("wrapped_class_not_found",
									Class::GetZendClassName(class_entry), module->GetFileName()),
									"pNativeData");
							}

							// instantiate the object
							array<Object ^> ^ctor_args = { ScriptContext::CurrentContext, static_cast<Object ^>(false) };
							System::Reflection::ConstructorInfo ^cinfo = wrapping_type->GetConstructor(Emit::Types::ScriptContext_Bool);

							if (cinfo == nullptr || (object = dynamic_cast<PhpObject ^>(cinfo->Invoke(ctor_args))) == nullptr)
							{
								throw gcnew ArgumentException(ExtResources::GetString("wrapper_class_instantiation_failed",
									Class::GetZendClassName(class_entry), module->GetFileName()),
									"pNativeData");
							}
						}

						// copy Zend properties to wrapper fields
						HashTable *properties = Z_OBJPROP_P(var);
						HashPosition pos;
						zval **elem;
						char *string_key;
						unsigned string_key_len;
						unsigned long num_key;

						zend_hash_internal_pointer_reset_ex(properties, &pos);
						while (zend_hash_get_current_data_ex(properties, (void **)&elem, &pos) == SUCCESS)
						{
							if (zend_hash_get_current_key_ex(properties, &string_key, &string_key_len, 
								&num_key, 0, &pos) == HASH_KEY_IS_LONG)
							{
								// key is an integer
								object->SetPropertyDirect((int)num_key, MarshalNativeToManaged(IntPtr(*elem)));
							}
							else
							{
								// key is a string
								object->SetPropertyDirect(gcnew System::String(string_key, 0, string_key_len - 1,
									Request::AppConf->Globalization->PageEncoding),
									MarshalNativeToManaged(IntPtr(*elem)));
							}

							zend_hash_move_forward_ex(var->value.ht, &pos);
						}

						ret_val = object;
						break;
					}

				default:
					throw gcnew ArgumentException(ExtResources::GetString("unsupported_variable_marshaled",
						var->type), "pNativeData");
			}

			nativeCache->Add(pNativeData, ret_val);
			return ret_val;
		}

		// Incarnates a given <see cref="PhpObject"/> using a native representation.
		void PhpMarshaler::IncarnateNativeToManaged(IntPtr pNativeData, PhpObject ^ManagedObj)
		{
			if (ManagedObj == nullptr) throw gcnew ArgumentNullException("ManagedObj");

			zval *var = (zval *)pNativeData.ToPointer();
			if (var == NULL) throw gcnew NullReferenceException(ExtResources::GetString("null_zval_marshaled"));

			if (var->type != IS_OBJECT)
			{
				throw gcnew ArgumentException(ExtResources::GetString("non_object_incarnated",
					var->type), "pNativeData");
			}

			TSRMLS_FETCH();
			zend_class_entry *class_entry = Z_OBJCE_P(var);

			// detect class name mismatch
			String ^ntv_class_name = Class::GetZendClassName(class_entry);
			String ^mng_class_name = ManagedObj->GetType()->Name;
			if (String::Compare(ntv_class_name, mng_class_name, true) != 0)
			{
				throw gcnew ArgumentException(ExtResources::GetString("managed_native_class_mismatch",
					mng_class_name, ntv_class_name));
			}	

			// perform Class lookup in the current Module
			Class ^cls = module->GetClassByName(ntv_class_name);
			if (cls == nullptr)
			{
				throw gcnew ArgumentException(ExtResources::GetString("unknown_class_marshaled",
					Class::GetZendClassName(class_entry), module->GetFileName()), "pNativeData");
			}
			cls->OnMarshalNativeToManaged(class_entry);

			// copy Zend properties to wrapper fields
			HashTable *properties = Z_OBJPROP_P(var);
			HashPosition pos;
			zval **elem;
			char *string_key;
			unsigned string_key_len;
			unsigned long num_key;

			if (ManagedObj->RuntimeFields != nullptr) ManagedObj->RuntimeFields->Clear();

			zend_hash_internal_pointer_reset_ex(properties, &pos);
			while (zend_hash_get_current_data_ex(properties, (void **)&elem, &pos) == SUCCESS)
			{
				if (ManagedObj->RuntimeFields == nullptr) ManagedObj->RuntimeFields = gcnew PhpArray();

				if (zend_hash_get_current_key_ex(properties, &string_key, &string_key_len, 
					&num_key, 0, &pos) == HASH_KEY_IS_LONG)
				{
					// key is an integer
					ManagedObj->RuntimeFields->Add(num_key.ToString(), MarshalNativeToManaged(IntPtr(*elem)));
				}
				else
				{
					// key is a string
					ManagedObj->RuntimeFields->Add(gcnew System::String(string_key, 0, string_key_len - 1,
						Request::AppConf->Globalization->PageEncoding),
						MarshalNativeToManaged(IntPtr(*elem)));
				}

				zend_hash_move_forward_ex(var->value.ht, &pos);
			}
		}

		// Converts a managed to string to unmanaged string (<c>char *</c>), uses <c>emalloc</c>.
		char *PhpMarshaler::MarshalManagedStringToNativeString(String ^str)
		{
			if (str == nullptr) return nullptr;

			array<unsigned char> ^bytes = Request::AppConf->Globalization->PageEncoding->GetBytes(str);
			int length = bytes->Length;

			char *ret_value = (char *)emalloc(length + 1);
			if (ret_value == NULL) throw gcnew OutOfMemoryException();
			
			Marshal::Copy(bytes, 0, IntPtr(ret_value), length);
			ret_value[length] = 0;

			return ret_value;
		}

		// Converts a managed to string to unmanaged string (<c>char *</c>), uses <c>malloc</c>.
		char *PhpMarshaler::MarshalManagedStringToNativeStringPersistent(String ^str)
		{
			if (str == nullptr) return NULL;

			array<unsigned char> ^bytes = Request::AppConf->Globalization->PageEncoding->GetBytes(str);
			int length = bytes->Length;

			char *ret_value = (char *)malloc(length + 1);
			if (ret_value == NULL) throw gcnew OutOfMemoryException();

			Marshal::Copy(bytes, 0, IntPtr(ret_value), length);
			ret_value[length] = 0;

			return ret_value;
		}
	}
}
