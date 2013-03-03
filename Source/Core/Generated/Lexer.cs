namespace PHP.Core.Parsers
{
	#region User Code
	
	/*
 Copyright (c) 2004-2006 Tomas Matousek. Based on PHP5 and PHP6 grammar tokens definition. 
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 You must not remove this notice from this software.
*/
using System;
using PHP.Core;
using System.Collections.Generic;
#endregion
	
	
	public partial class Lexer
	{
		public enum LexicalStates
		{
			INITIAL = 0,
			ST_IN_SCRIPTING = 1,
			ST_DOUBLE_QUOTES = 2,
			ST_SINGLE_QUOTES = 3,
			ST_BACKQUOTE = 4,
			ST_HEREDOC = 5,
			ST_NEWDOC = 6,
			ST_LOOKING_FOR_PROPERTY = 7,
			ST_LOOKING_FOR_VARNAME = 8,
			ST_DOC_COMMENT = 9,
			ST_COMMENT = 10,
			ST_ONE_LINE_COMMENT = 11,
		}
		
		[Flags]
		private enum AcceptConditions : byte
		{
			NotAccept = 0,
			AcceptOnStart = 1,
			AcceptOnEnd = 2,
			Accept = 4
		}
		
		public struct Position
		{
			public int Char;
			public int Line;
			public int Column;
			public Position(int ch, int line, int column)
			{
				this.Char = ch;
				this.Line = line;
				this.Column = column;
			}
		}
		private const int NoState = -1;
		private const char BOL = (char)128;
		private const char EOF = (char)129;
		
		private Tokens yyreturn;
		
		private System.IO.TextReader reader;
		private char[] buffer = new char[512];
		
		// whether the currently parsed token is being expanded (yymore has been called):
		private bool expanding_token;
		
		// offset in buffer where the currently parsed token starts:
		private int token_start;
		
		// offset in buffer where the currently parsed token chunk starts:
		private int token_chunk_start;
		
		// offset in buffer one char behind the currently parsed token (chunk) ending character:
		private int token_end;
		
		// offset of the lookahead character (number of characters parsed):
		private int lookahead_index;
		
		// number of characters read into the buffer:
		private int chars_read;
		
		// parsed token start position (wrt beginning of the stream):
		protected Position token_start_pos;
		
		// parsed token end position (wrt beginning of the stream):
		protected Position token_end_pos;
		
		private bool yy_at_bol = false;
		
		public LexicalStates CurrentLexicalState { get { return current_lexical_state; } set { current_lexical_state = value; } } 
		private LexicalStates current_lexical_state;
		
		public Lexer(System.IO.TextReader reader)
		{
			Initialize(reader, LexicalStates.INITIAL);
		}
		
		public void Initialize(System.IO.TextReader reader, LexicalStates lexicalState, bool atBol)
		{
			this.expanding_token = false;
			this.token_start = 0;
			this.chars_read = 0;
			this.lookahead_index = 0;
			this.token_chunk_start = 0;
			this.token_end = 0;
			this.reader = reader;
			this.yy_at_bol = atBol;
			this.current_lexical_state = lexicalState;
		}
		
		public void Initialize(System.IO.TextReader reader, LexicalStates lexicalState)
		{
			Initialize(reader, lexicalState, false);
		}
		
		#region Accept
		
		#pragma warning disable 162
		
		
		Tokens Accept0(int state,out bool accepted)
		{
			accepted = true;
			
			switch(state)
			{
				case 2:
					// #line 77
					{ 
						return Tokens.T_INLINE_HTML; 
					}
					break;
					
				case 3:
					// #line 105
					{ 
						if (AllowAspTags)
						{
							BEGIN(LexicalStates.ST_IN_SCRIPTING);
							return Tokens.T_OPEN_TAG;
						} 
						else 
						{
							return Tokens.T_INLINE_HTML;
						}
					}
					break;
					
				case 4:
					// #line 81
					{
						if (AllowShortTags || TokenLength>2) 
						{ 
							BEGIN(LexicalStates.ST_IN_SCRIPTING);
							return Tokens.T_OPEN_TAG;
						} 
						else 
						{
							return Tokens.T_INLINE_HTML;
						}
					}
					break;
					
				case 5:
					// #line 93
					{
						if (GetTokenChar(1) == '%' && AllowAspTags || GetTokenChar(1) == '?' && AllowShortTags) 
						{
							BEGIN(LexicalStates.ST_IN_SCRIPTING);
							return Tokens.T_OPEN_TAG_WITH_ECHO;
						} 
						else 
						{
							return Tokens.T_INLINE_HTML;
						}
					}
					break;
					
				case 6:
					// #line 117
					{
						BEGIN(LexicalStates.ST_IN_SCRIPTING);
						return Tokens.T_OPEN_TAG;
					}
					break;
					
				case 7:
					// #line 278
					{ return (Tokens)GetTokenChar(0); }
					break;
					
				case 8:
					// #line 354
					{ 
						BEGIN(LexicalStates.ST_BACKQUOTE); 
						return Tokens.T_BACKQUOTE; 
					}
					break;
					
				case 9:
					// #line 279
					{ return Tokens.T_STRING; }
					break;
					
				case 10:
					// #line 281
					{ return Tokens.T_WHITESPACE; }
					break;
					
				case 11:
					// #line 338
					{ 
						BEGIN(LexicalStates.ST_DOUBLE_QUOTES); 
						return (GetTokenChar(0) != '"') ? Tokens.T_BINARY_DOUBLE : Tokens.T_DOUBLE_QUOTES; 
					}
					break;
					
				case 12:
					// #line 344
					{ 
						// Gets here only in the case of unterminated singly-quoted string. That leads usually to an error token,
						// however when the source code is parsed per-line (as in Visual Studio colorizer) it is important to remember
						// that we are in the singly-quoted string at the end of the line.
						BEGIN(LexicalStates.ST_SINGLE_QUOTES); 
						yymore(); 
						break; 
					}
					break;
					
				case 13:
					// #line 282
					{ return Tokens.ParseDecimalNumber; }
					break;
					
				case 14:
					// #line 280
					{ return Tokens.T_NS_SEPARATOR; }
					break;
					
				case 15:
					// #line 293
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); yymore(); break; }
					break;
					
				case 16:
					// #line 316
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_LBRACE; }
					break;
					
				case 17:
					// #line 372
					{ return Tokens.ERROR; }
					break;
					
				case 18:
					// #line 317
					{ if (!yy_pop_state()) return Tokens.ERROR; return Tokens.T_RBRACE; }
					break;
					
				case 19:
					// #line 263
					{ return Tokens.T_MOD_EQUAL; }
					break;
					
				case 20:
					// #line 319
					{
						if (AllowAspTags) 
						{
							BEGIN(LexicalStates.INITIAL);
							return Tokens.T_CLOSE_TAG;
						} 
						else
						{
							yyless(1);
							return Tokens.T_PERCENT;
						}
					}
					break;
					
				case 21:
					// #line 271
					{ return Tokens.T_SL; }
					break;
					
				case 22:
					// #line 256
					{ return Tokens.T_IS_SMALLER_OR_EQUAL; }
					break;
					
				case 23:
					// #line 255
					{ return Tokens.T_IS_NOT_EQUAL; }
					break;
					
				case 24:
					// #line 229
					{ return Tokens.T_LGENERIC; }
					break;
					
				case 25:
					// #line 125
					{ 
						BEGIN(LexicalStates.INITIAL); 
						return Tokens.T_CLOSE_TAG; 
					}
					break;
					
				case 26:
					// #line 224
					{ return (InLinq) ? Tokens.T_LINQ_IN : Tokens.T_STRING; }
					break;
					
				case 27:
					// #line 139
					{ return Tokens.T_IF; }
					break;
					
				case 28:
					// #line 150
					{ return Tokens.T_AS; }
					break;
					
				case 29:
					// #line 254
					{ return Tokens.T_IS_EQUAL; }
					break;
					
				case 30:
					// #line 249
					{ return Tokens.T_DOUBLE_ARROW; }
					break;
					
				case 31:
					// #line 332
					{ return Tokens.DoubleQuotedString; }
					break;
					
				case 32:
					// #line 333
					{ return Tokens.SingleQuotedString; }
					break;
					
				case 33:
					// #line 257
					{ return Tokens.T_IS_GREATER_OR_EQUAL; }
					break;
					
				case 34:
					// #line 272
					{ return Tokens.T_SR; }
					break;
					
				case 35:
					// #line 261
					{ return Tokens.T_DIV_EQUAL; }
					break;
					
				case 36:
					// #line 294
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); yymore(); break; }
					break;
					
				case 37:
					// #line 296
					{ BEGIN(LexicalStates.ST_COMMENT); yymore(); break; }
					break;
					
				case 38:
					// #line 145
					{ return Tokens.T_DO; }
					break;
					
				case 39:
					// #line 175
					{ return Tokens.T_LOGICAL_OR; }
					break;
					
				case 40:
					// #line 225
					{ return (InLinq) ? Tokens.T_LINQ_BY : Tokens.T_STRING; }
					break;
					
				case 41:
					// #line 284
					{ return Tokens.ParseDouble; }
					break;
					
				case 42:
					// #line 230
					{ return Tokens.T_RGENERIC; }
					break;
					
				case 43:
					// #line 273
					{ return Tokens.T_DOUBLE_COLON; }
					break;
					
				case 44:
					// #line 258
					{ return Tokens.T_PLUS_EQUAL; }
					break;
					
				case 45:
					// #line 250
					{ return Tokens.T_INC; }
					break;
					
				case 46:
					// #line 259
					{ return Tokens.T_MINUS_EQUAL; }
					break;
					
				case 47:
					// #line 275
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 48:
					// #line 251
					{ return Tokens.T_DEC; }
					break;
					
				case 49:
					// #line 260
					{ return Tokens.T_MUL_EQUAL; }
					break;
					
				case 50:
					// #line 262
					{ return Tokens.T_CONCAT_EQUAL; }
					break;
					
				case 51:
					// #line 266
					{ return Tokens.T_AND_EQUAL; }
					break;
					
				case 52:
					// #line 270
					{ return Tokens.T_BOOLEAN_AND; }
					break;
					
				case 53:
					// #line 267
					{ return Tokens.T_OR_EQUAL; }
					break;
					
				case 54:
					// #line 269
					{ return Tokens.T_BOOLEAN_OR; }
					break;
					
				case 55:
					// #line 268
					{ return Tokens.T_XOR_EQUAL; }
					break;
					
				case 56:
					// #line 276
					{ return Tokens.T_VARIABLE; }
					break;
					
				case 57:
					// #line 264
					{ return Tokens.T_SL_EQUAL; }
					break;
					
				case 58:
					// #line 209
					{ return Tokens.T_INT_TYPE; }
					break;
					
				case 59:
					// #line 335
					{ return Tokens.ErrorInvalidIdentifier; }
					break;
					
				case 60:
					// #line 189
					{ return Tokens.T_TRY; }
					break;
					
				case 61:
					// #line 176
					{ return Tokens.T_LOGICAL_AND; }
					break;
					
				case 62:
					// #line 163
					{ return Tokens.T_NEW; }
					break;
					
				case 63:
					// #line 204
					{ return Tokens.T_USE; }
					break;
					
				case 64:
					// #line 252
					{ return Tokens.T_IS_IDENTICAL; }
					break;
					
				case 65:
					// #line 265
					{ return Tokens.T_SR_EQUAL; }
					break;
					
				case 66:
					// #line 135
					{ return Tokens.T_EXIT; }
					break;
					
				case 67:
					// #line 177
					{ return Tokens.T_LOGICAL_XOR; }
					break;
					
				case 68:
					// #line 146
					{ return Tokens.T_FOR; }
					break;
					
				case 69:
					// #line 164
					{ return Tokens.T_VAR; }
					break;
					
				case 70:
					// #line 285
					{ return Tokens.ParseDouble; }
					break;
					
				case 71:
					// #line 253
					{ return Tokens.T_IS_NOT_IDENTICAL; }
					break;
					
				case 72:
					// #line 283
					{ return Tokens.ParseHexadecimalNumber; }
					break;
					
				case 73:
					// #line 286
					{ return Tokens.ParseBinaryNumber; }
					break;
					
				case 74:
					// #line 243
					{ return Tokens.T_SELF; }
					break;
					
				case 75:
					// #line 153
					{ return Tokens.T_CASE; }
					break;
					
				case 76:
					// #line 334
					{ return Tokens.SingleQuotedIdentifier; }
					break;
					
				case 77:
					// #line 245
					{ return Tokens.T_TRUE; }
					break;
					
				case 78:
					// #line 178
					{ return Tokens.T_LIST; }
					break;
					
				case 79:
					// #line 247
					{ return Tokens.T_NULL; }
					break;
					
				case 80:
					// #line 206
					{ return Tokens.T_GOTO; }
					break;
					
				case 81:
					// #line 157
					{ return Tokens.T_ECHO; }
					break;
					
				case 82:
					// #line 142
					{ return Tokens.T_ELSE; }
					break;
					
				case 83:
					// #line 134
					{ return Tokens.T_EXIT; }
					break;
					
				case 84:
					// #line 165
					{ return Tokens.T_EVAL; }
					break;
					
				case 85:
					// #line 295
					{ BEGIN(LexicalStates.ST_DOC_COMMENT); yymore(); break; }
					break;
					
				case 86:
					// #line 217
					{ return Tokens.T_LINQ_FROM; }
					break;
					
				case 87:
					// #line 208
					{ return Tokens.T_BOOL_TYPE; }
					break;
					
				case 88:
					// #line 359
					{
						bool is_binary = GetTokenChar(0) != '<';
						hereDocLabel = GetTokenSubstring(is_binary ? 4 : 3).Trim();
						var newstate = LexicalStates.ST_HEREDOC;
						if (hereDocLabel[0] == '"' || hereDocLabel[0] == '\'')
						{
							if (hereDocLabel[0] == '\'') newstate = LexicalStates.ST_NEWDOC;	// newdoc syntax, continue in ST_NEWDOC lexical state
							hereDocLabel = hereDocLabel.Substring(1, hereDocLabel.Length - 2);	// trim quote characters around
						}
						BEGIN(newstate);
						return is_binary ? Tokens.T_BINARY_HEREDOC : Tokens.T_START_HEREDOC;
					}
					break;
					
				case 89:
					// #line 159
					{ return Tokens.T_CLASS; }
					break;
					
				case 90:
					// #line 194
					{ return Tokens.T_CLONE; }
					break;
					
				case 91:
					// #line 190
					{ return Tokens.T_CATCH; }
					break;
					
				case 92:
					// #line 137
					{ return Tokens.T_CONST; }
					break;
					
				case 93:
					// #line 171
					{ return Tokens.T_ISSET; }
					break;
					
				case 94:
					// #line 210
					{ return Tokens.T_INT64_TYPE; }
					break;
					
				case 95:
					// #line 158
					{ return Tokens.T_PRINT; }
					break;
					
				case 96:
					// #line 160
					{ return Tokens.T_TRAIT; }
					break;
					
				case 97:
					// #line 191
					{ return Tokens.T_THROW; }
					break;
					
				case 98:
					// #line 179
					{ return Tokens.T_ARRAY; }
					break;
					
				case 99:
					// #line 223
					{ return (InLinq) ? Tokens.T_LINQ_GROUP : Tokens.T_STRING; }
					break;
					
				case 100:
					// #line 174
					{ return Tokens.T_UNSET; }
					break;
					
				case 101:
					// #line 141
					{ return Tokens.T_ENDIF; }
					break;
					
				case 102:
					// #line 172
					{ return Tokens.T_EMPTY; }
					break;
					
				case 103:
					// #line 196
					{ return Tokens.T_FINAL; }
					break;
					
				case 104:
					// #line 246
					{ return Tokens.T_FALSE; }
					break;
					
				case 105:
					// #line 143
					{ return Tokens.T_WHILE; }
					break;
					
				case 106:
					// #line 218
					{ return (InLinq) ? Tokens.T_LINQ_WHERE : Tokens.T_STRING; }
					break;
					
				case 107:
					// #line 155
					{ return Tokens.T_BREAK; }
					break;
					
				case 108:
					// #line 234
					{ return Tokens.T_SET; }
					break;
					
				case 109:
					// #line 233
					{ return Tokens.T_GET; }
					break;
					
				case 110:
					// #line 300
					{ return Tokens.T_INT32_CAST; }
					break;
					
				case 111:
					// #line 212
					{ return Tokens.T_STRING_TYPE; }
					break;
					
				case 112:
					// #line 173
					{ return Tokens.T_STATIC; }
					break;
					
				case 113:
					// #line 222
					{ return (InLinq) ? Tokens.T_LINQ_SELECT : Tokens.T_STRING; }
					break;
					
				case 114:
					// #line 151
					{ return Tokens.T_SWITCH; }
					break;
					
				case 115:
					// #line 138
					{ return Tokens.T_RETURN; }
					break;
					
				case 116:
					// #line 205
					{ return Tokens.T_IMPORT; }
					break;
					
				case 117:
					// #line 242
					{ return Tokens.T_PARENT; }
					break;
					
				case 118:
					// #line 199
					{ return Tokens.T_PUBLIC; }
					break;
					
				case 119:
					// #line 232
					{ return Tokens.T_ASSERT; }
					break;
					
				case 120:
					// #line 170
					{ return Tokens.T_GLOBAL; }
					break;
					
				case 121:
					// #line 140
					{ return Tokens.T_ELSEIF; }
					break;
					
				case 122:
					// #line 147
					{ return Tokens.T_ENDFOR; }
					break;
					
				case 123:
					// #line 211
					{ return Tokens.T_DOUBLE_TYPE; }
					break;
					
				case 124:
					// #line 214
					{ return Tokens.T_OBJECT_TYPE; }
					break;
					
				case 125:
					// #line 235
					{ return Tokens.T_CALL; }
					break;
					
				case 126:
					// #line 306
					{ return Tokens.T_DOUBLE_CAST; }
					break;
					
				case 127:
					// #line 298
					{ return Tokens.T_INT8_CAST; }
					break;
					
				case 128:
					// #line 304
					{ return Tokens.T_UINT32_CAST; }
					break;
					
				case 129:
					// #line 313
					{ return Tokens.T_BOOL_CAST; }
					break;
					
				case 130:
					// #line 168
					{ return Tokens.T_REQUIRE; }
					break;
					
				case 131:
					// #line 166
					{ return Tokens.T_INCLUDE; }
					break;
					
				case 132:
					// #line 197
					{ return Tokens.T_PRIVATE; }
					break;
					
				case 133:
					// #line 227
					{ return Tokens.T_PARTIAL; }
					break;
					
				case 134:
					// #line 162
					{ return Tokens.T_EXTENDS; }
					break;
					
				case 135:
					// #line 130
					{
					  return Tokens.ErrorNotSupported; 
					}
					break;
					
				case 136:
					// #line 154
					{ return Tokens.T_DEFAULT; }
					break;
					
				case 137:
					// #line 148
					{ return Tokens.T_FOREACH; }
					break;
					
				case 138:
					// #line 219
					{ return (InLinq) ? Tokens.T_LINQ_ORDERBY : Tokens.T_STRING; }
					break;
					
				case 139:
					// #line 241
					{ return Tokens.T_SLEEP; }
					break;
					
				case 140:
					// #line 187
					{ return Tokens.T_DIR; }
					break;
					
				case 141:
					// #line 301
					{ return Tokens.T_INT64_CAST; }
					break;
					
				case 142:
					// #line 299
					{ return Tokens.T_INT16_CAST; }
					break;
					
				case 143:
					// #line 311
					{ return Tokens.T_ARRAY_CAST; }
					break;
					
				case 144:
					// #line 302
					{ return Tokens.T_UINT8_CAST; }
					break;
					
				case 145:
					// #line 314
					{ return Tokens.T_UNSET_CAST; }
					break;
					
				case 146:
					// #line 307
					{ return Tokens.T_FLOAT_CAST; }
					break;
					
				case 147:
					// #line 180
					{ return Tokens.T_CALLABLE; }
					break;
					
				case 148:
					// #line 156
					{ return Tokens.T_CONTINUE; }
					break;
					
				case 149:
					// #line 213
					{ return Tokens.T_RESOURCE_TYPE; }
					break;
					
				case 150:
					// #line 195
					{ return Tokens.T_ABSTRACT; }
					break;
					
				case 151:
					// #line 144
					{ return Tokens.T_ENDWHILE; }
					break;
					
				case 152:
					// #line 136
					{ return Tokens.T_FUNCTION; }
					break;
					
				case 153:
					// #line 185
					{ return Tokens.T_LINE; }
					break;
					
				case 154:
					// #line 186
					{ return Tokens.T_FILE; }
					break;
					
				case 155:
					// #line 240
					{ return Tokens.T_WAKEUP; }
					break;
					
				case 156:
					// #line 308
					{ return Tokens.T_STRING_CAST; }
					break;
					
				case 157:
					// #line 305
					{ return Tokens.T_UINT64_CAST; }
					break;
					
				case 158:
					// #line 303
					{ return Tokens.T_UINT16_CAST; }
					break;
					
				case 159:
					// #line 312
					{ return Tokens.T_OBJECT_CAST; }
					break;
					
				case 160:
					// #line 309
					{ return Tokens.T_BINARY_CAST; }
					break;
					
				case 161:
					// #line 215
					{ return Tokens.T_TYPEOF; }
					break;
					
				case 162:
					// #line 161
					{ return Tokens.T_INSTEADOF; }
					break;
					
				case 163:
					// #line 192
					{ return Tokens.T_INTERFACE; }
					break;
					
				case 164:
					// #line 198
					{ return Tokens.T_PROTECTED; }
					break;
					
				case 165:
					// #line 221
					{ return (InLinq) ? Tokens.T_LINQ_ASCENDING : Tokens.T_STRING; }
					break;
					
				case 166:
					// #line 203
					{ return Tokens.T_NAMESPACE; }
					break;
					
				case 167:
					// #line 152
					{ return Tokens.T_ENDSWITCH; }
					break;
					
				case 168:
					// #line 181
					{ return Tokens.T_CLASS_C; }
					break;
					
				case 169:
					// #line 182
					{ return Tokens.T_TRAIT_C; }
					break;
					
				case 170:
					// #line 310
					{ return Tokens.T_UNICODE_CAST; }
					break;
					
				case 171:
					// #line 200
					{ return Tokens.T_INSTANCEOF; }
					break;
					
				case 172:
					// #line 193
					{ return Tokens.T_IMPLEMENTS; }
					break;
					
				case 173:
					// #line 149
					{ return Tokens.T_ENDFOREACH; }
					break;
					
				case 174:
					// #line 220
					{ return (InLinq) ? Tokens.T_LINQ_DESCENDING : Tokens.T_STRING; }
					break;
					
				case 175:
					// #line 237
					{ return Tokens.T_TOSTRING; }
					break;
					
				case 176:
					// #line 244
					{ return Tokens.T_AUTOLOAD; }
					break;
					
				case 177:
					// #line 239
					{ return Tokens.T_DESTRUCT; }
					break;
					
				case 178:
					// #line 184
					{ return Tokens.T_METHOD_C; }
					break;
					
				case 179:
					// #line 238
					{ return Tokens.T_CONSTRUCT; }
					break;
					
				case 180:
					// #line 169
					{ return Tokens.T_REQUIRE_ONCE; }
					break;
					
				case 181:
					// #line 167
					{ return Tokens.T_INCLUDE_ONCE; }
					break;
					
				case 182:
					// #line 236
					{ return Tokens.T_CALLSTATIC; }
					break;
					
				case 183:
					// #line 183
					{ return Tokens.T_FUNC_C; }
					break;
					
				case 184:
					// #line 202
					{ return Tokens.T_NAMESPACE_C; }
					break;
					
				case 185:
					// #line 289
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_FILE; }
					break;
					
				case 186:
					// #line 288
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_LINE; }
					break;
					
				case 187:
					// #line 290
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_DEFAULT_LINE; }
					break;
					
				case 188:
					// #line 291
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_DEFAULT_FILE; }
					break;
					
				case 189:
					// #line 498
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 190:
					// #line 490
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 191:
					// #line 481
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 192:
					// #line 491
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_DOUBLE_QUOTES; }
					break;
					
				case 193:
					// #line 480
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 194:
					// #line 497
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 195:
					// #line 499
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 196:
					// #line 495
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 197:
					// #line 494
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 198:
					// #line 492
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 199:
					// #line 493
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 200:
					// #line 489
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 201:
					// #line 485
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 202:
					// #line 487
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 203:
					// #line 484
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 204:
					// #line 486
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 205:
					// #line 482
					{ return Tokens.OctalCharCode; }
					break;
					
				case 206:
					// #line 488
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 207:
					// #line 496
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 208:
					// #line 483
					{ return Tokens.HexCharCode; }
					break;
					
				case 209:
					// #line 440
					{ yymore(); break; }
					break;
					
				case 210:
					// #line 441
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.SingleQuotedString; }
					break;
					
				case 211:
					// #line 521
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 212:
					// #line 514
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_BACKQUOTE; }
					break;
					
				case 213:
					// #line 504
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 214:
					// #line 513
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 215:
					// #line 503
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 216:
					// #line 519
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 217:
					// #line 522
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 218:
					// #line 518
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 219:
					// #line 517
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 220:
					// #line 515
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 221:
					// #line 516
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 222:
					// #line 512
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 223:
					// #line 509
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 224:
					// #line 508
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 225:
					// #line 510
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 226:
					// #line 507
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 227:
					// #line 505
					{ return Tokens.OctalCharCode; }
					break;
					
				case 228:
					// #line 511
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 229:
					// #line 520
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 230:
					// #line 506
					{ return Tokens.HexCharCode; }
					break;
					
				case 231:
					// #line 476
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 232:
					// #line 469
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 233:
					// #line 461
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 234:
					// #line 460
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 235:
					// #line 474
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 236:
					// #line 477
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 237:
					// #line 473
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 238:
					// #line 472
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 239:
					// #line 470
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 240:
					// #line 471
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 241:
					// #line 468
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 242:
					// #line 465
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 243:
					// #line 466
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 244:
					// #line 464
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 245:
					// #line 462
					{ return Tokens.OctalCharCode; }
					break;
					
				case 246:
					// #line 467
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 247:
					// #line 475
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 248:
					// #line 463
					{ return Tokens.HexCharCode; }
					break;
					
				case 249:
					// #line 445
					{
						if (IsCurrentHeredocEnd(0))
						{
						  yyless(hereDocLabel.Length);
						  hereDocLabel = null;
						  BEGIN(LexicalStates.ST_IN_SCRIPTING);
							return Tokens.T_END_HEREDOC;
						}
						else 
						{
							inString = true;
							return Tokens.T_STRING;
						}
					}
					break;
					
				case 250:
					// #line 385
					{
						yyless(0);
						if (!yy_pop_state()) return Tokens.ERROR;
						break;
					}
					break;
					
				case 251:
					// #line 378
					{
						if (!yy_pop_state()) return Tokens.ERROR;
						inString = (CurrentLexicalState != LexicalStates.ST_IN_SCRIPTING); 
						isCode = true;
						return Tokens.T_STRING;
					}
					break;
					
				case 252:
					// #line 399
					{
						yyless(0);
						if (!yy_pop_state()) return Tokens.ERROR;
						yy_push_state(LexicalStates.ST_IN_SCRIPTING);
						break;
					}
					break;
					
				case 253:
					// #line 393
					{
						if (!yy_pop_state()) return Tokens.ERROR;
						yy_push_state(LexicalStates.ST_IN_SCRIPTING);
						return Tokens.T_STRING_VARNAME;
					}
					break;
					
				case 254:
					// #line 434
					{ yymore(); break; }
					break;
					
				case 255:
					// #line 436
					{ yymore(); break; }
					break;
					
				case 256:
					// #line 435
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_DOC_COMMENT; }
					break;
					
				case 257:
					// #line 428
					{ yymore(); break; }
					break;
					
				case 258:
					// #line 430
					{ yymore(); break; }
					break;
					
				case 259:
					// #line 429
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_COMMENT; }
					break;
					
				case 260:
					// #line 408
					{ yymore(); break; }
					break;
					
				case 261:
					// #line 409
					{ yymore(); break; }
					break;
					
				case 262:
					// #line 410
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_LINE_COMMENT; }
					break;
					
				case 263:
					// #line 412
					{ 
					  if (AllowAspTags || GetTokenChar(TokenLength - 2) != '%') 
					  { 
							yyless(0);
							BEGIN(LexicalStates.ST_IN_SCRIPTING);
							return Tokens.T_LINE_COMMENT;
						} 
						else 
						{
							yymore();
							break;
						}
					}
					break;
					
				case 266: goto case 2;
				case 267: goto case 4;
				case 268: goto case 6;
				case 269: goto case 7;
				case 270: goto case 9;
				case 271: goto case 13;
				case 272: goto case 20;
				case 273: goto case 23;
				case 274: goto case 25;
				case 275: goto case 88;
				case 276: goto case 186;
				case 277: goto case 189;
				case 278: goto case 193;
				case 279: goto case 194;
				case 280: goto case 195;
				case 281: goto case 200;
				case 282: goto case 201;
				case 283: goto case 203;
				case 284: goto case 205;
				case 285: goto case 208;
				case 286: goto case 211;
				case 287: goto case 215;
				case 288: goto case 216;
				case 289: goto case 217;
				case 290: goto case 222;
				case 291: goto case 224;
				case 292: goto case 226;
				case 293: goto case 227;
				case 294: goto case 230;
				case 295: goto case 231;
				case 296: goto case 232;
				case 297: goto case 234;
				case 298: goto case 235;
				case 299: goto case 236;
				case 300: goto case 241;
				case 301: goto case 242;
				case 302: goto case 244;
				case 303: goto case 245;
				case 304: goto case 248;
				case 305: goto case 249;
				case 306: goto case 260;
				case 307: goto case 262;
				case 309: goto case 2;
				case 310: goto case 7;
				case 311: goto case 9;
				case 312: goto case 20;
				case 313: goto case 25;
				case 314: goto case 193;
				case 315: goto case 194;
				case 316: goto case 215;
				case 317: goto case 216;
				case 318: goto case 234;
				case 319: goto case 235;
				case 321: goto case 7;
				case 322: goto case 9;
				case 324: goto case 7;
				case 325: goto case 9;
				case 327: goto case 7;
				case 328: goto case 9;
				case 330: goto case 7;
				case 331: goto case 9;
				case 333: goto case 7;
				case 334: goto case 9;
				case 336: goto case 7;
				case 337: goto case 9;
				case 339: goto case 7;
				case 340: goto case 9;
				case 342: goto case 7;
				case 343: goto case 9;
				case 345: goto case 7;
				case 346: goto case 9;
				case 348: goto case 7;
				case 349: goto case 9;
				case 351: goto case 7;
				case 352: goto case 9;
				case 354: goto case 7;
				case 355: goto case 9;
				case 357: goto case 7;
				case 358: goto case 9;
				case 360: goto case 7;
				case 361: goto case 9;
				case 363: goto case 7;
				case 364: goto case 9;
				case 366: goto case 9;
				case 368: goto case 9;
				case 370: goto case 9;
				case 372: goto case 9;
				case 374: goto case 9;
				case 376: goto case 9;
				case 378: goto case 9;
				case 380: goto case 9;
				case 382: goto case 9;
				case 384: goto case 9;
				case 386: goto case 9;
				case 388: goto case 9;
				case 390: goto case 9;
				case 392: goto case 9;
				case 394: goto case 9;
				case 396: goto case 9;
				case 398: goto case 9;
				case 400: goto case 9;
				case 402: goto case 9;
				case 404: goto case 9;
				case 406: goto case 9;
				case 408: goto case 9;
				case 410: goto case 9;
				case 412: goto case 9;
				case 414: goto case 9;
				case 416: goto case 9;
				case 418: goto case 9;
				case 420: goto case 9;
				case 422: goto case 9;
				case 424: goto case 9;
				case 426: goto case 9;
				case 428: goto case 9;
				case 430: goto case 9;
				case 432: goto case 9;
				case 434: goto case 9;
				case 436: goto case 9;
				case 438: goto case 9;
				case 440: goto case 9;
				case 442: goto case 9;
				case 444: goto case 9;
				case 446: goto case 9;
				case 448: goto case 9;
				case 450: goto case 9;
				case 452: goto case 9;
				case 454: goto case 9;
				case 456: goto case 9;
				case 458: goto case 9;
				case 460: goto case 9;
				case 462: goto case 9;
				case 464: goto case 9;
				case 466: goto case 9;
				case 468: goto case 9;
				case 470: goto case 9;
				case 472: goto case 9;
				case 474: goto case 9;
				case 476: goto case 9;
				case 478: goto case 9;
				case 480: goto case 9;
				case 482: goto case 9;
				case 484: goto case 9;
				case 486: goto case 9;
				case 488: goto case 9;
				case 490: goto case 9;
				case 492: goto case 9;
				case 494: goto case 9;
				case 496: goto case 9;
				case 498: goto case 9;
				case 500: goto case 9;
				case 502: goto case 9;
				case 504: goto case 9;
				case 506: goto case 9;
				case 508: goto case 9;
				case 510: goto case 9;
				case 512: goto case 9;
				case 514: goto case 9;
				case 516: goto case 9;
				case 518: goto case 9;
				case 520: goto case 9;
				case 522: goto case 9;
				case 524: goto case 9;
				case 526: goto case 9;
				case 528: goto case 9;
				case 530: goto case 9;
				case 532: goto case 9;
				case 534: goto case 9;
				case 536: goto case 9;
				case 538: goto case 9;
				case 603: goto case 9;
				case 604: goto case 203;
				case 605: goto case 205;
				case 606: goto case 226;
				case 607: goto case 227;
				case 608: goto case 244;
				case 609: goto case 245;
				case 631: goto case 9;
				case 633: goto case 9;
				case 634: goto case 9;
				case 635: goto case 9;
				case 636: goto case 9;
				case 637: goto case 9;
				case 638: goto case 9;
				case 639: goto case 9;
				case 640: goto case 9;
				case 641: goto case 9;
				case 642: goto case 9;
				case 643: goto case 9;
				case 644: goto case 9;
				case 645: goto case 9;
				case 646: goto case 9;
				case 647: goto case 9;
				case 648: goto case 9;
				case 649: goto case 9;
				case 650: goto case 9;
				case 651: goto case 9;
				case 652: goto case 9;
				case 653: goto case 9;
				case 654: goto case 9;
				case 655: goto case 9;
				case 656: goto case 9;
				case 657: goto case 9;
				case 658: goto case 9;
				case 659: goto case 9;
				case 660: goto case 9;
				case 661: goto case 9;
				case 662: goto case 9;
				case 663: goto case 9;
				case 664: goto case 9;
				case 665: goto case 9;
				case 666: goto case 9;
				case 667: goto case 9;
				case 668: goto case 9;
				case 669: goto case 9;
				case 670: goto case 9;
				case 671: goto case 9;
				case 672: goto case 9;
				case 673: goto case 9;
				case 674: goto case 9;
				case 675: goto case 9;
				case 676: goto case 9;
				case 677: goto case 9;
				case 678: goto case 9;
				case 679: goto case 9;
				case 680: goto case 9;
				case 681: goto case 9;
				case 682: goto case 9;
				case 683: goto case 9;
				case 684: goto case 9;
				case 685: goto case 9;
				case 686: goto case 9;
				case 687: goto case 9;
				case 688: goto case 9;
				case 689: goto case 9;
				case 690: goto case 9;
				case 691: goto case 9;
				case 692: goto case 9;
				case 693: goto case 9;
				case 694: goto case 9;
				case 695: goto case 9;
				case 696: goto case 9;
				case 697: goto case 9;
				case 698: goto case 9;
				case 699: goto case 9;
				case 700: goto case 9;
				case 701: goto case 9;
				case 702: goto case 9;
				case 703: goto case 9;
				case 704: goto case 9;
				case 705: goto case 9;
				case 706: goto case 9;
				case 707: goto case 9;
				case 708: goto case 9;
				case 709: goto case 9;
				case 710: goto case 9;
				case 711: goto case 9;
				case 712: goto case 9;
				case 713: goto case 9;
				case 714: goto case 9;
				case 715: goto case 9;
				case 716: goto case 9;
				case 717: goto case 9;
				case 718: goto case 9;
				case 719: goto case 9;
				case 720: goto case 9;
				case 721: goto case 9;
				case 722: goto case 9;
				case 723: goto case 9;
				case 724: goto case 9;
				case 725: goto case 9;
				case 726: goto case 9;
				case 727: goto case 9;
				case 728: goto case 9;
				case 729: goto case 9;
				case 730: goto case 9;
				case 731: goto case 9;
				case 732: goto case 9;
				case 733: goto case 9;
				case 734: goto case 9;
				case 735: goto case 9;
				case 736: goto case 9;
				case 737: goto case 9;
				case 738: goto case 9;
				case 739: goto case 9;
				case 740: goto case 9;
				case 741: goto case 9;
				case 742: goto case 9;
				case 743: goto case 9;
				case 744: goto case 9;
				case 745: goto case 9;
				case 746: goto case 9;
				case 747: goto case 9;
				case 748: goto case 9;
				case 749: goto case 9;
				case 750: goto case 9;
				case 751: goto case 9;
				case 752: goto case 9;
				case 753: goto case 9;
				case 754: goto case 9;
				case 755: goto case 9;
				case 756: goto case 9;
				case 757: goto case 9;
				case 758: goto case 9;
				case 759: goto case 9;
				case 760: goto case 9;
				case 761: goto case 9;
				case 762: goto case 9;
				case 763: goto case 9;
				case 764: goto case 9;
				case 765: goto case 9;
				case 766: goto case 9;
				case 767: goto case 9;
				case 768: goto case 9;
				case 769: goto case 9;
				case 770: goto case 9;
				case 771: goto case 9;
				case 772: goto case 9;
				case 773: goto case 9;
				case 774: goto case 9;
				case 775: goto case 9;
				case 776: goto case 9;
				case 777: goto case 9;
				case 778: goto case 9;
				case 779: goto case 9;
				case 780: goto case 9;
				case 781: goto case 9;
				case 782: goto case 9;
				case 783: goto case 9;
				case 784: goto case 9;
				case 785: goto case 9;
				case 786: goto case 9;
				case 787: goto case 9;
				case 788: goto case 9;
				case 789: goto case 9;
				case 790: goto case 9;
				case 791: goto case 9;
				case 792: goto case 9;
				case 793: goto case 9;
				case 794: goto case 9;
				case 795: goto case 9;
				case 796: goto case 9;
				case 797: goto case 9;
				case 798: goto case 9;
				case 799: goto case 9;
				case 800: goto case 9;
				case 801: goto case 9;
				case 802: goto case 9;
				case 803: goto case 203;
				case 804: goto case 226;
				case 805: goto case 244;
				case 808: goto case 9;
				case 809: goto case 9;
				case 810: goto case 9;
				case 811: goto case 9;
				case 812: goto case 9;
				case 813: goto case 9;
				case 814: goto case 9;
				case 815: goto case 9;
				case 816: goto case 9;
				case 817: goto case 9;
				case 818: goto case 9;
				case 819: goto case 9;
				case 820: goto case 9;
				case 821: goto case 9;
				case 822: goto case 9;
				case 823: goto case 9;
				case 824: goto case 9;
				case 825: goto case 9;
				case 826: goto case 9;
				case 827: goto case 9;
				case 828: goto case 9;
				case 829: goto case 9;
				case 830: goto case 9;
				case 831: goto case 9;
				case 832: goto case 9;
				case 833: goto case 9;
				case 834: goto case 9;
				case 835: goto case 9;
				case 836: goto case 9;
				case 837: goto case 9;
				case 838: goto case 9;
				case 839: goto case 9;
				case 840: goto case 9;
				case 841: goto case 9;
				case 842: goto case 9;
				case 843: goto case 9;
				case 844: goto case 9;
				case 845: goto case 9;
				case 846: goto case 9;
				case 847: goto case 9;
				case 848: goto case 9;
				case 849: goto case 9;
				case 850: goto case 9;
				case 851: goto case 9;
				case 852: goto case 9;
				case 853: goto case 9;
				case 854: goto case 9;
				case 855: goto case 9;
				case 856: goto case 9;
				case 857: goto case 9;
				case 858: goto case 9;
				case 859: goto case 9;
				case 860: goto case 9;
				case 861: goto case 9;
				case 862: goto case 9;
				case 863: goto case 9;
				case 864: goto case 9;
				case 865: goto case 9;
				case 866: goto case 9;
				case 867: goto case 9;
				case 868: goto case 9;
				case 869: goto case 9;
				case 870: goto case 9;
				case 871: goto case 9;
				case 872: goto case 9;
				case 873: goto case 9;
				case 874: goto case 9;
				case 875: goto case 9;
				case 876: goto case 9;
				case 877: goto case 9;
				case 878: goto case 9;
				case 879: goto case 9;
				case 880: goto case 9;
				case 881: goto case 9;
				case 882: goto case 9;
				case 883: goto case 9;
				case 884: goto case 9;
				case 885: goto case 9;
				case 886: goto case 9;
				case 887: goto case 9;
				case 888: goto case 9;
				case 889: goto case 9;
				case 890: goto case 9;
				case 891: goto case 9;
				case 892: goto case 9;
				case 893: goto case 9;
				case 894: goto case 9;
				case 895: goto case 9;
				case 896: goto case 9;
				case 897: goto case 9;
				case 898: goto case 9;
				case 899: goto case 9;
				case 900: goto case 9;
				case 901: goto case 9;
				case 902: goto case 9;
				case 903: goto case 9;
				case 904: goto case 9;
				case 905: goto case 9;
				case 906: goto case 9;
				case 907: goto case 203;
				case 908: goto case 226;
				case 909: goto case 244;
				case 911: goto case 9;
				case 912: goto case 9;
				case 913: goto case 9;
				case 914: goto case 9;
				case 915: goto case 9;
				case 916: goto case 9;
				case 917: goto case 9;
				case 918: goto case 9;
				case 919: goto case 203;
				case 920: goto case 226;
				case 921: goto case 244;
				case 922: goto case 203;
				case 923: goto case 226;
				case 924: goto case 244;
			}
			accepted = false;
			return yyreturn;
		}
		
		#pragma warning restore 162
		
		
		#endregion
		private void BEGIN(LexicalStates state)
		{
			current_lexical_state = state;
		}
		
		private char Advance()
		{
			if (lookahead_index >= chars_read)
			{
				if (token_start > 0)
				{
					// shift buffer left:
					int length = chars_read - token_start;
					System.Buffer.BlockCopy(buffer, token_start << 1, buffer, 0, length << 1);
					token_end -= token_start;
					token_chunk_start -= token_start;
					token_start = 0;
					chars_read = lookahead_index = length;
					
					// populate the remaining bytes:
					int count = reader.Read(buffer, chars_read, buffer.Length - chars_read);
					if (count <= 0) return EOF;
					
					chars_read += count;
				}
				
				while (lookahead_index >= chars_read)
				{
					if (lookahead_index >= buffer.Length)
						buffer = ResizeBuffer(buffer);
					
					int count = reader.Read(buffer, chars_read, buffer.Length - chars_read);
					if (count <= 0) return EOF;
					chars_read += count;
				}
			}
			
			return Map(buffer[lookahead_index++]);
		}
		
		private char[] ResizeBuffer(char[] buf)
		{
			char[] result = new char[buf.Length << 1];
			System.Buffer.BlockCopy(buf, 0, result, 0, buf.Length << 1);
			return result;
		}
		
		
		private void AdvanceEndPosition(int from, int to)
		{
			int last_eoln = from - token_end_pos.Column;
		
		    for (int i = from; i < to; i++)
			{
		        char ch = buffer[i];
		        
		        // Line endings supported by Visual Studio:
		
		        // CRLF:  Windows, U+000D + U+000A
		        // LF:    Unix, U+000A
		        // CR:    Mac, U+000D
		        // LS:    Line Separator, U+2028
		        // PS:    Paragraph Separator, U+2029
		
		        if ((ch == '\n') || // CRLF, LF
		            (ch == '\r' && ((i + 1 < buffer.Length) ? buffer[i + 1] : '\0') != '\n') ||    // CR, not CRLF
		            (ch == (char)0x2028) || 
		            (ch == (char)0x2029))
		        {
		            token_end_pos.Line++;
		            last_eoln = i;
		        }
			}
		
			token_end_pos.Char += to - from;
			token_end_pos.Column = to - last_eoln;
		}
		
		protected static bool IsNewLineCharacter(char ch)
		{
		    return ch == '\r' || ch == '\n' || ch == (char)0x2028 || ch == (char)0x2029;
		}
		private void TrimTokenEnd()
		{
			if (token_end > token_chunk_start && buffer[token_end - 1] == '\n')
				token_end--;
			if (token_end > token_chunk_start && buffer[token_end - 1] == '\r')
				token_end--;
			}
		
		private void MarkTokenChunkStart()
		{
			token_chunk_start = lookahead_index;
		}
		
		private void MarkTokenEnd()
		{
			token_end = lookahead_index;
		}
		
		private void MoveToTokenEnd()
		{
			lookahead_index = token_end;
			yy_at_bol = (token_end > token_chunk_start) && (buffer[token_end - 1] == '\r' || buffer[token_end - 1] == '\n');
		}
		
		public int TokenLength
		{
			get { return token_end - token_start; }
		}
		
		public int TokenChunkLength
		{
			get { return token_end - token_chunk_start; }
		}
		
		private void yymore()
		{
			if (!expanding_token)
			{
				token_start = token_chunk_start;
				expanding_token = true;
			}
		}
		
		private void yyless(int count)
		{
			lookahead_index = token_end = token_chunk_start + count;
		}
		
		private Stack<LexicalStates> stateStack = new Stack<LexicalStates>(20);
		
		private void yy_push_state(LexicalStates state)
		{
			stateStack.Push(current_lexical_state);
			current_lexical_state = state;
		}
		
		private bool yy_pop_state()
		{
			if (stateStack.Count == 0) return false;
			current_lexical_state = stateStack.Pop();
			return true;
		}
		
		private LexicalStates yy_top_state()
		{
			return stateStack.Peek();
		}
		
		#region Tables
		
		private static AcceptConditions[] acceptCondition = new AcceptConditions[]
		{
			AcceptConditions.NotAccept, // 0
			AcceptConditions.Accept, // 1
			AcceptConditions.Accept, // 2
			AcceptConditions.Accept, // 3
			AcceptConditions.Accept, // 4
			AcceptConditions.Accept, // 5
			AcceptConditions.Accept, // 6
			AcceptConditions.Accept, // 7
			AcceptConditions.Accept, // 8
			AcceptConditions.Accept, // 9
			AcceptConditions.Accept, // 10
			AcceptConditions.Accept, // 11
			AcceptConditions.Accept, // 12
			AcceptConditions.Accept, // 13
			AcceptConditions.Accept, // 14
			AcceptConditions.Accept, // 15
			AcceptConditions.Accept, // 16
			AcceptConditions.Accept, // 17
			AcceptConditions.Accept, // 18
			AcceptConditions.Accept, // 19
			AcceptConditions.Accept, // 20
			AcceptConditions.Accept, // 21
			AcceptConditions.Accept, // 22
			AcceptConditions.Accept, // 23
			AcceptConditions.Accept, // 24
			AcceptConditions.Accept, // 25
			AcceptConditions.Accept, // 26
			AcceptConditions.Accept, // 27
			AcceptConditions.Accept, // 28
			AcceptConditions.Accept, // 29
			AcceptConditions.Accept, // 30
			AcceptConditions.Accept, // 31
			AcceptConditions.Accept, // 32
			AcceptConditions.Accept, // 33
			AcceptConditions.Accept, // 34
			AcceptConditions.Accept, // 35
			AcceptConditions.Accept, // 36
			AcceptConditions.Accept, // 37
			AcceptConditions.Accept, // 38
			AcceptConditions.Accept, // 39
			AcceptConditions.Accept, // 40
			AcceptConditions.Accept, // 41
			AcceptConditions.Accept, // 42
			AcceptConditions.Accept, // 43
			AcceptConditions.Accept, // 44
			AcceptConditions.Accept, // 45
			AcceptConditions.Accept, // 46
			AcceptConditions.Accept, // 47
			AcceptConditions.Accept, // 48
			AcceptConditions.Accept, // 49
			AcceptConditions.Accept, // 50
			AcceptConditions.Accept, // 51
			AcceptConditions.Accept, // 52
			AcceptConditions.Accept, // 53
			AcceptConditions.Accept, // 54
			AcceptConditions.Accept, // 55
			AcceptConditions.Accept, // 56
			AcceptConditions.Accept, // 57
			AcceptConditions.Accept, // 58
			AcceptConditions.Accept, // 59
			AcceptConditions.Accept, // 60
			AcceptConditions.Accept, // 61
			AcceptConditions.Accept, // 62
			AcceptConditions.Accept, // 63
			AcceptConditions.Accept, // 64
			AcceptConditions.Accept, // 65
			AcceptConditions.Accept, // 66
			AcceptConditions.Accept, // 67
			AcceptConditions.Accept, // 68
			AcceptConditions.Accept, // 69
			AcceptConditions.Accept, // 70
			AcceptConditions.Accept, // 71
			AcceptConditions.Accept, // 72
			AcceptConditions.Accept, // 73
			AcceptConditions.Accept, // 74
			AcceptConditions.Accept, // 75
			AcceptConditions.Accept, // 76
			AcceptConditions.Accept, // 77
			AcceptConditions.Accept, // 78
			AcceptConditions.Accept, // 79
			AcceptConditions.Accept, // 80
			AcceptConditions.Accept, // 81
			AcceptConditions.Accept, // 82
			AcceptConditions.Accept, // 83
			AcceptConditions.Accept, // 84
			AcceptConditions.Accept, // 85
			AcceptConditions.Accept, // 86
			AcceptConditions.Accept, // 87
			AcceptConditions.Accept, // 88
			AcceptConditions.Accept, // 89
			AcceptConditions.Accept, // 90
			AcceptConditions.Accept, // 91
			AcceptConditions.Accept, // 92
			AcceptConditions.Accept, // 93
			AcceptConditions.Accept, // 94
			AcceptConditions.Accept, // 95
			AcceptConditions.Accept, // 96
			AcceptConditions.Accept, // 97
			AcceptConditions.Accept, // 98
			AcceptConditions.Accept, // 99
			AcceptConditions.Accept, // 100
			AcceptConditions.Accept, // 101
			AcceptConditions.Accept, // 102
			AcceptConditions.Accept, // 103
			AcceptConditions.Accept, // 104
			AcceptConditions.Accept, // 105
			AcceptConditions.Accept, // 106
			AcceptConditions.Accept, // 107
			AcceptConditions.Accept, // 108
			AcceptConditions.Accept, // 109
			AcceptConditions.Accept, // 110
			AcceptConditions.Accept, // 111
			AcceptConditions.Accept, // 112
			AcceptConditions.Accept, // 113
			AcceptConditions.Accept, // 114
			AcceptConditions.Accept, // 115
			AcceptConditions.Accept, // 116
			AcceptConditions.Accept, // 117
			AcceptConditions.Accept, // 118
			AcceptConditions.Accept, // 119
			AcceptConditions.Accept, // 120
			AcceptConditions.Accept, // 121
			AcceptConditions.Accept, // 122
			AcceptConditions.Accept, // 123
			AcceptConditions.Accept, // 124
			AcceptConditions.Accept, // 125
			AcceptConditions.Accept, // 126
			AcceptConditions.Accept, // 127
			AcceptConditions.Accept, // 128
			AcceptConditions.Accept, // 129
			AcceptConditions.Accept, // 130
			AcceptConditions.Accept, // 131
			AcceptConditions.Accept, // 132
			AcceptConditions.Accept, // 133
			AcceptConditions.Accept, // 134
			AcceptConditions.Accept, // 135
			AcceptConditions.Accept, // 136
			AcceptConditions.Accept, // 137
			AcceptConditions.Accept, // 138
			AcceptConditions.Accept, // 139
			AcceptConditions.Accept, // 140
			AcceptConditions.Accept, // 141
			AcceptConditions.Accept, // 142
			AcceptConditions.Accept, // 143
			AcceptConditions.Accept, // 144
			AcceptConditions.Accept, // 145
			AcceptConditions.Accept, // 146
			AcceptConditions.Accept, // 147
			AcceptConditions.Accept, // 148
			AcceptConditions.Accept, // 149
			AcceptConditions.Accept, // 150
			AcceptConditions.Accept, // 151
			AcceptConditions.Accept, // 152
			AcceptConditions.Accept, // 153
			AcceptConditions.Accept, // 154
			AcceptConditions.Accept, // 155
			AcceptConditions.Accept, // 156
			AcceptConditions.Accept, // 157
			AcceptConditions.Accept, // 158
			AcceptConditions.Accept, // 159
			AcceptConditions.Accept, // 160
			AcceptConditions.Accept, // 161
			AcceptConditions.Accept, // 162
			AcceptConditions.Accept, // 163
			AcceptConditions.Accept, // 164
			AcceptConditions.Accept, // 165
			AcceptConditions.Accept, // 166
			AcceptConditions.Accept, // 167
			AcceptConditions.Accept, // 168
			AcceptConditions.Accept, // 169
			AcceptConditions.Accept, // 170
			AcceptConditions.Accept, // 171
			AcceptConditions.Accept, // 172
			AcceptConditions.Accept, // 173
			AcceptConditions.Accept, // 174
			AcceptConditions.Accept, // 175
			AcceptConditions.Accept, // 176
			AcceptConditions.Accept, // 177
			AcceptConditions.Accept, // 178
			AcceptConditions.Accept, // 179
			AcceptConditions.Accept, // 180
			AcceptConditions.Accept, // 181
			AcceptConditions.Accept, // 182
			AcceptConditions.Accept, // 183
			AcceptConditions.Accept, // 184
			AcceptConditions.Accept, // 185
			AcceptConditions.Accept, // 186
			AcceptConditions.Accept, // 187
			AcceptConditions.Accept, // 188
			AcceptConditions.Accept, // 189
			AcceptConditions.Accept, // 190
			AcceptConditions.Accept, // 191
			AcceptConditions.Accept, // 192
			AcceptConditions.Accept, // 193
			AcceptConditions.Accept, // 194
			AcceptConditions.Accept, // 195
			AcceptConditions.Accept, // 196
			AcceptConditions.Accept, // 197
			AcceptConditions.Accept, // 198
			AcceptConditions.Accept, // 199
			AcceptConditions.Accept, // 200
			AcceptConditions.Accept, // 201
			AcceptConditions.Accept, // 202
			AcceptConditions.Accept, // 203
			AcceptConditions.Accept, // 204
			AcceptConditions.Accept, // 205
			AcceptConditions.Accept, // 206
			AcceptConditions.Accept, // 207
			AcceptConditions.Accept, // 208
			AcceptConditions.Accept, // 209
			AcceptConditions.Accept, // 210
			AcceptConditions.Accept, // 211
			AcceptConditions.Accept, // 212
			AcceptConditions.Accept, // 213
			AcceptConditions.Accept, // 214
			AcceptConditions.Accept, // 215
			AcceptConditions.Accept, // 216
			AcceptConditions.Accept, // 217
			AcceptConditions.Accept, // 218
			AcceptConditions.Accept, // 219
			AcceptConditions.Accept, // 220
			AcceptConditions.Accept, // 221
			AcceptConditions.Accept, // 222
			AcceptConditions.Accept, // 223
			AcceptConditions.Accept, // 224
			AcceptConditions.Accept, // 225
			AcceptConditions.Accept, // 226
			AcceptConditions.Accept, // 227
			AcceptConditions.Accept, // 228
			AcceptConditions.Accept, // 229
			AcceptConditions.Accept, // 230
			AcceptConditions.Accept, // 231
			AcceptConditions.Accept, // 232
			AcceptConditions.Accept, // 233
			AcceptConditions.Accept, // 234
			AcceptConditions.Accept, // 235
			AcceptConditions.Accept, // 236
			AcceptConditions.Accept, // 237
			AcceptConditions.Accept, // 238
			AcceptConditions.Accept, // 239
			AcceptConditions.Accept, // 240
			AcceptConditions.Accept, // 241
			AcceptConditions.Accept, // 242
			AcceptConditions.Accept, // 243
			AcceptConditions.Accept, // 244
			AcceptConditions.Accept, // 245
			AcceptConditions.Accept, // 246
			AcceptConditions.Accept, // 247
			AcceptConditions.Accept, // 248
			AcceptConditions.AcceptOnStart, // 249
			AcceptConditions.Accept, // 250
			AcceptConditions.Accept, // 251
			AcceptConditions.Accept, // 252
			AcceptConditions.Accept, // 253
			AcceptConditions.Accept, // 254
			AcceptConditions.Accept, // 255
			AcceptConditions.Accept, // 256
			AcceptConditions.Accept, // 257
			AcceptConditions.Accept, // 258
			AcceptConditions.Accept, // 259
			AcceptConditions.Accept, // 260
			AcceptConditions.Accept, // 261
			AcceptConditions.Accept, // 262
			AcceptConditions.Accept, // 263
			AcceptConditions.NotAccept, // 264
			AcceptConditions.Accept, // 265
			AcceptConditions.Accept, // 266
			AcceptConditions.Accept, // 267
			AcceptConditions.Accept, // 268
			AcceptConditions.Accept, // 269
			AcceptConditions.Accept, // 270
			AcceptConditions.Accept, // 271
			AcceptConditions.Accept, // 272
			AcceptConditions.Accept, // 273
			AcceptConditions.Accept, // 274
			AcceptConditions.Accept, // 275
			AcceptConditions.Accept, // 276
			AcceptConditions.Accept, // 277
			AcceptConditions.Accept, // 278
			AcceptConditions.Accept, // 279
			AcceptConditions.Accept, // 280
			AcceptConditions.Accept, // 281
			AcceptConditions.Accept, // 282
			AcceptConditions.Accept, // 283
			AcceptConditions.Accept, // 284
			AcceptConditions.Accept, // 285
			AcceptConditions.Accept, // 286
			AcceptConditions.Accept, // 287
			AcceptConditions.Accept, // 288
			AcceptConditions.Accept, // 289
			AcceptConditions.Accept, // 290
			AcceptConditions.Accept, // 291
			AcceptConditions.Accept, // 292
			AcceptConditions.Accept, // 293
			AcceptConditions.Accept, // 294
			AcceptConditions.Accept, // 295
			AcceptConditions.Accept, // 296
			AcceptConditions.Accept, // 297
			AcceptConditions.Accept, // 298
			AcceptConditions.Accept, // 299
			AcceptConditions.Accept, // 300
			AcceptConditions.Accept, // 301
			AcceptConditions.Accept, // 302
			AcceptConditions.Accept, // 303
			AcceptConditions.Accept, // 304
			AcceptConditions.AcceptOnStart, // 305
			AcceptConditions.Accept, // 306
			AcceptConditions.Accept, // 307
			AcceptConditions.NotAccept, // 308
			AcceptConditions.Accept, // 309
			AcceptConditions.Accept, // 310
			AcceptConditions.Accept, // 311
			AcceptConditions.Accept, // 312
			AcceptConditions.Accept, // 313
			AcceptConditions.Accept, // 314
			AcceptConditions.Accept, // 315
			AcceptConditions.Accept, // 316
			AcceptConditions.Accept, // 317
			AcceptConditions.Accept, // 318
			AcceptConditions.Accept, // 319
			AcceptConditions.NotAccept, // 320
			AcceptConditions.Accept, // 321
			AcceptConditions.Accept, // 322
			AcceptConditions.NotAccept, // 323
			AcceptConditions.Accept, // 324
			AcceptConditions.Accept, // 325
			AcceptConditions.NotAccept, // 326
			AcceptConditions.Accept, // 327
			AcceptConditions.Accept, // 328
			AcceptConditions.NotAccept, // 329
			AcceptConditions.Accept, // 330
			AcceptConditions.Accept, // 331
			AcceptConditions.NotAccept, // 332
			AcceptConditions.Accept, // 333
			AcceptConditions.Accept, // 334
			AcceptConditions.NotAccept, // 335
			AcceptConditions.Accept, // 336
			AcceptConditions.Accept, // 337
			AcceptConditions.NotAccept, // 338
			AcceptConditions.Accept, // 339
			AcceptConditions.Accept, // 340
			AcceptConditions.NotAccept, // 341
			AcceptConditions.Accept, // 342
			AcceptConditions.Accept, // 343
			AcceptConditions.NotAccept, // 344
			AcceptConditions.Accept, // 345
			AcceptConditions.Accept, // 346
			AcceptConditions.NotAccept, // 347
			AcceptConditions.Accept, // 348
			AcceptConditions.Accept, // 349
			AcceptConditions.NotAccept, // 350
			AcceptConditions.Accept, // 351
			AcceptConditions.Accept, // 352
			AcceptConditions.NotAccept, // 353
			AcceptConditions.Accept, // 354
			AcceptConditions.Accept, // 355
			AcceptConditions.NotAccept, // 356
			AcceptConditions.Accept, // 357
			AcceptConditions.Accept, // 358
			AcceptConditions.NotAccept, // 359
			AcceptConditions.Accept, // 360
			AcceptConditions.Accept, // 361
			AcceptConditions.NotAccept, // 362
			AcceptConditions.Accept, // 363
			AcceptConditions.Accept, // 364
			AcceptConditions.NotAccept, // 365
			AcceptConditions.Accept, // 366
			AcceptConditions.NotAccept, // 367
			AcceptConditions.Accept, // 368
			AcceptConditions.NotAccept, // 369
			AcceptConditions.Accept, // 370
			AcceptConditions.NotAccept, // 371
			AcceptConditions.Accept, // 372
			AcceptConditions.NotAccept, // 373
			AcceptConditions.Accept, // 374
			AcceptConditions.NotAccept, // 375
			AcceptConditions.Accept, // 376
			AcceptConditions.NotAccept, // 377
			AcceptConditions.Accept, // 378
			AcceptConditions.NotAccept, // 379
			AcceptConditions.Accept, // 380
			AcceptConditions.NotAccept, // 381
			AcceptConditions.Accept, // 382
			AcceptConditions.NotAccept, // 383
			AcceptConditions.Accept, // 384
			AcceptConditions.NotAccept, // 385
			AcceptConditions.Accept, // 386
			AcceptConditions.NotAccept, // 387
			AcceptConditions.Accept, // 388
			AcceptConditions.NotAccept, // 389
			AcceptConditions.Accept, // 390
			AcceptConditions.NotAccept, // 391
			AcceptConditions.Accept, // 392
			AcceptConditions.NotAccept, // 393
			AcceptConditions.Accept, // 394
			AcceptConditions.NotAccept, // 395
			AcceptConditions.Accept, // 396
			AcceptConditions.NotAccept, // 397
			AcceptConditions.Accept, // 398
			AcceptConditions.NotAccept, // 399
			AcceptConditions.Accept, // 400
			AcceptConditions.NotAccept, // 401
			AcceptConditions.Accept, // 402
			AcceptConditions.NotAccept, // 403
			AcceptConditions.Accept, // 404
			AcceptConditions.NotAccept, // 405
			AcceptConditions.Accept, // 406
			AcceptConditions.NotAccept, // 407
			AcceptConditions.Accept, // 408
			AcceptConditions.NotAccept, // 409
			AcceptConditions.Accept, // 410
			AcceptConditions.NotAccept, // 411
			AcceptConditions.Accept, // 412
			AcceptConditions.NotAccept, // 413
			AcceptConditions.Accept, // 414
			AcceptConditions.NotAccept, // 415
			AcceptConditions.Accept, // 416
			AcceptConditions.NotAccept, // 417
			AcceptConditions.Accept, // 418
			AcceptConditions.NotAccept, // 419
			AcceptConditions.Accept, // 420
			AcceptConditions.NotAccept, // 421
			AcceptConditions.Accept, // 422
			AcceptConditions.NotAccept, // 423
			AcceptConditions.Accept, // 424
			AcceptConditions.NotAccept, // 425
			AcceptConditions.Accept, // 426
			AcceptConditions.NotAccept, // 427
			AcceptConditions.Accept, // 428
			AcceptConditions.NotAccept, // 429
			AcceptConditions.Accept, // 430
			AcceptConditions.NotAccept, // 431
			AcceptConditions.Accept, // 432
			AcceptConditions.NotAccept, // 433
			AcceptConditions.Accept, // 434
			AcceptConditions.NotAccept, // 435
			AcceptConditions.Accept, // 436
			AcceptConditions.NotAccept, // 437
			AcceptConditions.Accept, // 438
			AcceptConditions.NotAccept, // 439
			AcceptConditions.Accept, // 440
			AcceptConditions.NotAccept, // 441
			AcceptConditions.Accept, // 442
			AcceptConditions.NotAccept, // 443
			AcceptConditions.Accept, // 444
			AcceptConditions.NotAccept, // 445
			AcceptConditions.Accept, // 446
			AcceptConditions.NotAccept, // 447
			AcceptConditions.Accept, // 448
			AcceptConditions.NotAccept, // 449
			AcceptConditions.Accept, // 450
			AcceptConditions.NotAccept, // 451
			AcceptConditions.Accept, // 452
			AcceptConditions.NotAccept, // 453
			AcceptConditions.Accept, // 454
			AcceptConditions.NotAccept, // 455
			AcceptConditions.Accept, // 456
			AcceptConditions.NotAccept, // 457
			AcceptConditions.Accept, // 458
			AcceptConditions.NotAccept, // 459
			AcceptConditions.Accept, // 460
			AcceptConditions.NotAccept, // 461
			AcceptConditions.Accept, // 462
			AcceptConditions.NotAccept, // 463
			AcceptConditions.Accept, // 464
			AcceptConditions.NotAccept, // 465
			AcceptConditions.Accept, // 466
			AcceptConditions.NotAccept, // 467
			AcceptConditions.Accept, // 468
			AcceptConditions.NotAccept, // 469
			AcceptConditions.Accept, // 470
			AcceptConditions.NotAccept, // 471
			AcceptConditions.Accept, // 472
			AcceptConditions.NotAccept, // 473
			AcceptConditions.Accept, // 474
			AcceptConditions.NotAccept, // 475
			AcceptConditions.Accept, // 476
			AcceptConditions.NotAccept, // 477
			AcceptConditions.Accept, // 478
			AcceptConditions.NotAccept, // 479
			AcceptConditions.Accept, // 480
			AcceptConditions.NotAccept, // 481
			AcceptConditions.Accept, // 482
			AcceptConditions.NotAccept, // 483
			AcceptConditions.Accept, // 484
			AcceptConditions.NotAccept, // 485
			AcceptConditions.Accept, // 486
			AcceptConditions.NotAccept, // 487
			AcceptConditions.Accept, // 488
			AcceptConditions.NotAccept, // 489
			AcceptConditions.Accept, // 490
			AcceptConditions.NotAccept, // 491
			AcceptConditions.Accept, // 492
			AcceptConditions.NotAccept, // 493
			AcceptConditions.Accept, // 494
			AcceptConditions.NotAccept, // 495
			AcceptConditions.Accept, // 496
			AcceptConditions.NotAccept, // 497
			AcceptConditions.Accept, // 498
			AcceptConditions.NotAccept, // 499
			AcceptConditions.Accept, // 500
			AcceptConditions.NotAccept, // 501
			AcceptConditions.Accept, // 502
			AcceptConditions.NotAccept, // 503
			AcceptConditions.Accept, // 504
			AcceptConditions.NotAccept, // 505
			AcceptConditions.Accept, // 506
			AcceptConditions.NotAccept, // 507
			AcceptConditions.Accept, // 508
			AcceptConditions.NotAccept, // 509
			AcceptConditions.Accept, // 510
			AcceptConditions.NotAccept, // 511
			AcceptConditions.Accept, // 512
			AcceptConditions.NotAccept, // 513
			AcceptConditions.Accept, // 514
			AcceptConditions.NotAccept, // 515
			AcceptConditions.Accept, // 516
			AcceptConditions.NotAccept, // 517
			AcceptConditions.Accept, // 518
			AcceptConditions.NotAccept, // 519
			AcceptConditions.Accept, // 520
			AcceptConditions.NotAccept, // 521
			AcceptConditions.Accept, // 522
			AcceptConditions.NotAccept, // 523
			AcceptConditions.Accept, // 524
			AcceptConditions.NotAccept, // 525
			AcceptConditions.Accept, // 526
			AcceptConditions.NotAccept, // 527
			AcceptConditions.Accept, // 528
			AcceptConditions.NotAccept, // 529
			AcceptConditions.Accept, // 530
			AcceptConditions.NotAccept, // 531
			AcceptConditions.Accept, // 532
			AcceptConditions.NotAccept, // 533
			AcceptConditions.Accept, // 534
			AcceptConditions.NotAccept, // 535
			AcceptConditions.Accept, // 536
			AcceptConditions.NotAccept, // 537
			AcceptConditions.Accept, // 538
			AcceptConditions.NotAccept, // 539
			AcceptConditions.NotAccept, // 540
			AcceptConditions.NotAccept, // 541
			AcceptConditions.NotAccept, // 542
			AcceptConditions.NotAccept, // 543
			AcceptConditions.NotAccept, // 544
			AcceptConditions.NotAccept, // 545
			AcceptConditions.NotAccept, // 546
			AcceptConditions.NotAccept, // 547
			AcceptConditions.NotAccept, // 548
			AcceptConditions.NotAccept, // 549
			AcceptConditions.NotAccept, // 550
			AcceptConditions.NotAccept, // 551
			AcceptConditions.NotAccept, // 552
			AcceptConditions.NotAccept, // 553
			AcceptConditions.NotAccept, // 554
			AcceptConditions.NotAccept, // 555
			AcceptConditions.NotAccept, // 556
			AcceptConditions.NotAccept, // 557
			AcceptConditions.NotAccept, // 558
			AcceptConditions.NotAccept, // 559
			AcceptConditions.NotAccept, // 560
			AcceptConditions.NotAccept, // 561
			AcceptConditions.NotAccept, // 562
			AcceptConditions.NotAccept, // 563
			AcceptConditions.NotAccept, // 564
			AcceptConditions.NotAccept, // 565
			AcceptConditions.NotAccept, // 566
			AcceptConditions.NotAccept, // 567
			AcceptConditions.NotAccept, // 568
			AcceptConditions.NotAccept, // 569
			AcceptConditions.NotAccept, // 570
			AcceptConditions.NotAccept, // 571
			AcceptConditions.NotAccept, // 572
			AcceptConditions.NotAccept, // 573
			AcceptConditions.NotAccept, // 574
			AcceptConditions.NotAccept, // 575
			AcceptConditions.NotAccept, // 576
			AcceptConditions.NotAccept, // 577
			AcceptConditions.NotAccept, // 578
			AcceptConditions.NotAccept, // 579
			AcceptConditions.NotAccept, // 580
			AcceptConditions.NotAccept, // 581
			AcceptConditions.NotAccept, // 582
			AcceptConditions.NotAccept, // 583
			AcceptConditions.NotAccept, // 584
			AcceptConditions.NotAccept, // 585
			AcceptConditions.NotAccept, // 586
			AcceptConditions.NotAccept, // 587
			AcceptConditions.NotAccept, // 588
			AcceptConditions.NotAccept, // 589
			AcceptConditions.NotAccept, // 590
			AcceptConditions.NotAccept, // 591
			AcceptConditions.NotAccept, // 592
			AcceptConditions.NotAccept, // 593
			AcceptConditions.NotAccept, // 594
			AcceptConditions.NotAccept, // 595
			AcceptConditions.NotAccept, // 596
			AcceptConditions.NotAccept, // 597
			AcceptConditions.NotAccept, // 598
			AcceptConditions.NotAccept, // 599
			AcceptConditions.NotAccept, // 600
			AcceptConditions.NotAccept, // 601
			AcceptConditions.NotAccept, // 602
			AcceptConditions.Accept, // 603
			AcceptConditions.Accept, // 604
			AcceptConditions.Accept, // 605
			AcceptConditions.Accept, // 606
			AcceptConditions.Accept, // 607
			AcceptConditions.Accept, // 608
			AcceptConditions.Accept, // 609
			AcceptConditions.NotAccept, // 610
			AcceptConditions.NotAccept, // 611
			AcceptConditions.NotAccept, // 612
			AcceptConditions.NotAccept, // 613
			AcceptConditions.NotAccept, // 614
			AcceptConditions.NotAccept, // 615
			AcceptConditions.NotAccept, // 616
			AcceptConditions.NotAccept, // 617
			AcceptConditions.NotAccept, // 618
			AcceptConditions.NotAccept, // 619
			AcceptConditions.NotAccept, // 620
			AcceptConditions.NotAccept, // 621
			AcceptConditions.NotAccept, // 622
			AcceptConditions.NotAccept, // 623
			AcceptConditions.NotAccept, // 624
			AcceptConditions.NotAccept, // 625
			AcceptConditions.NotAccept, // 626
			AcceptConditions.NotAccept, // 627
			AcceptConditions.NotAccept, // 628
			AcceptConditions.NotAccept, // 629
			AcceptConditions.NotAccept, // 630
			AcceptConditions.Accept, // 631
			AcceptConditions.NotAccept, // 632
			AcceptConditions.Accept, // 633
			AcceptConditions.Accept, // 634
			AcceptConditions.Accept, // 635
			AcceptConditions.Accept, // 636
			AcceptConditions.Accept, // 637
			AcceptConditions.Accept, // 638
			AcceptConditions.Accept, // 639
			AcceptConditions.Accept, // 640
			AcceptConditions.Accept, // 641
			AcceptConditions.Accept, // 642
			AcceptConditions.Accept, // 643
			AcceptConditions.Accept, // 644
			AcceptConditions.Accept, // 645
			AcceptConditions.Accept, // 646
			AcceptConditions.Accept, // 647
			AcceptConditions.Accept, // 648
			AcceptConditions.Accept, // 649
			AcceptConditions.Accept, // 650
			AcceptConditions.Accept, // 651
			AcceptConditions.Accept, // 652
			AcceptConditions.Accept, // 653
			AcceptConditions.Accept, // 654
			AcceptConditions.Accept, // 655
			AcceptConditions.Accept, // 656
			AcceptConditions.Accept, // 657
			AcceptConditions.Accept, // 658
			AcceptConditions.Accept, // 659
			AcceptConditions.Accept, // 660
			AcceptConditions.Accept, // 661
			AcceptConditions.Accept, // 662
			AcceptConditions.Accept, // 663
			AcceptConditions.Accept, // 664
			AcceptConditions.Accept, // 665
			AcceptConditions.Accept, // 666
			AcceptConditions.Accept, // 667
			AcceptConditions.Accept, // 668
			AcceptConditions.Accept, // 669
			AcceptConditions.Accept, // 670
			AcceptConditions.Accept, // 671
			AcceptConditions.Accept, // 672
			AcceptConditions.Accept, // 673
			AcceptConditions.Accept, // 674
			AcceptConditions.Accept, // 675
			AcceptConditions.Accept, // 676
			AcceptConditions.Accept, // 677
			AcceptConditions.Accept, // 678
			AcceptConditions.Accept, // 679
			AcceptConditions.Accept, // 680
			AcceptConditions.Accept, // 681
			AcceptConditions.Accept, // 682
			AcceptConditions.Accept, // 683
			AcceptConditions.Accept, // 684
			AcceptConditions.Accept, // 685
			AcceptConditions.Accept, // 686
			AcceptConditions.Accept, // 687
			AcceptConditions.Accept, // 688
			AcceptConditions.Accept, // 689
			AcceptConditions.Accept, // 690
			AcceptConditions.Accept, // 691
			AcceptConditions.Accept, // 692
			AcceptConditions.Accept, // 693
			AcceptConditions.Accept, // 694
			AcceptConditions.Accept, // 695
			AcceptConditions.Accept, // 696
			AcceptConditions.Accept, // 697
			AcceptConditions.Accept, // 698
			AcceptConditions.Accept, // 699
			AcceptConditions.Accept, // 700
			AcceptConditions.Accept, // 701
			AcceptConditions.Accept, // 702
			AcceptConditions.Accept, // 703
			AcceptConditions.Accept, // 704
			AcceptConditions.Accept, // 705
			AcceptConditions.Accept, // 706
			AcceptConditions.Accept, // 707
			AcceptConditions.Accept, // 708
			AcceptConditions.Accept, // 709
			AcceptConditions.Accept, // 710
			AcceptConditions.Accept, // 711
			AcceptConditions.Accept, // 712
			AcceptConditions.Accept, // 713
			AcceptConditions.Accept, // 714
			AcceptConditions.Accept, // 715
			AcceptConditions.Accept, // 716
			AcceptConditions.Accept, // 717
			AcceptConditions.Accept, // 718
			AcceptConditions.Accept, // 719
			AcceptConditions.Accept, // 720
			AcceptConditions.Accept, // 721
			AcceptConditions.Accept, // 722
			AcceptConditions.Accept, // 723
			AcceptConditions.Accept, // 724
			AcceptConditions.Accept, // 725
			AcceptConditions.Accept, // 726
			AcceptConditions.Accept, // 727
			AcceptConditions.Accept, // 728
			AcceptConditions.Accept, // 729
			AcceptConditions.Accept, // 730
			AcceptConditions.Accept, // 731
			AcceptConditions.Accept, // 732
			AcceptConditions.Accept, // 733
			AcceptConditions.Accept, // 734
			AcceptConditions.Accept, // 735
			AcceptConditions.Accept, // 736
			AcceptConditions.Accept, // 737
			AcceptConditions.Accept, // 738
			AcceptConditions.Accept, // 739
			AcceptConditions.Accept, // 740
			AcceptConditions.Accept, // 741
			AcceptConditions.Accept, // 742
			AcceptConditions.Accept, // 743
			AcceptConditions.Accept, // 744
			AcceptConditions.Accept, // 745
			AcceptConditions.Accept, // 746
			AcceptConditions.Accept, // 747
			AcceptConditions.Accept, // 748
			AcceptConditions.Accept, // 749
			AcceptConditions.Accept, // 750
			AcceptConditions.Accept, // 751
			AcceptConditions.Accept, // 752
			AcceptConditions.Accept, // 753
			AcceptConditions.Accept, // 754
			AcceptConditions.Accept, // 755
			AcceptConditions.Accept, // 756
			AcceptConditions.Accept, // 757
			AcceptConditions.Accept, // 758
			AcceptConditions.Accept, // 759
			AcceptConditions.Accept, // 760
			AcceptConditions.Accept, // 761
			AcceptConditions.Accept, // 762
			AcceptConditions.Accept, // 763
			AcceptConditions.Accept, // 764
			AcceptConditions.Accept, // 765
			AcceptConditions.Accept, // 766
			AcceptConditions.Accept, // 767
			AcceptConditions.Accept, // 768
			AcceptConditions.Accept, // 769
			AcceptConditions.Accept, // 770
			AcceptConditions.Accept, // 771
			AcceptConditions.Accept, // 772
			AcceptConditions.Accept, // 773
			AcceptConditions.Accept, // 774
			AcceptConditions.Accept, // 775
			AcceptConditions.Accept, // 776
			AcceptConditions.Accept, // 777
			AcceptConditions.Accept, // 778
			AcceptConditions.Accept, // 779
			AcceptConditions.Accept, // 780
			AcceptConditions.Accept, // 781
			AcceptConditions.Accept, // 782
			AcceptConditions.Accept, // 783
			AcceptConditions.Accept, // 784
			AcceptConditions.Accept, // 785
			AcceptConditions.Accept, // 786
			AcceptConditions.Accept, // 787
			AcceptConditions.Accept, // 788
			AcceptConditions.Accept, // 789
			AcceptConditions.Accept, // 790
			AcceptConditions.Accept, // 791
			AcceptConditions.Accept, // 792
			AcceptConditions.Accept, // 793
			AcceptConditions.Accept, // 794
			AcceptConditions.Accept, // 795
			AcceptConditions.Accept, // 796
			AcceptConditions.Accept, // 797
			AcceptConditions.Accept, // 798
			AcceptConditions.Accept, // 799
			AcceptConditions.Accept, // 800
			AcceptConditions.Accept, // 801
			AcceptConditions.Accept, // 802
			AcceptConditions.Accept, // 803
			AcceptConditions.Accept, // 804
			AcceptConditions.Accept, // 805
			AcceptConditions.NotAccept, // 806
			AcceptConditions.NotAccept, // 807
			AcceptConditions.Accept, // 808
			AcceptConditions.Accept, // 809
			AcceptConditions.Accept, // 810
			AcceptConditions.Accept, // 811
			AcceptConditions.Accept, // 812
			AcceptConditions.Accept, // 813
			AcceptConditions.Accept, // 814
			AcceptConditions.Accept, // 815
			AcceptConditions.Accept, // 816
			AcceptConditions.Accept, // 817
			AcceptConditions.Accept, // 818
			AcceptConditions.Accept, // 819
			AcceptConditions.Accept, // 820
			AcceptConditions.Accept, // 821
			AcceptConditions.Accept, // 822
			AcceptConditions.Accept, // 823
			AcceptConditions.Accept, // 824
			AcceptConditions.Accept, // 825
			AcceptConditions.Accept, // 826
			AcceptConditions.Accept, // 827
			AcceptConditions.Accept, // 828
			AcceptConditions.Accept, // 829
			AcceptConditions.Accept, // 830
			AcceptConditions.Accept, // 831
			AcceptConditions.Accept, // 832
			AcceptConditions.Accept, // 833
			AcceptConditions.Accept, // 834
			AcceptConditions.Accept, // 835
			AcceptConditions.Accept, // 836
			AcceptConditions.Accept, // 837
			AcceptConditions.Accept, // 838
			AcceptConditions.Accept, // 839
			AcceptConditions.Accept, // 840
			AcceptConditions.Accept, // 841
			AcceptConditions.Accept, // 842
			AcceptConditions.Accept, // 843
			AcceptConditions.Accept, // 844
			AcceptConditions.Accept, // 845
			AcceptConditions.Accept, // 846
			AcceptConditions.Accept, // 847
			AcceptConditions.Accept, // 848
			AcceptConditions.Accept, // 849
			AcceptConditions.Accept, // 850
			AcceptConditions.Accept, // 851
			AcceptConditions.Accept, // 852
			AcceptConditions.Accept, // 853
			AcceptConditions.Accept, // 854
			AcceptConditions.Accept, // 855
			AcceptConditions.Accept, // 856
			AcceptConditions.Accept, // 857
			AcceptConditions.Accept, // 858
			AcceptConditions.Accept, // 859
			AcceptConditions.Accept, // 860
			AcceptConditions.Accept, // 861
			AcceptConditions.Accept, // 862
			AcceptConditions.Accept, // 863
			AcceptConditions.Accept, // 864
			AcceptConditions.Accept, // 865
			AcceptConditions.Accept, // 866
			AcceptConditions.Accept, // 867
			AcceptConditions.Accept, // 868
			AcceptConditions.Accept, // 869
			AcceptConditions.Accept, // 870
			AcceptConditions.Accept, // 871
			AcceptConditions.Accept, // 872
			AcceptConditions.Accept, // 873
			AcceptConditions.Accept, // 874
			AcceptConditions.Accept, // 875
			AcceptConditions.Accept, // 876
			AcceptConditions.Accept, // 877
			AcceptConditions.Accept, // 878
			AcceptConditions.Accept, // 879
			AcceptConditions.Accept, // 880
			AcceptConditions.Accept, // 881
			AcceptConditions.Accept, // 882
			AcceptConditions.Accept, // 883
			AcceptConditions.Accept, // 884
			AcceptConditions.Accept, // 885
			AcceptConditions.Accept, // 886
			AcceptConditions.Accept, // 887
			AcceptConditions.Accept, // 888
			AcceptConditions.Accept, // 889
			AcceptConditions.Accept, // 890
			AcceptConditions.Accept, // 891
			AcceptConditions.Accept, // 892
			AcceptConditions.Accept, // 893
			AcceptConditions.Accept, // 894
			AcceptConditions.Accept, // 895
			AcceptConditions.Accept, // 896
			AcceptConditions.Accept, // 897
			AcceptConditions.Accept, // 898
			AcceptConditions.Accept, // 899
			AcceptConditions.Accept, // 900
			AcceptConditions.Accept, // 901
			AcceptConditions.Accept, // 902
			AcceptConditions.Accept, // 903
			AcceptConditions.Accept, // 904
			AcceptConditions.Accept, // 905
			AcceptConditions.Accept, // 906
			AcceptConditions.Accept, // 907
			AcceptConditions.Accept, // 908
			AcceptConditions.Accept, // 909
			AcceptConditions.NotAccept, // 910
			AcceptConditions.Accept, // 911
			AcceptConditions.Accept, // 912
			AcceptConditions.Accept, // 913
			AcceptConditions.Accept, // 914
			AcceptConditions.Accept, // 915
			AcceptConditions.Accept, // 916
			AcceptConditions.Accept, // 917
			AcceptConditions.Accept, // 918
			AcceptConditions.Accept, // 919
			AcceptConditions.Accept, // 920
			AcceptConditions.Accept, // 921
			AcceptConditions.Accept, // 922
			AcceptConditions.Accept, // 923
			AcceptConditions.Accept, // 924
		};
		
		private static int[] colMap = new int[]
		{
			64, 64, 64, 64, 64, 64, 64, 64, 64, 23, 11, 64, 64, 24, 64, 64, 
			64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 
			69, 44, 20, 57, 50, 1, 47, 21, 58, 60, 45, 42, 53, 43, 46, 25, 
			55, 56, 62, 61, 39, 68, 38, 68, 59, 52, 41, 66, 2, 18, 22, 5, 
			53, 13, 31, 6, 26, 17, 28, 15, 19, 8, 40, 32, 12, 36, 14, 29, 
			9, 35, 7, 4, 10, 16, 33, 30, 27, 37, 51, 70, 54, 70, 49, 34, 
			3, 13, 31, 6, 26, 17, 28, 15, 19, 8, 40, 32, 12, 36, 14, 29, 
			9, 35, 7, 4, 10, 16, 33, 30, 27, 37, 51, 63, 48, 65, 53, 64, 
			67, 0
		};
		
		private static int[] rowMap = new int[]
		{
			0, 1, 2, 3, 4, 1, 1, 5, 1, 6, 7, 8, 9, 10, 1, 11, 
			1, 1, 1, 1, 12, 13, 1, 1, 1, 14, 15, 16, 17, 18, 1, 1, 
			1, 1, 19, 1, 1, 20, 21, 22, 16, 23, 1, 1, 1, 1, 1, 1, 
			1, 1, 1, 1, 1, 1, 1, 1, 24, 1, 25, 1, 16, 16, 16, 16, 
			1, 1, 16, 16, 26, 16, 27, 1, 28, 29, 16, 16, 1, 16, 16, 16, 
			16, 16, 30, 16, 16, 31, 16, 16, 1, 16, 16, 16, 16, 16, 16, 16, 
			16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 1, 16, 
			16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 32, 16, 16, 33, 1, 1, 
			1, 1, 34, 35, 16, 16, 16, 16, 16, 16, 16, 16, 16, 1, 1, 1, 
			1, 1, 1, 16, 16, 16, 16, 16, 16, 16, 16, 16, 1, 1, 1, 1, 
			1, 16, 16, 16, 16, 16, 16, 16, 16, 16, 1, 16, 16, 16, 16, 16, 
			16, 16, 16, 16, 16, 16, 16, 16, 16, 36, 37, 38, 39, 40, 41, 42, 
			1, 43, 44, 45, 40, 1, 46, 1, 1, 47, 1, 48, 1, 49, 1, 1, 
			50, 51, 1, 52, 1, 53, 54, 55, 56, 57, 52, 1, 58, 1, 1, 1, 
			59, 1, 60, 61, 1, 1, 62, 63, 64, 65, 66, 67, 68, 63, 1, 69, 
			1, 1, 70, 1, 71, 72, 1, 1, 73, 1, 1, 74, 1, 75, 76, 77, 
			1, 78, 79, 1, 80, 81, 1, 1, 82, 83, 84, 1, 85, 86, 87, 88, 
			1, 89, 1, 90, 91, 92, 93, 94, 1, 95, 1, 1, 1, 1, 96, 97, 
			98, 1, 99, 1, 1, 1, 1, 100, 101, 102, 103, 1, 104, 1, 1, 1, 
			1, 105, 1, 106, 107, 108, 109, 110, 111, 112, 113, 1, 114, 1, 115, 1, 
			116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 
			132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 
			148, 149, 150, 151, 152, 153, 154, 155, 1, 156, 157, 158, 159, 160, 161, 162, 
			163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 
			179, 180, 181, 8, 182, 183, 184, 9, 185, 186, 187, 188, 189, 190, 191, 28, 
			192, 29, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 158, 204, 205, 
			206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 
			222, 31, 223, 224, 225, 27, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 
			236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 
			252, 253, 254, 255, 256, 257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 
			268, 269, 270, 271, 272, 273, 274, 275, 276, 277, 278, 279, 280, 281, 282, 283, 
			284, 285, 286, 287, 288, 289, 290, 291, 292, 293, 294, 295, 296, 297, 298, 299, 
			300, 301, 302, 303, 304, 305, 306, 307, 308, 309, 310, 311, 312, 313, 314, 315, 
			316, 317, 318, 319, 320, 321, 322, 323, 324, 325, 326, 327, 328, 329, 330, 331, 
			332, 333, 334, 335, 336, 337, 338, 339, 340, 341, 342, 343, 344, 345, 346, 347, 
			348, 349, 350, 351, 352, 353, 354, 355, 356, 36, 357, 358, 359, 360, 361, 362, 
			363, 364, 365, 366, 367, 368, 113, 369, 370, 371, 372, 373, 114, 374, 375, 376, 
			115, 377, 378, 379, 380, 381, 382, 383, 384, 385, 386, 387, 388, 389, 390, 391, 
			392, 393, 394, 395, 396, 219, 397, 398, 399, 400, 401, 402, 403, 404, 405, 406, 
			407, 408, 409, 410, 411, 412, 413, 414, 415, 416, 417, 418, 419, 420, 421, 422, 
			423, 424, 425, 426, 427, 428, 429, 430, 431, 432, 433, 434, 435, 436, 437, 438, 
			439, 440, 441, 442, 443, 444, 445, 446, 447, 448, 449, 450, 451, 452, 453, 454, 
			455, 456, 457, 458, 459, 460, 461, 462, 463, 464, 465, 466, 467, 468, 469, 470, 
			471, 472, 473, 474, 475, 476, 477, 478, 479, 480, 481, 482, 483, 484, 485, 486, 
			487, 488, 489, 490, 491, 492, 493, 494, 495, 496, 497, 498, 499, 500, 501, 502, 
			503, 504, 505, 506, 507, 508, 509, 510, 511, 512, 513, 514, 515, 516, 517, 518, 
			519, 520, 521, 522, 523, 524, 525, 526, 527, 528, 529, 530, 531, 532, 533, 534, 
			535, 536, 537, 538, 539, 540, 541, 542, 543, 544, 545, 546, 547, 548, 549, 550, 
			551, 552, 553, 554, 555, 556, 557, 558, 559, 560, 561, 562, 563, 564, 565, 566, 
			567, 568, 569, 570, 571, 572, 573, 574, 575, 576, 577, 578, 579, 580, 581, 582, 
			583, 584, 585, 586, 587, 588, 589, 590, 591, 592, 593, 594, 595, 596, 597, 598, 
			599, 600, 601, 602, 603, 604, 605, 606, 607, 608, 609, 610, 611, 612, 613, 614, 
			615, 616, 617, 618, 619, 620, 621, 622, 623, 624, 625, 626, 627, 628, 629, 630, 
			631, 632, 633, 634, 635, 636, 637, 638, 639, 640, 641, 642, 643, 644, 645, 646, 
			647, 648, 649, 650, 651, 652, 653, 654, 655, 656, 657, 658, 659, 660, 661, 662, 
			663, 664, 665, 666, 667, 668, 669, 670, 671, 672, 673, 674, 675, 676, 677, 678, 
			679, 557, 680, 681, 682, 683, 684, 16, 685, 686, 687, 688, 689, 690, 691, 692, 
			693, 694, 695, 696, 697, 698, 699, 700, 701, 702, 703, 704, 705
		};
		
		private static int[,] nextState = new int[,]
		{
			{ 1, 2, 266, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 2, 264, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 19, -1, -1, -1, 20, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 906, -1, 903, 903, 903, 903, 903, 637, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 638, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, -1 },
			{ -1, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 31, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, -1, 387, 387, 387, 389, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, -1, 387, 387, 387 },
			{ -1, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 32, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 393, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, -1, 391, 391, 391 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 397, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 13, 13, -1, -1, -1, -1, -1, -1, 41, -1, -1, -1, -1, -1, 13, -1, -1, 13, 13, -1, -1, 13, -1, 13, 13, -1, -1, -1, -1, -1, 13, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 272, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 423, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 57, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 274, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 313, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 873, -1, 851, 903, 903, 903, 58, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 815, -1, 852, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 65, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 433, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 818, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 895, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 397, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, 41, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, -1, -1, 41, 41, -1, -1, 41, -1, 41, 41, -1, -1, -1, -1, -1, 41, -1, -1 },
			{ -1, -1, -1, -1, 56, -1, 56, 56, 56, 56, 56, -1, 56, 56, 56, 56, 56, 56, -1, 56, -1, -1, -1, -1, -1, -1, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 56, 56, -1, -1, 56, 56, -1, -1, 56, -1, 56, 56, -1, -1, -1, -1, -1, 56, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 707, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 392, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 720, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, 70, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, -1, -1, 70, 70, -1, -1, 70, -1, 70, 70, -1, -1, -1, -1, -1, 70, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 72, -1, -1, -1, -1, -1, -1, 72, -1, -1, -1, 72, -1, -1, -1, -1, -1, -1, -1, -1, 72, -1, 72, -1, -1, 72, -1, -1, -1, -1, -1, -1, 72, 72, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 72, -1, -1, 72, 72, -1, -1, 72, -1, 72, 72, -1, -1, -1, -1, -1, 72, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, 73, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 444, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 85, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 85, 85, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 85, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 775, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 840, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 782, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 914, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, -1, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, -1, 185, 185, 185 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 276, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 186, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, 186, 186, -1, -1, 186, -1, 186, 186, -1, -1, -1, -1, -1, 186, 276, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 188, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 188, -1 },
			{ -1, 189, 189, -1, -1, 189, -1, -1, -1, -1, -1, 189, -1, -1, -1, -1, -1, -1, 189, -1, -1, 189, 189, 189, 189, 189, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 189, 189, 189, 189, 189, 189, 189, 189, 189, -1, -1, -1, 189, -1, -1, -1, 189, 189, -1, 189, -1, -1, -1, -1, -1, 189, -1, -1, 189, -1 },
			{ -1, -1, -1, 190, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 191, -1, 191, 191, 191, 191, 191, -1, 191, 191, 191, 191, 191, 191, -1, 191, -1, -1, -1, -1, -1, -1, 191, 191, 191, 191, 191, 191, 191, 191, 191, 191, 191, 191, 191, 191, 191, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 191, 191, -1, -1, 191, 191, -1, -1, 191, -1, 191, 191, -1, -1, -1, -1, -1, 191, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 193, 193, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 193, -1, -1, 193, 193, -1, -1, 193, -1, 193, 193, -1, -1, -1, -1, -1, 193, -1, -1 },
			{ -1, 197, 197, 197, 198, 197, 198, 198, 198, 198, 198, 197, 198, 198, 198, 198, 198, 198, 197, 198, 197, 197, 197, 197, 197, 197, 198, 198, 198, 198, 198, 198, 198, 198, 198, 198, 198, 198, 197, 197, 198, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 198, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 199, 197, 197, 197, -1, 197, 197, 197 },
			{ -1, 200, 200, 200, 200, 200, 201, 202, 200, 200, 202, 200, 200, 200, 202, 200, 203, 200, 200, 200, 204, 200, 200, 200, 200, 200, 200, 281, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 205, 205, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 202, 200, 200, 200, 202, 205, 205, 200, 200, 200, 200, 205, 205, 206, 200, 200, 200, -1, 205, 200, 200 },
			{ -1, -1, -1, -1, 198, -1, 198, 198, 198, 198, 198, -1, 198, 198, 198, 198, 198, 198, -1, 198, -1, -1, -1, -1, -1, -1, 198, 198, 198, 198, 198, 198, 198, 198, 198, 198, 198, 198, 198, 198, 198, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 198, 198, -1, -1, 198, 198, -1, -1, 198, -1, 198, 198, -1, -1, -1, -1, -1, 198, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 583, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 922, -1, -1, -1, -1, -1, -1, 922, -1, -1, -1, 922, -1, -1, -1, -1, -1, -1, -1, -1, 922, -1, 922, -1, -1, 922, -1, -1, -1, -1, -1, -1, 922, 922, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 922, -1, -1, 922, 922, -1, -1, 922, -1, 922, 922, -1, -1, -1, -1, -1, 922, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 605, 605, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 605, 605, -1, -1, -1, -1, 605, 605, -1, -1, -1, -1, -1, 605, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 285, -1, -1, -1, -1, -1, -1, 285, -1, -1, -1, 285, -1, -1, -1, -1, -1, -1, -1, -1, 285, -1, 285, -1, -1, 285, -1, -1, -1, -1, -1, -1, 285, 285, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 285, -1, -1, 285, 285, -1, -1, 285, -1, 285, 285, -1, -1, -1, -1, -1, 285, -1, -1 },
			{ -1, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, -1, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 586, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, -1, 209, 209, 209 },
			{ -1, 211, 211, -1, -1, 211, -1, -1, -1, -1, -1, 211, -1, -1, -1, -1, -1, -1, 211, -1, -1, 211, 211, 211, 211, 211, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 211, 211, 211, 211, 211, 211, 211, 211, 211, -1, -1, -1, 211, -1, -1, -1, 211, 211, -1, 211, -1, -1, -1, -1, -1, 211, -1, -1, 211, -1 },
			{ -1, -1, -1, -1, 213, -1, 213, 213, 213, 213, 213, -1, 213, 213, 213, 213, 213, 213, -1, 213, -1, -1, -1, -1, -1, -1, 213, 213, 213, 213, 213, 213, 213, 213, 213, 213, 213, 213, 213, 213, 213, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 213, 213, -1, -1, 213, 213, -1, -1, 213, -1, 213, 213, -1, -1, -1, -1, -1, 213, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 214, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 215, 215, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 215, -1, -1, 215, 215, -1, -1, 215, -1, 215, 215, -1, -1, -1, -1, -1, 215, -1, -1 },
			{ -1, 219, 219, 219, 220, 219, 220, 220, 220, 220, 220, 219, 220, 220, 220, 220, 220, 220, 219, 220, 219, 219, 219, 219, 219, 219, 220, 220, 220, 220, 220, 220, 220, 220, 220, 220, 220, 220, 219, 219, 220, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 220, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 221, 219, 219, 219, -1, 219, 219, 219 },
			{ -1, 222, 222, 223, 222, 222, 224, 225, 222, 222, 225, 222, 222, 222, 225, 222, 226, 222, 222, 222, 222, 222, 222, 222, 222, 222, 222, 290, 222, 222, 222, 222, 222, 222, 222, 222, 222, 222, 227, 227, 222, 222, 222, 222, 222, 222, 222, 222, 222, 222, 225, 222, 222, 222, 225, 227, 227, 222, 222, 222, 222, 227, 227, 228, 222, 222, 222, -1, 227, 222, 222 },
			{ -1, -1, -1, -1, 220, -1, 220, 220, 220, 220, 220, -1, 220, 220, 220, 220, 220, 220, -1, 220, -1, -1, -1, -1, -1, -1, 220, 220, 220, 220, 220, 220, 220, 220, 220, 220, 220, 220, 220, 220, 220, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 220, 220, -1, -1, 220, 220, -1, -1, 220, -1, 220, 220, -1, -1, -1, -1, -1, 220, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 589, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 923, -1, -1, -1, -1, -1, -1, 923, -1, -1, -1, 923, -1, -1, -1, -1, -1, -1, -1, -1, 923, -1, 923, -1, -1, 923, -1, -1, -1, -1, -1, -1, 923, 923, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 923, -1, -1, 923, 923, -1, -1, 923, -1, 923, 923, -1, -1, -1, -1, -1, 923, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 607, 607, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 607, 607, -1, -1, -1, -1, 607, 607, -1, -1, -1, -1, -1, 607, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 294, -1, -1, -1, -1, -1, -1, 294, -1, -1, -1, 294, -1, -1, -1, -1, -1, -1, -1, -1, 294, -1, 294, -1, -1, 294, -1, -1, -1, -1, -1, -1, 294, 294, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 294, -1, -1, 294, 294, -1, -1, 294, -1, 294, 294, -1, -1, -1, -1, -1, 294, -1, -1 },
			{ -1, 231, 231, -1, -1, 231, -1, -1, -1, -1, -1, 231, -1, -1, -1, -1, -1, -1, 231, -1, -1, 231, 231, 231, 231, 231, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 231, 231, 231, 231, 231, 231, 231, 231, 231, -1, -1, -1, 231, -1, -1, -1, 231, 231, -1, 231, -1, -1, -1, -1, -1, 231, -1, -1, 231, -1 },
			{ -1, -1, -1, 232, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 232, 232, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 233, -1, 233, 233, 233, 233, 233, -1, 233, 233, 233, 233, 233, 233, -1, 233, -1, -1, -1, -1, -1, -1, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 233, 233, -1, -1, 233, 233, -1, -1, 233, -1, 233, 233, -1, -1, -1, -1, -1, 233, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 234, 234, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 234, -1, -1, 234, 234, -1, -1, 234, -1, 234, 234, -1, -1, -1, -1, -1, 234, -1, -1 },
			{ -1, 238, 238, 238, 239, 238, 239, 239, 239, 239, 239, 238, 239, 239, 239, 239, 239, 239, 238, 239, 238, 238, 238, 238, 238, 238, 239, 239, 239, 239, 239, 239, 239, 239, 239, 239, 239, 239, 238, 238, 239, 238, 238, 238, 238, 238, 238, 238, 238, 238, 238, 239, 238, 238, 238, 238, 238, 238, 238, 238, 238, 238, 238, 240, 238, 238, 238, -1, 238, 238, 238 },
			{ -1, 241, 241, 241, 241, 241, 242, 243, 241, 241, 243, 241, 241, 241, 243, 241, 244, 241, 241, 241, 241, 241, 241, 241, 241, 241, 241, 300, 241, 241, 241, 241, 241, 241, 241, 241, 241, 241, 245, 245, 241, 241, 241, 241, 241, 241, 241, 241, 241, 241, 243, 241, 241, 241, 243, 245, 245, 241, 241, 241, 241, 245, 245, 246, 241, 241, 241, -1, 245, 241, 241 },
			{ -1, -1, -1, -1, 239, -1, 239, 239, 239, 239, 239, -1, 239, 239, 239, 239, 239, 239, -1, 239, -1, -1, -1, -1, -1, -1, 239, 239, 239, 239, 239, 239, 239, 239, 239, 239, 239, 239, 239, 239, 239, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 239, 239, -1, -1, 239, 239, -1, -1, 239, -1, 239, 239, -1, -1, -1, -1, -1, 239, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 594, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 924, -1, -1, -1, -1, -1, -1, 924, -1, -1, -1, 924, -1, -1, -1, -1, -1, -1, -1, -1, 924, -1, 924, -1, -1, 924, -1, -1, -1, -1, -1, -1, 924, 924, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 924, -1, -1, 924, 924, -1, -1, 924, -1, 924, 924, -1, -1, -1, -1, -1, 924, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 609, 609, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 609, 609, -1, -1, -1, -1, 609, 609, -1, -1, -1, -1, -1, 609, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 304, -1, -1, -1, -1, -1, -1, 304, -1, -1, -1, 304, -1, -1, -1, -1, -1, -1, -1, -1, 304, -1, 304, -1, -1, 304, -1, -1, -1, -1, -1, -1, 304, 304, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 304, -1, -1, 304, 304, -1, -1, 304, -1, 304, 304, -1, -1, -1, -1, -1, 304, -1, -1 },
			{ -1, -1, -1, -1, 251, -1, 251, 251, 251, 251, 251, -1, 251, 251, 251, 251, 251, 251, -1, 251, -1, -1, -1, -1, -1, -1, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 251, 251, -1, -1, 251, 251, -1, -1, 251, -1, 251, 251, -1, -1, -1, -1, -1, 251, -1, -1 },
			{ -1, -1, -1, -1, 253, -1, 253, 253, 253, 253, 253, -1, 253, 253, 253, 253, 253, 253, -1, 253, -1, -1, -1, -1, -1, -1, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 253, 253, -1, -1, 253, 253, -1, -1, 253, -1, 253, 253, -1, -1, -1, -1, -1, 253, -1, -1 },
			{ -1, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, -1, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, -1, 254, 254, 254 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 256, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, -1, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, -1, 257, 257, 257 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 259, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 263, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 261, 261, 261, -1, 261, 261, 261, 261, 261, -1, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, -1, 261, -1, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, -1, 261, 261, 261 },
			{ -1, -1, -1, 2, -1, -1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, 593, -1, 593, 593, 593, 593, 593, -1, 593, 593, 593, 593, 593, 593, -1, 593, -1, -1, -1, -1, -1, -1, 593, 593, 593, 593, 593, 593, 593, 593, 593, 593, 593, 593, -1, -1, 593, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 593, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 3, -1, 2, 309, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 21, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 22, -1, -1, -1, 23, -1, -1, 383, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 24, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 643, -1, 903, 903, 903, 903, 903, -1, 903, 903, 26, 903, 903, 903, -1, 903, -1, 385, -1, -1, -1, -1, 903, 903, 27, 903, 903, 903, 903, 903, 903, 903, 644, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 397, -1, -1, -1, -1, -1, -1, -1, -1, -1, 399, -1, -1, -1, 401, -1, -1, -1, -1, -1, -1, 13, 13, -1, -1, -1, -1, -1, -1, 41, -1, -1, -1, -1, -1, 13, -1, -1, 13, 13, -1, -1, 13, -1, 13, 13, -1, -1, -1, -1, -1, 13, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 88, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 276, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 276, -1 },
			{ -1, 189, 189, -1, -1, 189, -1, -1, -1, -1, -1, 189, -1, -1, -1, -1, -1, -1, 189, -1, -1, 189, 196, 189, 189, 189, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 189, 189, 189, 189, 189, 189, 189, 189, 189, -1, -1, -1, 189, -1, -1, -1, 189, 189, -1, 189, -1, -1, -1, -1, -1, 189, -1, -1, 189, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 582, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 193, 193, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 193, -1, -1, 193, 193, -1, -1, 193, -1, 193, 193, -1, -1, -1, -1, -1, 193, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 207, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 208, -1, -1, -1, -1, -1, -1, 208, -1, -1, -1, 208, -1, -1, -1, -1, -1, -1, -1, -1, 208, -1, 208, -1, -1, 208, -1, -1, -1, -1, -1, -1, 208, 208, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 208, -1, -1, 208, 208, -1, -1, 208, -1, 208, 208, -1, -1, -1, -1, -1, 208, -1, -1 },
			{ -1, 211, 211, -1, -1, 211, -1, -1, -1, -1, -1, 211, -1, -1, -1, -1, -1, -1, 211, -1, -1, 211, 218, 211, 211, 211, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 211, 211, 211, 211, 211, 211, 211, 211, 211, -1, -1, -1, 211, -1, -1, -1, 211, 211, -1, 211, -1, -1, -1, -1, -1, 211, -1, -1, 211, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 588, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 215, 215, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 215, -1, -1, 215, 215, -1, -1, 215, -1, 215, 215, -1, -1, -1, -1, -1, 215, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 229, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 230, -1, -1, -1, -1, -1, -1, 230, -1, -1, -1, 230, -1, -1, -1, -1, -1, -1, -1, -1, 230, -1, 230, -1, -1, 230, -1, -1, -1, -1, -1, -1, 230, 230, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 230, -1, -1, 230, 230, -1, -1, 230, -1, 230, 230, -1, -1, -1, -1, -1, 230, -1, -1 },
			{ -1, 231, 231, -1, -1, 231, -1, -1, -1, -1, -1, 231, -1, -1, -1, -1, -1, -1, 231, -1, -1, 231, 237, 231, 231, 231, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 231, 231, 231, 231, 231, 231, 231, 231, 231, -1, -1, -1, 231, -1, -1, -1, 231, 231, -1, 231, -1, -1, -1, -1, -1, 231, -1, -1, 231, -1 },
			{ -1, 231, 231, 232, -1, 231, -1, -1, -1, -1, -1, 231, -1, -1, -1, -1, -1, -1, 231, -1, 232, 296, 231, 231, 231, 231, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 231, 231, 231, 231, 231, 231, 231, 231, 231, -1, -1, -1, 231, -1, -1, -1, 231, 231, -1, 231, -1, -1, -1, -1, -1, 231, -1, -1, 231, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 592, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 234, 234, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 234, -1, -1, 234, 234, -1, -1, 234, -1, 234, 234, -1, -1, -1, -1, -1, 234, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 247, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 248, -1, -1, -1, -1, -1, -1, 248, -1, -1, -1, 248, -1, -1, -1, -1, -1, -1, -1, -1, 248, -1, 248, -1, -1, 248, -1, -1, -1, -1, -1, -1, 248, 248, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 248, -1, -1, 248, 248, -1, -1, 248, -1, 248, 248, -1, -1, -1, -1, -1, 248, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 249, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 262, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 308, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 25, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 28, -1, 903, 849, 903, 903, 903, -1, 903, 903, 334, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 810, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 272, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 274, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 314, -1, -1, -1, -1, -1, -1, 314, -1, -1, -1, 314, -1, -1, -1, -1, -1, -1, -1, -1, 314, -1, 314, -1, -1, 314, -1, -1, -1, -1, -1, -1, 314, 314, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 314, -1, -1, 314, 314, -1, -1, 314, -1, 314, 314, -1, -1, -1, -1, -1, 314, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, 316, -1, -1, 316, -1, -1, -1, -1, -1, -1, 316, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, 316, 316, -1, -1, 316, -1, 316, 316, -1, -1, -1, -1, -1, 316, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, 318, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, -1, 318, -1, 318, -1, -1, 318, -1, -1, -1, -1, -1, -1, 318, 318, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 318, -1, -1, 318, 318, -1, -1, 318, -1, 318, 318, -1, -1, -1, -1, -1, 318, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 326, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 29, -1, -1, -1, 30, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 343, 903, 903, -1, 903, 903, 903, 903, 903, 658, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 38, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 610, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 33, -1, -1, -1, 34, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 39, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 661, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 329, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 35, -1, -1, -1, -1, -1, -1, 36, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 37, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 395, -1, 903, -1, 903, 663, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, 11, 12, -1, -1, -1, -1, 903, 903, 903, 664, 903, 903, 903, 903, 903, 903, 903, 40, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, 268, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 42, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 43, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 676, 903, 903, 361, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 60, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 335, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 44, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 45, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 61, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 338, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 338, 338, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 338, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 46, -1, -1, -1, 47, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 48, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 62, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 338, 341, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 338, 338, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 338, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 63, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 344, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 49, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 66, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 347, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 50, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, 41, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, -1, -1, 41, 41, -1, -1, 41, -1, 41, 41, -1, -1, -1, -1, -1, 41, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 67, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 350, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 51, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 52, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 68, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 353, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 53, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 54, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 69, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 356, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 55, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 701, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 74, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 359, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 56, -1, 56, 56, 56, 56, 56, -1, 56, 56, 56, 56, 56, 56, -1, 56, -1, -1, -1, -1, -1, -1, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, -1, -1, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 75, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 362, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 77, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 362, -1, -1, -1, -1, -1, -1, 365, -1, -1, -1, -1, 362, 362, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 362, -1 },
			{ -1, -1, -1, -1, 405, -1, -1, 407, 409, -1, -1, -1, -1, 614, -1, -1, 411, -1, -1, -1, -1, -1, -1, 413, -1, -1, 415, -1, 417, 419, -1, 421, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 413, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 78, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 367, -1, 365, -1, -1, -1, -1, -1, -1, -1, -1, 369, 910, -1, 365, 365, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 365, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 79, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 611, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 80, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 371, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 81, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 375, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 82, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 373, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 267, 373, 373, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 373, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 83, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 377, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 84, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 373, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 86, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 373, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 87, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ 1, 7, 269, 8, 9, 310, 802, 846, 270, 870, 603, 10, 885, 311, 631, 894, 633, 900, 321, 903, 11, 12, 324, 10, 10, 327, 322, 634, 635, 325, 904, 328, 903, 636, 905, 903, 903, 903, 13, 13, 903, 330, 333, 336, 339, 342, 345, 348, 351, 354, 357, 903, 13, 360, 14, 271, 13, 15, 363, 13, 360, 13, 13, 16, 17, 18, 360, 1, 13, 10, 360 },
			{ -1, -1, -1, -1, 89, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 425, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 90, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 427, 429, 429, 427, 427, 427, 427, 427, 427, 427, 429, 427, 427, 427, 427, 427, 427, 427, 427, 427, 59, 429, 427, 429, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 431, 427, 427, 429, 427, 427, 427, 427, 427, 427, 427, 427, 427, -1, 427, 427, 427 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 91, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 92, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, -1, 387, 387, 387 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 93, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 94, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, 391, -1, 391, 391, 391 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 95, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, 435, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 96, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, 70, -1, -1, 437, 437, -1, -1, -1, -1, -1, -1, -1, -1, 70, -1, -1, 70, 70, -1, -1, 70, -1, 70, 70, -1, -1, -1, -1, -1, 70, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 97, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 98, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 99, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 439, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 100, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 441, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 101, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 615, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 102, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 443, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 103, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 445, -1, -1, -1, -1, -1, 447, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 104, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 105, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 449, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 106, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 451, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 107, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 453, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 108, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 617, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 618, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 109, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 455, -1, 455, 455, 455, 455, 455, -1, 455, 455, 455, 455, 455, 455, -1, 455, 457, 619, -1, 423, -1, -1, 455, 455, 455, 455, 455, 455, 455, 455, 455, 455, 455, 455, -1, -1, 455, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 455, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 423, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 111, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 807, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 112, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 427, 429, 429, 427, 427, 427, 427, 427, 427, 427, 429, 427, 427, 427, 427, 427, 427, 427, 427, 427, 76, 429, 427, 429, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 431, 427, 427, 429, 427, 427, 427, 427, 427, 427, 427, 427, 427, -1, 427, 427, 427 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 113, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 59, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 459, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, -1, 429, 429, 429 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 114, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 427, 613, 613, 427, 427, 427, 427, 427, 427, 427, 613, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 613, 427, 461, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 427, 613, 427, 427, 427, 427, 427, 427, 427, 427, 427, -1, 427, 427, 427 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 115, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 116, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, 423, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 117, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 118, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 463, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 119, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 465, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 120, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 469, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 121, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 473, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 122, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 475, -1, -1, -1, 477, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 123, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 479, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 124, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 622, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 125, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 623, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 130, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 455, -1, 455, 455, 455, 455, 455, 88, 455, 455, 455, 455, 455, 455, -1, 455, -1, -1, -1, -1, 275, -1, 455, 455, 455, 455, 455, 455, 455, 455, 455, 455, 455, 455, 455, 455, 455, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 455, 455, -1, -1, 455, 455, -1, -1, 455, -1, 455, 455, -1, -1, -1, -1, -1, 455, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 131, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 481, -1, 481, 481, 481, 481, 481, -1, 481, 481, 481, 481, 481, 481, -1, 481, -1, -1, -1, -1, -1, -1, 481, 481, 481, 481, 481, 481, 481, 481, 481, 481, 481, 481, -1, -1, 481, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 481, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 132, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 461, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, 613, -1, 613, 613, 613 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 133, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 613, 429, 429, 429, 429, 429, 429, 429, 429, 429, 59, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 459, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, 429, -1, 429, 429, 429 },
			{ -1, -1, -1, -1, 134, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 485, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 135, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 487, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 136, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 489, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 137, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 491, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 495, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 497, -1, -1, 499, 110, 501, -1, -1, -1, -1, -1, -1, -1, 493, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 138, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 503, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 139, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 505, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 140, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 507, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 147, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 509, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 148, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 511, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 149, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 481, -1, 481, 481, 481, 481, 481, -1, 481, 481, 481, 481, 481, 481, -1, 481, 519, -1, -1, -1, -1, -1, 481, 481, 481, 481, 481, 481, 481, 481, 481, 481, 481, 481, 481, 481, 481, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 481, 481, -1, -1, 481, 481, -1, -1, 481, -1, 481, 481, -1, -1, -1, -1, -1, 481, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 150, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 483, -1, 483, 483, 483, 483, 483, -1, 483, 483, 483, 483, 483, 483, -1, 483, -1, 519, -1, -1, -1, -1, 483, 483, 483, 483, 483, 483, 483, 483, 483, 483, 483, 483, 483, 483, 483, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 483, 483, -1, -1, 483, 483, -1, -1, 483, -1, 483, 483, -1, -1, -1, -1, -1, 483, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 151, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 523, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 152, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 624, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 153, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 489, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 126, -1, -1, -1, -1, -1, -1, -1, -1, 489, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 154, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 525, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 155, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 110, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 161, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 527, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 162, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 529, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 163, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 499, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 127, -1, -1, -1, -1, -1, -1, -1, -1, 499, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 164, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 165, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 531, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 166, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, -1, -1, 539, 128, 540, -1, -1, -1, -1, -1, -1, -1, 533, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 167, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 168, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 542, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 169, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 543, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 171, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 545, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 172, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 546, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 173, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 626, -1, -1, -1, -1, -1, 547, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 129, -1, -1, -1, -1, -1, -1, -1, -1, 547, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 174, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 88, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 275, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 175, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 627, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 176, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 177, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 550, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 178, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 527, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 141, -1, -1, -1, -1, -1, -1, -1, -1, 527, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 179, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 529, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 142, -1, -1, -1, -1, -1, -1, -1, -1, 529, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 180, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 531, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 143, -1, -1, -1, -1, -1, -1, -1, -1, 531, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 181, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 128, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1 },
			{ -1, -1, -1, -1, 903, -1, 182, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 551, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 183, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 552, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 184, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 539, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 144, -1, -1, -1, -1, -1, -1, -1, -1, 539, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 145, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 553, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 489, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 544, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 146, -1, -1, -1, -1, -1, -1, -1, -1, 544, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 554, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 555, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 547, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 129, -1, -1, -1, -1, -1, -1, -1, -1, 547, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 549, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 156, -1, -1, -1, -1, -1, -1, -1, -1, 549, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 551, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 157, -1, -1, -1, -1, -1, -1, -1, -1, 551, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 552, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 158, -1, -1, -1, -1, -1, -1, -1, -1, 552, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 559, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 554, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 159, -1, -1, -1, -1, -1, -1, -1, -1, 554, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 555, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 160, -1, -1, -1, -1, -1, -1, -1, -1, 555, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 547, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 557, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 25, 557, 557, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 557, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 560, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1, -1, 561, -1, 628, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 559, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 170, -1, -1, -1, -1, -1, -1, -1, -1, 559, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 562, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 563, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 565, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 566, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 629, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 567, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 568, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 570, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 570, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 571, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 570, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 186, 186, -1, -1, -1, 572, -1, -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, 186, 186, -1, -1, 186, -1, 186, 186, -1, -1, -1, -1, -1, 186, 570, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 573, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 186, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, 186, 186, -1, -1, 186, -1, 186, 186, -1, -1, -1, -1, -1, 186, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 574, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 575, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 575, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 576, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 575, -1, -1, -1, -1, 630, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 575, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 577, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 579, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 580, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 188, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ 1, 189, 189, 190, 191, 189, 191, 191, 191, 191, 191, 189, 191, 191, 191, 191, 191, 191, 189, 191, 192, 189, 189, 189, 189, 189, 191, 191, 191, 191, 191, 191, 191, 191, 191, 191, 191, 191, 193, 193, 191, 189, 189, 277, 189, 189, 189, 189, 189, 189, 194, 191, 193, 189, 195, 278, 193, 189, 189, 193, 189, 193, 193, 279, 280, 315, 189, 1, 193, 189, 315 },
			{ -1, -1, -1, -1, 584, -1, 584, 584, 584, 584, 584, -1, 584, 584, 584, 584, 584, 584, -1, 584, -1, -1, -1, -1, -1, -1, 584, 584, 584, 584, 584, 584, 584, 584, -1, 584, 584, 584, 584, 584, 584, -1, -1, 584, -1, -1, -1, -1, -1, -1, -1, 584, 584, -1, -1, 584, 584, -1, -1, 584, -1, 584, 584, -1, -1, -1, -1, -1, 584, 584, -1 },
			{ -1, -1, -1, -1, 584, -1, 584, 584, 584, 584, 584, -1, 584, 584, 584, 584, 584, 584, -1, 584, -1, -1, -1, -1, -1, -1, 584, 584, 584, 584, 584, 584, 584, 584, -1, 584, 584, 584, 584, 584, 584, -1, -1, 584, -1, -1, -1, -1, -1, -1, -1, 584, 584, -1, -1, 584, 584, -1, -1, 584, -1, 584, 584, -1, -1, 282, -1, -1, 584, 584, -1 },
			{ 1, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 210, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 586, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 1, 209, 209, 209 },
			{ -1, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, -1, 209, 209, 209 },
			{ 1, 211, 211, 212, 213, 211, 213, 213, 213, 213, 213, 211, 213, 213, 213, 213, 213, 213, 211, 213, 214, 211, 211, 211, 211, 211, 213, 213, 213, 213, 213, 213, 213, 213, 213, 213, 213, 213, 215, 215, 213, 211, 211, 286, 211, 211, 211, 211, 211, 211, 216, 213, 215, 211, 217, 287, 215, 211, 211, 215, 211, 215, 215, 288, 289, 317, 211, 1, 215, 211, 317 },
			{ -1, -1, -1, -1, 590, -1, 590, 590, 590, 590, 590, -1, 590, 590, 590, 590, 590, 590, -1, 590, -1, -1, -1, -1, -1, -1, 590, 590, 590, 590, 590, 590, 590, 590, -1, 590, 590, 590, 590, 590, 590, -1, -1, 590, -1, -1, -1, -1, -1, -1, -1, 590, 590, -1, -1, 590, 590, -1, -1, 590, -1, 590, 590, -1, -1, -1, -1, -1, 590, 590, -1 },
			{ -1, -1, -1, -1, 590, -1, 590, 590, 590, 590, 590, -1, 590, 590, 590, 590, 590, 590, -1, 590, -1, -1, -1, -1, -1, -1, 590, 590, 590, 590, 590, 590, 590, 590, -1, 590, 590, 590, 590, 590, 590, -1, -1, 590, -1, -1, -1, -1, -1, -1, -1, 590, 590, -1, -1, 590, 590, -1, -1, 590, -1, 590, 590, -1, -1, 291, -1, -1, 590, 590, -1 },
			{ 1, 231, 231, 232, 233, 231, 233, 233, 233, 233, 233, 231, 233, 233, 233, 233, 233, 233, 231, 233, 232, 296, 231, 231, 231, 231, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 234, 234, 233, 231, 231, 295, 231, 231, 231, 231, 231, 231, 235, 233, 234, 231, 236, 297, 234, 231, 231, 234, 231, 234, 234, 298, 299, 319, 231, 265, 234, 231, 319 },
			{ -1, -1, -1, -1, 593, -1, 593, 593, 593, 593, 593, 249, 593, 593, 593, 593, 593, 593, -1, 593, -1, -1, -1, -1, 305, -1, 593, 593, 593, 593, 593, 593, 593, 593, 593, 593, 593, 593, 593, 593, 593, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 593, 593, -1, -1, 593, 593, -1, -1, 593, -1, 593, 593, -1, -1, -1, 595, -1, 593, -1, -1 },
			{ -1, -1, -1, -1, 596, -1, 596, 596, 596, 596, 596, -1, 596, 596, 596, 596, 596, 596, -1, 596, -1, -1, -1, -1, -1, -1, 596, 596, 596, 596, 596, 596, 596, 596, -1, 596, 596, 596, 596, 596, 596, -1, -1, 596, -1, -1, -1, -1, -1, -1, -1, 596, 596, -1, -1, 596, 596, -1, -1, 596, -1, 596, 596, -1, -1, -1, -1, -1, 596, 596, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 249, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 305, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 596, -1, 596, 596, 596, 596, 596, -1, 596, 596, 596, 596, 596, 596, -1, 596, -1, -1, -1, -1, -1, -1, 596, 596, 596, 596, 596, 596, 596, 596, -1, 596, 596, 596, 596, 596, 596, -1, -1, 596, -1, -1, -1, -1, -1, -1, -1, 596, 596, -1, -1, 596, 596, -1, -1, 596, -1, 596, 596, -1, -1, 301, -1, -1, 596, 596, -1 },
			{ 1, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 299, 265, 299, 299, 299 },
			{ 1, 250, 250, 250, 251, 250, 251, 251, 251, 251, 251, 250, 251, 251, 251, 251, 251, 251, 250, 251, 250, 250, 250, 250, 250, 250, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 250, 250, 251, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 251, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 1, 250, 250, 250 },
			{ 1, 252, 252, 252, 253, 252, 253, 253, 253, 253, 253, 252, 253, 253, 253, 253, 253, 253, 252, 253, 252, 252, 252, 252, 252, 252, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 252, 252, 253, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 253, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 1, 252, 252, 252 },
			{ 1, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 255, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 1, 254, 254, 254 },
			{ 1, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 258, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 1, 257, 257, 257 },
			{ 1, 260, 261, 261, 261, 260, 261, 261, 261, 261, 261, 262, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 306, 261, 307, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 261, 1, 261, 261, 261 },
			{ -1, -1, -1, -1, 903, -1, 903, 331, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 812, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, -1, 283, -1, -1, -1, 283, -1, -1, -1, -1, -1, -1, -1, -1, 283, -1, 283, -1, -1, 283, -1, -1, -1, -1, -1, -1, 283, 283, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 283, -1, -1, 283, 283, -1, -1, 283, -1, 283, 283, -1, -1, -1, -1, -1, 283, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 284, 284, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 284, 284, -1, -1, -1, -1, 284, 284, -1, -1, -1, -1, -1, 284, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 292, -1, -1, -1, -1, -1, -1, 292, -1, -1, -1, 292, -1, -1, -1, -1, -1, -1, -1, -1, 292, -1, 292, -1, -1, 292, -1, -1, -1, -1, -1, -1, 292, 292, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 292, -1, -1, 292, 292, -1, -1, 292, -1, 292, 292, -1, -1, -1, -1, -1, 292, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 293, 293, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 293, 293, -1, -1, -1, -1, 293, 293, -1, -1, -1, -1, -1, 293, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 302, -1, -1, -1, -1, -1, -1, 302, -1, -1, -1, 302, -1, -1, -1, -1, -1, -1, -1, -1, 302, -1, 302, -1, -1, 302, -1, -1, -1, -1, -1, -1, 302, 302, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 302, -1, -1, 302, 302, -1, -1, 302, -1, 302, 302, -1, -1, -1, -1, -1, 302, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 303, 303, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 303, 303, -1, -1, -1, -1, 303, 303, -1, -1, -1, -1, -1, 303, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 332, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 373, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 379, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 616, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 467, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 471, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 632, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 621, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 483, -1, 483, 483, 483, 483, 483, -1, 483, 483, 483, 483, 483, 483, -1, 483, -1, -1, -1, -1, -1, -1, 483, 483, 483, 483, 483, 483, 483, 483, 483, 483, 483, 483, -1, -1, 483, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 483, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 521, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 517, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 625, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 513, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 549, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 544, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 557, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 564, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 569, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 578, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 649, 903, 903, 650, 337, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 515, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 340, -1, 903, 903, 903, 903, 903, -1, 903, 903, 848, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 346, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 659, 809, 903, 903, -1, 903, 660, 903, 903, 847, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 349, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 352, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 355, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 816, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 853, 903, 903, 903, -1, 903, 667, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 668, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 358, -1, 903, 903, 903, 903, 669, -1, 814, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 670, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 850, -1, 903, 903, 903, 903, 671, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 817, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 672, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 673, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 674, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 887, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 675, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 871, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 364, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 872, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 366, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 679, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 368, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 370, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 372, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 682, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 374, 903, 886, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 376, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 684, -1, 915, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 685, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 378, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 687, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 901, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 688, 903, 903, -1, 903, 903, 903, 903, 903, 689, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 690, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 380, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 691, -1, 692, 903, 903, 903, 693, -1, 694, 854, 820, 695, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 696, 903, 697, 903, 855, 903, 903, 903, 903, 903, 698, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 700, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 382, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 384, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 386, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 388, -1, 903, 903, 903, 903, 911, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 704, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 390, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 822, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 708, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 394, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 888, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 856, -1, 903, 903, 903, 903, 903, 709, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 396, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 398, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 400, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 402, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 714, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 404, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 715, -1, 903, 903, 406, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 876, 903, 716, 903, 717, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 408, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 889, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 823, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 410, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 412, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 414, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 416, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 418, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 898, 903, 903, 903, 903, 420, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 828, 722, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 859, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 861, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 826, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 878, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 422, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 877, 903, 903, -1, 903, 903, 903, 903, 903, 916, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 724, 903, 903, -1, 903, 903, 903, 903, 891, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 860, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 424, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 426, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 428, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 430, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 726, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 432, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 902, 903, 903, 903, 879, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 729, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 730, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 434, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 436, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 438, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 440, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 734, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 830, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 442, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 735, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 446, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 833, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 448, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 863, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 740, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 450, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 452, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 747, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 836, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 866, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 884, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 750, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 454, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 456, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 753, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 458, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 835, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 460, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 881, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 865, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 462, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 757, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 464, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 466, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 468, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 470, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 472, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 864, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 759, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 760, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 838, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 882, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 474, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 764, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 476, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 478, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 480, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 768, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 482, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 774, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 484, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 776, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 486, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 842, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 488, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 490, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 868, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 492, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 781, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 494, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 783, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 496, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 498, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 784, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 500, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 502, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 504, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 506, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 738, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 785, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 786, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 508, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 510, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 844, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 790, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 791, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 792, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 512, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 514, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 516, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 518, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 793, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 520, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 522, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 524, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 526, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 796, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 798, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 528, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 799, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 530, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 532, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 534, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 801, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 536, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 538, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 639, 640, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 641, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 604, -1, -1, -1, -1, -1, -1, 604, -1, -1, -1, 604, -1, -1, -1, -1, -1, -1, -1, -1, 604, -1, 604, -1, -1, 604, -1, -1, -1, -1, -1, -1, 604, 604, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 604, -1, -1, 604, 604, -1, -1, 604, -1, 604, 604, -1, -1, -1, -1, -1, 604, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 606, -1, -1, -1, -1, -1, -1, 606, -1, -1, -1, 606, -1, -1, -1, -1, -1, -1, -1, -1, 606, -1, 606, -1, -1, 606, -1, -1, -1, -1, -1, -1, 606, 606, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 606, -1, -1, 606, 606, -1, -1, 606, -1, 606, 606, -1, -1, -1, -1, -1, 606, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 608, -1, -1, -1, -1, -1, -1, 608, -1, -1, -1, 608, -1, -1, -1, -1, -1, -1, -1, -1, 608, -1, 608, -1, -1, 608, -1, -1, -1, -1, -1, -1, 608, 608, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 608, -1, -1, 608, 608, -1, -1, 608, -1, 608, 608, -1, -1, -1, -1, -1, 608, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 612, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 620, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 699, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 686, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 896, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 683, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 677, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 680, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 875, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 711, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 702, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 821, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 718, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 719, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 723, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 728, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 829, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 739, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 892, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 736, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 744, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 746, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 743, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 893, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 837, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 755, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 767, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 756, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 917, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 770, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 761, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 772, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 841, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 780, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 787, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 789, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 788, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 794, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 795, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 797, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 642, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 819, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 681, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 678, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 874, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 706, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 712, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 703, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 827, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 725, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 733, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 732, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 741, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 834, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 749, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 912, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 762, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 758, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 777, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 773, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 763, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 843, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 918, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 800, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 645, 903, 903, 903, -1, 903, 646, 903, 903, 647, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 710, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 713, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 705, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 890, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 727, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 831, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 748, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 745, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 832, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 766, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 771, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 779, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 778, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 765, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 648, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 825, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 857, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 731, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 737, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 752, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 862, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 754, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 769, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 651, 903, 903, 903, -1, 813, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 652, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 858, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 824, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 742, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 751, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 653, 903, 903, 903, 903, -1, 654, 903, 655, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 656, 903, 903, 903, 903, 903, 657, 903, 903, 811, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 721, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 880, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 662, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 665, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 808, 903, 903, 903, -1, 903, 666, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 803, -1, -1, -1, -1, -1, -1, 803, -1, -1, -1, 803, -1, -1, -1, -1, -1, -1, -1, -1, 803, -1, 803, -1, -1, 803, -1, -1, -1, -1, -1, -1, 803, 803, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 803, -1, -1, 803, 803, -1, -1, 803, -1, 803, 803, -1, -1, -1, -1, -1, 803, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 804, -1, -1, -1, -1, -1, -1, 804, -1, -1, -1, 804, -1, -1, -1, -1, -1, -1, -1, -1, 804, -1, 804, -1, -1, 804, -1, -1, -1, -1, -1, -1, 804, 804, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 804, -1, -1, 804, 804, -1, -1, 804, -1, 804, 804, -1, -1, -1, -1, -1, 804, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 805, -1, -1, -1, -1, -1, -1, 805, -1, -1, -1, 805, -1, -1, -1, -1, -1, -1, -1, -1, 805, -1, 805, -1, -1, 805, -1, -1, -1, -1, -1, -1, 805, 805, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 805, -1, -1, 805, 805, -1, -1, 805, -1, 805, 805, -1, -1, -1, -1, -1, 805, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 806, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 899, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 883, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 839, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 845, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 897, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 913, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 867, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, 903, -1, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 869, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 907, -1, -1, -1, -1, -1, -1, 907, -1, -1, -1, 907, -1, -1, -1, -1, -1, -1, -1, -1, 907, -1, 907, -1, -1, 907, -1, -1, -1, -1, -1, -1, 907, 907, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 907, -1, -1, 907, 907, -1, -1, 907, -1, 907, 907, -1, -1, -1, -1, -1, 907, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 908, -1, -1, -1, -1, -1, -1, 908, -1, -1, -1, 908, -1, -1, -1, -1, -1, -1, -1, -1, 908, -1, 908, -1, -1, 908, -1, -1, -1, -1, -1, -1, 908, 908, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 908, -1, -1, 908, 908, -1, -1, 908, -1, 908, 908, -1, -1, -1, -1, -1, 908, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 909, -1, -1, -1, -1, -1, -1, 909, -1, -1, -1, 909, -1, -1, -1, -1, -1, -1, -1, -1, 909, -1, 909, -1, -1, 909, -1, -1, -1, -1, -1, -1, 909, 909, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 909, -1, -1, 909, 909, -1, -1, 909, -1, 909, 909, -1, -1, -1, -1, -1, 909, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 919, -1, -1, -1, -1, -1, -1, 919, -1, -1, -1, 919, -1, -1, -1, -1, -1, -1, -1, -1, 919, -1, 919, -1, -1, 919, -1, -1, -1, -1, -1, -1, 919, 919, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 919, -1, -1, 919, 919, -1, -1, 919, -1, 919, 919, -1, -1, -1, -1, -1, 919, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 920, -1, -1, -1, -1, -1, -1, 920, -1, -1, -1, 920, -1, -1, -1, -1, -1, -1, -1, -1, 920, -1, 920, -1, -1, 920, -1, -1, -1, -1, -1, -1, 920, 920, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 920, -1, -1, 920, 920, -1, -1, 920, -1, 920, 920, -1, -1, -1, -1, -1, 920, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 921, -1, -1, -1, -1, -1, -1, 921, -1, -1, -1, 921, -1, -1, -1, -1, -1, -1, -1, -1, 921, -1, 921, -1, -1, 921, -1, -1, -1, -1, -1, -1, 921, 921, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 921, -1, -1, 921, 921, -1, -1, 921, -1, 921, 921, -1, -1, -1, -1, -1, 921, -1, -1 }
		};
		
		
		private static int[] yy_state_dtrans = new int[]
		{
			  0,
			  381,
			  581,
			  585,
			  587,
			  591,
			  597,
			  598,
			  599,
			  600,
			  601,
			  602
		};
		
		#endregion
		
		public Tokens GetNextToken()
		{
			int current_state = yy_state_dtrans[(int)current_lexical_state];
			int last_accept_state = NoState;
			bool is_initial_state = true;
			
			MarkTokenChunkStart();
			token_start = token_chunk_start;
			expanding_token = false;
			AdvanceEndPosition((token_end > 0) ? token_end - 1 : 0, token_start);
			
			// capture token start position:
			token_start_pos.Char = token_end_pos.Char;
			token_start_pos.Line = token_end_pos.Line;
			token_start_pos.Column = token_end_pos.Column;
			
			if (acceptCondition[current_state] != AcceptConditions.NotAccept)
			{
				last_accept_state = current_state;
				MarkTokenEnd();
			}
			
			while (true)
			{
				char lookahead = (is_initial_state && yy_at_bol) ? BOL : Advance();
				int next_state = nextState[rowMap[current_state], colMap[lookahead]];
				
				if (lookahead == EOF && is_initial_state)
				{
					return Tokens.EOF;
				}
				if (next_state != -1)
				{
					current_state = next_state;
					is_initial_state = false;
					
					if (acceptCondition[current_state] != AcceptConditions.NotAccept)
					{
						last_accept_state = current_state;
						MarkTokenEnd();
					}
				}
				else
				{
					if (last_accept_state == NoState)
					{
						return Tokens.ERROR;
					}
					else
					{
						if ((acceptCondition[last_accept_state] & AcceptConditions.AcceptOnEnd) != 0)
							TrimTokenEnd();
						MoveToTokenEnd();
						
						if (last_accept_state < 0)
						{
							System.Diagnostics.Debug.Assert(last_accept_state >= 925);
						}
						else
						{
							bool accepted = false;
							yyreturn = Accept0(last_accept_state, out accepted);
							if (accepted)
							{
								AdvanceEndPosition(token_start, token_end - 1);
								return yyreturn;
							}
						}
						
						// token ignored:
						is_initial_state = true;
						current_state = yy_state_dtrans[(int)current_lexical_state];
						last_accept_state = NoState;
						MarkTokenChunkStart();
						if (acceptCondition[current_state] != AcceptConditions.NotAccept)
						{
							last_accept_state = current_state;
							MarkTokenEnd();
						}
					}
				}
			}
		} // end of GetNextToken
	}
}

