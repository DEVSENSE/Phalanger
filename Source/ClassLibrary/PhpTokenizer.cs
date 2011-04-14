/*

 Copyright (c) 2005-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using PHP.Core;
using PHP.Core.Parsers;

namespace PHP.Library
{
	using CoreTokens = PHP.Core.Parsers.Tokens;

	/// <summary>
	/// Provides functions and constant related to PHP tokenizer.
	/// </summary>
	[ImplementsExtension(LibraryDescriptor.ExtTokenizer)]
	public static class PhpTokenizer
	{
		#region Tokens

		/// <exclude/>
		public enum Tokens
		{
			[ImplementsConstant("T_REQUIRE_ONCE")]
			T_REQUIRE_ONCE = CoreTokens.T_REQUIRE_ONCE,
			[ImplementsConstant("T_REQUIRE")]
			T_REQUIRE = CoreTokens.T_REQUIRE,
			[ImplementsConstant("T_EVAL")]
			T_EVAL = CoreTokens.T_EVAL,
			[ImplementsConstant("T_INCLUDE_ONCE")]
			T_INCLUDE_ONCE = CoreTokens.T_INCLUDE_ONCE,
			[ImplementsConstant("T_INCLUDE")]
			T_INCLUDE = CoreTokens.T_INCLUDE,
			[ImplementsConstant("T_LOGICAL_OR")]
			T_LOGICAL_OR = CoreTokens.T_LOGICAL_OR,
			[ImplementsConstant("T_LOGICAL_XOR")]
			T_LOGICAL_XOR = CoreTokens.T_LOGICAL_XOR,
			[ImplementsConstant("T_LOGICAL_AND")]
			T_LOGICAL_AND = CoreTokens.T_LOGICAL_AND,
			[ImplementsConstant("T_PRINT")]
			T_PRINT = CoreTokens.T_PRINT,
			[ImplementsConstant("T_SR_EQUAL")]
			T_SR_EQUAL = CoreTokens.T_SR_EQUAL,
			[ImplementsConstant("T_SL_EQUAL")]
			T_SL_EQUAL = CoreTokens.T_SL_EQUAL,
			[ImplementsConstant("T_XOR_EQUAL")]
			T_XOR_EQUAL = CoreTokens.T_XOR_EQUAL,
			[ImplementsConstant("T_OR_EQUAL")]
			T_OR_EQUAL = CoreTokens.T_OR_EQUAL,
			[ImplementsConstant("T_AND_EQUAL")]
			T_AND_EQUAL = CoreTokens.T_AND_EQUAL,
			[ImplementsConstant("T_MOD_EQUAL")]
			T_MOD_EQUAL = CoreTokens.T_MOD_EQUAL,
			[ImplementsConstant("T_CONCAT_EQUAL")]
			T_CONCAT_EQUAL = CoreTokens.T_CONCAT_EQUAL,
			[ImplementsConstant("T_DIV_EQUAL")]
			T_DIV_EQUAL = CoreTokens.T_DIV_EQUAL,
			[ImplementsConstant("T_MUL_EQUAL")]
			T_MUL_EQUAL = CoreTokens.T_MUL_EQUAL,
			[ImplementsConstant("T_MINUS_EQUAL")]
			T_MINUS_EQUAL = CoreTokens.T_MINUS_EQUAL,
			[ImplementsConstant("T_PLUS_EQUAL")]
			T_PLUS_EQUAL = CoreTokens.T_PLUS_EQUAL,
			[ImplementsConstant("T_BOOLEAN_OR")]
			T_BOOLEAN_OR = CoreTokens.T_BOOLEAN_OR,
			[ImplementsConstant("T_BOOLEAN_AND")]
			T_BOOLEAN_AND = CoreTokens.T_BOOLEAN_AND,
			[ImplementsConstant("T_IS_NOT_IDENTICAL")]
			T_IS_NOT_IDENTICAL = CoreTokens.T_IS_NOT_IDENTICAL,
			[ImplementsConstant("T_IS_IDENTICAL")]
			T_IS_IDENTICAL = CoreTokens.T_IS_IDENTICAL,
			[ImplementsConstant("T_IS_NOT_EQUAL")]
			T_IS_NOT_EQUAL = CoreTokens.T_IS_NOT_EQUAL,
			[ImplementsConstant("T_IS_EQUAL")]
			T_IS_EQUAL = CoreTokens.T_IS_EQUAL,
			[ImplementsConstant("T_IS_GREATER_OR_EQUAL")]
			T_IS_GREATER_OR_EQUAL = CoreTokens.T_IS_GREATER_OR_EQUAL,
			[ImplementsConstant("T_IS_SMALLER_OR_EQUAL")]
			T_IS_SMALLER_OR_EQUAL = CoreTokens.T_IS_SMALLER_OR_EQUAL,
			[ImplementsConstant("T_SR")]
			T_SR = CoreTokens.T_SR,
			[ImplementsConstant("T_SL")]
			T_SL = CoreTokens.T_SL,
			[ImplementsConstant("T_INSTANCEOF")]
			T_INSTANCEOF = CoreTokens.T_INSTANCEOF,
			[ImplementsConstant("T_UNSET_CAST")]
			T_UNSET_CAST = CoreTokens.T_UNSET_CAST,
			[ImplementsConstant("T_BOOL_CAST")]
			T_BOOL_CAST = CoreTokens.T_BOOL_CAST,
			[ImplementsConstant("T_OBJECT_CAST")]
			T_OBJECT_CAST = CoreTokens.T_OBJECT_CAST,
			[ImplementsConstant("T_ARRAY_CAST")]
			T_ARRAY_CAST = CoreTokens.T_ARRAY_CAST,
			[ImplementsConstant("T_STRING_CAST")]
			T_STRING_CAST = CoreTokens.T_STRING_CAST,
			[ImplementsConstant("T_DOUBLE_CAST")]
			T_DOUBLE_CAST = CoreTokens.T_DOUBLE_CAST,
			[ImplementsConstant("T_INT_CAST")]
			T_INT_CAST = CoreTokens.T_INT_CAST,
			[ImplementsConstant("T_DEC")]
			T_DEC = CoreTokens.T_DEC,
			[ImplementsConstant("T_INC")]
			T_INC = CoreTokens.T_INC,
			[ImplementsConstant("T_CLONE")]
			T_CLONE = CoreTokens.T_CLONE,
			[ImplementsConstant("T_NEW")]
			T_NEW = CoreTokens.T_NEW,
			[ImplementsConstant("T_EXIT")]
			T_EXIT = CoreTokens.T_EXIT,
			[ImplementsConstant("T_IF")]
			T_IF = CoreTokens.T_IF,
			[ImplementsConstant("T_ELSEIF")]
			T_ELSEIF = CoreTokens.T_ELSEIF,
			[ImplementsConstant("T_ELSE")]
			T_ELSE = CoreTokens.T_ELSE,
			[ImplementsConstant("T_ENDIF")]
			T_ENDIF = CoreTokens.T_ENDIF,
			[ImplementsConstant("T_LNUMBER")]
			T_LNUMBER = CoreTokens.T_LNUMBER,
			[ImplementsConstant("T_DNUMBER")]
			T_DNUMBER = CoreTokens.T_DNUMBER,
			[ImplementsConstant("T_STRING")]
			T_STRING = CoreTokens.T_STRING,
			[ImplementsConstant("T_STRING_VARNAME")]
			T_STRING_VARNAME = CoreTokens.T_STRING_VARNAME,
			[ImplementsConstant("T_VARIABLE")]
			T_VARIABLE = CoreTokens.T_VARIABLE,
			[ImplementsConstant("T_NUM_STRING")]
			T_NUM_STRING = CoreTokens.T_NUM_STRING,
			[ImplementsConstant("T_INLINE_HTML")]
			T_INLINE_HTML = CoreTokens.T_INLINE_HTML,
			[ImplementsConstant("T_CHARACTER")]
			T_CHARACTER = CoreTokens.T_CHARACTER,
			[ImplementsConstant("T_BAD_CHARACTER")]
			T_BAD_CHARACTER = CoreTokens.T_BAD_CHARACTER,
			[ImplementsConstant("T_ENCAPSED_AND_WHITESPACE")]
			T_ENCAPSED_AND_WHITESPACE = CoreTokens.T_ENCAPSED_AND_WHITESPACE,
			[ImplementsConstant("T_CONSTANT_ENCAPSED_STRING")]
			T_CONSTANT_ENCAPSED_STRING = CoreTokens.T_CONSTANT_ENCAPSED_STRING,
			[ImplementsConstant("T_ECHO")]
			T_ECHO = CoreTokens.T_ECHO,
			[ImplementsConstant("T_DO")]
			T_DO = CoreTokens.T_DO,
			[ImplementsConstant("T_WHILE")]
			T_WHILE = CoreTokens.T_WHILE,
			[ImplementsConstant("T_ENDWHILE")]
			T_ENDWHILE = CoreTokens.T_ENDWHILE,
			[ImplementsConstant("T_FOR")]
			T_FOR = CoreTokens.T_FOR,
			[ImplementsConstant("T_ENDFOR")]
			T_ENDFOR = CoreTokens.T_ENDFOR,
			[ImplementsConstant("T_FOREACH")]
			T_FOREACH = CoreTokens.T_FOREACH,
			[ImplementsConstant("T_ENDFOREACH")]
			T_ENDFOREACH = CoreTokens.T_ENDFOREACH,
			// [ImplementsConstant("T_DECLARE")] T_DECLARE = CoreTokens.T_DECLARE,
			// [ImplementsConstant("T_ENDDECLARE")] T_ENDDECLARE = CoreTokens.T_ENDDECLARE,
			[ImplementsConstant("T_AS")]
			T_AS = CoreTokens.T_AS,
			[ImplementsConstant("T_SWITCH")]
			T_SWITCH = CoreTokens.T_SWITCH,
			[ImplementsConstant("T_ENDSWITCH")]
			T_ENDSWITCH = CoreTokens.T_ENDSWITCH,
			[ImplementsConstant("T_CASE")]
			T_CASE = CoreTokens.T_CASE,
			[ImplementsConstant("T_DEFAULT")]
			T_DEFAULT = CoreTokens.T_DEFAULT,
			[ImplementsConstant("T_BREAK")]
			T_BREAK = CoreTokens.T_BREAK,
			[ImplementsConstant("T_CONTINUE")]
			T_CONTINUE = CoreTokens.T_CONTINUE,
			[ImplementsConstant("T_FUNCTION")]
			T_FUNCTION = CoreTokens.T_FUNCTION,
			[ImplementsConstant("T_CONST")]
			T_CONST = CoreTokens.T_CONST,
			[ImplementsConstant("T_RETURN")]
			T_RETURN = CoreTokens.T_RETURN,
			[ImplementsConstant("T_TRY")]
			T_TRY = CoreTokens.T_TRY,
			[ImplementsConstant("T_CATCH")]
			T_CATCH = CoreTokens.T_CATCH,
			[ImplementsConstant("T_THROW")]
			T_THROW = CoreTokens.T_THROW,
			// [ImplementsConstant("T_USE")] T_USE = CoreTokens.T_USE,
			[ImplementsConstant("T_GLOBAL")]
			T_GLOBAL = CoreTokens.T_GLOBAL,
			[ImplementsConstant("T_PUBLIC")]
			T_PUBLIC = CoreTokens.T_PUBLIC,
			[ImplementsConstant("T_PROTECTED")]
			T_PROTECTED = CoreTokens.T_PROTECTED,
			[ImplementsConstant("T_PRIVATE")]
			T_PRIVATE = CoreTokens.T_PRIVATE,
			[ImplementsConstant("T_FINAL")]
			T_FINAL = CoreTokens.T_FINAL,
			[ImplementsConstant("T_ABSTRACT")]
			T_ABSTRACT = CoreTokens.T_ABSTRACT,
			[ImplementsConstant("T_STATIC")]
			T_STATIC = CoreTokens.T_STATIC,
			[ImplementsConstant("T_VAR")]
			T_VAR = CoreTokens.T_VAR,
			[ImplementsConstant("T_UNSET")]
			T_UNSET = CoreTokens.T_UNSET,
			[ImplementsConstant("T_ISSET")]
			T_ISSET = CoreTokens.T_ISSET,
			[ImplementsConstant("T_EMPTY")]
			T_EMPTY = CoreTokens.T_EMPTY,
			[ImplementsConstant("T_HALT_COMPILER")]
			T_HALT_COMPILER = 351, // Unused
			[ImplementsConstant("T_CLASS")]
			T_CLASS = CoreTokens.T_CLASS,
			[ImplementsConstant("T_INTERFACE")]
			T_INTERFACE = CoreTokens.T_INTERFACE,
			[ImplementsConstant("T_EXTENDS")]
			T_EXTENDS = CoreTokens.T_EXTENDS,
			[ImplementsConstant("T_IMPLEMENTS")]
			T_IMPLEMENTS = CoreTokens.T_IMPLEMENTS,
			[ImplementsConstant("T_OBJECT_OPERATOR")]
			T_OBJECT_OPERATOR = CoreTokens.T_OBJECT_OPERATOR,
			[ImplementsConstant("T_DOUBLE_ARROW")]
			T_DOUBLE_ARROW = CoreTokens.T_DOUBLE_ARROW,
			[ImplementsConstant("T_LIST")]
			T_LIST = CoreTokens.T_LIST,
			[ImplementsConstant("T_ARRAY")]
			T_ARRAY = CoreTokens.T_ARRAY,
			[ImplementsConstant("T_CLASS_C")]
			T_CLASS_C = CoreTokens.T_CLASS_C,
			[ImplementsConstant("T_METHOD_C")]
			T_METHOD_C = CoreTokens.T_METHOD_C,
			[ImplementsConstant("T_FUNC_C")]
			T_FUNC_C = CoreTokens.T_FUNC_C,
			[ImplementsConstant("T_LINE")]
			T_LINE = CoreTokens.T_LINE,
			[ImplementsConstant("T_FILE")]
			T_FILE = CoreTokens.T_FILE,
			[ImplementsConstant("T_COMMENT")]
			T_COMMENT = CoreTokens.T_COMMENT,
			[ImplementsConstant("T_DOC_COMMENT")]
			T_DOC_COMMENT = CoreTokens.T_DOC_COMMENT,
			[ImplementsConstant("T_OPEN_TAG")]
			T_OPEN_TAG = CoreTokens.T_OPEN_TAG,
			[ImplementsConstant("T_OPEN_TAG_WITH_ECHO")]
			T_OPEN_TAG_WITH_ECHO = CoreTokens.T_OPEN_TAG_WITH_ECHO,
			[ImplementsConstant("T_CLOSE_TAG")]
			T_CLOSE_TAG = CoreTokens.T_CLOSE_TAG,
			[ImplementsConstant("T_WHITESPACE")]
			T_WHITESPACE = CoreTokens.T_WHITESPACE,
			[ImplementsConstant("T_START_HEREDOC")]
			T_START_HEREDOC = CoreTokens.T_START_HEREDOC,
			[ImplementsConstant("T_END_HEREDOC")]
			T_END_HEREDOC = CoreTokens.T_END_HEREDOC,
			[ImplementsConstant("T_DOLLAR_OPEN_CURLY_BRACES")]
			T_DOLLAR_OPEN_CURLY_BRACES = CoreTokens.T_DOLLAR_OPEN_CURLY_BRACES,
			[ImplementsConstant("T_CURLY_OPEN")]
			T_CURLY_OPEN = CoreTokens.T_CURLY_OPEN,
			[ImplementsConstant("T_DOUBLE_COLON")]
			T_DOUBLE_COLON = CoreTokens.T_DOUBLE_COLON,
			[ImplementsConstant("T_PAAMAYIM_NEKUDOTAYIM")]
			T_PAAMAYIM_NEKUDOTAYIM = CoreTokens.T_DOUBLE_COLON,  // Duplicate
            [ImplementsConstant("T_DIR")]
            T_DIR = CoreTokens.T_DIR,
            [ImplementsConstant("T_GOTO")]
            T_GOTO = CoreTokens.T_GOTO,
		}

		#endregion

		#region token_get_all, token_name

		/// <summary>
		/// Tokenize a source source and returns a list of tokens.
		/// </summary>
		/// <returns>
		/// Array of items that are either token string values of for unnamed tokens
		/// or arrays comprising of token id and token string value.
		/// </returns>
		[ImplementsFunction("token_get_all")]
		public static PhpArray GetAllTokens(string sourceCode)
		{
			PhpArray result = new PhpArray();
			Tokenizer tokenizer = new Tokenizer(new StringReader(sourceCode));

			for (; ; )
			{
				CoreTokens token = tokenizer.GetNextToken();
				if (token == CoreTokens.ERROR)
				{
					token = CoreTokens.T_STRING;
				}

				if (token == CoreTokens.EOF) break;

				if (Tokenizer.IsCharToken(token))
				{
					result.Add(tokenizer.TokenText);
				}
				else
				{
					PhpArray item = new PhpArray();
					item.Add((int)token);
					item.Add(tokenizer.TokenText);
					result.Add(item);
				}
			}

			return result;
		}

		/// <summary>
		/// Gets the name of the PHP grammar token.
		/// </summary>
		/// <param name="token">The token id.</param>
		/// <returns>The token name.</returns>
		[ImplementsFunction("token_name")]
		public static string GetTokenName(Tokens token)
		{
			return token.ToString();
		}

		#endregion
	}
}
