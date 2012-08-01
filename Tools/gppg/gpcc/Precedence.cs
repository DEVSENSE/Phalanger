using System;
namespace gpcc
{
	public class Precedence
	{
		public PrecType type;
		public int prec;
		public Precedence(PrecType type, int prec)
		{
			this.type = type;
			this.prec = prec;
		}
		public static void Calculate(Production p)
		{
			if (p.prec == null)
			{
				for (int i = p.rhs.Count - 1; i >= 0; i--)
				{
					if (p.rhs[i] is Terminal)
					{
						p.prec = ((Terminal)p.rhs[i]).prec;
						return;
					}
				}
			}
		}
	}
}
