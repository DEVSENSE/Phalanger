using System;
using System.Collections.Generic;
namespace gpcc
{
	public class Grammar
	{
		public List<Production> productions = new List<Production>();
		public string unionType;
		public int NumActions;
		public string headerCode;
		public string prologCode;
		public string epilogCode;
		public NonTerminal startSymbol;
		public Production rootProduction;
		public Dictionary<string, NonTerminal> nonTerminals = new Dictionary<string, NonTerminal>();
		public Dictionary<string, Terminal> terminals = new Dictionary<string, Terminal>();
		public string Namespace;
		public string Visibility = "public";
		public string Attributes = "";
		public string ParserName = "Parser";
		public string TokenName = "Tokens";
		public string ValueTypeName = "ValueType";
		public string PositionType;
		public Grammar()
		{
			this.LookupTerminal(GrammarToken.Symbol, "ERROR");
			this.LookupTerminal(GrammarToken.Symbol, "EOF");
		}
		public Terminal LookupTerminal(GrammarToken token, string name)
		{
			if (!this.terminals.ContainsKey(name))
			{
				this.terminals[name] = new Terminal(token == GrammarToken.Symbol, name);
			}
			return this.terminals[name];
		}
		public NonTerminal LookupNonTerminal(string name)
		{
			if (!this.nonTerminals.ContainsKey(name))
			{
				this.nonTerminals[name] = new NonTerminal(name);
			}
			return this.nonTerminals[name];
		}
		public void AddProduction(Production production)
		{
			this.productions.Add(production);
			production.num = this.productions.Count;
		}
		public void CreateSpecialProduction(NonTerminal root)
		{
			this.rootProduction = new Production(this.LookupNonTerminal("$accept"));
			this.AddProduction(this.rootProduction);
			this.rootProduction.rhs.Add(root);
			this.rootProduction.rhs.Add(this.LookupTerminal(GrammarToken.Symbol, "EOF"));
		}
	}
}
