using System;
using System.Collections.Generic;
namespace Lex
{
	public class Bunch
	{
		private class NfaComp : IComparer<Nfa>
		{
			public int Compare(Nfa a, Nfa b)
			{
				return a.Label - b.Label;
			}
		}
		private List<Nfa> nfa_set;
		private BitSet nfa_bit;
		private Accept accept;
		private int anchor;
		private int accept_index;
		public List<Nfa> GetNFASet()
		{
			return this.nfa_set;
		}
		public void SetNFASet(List<Nfa> a)
		{
			this.nfa_set = a;
		}
		public BitSet GetNFABit()
		{
			return this.nfa_bit;
		}
		public void SetNFABit(BitSet b)
		{
			this.nfa_bit = b;
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
		public void SetAnchor(int a)
		{
			this.anchor = a;
		}
		public int GetIndex()
		{
			return this.accept_index;
		}
		public void SetIndex(int i)
		{
			this.accept_index = i;
		}
		public Bunch(List<Nfa> nfa_start_states)
		{
			int count = nfa_start_states.Count;
			this.nfa_set = new List<Nfa>(nfa_start_states);
			this.nfa_bit = new BitSet(count);
			this.accept = null;
			this.anchor = 0;
			for (int i = 0; i < count; i++)
			{
				int label = this.nfa_set[i].Label;
				this.nfa_bit.Set(label, true);
			}
			this.accept_index = 2147483647;
		}
		public void dump()
		{
			Console.WriteLine("[CBunch Dump Begin]");
			if (this.nfa_set == null)
			{
				Console.WriteLine("nfa_set=null");
			}
			else
			{
				int count = this.nfa_set.Count;
				for (int i = 0; i < count; i++)
				{
					object obj = this.nfa_set[i];
					Console.Write("i={0} elem=", i);
					if (obj == null)
					{
						Console.WriteLine("null");
					}
					else
					{
						Nfa nfa = (Nfa)obj;
						nfa.dump();
					}
				}
			}
			if (this.nfa_bit == null)
			{
				Console.WriteLine("nfa_bit=null");
			}
			else
			{
				Console.Write("nfa_bit(" + this.nfa_bit.GetLength().ToString() + ")=");
				for (int j = 0; j < this.nfa_bit.GetLength(); j++)
				{
					if (this.nfa_bit.Get(j))
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
			if (this.accept == null)
			{
				Console.WriteLine("accept=null");
			}
			else
			{
				this.accept.Dump();
			}
			Console.WriteLine("anchor=" + this.anchor.ToString());
			Console.WriteLine("accept_index=" + this.accept_index.ToString());
		}
		public bool IsEmpty()
		{
			return this.nfa_set == null;
		}
		public void e_closure()
		{
			this.accept = null;
			this.anchor = 0;
			this.accept_index = 2147483647;
			Stack<Nfa> stack = new Stack<Nfa>();
			int count = this.nfa_set.Count;
			for (int i = 0; i < count; i++)
			{
				Nfa nfa = this.nfa_set[i];
				stack.Push(nfa);
			}
			while (stack.Count > 0)
			{
				object obj = stack.Pop();
				if (obj == null)
				{
					break;
				}
				Nfa nfa = (Nfa)obj;
				if (nfa.GetAccept() != null && nfa.Label < this.accept_index)
				{
					this.accept_index = nfa.Label;
					this.accept = nfa.GetAccept();
					this.anchor = nfa.GetAnchor();
				}
				if ('￼' == nfa.Edge)
				{
					if (nfa.Next != null && !this.nfa_set.Contains(nfa.Next))
					{
						this.nfa_bit.Set(nfa.Next.Label, true);
						this.nfa_set.Add(nfa.Next);
						stack.Push(nfa.Next);
					}
					if (nfa.Sibling != null && !this.nfa_set.Contains(nfa.Sibling))
					{
						this.nfa_bit.Set(nfa.Sibling.Label, true);
						this.nfa_set.Add(nfa.Sibling);
						stack.Push(nfa.Sibling);
					}
				}
			}
			if (this.nfa_set != null)
			{
				this.sort_states();
			}
		}
		public void sort_states()
		{
			this.nfa_set.Sort(0, this.nfa_set.Count, null);
		}
		public void move(Dfa dfa, int b)
		{
			List<Nfa> nFASet = dfa.GetNFASet();
			this.nfa_set = null;
			this.nfa_bit = null;
			int count = nFASet.Count;
			for (int i = 0; i < count; i++)
			{
				Nfa nfa = nFASet[i];
				if (b == (int)nfa.Edge || ('￾' == nfa.Edge && nfa.GetCharSet().contains(b)))
				{
					if (this.nfa_set == null)
					{
						this.nfa_set = new List<Nfa>();
						this.nfa_bit = new BitSet();
					}
					this.nfa_set.Add(nfa.Next);
					this.nfa_bit.Set(nfa.Next.Label, true);
				}
			}
			if (this.nfa_set != null)
			{
				this.sort_states();
			}
		}
	}
}
