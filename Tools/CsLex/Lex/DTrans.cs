using System;
namespace Lex
{
	public class DTrans
	{
		public const int F = -1;
		private int[] dtrans;
		private Accept accept;
		private int anchor;
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
		public int GetAnchor()
		{
			return this.anchor;
		}
		public void SetAnchor(int i)
		{
			this.anchor = i;
		}
		public Accept GetAccept()
		{
			return this.accept;
		}
		public void SetAccept(Accept a)
		{
			this.accept = a;
		}
		public void SetDTrans(int dest, int index)
		{
			this.dtrans[dest] = index;
		}
		public int GetDTrans(int i)
		{
			return this.dtrans[i];
		}
		public int GetDTransLength()
		{
			return this.dtrans.Length;
		}
		public DTrans(Spec s, Dfa dfa)
		{
			this.dtrans = new int[s.dtrans_ncols];
			this.label = s.dtrans_list.Count;
			this.accept = dfa.GetAccept();
			this.anchor = dfa.GetAnchor();
		}
	}
}
