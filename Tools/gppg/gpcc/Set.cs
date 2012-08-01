using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
namespace gpcc
{
	public class Set<T> : IEnumerable<T>, IEnumerable
	{
		private Dictionary<T, bool> elements = new Dictionary<T, bool>();
		public Set()
		{
		}
		public Set(Set<T> items)
		{
			this.AddRange(items);
		}
		public void Add(T item)
		{
			this.elements[item] = true;
		}
		public void AddRange(Set<T> items)
		{
			foreach (T current in items)
			{
				this.Add(current);
			}
		}
		public IEnumerator<T> GetEnumerator()
		{
			return this.elements.Keys.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new Exception("The method or operation is not implemented.");
		}
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("[");
			foreach (T current in this.elements.Keys)
			{
				stringBuilder.AppendFormat("{0}, ", current);
			}
			stringBuilder.Append("]");
			return stringBuilder.ToString();
		}
	}
}
