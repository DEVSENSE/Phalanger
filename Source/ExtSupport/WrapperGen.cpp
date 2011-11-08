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
// WrapperGen.cpp
// - contains definition of WrapperGen class
//

#include "stdafx.h"

#include "WrapperGen.h"
#include "Objects.h"
#include "Module.h"

using namespace System;
using namespace System::IO;
using namespace System::Text;
using namespace System::Threading;
using namespace System::Collections;
using namespace System::Reflection::Emit;

using namespace PHP::Core;
using namespace PHP::ExtManager;

namespace PHP
{
	namespace ExtManager
	{
		// WrapperGen implementation:

		// Generates the wrapper assembly.
		String ^WrapperGen::GenerateManagedWrapper()
		{
			message = gcnew StringBuilder();

			// create assembly name
			AssemblyName ^assembly_name = gcnew AssemblyName();
			assembly_name->Name = String::Concat(Path::GetFileName(fileName), Externals::WrapperAssemblySuffix);
			assembly_name->Version = wrapperVersion;
			String ^full_name = String::Concat(assembly_name->Name, ".dll");
			
#ifdef DEBUG
			Debug::WriteLine("EXT SUP", String::Concat("About to generate managed wrapper assembly ", full_name));
#endif

			// try to get type information
			ModuleTypeInfo type_info = WrapperTypeDef::GetModuleTypeInfo(fileName, message, functions->Count, classes->Count);

			String ^wrappers_path = PHP::Core::Configuration::Application->Paths->ExtWrappers;
			if (!System::IO::Directory::Exists(wrappers_path))
			{
				message->Append(ExtResources::GetString("path_to_wrappers_does_not_exist", wrappers_path));
			}
			else
			{
				// sign the assembly
				assembly_name->KeyPair = gcnew StrongNameKeyPair(keyPair);

				// create assembly builder
				AssemblyBuilder ^assembly_builder = Thread::GetDomain()->DefineDynamicAssembly(
					assembly_name, AssemblyBuilderAccess::Save, wrappers_path);

				assembly_builder->DefineVersionInfoResource("Phalanger", "3.0.0.0", "The Phalanger Project Team",
					String::Empty, String::Empty);

				// decorate the assembly with ExtensionDescriptorAttribute
				array<Object ^> ^ctor_params = { fileName, internalName, type_info.EarlyInit };
				CustomAttributeBuilder ^descriptorAttrBuilder = 
					gcnew CustomAttributeBuilder(Constructors::ExtensionDescriptor, ctor_params);
				assembly_builder->SetCustomAttribute(descriptorAttrBuilder);

				// create module builder
				ModuleBuilder ^module_builder = assembly_builder->DefineDynamicModule("Stubs", full_name);

				// build wrapping class name - make sure it is a valid C# identifier
				StringBuilder ^class_name_builder;
				if (String::IsNullOrEmpty(internalName)) class_name_builder = gcnew StringBuilder(fileName);
				else
				{
					class_name_builder = gcnew StringBuilder(internalName->Length);
					class_name_builder->Append(Char::ToUpper(internalName[0]));
					class_name_builder->Append(internalName, 1, internalName->Length - 1);
				}

				if (Char::IsDigit(class_name_builder[0])) class_name_builder->Insert(0, '_');
				for (int i = 0; i < class_name_builder->Length; i++)
				{
					if (!Char::IsLetterOrDigit(class_name_builder[i])) class_name_builder[i] = '_';
				}

				// make sure that wrapping class name does not collide with any PHP class defined by this extension
				String ^class_name;
				while (true)
				{
					class_name = class_name_builder->ToString();
					if (classes->ContainsKey(class_name->ToLower())) class_name_builder->Insert(0, "Php");
					else break;
				}

				// create type builders
				TypeBuilder ^type_builder = module_builder->DefineType(
					String::Concat(Namespaces::Library, ".", class_name),
					TypeAttributes::Public | TypeAttributes::Sealed | TypeAttributes::Class,
					Types::Object[0]);

				TypeBuilder ^dynamic_type_builder = module_builder->DefineType(
					String::Concat(Namespaces::LibraryStubs, ".", class_name),
					TypeAttributes::Public | TypeAttributes::Sealed | TypeAttributes::Class,
					Types::Object[0]);

				// define private constructors
				type_builder->DefineDefaultConstructor(MethodAttributes::Private | MethodAttributes::HideBySig);

				dynamic_type_builder->DefineDefaultConstructor(MethodAttributes::Private | MethodAttributes::HideBySig);

				// create fields (wrapping PHP constants)
				GenerateConstants(type_builder);

				// create methods (wrapping PHP functions)
				// arg-full overloads are generated into type_builder (intended to be used from C#)
				// arg-less overloads are generated into dynamic_type_builder (intended to be used by PHP.NET compiler)
				GenerateFunctions(type_builder, dynamic_type_builder, type_info.Functions);

				// create classes (wrapping PHP classes)
				GenerateClasses(module_builder, type_info.Classes);

				type_builder->CreateType();
				dynamic_type_builder->CreateType();

				try
				{
					assembly_builder->Save(full_name);
				}
				catch (Exception ^e)
				{
					message->Append(ExtResources::GetString("could_not_save_wrapper",
						full_name, wrappers_path, e->Message));
				}
			}

			return (message == nullptr ? nullptr : message->ToString());
		}

		// Emits the most efficient form of the <c>Ldarg</c> IL instruction given the constant.
		void WrapperGen::EmitLdarg(ILEmitter ^il, int i)
		{
			// arguments are 1-based in instance methods (arg0 is this)
			if (!il->MethodBase->IsStatic) i++;
			il->Ldarg(i);
		}

		// Emits the most efficient form of the <c>Ldarga</c> IL instruction given the constant.
		void WrapperGen::EmitLdarga(ILEmitter ^il, int i)
		{
			// arguments are 1-based in instance methods (arg0 is this)
			if (!il->MethodBase->IsStatic) i++;
			il->Ldarga(i);
		}

		// Emits IL instruction that cast the reference currently on stack to a reference or
		// value of a given type.
		void WrapperGen::EmitCast(ILEmitter ^il, Type ^type)
		{
			if (type->IsValueType)
			{
				// type is a value type -> unbox it
				il->Emit(OpCodes::Unbox, type);

				switch (Type::GetTypeCode(type))
				{
					case TypeCode::Boolean:	il->Emit(OpCodes::Ldind_I1); break;
					case TypeCode::Int32:	il->Emit(OpCodes::Ldind_I4); break;
					case TypeCode::Double:	il->Emit(OpCodes::Ldind_R8); break;
					default:
						throw gcnew ArgumentException(ExtResources::GetString("must_be_phalanger_value_type"),
							"type");
				}
			}
			else 
			{
				// type is a reference type
				if (type->Equals(Types::Object[0]) == false)
				{
					// type is not Object -> cast it (safely)
					il->Emit(OpCodes::Isinst, type);
				}
			}
		}

		// Emits IL instruction that convert the reference currently on stack to a reference or
		// value of a given type (using methods in <c>PHP.Core.Convert</c> class).
		void WrapperGen::EmitConvert(ILEmitter ^il, Type ^type)
		{
			MethodInfo ^info = nullptr;
			switch (Type::GetTypeCode(type))
			{
				case TypeCode::Boolean:	info = Methods::Convert::ObjectToBoolean;	break;
				case TypeCode::Int32:	info = Methods::Convert::ObjectToInteger;	break;
				case TypeCode::Double:	info = Methods::Convert::ObjectToDouble;	break;
				case TypeCode::String:	info = Methods::Convert::ObjectToString;	break;
				case TypeCode::Object:
				{
					if (type->Equals(PHP::Core::PhpArray::typeid) ||
						type->Equals(PHP::Core::PhpResource::typeid) ||
						type->Equals(PHP::Core::PhpBytes::typeid) ||
						type->Equals(PHP::Core::PhpObject::typeid))
					{
						il->Emit(OpCodes::Isinst, type);
						break;
					}
					else if (type->Equals(Types::Object[0])) break;

					// break omitted intentionally
				}

				default:
					throw gcnew ArgumentException(ExtResources::GetString("must_be_phalanger_type"), "type");
			}

			if (info != nullptr) il->Emit(OpCodes::Call, info);
		}

		// Emits IL instructions that create an array and fill it with (appropriately boxed) method parameters.
		// Called from <see cref="GenerateFunction"/>.
		LocalBuilder ^ WrapperGen::EmitParameterHandlingPrologue(ILEmitter ^il, MethodBuilder ^methodBuilder, 
			FunctionTypeInfo ^typeInfo, int extraParamCount, bool generatingArgfull)
		{
			LocalBuilder ^local_builder = il->DeclareLocal(array<Object ^>::typeid);

			if (typeInfo != nullptr)
			{
				if (typeInfo->Params->Count > 0)
				{
					// if vararg param is defined, array len is number of params + length of the input array - 1
					if (typeInfo->VarargParamDefined)
					{
						il->LdcI4(typeInfo->Params->Count - 1);
						EmitLdarg(il, typeInfo->Params->Count - 1 + extraParamCount);
						il->Emit(OpCodes::Ldlen);
						il->Emit(OpCodes::Add);
					}
					else il->LdcI4(typeInfo->Params->Count);

					// create array to be passed to InvokeFunction/InvokeMethod
					il->Emit(OpCodes::Newarr, Types::Object[0]);
					
					for (int i = 0; i < typeInfo->Params->Count; i++)
					{
						// set parameter names
						ParameterTypeInfo ^param_info = typeInfo->GetParameterByIndex(i);

						ParameterAttributes attrs =
							((param_info->Direction == DirectionFlag::Out && !generatingArgfull) ?
							ParameterAttributes::Out : ParameterAttributes::None);

						ParameterBuilder ^parameter_builder;
						parameter_builder = methodBuilder->DefineParameter(i + 1 + extraParamCount, attrs, param_info->ParamName);

						// create ParamArray attribute for vararg param (but not byref vararg param)
						if (param_info->Optional == OptionalFlag::Vararg && 
							param_info->Direction == DirectionFlag::In)
						{
							parameter_builder->SetCustomAttribute(AttributeBuilders::ParamArray);
						}

						// create Optional attribute for optional parameters if generating argfull
						if (generatingArgfull && param_info->Optional == OptionalFlag::Yes)
						{
							parameter_builder->SetCustomAttribute(AttributeBuilders::Optional);
						}

						// only in/inout parameters and parameters to be bound are marshaled to the callee
						if (param_info->Bind || (param_info->Direction & DirectionFlag::In) == DirectionFlag::In)
						{
							// emit parameter preparation
							if (param_info->Optional == OptionalFlag::Vararg)
							{
								il->Stloc(local_builder);

								// call Array.CopyTo on the incoming array
								EmitLdarg(il, i + extraParamCount);
								il->Ldloc(local_builder);
								il->LdcI4(i);
								
								il->Emit(OpCodes::Callvirt, Methods::ArrayCopyTo);

								//
								// quit
								//
								return local_builder;
							}
							else
							{
								il->Emit(OpCodes::Dup);
								il->LdcI4(i);
								EmitLdarg(il, i + extraParamCount);
								if (param_info->IsOut && param_info->Bind == false)
								{
									Type ^unref_type = param_info->ParamType->GetElementType();
									il->Ldind(unref_type);

									if (unref_type->IsValueType) il->Emit(OpCodes::Box, unref_type);
								}
								else if (param_info->ParamType->IsValueType) il->Emit(OpCodes::Box, param_info->ParamType);
								il->Emit(OpCodes::Stelem_Ref);
							}
						}
					}
				}
				else
				{
					// if there are no parameters, the parameter array does not need to be constructed
					il->Emit(OpCodes::Ldnull);
				}

				// load address of the args array
				il->Stloc(local_builder);
			}
			else
			{
				if (generatingArgfull)
				{
					il->Emit(OpCodes::Ldnull);
					il->Stloc(local_builder);
				}
				else
				{
					// create ParamArray attribute for the only vararg param if we have no type info
					CustomAttributeBuilder ^attribute_builder = gcnew CustomAttributeBuilder(
						Constructors::ParamArray, gcnew array<Object ^>(0));

					ParameterBuilder ^parameter_builder = methodBuilder->DefineParameter(1 + extraParamCount, 
						ParameterAttributes::None, "args");
					parameter_builder->SetCustomAttribute(attribute_builder);

					EmitLdarg(il, 0);	// just copy 0.arg into local variable // can be optimized by omitting
					il->Stloc(local_builder);
				}
			}

			return local_builder;
		}

		// Emits IL instructions that create an array and fill it with (appropriately boxed) method parameters.
		// Called from <see cref="GenerateFunction"/>.
		void WrapperGen::EmitConstructorParameterHandlingPrologue(ILEmitter ^il, ConstructorBuilder ^ctorBuilder, 
			FunctionTypeInfo ^typeInfo)
		{
			il->Ldarg(0);

			if (typeInfo != nullptr)
			{
				for (int i = 0; i < typeInfo->Params->Count; i++)
				{
					// set parameter names and out attributes
					ParameterTypeInfo ^param_info = typeInfo->GetParameterByIndex(i);

					ParameterAttributes attrs =
						((param_info->Direction == DirectionFlag::Out) ? ParameterAttributes::Out : ParameterAttributes::None);

					ParameterBuilder ^parameter_builder = 
						ctorBuilder->DefineParameter(i + 1, attrs, param_info->ParamName);

					// create ParamArray attribute for vararg param (but not byref vararg param)
					if (param_info->Optional == OptionalFlag::Vararg && 
						param_info->Direction == DirectionFlag::In)
					{
						CustomAttributeBuilder ^attribute_builder = gcnew CustomAttributeBuilder(
							Constructors::ParamArray, gcnew array<Object ^>(0));

						parameter_builder->SetCustomAttribute(attribute_builder);
					}

					il->Ldarg(i + 1);
				}
			}
			else
			{
				// create ParamArray attribute for the only vararg param if we have no type info
				CustomAttributeBuilder ^attribute_builder = gcnew CustomAttributeBuilder(
					Constructors::ParamArray, gcnew array<Object ^>(0));

				ParameterBuilder ^parameter_builder = ctorBuilder->DefineParameter(1, 
					ParameterAttributes::None, "args");
				parameter_builder->SetCustomAttribute(attribute_builder);

				il->Ldarg(1);
			}
		}

		// Emits IL instructions that extract (appropriately unboxed byref parameters from the array created by
		// <see cref="EmitParameterHandlingPrologue"/>). Called from <see cref="GenerateFunction"/>.
		void WrapperGen::EmitParameterHandlingEpilogue(ILEmitter ^il, MethodBuilder ^methodBuilder,
				FunctionTypeInfo ^typeInfo, int extraParamCount, LocalBuilder^ local_args)
		{
			for (int i = 0; i < typeInfo->Params->Count; i++)
			{
				// get param info
				ParameterTypeInfo ^param_info = typeInfo->GetParameterByIndex(i);

				if (param_info->IsOut && param_info->Bind == false)
				{
					if (param_info->Optional != OptionalFlag::Vararg)
					{
						// non-vararg param
						EmitLdarg(il, i + extraParamCount);
						il->Ldloc(local_args);
						il->LdcI4(i);
						il->Emit(OpCodes::Ldelem_Ref);

						if (param_info->ParamType == Types::PhpReference[0])
						{
							il->Emit(OpCodes::Stfld, Fields::PhpReference_Value);
						}
						else
						{
							EmitConvert(il, param_info->ParamType->GetElementType());
							il->Stind(param_info->ParamType->GetElementType());
						}
					}
					else
					{
						// vararg param
						il->Ldloc(local_args);
						il->LdcI4(i);
						EmitLdarg(il, i + extraParamCount);
						il->Emit(OpCodes::Ldc_I4_0);
						EmitLdarg(il, i + extraParamCount);
						il->Emit(OpCodes::Ldlen);

						il->Emit(OpCodes::Call, Methods::ArrayCopy);
					}
				}
			}
		}

		// Generates code that loads reference to a given integer array onto evaluation stack.
		LocalBuilder^ WrapperGen::EmitRefInfoArrayInit(ILEmitter ^il, String ^functionName, TypeBuilder ^typeBuilder, array<int> ^refInfo)
		{
			int byref_count;
			if (refInfo != nullptr && (byref_count = refInfo->Length) > 0)
			{
				LocalBuilder^local_refinfo = il->DeclareLocal(array<int>::typeid);

				// create token to be used for refInfo initialization
				array<unsigned char> ^token = gcnew array<unsigned char>(4 * byref_count);
				for (int k = 0; k < byref_count; k++) BitConverter::GetBytes(refInfo[k])->CopyTo(token, k * 4);
				
				// TODO: reuse this static fields, if they are same
				FieldBuilder ^token_field = typeBuilder->DefineInitializedData(String::Format(
					"${0}${1}$refinfo", functionName, overloadID), token,
					FieldAttributes::Static | FieldAttributes::Assembly);

				il->LdcI4(byref_count);
				il->Emit(OpCodes::Newarr, Int32::typeid);
				il->Emit(OpCodes::Dup);
				
				il->Emit(OpCodes::Ldtoken, token_field);
				il->Emit(OpCodes::Call, Methods::InitializeArray);

				il->Stloc(local_refinfo);

				return local_refinfo;
			}
			else
			{
				return nullptr;
			}
		}

		// Emits a cast of null or -1 to false.
		void WrapperGen::EmitCastToFalse(ILEmitter ^il, Type ^returnType)
		{
			Label no_cast_label = il->DefineLabel();

			// NULL means a failure
			il->Emit(OpCodes::Dup);

			if (returnType->Equals(Types::Int[0]))
			{
				Label cast_label = il->DefineLabel();
				il->Emit(OpCodes::Brfalse_S, cast_label);

				// -1 means a failure
				il->Emit(OpCodes::Dup);
				il->Emit(OpCodes::Ldc_I4_M1);
				il->Emit(OpCodes::Box, Types::Int[0]);
				il->Emit(OpCodes::Callvirt, Methods::Object_Equals);

				il->Emit(OpCodes::Brfalse_S, no_cast_label);

				il->MarkLabel(cast_label, true);
			}
			else il->Emit(OpCodes::Brtrue_S, no_cast_label);

			// replace the return value with boxed false
			il->Emit(OpCodes::Pop);
			il->Emit(OpCodes::Ldc_I4_0);
			il->Emit(OpCodes::Box, Types::Bool[0]);

			il->MarkLabel(no_cast_label, true);
		}

		// Emits code that marshals bound variables.
		void WrapperGen::EmitBoundVariableMarshal(ILEmitter ^il, bool marshalOut)
		{
			// call Externals::MarshalBoundVariables(fileName, <marshalOut>)
			il->Emit(OpCodes::Ldstr, fileName);
			il->LoadBool(marshalOut);
			il->Emit(OpCodes::Call, Methods::Externals::MarshalBoundVariables);
		}

		// Generates an exported CLI constructor.
		void WrapperGen::GenerateConstructor(TypeBuilder ^typeBuilder, MethodInfo ^ctorMethod, array<Type ^> ^paramTypes,
			FunctionTypeInfo ^typeInfo)
		{
			// generate a CLI constructor
			ConstructorBuilder ^ctor_builder = typeBuilder->DefineConstructor(
				MethodAttributes::Public | MethodAttributes::HideBySig | MethodAttributes::SpecialName |
				MethodAttributes::RTSpecialName, CallingConventions::Standard, paramTypes);

			ILEmitter ^il = gcnew ILEmitter(ctor_builder);
			
			// call the base class's (ScriptContext) constructor
			il->Emit(OpCodes::Ldarg_0);
			il->Emit(OpCodes::Call, Methods::ScriptContext::GetCurrentContext);
			il->LdcI4(1);
			il->Emit(OpCodes::Call, typeBuilder->BaseType->GetConstructor(Types::ScriptContext_Bool));

			// prepare parameters (perform a mere pass-thru)
			EmitConstructorParameterHandlingPrologue(il, ctor_builder, typeInfo);
			
			// call the constructor method
			il->Emit(OpCodes::Call, ctorMethod);
			il->Emit(OpCodes::Ret);

			constructorDefined = true;
		}


		// Generates a static cache for IExternalFunction proxy.
		MethodInfo^ WrapperGen::GenerateExternalFunctionGetter(TypeBuilder ^typeBuilder, String ^className, String ^functionName)
		{
			// add field used as IExternalFunction cache
			FieldBuilder ^field = typeBuilder->DefineField(String::Format("${0}$fn$cache", functionName), PHP::Core::IExternalFunction::typeid, FieldAttributes::Assembly | FieldAttributes::Static);

			// add method returning the IExternalFunction / initializing the field
			MethodBuilder^ getter = typeBuilder->DefineMethod(
				String::Format("${0}$fn$get", functionName),
				MethodAttributes::Assembly | MethodAttributes::HideBySig | MethodAttributes::Static,
				field->FieldType, Type::EmptyTypes);

			ILEmitter^ il = gcnew ILEmitter(getter);

			// emit getter:
			// return (field ?? (field = GetFunctionProxy(filename, className, functionName)));
			Label lblret = il->DefineLabel();
			il->Emit(OpCodes::Ldsfld, field);
			il->Emit(OpCodes::Dup);
			il->Emit(OpCodes::Brtrue_S, lblret);
			il->Emit(OpCodes::Pop);

			// GetFunctionProxy(filename, className, functionName)
			il->Emit(OpCodes::Ldstr, fileName);
			if (className == nullptr) il->Emit(OpCodes::Ldnull); else il->Emit(OpCodes::Ldstr, className);
			il->Emit(OpCodes::Ldstr, functionName);
			il->Emit(OpCodes::Call, Methods::Externals::GetFunctionProxy);

			// field =
			il->Emit(OpCodes::Dup);
			il->Emit(OpCodes::Stsfld, field);

			il->MarkLabel(lblret);
			il->Emit(OpCodes::Ret);

			//
			return getter;
		}


		// Retrieve Type[] information of specified function used to be emitted.
		array<Type ^> ^WrapperGen::BuildFunctionArguments(FunctionTypeInfo ^typeInfo, bool argfullOverload,
			bool% is_static, int% byref_count, array<int> ^%ref_info, Type^% return_type, int% extra_param_count, String ^% wrapping_function_name)
		{
			array<Type ^> ^param_types;

			is_static = false;
			byref_count = 0;
			extra_param_count = (argfullOverload ? 1 : 0);
			ref_info = nullptr;
			
			if (typeInfo != nullptr)
			{
				// type information is available
				if (typeInfo->Name != nullptr) wrapping_function_name = typeInfo->Name;

				is_static = typeInfo->Static;
				const int param_count = typeInfo->Params->Count;
				return_type = typeInfo->ReturnType;

				param_types = gcnew array<Type ^>(param_count + extra_param_count);
				for (int i = 0; i < param_count; i++)
				{
					ParameterTypeInfo ^param_info = typeInfo->GetParameterByIndex(i);

					param_types[i + extra_param_count] = param_info->ParamType;
					if (param_info->IsOut) ++byref_count;
				}
				if (argfullOverload) param_types[0] = Types::ScriptContext[0];

				if (typeInfo->VarargParamDefined && byref_count > 0) ++byref_count;

				// ref_info
				ref_info = gcnew array<int>(byref_count);

				if (typeInfo->VarargParamDefined && byref_count > 0) ref_info[byref_count - 1] = -1;

				// build by_ref array
				int ref_counter = 0;
				for (int i = 0; i < param_count; ++i)
				{
					if (typeInfo->GetParameterByIndex(i)->IsOut) ref_info[ref_counter++] = i;
				}
			}
			else
			{
				// type information is not available
				return_type = Types::Object[0];
				
				if (argfullOverload)
				{
					param_types = gcnew array<Type ^> { Types::ScriptContext[0] };
				}
				else
				{
					param_types = Types::ObjectArray;
				}
			}

			//
			return param_types;
		}


		// Create MethodBuilder of specified function. Set custom attributes.
		MethodBuilder^ WrapperGen::DefineFunctionMethod( String ^ className, bool argfullOverload, String ^ functionName, TypeBuilder ^ typeBuilder,
			bool is_static, String^ wrapping_function_name, Type^ %return_type, array<Type^>^ param_types, FunctionTypeInfo ^ typeInfo )
		{
			MethodBuilder ^method_builder;

			if (className != nullptr)
			{
				bool generate_ctor = false;

				// we're generating a PHP method
				if (!argfullOverload && String::Compare(functionName, typeBuilder->Name, true) == 0)
				{
					// this is a PHP 4 - style constructor
					if (is_static)
					{
						message->Append(ExtResources::GetString("constructor_static", className, wrapping_function_name));
						return nullptr;
					}

					return_type = Void::typeid;
					generate_ctor = true;
				}

				MethodAttributes attrs = (is_static ? MethodAttributes::Static : MethodAttributes::Virtual) | MethodAttributes::Public /*| MethodAttributes::HideBySig*/;

				method_builder = typeBuilder->DefineMethod(wrapping_function_name, attrs, return_type, param_types);

				if (generate_ctor)
				{
					// generate a CLI constructor that delegates to this method
					GenerateConstructor(typeBuilder, method_builder, param_types, typeInfo);
				}
			}
			else
			{
				if (is_static)
				{
					message->Append(ExtResources::GetString("function_static", wrapping_function_name));
					return nullptr;
				}
				MethodAttributes attrs = MethodAttributes::Static | MethodAttributes::Public /*| MethodAttributes::HideBySig*/;

				// we're generating a PHP function
				method_builder = typeBuilder->DefineMethod(wrapping_function_name, attrs, return_type, param_types);

				// create method attribute ImplementsFunctionAttribute
				array<Object ^> ^ctor_params = { functionName };

				CustomAttributeBuilder ^attribute_builder = gcnew CustomAttributeBuilder(
					Constructors::ImplementsFunction, ctor_params);

				method_builder->SetCustomAttribute(attribute_builder);
			}

			// name the SC parameter and decorate argfull overloads with EditorBrowsable(Never)
			if (argfullOverload)
			{
				method_builder->DefineParameter(1, ParameterAttributes::None, "context");
				method_builder->SetCustomAttribute(AttributeBuilders::EditorBrowsableNever);
			}

			// create return value attribute CastToFalseAttribute
			if (!argfullOverload && typeInfo != nullptr && typeInfo->CastToFalse == true)
			{
				CustomAttributeBuilder ^attribute_builder = gcnew CustomAttributeBuilder(
					Constructors::CastToFalse, gcnew array<Object ^>(0));

				ParameterBuilder ^param_builder = method_builder->DefineParameter(0, ParameterAttributes::None, nullptr);
				param_builder->SetCustomAttribute(attribute_builder);
			}

			return method_builder;
		}

		// Generates one managed wrapper method (exported stub or argfull overload).
		void WrapperGen::GenerateFunction(String ^className, String ^functionName, TypeBuilder ^typeBuilder,
			FunctionTypeInfo ^typeInfo, MethodInfo ^externalFunctionGetter, bool argfullOverload)
		{
			// this is a counter that counts number of overloads (methods with identical names)
			// it is used when something with unique name (for example a field) needs to be generated
			++overloadID;

			// define types of method arguments
			int extra_param_count, byref_count;
			bool is_static;
			array<int> ^ref_info;
			Type^ return_type;
			String^ wrapping_function_name = functionName;
			array<Type^>^ param_types = BuildFunctionArguments(typeInfo, argfullOverload,
				is_static, byref_count, ref_info, return_type, extra_param_count, wrapping_function_name);
			int param_count = param_types->Length - extra_param_count;


#ifdef DEBUG
			StringBuilder ^param_types_str = gcnew StringBuilder();
			for (int _i = 0; _i < param_count; _i++)
			{
				if (_i > 0) param_types_str->Append(",");
				param_types_str->Append(param_types[_i]->ToString());
			}
			Debug::WriteLine("EXT SUP", String::Concat("- generating method ", functionName, "",
				return_type->ToString(), "(", param_types_str, ")"));
#endif

			// create method builder
			MethodBuilder ^method_builder = DefineFunctionMethod(
				className, argfullOverload, functionName, typeBuilder, is_static,
				wrapping_function_name, return_type, param_types, typeInfo);

			//
			// generate method body
			//
			ILEmitter ^il = gcnew ILEmitter(method_builder);

			if (typeInfo != nullptr && typeInfo->MarshalBoundVarsIn) EmitBoundVariableMarshal(il, false);

			// object[] args = {...};	// local variable // set parameter names & emit parameter preparation code
			LocalBuilder^ local_args = EmitParameterHandlingPrologue(il, method_builder, typeInfo, extra_param_count, argfullOverload);

			// {ScriptContext context = ScriptContext.Current;}
			LocalBuilder^ local_context;
			if (argfullOverload){}	// 0. argument
			else	// in local variable <context>
			{
				local_context = il->DeclareLocal(Types::ScriptContext[0]);
				il->Emit(OpCodes::Call, Methods::ScriptContext::GetCurrentContext);
				il->Emit(OpCodes::Stloc, local_context);
			}
			#define LD_SELF				{if (className!=nullptr && !is_static) il->Emit(OpCodes::Ldarg_0); else il->Emit(OpCodes::Ldnull);}
			#define LD_SCRIPTCONTEXT	{if (argfullOverload) EmitLdarg(il, 0); else il->Emit(OpCodes::Ldloc, local_context);}

			// int[] refinfo // prepare refInfo int[] array
			LocalBuilder^ local_refinfo = EmitRefInfoArrayInit(il, functionName, typeBuilder, ref_info);// can be null

			#define LD_REFINFO		{if (local_refinfo != nullptr)il->Ldloc(local_refinfo);else il->Emit(OpCodes::Ldnull);}

			/**/
			/**/ // see Externals::InvokeExternalFunction
			/**/

			// // extract parameters that should be bound
			// PhpReference[] param_binding = PrepareParametersForBinding(args);   // needed only if PhpReference was passed (typeof args[i] = PhpReference|Object)
			LocalBuilder^ local_param_binding = nullptr;
			for(int i = 0; i< param_types->Length; ++i)
				if (WrapperTypeDef::TypeTriple(Object::typeid).IsEqualsAny(param_types[i]) ||
					WrapperTypeDef::TypeTriple(PHP::Core::PhpReference::typeid).IsEqualsAny(param_types[i]))
				{	// parameter that can contain PhpReference can be passed in
					local_param_binding = il->DeclareLocal(array<PHP::Core::PhpReference^>::typeid);
					il->Ldloc(local_args);
					il->Emit(OpCodes::Call, Methods::Externals::PrepareParametersForBinding);
					il->Stloc(local_param_binding);
					break;
				}

			// // transform parameters // needed only for DObject only (typeof args[i] = PhpReference|DObject|Object|PhpArray)
			// ParameterTransformation.TransformInParameters(self, context, args);
			bool performTransformParameters = (local_refinfo != nullptr);
			if (!performTransformParameters)
				for(int i = 0; i< param_types->Length; ++i)
					if (WrapperTypeDef::TypeTriple(Object::typeid).IsEqualsAny(param_types[i]) ||
						WrapperTypeDef::TypeTriple(PHP::Core::PhpObject::typeid).IsEqualsAny(param_types[i]) ||
						WrapperTypeDef::TypeTriple(PHP::Core::PhpReference::typeid).IsEqualsAny(param_types[i]) ||
						WrapperTypeDef::TypeTriple(PHP::Core::PhpArray::typeid).IsEqualsAny(param_types[i]))
					{	// parameter that can contain PhpReference can be passed in
						performTransformParameters = true;
						break;
					}
			if (performTransformParameters || (className!=nullptr && !is_static))
			{
				LD_SELF;
				LD_SCRIPTCONTEXT;
				if (performTransformParameters) il->Ldloc(local_args); else il->Emit(OpCodes::Ldnull);
				il->Emit(OpCodes::Call, Methods::Externals::ParameterTransformation::TransformInParameters);
			}
			
			// // invoke the function
			// // note: args reference will not be changed
			// ret_value = externalfunc.Invoke(self, ref args, refInfo, context.WorkingDirectory);
			LocalBuilder^local_ret_value = il->DeclareLocal(Object::typeid);
			LocalBuilder^local_externalfunc = nullptr;	// !!! is used only if local_param_binding != nullptr
			il->Emit(OpCodes::Call, externalFunctionGetter);	// cannot be null
			if (local_param_binding != nullptr)
			{
				il->Emit(OpCodes::Dup);
				local_externalfunc = il->DeclareLocal(PHP::Core::IExternalFunction::typeid);				
				il->Stloc(local_externalfunc);
			}
			LD_SELF;
			il->Ldloca(local_args);
			LD_REFINFO;
			LD_SCRIPTCONTEXT; il->Emit(OpCodes::Call, Methods::ScriptContext::GetWorkingDirectory);
			il->Emit(OpCodes::Callvirt, Methods::Externals::IExternalFunction::Invoke);

			il->Stloc(local_ret_value);

			// // transform parameters (opposite of TransformInParameters)
			// ParameterTransformation.TransformOutParameters(self, context, args);
			if (performTransformParameters || (className!=nullptr && !is_static))
			{
				LD_SELF;
				LD_SCRIPTCONTEXT;
				if (performTransformParameters) il->Ldloc(local_args); else il->Emit(OpCodes::Ldnull);
				il->Emit(OpCodes::Call, Methods::Externals::ParameterTransformation::TransformOutParameters);
			}

			// // bind parameters
			// if (param_binding != null)
			//	BindParameters(moduleName, args, refInfo, externalfunc.ExtManager, /*!*/param_binding);
			if (local_param_binding != nullptr)
			{
				Debug::Assert(local_externalfunc != nullptr);

				Label lbl_nobind = il->DefineLabel();
				il->Ldloc(local_param_binding);
				il->Emit(OpCodes::Brfalse, lbl_nobind);

				//BindParameters("!!!MODULE NAME!!!", args, refInfo, externalfunc.ExtManager, /*!*/param_binding);
				il->Emit(OpCodes::Ldstr, fileName);
				il->Ldloc(local_args);
				LD_REFINFO;
				il->Ldloc(local_externalfunc);
				il->Emit(OpCodes::Callvirt, Methods::Externals::IExternalFunction::GetExtManager);
				il->Ldloc(local_param_binding);
				il->Emit(OpCodes::Call, Methods::Externals::BindParameters);

				il->MarkLabel(lbl_nobind, true);
			}
			/**/
			/**/
			/**/

/*
			//
			// call {InvokeFunction|InvokeMethod}(moduleName, [className], functioName, [self, context], args, refInfo)
			//

			// extension moduleName, [class name], functioName
			il->Emit(OpCodes::Ldstr, fileName);
			if (className != nullptr) il->Emit(OpCodes::Ldstr, className);
			il->Emit(OpCodes::Ldstr, functionName);

			// [$this, ScriptContext]
			if (className != nullptr)
			{
				LD_SELF;
				LD_SCRIPTCONTEXT;
			}

			// ref args
			il->Ldloca(local_args);

			// int[] refInfo
			LD_REFINFO;

			// call InvokeFunctionEx/InvokeMethodEx
			il->Emit(OpCodes::Call, (className == nullptr ? Methods::Externals::InvokeFunction : Methods::Externals::InvokeMethod));
*/
			//
			// emit byref parameters conversion
			//
			if (typeInfo != nullptr && byref_count > 0)
				EmitParameterHandlingEpilogue(il, method_builder, typeInfo, extra_param_count, local_args);
			
			// emit return value conversion
			if (!return_type->Equals(Void::typeid))
			{
				// ParameterTransformation.TransformOutParameter(context, ref ret_value)
				LD_SCRIPTCONTEXT;
				il->Ldloca(local_ret_value);
				il->Emit(OpCodes::Call, Methods::Externals::ParameterTransformation::TransformOutParameter);

				// convert return value
				il->Ldloc(local_ret_value);

				// change the return value to 'false' if it represents a failure
				if (argfullOverload && typeInfo != nullptr && typeInfo->CastToFalse) EmitCastToFalse(il, return_type);

				EmitConvert(il, return_type);
			}

			if (typeInfo != nullptr && typeInfo->MarshalBoundVarsOut) EmitBoundVariableMarshal(il, false);

			il->Emit(OpCodes::Ret);
		}

		// Handles generating more managed wrapper method overloads when there are some optional parameters
		// defined in the associated typedef XML file.
		void WrapperGen::GenerateFunctionOverloadsPhase2(String ^className, String ^functionName,
			TypeBuilder ^typeBuilder, FunctionTypeInfo ^typeInfo, MethodInfo ^externalFunctionGetter)
		{
			if (typeInfo != nullptr && typeInfo->FirstOptionalParamIndex >= 0)
			{
				// this function has some optional parameters
				if (typeInfo->VarargParamDefined)
				{
					// this function has exactly one multiple param and one or more optional params
					int count = typeInfo->Params->Count;
					do
					{
						// generate appropriate wrapper methods, for example f([optional]a, [optional]b, [vararg]c) yields:
						// f(a, b, c), f(a, c) and f(c)
						GenerateFunction(className, functionName, typeBuilder, typeInfo, externalFunctionGetter, false);

						if (--count == 0) break;
						typeInfo->Params->RemoveAt(count - 1);
					}
					while (count - 1 >= typeInfo->FirstOptionalParamIndex);
				}
				else
				{
					// this function has one or more optional params
					int count = typeInfo->Params->Count;
					do
					{
						GenerateFunction(className, functionName, typeBuilder, typeInfo, externalFunctionGetter, false);

						if (count == 0) break;
						typeInfo->Params->RemoveAt(count - 1);
						count--;
					
					}
					while (count >= typeInfo->FirstOptionalParamIndex);
				}
			}
			else GenerateFunction(className, functionName, typeBuilder, typeInfo, externalFunctionGetter, false);
		}

		// Handles generating more managed wrapper method overloads when there are more signatures defined
		// in the associated typedef XML file.
		void WrapperGen::GenerateFunctionOverloads(String ^className, String ^functionName,
			TypeBuilder ^typeBuilder, FunctionTypeInfo ^typeInfo, MethodInfo ^externalFunctionGetter)
		{
			overloadID = 0;
			
			if (typeInfo != nullptr)
			{
				do
				{
					// generate overloads for one signature defined in XML
					GenerateFunctionOverloadsPhase2(className, functionName, typeBuilder, typeInfo, externalFunctionGetter);

					// traverse the list
					typeInfo = typeInfo->Next;
				}
				while (typeInfo != nullptr);
			}
			else GenerateFunction(className, functionName, typeBuilder, nullptr, externalFunctionGetter, false);
		}

		// Generates the <B>ArrayAccess</B> interface implementation that delegates to the given methods.
		void WrapperGen::GenerateArrayAccess(TypeBuilder ^typeBuilder, String ^arrayGetter, String ^arraySetter)
		{
			MethodAttributes attrs = MethodAttributes::Public | MethodAttributes::HideBySig | MethodAttributes::Static;
			ILEmitter ^il;

			// offsetGet
			MethodBuilder ^get_argless = typeBuilder->DefineMethod("offsetGet", attrs, Types::Object[0], Types::Object_PhpStack);
			il = gcnew ILEmitter(get_argless);
			if (arrayGetter != nullptr)
			{
				// [ return instance.InvokeMethod(arrayGetter, null) ]
				il->Ldarg(0);
				il->Emit(OpCodes::Castclass, Types::PhpObject[0]);
				il->Emit(OpCodes::Ldstr, arrayGetter);
				il->Emit(OpCodes::Ldnull);
				il->Ldarg(1);
				il->Emit(OpCodes::Ldfld, Fields::PhpStack_Context);
				il->Emit(OpCodes::Call, Methods::DObject_InvokeMethod);
			}
			else
			{
				// [ stack.RemoveFrame(); return null ]
				il->Ldarg(1);
				il->Emit(OpCodes::Call, Methods::PhpStack::RemoveFrame);
				il->Emit(OpCodes::Ldnull);
			}
			il->Emit(OpCodes::Ret);

			// offsetSet
			MethodBuilder ^set_argless = typeBuilder->DefineMethod("offsetSet", attrs, Types::Object[0], Types::Object_PhpStack);
			il = gcnew ILEmitter(set_argless);
			if (arraySetter != nullptr)
			{
				// [ return instance.InvokeMethod(arraySetter, null) ]
				il->Ldarg(0);
				il->Emit(OpCodes::Castclass, Types::PhpObject[0]);
				il->Emit(OpCodes::Ldstr, arraySetter);
				il->Emit(OpCodes::Ldnull);
				il->Ldarg(1);
				il->Emit(OpCodes::Ldfld, Fields::PhpStack_Context);
				il->Emit(OpCodes::Call, Methods::DObject_InvokeMethod);
			}
			else
			{
				// [ stack.RemoveFrame(); return null ]
				il->Ldarg(1);
				il->Emit(OpCodes::Call, Methods::PhpStack::RemoveFrame);
				il->Emit(OpCodes::Ldnull);
			}
			il->Emit(OpCodes::Ret);

			// offsetUnset
			MethodBuilder ^unset_argless = typeBuilder->DefineMethod("offsetUnset", attrs, Types::Object[0], Types::Object_PhpStack);
			il = gcnew ILEmitter(unset_argless);
			// [ stack.RemoveFrame(); return null ]
			il->Ldarg(1);
			il->Emit(OpCodes::Call, Methods::PhpStack::RemoveFrame);
			il->Emit(OpCodes::Ldnull);
			il->Emit(OpCodes::Ret);

			// offsetExists
			MethodBuilder ^exists_argless = typeBuilder->DefineMethod("offsetExists", attrs, Types::Object[0], Types::Object_PhpStack);
			il = gcnew ILEmitter(exists_argless);
			// [ stack.RemoveFrame(); return true ]
			il->Ldarg(1);
			il->Emit(OpCodes::Call, Methods::PhpStack::RemoveFrame);
			il->LdcI4(1);
			il->Emit(OpCodes::Box, Types::Bool[0]);
			il->Emit(OpCodes::Ret);

			// make all argless stubs [EditorBrowsable(Never)]
			set_argless->SetCustomAttribute(AttributeBuilders::EditorBrowsableNever);
			get_argless->SetCustomAttribute(AttributeBuilders::EditorBrowsableNever);
			unset_argless->SetCustomAttribute(AttributeBuilders::EditorBrowsableNever);
			exists_argless->SetCustomAttribute(AttributeBuilders::EditorBrowsableNever);

			// implement the ArrayAccess interface and delegate to the argless stubs
			GenerateArrayAccessMethod(typeBuilder, set_argless->Name, 2, set_argless);
			GenerateArrayAccessMethod(typeBuilder, get_argless->Name, 1, get_argless);
			GenerateArrayAccessMethod(typeBuilder, unset_argless->Name, 1, unset_argless);
			GenerateArrayAccessMethod(typeBuilder, exists_argless->Name, 1, exists_argless);
		}

		// Generates argfull overload implementing one ArrayAccess method.
		void WrapperGen::GenerateArrayAccessMethod(TypeBuilder ^typeBuilder, String ^name, int paramCount,
			MethodInfo ^argless)
		{
			MethodAttributes attrs = MethodAttributes::Public | MethodAttributes::HideBySig |
				MethodAttributes::NewSlot | MethodAttributes::Virtual | MethodAttributes::Final;

			array<Type ^> ^param_types = gcnew array<Type ^>(paramCount + 1);

			param_types[0] = Types::ScriptContext[0];
			for (int i = 1; i <= paramCount; i++) param_types[i] = Types::Object[0];

			ILEmitter ^il = gcnew ILEmitter(typeBuilder->DefineMethod(name, attrs, Types::Object[0], param_types));
			
			il->Ldarg(1);
			il->Emit(OpCodes::Ldfld, Fields::ScriptContext_Stack);
			
			for (int i = 0; i < paramCount; i++) il->Ldarg(i + 2);
			
			il->Emit(OpCodes::Call, Methods::PhpStack::AddFrame::Overload(paramCount));

			// instance
			il->Ldarg(0);

			// stack
			il->Ldarg(1);
			il->Emit(OpCodes::Ldfld, Fields::ScriptContext_Stack);

			il->Emit(OpCodes::Call, argless);
			il->Emit(OpCodes::Ret);
		}

		// Generates static literal fields (wrapping PHP constants).
		void WrapperGen::GenerateConstants(TypeBuilder ^typeBuilder)
		{
			// foreach constant
			IDictionaryEnumerator ^enumerator = constants->GetEnumerator();
			while (enumerator->MoveNext() == true)
			{
				Constant ^con = static_cast<Constant ^>(enumerator->Value);

				String ^con_name = con->GetName();
				Object ^con_value = con->GetValue();
				Type ^con_type = (con_value == nullptr ? Types::Object[0] : con_value->GetType());

				// IS_BOOL, IS_LONG, IS_DOUBLE, IS_STRING, IS_NULL
				if (con_type->Equals(Boolean::typeid) || con_type->Equals(Int32::typeid) || con_type->Equals(Int64::typeid) ||
					con_type->Equals(Double::typeid)  || con_type->Equals(String::typeid) || 
					con_value == nullptr)
				{
#ifdef DEBUG
					Debug::WriteLine("EXT SUP", String::Concat("- generating literal field ",
						con_name, " of type ", con_type, " and value ", con_value));
#endif

					// create field builder
					FieldBuilder ^field_builder = typeBuilder->DefineField(con_name, con_type, 
						FieldAttributes::Public | FieldAttributes::Static | FieldAttributes::Literal);

					field_builder->SetConstant(con_value);

					// add ImplementsConstantAttribute
					CustomAttributeBuilder ^attribute_builder;
					array<Object ^> ^ctor_params = { con_name };
					if (con->IsCaseInsensitive())
					{
						array<PropertyInfo ^> ^property_names = { Properties::ImplementsConstantCase };
						array<Object ^> ^property_values = { true };

						// [ImplementsConstant(con_name, CaseInsensitive = true)]
						attribute_builder = gcnew CustomAttributeBuilder(Constructors::ImplementsConstant,
							ctor_params, property_names, property_values);
					}
					else
					{
						// [ImplementsConstant(con_name)]
						attribute_builder = gcnew CustomAttributeBuilder(Constructors::ImplementsConstant,
							ctor_params);
					}

					field_builder->SetCustomAttribute(attribute_builder);
				}
				else Debug::Assert(false, "Invalid constant type.");
			}
		}

		// Generates static methods (wrapping PHP functions).
		void WrapperGen::GenerateFunctions(TypeBuilder ^typeBuilder, TypeBuilder ^dynamicTypeBuilder,
			scg::Dictionary<String ^, FunctionTypeInfo ^> ^typeInfo)
		{
			// foreach function
			for each (scg::KeyValuePair<String ^, FunctionTypeInfo ^> pair in typeInfo)
			{
				// generate getter of IExternalFunction
				MethodInfo^ externalFunctionGetter = GenerateExternalFunctionGetter(typeBuilder, nullptr, pair.Key );

				// generate arg-less overload
				GenerateDynamicFunction(nullptr, pair.Key, dynamicTypeBuilder, pair.Value, externalFunctionGetter);

				// generate "exported" overloads (class-lib argfulls)
				GenerateFunctionOverloads(nullptr, pair.Key, typeBuilder, pair.Value, externalFunctionGetter);
			}
		}

		// Generates the 'arg-less' method overload - the one taking just a <see cref="PhpStack"> parameter
		// (expecting the real parameters on this stack) and returning <see cref="Object"/>.
		MethodInfo ^WrapperGen::GenerateDynamicFunction(String ^className, String ^functionName,
				TypeBuilder ^typeBuilder, FunctionTypeInfo ^typeInfo, MethodInfo ^externalFunctionGetter)
		{
			FunctionTypeInfo ^fun_type_info = nullptr;

			OverloadTypeInfo ovr_type_info;
			ovr_type_info.Name = functionName;

			// inspect type info to find out
			// 1) real name of the method, 2) MarshalBoundVars flag, 3) 'bind' parameters 4) 'byref' parameters
			while (typeInfo != nullptr)
			{
				// if type definition defines more signatures, combine them to get one dynamic signature
				ovr_type_info.Combine(typeInfo);
				typeInfo = typeInfo->Next;
			}

			// convert byref_param_map and vararg_position into ref_info array
			int byref_count = 0;
			for (int i = 0; i < ovr_type_info.ByrefParamMap->Count; i++)
			{
				if (i == ovr_type_info.ByrefVarargPosition) byref_count++;
				if (ovr_type_info.ByrefParamMap[i]) byref_count++;
			}

			array<int> ^ref_info = gcnew array<int>(byref_count);
			int ref_index = 0;
			for (int i = 0; i < ovr_type_info.ByrefParamMap->Count; i++)
			{
				if (i == ovr_type_info.ByrefVarargPosition) ref_info[ref_index+1] = -1;
				if (ovr_type_info.ByrefParamMap[i]) ref_info[ref_index++] = i;
			}

			MethodBuilder ^method_builder = typeBuilder->DefineMethod(
					ovr_type_info.Name,
					MethodAttributes::Public | MethodAttributes::Static | MethodAttributes::HideBySig,
					Types::Object[0],
					Types::Object_PhpStack);

			// define parameter names "instance" and "stack"
			method_builder->DefineParameter(1, ParameterAttributes::None, "instance");
			method_builder->DefineParameter(2, ParameterAttributes::None, "stack");

			// annotate argless methods with EditorBrowsable
			if (className != nullptr) method_builder->SetCustomAttribute(AttributeBuilders::EditorBrowsableNever);

			ILEmitter ^il = gcnew ILEmitter(method_builder);

			// mark parameters for binding
			for (int i = 0; i < ovr_type_info.BindParamMap->Length; i++)
			{
				if (ovr_type_info.BindParamMap->Get(i))
				{
					il->Ldarg(1);
					il->LdcI4((ovr_type_info.VarargPosition == i) ? (-i - 1) : i);
					il->Emit(OpCodes::Call, Methods::Externals::MarkParameterForBinding);
				}
			}

			if (ovr_type_info.MarshalBoundVarsIn) EmitBoundVariableMarshal(il, false);

			LocalBuilder^ local_refinfo = EmitRefInfoArrayInit(il, functionName, typeBuilder, ref_info);
			
			// push extension name, class name, method name, $this
			il->Emit(OpCodes::Ldstr, fileName);
			if (className != nullptr) il->Emit(OpCodes::Ldstr, className);
			il->Emit(OpCodes::Ldstr, functionName);

			// stack parameter
			if (className == nullptr) il->Ldarg(1);
			else
			{
				if (ovr_type_info.IsStatic) il->Emit(OpCodes::Ldnull);
				else
				{
					il->Ldarg(0);
					il->Emit(OpCodes::Castclass, Types::PhpObject[0]);
				}

				il->Ldarg(1);
			}

			// refInfo parameter
			if (local_refinfo!=nullptr)il->Ldloc(local_refinfo);else il->Emit(OpCodes::Ldnull);

			// call Externals.InvokeFunctionDynamicEx/InvokeMethodDynamicEx
			il->Emit(OpCodes::Call, (className == nullptr ? Methods::Externals::InvokeFunctionDynamic : Methods::Externals::InvokeMethodDynamic));

			if (ovr_type_info.MarshalBoundVarsOut) EmitBoundVariableMarshal(il, false);

			// change the return value to 'false' if it represents a failure
			if (ovr_type_info.CastToFalse) EmitCastToFalse(il, ovr_type_info.ReturnType);

			il->Emit(OpCodes::Ret);

			return method_builder;
		}

		// Generates a class constant to the provided type builder.
		FieldInfo ^WrapperGen::GenerateClassConstant(TypeBuilder ^classBuilder, Constant ^constant)
		{
			Object ^value = constant->GetValue();

			FieldBuilder ^fb = classBuilder->DefineField(constant->GetName(), value->GetType(),
				FieldAttributes::Public | FieldAttributes::Static | FieldAttributes::Literal);
			fb->SetConstant(value);

			return fb;
		}

		// Generates classes (wrapping PHP classes).
		void WrapperGen::GenerateClasses(ModuleBuilder ^moduleBuilder, scg::Dictionary<String ^, ClassTypeInfo ^> ^typeInfo)
		{
			for each (scg::KeyValuePair<String ^, Object^> entry in classes)
			{
				Class ^cls = static_cast<Class ^>(entry.Value);
				if (cls->IsStdClass()) continue;

				String ^wrapping_class_name;
				String ^class_name = cls->GetClassName();
				
				ClassTypeInfo ^class_type_info = nullptr;
				if (typeInfo != nullptr) typeInfo->TryGetValue(class_name->ToLower(), class_type_info);
				
				if (class_type_info != nullptr && class_type_info->Name != nullptr)
				{
					wrapping_class_name = class_type_info->Name;
				}
				else wrapping_class_name = class_name;

#ifdef DEBUG
				Debug::WriteLine("EXT SUP", String::Concat("- generating class ", class_name));
#endif

				// determine base (aka parent) class
				String ^parent_name = cls->GetParentClassName();
				Type ^parent_type;
				if (parent_name != nullptr)
				{
					if (String::Compare(parent_name, "stdClass", true) == 0) parent_type = PHP::Library::stdClass::typeid;
					else if (String::Compare(parent_name, "RuntimeException", true) == 0 ||
						String::Compare(parent_name, "Exception", true) == 0) parent_type = PHP::Library::SPL::Exception::typeid;
					else
					{
						parent_type = moduleBuilder->GetType(String::Concat(Namespaces::Library, ".", parent_name), true);

						if (parent_type == nullptr)
						{
							throw gcnew ExtensionException(ExtResources::GetString("invalid_base_class",
								class_name, fileName));
						}
					}
				}
				else parent_type = PhpObject::typeid;
				
				// determine implemented interfaces
				array<Type ^> ^interface_types;
				if (class_type_info != nullptr &&
					(class_type_info->ArrayGetter != nullptr || class_type_info->ArraySetter != nullptr))
				{
					interface_types = gcnew array<Type ^>(1);
					interface_types[0] = PHP::Library::SPL::ArrayAccess::typeid;
				}
				else interface_types = gcnew array<Type ^>(0);

				// create the type builder
				TypeBuilder ^class_builder = moduleBuilder->DefineType(
					String::Concat(Namespaces::Library, ".", wrapping_class_name),
					TypeAttributes::Public | TypeAttributes::Class | TypeAttributes::Serializable |
					TypeAttributes::BeforeFieldInit,
					parent_type, interface_types);
				
				// decorate with [ImplementsType]
				class_builder->SetCustomAttribute(AttributeBuilders::ImplementsType);

				// generate the <typeDesc> field
				FieldBuilder ^type_desc_field = class_builder->DefineField(PhpObjectBuilder::TypeDescFieldName,
					Types::PhpTypeDesc[0], FieldAttributes::Public | FieldAttributes::Static | FieldAttributes::InitOnly);

				// generate short constructor (ScriptContext)
				ConstructorBuilder ^ctor_builder = PhpObjectBuilder::DefineShortConstructor(class_builder);
				ctor_builder->GetILGenerator()->Emit(OpCodes::Ret);

				// generate long constructor (ScriptContext, DTypeDesc)
				PhpObjectBuilder::GenerateLongConstructor(class_builder, ctor_builder);

				// generate deserializing constructor (SerializationInfo, StreamingContext)
				ctor_builder = PhpObjectBuilder::DefineDeserializingConstructor(class_builder);
				ctor_builder->GetILGenerator()->Emit(OpCodes::Ret);

				// generate SPL::ArrayAccess implementation
				if (interface_types->Length > 0)
				{
					GenerateArrayAccess(class_builder, class_type_info->ArrayGetter, class_type_info->ArraySetter);
				}

				// generate class constants
				/*not used, empty list*/scg::List<scg::KeyValuePair<FieldInfo ^,Object^>> ^constant_list = gcnew scg::List<scg::KeyValuePair<FieldInfo ^,Object^>>();

				IDictionaryEnumerator ^enumerator = cls->GetConstantEnumerator();
				while (enumerator->MoveNext() == true)
				{
					Constant ^constant = static_cast<Constant ^>(enumerator->Value);
					
					// // // if class constant are public static LITERAL, you can use it
					// will not be included in __PopulateTypeDesc
					/* // // // constant_list->Add(
						scg::KeyValuePair<FieldInfo ^,Object^>(
							GenerateClassConstant(class_builder, constant),
							constant->GetValue()
							));*/
					GenerateClassConstant(class_builder, constant);
				}

				// generate static constructor to initialize <typeDesc>
				ctor_builder = class_builder->DefineTypeInitializer();
				ILEmitter ^il = gcnew ILEmitter(ctor_builder);
				il->Emit(OpCodes::Ldtoken, class_builder);
				il->Emit(OpCodes::Call, Methods::PhpTypeDesc_Create);
				il->Emit(OpCodes::Stsfld, type_desc_field);
				il->Emit(OpCodes::Ret);

				// ADD INITIALIZATION OF CONSTANTS ?

				// generate methods
				constructorDefined = false;

				scg::List<PhpObjectBuilder::InfoWithAttributes<MethodInfo ^>> ^dyn_method_list =
					gcnew scg::List<PhpObjectBuilder::InfoWithAttributes<MethodInfo ^>>();

				enumerator = cls->GetMethodEnumerator();
				while (enumerator->MoveNext() == true)
				{
					String ^method_name = static_cast<String ^>(enumerator->Key);

					FunctionTypeInfo ^func_type_info = nullptr;
					if (class_type_info != nullptr) class_type_info->Methods->TryGetValue(method_name, func_type_info);

					// generate getter of IExternalFunction
					MethodInfo^ externalFunctionGetter = GenerateExternalFunctionGetter(class_builder, class_name, method_name );

					// arg-less overload
					MethodInfo ^argless = GenerateDynamicFunction(class_name, method_name, class_builder, func_type_info, externalFunctionGetter);

					// arg-full overload
					FunctionTypeInfo ^argfull_type_info = (func_type_info == nullptr ? nullptr : func_type_info->ToArgFullSignature());
					GenerateFunction(class_name, method_name, class_builder, argfull_type_info, externalFunctionGetter, true);

					// "exported" overloads
					GenerateFunctionOverloads(class_name, method_name, class_builder, func_type_info, externalFunctionGetter);

					PhpObjectBuilder::InfoWithAttributes<MethodInfo ^> info;
					info.Info = argless;
					info.Attributes = PHP::Core::Reflection::PhpMemberAttributes::Public;

					dyn_method_list->Add(info);
				}

				if (!constructorDefined)
				{
					// create parameter-less constructor
					ConstructorBuilder ^ctor_builder = class_builder->DefineConstructor(
						MethodAttributes::Public | MethodAttributes::HideBySig | MethodAttributes::SpecialName |
						MethodAttributes::RTSpecialName, CallingConventions::Standard, Type::EmptyTypes);

					// emit parameter-less constructor body
					ILEmitter ^il = gcnew ILEmitter(ctor_builder);
					il->Emit(OpCodes::Ldarg_0);
					il->Emit(OpCodes::Call, Methods::ScriptContext::GetCurrentContext);
					il->LdcI4(1);
					il->Emit(OpCodes::Call, parent_type->GetConstructor(Types::ScriptContext_Bool));
					il->Emit(OpCodes::Ret);
				}

				// generate the typedesc filling method
				PhpObjectBuilder::GenerateTypeDescPopulation(
					class_builder,
					dyn_method_list,
					nullptr,
					constant_list);

				class_builder->CreateType();
			}
		}
	}
}
