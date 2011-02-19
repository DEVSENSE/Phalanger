using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Package;
using System.IO;
using PHP.Core.Parsers;

namespace PHP.VisualStudio.PhalangerLanguageService.Parsing 
{
    /// <summary>
    /// An object reading a line of the source code and giving the tokens.
    /// </summary>
	internal sealed class LineReader : TextReader
	{
		#region Properties

		/// <summary>
		/// Offset of next character to be read.
		/// Each line is virtually suffixed with \r\n so the offset can point at most 2 characters behind.
		/// </summary>
		public int Offset { get { return offset; } }
		private int offset;

		/// <summary>
		/// Line data.
		/// </summary>
		internal string/*!*/ Data { get { return data; } }
		private string/*!*/ data;

		/// <summary>
		/// Next character.
		/// </summary>
		private int lookahead;

		#endregion
		
		#region Methods

        public LineReader(string/*!*/ data, int offset)
        {
            Debug.Assert(data != null && offset <= data.Length);

            this.data = data;
            this.offset = offset;
            UpdateLookahead();
        }

		private void UpdateLookahead()
		{
			if (offset < data.Length) lookahead = data[offset];
			else if (offset == data.Length) lookahead = '\r';
			else if (offset == data.Length + 1) lookahead = '\n';
			else lookahead = -1;
		}

		public override int Peek()
		{
			return lookahead;
		}

		public override int Read()
		{
			int result = lookahead;
			offset++;
			UpdateLookahead();
			return result;
		}

		public override int Read(char[]/*!*/ buffer, int index, int count)
		{
			Debug.Assert(buffer != null);

			if (offset == data.Length + 2) return 0;

			int end = Math.Min(offset + count, data.Length + 2);
			int result = Math.Min(data.Length, end) - offset;

			data.CopyTo(offset, buffer, index, result);

			if (end > data.Length)
			{
				buffer[index + result] = '\r';
				result++;
			}

			if (end > data.Length + 1)
			{
				buffer[index + result] = '\n';
				result++;
			}

			offset += result;
			UpdateLookahead();
			return result;
		}

		#endregion
	}

    /// <summary>
    /// Scanner of the source code, giving tokens.
    /// </summary>
	public sealed class PhpScanner : IScanner
	{
		#region Fields & Construction

		private LineReader lineReader = null;
		private bool bol = true;
		private bool eol = false;
        private Tokens prevtoken = 0;

		private int lineOffset = 0;
		private Tokenizer tokenizer = new Tokenizer(StringReader.Null);

		private Dictionary<Tokenizer.CompressedState, int> stateMap;
		private List<Tokenizer.CompressedState> states;

        /// <summary>
        /// Init the scanner.
        /// </summary>
		public PhpScanner()
		{
			Tokenizer.CompressedState init_state = tokenizer.GetCompressedState();

			states = new List<Tokenizer.CompressedState>(50);
			states.Add(init_state);

			stateMap = new Dictionary<Tokenizer.CompressedState, int>(50);
			stateMap[init_state] = 0;
		}

		#endregion

		#region IScanner implementation

        /// <summary>
        /// Scan the next token and fill in the info about it.
        /// </summary>
        /// <param name="tokenInfo">The result token info.</param>
        /// <param name="state">The last state of the scanner.</param>
        /// <returns>True if the token was available, false if there are no more tokens.</returns>
		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public bool ScanTokenAndProvideInfoAboutIt(TokenInfo tokenInfo, ref int state)
		{
            if (eol) return false;

			if (bol)
			{
				tokenizer.RestoreCompressedState(states[state]);
				bol = false;
                prevtoken = 0;
			}

			Tokens token = tokenizer.GetNextToken();
			TokenCategory category = tokenizer.TokenCategory;

			if (token == Tokens.EOF)
			{
				eol = true;

				Tokenizer.CompressedState last_state = tokenizer.GetCompressedState();

				// yymore:
				switch (tokenizer.CurrentLexicalState)
				{
					case Tokenizer.LexicalStates.ST_DOC_COMMENT:
						token = Tokens.T_DOC_COMMENT;
						category = TokenCategory.Comment;
						break;

					case Tokenizer.LexicalStates.ST_COMMENT:
						token = Tokens.T_COMMENT;
						category = TokenCategory.Comment;
						break;

					case Tokenizer.LexicalStates.ST_SINGLE_QUOTES:
						token = Tokens.SingleQuotedString;
						category = TokenCategory.String;
						break;
				}

				if (!stateMap.TryGetValue(last_state, out state))
				{
					stateMap.Add(last_state, state = states.Count);
					states.Add(last_state);
				}

				if (token == Tokens.EOF)
					return false;
			}

			tokenInfo.StartIndex = lineOffset;

			int token_length = tokenizer.TokenText.Length;

			// tokens ending by '\r\n':
			if (lineOffset + token_length == lineReader.Data.Length + 2)
				token_length -= 2;

			lineOffset += token_length;
			tokenInfo.EndIndex = lineOffset - 1;

			UpdateTokenInfo(token, category, tokenInfo);

            prevtoken = token;
			return true;
		}

        /// <summary>
        /// Set the source code line.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="offset"></param>
		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public void SetSource(string source, int offset)
		{
            //System.Diagnostics.Debug.Print("3. PhpScanner.SetSource, line:{0}, source:{1}", offset, source);

            this.lineOffset = offset;
			this.lineReader = new LineReader(source, offset);
			this.bol = true;
			this.eol = false;
			tokenizer.Initialize(lineReader, Tokenizer.LexicalStates.INITIAL, true);
		}

        /// <summary>
        /// Update token info from the given token category and token index.
        /// </summary>
        /// <param name="token">Token index.</param>
        /// <param name="category">token category.</param>
        /// <param name="info">Result token info.</param>
		private void UpdateTokenInfo(Tokens token, TokenCategory category, TokenInfo info)
		{
			info.Token = (int)token;
			info.Trigger = TokenTriggers.None;

            switch (category)
			{
				case TokenCategory.WhiteSpace:
					info.Type = TokenType.WhiteSpace;
					info.Color = TokenColor.Text;
					break;

				case TokenCategory.Comment:
					info.Type = TokenType.Comment;
					info.Color = TokenColor.Comment;
					break;

				case TokenCategory.LineComment:
					info.Type = TokenType.LineComment;
					info.Color = TokenColor.Comment;
					break;

				case TokenCategory.Keyword:
					info.Type = TokenType.Keyword;
					info.Color = TokenColor.Keyword;

					break;
				case TokenCategory.Identifier:
					info.Type = TokenType.Identifier;
					info.Color = TokenColor.Identifier;
                    info.Trigger = TokenTriggers.MemberSelect;
					break;

				case TokenCategory.Delimiter:
					info.Type = TokenType.Delimiter;
					info.Color = TokenColor.Text;

					switch (token)
					{
						case Tokens.T_COMMA:
							info.Trigger = TokenTriggers.ParameterNext;
							break;

						// braces, parentheses, brackets:
						case Tokens.T_LPAREN:
						case Tokens.T_LBRACE:
						case Tokens.T_LBRACKET:
							info.Trigger = TokenTriggers.MatchBraces | TokenTriggers.ParameterStart;
							break;

						case Tokens.T_RPAREN:
						case Tokens.T_RBRACE:
						case Tokens.T_RBRACKET:
							info.Trigger = TokenTriggers.MatchBraces | TokenTriggers.ParameterEnd;
							break;
					}

					break;

				case TokenCategory.Operator:
					info.Type = TokenType.Operator;
					info.Color = TokenColor.Text;

                    switch (token)
                    {
                        case Tokens.T_DOUBLE_COLON:
                        case Tokens.T_DOLLAR:
                        case Tokens.T_OBJECT_OPERATOR:
                            info.Trigger = TokenTriggers.MemberSelect;
                            break;
                        case Tokens.T_COLON:
                            if (prevtoken == Tokens.T_DOUBLE_COLON)
                                info.Trigger = TokenTriggers.MemberSelect;  // only for :::
                            break;
                    }

					break;

				case TokenCategory.Number:
					info.Type = TokenType.Literal;
					info.Color = TokenColor.Number;
					break;

				case TokenCategory.String:
					info.Type = TokenType.String;
					info.Color = TokenColor.String;
                    switch (token)
                    {
                        case Tokens.DoubleQuotedString:
                        case Tokens.SingleQuotedString:
                            info.Trigger |= TokenTriggers.MatchBraces;
                            break;
                    }
					return;

				case TokenCategory.StringCode:
					info.Type = TokenType.String;
					info.Color = PhpLanguage.EncapsulatedVariableColor;
                    return;

				case TokenCategory.ScriptTags:
					info.Type = TokenType.Text;
					info.Color = PhpLanguage.ScriptTagsColor;
					break;

				case TokenCategory.Html:
					info.Type = TokenType.Text;
					info.Color = PhpLanguage.OuterHtmlColor;
					break;

				case TokenCategory.Variable:
					info.Type = TokenType.Text;
					info.Color = TokenColor.Text;
					break;

				default:
					info.Type = TokenType.Text;
					info.Color = TokenColor.Text;
					break;
			}

            // add MemberSelect to all function/variable/type names
            if (PhpScope.TokenShouldBeFunctionName(token))
            {
                info.Trigger |= TokenTriggers.MemberSelect;
            }
		}

		#endregion
	}
}
