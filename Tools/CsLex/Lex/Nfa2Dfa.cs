using System;
using System.Collections.Generic;
namespace Lex
{
	internal class Nfa2Dfa
	{
		private const int NOT_IN_DSTATES = -1;
		public static void MakeDFA(Spec s)
		{
			Nfa2Dfa.make_dtrans(s);
			Nfa2Dfa.free_nfa_states(s);
			Nfa2Dfa.free_dfa_states(s);
		}
		private static void make_dtrans(Spec s)
		{
			Console.WriteLine("Working on DFA states.");
			s.InitUnmarkedDFA();
			int num = s.state_rules.Length;
			s.state_dtrans = new int[num];
			for (int i = 0; i < num; i++)
			{
				Bunch bunch = new Bunch(s.state_rules[i]);
				bunch.e_closure();
				Nfa2Dfa.add_to_dstates(s, bunch);
				s.state_dtrans[i] = s.dtrans_list.Count;
				Dfa nextUnmarkedDFA;
				while ((nextUnmarkedDFA = s.GetNextUnmarkedDFA()) != null)
				{
					nextUnmarkedDFA.SetMarked();
					DTrans dTrans = new DTrans(s, nextUnmarkedDFA);
					for (int j = 0; j < s.dtrans_ncols; j++)
					{
						bunch.move(nextUnmarkedDFA, j);
						if (!bunch.IsEmpty())
						{
							bunch.e_closure();
						}
						int num2;
						if (bunch.IsEmpty())
						{
							num2 = -1;
						}
						else
						{
							num2 = Nfa2Dfa.in_dstates(s, bunch);
							if (num2 == -1)
							{
								num2 = Nfa2Dfa.add_to_dstates(s, bunch);
							}
						}
						dTrans.SetDTrans(j, num2);
					}
					s.dtrans_list.Add(dTrans);
				}
			}
			Console.WriteLine("");
		}
		private static void free_dfa_states(Spec s)
		{
			s.dfa_states = null;
			s.dfa_sets = null;
		}
		private static void free_nfa_states(Spec s)
		{
			s.nfa_states = null;
			s.nfa_start = null;
			s.state_rules = null;
		}
		private static int add_to_dstates(Spec s, Bunch bunch)
		{
			Dfa dfa = Alloc.NewDfa(s);
			dfa.SetNFASet(new List<Nfa>(bunch.GetNFASet()));
			dfa.SetNFABit(new BitSet(bunch.GetNFABit()));
			dfa.SetAccept(bunch.GetAccept());
			dfa.SetAnchor(bunch.GetAnchor());
			dfa.ClearMarked();
			s.dfa_sets[dfa.GetNFABit()] = dfa;
			return dfa.Label;
		}
		private static int in_dstates(Spec s, Bunch bunch)
		{
			Dfa dfa;
			if (!s.dfa_sets.TryGetValue(bunch.GetNFABit(), out dfa))
			{
				return -1;
			}
			return dfa.Label;
		}
	}
}
