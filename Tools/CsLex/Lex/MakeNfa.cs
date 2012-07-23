using System;
using System.Collections.Generic;
namespace Lex
{
	public class MakeNfa
	{
		private static Spec spec;
		private static Gen gen;
		private static Input input;
		public static void Allocate_BOL_EOF(Spec s)
		{
			s.BOL = (char)s.dtrans_ncols++;
			s.EOF = (char)s.dtrans_ncols++;
		}
		public static void CreateMachine(Gen cmg, Spec cms, Input cmi)
		{
			MakeNfa.spec = cms;
			MakeNfa.gen = cmg;
			MakeNfa.input = cmi;
			MakeNfa.spec.AddInitialState();
			int count = MakeNfa.spec.States.Count;
			MakeNfa.spec.state_rules = new List<Nfa>[count];
			for (int i = 0; i < count; i++)
			{
				MakeNfa.spec.state_rules[i] = new List<Nfa>();
			}
			MakeNfa.spec.nfa_start = MakeNfa.machine();
			count = MakeNfa.spec.nfa_states.Count;
			for (int i = 0; i < count; i++)
			{
				Nfa nfa = MakeNfa.spec.nfa_states[i];
				nfa.Label = i;
			}
			if (MakeNfa.spec.verbose)
			{
				Console.WriteLine("NFA comprised of " + (MakeNfa.spec.nfa_states.Count + 1) + " states.");
			}
		}
		private static void discardNfa(Nfa nfa)
		{
			MakeNfa.spec.nfa_states.Remove(nfa);
		}
		private static void ProcessStates(BitSet bset, Nfa current)
		{
			foreach (int current2 in bset)
			{
				List<Nfa> list = MakeNfa.spec.state_rules[current2];
				list.Add(current);
			}
		}
		private static Nfa machine()
		{
			Nfa nfa = Alloc.NewNfa(MakeNfa.spec);
			Nfa nfa2 = nfa;
			BitSet states = MakeNfa.gen.GetStates();
			MakeNfa.spec.current_token = Tokens.EOS;
			MakeNfa.gen.Advance();
			if (Tokens.END_OF_INPUT != MakeNfa.spec.current_token)
			{
				nfa2.Next = MakeNfa.rule();
				MakeNfa.ProcessStates(states, nfa2.Next);
			}
			while (Tokens.END_OF_INPUT != MakeNfa.spec.current_token)
			{
				states = MakeNfa.gen.GetStates();
				MakeNfa.gen.Advance();
				if (Tokens.END_OF_INPUT == MakeNfa.spec.current_token)
				{
					break;
				}
				nfa2.Sibling = Alloc.NewNfa(MakeNfa.spec);
				nfa2 = nfa2.Sibling;
				nfa2.Next = MakeNfa.rule();
				MakeNfa.ProcessStates(states, nfa2.Next);
			}
			nfa2.Sibling = Alloc.NewNfa(MakeNfa.spec);
			nfa2 = nfa2.Sibling;
			nfa2.Next = Alloc.NewNfa(MakeNfa.spec);
			Nfa next = nfa2.Next;
			next.Edge = '￾';
			next.Next = Alloc.NewNfa(MakeNfa.spec);
			next.SetCharSet(new CharSet());
			next.GetCharSet().add((int)MakeNfa.spec.BOL);
			next.GetCharSet().add((int)MakeNfa.spec.EOF);
			next.Next.SetAccept(new Accept(null));
			for (int i = 0; i < MakeNfa.spec.States.Count; i++)
			{
				List<Nfa> list = MakeNfa.spec.state_rules[i];
				list.Add(next);
			}
			return nfa;
		}
		private static Nfa rule()
		{
			int num = 0;
			NfaPair nfaPair = Alloc.NewNfaPair();
			Nfa nfa;
			Nfa end;
			if (MakeNfa.spec.current_token == Tokens.AT_BOL)
			{
				num |= 1;
				MakeNfa.gen.Advance();
				MakeNfa.expr(nfaPair);
				nfa = Alloc.NewNfa(MakeNfa.spec);
				nfa.Edge = MakeNfa.spec.BOL;
				nfa.Next = nfaPair.start;
				end = nfaPair.end;
			}
			else
			{
				MakeNfa.expr(nfaPair);
				nfa = nfaPair.start;
				end = nfaPair.end;
			}
			if (Tokens.AT_EOL == MakeNfa.spec.current_token)
			{
				MakeNfa.gen.Advance();
				NfaPair nfaPair2 = Alloc.NewNLPair(MakeNfa.spec);
				end.Next = Alloc.NewNfa(MakeNfa.spec);
				Nfa next = end.Next;
				next.Next = nfaPair2.start;
				next.Sibling = Alloc.NewNfa(MakeNfa.spec);
				next.Sibling.Edge = MakeNfa.spec.EOF;
				next.Sibling.Next = nfaPair2.end;
				end = nfaPair2.end;
				num |= 2;
			}
			if (end == null)
			{
				Error.ParseError(Errors.ZERO, MakeNfa.gen.InputFilePath, MakeNfa.input.line_number);
			}
			end.SetAccept(MakeNfa.gen.packAccept());
			end.SetAnchor(num);
			return nfa;
		}
		private static void expr(NfaPair pair)
		{
			NfaPair nfaPair = Alloc.NewNfaPair();
			MakeNfa.cat_expr(pair);
			while (Tokens.OR == MakeNfa.spec.current_token)
			{
				MakeNfa.gen.Advance();
				MakeNfa.cat_expr(nfaPair);
				Nfa nfa = Alloc.NewNfa(MakeNfa.spec);
				nfa.Sibling = nfaPair.start;
				nfa.Next = pair.start;
				pair.start = nfa;
				nfa = Alloc.NewNfa(MakeNfa.spec);
				pair.end.Next = nfa;
				nfaPair.end.Next = nfa;
				pair.end = nfa;
			}
		}
		private static void cat_expr(NfaPair pair)
		{
			NfaPair nfaPair = Alloc.NewNfaPair();
			if (MakeNfa.first_in_cat(MakeNfa.spec.current_token))
			{
				MakeNfa.factor(pair);
			}
			while (MakeNfa.first_in_cat(MakeNfa.spec.current_token))
			{
				MakeNfa.factor(nfaPair);
				pair.end.mimic(nfaPair.start);
				MakeNfa.discardNfa(nfaPair.start);
				pair.end = nfaPair.end;
			}
		}
		private static bool first_in_cat(Tokens token)
		{
			if (token == Tokens.CLOSE_PAREN || token == Tokens.AT_EOL || token == Tokens.OR || token == Tokens.EOS)
			{
				return false;
			}
			if (token == Tokens.CLOSURE || token == Tokens.PLUS_CLOSE || token == Tokens.OPTIONAL)
			{
				Error.ParseError(Errors.CLOSE, MakeNfa.gen.InputFilePath, MakeNfa.input.line_number);
				return false;
			}
			if (token == Tokens.CCL_END)
			{
				Error.ParseError(Errors.BRACKET, MakeNfa.gen.InputFilePath, MakeNfa.input.line_number);
				return false;
			}
			if (token == Tokens.AT_BOL)
			{
				Error.ParseError(Errors.BOL, MakeNfa.gen.InputFilePath, MakeNfa.input.line_number);
				return false;
			}
			return true;
		}
		private static void factor(NfaPair pair)
		{
			MakeNfa.term(pair);
			if (Tokens.CLOSURE == MakeNfa.spec.current_token || Tokens.PLUS_CLOSE == MakeNfa.spec.current_token || Tokens.OPTIONAL == MakeNfa.spec.current_token)
			{
				Nfa nfa = Alloc.NewNfa(MakeNfa.spec);
				Nfa nfa2 = Alloc.NewNfa(MakeNfa.spec);
				nfa.Next = pair.start;
				pair.end.Next = nfa2;
				if (MakeNfa.spec.current_token == Tokens.CLOSURE || MakeNfa.spec.current_token == Tokens.OPTIONAL)
				{
					nfa.Sibling = nfa2;
				}
				if (MakeNfa.spec.current_token == Tokens.CLOSURE || MakeNfa.spec.current_token == Tokens.PLUS_CLOSE)
				{
					pair.end.Sibling = pair.start;
				}
				pair.start = nfa;
				pair.end = nfa2;
				MakeNfa.gen.Advance();
			}
		}
		private static void term(NfaPair pair)
		{
			if (Tokens.OPEN_PAREN == MakeNfa.spec.current_token)
			{
				MakeNfa.gen.Advance();
				MakeNfa.expr(pair);
				if (Tokens.CLOSE_PAREN == MakeNfa.spec.current_token)
				{
					MakeNfa.gen.Advance();
					return;
				}
				Error.ParseError(Errors.SYNTAX, MakeNfa.gen.InputFilePath, MakeNfa.input.line_number);
				return;
			}
			else
			{
				Nfa nfa = Alloc.NewNfa(MakeNfa.spec);
				pair.start = nfa;
				nfa.Next = Alloc.NewNfa(MakeNfa.spec);
				pair.end = nfa.Next;
				bool flag = MakeNfa.spec.current_token == Tokens.LETTER && char.IsLetter(MakeNfa.spec.current_token_value);
				if (MakeNfa.spec.current_token != Tokens.ANY && MakeNfa.spec.current_token != Tokens.CCL_START && (!MakeNfa.spec.IgnoreCase || !flag))
				{
					nfa.Edge = MakeNfa.spec.current_token_value;
					MakeNfa.gen.Advance();
					return;
				}
				nfa.Edge = '￾';
				nfa.SetCharSet(new CharSet());
				CharSet charSet = nfa.GetCharSet();
				if (MakeNfa.spec.IgnoreCase && flag)
				{
					charSet.addncase(MakeNfa.spec.current_token_value);
				}
				else
				{
					if (MakeNfa.spec.current_token == Tokens.ANY)
					{
						charSet.add(10);
						charSet.add(13);
						charSet.add((int)MakeNfa.spec.BOL);
						charSet.add((int)MakeNfa.spec.EOF);
						charSet.complement();
					}
					else
					{
						MakeNfa.gen.Advance();
						if (MakeNfa.spec.current_token == Tokens.CHAR_CLASS)
						{
							MakeNfa.gen.Advance();
							if (!charSet.AddClass(MakeNfa.spec.class_name.ToLower()))
							{
								Error.ParseError(Errors.InvalidCharClass, MakeNfa.gen.InputFilePath, MakeNfa.input.line_number);
							}
						}
						else
						{
							if (MakeNfa.spec.current_token == Tokens.AT_BOL)
							{
								MakeNfa.gen.Advance();
								charSet.add((int)MakeNfa.spec.BOL);
								charSet.add((int)MakeNfa.spec.EOF);
								charSet.complement();
							}
						}
						if (MakeNfa.spec.current_token != Tokens.CCL_END)
						{
							MakeNfa.dodash(charSet);
						}
					}
				}
				MakeNfa.gen.Advance();
				return;
			}
		}
		private static void dodash(CharSet set)
		{
			int i = -1;
			while (Tokens.EOS != MakeNfa.spec.current_token && Tokens.CCL_END != MakeNfa.spec.current_token)
			{
				if (Tokens.DASH == MakeNfa.spec.current_token && -1 != i)
				{
					MakeNfa.gen.Advance();
					if (MakeNfa.spec.current_token == Tokens.CCL_END)
					{
						set.add(45);
						return;
					}
					while (i <= (int)MakeNfa.spec.current_token_value)
					{
						if (MakeNfa.spec.IgnoreCase)
						{
							set.addncase((char)i);
						}
						else
						{
							set.add(i);
						}
						i++;
					}
				}
				else
				{
					i = (int)MakeNfa.spec.current_token_value;
					if (MakeNfa.spec.IgnoreCase)
					{
						set.addncase(MakeNfa.spec.current_token_value);
					}
					else
					{
						set.add((int)MakeNfa.spec.current_token_value);
					}
				}
				MakeNfa.gen.Advance();
			}
		}
	}
}
