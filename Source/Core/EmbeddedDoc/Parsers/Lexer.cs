using System;
using System.Collections.Generic;
using System.Text;

namespace PHP.Core.EmbeddedDoc
{
	public partial class DocLexer
	{
		protected string GetTokenString()
		{
			return new String(buffer, token_start, token_end - token_start);
		}

		private char Map(char c)
		{
			return (c > SByte.MaxValue) ? 'a' : c;
		}
	}
}
