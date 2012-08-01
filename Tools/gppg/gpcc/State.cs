using System;
using System.Collections.Generic;
using System.Text;
namespace gpcc
{
	public class State
	{
		private static int TotalStates;
		public int num;
		public Symbol accessedBy;
		public List<ProductionItem> kernal_items = new List<ProductionItem>();
		public List<ProductionItem> all_items = new List<ProductionItem>();
		public Dictionary<Symbol, State> Goto = new Dictionary<Symbol, State>();
		public Set<Terminal> terminalTransitions = new Set<Terminal>();
		public Dictionary<NonTerminal, Transition> nonTerminalTransitions = new Dictionary<NonTerminal, Transition>();
		public Dictionary<Terminal, ParserAction> parseTable = new Dictionary<Terminal, ParserAction>();
		public State(Production production)
		{
			this.num = State.TotalStates++;
			this.AddKernal(production, 0);
		}
		public State(List<ProductionItem> itemSet)
		{
			this.num = State.TotalStates++;
			this.kernal_items.AddRange(itemSet);
			this.all_items.AddRange(itemSet);
		}
		public void AddClosure()
		{
			foreach (ProductionItem current in this.kernal_items)
			{
				this.AddClosure(current);
			}
		}
		private void AddClosure(ProductionItem item)
		{
			if (item.pos < item.production.rhs.Count)
			{
				Symbol symbol = item.production.rhs[item.pos];
				if (symbol is NonTerminal)
				{
					foreach (Production current in ((NonTerminal)symbol).productions)
					{
						this.AddNonKernal(current);
					}
				}
			}
		}
		private void AddKernal(Production production, int pos)
		{
			ProductionItem item = new ProductionItem(production, pos);
			this.kernal_items.Add(item);
			this.all_items.Add(item);
		}
		private void AddNonKernal(Production production)
		{
			ProductionItem item = new ProductionItem(production, 0);
			if (!this.all_items.Contains(item))
			{
				this.all_items.Add(item);
				this.AddClosure(item);
			}
		}
		public void AddGoto(Symbol s, State next)
		{
			this.Goto[s] = next;
			if (s is Terminal)
			{
				this.terminalTransitions.Add((Terminal)s);
				return;
			}
			this.nonTerminalTransitions.Add((NonTerminal)s, new Transition(this, (NonTerminal)s, next));
		}
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendFormat("State {0}", this.num);
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
			foreach (ProductionItem current in this.kernal_items)
			{
				stringBuilder.AppendFormat("    {0}", current);
				stringBuilder.AppendLine();
			}
			stringBuilder.AppendLine();
			foreach (KeyValuePair<Terminal, ParserAction> current2 in this.parseTable)
			{
				stringBuilder.AppendFormat("    {0,-14} {1}", current2.Key, current2.Value);
				stringBuilder.AppendLine();
			}
			stringBuilder.AppendLine();
			foreach (KeyValuePair<NonTerminal, Transition> current3 in this.nonTerminalTransitions)
			{
				stringBuilder.AppendFormat("    {0,-14} go to state {1}", current3.Key, this.Goto[current3.Key].num);
				stringBuilder.AppendLine();
			}
			stringBuilder.AppendLine();
			return stringBuilder.ToString();
		}
	}
}
