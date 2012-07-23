using System;
namespace Lex
{
	public class Nfa : IComparable
	{
		public const char CCL = '￾';
		public const char EMPTY = '�';
		public const char EPSILON = '￼';
		public const int NO_LABEL = -1;
		private char edge;
		private CharSet cset;
		private Nfa next;
		private Nfa sibling;
		private Accept accept;
		private int anchor;
		private int label;
		private BitSet states;
		public char Edge
		{
			get
			{
				return this.edge;
			}
			set
			{
				this.edge = value;
			}
		}
		public Nfa Next
		{
			get
			{
				return this.next;
			}
			set
			{
				this.next = value;
			}
		}
		public Nfa Sibling
		{
			get
			{
				return this.sibling;
			}
			set
			{
				this.sibling = value;
			}
		}
		public int Label
		{
			get
			{
				return this.label;
			}
			set
			{
				this.label = value;
			}
		}
		public CharSet GetCharSet()
		{
			return this.cset;
		}
		public void SetCharSet(CharSet s)
		{
			this.cset = s;
		}
		public Accept GetAccept()
		{
			return this.accept;
		}
		public void SetAccept(Accept a)
		{
			this.accept = a;
		}
		public int GetAnchor()
		{
			return this.anchor;
		}
		public void SetAnchor(int i)
		{
			this.anchor = i;
		}
		public BitSet GetStates()
		{
			return this.states;
		}
		public void SetStates(BitSet b)
		{
			this.states = b;
		}
		public Nfa()
		{
			this.edge = '�';
			this.cset = null;
			this.next = null;
			this.sibling = null;
			this.accept = null;
			this.anchor = 0;
			this.label = -1;
			this.states = null;
		}
		public void dump()
		{
			Console.WriteLine("[Nfa begin dump]");
			Console.WriteLine("label=" + this.label);
			Console.WriteLine("edge=" + this.edge);
			Console.Write("set=");
			if (this.cset == null)
			{
				Console.WriteLine("null");
			}
			else
			{
				Console.WriteLine(this.cset);
			}
			Console.Write("next=");
			if (this.next == null)
			{
				Console.WriteLine("null");
			}
			else
			{
				Console.WriteLine(this.next);
			}
			Console.Write("next2=");
			if (this.sibling == null)
			{
				Console.WriteLine("null");
			}
			else
			{
				Console.WriteLine(this.sibling);
			}
			Console.Write("accept=");
			if (this.accept == null)
			{
				Console.WriteLine("null");
			}
			else
			{
				this.accept.Dump();
			}
			Console.WriteLine("anchor=" + this.anchor);
			Console.Write("states=");
			if (this.states == null)
			{
				Console.WriteLine("null");
			}
			else
			{
				for (int i = 0; i < this.states.GetLength(); i++)
				{
					if (this.states.Get(i))
					{
						Console.Write("1");
					}
					else
					{
						Console.Write("0");
					}
				}
				Console.WriteLine("");
			}
			Console.WriteLine("[Nfa end dump]");
		}
		public void mimic(Nfa nfa)
		{
			this.edge = nfa.edge;
			if (nfa.cset != null)
			{
				if (this.cset == null)
				{
					this.cset = new CharSet();
				}
				this.cset.mimic(nfa.cset);
			}
			else
			{
				this.cset = null;
			}
			this.next = nfa.next;
			this.sibling = nfa.sibling;
			this.accept = nfa.accept;
			this.anchor = nfa.anchor;
			if (nfa.states != null)
			{
				this.states = new BitSet(nfa.states);
				return;
			}
			this.states = null;
		}
		public int CompareTo(object y)
		{
			return this.label - ((Nfa)y).label;
		}
	}
}
