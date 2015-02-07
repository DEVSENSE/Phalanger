using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace PHP.Core
{
	#region Multitype interfaces
	public interface IDuckOptionalValue
	{
		/// <summary>
		/// Gets a logical value indicating whether value represented by this object is valid.
		/// </summary>
		bool IsValid { get; }

		/// <summary>
		/// Gets an value represented by this object.
		/// </summary>
		object Value { get; }

		Type ValueType { get; }
	}

	/// <summary>
	/// Represents an generic optional value used in IDuckMultitype interface.
	/// </summary>
	/// <typeparam name="T">Type argument.</typeparam>
	public interface IDuckOptionalValue<T>
	{
		/// <summary>
		/// Gets a logical value indicating whether value represented by this object is valid.
		/// </summary>
		bool IsValid { get; }

		/// <summary>
		/// Gets an value represented by this object.
		/// </summary>
		T Value { get; }
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IDuckMultitype : IEnumerable<Type>
	{
		int OptionCount { get; }

		Type GetOptionType(int i);

		IDuckOptionalValue<T> GetOption<T>();
	}

	/// <summary>
	/// Use this type as a return or input type if you want the function
	/// to return two different types of values. The values are represented
	/// by IDuckOptionalValue objects and should be specified by duck-typing
	/// compliant type.
	/// </summary>
	/// <typeparam name="T">Specifies a type of first optional value.</typeparam>
	/// <typeparam name="U">Specifies a type of second optional value.</typeparam>
	public interface IDuckMultitype<T, U> : IDuckMultitype
	{
		/// <summary>
		/// First optional value of the multitype.
		/// </summary>
		IDuckOptionalValue<T> First { get; }

		/// <summary>
		/// Second optional value of the multitype.
		/// </summary>
		IDuckOptionalValue<U> Second { get; }
	}

	/// <summary>
	/// Use this type as a return or input type if you want the function
	/// to return three different types of values. The values are represented
	/// by IDuckOptionalValue objects and should be specified by duck-typing
	/// compliant type.
	/// </summary>
	/// <typeparam name="T">Specifies a type of first optional value.</typeparam>
	/// <typeparam name="U">Specifies a type of second optional value.</typeparam>
	/// <typeparam name="V">Specifies a type of third optional value.</typeparam>
	public interface IDuckMultitype<T, U, V>
	{
		/// <summary>
		/// First optional value of the multitype.
		/// </summary>
		IDuckOptionalValue<T> First { get; }

		/// <summary>
		/// Second optional value of the multitype.
		/// </summary>
		IDuckOptionalValue<U> Second { get; }

		/// <summary>
		/// Third optional value of the multitype.
		/// </summary>
		IDuckOptionalValue<V> Third { get; }
	}
	#endregion

	#region Multitype implementation

	/// <summary>
	/// Implementation of IDuckOptionalValue interface.
	/// </summary>
	/// <typeparam name="T">Type argument.</typeparam>
	public class DuckOptionalValue<T> : IDuckOptionalValue<T>
	{
		/// <summary>
		/// Gets an value represented by this object.
		/// </summary>
		public T Value { get { if (!isValid) throw new InvalidOperationException("Optional value is not valid."); else return value; } }
		T value;

		/// <summary>
		/// Gets a logical value indicating whether value represented by this object is valid.
		/// </summary>
		public bool IsValid { get { return isValid; } }
		bool isValid;

		/// <summary>
		/// Initializes an invalid instance of duck optional value. This means that the value
		/// contained is not meant to be read.
		/// </summary>
		internal DuckOptionalValue()
		{
			this.isValid = false;
		}

		/// <summary>
		/// Initializes an valid instance of duck optional value. This means that the value
		/// contained can be read.
		/// </summary>
		/// <param name="value">Value of the object.</param>
		public DuckOptionalValue(T value)
		{
			this.value = value;
			this.isValid = true;
		}
	}

	/// <summary>
	/// Common implementation of IDuckMultitype interface.
	/// </summary>
	public abstract class DuckMultitype : IDuckMultitype
	{
		List<Type> types;
		IDictionary<Type,object> values;

		public int OptionCount { get { return types.Count; } }

		internal DuckMultitype(params Tuple<Type,object>[] objects)
		{
			values = new Dictionary<Type,object>();
			types = new List<Type>();

			for(int i = 0; i < objects.Length; i++)
			{
				types.Add(objects[i].Item1);
                values.Add(objects[i].Item1, objects[i].Item2);
			}
		}

		public Type GetOptionType(int i)
		{
			return types[i];
		}

		public IDuckOptionalValue<T> GetOption<T>()
		{
			if (values.ContainsKey(typeof(T)))
			{
				T a = (T)values[typeof(T)];

				if (values[typeof(T)] == null)
				{
					return new DuckOptionalValue<T>();
				}
				else
				{
					return new DuckOptionalValue<T>(a);
				}
			}
			else
			{
				return null;
			}
		}

		public IEnumerator<Type> GetEnumerator()
		{
			return (IEnumerator<Type>)types.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return (IEnumerator)types.GetEnumerator();
		}
	}

	/// <summary>
	/// Implementation of IDuckMultitype interface.
	/// </summary>
	/// <typeparam name="T">Specifies a type of first optional value.</typeparam>
	/// <typeparam name="U">Specifies a type of second optional value.</typeparam>
	public class DuckMultitype<T, U> : DuckMultitype, IDuckMultitype<T, U>
	{
		/// <summary>
		/// First optional value of the multitype.
		/// </summary>
		public IDuckOptionalValue<T> First { get { return GetOption<T>(); } }
 
		/// <summary>
		/// Second optional value of the multitype.
		/// </summary>
		public IDuckOptionalValue<U> Second { get { return GetOption<U>(); } }

		/// <summary>
		/// Initializes an instance of DuckMultitype class implementing two-value IDuckMultitype interface.
		/// </summary>
		/// <param name="first"></param>
		/// <param name="second"></param>
		public DuckMultitype(object first, object second) : 
			base(new Tuple<Type, object>(typeof(T), first), new Tuple<Type, object>(typeof(U), second))
		{
		}
	}

	/// <summary>
	/// Implementation of IDuckMultitype interface.
	/// </summary>
	/// <typeparam name="T">Specifies a type of first optional value.</typeparam>
	/// <typeparam name="U">Specifies a type of second optional value.</typeparam>
	/// <typeparam name="V">Specifies a type of third optional value.</typeparam>
	public class DuckMultitype<T, U, V> : DuckMultitype, IDuckMultitype<T, U, V>
	{
		/// <summary>
		/// First optional value of the multitype.
		/// </summary>
		public IDuckOptionalValue<T> First { get { return GetOption<T>();} }
 
		/// <summary>
		/// Second optional value of the multitype.
		/// </summary>
		public IDuckOptionalValue<U> Second { get { return GetOption<U>();} }

		/// <summary>
		/// Third optional value of the multitype.
		/// </summary>
		public IDuckOptionalValue<V> Third { get { return GetOption<V>();} }

		/// <summary>
		/// Initializes an instance of DuckMultitype class implementing three-value IDuckMultitype interface.
		/// </summary>
		/// <param name="first"></param>
		/// <param name="second"></param>
        /// <param name="third"></param>
		public DuckMultitype(object first, object second, object third) :
			base(new Tuple<Type, object>(typeof(T), first), new Tuple<Type, object>(typeof(U), second), new Tuple<Type, object>(typeof(V), third))
		{
		}
	}

	#endregion
}
