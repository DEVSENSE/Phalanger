using System;
using System.Collections.Generic;
using System.IO;
namespace gpcc
{
	public class LR0Generator
	{
		protected List<State> states = new List<State>();
		protected Grammar grammar;
		private Dictionary<Symbol, List<State>> accessedBy = new Dictionary<Symbol, List<State>>();
		public LR0Generator(Grammar grammar)
		{
			this.grammar = grammar;
		}
		public List<State> BuildStates()
		{
			this.ExpandState(this.grammar.rootProduction.lhs, new State(this.grammar.rootProduction));
			return this.states;
		}
		private void ExpandState(Symbol sym, State newState)
		{
			newState.accessedBy = sym;
			this.states.Add(newState);
			if (!this.accessedBy.ContainsKey(sym))
			{
				this.accessedBy[sym] = new List<State>();
			}
			this.accessedBy[sym].Add(newState);
			newState.AddClosure();
			this.ComputeGoto(newState);
		}
		private void ComputeGoto(State state)
		{
			foreach (ProductionItem current in state.all_items)
			{
				if (!current.expanded && !current.isReduction())
				{
					current.expanded = true;
					Symbol symbol = current.production.rhs[current.pos];
					List<ProductionItem> list = new List<ProductionItem>();
					list.Add(new ProductionItem(current.production, current.pos + 1));
					foreach (ProductionItem current2 in state.all_items)
					{
						if (!current2.expanded && !current2.isReduction())
						{
							Symbol symbol2 = current2.production.rhs[current2.pos];
							if (symbol == symbol2)
							{
								current2.expanded = true;
								list.Add(new ProductionItem(current2.production, current2.pos + 1));
							}
						}
					}
					State state2 = this.FindExistingState(symbol, list);
					if (state2 == null)
					{
						State state3 = new State(list);
						state.AddGoto(symbol, state3);
						this.ExpandState(symbol, state3);
					}
					else
					{
						state.AddGoto(symbol, state2);
					}
				}
			}
		}
		private State FindExistingState(Symbol sym, List<ProductionItem> itemSet)
		{
			if (this.accessedBy.ContainsKey(sym))
			{
				foreach (State current in this.accessedBy[sym])
				{
					if (ProductionItem.SameProductions(current.kernal_items, itemSet))
					{
						return current;
					}
				}
			}
			return null;
		}
		public void BuildParseTable()
		{
			foreach (State current in this.states)
			{
				foreach (Terminal current2 in current.terminalTransitions)
				{
					current.parseTable[current2] = new Shift(current.Goto[current2]);
				}
				foreach (ProductionItem current3 in current.all_items)
				{
					if (current3.isReduction())
					{
						if (current3.production == this.grammar.rootProduction)
						{
							foreach (Terminal current4 in this.grammar.terminals.Values)
							{
								current.parseTable[current4] = new Reduce(current3);
							}
						}
						foreach (Terminal current5 in current3.LA)
						{
							if (current.parseTable.ContainsKey(current5))
							{
								ParserAction parserAction = current.parseTable[current5];
								if (parserAction is Reduce)
								{
									Console.Error.WriteLine("Reduce/Reduce conflict, state {0}: {1} vs {2} on {3}", new object[]
									{
										current.num,
										current3.production.num,
										((Reduce)parserAction).item.production.num,
										current5
									});
									if (((Reduce)parserAction).item.production.num > current3.production.num)
									{
										current.parseTable[current5] = new Reduce(current3);
									}
								}
								else
								{
									if (current3.production.prec != null && current5.prec != null)
									{
										if (current3.production.prec.prec > current5.prec.prec || (current3.production.prec.prec == current5.prec.prec && current3.production.prec.type == PrecType.left))
										{
											current.parseTable[current5] = new Reduce(current3);
										}
									}
									else
									{
										Console.Error.WriteLine("Shift/Reduce conflict, state {0} on {1}", current.num, current5);
									}
								}
							}
							else
							{
								current.parseTable[current5] = new Reduce(current3);
							}
						}
					}
				}
			}
		}
		public void Report(string log)
		{
			using (TextWriter textWriter = (log != null) ? File.CreateText(log) : Console.Out)
			{
				textWriter.WriteLine("Grammar");
				NonTerminal nonTerminal = null;
				foreach (Production current in this.grammar.productions)
				{
					if (current.lhs != nonTerminal)
					{
						nonTerminal = current.lhs;
						textWriter.WriteLine();
						textWriter.Write("{0,5} {1}: ", current.num, nonTerminal);
					}
					else
					{
						textWriter.Write("{0,5} {1}| ", current.num, new string(' ', nonTerminal.ToString().Length));
					}
					for (int i = 0; i < current.rhs.Count - 1; i++)
					{
						textWriter.Write("{0} ", current.rhs[i].ToString());
					}
					if (current.rhs.Count > 0)
					{
						textWriter.WriteLine("{0}", current.rhs[current.rhs.Count - 1]);
					}
					else
					{
						textWriter.WriteLine("/* empty */");
					}
				}
				textWriter.WriteLine();
				foreach (State current2 in this.states)
				{
					textWriter.WriteLine(current2.ToString());
				}
			}
		}
	}
}
