using System;
namespace Lex
{
	public class Anchor
	{
		public Accept accept;
		public int anchor;
		private Anchor()
		{
			this.accept = null;
			this.anchor = 0;
		}
	}
}
