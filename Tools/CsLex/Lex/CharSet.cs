using System;
using System.Collections.Generic;
using System.Globalization;
namespace Lex
{
	public sealed class CharSet
	{
		private static BitSet lowerLetters;
		private static BitSet upperLetters;
		private static BitSet letters;
		private BitSet set;
		private bool compflag;
		static CharSet()
		{
			CharSet.lowerLetters = new BitSet();
			CharSet.upperLetters = new BitSet();
			CharSet.letters = new BitSet();
			for (int i = 0; i <= 65535; i++)
			{
				switch (char.GetUnicodeCategory((char)i))
				{
				case UnicodeCategory.UppercaseLetter:
					CharSet.upperLetters.Set(i, true);
					CharSet.letters.Set(i, true);
					break;
				case UnicodeCategory.LowercaseLetter:
					CharSet.lowerLetters.Set(i, true);
					CharSet.letters.Set(i, true);
					break;
				case UnicodeCategory.TitlecaseLetter:
				case UnicodeCategory.ModifierLetter:
				case UnicodeCategory.OtherLetter:
					CharSet.letters.Set(i, true);
					break;
				}
			}
		}
		public bool AddClass(string name)
		{
			switch (name)
			{
			case "lower":
				this.set.Or(CharSet.lowerLetters);
				return true;
			case "upper":
				this.set.Or(CharSet.upperLetters);
				return true;
			case "alpha":
				this.set.Or(CharSet.letters);
				return true;
			}
			return false;
		}
		public CharSet()
		{
			this.set = new BitSet();
			this.compflag = false;
		}
		public void complement()
		{
			this.compflag = true;
		}
		public void add(int i)
		{
			this.set.Set(i, true);
		}
		public void addncase(char c)
		{
			this.add((int)c);
			this.add((int)char.ToLower(c));
			this.add((int)char.ToUpper(c));
		}
		public bool contains(int i)
		{
			bool flag = this.set.Get(i);
			if (this.compflag)
			{
				return !flag;
			}
			return flag;
		}
		public void mimic(CharSet s)
		{
			this.compflag = s.compflag;
			this.set = new BitSet(s.set);
		}
		public IEnumerator<int> GetEnumerator()
		{
			return this.set.GetEnumerator();
		}
		public void map(CharSet old, char[] mapping)
		{
			this.compflag = old.compflag;
			this.set = new BitSet();
			foreach (int current in old)
			{
				if (current < mapping.Length)
				{
					this.set.Set((int)mapping[current], true);
				}
			}
		}
	}
}
