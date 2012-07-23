using System;
namespace Lex
{
	public class NfaPair
	{
		public Nfa start;
		public Nfa end;
		public NfaPair()
		{
			this.start = null;
			this.end = null;
		}
	}
}
