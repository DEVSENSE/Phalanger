/*

 Copyright (c) 2008 Daniel Balas.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Text;
using GPPG = PHP.Core.Parsers.GPPG;
using System.IO;

namespace PHP.Core.EmbeddedDoc
{
	public class Scanner : DocLexer, GPPG.ITokenProvider<SemanticValueType, Position>
	{
		SemanticValueType tokenSemantics;
		PHP.Core.EmbeddedDoc.Position tokenPosition;

		/// <summary>
		/// Gets or sets a value indicating whether scanner should compound tokens into string. All
		/// normal tokens will be grouped into a special type of token T_COMPOUND until inline tags 
		/// or element tags are encountered. This behavior is controlled by the parser and is
		/// present because of high ambiguousness of the embedded doc language. 
		/// </summary>
		public bool CompoundTokens { get { return compoundTokens; } set { compoundTokens = value; } }// Console.WriteLine("Compounding is now {0}", compoundTokens); } }
		private bool compoundTokens = false;

		private bool useLastToken = false;
		private int lastToken = -1;
		private SemanticValueType lastSemantics;
		private PHP.Core.EmbeddedDoc.Position lastPosition;

		public Scanner(TextReader/*!*/ reader)
			: base(reader)
		{ }

		#region ITokenProvider<SemanticValueType,Position> Members

		public SemanticValueType TokenValue
		{
			get { return tokenSemantics; }
		}

		public PHP.Core.EmbeddedDoc.Position TokenPosition
		{
			get { return tokenPosition; }
		}

		public int FetchToken()
		{
			tokenPosition = new PHP.Core.EmbeddedDoc.Position();
			tokenSemantics = new SemanticValueType();

			while (true)
			{
				Tokens token = base.GetNextToken();

				switch (token)
				{
					case Tokens.T_BEGIN:
					case Tokens.T_END:
					case Tokens.T_LINE_BEGIN:
						continue;
					case Tokens.T_IDENTIFIER:
					case Tokens.T_WHITESPACE:
					case Tokens.T_INTEGER:
					case Tokens.T_SYMBOL:
					case Tokens.T_LBRA:
					case Tokens.T_RBRA:
					case Tokens.T_ARRAY:
					case Tokens.T_PUBLIC:
					case Tokens.T_PRIVATE:
					case Tokens.T_PROTECTED:
					case Tokens.T_DOLLAR:
					case Tokens.T_BAR:
						tokenSemantics.String = GetTokenString();
						break;
					case Tokens.T_NEWLINE:
						tokenSemantics.String = "\n";
						token = Tokens.T_WHITESPACE;
						break;
				}

				//Console.WriteLine(token + " \"" + TokenValue.ToString() + "\"");

				return (int)token;
			}
		}

		public int CompoundToken()
		{
			StringBuilder compoundBuilder = new StringBuilder();

			if (useLastToken)
			{
				tokenSemantics = lastSemantics;
				tokenPosition = lastPosition;
				useLastToken = false;

				return lastToken;
			}

			while (true)
			{
				Tokens token = (Tokens)FetchToken();

				switch(token)
				{
					case Tokens.T_IDENTIFIER:
					case Tokens.T_WHITESPACE:
					case Tokens.T_INTEGER:
					case Tokens.T_SYMBOL:
					case Tokens.T_LBRA:
					case Tokens.T_RBRA:					
					case Tokens.T_ARRAY:
					case Tokens.T_PUBLIC:
					case Tokens.T_PRIVATE:
					case Tokens.T_PROTECTED:
					case Tokens.T_DOLLAR:
					case Tokens.T_BAR:
						compoundBuilder.Append(tokenSemantics.String);
						continue;
					default:
						useLastToken = true;
						lastToken = (int)token;
						lastSemantics = tokenSemantics;
						lastPosition = tokenPosition;
						break;
				}

				break;
			}

			tokenSemantics = new SemanticValueType();
			tokenSemantics.String = compoundBuilder.ToString();

			return (int)Tokens.T_COMPOUND;
		}

		public new int GetNextToken()
		{
			int token;

			if (CompoundTokens)
			{
				token = CompoundToken();
			}
			else
			{
				token = FetchToken();
			}

			//Console.WriteLine((Tokens)token + " \"" + TokenValue.ToString() + "\"");

			return token;
		}

		public void ReportError(string[] expectedTokens)
		{
		}

		#endregion
	}
}
