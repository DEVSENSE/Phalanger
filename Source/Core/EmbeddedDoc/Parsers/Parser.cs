using System;
using System.Collections.Generic;
using System.Text;

using PHP.Core.Parsers.GPPG;

namespace PHP.Core.EmbeddedDoc
{
	public partial struct SemanticValueType
	{
		public override string ToString()
		{
			if (Object != null) return Object.ToString();
			return String;
		}
	}

	/// <summary>
	/// Position in a source file.
	/// </summary>
	public partial struct Position
	{
		public static Position Invalid = new Position(-1, -1, -1, -1, -1, -1);
		public static Position Initial = new Position(+1, 0, 0, +1, 0, 0);

		public override string ToString()
		{
			return string.Format("({0},{1})-({2},{3})", FirstLine, FirstColumn, LastLine, LastColumn);
		}

		public ShortPosition Short
		{
			get { return new ShortPosition(FirstLine, FirstColumn); }
		}

		public static implicit operator ShortPosition(Position pos)
		{
			return new ShortPosition(pos.FirstLine, pos.FirstColumn);
		}

		public static implicit operator ErrorPosition(Position pos)
		{
			return new ErrorPosition(pos.FirstLine, pos.FirstColumn, pos.LastLine, pos.LastColumn);
		}

		public bool IsValid
		{
			get
			{
				return FirstOffset != -1;
			}
		}
	}

	public partial class Parser : ShiftReduceParser<SemanticValueType, Position>
	{
		public Tuple<DocElementType, DocElement>[] Elements { get { return elements; } }
		private Tuple<DocElementType, DocElement>[] elements;

		protected sealed override int EofToken
		{
			get { return (int)Tokens.EOF; }
		}

		protected sealed override int ErrorToken
		{
			get { return (int)Tokens.ERROR; }
		}

		protected bool CompoundTokens
		{
			get { return ((Scanner)Scanner).CompoundTokens; }
			set { ((Scanner)Scanner).CompoundTokens = value; }
		}
	}
}
