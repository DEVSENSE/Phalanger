using System;
using System.Collections.Generic;
namespace Lex
{
	public class Dfa
	{
		private bool mark;
		private Accept accept;
		private int anchor;
		private List<Nfa> nfa_set;
		private BitSet nfa_bit;
		private int label;
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
		public Dfa(int l)
		{
			this.mark = false;
			this.accept = null;
			this.anchor = 0;
			this.nfa_set = null;
			this.nfa_bit = null;
			this.label = l;
		}
		public void dump()
		{
		}
		public bool IsMarked()
		{
			return this.mark;
		}
		public void SetMarked()
		{
			this.mark = true;
		}
		public void ClearMarked()
		{
			this.mark = false;
		}
	}
}
