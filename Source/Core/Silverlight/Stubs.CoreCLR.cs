using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Runtime.Serialization;

namespace PHP.CoreCLR
{

    public interface ICloneable
    {
        object Clone();
    }

    public class Hashtable: Dictionary<object,object>
    {
        class EqualityComparer : IEqualityComparer<object>
        {
            IEqualityComparer comparer;

            public EqualityComparer(IEqualityComparer cmp) 
            { 
                this.comparer = cmp; 
            }

            bool IEqualityComparer<object>.Equals(object x, object y)
            {
                return comparer.Equals(x, y);
            }

            int IEqualityComparer<object>.GetHashCode(object obj)
            {
                return comparer.GetHashCode(obj);
            }
        }

        public Hashtable()
        {

        }

        public Hashtable(IEqualityComparer cmp)
            : base(new EqualityComparer(cmp))
        {
        }

        public Hashtable(int capacity): base(capacity)
        {
            
        }

        public bool Contains(Object key)
        {
            return base.ContainsKey(key);
        }
        
    }

    public abstract class MarshalByRefObject
    {

    }

    public class Queue : Queue<object>
    {
        public Queue()
        {
        }

        public Queue(int capacity)
            : base(capacity)
        {
            
        }

        public Queue(IEnumerable collection)
        {
            foreach (object obj in collection)
            {
                base.Enqueue(obj);
            }
        }
    }

    public class Stack : Stack<object>
    {

    }

    public class ArrayList : List<object>
    {
        public ArrayList(int capacity)
            : base(capacity)
        {
        }

        public ArrayList()
        {
        }

        public new int Add(object obj)
        {
            base.Add(obj);
            return this.Count - 1;
        }
    }

	[AttributeUsage(AttributeTargets.All)]
	class SerializableAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.All)]
	class NonSerializedAttribute : Attribute
	{
	}

	public interface SerializationInfo
	{
		void AddValue(string key, object value);
	}

	public interface ISerializable
	{
	}

	public interface IDeserializationCallback
	{
	}

	public interface IObjectReference
	{
		object GetRealObject(StreamingContext context);
	}

	public class Win32IconResource
	{
		private Win32IconResource() { }
	}
}