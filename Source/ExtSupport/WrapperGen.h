//
// ExtSupport - substitute for php4ts.dll/php5ts.dll
//
// WrapperGen.h
// - contains declaration of WrapperGen class
//

#pragma once

#include "stdafx.h"
#include "AssemblyInternals.h"

#include "Module.h"
#include "WrapperTypeDef.h"

using namespace System;
using namespace System::Text;
using namespace System::Collections;
using namespace System::Reflection;
using namespace System::Reflection::Emit;

using namespace PHP::Core;
using namespace PHP::Core::Emit;

/*

  Designed and implemented by Ladislav Prosek.
  
*/

namespace PHP
{
	namespace ExtManager
	{
		/// <summary>
		/// Contains the managed wrapper generating functionality.
		/// </summary>
		private ref class WrapperGen
		{
		public:
			/// <summary>
			/// Creates a new <see cref="WrapperGen"/>.
			/// </summary>
			/// <param name="fileName">The file name of the extension for which the wrapper should be generated.</param>
			/// <param name="internalName">The internal name of the extension for which the wrapper should be generated.</param>
			/// <param name="constants">A dictionary of constants defined by the extension.</param>
			/// <param name="functions">A dictionary of functions defined by the extension.</param>
			/// <param name="classes">A dictionary of classes defined by the extension.</param>
			WrapperGen(String ^fileName, String ^internalName, IDictionary ^constants,
				IDictionary ^functions, OrderedHashtable<String ^> ^classes)
			{
				this->fileName = fileName;
				this->internalName = internalName;
				this->constants = constants;
				this->functions = functions;
				this->classes = classes;
			}

			/// <summary>
			/// Generates the wrapper assembly.
			/// </summary>
			String ^GenerateManagedWrapper();

			#pragma region WrapperGen implementation

		private:

			/// <summary>
			/// Emits the most efficient form of the <c>Ldarg</c> IL instruction given the constant.
			/// </summary>
			/// <param name="il">The IL generator to emit the instruction to.</param>
			/// <param name="i">The constant.</param>
			/// <remarks>
			/// <paramref name="i"/> is treated as 0-based parameter number (not counting arg0) regardless of
			/// whether <parameref name="il"/> emits to a static or instance method.
			/// </remarks>
			void EmitLdarg(ILEmitter ^il, int i);

			/// <summary>
			/// Emits the most efficient form of the <c>Ldarga</c> IL instruction given the constant.
			/// </summary>
			/// <param name="il">The IL generator to emit the instruction to.</param>
			/// <param name="i">The constant.</param>
			/// <remarks>
			/// <paramref name="i"/> is treated as 0-based parameter number (not counting arg0) regardless of
			/// whether <parameref name="il"/> emits to a static or instance method.
			/// </remarks>
			void EmitLdarga(ILEmitter ^il, int i);

			/// <summary>
			/// Emits IL instruction that cast the reference currently on stack to a reference or
			/// value of a given type.
			/// </summary>
			/// <param name="il">The IL generator to emit the instructions to.</param>
			/// <param name="type">The expected type atop the evaluation stack.</param>
			/// <remarks>If <paramref name="type"/> is a value type, the resulting item atop the stack
			/// will be the unboxed value.</remarks>
			void EmitCast(ILEmitter ^il, Type ^type);

			/// <summary>
			/// Emits IL instruction that convert the reference currently on stack to a reference or
			/// value of a given type (using methods in <c>PHP.Core.Convert</c> class).
			/// </summary>
			/// <param name="il">The IL generator to emit the instructions to.</param>
			/// <param name="type">The expected type atop the evaluation stack.</param>
			/// <remarks>If <paramref name="type"/> is a value type, the resulting item atop the stack
			/// will be the unboxed value. If <paramref name="type"/> is a reference type, the reference
			/// is just cast to that type, calls to <c>ObjectToPhpArray</c> and <c>ObjectToPhpObject</c>
			/// are not emitted.</remarks>
			void EmitConvert(ILEmitter ^il, Type ^type);

			/// <summary>
			/// Emits IL instructions that create an array and fill it with (appropriately boxed) method parameters.
			/// Called from <see cref="GenerateFunction"/>.
			/// </summary>
			/// <param name="il">The IL generator to emit the instructions to.</param>
			/// <param name="methodBuilder">The builder of the method being generated.</param>
			/// <param name="typeInfo">The type information of the method being generated.</param>
			/// <param name="extraParamCount">Number of extra params.</param>
			/// <param name="generatingArgfull"><B>True</B> if generating argfull overload, <B>false</B>
			/// if generating an export stub.</param>
			LocalBuilder ^ EmitParameterHandlingPrologue(ILEmitter ^il, MethodBuilder ^methodBuilder,
					FunctionTypeInfo ^typeInfo, int extraParamCount, bool generatingArgfull);

			/// <summary>
			/// Emits IL instructions that push all parameters on evaluation stack and set their attributes.
			/// Called from <see cref="GenerateFunction"/>.
			/// </summary>
			/// <param name="il">The IL generator to emit the instructions to.</param>
			/// <param name="ctorBuilder">The builder of the constructor being generated.</param>
			/// <param name="typeInfo">The type information of the method called from this constructor.</param>
			void EmitConstructorParameterHandlingPrologue(ILEmitter ^il, ConstructorBuilder ^ctorBuilder, 
					FunctionTypeInfo ^typeInfo);

			/// <summary>
			/// Emits IL instructions that extract (appropriately unboxed byref parameters from the array created by
			/// <see cref="EmitParameterHandlingPrologue"/>). Called from <see cref="GenerateFunction"/>.
			/// </summary>
			/// <param name="il">The IL generator to emit the instructions to.</param>
			/// <param name="methodBuilder">The builder of the method being generated.</param>
			/// <param name="typeInfo">The type information of the method being generated.</param>
			/// <param name="extraParamCount">Number of extra params.</param>
			void EmitParameterHandlingEpilogue(ILEmitter ^il, MethodBuilder ^methodBuilder,
					FunctionTypeInfo ^typeInfo, int extraParamCount, LocalBuilder^ local_args);

			/// <summary>
			/// Emits code that loads reference to a given integer array onto evaluation stack.
			/// </summary>
			/// <param name="il">The IL generator to emit the instructions to.</param>
			/// <param name="functionName">The name of the current method/function (only used to name the
			/// persisted metadata).</param>
			/// <param name="typeBuilder">Current <see cref="TypeBuilder"/>. Note that <paramref name="IL"/>
			/// must be associated with this type builder's method.</param>
			/// <param name="refInfo">The integer array that should be created at run-time.</param>
			LocalBuilder^ EmitRefInfoArrayInit(ILEmitter ^il, String ^functionName, TypeBuilder ^typeBuilder,
				array<int> ^refInfo);

			/// <summary>
			/// Emits a cast of <B>null</B> or <B>-1</B> to <B>false</B>.
			/// </summary>
			/// <param name="il">The <see cref="ILEmitter"/> emitting the method.</param>
			/// <param name="returnType">The method's return type.</param>
			void EmitCastToFalse(ILEmitter ^il, Type ^returnType);

			/// <summary>
			/// Emits code that marshals bound variables.
			/// </summary>
			void EmitBoundVariableMarshal(ILEmitter ^il, bool marshalOut);

			/// <summary>
			/// Generates one exported CLI constructor.
			/// </summary>
			/// <param name="typeBuilder">Builder of the <see cref="System.Type"/> to generate the method to.</param>
			/// <param name="ctorMethod">The constructor method that the constructor should delegate to.</param>
			/// <param name="paramTypes">The constructor parameter types.</param>
			/// <param name="typeInfo">The type information of the method being generated.</param>
			void GenerateConstructor(TypeBuilder ^typeBuilder, MethodInfo ^ctorMethod, array<Type ^> ^paramTypes,
				FunctionTypeInfo ^typeInfo);

			/// <summary>
			/// Generates static cache for IExternalFunction proxy and static method that lazily initializes this cache by calling GetFunctionProxy method.
			/// </summary>
			/// <param name="typeBuilder">Builder of the <see cref="System.Type"/> to generate the method to.</param> 
			/// <param name="className">The name of the declaring PHP class (not name of the wrapping class) or <B>null</B> if this should be a function.</param>
			/// <param name="functionName">The name of the function.</param>
			MethodInfo^ GenerateExternalFunctionGetter(TypeBuilder ^typeBuilder, String ^className, String ^functionName);

			/// <summary>Retrieve Type[] information of specified function used to be emitted.</summary>
			array<Type ^> ^BuildFunctionArguments(FunctionTypeInfo ^typeInfo, bool argfullOverload,
				bool% is_static, int% byref_count, array<int> ^%ref_info, Type^% return_type, int% extra_param_count, String ^% wrapping_function_name);

			/// <summary>
			/// Generates one managed wrapper method (exported stub or argfull overload).
			/// </summary>
			/// <param name="className">The name of the declaring PHP class (not name of the wrapping class) or
			/// <B>null</B> if this should be a function.</param>
			/// <param name="functionName">The name of the function.</param>
			/// <param name="typeBuilder">Builder of the <see cref="System.Type"/> to generate the method to.</param> 
			/// <param name="typeInfo">The type information of the method being generated.</param>
			/// <param name="argfullOverload"><B>True</B> if the argfull overload should be generated.</param>
			void GenerateFunction(String ^className, String ^functionName, TypeBuilder ^typeBuilder,
					FunctionTypeInfo ^typeInfo, MethodInfo ^externalFunctionGetter, bool argfullOverload);

			/// <summary>Create MethodBuilder of specified function. Set custom attributes.</summary>
			MethodBuilder^ DefineFunctionMethod( String ^ className, bool argfullOverload, String ^ functionName, TypeBuilder ^ typeBuilder, bool is_static, String^ wrapping_function_name, Type^ %return_type, array<Type^>^ param_types, FunctionTypeInfo ^ typeInfo );

			/// <summary>
			/// Handles generating more managed wrapper method overloads when there are some optional parameters
			/// defined in the associated typedef XML file.
			/// </summary>
			/// <param name="className">The name of the declaring PHP class (not name of the wrapping class) or
			/// <B>null</B> if this should be a function.</param>
			/// <param name="functionName">The name of the function.</param>
			/// <param name="typeBuilder">Builder of the <see cref="System.Type"/> to generate the method to.</param> 
			/// <param name="typeInfo">The type information of the method being generated.</param>
			/// <remarks>
			/// Optional parameters are not directly supported by all .NET languages, so they result in more
			/// overloads (more methods with the same name but different set of parameters).
			/// </remarks>
			void GenerateFunctionOverloadsPhase2(String ^className, String ^functionName,
					TypeBuilder ^typeBuilder, FunctionTypeInfo ^typeInfo, MethodInfo ^externalFunctionGetter);

			/// <summary>
			/// Handles generating more managed wrapper method overloads when there are more signatures defined
			/// in the associated typedef XML file.
			/// </summary>
			/// <param name="className">The name of the declaring PHP class (not name of the wrapping class) or
			/// <B>null</B> if this should be a function.</param>
			/// <param name="functionName">The name of the function.</param>
			/// <param name="typeBuilder">Builder of the <see cref="System.Type"/> to generate the method to.</param> 
			/// <param name="typeInfo">Function type information. <seealso cref="ModuleTypeInfo.Functions"/></param>
			void GenerateFunctionOverloads(String ^className, String ^functionName, TypeBuilder ^typeBuilder,
				FunctionTypeInfo ^typeInfo, MethodInfo ^externalFunctionGetter);

			/// <summary>
			/// Generates the <B>ArrayAccess</B> interface implementation that delegates to the given methods.
			/// </summary>
			/// <param name="typeBuilder">Builder of the <see cref="System.Type"/> to generate the implementation to.</param>
			/// <param name="arrayGetter">Name of the array getter method or <B>null</B>.</param>
			/// <param name="arraySetter">Name of the array setter method or <B>null</B>.</param>
			/// <remarks>
			/// At least one of <paramref name="arrayGetter"/>, <paramref name="arraySetter"/> should be non-<B>null</B>.
			/// </remarks>
			void GenerateArrayAccess(TypeBuilder ^typeBuilder, String ^arrayGetter, String ^arraySetter);

			/// <summary>
			/// Generates argfull overload implementing one <B>ArrayAccess</B> method.
			/// <summary>
			/// <param name="typeBuilder">Builder of the <see cref="System.Type"/> to generate the implementation to.</param>
			/// <param name="name">Method name.</param>
			/// <param name="paramCount">Method's parameter count.</param>
			/// <param name="argless">The argless overload that should called by the generated method.</param>
			void GenerateArrayAccessMethod(TypeBuilder ^typeBuilder, String ^name, int paramCount, MethodInfo ^argless);

			/// <summary>
			/// Generates static literal fields (wrapping PHP constants).
			/// </summary>
			/// <param name="typeBuilder">Builder of the <see cref="System.Type"/> to generate the constants to.</param> 
			void GenerateConstants(TypeBuilder ^typeBuilder);

			/// <summary>
			/// Generates static methods (wrapping PHP functions).
			/// </summary>
			/// <param name="typeBuilder">Builder of the <see cref="System.Type"/> to generate arg-full overloads to.</param>
			/// <param name="dynamicTypeBuilder">Builder of the <see cref="System.Type"/> to generate arg-less overloads to.</param>
			/// <param name="typeInfo">Function type information. <seealso cref="ModuleTypeInfo.Functions"/></param>
			void GenerateFunctions(TypeBuilder ^typeBuilder, TypeBuilder ^dynamicTypeBuilder,
				scg::Dictionary<String ^, FunctionTypeInfo ^> ^typeInfo);

			/// <summary>
			/// Generates the 'arg-less' method overload - the one taking just a <see cref="PhpStack"> parameter
			/// (expecting the real parameters on this stack) and returning <see cref="Object"/>.
			/// </summary>
			/// <param name="className">The name of the declaring PHP class (not name of the wrapping class) or <B>null</B>
			/// when wrapping a PHP function.</param>
			/// <param name="methodName">The name of the method/function.</param>
			/// <param name="typeBuilder">Builder of the <see cref="System.Type"/> to generate the method to.</param>
			/// <param name="typeInfo">Function type information. <seealso cref="ModuleTypeInfo.Functions"/></param>
			/// <returns>The <see cref="MethodInfo/> of the generated method.</returns>
			MethodInfo ^GenerateDynamicFunction(String ^className, String ^functionName, TypeBuilder ^typeBuilder,
				FunctionTypeInfo ^typeInfo, MethodInfo ^externalFunctionGetter);

			/// <summary>
			/// Generates a class constant to the provided type builder.
			/// </summary>
			/// <param name="classBuilder">The builder to generate the constant to.</param>
			/// <param name="constant">The constant.</param>
			FieldInfo ^GenerateClassConstant(TypeBuilder ^classBuilder, PHP::ExtManager::Constant ^constant);

			/// <summary>
			/// Generates classes (wrapping PHP classes).
			/// </summary>
			/// <param name="moduleBuilder">Builder of the <see cref="System.Reflection.Module"/> to generate the
			/// classes to.</param> 
			/// <param name="typeInfo">Class type information. <seealso cref="ModuleTypeInfo.Classes"/></param>
			void GenerateClasses(ModuleBuilder ^moduleBuilder, scg::Dictionary<String ^, ClassTypeInfo ^> ^typeInfo);

			/// <summary>
			/// The file name of the extension for which this <see cref="WrapperGen"/> is generating the wrapper.
			/// </summary>
			String ^fileName;

			/// <summary>
			/// The internal name of the extension for which this <see cref="WrapperGen"/> is generating the wrapper.
			/// </summary>
			String ^internalName;

			/// <summary>
			/// Dictionary of constants defined by the associated extension.
			/// </summary>
			IDictionary ^constants;

			/// <summary>
			/// Dictionary of functions defined by the associated extension.
			/// </summary>
			IDictionary ^functions;

			/// <summary>
			/// Dictionary of classes defined by the associated extension.
			/// </summary>
			OrderedHashtable<String ^> ^classes;

			/// <summary>
			/// Overload counter. It is used when something with unique name (for example a field) needs to be generated.
			/// </summary>
			int overloadID;

			/// <summary>
			/// <B>true</B> if at least one &quotC#-callable&quot; constructor has been generated, <B>false</B>
			/// otherwise.
			/// </summary>
			bool constructorDefined;

			/// <summary>
			/// An error/notice message to be returned from <see cref="GenerateManagedWrapper"/>.
			/// </summary>
			StringBuilder ^message;

			// Static members

			/// <summary>
			/// Contents of the .snk file that will be used to sign generated assemblies.
			/// </summary>
			static array<unsigned char> ^keyPair = {
				0x07, 0x02, 0x00, 0x00, 0x00, 0x24, 0x00, 0x00, 0x52, 0x53, 0x41, 0x32, 0x00, 0x04, 0x00, 0x00,
				0x01, 0x00, 0x01, 0x00, 0x03, 0x3B, 0x37, 0x1F, 0xE1, 0x44, 0x58, 0x64, 0xD3, 0xCA, 0xE9, 0x68,
				0xEA, 0x2B, 0x8D, 0x31, 0x8B, 0x7B, 0x92, 0xC8, 0x64, 0xB6, 0x92, 0x67, 0xB0, 0x74, 0x16, 0x3B,
				0xF7, 0x4D, 0xF5, 0xC7, 0x52, 0xE7, 0xE0, 0xB8, 0x5C, 0x0F, 0x12, 0xD1, 0x83, 0x4C, 0x7D, 0xE3,
				0xD2, 0x69, 0xB0, 0x92, 0xED, 0xE4, 0xD5, 0x20, 0xFE, 0x8D, 0x4F, 0x59, 0x48, 0x27, 0x14, 0xEB,
				0xE9, 0x54, 0x43, 0x6F, 0xE6, 0x67, 0x11, 0x5E, 0x4A, 0x8C, 0xC2, 0xB1, 0x50, 0x4D, 0x70, 0xA2,
				0x5A, 0x81, 0xD9, 0x20, 0x34, 0x79, 0x8A, 0x99, 0x5F, 0xA4, 0x88, 0x2D, 0x25, 0x98, 0x3C, 0x4D,
				0x73, 0x25, 0xF9, 0x57, 0x2C, 0x4D, 0xF2, 0x9B, 0xF6, 0x56, 0x3D, 0xF9, 0xAB, 0x3D, 0x70, 0x20,
				0x16, 0x6A, 0x15, 0x88, 0x76, 0x15, 0x81, 0xDA, 0xEA, 0x07, 0x06, 0x5C, 0x43, 0x89, 0xAD, 0x8F,
				0xB3, 0xCB, 0xA7, 0xBD, 0x17, 0xEC, 0x3F, 0x8A, 0x06, 0x49, 0x73, 0x9A, 0x08, 0x38, 0x40, 0xA7,
				0xD6, 0x69, 0x1D, 0x32, 0xF7, 0xBC, 0x6C, 0xF6, 0x20, 0xF8, 0x8C, 0xE5, 0xFD, 0x57, 0xB7, 0x87,
				0x82, 0xC0, 0x57, 0xA9, 0xE7, 0x4C, 0xA4, 0x01, 0x8D, 0xDD, 0xFD, 0xC6, 0xB5, 0x0C, 0xF4, 0x4D,
				0xCB, 0x9B, 0x03, 0x1A, 0xEC, 0x73, 0xFB, 0xAC, 0x6B, 0x04, 0xFC, 0x32, 0x64, 0xB6, 0x6B, 0xC7,
				0xFA, 0xEE, 0x33, 0xFC, 0xF5, 0x9F, 0xD8, 0xFF, 0xF1, 0xF2, 0x65, 0xE0, 0x4E, 0x73, 0xB1, 0x60,
				0x94, 0x54, 0xAA, 0xDA, 0x7E, 0x38, 0xE4, 0x9B, 0x41, 0x7A, 0x6A, 0x73, 0x07, 0xDC, 0x93, 0x18,
				0x0F, 0x70, 0x9F, 0x3D, 0x3C, 0x62, 0xAD, 0x8E, 0x24, 0x4D, 0x1E, 0x1E, 0x37, 0x67, 0xA1, 0x66,
				0xD8, 0x78, 0xBF, 0x63, 0x4F, 0xAE, 0xE8, 0x3C, 0x7C, 0xEA, 0x14, 0x06, 0x14, 0x96, 0xAF, 0xA2,
				0x13, 0xC9, 0x82, 0xC0, 0x81, 0x5D, 0x35, 0x28, 0xB6, 0x78, 0xC3, 0x95, 0x06, 0xDC, 0xEB, 0x10,
				0xFB, 0x12, 0xF2, 0x9E, 0xB1, 0x14, 0xED, 0x97, 0x60, 0x6F, 0xCE, 0xA3, 0x07, 0x46, 0x2D, 0x31,
				0x6F, 0x3D, 0xC4, 0x57, 0x77, 0x55, 0x43, 0xCB, 0xD5, 0x4C, 0xDE, 0x58, 0x2E, 0x8F, 0x62, 0xC4,
				0xA2, 0x02, 0x99, 0x5A, 0xD7, 0x5D, 0xCB, 0x88, 0x51, 0x17, 0xF7, 0x99, 0x5F, 0xA9, 0x99, 0xBB,
				0xEB, 0x51, 0x49, 0x82, 0x8D, 0x98, 0xFA, 0x00, 0x47, 0x83, 0xF6, 0xB7, 0x26, 0x8C, 0x18, 0xF6,
				0x46, 0xF0, 0xF1, 0x19, 0x2D, 0xA1, 0x39, 0x83, 0x3D, 0xEC, 0x18, 0x09, 0x99, 0xF3, 0xFE, 0x84,
				0x0F, 0xCC, 0xFB, 0x4C, 0xB0, 0x89, 0xB2, 0x42, 0x98, 0x13, 0x55, 0x19, 0x50, 0x3B, 0x30, 0xF3,
				0x49, 0xBB, 0x65, 0x0E, 0x15, 0x04, 0xE8, 0x86, 0x4A, 0xEF, 0xF9, 0x60, 0x5C, 0x17, 0x98, 0xD2,
				0x70, 0x33, 0xA0, 0x3C, 0x8B, 0xD6, 0xAE, 0x15, 0xE8, 0xFD, 0x1A, 0xEE, 0x7B, 0x6C, 0xA6, 0x46,
				0x3E, 0x45, 0x1A, 0xE5, 0x3A, 0x35, 0xAB, 0x0E, 0x7C, 0x03, 0x4A, 0xC8, 0x6B, 0x89, 0xB5, 0x39,
				0x71, 0x65, 0x58, 0xF4, 0x23, 0xB8, 0xDD, 0x6D, 0x36, 0xE4, 0xF4, 0x61, 0xE7, 0x39, 0x47, 0xF1,
				0x6D, 0xD1, 0xCF, 0x3C, 0x35, 0xD1, 0x05, 0xBE, 0xB9, 0x29, 0x1F, 0x92, 0xE6, 0x81, 0xF2, 0x3A,
				0x10, 0xE0, 0xCA, 0x37, 0xD1, 0x0A, 0x2B, 0x88, 0xB4, 0x03, 0x9A, 0x3F, 0x49, 0x75, 0x40, 0x82,
				0x15, 0x5B, 0x6E, 0x89, 0x48, 0x57, 0x34, 0xAF, 0xD7, 0x75, 0xFB, 0xB1, 0xFC, 0xBE, 0xE2, 0x4B,
				0x6C, 0x19, 0x75, 0x01, 0x0E, 0x63, 0xB7, 0xD9, 0xD9, 0xEF, 0x88, 0xDC, 0xAA, 0xAD, 0xDC, 0xFD,
				0x71, 0x76, 0x72, 0xF9, 0xF4, 0x6C, 0x0D, 0xA9, 0xFA, 0xB3, 0x63, 0x7D, 0x3A, 0x2A, 0xA0, 0x91,
				0x1A, 0xA5, 0x4A, 0x84, 0xAA, 0x4A, 0x00, 0x90, 0xA6, 0xEE, 0xE0, 0x92, 0x17, 0x77, 0x50, 0xAB,
				0x49, 0x51, 0xA7, 0x71, 0xD2, 0x77, 0xF4, 0x2E, 0xEF, 0x5C, 0x0A, 0x37, 0x40, 0x34, 0x29, 0x5F,
				0xF0, 0x86, 0xA2, 0x60, 0xEC, 0xD2, 0x46, 0x04, 0x55, 0xEE, 0x79, 0x31, 0xE6, 0x92, 0x25, 0xC3,
				0x5D, 0xFB, 0xF1, 0xB2, 0x34, 0xE6, 0x2F, 0xEF, 0x14, 0xC4, 0x7D, 0x07, 0xD3, 0x9B, 0xC4, 0xDD,
				0x5C, 0xA7, 0x5E, 0x04 };

		internal:
			static Version ^wrapperVersion = gcnew Version(3, 0, 0, 0);

			#pragma endregion
		};
	}
}
