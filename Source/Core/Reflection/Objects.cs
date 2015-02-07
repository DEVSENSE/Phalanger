/*

 Copyright (c) 2004-2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Dynamic;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

using PHP.Core.Emit;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core.Reflection
{
	[Serializable]
	[DebuggerNonUserCode]
	public abstract class DObject : IPhpComparable, IPhpConvertible, IPhpCloneable, IPhpPrintable,
		IPhpVariable, IDisposable, IPhpObjectGraphNode, IPhpEnumerable, ISerializable,
		IDeserializationCallback , IDynamicMetaObjectProvider
	{
        #region ObjectFlags

		/// <summary>
		/// Instance flags grouped into an enum to conserve space.
		/// </summary>
		[Flags]
		private enum ObjectFlags : byte
		{
			/// <summary>
			/// Marks visited <see cref="DObject"/> instances when printing variable contents or comparing
			/// <see cref="DObject"/>s.
			/// </summary>
			Visited = 1,

			/// <summary>
			/// <B>True</B> means there is <c>__get</c> in current call stack (used to prevent getter recursion).
			/// </summary>
			InsideGetter = 2,

			/// <summary>
			/// <B>True</B> means there is <c>__set</c> in current call stack (used to prevent setter recursion).
			/// </summary>
			InsideSetter = 4,

			/// <summary>
			/// <B>True</B> means there is <c>__call</c> in current call stack (used to prevent caller recursion).
			/// </summary>
			InsideCaller = 8,

			/// <summary>
			/// <B>True</B> means there is <c>__unset</c> in current call stack (used to prevent caller recursion).
			/// </summary>
			InsideUnsetter = 16,

			/// <summary>
			/// <B>True</B> means there is <c>__isset</c> in current call stack (used to prevent caller recursion).
			/// </summary>
			InsideIssetter = 32
		}

		#endregion

		#region SpecialMethodNames

		/// <summary>
		/// Contains special (or &quot;magic&quot;) method names.
		/// </summary>
		public static class SpecialMethodNames
		{
			/// <summary>Constructor.</summary>
			public static readonly Name Construct = new Name("__construct");

			/// <summary>Destructor.</summary>
			public static readonly Name Destruct = new Name("__destruct");

			/// <summary>Invoked when cloning instances.</summary>
			public static readonly Name Clone = new Name("__clone");

			/// <summary>Invoked when casting to string.</summary>
			public static readonly Name Tostring = new Name("__tostring");

			/// <summary>Invoked when serializing instances.</summary>
			public static readonly Name Sleep = new Name("__sleep");

			/// <summary>Invoked when deserializing instanced.</summary>
			public static readonly Name Wakeup = new Name("__wakeup");

			/// <summary>Invoked when an unknown field is read.</summary>
			public static readonly Name Get = new Name("__get");

			/// <summary>Invoked when an unknown field is written.</summary>
			public static readonly Name Set = new Name("__set");

			/// <summary>Invoked when an unknown method is called.</summary>
			public static readonly Name Call = new Name("__call");

            /// <summary>Invoked when an object is called like a function.</summary>
            public static readonly Name Invoke = new Name("__invoke");
            
            /// <summary>Invoked when an unknown method is called statically.</summary>
            public static readonly Name CallStatic = new Name("__callStatic");

			/// <summary>Invoked when an unknown field is unset.</summary>
			public static readonly Name Unset = new Name("__unset");

			/// <summary>Invoked when an unknown field is tested for being set.</summary>
			public static readonly Name Isset = new Name("__isset");
		};

		#endregion

		#region AttributedValue

		protected struct AttributedValue
		{
			public readonly object Value;
			public readonly PhpMemberAttributes Attributes;

            public string DeclaringTypeName { get { return (DeclaringType != null) ? DeclaringType.MakeSimpleName() : null; } }
            private readonly DTypeDesc DeclaringType;

            public AttributedValue(object value, PhpMemberAttributes attributes, DTypeDesc DeclaringType)
			{
				this.Value = value;
				this.Attributes = attributes;
                this.DeclaringType = DeclaringType; // needed only when value is private
			}

            public AttributedValue(object value)
                :this(value, PhpMemberAttributes.None, null)
            {
            }
		}

		#endregion

		/// <summary>
		/// Provides the <see cref="IDictionaryEnumerator"/> interface by wrapping a user-implemeted
		/// <see cref="Library.SPL.Iterator"/>.
		/// </summary>
		/// <remarks>
		/// Instances of this class are iterated when <c>foreach</c> is used on object of a class
		/// that implements <see cref="Library.SPL.Iterator"/> or <see cref="Library.SPL.IteratorAggregate"/>.
		/// </remarks>
		[Serializable]
		public class PhpIteratorEnumerator : IDictionaryEnumerator
		{
			#region Fields

			/// <summary>
			/// The underlying user iterator.
			/// </summary>
			private DObject/*!*/ iterator;

			/// <summary>
			/// Current script context.
			/// </summary>
			private ScriptContext/*!*/ context;

			/// <summary>
			/// Whether the enumerator should return values as <see cref="PhpReference"/>s.
			/// </summary>
			private bool aliasedValues;

			/// <summary>
			/// <b>true</b> if <see cref="iterator"/> points to the first element.
			/// </summary>
			private bool firstElement;

			private const string iteratorRewind = "rewind";
			private const string itetatorNext = "next";
			private const string iteratorValid = "valid";
			private const string iteratorKey = "key";
			private const string iteratorCurrent = "current";

			#endregion

			#region Construction

			/// <summary>
			/// Creates a new <see cref="PhpIteratorEnumerator"/>.
			/// </summary>
			/// <param name="iterator">The underlying user iterator.</param>
			/// <param name="context">Current script context.</param>
			/// <param name="aliasedValues">Whether the enumerator should return values as <see cref="PhpReference"/>s.</param>
			internal PhpIteratorEnumerator(DObject/*!*/ iterator, ScriptContext/*!*/ context, bool aliasedValues)
			{
				Debug.Assert(iterator != null && iterator.RealObject is Library.SPL.Iterator);

				this.iterator = iterator;
				this.context = context;
				this.aliasedValues = aliasedValues;
				Reset();
			}

			#endregion

			#region IDictionaryEnumerator Members

			/// <summary>
			/// Returns the key of the current dictionary entry.
			/// </summary>
			public object Key
			{
				get
				{
					// call Iterator::key
					context.Stack.AddFrame();
					object obj = iterator.InvokeMethod(iteratorKey, null, context);

					// dereference the result
					obj = PhpVariable.Dereference(obj);

					// special to-int & to-string conversion
					if (obj == null) return 0;
					if (PhpVariable.IsString(obj)) return obj;
					if (obj is int) return obj;
                    if (obj is long) return obj;
					if (obj is bool) return ((bool)obj ? 1 : 0);
					if (obj is double) return unchecked((int)(double)obj);

					PhpResource resource = obj as PhpResource;
					if (resource != null) return resource.ToInteger();

					// PhpArray, PhpObject
					PhpException.Throw(PhpError.Warning, CoreResources.GetString("illegal_key_return_type", iterator.TypeName));
					return 0;
				}
			}

			/// <summary>
			/// Returns the value of the current dictionary entry.
			/// </summary>
			public object Value
			{
				get
				{
					// call Iterator::current
					context.Stack.AddFrame();
					object obj = iterator.InvokeMethod(iteratorCurrent, null, context);

					PhpReference reference = obj as PhpReference;

					// copy the value when creating a new PhpReference to prevent the "phantom reference"
					if (aliasedValues)
					{
						return (reference != null ?
							reference : new PhpReference(PhpVariable.Copy(obj, CopyReason.Assigned)));
					}
					else return PhpVariable.Copy((reference != null ? reference.Value : obj), CopyReason.Assigned);
				}
			}

			/// <summary>
			/// Returns both the key (field name) and the value (field value) of the current dictionary entry.
			/// </summary>
			public DictionaryEntry Entry
			{
				get
				{ return new DictionaryEntry(Key, Value); }
			}

			#endregion

			#region IEnumerator Members

			/// <summary>
			/// Returns both the key (field name) and the value (field value) of the current dictionary entry.
			/// </summary>
			public object Current
			{
				get
				{ return new DictionaryEntry(Key, Value); }
			}

			/// <summary>
			/// Sets the enumerator to its initial position, which is before the first element in the collection.
			/// </summary>
			public void Reset()
			{
				// call Iterator::rewind
				context.Stack.AddFrame();
				iterator.InvokeMethod(iteratorRewind, null, context);

				firstElement = true;
			}

			/// <summary>
			/// Advances the enumerator to the next element of the collection.
			/// </summary>
			/// <returns><B>true</B> if the enumerator was successfully advanced to the next element; <B>false</B>
			/// if the enumerator has passed the end of the collection.</returns>
			public bool MoveNext()
			{
				if (!firstElement)
				{
					// call Iterator::next
					context.Stack.AddFrame();
					iterator.InvokeMethod(itetatorNext, null, context);
				}
				else firstElement = false;

				// call Iterator::valid
				context.Stack.AddFrame();
				return !PhpVariable.IsEmpty(iterator.InvokeMethod(iteratorValid, null, context));
			}

			#endregion
		}

		#region Fields and Properties

		/// <summary>
		/// Ordered hashtable containing fields added at runtime (so called RT fields).
		/// </summary>
		/// <remarks>
		/// This field is initialized in a lazy manner. It is <B>null</B> until the first RT field is created.
		/// </remarks>
		public PhpArray RuntimeFields;

		/// <summary>
		/// <B>True</B> iff this instance has already been disposed off.
		/// </summary>
		internal bool IsDisposed { get { return (disposed != 0); } }

		[NonSerialized]
		protected int disposed;

		/// <summary>
		/// <B>True</B> iff this instance can be finalized.
		/// </summary>
		/// <remarks>
		/// If a derived class practices resurrection, this property must be overriden in order to
		/// prevent this class's finalizer from calling <c>__destruct</c>
		/// </remarks>
		protected virtual bool ReadyForDisposal { get { return true; } }

		public DTypeDesc/*!*/ TypeDesc
		{
			get
			{
				if (typeDesc == null) typeDesc = DTypeDesc.Create(RealObject.GetType());
				return typeDesc;
			}
		}

        /// <summary>
        /// Real <see cref="Type"/> of the object.
        /// </summary>
        public Type/*!*/RealType { get { return TypeDesc.RealType; } }

		/// <summary>
		/// Caches the type desc describing this instance's type.
		/// </summary>
		[NonSerialized]
		protected DTypeDesc typeDesc;

        /// <summary>
        /// The real CLR object contained by the DObject.
        /// </summary>
		public abstract object/*!*/ RealObject { get; }

        /// <summary>
        /// The instance passed to method and property invocation.
        /// </summary>
        public abstract object/*!*/ InstanceObject { get; }

		public string TypeName
		{
			get
			{
				return TypeDesc.MakeFullName();
			}
		}

		/// <summary>
		/// PHP name of this type. Default result of to-string conversion.
		/// </summary>
		public const string PhpTypeName = "object";

        /// <summary>
        /// Used by print_r.
        /// </summary>
        public const string PrintablePhpTypeName = "Object";

		#region PHP-specific Object Flags

		[NonSerialized]
		private ObjectFlags flags;

		/// <summary>
		/// Marks visited <see cref="DObject"/> instances when printing variable contents or comparing
		/// <see cref="DObject"/>s.
		/// </summary>
		private bool visited
		{
			get { return (flags & ObjectFlags.Visited) != 0; }
			set { if (value) flags |= ObjectFlags.Visited; else flags &= ~ObjectFlags.Visited; }
		}

		/// <summary>
		/// <B>True</B> means there is <c>__get</c> in current call stack (used to prevent getter recursion).
		/// </summary>
		private bool insideGetter
		{
			get { return (flags & ObjectFlags.InsideGetter) != 0; }
			set { if (value) flags |= ObjectFlags.InsideGetter; else flags &= ~ObjectFlags.InsideGetter; }
		}

		/// <summary>
		/// <B>True</B> means there is <c>__set</c> in current call stack (used to prevent setter recursion).
		/// </summary>
		private bool insideSetter
		{
			get { return (flags & ObjectFlags.InsideSetter) != 0; }
			set { if (value) flags |= ObjectFlags.InsideSetter; else flags &= ~ObjectFlags.InsideSetter; }
		}

		/// <summary>
		/// <B>True</B> means there is <c>__call</c> in current call stack (used to prevent caller recursion).
		/// </summary>
		public bool insideCaller/*TODO:(MB) change back to private when IDynamicMetaObjectProvider implemented on DObject*/
		{
			get { return (flags & ObjectFlags.InsideCaller) != 0; }
			set { if (value) flags |= ObjectFlags.InsideCaller; else flags &= ~ObjectFlags.InsideCaller; }
		}

		/// <summary>
		/// <B>True</B> means there is <c>__unset</c> in current call stack (used to prevent setter recursion).
		/// </summary>
		private bool insideUnsetter
		{
			get { return (flags & ObjectFlags.InsideUnsetter) != 0; }
			set { if (value) flags |= ObjectFlags.InsideUnsetter; else flags &= ~ObjectFlags.InsideUnsetter; }
		}

		/// <summary>
		/// <B>True</B> means there is <c>__isset</c> in current call stack (used to prevent setter recursion).
		/// </summary>
		private bool insideIssetter
		{
			get { return (flags & ObjectFlags.InsideIssetter) != 0; }
			set { if (value) flags |= ObjectFlags.InsideIssetter; else flags &= ~ObjectFlags.InsideIssetter; }
		}

		#endregion

		#endregion

		#region Construction

		protected DObject()
		{ }

		protected DObject(DTypeDesc/*!*/ typeDesc)
		{
			this.typeDesc = typeDesc;
		}

		#endregion

		#region InvokeConstructor, InvokeMethod, GetMethodHandle

		/// <summary>
		/// Invokes a PHP constructor (<c>__construct</c> or PHP 4 style constructor) of this instance's class.
		/// </summary>
		/// <param name="context">Current <see cref="ScriptContext"/>.</param>
		/// <param name="caller"><see cref="DTypeDesc"/> of the object that requests the invocation.
		/// </param>
        [Emitted]
        public void InvokeConstructor(ScriptContext context, DTypeDesc caller)
        {
            // search for __construct and the PHP 4 style constructor - a method with the same name as its declaring class
            PhpStack stack = context.Stack;

            DRoutineDesc method = null; // constructor candidate

            bool seen_context = false;
            GetMemberResult get_res = GetMemberResult.NotFound;

            for (DTypeDesc type_desc = TypeDesc; type_desc != null; type_desc = type_desc.Base as PhpTypeDesc)
            {
                if (type_desc == caller) seen_context = true;

                method = type_desc.GetMethod(SpecialMethodNames.Construct);
                if (method == null)
                    method = type_desc.GetMethod(new Name(type_desc.MakeSimpleName()));

                if (method == null)
                    continue;   // try the base class

                if (method.IsPublic)
                {
                    get_res = GetMemberResult.OK;
                }
                else if (method.IsProtected)
                {
                    if (seen_context || (caller != null && type_desc.IsRelatedTo(caller)))
                        get_res = GetMemberResult.OK;
                    else
                        get_res = GetMemberResult.BadVisibility;
                }
                else if (method.IsPrivate)
                {
                    if (caller == type_desc)
                        get_res = GetMemberResult.OK;
                    else
                        get_res = GetMemberResult.BadVisibility;
                }

                // constructor must not be static (this error is reported first on PHP)
                if (method.IsStatic)
                {
                    stack.RemoveFrame();

                    PhpException.Throw(PhpError.Error, CoreResources.GetString("constructor_cannot_be_static", method.DeclaringType.MakeFullName(), method.MakeFullName()));
                    return;
                }

                // check the visibility flag
                if (get_res == GetMemberResult.BadVisibility)
                {
                    stack.RemoveFrame();

                    ThrowMethodVisibilityError(method, caller);
                    return;
                }

                // ok
                method.Invoke(this, stack, caller);
                return;
            }

            // no 'constructor' method was found
            stack.RemoveFrame();
        }

		/// <summary>
		/// Invokes a method (both instance and static) on this object.
		/// </summary>
		/// <param name="name">The name of the method to invoke.</param>
		/// <param name="caller">The <see cref="Type"/> of the object that request the invocation or <B>null</B>
		/// if it should be determined lazily.</param>
		/// <param name="context">Current <see cref="ScriptContext"/>.</param>
		/// <returns>The return value of the method (might be a <see cref="PhpReference"/>).</returns>
		/// <remarks>
		/// Arguments are expected on the current <see cref="PHP.Core.ScriptContext.Stack"/>.
		/// </remarks>
		[Emitted]
		public object InvokeMethod(string name, DTypeDesc caller, ScriptContext context)
		{
			// obtain the appropriate method table
			string caller_arg = null;
			DTypeDesc type_desc = TypeDesc;

			// perform method lookup
			DRoutineDesc method;
			GetMemberResult result = type_desc.GetMethod(new Name(name), caller, out method);

			PhpStack stack = context.Stack;

			if (result == GetMemberResult.NotFound)
			{
				// if not found, perform __call 'magic' method lookup, but not if we are already inside a __call
				if (insideCaller ||
					(result = type_desc.GetMethod(SpecialMethodNames.Call, caller, out method)) ==
					GetMemberResult.NotFound)
				{
					stack.RemoveFrame();
					PhpException.UndefinedMethodCalled(TypeName, name);
					return null;
				}

				caller_arg = name;
			}

			// throw an error if the method was found but the caller is not allowed to call it due to its visibility
			if (result == GetMemberResult.BadVisibility)
			{
				stack.RemoveFrame();
				ThrowMethodVisibilityError(method, caller);
				return null;
			}

			if (caller_arg != null)
			{
				PhpArray args = stack.CollectFrame();

				// original parameters are passed to __call in an array as the second parameter
				stack.AddFrame(caller_arg, args);
				object ret_val = null;

				insideCaller = true;
				try
				{
					ret_val = method.Invoke(this, stack, caller);
				}
				finally
				{
					insideCaller = false;
				}
				return ret_val;
			}
			else
			{
				// we are invoking the method
				return method.Invoke(this, stack, caller);
			}
		}

		/// <summary>
		/// Returns a handle to this object's method or <B>null</B> if an error (not found or bad visibility) occurs.
		/// </summary>
		/// <param name="name">The name of the method.</param>
		/// <param name="caller">The <see cref="Type"/> of the object that request the operation or <B>null</B>
		/// if it should be determined lazily.</param>
		/// <param name="quiet">If <B>true</B>, no errors should be thrown.</param>
		/// <param name="isCallerMethod">Receives <B>true</B> if the returned <see cref="DRoutineDesc"/> represents
		/// the <c>__call</c> method, <B>false</B> otherwise.</param>
		/// <returns>The handle or <B>null</B>.</returns>
		public DRoutineDesc GetMethodDesc(string name, DTypeDesc caller, bool quiet, out bool isCallerMethod)
		{
			isCallerMethod = false;
			DTypeDesc type_desc = TypeDesc;

			// perform method lookup
			DRoutineDesc method;
			GetMemberResult result = type_desc.GetMethod(new Name(name), caller, out method);

			if (result == GetMemberResult.NotFound)
			{
				// if not found, perform __call 'magic' method lookup
				if ((result = type_desc.GetMethod(SpecialMethodNames.Call, caller, out method)) ==
					GetMemberResult.NotFound)
				{
					if (!quiet) PhpException.UndefinedMethodCalled(TypeName, name);
					return null;
				}

				isCallerMethod = true;
			}

			// throw an error if the method was found but the caller is not allowed to call it due to its visibility
			if (result == GetMemberResult.BadVisibility)
			{
				if (!quiet) ThrowMethodVisibilityError(method, caller);
				return null;
			}

			return method;
		}

		#endregion

		#region PropertyReadHandler, PropertyWriteHandler, PropertyUnsetHandler, PropertyIssetHandler

		private object InvokeSpecialMethod(Name methodName, ObjectFlags recursionFlag, DTypeDesc caller,
			out bool found, params object[] args)
		{
			// perform the 'magic' method lookup, but not if we are already inside it
			DRoutineDesc method;
			GetMemberResult result;

			if ((flags & recursionFlag) == recursionFlag ||
				(result = TypeDesc.GetMethod(methodName, caller, out method)) == GetMemberResult.NotFound)
			{
				found = false;
				return null;
			}

			found = true;

			// throw an error if the method was found but the caller is not allowed to call it due to its visibility
			if (result == GetMemberResult.BadVisibility)
			{
				ThrowMethodVisibilityError(method, caller);
				return null;
			}

			PhpStack stack = ScriptContext.CurrentContext.Stack;
			stack.AddFrame(args);

			// invoke the getter
			flags |= recursionFlag;
			try
			{
				return method.Invoke(this, stack, caller);
			}
			finally
			{
				flags &= ~recursionFlag;
			}
		}

		/// <summary>
		/// Invoked when an unknown property is read.
		/// </summary>
		/// <remarks>Override in order to get custom property reading behavior.</remarks>
		protected virtual object PropertyReadHandler(string name, DTypeDesc caller, out bool handled)
		{
			return InvokeSpecialMethod(SpecialMethodNames.Get, ObjectFlags.InsideGetter, caller, out handled, name);
		}

		/// <summary>
		/// Invoked when an unknown property is written (<paramref name="name"/> can be a <see cref="RuntimeChainElement"/>).
		/// </summary>
		protected virtual bool PropertyWriteHandler(object name, object value, DTypeDesc caller)
		{
			bool handled;
			InvokeSpecialMethod(SpecialMethodNames.Set, ObjectFlags.InsideSetter, caller, out handled, name, value);

			return handled;
		}

		/// <summary>
		/// Invoked when an unknown property is unset.
		/// </summary>
		protected virtual bool PropertyUnsetHandler(string name, DTypeDesc caller)
		{
			bool handled;
			InvokeSpecialMethod(SpecialMethodNames.Unset, ObjectFlags.InsideUnsetter, caller, out handled, name);

			return handled;
		}

		/// <summary>
		/// Invoked when an unknown property is tested for being set.
		/// </summary>
		public virtual object PropertyIssetHandler(string name, DTypeDesc caller, out bool handled)
		{
			return InvokeSpecialMethod(SpecialMethodNames.Isset, ObjectFlags.InsideIssetter, caller, out handled, name);
		}

		#endregion

		#region GetProperty, GetPropertyRef, GetRuntimeField, InvokeGetterRef

		/// <summary>
		/// Gets the value of an instance property (both CT and RT).
		/// </summary>
		/// <param name="name">The property name.</param>
		/// <param name="caller">Class context of the code that requests the retrieval.</param>
		/// <returns>The property value.</returns>
		public object GetProperty(string name, DTypeDesc caller)
		{
			return GetProperty(name, caller, false);
		}

		/// <summary>
		/// Gets the value of an instance property (both CT and RT).
		/// </summary>
		/// <param name="name">The property name.</param>
		/// <param name="caller">Class context of the code that requests the retrieval.</param>
		/// <param name="issetSemantics">If <B>true</B>, the operation should have the <c>isset</c> semantics - 
		/// it should try to call <c>__isset</c> instead of <c>__get</c> and throw any exceptions when the property
		/// is not found.</param>
		/// <returns>The property value.</returns>
		public object GetProperty(string name, DTypeDesc caller, bool issetSemantics)
		{
			// perform property lookup
			DPropertyDesc property;
			GetMemberResult result = TypeDesc.GetInstanceProperty(new VariableName(name), caller, out property);

			switch (result)
			{
				case GetMemberResult.OK:
				{
					object value = property.Get(this);
					PhpReference reference = value as PhpReference;

					if (reference != null && !reference.IsSet)
					{
						// the property is CT but has been unset
						if (issetSemantics)
						{
							bool handled;
							return PropertyIssetHandler(name, caller, out handled);
						}
						else return GetRuntimeField(name, caller);
					}
					else return value;
				}

				case GetMemberResult.NotFound:
				{
					if (issetSemantics)
					{
						var namekey = new IntStringKey(name);
                        object value;
						if (RuntimeFields != null && RuntimeFields.TryGetValue(namekey, out value))
						{
							return value;
						}
						else
						{
							bool handled;
							return PropertyIssetHandler(name, caller, out handled);
						}
					}
					else return GetRuntimeField(name, caller);
				}

				case GetMemberResult.BadVisibility:
				{
					// throw an error if the property was found but the caller is not allowed to access it due to its visibility
					ThrowPropertyVisibilityError(name, property, caller);
					return null;
				}
			}
			return null;
		}

		/// <summary>
		/// Gets the value of an instance property (both CT and RT) as a <see cref="PhpReference"/>.
		/// </summary>
		/// <param name="name">The property name.</param>
		/// <param name="caller">Class context of the code that requests the retrieval.</param>
		/// <returns>The property value (never <B>null</B>).</returns>
		public PhpReference/*!*/ GetPropertyRef(string name, DTypeDesc caller)
		{
			PhpReference reference;
			object value;
			bool getter_exists;

			// search in CT fields
			DPropertyDesc property;
			GetMemberResult result = TypeDesc.GetInstanceProperty(new VariableName(name), caller, out property);

			if (result == GetMemberResult.BadVisibility)
			{
				ThrowPropertyVisibilityError(name, property, caller);
				return new PhpReference();
			}

			// if the CT property a PhpReference?
			if (result == GetMemberResult.OK)
			{
				value = property.Get(this);
				reference = value as PhpReference;

				if (reference != null && reference.IsSet)
				{
					reference.IsAliased = true;
					return reference;
				}

				// the CT property has been unset -> try to invoke __get
				PhpReference get_ref = InvokeGetterRef(name, caller, out getter_exists);
				if (getter_exists) return (get_ref == null ? new PhpReference() : get_ref);

				if (reference == null)
				{
					reference = new PhpReference(value);
					property.Set(this, reference);
				}
				else
				{
					reference.IsAliased = true;
					reference.IsSet = true;
				}

				return reference;
			}

			// search in RT fields

			if (RuntimeFields != null && RuntimeFields.ContainsKey(name))
            {
                var namekey = new IntStringKey(name);
                return RuntimeFields.table._ensure_item_ref(ref namekey, RuntimeFields);
            }

			// property is not present -> try to invoke __get
			reference = InvokeGetterRef(name, caller, out getter_exists);
			if (getter_exists) return (reference == null) ? new PhpReference() : reference;

			// (no notice/warning/error thrown by PHP)

			// add the field
			reference = new PhpReference();
            if (RuntimeFields == null) RuntimeFields = new PhpArray();
			RuntimeFields[name] = reference;

			return reference;
		}

		/// <summary>
		/// Gets the value of an instance field when CT property lookup has failed.
		/// </summary>
		/// <param name="name">The field name.</param>
		/// <param name="caller">Class context of the code that requests the retrieval.</param>
		/// <returns>The field value.</returns>
		/// <remarks>This is merely a helper method called from <see cref="GetProperty"/>.</remarks>
		public object GetRuntimeField(string name, DTypeDesc caller)
		{
			object value;
            var namekey = new IntStringKey(name);
            if (RuntimeFields == null || !RuntimeFields.table.TryGetValue(namekey, out value))
            {
                // invoke __get
                bool handled;
                value = PropertyReadHandler(name, caller, out handled);

                if (!handled)
                {
                    PhpException.Throw(PhpError.Notice, CoreResources.GetString("undefined_property_accessed",
                        TypeName, name));
                    return null;
                }
            }
            
            return value;
		}

		/// <summary>
		/// Invokes the <c>__get</c> handler of this instance and returns a reference.
		/// </summary>
		/// <param name="name">The name of the property whose value is requested.</param>
		/// <param name="caller">The class context, in which the operation should be performed.</param>
		/// <param name="getterExists">When this method returns, contains <B>true</B> if the getter exists and
		/// an attempt was made to invoke it, and <B>false</B> if this instance does not define the getter.</param>
		/// <returns>A reference representing the value returned by getter or <B>null</B> if an error occured or
		/// this instance does not have the overloaded field getter (<c>__get</c>).</returns>
		/// <remarks>This method is called by <c>*Ref</c> operators to retrieve a reference to a field
		/// when the instance has an overloaded field getter.</remarks>
		internal PhpReference InvokeGetterRef(string name, DTypeDesc caller, out bool getterExists)
		{
			object ret_val = PropertyReadHandler(name, caller, out getterExists);
			if (getterExists)
			{
				PhpReference reference = ret_val as PhpReference;
				return (reference != null ? reference : new PhpSmartReference(ret_val));
			}
			else return null;
		}

		#endregion

		#region SetProperty, SetPropertyDirect, SetRuntimeField, InvokeSetter


		/// <summary>
		/// Sets the value of an instance property (both CT and RT).
		/// </summary>
		/// <param name="name">The property name.</param>
		/// <param name="value">The new property value.</param>
		/// <param name="caller">Class context of the code that requests the operation.</param>
		public void SetProperty(string name, object value, DTypeDesc caller)
		{
			// perform field lookup
			DPropertyDesc property;
			GetMemberResult result = TypeDesc.GetInstanceProperty(new VariableName(name), caller, out property);

			PhpReference reference = null;
			switch (result)
			{
				case GetMemberResult.OK:
				{
					if ((reference = property.Set(this, value)) != null)
					{
						// the property is CT but has been unset
						goto case GetMemberResult.NotFound;
					}
					break;
				}

				case GetMemberResult.NotFound:
				{
					SetRuntimeField(name, value, property, reference, caller);
					return;
				}

				case GetMemberResult.BadVisibility:
				{
					// throw an error if the field was found but the caller is not allowed to access it due to its visibility
					ThrowPropertyVisibilityError(name, property, caller);
					return;
				}
			}
		}

		/// <summary>
		/// Sets the value of an instance property (both CT and RT) without visibility checks and without attempting
		/// to invoke <c>__set</c>.
		/// </summary>
		/// <param name="name">The property name.</param>
		/// <param name="value">The new property value.</param>
		/// <remarks>
		/// This method is useful for deserialization and marshaling purposes.
		/// </remarks>
		public void SetPropertyDirect(object name, object value)
		{
			DPropertyDesc property;
			string name_str = Convert.ObjectToString(name);

			// check whether it is a CT property
			if (TypeDesc.GetInstanceProperty(new VariableName(name_str), TypeDesc, out property)
				!= GetMemberResult.OK)
			{
				// no, it is an RT field
                if (RuntimeFields == null) RuntimeFields = new PhpArray();
				RuntimeFields[name_str] = value;
				return;
			}

			// yes, it is a CT property
			PhpReference reference = value as PhpReference;
			if (reference != null)
			{
				property.Set(this, reference);
			}
			else
			{
				object old_value = property.Get(this);

				if (reference != null)
				{
					reference.Value = value;
					reference.IsSet = true;
				}
				else property.Set(this, new PhpSmartReference(value));
			}
		}

		/// <summary>
		/// Sets the value of an instance field when CT property lookup has failed.
		/// </summary>
		/// <param name="name">The field name.</param>
		/// <param name="value">The new field value.</param>
		/// <param name="ctCandidate">CT property candidate (may be <B>null</B>).</param>
		/// <param name="ctCandidateValue">CT property candidate value (is <B>null</B> iff <paramref name="ctCandidate"/>
		/// is <B>null</B>).</param>
		/// <param name="caller">Class context of the code that requests the retrieval.</param>
		/// <remarks>This is merely a helper method called from <see cref="SetProperty"/>.</remarks>
		internal void SetRuntimeField(string name, object value, DPropertyDesc ctCandidate, PhpReference ctCandidateValue,
			DTypeDesc caller)
		{
			PhpReference reference = value as PhpReference;

            object elementvalue;
            if (RuntimeFields == null || !RuntimeFields.TryGetValue(name, out elementvalue))
			{
				if (reference != null)
				{
					// here comes the controversial part - let's mimic PHP and make it clear with the Zend guys
					bool getter_exists;
					PhpReference get_ref = InvokeGetterRef(name, caller, out getter_exists);

					if (getter_exists)
					{
						if (get_ref != null)
						{
							get_ref.Value = reference.value;
							get_ref.IsSet = true;
						}
						return;
					}
				}

				if (!PropertyWriteHandler(name, value, caller))
				{
					// not handled -> revive a CT property or add a runtime field
					if (ctCandidate != null)
					{
						ctCandidateValue.IsSet = true;
						
						if (reference != null) ctCandidate.Set(this, reference);
						else ctCandidateValue.Value = value;
					}
					else
					{
                        if (RuntimeFields == null) RuntimeFields = new PhpArray();
						RuntimeFields[name] = value;
					}
				}
			}
			else
			{
				// if the new value is a PhpReference, it is always directly written to the RuntimeFields table
				if (reference != null) RuntimeFields[name] = reference;
				else
				{
					// if the new value is not a PhpReference, check has to be made whether the original field value
					// was a PhpReference
                    reference = elementvalue as PhpReference;
					if (reference != null) reference.Value = value;
                    else RuntimeFields[name] = value;
				}
			}
		}

		/// <summary>
		/// Invokes the <c>__set</c> handler of this instance with a &quot;setter chain&quot;.
		/// </summary>
		/// <param name="chain">A linked list of <see cref="RuntimeChainElement"/>s that should be passed
		/// to the setter.</param>
		/// <param name="value">A value that should be assigned to the last chain element.</param>
		/// <returns><B>true</B> if the setter was successfully called, <B>false</B> otherwise.</returns>
		/// <remarks>This method is intended for system classes, especially for classes in managed wrappers.</remarks>
		internal bool InvokeSetter(RuntimeChainElement chain, object value)
		{
			return PropertyWriteHandler(chain, value, TypeDesc);
		}

		#endregion

		#region UnsetProperty

		/// <summary>
		/// Unsets an instance property (both CT and RT).
		/// </summary>
		/// <param name="name">The property name.</param>
		/// <param name="caller">Class context of the code that requests the operation.</param>
		public void UnsetProperty(string name, DTypeDesc caller)
		{
			// search in CT properties
			DPropertyDesc property;
			GetMemberResult get_res = TypeDesc.GetInstanceProperty(new VariableName(name), caller, out property);

			if (get_res == GetMemberResult.BadVisibility)
			{
				ThrowPropertyVisibilityError(name, property, caller);
				return;
			}

			// was a CT property found?
			if (get_res == GetMemberResult.OK)
			{
				// set the new reference's IsSet property to false
				PhpSmartReference new_ref = new PhpSmartReference();
				new_ref.IsSet = false;
				property.Set(this, new_ref);
			}
			else
			{
				// search in RT fields
				if (RuntimeFields == null || !RuntimeFields.table.Remove(new IntStringKey(name)))
				{
					// invoke the unset handler (will call the __unset special method)
					PropertyUnsetHandler(name, caller);
				}
			}
		}

		#endregion

		#region CloneObject

		protected virtual DObject CloneObjectInternal(DTypeDesc caller, ScriptContext context, bool deepCopyFields)
		{
			DTypeDesc type_desc = TypeDesc;

			DObject copy = (DObject)type_desc.New(context);

			// copy CT properties
			foreach (KeyValuePair<VariableName, DPropertyDesc> pair in type_desc.EnumerateProperties())
			{
				if (pair.Value.IsStatic) continue;
				object value = pair.Value.Get(this);

				if (deepCopyFields) value = PhpVariable.DeepCopy(value);
				else value = PhpVariable.Copy(value, CopyReason.Assigned);

				pair.Value.Set(copy, value);
			}

			// copy RT fields
			if (RuntimeFields != null)
			{
                copy.RuntimeFields = new PhpArray(RuntimeFields.Count);
                using (var fields = RuntimeFields.GetFastEnumerator())
                    while (fields.MoveNext())
				    {
					    copy.RuntimeFields.Add(fields.CurrentKey,
                            deepCopyFields ?
						        PhpVariable.DeepCopy(fields.CurrentValue) :
                                PhpVariable.Copy(fields.CurrentValue, CopyReason.Assigned));
				    }
			}

			return copy;
		}

		/// <summary>
		/// Creates a clone of this instance, which is either a deep copy or a <c>clone</c>-style
		/// copy according to the <paramref name="deepCopyFields"/> parameter.
		/// </summary>
		/// <param name="caller">Class context of the code that requests the lookup.</param>
		/// <param name="context">Current <see cref="ScriptContext"/>.</param>
		/// <param name="deepCopyFields">If <B>true</B>, fields will be deep-copied, if <B>false</B>,
		/// fields will be copied by assignment.</param>
		/// <returns>The clone.</returns>
		public object CloneObject(DTypeDesc caller, ScriptContext context, bool deepCopyFields)
		{
			DObject copy;

			ICloneable cloneable = RealObject as ICloneable;
			if (cloneable != null)
			{
				// use real object's Clone if available
				copy = ClrObject.WrapDynamic(cloneable.Clone()) as DObject;
			}
			else copy = CloneObjectInternal(caller, context, deepCopyFields);

			if (copy != null)
			{
				// try to invoke __clone on the copy
				DRoutineDesc method;
				switch (copy.TypeDesc.GetMethod(SpecialMethodNames.Clone, caller, out method))
				{
					case GetMemberResult.BadVisibility:
					{
						ThrowMethodVisibilityError(method, caller);
						break;
					}

					case GetMemberResult.OK:
					{
						// __clone must not be static
						if (method.IsStatic)
						{
							PhpException.Throw(PhpError.Error, CoreResources.GetString("clone_cannot_be_static",
								method.DeclaringType.MakeFullName()));
						}
						else
						{
							method.Invoke(copy, context.Stack, caller);
						}
						break;
					}

					// if not found, nothing happens
				}
			}
			return copy;
		}

		#endregion

		#region ThrowMethodVisibilityError, ThrowPropertyVisibilityError

		/// <summary>
		/// Throws a 'Protected method called' or 'Private method called' <see cref="PhpException"/>.
		/// </summary>
		/// <param name="method">The <see cref="DRoutineDesc"/>.</param>
		/// <param name="caller">The caller that was passed to method lookup or <B>null</B>
		/// if it should be determined by this method (by tracing the stack.</param>
		/// <remarks>
		/// This method is intended to be called after <see cref="DTypeDesc.GetMethod"/> has returned
		/// <see cref="GetMemberResult.BadVisibility"/> while performing a method lookup.
		/// </remarks>
		internal static void ThrowMethodVisibilityError(DRoutineDesc method, DTypeDesc caller)
		{
			if (method.IsProtected)
			{
				PhpException.Throw(PhpError.Error, CoreResources.GetString("protected_method_called",
					method.DeclaringType.MakeFullName(),
					method.MakeFullName(),
					(caller == null ? String.Empty : caller.MakeFullName())));
			}
			else if (method.IsPrivate)
			{
				PhpException.Throw(PhpError.Error, CoreResources.GetString("private_method_called",
					method.DeclaringType.MakeFullName(),
					method.MakeFullName(),
					(caller == null ? String.Empty : caller.MakeFullName())));
			}
		}

		/// <summary>
		/// Throws a 'Cannot access protected property' or 'Cannot access private property' <see cref="PhpException"/>.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <param name="property">The <see cref="DPropertyDesc"/>.</param>
		/// <param name="caller">The caller.</param>
		internal static void ThrowPropertyVisibilityError(string/*!*/ name, DPropertyDesc/*!*/ property, DTypeDesc/*!*/ caller)
		{
			PhpException.PropertyNotAccessible(
				property.DeclaringType.MakeFullName(),
				name.ToString(),
				(caller == null ? String.Empty : caller.MakeFullName()),
				property.IsProtected);
		}

		#endregion

		#region Sleep, Wakeup

		/// <summary>
		/// Tries to invoke <c>__wakeup</c> on this instance.
		/// </summary>
		/// <param name="caller"><see cref="Type"/> of the object that requests the invocation or <B>null</B> if
		/// it should be determined lazily.</param>
		/// <param name="context">Current <see cref="ScriptContext"/>.</param>
		public void Wakeup(DTypeDesc caller, ScriptContext context)
		{
			DRoutineDesc method;
			switch (TypeDesc.GetMethod(SpecialMethodNames.Wakeup, caller, out method))
			{
				case GetMemberResult.BadVisibility:
				{
					ThrowMethodVisibilityError(method, caller);
					break;
				}

				case GetMemberResult.OK:
				{
					context.Stack.AddFrame();
					method.Invoke(this, context.Stack, caller);
					break;
				}
			}
		}

		/// <summary>
		/// Tries to invoke <c>__sleep</c> on this instance.
		/// </summary>
		/// <param name="caller"><see cref="Type"/> of the object that requests the invocation or <B>null</B> if
		/// it should be determined lazily.</param>
		/// <param name="context">Current <see cref="ScriptContext"/>.</param>
		/// <param name="sleepCalled">Receives <B>true</B> if <c>__sleep</c> was called, <B>false</B> otherwise.</param>
		/// <returns>The <c>__sleep</c> result (an array containing names of fields that should be preserved) or
		/// <B>null</B> if <c>__sleep</c> was not callable or did not return an array.</returns>
		/// <remarks>
		/// There are three possible results of this method. If <paramref name="sleepCalled"/> is <B>false</B>,
		/// then <c>__sleep</c> was not called because it was not found or invisible for the <paramref name="caller"/>.
		/// Otherwise, the return value is valid and can be either <B>null</B> (<c>__sleep</c> did not return
		/// an array) or a <see cref="PhpArray"/> (<c>__sleep</c> returned an array).
		/// </remarks>
		public PhpArray Sleep(DTypeDesc caller, ScriptContext context, out bool sleepCalled)
		{
			sleepCalled = false;
			PhpArray sleep_result = null;

			DRoutineDesc method;
			switch (TypeDesc.GetMethod(SpecialMethodNames.Sleep, caller, out method))
			{
				case GetMemberResult.BadVisibility:
				{
					ThrowMethodVisibilityError(method, caller);
					break;
				}

				case GetMemberResult.OK:
				{
					context.Stack.AddFrame();
					sleep_result = PhpVariable.Dereference(method.Invoke(this, context.Stack, caller)) as PhpArray;
					sleepCalled = true;
					if (sleep_result == null)
					{
						PhpException.Throw(PhpError.Notice, CoreResources.GetString("sleep_must_return_array"));
					}
					break;
				}
			}
			return sleep_result;
		}

		#endregion

		#region Incarnate, IncarnateFast

		/// <summary>
		/// Copies fields from <paramref name="newState"/> to this instance. Called from <see cref="Externals"/>.
		/// Must try to preserve <see cref="PhpReference"/>s.
		/// </summary>
		/// <param name="newState">The instance whose state should be incarnated into this instance.</param>
		public void Incarnate(DObject/*!*/ newState)
		{
			if (newState == null || newState.TypeDesc == TypeDesc) throw new ArgumentNullException("newState");

			// incarnate CT properties
			foreach (KeyValuePair<VariableName, DPropertyDesc> pair in TypeDesc.EnumerateProperties())
			{
				object new_value = pair.Value.Get(newState);
				PhpReference new_value_ref = new_value as PhpReference;

				if (new_value_ref == null)
				{
					object old_value = pair.Value.Get(this);
					PhpReference old_value_ref = old_value as PhpReference;

					if (old_value_ref != null) old_value_ref.Value = new_value;
					else pair.Value.Set(this, new_value);
				}
				else pair.Value.Set(this, new_value_ref);
			}

			// incarnate RT fields
			if (RuntimeFields == null)
			{
				if (newState.RuntimeFields != null)
					RuntimeFields = (PhpArray)newState.RuntimeFields.Clone();
			}
			else
			{
				if (newState.RuntimeFields == null) RuntimeFields = null;
				else
				{
					// both this.RuntimeFields and newSate.RuntimeFields are non-null
					var new_rt_fields = new PhpArray(newState.RuntimeFields.Count);

					foreach (var pair in newState.RuntimeFields)
					{
						PhpReference new_ref = pair.Value as PhpReference;
						if (new_ref == null) new_rt_fields.Add(pair.Key, pair.Value);
						else
						{
							PhpReference old_ref = RuntimeFields[pair.Key] as PhpReference;
							if (old_ref == null) new_rt_fields.Add(pair.Key, new_ref);
							else
							{
								old_ref.Value = new_ref.Value;
								new_rt_fields.Add(pair.Key, old_ref);
							}
						}
					}
					RuntimeFields = new_rt_fields;
				}
			}
		}

		/// <summary>
		/// Copies fields from <paramref name="newState"/> to this instance without checks. Runtime fields nor references
		/// are preserved.
		/// </summary>
		/// <param name="newState">The instance whose state should be incarnated into this instance.</param>
		internal void IncarnateFast(PhpObject newState)
		{
			// incarnate CT properties
			foreach (KeyValuePair<VariableName, DPropertyDesc> pair in TypeDesc.EnumerateProperties())
			{
				pair.Value.Set(this, pair.Value.Get(newState));
			}

			// incarnate RT fields
			if (newState.RuntimeFields == null) RuntimeFields = null;
			else RuntimeFields = new PhpArray(newState.RuntimeFields, true);
		}

		#endregion

		#region IPhpComparable Members

		/// <summary>
		/// Compares this instance with an object of arbitrary PHP.NET type.
		/// </summary>
		/// <include file='Doc/Common.xml' path='docs/method[@name="CompareTo(obj)"]/*'/>
		public int CompareTo(object obj)
		{
			return CompareTo(obj, PhpComparer.Default);
		}

		/// <summary>
		/// Compares this instance with an object of arbitrary PHP.NET type.
		/// </summary>
		/// <remarks>
		/// <include file='Doc/Common.xml' path='docs/method[@name="CompareTo(obj,comparer)"]/*'/>
		/// </remarks>
		public virtual int CompareTo(object obj, IComparer/*!*/ comparer)
		{
			Debug.Assert(comparer != null);

			DObject php_obj;

			if (obj == null) return 1;                      // 1 > null
            if (obj is bool) return ((bool)obj ? 0 : 1);    // 1 == true, 1 > false

			if ((php_obj = obj as DObject) != null)
			{
				bool incomparable;
				int result = CompareObjects(this, php_obj, comparer, out incomparable);
				if (incomparable)
				{
					//PhpException.Throw(PhpError.Warning, CoreResources.GetString("incomparable_objects_compared"));
                    throw new ArgumentException();  // according to the IComparable remarks
				}
				return result;
			}
			return 1;
		}

		/// <summary>
		/// Compares two instances of <see cref="PhpObject"/>.
		/// </summary>
		/// <param name="comparer">The comparer.</param>
		/// <param name="incomparable">Whether objects are incomparable (no difference is found before both objects enter
		/// an infinite recursion). Returns zero then.</param>
		/// <include file='Doc/Common.xml' path='docs/method[@name="Compare(x,y)"]/*'/>
		private static int CompareObjects(DObject x, DObject y, IComparer comparer, out bool incomparable)
		{
			Debug.Assert(x != null && y != null);

			incomparable = false;

			// check for same instance
			if (ReferenceEquals(x, y)) return 0;

			// check for different number of fields
			int result = x.Count - y.Count;
			if (result != 0) return result;

			// check for different types
			DTypeDesc type_x = x.TypeDesc;
			DTypeDesc type_y = y.TypeDesc;
			if (type_x != type_y)
			{
				if (type_x.IsSubclassOf(type_y)) return -1;
				if (type_y.IsSubclassOf(type_x)) return 1;

				incomparable = true;
				return 1; // they really are 'incomparable' so what to return?
			}

			// marks objects as visited (will be always restored to false before return)
			x.visited = true;
			y.visited = true;

			try
			{
				// compare CT properties
				foreach (KeyValuePair<VariableName, DPropertyDesc> pair in type_x.EnumerateProperties())
				{
					if (pair.Value.IsStatic) continue;

					result = CompareObjectsCore(
						PhpVariable.Dereference(pair.Value.Get(x)),
						PhpVariable.Dereference(pair.Value.Get(y)),
						comparer,
						out incomparable);

					if (incomparable || result != 0) return result;
				}

				// compare RT fields
				if (x.RuntimeFields != null && y.RuntimeFields != null)
				{
					var enum_x = x.RuntimeFields.GetFastEnumerator();
					var enum_y = y.RuntimeFields.GetFastEnumerator();

					while (enum_x.MoveNext())
					{
						enum_y.MoveNext();

						// compare keys
						result = PhpArrayKeysComparer.Default.Compare(enum_x.CurrentKey, enum_y.CurrentKey);
						if (result != 0) return result;

						// compare values
						result = CompareObjectsCore(
							PhpVariable.Dereference(enum_x.CurrentValue),
							PhpVariable.Dereference(enum_y.CurrentValue),
							comparer,
							out incomparable);

						if (incomparable || result != 0) return result;
					}
				}
			}
			finally
			{
				x.visited = false;
				y.visited = false;
			}
			return 0;
		}

		private static int CompareObjectsCore(object propValue_x, object propValue_y, IComparer/*!*/ comparer,
			out bool incomparable)
		{
			Debug.Assert(comparer != null);

			incomparable = false;
			DObject object_x, object_y;

			// compare values
			if ((object_x = propValue_x as DObject) != null)
			{
				if ((object_y = propValue_y as DObject) != null)
				{
					// at least one child has not been visited yet => continue with recursion:
					if (!object_x.visited || !object_y.visited)
					{
						return CompareObjects(object_x, object_y, comparer, out incomparable);
					}
					else return 0;
				}
				else
				{
					// compare the array with a non-object
                    return object_x.CompareTo(propValue_y, comparer);
				}
			}
			else
			{
				// compares unknown item with a non-object
                return -comparer.Compare(propValue_y, propValue_x);
			}
		}

		#endregion

		#region IPhpConvertible Members, ToString, ToPhpArray

		/// <summary>
		/// Returns Phalanger type code.
		/// </summary>
		/// <returns>The type code.</returns>
		public PhpTypeCode GetTypeCode()
		{
			return PhpTypeCode.DObject;
		}

		/// <summary>
		/// Converts this instance to its <see cref="int"/> representation according to PHP conversion algorithm.
		/// </summary>
		/// <returns>The converted value.</returns>
		/// <remarks>
		/// The result is <c>1</c> if there is at least one field in this instance, <c>0</c> otherwise.
		/// </remarks>
		public virtual int ToInteger()
		{
            PhpException.Throw(PhpError.Notice, CoreResources.GetString("object_could_not_be_converted", TypeName, PhpVariable.TypeNameInt));
			return 1;
		}

		/// <summary>
		/// Converts this instance to its <see cref="long"/> representation according to PHP conversion algorithm.
		/// </summary>
		/// <returns>The converted value.</returns>
		/// <remarks>
		/// The result is <c>1</c> if there is at least one field in this instance, <c>0</c> otherwise.
		/// </remarks>
		public virtual long ToLongInteger()
		{
            PhpException.Throw(PhpError.Notice, CoreResources.GetString("object_could_not_be_converted", TypeName, PhpVariable.TypeNameInt));
			return 1;
		}

		/// <summary>
		/// Converts this instance to its <see cref="double"/> representation according to PHP conversion algorithm.
		/// </summary>
		/// <returns>The converted value.</returns>
		/// <remarks>
		/// The result is <c>1.0</c> if there is at least one field in this instance, <c>0.0</c> otherwise.
		/// </remarks>
		public virtual double ToDouble()
		{
            PhpException.Throw(PhpError.Notice, CoreResources.GetString("object_could_not_be_converted", TypeName, PhpVariable.TypeNameDouble));
			return 1.0;
		}

		/// <summary>
		/// Converts this instance to its <see cref="bool"/> representation according to PHP conversion algorithm.
		/// </summary>
		/// <returns>The converted value.</returns>
		/// <remarks>
		/// The result is <B>true</B> if there is at least one field in this instance, <B>false</B> otherwise.
		/// </remarks>
		public virtual bool ToBoolean()
		{
			return true;
		}

		/// <summary>
		/// Converts this instance to its <see cref="PhpBytes"/> representation.
		/// </summary>
		/// <returns>The converted value.</returns>
		/// <remarks>
		/// If this object contains the <c>__toString</c> method, it is invoked and its result returned.
		/// Otherwise, <see cref="Object.ToString"/> is invoked on the real object.
		/// </remarks>
		public virtual PhpBytes ToPhpBytes()
		{
            GetMemberResult lookup_result;
            object result = InvokeToString(out lookup_result);

            if (lookup_result != GetMemberResult.NotFound)
            {
                // ignoring __toString() visibility as it is in PHP:
                var bytes = PhpVariable.AsBytes(result);
                if (bytes != null)
                    return bytes;

                PhpException.Throw(PhpError.Error, CoreResources.GetString("tostring_must_return_string", TypeName));
            }

            return new PhpBytes();
		}

		private object InvokeToString(out GetMemberResult lookupResult)
		{
			DRoutineDesc method;
            if ((lookupResult = TypeDesc.GetMethod(SpecialMethodNames.Tostring, null/*ignoring visibility*/, out method)) != GetMemberResult.NotFound)
            {
                // ignoring __toString() visibility as it is in PHP:
                PhpStack stack = ScriptContext.CurrentContext.Stack;

                stack.AddFrame();
                return method.Invoke(this, stack);
            }

			// __toString() not found:
            // "Object of class %s could not be converted to %s"
            PhpException.Throw(PhpError.RecoverableError, CoreResources.GetString("object_could_not_be_converted", TypeName, PhpVariable.TypeNameString));
			return null;
		}

		/// <summary>
		/// Converts this instance to a number of type <see cref="int"/>.
		/// </summary>
		/// <param name="doubleValue">Not applicable.</param>
		/// <param name="intValue">This instance converted to integer.</param>
		/// <param name="longValue">Not applicable.</param>
		/// <returns><see cref="Convert.NumberInfo.Integer"/>.</returns>
		public virtual Convert.NumberInfo ToNumber(out int intValue, out long longValue, out double doubleValue)
		{
			intValue = 1;
			doubleValue = 1.0;
			longValue = 1;
            PhpException.Throw(PhpError.Notice, CoreResources.GetString("object_could_not_be_converted", TypeName, PhpVariable.TypeNameInt));
			return Convert.NumberInfo.Integer;
		}

		/// <summary>
		/// Converts this instance to its <see cref="string"/> representation according to PHP conversion algorithm.
		/// </summary>
		/// <returns>The converted value.</returns>
		/// <remarks>
		/// If this object contains the __toString method, it is invoked and its result returned.
		/// Otherwise, &quot;Object&quot; is returned.
		/// </remarks>
		string IPhpConvertible.ToString()
		{
            bool b;
            return ((IPhpConvertible)this).ToString(true, out b);
		}

        /// <summary>
		/// Converts this instance to its <see cref="string"/> representation according to PHP conversion algorithm.
		/// </summary>
		/// <param name="throwOnError">
		/// Should the method throw 'object_to_string_conversion' notice when no conversion method is found?
		/// </param>
		/// <param name="success">Indicates whether conversion was successful.</param>
		/// <returns>The converted value.</returns>
		/// <remarks>
		/// If this object contains the __toString method, it is invoked and its result returned.
		/// Otherwise, &quot;Object&quot; is returned.
		/// </remarks>
		public virtual string ToString(bool throwOnError, out bool success)
		{
            GetMemberResult lookup_result;
            object result = InvokeToString(out lookup_result);
            success = true;

            if (lookup_result != GetMemberResult.NotFound)
            {
                // ignoring __toString() visibility as it is in PHP:

                string str = PhpVariable.AsString(result);
                if (str != null)
                    return str;

                PhpException.Throw(PhpError.Error, CoreResources.GetString("tostring_must_return_string", TypeName));
            }
            return string.Empty;
		}

		/// <summary>
		/// Converts this instance to its <see cref="PhpArray"/> representation.
		/// </summary>
		/// <returns>The converted value.</returns>
		public virtual PhpArray ToPhpArray()
		{
			PhpArray array = new PhpArray();

			// if the real object is CLR enumerable, use it to populate the array
			IDictionaryEnumerator enumerator = GetClrEnumerator(false, false);
			if (enumerator != null)
			{
				while (enumerator.MoveNext())
				{
                    if (enumerator.Key != null)
                        array.Add(enumerator.Key, enumerator.Value);
					else
                        array.Add(enumerator.Value);
				}
			}
			else
			{
				// convert CT properties
				foreach (KeyValuePair<VariableName, DPropertyDesc> pair in TypeDesc.EnumerateProperties())
				{
					DPropertyDesc property = pair.Value;

                    if ((property.MemberAttributes & PhpMemberAttributes.AppStatic) != 0)
                        continue;   // skip static properties

                    object property_value = property.Get(this);
					PhpReference property_value_ref = property_value as PhpReference;

                    //
                    // in PHP once aliased reference is copied into new array as the same reference
                    //
					object item_value;
					if (property_value_ref != null)
					{
						if (property_value_ref.IsAliased) item_value = property_value_ref;
						else item_value = property_value_ref.Value;
					}
					else item_value = property_value;

					// add new array item
					switch (property.MemberAttributes & PhpMemberAttributes.VisibilityMask)
					{
						case PhpMemberAttributes.Public:
						{
							array.Add(pair.Key.ToString(), item_value);
							break;
						}

						case PhpMemberAttributes.Protected:
						{
							array.Add(" * " + pair.Key.ToString(), item_value);
							break;
						}

						case PhpMemberAttributes.Private:
						{
							array.Add(String.Format(" {0} {1}", property.DeclaringType.MakeFullName(),
								pair.Key.ToString()), item_value);
							break;
						}
					}
				}

				// convert RT fields
				if (RuntimeFields != null)
				{
                    using (var fields = RuntimeFields.GetFastEnumerator())
                        while (fields.MoveNext())
                            array[fields.CurrentKey] = fields.CurrentValue;
				}
			}

			return array;
		}

		#endregion

		#region IPhpCloneable Members

		/// <summary>
		/// Creates a deep copy of this instance.
		/// </summary>
		/// <returns>The copy.</returns>
		public object DeepCopy()
		{
            //ScriptContext context = ScriptContext.CurrentContext;

            //if (context.Config.Variables.ZendEngineV1Compatible)
            //    return CloneObject(null, context, true);
            //else
                return this;
		}

		/// <summary>
		/// Creates a copy of this instance.
		/// </summary>
		/// <param name="reason">The copy reason.</param>
		/// <returns>The copy.</returns>
		public object Copy(CopyReason reason)
		{
			return DeepCopy();
		}

		#endregion

		#region IPhpPrintable Members

		/// <summary>
		/// Iterator used for <see cref="Print"/>, <see cref="Dump"/>, and <see cref="Export"/>.
		/// </summary>
		/// <remarks>Override this to get a different print/dump/export behavior.</remarks>
		protected virtual IEnumerable<KeyValuePair<VariableName, AttributedValue>> PropertyIterator()
		{
			foreach (KeyValuePair<VariableName, DPropertyDesc> pair in TypeDesc.EnumerateProperties())
			{
				DPropertyDesc property = pair.Value;
				if (property.IsStatic) continue;

				object property_value;

				try
				{
					property_value = property.DumpGet(this);
				}
				catch (Exception e)
				{
					property_value = String.Format("{0}: \"{1}\"", e.GetType().Name, e.Message);
				}

				yield return new KeyValuePair<VariableName, AttributedValue>
					(pair.Key, new AttributedValue(property_value, property.MemberAttributes, property.DeclaringType));
			}
		}

		/// <summary>
		/// Prints this instance's fields in a human readable form. Mimics the <c>print_r</c> PHP function.
		/// </summary>
		/// <param name="output">The <see cref="System.IO.TextWriter"/> to print to.</param>
		public void Print(System.IO.TextWriter output)
		{
            string type_name = TypeName + " " + PrintablePhpTypeName;

			if (visited)
			{
				output.WriteLine(type_name + " [recursion]");
				return;
			}

			output.WriteLine(type_name);
			PhpVariable.PrintIndentation(output);
			output.WriteLine('(');

			PhpVariable.PrintIndentationLevel+=2;
			visited = true;

			try
			{
				// print CT properties
				foreach (KeyValuePair<VariableName, AttributedValue> pair in PropertyIterator())
				{
					PhpReference property_value_ref = pair.Value.Value as PhpReference;

					if (property_value_ref == null || property_value_ref.IsSet)
					{
						PhpVariable.PrintIndentation(output);
						switch (pair.Value.Attributes & PhpMemberAttributes.VisibilityMask)
						{
							case PhpMemberAttributes.Public: output.Write("[{0}] => ", pair.Key); break;
							case PhpMemberAttributes.Protected: output.Write("[{0}:protected] => ", pair.Key); break;
                            case PhpMemberAttributes.Private: output.Write("[{0}:{1}:private] => ", pair.Key, pair.Value.DeclaringTypeName); break;
						}
						PhpVariable.Print(output, pair.Value.Value);
                        output.WriteLine();
					}
				}

				// print RT fields
				if (RuntimeFields != null)
				{
					foreach (var pair in RuntimeFields)
					{
						PhpVariable.PrintIndentation(output);
						output.Write("[{0}] => ", pair.Key.Object);
						PhpVariable.Print(output, pair.Value);
					}
				}
			}
			finally
			{
				visited = false;
				PhpVariable.PrintIndentationLevel-=2;

				PhpVariable.PrintIndentation(output);
				output.WriteLine(')');
			}
		}

		/// <summary>
		/// Dumps this instance's fields in a human readable form including types.
		/// Mimics the <c>var_dump</c> PHP function.
		/// </summary>
		/// <param name="output">The <see cref="System.IO.TextWriter"/> to dump to.</param>
		public void Dump(System.IO.TextWriter output)
		{
			List<KeyValuePair<string, object>> ct_props = new List<KeyValuePair<string, object>>();

			PhpReference property_value_ref;

			// enumerate CT properties
			foreach (KeyValuePair<VariableName, AttributedValue> pair in PropertyIterator())
			{
				property_value_ref = pair.Value.Value as PhpReference;

				if (property_value_ref == null || property_value_ref.IsSet)
				{
					string prop_name = null;
					switch (pair.Value.Attributes & PhpMemberAttributes.VisibilityMask)
					{
						case PhpMemberAttributes.Public: prop_name = String.Format("[\"{0}\"]=>", pair.Key); break;
                        case PhpMemberAttributes.Protected: prop_name = String.Format("[\"{0}\":protected]=>", pair.Key); break;
                        case PhpMemberAttributes.Private: prop_name = String.Format("[\"{0}\":\"{1}\":private]=>", pair.Key, pair.Value.DeclaringTypeName); break;
					}

					ct_props.Add(new KeyValuePair<string, object>(prop_name, pair.Value.Value));
				}
			}

			// print the header
			int count = ct_props.Count;
			if (RuntimeFields != null) count += RuntimeFields.Count;

            string type_name = string.Format("object({0})#{2} ({1})", TypeDesc.MakeFullGenericName(), count, unchecked((uint)this.GetHashCode()));

			if (visited)
			{
				output.WriteLine(type_name + " [recursion]");
				return;
			}

			output.Write(type_name);
            output.WriteLine(" {");
			//PhpVariable.PrintIndentation(output);
			
			PhpVariable.PrintIndentationLevel+=1;
			visited = true;

			try
			{
				// print CT properties
				for (int i = 0; i < ct_props.Count; i++)
				{
					KeyValuePair<string, object> pair = ct_props[i];

					PhpVariable.PrintIndentation(output);
					output.WriteLine(pair.Key);

                    PhpVariable.PrintIndentation(output);
					property_value_ref = pair.Value as PhpReference;
					if (property_value_ref != null && !property_value_ref.IsAliased)
                        PhpVariable.Dump(output, property_value_ref.Value); // skip "&" at the beginning if field is not aliased
					else
                        PhpVariable.Dump(output, pair.Value);
				}

				// print RT fields
				if (RuntimeFields != null)
				{
					foreach (var pair in RuntimeFields)
					{
						PhpVariable.PrintIndentation(output);
						output.WriteLine("[\"{0}\"]=>", pair.Key.String);

                        PhpVariable.PrintIndentation(output);
						property_value_ref = pair.Value as PhpReference;
                        if (property_value_ref != null && !property_value_ref.IsAliased)
                            PhpVariable.Dump(output, property_value_ref.Value); // skip "&" at the beginning if field is not aliased
                        else
                            PhpVariable.Dump(output, pair.Value);
					}
				}
			}
			finally
			{
				visited = false;
				PhpVariable.PrintIndentationLevel-=1;

				PhpVariable.PrintIndentation(output);
				output.WriteLine("}");
			}
		}

		/// <summary>
		/// Exports this instance's fields in a human readable form including types.
		/// Mimics the <c>var_export</c> PHP function.
		/// </summary>
		/// <param name="output">The <see cref="System.IO.TextWriter"/> to export to.</param>
		public void Export(System.IO.TextWriter output)
		{
			string type_name = "class " + TypeName;

			if (visited)
			{
				output.Write(type_name + " {/* recursion */}");
				return;
			}

			output.WriteLine(type_name);
			PhpVariable.PrintIndentation(output);
			output.WriteLine("{");

			PhpVariable.PrintIndentationLevel++;
			visited = true;

			try
			{
				bool first = true;

				// print CT properties
				foreach (KeyValuePair<VariableName, AttributedValue> pair in PropertyIterator())
				{
					PhpReference property_value_ref = pair.Value.Value as PhpReference;

					if (property_value_ref == null || property_value_ref.IsSet)
					{
						if (!first) output.WriteLine(String.Empty);
						else first = false;

						PhpVariable.PrintIndentation(output);

						switch (pair.Value.Attributes & PhpMemberAttributes.VisibilityMask)
						{
							case PhpMemberAttributes.Public: output.Write("public ${0} = ", pair.Key); break;
							case PhpMemberAttributes.Protected: output.Write("protected ${0} = ", pair.Key); break;
							case PhpMemberAttributes.Private: output.Write("private ${0} = ", pair.Key); break;
						}

						PhpVariable.Export(output, pair.Value.Value);
						output.Write(';');
					}
				}

				// print RT fields
				if (RuntimeFields != null)
				{
					foreach (var pair in RuntimeFields)
					{
						if (!first) output.WriteLine(String.Empty);
						else first = false;

						PhpVariable.PrintIndentation(output);

						output.Write("public ${0} = ", pair.Key.String);

						PhpVariable.Export(output, pair.Value);
						output.Write(';');
					}
				}
			}
			finally
			{
				visited = false;
				PhpVariable.PrintIndentationLevel--;

				output.WriteLine(String.Empty);
				PhpVariable.PrintIndentation(output);
				output.Write('}');
			}
		}

		#endregion

		#region IPhpVariable Members

		/// <summary>
		/// Defines emptiness of the <see cref="PhpObject"/>.
		/// </summary>
		/// <returns>An instance is never empty.</returns>
		public bool IsEmpty()
		{
			// obsolete since PHP 5.1.0: return Count == 0;
			return false;
		}

		/// <summary>
		/// Defines whether <see cref="PhpObject"/> is a scalar.
		/// </summary>
		/// <returns><B>false</B></returns>
		public bool IsScalar()
		{
			return false;
		}

		/// <summary>
		/// Returns a name of declaring type.
		/// </summary>
		/// <returns>The name.</returns>
		public virtual string GetTypeName()
		{
			return PhpTypeName;
		}

		#endregion

		#region IPhpObjectGraphNode Members

		/// <summary>
		/// Walks the object graph rooted in this node.
		/// </summary>
		/// <param name="callback">The callback method.</param>
		/// <param name="context">Current <see cref="ScriptContext"/>.</param>
		public void Walk(PhpWalkCallback callback, ScriptContext context)
		{
			// prevents recursion:
			if (!this.visited)
			{
				// marks this instance as visited:
				this.visited = true;

				try
				{
					// walks CT properties:
					foreach (KeyValuePair<VariableName, DPropertyDesc> pair in TypeDesc.EnumerateProperties())
					{
						DPropertyDesc property = pair.Value;
						object property_value = property.Get(this);
						PhpReference property_value_ref = property_value as PhpReference;

						if (property_value_ref != null)
						{
							if (property_value_ref.IsSet)
							{
								if (property_value_ref.IsAliased)
								{
									// the reference is 'real'
									object res = callback(property_value_ref, context);
									PhpReference new_ref = res as PhpReference;
									if (new_ref == null) new_ref = new PhpSmartReference(res);

									property.Set(this, new_ref);
									new_ref.Walk(callback, context);
								}
								else property_value_ref.Walk(callback, context);
							}
						}
						else
						{
							// the property is not a PhpReference
							IPhpObjectGraphNode node = property_value as IPhpObjectGraphNode;
							if (node != null)
							{
								object res = callback(node, context);
								if (!Object.ReferenceEquals(res, property_value)) property.Set(this, res);
								if ((node = res as IPhpObjectGraphNode) != null) node.Walk(callback, context);
							}
						}
					}

					// walks RT fields:
					if (RuntimeFields != null)
					{
                        using (var fields = RuntimeFields.GetFastEnumerator())
                            while (fields.MoveNext())
                            {
                                IPhpObjectGraphNode node = fields.CurrentValue as IPhpObjectGraphNode;
                                if (node != null)
                                {
                                    object res = callback(node, context);
                                    if (!Object.ReferenceEquals(res, fields.CurrentValue)) fields.ModifyCurrentValue(res);
                                    if ((node = res as IPhpObjectGraphNode) != null) node.Walk(callback, context);
                                }
                            }
					}
				}
				finally
				{
					this.visited = false;
				}
			}
		}

		#endregion

		#region IPhpEnumerable Members

		/// <summary>
		/// Not supported in objects.
		/// </summary>
		public IPhpEnumerator IntrinsicEnumerator
		{
			get
			{ throw new NotSupportedException(); }
		}

		/// <summary>
		/// Creates an enumerator used in the <c>foreach</c> statement.
		/// </summary>
		/// <param name="keyed">Whether the foreach statement uses keys.</param>
		/// <param name="aliasedValues">Whether the values returned by enumerator are assigned by reference.</param>
		/// <param name="caller">Type <see cref="DTypeDesc"/> of the class in whose context the caller operates.</param>
		/// <returns>The dictionary enumerator.</returns>
		public IDictionaryEnumerator GetForeachEnumerator(bool keyed, bool aliasedValues, DTypeDesc caller)
		{
			IDictionaryEnumerator result;

			// does this instance implement SPL.Iterator or SPL.IteratorAggregate?
			result = GetSplEnumerator(keyed, aliasedValues);

			if (result == null)
			{
				// does this instance implement one of the CLR enumerating interfaces?
				result = GetClrEnumerator(keyed, aliasedValues);

				if (result == null)
				{
					// fall back to standard PHP property enumeration
					result = new GenericDictionaryAdapter<object, object>(InstancePropertyIterator(caller, aliasedValues), false);
				}
			}

			return result;
		}

		private IDictionaryEnumerator GetSplEnumerator(bool keyed, bool aliasedValues)
		{
			ScriptContext context = null;

			DObject obj = this;
			if (!(RealObject is Library.SPL.Iterator))
			{
				if (!(RealObject is Library.SPL.IteratorAggregate))
				{
					return null;
				}
				else context = ScriptContext.CurrentContext;

				DObject last_obj;
				do
				{
					last_obj = obj;

					// call IteratorAggregate::getIterator & dereference the return value
					context.Stack.AddFrame();
					obj = PhpVariable.Dereference(obj.InvokeMethod("getIterator", null, context)) as DObject;

					// check whether another IteratorAggregate was returned
				}
				while (obj != null && obj.RealObject is Library.SPL.IteratorAggregate);

				if (obj == null || !(obj.RealObject is Library.SPL.Iterator))
				{
                    Library.SPL.Exception.ThrowSplException(
                        _ctx => new Library.SPL.Exception(_ctx, true),
                        context,
                        string.Format(CoreResources.getiterator_must_return_traversable, last_obj.TypeName), 0, null);
				}
			}
			else context = ScriptContext.CurrentContext;

			return new PhpIteratorEnumerator(obj, context, aliasedValues);
		}

		private IDictionaryEnumerator GetClrEnumerator(bool keyed, bool aliasedValues)
		{
			// IDictionary
			IDictionary dictionary = RealObject as IDictionary;
			if (dictionary != null)
			{
				return new GenericDictionaryAdapter<object, object>(IDictionaryIterator(dictionary), true);
			}

			// IEnumerable<KeyValuePair<object, object>> (fast path, used by LINQ)
			IEnumerable<KeyValuePair<object, object>> gendict = RealObject as IEnumerable<KeyValuePair<object, object>>;
			if (gendict != null)
			{
				return new GenericDictionaryAdapter<object, object>(gendict.GetEnumerator(), true);
			}

            if (!keyed)
			{
				// IEnumerable
				IEnumerable enumerable = RealObject as IEnumerable;
				if (enumerable != null)
				{
					return new GenericEnumerableAdapter<object>(IEnumerableIterator(enumerable), true);
				}

				// IEnumerable<object> (fast path, used by LINQ)
				IEnumerable<object> genenum = RealObject as IEnumerable<object>;
				if (genenum != null)
				{
					return new GenericEnumerableAdapter<object>(genenum.GetEnumerator(), true);
				}
			}

			// IEnumerable<T>
			// IEnumerable<KeyValuePair<T, S>>

			foreach (Type iface_type in GetType().GetInterfaces())
			{
				if (iface_type.IsGenericType && iface_type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				{
					Type item_type = iface_type.GetGenericArguments()[0];

					if (item_type.IsGenericType && item_type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
					{
						// IEnumerable<KeyValuePair<T, S>>
						Type iterator_type = typeof(GenericDictionaryAdapter<,>).MakeGenericType(item_type.GetGenericArguments());
						return (IDictionaryEnumerator)Activator.CreateInstance(iterator_type, RealObject, true);
					}

					if (!keyed)
					{
						// IEnumerable<T>
						Type iterator_type = typeof(GenericEnumerableAdapter<>).MakeGenericType(item_type);
						return (IDictionaryEnumerator)Activator.CreateInstance(iterator_type, RealObject, true);
					}
				}
			}

			return null;
		}

		private IEnumerator<KeyValuePair<object, object>> IDictionaryIterator(IDictionary dictionary)
		{
			foreach (DictionaryEntry entry in dictionary)
			{
				yield return new KeyValuePair<object, object>(entry.Key, entry.Value);
			}
		}

		private IEnumerator<object> IEnumerableIterator(IEnumerable enumerable)
		{
			foreach (object value in enumerable)
			{
				yield return value;
			}
		}

		/// <summary>
		/// An PHP style iterator for <see cref="DObject"/>.
		/// </summary>
		/// <remarks>
		/// This iterator returns instance property names as keys and instance property values as values. Only
		/// properties that are visible for the <paramref name="caller"/> are enumerated - both CT and RT, CT first.
		/// </remarks>
		internal IEnumerator<KeyValuePair<object, object>> InstancePropertyIterator(DTypeDesc caller, bool aliasedValues)
		{
			object value;
			PhpReference reference;

			// enumerate CT properties
			foreach (KeyValuePair<VariableName, DPropertyDesc> pair in TypeDesc.EnumerateProperties(caller))
			{
                if (pair.Value.IsStatic)
                    continue;  // ignore static fields here

				string key = pair.Key.ToString();

				value = pair.Value.Get(this);
				if (aliasedValues)
				{
					reference = value as PhpReference;
					if (reference == null)
					{
						// create a new reference and store it back
						value = new PhpReference(value);
						pair.Value.Set(this, value);
					}
				}
				else value = PhpVariable.Copy(PhpVariable.Dereference(value), CopyReason.Assigned);

				yield return new KeyValuePair<object, object>(key, value);
			}

			// enumerate RT fields
			if (RuntimeFields != null)
			{
				foreach (var pair in RuntimeFields)
				{
					value = pair.Value;

					if (aliasedValues)
					{
						reference = value as PhpReference;
						if (reference == null)
						{
							// create a new reference and store it back
							value = new PhpReference(value);
							RuntimeFields[pair.Key] = value;
						}
					}
					else value = PhpVariable.Copy(PhpVariable.Dereference(value), CopyReason.Assigned);

					yield return new KeyValuePair<object, object>(pair.Key.Object, value);
				}
			}
		}

		#endregion

		#region IDictionary-like Members

		/// <summary>
		/// Adds a runtime instance field with the provided name and value.
		/// </summary>
		public void Add(object name, object value)
		{
			if (name == null) throw new ArgumentNullException("name");
			if (Contains(name)) throw new ArgumentException(CoreResources.GetString("field_already_exists"));

			if (RuntimeFields == null) RuntimeFields = new PhpArray();
			RuntimeFields.Add(Convert.ObjectToString(name), value);
		}

        public void AddRange( IEnumerable<KeyValuePair<string,object>> members )
        {
            if (members == null)
                return;

            if (RuntimeFields == null)
                RuntimeFields = new PhpArray();

            foreach (var member in members)
            {
                if (member.Key == null) throw new ArgumentNullException("members[].Key");
                if (Contains(member.Key)) throw new ArgumentException(CoreResources.GetString("field_already_exists"));

                RuntimeFields.Add(member.Key, member.Value);
            }
        }

		/// <summary>
		/// Unsets all runtime fields.
		/// </summary>
		public void Clear()
		{
			if (RuntimeFields != null) RuntimeFields.Clear();
		}

		/// <summary>
		/// Determines whether this instance contains an instance property with the specified name.
		/// </summary>
		public bool Contains(object name)
		{
			if (name == null) throw new ArgumentNullException("name");

			string sname = Convert.ObjectToString(name);

			// check CT properties
			DPropertyDesc property;
			if (TypeDesc.GetInstanceProperty(new VariableName(sname), TypeDesc, out property) !=
				GetMemberResult.NotFound) return true;

			// check RT fields
			if (RuntimeFields == null) return false;
			return RuntimeFields.ContainsKey(sname);
		}
        
		/// <summary>
		/// Returns an enumerator that enumerates instance properties visible in the current class context.
		/// </summary>
		/// <returns>The enumerator.</returns>
        [Obsolete("This method has performance issue. Use GetEnumerator(DTypeDesc caller) with a caller already determined.")] 
		public IDictionaryEnumerator GetEnumerator()
		{
			return GetEnumerator(PhpStackTrace.GetClassContext());
		}

		/// <summary>
		/// Returns an enumerator that enumerates instance properties visible in a given class context.
		/// </summary>
		/// <param name="caller"><see cref="DTypeDesc"/> of the object that request the enumeration.</param>
		/// <returns>The enumerator.</returns>
		public IDictionaryEnumerator GetEnumerator(DTypeDesc caller)
		{
			return GetForeachEnumerator(true, false, caller);
		}

		public void Remove(object key)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		/// Gets or sets an instance property with the specified name.
		/// </summary>
		public object this[object name]
		{
			get
			{
				if (name == null) throw new ArgumentNullException("name");

				DTypeDesc caller = PhpStackTrace.GetClassContext();
				return Operators.GetObjectProperty(this, Convert.ObjectToString(name), caller, false);
			}
			set
			{
				if (name == null) throw new ArgumentNullException("name");

				DTypeDesc caller = PhpStackTrace.GetClassContext();
				Operators.SetObjectProperty(this, Convert.ObjectToString(name), value, caller);
			}
		}

		/// <summary>
		/// Gets the number of (set) instance properties contained in this <see cref="DObject"/>.
		/// </summary>
		public int Count
		{
			get
			{
				int count = 0;
				
				// enumerate CT properties
				foreach (KeyValuePair<VariableName, DPropertyDesc> pair in TypeDesc.EnumerateProperties())
				{
					DPropertyDesc property = pair.Value;
					if (property.IsStatic) continue;

					if (!(property is DPhpFieldDesc)) count++;
					else
					{
						// PHP fields can be unset!
						PhpReference property_value_ref = property.Get(this) as PhpReference;
						if (property_value_ref == null || property_value_ref.IsSet) count++;
					}
				}

				// add RT fields
				if (RuntimeFields != null) count += RuntimeFields.Count;

				return count;
			}
		}

        ///// <summary>
        ///// Returns <B>true</B> is this instance contains at least one instance property that is not unset.
        ///// </summary>
        //public bool HasSetInstanceProperties
        //{
        //    get
        //    {
        //        if (RuntimeFields != null && RuntimeFields.Count > 0) return true;

        //        // enumerate CT properties
        //        foreach (KeyValuePair<VariableName, DPropertyDesc> pair in TypeDesc.EnumerateProperties())
        //        {
        //            DPropertyDesc property = pair.Value;
        //            if (property.IsStatic) continue;

        //            if (!(property is DPhpFieldDesc)) return true;
        //            else
        //            {
        //                // PHP fields can be unset!
        //                PhpReference property_value_ref = property.Get(this) as PhpReference;
        //                if (property_value_ref == null || property_value_ref.IsSet) return true;
        //            }
        //        }

        //        return false;
        //    }
        //}

		#endregion

		#region Serialization - CLR Only (ISerializable & IDeserializationCallback)
#if !SILVERLIGHT

		/// <include file='Doc/Common.xml' path='/docs/method[@name="serialization.ctor"]/*'/>
		protected DObject(SerializationInfo/*!*/ info, StreamingContext context)
		{
			SerializationSurrogate.Instance.SetObjectData(this, info, context, null);
		}


		/// <include file='Doc/Common.xml' path='/docs/method[@name="GetObjectData"]/*'/>
        [System.Security.SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			SerializationSurrogate.Instance.GetObjectData(this, info, context);
		}


		/// <summary>
		/// Runs when the entire object graph has been deserialized.
		/// </summary>
		public void OnDeserialization(object sender)
		{
			SerializationSurrogate.Instance.OnDeserialization(this);
		}

#endif
		#endregion

		#region IDisposable Members, ~DObject

		/// <summary>
		/// Disposes of unmanaged and optionally also managed resources.
		/// </summary>
		/// <param name="disposing">If <B>true</B>, both managed and unmanaged resources should be released.
		/// If <B>false</B> only unmanaged resources should be released.</param>
		/// <remarks>
		/// <para>
		/// User defined destructor (<c>__destruct</c>) can only be called, when <parameref name="disposing"/>
		/// is <B>true</B>, because the destructor may (and probably will) manipulate managed resources which is
		/// forbidden when this method is called by the runtime with <parameref name="disposing"/> set to
		/// <B>false</B>).
		/// </para>
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			if (ReadyForDisposal && System.Threading.Interlocked.Exchange(ref disposed, 1) == 0)
			{
                // only PHP types with public non static __destruct() function are IPhpDestructable
                IPhpDestructable obj = this as IPhpDestructable;
                if (obj != null)
                {
                    obj.__destruct(ScriptContext.CurrentContext);
                    return;
                }

				// perform __destruct method lookup and runtime check
				DRoutineDesc method;
				switch (TypeDesc.GetMethod(SpecialMethodNames.Destruct, TypeDesc, out method))
				{
					case GetMemberResult.BadVisibility:
					{
						ThrowMethodVisibilityError(method, TypeDesc);
						return;
					}

					case GetMemberResult.OK:
					{
						// destructor must not be static
						if (method.IsStatic)
						{
							PhpException.Throw(PhpError.Error, CoreResources.GetString("destructor_cannot_be_static",
							  method.DeclaringType.MakeFullName(), SpecialMethodNames.Destruct));
							return;
						}

						PhpStack stack = ScriptContext.CurrentContext.Stack;

						stack.AddFrame();
						method.Invoke(this, stack);
						break;
					}
				}                
			}
		}

		/// <summary>
		/// Standard <see cref="IDisposable.Dispose"/> implementation.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

        // Finalizer emitted only in PHP types with __destruct() function
		///// <summary>
		///// Overriden <see cref="Object.Finalize"/>.
		///// </summary>
		//~DObject()
	    //{
		//	Dispose(false);
		//}

		#endregion


        #region IDynamicMetaObjectProvider Members

        public DynamicMetaObject GetMetaObject(System.Linq.Expressions.Expression parameter)
        {
            return new DMetaObject(parameter, this);
        }

        #endregion
    }

    #region ClrObject

	/// <summary>
	/// Represents a non-PHP object at runtime.
	/// </summary>
	/// <remarks>
	/// TODO: Should override conversion routines and delegate to real object's ToString, IConvertible, ...
	/// </remarks>
    [Serializable]
    [DebuggerNonUserCode]
    [DebuggerDisplay("{realObject}", Type = "{TypeName,nq}")]
    public sealed class ClrObject : DObject
    {
        #region Fields and Properties

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly WeakCache<ClrObject>/*!*/ cache = new WeakCache<ClrObject>();

        /// <summary>
        /// The real object contained by this ClrObject wrapper.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public override object/*!*/ RealObject { get { return realObject; } }
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private object/*!*/ realObject;

        /// <summary>
        /// The reference passed to the methods and properties.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public override object InstanceObject { get { return realObject; } }

        /// <summary>
        /// To be used by serialization only.
        /// </summary>
        internal void SetRealObject(object obj)
        {
            Debug.Assert(obj != null && typeDesc == null);
            this.realObject = obj;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected override bool ReadyForDisposal
        {
            get
            {
                lock (cache)
                {
                    return !cache.ContainsKey(realObject);
                }
            }
        }

        #endregion

        #region Construction and Finalization

        private ClrObject(object/*!*/ realObject)
            : base()
        {
            Debug.Assert(realObject != null);
            this.realObject = realObject;
        }

        private ClrObject(object/*!*/ realObject, DTypeDesc/*!*/ typeDesc)
            : base(typeDesc)
        {
            Debug.Assert(realObject != null);
            this.realObject = realObject;
        }

        ~ClrObject()
        {
            // if the real object is still held in the cache, resurrect this instance
            lock (cache)
            {
                try
                {
                    if (cache.ContainsKey(realObject))
                    {
                        cache.Resurrect(realObject, this);
                        GC.ReRegisterForFinalize(this);
                    }
                }
                catch (System.Runtime.Remoting.RemotingException)
                {
                    // do not ressurect dead remote objects
                }
            }
        }

        /// <summary>
        /// Performs "dynamic" type check and wraps only if the type is not primitive
        /// </summary>
        /// <param name="instance">Object to be converted to PHP world.</param>
        /// <returns>PHP type variable.</returns>
        [Emitted]
        public static object WrapDynamic(object instance)
        {
            // keep PHP literals
            if (PhpVariable.HasPrimitiveType(instance)) return instance;

            // keep DObject
            if (instance is DObject) return instance;

            // convert byte[] into PhpBytes
            byte[] bytes;
            if ((bytes = instance as byte[]) != null) return new PhpBytes(bytes);

            // convert MulticastDelegate into callable PHP object
            if (instance is MulticastDelegate) return WrapDelegate((MulticastDelegate)instance);

            // create ClrObject from instance
            return WrapRealObject(instance);
        }

        [Emitted]
        public static DObject Wrap(object instance)
        {
            DObject obj;
            if ((obj = instance as DObject) != null) return obj;

            return WrapRealObject(instance);
        }

        [Emitted]
        public static DObject WrapRealObject(object instance)
        {
            if (instance == null) return null;

            Debug.Assert(!PhpVariable.HasPrimitiveType(instance));
            Debug.Assert(!(instance is DObject));

            ClrObject result;

            lock (cache)
            {
                if (!cache.TryGetValue(instance, out result))
                {
                    return Create(instance);
                }
            }
            return result;
        }

        #region MulticastDelegate wrapping

        [ImplementsType]
        public class DelegateClosure : PhpObject
        {
            /// <summary>
            /// Delegate to be called when this object is converted into <see cref="PhpCallback"/> and invoked.
            /// </summary>
            private readonly MulticastDelegate/*!*/function;

            /// <summary>
            /// Cache args count of the <see cref="function"/>.
            /// </summary>
            private System.Reflection.ParameterInfo[] functionParams = null;

            internal DelegateClosure(MulticastDelegate/*!*/function)
                : base(ScriptContext.CurrentContext, true)
            {
                Debug.Assert(function != null);
                this.function = function;
            }

            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public static object __invoke(object instance, PhpStack stack)
            {
                // prepare args array
                object[] args = new object[stack.ArgCount];
                for (int i = 0; i < args.Length; i++)
                    args[i] = stack.PeekValue(i + 1);

                // call __invoke
                stack.RemoveFrame();
                return ((DelegateClosure)instance).invokeDelegate(args);
            }


            [ImplementsMethod, System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public object __invoke(ScriptContext/*!*/context)
            {
                return invokeDelegate();
            }

            /// <summary>
            /// Check the <paramref name="args"/> to correspond to <see cref="function"/>.
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            private object[] CheckArgs(object[]/*!*/args)
            {
                if (functionParams == null)
                    functionParams = function.Method.GetParameters();

                if (args.Length != functionParams.Length)
                {
                    if (args.Length < functionParams.Length)
                    {
                        // Warning: InvalidArgumentCount
                        PhpException.InvalidArgumentCount(
                            (function.Target == null) ? null : function.Target.GetType().ToString().Replace('.', QualifiedName.Separator),
                            function.Method.Name);
                    }

                    var args2 = new object[functionParams.Length];  // valid params count
                    int copiedArgs = Math.Min(args2.Length, args.Length);
                    Array.Copy(args, args2, copiedArgs);    // copy passed params
                    args = args2;

                    // default value for missing args
                    for (int i = copiedArgs; i < args.Length; i++)
                        args[i] = /*functionParams[i].DefaultValue ?? */ReflectionUtils.GetDefault(functionParams[i].ParameterType);
                }

                return args;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="args">Arguments to be unwrapped and passed to <see cref="function"/>.</param>
            /// <returns></returns>
            private object invokeDelegate(params object[] args)
            {
                // check args count
                args = CheckArgs(args);

                // unwraps arguments
                for (int i = 0; i < args.Length; i++)
                    args[i] = PhpVariable.Unwrap(args[i]);

                // invoke the .NET delegate
                return ClrObject.WrapDynamic(this.function.DynamicInvoke(args));
            }

            /// <summary>
            /// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
            /// </summary>
            /// <param name="typeDesc">The type desc to populate.</param>
            private static void __PopulateTypeDesc(PhpTypeDesc typeDesc)
            {
                typeDesc.AddMethod("__invoke", PhpMemberAttributes.Public, __invoke);

            }
        }
        /// <summary>
        /// Wrap <see cref="MulticastDelegate"/> into PHP invokable object.
        /// </summary>
        /// <param name="function">.NET <see cref="MulticastDelegate"/> to be wrapped.</param>
        /// <returns>PHP callable object.</returns>
        public static DObject/*!*/WrapDelegate(MulticastDelegate/*!*/function)
        {
            return new DelegateClosure(function);
        }

        #endregion

        /// <summary>
        /// Called by compiled code when a new real object is being constructed.
        /// </summary>
        [Emitted]
        public static DObject/*!*/ Create(object/*!*/ realObject)
        {
            var type = realObject.GetType();
            if (type.IsValueType) // wrapped boxed value types are not cached
                return (DObject)valueTypesCache.Get(type).Item2.DynamicInvoke(realObject);
            
            ClrObject result = new ClrObject(realObject);

            lock (cache)
            {
                // realObject is a fresh new object which is surely not in the cache
                cache.Add(realObject, result);
            }
            return result;
        }

        #endregion

        #region ClrValue<> creation

        /// <summary>
        /// Cache of <see cref="ClrValue&lt;T&gt;"/> and its <see cref="ClrValue&lt;T&gt;.Create"/> method associated with specific value type.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal static readonly SynchronizedCache<Type, Tuple<Type, Delegate>>/*!*/valueTypesCache =
            new SynchronizedCache<Type, Tuple<Type, Delegate>>(type =>
            {
                Debug.Assert(type != null, "type must not be null!");
                Debug.Assert(type.IsValueType, "type must be a value type!");

                var generictype = typeof(ClrValue<>).MakeGenericType(new Type[] { type });
                var create = Delegate.CreateDelegate(
                    System.Linq.Expressions.Expression.GetFuncType(new Type[] { type, generictype }),
                    generictype.GetMethod("Create"));

                return new Tuple<Type, Delegate>(generictype, create);
            });

        #endregion

        #region Misc

        public override string GetTypeName()
        {
            return realObject.GetType().Name;
        }

        public override string ToString()
        {
            return realObject.ToString();
        }

#if DEBUG
        public static int GetCacheSize()
        {
            return cache.Count;
        }
#endif

        #endregion

        #region PHP Operators

        /// <summary>
        /// Overrides basic string conversion of CLR object by calling its <c>ToString</c> method.
        /// </summary>
        public override string ToString(bool throwOnError, out bool success)
        {
            success = true;
            return this.realObject.ToString();
        }

        /// <summary>
        /// Overrides basic string conversion of CLR object by calling its <c>ToString</c> method.
        /// </summary>
        public override PhpBytes ToPhpBytes()
        {
            return new PhpBytes(this.realObject.ToString());
        }

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <include file='Doc/Common.xml' path='/docs/method[@name="serialization.ctor"]/*'/>
        private ClrObject(SerializationInfo/*!*/ info, StreamingContext context)
            : base(info, context)
        { }

#endif
        #endregion
    }

    #endregion

    #region ClrValue<T>

    /// <summary>
    /// An interface identifying ClrValue&lt;T&gt; instance object.
    /// </summary>
    public interface IClrValue { }

    /// <summary>
    /// Represents non-PHP value typed object at runtime.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    [DebuggerNonUserCode]
    [DebuggerDisplay("{realValue}", Type = "{TypeName,nq}")]
    internal sealed class ClrValue<T> : DObject, IClrValue where T : struct
    {
        #region Fields and properties

        /// <summary>
        /// The CLR value represented by this <see cref="ClrValue&lt;T&gt;"/> instance.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T realValue;

        /// <summary>
        /// The CLR value represented by this <see cref="ClrValue&lt;T&gt;"/> instance. The returned value is boxed.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public override object RealObject { get { return (object)realValue; } } // box the realValue

        /// <summary>
        /// The object passed as an instance to called methods and properties of this value type.
        /// The whole <see cref="ClrValue&lt;T&gt;"/> is returned too not box the wrapped value. Therefore the value
        /// can be modified in-place by the called method or property.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public override object InstanceObject { get { return this; } }  // pass the whole ClrValue into the method/property stub
        
        #endregion

        #region Construction and Finalization

        private ClrValue(T/*!*/ realValue)
			: base()
		{
            Debug.Assert(realValue.GetType().IsValueType);
            this.realValue = realValue;
		}

        private ClrValue(T/*!*/ realValue, DTypeDesc/*!*/ typeDesc)
			: base(typeDesc)
		{
            Debug.Assert(realValue.GetType().IsValueType);
            this.realValue = realValue;
		}

        /// <summary>
        /// Create the instance of <see cref="ClrValue&lt;T&gt;"/>.
        /// </summary>
        /// <param name="value">The value to be wrapped into <see cref="ClrValue&lt;T&gt;"/>.</param>
        /// <returns>New instance of <see cref="ClrValue&lt;T&gt;"/> containing <paramref name="value"/> as its <see cref="RealObject"/>.</returns>
        public static ClrValue<T> Create(T value)
        {
            return new ClrValue<T>(value);
        }

        #endregion

        #region Misc

        public override string GetTypeName()
        {
            return realValue.GetType().Name;
        }

        public override string ToString()
        {
            return realValue.ToString();
        }
        
        #endregion

        #region PHP Operators

        /// <summary>
        /// Overrides basic string conversion of CLR object by calling its <c>ToString</c> method.
        /// </summary>
        public override string ToString(bool throwOnError, out bool success)
        {
            success = true;
            return this.realValue.ToString();
        }

        /// <summary>
        /// Overrides basic string conversion of CLR object by calling its <c>ToString</c> method.
        /// </summary>
        public override PhpBytes ToPhpBytes()
        {
            return new PhpBytes(this.realValue.ToString());
        }

        /// <summary>
        /// Overrides default double cast of CLR object in case of <see cref="decimal"/> type.
        /// </summary>
        public override double ToDouble()
        {
            if (typeof(T) == typeof(decimal))
                return (double)(decimal)this.RealObject;

            return base.ToDouble();
        }

        /// <summary>
        /// Overrides default number conversion of CLR object in of <see cref="decimal"/> type.
        /// </summary>
        public override Convert.NumberInfo ToNumber(out int intValue, out long longValue, out double doubleValue)
        {
            if (typeof(T) == typeof(decimal))
            {
                doubleValue = (double)(decimal)this.RealObject;
                intValue = unchecked((int)doubleValue);
                longValue = unchecked((long)doubleValue);
                return Convert.NumberInfo.Double | Convert.NumberInfo.IsNumber;
            }

            return base.ToNumber(out intValue, out longValue, out doubleValue);
        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone the value typed object and create new <see cref="ClrValue&lt;T&gt;"/> instance containing copy of <see cref="realValue"/>.
        /// </summary>
        /// <param name="caller">Current class contetext. Ignored.</param>
        /// <param name="context">Current <see cref="ScriptContext"/>. Ignored.</param>
        /// <param name="deepCopyFields"></param>
        /// <returns>New instance of <see cref="ClrValue&lt;T&gt;"/>.</returns>
        protected override DObject CloneObjectInternal(DTypeDesc caller, ScriptContext context, bool deepCopyFields)
        {
            return Create(realValue);
        }

        #endregion
    }

    #endregion
}
