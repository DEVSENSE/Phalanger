//
// ExtSupport - substitute for php4ts.dll/php5ts.dll
//
// Module.h
// - contains declaration of Constant class
// - contains declaration of Function class
// - contains declaration of Method class
// - contains declaration of CallerMethod class
// - contains declaration of ConstructorMethod class
// - contains declaration of GetterMethod class
// - contains declaration of SetterMethod class
// - contains declaration of Class class
// - contains declaration of IniEntry class
// - contains declaration of Module class
//

#pragma once

#include "stdafx.h"
#include "Objects.h"
#include "PhpMarshaler.h"
#include "VirtualWorkingDir.h"
#include "Memory.h"
#include "TsrmLs.h"

#undef GetClassName

using namespace System;
using namespace System::Collections;
using namespace System::Runtime::InteropServices;

/*

  Designed and implemented by Ladislav Prosek.
  
*/

extern MUTEX_T mx_module_entries;

/// <summary>
/// Holds information about one class registered by an extension.
/// </summary>
struct unmng_class_entry
{
	zend_class_entry *original_entry; // as given by the extension
	zend_class_entry *entry;          // copied
	zend_class_entry *parent;         // should point to the appropriate _copy_
};

/// <summary>
/// Holds information about classes and contants registered by one extension.
/// </summary>
struct unmng_class_and_constant_info
{
	HashTable *class_entries;
	HashTable *constant_entries;
};

// Performs unmanaged class registration (necessary for reregistration in another AppDomain).
void unmng_register_class(int module_number, zend_class_entry *ce, zend_class_entry *parent_ce);

// Performs unmanaged constant registration (necessary for reregistration in another AppDomain).
void unmng_register_constant(zend_constant *c);

namespace PHP
{
	namespace ExtManager
	{
		ref class Class;
		ref class Module;
		ref class Request;

		/// <summary>
		/// Represents a constant defined by an extension.
		/// </summary>
		/// <remarks>
		/// Instances of this class are stored in hash tables keyed by constant name. The reason why
		/// the constant's value is not stored in hash tables directly is that the Case-Sensitive
		/// flag also needs to be remembered.
		/// </remarks>
		private ref class Constant
		{
		public:
			/// <summary>
			/// Creates a new <see cref="Constant"/>.
			/// </summary>
			/// <param name="mod">Reference to the module (extension) that has defined this constant.</param>
			/// <param name="_value">The value of this constant.</param>
			/// <param name="_name">The name of this constant.</param>
			/// <param name="_caseInsensitive">Specifies whether this constant is case insensitive.</param>
			Constant(Module ^mod, String ^_name, Object ^_value, bool _caseInsensitive)
			{
				containingModule = mod;
				name = _name;
				value = _value;
				caseInsensitive = _caseInsensitive;
			}

			/// <summary>
			/// Adds this constant to a <see cref="IDictionary"/>.
			/// </summary>
			/// <param name="dictionary">The <see cref="IDictionary"/> to add this constant to.</param>
			/// <param name="name">The name of this constant.</param>
			/// <returns><B>true</B> if successfully added, <B>false</B> otherwise.</returns>
			bool AddToDictionary(IDictionary ^dictionary, String ^name);

			/// <summary>
			/// Retrievs a constant from a <see cref="IDictionary"/>.
			/// </summary>
			/// <param name="dictionary">The <see cref="IDictionary"/> to retrieve a constant from.</param>
			/// <param name="name">The name of the constant to retrieve.</param>
			/// <returns>The <see cref="Constant"/> or <B>null</B> if not found.</returns>
			static Constant ^RetrieveFromDictionary(IDictionary ^dictionary, String ^name);

			/// <summary>
			/// Returns the name of this constant.
			/// </summary>
			/// <returns>The name of this constant.</returns>
			String ^GetName()
			{
				return name;
			}

			/// <summary>
			/// Returns the value of this constant.
			/// </summary>
			/// <returns>The value of this constant.</returns>
			Object ^GetValue()
			{
				return value;
			}

			/// <summary>
			/// Returns <B>true</B> if this <see cref="Constant"/> is case insensitive, <B>false</B> otherwise.
			/// </summary>
			/// <returns>
			/// <B>true</B> if this <see cref="Constant"/> is case insensitive, <B>false</B> otherwise.
			/// </returns>
			bool IsCaseInsensitive()
			{
				return caseInsensitive;
			}

			/// <summary>
			/// Returns the containing <see cref="Module"/> of this <see cref="Constant"/>.
			/// </summary>
			/// <returns>The containing <see cref="Module"/>.</returns>
			Module ^GetContainingModule()
			{
				return containingModule;
			}

			/// <summary>
			/// Registers this constant in the appropriate context (module or request).
			/// </summary>
			/// <param name="persistent"><B>true</B> if the constant should be registered as persistent,
			/// <B>false</B> otherwise.</param>
			/// <returns><B>true</B> if the constant was successfully registered, <B>false</B> otherwise.
			/// </returns>
			bool Register(bool persistent);

			/// <summary>
			/// Looks up a constant based on its name.
			/// </summary>
			/// <param name="name">The name of the constant.</param>
			/// <returns>The constant or <B>null</B> if not found.</returns>
			static Constant ^Lookup(String ^name);

		private:
			/// <summary>
			/// Specifies whether this <see cref="Constant"/> is case insensitive.
			/// </summary>
			/// <remarks>
			/// Case insensitive constants are converted to lower case before adding to a dictionary
			/// and loookups.
			/// <remarks>
			bool caseInsensitive;

			///<summary>The name of this constant.</summary>
			String ^name;

			///<summary>The value of this constant.</summary>
			Object ^value;

			/// <summary>The <see cref="Module"/> that has defined this constant.</summary>
			Module ^containingModule;
		};
		
		/// <summary>
		/// Represents a function defined by an extension. 
		/// </summary>
		/// <remarks>
		/// Instances of this class are stored in hash tables keyed by function name.
		/// </remarks>
		private ref class Function : public MarshalByRefObject, public PHP::Core::IExternalFunction
		{
		public:

			virtual Object^ Invoke(PhpObject^ self, array<Object ^> ^%args, array<int> ^refInfo, String ^workingDir);

			// ignored here, implemented in Externasl.CLR.cs
			virtual property IExternals^ ExtManager{IExternals^ get(){throw gcnew NotImplementedException();}}
		
		internal:
			/// <summary>
			/// Creates a new <see cref="Function"/>.
			/// </summary>
			/// <param name="mod">Reference to the module (extension) this function is defined in.</param>
			/// <param name="func">Unmanaged pointer to the corresponding <c>zend_function_entry</c> block.</param>
			Function(Module ^mod, zend_function_entry *func)
			{
				functionEntry = func;
				containingModule = mod;
			}

			/// <summary>
			/// Returns unmanaged pointer to the function itself (the handler).
			/// </summary>
			/// <returns>Unmanaged pointer to the function entry point.</returns>
			virtual void (*GetFunctionPtr())(INTERNAL_FUNCTION_PARAMETERS)
			{
				return functionEntry->handler;
			}

			/// <summary>
			/// Returns the name of this function.
			/// </summary>
			/// <returns>The name of this function.</returns>
			virtual String ^GetFunctionName()
			{
				return gcnew String(functionEntry->fname);
			}

			/// <summary>
			/// Returns the containing <see cref="Module"/> of this <see cref="Function"/>.
			/// </summary>
			/// <returns>The containing <see cref="Module"/>.</returns>
			Module ^GetContainingModule()
			{
				return containingModule;
			}

			/// <summary>
			/// Determines whether a parameter must always be passed by reference.
			/// </summary>
			/// <param name="paramNumber">Zero-based parameter index.</param>
			/// <returns><B>true</B> if the parameter with index <paramref name="paramNumber"/> must be
			/// passed by reference, <B>false</B> otherwise.
			virtual bool ParameterForcedByRef(int paramNumber)
			{
#if defined(PHP4TS)
				unsigned char *arg_types = functionEntry->func_arg_types;
				return (arg_types != NULL &&
					((paramNumber < arg_types[0] && arg_types[paramNumber + 1] == BYREF_FORCE) ||
					arg_types[arg_types[0]] == BYREF_FORCE_REST));
#elif defined(PHP5TS)
				_zend_arg_info *info = functionEntry->arg_info;
				return (info != NULL &&
					((paramNumber < (int)functionEntry->num_args && info[paramNumber + 1].pass_by_reference) ||
					info[0].pass_by_reference));
#else
				Debug::Assert(false);
				return false;
#endif
			}

			/// <summary>
			/// Invokes this function with given parameters.
			/// </summary>
			/// <param name="retValueUsed">Specifies whether the caller is interested in return value.</param>
			/// <param name="self">The instance on which the function should be invoked (if it is a method).</param>
			/// <param name="args">Function parameters.</param>
			/// <param name="refInfo">Indexes into the <paramref name="args"/> marking parameters that should
			/// be passed by reference, and optionally terminated with <c>-1</c> which means that from
			/// the last marked parameter on, everything should be passed by reference.</param>
			/// <param name="propertyReference">Additional parameter to be passed to the function handler,
			/// used by the <c>handle_function_call</c> handler.</param>
			/// <returns>The return value (only valid if <paramref name="retValueUsed"/> is <B>true</B>.</returns>
			virtual Object ^Invoke(Request ^request, bool retValueUsed, PHP::Core::PhpObject ^self, array<Object ^> ^args,
				array<int> ^refInfo, zend_property_reference *propertyReference);

		protected:
			/// <summary>
			/// Construct a bool array marking parameters to be passed by reference and check whether all
			/// parameters that are forced to be passed by reference actually are.
			/// </summary>
			/// <param name="request">Current <see cref="Request"/>.</param>
			/// <param name="numArgs">Number of actual paramaters.</param>
			/// <param name="refInfo">Parameter indexes marking parameters that should be passed by reference,
			/// and optionally terminated with <c>-1</c> which means that from the last marked parameter on,
			/// everything should be passed by reference.</param>
			/// <returns><B>true</B> if successfully validated, <B>false</B> otherwise.</returns>
			bool ValidateParameters(Request ^request, int numArgs, array<int> ^refInfo);

			/// <summary>
			/// Unmanaged pointer to the corresponding <c>zend_function_entry</c> block.
			/// </summary>
			zend_function_entry *functionEntry;

			/// <summary>
			/// Reference to the module (extension) this function is defined in.
			/// </summary>
			Module ^containingModule;
		};

		/// <summary>
		/// Represents a method defined by an extension. 
		/// </summary>
		/// <remarks>
		/// Instances of this class are stored in hash tables keyed by method name.
		/// </remarks>
		private ref class Method : public Function
		{
		public:
			

		internal:
			/// <summary>
			/// Creates a new <see cref="Method"/>.
			/// </summary>
			/// <param name="cls">Reference to the class this method is defined in.</param>
			/// <param name="func">Unmanaged pointer to the corresponding <c>zend_function_entry</c> block.</param>
			Method(Class ^cls, zend_function_entry *func);

#if defined(PHP5TS)
			/// <summary>
			/// Allocates a <c>zend_function</c> describing this instance.
			/// </summary>
			/// <returns>Pointer to the <c>zend_function</c>.</returns>
			zend_function *CreateZendFunction();
#endif

			/// <summary>
			/// Returns the declaring <see cref="Class"/> of this <see cref="Method"/>.
			/// </summary>
			/// <returns>The declaring <see cref="Class"/>.</returns>
			Class ^GetDeclaringClass()
			{
				return declaringClass;
			}

			/// <summary>
			/// Invokes this method with given parameters.
			/// </summary>
			/// <param name="retValueUsed">Specifies whether the caller is interested in return value.</param>
			/// <param name="self">The instance on which the function should be invoked.</param>
			/// <param name="args">Method parameters.</param>
			/// <param name="refInfo">Indexes into the <paramref name="args"/> marking parameters that should
			/// be passed by reference, and optionally terminated with <c>-1</c> which means that from
			/// the last marked parameter on, everything should be passed by reference.</param>
			/// <param name="propertyReference">Additional parameter to be passed to the function handler,
			/// used by the <c>handle_function_call</c> handler.</param>
			/// <returns>The return value (only valid if <paramref name="retValueUsed"/> is <B>true</B>.</returns>
			/// <remarks>Checks are made to ensure that <paramref name="self"/> is valid.</remarks>
			virtual Object ^Invoke(Request ^request, bool retValueUsed, PHP::Core::PhpObject ^self, array<Object ^> ^args,
				array<int> ^refInfo, zend_property_reference *propertyReference) override;

		protected:
			/// <summary>
			/// Reference to the class this method is defined in.
			/// </summary>
			Class ^declaringClass;
		};

#if defined(PHP4TS) || defined (PHP5TS)
		/// <summary>
		/// Represents a <c>handle_function_call</c> handler defined by an extension.
		/// </summary>
		/// <remarks>
		/// The handler is wrapped by this class in order to be callable like a <c>__call</c> method.
		/// </remarks>
		private ref class CallerMethod : public Method
		{
		public:
#ifdef PHP4TS
			/// <summary>
			/// Creates a new <see cref="CallerMethod"/>.
			/// </summary>
			/// <param name="cls">Reference to the class this method is defined in.</param>
			/// <param name="handler">Unmanaged pointer to the corresponding <c>handle_function_call</c> function.</param>
			CallerMethod(Class ^cls,
				void (*handler)(INTERNAL_FUNCTION_PARAMETERS, zend_property_reference *property_reference))
				: Method(cls, NULL)
			{
				this->handler = (void (*)(INTERNAL_FUNCTION_PARAMETERS))handler;
			}
#elif defined(PHP5TS)
			/// <summary>
			/// Creates a new <see cref="CallerMethod"/>.
			/// </summary>
			/// <param name="cls">Reference to the class this method is defined in.</param>
			/// <param name="handler">Unmanaged pointer to the corresponding <c>handle_function_call</c> function.</param>
			CallerMethod(Class ^cls,
				void (*handler)(INTERNAL_FUNCTION_PARAMETERS))
				: Method(cls, NULL)
			{
				this->handler = handler;
			}
#endif

			/// <summary>
			/// Returns unmanaged pointer to the function itself (the handler).
			/// </summary>
			/// <returns>Unmanaged pointer to the function entry point.</returns>
			virtual void (*GetFunctionPtr())(INTERNAL_FUNCTION_PARAMETERS) override
			{
				return handler;
			}

			/// <summary>
			/// Invokes the handler.
			/// </summary>
			/// <param name="retValueUsed">Specifies whether the caller is interested in return value.</param>
			/// <param name="self">The instance on which the handler should be invoked.</param>
			/// <param name="args">Method parameters.</param>
			/// <param name="refInfo">Indexes into the <paramref name="args"/> marking parameters that should
			/// be passed by reference, and optionally terminated with <c>-1</c> which means that from
			/// the last marked parameter on, everything should be passed by reference.</param>
			/// <param name="methodName">The name of the method to invoke.</param>
			/// <returns>The return value (only valid if <paramref name="retValueUsed"/> is <B>true</B>.</returns>
			Object ^InvokeHandler(Request ^request, bool retValueUsed, PHP::Core::PhpObject ^self, array<Object ^> ^args,
				array<int> ^refInfo, String ^methodName);

			/// <summary>
			/// Determines whether a parameter must always be passed by reference.
			/// </summary>
			/// <param name="paramNumber">Zero-based parameter index.</param>
			/// <returns><B>true</B> if the parameter with index <paramref name="paramNumber"/> must be
			/// passed by reference, <B>false</B> otherwise.
			virtual bool ParameterForcedByRef(int paramNumber) override
			{
				// handle_function_call has no info block with such information
				return false;
			}

			
			/// <summary>
			/// Returns the name of this &quot;function&quot;.
			/// </summary>
			/// <returns>The name of this function.</returns>
			virtual String ^GetFunctionName() override
			{
#pragma warning (push)
#pragma warning (disable: 4395)
				return PHP::Core::PhpObject::SpecialMethodNames::Call.ToString();
#pragma warning (pop)
			}

			
			/// <summary>
			/// Invokes the handler as a <c>__call</c> method - name of the method to invoke is expected in argument 0.
			/// </summary>
			/// <param name="retValueUsed">Specifies whether the caller is interested in return value.</param>
			/// <param name="self">The instance on which the handler should be invoked.</param>
			/// <param name="args">Method name followed by method parameters.</param>
			/// <param name="refInfo">Indexes into the <paramref name="args"/> marking parameters that should
			/// be passed by reference, and optionally terminated with <c>-1</c> which means that from
			/// the last marked parameter on, everything should be passed by reference.</param>
			/// <param name="propertyReference">Additional parameter to be passed to the function handler,
			/// used by the <c>handle_function_call</c> handler.</param>
			/// <returns>The return value (only valid if <paramref name="retValueUsed"/> is <B>true</B>.</returns>
			virtual Object ^Invoke(Request ^request, bool retValueUsed, PHP::Core::PhpObject ^self, array<Object ^> ^args,
				array<int> ^refInfo, zend_property_reference *propertyReference) override;

		protected:
			/// <summary>Unmanaged pointer to the corresponding <c>handle_function_call</c> function.</summary>
			void (*handler)(INTERNAL_FUNCTION_PARAMETERS);
		};
#endif
#ifdef PHP4TS

		/// <summary>
		/// Represents an artificial PHP constructor.
		/// </summary>
		/// <remarks>
		/// <para>
		/// When a user-defined PHP class has no constructor, the <c>__call</c> method - if defined - is not called
		/// during instantiation. However, if an extension defines a class with a non-null <c>handle_function_call</c>
		/// handler (for example <c>Java</c> class in <c>php_java</c> extension), this handler is called during
		/// instantiation. That's why this artificial constructor is added to such classes.
		/// </para>
		/// <para>
		/// This method is added to the declaring class's method table under the <c>$(class_name)</c> name, so that
		/// it is recognized as a PHP contructor by managed wrapper generator as well as by certain object support
		/// routines at run-time.
		/// </para>
		/// </remarks>
		private ref class ConstructorMethod : public Method
		{
		public:
			/// <summary>
			/// Creates a new <see cref="ConstructorMethod"/>.
			/// </summary>
			/// <param name="callerMethod">The <see cref="CallerMethod"/> that invocations of this method
			/// should be delegated to.</param>
			ConstructorMethod(CallerMethod ^callerMethod) : Method(callerMethod->GetDeclaringClass(), NULL)
			{
				this->callerMethod = callerMethod;
			}

			/// <summary>
			/// Returns the name of this function.
			/// </summary>
			/// <returns>The name of this function.</returns>
			virtual String ^GetFunctionName() override;

			/// <summary>
			/// Invokes the associated <see cref="CallerMethod"/> with given parameters.
			/// </summary>
			/// <param name="retValueUsed">Specifies whether the caller is interested in return value.</param>
			/// <param name="self">The instance on which the handler should be invoked.</param>
			/// <param name="args">Constructor parameters.</param>
			/// <param name="refInfo">Indexes into the <paramref name="args"/> marking parameters that should
			/// be passed by reference, and optionally terminated with <c>-1</c> which means that from
			/// the last marked parameter on, everything should be passed by reference.</param>
			/// <param name="propertyReference">Additional parameter to be passed to the function handler,
			/// used by the <c>handle_function_call</c> handler.</param>
			/// <returns>The return value (only valid if <paramref name="retValueUsed"/> is <B>true</B>.</returns>
			virtual Object ^Invoke(Request ^request, bool retValueUsed, PHP::Core::PhpObject ^self, array<Object ^> ^args,
				array<int> ^refInfo, zend_property_reference *propertyReference) override;

		protected:
			/// <summary>The associated <see cref="CallerMethod"/>.</summary>
			CallerMethod ^callerMethod;
		};

		/// <summary>
		/// Represents a <c>handle_propery_get</c> handler defined by an extension.
		/// </summary>
		/// <remarks>
		/// The handler is wrapped by this class in order to be callable like a <c>__get</c> method.
		/// </remarks>
		private ref class GetterMethod : public Method
		{
		public:
			/// <summary>
			/// Creates a new <see cref="GetterMethod"/>.
			/// </summary>
			/// <param name="cls">Reference to the class this method is defined in.</param>
			/// <param name="handler">Unmanaged pointer to the corresponding <c>handle_property_get</c> function.</param>
			GetterMethod(Class ^cls, zval (*handler)(zend_property_reference *property_reference))
				: Method(cls, NULL)
			{
				this->handler = handler;
			}

			/// <summary>
			/// Returns the name of this function.
			/// </summary>
			/// <returns>The name of this function.</returns>
			virtual String ^GetFunctionName() override
			{
#pragma warning (push)
#pragma warning (disable: 4395)
				return PHP::Core::PhpObject::SpecialMethodNames::Get.ToString();
#pragma warning (pop)
			}

			/// <summary>
			/// Invokes the handler as a <c>__get</c> method - name of the field to retrieve is expected in argument 0.
			/// </summary>
			/// <param name="retValueUsed">Specifies whether the caller is interested in return value.</param>
			/// <param name="self">The instance on which the handler should be invoked.</param>
			/// <param name="args">Field name.</param>
			/// <param name="refInfo">Indexes into the <paramref name="args"/> marking parameters that should
			/// be passed by reference, and optionally terminated with <c>-1</c> which means that from
			/// the last marked parameter on, everything should be passed by reference.</param>
			/// <param name="propertyReference">Additional parameter to be passed to the function handler,
			/// used by the <c>handle_function_call</c> handler.</param>
			/// <returns>The return value (only valid if <paramref name="retValueUsed"/> is <B>true</B>.</returns>
			virtual Object ^Invoke(Request ^request, bool retValueUsed, PHP::Core::PhpObject ^self, array<Object ^> ^args,
				array<int> ^refInfo, zend_property_reference *propertyReference) override;

		protected:
			/// <summary>Unmanaged pointer to the corresponding <c>handle_property_get</c> function.</summary>
			zval (*handler)(zend_property_reference *property_reference);
		};

		/// <summary>
		/// Represents a <c>handle_propery_set</c> handler defined by an extension.
		/// </summary>
		/// <remarks>
		/// The handler is wrapped by this class in order to be callable like a <c>__set</c> method.
		/// </remarks>
		private ref class SetterMethod : public Method
		{
		public:
			/// <summary>
			/// Creates a new <see cref="SetterMethod"/>.
			/// </summary>
			/// <param name="cls">Reference to the class this method is defined in.</param>
			/// <param name="handler">Unmanaged pointer to the corresponding <c>handle_property_set</c> function.</param>
			SetterMethod(Class ^cls, int (*handler)(zend_property_reference *property_reference, zval *value))
				: Method(cls, NULL)
			{
				this->handler = handler;
			}

			/// <summary>
			/// Returns the name of this function.
			/// </summary>
			/// <returns>The name of this function.</returns>
			virtual String ^GetFunctionName() override
			{
#pragma warning (push)
#pragma warning (disable: 4395)
				return PHP::Core::PhpObject::SpecialMethodNames::Set.ToString();
#pragma warning (pop)
			}

			/// <summary>
			/// Invokes the handler as a <c>__set</c> method - name of the field to set or an array of
			/// <see cref="RuntimeChainElement"/>s is expected in argument 0, the new field value is expected
			/// in argument 1.
			/// </summary>
			/// <param name="retValueUsed">Specifies whether the caller is interested in return value.</param>
			/// <param name="self">The instance on which the handler should be invoked.</param>
			/// <param name="args">Field name followed by field value.</param>
			/// <param name="refInfo">Indexes into the <paramref name="args"/> marking parameters that should
			/// be passed by reference, and optionally terminated with <c>-1</c> which means that from
			/// the last marked parameter on, everything should be passed by reference.</param>
			/// <param name="propertyReference">Additional parameter to be passed to the function handler,
			/// used by the <c>handle_function_call</c> handler.</param>
			/// <returns>The return value (only valid if <paramref name="retValueUsed"/> is <B>true</B>.</returns>
			virtual Object ^Invoke(Request ^request, bool retValueUsed, PHP::Core::PhpObject ^self, array<Object ^> ^args,
				array<int> ^refInfo, zend_property_reference *propertyReference) override;

		protected:
			/// <summary>Unmanaged pointer to the corresponding <c>handle_property_set</c> function.</summary>
			int (*handler)(zend_property_reference *property_reference, zval *value);
		};
#elif defined (PHP5TS)
		private ref class SpecialMethod : public Method
		{
		public:

			/// <summary>
			/// Creates a new <see cref="SpecialMethod"/>, such as _get, _set, etc.
			/// </summary>
			/// <param name="cls">Reference to the class this method is defined in.</param>
			/// <param name="handler">Unmanaged pointer to the corresponding <c>zend_function</c> function.</param>
			SpecialMethod(String^ name, Class ^cls,
				void (*handler)(INTERNAL_FUNCTION_PARAMETERS))
				: Method(cls, NULL)
			{
				this->handler = handler;
				this->name = name;
			}

			/// <summary>
			/// Returns the name of this &quot;function&quot;.
			/// </summary>
			/// <returns>The name of this function.</returns>
			virtual String ^GetFunctionName() override
			{
				return name;
			}

			/// <summary>
			/// Returns unmanaged pointer to the function itself (the handler).
			/// </summary>
			/// <returns>Unmanaged pointer to the function entry point.</returns>
			virtual void (*GetFunctionPtr())(INTERNAL_FUNCTION_PARAMETERS) override
			{
				return handler;
			}

		protected:
			/// <summary>Unmanaged pointer to the corresponding <c>handle_function_call</c> function.</summary>
			void (*handler)(INTERNAL_FUNCTION_PARAMETERS);

			/// <summary>Name of the special function.</summary>
			String^ name;
		};
#endif

		/// <summary>
		/// Represents a class defined by an extension. 
		/// </summary>
		/// <remarks>
		/// Instances of <see cref="Class"/> are stored in hash tables keyed by class name.
		/// </remarks>
		private ref class Class : public IDisposable
		{
		public:
			/// <summary>
			/// Creates a new <see cref="Class"/>.
			/// </summary>
			/// <param name="mod">Reference to the module (extension) this class is defined in.</param>
			/// <param name="entry">Unmanaged pointer to the corresponding <c>zend_class_entry</c> block.</param>
			/// <param name="parentEntry">Unmanaged pointer to the parent <c>zend_class_entry</c> block.</param>
			Class(Module ^mod, zend_class_entry *entry, zend_class_entry *parentEntry);

			/// <summary>
			/// Returns the name of this class.
			/// </summary>
			/// <returns>The name of this class.</returns>
			String ^GetClassName()
			{
				if (classEntry->name == NULL) return String::Empty;
				return gcnew String(classEntry->name, 0, classEntry->name_length);
			}

			/// <summary>
			/// Returns the name of the base class.
			/// </summary>
			/// <returns>The name of this class's base class or <B>null</B> if this class has no base class.</returns>
			String ^GetParentClassName()
			{
				if (classEntry == NULL || classEntry->parent == NULL) return nullptr;
				if (classEntry->parent->name == NULL) return String::Empty;
				return gcnew String(classEntry->parent->name, 0, classEntry->parent->name_length);
			}

			/// <summary>
			/// Returns pointer to the corresponding <c>zend_class_entry</c>.
			/// </summary>
			/// <returns>Unmanaged pointer to the corresponding <c>zend_class_entry</c> block.</returns>
			zend_class_entry *GetClassEntry()
			{
				return isOriginalClassEntryValid ? originalClassEntry : classEntry;
			}

			/// <summary>
			/// Returns the containing <see cref="Module"/> of this <see cref="Class"/>.
			/// </summary>
			/// <returns>The containing <see cref="Module"/>.</returns>
			Module ^GetContainingModule()
			{
				return containingModule;
			}

			/// <summary>
			/// Returns <B>true</B> if this class represents <c>stdClass</c>.
			/// </summary>
			/// <returns><B>true</B> if this class represents <c>stdClass</c>, <B>false</B> otherwise.</returns>
			bool IsStdClass()
			{
				return originalClassEntry == zend_stdClass_ptr;
			}

			/// <summary>
			/// Returns a <see cref="Type"/> defined in the corresponding managed wrapper that wraps this Zend class.
			/// </summary>
			/// <returns>The wrapping type.</returns>
			/// <remarks>
			/// The wrapping type plays a key role in native-to-managed marshaling, see <see cref="PhpMarshaler"/>.
			/// </remarks>
			Type ^GetWrappingType()
			{
				return wrappingType;
			}

			/// <summary>
			/// Returns a <see cref="Method"/> with a given name that was defined in this class.
			/// </summary>
			/// <param name="name">The name of the method.</param>
			/// <returns>The <see cref="Method"/> or <B>null</B> if not found.
			Method ^GetMethodByName(String ^name)
			{
				return static_cast<Method ^>(methods[name]);
			}

			/// <summary>
			/// Returns a <see cref="Constants"/> with a given name that was defined in this class.
			/// </summary>
			/// <param name="name">The name of the constant.</param>
			/// <returns>The <see cref="Constant"/> or <B>null</B> if not found.
			Constant ^GetConstantByName(String ^name);

			/// <summary>
			/// Returns an <see cref="IDictionaryEnumerator"/> to be used for method enumeration.
			/// </summary>
			/// <returns>The enumerator.</returns>
			IDictionaryEnumerator ^GetMethodEnumerator()
			{
				return methods->GetEnumerator();
			}

			/// <summary>
			/// Returns an <see cref="IDictionaryEnumerator"/> to be used for constant enumeration.
			/// </summary>
			/// <returns>The enumerator.</returns>
			IDictionaryEnumerator ^GetConstantEnumerator();

			/// <summary>
			/// Called when an instance of this class is being marshal from native to managed representation.
			/// <summary>
			/// <param name="entry">The <c>zend_class_entry</c> pointed to by the native representation.</param>
			void OnMarshalNativeToManaged(zend_class_entry *entry)
			{
				if (entry == originalClassEntry) isOriginalClassEntryValid = true;
			}

			/// <summary>
			/// Releases the unmanaged <c>zend_class_entry</c>.
			/// </summary>
			~Class()
			{
				GC::SuppressFinalize(this);

#if defined(PHP4TS)
				destroy_zend_class(classEntry);
				free(classEntry);
#elif defined(PHP5TS)
				zend_class_entry *_tmp = classEntry;
				destroy_zend_class(&_tmp);
				classEntry = _tmp;
#else
				Debug::Assert();
#endif
			}

		private:
			/// <summary>
			/// Unmanaged pointer to the corresponding <c>zend_function_entry</c> block.
			/// </summary>
			zend_class_entry *classEntry;

			/// <summary>
			/// Unmanaged pointer to the original <c>zend_function_entry</c> block (as supplied to
			/// <c>register_internal_class</c>.
			/// </summary>
			zend_class_entry *originalClassEntry;

			/// <summary>
			/// Determines whether <see cref="originalClassEntry"/> is assumed valid.
			/// </summary>
			/// <remarks>
			/// When creating new objects, some extensions use the original <c>zend_class_entry</c> pointer
			/// that they had passed to <c>register_internal_class</c>, some use the pointer returned by
			/// the call (pointer to a copy). The purpose of this field is to signal which of these two patterns
			/// is followed by the <see cref="containingModule"/>.
			/// </remarks>
			bool isOriginalClassEntryValid;

			/// <summary>
			/// Reference to the module (extension) this class is defined in.
			/// </summary>
			Module ^containingModule;

			/// <summary>
			/// Collection of <see cref="Method"/>s declared by this class.
			/// </summary>
			Hashtable ^methods;

			/// <summary>
			/// The <see cref="Type"/> defined in the corresponding managed wrapper that wraps this Zend class.
			/// </summary>
			/// <remarks>
			/// The wrapping type plays a key role in native-to-managed marshaling, see <see cref="PhpMarshaler"/>.
			/// </remarks>
			Type ^wrappingType;

			// Static members

		public:
			/// <summary>
			/// Returns the name of a Zend class.
			/// </summary>
			/// <returns>The name of the class.</returns>
			static String ^GetZendClassName(zend_class_entry *entry)
			{
				if (entry == NULL) return String::Empty;
				return gcnew String(entry->name, 0, entry->name_length);
			}

			/// <summary>A singleton representing the <c>stdClass</c> standard class.</summary>
			static Class ^StdClass = gcnew Class(nullptr, zend_stdClass_ptr, NULL);
		};

		/// <summary>
		/// Represents one configuration entry - one <c>&lt;set/&gt; node in <c>ExtManager.exe.config</c>.
		/// </summary>
		/// <remarks>
		/// The value is kept in both managed and unmanaged form, because some configuration related
		/// Zend API functions return pointer to <c>zval</c>, pointer to <c>char</c> etc.
		/// </remarks>
		private ref class IniEntry
		{
		public:
			/// <summary>
			/// Creates a new <see cref="IniEntry"/>.
			/// </summary>
			/// <param name="value">The entry value.</param>
			IniEntry(String ^value)
			{
				MngValue = value;
				ntvValue = NULL;
			}

			/// <summary>
			/// Cleans up unmanaged resources.
			/// </summary>
			~IniEntry()
			{
				if (ntvValue != NULL)
				{
					free(ntvValue->value.str.val);
					free(ntvValue);
					ntvValue = NULL;
				}
			}

			/// <summary>
			/// Returns the value in unmanaged form (pointer to <c>zval</c> structure).
			/// </summary>
			/// <returns>The value in unmanaged form.</returns>
			zval *GetNativeValue()
			{
				Threading::Monitor::Enter(MngValue);
				try
				{
					if (ntvValue == NULL)
					{
						ntvValue = (zval *)malloc(sizeof(zval));
						INIT_PZVAL(ntvValue);

						char *s = PhpMarshaler::GetInstance(nullptr)->MarshalManagedStringToNativeStringPersistent(MngValue);
						ZVAL_STRING(ntvValue, s, false);
					}
					return ntvValue;
				}
				finally
				{
					Threading::Monitor::Exit(MngValue);
				}
			}
			
			/// <summary>
			/// The entry value.
			/// </summary>
			String ^MngValue;

		private:
			/// <summary>
			/// Unmanaged form of the entry value.
			/// </summary>
			zval *ntvValue;
		};

		/// <summary>
		/// Represents an abstract extension module.
		/// </summary>
		private ref class Module abstract
		{
		public:
			/// <summary>
			/// Calls extension's <c>info_func</c> handler.
			/// </summary>
			/// <returns><B>true</B> if the module has non-null <c>info_func</c> handler, <B>false</B> otherwise.
			/// <remarks>
			/// The result (extension information) is appended to <see cref="Request.phpInfoBuilder"/> of the current
			/// <see cref="Request"/>.
			/// </remarks>
			virtual bool PhpInfo() = 0;
			
			/// <summary>
			/// Unloads this <see cref="Module"/>.
			/// </summary>
			virtual void ModuleShutdown() = 0;

			/// <summary>
			/// Calls extension's <c>request_startup_func</c> handler.
			/// </summary>
			virtual void RequestStartup(Request ^request) = 0;

			/// <summary>
			/// Calls extensions's <c>request_shutdown_func</c> handler.
			/// </summary>
			virtual void RequestShutdown(Request ^request) = 0;

			/// <summary>
			/// Generates managed wrapper for this extension.
			/// </summary>
			virtual String ^GenerateManagedWrapper() = 0;

			/// <summary>
			/// Returns the internal name of this extension. For example &quot;sockets&quot;.
			/// </summary>
			/// <returns>The module name.</returns>
			virtual String ^GetModuleName() = 0;

			/// <summary>
			/// Returns file name of this extension (without trailing <c>.DLL</c>). For example &quot;php_sockets&quot;.
			/// </summary>
			/// <returns>The file name.</returns>
			virtual String ^GetFileName() = 0;

			/// <summary>
			/// Returns module number (a unique integer ID).
			/// </summary>
			/// <returns>The module number.</returns>
			virtual int GetModuleNumber() = 0;

			/// <summary>
			/// Returns module version.
			/// </summary>
			/// <returns>The module version or <B>null</B> if it's not specified.</returns>
			virtual String ^GetVersion() = 0;

			/// <summary>
			/// Returns reference to the corresponding managed wrapper assembly.
			/// </summary>
			/// <returns>The <see cref="Reflection.Assembly"/> or <B>null</B> if the wrapper could not be found.</returns>
			virtual System::Reflection::Assembly ^GetWrappingAssembly() = 0;

			/// <summary>
			/// Supports &quot;one request&quot; extensions that do their initialization in request startup handler
			/// and are meant to server only one request (e.g. GTK).
			/// </summary>
			/// <return><B>true</B> if this is a one request extension, <B>false</B> otherwise.</return>
			virtual bool IsEarlyInit()
			{
				return false;
			}

			/// <summary>
			/// Returns a <see cref="Constant"/> with given name that was defined by this extension.
			/// </summary>
			/// <param name="name">The name of the constant.</param>
			/// <returns>The <see cref="Constant"/> or <B>null</B> if not found.
			Constant ^GetConstantByName(String ^name);

			/// <summary>
			/// Returns a <see cref="Function"/> with given name that was defined by this extension.
			/// </summary>
			/// <param name="name">The name of the function.</param>
			/// <returns>The <see cref="Function"/> or <B>null</B> if not found.
			Function ^GetFunctionByName(String ^name);

			/// <summary>
			/// Returns a <see cref="Class"/> with given name that was defined by this extension.
			/// </summary>
			/// <param name="name">The name of the class.</param>
			/// <returns>The <see cref="Class"/> or <B>null</B> if not found.
			Class ^GetClassByName(String ^name);
			
			/// <summary>
			/// Adds a persistent constant to the table of constants defined by this extension.
			/// </summary>
			/// <param name="name">Name of the constant.</param>
			/// <param name="constant">The <see cref="Constant"/> representing the constant.</param>
			/// <returns><B>true</B> if successfully added, <B>false</B> otherwise.</returns>
			bool AddConstant(String ^name, Constant ^constant);
			
			/// <summary>
			/// Adds a class to the table of classes defined by this extension.
			/// </summary>
			/// <param name="name">Name of the class.</param>
			/// <param name="entry">The <see cref="Class"/> representing the class.</param>
			/// <param name="transient"><B>true</B> to register this constant as transient (as part of this request),
			/// <B>false</B> to register it in module context.</param>
			/// <returns><B>true</B> if successfully added, <B>false</B> otherwise.</returns>
			void AddClass(String ^name, Class ^entry, bool transient);

			/// <summary>
			/// Determines whether the parameter represents a method table of a Zend class registered by this extension.
			/// </summary>
			/// <param name="ht">A Zend <c>HashTable</c> pointer.</ht>
			/// <return><B>true</B> if <paramref name="ht"/> is a method table.</return>
			bool IsNativeMethodTable(HashTable *ht);

			/// <summary>
			/// Returns an <see cref="ICollection"/> of names of functions defined by this extension.
			/// </summary>
			/// <returns>The collection.</returns>
			ICollection ^GetFunctionNames();

			/// <summary>
			/// Returns an <see cref="ICollection"/> of names of classes defined by this extension.
			/// </summary>
			/// <returns>The collection.</returns>
			ICollection ^GetClassNames();

			/// <summary>
			/// Returns an <see cref="IniEntry"/> given the key (INI entry name).
			/// </summary>
			/// <param name="key">The INI entry name.</param>
			/// <returns>The <see cref="IniEntry"/> or <B>null</B> if not found.</returns>
			IniEntry ^GetConfigEntry(String ^key);

		protected:
			/// <summary>Collection of <see cref="Function"/>s defined in this extension.</summary>
			Hashtable ^functions;

			/// <summary>Collection of <see cref="Constant"/>s that were registered by this extension.</summary>
			Hashtable ^constants;

			/// <summary>
			/// Collection of classes that were registered by this extension.<summary>
			/// </summary>
			/// <remarks>
			/// The reason that <see cref="OrderedHashtable"/> is used is that it is also necessary to maintain order.
			/// When generating wrappers base class (registered earlier) has to be wrapped before derived
			/// classes (registered later).
			/// </remarks>
			PHP::Core::OrderedHashtable<String ^> ^classes;

			/// <summary>
			/// Collection of INI settings for this extension that were found in the <c>ExtManager.exe.config</c>
			/// file.
			/// </summary>
			Hashtable ^iniSettings;

		// static members
		public:
			/// <summary>
			/// Returns a module with given file name.
			/// </summary>
			/// <param name="name">The file name of the extension (without trailing <c>.DLL</c>). For example
			/// &quot;php_sockets&quot;.</param>
			/// <returns>The <see cref="Module"/> or <B>null</B> if not found.</returns>
			static Module ^GetModule(String ^name)
			{
				return static_cast<Module ^>(modules[name]);
			}

			/// <summary>
			/// Returns a module with given zero-based index.
			/// </summary>
			/// <param name="index">The zero-based index.</param>
			/// <returns>The <see cref="Module"/>.</returns>
			/// <exception cref="ArgumentOutOfRangeException">Index out of range.</exception>
			static Module ^GetModule(int index)
			{
				return static_cast<Module ^>(modules->GetByIndex(index));
			}

			/// <summary>
			/// Returns a module with a given module number.
			/// </summary>
			/// <param name="number">The one-based module number.</param>
			/// <returns>The <see cref="Module"/> or <B>null</B> if not found.</returns>
			static Module ^GetModuleByModuleNumber(int number)
			{
				for (int i = 0; i < modules->Count; i++)
				{
					Module ^mod = static_cast<Module ^>(modules->GetByIndex(i));
					if (mod != nullptr && mod->GetModuleNumber() == number) return mod;
				}
				return nullptr;
			}

			/// <summary>
			/// Returns an early init module with given zero-based index.
			/// </summary>
			/// <param name="index">The zero-based index.</param>
			/// <returns>The <see cref="Module"/>.</returns>
			/// <exception cref="ArgumentOutOfRangeException">Index out of range.</exception>
			static Module ^GetEarlyInitModule(int index)
			{
				return static_cast<Module ^>(earlyInitModules[index]);
			}

			/// <summary>
			/// Returns a module with given module name (NOT file name).
			/// </summary>
			/// <param name="name">The module name of the extension. For example &quot;sockets&quot;.</param>
			/// <returns>The <see cref="Module"/> or <B>null</B> if not found.</returns>
			static Module ^GetModuleByInternalName(String ^name)
			{
				return static_cast<Module ^>(modulesByInternalName[name]);
			}

			/// <summary>
			/// Returns the number of loaded extensions.
			/// </summary>
			/// <returns>The number of loaded extensions.</returns>
			static int GetModuleCount()
			{
				return modules->Count;
			}

			/// <summary>
			/// Returns the number of loaded early init extensions.
			/// </summary>
			/// <returns>The number of loaded early init extensions.</returns>
			static int GetEarlyInitModuleCount()
			{
				return earlyInitModules->Count;
			}

			/// <summary>
			/// Returns an <see cref="ICollection"/> of file names (without trailing <c>.DLL</c>) of loaded
			/// extensions.
			/// </summary>
			/// <returns>The collection.</returns>
			static ICollection ^GetModuleNames()
			{
				return modules->Keys;
			}

			/// <summary>
			/// Returns an <see cref="ICollection"/> of internal names of loaded extensions.
			/// </summary>
			/// <returns>The collection.</returns>
			static ICollection ^GetInternalModuleNames()
			{
				return modulesByInternalName->Keys;
			}

			/// <summary>
			/// Returns reference to the <see cref="Module/> that contains currently executing function.
			/// </summary>
			/// <returns>The <see cref="Module/> that contains currently executing function or <B>null</B>
			/// if no external function is currently executing.</returns>
			/// <remarks>
			/// Understands <see cref="ModuleBoundContext"/> as well as <see cref="Request"/> context notion.
			/// </remarks>
			static Module ^GetCurrentModule();

			/// <summary>
			/// Contains reference to current <see cref="Module"/> for calling thread.
			/// </summary>
			/// <remarks>
			/// This thread-static member is not <B>null</B> when a function in the extension is called
			/// with no <see cref="Request"/> context (typically <c>module_startup</c>, <c>module_shutdown</c>).
			[ThreadStatic]
			static Module ^ModuleBoundContext = nullptr;

		protected:
			/// <summary>
			/// Collection of all loaded extensions, keyed by file name without trailing <c>.DLL</c>.
			/// </summary>
			static SortedList ^modules = SortedList::Synchronized(gcnew SortedList());

			/// <summary>
			/// Collection of all loaded extensions, keyed by <c>moduleEntry-&gt;name</c> (internal name).
			/// </summary>
			static Hashtable ^modulesByInternalName = Hashtable::Synchronized(gcnew Hashtable());

			/// <summary>
			/// Collection of early init extensions.
			/// </summary>
			static ArrayList ^earlyInitModules = ArrayList::Synchronized(gcnew ArrayList());
		};

		/// <summary>
		/// Represents a module (PHP extension DLL).
		/// </summary>
		private ref class DynamicModule : public Module
		{
		public:
			/// <summary>
			/// Creates and loads a new <see cref="DynamicModule"/>.
			/// </summary>
			/// <remarks>
			/// Note that dynamic loading of extensions at runtime using the <c>dl()</c> PHP function is
			/// not supported (as it is in PHP with multithreaded web servers). All extensions are loaded
			/// and corresponding <see cref="Module"/> instances are constructed during start up.
			/// </remarks>
			DynamicModule(String ^_path, String ^_fileName, bool _earlyInit);

			/// <summary>
			/// Calls extension's <c>info_func</c> handler.
			/// </summary>
			/// <returns><B>true</B> if the module has non-null <c>info_func</c> handler, <B>false</B> otherwise.
			/// <remarks>
			/// The result (extension information) is appended to <see cref="Request.phpInfoBuilder"/> of the current
			/// <see cref="Request"/>.
			/// </remarks>
			virtual bool PhpInfo() override;

			/// <summary>
			/// Unloads this <see cref="Module"/>.
			/// </summary>
			virtual void ModuleShutdown() override;

			/// <summary>
			/// Calls extension's <c>request_startup_func</c> handler.
			/// </summary>
			virtual void RequestStartup(Request ^request) override;

			/// <summary>
			/// Calls extensions's <c>request_shutdown_func</c> handler.
			/// </summary>
			virtual void RequestShutdown(Request ^request) override;

			/// <summary>
			/// Generates managed wrapper for this extension.
			/// </summary>
			virtual String ^GenerateManagedWrapper() override;

			//
			//
			
			/// <summary>
			/// Returns file name of this extension (without trailing <c>.DLL</c>). For example &quot;php_sockets&quot;.
			/// </summary>
			/// <returns>The file name.</returns>
			virtual String ^GetFileName() override
			{
				return fileName;
			}

			/// <summary>
			/// Returns the internal name of this extension. For example &quot;sockets&quot;.
			/// </summary>
			/// <returns>The module name.</returns>
			virtual String ^GetModuleName() override
			{
				return gcnew String(moduleEntry->name);
			}

			/// <summary>
			/// Returns reference to the corresponding managed wrapper assembly.
			/// </summary>
			/// <returns>The <see cref="Reflection.Assembly"/> or <B>null</B> if the wrapper could not be found.</returns>
			virtual System::Reflection::Assembly ^GetWrappingAssembly() override;

			/// <summary>
			/// Returns module number (a unique integer ID).
			/// </summary>
			/// <returns>The module number.</returns>
			virtual int GetModuleNumber() override
			{
				return moduleEntry->module_number;
			}

			/// <summary>
			/// Returns module version.
			/// </summary>
			/// <returns>The module version or <B>null</B> if it's not specified.</returns>
			virtual String ^GetVersion() override
			{
				if (moduleEntry->version == NULL) return nullptr;
				return gcnew String(moduleEntry->version);
			}

			/// <summary>
			/// Supports &quot;one request&quot; extensions that do their initialization in request startup handler
			/// and are meant to serve only one request (e.g. GTK).
			/// </summary>
			/// <return><B>true</B> if this is a one request extension, <B>false</B> otherwise.</return>
			virtual bool IsEarlyInit() override
			{
				return earlyInit;
			}

		private:
			/// <summary>
			/// P/Invoke declaration of <c>LoadLibraryEx</c>.
			/// </summary>
			/// <remarks>
			/// This declaration merely saves us from doing manual <see cref="String"/> marshaling.
			/// </remarks>
			[DllImport("kernel32.dll", CharSet = CharSet::Unicode, SetLastError = true)]
			static HMODULE LoadLibraryEx(String ^fileName,IntPtr reserved,int flags);

			/// <summary><B>true</B> if this instance is fully initialized, <B>false</B> otherwise (tested in finalizer).</summary>
			bool initialized;

			/// <summary><B>true</B> if this is a one request extension (e.g. GTK)</summary>
			bool earlyInit;

			/// <summary>Handle of the loaded DLL.</summary>
			HMODULE hLib;

			/// <summary>
			/// The file name of this extension (without trailing <c>.DLL</c>). For example &quot;php_sockets&quot;.
			/// </summary>
			String ^fileName;

			/// <summary>
			/// Reference to the corresponding managed wrapper assembly (loaded lazily only when necessary).
			/// </summary>
			/// <remarks><seealso cref="GetWrappingAssembly"/></remarks>
			System::Reflection::Assembly ^wrappingAssembly;

			/// <summary>Unmanaged pointer to this extension's <c>zend_module_entry block</c>.</summary>
			zend_module_entry *moduleEntry;
			
			/// <summary>
			/// Signature of the <c>get_module</c> function, which is the only symbol exported by
			/// PHP extension DLLs.
			/// </summary>
			typedef zend_module_entry *(*GetModuleProto)();

		// static members
		public:
			/// <summary>
			/// Loads specified extension implemented in a DLL.
			/// </summary>
			/// <remarks>
			/// A new <see cref="Module"/> is instantiated to represent the loaded extension. Reference to
			/// the newly created instance is added to <see cref="modules"/>, <see cref="modulesByName"/> and
			/// the Zend <c>module_registry</c>.
			/// </remarks>
			static DynamicModule ^LoadDynamicModule(ExtensionLibraryDescriptor ^descriptor);
		};

		/// <summary>
		/// Represents built-in stream wrappers &quot;extension&quot; (currently ftp, http).
		/// </summary>
		private ref class InternalStreamWrappers : public Module
		{
		public:
			/// <summary>
			/// Registers built-in stream wrappers.
			/// </summary>
			InternalStreamWrappers();

			/// <summary>
			/// Returns <B>false</B>.
			/// </summary>
			virtual bool PhpInfo() override
			{
				return false;
			}
			
			/// <summary>
			/// Unloads this <see cref="Module"/>.
			/// </summary>
			virtual void ModuleShutdown() override;

			/// <summary>
			/// Does nothing.
			/// </summary>
			virtual void RequestStartup(Request ^request) override
			{ }

			/// <summary>
			/// Does nothing.
			/// </summary>
			virtual void RequestShutdown(Request ^request) override
			{ }

			/// <summary>
			/// Returns <B>null</B>.
			/// </summary>
			virtual String ^GenerateManagedWrapper() override
			{
				return nullptr;
			}

			/// <summary>
			/// Returns the internal name of this &quot;extension&quot;.
			/// </summary>
			/// <returns>The module name.</returns>
			virtual String ^GetModuleName() override
			{
				return "#stream_wrappers";
			}

			/// <summary>
			/// Returns <B>null</B>.
			/// </summary>
			/// <returns><B>null</B></returns>
			virtual String ^GetFileName() override
			{
				return "#stream_wrappers";
			}

			/// <summary>
			/// Returns <c>-1</c>.
			/// </summary>
			/// <returns><c>-1</c></returns>
			virtual int GetModuleNumber() override
			{
				return -1;
			}

			/// <summary>
			/// Returns module version.
			/// </summary>
			/// <returns><B>null</B></returns>
			virtual String ^GetVersion() override
			{
				return nullptr;
			}

			/// <summary>
			/// Returns <B>null</B>.,
			/// </summary>
			/// <returns><B>null</B></returns>
			virtual System::Reflection::Assembly ^GetWrappingAssembly() override
			{
				return nullptr;
			}

		// static members
		public:
			/// <summary>
			/// Loads this extension.
			/// </summary>
			/// <remarks>
			/// A new <see cref="Module"/> is instantiated to represent the loaded extension. Reference to
			/// the newly created instance is added to <see cref="modules"/> and <see cref="modulesByName"/>.
			/// </remarks>
			static void LoadModule();
		};
	}
}
