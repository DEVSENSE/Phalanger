/*

 Copyright (c) 2004-2006 Ladislav Prosek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Serialization;
using PHP.Core.Reflection;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core
{
	/// <summary>
	/// Represents a callback designation passed to a system function.
	/// </summary>
	/// <remarks>
	/// <seealso cref="Convert.ObjectToCallback"/>
	/// </remarks>
	[Serializable]
	public class PhpCallback : ISerializable, IPhpConvertible
	{
		#region State

		/// <summary>
		/// State of a <see cref="PhpCallback"/>.
		/// </summary>
		public enum State
		{
			/// <summary>
			/// Unresolved function name (<see cref="PhpCallback.targetName"/>) is known.
			/// </summary>
			UnboundFunction = 0,

			/// <summary>
			/// Unresolved class name (<see cref="PhpCallback.className"/>) and method name (<see cref="PhpCallback.targetName"/>)
			/// are known.
			/// </summary>
			UnboundStaticMethod = 1,

			/// <summary>
			/// A <see cref="PhpObject"/> instance (<see cref="PhpCallback.instance"/>) and an unresolved method name
			/// (<see cref="PhpCallback.targetName"/>) are known.
			/// </summary>
			UnboundInstanceMethod = 2,

			/// <summary>
			/// The callback has been resolved into a <see cref="DRoutineDesc"/> (<see cref="PhpCallback.routineDesc"/>).
			/// </summary>
			Bound = 16,

			/// <summary>
			/// The callback has been resolved into a <see cref="DRoutineDesc"/> pointing to the <c>__call</c> method.
			/// </summary>
			BoundToCaller = 17
		}

		#endregion

		#region Fields and Properties

		/// <summary>
		/// The state of the callback.
		/// </summary>
		private State state;

		/// <summary>A handle of the target PHP method.</summary>
		/// <remarks>
		/// Valid if <see cref="state"/> is <see cref="State.Bound"/>, otherwise <B>null</B>.
		/// </remarks>
		private DRoutineDesc routineDesc;

		/// <summary>
		/// Script context the <see cref="routineDesc"/> should be called with.
		/// </summary>
		private ScriptContext context;

		/// <summary>
		/// The name of the target function or method for unbound callback.
		/// </summary>
		private string targetName;

		/// <summary>
		/// The name of the target class for unbound callback.
		/// </summary>
		private string className;

		/// <summary>
		/// The target instance.
		/// </summary>
		private DObject instance;

        /// <summary>
        /// Type used to call this routine.
        /// Used for late static binding.
        /// </summary>
        private DTypeDesc lateStaticBindType;

		/// <summary>
		/// <B>true</B> if <see cref="instance"/> is just a dummy instance created ad-hoc to be able to call an instance method
		/// statically, <B>false</B> otherwise.
		/// </summary>
		private bool dummyInstance;

		/// <summary>
		/// Type context (determined at bind time).
		/// </summary>
		private DTypeDesc callingContext;
        
		/// <summary>
		/// Returns <B>true</B> if this callback is bound, <B>false</B> otherwise.
		/// </summary>
		public bool IsBound
		{
			get
			{
				return ((state & State.Bound) != 0);
			}
		}

		/// <summary>
        /// Returns <B>true</B> if this callback is bound to a <c>__call</c> or <c>__callStatic</c> method, <B>false</B> otherwise.
		/// </summary>
		public bool IsBoundToCaller
		{
			get
			{
				return (state == State.BoundToCaller);
			}
		}

		/// <summary>
		/// Returns <B>true</B> if this <see cref="PhpCallback"/> is &quot;invalid&quot;, <B>false</B> otherwise.
		/// </summary>
		public bool IsInvalid
		{
			get
			{
				return (this == Invalid);
			}
		}

		/// <summary>
		/// Returns the target routine name (this is the real name even if bound to <c>__call</c>).
		/// </summary>
		public string RoutineName
		{
			get
			{ return targetName; }
		}

		/// <summary>
		/// Returns the target <see cref="DObject"/> if this <see cref="PhpCallback"/> references an instance method,
		/// <B>null</B> otherwise.
		/// </summary>
		public DObject TargetInstance
		{
			get
			{
				return (dummyInstance ? null : instance);
			}
			set
			{
				if (IsBound) throw new InvalidOperationException(CoreResources.GetString("cannot_change_target_instance"));
				instance = value;

                if (instance != null)
                    lateStaticBindType = instance.TypeDesc;
			}
		}

		/// <summary>
		/// Returns the target routine to which this callback is bound.
		/// </summary>
		public DRoutineDesc TargetRoutine
		{
			get
			{ return routineDesc; }
		}

		/// <summary>
		/// Invalid <see cref="PhpCallback"/> singleton.
		/// </summary>
		public static PhpCallback Invalid = new PhpCallback();

		#endregion

		#region Construction

		/// <summary>
		/// Creates an invalid <see cref="PhpCallback"/>.
		/// </summary>
		private PhpCallback()
		{ }

		public PhpCallback(RoutineDelegate functionDelegate, ScriptContext context)
		{
			// create a new DRoutineDesc based on the passed delegate
			routineDesc = new PhpRoutineDesc(PhpMemberAttributes.Static | PhpMemberAttributes.NamespacePrivate, functionDelegate, false);

			this.context = context;
			this.state = State.Bound;
		}

		/// <summary>
		/// Creates a callback bound to the specified PHP method represented by a <see cref="DRoutineDesc"/>.
		/// </summary>
		/// <param name="instance">The target PHP object.</param>
		/// <param name="handle">The handle of the target PHP method.</param>
		/// <param name="context">The script context to call the method with.</param>
		public PhpCallback(DObject instance, DRoutineDesc handle, ScriptContext context)
		{
			if (handle == null) throw new ArgumentNullException("handle");
            if (!handle.IsStatic)
            {
				if (instance == null) throw new ArgumentNullException("instance");
				this.instance = instance;
			}

			this.context = context;
			this.routineDesc = handle;
			this.state = State.Bound;

            if (instance != null)
                this.lateStaticBindType = instance.TypeDesc;
		}

		/// <summary>
		/// Creates an unbound PHP function callback given a function name.
		/// </summary>
		/// <param name="functionName">The target PHP function name.</param>
        public PhpCallback(string functionName)
        {
            if (functionName == null) throw new ArgumentNullException("functionName");

            this.state =
                (Name.IsClassMemberSyntax(functionName, out this.className, out this.targetName))
                ? this.state = State.UnboundStaticMethod
                : this.state = State.UnboundFunction;
        }

		/// <summary>
		/// Creates an unbound PHP function callback given a function name and <see cref="ScriptContext"/>.
		/// </summary>
		/// <param name="functionName">The target PHP function name.</param>
		/// <param name="context">The script context to call the function with.</param>
		public PhpCallback(string functionName, ScriptContext context)
            :this(functionName)
		{
			if (context == null) throw new ArgumentNullException("context");
			this.context = context;
		}

		/// <summary>
		/// Creates an unbound PHP static method callback given a class name and method name.
		/// </summary>
		/// <param name="className">The target PHP class name.</param>
		/// <param name="methodName">The target PHP method name.</param>
		public PhpCallback(string className, string methodName)
		{
			if (className == null) throw new ArgumentNullException("className");
			if (methodName == null) throw new ArgumentNullException("methodName");

			this.className = className;
			this.targetName = methodName;
			this.state = State.UnboundStaticMethod;
		}

		/// <summary>
		/// Creates an unbound PHP static method callback given a class name, method name and <see cref="ScriptContext"/>.
		/// </summary>
		/// <param name="className">The target PHP class name.</param>
		/// <param name="methodName">The target PHP method name.</param>
		/// <param name="context">The script context to call the method with.</param>
		public PhpCallback(string className, string methodName, ScriptContext context)
            :this(className, methodName)
		{
			if (context == null) throw new ArgumentNullException("context");
			this.context = context;
		}

		/// <summary>
		/// Creates an unbound PHP instance method callback given a <see cref="PhpObject"/> instance and a method name.
		/// </summary>
		/// <param name="instance">The target PHP object.</param>
		/// <param name="targetName">The target PHP function name.</param>
		public PhpCallback(DObject instance, string targetName)
		{
			if (instance == null) throw new ArgumentNullException("instance");
			if (targetName == null) throw new ArgumentNullException("targetName");

			this.instance = instance;
			this.targetName = targetName;
			this.state = State.UnboundInstanceMethod;
            this.lateStaticBindType = instance.TypeDesc;
		}

        /// <summary>
        /// Creates bounded PHP instance method callback. Used when we already know the routine.
        /// </summary>
        /// <param name="instance">The target PHP object.</param>
        /// <param name="routine">The target PHP method.</param>
        internal PhpCallback(DObject instance, DRoutineDesc routine)
        {
            Debug.Assert(instance != null);
            Debug.Assert(routine != null);

            this.instance = instance;
            this.targetName = routine.Member.FullName;
            this.state = State.Bound;
            this.routineDesc = routine;
            this.lateStaticBindType = instance.TypeDesc;
        }

		#endregion

		#region Binding

		/// <summary>
		/// Attempts to bind this callback to its target with no naming context.
		/// </summary>
		/// <returns><B>True</B> if the callback was successfully bound, <B>false</B> if an error occured.</returns>
		public bool Bind()
		{
			return Bind(false, UnknownTypeDesc.Singleton, null);
		}

		/// <summary>
		/// Attempts to bind this callback to its target with no naming context.
		/// </summary>
		/// <param name="quiet"><B>true</B> of no errors should be thrown, <B>false</B> otherwise.</param>
		/// <returns><B>True</B> if the callback was successfully bound, <B>false</B> if an error occured.</returns>
		public bool Bind(bool quiet)
		{
            return Bind(quiet, UnknownTypeDesc.Singleton, null);
		}

		/// <summary>
		/// Attempts to bind this callback to its target.
		/// </summary>
		/// <param name="quiet"><B>true</B> of no errors should be thrown, <B>false</B> otherwise.</param>
		/// <param name="nameContext">Current <see cref="NamingContext"/> for function and class name resolution.</param>
		/// <param name="caller">Current class context or a <see cref="UnknownTypeDesc"/> if the class context
		/// should be determined ad-hoc.</param>
		/// <returns><B>True</B> if the callback was successfully bound, <B>false</B> if an error occured.</returns>
		public bool Bind(bool quiet, DTypeDesc caller, NamingContext nameContext)
		{
			if (IsInvalid) return false;

			switch (state)
			{
				case State.UnboundFunction:
					{
						if (context == null) context = ScriptContext.CurrentContext;

						routineDesc = context.ResolveFunction(targetName, nameContext, quiet);
						if (routineDesc == null) return false;

						state = State.Bound;
						return true;
					}

				case State.UnboundStaticMethod:
					{
						if (context == null) context = ScriptContext.CurrentContext;

                        if (caller != null && caller.IsUnknown) callingContext = PhpStackTrace.GetClassContext();
                        else callingContext = caller;

						// try to find the CLR method

						// find the class according to className
						ResolveTypeFlags flags = ResolveTypeFlags.UseAutoload;
						if (!quiet) flags |= ResolveTypeFlags.ThrowErrors;

						DTypeDesc type = context.ResolveType(className, nameContext, callingContext, null, flags);
						if (type == null) return false;

						// find the method
                        bool is_caller_method;
                        lateStaticBindType = type;
						routineDesc = Operators.GetStaticMethodDesc(type, targetName,
                            ref instance, callingContext, context, quiet, false, out is_caller_method);

						if (routineDesc == null) return false;

						if (instance != null) dummyInstance = true;
                        state = is_caller_method ? State.BoundToCaller : State.Bound;
						return true;
					}

				case State.UnboundInstanceMethod:
					{
                        if (caller != null && caller.IsUnknown) callingContext = PhpStackTrace.GetClassContext();
                        else callingContext = caller;

						// ask the instance for a handle to the method
						bool is_caller_method;
						routineDesc = instance.GetMethodDesc(targetName, callingContext, quiet, out is_caller_method);
						if (routineDesc == null) return false;

						state = (is_caller_method ? State.BoundToCaller : State.Bound);
						return true;
					}
			}
			return true;
		}

        public void SwitchContext(ScriptContext/*!*/ newContext)
		{
			if (state != State.Bound && state != State.BoundToCaller)
				throw new InvalidOperationException();

			this.context = newContext;
		}

		#endregion

		#region Invocation

		/// <summary>
		/// Invokes this callback.
		/// </summary>
		/// <param name="args">Arguments to be passed to target function or method (can be <see cref="PhpReference"/>s).</param>
		/// <returns>The value returned by the called function or method (can be a <see cref="PhpReference"/>).</returns>
        [Emitted]
		public object Invoke(params object[] args)
		{
			if (!IsBound && !Bind()) return null;
			return InvokeInternal(args);
		}

        /// <summary>
        /// Invokes this callback. Can be used if DTypeDesc of caller's class context is known without an overhead of determining it.
        /// </summary>
        /// <param name="caller">DTypeDesc of the caller's class context.</param>
        /// <param name="args">Arguments to be passed to target function or method (can be <see cref="PhpReference"/>s).</param>
        /// <returns>The value returned by the called function or method (can be a <see cref="PhpReference"/>).</returns>
        public object Invoke(DTypeDesc caller, params object[] args)
        {
            if (!IsBound && !Bind(false, caller, null)) return null;
            return InvokeInternal(args);
        }


		/// <summary>
		/// Invokes this callback (must be bound).
		/// </summary>
		/// <param name="args">Arguments to be passed to target function or method (can be <see cref="PhpReference"/>s).</param>
		/// <returns>The value returned by the called function or method (can be a <see cref="PhpReference"/>).</returns>
		internal object InvokeInternal(params object[] args)
		{
			Debug.Assert(IsBound, "Callback must be bound.");
			Debug.Assert(routineDesc != null);// Since it's possible to call non static method statically we can't use this condition in the assert && (routineDesc.IsStatic || instance != null));

			// push args to PHP stack and invoke the desc
			PhpStack stack = (context != null ? context.Stack : ScriptContext.CurrentContext.Stack);

			if (state == State.BoundToCaller)
			{
				// push the real target name and args as a PhpArray
				stack.AddFrame(targetName, new PhpArray(args));
			}
			else stack.AddFrame(args);

			stack.Callback = true;
            stack.LateStaticBindType = this.lateStaticBindType;
			return routineDesc.Invoke(instance, stack, callingContext);
		}

		#endregion

		#region IPhpConvertible Members, ToPhpRepresentation

		/// <summary>
		/// Returns <see cref="PhpTypeCode.Invalid"/>.
		/// </summary>
		/// <returns><see cref="PhpTypeCode.Invalid"/></returns>
		public PhpTypeCode GetTypeCode()
		{
			return PhpTypeCode.Invalid;
		}

		/// <summary>
		/// Returns <c>0</c>.
		/// </summary>
		/// <returns><c>0</c></returns>
		public double ToDouble()
		{
			return 0.0;
		}

		/// <summary>
		/// Returns <c>0</c>.
		/// </summary>
		/// <returns><c>0</c></returns>
		public int ToInteger()
		{
			return 0;
		}

		/// <summary>
		/// Returns <c>0</c>.
		/// </summary>
		/// <returns><c>0</c></returns>
		public long ToLongInteger()
		{
			return 0;
		}

		/// <summary>
		/// Returns <B>false</B>.
		/// </summary>
		/// <returns><B>false</B></returns>
		public bool ToBoolean()
		{
			return false;
		}

		/// <summary>
		/// Converts this instance to a number of type <see cref="int"/>.
		/// </summary>
		public Convert.NumberInfo ToNumber(out int intValue, out long longValue, out double doubleValue)
		{
			doubleValue = 0.0;
			intValue = 0;
			longValue = 0;
			return Convert.NumberInfo.Integer;
		}

		/// <summary>
		/// Converts this instance to its <see cref="PhpBytes"/> representation.
		/// </summary>
		/// <returns>The converted value.</returns>
		public PhpBytes ToPhpBytes()
		{
			return new PhpBytes(((IPhpConvertible)this).ToString());
		}

		/// <summary>
		/// Converts this instance to its <see cref="String"/> representation.
		/// </summary>
		/// <returns>The converted value.</returns>
		string IPhpConvertible.ToString()
		{
			switch (state)
			{
				case State.UnboundFunction: return targetName;
				case State.UnboundStaticMethod: return String.Format("{0}::{1}", className, targetName);
				case State.UnboundInstanceMethod: return String.Format("{0}::{1}", instance.TypeName, targetName);

				case State.Bound:
					{
						if (instance == null)
						{
							return String.Format("{0}::{1}", routineDesc.DeclaringType.MakeFullName(), routineDesc.MakeFullName());
						}
						return String.Format("{0}::{1}", instance.TypeName, routineDesc.MakeFullName());
					}

				case State.BoundToCaller:
					{
						if (instance == null)
						{
							return String.Format("{0}::{1}", routineDesc.DeclaringType.MakeFullName(), targetName);
						}
						return String.Format("{0}::{1}", instance.TypeName, targetName);
					}

				default:
					Debug.Fail(); return null;
			}
		}

        /// <summary>
		/// Converts instance to its string representation according to PHP conversion algorithm.
		/// </summary>
		/// <param name="success">Indicates whether conversion was successful.</param>
		/// <param name="throwOnError">Throw out 'Notice' when conversion wasn't successful?</param>
		/// <returns>The converted value.</returns>
		string IPhpConvertible.ToString(bool throwOnError, out bool success)
		{
			success = false;
            return ((IPhpConvertible)this).ToString();
		}


		/// <summary>
		/// Returns PHP representation of this callback (either a string or a <see cref="PhpArray"/>
		/// with two items denoting the class/instance and method name).
		/// </summary>
		/// <returns>A string or <see cref="PhpArray"/> containing the two items.</returns>
		public object ToPhpRepresentation()
		{
			PhpArray array;

			switch (state)
			{
				// function_name
				case State.UnboundFunction:
					return targetName;

				// array(class_name, method_name)
				case State.UnboundStaticMethod:
					{
						array = new PhpArray();
						array.Add(className);
						array.Add(targetName);
						return array;
					}

				// array(instance, method_name)
				case State.UnboundInstanceMethod:
					{
						array = new PhpArray();
						array.Add(instance);
						array.Add(targetName);
						return array;
					}

				// function_name
				case State.Bound:
				case State.BoundToCaller:
					{
						if (routineDesc.DeclaringType.IsGlobal) return routineDesc.MakeFullName(); // function

						array = new PhpArray();
						if (instance != null && !dummyInstance) array.Add(instance);
						else array.Add(routineDesc.DeclaringType.MakeFullName());

						if (state == State.BoundToCaller) array.Add(targetName);
						else array.Add(routineDesc.MakeFullName());

						return array;
					}

				default:
					Debug.Fail(); return null;
			}
		}

		#endregion

		#region Serialization & ISerializable (CLR only)

#if !SILVERLIGHT
		/// <include file='Doc/Common.xml' path='/docs/method[@name="serialization.ctor"]/*'/>
		protected PhpCallback(SerializationInfo info, StreamingContext context)
		{
			className = info.GetString("className");
			targetName = info.GetString("targetName");
			instance = (PhpObject)info.GetValue("instance", typeof(PhpObject));
			state = (State)info.GetValue("state", typeof(State));

            if (instance != null)
                lateStaticBindType = instance.TypeDesc;
		}


		/// <include file='Doc/Common.xml' path='/docs/method[@name="GetObjectData"]/*'/>
        [System.Security.SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("className", className);
			info.AddValue("targetName", targetName);
			info.AddValue("instance", dummyInstance ? null : instance);

			// serialize an unbound callback (if possible)
			State new_state = state;
			if (new_state == State.Bound)
			{
				if (className != null) new_state = State.UnboundStaticMethod;
				else if (instance != null && !dummyInstance) new_state = State.UnboundInstanceMethod;
				else if (targetName != null) new_state = State.UnboundFunction;
			}
			info.AddValue("state", new_state);
		}
#endif

		#endregion

	}

	#region PhpCallbackParameterized

	/// <summary>
	/// Represents a callback along with the arguments that will be used for invocation.
	/// </summary>
	public struct PhpCallbackParameterized
	{
		/// <summary>
		/// The callback. Cannot be a <B>null</B> reference.
		/// </summary>
		public PhpCallback/*!*/ Callback { get { return callback; } }
		PhpCallback callback;

		/// <summary>
		/// The parameters. Can be a <B>null</B> reference.
		/// </summary>
		public object[] Parameters { get { return parameters; } }
		object[] parameters;

		/// <summary>
		/// Creates parameterized callback.
		/// </summary>
		/// <param name="callback">The callback.</param>
		/// <param name="parameters">The parameters.</param>
		public PhpCallbackParameterized(PhpCallback/*!*/ callback, params object[] parameters)
		{
			this.callback = callback;
			this.parameters = parameters;
		}

		/// <summary>
		/// Invokes the callback with the parameters.
		/// </summary>
		public void Invoke()
		{
            Invoke(UnknownTypeDesc.Singleton);
		}

        /// <summary>
        /// Invokes the callback with the parameters.
        /// </summary>
        /// <param name="caller">Current class context.</param>
        public void Invoke(DTypeDesc caller)
        {
            callback.Invoke(caller, parameters);
        }
	}

	#endregion
}
