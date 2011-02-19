//
// ExtSupport - substitute for php4ts.dll/php5ts.dll
//
// WrapperTypeDef.h
// - contains declaration of ModuleTypeInfo class
// - contains declaration of ClassTypeInfo class
// - contains declaration of OverloadTypeInfo class
// - contains declaration of FunctionTypeInfo class
// - contains declaration of ParameterTypeInfo class
//
// - contains declaration of WrapperTypeDef
//

#pragma once

#include "stdafx.h"
#include "AssemblyInternals.h"

#include "Module.h"

using namespace System;
using namespace System::Text;

/*

  Designed and implemented by Ladislav Prosek.
  
*/

namespace PHP
{
	namespace ExtManager
	{
		#pragma region Enumerations

		/// <summary>
		/// Represents a property of a function.
		/// </summary>
		/// <remarks>
		/// <c>None</c> means that the function does not work with bound variables, <c>In</c> means that the
		/// function reads bound variables, <c>Out</c> means that the function writes to bound variables.
		/// </remarks>
		[Flags]
		private enum struct MarshalBoundVarsFlag
		{
			None  = 0,
			In    = 1,
			Out   = 2,
			InOut = 3
		};

		/// <summary>
		/// Represents a property of one function parameter.
		/// </summary>
		/// <remarks>
		/// <para>
		/// <c>No</c> means that the parameter is mandatory, <c>Yes</c> means that the parameter can but
		/// need not be given and <c>Vararg</c> means that any number (zero or more) parameters can be given.
		/// </para>
		/// <para>
		/// The rules are as follows. If a <c>Vararg</c> parameter is present, it must the last one,
		/// and all optional (<c>Yes</c>) parameters must follow all mandatory (<c>No</c>) parameters.
		/// </para>
		/// </remarks>
		private enum struct OptionalFlag
		{
			No, Yes, Vararg
		};

		/// <summary>
		/// Represents a property of one function parameter.
		/// </summary>
		/// <remarks>
		/// <para>
		/// <c>In</c> means that the parameter is passed from caller to callee.mandatory, <c>Yes</c> means that the parameter can but
		/// need not be given and <c>Vararg</c> means that any number (zero or more) parameters can be given.
		/// </para>
		/// <para>
		/// The rules are as follows. If a <c>Vararg</c> parameter is present, it must the last one,
		/// and all optional (<c>Yes</c>) parameters must follow all mandatory (<c>No</c>) parameters.
		/// </para>
		/// </remarks>
		[Flags]
		private enum struct DirectionFlag
		{
			In    = 1,
			Out   = 2,
			InOut = 3
		};

		#pragma endregion

		#pragma region Module, class, function, parameter, and overload type info

		ref class FunctionTypeInfo;
		ref class ClassTypeInfo;

		/// <summary>
		/// Represents module (aka extension) type information.
		/// </summary>
		/// <remarks>
		/// Contains a <see cref="Hashtable"/> with function type information and a <see cref="Hashtable"/>
		/// with class type information.
		/// </remarks>
		public value struct ModuleTypeInfo
		{
		public:
			/// <summary>
			/// Creates a new <see cref="ModuleTypeInfo"/>.
			/// <summary>
			/// <param name="_functions"/>Function type table.</param>
			/// <param name="_classes"/>Class type table.</param>
			ModuleTypeInfo(scg::Dictionary<String ^, FunctionTypeInfo ^> ^_functions,
				scg::Dictionary<String ^, ClassTypeInfo ^> ^_classes, bool _earlyInit)
				: Functions(_functions), Classes(_classes), EarlyInit(_earlyInit)
			{ }

			/// <summary>
			/// Function type table.
			/// </summary>
			/// <remarks>
			/// Keys are function names, values are instances of <see cref="FunctionTypeInfo"/> class.
			/// </remarks>
			scg::Dictionary<String ^, FunctionTypeInfo ^> ^Functions;

			/// <summary>
			/// Class type table.
			/// </summary>
			/// <remarks>
			/// Keys are class names, values are instances of <see cref="ClassTypeInfo"/> class.
			/// </remarks>
			scg::Dictionary<String ^, ClassTypeInfo ^> ^Classes;

			/// <summary>
			/// <B>True<B> if this extension should be eagerly initialized, <B>false</B> otherwise.
			/// </summary>
			bool EarlyInit;
		};

		/// <summary>
		/// Represents class type information.
		/// </summary>
		/// <remarks>
		/// Class name a method type information is stored in this class.<summary>
		/// </remarks>
		private ref class ClassTypeInfo
		{
		public:
			/// <summary>The name of the class.</summary<
			String ^Name;

			/// <summary>Name of the array access getter method or <B>null</B>.</summary>
			/// <remarks>
			/// If at least one of <see cref="ArrayGetter"/> and <see cref="ArraySetter"/> is non-<B>null</B>,
			/// an <B>ArrayAccess</B> interface implementation is generated into the wrapper class.
			/// </remarks>
			String ^ArrayGetter;

			/// <summary>Name of the array access setter method or <B>null</B>.</summary>
			/// <remarks>
			/// If at least one of <see cref="ArrayGetter"/> and <see cref="ArraySetter"/> is non-<B>null</B>,
			/// an <B>ArrayAccess</B> interface implementation is generated into the wrapper class.
			/// </remarks>
			String ^ArraySetter;

			/// <summary>
			/// Method type table.
			/// </summary>
			/// <remarks>
			/// Keys are method names, values are instances of <see cref="FunctionTypeInfo"/> class.
			/// </remarks>
			scg::Dictionary<String ^, FunctionTypeInfo ^> ^Methods;
		};

		/// <summary>
		/// Represents parameter type information.
		/// </summary>
		/// <remarks>
		/// Parameter name, parameter type and <c>byref</c> and <see cref="Optional"/> flags are stored
		/// in this class.
		/// </remarks>
		private ref class ParameterTypeInfo
		{
		public:
			/// <summary>
			/// Parameterless constructor creates a new <see cref="ParameterTypeInfo"/> that represents
			/// a mandatory parameter with empty name and undefined type.
			/// </summary>
			ParameterTypeInfo()
			{
				Optional = OptionalFlag::No;
			}

			/// <summary>
			/// Creates a new <see cref="ParameterTypeInfo"/>.
			/// </summary>
			/// <param name="_name">Parameter name.</param>
			/// <param name="_type">Parameter type.</param>
			/// <param name="_optional">Specifies whether this parameter is mandatory, optional or vararg.</param>
			/// <param name="_byRef">Specifies whether this parameter is passed by value (<B>false</B>) or
			/// by reference (<B>true</B>).</param>
			/// <param name="_bind">Specifies whether a binding between the managed parameter and its native
			/// <c>zval</c> representation should be established (<B>true</B>) or not (<B>false</B>).</param>
			ParameterTypeInfo(String ^_name, Type ^_type, OptionalFlag _optional, DirectionFlag _direction, bool _bind)
				: ParamName(_name), ParamType(_type), Optional(_optional), Direction(_direction), Bind(_bind)
			{ }

			/// <summary>
			/// Creates a shallow copy of the current instance.
			/// </summary>
			/// <returns>Shallow copy of this instance.</returns>
			ParameterTypeInfo ^Clone()
			{
				return static_cast<ParameterTypeInfo ^>(MemberwiseClone());
			}

			property bool IsIn { bool get() { return (Direction & DirectionFlag::In) == DirectionFlag::In; } }
			property bool IsOut { bool get() { return (Direction & DirectionFlag::Out) == DirectionFlag::Out; } }

			/// <summary>Parameter name.</summary>
			String ^ParamName;

			/// <summary>Parameter type.</summary>
			Type ^ParamType;

			/// <summary>Specifies whether this parameter is mandatory, optional or vararg.</summary>
			OptionalFlag Optional;

			/// <summary>
			/// Specifies whether this parameter is passed in, out or inout (ref).
			/// </summary>
			DirectionFlag Direction;

			/// <summary>
			/// Specifies whether a binding between the managed parameter and its native <c>zval</c> representation
			/// should be established (<B>true</B>) or not (<B>false</B>).
			/// </summary>
			bool Bind;
		};

		/// <summary>
		/// Represents function type information.
		/// </summary>
		/// <remarks>
		/// Function description, return type, parameter information and index of the first optional parameter
		/// are stored in this class.<summary>
		/// </remarks>
		private ref class FunctionTypeInfo
		{
		public:
			/// <summary>
			/// Parameterless constructor creates an empty <see cref="FunctionTypeInfo"/>.
			/// </summary>
			FunctionTypeInfo()
			{
				Params = gcnew scg::List<ParameterTypeInfo ^>();
				MarshalBoundVars = MarshalBoundVarsFlag::None;
				FirstOptionalParamIndex = -1;
				VarargParamDefined = false;
				CastToFalse = false;
				Static = false;
				Next = nullptr;
			}
			
			/// <summary>
			/// Creates a new <see cref="FunctionTypeInfo"/>.
			/// </summary>
			/// <param name="_description">Function description.</param>
			/// <param name="_returnType">Type of the return value.</param>
			/// <param name="_marshalVars">Treatment of bound variables.</param>
			FunctionTypeInfo(String ^_description, Type ^_returnType, MarshalBoundVarsFlag _marshalVars)
				: Description(_description), ReturnType(_returnType), MarshalBoundVars(_marshalVars)
			{
				Params = gcnew scg::List<ParameterTypeInfo ^>();
				FirstOptionalParamIndex = -1;
				VarargParamDefined = false;
				CastToFalse = false;
				Static = false;
				Next = nullptr;
			}

			property bool MarshalBoundVarsIn { bool get() { return (MarshalBoundVars & MarshalBoundVarsFlag::In) == MarshalBoundVarsFlag::In; } }
			property bool MarshalBoundVarsOut { bool get() { return (MarshalBoundVars & MarshalBoundVarsFlag::Out) == MarshalBoundVarsFlag::Out; } }

			/// <summary>
			/// Adds a parameter type information (left-to-right order).
			/// </summary>
			/// <param name="param">The parameter.</param>
			/// <exception cref="ArgumentException">A mandatory parameter is being added after an optional
			/// parameter or vararg parameter is not the last one.</exception>
			void AddParameter(ParameterTypeInfo ^param);

			/// <summary>
			/// Returns a <see cref="ParameterTypeInfo"/> of a parameter with the given index.
			/// </summary>
			/// <param name="index">Parameter index (zero-based, left-to-right order).</param>
			/// <returns>The <see cref="ParameterTypeInfo"/>.</returns>
			/// <exception cref="ArgumentOutOfRangeException"><pamref name="index"/> is out of bounds.</exception>
			ParameterTypeInfo ^GetParameterByIndex(int index)
			{
				return Params[index];
			}

			/// <summary>
			/// Creates a shallow copy of the current instance.
			/// </summary>
			/// <returns>Shallow copy of this instance.</returns>
			FunctionTypeInfo ^Clone()
			{
				return static_cast<FunctionTypeInfo ^>(MemberwiseClone());
			}

			/// <summary>
			/// Converts this instance to argfull overload type info.
			/// </summary>
			FunctionTypeInfo ^ToArgFullSignature();

			/// <summary>The name of the function.</summary>
			String ^Name;

			/// <summary>Function description.</summary>
			String ^Description;

			/// <summary>Type of the return value.</summary>
			Type ^ReturnType;

			/// <summary>
			/// Specifies whether and in which direction bound variables should be marshaled when this
			/// function is invoked.
			/// </summary>
			MarshalBoundVarsFlag MarshalBoundVars;

			/// <summary>
			/// Specifies whether <c>CastNullToCastAttribute</c> should be applied to the return value.
			/// </summary>
			bool CastToFalse;

			/// <summary>
			/// Specifies whether this is a static method (has no effect on functions).
			bool Static;

			/// <summary>Collection of function parameters.</summary>
			scg::List<ParameterTypeInfo ^> ^Params;
			
			/// <summary>
			/// Zero-based index of the first optional parameter or <c>-1</c> if this function has
			/// no optional parameters.
			/// </summary>
			int FirstOptionalParamIndex;

			/// <summary>
			/// Specifies whether a parameter with <see cref="Optional"/> equal to <c>Aararg</c> has
			/// already been added.
			/// </summary>
			bool VarargParamDefined;

			/// <summary>
			/// Next overload of this function (or <B>null</B>).
			/// </summary>
			FunctionTypeInfo ^Next;
		};

		/// <summary>
		/// Analyzes information contained in <see cref="FunctionTypeInfo"/>s and is capable of combining
		/// type information of more function/method overloads into one generalized description.
		/// </summary>
		/// <remarks>
		/// Used when generating arg-less overloads (aka dynamic stubs).
		/// </remarks>
		private ref class OverloadTypeInfo
		{
		public:
			/// <summary>
			/// Creates a new empty <see cref="OverloadTypeInfo"/>.
			/// </summary>
			OverloadTypeInfo();

			property bool MarshalBoundVarsIn { bool get() { return (MarshalBoundVars & MarshalBoundVarsFlag::In) == MarshalBoundVarsFlag::In; } }
			property bool MarshalBoundVarsOut { bool get() { return (MarshalBoundVars & MarshalBoundVarsFlag::Out) == MarshalBoundVarsFlag::Out; } }

			/// <summary>
			/// Fills this <see cref="OverloadTypeInfo"/> with information contained in a given
			/// <see cref="FunctionTypeInfo"/>.
			/// <param name="info">The <see cref="FunctionTypeInfo"/>.</param>
			void Load(FunctionTypeInfo ^info);

			/// <summary>
			/// Combines this <see cref="OverloadTypeInfo"/> with information contained in a given
			/// <see cref="FunctionTypeInfo"/>.
			/// <param name="info">The <see cref="FunctionTypeInfo"/>.</param>
			/// <remarks>
			/// Information are combined as follows. The <see cref="IsStatic"/> and <see cref="CastToFalse"/>
			/// flags are ANDed, <see cref="MarshalVars"/>, <see cref="BindParamMap"/> and
			/// <see cref="ByrefParamMap"/> are ORed. <see cref="ReturnType"/> becomes a common supertype
			/// of the combined return types. <see cref="Name"/> becomes one of the combined names.
			/// </remarks>
			void Combine(FunctionTypeInfo ^info);

			/// <summary>The function/method name.</summary>
			String ^Name;

			/// <summary>Specifies whether bound variables should be marshaled.</summary>
			MarshalBoundVarsFlag MarshalBoundVars;

			/// <summary>A bitmap that specifies which parameters should be bound.</summary>
			BitArray ^BindParamMap;

			/// <summary>A bitmap that specifies which parameters should be passed by reference.</summary>
			BitArray ^ByrefParamMap;

			/// <summary>Position of the byref vararg parameter (0-based) or -1 if there is no such.</summary>
			int ByrefVarargPosition;

			/// <summary>Position of the vararg parameter (0-based) or -1 if there is no such.</summary>
			int VarargPosition;

			/// <summary><B>true</B> if a <B>null</B>/<B>-1</B> return value should be cast to <B>FALSE</B>.</summary>
			bool CastToFalse;

			/// <summary><B>true</B> if the method is static.</summary>
			bool IsStatic;

			/// <summary>The return type.</summary>
			Type ^ReturnType;

		private:
			/// <summary><B>true</B> if <see cref="Combine"/> has never been called.</summary>
			bool neverCombined;
		};

		#pragma endregion

		private ref class WrapperTypeDef
		{
		public:
			/// <summary>
			/// Reads type information for the extension from the typedef XML file.
			/// </summary>
			/// <param name="fileName">The extension file name.</param>
			/// <returns>The type information.</returns>
			static ModuleTypeInfo GetModuleTypeInfo(String ^fileName, StringBuilder ^message,
				int estimatedFunctionCount, int estimatedClassCount);

			/// <summary>
			/// Contains references to types <c>T</c>, <c>T[]</c> and <c>T&amp;</c> for a <see cref="Type"/> <c>T</c>.
			/// </summary>
			value class TypeTriple
			{
			public:
				/// <summary>Creates a new <see cref="TypeTriple"/>.</summary>
				/// <param name="_t">Type <c>T</c>.</param>
				TypeTriple(Type ^_t)
				{
					this->T = _t;
					this->Tarr = _t->MakeArrayType();
					this->Tref = _t->MakeByRefType();
				}

				bool IsEqualsAny(Type^ t)
				{
					return t->Equals(T) || t->Equals(Tarr) || t->Equals(Tref);
				}

				/// <summary>Type <c>T</c>.</summary>
				Type ^T;

				/// <summary>Type <c>T[]</c>.</summary>
				Type ^Tarr;

				/// <summary>Type <c>T&amp;</c>.</summary>
				Type ^Tref;
			};

		private:
			
			/// <summary>
			/// Adds function type info to the parent type info dictionary.
			/// </summary>
			/// <param name="functionInfo">The function type info extracted from the typedef XML file.</param>
			/// <param name="typeInfo">The <see cref="Hashtable"/> to add the type info to.</param>
			static void AddFunctionTypeInfo(FunctionTypeInfo ^functionInfo, scg::Dictionary<String ^, FunctionTypeInfo ^> ^typeInfo);

			/// <summary>
			/// Reads type information for a function or method from the typedef XML file.
			/// </summary>
			/// <param name="funNav">Represents a &lt;function&gt; node in the XML document.</param>
			/// <param name="typeInfo">The <see cref="Hashtable"/> to add the type info to.</param>
			static void GetFunctionTypeInfo(System::Xml::XPath::XPathNavigator ^funNav,
				scg::Dictionary<String ^, FunctionTypeInfo ^> ^typeInfo);

		public:
			/// <summary>
			/// Static constructor. Initializes <see cref="typeMapping"/> and other static fields.
			/// </summary>
			static WrapperTypeDef()
			{
				// fill type mapping table
				typeMapping = gcnew scg::Dictionary<String ^, TypeTriple>(9);

				typeMapping->Add("void",     TypeTriple(System::Void::typeid));
				typeMapping->Add("int",      TypeTriple(System::Int32::typeid));
				typeMapping->Add("float",    TypeTriple(System::Double::typeid));
				typeMapping->Add("bool",     TypeTriple(System::Boolean::typeid));
				typeMapping->Add("string",   TypeTriple(PHP::Core::PhpBytes::typeid));
				typeMapping->Add("array",    TypeTriple(PHP::Core::PhpArray::typeid));
				typeMapping->Add("resource", TypeTriple(PHP::Core::PhpResource::typeid));
				typeMapping->Add("object",   TypeTriple(PHP::Core::PhpObject::typeid));
				typeMapping->Add("mixed",    TypeTriple(System::Object::typeid));
			}

		private:
			/// <summary>
			/// Collection of <see cref="TypeTriple"/>s keyed by PHP &quot;type&quot; name.
			/// </summary>
			/// <remarks>
			/// Contains these keys: <c>void</c>, <c>int</c>, <c>float</c>, <c>bool</c>, <c>string</c>,
			/// <c>array</c>, <c>resource</c>, <c>object</c>, <c>mixed</c>.
			/// </remarks>
			static scg::Dictionary<String ^, TypeTriple> ^typeMapping;
		};
	}
}
