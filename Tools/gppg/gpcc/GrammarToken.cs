using System;
namespace gpcc
{
	public enum GrammarToken
	{
		Eof,
		Symbol,
		Literal,
		Action,
		Divider,
		Colon,
		SemiColon,
		EndOfSection,
		Union,
		Type,
		Token,
		Left,
		Right,
		NonAssoc,
		Prelude,
		Prolog,
		Epilog,
		Kind,
		LeftCurly,
		RightCurly,
		Prec,
		Start,
		Namespace,
		Visibility,
		Attributes,
		ParserName,
		GenerateTokens,
		TokenName,
		ValueTypeName,
		PositionType
	}
}
