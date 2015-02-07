/*

 Copyright (c) 2006 Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 414

namespace PHP.Core.CodeDom
{
	/// <summary>
	/// Keywords (forbidden identifiers).
	/// </summary>
	internal static class Keywords
	{
		public const string Exit           = "exit";
		public const string Die            = "die";
		public const string Function       = "function";
		public const string Const          = "const";
		public const string Return         = "return";
		public const string If             = "if";
		public const string ElseIf         = "elseif";
		public const string EndIf          = "endif";
		public const string Else           = "else";
		public const string While          = "while";
		public const string EndWhile       = "endwhile";
		public const string Do             = "do";
		public const string For            = "for";
		public const string EndFor         = "endfor";
		public const string ForEach        = "foreach";
		public const string EndForEach     = "endforeach";
		public const string Declare        = "declare";
		public const string EndDeclare     = "enddeclare";
		public const string As             = "as";
		public const string Switch         = "switch";
		public const string EndSwitc       = "endswitch";
		public const string Case           = "case";
		public const string Default        = "default";
		public const string Break          = "break";
		public const string Continue       = "continue";
		public const string Echo           = "echo";
		public const string Print          = "print";
		public const string Class          = "class";
        public const string Trait          = "trait";
		public const string Extends        = "extends";
		public const string New            = "new";
		public const string Var            = "var";
		public const string Eval           = "eval";
		public const string Include        = "include";
		public const string IncludeOnce    = "include_once";
		public const string Require        = "require";
		public const string RequireOnce    = "require_once";
		public const string Use            = "use";
        public const string Import = "import";
		public const string Global         = "global";
		public const string IsSet          = "isset";
		public const string Empty          = "empty";
		public const string Static         = "static";
		public const string Unset          = "unset";
		public const string Or             = "or";
		public const string And            = "and";
		public const string Xor            = "xor";
		public const string List           = "list";
		public const string Array          = "array";
		public const string Try            = "try";
		public const string Catch          = "catch";
		public const string Throw          = "throw";
		public const string Interface      = "interface";
		public const string Implements     = "implements";
		public const string Clone          = "clone";
		public const string Abstract       = "abstract";
		public const string Final          = "final";
		public const string Private        = "private";
		public const string Protected      = "protected";
		public const string Public         = "public";
		public const string InstanceOf     = "instanceof";
		public const string Namespace      = "namespace";
		public const string Partial        = "partial";
        
		public const string NAMESPACE      = "__NAMESPACE__";
		public const string CLASS          = "__CLASS__";
		public const string FUNCTION       = "__FUNCTION__";
		public const string METHOD         = "__METHOD__";
		public const string LINE           = "__LINE__";
		public const string FILE           = "__FILE__";
        public const string DIR            = "__DIR__ ";

        public const string TypeOf      = "CLRTypeOf";//Added by Ðonny 03-09-2008

		private static volatile Dictionary<string, string> _keywordDictionary;
		internal static Dictionary<string, string> KeywordDictionary
		{
			get
			{
				if (_keywordDictionary == null)
				{
					Type self = typeof(Keywords);
					lock (self)
					{
						if (_keywordDictionary == null)
						{
							FieldInfo[] fields = self.GetFields(BindingFlags.Public | BindingFlags.Static);

							_keywordDictionary = new Dictionary<string, string>(fields.Length);

							for (int i = 0; i < fields.Length; i++)
							{
								if (fields[i].IsLiteral)
								{
									string kw = (string)fields[i].GetValue(null);
									_keywordDictionary.Add(kw.ToLowerInvariant(), kw);
								}
							}
						}
					}
				}
				return _keywordDictionary;
			}
		}

		/// <summary>
		/// Determines whether a given string is a keyword.
		/// </summary>
		/// <param name="str">A string.</param>
		/// <returns><B>True</B> if <paramref name="str"/> is a keyword, <B>false</B> otherwise.</returns>
		public static bool IsKeyword(string/*!*/ str)
		{
			return KeywordDictionary.ContainsKey(str.ToLowerInvariant());
		}
	}

	/// <summary>
	/// Special words (allowed identifiers).
	/// </summary>
	internal static class SpecialWords
	{
		public const string Goto           = "goto";

		public const string Int            = "int";
		public const string String         = "string";
		public const string Bool           = "bool";
		public const string Double         = "double";
		public const string Object         = "object";
		public const string Array          = "array";

		public const string From           = "from";
		public const string Where          = "where";
		public const string OrderBy        = "orderby";
		public const string Descending     = "descending";
		public const string Ascending      = "ascending";
		public const string Select         = "select";
		public const string Group          = "group";
		public const string In             = "in";
		public const string By             = "by";

		public const string Assert         = "assert";
		public const string Get            = "__get";
		public const string Set            = "__set";
		public const string Call           = "__call";
		public const string ToStringMethod = "__tostring";
		public const string Construct      = "__construct";
		public const string Destruct       = "__destruct";
		public const string WakeUp         = "__wakeup";
		public const string Sleep          = "__sleep";
		public const string AutoLoad       = "__autoload";

		public const string This           = "this";
		public const string Parent         = "parent";
		public const string Self           = "self";

		public const string True           = "true";
		public const string False          = "false";
		public const string Null           = "NULL";
	
		public const string Add            = "Add";
		public const string Remove         = "Remove";
		public const string Invoke         = "Invoke";
		public const string Main           = "Main";

		public const string IndexerGet     = "get_Item";
		public const string IndexerSet     = "set_Item";

		public const string AssemblyAttr   = "assembly: ";
		public const string AssemblyInfo   = "AssemblyInfo_";
	}

	/// <summary>
	/// Non-word tokens (brackets, operators, etc).
	/// </summary>
	internal static class Tokens
	{
		public const string BraceLeft             = "{";
		public const string BraceRight            = "}";
		public const string BracketLeft           = "[";
		public const string BracketRight          = "]";
		public const string ParenthesisLeft       = "(";
		public const string ParenthesisRight      = ")";
		public const string PhpBracketLeft        = "<?";
		public const string PhpBracketRight       = "?>";
		public const string GenericBracketLeft    = "<:";
		public const string GenericBracketRight   = ":>";
		public const string AttributeBracketLeft  = "[";
		public const string AttributeBracketRight = "]";

		public const string Dollar                = "$";
		public const string Reference             = "&";
		public const string Colon                 = ":";
		public const string Semicolon             = ";";
		public const string Comma                 = ",";
		public const string Arrow                 = "->";
		public const string DoubleArrow           = "=>";
		public const string DoubleColon           = "::";
		public const string NamespaceSeparator    = "\\";

		public const string Increment             = "++";
		public const string Decrement             = "--";

		public const string Identity              = "===";
		public const string NotIdentity           = "!==";
		public const string Equality              = "==";
		public const string NotEquality           = "!=";
		public const string NotEqualityAlt        = "<>";
		public const string LessOrEqual           = "<=";
		public const string GreaterOrEqual        = ">=";

		public const string Assignment            = "=";
		public const string AddAssignment         = "+=";
		public const string SubAssignment         = "-=";
		public const string MultAssignment        = "*=";
		public const string DivAssignment         = "/=";
		public const string ConcatAssignment      = ".=";
		public const string ModAssignment         = "%=";
		public const string ShiftLeftAssignment   = "<<=";
		public const string ShiftRightAssignment  = ">>=";
		public const string AndAssignment         = "&=";
		public const string OrAssignment          = "|=";
		public const string XorAssignent          = "^=";
		public const string BooleanOr             = "||";
		public const string BooleanAnd            = "&&";
		public const string ShiftLeft             = "<<";
		public const string ShiftRight            = ">>";
		
		public const string Addition              = "+";
		public const string Subtraction           = "-";
		public const string Multiplication        = "*";
		public const string Division              = "/";
		public const string Concatenation         = ".";
		public const string Modulo                = "%";
		public const string LogicalOr             = "|";
		public const string LogicalAnd            = "&";
		public const string Xor                   = "^";
		public const string Neg                   = "~";
		public const string Not                   = "!";

		public const string Pound                 = "#";
		public const string Comment               = "//";
		public const string CommentLeft           = "/*";
		public const string CommentRight          = "*/";
		public const string DocCommentLeft        = "/**";
		public const string DocCommentMiddle      = " *";
		public const string DocCommentRight       = " */";
	}

	/// <summary>
	/// White space characters.
	/// </summary>
	internal static class WhiteSpace
	{
		public const char Space                   = ' ';
		public const char Tab                     = '\t';
		public const char NewLine                 = '\n';
	}
}
