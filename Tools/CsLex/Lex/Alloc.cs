using System;
namespace Lex
{
	public class Alloc
	{
		public static Dfa NewDfa(Spec spec)
		{
			Dfa dfa = new Dfa(spec.dfa_states.Count);
			spec.dfa_states.Add(dfa);
			return dfa;
		}
		public static NfaPair NewNfaPair()
		{
			return new NfaPair();
		}
		public static Nfa NewNfa(Spec spec)
		{
			Nfa nfa = new Nfa();
			spec.nfa_states.Add(nfa);
			nfa.Edge = '￼';
			return nfa;
		}
		public static NfaPair NewNLPair(Spec spec)
		{
			NfaPair nfaPair = Alloc.NewNfaPair();
			nfaPair.end = Alloc.NewNfa(spec);
			nfaPair.start = Alloc.NewNfa(spec);
			Nfa start = nfaPair.start;
			start.Next = Alloc.NewNfa(spec);
			Nfa next = start.Next;
			next.Edge = '￾';
			next.SetCharSet(new CharSet());
			next.GetCharSet().add(10);
			next.Next = nfaPair.end;
			start.Sibling = Alloc.NewNfa(spec);
			Nfa sibling = start.Sibling;
			sibling.Edge = '\r';
			sibling.Next = Alloc.NewNfa(spec);
			Nfa next2 = sibling.Next;
			next2.Next = null;
			next2.Sibling = Alloc.NewNfa(spec);
			next2.Sibling.Edge = '\n';
			next2.Sibling.Next = nfaPair.end;
			return nfaPair;
		}
	}
}
