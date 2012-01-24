/*

 Copyright (c) 2004-2006 Tomas Matousek and Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using PHP.Core.Emit;

namespace PHP.Core.Emit
{
	#region PlaceHolder, IPlace

	/// <summary>
	/// Type of the place where a value is stored.
	/// </summary>
	internal enum PlaceHolder
	{
		/// <summary>
		/// The value has no storage, it is a direct value.
		/// </summary>
		None,

		/// <summary>
		/// The value is stored in a method argument.
		/// </summary>
		Argument,

		/// <summary>
		/// The value is stored in a local variable.
		/// </summary>
		Local,

		/// <summary>
		/// The value is stored in a field.
		/// </summary>
		Field
	}

	/// <summary>
	/// Interface supported by storage places.
	/// </summary>
	public interface IPlace
	{
		/// <summary>
		/// Emits code that loads the value from this storage place.
		/// </summary>
		/// <param name="il">The <see cref="ILEmitter"/> to emit the code to.</param>
		void EmitLoad(ILEmitter il);

		/// <summary>
		/// Emits code that stores a value to this storage place.
		/// </summary>
		/// <param name="il">The <see cref="ILEmitter"/> to emit the code to.</param>
		void EmitStore(ILEmitter il);

		/// <summary>
		/// Emits code that loads address of this storage place.
		/// </summary>
		/// <param name="il">The <see cref="ILEmitter"/> to emit the code to.</param>
		void EmitLoadAddress(ILEmitter il);

		/// <summary>
		/// Gets whether the place has an address.
		/// </summary>
		bool HasAddress { get; }

		/// <summary>
		/// Returns the <see cref="Type"/> of the value stored in this storage place.
		/// </summary>
		Type PlaceType { get; }
	}

	#endregion

	#region IndexedPlace

	/// <summary>
	/// A storage place that represents a local variable or a method argument given by their index,
	/// or a direct integer value.
	/// </summary>
	internal sealed class IndexedPlace : IPlace
	{
		/// <summary>
		/// The type of this place - can be either <see cref="PlaceHolder.None"/>, <see cref="PlaceHolder.Argument"/> or
		/// <see cref="PlaceHolder.Local"/>.
		/// </summary>
		private PlaceHolder holder;

		/// <summary>
		/// Sets or gets the index (direct value).
		/// </summary>
		public int Index { get { return index; } set { index = value; } }

		/// <summary>
		/// The index/direct value.
		/// </summary>
		private int index;

		/// <summary>
		/// A special read-only <see cref="IndexedPlace"/> that loads <B>this</B> (0th argument).
		/// </summary>
		public static readonly IndexedPlace ThisArg = new IndexedPlace(PlaceHolder.Argument, 0);

		/// <summary>
		/// Creates a new <see cref="IndexedPlace"/> of a given type and with a given index/direct value.
		/// </summary>
		/// <param name="holder">The place type. Should be either <see cref="PlaceHolder.None"/>,
		/// <see cref="PlaceHolder.Argument"/> or <see cref="PlaceHolder.Local"/>.</param>
		/// <param name="index">The index (direct value).</param>
		public IndexedPlace(PlaceHolder holder, int index)
		{
			if (holder != PlaceHolder.None && holder != PlaceHolder.Argument && holder != PlaceHolder.Local)
				throw new ArgumentOutOfRangeException("holder");

			this.holder = holder;
			this.index = index;
		}

        /// <summary>
        /// Creates a new <see cref="IndexedPlace"/> of given local variable.
        /// </summary>
        /// <param name="local">Local variable to be used.</param>
        public IndexedPlace(LocalBuilder/*!*/local)
            :this(PlaceHolder.Local, local.LocalIndex)
        {
        }

		/// <summary>
		/// Emits code that loads the value from this storage place.
		/// </summary>
		/// <param name="il">The <see cref="ILEmitter"/> to emit the code to.</param>
		public void EmitLoad(ILEmitter il)
		{
			switch (holder)
			{
				case PlaceHolder.Local: il.Ldloc(index); break;
				case PlaceHolder.Argument: il.Ldarg(index); break;
				case PlaceHolder.None: il.LdcI4(index); break;
			}
		}

		/// <summary>
		/// Emits code that loads address of this storage place.
		/// </summary>
		/// <param name="il">The <see cref="ILEmitter"/> to emit the code to.</param>
		public void EmitLoadAddress(ILEmitter il)
		{
			switch (holder)
			{
				case PlaceHolder.Local: il.Ldloca(index); break;
				case PlaceHolder.Argument: il.Ldarga(index); break;
				case PlaceHolder.None: throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Gets whether the place has an address.
		/// </summary>
		public bool HasAddress
		{
			get
			{
				switch (holder)
				{
					case PlaceHolder.Local:
					case PlaceHolder.Argument: return true;
					default: return false;
				}
			}
		}

		/// <summary>
		/// Emits code that stores a value to this storage place.
		/// </summary>
		/// <param name="il">The <see cref="ILEmitter"/> to emit the code to.</param>
		public void EmitStore(ILEmitter il)
		{
			switch (holder)
			{
				case PlaceHolder.Local: il.Stloc(index); break;
				case PlaceHolder.Argument: il.Starg(index); break;
				case PlaceHolder.None: throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Returns the <see cref="Type"/> of the value stored in this storage place.
		/// </summary>
		public Type PlaceType
		{
			get
			{
				switch (holder)
				{
					case PlaceHolder.Local: throw new InvalidOperationException();
					case PlaceHolder.Argument: throw new InvalidOperationException();
					case PlaceHolder.None: return typeof(int);
				}
				return null;
			}
		}
	}

	#endregion

	#region TokenPlace

	/// <summary>
	/// A read-only storage place that represents a metadata token.
	/// </summary>
	internal sealed class TokenPlace : IPlace
	{
		/// <summary>
		/// Runtime representation of the token.
		/// </summary>
		private MemberInfo source;

		/// <summary>
		/// Creates a new <see cref="TokenPlace"/> given a <see cref="MemberInfo"/>.
		/// </summary>
		/// <param name="source">The <see cref="MemberInfo"/>.</param>
		public TokenPlace(MemberInfo source)
		{
			this.source = source;
		}

		#region IPlace Members

		/// <summary>
		/// Emits code that loads the value from this storage place.
		/// </summary>
		/// <param name="il">The <see cref="ILEmitter"/> to emit the code to.</param>
		public void EmitLoad(ILEmitter il)
		{
			MethodInfo method;
			FieldInfo field;
			Type type;

			if ((type = source as Type) != null)
			{
				il.Emit(OpCodes.Ldtoken, type);
			}
			else
				if ((method = source as MethodInfo) != null)
				{
					il.Emit(OpCodes.Ldtoken, method);
				}
				else
					if ((field = source as FieldInfo) != null)
					{
						il.Emit(OpCodes.Ldtoken, field);
					}
					else
						throw new InvalidOperationException();
		}

		public void EmitLoadAddress(ILEmitter il)
		{
			throw new InvalidOperationException();
		}

		public bool HasAddress { get { return false; } }

		/// <summary>
		/// Emits code that stores a value to this storage place.
		/// </summary>
		/// <param name="il">The <see cref="ILEmitter"/> to emit the code to.</param>
		public void EmitStore(ILEmitter il)
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Returns the <see cref="Type"/> of the value stored in this storage place.
		/// </summary>
		public Type PlaceType
		{
			get
			{
				MethodInfo method;
				FieldInfo field;
				Type type;

				if ((type = source as Type) != null) return typeof(RuntimeTypeHandle);
				else
					if ((method = source as MethodInfo) != null) return typeof(RuntimeMethodHandle);
					else
						if ((field = source as FieldInfo) != null) return typeof(RuntimeFieldHandle);
						else
							throw new InvalidOperationException();

			}
		}

		#endregion
	}

	#endregion

	#region Place

	/// <summary>
	/// A storage place that represents a local variable, a field, or a property given by their
	/// <see cref="LocalBuilder"/>, <see cref="FieldInfo"/>, or <see cref="PropertyInfo"/>.
	/// </summary>
	internal sealed class Place : IPlace
	{
		/// <summary>
		/// Holder of the field or a <B>null</B> reference (a local variable or a static field).
		/// </summary>
		private IPlace holder;

		/// <summary>
		/// The <see cref="LocalBuilder"/>, <see cref="FieldInfo"/>, or <see cref="PropertyInfo"/>
		/// where the value is stored.
		/// </summary>
		private object/*!*/ source;

		#region Construction

		/// <summary>
		/// Creates a new <see cref="Place"/> given an <see cref="IPlace"/> representing an instance
		/// and a <see cref="FieldInfo"/>.
		/// </summary>
		/// <param name="holder">The instance <see cref="IPlace"/> (<B>null</B> for static fields).</param>
		/// <param name="field">The <see cref="FieldInfo"/>.</param>
		public Place(IPlace holder, FieldInfo/*!*/ field)
		{
			Debug.Assert(field != null && (holder == null) == field.IsStatic);

			this.holder = holder;
			this.source = field;
		}

		/// <summary>
		/// Creates a new <see cref="Place"/> given an <see cref="IPlace"/> representing an instance
		/// and a <see cref="PropertyInfo"/>.
		/// </summary>
		/// <param name="holder">The instance <see cref="IPlace"/> (<B>null</B> for static properties).</param>
		/// <param name="property">The <see cref="PropertyInfo"/>.</param>
		public Place(IPlace holder, PropertyInfo/*!*/ property)
		{
			Debug.Assert(property != null && (holder == null) == property.GetGetMethod().IsStatic);

			this.holder = holder;
			this.source = property;
		}

		/// <summary>
		/// Creates a new <see cref="Place"/> given a <see cref="LocalBuilder"/>.
		/// </summary>
		/// <param name="local">The <see cref="LocalBuilder"/>.</param>
		public Place(LocalBuilder/*!*/ local)
		{
			Debug.Assert(local != null);

			holder = null;
			source = local;
		}

		#endregion

		#region IPlace Members

		/// <summary>
		/// Emits code that loads the value from this storage place.
		/// </summary>
		/// <param name="il">The <see cref="ILEmitter"/> to emit the code to.</param>
		public void EmitLoad(ILEmitter il)
		{
			if (holder != null) il.Load(holder);
			il.Load(source);
		}

		/// <summary>
		/// Emits code that stores a value to this storage place.
		/// </summary>
		/// <param name="il">The <see cref="ILEmitter"/> to emit the code to.</param>
		public void EmitStore(ILEmitter il)
		{
			if (holder != null) il.Store(holder);
			il.Store(source);
		}

		/// <summary>
		/// Emits code that loads address of this storage place.
		/// </summary>
		/// <param name="il">The <see cref="ILEmitter"/> to emit the code to.</param>
		public void EmitLoadAddress(ILEmitter il)
		{
            if (holder != null) il.Load(holder);
			il.LoadAddress(source);
		}

		/// <summary>
		/// Gets whether the place has an address.
		/// </summary>
		public bool HasAddress
		{
			get
			{
				return ILEmitter.HasAddress(source);
			}
		}

		/// <summary>
		/// Returns the <see cref="Type"/> of the value stored in this storage place.
		/// </summary>
		public Type PlaceType
		{
			get
			{
				LocalBuilder local;
				FieldInfo field;

				if ((local = source as LocalBuilder) != null)
					return local.LocalType;
				else if ((field = source as FieldInfo) != null)
					return field.FieldType;
				else
					return ((PropertyInfo)source).PropertyType;
			}
		}

		#endregion
	}

	#endregion

	#region LiteralPlace

	/// <summary>
	/// Represents a literal.
	/// </summary>
	internal sealed class LiteralPlace : IPlace
	{
		/// <summary>
		/// Literal represented by the place.
		/// </summary>
		private object literal;

		/// <summary>
		/// A special read-only <see cref="Place"/> that loads <B>null</B>.
		/// </summary>
		public static readonly LiteralPlace Null = new LiteralPlace(null);

		public LiteralPlace(object literal)
		{
			this.literal = literal;
		}

		public void EmitLoad(ILEmitter il)
		{
			il.LoadLiteral(literal);
		}

		public void EmitStore(ILEmitter il)
		{
			throw new InvalidOperationException();
		}

		public void EmitLoadAddress(ILEmitter il)
		{
			throw new InvalidOperationException();
		}

		public bool HasAddress
		{
			get
			{
				return false;
			}
		}

		public Type PlaceType
		{
			get
			{
				return (literal != null) ? literal.GetType() : null;
			}
		}
	}

	#endregion

	#region MethodCallPlace

	internal sealed class MethodCallPlace : IPlace
	{
		private MethodInfo/*!*/ methodInfo;
		private IPlace[]/*!!*/ argumentPlaces;
		private bool virtualCall;

		public MethodCallPlace(MethodInfo/*!*/ methodInfo, bool virtualCall, params IPlace[]/*!!*/ argumentPlaces)
		{
			Debug.Assert(methodInfo.ReturnParameter.ParameterType != Types.Void);
			this.methodInfo = methodInfo;
			this.argumentPlaces = argumentPlaces;
			this.virtualCall = virtualCall;
		}

		#region IPlace Members

		public void EmitLoad(ILEmitter/*!*/ il)
		{
			for (int i = 0; i < argumentPlaces.Length; i++)
				argumentPlaces[i].EmitLoad(il);

			il.Emit((virtualCall) ? OpCodes.Callvirt : OpCodes.Call, methodInfo);
		}

		public void EmitStore(ILEmitter/*!*/ il)
		{
			throw new InvalidOperationException();
		}

		public void EmitLoadAddress(ILEmitter/*!*/ il)
		{
			throw new InvalidOperationException();
		}

		public bool HasAddress
		{
			get { return false; }
		}

		public Type/*!*/ PlaceType
		{
			get { return methodInfo.ReturnType; }
		}

		#endregion
	}

	#endregion

    #region NewobjPlace

    internal sealed class NewobjPlace : IPlace
    {
        private ConstructorInfo/*!*/ ctorInfo;
        private IPlace[]/*!!*/ argumentPlaces;

        public NewobjPlace(ConstructorInfo/*!*/ ctorInfo, params IPlace[]/*!!*/ argumentPlaces)
        {
            Debug.Assert(argumentPlaces.Length == ctorInfo.GetParameters().Length);

            this.ctorInfo = ctorInfo;
            this.argumentPlaces = argumentPlaces;
        }

        #region IPlace Members

        public void EmitLoad(ILEmitter/*!*/ il)
        {
            for (int i = 0; i < argumentPlaces.Length; ++i)
                argumentPlaces[i].EmitLoad(il);

            il.Emit(OpCodes.Newobj, ctorInfo);
        }

        public void EmitStore(ILEmitter/*!*/ il)
        {
            throw new InvalidOperationException();
        }

        public void EmitLoadAddress(ILEmitter/*!*/ il)
        {
            throw new InvalidOperationException();
        }

        public bool HasAddress
        {
            get { return false; }
        }

        public Type/*!*/ PlaceType
        {
            get { return ctorInfo.DeclaringType; }
        }

        #endregion
    }

    #endregion
}
