using System;
using System.Collections.Generic;
namespace Lex
{
	internal class SimplifyNfa
	{
		private static char[] ccls;
		private static int original_charset_size;
		private static int mapped_charset_size;
		internal static void simplify(Spec spec)
		{
			SimplifyNfa.computeClasses(spec);
			for (int i = 0; i < spec.nfa_states.Count; i++)
			{
				Nfa nfa = spec.nfa_states[i];
				if (nfa.Edge != '�' && nfa.Edge != '￼')
				{
					if (nfa.Edge == '￾')
					{
						CharSet charSet = new CharSet();
						charSet.map(nfa.GetCharSet(), SimplifyNfa.ccls);
						nfa.SetCharSet(charSet);
					}
					else
					{
						nfa.Edge = SimplifyNfa.ccls[(int)nfa.Edge];
					}
				}
			}
			spec.ccls_map = SimplifyNfa.ccls;
			spec.dtrans_ncols = SimplifyNfa.mapped_charset_size;
		}
		private static void computeClasses(Spec spec)
		{
			SimplifyNfa.original_charset_size = spec.dtrans_ncols;
			SimplifyNfa.ccls = new char[SimplifyNfa.original_charset_size];
			char c = '\u0001';
			BitSet bitSet = new BitSet();
			BitSet bitSet2 = new BitSet();
			Dictionary<char, char> dictionary = new Dictionary<char, char>();
			Console.WriteLine("Working on character classes.");
			for (int i = 0; i < spec.nfa_states.Count; i++)
			{
				Nfa nfa = spec.nfa_states[i];
				if (nfa.Edge != '�' && nfa.Edge != '￼')
				{
					bitSet.ClearAll();
					bitSet2.ClearAll();
					for (int j = 0; j < SimplifyNfa.ccls.Length; j++)
					{
						if ((int)nfa.Edge == j || (nfa.Edge == '￾' && nfa.GetCharSet().contains(j)))
						{
							bitSet.Set((int)SimplifyNfa.ccls[j], true);
						}
						else
						{
							bitSet2.Set((int)SimplifyNfa.ccls[j], true);
						}
					}
					bitSet.And(bitSet2);
					if (bitSet.GetLength() != 0)
					{
						dictionary.Clear();
						for (int k = 0; k < SimplifyNfa.ccls.Length; k++)
						{
							if (bitSet.Get((int)SimplifyNfa.ccls[k]) && ((int)nfa.Edge == k || (nfa.Edge == '￾' && nfa.GetCharSet().contains(k))))
							{
								char c2 = SimplifyNfa.ccls[k];
								if (!dictionary.ContainsKey(c2))
								{
									Dictionary<char, char> arg_14F_0 = dictionary;
									char arg_14F_1 = c2;
									char expr_14A = c;
									c = (char)(expr_14A + '\u0001');
									arg_14F_0.Add(arg_14F_1, expr_14A);
								}
								SimplifyNfa.ccls[k] = dictionary[c2];
							}
						}
					}
				}
			}
			SimplifyNfa.mapped_charset_size = (int)c;
		}
	}
}
