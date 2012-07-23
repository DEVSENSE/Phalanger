using System;
using System.Collections.Generic;
namespace Lex
{
	public class Spec
	{
		public const int NUM_PSEUDO = 2;
		public const int NONE = 0;
		public const int START = 1;
		public const int END = 2;
		private readonly Dictionary<string, int> states = new Dictionary<string, int>();
		public string InitialState = "YYINITIAL";
		public Dictionary<string, string> macros = new Dictionary<string, string>();
		public Nfa nfa_start;
		public List<Nfa> nfa_states = new List<Nfa>();
		public List<Nfa>[] state_rules;
		public int[] state_dtrans;
		public List<Dfa> dfa_states = new List<Dfa>();
		public Dictionary<BitSet, Dfa> dfa_sets = new Dictionary<BitSet, Dfa>();
		public List<Accept> accept_list;
		public int[] anchor_array;
		public List<DTrans> dtrans_list = new List<DTrans>();
		public int dtrans_ncols = 128;
		public int[] row_map;
		public int[] col_map;
		public char BOL;
		public char EOF;
		public char[] ccls_map;
		public Tokens current_token = Tokens.EOS;
		public char current_token_value;
		public string class_name;
		public bool in_quote;
		public bool in_ccl;
		public bool verbose;
		public bool integer_type;
		public bool yyeof;
		public bool CountChars;
		public bool CountLines;
		public bool CountColumns;
		public bool cup_compatible;
		public string ClassAttributes = "public";
		public bool IgnoreCase;
		public int Version = 2;
		public List<string> InitCode = new List<string>();
		public List<string> CtorCode = new List<string>();
		public List<string> ClassCode = new List<string>();
		public List<string> EofCode = new List<string>();
		public string EofTokenName;
		public string ErrorTokenName;
		public string LexerName = "Yylex";
		public string ImplementsName;
		public string FunctionName = "yylex";
		public string TokenTypeName = "Yytoken";
		public string Namespace = "YyNameSpace";
		public string SemanticValueType;
		public string CharMapMethod;
		public int VariantCount = 1;
		private int unmarked_dfa;
		public Dictionary<string, int> States
		{
			get
			{
				return this.states;
			}
		}
		public void InitUnmarkedDFA()
		{
			this.unmarked_dfa = 0;
		}
		public void AddInitialState()
		{
			if (this.states.Count == 0)
			{
				this.states.Add(this.InitialState, 0);
			}
		}
		public void AddState(string name)
		{
			if (this.states.Count == 0)
			{
				this.InitialState = name;
			}
			this.states.Add(name, this.states.Count);
		}
		public Dfa GetNextUnmarkedDFA()
		{
			while (this.unmarked_dfa < this.dfa_states.Count)
			{
				Dfa dfa = this.dfa_states[this.unmarked_dfa];
				if (!dfa.IsMarked())
				{
					return dfa;
				}
				this.unmarked_dfa++;
			}
			return null;
		}
	}
}
