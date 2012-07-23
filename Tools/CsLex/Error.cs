using System;
public class Error
{
	public static void impos(string message)
	{
		Console.WriteLine("Lex Error: " + message);
	}
	public static string GetErrorMessage(Errors error)
	{
		switch (error)
		{
		case Errors.BADEXPR:
			return "Malformed regular expression.";
		case Errors.PAREN:
			return "Missing close parenthesis.";
		case Errors.LENGTH:
			return "Too many regular expressions or expression too long.";
		case Errors.BRACKET:
			return "Missing [ in character class.";
		case Errors.BOL:
			return "^ must be at start of expression or after [.";
		case Errors.CLOSE:
			return "+ ? or * must follow an expression or subexpression.";
		case Errors.NEWLINE:
			return "Newline in quoted string.";
		case Errors.BADMAC:
			return "Missing } in macro expansion.";
		case Errors.NOMAC:
			return "Macro does not exist.";
		case Errors.MACDEPTH:
			return "Macro expansions nested too deeply.";
		case Errors.INIT:
			return "Lex has not been successfully initialized.";
		case Errors.EOF:
			return "Unexpected end-of-file found.";
		case Errors.DIRECT:
			return "Undefined or badly-formed Lex directive.";
		case Errors.INTERNAL:
			return "Internal Lex error.";
		case Errors.STATE:
			return "Unitialized state name.";
		case Errors.MACDEF:
			return "Badly formed macro definition.";
		case Errors.SYNTAX:
			return "Syntax error.";
		case Errors.BRACE:
			return "Missing brace at start of lexical action.";
		case Errors.DASH:
			return "Special character dash - in character class [...] must be preceded by start-of-range character.";
		case Errors.ZERO:
			return "Zero-length regular expression.";
		case Errors.InvalidCharClass:
			return "Invalid character class.";
		case Errors.DuplicatedMacro:
			return "Duplicated macro.";
		default:
			return null;
		}
	}
	public static void ParseError(Errors error, string file, int line)
	{
		Error.ParseError(error, file, line, 1);
	}
	public static void ParseError(Errors error, string file, int line, int column)
	{
		throw new ApplicationException(string.Format("{0}({1},{2}): error LEX{3:00}: Parse error. {4}", new object[]
		{
			file,
			line,
			column,
			(int)error,
			Error.GetErrorMessage(error)
		}));
	}
}
