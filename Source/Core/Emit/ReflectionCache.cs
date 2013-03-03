/*

 Copyright (c) 2004-2006 Tomas Matousek and Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Serialization = System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using PHP.Core;
using PHP.Core.Reflection;
using Core = PHP.Core;

namespace PHP.Core.Emit
{
	/// <exclude/>
	public static class Types
	{
		// singles:
        public static Type Void { get { return typeof(void); } }

		public static readonly Type IEnumerableOfObject = typeof(IEnumerable<>).MakeGenericType(typeof(object));
        public static Type RoutineDelegate { get { return typeof(PHP.Core.RoutineDelegate); } }
        public static Type DebuggerHiddenAttribute { get { return typeof(System.Diagnostics.DebuggerHiddenAttribute); } }

        public static Type ImplementsTypeAttribute { get { return typeof(ImplementsTypeAttribute); } }
        public static Type ImplementsMethodAttribute { get { return typeof(ImplementsMethodAttribute); } }
        public static Type ImplementsFunctionAttribute { get { return typeof(ImplementsFunctionAttribute); } }

		// singletons:
		public static readonly Type[] Bool = new Type[] { typeof(bool) };
		public static readonly Type[] Double = new Type[] { typeof(double) };
		public static readonly Type[] Int = new Type[] { typeof(int) };
		public static readonly Type[] LongInt = new Type[] { typeof(long) };
		public static readonly Type[] Object = new Type[] { typeof(object) };
		public static readonly Type[] String = new Type[] { typeof(string) };
		public static readonly Type[] ObjectRef = new Type[] { typeof(object).MakeByRefType() };
		public static readonly Type[] ScriptContext = new Type[] { typeof(PHP.Core.ScriptContext) };
		public static readonly Type[] PhpReference = new Type[] { typeof(PHP.Core.PhpReference) };
        public static readonly Type[] PhpSmartReference = new Type[] { typeof(PHP.Core.PhpSmartReference) };
        public static readonly Type[] PhpRuntimeChain = new Type[] { typeof(PHP.Core.PhpRuntimeChain) };
		public static readonly Type[] ObjectArray = new Type[] { typeof(object[]) };
		public static readonly Type[] StringArray = new Type[] { typeof(string[]) };
		public static readonly Type[] PhpReferenceArray = new Type[] { typeof(PhpReference[]) };
		public static readonly Type[] IPhpEnumerable = new Type[] { typeof(PHP.Core.IPhpEnumerable) };
		public static readonly Type[] PhpStack = new Type[] { typeof(PHP.Core.PhpStack) };
		public static readonly Type[] PhpArray = new Type[] { typeof(PHP.Core.PhpArray) };
        public static readonly Type[] PhpBytes = new Type[] { typeof(PHP.Core.PhpBytes) };
		public static readonly Type[] PhpObject = new Type[] { typeof(PHP.Core.PhpObject) };
		public static readonly Type[] DObject = new Type[] { typeof(PHP.Core.Reflection.DObject) };
		public static readonly Type[] RoutineProperties = new Type[] { typeof(PHP.Core.Reflection.RoutineProperties) };
		public static readonly Type[] DTypeDesc = new Type[] { typeof(PHP.Core.Reflection.DTypeDesc) };
		public static readonly Type[] PhpTypeDesc = new Type[] { typeof(PHP.Core.Reflection.PhpTypeDesc) };
        public static readonly Type[] CallSiteBinder = new Type[] { typeof(CallSiteBinder) };
        public static readonly Type[] CallSite = new Type[] { typeof(CallSite) };
        public static readonly Type[] CallSiteGeneric = new Type[] { typeof(CallSite<>) };
        public static readonly Type[] Action = new Type[] { typeof(Action) };

        public static readonly Type[] Float = new Type[] { typeof(float) };

		// doublets:
		public static readonly Type[] Int_Int = new Type[] { typeof(int), typeof(int) };
		public static readonly Type[] Int_Object = new Type[] { typeof(int), typeof(object) };
        public static readonly Type[] Object_Int = new Type[] { typeof(object), typeof(int) };
		public static readonly Type[] Object_Object = new Type[] { typeof(object), typeof(object) };
		public static readonly Type[] String_Bool = new Type[] { typeof(string), typeof(bool) };
		public static readonly Type[] String_Object = new Type[] { typeof(string), typeof(object) };
		public static readonly Type[] ObjectRef_Object = new Type[] { ObjectRef[0], typeof(object) };
		public static readonly Type[] Object_ObjectRef = new Type[] { typeof(object), ObjectRef[0] };
		public static readonly Type[] Object_PhpStack = new Type[] { typeof(object), typeof(PhpStack) };

		public static readonly Type[] PhpError_String = new Type[] { typeof(PhpError), typeof(string) };
		public static readonly Type[] Object_ScriptContext = new Type[] { typeof(object), typeof(PHP.Core.ScriptContext) };
		public static readonly Type[] ScriptContext_IDictionary = new Type[] { typeof(PHP.Core.ScriptContext), typeof(IDictionary) };
		public static readonly Type[] ScriptContext_Bool = new Type[] { typeof(PHP.Core.ScriptContext), typeof(bool) };

        public static readonly Type[] Object_DTypeDesc = new Type[] { typeof(object), typeof(PHP.Core.Reflection.DTypeDesc) };
		public static readonly Type[] ScriptContext_DTypeDesc = new Type[] { typeof(PHP.Core.ScriptContext), typeof(PHP.Core.Reflection.DTypeDesc) };
		public static readonly Type[] DelegateCtorArgs = new Type[] { typeof(object), typeof(IntPtr) };

		// triplets:
		public static readonly Type[] String_Object_Bool = new Type[] { typeof(string), typeof(object), typeof(bool) };
		public static readonly Type[] String_Bool_Bool = new Type[] { typeof(string), typeof(bool), typeof(bool) };
		public static readonly Type[] Object_Object_Bool = new Type[] { typeof(object), typeof(object), typeof(bool) };
		public static readonly Type[] PhpArray_Object_Object = new Type[] { typeof(PhpArray), typeof(object), typeof(object) };
		public static readonly Type[] Object_Object_ObjectRef = new Type[] { typeof(object), typeof(object), ObjectRef[0] };
		public static readonly Type[] Object_Object_Object = new Type[] { typeof(object), typeof(object), typeof(object) };

		// quadruplets:
		public static readonly Type[] LinqContextArgs = new Type[] { typeof(Core.Reflection.DObject), typeof(Dictionary<string, object>), 
			typeof(PHP.Core.ScriptContext), typeof(PHP.Core.Reflection.DTypeDesc) };

		// CLR only
#if !SILVERLIGHT
		// singles:
        public static Type EditorBrowsableAttribute { get { return typeof(System.ComponentModel.EditorBrowsableAttribute); } }

		// doublets:
		public static readonly Type[] SerializationInfo_StreamingContext = new Type[] { typeof(System.Runtime.Serialization.SerializationInfo), typeof(System.Runtime.Serialization.StreamingContext) };
#endif
	}

	// pattern:
	// {[a-zA-Z0-9]+}
	// public static MethodInfo \1 { get { if (_\1==null) _\1 = _this.GetMethod("\1"); return _\1; } }
	/// <exclude/>
    public static class Methods
    {
        #region Operators

        // automatically generated by MethodsGenerator:
        public struct Operators
        {
            static Type _this { get { return typeof(PHP.Core.Operators); } }
            static MethodInfo _Minus, _Plus, _Increment, _Decrement, _BitOperation, _BitNot, _ShiftLeft, _ShiftRight,
                _UnsetItem, _GetVariableUnchecked, _GetVariable, _GetVariableRef, _SetVariable, _UnsetVariable, _SetVariableRef, _IsEmptyForEnsure,
                _EnsureVariableIsArray, _EnsureVariableIsObject, _EnsurePropertyIsObject, _EnsurePropertyIsArray, _EnsureStaticPropertyIsArray,
                _EnsureStaticPropertyIsObject, _GetProperty, _GetObjectProperty, _GetPropertyRef, _GetObjectPropertyRef, _SetProperty,
                _SetObjectProperty, _UnsetProperty, _InvokeMethodObj, _InvokeMethodStr, _Clone, _GetClassConstant, _GetStaticProperty, _GetStaticPropertyRef,
                _SetStaticProperty, _UnsetStaticProperty, _InvokeStaticMethod, _New, _NewClr, _InstanceOf, _TypeOf, _StrictEquality, _Throw,
                _SetObjectFieldDirect, _SetObjectFieldDirectRef, _GetObjectFieldDirectRef, _GetObjectFieldDirect, _ToAbsoluteSourcePath,
                _GetItemExact, _SetItemExact,

                _IsCallable,
                _Select, _Where;

            public struct Add
            {
                static MethodInfo _Object_Object, _Object_Int32, _Object_Double, _Double_Object;

                public static MethodInfo Object_Object { get { if (_Object_Object == null) _Object_Object = new Func<object,object,object>(PHP.Core.Operators.Add).Method; return _Object_Object; } }
                public static MethodInfo Object_Int32 { get { if (_Object_Int32 == null) _Object_Int32 = new Func<object, int, object>(PHP.Core.Operators.Add).Method; return _Object_Int32; } }
                public static MethodInfo Object_Double { get { if (_Object_Double == null) _Object_Double = new Func<object, double, double>(PHP.Core.Operators.Add).Method; return _Object_Double; } }
                public static MethodInfo Double_Object { get { return _Double_Object ?? (_Double_Object = new Func<double, object, double>(PHP.Core.Operators.Add).Method); } }
            }

            public struct Subtract
            {
                static MethodInfo _Object_Object, _Object_Int, _Int32_Object, _Double_Object;

                public static MethodInfo Object_Object { get { if (_Object_Object == null) _Object_Object = new Func<object, object, object>(PHP.Core.Operators.Subtract).Method; return _Object_Object; } }
                public static MethodInfo Object_Int { get { return _Object_Int ?? (_Object_Int = new Func<object, int, object>(PHP.Core.Operators.Subtract).Method); } }
                public static MethodInfo Int32_Object { get { if (_Int32_Object == null) _Int32_Object = new Func<int, object, object>(PHP.Core.Operators.Subtract).Method; return _Int32_Object; } }
                public static MethodInfo Double_Object { get { if (_Double_Object == null) _Double_Object = new Func<double, object, double>(PHP.Core.Operators.Subtract).Method; return _Double_Object; } }
            }

            public static MethodInfo Minus { get { if (_Minus == null) _Minus = new Func<object, object>(PHP.Core.Operators.Minus).Method; return _Minus; } }
            public static MethodInfo Plus { get { if (_Plus == null) _Plus = new Func<object, object>(PHP.Core.Operators.Plus).Method; return _Plus; } }
            public struct Divide
            {
                static MethodInfo _Object_Object, _Object_Int32, _Object_Double, _Int32_Object, _Double_Object;

                public static MethodInfo Object_Object { get { if (_Object_Object == null) _Object_Object = new Func<object, object, object>(PHP.Core.Operators.Divide).Method; return _Object_Object; } }
                public static MethodInfo Object_Int32 { get { if (_Object_Int32 == null) _Object_Int32 = new Func<object, int, object>(PHP.Core.Operators.Divide).Method; return _Object_Int32; } }
                public static MethodInfo Object_Double { get { if (_Object_Double == null) _Object_Double = new Func<object, double, double>(PHP.Core.Operators.Divide).Method; return _Object_Double; } }
                public static MethodInfo Int32_Object { get { if (_Int32_Object == null) _Int32_Object = new Func<int, object, object>(PHP.Core.Operators.Divide).Method; return _Int32_Object; } }
                public static MethodInfo Double_Object { get { if (_Double_Object == null) _Double_Object = new Func<double, object, object>(PHP.Core.Operators.Divide).Method; return _Double_Object; } }
            }

            public struct Multiply
            {
                static MethodInfo _Object_Object, _Object_Int32, _Object_Double, _Double_Object;

                public static MethodInfo Object_Object { get { if (_Object_Object == null) _Object_Object = new Func<object, object, object>(PHP.Core.Operators.Multiply).Method; return _Object_Object; } }
                public static MethodInfo Object_Int32 { get { if (_Object_Int32 == null) _Object_Int32 = new Func<object, int, object>(PHP.Core.Operators.Multiply).Method; return _Object_Int32; } }
                public static MethodInfo Object_Double { get { if (_Object_Double == null) _Object_Double = new Func<object, double, double>(PHP.Core.Operators.Multiply).Method; return _Object_Double; } }
                public static MethodInfo Double_Object { get { return _Double_Object ?? (_Double_Object = new Func<double, object, double>(PHP.Core.Operators.Multiply).Method); } }
            }

            public struct Remainder
            {
                static MethodInfo _Object_Object, _Object_Int32;

                public static MethodInfo Object_Object { get { if (_Object_Object == null) _Object_Object = new Func<object, object, object>(PHP.Core.Operators.Remainder).Method; return _Object_Object; } }
                public static MethodInfo Object_Int32 { get { if (_Object_Int32 == null) _Object_Int32 = new Func<object, int, object>(PHP.Core.Operators.Remainder).Method; return _Object_Int32; } }
            }

            public static MethodInfo Increment { get { if (_Increment == null) _Increment = new Func<object, object>(PHP.Core.Operators.Increment).Method; return _Increment; } }
            public static MethodInfo Decrement { get { if (_Decrement == null) _Decrement = new Func<object, object>(PHP.Core.Operators.Decrement).Method; return _Decrement; } }
            public static MethodInfo BitOperation { get { if (_BitOperation == null) _BitOperation = new Func<object, object, PHP.Core.Operators.BitOp, object>(PHP.Core.Operators.BitOperation).Method; return _BitOperation; } }
            public static MethodInfo BitNot { get { if (_BitNot == null) _BitNot = new Func<object, object>(PHP.Core.Operators.BitNot).Method; return _BitNot; } }
            public static MethodInfo ShiftLeft { get { if (_ShiftLeft == null) _ShiftLeft = new Func<object, object, object>(PHP.Core.Operators.ShiftLeft).Method; return _ShiftLeft; } }
            public static MethodInfo ShiftRight { get { if (_ShiftRight == null) _ShiftRight = new Func<object, object, object>(PHP.Core.Operators.ShiftRight).Method; return _ShiftRight; } }
            public struct Concat
            {
                static MethodInfo _Object_Object, _Object_String, _String_Object, _ObjectArray;

                public static MethodInfo Object_Object { get { if (_Object_Object == null) _Object_Object = new Func<object, object, object>(PHP.Core.Operators.Concat).Method; return _Object_Object; } }
                public static MethodInfo Object_String { get { if (_Object_String == null) _Object_String = new Func<object, string, object>(PHP.Core.Operators.Concat).Method; return _Object_String; } }
                public static MethodInfo String_Object { get { if (_String_Object == null) _String_Object = new Func<string, object, object>(PHP.Core.Operators.Concat).Method; return _String_Object; } }
                public static MethodInfo ObjectArray { get { if (_ObjectArray == null) _ObjectArray = new Func<object[], object>(PHP.Core.Operators.Concat).Method; return _ObjectArray; } }
            }

            public struct Append
            {
                static MethodInfo _Object_Object, _Object_String, _Object_ObjectArray;

                public static MethodInfo Object_Object { get { if (_Object_Object == null) _Object_Object = new Func<object, object, object>(PHP.Core.Operators.Append).Method; return _Object_Object; } }
                public static MethodInfo Object_String { get { if (_Object_String == null) _Object_String = new Func<object, string, object>(PHP.Core.Operators.Append).Method; return _Object_String; } }
                public static MethodInfo Object_ObjectArray { get { if (_Object_ObjectArray == null) _Object_ObjectArray = new Func<object, object[], object>(PHP.Core.Operators.Append).Method; return _Object_ObjectArray; } }
            }

            public struct Prepend
            {
                static MethodInfo _Object_Object, _Object_String, _Object_ObjectArray;

                public static MethodInfo Object_Object { get { if (_Object_Object == null) _Object_Object = new Func<object, object, object>(PHP.Core.Operators.Prepend).Method; return _Object_Object; } }
                public static MethodInfo Object_String { get { if (_Object_String == null) _Object_String = new Func<object, string, object>(PHP.Core.Operators.Prepend).Method; return _Object_String; } }
                public static MethodInfo Object_ObjectArray { get { if (_Object_ObjectArray == null) _Object_ObjectArray = new Func<object, object[], object>(PHP.Core.Operators.Prepend).Method; return _Object_ObjectArray; } }
            }

            public struct GetItem
            {
                static MethodInfo _Object, _Int32, _String;

                public static MethodInfo Object { get { if (_Object == null) _Object = new Func<object, object, Core.Operators.GetItemKinds, object>(PHP.Core.Operators.GetItem).Method; return _Object; } }
                public static MethodInfo Int32 { get { if (_Int32 == null) _Int32 = new Func<object, int, Core.Operators.GetItemKinds, object>(PHP.Core.Operators.GetItem).Method; return _Int32; } }
                public static MethodInfo String { get { if (_String == null) _String = new Func<object, string, Core.Operators.GetItemKinds, object>(PHP.Core.Operators.GetItem).Method; return _String; } }
            }

            public static MethodInfo GetItemExact { get { if (_GetItemExact == null) _GetItemExact = new Func<object, string, Core.Operators.GetItemKinds, int, object>(PHP.Core.Operators.GetItemExact).Method; return _GetItemExact; } }

            public struct GetItemRef
            {
                static MethodInfo _Keyless, _Object, _Int32, _String;

                public static MethodInfo Keyless { get { if (_Keyless == null) _Keyless = _this.GetMethod("GetItemRef", new Type[] { Types.ObjectRef[0] }); return _Keyless; } }
                public static MethodInfo Object { get { if (_Object == null) _Object = _this.GetMethod("GetItemRef", new Type[] { Types.Object[0], Types.ObjectRef[0] }); return _Object; } }
                public static MethodInfo Int32 { get { if (_Int32 == null) _Int32 = _this.GetMethod("GetItemRef", new Type[] { Types.Int[0], Types.ObjectRef[0] }); return _Int32; } }
                public static MethodInfo String { get { if (_String == null) _String = _this.GetMethod("GetItemRef", new Type[] { Types.String[0], Types.ObjectRef[0] }); return _String; } }
            }

            public struct SetItem
            {
                static MethodInfo _Keyless, _Object, _Int32, _String;

                public static MethodInfo Keyless { get { if (_Keyless == null) _Keyless = _this.GetMethod("SetItem", new Type[] { Types.Object[0], Types.ObjectRef[0] }); return _Keyless; } }
                public static MethodInfo Object { get { if (_Object == null) _Object = _this.GetMethod("SetItem", new Type[] { Types.Object[0], Types.Object[0], Types.ObjectRef[0] }); return _Object; } }
                public static MethodInfo Int32 { get { if (_Int32 == null) _Int32 = _this.GetMethod("SetItem", new Type[] { Types.Object[0], Types.Int[0], Types.ObjectRef[0] }); return _Int32; } }
                public static MethodInfo String { get { if (_String == null) _String = _this.GetMethod("SetItem", new Type[] { Types.Object[0], Types.String[0], Types.ObjectRef[0] }); return _String; } }
            }

            public static MethodInfo SetItemExact { get { if (_SetItemExact == null) _SetItemExact = _this.GetMethod("SetItemExact", new Type[] { Types.Object[0], Types.String[0], Types.ObjectRef[0], typeof(int) }); return _SetItemExact; } }

            public struct SetItemRef
            {
                static MethodInfo _Object, _Int32, _String;

                public static MethodInfo Object { get { if (_Object == null) _Object = _this.GetMethod("SetItemRef", new Type[] { Types.PhpReference[0], Types.Object[0], Types.ObjectRef[0] }); return _Object; } }
                public static MethodInfo Int32 { get { if (_Int32 == null) _Int32 = _this.GetMethod("SetItemRef", new Type[] { Types.PhpReference[0], Types.Int[0], Types.ObjectRef[0] }); return _Int32; } }
                public static MethodInfo String { get { if (_String == null) _String = _this.GetMethod("SetItemRef", new Type[] { Types.PhpReference[0], Types.String[0], Types.ObjectRef[0] }); return _String; } }
            }

            public static MethodInfo UnsetItem { get { if (_UnsetItem == null) _UnsetItem = _this.GetMethod("UnsetItem"); return _UnsetItem; } }
            public static MethodInfo GetVariableUnchecked { get { if (_GetVariableUnchecked == null) _GetVariableUnchecked = _this.GetMethod("GetVariableUnchecked"); return _GetVariableUnchecked; } }
            public static MethodInfo GetVariable { get { if (_GetVariable == null) _GetVariable = _this.GetMethod("GetVariable"); return _GetVariable; } }
            public static MethodInfo GetVariableRef { get { if (_GetVariableRef == null) _GetVariableRef = _this.GetMethod("GetVariableRef"); return _GetVariableRef; } }
            public static MethodInfo SetVariable { get { if (_SetVariable == null) _SetVariable = _this.GetMethod("SetVariable"); return _SetVariable; } }
            public static MethodInfo SetVariableRef { get { if (_SetVariableRef == null) _SetVariableRef = _this.GetMethod("SetVariableRef"); return _SetVariableRef; } }
            public static MethodInfo UnsetVariable { get { if (_UnsetVariable == null) _UnsetVariable = _this.GetMethod("UnsetVariable"); return _UnsetVariable; } }
            public static MethodInfo IsEmptyForEnsure { get { if (_IsEmptyForEnsure == null) _IsEmptyForEnsure = _this.GetMethod("IsEmptyForEnsure"); return _IsEmptyForEnsure; } }
            public static MethodInfo EnsureVariableIsArray { get { if (_EnsureVariableIsArray == null) _EnsureVariableIsArray = _this.GetMethod("EnsureVariableIsArray"); return _EnsureVariableIsArray; } }
            public static MethodInfo EnsureVariableIsObject { get { if (_EnsureVariableIsObject == null) _EnsureVariableIsObject = _this.GetMethod("EnsureVariableIsObject"); return _EnsureVariableIsObject; } }
            public static MethodInfo EnsurePropertyIsObject { get { if (_EnsurePropertyIsObject == null) _EnsurePropertyIsObject = _this.GetMethod("EnsurePropertyIsObject"); return _EnsurePropertyIsObject; } }
            public static MethodInfo EnsurePropertyIsArray { get { if (_EnsurePropertyIsArray == null) _EnsurePropertyIsArray = _this.GetMethod("EnsurePropertyIsArray"); return _EnsurePropertyIsArray; } }
            public static MethodInfo EnsureStaticPropertyIsArray { get { if (_EnsureStaticPropertyIsArray == null) _EnsureStaticPropertyIsArray = _this.GetMethod("EnsureStaticPropertyIsArray"); return _EnsureStaticPropertyIsArray; } }
            public static MethodInfo EnsureStaticPropertyIsObject { get { if (_EnsureStaticPropertyIsObject == null) _EnsureStaticPropertyIsObject = _this.GetMethod("EnsureStaticPropertyIsObject"); return _EnsureStaticPropertyIsObject; } }
            public static MethodInfo GetProperty { get { if (_GetProperty == null) _GetProperty = _this.GetMethod("GetProperty"); return _GetProperty; } }
            public static MethodInfo GetObjectProperty { get { if (_GetObjectProperty == null) _GetObjectProperty = _this.GetMethod("GetObjectProperty"); return _GetObjectProperty; } }
            public static MethodInfo GetPropertyRef { get { if (_GetPropertyRef == null) _GetPropertyRef = _this.GetMethod("GetPropertyRef"); return _GetPropertyRef; } }
            public static MethodInfo GetObjectPropertyRef { get { if (_GetObjectPropertyRef == null) _GetObjectPropertyRef = _this.GetMethod("GetObjectPropertyRef"); return _GetObjectPropertyRef; } }
            public static MethodInfo SetProperty { get { if (_SetProperty == null) _SetProperty = _this.GetMethod("SetProperty"); return _SetProperty; } }
            public static MethodInfo SetObjectProperty { get { if (_SetObjectProperty == null) _SetObjectProperty = _this.GetMethod("SetObjectProperty"); return _SetObjectProperty; } }
            public static MethodInfo SetObjectFieldDirect { get { if (_SetObjectFieldDirect == null) _SetObjectFieldDirect = _this.GetMethod("SetObjectFieldDirect"); return _SetObjectFieldDirect; } }
            public static MethodInfo SetObjectFieldDirectRef { get { if (_SetObjectFieldDirectRef == null) _SetObjectFieldDirectRef = _this.GetMethod("SetObjectFieldDirectRef"); return _SetObjectFieldDirectRef; } }
            public static MethodInfo GetObjectFieldDirect { get { if (_GetObjectFieldDirect == null) _GetObjectFieldDirect = _this.GetMethod("GetObjectFieldDirect"); return _GetObjectFieldDirect; } }
            public static MethodInfo GetObjectFieldDirectRef { get { if (_GetObjectFieldDirectRef == null) _GetObjectFieldDirectRef = _this.GetMethod("GetObjectFieldDirectRef"); return _GetObjectFieldDirectRef; } }
            public static MethodInfo UnsetProperty { get { if (_UnsetProperty == null) _UnsetProperty = _this.GetMethod("UnsetProperty"); return _UnsetProperty; } }
            public static MethodInfo InvokeMethodObj { get { if (_InvokeMethodObj == null) _InvokeMethodObj = _this.GetMethod("InvokeMethod", new Type[] { Types.Object[0], Types.Object[0], Types.DTypeDesc[0], Types.ScriptContext[0] }); return _InvokeMethodObj; } }
            public static MethodInfo InvokeMethodStr { get { if (_InvokeMethodStr == null) _InvokeMethodStr = _this.GetMethod("InvokeMethod", new Type[] { Types.Object[0], Types.String[0], Types.DTypeDesc[0], Types.ScriptContext[0] }); return _InvokeMethodStr; } }
            public static MethodInfo Clone { get { if (_Clone == null) _Clone = _this.GetMethod("Clone"); return _Clone; } }
            public static MethodInfo GetClassConstant { get { if (_GetClassConstant == null) _GetClassConstant = _this.GetMethod("GetClassConstant"); return _GetClassConstant; } }
            public static MethodInfo GetStaticProperty { get { if (_GetStaticProperty == null) _GetStaticProperty = _this.GetMethod("GetStaticProperty"); return _GetStaticProperty; } }
            public static MethodInfo GetStaticPropertyRef { get { if (_GetStaticPropertyRef == null) _GetStaticPropertyRef = _this.GetMethod("GetStaticPropertyRef"); return _GetStaticPropertyRef; } }
            public static MethodInfo SetStaticProperty { get { if (_SetStaticProperty == null) _SetStaticProperty = _this.GetMethod("SetStaticProperty"); return _SetStaticProperty; } }
            public static MethodInfo UnsetStaticProperty { get { if (_UnsetStaticProperty == null) _UnsetStaticProperty = _this.GetMethod("UnsetStaticProperty"); return _UnsetStaticProperty; } }
            public static MethodInfo InvokeStaticMethod { get { if (_InvokeStaticMethod == null) _InvokeStaticMethod = _this.GetMethod("InvokeStaticMethod"); return _InvokeStaticMethod; } }
            public static MethodInfo New { get { if (_New == null) _New = _this.GetMethod("New"); return _New; } }
            public static MethodInfo NewClr { get { if (_NewClr == null) _NewClr = _this.GetMethod("NewClr"); return _NewClr; } }
            public static MethodInfo InstanceOf { get { if (_InstanceOf == null) _InstanceOf = _this.GetMethod("InstanceOf"); return _InstanceOf; } }
            public static MethodInfo TypeOf { get { if (_TypeOf == null) _TypeOf = _this.GetMethod("TypeOf"); return _TypeOf; } }
            public static MethodInfo StrictEquality { get { if (_StrictEquality == null) _StrictEquality = _this.GetMethod("StrictEquality"); return _StrictEquality; } }
            public static MethodInfo Throw { get { if (_Throw == null) _Throw = _this.GetMethod("Throw"); return _Throw; } }
            public static MethodInfo ToAbsoluteSourcePath { get { if (_ToAbsoluteSourcePath == null) _ToAbsoluteSourcePath = _this.GetMethod("ToAbsoluteSourcePath"); return _ToAbsoluteSourcePath; } }
            public static MethodInfo IsCallable { get { return _IsCallable ?? (_IsCallable = _this.GetMethod("IsCallable")); } }

            // LINQ stuff
            public static MethodInfo Where { get { if (_Where == null) _Where = _this.GetMethod("Where"); return _Where; } }
            public static MethodInfo Select { get { if (_Select == null) _Select = _this.GetMethod("Select"); return _Select; } }

            public static class MakeGenericTypeInstantiation
            {
                private static MethodInfo _1, _2, _3, _4, _N;
                public const int ExplicitOverloads = 4;

                public static MethodInfo Overload(int i)
                {
                    Type d = Types.DTypeDesc[0];
                    switch (i)
                    {
                        case 1: if (_1 == null) _1 = _this.GetMethod("MakeGenericTypeInstantiation", new Type[] { d, d }); return _1;
                        case 2: if (_2 == null) _2 = _this.GetMethod("MakeGenericTypeInstantiation", new Type[] { d, d, d }); return _2;
                        case 3: if (_3 == null) _3 = _this.GetMethod("MakeGenericTypeInstantiation", new Type[] { d, d, d, d }); return _3;
                        case 4: if (_4 == null) _4 = _this.GetMethod("MakeGenericTypeInstantiation", new Type[] { d, d, d, d, d }); return _4;

                        default:
                            Debug.Assert(i > 4);
                            return N;
                    }
                }

                public static MethodInfo N { get { if (_N == null) _N = _this.GetMethod("MakeGenericTypeInstantiation", new Type[] { Types.DTypeDesc[0], typeof(Reflection.DTypeDesc[]) }); return _N; } }
            }
        }

        #endregion

        #region Application Context

        public static class ApplicationContext
        {
            static Type _this { get { return typeof(PHP.Core.ApplicationContext); } }
            static MethodInfo _DeclareType_TypeDesc, _DeclareType_Handle, _DeclareFunction, _DeclareConstant;

            public static MethodInfo DeclareType_TypeDesc { get { if (_DeclareType_TypeDesc == null) _DeclareType_TypeDesc = _this.GetMethod("DeclareType", new Type[] { Types.DTypeDesc[0], Types.String[0] }); return _DeclareType_TypeDesc; } }
            public static MethodInfo DeclareType_Handle { get { if (_DeclareType_Handle == null) _DeclareType_Handle = _this.GetMethod("DeclareType", new Type[] { typeof(RuntimeTypeHandle), Types.String[0] }); return _DeclareType_Handle; } }
            public static MethodInfo DeclareFunction { get { if (_DeclareFunction == null) _DeclareFunction = _this.GetMethod("DeclareFunction"); return _DeclareFunction; } }
            public static MethodInfo DeclareConstant { get { if (_DeclareConstant == null) _DeclareConstant = _this.GetMethod("DeclareConstant"); return _DeclareConstant; } }
        }

        #endregion

        #region ScriptContext

        public static class ScriptContext
        {
            static Type _this { get { return typeof(PHP.Core.ScriptContext); } }

            /*public struct Echo
            {
                static MethodInfo _bool, _double, _int, _longInt, _object, _string, _PhpBytes, _ObjectArray;

                public static MethodInfo Bool { get { if (_bool == null)   _bool = _this.GetMethod("Echo", Types.Bool); return _bool; } }
                public static MethodInfo Double { get { if (_double == null) _double = _this.GetMethod("Echo", Types.Double); return _double; } }
                public static MethodInfo Int { get { if (_int == null)    _int = _this.GetMethod("Echo", Types.Int); return _int; } }
                public static MethodInfo LongInt { get { if (_longInt == null)_longInt = _this.GetMethod("Echo", Types.LongInt); return _longInt; } }
                public static MethodInfo Object { get { if (_object == null) _object = _this.GetMethod("Echo", Types.Object); return _object; } }
                public static MethodInfo String { get { if (_string == null) _string = _this.GetMethod("Echo", Types.String); return _string; } }
                public static MethodInfo PhpBytes { get { if (_PhpBytes == null) _PhpBytes = _this.GetMethod("Echo", Types.PhpBytes); return _PhpBytes; } }
                public static MethodInfo ObjectArray { get { if (_ObjectArray == null) _ObjectArray = _this.GetMethod("Echo", Types.ObjectArray); return _ObjectArray; } }
            }*/

            /// <summary>
            /// Same as Echo.*, but methods are static and the ScriptContext argument is the second one.
            /// </summary>
            public struct EchoStatic
            {
                static MethodInfo _bool, _double, _int, _longInt, _object, _string, _phpBytes;

                public static MethodInfo Bool { get { return _bool ?? (_bool = new Action<bool, PHP.Core.ScriptContext>(PHP.Core.ScriptContext.Echo).Method); } }
                public static MethodInfo Double { get { return _double ?? (_double = new Action<double, PHP.Core.ScriptContext>(PHP.Core.ScriptContext.Echo).Method); } }
                public static MethodInfo Int { get { return _int ?? (_int = new Action<int, PHP.Core.ScriptContext>(PHP.Core.ScriptContext.Echo).Method); } }
                public static MethodInfo LongInt { get { return _longInt ?? (_longInt = new Action<long, PHP.Core.ScriptContext>(PHP.Core.ScriptContext.Echo).Method); } }
                public static MethodInfo Object { get { return _object ?? (_object = new Action<object, PHP.Core.ScriptContext>(PHP.Core.ScriptContext.Echo).Method); } }
                public static MethodInfo String { get { return _string?? (_string = new Action<string, PHP.Core.ScriptContext>(PHP.Core.ScriptContext.Echo).Method); } }
                public static MethodInfo PhpBytes { get { return _phpBytes ?? (_phpBytes = new Action<PHP.Core.PhpBytes, PHP.Core.ScriptContext>(PHP.Core.ScriptContext.Echo).Method); } }
            }

            static MethodInfo
                _DisableErrorReporting, _EnableErrorReporting,
                _GetCurrentContext, _Die, _StaticInclude, _DynamicInclude, _GetStaticLocal, _GetStaticLocalId, _AddStaticLocal, _RunApplication,
                _IsConstantDefined, _GetConstantValue, _DeclareConstant, _RegisterDObjectForFinalization,
                _DeclareFunction, _DeclareLambda, _Call, _CallValue, _CallVoid, _DeclareType_TypeDesc, _DeclareType_Handle, _DeclareIncompleteTypeHelper, _IncompleteTypeDeclared,
                _GetWorkingDirectory;

            public static MethodInfo RunApplication { get { if (_RunApplication == null) _RunApplication = _this.GetMethod("RunApplication", new Type[] { typeof(Delegate), typeof(string), typeof(string) }); return _RunApplication; } }
            public static MethodInfo GetCurrentContext { get { if (_GetCurrentContext == null) _GetCurrentContext = _this.GetMethod("get_CurrentContext"); return _GetCurrentContext; } }
            public static MethodInfo GetWorkingDirectory { get { if (_GetWorkingDirectory == null) _GetWorkingDirectory = _this.GetMethod("get_WorkingDirectory"); return _GetWorkingDirectory; } }
            public static MethodInfo Die { get { if (_Die == null) _Die = _this.GetMethod("Die"); return _Die; } }

            public static MethodInfo StaticInclude { get { return _StaticInclude ?? (_StaticInclude = _this.GetMethod("StaticInclude")); } }
            public static MethodInfo DynamicInclude { get { if (_DynamicInclude == null) _DynamicInclude = _this.GetMethod("DynamicInclude"); return _DynamicInclude; } }

            public static MethodInfo DisableErrorReporting { get { if (_DisableErrorReporting == null) _DisableErrorReporting = _this.GetMethod("DisableErrorReporting"); return _DisableErrorReporting; } }
            public static MethodInfo EnableErrorReporting { get { if (_EnableErrorReporting == null) _EnableErrorReporting = _this.GetMethod("EnableErrorReporting"); return _EnableErrorReporting; } }

            public static MethodInfo GetStaticLocal { get { if (_GetStaticLocal == null) _GetStaticLocal = _this.GetMethod("GetStaticLocal"); return _GetStaticLocal; } }
            public static MethodInfo GetStaticLocalId { get { return _GetStaticLocalId ?? (_GetStaticLocalId = _this.GetMethod("GetStaticLocalId")); } }
            public static MethodInfo AddStaticLocal { get { if (_AddStaticLocal == null) _AddStaticLocal = _this.GetMethod("AddStaticLocal"); return _AddStaticLocal; } }
            
            public static MethodInfo GetConstantValue { get { if (_GetConstantValue == null) _GetConstantValue = _this.GetMethod("GetConstantValue", new Type[] { typeof(string), typeof(string) }); return _GetConstantValue; } }
            public static MethodInfo IsConstantDefined { get { if (_IsConstantDefined == null) _IsConstantDefined = _this.GetMethod("IsConstantDefined", new Type[] { typeof(string) }); return _IsConstantDefined; } }
            public static MethodInfo DeclareConstant { get { return _DeclareConstant ?? (_DeclareConstant = _this.GetMethod("DeclareConstant", new Type[] { typeof(string), typeof(object) })); } }

            public static MethodInfo RegisterDObjectForFinalization { get { if (_RegisterDObjectForFinalization == null) _RegisterDObjectForFinalization = _this.GetMethod("RegisterDObjectForFinalization"); return _RegisterDObjectForFinalization; } }

            public static MethodInfo DeclareFunction { get { if (_DeclareFunction == null) _DeclareFunction = _this.GetMethod("DeclareFunction"); return _DeclareFunction; } }
            public static MethodInfo DeclareLambda { get { if (_DeclareLambda == null) _DeclareLambda = _this.GetMethod("DeclareLambda"); return _DeclareLambda; } }
            
            public static MethodInfo Call { get { if (_Call == null) _Call = _this.GetMethod("Call", new Type[] { typeof(Dictionary<string, object>), typeof(PHP.Core.NamingContext), typeof(object), typeof(string), typeof(DRoutineDesc).MakeByRefType(), typeof(PHP.Core.ScriptContext) }); return _Call; } }
            public static MethodInfo CallValue { get { return _CallValue ?? (_CallValue = _this.GetMethod("CallValue", new Type[] { typeof(Dictionary<string, object>), typeof(PHP.Core.NamingContext), typeof(object), typeof(string), typeof(DRoutineDesc).MakeByRefType(), typeof(PHP.Core.ScriptContext) })); } }
            public static MethodInfo CallVoid { get { return _CallVoid ?? (_CallVoid = _this.GetMethod("CallVoid", new Type[] { typeof(Dictionary<string, object>), typeof(PHP.Core.NamingContext), typeof(object), typeof(string), typeof(DRoutineDesc).MakeByRefType(), typeof(PHP.Core.ScriptContext) })); } }

            public static MethodInfo DeclareType_Handle { get { if (_DeclareType_Handle == null) _DeclareType_Handle = _this.GetMethod("DeclareType", new Type[] { typeof(RuntimeTypeHandle), Types.String[0] }); return _DeclareType_Handle; } }
            public static MethodInfo DeclareType_TypeDesc { get { if (_DeclareType_TypeDesc == null) _DeclareType_TypeDesc = _this.GetMethod("DeclareType", new Type[] { Types.PhpTypeDesc[0], Types.String[0] }); return _DeclareType_TypeDesc; } }
            public static MethodInfo DeclareIncompleteTypeHelper { get { return _DeclareIncompleteTypeHelper ?? (_DeclareIncompleteTypeHelper = _this.GetMethod("DeclareIncompleteTypeHelper")); } }
            public static MethodInfo IncompleteTypeDeclared { get { return _IncompleteTypeDeclared ?? (_IncompleteTypeDeclared = _this.GetMethod("IncompleteTypeDeclared")); } }
        }

        #endregion

        #region PhpVariable

        public struct PhpVariable
        {
            static Type _this { get { return typeof(PHP.Core.PhpVariable); } }
            static MethodInfo _Copy, _IsEmpty, _IsString, _MakeReference, _AsString, _Dereference, _Unwrap;

            public static MethodInfo Copy { get { if (_Copy == null) _Copy = _this.GetMethod("Copy"); return _Copy; } }
            public static MethodInfo IsEmpty { get { if (_IsEmpty == null) _IsEmpty = _this.GetMethod("IsEmpty"); return _IsEmpty; } }
            public static MethodInfo IsString { get { if (_IsString == null) _IsString = _this.GetMethod("IsString"); return _IsString; } }
            public static MethodInfo AsString { get { if (_AsString == null) _AsString = _this.GetMethod("AsString"); return _AsString; } }
            public static MethodInfo MakeReference { get { if (_MakeReference == null) _MakeReference = _this.GetMethod("MakeReference"); return _MakeReference; } }
            public static MethodInfo Dereference { get { return _Dereference ?? (_Dereference = _this.GetMethod("Dereference", Types.Object)); } }
            public static MethodInfo Unwrap { get { return _Unwrap ?? (_Unwrap = _this.GetMethod("Unwrap")); } }
        }

        #endregion

        #region Conversions

        // automatically generated by MethodsGenerator:
        // removed: 
        //   ByteArrayToCharArray
        public struct Convert
        {
            static Type _this { get { return typeof(PHP.Core.Convert); } }
            static MethodInfo _ObjectToString, _ObjectToChar, _ObjectToPhpBytes, _ObjectToBoolean, _ObjectToInteger, _ObjectToLongInteger,
                _ObjectToDouble, _ObjectToPhpArray, _ObjectToDObject, _ObjectToCallback, _ObjectToLinqSource, _ObjectToTypeDesc,
                _TryObjectToBoolean, _TryObjectToInt8, _TryObjectToInt16, _TryObjectToInt32, _TryObjectToInt64, _TryObjectToUInt8,
                _TryObjectToUInt16, _TryObjectToUInt32, _TryObjectToUInt64, _TryObjectToSingle, _TryObjectToDouble, _TryObjectToDecimal,
                _TryObjectToChar, _TryObjectToString, _TryObjectToDateTime, _TryObjectToDBNull, _TryObjectToClass, _TryObjectToStruct,
                _TryObjectToDelegate, _TryObjectToArray, _TryObjectToType, _StringToTypeDesc;

            public static MethodInfo ObjectToString { get { if (_ObjectToString == null) _ObjectToString = _this.GetMethod("ObjectToString", Types.Object); return _ObjectToString; } }
            public static MethodInfo ObjectToChar { get { if (_ObjectToChar == null) _ObjectToChar = _this.GetMethod("ObjectToChar"); return _ObjectToChar; } }
            public static MethodInfo ObjectToPhpBytes { get { if (_ObjectToPhpBytes == null) _ObjectToPhpBytes = _this.GetMethod("ObjectToPhpBytes"); return _ObjectToPhpBytes; } }
            public static MethodInfo ObjectToBoolean { get { if (_ObjectToBoolean == null) _ObjectToBoolean = _this.GetMethod("ObjectToBoolean"); return _ObjectToBoolean; } }
            public static MethodInfo ObjectToInteger { get { if (_ObjectToInteger == null) _ObjectToInteger = _this.GetMethod("ObjectToInteger"); return _ObjectToInteger; } }
            public static MethodInfo ObjectToLongInteger { get { if (_ObjectToLongInteger == null) _ObjectToLongInteger = _this.GetMethod("ObjectToLongInteger"); return _ObjectToLongInteger; } }
            public static MethodInfo ObjectToDouble { get { if (_ObjectToDouble == null) _ObjectToDouble = _this.GetMethod("ObjectToDouble"); return _ObjectToDouble; } }
            public static MethodInfo ObjectToPhpArray { get { if (_ObjectToPhpArray == null) _ObjectToPhpArray = _this.GetMethod("ObjectToPhpArray"); return _ObjectToPhpArray; } }
            public static MethodInfo ObjectToDObject { get { if (_ObjectToDObject == null) _ObjectToDObject = _this.GetMethod("ObjectToDObject"); return _ObjectToDObject; } }
            public static MethodInfo ObjectToCallback { get { if (_ObjectToCallback == null) _ObjectToCallback = _this.GetMethod("ObjectToCallback", Types.Object); return _ObjectToCallback; } }
            public static MethodInfo ObjectToTypeDesc { get { if (_ObjectToTypeDesc == null) _ObjectToTypeDesc = _this.GetMethod("ObjectToTypeDesc"); return _ObjectToTypeDesc; } }
            public static MethodInfo ObjectToLinqSource { get { if (_ObjectToLinqSource == null) _ObjectToLinqSource = _this.GetMethod("ObjectToLinqSource"); return _ObjectToLinqSource; } }

            public static MethodInfo StringToTypeDesc { get { return _StringToTypeDesc ?? (_StringToTypeDesc = new Func<string, ResolveTypeFlags, DTypeDesc, Core.ScriptContext, Core.NamingContext, object[], DTypeDesc>(Core.Convert.StringToTypeDesc).Method); } }

            public static MethodInfo TryObjectToBoolean { get { if (_TryObjectToBoolean == null) _TryObjectToBoolean = _this.GetMethod("TryObjectToBoolean"); return _TryObjectToBoolean; } }
            public static MethodInfo TryObjectToInt8 { get { if (_TryObjectToInt8 == null) _TryObjectToInt8 = _this.GetMethod("TryObjectToInt8"); return _TryObjectToInt8; } }
            public static MethodInfo TryObjectToInt16 { get { if (_TryObjectToInt16 == null) _TryObjectToInt16 = _this.GetMethod("TryObjectToInt16"); return _TryObjectToInt16; } }
            public static MethodInfo TryObjectToInt32 { get { if (_TryObjectToInt32 == null) _TryObjectToInt32 = _this.GetMethod("TryObjectToInt32"); return _TryObjectToInt32; } }
            public static MethodInfo TryObjectToInt64 { get { if (_TryObjectToInt64 == null) _TryObjectToInt64 = _this.GetMethod("TryObjectToInt64"); return _TryObjectToInt64; } }
            public static MethodInfo TryObjectToUInt8 { get { if (_TryObjectToUInt8 == null) _TryObjectToUInt8 = _this.GetMethod("TryObjectToUInt8"); return _TryObjectToUInt8; } }
            public static MethodInfo TryObjectToUInt16 { get { if (_TryObjectToUInt16 == null) _TryObjectToUInt16 = _this.GetMethod("TryObjectToUInt16"); return _TryObjectToUInt16; } }
            public static MethodInfo TryObjectToUInt32 { get { if (_TryObjectToUInt32 == null) _TryObjectToUInt32 = _this.GetMethod("TryObjectToUInt32"); return _TryObjectToUInt32; } }
            public static MethodInfo TryObjectToUInt64 { get { if (_TryObjectToUInt64 == null) _TryObjectToUInt64 = _this.GetMethod("TryObjectToUInt64"); return _TryObjectToUInt64; } }
            public static MethodInfo TryObjectToSingle { get { if (_TryObjectToSingle == null) _TryObjectToSingle = _this.GetMethod("TryObjectToSingle"); return _TryObjectToSingle; } }
            public static MethodInfo TryObjectToDouble { get { if (_TryObjectToDouble == null) _TryObjectToDouble = _this.GetMethod("TryObjectToDouble"); return _TryObjectToDouble; } }
            public static MethodInfo TryObjectToDecimal { get { if (_TryObjectToDecimal == null) _TryObjectToDecimal = _this.GetMethod("TryObjectToDecimal"); return _TryObjectToDecimal; } }
            public static MethodInfo TryObjectToChar { get { if (_TryObjectToChar == null) _TryObjectToChar = _this.GetMethod("TryObjectToChar"); return _TryObjectToChar; } }
            public static MethodInfo TryObjectToString { get { if (_TryObjectToString == null) _TryObjectToString = _this.GetMethod("TryObjectToString"); return _TryObjectToString; } }
            public static MethodInfo TryObjectToDateTime { get { if (_TryObjectToDateTime == null) _TryObjectToDateTime = _this.GetMethod("TryObjectToDateTime"); return _TryObjectToDateTime; } }
            public static MethodInfo TryObjectToDBNull { get { if (_TryObjectToDBNull == null) _TryObjectToDBNull = _this.GetMethod("TryObjectToDBNull"); return _TryObjectToDBNull; } }
            public static MethodInfo TryObjectToClass { get { if (_TryObjectToClass == null) _TryObjectToClass = _this.GetMethod("TryObjectToClass"); return _TryObjectToClass; } }
            public static MethodInfo TryObjectToStruct { get { if (_TryObjectToStruct == null) _TryObjectToStruct = _this.GetMethod("TryObjectToStruct"); return _TryObjectToStruct; } }
            public static MethodInfo TryObjectToDelegate { get { if (_TryObjectToDelegate == null) _TryObjectToDelegate = _this.GetMethod("TryObjectToDelegate"); return _TryObjectToDelegate; } }
            public static MethodInfo TryObjectToArray { get { if (_TryObjectToArray == null) _TryObjectToArray = _this.GetMethod("TryObjectToArray"); return _TryObjectToArray; } }
            public static MethodInfo TryObjectToType { get { if (_TryObjectToType == null) _TryObjectToType = _this.GetMethod("TryObjectToType"); return _TryObjectToType; } }
        }

        // automatically generated by MethodsGenerator:
        public struct ConvertToClr
        {
            static Type _this { get { return typeof(PHP.Core.ConvertToClr); } }
            static MethodInfo _TryObjectToClass, _TryObjectToDelegate, _TryObjectToArray, _TryObjectToStruct, _TryObjectToType, _TryObjectToBoolean, _TryObjectToInt8, _TryObjectToInt16, _TryObjectToUInt8, _TryObjectToUInt16, _TryObjectToUInt32, _TryObjectToInt32, _TryObjectToInt64, _TryObjectToUInt64, _TryObjectToSingle, _TryObjectToDouble, _TryObjectToDecimal, _TryObjectToChar, _TryObjectToString, _TryObjectToDateTime, _TryObjectToDBNull, _UnwrapNullable;

            public static MethodInfo TryObjectToClass { get { if (_TryObjectToClass == null) _TryObjectToClass = _this.GetMethod("TryObjectToClass"); return _TryObjectToClass; } }
            public static MethodInfo TryObjectToDelegate { get { if (_TryObjectToDelegate == null) _TryObjectToDelegate = _this.GetMethod("TryObjectToDelegate"); return _TryObjectToDelegate; } }
            public static MethodInfo TryObjectToArray { get { if (_TryObjectToArray == null) _TryObjectToArray = _this.GetMethod("TryObjectToArray"); return _TryObjectToArray; } }
            public static MethodInfo TryObjectToStruct { get { if (_TryObjectToStruct == null) _TryObjectToStruct = _this.GetMethod("TryObjectToStruct"); return _TryObjectToStruct; } }
            public static MethodInfo UnwrapNullable { get { if (_UnwrapNullable == null) _UnwrapNullable = _this.GetMethod("UnwrapNullable"); return _UnwrapNullable; } }
            public static MethodInfo TryObjectToType { get { if (_TryObjectToType == null) _TryObjectToType = _this.GetMethod("TryObjectToType"); return _TryObjectToType; } }
            public static MethodInfo TryObjectToBoolean { get { if (_TryObjectToBoolean == null) _TryObjectToBoolean = _this.GetMethod("TryObjectToBoolean"); return _TryObjectToBoolean; } }
            public static MethodInfo TryObjectToInt8 { get { if (_TryObjectToInt8 == null) _TryObjectToInt8 = _this.GetMethod("TryObjectToInt8"); return _TryObjectToInt8; } }
            public static MethodInfo TryObjectToInt16 { get { if (_TryObjectToInt16 == null) _TryObjectToInt16 = _this.GetMethod("TryObjectToInt16"); return _TryObjectToInt16; } }
            public static MethodInfo TryObjectToUInt8 { get { if (_TryObjectToUInt8 == null) _TryObjectToUInt8 = _this.GetMethod("TryObjectToUInt8"); return _TryObjectToUInt8; } }
            public static MethodInfo TryObjectToUInt16 { get { if (_TryObjectToUInt16 == null) _TryObjectToUInt16 = _this.GetMethod("TryObjectToUInt16"); return _TryObjectToUInt16; } }
            public static MethodInfo TryObjectToUInt32 { get { if (_TryObjectToUInt32 == null) _TryObjectToUInt32 = _this.GetMethod("TryObjectToUInt32"); return _TryObjectToUInt32; } }
            public static MethodInfo TryObjectToInt32 { get { if (_TryObjectToInt32 == null) _TryObjectToInt32 = _this.GetMethod("TryObjectToInt32"); return _TryObjectToInt32; } }
            public static MethodInfo TryObjectToInt64 { get { if (_TryObjectToInt64 == null) _TryObjectToInt64 = _this.GetMethod("TryObjectToInt64"); return _TryObjectToInt64; } }
            public static MethodInfo TryObjectToUInt64 { get { if (_TryObjectToUInt64 == null) _TryObjectToUInt64 = _this.GetMethod("TryObjectToUInt64"); return _TryObjectToUInt64; } }
            public static MethodInfo TryObjectToSingle { get { if (_TryObjectToSingle == null) _TryObjectToSingle = _this.GetMethod("TryObjectToSingle"); return _TryObjectToSingle; } }
            public static MethodInfo TryObjectToDouble { get { if (_TryObjectToDouble == null) _TryObjectToDouble = _this.GetMethod("TryObjectToDouble"); return _TryObjectToDouble; } }
            public static MethodInfo TryObjectToDecimal { get { if (_TryObjectToDecimal == null) _TryObjectToDecimal = _this.GetMethod("TryObjectToDecimal"); return _TryObjectToDecimal; } }
            public static MethodInfo TryObjectToChar { get { if (_TryObjectToChar == null) _TryObjectToChar = _this.GetMethod("TryObjectToChar"); return _TryObjectToChar; } }
            public static MethodInfo TryObjectToString { get { if (_TryObjectToString == null) _TryObjectToString = _this.GetMethod("TryObjectToString"); return _TryObjectToString; } }
            public static MethodInfo TryObjectToDateTime { get { if (_TryObjectToDateTime == null) _TryObjectToDateTime = _this.GetMethod("TryObjectToDateTime"); return _TryObjectToDateTime; } }
            public static MethodInfo TryObjectToDBNull { get { if (_TryObjectToDBNull == null) _TryObjectToDBNull = _this.GetMethod("TryObjectToDBNull"); return _TryObjectToDBNull; } }
        }

        #endregion

        #region Comparisons

        static MethodInfo _CompareEq_object_object, _CompareEq_object_int, _CompareEq_object_string, _CompareOp_object_object_bool, _CompareOp_int_int, _CompareOp_object_int_bool, _CompareOp_int_object_bool;

        public static MethodInfo CompareEq_object_object { get { return _CompareEq_object_object ?? (_CompareEq_object_object = new Func<object,object,bool>(PHP.Core.PhpComparer.CompareEq).Method); } }
        public static MethodInfo CompareEq_object_int { get { return _CompareEq_object_int ?? (_CompareEq_object_int = new Func<object, int, bool>(PHP.Core.PhpComparer.CompareEq).Method); } }
        public static MethodInfo CompareEq_object_string { get { return _CompareEq_object_string ?? (_CompareEq_object_string = new Func<object, string, bool>(PHP.Core.PhpComparer.CompareEq).Method); } }
        public static MethodInfo CompareOp_object_object_bool { get { return _CompareOp_object_object_bool ?? (_CompareOp_object_object_bool = typeof(PHP.Core.PhpComparer).GetMethod("CompareOp", Types.Object_Object_Bool)); } }
        public static MethodInfo CompareOp_int_int { get { return _CompareOp_int_int ?? (_CompareOp_int_int = typeof(PHP.Core.PhpComparer).GetMethod("CompareOp", Types.Int_Int)); } }
        public static MethodInfo CompareOp_object_int_bool { get { return _CompareOp_object_int_bool ?? (_CompareOp_object_int_bool = typeof(PHP.Core.PhpComparer).GetMethod("CompareOp", new Type[] { typeof(object), typeof(int), typeof(bool) })); } }
        public static MethodInfo CompareOp_int_object_bool { get { return _CompareOp_int_object_bool ?? (_CompareOp_int_object_bool = typeof(PHP.Core.PhpComparer).GetMethod("CompareOp", new Type[] { typeof(int), typeof(object), typeof(bool) })); } }

        #endregion

        #region Externals (CLR only)
#if !SILVERLIGHT
        public struct Externals
        {
            static Type _this { get { return typeof(PHP.Core.Externals); } }

            static MethodInfo _InvokeFunction, _InvokeMethod, _InvokeFunctionDynamic, _InvokeMethodDynamic,
                _GetFunctionProxy,
                _MarshalBoundVariables, _MarkParameterForBinding,
                _PrepareParametersForBinding, _BindParameters;

            public static MethodInfo InvokeFunction { get { if (_InvokeFunction == null)          _InvokeFunction = _this.GetMethod("InvokeFunction"); return _InvokeFunction; } }
            public static MethodInfo InvokeMethod { get { if (_InvokeMethod == null)            _InvokeMethod = _this.GetMethod("InvokeMethod"); return _InvokeMethod; } }
            public static MethodInfo InvokeFunctionDynamic { get { if (_InvokeFunctionDynamic == null)   _InvokeFunctionDynamic = _this.GetMethod("InvokeFunctionDynamic"); return _InvokeFunctionDynamic; } }
            public static MethodInfo InvokeMethodDynamic { get { if (_InvokeMethodDynamic == null)     _InvokeMethodDynamic = _this.GetMethod("InvokeMethodDynamic"); return _InvokeMethodDynamic; } }

            public static MethodInfo GetFunctionProxy { get { return (_GetFunctionProxy ?? (_GetFunctionProxy = _this.GetMethod("GetFunctionProxy"))); } }

            public static MethodInfo MarshalBoundVariables { get { if (_MarshalBoundVariables == null)   _MarshalBoundVariables = _this.GetMethod("MarshalBoundVariables"); return _MarshalBoundVariables; } }
            public static MethodInfo MarkParameterForBinding { get { if (_MarkParameterForBinding == null) _MarkParameterForBinding = _this.GetMethod("MarkParameterForBinding"); return _MarkParameterForBinding; } }

            public static MethodInfo PrepareParametersForBinding { get { if (_PrepareParametersForBinding == null)   _PrepareParametersForBinding = _this.GetMethod("PrepareParametersForBinding"); return _PrepareParametersForBinding; } }
            public static MethodInfo BindParameters { get { if (_BindParameters == null) _BindParameters = _this.GetMethod("BindParameters"); return _BindParameters; } }

            public struct ParameterTransformation
            {
                static Type _this { get { return typeof(PHP.Core.Externals.ParameterTransformation); } }

                static MethodInfo _TransformInParameters, _TransformOutParameters, _TransformInParameter, _TransformOutParameter;

                public static MethodInfo TransformInParameters { get { if (_TransformInParameters == null)   _TransformInParameters = _this.GetMethod("TransformInParameters"); return _TransformInParameters; } }
                public static MethodInfo TransformOutParameters { get { if (_TransformOutParameters == null) _TransformOutParameters = _this.GetMethod("TransformOutParameters"); return _TransformOutParameters; } }

                public static MethodInfo TransformInParameter { get { if (_TransformInParameter == null)   _TransformInParameter = _this.GetMethod("TransformInParameter"); return _TransformInParameter; } }
                public static MethodInfo TransformOutParameter { get { if (_TransformOutParameter == null) _TransformOutParameter = _this.GetMethod("TransformOutParameter"); return _TransformOutParameter; } }
            }

            public struct IExternalFunction
            {
                static Type _this { get { return typeof(PHP.Core.IExternalFunction); } }

                static MethodInfo _Invoke, _GetExtManager;

                public static MethodInfo Invoke { get { if (_Invoke == null)   _Invoke = _this.GetMethod("Invoke"); return _Invoke; } }
                public static MethodInfo GetExtManager { get { if (_GetExtManager == null)   _GetExtManager = _this.GetMethod("get_ExtManager"); return _GetExtManager; } }
            }
        }
#endif
        #endregion

        #region PhpStack

        public struct PhpStack
        {
            static Type _this { get { return typeof(PHP.Core.PhpStack); } }
            static MethodInfo _RemoveFrame, _RemoveArgsAwareFrame, _PeekValue, _PeekValueOptional,
              _PeekReference, _PeekReferenceOptional, _PeekReferenceUnchecked, _PeekValueUnchecked,
              _MakeArgsAware, _PeekType, _PeekTypeOptional, _ThrowIfNotArgsaware;


            public static class AddFrame
            {
                private static MethodInfo _0, _1, _2, _3, _4, _5, _6, _7, _8, _N;
                public const int ExplicitOverloads = 8;

                public static MethodInfo Overload(int i)
                {
                    Type t = Types.Object[0];
                    switch (i)
                    {
                        case 0: if (_0 == null) _0 = _this.GetMethod("AddFrame", Type.EmptyTypes/*new Type[] {}*/); return _0;
                        case 1: if (_1 == null) _1 = _this.GetMethod("AddFrame", Types.Object/*new Type[] {t}*/); return _1;
                        case 2: if (_2 == null) _2 = _this.GetMethod("AddFrame", new Type[] { t, t }); return _2;
                        case 3: if (_3 == null) _3 = _this.GetMethod("AddFrame", new Type[] { t, t, t }); return _3;
                        case 4: if (_4 == null) _4 = _this.GetMethod("AddFrame", new Type[] { t, t, t, t }); return _4;
                        case 5: if (_5 == null) _5 = _this.GetMethod("AddFrame", new Type[] { t, t, t, t, t }); return _5;
                        case 6: if (_6 == null) _6 = _this.GetMethod("AddFrame", new Type[] { t, t, t, t, t, t }); return _6;
                        case 7: if (_7 == null) _7 = _this.GetMethod("AddFrame", new Type[] { t, t, t, t, t, t, t }); return _7;
                        case 8: if (_8 == null) _8 = _this.GetMethod("AddFrame", new Type[] { t, t, t, t, t, t, t, t }); return _8;

                        default:
                            return N;
                    }
                }

                public static MethodInfo N { get { if (_N == null) _N = _this.GetMethod("AddFrame", new Type[] { typeof(object[]) }); return _N; } }
            }

            public static class AddTypeFrame
            {
                private static MethodInfo _0, _1, _2, _3, _4, _5, _6, _7, _8, _N;
                public const int ExplicitOverloads = 8;

                public static MethodInfo Overload(int i)
                {
                    Type t = Types.DTypeDesc[0];
                    switch (i)
                    {
                        case 0: if (_0 == null) _0 = _this.GetMethod("AddTypeFrame", Type.EmptyTypes/*new Type[] {}*/); return _0;
                        case 1: if (_1 == null) _1 = _this.GetMethod("AddTypeFrame", Types.DTypeDesc/*new Type[] {t}*/); return _1;
                        case 2: if (_2 == null) _2 = _this.GetMethod("AddTypeFrame", new Type[] { t, t }); return _2;
                        case 3: if (_3 == null) _3 = _this.GetMethod("AddTypeFrame", new Type[] { t, t, t }); return _3;
                        case 4: if (_4 == null) _4 = _this.GetMethod("AddTypeFrame", new Type[] { t, t, t, t }); return _4;
                        case 5: if (_5 == null) _5 = _this.GetMethod("AddTypeFrame", new Type[] { t, t, t, t, t }); return _5;
                        case 6: if (_6 == null) _6 = _this.GetMethod("AddTypeFrame", new Type[] { t, t, t, t, t, t }); return _6;
                        case 7: if (_7 == null) _7 = _this.GetMethod("AddTypeFrame", new Type[] { t, t, t, t, t, t, t }); return _7;
                        case 8: if (_8 == null) _8 = _this.GetMethod("AddTypeFrame", new Type[] { t, t, t, t, t, t, t, t }); return _8;

                        default:
                            return N;
                    }
                }

                public static MethodInfo N { get { if (_N == null) _N = _this.GetMethod("AddTypeFrame", new Type[] { typeof(Reflection.DTypeDesc[]) }); return _N; } }
            }

            public static MethodInfo PeekValue { get { if (_PeekValue == null) _PeekValue = _this.GetMethod("PeekValue"); return _PeekValue; } }
            public static MethodInfo PeekValueOptional { get { if (_PeekValueOptional == null) _PeekValueOptional = _this.GetMethod("PeekValueOptional"); return _PeekValueOptional; } }
            public static MethodInfo PeekValueUnchecked { get { if (_PeekValueUnchecked == null) _PeekValueUnchecked = _this.GetMethod("PeekValueUnchecked"); return _PeekValueUnchecked; } }
            public static MethodInfo PeekReference { get { if (_PeekReference == null) _PeekReference = _this.GetMethod("PeekReference"); return _PeekReference; } }
            public static MethodInfo PeekReferenceOptional { get { if (_PeekReferenceOptional == null) _PeekReferenceOptional = _this.GetMethod("PeekReferenceOptional"); return _PeekReferenceOptional; } }
            public static MethodInfo PeekReferenceUnchecked { get { if (_PeekReferenceUnchecked == null) _PeekReferenceUnchecked = _this.GetMethod("PeekReferenceUnchecked"); return _PeekReferenceUnchecked; } }
            public static MethodInfo PeekType { get { if (_PeekType == null) _PeekType = _this.GetMethod("PeekType"); return _PeekType; } }
            public static MethodInfo PeekTypeOptional { get { if (_PeekTypeOptional == null) _PeekTypeOptional = _this.GetMethod("PeekTypeOptional"); return _PeekTypeOptional; } }
            public static MethodInfo RemoveFrame { get { if (_RemoveFrame == null) _RemoveFrame = _this.GetMethod("RemoveFrame"); return _RemoveFrame; } }
            public static MethodInfo RemoveArgsAwareFrame { get { if (_RemoveArgsAwareFrame == null) _RemoveArgsAwareFrame = _this.GetMethod("RemoveArgsAwareFrame"); return _RemoveArgsAwareFrame; } }
            public static MethodInfo MakeArgsAware { get { if (_MakeArgsAware == null) _MakeArgsAware = _this.GetMethod("MakeArgsAware"); return _MakeArgsAware; } }
            public static MethodInfo ThrowIfNotArgsaware { get { return _ThrowIfNotArgsaware ?? (_ThrowIfNotArgsaware = _this.GetMethod("ThrowIfNotArgsaware")); } }
        }

        #endregion

        #region PhpException

        public struct PhpException
        {
            static MethodInfo _Throw, _MissingArgument, _MissingTypeArgument, _MissingArguments, _InvalidForeachArgument,
                      _InvalidImplicitCast, _InvalidBreakLevelCount,
                      _InvalidArgumentCount, _UndefinedVariable, _CannotReassignThis, _InvalidArgumentType, _ThisUsedOutOfObjectContext,
                      _StaticPropertyUnset, _AbstractMethodCalled, _NoSuitableOverload, _PropertyTypeMismatch, _UndefinedMethodCalled,
                      _FunctionNotSupported_String;

            public static MethodInfo Throw { get { if (_Throw == null) _Throw = new Action<PhpError,string>(Core.PhpException.Throw).Method; return _Throw; } }
            public static MethodInfo MissingArgument { get { if (_MissingArgument == null) _MissingArgument = new Action<int, string>(Core.PhpException.MissingArgument).Method; return _MissingArgument; } }
            public static MethodInfo MissingTypeArgument { get { if (_MissingTypeArgument == null) _MissingTypeArgument = new Action<int, string>(Core.PhpException.MissingTypeArgument).Method; return _MissingTypeArgument; } }
            public static MethodInfo MissingArguments { get { if (_MissingArguments == null) _MissingArguments = new Action<string,string,int,int>(Core.PhpException.MissingArguments).Method; return _MissingArguments; } }
            public static MethodInfo InvalidArgumentCount { get { if (_InvalidArgumentCount == null) _InvalidArgumentCount = new Action<string, string>(Core.PhpException.InvalidArgumentCount).Method; return _InvalidArgumentCount; } }
            public static MethodInfo InvalidForeachArgument { get { if (_InvalidForeachArgument == null) _InvalidForeachArgument = new Action(Core.PhpException.InvalidForeachArgument).Method; return _InvalidForeachArgument; } }
            public static MethodInfo InvalidImplicitCast { get { if (_InvalidImplicitCast == null) _InvalidImplicitCast = new Action<object,string,string>(Core.PhpException.InvalidImplicitCast).Method; return _InvalidImplicitCast; } }
            public static MethodInfo InvalidBreakLevelCount { get { if (_InvalidBreakLevelCount == null) _InvalidBreakLevelCount = new Action<int>(Core.PhpException.InvalidBreakLevelCount).Method; return _InvalidBreakLevelCount; } }
            public static MethodInfo UndefinedVariable { get { if (_UndefinedVariable == null) _UndefinedVariable = new Action<string>(Core.PhpException.UndefinedVariable).Method; return _UndefinedVariable; } }
            public static MethodInfo AbstractMethodCalled { get { if (_AbstractMethodCalled == null) _AbstractMethodCalled = new Action<string, string>(Core.PhpException.AbstractMethodCalled).Method; return _AbstractMethodCalled; } }
            public static MethodInfo CannotReassignThis { get { if (_CannotReassignThis == null) _CannotReassignThis = new Action(Core.PhpException.CannotReassignThis).Method; return _CannotReassignThis; } }
            public static MethodInfo InvalidArgumentType { get { if (_InvalidArgumentType == null) _InvalidArgumentType = new Action<string, string>(Core.PhpException.InvalidArgumentType).Method; return _InvalidArgumentType; } }
            public static MethodInfo ThisUsedOutOfObjectContext { get { if (_ThisUsedOutOfObjectContext == null) _ThisUsedOutOfObjectContext = new Action(Core.PhpException.ThisUsedOutOfObjectContext).Method; return _ThisUsedOutOfObjectContext; } }
            public static MethodInfo StaticPropertyUnset { get { if (_StaticPropertyUnset == null) _StaticPropertyUnset = new Action<string, string>(Core.PhpException.StaticPropertyUnset).Method; return _StaticPropertyUnset; } }
            public static MethodInfo NoSuitableOverload { get { if (_NoSuitableOverload == null) _NoSuitableOverload = new Action<string, string>(Core.PhpException.NoSuitableOverload).Method; return _NoSuitableOverload; } }
            public static MethodInfo PropertyTypeMismatch { get { if (_PropertyTypeMismatch == null) _PropertyTypeMismatch = new Action<string, string>(Core.PhpException.PropertyTypeMismatch).Method; return _PropertyTypeMismatch; } }
            public static MethodInfo UndefinedMethodCalled { get { if (_UndefinedMethodCalled == null) _UndefinedMethodCalled = new Action<string, string>(Core.PhpException.UndefinedMethodCalled).Method; return _UndefinedMethodCalled; } }
            public static MethodInfo FunctionNotSupported_String { get { return _FunctionNotSupported_String ?? (_FunctionNotSupported_String = new Action<string>(Core.PhpException.FunctionNotSupported).Method); } }
        }

        #endregion

        #region SPL.Exception



        #endregion

        #region PhpRuntimeChain

        public struct PhpRuntimeChain
        {
            static Type _this { get { return typeof(PHP.Core.PhpRuntimeChain); } }
            static MethodInfo _AddField, _AddItem_Object, _AddItem_Void, _GetValue, _GetReference;

            public static MethodInfo AddField { get { if (_AddField == null) _AddField = _this.GetMethod("AddField"); return _AddField; } }
            public static MethodInfo AddItem_Object { get { if (_AddItem_Object == null) _AddItem_Object = _this.GetMethod("AddItem", Types.Object); return _AddItem_Object; } }
            public static MethodInfo AddItem_Void { get { if (_AddItem_Void == null) _AddItem_Void = _this.GetMethod("AddItem", Type.EmptyTypes); return _AddItem_Void; } }

            public static MethodInfo GetValue { get { if (_GetValue == null) _GetValue = _this.GetMethod("GetValue"); return _GetValue; } }
            public static MethodInfo GetReference { get { if (_GetReference == null) _GetReference = _this.GetMethod("GetReference"); return _GetReference; } }
        }

        #endregion

        #region PhpArray

        public struct PhpArray
        {
            static Type _this { get { return typeof(PHP.Core.PhpArray); } }

            static MethodInfo
                _GetArrayItem_Object,
                _GetArrayItem_Int32,
                _GetArrayItem_String,
                _GetArrayItemExact_String,

                _GetArrayItemRef,
                _GetArrayItemRef_Object,
                _GetArrayItemRef_Int32,
                _GetArrayItemRef_String,

                _SetArrayItem,
                _SetArrayItem_Object,
                _SetArrayItem_Int32,
                _SetArrayItem_String,
                _SetArrayItemExact_String,

                _AddToEnd_Object,

                _SetArrayItemRef_Object,
                _SetArrayItemRef_Int32,
                _SetArrayItemRef_String,

                _EnsureItemIsArray,
                _EnsureItemIsArray_Object,

                _EnsureItemIsObject,
                _EnsureItemIsObject_Object

            ;

            public static MethodInfo GetArrayItem_Object { get { if (_GetArrayItem_Object == null) _GetArrayItem_Object = _this.GetMethod("GetArrayItem", new Type[] { typeof(object), typeof(bool) }); return _GetArrayItem_Object; } }
            public static MethodInfo GetArrayItem_Int32 { get { if (_GetArrayItem_Int32 == null) _GetArrayItem_Int32 = _this.GetMethod("GetArrayItem", new Type[] { typeof(int), typeof(bool) }); return _GetArrayItem_Int32; } }
            public static MethodInfo GetArrayItem_String { get { if (_GetArrayItem_String == null) _GetArrayItem_String = _this.GetMethod("GetArrayItem", new Type[] { typeof(string), typeof(bool) }); return _GetArrayItem_String; } }
            public static MethodInfo GetArrayItemExact_String { get { if (_GetArrayItemExact_String == null) _GetArrayItemExact_String = _this.GetMethod("GetArrayItemExact", new Type[] { typeof(string), typeof(bool), typeof(int) }); return _GetArrayItemExact_String; } }

            public static MethodInfo GetArrayItemRef { get { if (_GetArrayItemRef == null) _GetArrayItemRef = _this.GetMethod("GetArrayItemRef", Type.EmptyTypes); return _GetArrayItemRef; } }
            public static MethodInfo GetArrayItemRef_Object { get { if (_GetArrayItemRef_Object == null) _GetArrayItemRef_Object = _this.GetMethod("GetArrayItemRef", Types.Object); return _GetArrayItemRef_Object; } }
            public static MethodInfo GetArrayItemRef_Int32 { get { if (_GetArrayItemRef_Int32 == null) _GetArrayItemRef_Int32 = _this.GetMethod("GetArrayItemRef", Types.Int); return _GetArrayItemRef_Int32; } }
            public static MethodInfo GetArrayItemRef_String { get { if (_GetArrayItemRef_String == null) _GetArrayItemRef_String = _this.GetMethod("GetArrayItemRef", Types.String); return _GetArrayItemRef_String; } }

            public static MethodInfo SetArrayItem { get { if (_SetArrayItem == null) _SetArrayItem = _this.GetMethod("SetArrayItem", Types.Object); return _SetArrayItem; } }
            public static MethodInfo SetArrayItem_Object { get { if (_SetArrayItem_Object == null) _SetArrayItem_Object = _this.GetMethod("SetArrayItem", new Type[] { typeof(object), typeof(object) }); return _SetArrayItem_Object; } }
            public static MethodInfo SetArrayItem_Int32 { get { if (_SetArrayItem_Int32 == null) _SetArrayItem_Int32 = _this.GetMethod("SetArrayItem", new Type[] { typeof(int), typeof(object) }); return _SetArrayItem_Int32; } }
            public static MethodInfo SetArrayItem_String { get { if (_SetArrayItem_String == null) _SetArrayItem_String = _this.GetMethod("SetArrayItem", new Type[] { typeof(string), typeof(object) }); return _SetArrayItem_String; } }
            public static MethodInfo SetArrayItemExact_String { get { if (_SetArrayItemExact_String == null) _SetArrayItemExact_String = _this.GetMethod("SetArrayItemExact", new Type[] { typeof(string), typeof(object), typeof(int) }); return _SetArrayItemExact_String; } }

            public static MethodInfo AddToEnd_Object { get { return _AddToEnd_Object ?? (_AddToEnd_Object = _this.GetMethod("AddToEnd", Types.Object)); } }

            public static MethodInfo SetArrayItemRef_Object { get { if (_SetArrayItemRef_Object == null) _SetArrayItemRef_Object = _this.GetMethod("SetArrayItemRef", new Type[] { typeof(object), typeof(PhpReference) }); return _SetArrayItemRef_Object; } }
            public static MethodInfo SetArrayItemRef_Int32 { get { if (_SetArrayItemRef_Int32 == null) _SetArrayItemRef_Int32 = _this.GetMethod("SetArrayItemRef", new Type[] { typeof(int), typeof(PhpReference) }); return _SetArrayItemRef_Int32; } }
            public static MethodInfo SetArrayItemRef_String { get { if (_SetArrayItemRef_String == null) _SetArrayItemRef_String = _this.GetMethod("SetArrayItemRef", new Type[] { typeof(string), typeof(PhpReference) }); return _SetArrayItemRef_String; } }

            public static MethodInfo EnsureItemIsArray { get { if (_EnsureItemIsArray == null) _EnsureItemIsArray = _this.GetMethod("EnsureItemIsArray", Type.EmptyTypes); return _EnsureItemIsArray; } }
            public static MethodInfo EnsureItemIsArray_Object { get { if (_EnsureItemIsArray_Object == null) _EnsureItemIsArray_Object = _this.GetMethod("EnsureItemIsArray", Types.Object); return _EnsureItemIsArray_Object; } }

            public static MethodInfo EnsureItemIsObject { get { if (_EnsureItemIsObject == null) _EnsureItemIsObject = _this.GetMethod("EnsureItemIsObject", Types.ScriptContext); return _EnsureItemIsObject; } }
            public static MethodInfo EnsureItemIsObject_Object { get { if (_EnsureItemIsObject_Object == null) _EnsureItemIsObject_Object = _this.GetMethod("EnsureItemIsObject", Types.Object_ScriptContext); return _EnsureItemIsObject_Object; } }

        }

        #endregion

        #region PhpBytes

        public struct PhpBytes
        {
            static Type _this { get { return typeof(PHP.Core.PhpBytes); } }

            static MethodInfo
                _Concat_PhpBytes_PhpBytes, _Concat_PhpBytes_Object, _Concat_Object_PhpBytes, _Append_Object_PhpBytes
            ;

            public static MethodInfo Concat_PhpBytes_PhpBytes { get { if (_Concat_PhpBytes_PhpBytes == null) _Concat_PhpBytes_PhpBytes = _this.GetMethod("Concat", new Type[] { typeof(PHP.Core.PhpBytes), typeof(PHP.Core.PhpBytes) }); return _Concat_PhpBytes_PhpBytes; } }
            public static MethodInfo Concat_PhpBytes_Object { get { if (_Concat_PhpBytes_Object == null) _Concat_PhpBytes_Object = _this.GetMethod("Concat", new Type[] { typeof(PHP.Core.PhpBytes), typeof(Object) }); return _Concat_PhpBytes_Object; } }
            public static MethodInfo Concat_Object_PhpBytes { get { if (_Concat_Object_PhpBytes == null) _Concat_Object_PhpBytes = _this.GetMethod("Concat", new Type[] { typeof(Object), typeof(PHP.Core.PhpBytes) }); return _Concat_Object_PhpBytes; } }

            public static MethodInfo Append_Object_PhpBytes { get { if (_Append_Object_PhpBytes == null) _Append_Object_PhpBytes = _this.GetMethod("Append", new Type[] { typeof(Object), typeof(PHP.Core.PhpBytes) }); return _Append_Object_PhpBytes; } }
        }

        #endregion

        #region Binder

        public struct Binder
        {
            static Type _this { get { return typeof(PHP.Core.Binders.Binder); } }

            static MethodInfo _MethodCall, _StaticMethodCall, _GetProperty, _StaticGetProperty;

            public static MethodInfo MethodCall { get { return _MethodCall ?? (_MethodCall = _this.GetMethod("MethodCall")); } }
            public static MethodInfo StaticMethodCall { get { return _StaticMethodCall ?? (_StaticMethodCall = _this.GetMethod("StaticMethodCall")); } }

            public static MethodInfo GetProperty { get { return _GetProperty ?? (_GetProperty = _this.GetMethod("GetProperty")); } }
            public static MethodInfo StaticGetProperty { get { return _StaticGetProperty ?? (_StaticGetProperty = _this.GetMethod("StaticGetProperty")); } }
        }

        #endregion

        #region Others

        static MethodInfo _GetTypeFromHandle, _Equality_Type_Type, _Object_Equals, _SetStaticInit, _AddConstant, _AddProperty, _AddMethod,
            _ShellExec, _IPhpEnumerable_GetForeachEnumerator,
            _String_IsInterned, _String_Concat_String_String, _IEnumerator_MoveNext, _DTypeDesc_Create,
            _PhpTypeDesc_Create, _ClrObject_Wrap, _ClrObject_WrapDynamic, _ClrObject_WrapRealObject, _ClrObject_Create, _Object_GetType,
            _Object_ToString, _Object_Finalize, _DObject_InvokeMethod, _DObject_InvokeConstructor, _DObject_Dispose, _DObject_GetRuntimeField, _DObject_SetProperty,
            _DRoutineDesc_Invoke, _PhpHashtable_Add, _InitializeArray, _ArrayCopy, _ArrayCopyTo, _PhpCallback_Invoke;

        public static MethodInfo GetTypeFromHandle { get { if (_GetTypeFromHandle == null)  _GetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle"); return _GetTypeFromHandle; } }
        public static MethodInfo Equality_Type_Type { get { return _Equality_Type_Type ?? (_Equality_Type_Type = typeof(Type).GetMethod("op_Equality")); } }

        public static MethodInfo Object_Equals { get { if (_Object_Equals == null)  _Object_Equals = Types.Object[0].GetMethod("Equals", Types.Object); return _Object_Equals; } }
        public static MethodInfo Object_GetType { get { if (_Object_GetType == null)  _Object_GetType = Types.Object[0].GetMethod("GetType", Type.EmptyTypes); return _Object_GetType; } }
        public static MethodInfo Object_ToString { get { if (_Object_ToString == null)  _Object_ToString = Types.Object[0].GetMethod("ToString", Type.EmptyTypes); return _Object_ToString; } }
        public static MethodInfo Object_Finalize { get { return _Object_Finalize ?? (_Object_Finalize = Types.Object[0].GetMethod("Finalize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)); } }

        public static MethodInfo SetStaticInit { get { if (_SetStaticInit == null)   _SetStaticInit = typeof(Reflection.PhpTypeDesc).GetMethod("SetStaticInit"); return _SetStaticInit; } }
        public static MethodInfo AddConstant { get { if (_AddConstant == null)     _AddConstant = typeof(Reflection.PhpTypeDesc).GetMethod("AddConstant", Types.String_Object); return _AddConstant; } }
        public static MethodInfo AddProperty { get { if (_AddProperty == null)     _AddProperty = typeof(Reflection.PhpTypeDesc).GetMethod("AddProperty", new Type[] { Types.String[0], typeof(Reflection.PhpMemberAttributes), typeof(GetterDelegate), typeof(SetterDelegate) }); return _AddProperty; } }
        public static MethodInfo AddMethod { get { if (_AddMethod == null)       _AddMethod = typeof(Reflection.PhpTypeDesc).GetMethod("AddMethod", new Type[] { Types.String[0], typeof(Reflection.PhpMemberAttributes), typeof(RoutineDelegate) }); return _AddMethod; } }

        public static MethodInfo DTypeDesc_Create { get { if (_DTypeDesc_Create == null)   _DTypeDesc_Create = Types.DTypeDesc[0].GetMethod("Create", new Type[] { typeof(RuntimeTypeHandle) }); return _DTypeDesc_Create; } }
        public static MethodInfo PhpTypeDesc_Create { get { if (_PhpTypeDesc_Create == null) _PhpTypeDesc_Create = Types.PhpTypeDesc[0].GetMethod("Create"); return _PhpTypeDesc_Create; } }

        public static MethodInfo ClrObject_Wrap { get { if (_ClrObject_Wrap == null) _ClrObject_Wrap = typeof(ClrObject).GetMethod("Wrap", Types.Object); return _ClrObject_Wrap; } }
        public static MethodInfo ClrObject_WrapDynamic { get { if (_ClrObject_WrapDynamic == null) _ClrObject_WrapDynamic = typeof(ClrObject).GetMethod("WrapDynamic", Types.Object); return _ClrObject_WrapDynamic; } }
        public static MethodInfo ClrObject_WrapRealObject { get { if (_ClrObject_WrapRealObject == null) _ClrObject_WrapRealObject = typeof(ClrObject).GetMethod("WrapRealObject", Types.Object); return _ClrObject_WrapRealObject; } }

        public static MethodInfo ClrObject_Create { get { if (_ClrObject_Create == null) _ClrObject_Create = typeof(ClrObject).GetMethod("Create"); return _ClrObject_Create; } }
        public static MethodInfo DObject_InvokeConstructor { get { if (_DObject_InvokeConstructor == null) _DObject_InvokeConstructor = Types.DObject[0].GetMethod("InvokeConstructor"); return _DObject_InvokeConstructor; } }
        public static MethodInfo DObject_InvokeMethod { get { if (_DObject_InvokeMethod == null) _DObject_InvokeMethod = Types.DObject[0].GetMethod("InvokeMethod"); return _DObject_InvokeMethod; } }
        public static MethodInfo DObject_Dispose { get { return _DObject_Dispose ?? (_DObject_Dispose = Types.DObject[0].GetMethod("Dispose", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Types.Bool, null)); } }
        public static MethodInfo DObject_GetRuntimeField { get { return _DObject_GetRuntimeField ?? (_DObject_GetRuntimeField = Types.DObject[0].GetMethod("GetRuntimeField")); } }
        public static MethodInfo DObject_SetProperty { get { return _DObject_SetProperty ?? (_DObject_SetProperty = Types.DObject[0].GetMethod("SetProperty")); } }
        public static MethodInfo DRoutineDesc_Invoke { get { if (_DRoutineDesc_Invoke == null) _DRoutineDesc_Invoke = typeof(DRoutineDesc).GetMethod("Invoke", new Type[] { Types.DObject[0], Types.PhpStack[0] }); return _DRoutineDesc_Invoke; } }
        
        public static MethodInfo InitializeArray { get { if (_InitializeArray == null) _InitializeArray = typeof(RuntimeHelpers).GetMethod("InitializeArray"); return _InitializeArray; } }
        public static MethodInfo ArrayCopy { get { if (_ArrayCopy == null)       _ArrayCopy = typeof(Array).GetMethod("Copy", new Type[] { typeof(Array), typeof(Int32), typeof(Array), typeof(Int32), typeof(Int32) }); return _ArrayCopy; } }
        public static MethodInfo ArrayCopyTo { get { if (_ArrayCopyTo == null)     _ArrayCopyTo = typeof(Array).GetMethod("CopyTo", new Type[] { typeof(Array), typeof(Int32) }); return _ArrayCopyTo; } }

        public static MethodInfo IPhpEnumerable_GetForeachEnumerator { get { if (_IPhpEnumerable_GetForeachEnumerator == null) _IPhpEnumerable_GetForeachEnumerator = typeof(PHP.Core.IPhpEnumerable).GetMethod("GetForeachEnumerator"); return _IPhpEnumerable_GetForeachEnumerator; } }
        public static MethodInfo IEnumerator_MoveNext { get { if (_IEnumerator_MoveNext == null) _IEnumerator_MoveNext = typeof(System.Collections.IEnumerator).GetMethod("MoveNext"); return _IEnumerator_MoveNext; } }

        public static MethodInfo String_IsInterned { get { if (_String_IsInterned == null) _String_IsInterned = Types.String[0].GetMethod("IsInterned"); return _String_IsInterned; } }
        public static MethodInfo String_Concat_String_String { get { if (_String_Concat_String_String == null) _String_Concat_String_String = Types.String[0].GetMethod("Concat", new Type[] { Types.String[0], Types.String[0] }); return _String_Concat_String_String; } }

#if !SILVERLIGHT
        public static MethodInfo ShellExec { get { if (_ShellExec == null) _ShellExec = typeof(PHP.Core.Execution).GetMethod("ShellExec", Types.String); return _ShellExec; } }
#endif

        public static MethodInfo PhpHashtable_Add { get { if (_PhpHashtable_Add == null) _PhpHashtable_Add = typeof(PHP.Core.PhpHashtable).GetMethod("Add", Types.Object); return _PhpHashtable_Add; } }

        public static MethodInfo PhpCallback_Invoke { get { if (_PhpCallback_Invoke == null) _PhpCallback_Invoke = typeof(PHP.Core.PhpCallback).GetMethod("Invoke",Types.ObjectArray); return _PhpCallback_Invoke; } }

        public struct DynamicCode
        {
            static Type _this { get { return typeof(PHP.Core.DynamicCode); } }
            static MethodInfo _Eval, _Assert, _PreAssert, _PostAssert, _CheckAssertion;

            public static MethodInfo Eval { get { if (_Eval == null) _Eval = _this.GetMethod("Eval"); return _Eval; } }
            public static MethodInfo Assert { get { if (_Assert == null) _Assert = _this.GetMethod("Assert"); return _Assert; } }
            public static MethodInfo PreAssert { get { if (_PreAssert == null) _PreAssert = _this.GetMethod("PreAssert"); return _PreAssert; } }
            public static MethodInfo PostAssert { get { if (_PostAssert == null) _PostAssert = _this.GetMethod("PostAssert"); return _PostAssert; } }
            public static MethodInfo CheckAssertion { get { if (_CheckAssertion == null) _CheckAssertion = _this.GetMethod("CheckAssertion"); return _CheckAssertion; } }
        }

        public struct Path
        {
            static MethodInfo _GetDirectoryName;

            public static MethodInfo GetDirectoryName { get { return _GetDirectoryName ?? (_GetDirectoryName = typeof(System.IO.Path).GetMethod("GetDirectoryName", Types.String)); } }
        }

        public struct Assembly
        {
            static MethodInfo _GetEntryAssembly;

            public static MethodInfo GetEntryAssembly { get { return _GetEntryAssembly ?? (_GetEntryAssembly = typeof(System.Reflection.Assembly).GetMethod("GetEntryAssembly", Type.EmptyTypes)); } }
        }

        public struct NamingContext
        {
            static MethodInfo _AddAlias;

            public static MethodInfo AddAlias { get { return _AddAlias ?? (_AddAlias = typeof(PHP.Core.NamingContext).GetMethod("AddAlias", new[] { Types.String[0], Types.String[0] })); } }
        }

        #endregion
    }

	#region LINQ Externs

	public static class LinqExterns
	{
		#region From LINQ assemblies

		// LINQ: TODO: Using Orcas beta2 or Silverlight preview
#if !SILVERLIGHT
		static readonly Assembly _Assembly = Assembly.LoadFrom(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)+@"\Reference Assemblies\Microsoft\Framework\v3.5\System.Core.dll");
#else
		static readonly Assembly _Assembly = Assembly.Load("System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
#endif

		public static readonly Type Sequence;
		public static readonly Type Func2;

		public static readonly Type Func2_object_object;
		public static readonly Type Func2_object_bool;
		public static readonly Type Func2_object_IEnumerable_object;

		public static readonly ConstructorInfo Func2_object_object_ctor;
		public static readonly ConstructorInfo Func2_object_bool_ctor;
		public static readonly ConstructorInfo Func2_object_IEnumerable_object_ctor;

		public static MethodInfo Where;
		public static MethodInfo ThenBy;
		public static MethodInfo ThenByDescending;
		public static MethodInfo OrderBy;
		public static MethodInfo OrderByDescending;
		public static MethodInfo Select;
		public static MethodInfo SelectMany;
		public static MethodInfo GroupBy;
		public static MethodInfo GroupByElementSel;

		static LinqExterns()
		{
			// Load stuff from LINQ assembly
			Sequence = _Assembly.GetType("System.Linq.Enumerable", true);

			Func2 = _Assembly.GetType("System.Func`2", true);
			Func2_object_object = Func2.MakeGenericType(typeof(object), typeof(object));
			Func2_object_bool = Func2.MakeGenericType(typeof(object), typeof(bool));
			Func2_object_IEnumerable_object = Func2.MakeGenericType(typeof(object), Types.IEnumerableOfObject);

			Func2_object_object_ctor = Func2_object_object.GetConstructor(Types.DelegateCtorArgs);
			Func2_object_bool_ctor = Func2_object_bool.GetConstructor(Types.DelegateCtorArgs);
			Func2_object_IEnumerable_object_ctor = Func2_object_IEnumerable_object.GetConstructor(Types.DelegateCtorArgs);

			Sequence.FindMembers(MemberTypes.Method, BindingFlags.Public | BindingFlags.Static,
				delegate(MemberInfo member, object _)
				{
					MethodInfo method = (MethodInfo)member;

					switch (method.ToString())
					{
						case "System.Collections.Generic.IEnumerable`1[TSource] Where[TSource](System.Collections.Generic.IEnumerable`1[TSource], System.Func`2[TSource,System.Boolean])":
							Where = method.MakeGenericMethod(Types.Object);
							break;

						case "System.Collections.Generic.IEnumerable`1[TResult] Select[TSource,TResult](System.Collections.Generic.IEnumerable`1[TSource], System.Func`2[TSource,TResult])":
							Select = method.MakeGenericMethod(Types.Object_Object);
							break;

						case "System.Collections.Generic.IEnumerable`1[TResult] SelectMany[TSource,TResult](System.Collections.Generic.IEnumerable`1[TSource], System.Func`2[TSource,System.Collections.Generic.IEnumerable`1[TResult]])":
							SelectMany = method.MakeGenericMethod(Types.Object_Object);
							break;

						case "System.Linq.IOrderedEnumerable`1[TSource] ThenBy[TSource,TKey](System.Linq.IOrderedEnumerable`1[TSource], System.Func`2[TSource,TKey])":
							ThenBy = method.MakeGenericMethod(Types.Object_Object);
							break;

						case "System.Linq.IOrderedEnumerable`1[TSource] ThenByDescending[TSource,TKey](System.Linq.IOrderedEnumerable`1[TSource], System.Func`2[TSource,TKey])":
							ThenByDescending = method.MakeGenericMethod(Types.Object_Object);
							break;

						case "System.Linq.IOrderedEnumerable`1[TSource] OrderBy[TSource,TKey](System.Collections.Generic.IEnumerable`1[TSource], System.Func`2[TSource,TKey])":
							OrderBy = method.MakeGenericMethod(Types.Object_Object);
							break;

						case "System.Linq.IOrderedEnumerable`1[TSource] OrderByDescending[TSource,TKey](System.Collections.Generic.IEnumerable`1[TSource], System.Func`2[TSource,TKey])":
							OrderByDescending = method.MakeGenericMethod(Types.Object_Object);
							break;

						case "System.Collections.Generic.IEnumerable`1[System.Linq.IGrouping`2[TKey,TSource]] GroupBy[TSource,TKey](System.Collections.Generic.IEnumerable`1[TSource], System.Func`2[TSource,TKey])":
							GroupBy = method.MakeGenericMethod(Types.Object_Object);
							break;

						case "System.Collections.Generic.IEnumerable`1[System.Linq.IGrouping`2[TKey,TElement]] GroupBy[TSource,TKey,TElement](System.Collections.Generic.IEnumerable`1[TSource], System.Func`2[TSource,TKey], System.Func`2[TSource,TElement])":
							GroupByElementSel = method.MakeGenericMethod(Types.Object_Object_Object);
							break;
					}
					return false;
				}, null);


			// Initialize PHP objects cache
			tupleTypes = new List<TupleInfo>();
			tupleGenerator = TupleGenerator();
		}

		#endregion
		#region From PHP assemblies

		/// <summary>
		/// Stores information about constructed tuple type
		/// </summary>
		public struct TupleInfo
		{
			public TupleInfo(Type type)
			{
				this.type = type;
				this.constructor = type.GetConstructors()[0];
				this.firstGetter = type.GetProperty("First").GetGetMethod();
				this.secondGetter = type.GetProperty("Second").GetGetMethod();
			}

			private Type type;
			private ConstructorInfo constructor;
			private MethodInfo firstGetter, secondGetter;

			// public properties
			public Type Type { get { return type; } }
			public ConstructorInfo Constructor { get { return constructor; } }
			public MethodInfo FirstGetter { get { return firstGetter; } }
			public MethodInfo SecondGetter { get { return secondGetter; } }
		}

		static List<TupleInfo> tupleTypes;
		static IEnumerator<TupleInfo> tupleGenerator;

		/// <summary>
		/// Enumerator for lazy loading of tuple types
		/// </summary>
		private static IEnumerator<TupleInfo> TupleGenerator()
		{
			Type lastType = typeof(object);
			while (true)
			{
				lastType = typeof(Tuple<,>).MakeGenericType(new Type[] { typeof(object), lastType });
				yield return new TupleInfo(lastType);
			}
		}


		/// <summary>
		/// Returns cached tuple information for specified indirection level.
		/// Indirection equals to zero means Tuple&lt;object, object&gt;>, 1 means
		/// Tuple&lt;Tuple&lt;object, object&gt;>, object&gt;> etc.
		/// </summary>
		public static TupleInfo GetTupleInfo(int indirection)
		{
			Debug.Assert(indirection >= 0);
			while (tupleTypes.Count <= indirection)
			{
				tupleGenerator.MoveNext();
				tupleTypes.Add(tupleGenerator.Current);
			}
			return tupleTypes[indirection];
		}

		#endregion
	}

	#endregion


	// TODO: mark following by [Emitted]

	/// <exclude/>
	public static class Constructors
	{
		#region Attributes

        static ConstructorInfo _ImplementsConstant, _ImplementsFunction, _CastToFalse,
            _Script, _ScriptIncluders, _ScriptIncludees, _ScriptDeclares,
			_ParamArray, _NeedsVariables, _Includes, _DTypeSpec_IntArray, _DTypeSpec_IntInt, _Debuggable, _Routine,
			_PhpEvalId, _ScriptAssembly, _PurePhpAssembly, _PhpPublicField,
            _DTypeSpec_IntArray_ByteArray, _DTypeSpec_IntInt_ByteArray, _UnknownTypeDesc;

		public static ConstructorInfo ImplementsConstant { get { if (_ImplementsConstant == null)  _ImplementsConstant = typeof(ImplementsConstantAttribute).GetConstructor(Types.String); return _ImplementsConstant; } }
		public static ConstructorInfo ImplementsFunction { get { if (_ImplementsFunction == null)  _ImplementsFunction = typeof(ImplementsFunctionAttribute).GetConstructor(Types.String); return _ImplementsFunction; } }
		public static ConstructorInfo CastToFalse { get { if (_CastToFalse == null)         _CastToFalse = typeof(CastToFalseAttribute).GetConstructor(Type.EmptyTypes); return _CastToFalse; } }
		public static ConstructorInfo ParamArray { get { if (_ParamArray == null)          _ParamArray = typeof(ParamArrayAttribute).GetConstructor(Type.EmptyTypes); return _ParamArray; } }
		public static ConstructorInfo NeedsVariables { get { if (_NeedsVariables == null)      _NeedsVariables = typeof(NeedsVariablesAttribute).GetConstructor(Type.EmptyTypes); return _NeedsVariables; } }
		public static ConstructorInfo Includes { get { if (_Includes == null)            _Includes = typeof(IncludesAttribute).GetConstructor(new Type[] { typeof(string), typeof(sbyte), typeof(bool), typeof(bool) }); return _Includes; } }
		public static ConstructorInfo DTypeSpec_IntInt { get { if (_DTypeSpec_IntInt == null) _DTypeSpec_IntInt = typeof(DTypeSpecAttribute).GetConstructor(Types.Int_Int); return _DTypeSpec_IntInt; } }
		public static ConstructorInfo DTypeSpec_IntInt_ByteArray { get { if (_DTypeSpec_IntInt_ByteArray == null) _DTypeSpec_IntInt_ByteArray = typeof(DTypeSpecAttribute).GetConstructor(new Type[] { typeof(int), typeof(int), typeof(byte[]) }); return _DTypeSpec_IntInt_ByteArray; } }
		public static ConstructorInfo DTypeSpec_IntArray { get { if (_DTypeSpec_IntArray == null) _DTypeSpec_IntArray = typeof(DTypeSpecAttribute).GetConstructor(new Type[] { typeof(int[]) }); return _DTypeSpec_IntArray; } }
		public static ConstructorInfo DTypeSpec_IntArray_ByteArray { get { if (_DTypeSpec_IntArray_ByteArray == null) _DTypeSpec_IntArray_ByteArray = typeof(DTypeSpecAttribute).GetConstructor(new Type[] { typeof(int[]), typeof(byte[]) }); return _DTypeSpec_IntArray_ByteArray; } }
		public static ConstructorInfo Debuggable { get { if (_Debuggable == null)          _Debuggable = typeof(DebuggableAttribute).GetConstructor(new Type[] { typeof(bool), typeof(bool) }); return _Debuggable; } }
        public static ConstructorInfo Script { get { return _Script ?? (_Script = typeof(ScriptAttribute).GetConstructor(new Type[] { typeof(long), typeof(string) })); } }
        public static ConstructorInfo ScriptIncluders { get { return _ScriptIncluders ?? (_ScriptIncluders = typeof(ScriptIncludersAttribute).GetConstructor(new Type[] { typeof(int[]) })); } }
        public static ConstructorInfo ScriptIncludees { get { return _ScriptIncludees ?? (_ScriptIncludees = typeof(ScriptIncludeesAttribute).GetConstructor(new Type[] { typeof(int[]), typeof(byte[]) })); } }
        public static ConstructorInfo ScriptDeclares { get { return _ScriptDeclares ?? (_ScriptDeclares = typeof(ScriptDeclaresAttribute).GetConstructor(new Type[] { typeof(int[]) })); } }
		public static ConstructorInfo PhpRoutineProperties { get { if (_Routine == null)             _Routine = typeof(RoutineAttribute).GetConstructor(Types.RoutineProperties); return _Routine; } }
		public static ConstructorInfo PhpEvalId { get { if (_PhpEvalId == null)           _PhpEvalId = typeof(PhpEvalIdAttribute).GetConstructor(Types.Int); return _PhpEvalId; } }
        public static ConstructorInfo ScriptAssembly { get { return _ScriptAssembly ?? (_ScriptAssembly = typeof(ScriptAssemblyAttribute).GetConstructor(new Type[] { typeof(bool), typeof(Type) })); } }
		public static ConstructorInfo PurePhpAssembly { get { if (_PurePhpAssembly == null)    _PurePhpAssembly = typeof(PurePhpAssemblyAttribute).GetConstructor(new Type[] { typeof(string[]) }); return _PurePhpAssembly; } }
		public static ConstructorInfo PhpPublicField { get { if (_PhpPublicField == null)    _PhpPublicField = typeof(PhpPublicFieldAttribute).GetConstructor(Types.String_Bool_Bool); return _PhpPublicField; } }
        public static ConstructorInfo UnknownTypeDesc { get { return _UnknownTypeDesc ?? (_UnknownTypeDesc = typeof(UnknownTypeDesc).GetConstructor(Type.EmptyTypes)); } }

		#endregion

		#region Attributes - CLR only
#if !SILVERLIGHT
		static ConstructorInfo _EditorBrowsable, _ThreadStatic, _STAThread, _MTAThread, _ExtensionDescriptor;

		public static ConstructorInfo EditorBrowsable { get { return _EditorBrowsable ?? (_EditorBrowsable = typeof(EditorBrowsableAttribute).GetConstructor(new Type[] { typeof(EditorBrowsableState) })); } }
		public static ConstructorInfo ThreadStatic { get { if (_ThreadStatic == null)        _ThreadStatic = typeof(ThreadStaticAttribute).GetConstructor(Type.EmptyTypes); return _ThreadStatic; } }
		public static ConstructorInfo STAThread { get { if (_STAThread == null)           _STAThread = typeof(STAThreadAttribute).GetConstructor(Type.EmptyTypes); return _STAThread; } }
		public static ConstructorInfo MTAThread { get { if (_MTAThread == null)           _MTAThread = typeof(MTAThreadAttribute).GetConstructor(Type.EmptyTypes); return _MTAThread; } }
		public static ConstructorInfo ExtensionDescriptor { get { if (_ExtensionDescriptor == null) _ExtensionDescriptor = typeof(ExtensionDescriptorAttribute).GetConstructor(new Type[] { typeof(string), typeof(string), typeof(bool) }); return _ExtensionDescriptor; } }
#endif
		#endregion

		#region Others

		static ConstructorInfo _PhpReference_Void, _PhpReference_Object,
			_PhpBytes_ByteArray, _StdClass_ScriptContext, _PhpRuntimeChain_Object_DTypeDesc,
            _RoutineDelegate, _PhpRoutineDesc_Attr_Delegate, _GetterDelegate, _SetterDelegate, _PhpScript_MainHelperDelegate, _LinqContext, _NamingContext,
            _Action_ScriptContext, _PurePhpFunction;

		public static ConstructorInfo RoutineDelegate { get { if (_RoutineDelegate == null) _RoutineDelegate = typeof(RoutineDelegate).GetConstructor(Types.DelegateCtorArgs); return _RoutineDelegate; } }
		public static ConstructorInfo GetterDelegate { get { if (_GetterDelegate == null) _GetterDelegate = typeof(GetterDelegate).GetConstructor(Types.DelegateCtorArgs); return _GetterDelegate; } }
		public static ConstructorInfo SetterDelegate { get { if (_SetterDelegate == null) _SetterDelegate = typeof(SetterDelegate).GetConstructor(Types.DelegateCtorArgs); return _SetterDelegate; } }

		public static ConstructorInfo MainHelperDelegate { get { if (_PhpScript_MainHelperDelegate == null) _PhpScript_MainHelperDelegate = typeof(MainRoutineDelegate).GetConstructor(Types.DelegateCtorArgs); return _PhpScript_MainHelperDelegate; } }

		public static ConstructorInfo PhpReference_Void { get { if (_PhpReference_Void == null) _PhpReference_Void = typeof(PhpReference).GetConstructor(Type.EmptyTypes); return _PhpReference_Void; } }
		public static ConstructorInfo PhpReference_Object { get { if (_PhpReference_Object == null) _PhpReference_Object = typeof(PhpReference).GetConstructor(Types.Object); return _PhpReference_Object; } }
		public static ConstructorInfo PhpBytes_ByteArray { get { if (_PhpBytes_ByteArray == null) _PhpBytes_ByteArray = typeof(PhpBytes).GetConstructor(new Type[] { typeof(byte[]) }); return _PhpBytes_ByteArray; } }
        public static ConstructorInfo PhpRoutineDesc_Attr_Delegate_Bool { get { if (_PhpRoutineDesc_Attr_Delegate == null) _PhpRoutineDesc_Attr_Delegate = typeof(PhpRoutineDesc).GetConstructor(new Type[] { typeof(PhpMemberAttributes), typeof(RoutineDelegate), typeof(bool) }); return _PhpRoutineDesc_Attr_Delegate; } }

		public static ConstructorInfo StdClass_ScriptContext { get { if (_StdClass_ScriptContext == null) _StdClass_ScriptContext = typeof(PHP.Library.stdClass).GetConstructor(Types.ScriptContext); return _StdClass_ScriptContext; } }
        public static ConstructorInfo NamingContext { get { return _NamingContext ?? (_NamingContext = typeof(PHP.Core.NamingContext).GetConstructor(new[] { Types.String[0], Types.Int[0] })); } }

		public static ConstructorInfo Action_ScriptContext { get { if (_Action_ScriptContext == null) _Action_ScriptContext = typeof(Action<ScriptContext>).GetConstructor(Types.DelegateCtorArgs); return _Action_ScriptContext; } }
        public static ConstructorInfo PurePhpFunction { get { return _PurePhpFunction ?? (_PurePhpFunction = typeof(PurePhpFunction).GetConstructor(new Type[] { typeof(PhpRoutineDesc), Types.String[0], typeof(MethodInfo) })); } }

		public struct PhpArray
		{
			static ConstructorInfo _Int32_Int32, _Void;

			public static ConstructorInfo Int32_Int32 { get { if (_Int32_Int32 == null) _Int32_Int32 = typeof(PHP.Core.PhpArray).GetConstructor(new Type[] { typeof(int), typeof(int) }); return _Int32_Int32; } }
			public static ConstructorInfo Void { get { if (_Void == null) _Void = typeof(PHP.Core.PhpArray).GetConstructor(Type.EmptyTypes); return _Void; } }
		}

		public struct PhpSmartReference
		{
			static ConstructorInfo _Void, _Object;

			public static ConstructorInfo Void { get { if (_Void == null) _Void = typeof(PHP.Core.PhpSmartReference).GetConstructor(Type.EmptyTypes); return _Void; } }
			public static ConstructorInfo Object { get { if (_Object == null) _Object = typeof(PHP.Core.PhpSmartReference).GetConstructor(Types.Object); return _Object; } }
		}

		public struct PhpObject
		{
			static ConstructorInfo _ScriptContext_Bool, _SerializationInfo_StreamingContext;

			public static ConstructorInfo ScriptContext_Bool { get { if (_ScriptContext_Bool == null) _ScriptContext_Bool = typeof(PHP.Core.PhpObject).GetConstructor(Types.ScriptContext_Bool); return _ScriptContext_Bool; } }
#if !SILVERLIGHT
			public static ConstructorInfo SerializationInfo_StreamingContext { get { if (_SerializationInfo_StreamingContext == null) _SerializationInfo_StreamingContext = typeof(PHP.Core.PhpObject).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Types.SerializationInfo_StreamingContext, null); return _SerializationInfo_StreamingContext; } }
#endif
		}

		public static ConstructorInfo PhpRuntimeChain_Object_DTypeDesc { get { if (_PhpRuntimeChain_Object_DTypeDesc == null) _PhpRuntimeChain_Object_DTypeDesc = typeof(PHP.Core.PhpRuntimeChain).GetConstructor(new Type[] { typeof(object), typeof(PHP.Core.Reflection.DTypeDesc) }); return _PhpRuntimeChain_Object_DTypeDesc; } }
		public static ConstructorInfo LinqContext { get { if (_LinqContext == null) _LinqContext = typeof(PHP.Core.LinqContext).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Types.LinqContextArgs, null); return _LinqContext; } }

		#endregion
	}

	/// <exclude/>
	public static class Fields
	{
		static FieldInfo _DObject_TypeDesc, _PhpReference_Value, _ScriptContext_Stack,
			_ScriptContext_Default, _ScriptContext_AutoGlobals, _ScriptContext_HttpVarsArrays,
            _PhpStack_ArgCount, _PhpStack_Context, _PhpStack_Variables, _PhpStack_NamingContext, _PhpStack_AllowProtectedCall, _PhpStack_LateStaticBindType,
	        _PhpStack_CalleeName, _Arg_Default, _Arg_DefaultType, _ScriptContext_EvalId, _ScriptContext_EvalRelativeSourcePath,
			_ScriptContext_EvalLine, _ScriptContext_EvalColumn, _PhpUserException_UserException,
			_LinqContext_outerType, _LinqContext_typeHandle, _LinqContext_variables, _LinqContext_context,
			_PhpVariable_LiteralNull, _PhpVariable_LiteralTrue, _PhpVariable_LiteralFalse;

		public static FieldInfo ScriptContext_Stack { get { if (_ScriptContext_Stack == null)  _ScriptContext_Stack = typeof(ScriptContext).GetField("Stack"); return _ScriptContext_Stack; } }
		public static FieldInfo ScriptContext_Default { get { if (_ScriptContext_Default == null)_ScriptContext_Default = typeof(ScriptContext).GetField("Default"); return _ScriptContext_Default; } }
		public static FieldInfo ScriptContext_AutoGlobals { get { if (_ScriptContext_AutoGlobals == null)  _ScriptContext_AutoGlobals = typeof(ScriptContext).GetField("AutoGlobals"); return _ScriptContext_AutoGlobals; } }
		public static FieldInfo ScriptContext_HttpVarsArrays { get { if (_ScriptContext_HttpVarsArrays == null)  _ScriptContext_HttpVarsArrays = typeof(ScriptContext).GetField("HttpVarsArrays"); return _ScriptContext_HttpVarsArrays; } }

		#region AutoGlobals

		public struct AutoGlobals
		{
            static Type _this { get { return typeof(PHP.Core.AutoGlobals); } }

			static FieldInfo _Globals;
			public static FieldInfo Globals { get { if (_Globals == null) _Globals = _this.GetField("Globals"); return _Globals; } }
			
#if !SILVERLIGHT
            static FieldInfo _Server, _Env, _Request, _Get, _Post, _Cookie, _Files, _Session, _HttpRawPostData;
			
			public static FieldInfo Server { get { if (_Server == null) _Server = _this.GetField("Server"); return _Server; } }
			public static FieldInfo Env { get { if (_Env == null) _Env = _this.GetField("Env"); return _Env; } }
			public static FieldInfo Request { get { if (_Request == null) _Request = _this.GetField("Request"); return _Request; } }
			public static FieldInfo Get { get { if (_Get == null) _Get = _this.GetField("Get"); return _Get; } }
			public static FieldInfo Post { get { if (_Post == null) _Post = _this.GetField("Post"); return _Post; } }
			public static FieldInfo Cookie { get { if (_Cookie == null) _Cookie = _this.GetField("Cookie"); return _Cookie; } }
			public static FieldInfo Files { get { if (_Files == null) _Files = _this.GetField("Files"); return _Files; } }
			public static FieldInfo Session { get { if (_Session == null) _Session = _this.GetField("Session"); return _Session; } }
            public static FieldInfo HttpRawPostData { get { return _HttpRawPostData ?? (_HttpRawPostData = _this.GetField("HttpRawPostData")); } }
#else
			static FieldInfo _Canvas;
			public static FieldInfo Canvas { get { if (_Canvas == null) _Canvas = _this.GetField("Canvas"); return _Canvas; } }

            static FieldInfo _Addr;
            public static FieldInfo Addr { get { if (_Addr == null) _Addr = _this.GetField("Addr"); return _Addr; } }
#endif
		}

		#endregion

		public static FieldInfo Arg_Default { get { if (_Arg_Default == null)          _Arg_Default = typeof(Arg).GetField("Default"); return _Arg_Default; } }
		public static FieldInfo Arg_DefaultType { get { if (_Arg_DefaultType == null)          _Arg_DefaultType = typeof(Arg).GetField("DefaultType"); return _Arg_DefaultType; } }
		public static FieldInfo DObject_TypeDesc { get { if (_DObject_TypeDesc == null)     _DObject_TypeDesc = typeof(DObject).GetField("typeDesc", BindingFlags.Instance | BindingFlags.NonPublic); return _DObject_TypeDesc; } }
		public static FieldInfo PhpReference_Value { get { if (_PhpReference_Value == null)   _PhpReference_Value = typeof(PhpReference).GetField("value"); return _PhpReference_Value; } }

		public static FieldInfo PhpStack_Context { get { if (_PhpStack_Context == null)     _PhpStack_Context = typeof(PhpStack).GetField("Context"); return _PhpStack_Context; } }
		public static FieldInfo PhpStack_ArgCount { get { if (_PhpStack_ArgCount == null)    _PhpStack_ArgCount = typeof(PhpStack).GetField("ArgCount"); return _PhpStack_ArgCount; } }
		public static FieldInfo PhpStack_Variables { get { if (_PhpStack_Variables == null)   _PhpStack_Variables = typeof(PhpStack).GetField("Variables"); return _PhpStack_Variables; } }
		public static FieldInfo PhpStack_NamingContext { get { if (_PhpStack_NamingContext == null)   _PhpStack_NamingContext = typeof(PhpStack).GetField("NamingContext"); return _PhpStack_NamingContext; } }
		public static FieldInfo PhpStack_CalleeName { get { if (_PhpStack_CalleeName == null)  _PhpStack_CalleeName = typeof(PhpStack).GetField("CalleeName"); return _PhpStack_CalleeName; } }
		public static FieldInfo PhpStack_AllowProtectedCall { get { if (_PhpStack_AllowProtectedCall == null)   _PhpStack_AllowProtectedCall = typeof(PhpStack).GetField("AllowProtectedCall"); return _PhpStack_AllowProtectedCall; } }
        public static FieldInfo PhpStack_LateStaticBindType { get { return _PhpStack_LateStaticBindType ?? (_PhpStack_LateStaticBindType = typeof(PhpStack).GetField("LateStaticBindType")); } }

		public static FieldInfo ScriptContext_EvalLine { get { if (_ScriptContext_EvalLine == null) _ScriptContext_EvalLine = typeof(ScriptContext).GetField("EvalLine"); return _ScriptContext_EvalLine; } }
		public static FieldInfo ScriptContext_EvalColumn { get { if (_ScriptContext_EvalColumn == null) _ScriptContext_EvalColumn = typeof(ScriptContext).GetField("EvalColumn"); return _ScriptContext_EvalColumn; } }
		public static FieldInfo ScriptContext_EvalId { get { if (_ScriptContext_EvalId == null) _ScriptContext_EvalId = typeof(ScriptContext).GetField("EvalId"); return _ScriptContext_EvalId; } }
		public static FieldInfo ScriptContext_EvalRelativeSourcePath { get { if (_ScriptContext_EvalRelativeSourcePath == null) _ScriptContext_EvalRelativeSourcePath = typeof(ScriptContext).GetField("EvalRelativeSourcePath"); return _ScriptContext_EvalRelativeSourcePath; } }

		public static FieldInfo PhpUserException_UserException { get { if (_PhpUserException_UserException == null) _PhpUserException_UserException = typeof(PhpUserException).GetField("UserException"); return _PhpUserException_UserException; } }

		public static FieldInfo PhpVariable_LiteralNull { get { if (_PhpVariable_LiteralNull == null) _PhpVariable_LiteralNull = typeof(PhpVariable).GetField("LiteralNull"); return _PhpVariable_LiteralNull; } }
		public static FieldInfo PhpVariable_LiteralTrue { get { if (_PhpVariable_LiteralTrue == null) _PhpVariable_LiteralTrue = typeof(PhpVariable).GetField("LiteralTrue"); return _PhpVariable_LiteralTrue; } }
		public static FieldInfo PhpVariable_LiteralFalse { get { if (_PhpVariable_LiteralFalse == null) _PhpVariable_LiteralFalse = typeof(PhpVariable).GetField("LiteralFalse"); return _PhpVariable_LiteralFalse; } }

		public static class DTypeDesc
		{
            private static Type _this { get { return typeof(Reflection.DTypeDesc); } }
			private static FieldInfo _BooleanTypeDesc, _IntergerTypeDesc, _LongIntegerTypeDesc, _DoubleTypeDesc, _StringTypeDesc,
			  _ResourceTypeDesc, _ArrayTypeDesc, _ObjectTypeDesc;


			public static FieldInfo BooleanTypeDesc { get { if (_BooleanTypeDesc == null) _BooleanTypeDesc = _this.GetField("BooleanTypeDesc"); return _BooleanTypeDesc; } }
			public static FieldInfo IntegerTypeDesc { get { if (_IntergerTypeDesc == null) _IntergerTypeDesc = _this.GetField("IntegerTypeDesc"); return _IntergerTypeDesc; } }
			public static FieldInfo LongIntegerTypeDesc { get { if (_LongIntegerTypeDesc == null) _LongIntegerTypeDesc = _this.GetField("LongIntegerTypeDesc"); return _LongIntegerTypeDesc; } }
			public static FieldInfo DoubleTypeDesc { get { if (_DoubleTypeDesc == null) _DoubleTypeDesc = _this.GetField("DoubleTypeDesc"); return _DoubleTypeDesc; } }
			public static FieldInfo StringTypeDesc { get { if (_StringTypeDesc == null) _StringTypeDesc = _this.GetField("StringTypeDesc"); return _StringTypeDesc; } }
			public static FieldInfo ResourceTypeDesc { get { if (_ResourceTypeDesc == null) _ResourceTypeDesc = _this.GetField("ResourceTypeDesc"); return _ResourceTypeDesc; } }
			public static FieldInfo ArrayTypeDesc { get { if (_ArrayTypeDesc == null) _ArrayTypeDesc = _this.GetField("ArrayTypeDesc"); return _ArrayTypeDesc; } }
			public static FieldInfo ObjectTypeDesc { get { if (_ObjectTypeDesc == null) _ObjectTypeDesc = _this.GetField("ObjectTypeDesc"); return _ObjectTypeDesc; } }
		}

        public static class UnknownTypeDesc
        {
            private static Type _this { get { return typeof(Reflection.UnknownTypeDesc); } }
            private static FieldInfo _Singleton;

            public static FieldInfo Singleton { get { return _Singleton ?? (_Singleton = _this.GetField("Singleton")); } }
        }

		public static FieldInfo LinqContext_context { get { if (_LinqContext_context == null) _LinqContext_context = typeof(Core.LinqContext).GetField("context", BindingFlags.NonPublic | BindingFlags.Instance); return _LinqContext_context; } }
		public static FieldInfo LinqContext_variables { get { if (_LinqContext_variables == null) _LinqContext_variables = typeof(Core.LinqContext).GetField("variables", BindingFlags.NonPublic | BindingFlags.Instance); return _LinqContext_variables; } }
		public static FieldInfo LinqContext_typeHandle { get { if (_LinqContext_typeHandle == null) _LinqContext_typeHandle = typeof(Core.LinqContext).GetField("typeHandle", BindingFlags.NonPublic | BindingFlags.Instance); return _LinqContext_typeHandle; } }
		public static FieldInfo LinqContext_outerType { get { if (_LinqContext_outerType == null) _LinqContext_outerType = typeof(Core.LinqContext).GetField("outerType", BindingFlags.NonPublic | BindingFlags.Instance); return _LinqContext_outerType; } }
	}

	/// <exclude/>
	public static class Properties
	{
		static PropertyInfo _ImplementsConstantCase, _PhpReference_IsSet, _PhpReference_IsAliased,
          _IDictionaryEnumerator_Key, _IDictionaryEnumerator_Value, _Type_TypeHandle, _DObject_RealObject, _DObject_RealType, _DObject_TypeDesc,
          _ClrTypeDesc_Constructor, _ScriptContext_CurrentContext, _Assembly_Location, _InsideCaller, _Delegate_Method,
          _PhpArray_InplaceCopyOnReturn;

		public static PropertyInfo ImplementsConstantCase { get { if (_ImplementsConstantCase == null) _ImplementsConstantCase = typeof(ImplementsConstantAttribute).GetProperty("CaseInsensitive"); return _ImplementsConstantCase; } }
		public static PropertyInfo PhpReference_IsSet { get { if (_PhpReference_IsSet == null) _PhpReference_IsSet = typeof(PhpReference).GetProperty("IsSet"); return _PhpReference_IsSet; } }
		public static PropertyInfo PhpReference_IsAliased { get { if (_PhpReference_IsAliased == null) _PhpReference_IsAliased = typeof(PhpReference).GetProperty("IsAliased"); return _PhpReference_IsAliased; } }

		public static PropertyInfo IDictionaryEnumerator_Key { get { if (_IDictionaryEnumerator_Key == null) _IDictionaryEnumerator_Key = typeof(System.Collections.IDictionaryEnumerator).GetProperty("Key"); return _IDictionaryEnumerator_Key; } }
		public static PropertyInfo IDictionaryEnumerator_Value { get { if (_IDictionaryEnumerator_Value == null) _IDictionaryEnumerator_Value = typeof(System.Collections.IDictionaryEnumerator).GetProperty("Value"); return _IDictionaryEnumerator_Value; } }

		public static PropertyInfo Type_TypeHandle { get { if (_Type_TypeHandle == null) _Type_TypeHandle = typeof(System.Type).GetProperty("TypeHandle"); return _Type_TypeHandle; } }

		public static PropertyInfo DObject_RealObject { get { if (_DObject_RealObject == null) _DObject_RealObject = typeof(DObject).GetProperty("RealObject"); return _DObject_RealObject; } }
        public static PropertyInfo DObject_RealType { get { return _DObject_RealType ?? (_DObject_RealType = typeof(DObject).GetProperty("RealType")); } }
        public static PropertyInfo DObject_InsideCaller { get { if (_InsideCaller == null) _InsideCaller = typeof(DObject).GetProperty("insideCaller",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); return _InsideCaller; } }
        public static PropertyInfo DObject_TypeDesc { get { return _DObject_TypeDesc ?? (_DObject_TypeDesc = typeof(DObject).GetProperty("TypeDesc", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)); } }

        public static PropertyInfo ClrTypeDesc_Constructor { get { if (_ClrTypeDesc_Constructor == null) _ClrTypeDesc_Constructor = typeof(ClrTypeDesc).GetProperty("Constructor"); return _ClrTypeDesc_Constructor; } }

		public static PropertyInfo ScriptContext_CurrentContext { get { if (_ScriptContext_CurrentContext == null) _ScriptContext_CurrentContext = typeof(ScriptContext).GetProperty("CurrentContext"); return _ScriptContext_CurrentContext; } }

        public static PropertyInfo Assembly_Location { get { return _Assembly_Location ?? (_Assembly_Location = typeof(System.Reflection.Assembly).GetProperty("Location")); } }

        public static PropertyInfo Delegate_Method { get { return _Delegate_Method ?? (_Delegate_Method = typeof(System.Delegate).GetProperty("Method")); } }

        public static PropertyInfo PhpArray_InplaceCopyOnReturn { get { return _PhpArray_InplaceCopyOnReturn ?? (_PhpArray_InplaceCopyOnReturn = typeof(PhpArray).GetProperty("InplaceCopyOnReturn")); } }
	}

	/// <exclude/>
	public class AttributeBuilders
	{
		private static CustomAttributeBuilder _PhpFinal, _PhpAbstract,
            _ImplementsTrait,
			_PhpHasInitValue, _ImplementsType, _DebuggerNonUserCode, _DebuggerHidden, _ParamArray, _Optional;

		public static CustomAttributeBuilder PhpFinal
		{
			get
			{
                return _PhpFinal ?? (_PhpFinal = new CustomAttributeBuilder(typeof(PhpFinalAttribute).GetConstructor(Type.EmptyTypes), ArrayUtils.EmptyObjects));
			}
		}

		public static CustomAttributeBuilder PhpAbstract
		{
			get
			{
                return _PhpAbstract ?? (_PhpAbstract = new CustomAttributeBuilder(typeof(PhpAbstractAttribute).GetConstructor(Type.EmptyTypes), ArrayUtils.EmptyObjects));
			}
		}

		public static CustomAttributeBuilder PhpHasInitValue
		{
			get
			{
                return _PhpHasInitValue ?? (_PhpHasInitValue = new CustomAttributeBuilder(typeof(PhpHasInitValueAttribute).GetConstructor(Type.EmptyTypes), ArrayUtils.EmptyObjects));
            }
		}

		public static CustomAttributeBuilder ImplementsType
		{
			get
			{
                return _ImplementsType ?? (_ImplementsType = new CustomAttributeBuilder(typeof(ImplementsTypeAttribute).GetConstructor(Type.EmptyTypes), ArrayUtils.EmptyObjects));
            }
		}

        public static CustomAttributeBuilder ImplementsTrait
        {
            get
            {
                return _ImplementsTrait ?? (_ImplementsTrait = new CustomAttributeBuilder(typeof(PhpTraitAttribute).GetConstructor(Type.EmptyTypes), ArrayUtils.EmptyObjects));
            }
        }

		public static CustomAttributeBuilder DebuggerNonUserCode
		{
			get
			{
                return _DebuggerNonUserCode ?? (_DebuggerNonUserCode = new CustomAttributeBuilder(typeof(DebuggerNonUserCodeAttribute).GetConstructor(Type.EmptyTypes), ArrayUtils.EmptyObjects));
			}
		}

		public static CustomAttributeBuilder DebuggerHidden
		{
			get
			{
                return _DebuggerHidden ?? (_DebuggerHidden = new CustomAttributeBuilder(typeof(DebuggerHiddenAttribute).GetConstructor(Type.EmptyTypes), ArrayUtils.EmptyObjects));
			}
		}

		public static CustomAttributeBuilder ParamArray
		{
			get
			{
				return _ParamArray ?? (_ParamArray = new CustomAttributeBuilder(typeof(ParamArrayAttribute).GetConstructor(Type.EmptyTypes), ArrayUtils.EmptyObjects));
			}
		}

		public static CustomAttributeBuilder Optional
		{
			get
			{
				return _Optional ?? (_Optional = new CustomAttributeBuilder(typeof(System.Runtime.InteropServices.OptionalAttribute).GetConstructor(Type.EmptyTypes), ArrayUtils.EmptyObjects));
			}
		}

		// CLR Only

#if !SILVERLIGHT
		private static CustomAttributeBuilder _EditorBrowsableNever, _ThreadStatic;

		public static CustomAttributeBuilder EditorBrowsableNever
		{
			get
			{
                return _EditorBrowsableNever ?? (_EditorBrowsableNever = new CustomAttributeBuilder(Constructors.EditorBrowsable, new object[] { EditorBrowsableState.Never }));
			}
		}

		public static CustomAttributeBuilder ThreadStatic
		{
			get
			{
                return _ThreadStatic ?? (_ThreadStatic = new CustomAttributeBuilder(Constructors.ThreadStatic, ArrayUtils.EmptyObjects));
			}
		}
#endif
	}
}
