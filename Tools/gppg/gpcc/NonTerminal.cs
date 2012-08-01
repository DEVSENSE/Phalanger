using System;
using System.Collections.Generic;
namespace gpcc
{
	public class NonTerminal : Symbol
	{
		private static int count;
		private int n;
		public List<Production> productions = new List<Production>();
		private object isNullable;
		public override int num
		{
			get
			{
				return -this.n;
			}
		}
		public NonTerminal(string name) : base(name)
		{
			this.n = ++NonTerminal.count;
		}
		public override bool IsNullable()
		{
			if (this.isNullable == null)
			{
				this.isNullable = false;
				foreach (Production current in this.productions)
				{
					bool flag = true;
					foreach (Symbol current2 in current.rhs)
					{
						if (!current2.IsNullable())
						{
							flag = false;
							break;
						}
					}
					if (flag)
					{
						this.isNullable = true;
						break;
					}
				}
			}
			return (bool)this.isNullable;
		}
	}
}
