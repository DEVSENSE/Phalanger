using System;
using System.Collections.Generic;
namespace gpcc
{
	public class LALRGenerator : LR0Generator
	{
		private Stack<Transition> S;
		public LALRGenerator(Grammar grammar) : base(grammar)
		{
		}
		public void ComputeLookAhead()
		{
			this.ComputeDRs();
			this.ComputeReads();
			this.ComputeIncludes();
			this.ComputeFollows();
			this.ComputeLA();
		}
		private void ComputeDRs()
		{
			foreach (State current in this.states)
			{
				foreach (Transition current2 in current.nonTerminalTransitions.Values)
				{
					current2.DR = current2.next.terminalTransitions;
				}
			}
		}
		private void ComputeReads()
		{
			this.S = new Stack<Transition>();
			foreach (State current in this.states)
			{
				foreach (Transition current2 in current.nonTerminalTransitions.Values)
				{
					current2.N = 0;
				}
			}
			foreach (State current3 in this.states)
			{
				foreach (Transition current4 in current3.nonTerminalTransitions.Values)
				{
					if (current4.N == 0)
					{
						this.TraverseReads(current4, 1);
					}
				}
			}
		}
		private void TraverseReads(Transition x, int k)
		{
			this.S.Push(x);
			x.N = k;
			x.Read = new Set<Terminal>(x.DR);
			foreach (Transition current in x.next.nonTerminalTransitions.Values)
			{
				if (current.A.IsNullable())
				{
					if (current.N == 0)
					{
						this.TraverseReads(current, k + 1);
					}
					if (current.N < x.N)
					{
						x.N = current.N;
					}
					x.Read.AddRange(current.Read);
				}
			}
			if (x.N == k)
			{
				do
				{
					this.S.Peek().N = 2147483647;
					this.S.Peek().Read = new Set<Terminal>(x.Read);
				}
				while (this.S.Pop() != x);
			}
		}
		private void ComputeIncludes()
		{
			foreach (State current in this.states)
			{
				foreach (Transition current2 in current.nonTerminalTransitions.Values)
				{
					foreach (Production current3 in current2.A.productions)
					{
						for (int i = current3.rhs.Count - 1; i >= 0; i--)
						{
							Symbol symbol = current3.rhs[i];
							if (symbol is NonTerminal)
							{
								State state = this.PathTo(current, current3, i);
								state.nonTerminalTransitions[(NonTerminal)symbol].includes.Add(current2);
							}
							if (!symbol.IsNullable())
							{
								break;
							}
						}
					}
				}
			}
		}
		private State PathTo(State q, Production prod, int prefix)
		{
			for (int i = 0; i < prefix; i++)
			{
				Symbol key = prod.rhs[i];
				if (!q.Goto.ContainsKey(key))
				{
					return null;
				}
				q = q.Goto[key];
			}
			return q;
		}
		private void ComputeFollows()
		{
			this.S = new Stack<Transition>();
			foreach (State current in this.states)
			{
				foreach (Transition current2 in current.nonTerminalTransitions.Values)
				{
					current2.N = 0;
				}
			}
			foreach (State current3 in this.states)
			{
				foreach (Transition current4 in current3.nonTerminalTransitions.Values)
				{
					if (current4.N == 0)
					{
						this.TraverseFollows(current4, 1);
					}
				}
			}
		}
		private void TraverseFollows(Transition x, int k)
		{
			this.S.Push(x);
			x.N = k;
			x.Follow = new Set<Terminal>(x.Read);
			foreach (Transition current in x.includes)
			{
				if (x != current)
				{
					if (current.N == 0)
					{
						this.TraverseFollows(current, k + 1);
					}
					if (current.N < x.N)
					{
						x.N = current.N;
					}
					x.Follow.AddRange(current.Follow);
				}
			}
			if (x.N == k)
			{
				do
				{
					this.S.Peek().N = 2147483647;
					this.S.Peek().Follow = new Set<Terminal>(x.Follow);
				}
				while (this.S.Pop() != x);
			}
		}
		private void ComputeLA()
		{
			foreach (State current in this.states)
			{
				foreach (ProductionItem current2 in current.all_items)
				{
					if (current2.isReduction())
					{
						current2.LA = new Set<Terminal>();
						foreach (State current3 in this.states)
						{
							if (this.PathTo(current3, current2.production, current2.pos) == current)
							{
								NonTerminal lhs = current2.production.lhs;
								if (current3.nonTerminalTransitions.ContainsKey(lhs))
								{
									Transition transition = current3.nonTerminalTransitions[lhs];
									current2.LA.AddRange(transition.Follow);
								}
							}
						}
					}
				}
			}
		}
	}
}
