/*

 Copyright (c) 2004-2006 Ladislav Prosek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Collections.Generic;
using D = System.Diagnostics;

using PHP.Core;
using PHP.Core.Reflection;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

/*

  Designed and implemented by Ladislav Prosek and Tomas Matousek.
  
*/

namespace PHP.Library
{
	#region stdClass

	/// <summary>
	/// &quot;Standard&quot; built-in PHP class.
	/// </summary>
	/// <remarks>
	/// This class contains no CT fields and no methods. It is implicitly instantiated when you apply
	/// certain field access operators (-&gt;) on an empty variable.
	/// </remarks>
	[Serializable, ImplementsType]
	public class stdClass : PhpObject
	{
		#region Fields

		/// <summary>
		/// The name of this class.
		/// </summary>
		public const string ClassName = "stdClass";

		#endregion

		#region Construction

		/// <summary>
		/// Creates a new <see cref="stdClass"/>.
		/// <seealso cref="PHP.Core.PhpObject(PHP.Core.ScriptContext,DTypeDesc)"/>
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public stdClass(ScriptContext context, DTypeDesc caller)
			: base(context, true)
		{ }

		/// <summary>
		/// Creates a new <see cref="stdClass"/>.
		/// <seealso cref="PHP.Core.PhpObject(PHP.Core.ScriptContext,bool)"/>
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public stdClass(ScriptContext context, bool newInstance)
			: base(context, newInstance)
		{ }

		/// <summary>
		/// Creates a new <see cref="stdClass"/> (<c>newInstance</c> is <B>false</B>).
		/// </summary>
		[Emitted]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public stdClass(ScriptContext context)
			: this(context, false)
		{ }

		/// <summary>
		/// Creates a new <see cref="stdClass"/> with current <see cref="ScriptContext"/>.
		/// </summary>
		public stdClass()
			: this(ScriptContext.CurrentContext, true)
		{ }

		/// <summary>
		/// Creates an empty <see cref="stdClass"/> and throws the &quot;Creating default object from empty value&quot;
		/// strict message.
		/// </summary>
		/// <param name="context">The <see cref="ScriptContext"/> to create the <see cref="stdClass"/> with.</param>
		/// <returns>The created <see cref="stdClass"/> instance.</returns>
		/// <exception cref="PhpException">Always (Strict).</exception>
		public static stdClass CreateDefaultObject(ScriptContext context)
		{
			PhpException.Throw(PhpError.Strict, CoreResources.GetString("default_object_created"));
			return new stdClass(context);
		}

		#endregion

		#region __PopulateTypeDesc

		internal static void __PopulateTypeDesc(PhpTypeDesc typeDesc)
		{
			// the method is empty, which means that stdClass has no methods or CT properties;
			// existence of this method however avoids the slow reflection when creating its DTypeDesc
		}

		#endregion

		#region Serialization (CLR only)
#if !SILVERLIGHT

		/// <include file='Doc/Common.xml' path='/docs/method[@name="serialization.ctor"]/*'/>
		protected stdClass(SerializationInfo info, StreamingContext context)
				: base(info, context)
		{ }

#endif
		#endregion
	}

	#endregion

	#region __PHP_Incomplete_Class

	/// <summary>
	/// Type of the result of failed deserialization.
	/// </summary>
	/// <remarks>
	/// Instances of this class are created when an attempts is made to deserialize an object whose class
	/// is undefined for the current script.
	/// </remarks>
	[Serializable, ImplementsType]
	public class __PHP_Incomplete_Class : PhpObject
	{
		#region Fields

		/// <summary>
		/// The name of this class.
		/// </summary>
		public const string ClassName = "__PHP_Incomplete_Class";

		/// <summary>
		/// Name of the field that holds name of the class that was originally serialized.
		/// </summary>
		public const string ClassNameFieldName = "__PHP_Incomplete_Class_Name";

		/// <summary>
		/// Holds name of the class that was originally serialized.
		/// </summary>
		public PhpReference __PHP_Incomplete_Class_Name = new PhpSmartReference();

		#endregion

		#region Construction

		/// <summary>
		/// Creates a new <see cref="__PHP_Incomplete_Class"/>.
		/// <seealso cref="PHP.Core.PhpObject(PHP.Core.ScriptContext,DTypeDesc)"/>
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public __PHP_Incomplete_Class(ScriptContext context, DTypeDesc caller)
			: base(context, true)
		{ }

		/// <summary>
		/// Creates a new <see cref="__PHP_Incomplete_Class"/>.
		/// <seealso cref="PHP.Core.PhpObject(PHP.Core.ScriptContext, bool)"/>
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public __PHP_Incomplete_Class(ScriptContext context, bool newInstance)
			: base(context, newInstance)
		{ }

		/// <summary>
		/// Creates a new <see cref="__PHP_Incomplete_Class"/> with current <see cref="ScriptContext"/>.
		/// </summary>
		public __PHP_Incomplete_Class()
			: this(ScriptContext.CurrentContext, true)
		{ }

#if !SILVERLIGHT
		/// <include file='Doc/Common.xml' path='/docs/method[@name="serialization.ctor"]/*'/>
		protected __PHP_Incomplete_Class(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{ }
#endif

		#endregion

		#region __PopulateTypeDesc & Stubs

		/// <summary>
		/// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
		/// </summary>
		/// <param name="typeDesc">The type desc to populate.</param>
		internal static void __PopulateTypeDesc(PhpTypeDesc typeDesc)
		{
			typeDesc.AddProperty(ClassNameFieldName, PhpMemberAttributes.Public,
                (instance) => ((__PHP_Incomplete_Class)instance).__PHP_Incomplete_Class_Name,
                (instance, value) => ((__PHP_Incomplete_Class)instance).__PHP_Incomplete_Class_Name = (PhpReference)value);
		}

		#endregion
	}

	#endregion

	#region EventClass<>

	/// <summary>
	/// Represents a CLR event acquired using the property-getting syntax.
	/// </summary>
	/// <typeparam name="T">Event handler type (a delegate).</typeparam>
	[Serializable, ImplementsType]
	public sealed class EventClass<T> : PhpObject where T : class
	//where T : Delegate (does anyone know the reason why this constraint is not permitted?)
	{
		[Serializable]
		public delegate void HookDelegate(T del);

		#region Fields and Properties

		/// <summary>
		/// The desc of the CLR event's delegate type.
		/// </summary>
		private static ClrDelegateDesc/*!*/ delegateDesc = (ClrDelegateDesc)DTypeDesc.Create(typeof(T));

		/// <summary>
		/// Name of the CLR event.
		/// </summary>
		private string/*!*/ eventName;

		/// <summary>
		/// Delegate pointing to the event's add method (may be <B>null</B>).
		/// </summary>
		private HookDelegate addMethod;
		
		/// <summary>
		/// Delegate pointing to the event's remove method (may be <B>null</B>).
		/// </summary>
		private HookDelegate removeMethod;

		#endregion

		#region Construction

#if DEBUG
		static EventClass()
		{
			Debug.Assert(typeof(Delegate).IsAssignableFrom(typeof(T)));
		}
#endif

		/// <summary>
		/// Creates a new <see cref="EventClass{T}"/>.
		/// </summary>
		/// <param name="context">Current <see cref="ScriptContext"/>.</param>
		/// <param name="eventName">Name of the CLR event.</param>
		/// <param name="addMethod">Delegate pointing to the event's add method (may be <B>null</B>).</param>
		/// <param name="removeMethod">Delegate pointing to the event's remove method (may be <B>null</B>).</param>
		/// <remarks>
		/// At least one of <paramref name="addMethod"/> and <paramref name="removeMethod"/> must be non-<B>null</B>.
		/// </remarks>
		private EventClass(ScriptContext/*!*/ context, string/*!*/ eventName, HookDelegate addMethod, HookDelegate removeMethod)
			: this(context, true)
		{
			Debug.Assert(addMethod != null || removeMethod != null);

			this.eventName = eventName;
			this.addMethod = addMethod;
			this.removeMethod = removeMethod;
		}

#if !SILVERLIGHT
		/// <include file='Doc/Common.xml' path='/docs/method[@name="serialization.ctor"]/*'/>
		private EventClass(SerializationInfo/*!*/ info, StreamingContext context)
			: base(info, context)
		{ }
#endif

		[Emitted]
		public static EventClass<T> Wrap(ScriptContext/*!*/context, string eventName,
			HookDelegate addMethod, HookDelegate removeMethod)
		{
			// cache the result?
			// it would support things like:
			// $x = $y->EventName->runtimeField = "test";
			// echo $y->EventName->runtimeField;
			return new EventClass<T>(context, eventName, addMethod, removeMethod);
		}

		#endregion

		#region Add, Remove

		public void Add(T @delegate)
		{
			// pass the call to the real add accessor method
			if (addMethod != null) addMethod(@delegate);
			else
			{
				// removeMethod is surely non-null
				PhpException.Throw(PhpError.Error, CoreResources.GetString("event_has_no_add_accessor",
					DTypeDesc.Create(removeMethod.Method.DeclaringType).MakeFullName(), eventName));
			}
		}

		public void Remove(T @delegate)
		{
			// pass the call to the real remove accessor method
			if (removeMethod != null) removeMethod(@delegate);
			else
			{
				// addMethod is surely non-null
				PhpException.Throw(PhpError.Error, CoreResources.GetString("event_has_no_remove_accessor",
					DTypeDesc.Create(addMethod.Method.DeclaringType).MakeFullName(), eventName));
			}
		}

		#endregion

		#region GetStub

		/// <summary>
		/// Returns delegate of T type to CLR stub of the given target-routine pair.
		/// </summary>
		/// <param name="target">The target instance or <B>null</B>.</param>
		/// <param name="routine">The target routine desc.</param>
		/// <param name="realCalleeName">Real callee name if <paramref name="routine"/> is in fact <c>__call</c>,
		/// or <B>null</B> if <paramref name="routine"/> if the real callee.</param>
		/// <returns>
		/// Delegate to the stub or <B>null</B> if stub for this target-routine pair cannot be generated.
		/// </returns>
		/// <remarks>
		/// This method is used in cases when delegate type T is known at compile-time. By caching the corresponding
		/// delegate type desc in a static field (see <see cref="delegateDesc"/>), repeated type desc lookups are
		/// completely avoided.
		/// </remarks>
		internal static T GetStub(DObject target, DRoutineDesc/*!*/ routine, string realCalleeName)
		{
			return (T)(object)delegateDesc.StubBuilder.GetStub(target, routine, realCalleeName);
		}

		#endregion

		#region PHP Methods

		/// <summary>
		/// Private <c>__construct</c> prevents instantiation from PHP.
		/// </summary>
		[ImplementsMethod]
		private object __construct(ScriptContext context)
		{
			return null;
		}

		/// <summary>
		/// Converts the instance to a string.
		/// </summary>
		/// <returns>The string containing formatted trace.</returns>
		[ImplementsMethod]
		public object __toString(ScriptContext/*!*/ context)
		{
			return eventName;
		}

		/// <summary>
		/// Adds a delegate to the event's invocation list.
		/// </summary>
		[ImplementsMethod]
		public object Add(ScriptContext/*!*/ context, object handler)
		{
			bool success;
			T @delegate = Core.Convert.TryObjectToDelegate<T>(handler, out success);

			if (success) Add(@delegate);
			else PhpException.InvalidArgumentType("handler", delegateDesc.MakeFullName());

			return null;
		}

		/// <summary>
		/// Removes a delegate from the event's invocation list.
		/// </summary>
		[ImplementsMethod]
		public object Remove(ScriptContext/*!*/ context, object handler)
		{
			bool success;
			T @delegate = Core.Convert.TryObjectToDelegate<T>(handler, out success);

			if (success) Remove(@delegate);
			else PhpException.InvalidArgumentType("handler", delegateDesc.MakeFullName());

			return null;
		}

		#endregion

		#region Implementation Details

		/// <summary>
		/// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
		/// </summary>
		/// <param name="typeDesc">The type desc to populate.</param>
		internal static void __PopulateTypeDesc(PhpTypeDesc typeDesc)
		{
			typeDesc.AddMethod("__construct", PhpMemberAttributes.Private, __construct);
			typeDesc.AddMethod("__toString", PhpMemberAttributes.Public, __toString);
			typeDesc.AddMethod("Add", PhpMemberAttributes.Public, Add);
			typeDesc.AddMethod("Remove", PhpMemberAttributes.Public, Remove);
		}

		/// <summary>
		/// Creates a new <see cref="EventClass{T}"/>.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public EventClass(ScriptContext context, DTypeDesc caller)
			: base(context, true)
		{ }

		/// <summary>
		/// Creates a new <see cref="EventClass{T}"/>.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public EventClass(ScriptContext context, bool newInstance)
			: base(context, newInstance)
		{ }

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object __construct(object instance, PhpStack stack)
		{
			stack.RemoveFrame();
			return ((EventClass<T>)instance).__construct(stack.Context);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object __toString(object instance, PhpStack stack)
		{
			stack.RemoveFrame();
			return ((EventClass<T>)instance).__toString(stack.Context);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object Add(object instance, PhpStack stack)
		{
			object arg = stack.PeekValue(1);
			stack.RemoveFrame();
			return ((EventClass<T>)instance).Add(stack.Context, arg);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object Remove(object instance, PhpStack stack)
		{
			object arg = stack.PeekValue(1);
			stack.RemoveFrame();
			return ((EventClass<T>)instance).Remove(stack.Context, arg);
		}

		#endregion
	}

	#endregion
}

namespace PHP.Core
{
    #region IPhpDestructable

    /// <summary>
    /// Interface specifying PHP object with __destruct function defined. Used when PHP class is being emitted.
    /// </summary>
    public interface IPhpDestructable
    {
        /// <summary>
        /// PHP class destructor.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        object __destruct(ScriptContext context);
    }

    #endregion

    #region PhpObject

    /// <summary>
	/// Serves as a base for user defined PHP classes.
	/// </summary>
	[Serializable]
    [D.DebuggerNonUserCodeAttribute]
#if !SILVERLIGHT
    [D.DebuggerTypeProxy(typeof(PhpObject.DebugView))]
    [D.DebuggerDisplay("object ({this.TypeName,nq})", Type = "{this.TypeName,nq}")]
#endif
    public abstract class PhpObject : DObject
	{
        #region Debug View

        [D.DebuggerDisplay("object ({this.obj.TypeName,nq})", Type = "{this.obj.TypeName,nq}")]
        internal sealed class DebugView
        {
            [D.DebuggerBrowsable(D.DebuggerBrowsableState.Never)]
            private readonly PhpObject obj;

            public DebugView(PhpObject obj)
            {
                this.obj = obj;
            }

            [D.DebuggerBrowsable(D.DebuggerBrowsableState.RootHidden)]
            public PhpHashEntryDebugView[] Items
            {
                get
                {
                    List<PhpHashEntryDebugView> result = new List<PhpHashEntryDebugView>();

                    for (DTypeDesc desc = obj.TypeDesc; desc != null; desc = desc.Base)
                    {
                        foreach (var field in desc.Properties)
                        {
                            //if (field.Key.Value == PhpObjectBuilder.ProxyFieldName ||
                            //    field.Key.Value == PhpObjectBuilder.TypeDescFieldName)
                            //    continue;

                            var fielddesc = field.Value;

                            object value = fielddesc.Get(obj);
                            if (value is PhpReference && !((PhpReference)value).IsAliased) value = ((PhpReference)value).Value;

                            result.Add(new PhpHashEntryDebugView(new IntStringKey(field.Key.Value), value));
                        }
                    }

                    if (obj.RuntimeFields != null)
                        foreach (var field in obj.RuntimeFields)
                            result.Add(new PhpHashEntryDebugView(field.Key, field.Value));

                    return result.ToArray();
                }
            }
        }

        #endregion

		#region Fields and Properties

		/// <summary>
		/// <see cref="PhpObject"/> is its own real object.
		/// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public override object/*!*/ RealObject { get { return this; } }

        /// <summary>
        /// <see cref="PhpObject"/> is passed to its method and property invokes.
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public override object InstanceObject { get { return this; } }

		#endregion

		#region Construction and initialization

		/// <summary>
		/// Creates a new <c>PhpObject</c> and calls its PHP constructors (<c>__construct</c> or PHP 4 style constructor).
		/// </summary>
		/// <param name="context">The <see cref="ScriptContext"/> to create the instance with.</param>
		/// <param name="caller"><see cref="DTypeDesc"/> of the object that requests the construction.
		/// </param>
		/// <remarks>
		/// <para>
		/// This constructor should be used when instantiating PHP classes by the Phalanger compiler. Derived classes must
		/// also contain a constructor with this signature.
		/// </para>
		/// <para>
		/// In fact, this very constructor will never be called and is included here just to show how constructors
		/// with this signature whould work in derived classes. Firstly, the <see cref="PhpObject(ScriptContext,bool)"/>
		/// constructor of the same class is invoked in order to initialize all fields along the inheritance hierarchy.
		/// Then the <see cref="DObject.InvokeConstructor"/> method is called in order to locate and invoke a PHP constructor.
		/// There is no <c>base(context, callingTypeHandle)</c> upcall, in particular.
		/// </para>
		/// </remarks>
		public PhpObject(ScriptContext context, DTypeDesc caller)
			: this(context, true)
		{
			InvokeConstructor(context, caller);
		}

		/// <summary>
		/// Creates a new <c>PhpObject</c> without calling its PHP constructor.
		/// </summary>
		/// <param name="context">The <see cref="ScriptContext"/> to create the instance with.</param>
		/// <param name="newInstance">Determines whether this instance was created using the <c>new</c> construct
		/// (<B>true</B>), or just as a clone of another instance (<B>false</B>).</param>
		/// <remarks>
		/// This constructor is used when PHP constructors must not be called - when cloning <see cref="PhpObject"/>s
		/// (<paramref name="newInstance"/> is <B>false</B>) and when the caller is going to invoke a PHP constructor
		/// (<paramref name="newInstance"/> is <B>true</B>). Derived classes must also contain a constructor with this
		/// signature.
		/// </remarks>
		public PhpObject(ScriptContext context, bool newInstance)
		{
			if (newInstance)
			{
				// notify derived classes that a new instance has just been created
				InstanceCreated(context);
			}
		}

		/// <summary>
		/// Notifies derived classes that this instance has just been created.
		/// </summary>
		/// <remarks>
		/// Called before <c>__construct</c> is invoked. When overriding, make sure you call the
		/// overriden method (<c>base.InstanceCreated(context)</c>).
		/// </remarks>
		protected virtual void InstanceCreated(ScriptContext context)
		{ }

		#endregion

		#region ToString

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		public override string ToString()
		{
			return PhpTypeName;
		}

		#endregion

		#region Serialization (CLR only)
#if !SILVERLIGHT

		/// <include file='Doc/Common.xml' path='/docs/method[@name="serialization.ctor"]/*'/>
		protected PhpObject(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{ }

#endif
		#endregion
	}

    #endregion
}
