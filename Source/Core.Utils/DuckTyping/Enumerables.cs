using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace PHP.Core
{
	#region Enumerable interfaces

	/// <summary>
	/// Use this type as a return type if you want to return 
	/// value enumerator from PHP type using duck typing
	/// </summary>
	/// <typeparam name="T">Enumerated elements will be casted to this type</typeparam>
	public interface IDuckEnumerable<T> : IEnumerable<T>, IDuckType
	{		
	}

	/// <summary>
	/// Represents key-value pair used by PHP duck typing
	/// </summary>
	/// <typeparam name="K">Key type</typeparam>
	/// <typeparam name="T">Value type</typeparam>
	public interface IDuckKeyValue<K, T>
	{
		K Key { get; }
		T Value { get; }
	}

	/// <summary>
	/// Use this type as a return type if you want to retrun
	/// key-value enumerator from PHP type using duck typing
	/// </summary>
	/// <typeparam name="K">Keys will be converted to this type</typeparam>
	/// <typeparam name="T">Values will be converted to this type</typeparam>
	public interface IDuckKeyedEnumerable<K, T> : IEnumerable<IDuckKeyValue<K, T>>
	{
	}

	#endregion

	#region Enumerable implementation

	/// <summary>
	/// Key-value implementation
	/// </summary>
	public class DuckKeyValue<K, T> : IDuckKeyValue<K, T>
	{
		#region Members

		K _key; T _value;

		public DuckKeyValue(K key, T value)
		{
			_key = key; _value = value;
		}

		public K Key { get { return _key; } }
		public T Value { get { return _value; } }

		#endregion
	}

	/// <summary>
	/// Implements <seealso cref="IDuckEnumerable&lt;T>"/>
	/// </summary>
	public class DuckEnumerableWrapper<T> : DuckTypeBase, IDuckEnumerable<T>
	{
		#region Members

		public IDictionaryEnumerator _en;

		public DuckEnumerableWrapper(object original, IDictionaryEnumerator/*!*/ en) : base(original)
		{
			Debug.Assert(en != null);
			_en = en;
		}

		public IEnumerator<T> GetEnumerator()
		{
			object[] ducktype = typeof(T).GetCustomAttributes(typeof(DuckTypeAttribute), false);
			bool isDuck = (ducktype.Length > 0);

			while (_en.MoveNext())
			{
				if (isDuck)
				{
					yield return DuckTyping.Instance.ImplementDuckType<T>(_en.Value);
				}
				else
				{
					ConvertToClr.ConversionStrictness str;
					T ret = ConvertToClr.TryObjectToType<T>(_en.Value, out str);
					if (str == ConvertToClr.ConversionStrictness.Failed)
						throw new InvalidCastException();
					yield return ret;
				}
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}

	/// <summary>
	/// Implements <seealso cref="IDuckKeyedEnumerable&lt;K,T>"/>
	/// </summary>
	public class DuckKeyedEnumerableWrapper<K, T> : DuckTypeBase, IDuckKeyedEnumerable<K, T>
	{
		#region Members

		public IDictionaryEnumerator _en;

		public DuckKeyedEnumerableWrapper(object original, IDictionaryEnumerator/*!*/ en) : base(original)
		{
			Debug.Assert(en != null);
			_en = en;
		}

		public IEnumerator<IDuckKeyValue<K, T>> GetEnumerator()
		{
			object[] ducktypek = typeof(K).GetCustomAttributes(typeof(DuckTypeAttribute), false);
			object[] ducktypev = typeof(T).GetCustomAttributes(typeof(DuckTypeAttribute), false);
			bool isKeyDuck = (ducktypek.Length > 0);
			bool isValDuck = (ducktypev.Length > 0);

			while (_en.MoveNext())
			{
				K k; T t;
				if (isKeyDuck)
					k = DuckTyping.Instance.ImplementDuckType<K>(_en.Key);
				else
				{
					ConvertToClr.ConversionStrictness str;
					k = ConvertToClr.TryObjectToType<K>(_en.Key, out str);
					if (str == ConvertToClr.ConversionStrictness.Failed) throw new InvalidCastException();
				}
				if (isValDuck)
					t = DuckTyping.Instance.ImplementDuckType<T>(_en.Value);
				else
				{
					ConvertToClr.ConversionStrictness str;
					t = ConvertToClr.TryObjectToType<T>(_en.Value, out str);
					if (str == ConvertToClr.ConversionStrictness.Failed) throw new InvalidCastException();
				}
				yield return new DuckKeyValue<K, T>(k, t);
			}
		}


		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}

	#endregion
}
