/*

 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using PHP.Core;
using PHP.Core.Parsers.GPPG;
using PHP.Core.Reflection;

namespace PHP.Core.Parsers
{
    #region ICommentsSink

    /// <summary>
    /// Sink for comment tokens and tokens not handled in parser.
    /// These tokens are ignored by tokenizer, so they are not available in resulting AST.
    /// By providing this interface as a part of <see cref="IReductionsSink"/> implementation, implementers may handle additional language elements at token level.
    /// </summary>
    public interface ICommentsSink
    {
        void OnLineComment(Scanner/*!*/scanner, Parsers.Position position);
        void OnComment(Scanner/*!*/scanner, Parsers.Position position);
        void OnPhpDocComment(Scanner/*!*/scanner, PHPDocBlock phpDocBlock);

        void OnOpenTag(Scanner/*!*/scanner, Parsers.Position position);
        void OnCloseTag(Scanner/*!*/scanner, Parsers.Position position);
    }

    #endregion

    public sealed class Scanner : Lexer, ITokenProvider<SemanticValueType, Position>
    {
        #region Nested class: _NullCommentsSink

        private sealed class _NullCommentsSink : ICommentsSink
        {
            #region ICommentsSink Members

            public void OnLineComment(Scanner scanner, Parsers.Position position) { }
            public void OnComment(Scanner scanner, Parsers.Position position) { }
            public void OnPhpDocComment(Scanner scanner, PHPDocBlock phpDocBlock) { }
            public void OnOpenTag(Scanner scanner, Parsers.Position position) { }
            public void OnCloseTag(Scanner scanner, Parsers.Position position) { }

            #endregion
        }

        #endregion

        public ErrorSink/*!*/ ErrorSink { get { return errors; } }
		private readonly ErrorSink/*!*/ errors;

        /// <summary>
        /// Sink for comments.
        /// </summary>
        private readonly ICommentsSink/*!*/commentsSink;

		public LanguageFeatures LanguageFeatures { get { return features; } }
		private readonly LanguageFeatures features;

		public SourceUnit/*!*/ SourceUnit { get { return sourceUnit; } }
		private readonly SourceUnit/*!*/ sourceUnit;

		// encapsed string buffering:
		public StringBuilder/*!*/ EncapsedStringBuffer { get { return encapsedStringBuffer; } }
		private readonly StringBuilder/*!*/ encapsedStringBuffer = new StringBuilder(1000);

        ///// <summary>
        ///// Buffer used for qualified names parsing.
        ///// </summary>
        //private readonly List<string>/*!*/ qualifiedNameBuffer = new List<string>(10);

		private SemanticValueType tokenSemantics;
		private Parsers.Position tokenPosition;

        #region T_DOC_COMMENT handling

        /// <summary>
        /// Last doc comment read from input.
        /// </summary>
        private PHPDocBlock lastDocComment;

        /// <summary>
        /// Tokens that should remember current <see cref="lastDocComment"/>.
        /// </summary>
        private static readonly HashSet<Tokens>/*!*/lastDocCommentRememberTokens = new HashSet<Tokens>()
        {
            Tokens.T_ABSTRACT, Tokens.T_FINAL, Tokens.T_STATIC, Tokens.T_PUBLIC, Tokens.T_PRIVATE, Tokens.T_PROTECTED,  // modifiers, also holds the doc comment (for class fields without T_VAR)
            Tokens.T_VAR,       // for class fields
            Tokens.T_CONST,     // for constants
            Tokens.T_CLASS, Tokens.T_TRAIT, Tokens.T_INTERFACE, // for type decl
            Tokens.T_FUNCTION,  // for function/method
        };

        /// <summary>
        /// Tokens that keep value of <see cref="lastDocComment"/> to be used later for <see cref="lastDocCommentRememberTokens"/>.
        /// </summary>
        private static readonly HashSet<Tokens>/*!*/lastDocCommentKeepTokens = new HashSet<Tokens>()
        {
            Tokens.T_ABSTRACT, Tokens.T_STATIC, Tokens.T_PUBLIC, Tokens.T_PRIVATE, Tokens.T_PROTECTED,
            Tokens.T_FINAL, Tokens.T_PARTIAL,
            // TODO: tokens within attributes_opt
        };

        #endregion

        private int streamOffset;

		// position shifts:
		private int lineShift;
		private int columnShift;
		private int offsetShift;

		private readonly Encoding encoding;
		private bool pure;

        public Scanner(Parsers.Position initialPosition, TextReader/*!*/ reader, SourceUnit/*!*/ sourceUnit,
			ErrorSink/*!*/ errors, ICommentsSink commentsSink, LanguageFeatures features)
			: base(reader)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");
			if (sourceUnit == null)
				throw new ArgumentNullException("sourceUnit");
			if (errors == null)
				throw new ArgumentNullException("errors");

			this.lineShift = initialPosition.FirstLine;
			this.columnShift = initialPosition.FirstColumn;
			this.offsetShift = initialPosition.FirstOffset;

			this.errors = errors;
            this.commentsSink = commentsSink ?? new _NullCommentsSink();
			this.features = features;
			this.sourceUnit = sourceUnit;

			this.streamOffset = 0;
			this.pure = sourceUnit.CompilationUnit.IsPure;
			this.encoding = sourceUnit.Encoding;

			AllowAspTags = (features & LanguageFeatures.AspTags) != 0;
			AllowShortTags = (features & LanguageFeatures.ShortOpenTags) != 0;
		}

		private void StoreEncapsedString()
		{
			tokenSemantics.Integer = TokenLength;
			tokenSemantics.Offset = encapsedStringBuffer.Length;
			AppendTokenTextTo(encapsedStringBuffer);
		}

		private void StoreEncapsedString(string str)
		{
			tokenSemantics.Integer = str.Length;
			tokenSemantics.Offset = encapsedStringBuffer.Length;
			encapsedStringBuffer.Append(str);
		}

		public string GetEncapsedString(int offset, int length)
		{
			return encapsedStringBuffer.ToString(offset, length);
		}

        /// <summary>
        /// Updates <see cref="streamOffset"/> and <see cref="tokenPosition"/>.
        /// </summary>
        private void UpdateTokenPosition()
		{
			// update token position info:
			int byte_length = base.GetTokenByteLength(encoding);

			tokenPosition.FirstOffset = offsetShift + streamOffset;
			tokenPosition.FirstLine = lineShift + token_start_pos.Line;

			if (token_start_pos.Line == 0)
				tokenPosition.FirstColumn = columnShift + token_start_pos.Column;
			else
				tokenPosition.FirstColumn = token_start_pos.Column;

			tokenPosition.LastOffset = tokenPosition.FirstOffset + byte_length - 1;
			tokenPosition.LastLine = lineShift + token_end_pos.Line;

			if (token_end_pos.Line == 0)
				tokenPosition.LastColumn = columnShift + token_end_pos.Column;
			else
				tokenPosition.LastColumn = token_end_pos.Column;

			streamOffset += byte_length;
		}

        public new Tokens GetNextToken()
		{
			for (; ; )
			{
                inString = false;
                isCode = false;
                
                Tokens token = base.GetNextToken();
                UpdateTokenPosition();

				switch (token)
				{
					#region Comments

                    // ignored tokens:
                    case Tokens.T_WHITESPACE: break;
                    case Tokens.T_COMMENT: this.commentsSink.OnComment(this, this.tokenPosition); break;
                    case Tokens.T_LINE_COMMENT: this.commentsSink.OnLineComment(this, this.tokenPosition); break;
                    case Tokens.T_OPEN_TAG: this.commentsSink.OnOpenTag(this, this.tokenPosition); break;

                    case Tokens.T_DOC_COMMENT:
                        // remember token value to be used by the next token and skip the current:
                        this.lastDocComment = new PHPDocBlock(base.GetTokenString(), this.tokenPosition);
                        this.commentsSink.OnPhpDocComment(this, this.lastDocComment);
                        break;

					case Tokens.T_PRAGMA_FILE:
						sourceUnit.AddSourceFileMapping(tokenPosition.FirstLine, base.GetTokenAsFilePragma());
						break;

					case Tokens.T_PRAGMA_LINE:
						{
							int? value = base.GetTokenAsLinePragma();

							if (value.HasValue)
								sourceUnit.AddSourceLineMapping(tokenPosition.FirstLine, value.Value);
							else
								errors.Add(Warnings.InvalidLinePragma, sourceUnit, tokenPosition);
							
							break;
						}

					case Tokens.T_PRAGMA_DEFAULT_FILE:
						sourceUnit.AddSourceFileMapping(tokenPosition.FirstLine, SourceUnit.DefaultFile);
						break;

					case Tokens.T_PRAGMA_DEFAULT_LINE:
						sourceUnit.AddSourceLineMapping(tokenPosition.FirstLine, SourceUnit.DefaultLine);
						break;

					#endregion

					#region String Semantics

					case Tokens.T_VARIABLE:
						// exclude initial $ from the name:
						Debug.Assert(GetTokenChar(0) == '$');
						tokenSemantics.Object = base.GetTokenSubstring(1);
						goto default;

					case Tokens.T_STRING:
						if (inString)
							StoreEncapsedString();
						else
							tokenSemantics.Object = base.GetTokenString();

						goto default;

                    case Tokens.T_ARRAY:
                    case Tokens.T_LIST:
                    case Tokens.T_ASSERT:
                        tokenSemantics.Object = base.GetTokenString();  // remember the token string, so we can use these tokens as literals later, case sensitively
                        goto default;

					case Tokens.T_STRING_VARNAME:
					case Tokens.T_NUM_STRING:
					case Tokens.T_ENCAPSED_AND_WHITESPACE:
					case Tokens.T_BAD_CHARACTER:
						StoreEncapsedString();
						goto default;

					case Tokens.T_INLINE_HTML:
						tokenSemantics.Object = base.GetTokenString();
						goto default;


					// \[uU]#{0-6}
					case Tokens.UnicodeCharCode:
						{
							Debug.Assert(inString);

							//if (GetTokenChar(1) == 'u')
							//{
							//  if (TokenLength != 2 + 4)
							//    errors.Add(Warnings.InvalidEscapeSequenceLength, sourceFile, tokenPosition.Short, GetTokenString(), 4);
							//}
							//else
							//{
							//  if (TokenLength != 2 + 6)
							//    errors.Add(Warnings.InvalidEscapeSequenceLength, sourceFile, tokenPosition.Short, GetTokenString(), 6);
							//}

							int code_point = GetTokenAsInteger(2, 16);

							try
							{
								if ((code_point < 0 || code_point > 0x10ffff) || (code_point >= 0xd800 && code_point <= 0xdfff))
								{
									errors.Add(Errors.InvalidCodePoint, SourceUnit, tokenPosition, GetTokenString());
									StoreEncapsedString("?");
								}
								else
								{
									StoreEncapsedString(StringUtils.Utf32ToString(code_point));
								}
							}
							catch (ArgumentOutOfRangeException)
							{
								errors.Add(Errors.InvalidCodePoint, SourceUnit, tokenPosition, GetTokenString());
								StoreEncapsedString("?");
							}
							token = Tokens.T_STRING;
							goto default;
						}

					// \C{name}
					case Tokens.UnicodeCharName:
						Debug.Assert(inString);
						StoreEncapsedString(); // N/S
						token = Tokens.T_STRING;
						goto default;

					// b?"xxx"
                    case Tokens.DoubleQuotedString:
                        {
                            bool forceBinaryString = GetTokenChar(0) == 'b';

                            tokenSemantics.Object = GetTokenAsDoublyQuotedString(forceBinaryString ? 1 : 0, encoding, forceBinaryString);
                            token = Tokens.T_CONSTANT_ENCAPSED_STRING;
                            goto default;
                        }

					// b?'xxx'
					case Tokens.SingleQuotedString:
                        {
                            bool forceBinaryString = GetTokenChar(0) == 'b';

                            tokenSemantics.Object = GetTokenAsSinglyQuotedString(forceBinaryString ? 1 : 0, encoding, forceBinaryString);
                            token = Tokens.T_CONSTANT_ENCAPSED_STRING;
                            goto default;
                        }

					#endregion

					#region Numeric Semantics

					case Tokens.T_CURLY_OPEN:
						tokenSemantics.Integer = (int)Tokens.T_CURLY_OPEN;
						goto default;

					case Tokens.T_CHARACTER:
						tokenSemantics.Integer = (int)GetTokenChar(0);
						goto default;

					case Tokens.EscapedCharacter:
						tokenSemantics.Integer = (int)GetTokenAsEscapedCharacter(0);
						token = Tokens.T_CHARACTER;
						goto default;

					case Tokens.T_LINE:
						// TODO: 
						tokenSemantics.Integer = 1;
						goto default;

					// "\###"
					case Tokens.OctalCharCode:
						tokenSemantics.Integer = GetTokenAsInteger(1, 10);
						token = Tokens.T_CHARACTER;
						goto default;

					// "\x##"
					case Tokens.HexCharCode:
						tokenSemantics.Integer = GetTokenAsInteger(2, 16);
						token = Tokens.T_CHARACTER;
						goto default;

					// {LNUM}
					case Tokens.ParseDecimalNumber:
						{
							// [0-9]* - value is either in octal or in decimal
							if (GetTokenChar(0) == '0')
								token = GetTokenAsDecimalNumber(1, 8, ref tokenSemantics);
							else
								token = GetTokenAsDecimalNumber(0, 10, ref tokenSemantics);

							if (token == Tokens.T_DNUMBER)
							{
								// conversion to double causes data loss
								errors.Add(Warnings.TooBigIntegerConversion, SourceUnit, tokenPosition, GetTokenString());
							}
							goto default;
						}

					// {HNUM}
                    case Tokens.ParseHexadecimalNumber:
                        {
                            // parse hexadecimal value
                            token = GetTokenAsDecimalNumber(2, 16, ref tokenSemantics);

                            if (token == Tokens.T_DNUMBER)
                            {
                                // conversion to double causes data loss
                                errors.Add(Warnings.TooBigIntegerConversion, SourceUnit, tokenPosition, GetTokenString());
                            }
                            goto default;
                        }

                    // {BNUM}
                    case Tokens.ParseBinaryNumber:
                        // parse binary number value
                        token = GetTokenAsDecimalNumber(2, 2, ref tokenSemantics);

                        if (token == Tokens.T_DNUMBER)
                        {
                            // conversion to double causes data loss
                            errors.Add(Warnings.TooBigIntegerConversion, SourceUnit, tokenPosition, GetTokenString());
                        }
                        goto default;

					// {DNUM}|{EXPONENT_DNUM}
					case Tokens.ParseDouble:
						tokenSemantics.Double = GetTokenAsDouble(0);
						token = Tokens.T_DNUMBER;
						goto default;

                    #endregion

					#region Another Semantics

                    // i'xxx'	
					case Tokens.SingleQuotedIdentifier:
						tokenSemantics.Object = (string)GetTokenAsSinglyQuotedString(1, encoding, false);
						token = Tokens.T_STRING;
						goto default;

					#endregion

					#region Token Reinterpreting

					case Tokens.T_OPEN_TAG_WITH_ECHO:
                        this.commentsSink.OnOpenTag(this, this.tokenPosition);                        
						token = Tokens.T_ECHO;
						goto default;

					case Tokens.T_CLOSE_TAG:
                        this.commentsSink.OnCloseTag(this, this.tokenPosition);                        
						token = Tokens.T_SEMI;
						goto case Tokens.T_SEMI;

					case Tokens.T_TRUE:
					case Tokens.T_FALSE:
					case Tokens.T_NULL:
					case Tokens.T_GET:
					case Tokens.T_SET:
					case Tokens.T_CALL:
                    case Tokens.T_CALLSTATIC:
					case Tokens.T_WAKEUP:
					case Tokens.T_SLEEP:
					case Tokens.T_TOSTRING:
					case Tokens.T_CONSTRUCT:
					case Tokens.T_DESTRUCT:
					case Tokens.T_PARENT:
					case Tokens.T_SELF:
					case Tokens.T_AUTOLOAD:
						token = Tokens.T_STRING;
						goto case Tokens.T_STRING;

					case Tokens.T_TRY:
					case Tokens.T_CATCH:
					case Tokens.T_THROW:
					case Tokens.T_IMPLEMENTS:
					case Tokens.T_CLONE:
					case Tokens.T_ABSTRACT:
					case Tokens.T_FINAL:
					case Tokens.T_PRIVATE:
					case Tokens.T_PROTECTED:
					case Tokens.T_PUBLIC:
					case Tokens.T_INSTANCEOF:
					case Tokens.T_INTERFACE:
                    case Tokens.T_GOTO:
                    case Tokens.T_NAMESPACE:
                    case Tokens.T_NAMESPACE_C:
                    case Tokens.T_NS_SEPARATOR:
                    case Tokens.T_USE:
                        {
							if ((features & LanguageFeatures.V5Keywords) == 0)
							{
								token = Tokens.T_STRING;
								goto case Tokens.T_STRING;
							}

                            if (token == Tokens.T_ABSTRACT)
                            {
                                // remember this for possible CLR qualified name:
                                tokenSemantics.Object = base.GetTokenString();
                            }

							goto default;
						}

					case Tokens.T_LINQ_FROM:
					case Tokens.T_LINQ_SELECT:
					case Tokens.T_LINQ_BY:
					case Tokens.T_LINQ_WHERE:
					case Tokens.T_LINQ_DESCENDING:
					case Tokens.T_LINQ_ASCENDING:
					case Tokens.T_LINQ_ORDERBY:
					case Tokens.T_LINQ_GROUP:
					case Tokens.T_LINQ_IN:
						{
							if ((features & LanguageFeatures.Linq) == 0)
							{
								token = Tokens.T_STRING;
								goto case Tokens.T_STRING;
							}

							goto default;
						}

                    case Tokens.T_IMPORT:
                        {
                            if (!sourceUnit.CompilationUnit.IsPure)
                            {
                                token = Tokens.T_STRING;
                                goto case Tokens.T_STRING;
                            }

                            goto default;
                        }

					case Tokens.T_BOOL_TYPE:
					case Tokens.T_INT_TYPE:
					case Tokens.T_INT64_TYPE:
					case Tokens.T_DOUBLE_TYPE:
					case Tokens.T_STRING_TYPE:
					case Tokens.T_RESOURCE_TYPE:
					case Tokens.T_OBJECT_TYPE:
					case Tokens.T_TYPEOF:
						{
							if ((features & LanguageFeatures.TypeKeywords) == 0)
							{
								token = Tokens.T_STRING;
								goto case Tokens.T_STRING;
							}

                            tokenSemantics.Object = base.GetTokenString();

							goto default;
						}

					case Tokens.T_PARTIAL:
						{
							if (!pure)
							{
								token = Tokens.T_STRING;
								goto case Tokens.T_STRING;
							}

							goto default;
						}

					#endregion

					#region Error Tokens

					case Tokens.ERROR:
						goto default;

					case Tokens.ErrorInvalidIdentifier:
						{
							// invalid identifier i'XXX':
							errors.Add(Errors.InvalidIdentifier, SourceUnit, tokenPosition, (string)GetTokenAsSinglyQuotedString(1, encoding, false));

							tokenSemantics.Object = GetErrorIdentifier();
							token = Tokens.T_STRING;
							goto default;
						}

					case Tokens.ErrorNotSupported:
						errors.Add(Errors.ConstructNotSupported, SourceUnit, tokenPosition, GetTokenString());
						tokenSemantics.Object = GetErrorIdentifier();
						token = Tokens.T_STRING;
						goto default;

					#endregion

					case Tokens.T_SEMI:
					default:

                        if (lastDocComment != null)
                        {
                            // remember PHPDoc for current token
                            if (lastDocCommentRememberTokens.Contains(token))
                                tokenSemantics.Object = this.lastDocComment;

                            // forget last doc comment text
                            if (!lastDocCommentKeepTokens.Contains(token))
                                lastDocComment = null;
                        }
                        
						return token;
				}
			}
		}

        #region ITokenProvider<SemanticValueType, Parsers.Position> Members

		int ITokenProvider<SemanticValueType, Parsers.Position>.GetNextToken()
		{
			return (int)GetNextToken();
		}

		void ITokenProvider<SemanticValueType, Parsers.Position>.ReportError(string[] expectedTerminals)
		{
			// TODO (expected tokens....)
			errors.Add(FatalErrors.SyntaxError, SourceUnit, tokenPosition,
				CoreResources.GetString("unexpected_token", GetTokenString()));

			//throw new CompilerException();	
		}

		SemanticValueType ITokenProvider<SemanticValueType, Parsers.Position>.TokenValue
		{
			get { return tokenSemantics; }
		}

		Parsers.Position ITokenProvider<SemanticValueType, Parsers.Position>.TokenPosition
		{
			get { return tokenPosition; }
		}

		#endregion

		#region Positions

		//This field is setted by updateTokenPosition (both versions)
		//and it is read in GetNextToken.
		//Position is expressed as byte count, NOT as char count which
		//are different when e.g. UTF-8 is used.  
		//AST.Position tokenPosition;

		///// <sumary>
		///// Position of token before the current token
		///// </sumary>
		///// <remarks>
		///// It is necessarry to remember this because of yymore, when we must return
		///// to oldTokenPosition to set the next token position right.
		///// </remarks>
		//Position oldTokenPosition;

		//private void updateTokenPositionToNewLine()
		//{
		//  tokenPosition.LastLine++;
		//  tokenPosition.LastColumn = 0;
		//}

		//private void updateTokenPosition(int leng)
		//{
		//  //tokenPosition is now position of the previously read token
		//  oldTokenPosition = tokenPosition;

		//  //This token begins right behind the previously read token
		//  //First line and column and first and last char are setted always in the right
		//  //way.
		//  //In cases that token (potentialy) contains \n, 
		//  //other updateTokenPosition version is called to repair LastLine and LastColumn
		//  tokenPosition.FirstLine = tokenPosition.LastLine;
		//  tokenPosition.FirstColumn = tokenPosition.LastColumn + 1;
		//  tokenPosition.FirstChar = tokenPosition.LastChar + 1;
		//  //LastLine has not changed
		//  tokenPosition.LastColumn += leng;
		//  tokenPosition.LastChar += leng;

		//  //tokenPosition is now position of the right now read token 
		//}

		//private unsafe void updateTokenPosition(char* text, int leng)
		//{
		//  char *pos = text;
		//  char *begin_line_pos = 0;

		//  do
		//  {
		//    pos = strchr(pos,'\n');
		//    if(pos != 0) 
		//    {
		//      tokenPosition.LastLine++;
		//      begin_line_pos = ++pos;
		//    }
		//  } while(pos != 0);

		//  if(begin_line_pos) tokenPosition.LastColumn = (int)(text + leng - begin_line_pos);
		//}		

		#endregion

		#region Erroneous Identifiers

		private int errorNameCounter = 0;
		private const string ErrorNamePrefix = "__error#";

		internal string/*!*/ GetErrorIdentifier()
		{
			return ErrorNamePrefix + errorNameCounter++;
		}

		#endregion
	}
}