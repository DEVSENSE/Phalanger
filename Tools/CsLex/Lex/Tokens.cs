using System;
namespace Lex
{
	public enum Tokens
	{
		EOS = -1,
		END_OF_INPUT = -2,
		LETTER = -3,
		CHAR_CLASS = -4,
		AT_EOL = 36,
		OPEN_PAREN = 40,
		CLOSE_PAREN,
		CLOSURE,
		PLUS_CLOSE,
		DASH = 45,
		ANY,
		OPTIONAL = 63,
		CCL_START = 91,
		CCL_END = 93,
		AT_BOL,
		OPEN_CURLY = 123,
		OR,
		CLOSE_CURLY
	}
}
