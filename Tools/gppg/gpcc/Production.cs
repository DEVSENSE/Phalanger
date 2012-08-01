using System;
using System.Collections.Generic;
using System.Text;
namespace gpcc
{
	public class Production
	{
		public int num;
		public NonTerminal lhs;
		public List<Symbol> rhs = new List<Symbol>();
		public SemanticAction semanticAction;
		public Precedence prec;
		public Production(NonTerminal lhs)
		{
			this.lhs = lhs;
			lhs.productions.Add(this);
		}
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendFormat("{0} -> ", this.lhs);
			foreach (Symbol current in this.rhs)
			{
				stringBuilder.AppendFormat("{0} ", current);
			}
			return stringBuilder.ToString();
		}
	}
}
