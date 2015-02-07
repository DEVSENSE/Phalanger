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
			public Position(int ch)
			{
				this.Char = ch;
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
					// #line 75
					{ 
						return Tokens.T_INLINE_HTML; 
					}
					break;
					
				case 3:
					// #line 107
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
					// #line 79
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
					// #line 91
					{ 
						return Tokens.T_INLINE_HTML; 
					}
					break;
					
				case 6:
					// #line 95
					{
						if (GetTokenChar(1) != '%' || AllowAspTags) 
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
					
				case 7:
					// #line 119
					{
						BEGIN(LexicalStates.ST_IN_SCRIPTING);
						return Tokens.T_OPEN_TAG;
					}
					break;
					
				case 8:
					// #line 269
					{ return (Tokens)GetTokenChar(0); }
					break;
					
				case 9:
					// #line 345
					{ 
						BEGIN(LexicalStates.ST_BACKQUOTE); 
						return Tokens.T_BACKQUOTE; 
					}
					break;
					
				case 10:
					// #line 270
					{ return Tokens.T_STRING; }
					break;
					
				case 11:
					// #line 272
					{ return Tokens.T_WHITESPACE; }
					break;
					
				case 12:
					// #line 329
					{ 
						BEGIN(LexicalStates.ST_DOUBLE_QUOTES); 
						return (GetTokenChar(0) != '"') ? Tokens.T_BINARY_DOUBLE : Tokens.T_DOUBLE_QUOTES; 
					}
					break;
					
				case 13:
					// #line 335
					{ 
						// Gets here only in the case of unterminated singly-quoted string. That leads usually to an error token,
						// however when the source code is parsed per-line (as in Visual Studio colorizer) it is important to remember
						// that we are in the singly-quoted string at the end of the line.
						BEGIN(LexicalStates.ST_SINGLE_QUOTES); 
						yymore(); 
						break; 
					}
					break;
					
				case 14:
					// #line 273
					{ return Tokens.ParseDecimalNumber; }
					break;
					
				case 15:
					// #line 271
					{ return Tokens.T_NS_SEPARATOR; }
					break;
					
				case 16:
					// #line 284
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); yymore(); break; }
					break;
					
				case 17:
					// #line 307
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_LBRACE; }
					break;
					
				case 18:
					// #line 363
					{ return Tokens.ERROR; }
					break;
					
				case 19:
					// #line 308
					{ if (!yy_pop_state()) return Tokens.ERROR; return Tokens.T_RBRACE; }
					break;
					
				case 20:
					// #line 254
					{ return Tokens.T_MOD_EQUAL; }
					break;
					
				case 21:
					// #line 310
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
					
				case 22:
					// #line 262
					{ return Tokens.T_SL; }
					break;
					
				case 23:
					// #line 247
					{ return Tokens.T_IS_SMALLER_OR_EQUAL; }
					break;
					
				case 24:
					// #line 246
					{ return Tokens.T_IS_NOT_EQUAL; }
					break;
					
				case 25:
					// #line 221
					{ return Tokens.T_LGENERIC; }
					break;
					
				case 26:
					// #line 127
					{ 
						BEGIN(LexicalStates.INITIAL); 
						return Tokens.T_CLOSE_TAG; 
					}
					break;
					
				case 27:
					// #line 138
					{ return Tokens.T_IF; }
					break;
					
				case 28:
					// #line 151
					{ return Tokens.T_AS; }
					break;
					
				case 29:
					// #line 245
					{ return Tokens.T_IS_EQUAL; }
					break;
					
				case 30:
					// #line 240
					{ return Tokens.T_DOUBLE_ARROW; }
					break;
					
				case 31:
					// #line 323
					{ return Tokens.DoubleQuotedString; }
					break;
					
				case 32:
					// #line 324
					{ return Tokens.SingleQuotedString; }
					break;
					
				case 33:
					// #line 248
					{ return Tokens.T_IS_GREATER_OR_EQUAL; }
					break;
					
				case 34:
					// #line 263
					{ return Tokens.T_SR; }
					break;
					
				case 35:
					// #line 252
					{ return Tokens.T_DIV_EQUAL; }
					break;
					
				case 36:
					// #line 285
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); yymore(); break; }
					break;
					
				case 37:
					// #line 287
					{ BEGIN(LexicalStates.ST_COMMENT); yymore(); break; }
					break;
					
				case 38:
					// #line 144
					{ return Tokens.T_DO; }
					break;
					
				case 39:
					// #line 176
					{ return Tokens.T_LOGICAL_OR; }
					break;
					
				case 40:
					// #line 275
					{ return Tokens.ParseDouble; }
					break;
					
				case 41:
					// #line 222
					{ return Tokens.T_RGENERIC; }
					break;
					
				case 42:
					// #line 264
					{ return Tokens.T_DOUBLE_COLON; }
					break;
					
				case 43:
					// #line 249
					{ return Tokens.T_PLUS_EQUAL; }
					break;
					
				case 44:
					// #line 241
					{ return Tokens.T_INC; }
					break;
					
				case 45:
					// #line 250
					{ return Tokens.T_MINUS_EQUAL; }
					break;
					
				case 46:
					// #line 266
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 47:
					// #line 242
					{ return Tokens.T_DEC; }
					break;
					
				case 48:
					// #line 251
					{ return Tokens.T_MUL_EQUAL; }
					break;
					
				case 49:
					// #line 253
					{ return Tokens.T_CONCAT_EQUAL; }
					break;
					
				case 50:
					// #line 257
					{ return Tokens.T_AND_EQUAL; }
					break;
					
				case 51:
					// #line 261
					{ return Tokens.T_BOOLEAN_AND; }
					break;
					
				case 52:
					// #line 258
					{ return Tokens.T_OR_EQUAL; }
					break;
					
				case 53:
					// #line 260
					{ return Tokens.T_BOOLEAN_OR; }
					break;
					
				case 54:
					// #line 259
					{ return Tokens.T_XOR_EQUAL; }
					break;
					
				case 55:
					// #line 267
					{ return Tokens.T_VARIABLE; }
					break;
					
				case 56:
					// #line 255
					{ return Tokens.T_SL_EQUAL; }
					break;
					
				case 57:
					// #line 211
					{ return Tokens.T_INT_TYPE; }
					break;
					
				case 58:
					// #line 326
					{ return Tokens.ErrorInvalidIdentifier; }
					break;
					
				case 59:
					// #line 190
					{ return Tokens.T_TRY; }
					break;
					
				case 60:
					// #line 177
					{ return Tokens.T_LOGICAL_AND; }
					break;
					
				case 61:
					// #line 164
					{ return Tokens.T_NEW; }
					break;
					
				case 62:
					// #line 206
					{ return Tokens.T_USE; }
					break;
					
				case 63:
					// #line 243
					{ return Tokens.T_IS_IDENTICAL; }
					break;
					
				case 64:
					// #line 256
					{ return Tokens.T_SR_EQUAL; }
					break;
					
				case 65:
					// #line 178
					{ return Tokens.T_LOGICAL_XOR; }
					break;
					
				case 66:
					// #line 133
					{ return Tokens.T_EXIT; }
					break;
					
				case 67:
					// #line 145
					{ return Tokens.T_FOR; }
					break;
					
				case 68:
					// #line 165
					{ return Tokens.T_VAR; }
					break;
					
				case 69:
					// #line 276
					{ return Tokens.ParseDouble; }
					break;
					
				case 70:
					// #line 244
					{ return Tokens.T_IS_NOT_IDENTICAL; }
					break;
					
				case 71:
					// #line 274
					{ return Tokens.ParseHexadecimalNumber; }
					break;
					
				case 72:
					// #line 277
					{ return Tokens.ParseBinaryNumber; }
					break;
					
				case 73:
					// #line 234
					{ return Tokens.T_SELF; }
					break;
					
				case 74:
					// #line 154
					{ return Tokens.T_CASE; }
					break;
					
				case 75:
					// #line 325
					{ return Tokens.SingleQuotedIdentifier; }
					break;
					
				case 76:
					// #line 236
					{ return Tokens.T_TRUE; }
					break;
					
				case 77:
					// #line 179
					{ return Tokens.T_LIST; }
					break;
					
				case 78:
					// #line 238
					{ return Tokens.T_NULL; }
					break;
					
				case 79:
					// #line 208
					{ return Tokens.T_GOTO; }
					break;
					
				case 80:
					// #line 158
					{ return Tokens.T_ECHO; }
					break;
					
				case 81:
					// #line 141
					{ return Tokens.T_ELSE; }
					break;
					
				case 82:
					// #line 132
					{ return Tokens.T_EXIT; }
					break;
					
				case 83:
					// #line 166
					{ return Tokens.T_EVAL; }
					break;
					
				case 84:
					// #line 286
					{ BEGIN(LexicalStates.ST_DOC_COMMENT); yymore(); break; }
					break;
					
				case 85:
					// #line 210
					{ return Tokens.T_BOOL_TYPE; }
					break;
					
				case 86:
					// #line 350
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
					
				case 87:
					// #line 160
					{ return Tokens.T_CLASS; }
					break;
					
				case 88:
					// #line 196
					{ return Tokens.T_CLONE; }
					break;
					
				case 89:
					// #line 191
					{ return Tokens.T_CATCH; }
					break;
					
				case 90:
					// #line 135
					{ return Tokens.T_CONST; }
					break;
					
				case 91:
					// #line 172
					{ return Tokens.T_ISSET; }
					break;
					
				case 92:
					// #line 212
					{ return Tokens.T_INT64_TYPE; }
					break;
					
				case 93:
					// #line 159
					{ return Tokens.T_PRINT; }
					break;
					
				case 94:
					// #line 161
					{ return Tokens.T_TRAIT; }
					break;
					
				case 95:
					// #line 193
					{ return Tokens.T_THROW; }
					break;
					
				case 96:
					// #line 180
					{ return Tokens.T_ARRAY; }
					break;
					
				case 97:
					// #line 175
					{ return Tokens.T_UNSET; }
					break;
					
				case 98:
					// #line 140
					{ return Tokens.T_ENDIF; }
					break;
					
				case 99:
					// #line 173
					{ return Tokens.T_EMPTY; }
					break;
					
				case 100:
					// #line 198
					{ return Tokens.T_FINAL; }
					break;
					
				case 101:
					// #line 237
					{ return Tokens.T_FALSE; }
					break;
					
				case 102:
					// #line 137
					{ return Tokens.T_YIELD; }
					break;
					
				case 103:
					// #line 142
					{ return Tokens.T_WHILE; }
					break;
					
				case 104:
					// #line 156
					{ return Tokens.T_BREAK; }
					break;
					
				case 105:
					// #line 225
					{ return Tokens.T_SET; }
					break;
					
				case 106:
					// #line 224
					{ return Tokens.T_GET; }
					break;
					
				case 107:
					// #line 291
					{ return Tokens.T_INT32_CAST; }
					break;
					
				case 108:
					// #line 214
					{ return Tokens.T_STRING_TYPE; }
					break;
					
				case 109:
					// #line 174
					{ return Tokens.T_STATIC; }
					break;
					
				case 110:
					// #line 152
					{ return Tokens.T_SWITCH; }
					break;
					
				case 111:
					// #line 136
					{ return Tokens.T_RETURN; }
					break;
					
				case 112:
					// #line 207
					{ return Tokens.T_IMPORT; }
					break;
					
				case 113:
					// #line 233
					{ return Tokens.T_PARENT; }
					break;
					
				case 114:
					// #line 201
					{ return Tokens.T_PUBLIC; }
					break;
					
				case 115:
					// #line 171
					{ return Tokens.T_GLOBAL; }
					break;
					
				case 116:
					// #line 139
					{ return Tokens.T_ELSEIF; }
					break;
					
				case 117:
					// #line 146
					{ return Tokens.T_ENDFOR; }
					break;
					
				case 118:
					// #line 213
					{ return Tokens.T_DOUBLE_TYPE; }
					break;
					
				case 119:
					// #line 216
					{ return Tokens.T_OBJECT_TYPE; }
					break;
					
				case 120:
					// #line 226
					{ return Tokens.T_CALL; }
					break;
					
				case 121:
					// #line 297
					{ return Tokens.T_DOUBLE_CAST; }
					break;
					
				case 122:
					// #line 289
					{ return Tokens.T_INT8_CAST; }
					break;
					
				case 123:
					// #line 295
					{ return Tokens.T_UINT32_CAST; }
					break;
					
				case 124:
					// #line 304
					{ return Tokens.T_BOOL_CAST; }
					break;
					
				case 125:
					// #line 169
					{ return Tokens.T_REQUIRE; }
					break;
					
				case 126:
					// #line 167
					{ return Tokens.T_INCLUDE; }
					break;
					
				case 127:
					// #line 199
					{ return Tokens.T_PRIVATE; }
					break;
					
				case 128:
					// #line 219
					{ return Tokens.T_PARTIAL; }
					break;
					
				case 129:
					// #line 163
					{ return Tokens.T_EXTENDS; }
					break;
					
				case 130:
					// #line 149
					{ return Tokens.T_DECLARE; }
					break;
					
				case 131:
					// #line 155
					{ return Tokens.T_DEFAULT; }
					break;
					
				case 132:
					// #line 192
					{ return Tokens.T_FINALLY; }
					break;
					
				case 133:
					// #line 147
					{ return Tokens.T_FOREACH; }
					break;
					
				case 134:
					// #line 232
					{ return Tokens.T_SLEEP; }
					break;
					
				case 135:
					// #line 188
					{ return Tokens.T_DIR; }
					break;
					
				case 136:
					// #line 292
					{ return Tokens.T_INT64_CAST; }
					break;
					
				case 137:
					// #line 290
					{ return Tokens.T_INT16_CAST; }
					break;
					
				case 138:
					// #line 302
					{ return Tokens.T_ARRAY_CAST; }
					break;
					
				case 139:
					// #line 293
					{ return Tokens.T_UINT8_CAST; }
					break;
					
				case 140:
					// #line 305
					{ return Tokens.T_UNSET_CAST; }
					break;
					
				case 141:
					// #line 298
					{ return Tokens.T_FLOAT_CAST; }
					break;
					
				case 142:
					// #line 181
					{ return Tokens.T_CALLABLE; }
					break;
					
				case 143:
					// #line 157
					{ return Tokens.T_CONTINUE; }
					break;
					
				case 144:
					// #line 215
					{ return Tokens.T_RESOURCE_TYPE; }
					break;
					
				case 145:
					// #line 197
					{ return Tokens.T_ABSTRACT; }
					break;
					
				case 146:
					// #line 143
					{ return Tokens.T_ENDWHILE; }
					break;
					
				case 147:
					// #line 134
					{ return Tokens.T_FUNCTION; }
					break;
					
				case 148:
					// #line 186
					{ return Tokens.T_LINE; }
					break;
					
				case 149:
					// #line 187
					{ return Tokens.T_FILE; }
					break;
					
				case 150:
					// #line 231
					{ return Tokens.T_WAKEUP; }
					break;
					
				case 151:
					// #line 299
					{ return Tokens.T_STRING_CAST; }
					break;
					
				case 152:
					// #line 296
					{ return Tokens.T_UINT64_CAST; }
					break;
					
				case 153:
					// #line 294
					{ return Tokens.T_UINT16_CAST; }
					break;
					
				case 154:
					// #line 303
					{ return Tokens.T_OBJECT_CAST; }
					break;
					
				case 155:
					// #line 300
					{ return Tokens.T_BINARY_CAST; }
					break;
					
				case 156:
					// #line 217
					{ return Tokens.T_TYPEOF; }
					break;
					
				case 157:
					// #line 162
					{ return Tokens.T_INSTEADOF; }
					break;
					
				case 158:
					// #line 194
					{ return Tokens.T_INTERFACE; }
					break;
					
				case 159:
					// #line 200
					{ return Tokens.T_PROTECTED; }
					break;
					
				case 160:
					// #line 205
					{ return Tokens.T_NAMESPACE; }
					break;
					
				case 161:
					// #line 153
					{ return Tokens.T_ENDSWITCH; }
					break;
					
				case 162:
					// #line 182
					{ return Tokens.T_CLASS_C; }
					break;
					
				case 163:
					// #line 183
					{ return Tokens.T_TRAIT_C; }
					break;
					
				case 164:
					// #line 301
					{ return Tokens.T_UNICODE_CAST; }
					break;
					
				case 165:
					// #line 202
					{ return Tokens.T_INSTANCEOF; }
					break;
					
				case 166:
					// #line 195
					{ return Tokens.T_IMPLEMENTS; }
					break;
					
				case 167:
					// #line 150
					{ return Tokens.T_ENDDECLARE; }
					break;
					
				case 168:
					// #line 148
					{ return Tokens.T_ENDFOREACH; }
					break;
					
				case 169:
					// #line 228
					{ return Tokens.T_TOSTRING; }
					break;
					
				case 170:
					// #line 235
					{ return Tokens.T_AUTOLOAD; }
					break;
					
				case 171:
					// #line 230
					{ return Tokens.T_DESTRUCT; }
					break;
					
				case 172:
					// #line 185
					{ return Tokens.T_METHOD_C; }
					break;
					
				case 173:
					// #line 229
					{ return Tokens.T_CONSTRUCT; }
					break;
					
				case 174:
					// #line 170
					{ return Tokens.T_REQUIRE_ONCE; }
					break;
					
				case 175:
					// #line 168
					{ return Tokens.T_INCLUDE_ONCE; }
					break;
					
				case 176:
					// #line 227
					{ return Tokens.T_CALLSTATIC; }
					break;
					
				case 177:
					// #line 184
					{ return Tokens.T_FUNC_C; }
					break;
					
				case 178:
					// #line 204
					{ return Tokens.T_NAMESPACE_C; }
					break;
					
				case 179:
					// #line 280
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_FILE; }
					break;
					
				case 180:
					// #line 279
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_LINE; }
					break;
					
				case 181:
					// #line 281
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_DEFAULT_LINE; }
					break;
					
				case 182:
					// #line 282
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_DEFAULT_FILE; }
					break;
					
				case 183:
					// #line 489
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 184:
					// #line 481
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 185:
					// #line 472
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 186:
					// #line 482
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_DOUBLE_QUOTES; }
					break;
					
				case 187:
					// #line 471
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 188:
					// #line 488
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 189:
					// #line 490
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 190:
					// #line 486
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 191:
					// #line 485
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 192:
					// #line 483
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 193:
					// #line 484
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 194:
					// #line 480
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 195:
					// #line 476
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 196:
					// #line 478
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 197:
					// #line 475
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 198:
					// #line 477
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 199:
					// #line 473
					{ return Tokens.OctalCharCode; }
					break;
					
				case 200:
					// #line 479
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 201:
					// #line 487
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 202:
					// #line 474
					{ return Tokens.HexCharCode; }
					break;
					
				case 203:
					// #line 431
					{ yymore(); break; }
					break;
					
				case 204:
					// #line 432
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.SingleQuotedString; }
					break;
					
				case 205:
					// #line 512
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 206:
					// #line 505
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_BACKQUOTE; }
					break;
					
				case 207:
					// #line 495
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 208:
					// #line 504
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 209:
					// #line 494
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 210:
					// #line 510
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 211:
					// #line 513
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 212:
					// #line 509
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 213:
					// #line 508
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 214:
					// #line 506
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 215:
					// #line 507
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 216:
					// #line 503
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 217:
					// #line 500
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 218:
					// #line 499
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 219:
					// #line 501
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 220:
					// #line 498
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 221:
					// #line 496
					{ return Tokens.OctalCharCode; }
					break;
					
				case 222:
					// #line 502
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 223:
					// #line 511
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 224:
					// #line 497
					{ return Tokens.HexCharCode; }
					break;
					
				case 225:
					// #line 467
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 226:
					// #line 460
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 227:
					// #line 452
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 228:
					// #line 451
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 229:
					// #line 465
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 230:
					// #line 468
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 231:
					// #line 464
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 232:
					// #line 463
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 233:
					// #line 461
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 234:
					// #line 462
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 235:
					// #line 459
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 236:
					// #line 456
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 237:
					// #line 457
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 238:
					// #line 455
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 239:
					// #line 453
					{ return Tokens.OctalCharCode; }
					break;
					
				case 240:
					// #line 458
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 241:
					// #line 466
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 242:
					// #line 454
					{ return Tokens.HexCharCode; }
					break;
					
				case 243:
					// #line 436
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
					
				case 244:
					// #line 376
					{
						yyless(0);
						if (!yy_pop_state()) return Tokens.ERROR;
						break;
					}
					break;
					
				case 245:
					// #line 369
					{
						if (!yy_pop_state()) return Tokens.ERROR;
						inString = (CurrentLexicalState != LexicalStates.ST_IN_SCRIPTING); 
						isCode = true;
						return Tokens.T_STRING;
					}
					break;
					
				case 246:
					// #line 390
					{
						yyless(0);
						if (!yy_pop_state()) return Tokens.ERROR;
						yy_push_state(LexicalStates.ST_IN_SCRIPTING);
						break;
					}
					break;
					
				case 247:
					// #line 384
					{
						if (!yy_pop_state()) return Tokens.ERROR;
						yy_push_state(LexicalStates.ST_IN_SCRIPTING);
						return Tokens.T_STRING_VARNAME;
					}
					break;
					
				case 248:
					// #line 425
					{ yymore(); break; }
					break;
					
				case 249:
					// #line 427
					{ yymore(); break; }
					break;
					
				case 250:
					// #line 426
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_DOC_COMMENT; }
					break;
					
				case 251:
					// #line 419
					{ yymore(); break; }
					break;
					
				case 252:
					// #line 421
					{ yymore(); break; }
					break;
					
				case 253:
					// #line 420
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_COMMENT; }
					break;
					
				case 254:
					// #line 399
					{ yymore(); break; }
					break;
					
				case 255:
					// #line 400
					{ yymore(); break; }
					break;
					
				case 256:
					// #line 401
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_LINE_COMMENT; }
					break;
					
				case 257:
					// #line 403
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
					
				case 260: goto case 2;
				case 261: goto case 4;
				case 262: goto case 5;
				case 263: goto case 7;
				case 264: goto case 8;
				case 265: goto case 10;
				case 266: goto case 14;
				case 267: goto case 21;
				case 268: goto case 24;
				case 269: goto case 26;
				case 270: goto case 86;
				case 271: goto case 180;
				case 272: goto case 183;
				case 273: goto case 187;
				case 274: goto case 188;
				case 275: goto case 189;
				case 276: goto case 194;
				case 277: goto case 195;
				case 278: goto case 197;
				case 279: goto case 199;
				case 280: goto case 202;
				case 281: goto case 205;
				case 282: goto case 209;
				case 283: goto case 210;
				case 284: goto case 211;
				case 285: goto case 216;
				case 286: goto case 218;
				case 287: goto case 220;
				case 288: goto case 221;
				case 289: goto case 224;
				case 290: goto case 225;
				case 291: goto case 226;
				case 292: goto case 228;
				case 293: goto case 229;
				case 294: goto case 230;
				case 295: goto case 235;
				case 296: goto case 236;
				case 297: goto case 238;
				case 298: goto case 239;
				case 299: goto case 242;
				case 300: goto case 243;
				case 301: goto case 254;
				case 302: goto case 256;
				case 304: goto case 8;
				case 305: goto case 10;
				case 306: goto case 21;
				case 307: goto case 26;
				case 308: goto case 187;
				case 309: goto case 188;
				case 310: goto case 209;
				case 311: goto case 210;
				case 312: goto case 228;
				case 313: goto case 229;
				case 315: goto case 8;
				case 316: goto case 10;
				case 318: goto case 8;
				case 319: goto case 10;
				case 321: goto case 8;
				case 322: goto case 10;
				case 324: goto case 8;
				case 325: goto case 10;
				case 327: goto case 8;
				case 328: goto case 10;
				case 330: goto case 8;
				case 331: goto case 10;
				case 333: goto case 8;
				case 334: goto case 10;
				case 336: goto case 8;
				case 337: goto case 10;
				case 339: goto case 8;
				case 340: goto case 10;
				case 342: goto case 8;
				case 343: goto case 10;
				case 345: goto case 8;
				case 346: goto case 10;
				case 348: goto case 8;
				case 349: goto case 10;
				case 351: goto case 8;
				case 352: goto case 10;
				case 354: goto case 8;
				case 355: goto case 10;
				case 357: goto case 8;
				case 358: goto case 10;
				case 360: goto case 10;
				case 362: goto case 10;
				case 364: goto case 10;
				case 366: goto case 10;
				case 368: goto case 10;
				case 370: goto case 10;
				case 372: goto case 10;
				case 374: goto case 10;
				case 376: goto case 10;
				case 378: goto case 10;
				case 380: goto case 10;
				case 382: goto case 10;
				case 384: goto case 10;
				case 386: goto case 10;
				case 388: goto case 10;
				case 390: goto case 10;
				case 392: goto case 10;
				case 394: goto case 10;
				case 396: goto case 10;
				case 398: goto case 10;
				case 400: goto case 10;
				case 402: goto case 10;
				case 404: goto case 10;
				case 406: goto case 10;
				case 408: goto case 10;
				case 410: goto case 10;
				case 412: goto case 10;
				case 414: goto case 10;
				case 416: goto case 10;
				case 418: goto case 10;
				case 420: goto case 10;
				case 422: goto case 10;
				case 424: goto case 10;
				case 426: goto case 10;
				case 428: goto case 10;
				case 430: goto case 10;
				case 432: goto case 10;
				case 434: goto case 10;
				case 436: goto case 10;
				case 438: goto case 10;
				case 440: goto case 10;
				case 442: goto case 10;
				case 444: goto case 10;
				case 446: goto case 10;
				case 448: goto case 10;
				case 450: goto case 10;
				case 452: goto case 10;
				case 454: goto case 10;
				case 456: goto case 10;
				case 458: goto case 10;
				case 460: goto case 10;
				case 462: goto case 10;
				case 464: goto case 10;
				case 466: goto case 10;
				case 468: goto case 10;
				case 470: goto case 10;
				case 472: goto case 10;
				case 474: goto case 10;
				case 476: goto case 10;
				case 478: goto case 10;
				case 480: goto case 10;
				case 482: goto case 10;
				case 484: goto case 10;
				case 486: goto case 10;
				case 488: goto case 10;
				case 490: goto case 10;
				case 492: goto case 10;
				case 494: goto case 10;
				case 496: goto case 10;
				case 498: goto case 10;
				case 500: goto case 10;
				case 502: goto case 10;
				case 504: goto case 10;
				case 506: goto case 10;
				case 508: goto case 10;
				case 510: goto case 10;
				case 512: goto case 10;
				case 514: goto case 10;
				case 516: goto case 10;
				case 518: goto case 10;
				case 520: goto case 10;
				case 522: goto case 10;
				case 524: goto case 10;
				case 589: goto case 5;
				case 590: goto case 10;
				case 591: goto case 197;
				case 592: goto case 199;
				case 593: goto case 220;
				case 594: goto case 221;
				case 595: goto case 238;
				case 596: goto case 239;
				case 617: goto case 10;
				case 619: goto case 10;
				case 620: goto case 10;
				case 621: goto case 10;
				case 622: goto case 10;
				case 623: goto case 10;
				case 624: goto case 10;
				case 625: goto case 10;
				case 626: goto case 10;
				case 627: goto case 10;
				case 628: goto case 10;
				case 629: goto case 10;
				case 630: goto case 10;
				case 631: goto case 10;
				case 632: goto case 10;
				case 633: goto case 10;
				case 634: goto case 10;
				case 635: goto case 10;
				case 636: goto case 10;
				case 637: goto case 10;
				case 638: goto case 10;
				case 639: goto case 10;
				case 640: goto case 10;
				case 641: goto case 10;
				case 642: goto case 10;
				case 643: goto case 10;
				case 644: goto case 10;
				case 645: goto case 10;
				case 646: goto case 10;
				case 647: goto case 10;
				case 648: goto case 10;
				case 649: goto case 10;
				case 650: goto case 10;
				case 651: goto case 10;
				case 652: goto case 10;
				case 653: goto case 10;
				case 654: goto case 10;
				case 655: goto case 10;
				case 656: goto case 10;
				case 657: goto case 10;
				case 658: goto case 10;
				case 659: goto case 10;
				case 660: goto case 10;
				case 661: goto case 10;
				case 662: goto case 10;
				case 663: goto case 10;
				case 664: goto case 10;
				case 665: goto case 10;
				case 666: goto case 10;
				case 667: goto case 10;
				case 668: goto case 10;
				case 669: goto case 10;
				case 670: goto case 10;
				case 671: goto case 10;
				case 672: goto case 10;
				case 673: goto case 10;
				case 674: goto case 10;
				case 675: goto case 10;
				case 676: goto case 10;
				case 677: goto case 10;
				case 678: goto case 10;
				case 679: goto case 10;
				case 680: goto case 10;
				case 681: goto case 10;
				case 682: goto case 10;
				case 683: goto case 10;
				case 684: goto case 10;
				case 685: goto case 10;
				case 686: goto case 10;
				case 687: goto case 10;
				case 688: goto case 10;
				case 689: goto case 10;
				case 690: goto case 10;
				case 691: goto case 10;
				case 692: goto case 10;
				case 693: goto case 10;
				case 694: goto case 10;
				case 695: goto case 10;
				case 696: goto case 10;
				case 697: goto case 10;
				case 698: goto case 10;
				case 699: goto case 10;
				case 700: goto case 10;
				case 701: goto case 10;
				case 702: goto case 10;
				case 703: goto case 10;
				case 704: goto case 10;
				case 705: goto case 10;
				case 706: goto case 10;
				case 707: goto case 10;
				case 708: goto case 10;
				case 709: goto case 10;
				case 710: goto case 10;
				case 711: goto case 10;
				case 712: goto case 10;
				case 713: goto case 10;
				case 714: goto case 10;
				case 715: goto case 10;
				case 716: goto case 10;
				case 717: goto case 10;
				case 718: goto case 10;
				case 719: goto case 10;
				case 720: goto case 10;
				case 721: goto case 10;
				case 722: goto case 10;
				case 723: goto case 10;
				case 724: goto case 10;
				case 725: goto case 10;
				case 726: goto case 10;
				case 727: goto case 10;
				case 728: goto case 10;
				case 729: goto case 10;
				case 730: goto case 10;
				case 731: goto case 10;
				case 732: goto case 10;
				case 733: goto case 10;
				case 734: goto case 10;
				case 735: goto case 10;
				case 736: goto case 10;
				case 737: goto case 10;
				case 738: goto case 10;
				case 739: goto case 10;
				case 740: goto case 10;
				case 741: goto case 10;
				case 742: goto case 10;
				case 743: goto case 10;
				case 744: goto case 10;
				case 745: goto case 10;
				case 746: goto case 10;
				case 747: goto case 10;
				case 748: goto case 10;
				case 749: goto case 10;
				case 750: goto case 10;
				case 751: goto case 10;
				case 752: goto case 10;
				case 753: goto case 10;
				case 754: goto case 10;
				case 755: goto case 10;
				case 756: goto case 10;
				case 757: goto case 10;
				case 758: goto case 10;
				case 759: goto case 10;
				case 760: goto case 10;
				case 761: goto case 10;
				case 762: goto case 10;
				case 763: goto case 10;
				case 764: goto case 10;
				case 765: goto case 10;
				case 766: goto case 10;
				case 767: goto case 10;
				case 768: goto case 10;
				case 769: goto case 10;
				case 770: goto case 10;
				case 771: goto case 10;
				case 772: goto case 10;
				case 773: goto case 10;
				case 774: goto case 10;
				case 775: goto case 10;
				case 776: goto case 10;
				case 777: goto case 10;
				case 778: goto case 5;
				case 779: goto case 10;
				case 780: goto case 197;
				case 781: goto case 220;
				case 782: goto case 238;
				case 785: goto case 10;
				case 786: goto case 10;
				case 787: goto case 10;
				case 788: goto case 10;
				case 789: goto case 10;
				case 790: goto case 10;
				case 791: goto case 10;
				case 792: goto case 10;
				case 793: goto case 10;
				case 794: goto case 10;
				case 795: goto case 10;
				case 796: goto case 10;
				case 797: goto case 10;
				case 798: goto case 10;
				case 799: goto case 10;
				case 800: goto case 10;
				case 801: goto case 10;
				case 802: goto case 10;
				case 803: goto case 10;
				case 804: goto case 10;
				case 805: goto case 10;
				case 806: goto case 10;
				case 807: goto case 10;
				case 808: goto case 10;
				case 809: goto case 10;
				case 810: goto case 10;
				case 811: goto case 10;
				case 812: goto case 10;
				case 813: goto case 10;
				case 814: goto case 10;
				case 815: goto case 10;
				case 816: goto case 10;
				case 817: goto case 10;
				case 818: goto case 10;
				case 819: goto case 10;
				case 820: goto case 10;
				case 821: goto case 10;
				case 822: goto case 10;
				case 823: goto case 10;
				case 824: goto case 10;
				case 825: goto case 10;
				case 826: goto case 10;
				case 827: goto case 10;
				case 828: goto case 10;
				case 829: goto case 10;
				case 830: goto case 10;
				case 831: goto case 10;
				case 832: goto case 10;
				case 833: goto case 10;
				case 834: goto case 10;
				case 835: goto case 10;
				case 836: goto case 10;
				case 837: goto case 10;
				case 838: goto case 10;
				case 839: goto case 10;
				case 840: goto case 10;
				case 841: goto case 10;
				case 842: goto case 10;
				case 843: goto case 10;
				case 844: goto case 10;
				case 845: goto case 10;
				case 846: goto case 10;
				case 847: goto case 10;
				case 848: goto case 10;
				case 849: goto case 10;
				case 850: goto case 10;
				case 851: goto case 10;
				case 852: goto case 10;
				case 853: goto case 10;
				case 854: goto case 10;
				case 855: goto case 10;
				case 856: goto case 10;
				case 857: goto case 10;
				case 858: goto case 10;
				case 859: goto case 10;
				case 860: goto case 10;
				case 861: goto case 10;
				case 862: goto case 10;
				case 863: goto case 10;
				case 864: goto case 10;
				case 865: goto case 10;
				case 866: goto case 10;
				case 867: goto case 10;
				case 868: goto case 10;
				case 869: goto case 10;
				case 870: goto case 10;
				case 871: goto case 10;
				case 872: goto case 10;
				case 873: goto case 10;
				case 874: goto case 10;
				case 875: goto case 10;
				case 876: goto case 10;
				case 877: goto case 5;
				case 878: goto case 197;
				case 879: goto case 220;
				case 880: goto case 238;
				case 882: goto case 10;
				case 883: goto case 10;
				case 884: goto case 10;
				case 885: goto case 10;
				case 886: goto case 10;
				case 887: goto case 10;
				case 888: goto case 10;
				case 889: goto case 10;
				case 890: goto case 5;
				case 891: goto case 197;
				case 892: goto case 220;
				case 893: goto case 238;
				case 894: goto case 5;
				case 895: goto case 197;
				case 896: goto case 220;
				case 897: goto case 238;
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
			token_end_pos.Char += to - from;
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
			AcceptConditions.AcceptOnStart, // 243
			AcceptConditions.Accept, // 244
			AcceptConditions.Accept, // 245
			AcceptConditions.Accept, // 246
			AcceptConditions.Accept, // 247
			AcceptConditions.Accept, // 248
			AcceptConditions.Accept, // 249
			AcceptConditions.Accept, // 250
			AcceptConditions.Accept, // 251
			AcceptConditions.Accept, // 252
			AcceptConditions.Accept, // 253
			AcceptConditions.Accept, // 254
			AcceptConditions.Accept, // 255
			AcceptConditions.Accept, // 256
			AcceptConditions.Accept, // 257
			AcceptConditions.NotAccept, // 258
			AcceptConditions.Accept, // 259
			AcceptConditions.Accept, // 260
			AcceptConditions.Accept, // 261
			AcceptConditions.Accept, // 262
			AcceptConditions.Accept, // 263
			AcceptConditions.Accept, // 264
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
			AcceptConditions.AcceptOnStart, // 300
			AcceptConditions.Accept, // 301
			AcceptConditions.Accept, // 302
			AcceptConditions.NotAccept, // 303
			AcceptConditions.Accept, // 304
			AcceptConditions.Accept, // 305
			AcceptConditions.Accept, // 306
			AcceptConditions.Accept, // 307
			AcceptConditions.Accept, // 308
			AcceptConditions.Accept, // 309
			AcceptConditions.Accept, // 310
			AcceptConditions.Accept, // 311
			AcceptConditions.Accept, // 312
			AcceptConditions.Accept, // 313
			AcceptConditions.NotAccept, // 314
			AcceptConditions.Accept, // 315
			AcceptConditions.Accept, // 316
			AcceptConditions.NotAccept, // 317
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
			AcceptConditions.NotAccept, // 361
			AcceptConditions.Accept, // 362
			AcceptConditions.NotAccept, // 363
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
			AcceptConditions.NotAccept, // 526
			AcceptConditions.NotAccept, // 527
			AcceptConditions.NotAccept, // 528
			AcceptConditions.NotAccept, // 529
			AcceptConditions.NotAccept, // 530
			AcceptConditions.NotAccept, // 531
			AcceptConditions.NotAccept, // 532
			AcceptConditions.NotAccept, // 533
			AcceptConditions.NotAccept, // 534
			AcceptConditions.NotAccept, // 535
			AcceptConditions.NotAccept, // 536
			AcceptConditions.NotAccept, // 537
			AcceptConditions.NotAccept, // 538
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
			AcceptConditions.Accept, // 589
			AcceptConditions.Accept, // 590
			AcceptConditions.Accept, // 591
			AcceptConditions.Accept, // 592
			AcceptConditions.Accept, // 593
			AcceptConditions.Accept, // 594
			AcceptConditions.Accept, // 595
			AcceptConditions.Accept, // 596
			AcceptConditions.NotAccept, // 597
			AcceptConditions.NotAccept, // 598
			AcceptConditions.NotAccept, // 599
			AcceptConditions.NotAccept, // 600
			AcceptConditions.NotAccept, // 601
			AcceptConditions.NotAccept, // 602
			AcceptConditions.NotAccept, // 603
			AcceptConditions.NotAccept, // 604
			AcceptConditions.NotAccept, // 605
			AcceptConditions.NotAccept, // 606
			AcceptConditions.NotAccept, // 607
			AcceptConditions.NotAccept, // 608
			AcceptConditions.NotAccept, // 609
			AcceptConditions.NotAccept, // 610
			AcceptConditions.NotAccept, // 611
			AcceptConditions.NotAccept, // 612
			AcceptConditions.NotAccept, // 613
			AcceptConditions.NotAccept, // 614
			AcceptConditions.NotAccept, // 615
			AcceptConditions.NotAccept, // 616
			AcceptConditions.Accept, // 617
			AcceptConditions.NotAccept, // 618
			AcceptConditions.Accept, // 619
			AcceptConditions.Accept, // 620
			AcceptConditions.Accept, // 621
			AcceptConditions.Accept, // 622
			AcceptConditions.Accept, // 623
			AcceptConditions.Accept, // 624
			AcceptConditions.Accept, // 625
			AcceptConditions.Accept, // 626
			AcceptConditions.Accept, // 627
			AcceptConditions.Accept, // 628
			AcceptConditions.Accept, // 629
			AcceptConditions.Accept, // 630
			AcceptConditions.Accept, // 631
			AcceptConditions.Accept, // 632
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
			AcceptConditions.NotAccept, // 783
			AcceptConditions.NotAccept, // 784
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
			AcceptConditions.Accept, // 806
			AcceptConditions.Accept, // 807
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
			AcceptConditions.NotAccept, // 881
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
		};
		
		private static int[] colMap = new int[]
		{
			64, 64, 64, 64, 64, 64, 64, 64, 64, 23, 11, 64, 64, 24, 64, 64, 
			64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 
			69, 44, 20, 57, 50, 1, 47, 21, 58, 60, 45, 42, 53, 43, 46, 25, 
			55, 56, 62, 61, 39, 68, 38, 68, 59, 52, 41, 66, 2, 18, 22, 4, 
			53, 13, 32, 6, 27, 17, 28, 15, 19, 8, 40, 33, 12, 37, 14, 29, 
			9, 36, 7, 5, 10, 16, 34, 31, 26, 30, 51, 70, 54, 70, 49, 35, 
			3, 13, 32, 6, 27, 17, 28, 15, 19, 8, 40, 33, 12, 37, 14, 29, 
			9, 36, 7, 5, 10, 16, 34, 31, 26, 30, 51, 63, 48, 65, 53, 64, 
			67, 0
		};
		
		private static int[] rowMap = new int[]
		{
			0, 1, 2, 3, 4, 5, 1, 1, 6, 1, 7, 8, 9, 10, 11, 1, 
			12, 1, 1, 1, 1, 13, 14, 1, 1, 1, 15, 16, 16, 17, 1, 1, 
			1, 1, 18, 1, 1, 19, 20, 16, 21, 1, 1, 1, 1, 1, 1, 1, 
			1, 1, 1, 1, 1, 1, 1, 22, 1, 23, 1, 16, 16, 16, 16, 1, 
			1, 16, 16, 24, 16, 25, 1, 26, 27, 16, 16, 1, 16, 16, 16, 16, 
			16, 28, 16, 16, 29, 16, 1, 16, 16, 16, 16, 16, 16, 16, 16, 16, 
			16, 16, 16, 16, 30, 16, 16, 16, 16, 16, 16, 1, 16, 16, 16, 16, 
			16, 16, 16, 16, 16, 31, 16, 16, 32, 1, 1, 1, 1, 33, 34, 16, 
			16, 16, 16, 16, 16, 16, 16, 16, 1, 1, 1, 1, 1, 1, 16, 16, 
			16, 16, 16, 16, 16, 16, 16, 1, 1, 1, 1, 1, 16, 16, 16, 16, 
			16, 16, 16, 16, 1, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 
			16, 16, 16, 35, 36, 37, 38, 39, 40, 41, 1, 42, 43, 44, 39, 1, 
			45, 1, 1, 46, 1, 47, 1, 48, 1, 1, 49, 50, 1, 51, 1, 52, 
			53, 54, 55, 56, 51, 1, 57, 1, 1, 1, 58, 1, 59, 60, 1, 1, 
			61, 62, 63, 64, 65, 66, 67, 62, 1, 68, 1, 1, 69, 1, 70, 71, 
			1, 1, 72, 1, 1, 73, 1, 74, 75, 76, 1, 77, 78, 1, 79, 80, 
			1, 1, 81, 82, 83, 1, 84, 85, 86, 87, 88, 1, 89, 1, 90, 91, 
			92, 93, 94, 1, 95, 1, 1, 1, 1, 96, 97, 98, 1, 99, 1, 1, 
			1, 1, 100, 101, 102, 103, 1, 104, 1, 1, 1, 1, 105, 1, 106, 107, 
			108, 109, 110, 111, 112, 1, 113, 1, 114, 1, 115, 116, 117, 118, 119, 120, 
			121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 
			137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 
			153, 154, 1, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167, 
			168, 169, 170, 171, 172, 9, 173, 174, 175, 10, 176, 177, 178, 179, 180, 181, 
			182, 26, 183, 27, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 157, 
			195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 
			211, 212, 213, 29, 214, 215, 216, 25, 217, 218, 219, 220, 221, 222, 223, 224, 
			225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 
			241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 256, 
			257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 268, 269, 270, 271, 272, 
			273, 274, 275, 276, 277, 278, 279, 280, 281, 282, 283, 284, 285, 286, 287, 288, 
			289, 290, 291, 292, 293, 294, 295, 296, 297, 298, 299, 300, 301, 302, 303, 304, 
			305, 306, 307, 308, 309, 310, 311, 312, 313, 314, 315, 316, 317, 318, 319, 320, 
			321, 322, 323, 324, 325, 326, 327, 328, 329, 330, 331, 332, 333, 334, 335, 336, 
			337, 338, 339, 340, 341, 342, 343, 344, 345, 346, 347, 35, 348, 349, 350, 351, 
			352, 353, 354, 355, 356, 357, 358, 359, 112, 360, 361, 362, 363, 364, 113, 365, 
			366, 367, 114, 368, 369, 370, 371, 372, 373, 374, 375, 376, 377, 378, 379, 380, 
			381, 382, 383, 384, 385, 386, 387, 210, 388, 389, 390, 391, 392, 393, 394, 395, 
			396, 397, 398, 399, 400, 401, 402, 403, 404, 405, 406, 407, 408, 409, 410, 411, 
			412, 413, 414, 415, 416, 417, 418, 419, 420, 421, 422, 423, 424, 425, 426, 427, 
			428, 429, 430, 431, 432, 433, 434, 435, 436, 437, 438, 439, 440, 441, 442, 443, 
			444, 445, 446, 447, 448, 449, 450, 451, 452, 453, 454, 455, 456, 457, 458, 459, 
			460, 461, 462, 463, 464, 465, 466, 467, 468, 469, 470, 471, 472, 473, 474, 475, 
			476, 477, 478, 479, 480, 481, 482, 483, 484, 485, 486, 487, 488, 489, 490, 491, 
			492, 493, 494, 495, 496, 497, 498, 499, 500, 501, 502, 503, 504, 505, 506, 507, 
			508, 509, 510, 511, 512, 513, 514, 515, 516, 517, 518, 519, 520, 521, 522, 523, 
			524, 525, 526, 527, 528, 529, 530, 531, 532, 533, 534, 535, 536, 537, 538, 539, 
			540, 541, 542, 543, 544, 545, 546, 547, 548, 549, 550, 551, 552, 553, 554, 555, 
			556, 557, 558, 559, 560, 561, 562, 563, 564, 565, 566, 567, 568, 569, 570, 571, 
			572, 573, 574, 575, 576, 577, 578, 579, 580, 581, 582, 583, 584, 585, 586, 587, 
			588, 589, 590, 591, 592, 593, 594, 595, 596, 597, 598, 599, 600, 601, 602, 603, 
			604, 605, 606, 607, 608, 609, 610, 611, 612, 613, 614, 615, 616, 617, 618, 619, 
			620, 621, 622, 623, 624, 625, 626, 627, 628, 629, 630, 631, 632, 633, 634, 635, 
			636, 637, 638, 639, 640, 641, 642, 643, 644, 645, 646, 647, 648, 649, 650, 651, 
			652, 653, 654, 655, 656, 657, 658, 659, 16, 660, 661, 662, 663, 664, 665, 666, 
			667, 668, 669, 670, 671, 672, 673, 674, 675, 676, 677, 678, 679, 680, 681, 682, 
			683, 684
		};
		
		private static int[,] nextState = new int[,]
		{
			{ 1, 2, 260, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 2, 258, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 303, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 894, -1, 894, 894, 894, 890, 894, 894, 894, 894, -1, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, -1, -1, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, -1, 894, -1, 894 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 20, -1, -1, -1, 21, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 876, -1, 872, 872, 872, 872, 872, 623, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 624, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 11, -1 },
			{ -1, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 31, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, -1, 373, 373, 373, 375, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, -1, 373, 373, 373 },
			{ -1, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 32, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 379, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, -1, 377, 377, 377 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 383, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 14, 14, -1, -1, -1, -1, -1, -1, 40, -1, -1, -1, -1, -1, 14, -1, -1, 14, 14, -1, -1, 14, -1, 14, 14, -1, -1, -1, -1, -1, 14, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 389, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 267, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 306, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 409, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 269, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 307, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 63, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 419, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 795, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 383, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 40, 40, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 40, -1, -1, 40, 40, -1, -1, 40, -1, 40, 40, -1, -1, -1, -1, -1, 40, -1, -1 },
			{ -1, -1, -1, -1, -1, 55, 55, 55, 55, 55, 55, -1, 55, 55, 55, 55, 55, 55, -1, 55, -1, -1, -1, -1, -1, -1, 55, 55, 55, 55, 55, 55, 55, 55, 55, 55, 55, 55, 55, 55, 55, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 55, 55, -1, -1, 55, 55, -1, -1, 55, -1, 55, 55, -1, -1, -1, -1, -1, 55, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 689, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 386, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 701, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 69, 69, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 69, -1, -1, 69, 69, -1, -1, 69, -1, 69, 69, -1, -1, -1, -1, -1, 69, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, 71, -1, -1, -1, 71, -1, -1, -1, -1, -1, 71, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, 71, 71, -1, -1, 71, -1, 71, 71, -1, -1, -1, -1, -1, 71, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 72, 72, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 432, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 84, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 84, 84, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 84, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 456, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 817, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 815, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 758, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 885, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, -1, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, -1, 179, 179, 179 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 271, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 180, 180, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 180, -1, -1, 180, 180, -1, -1, 180, -1, 180, 180, -1, -1, -1, -1, -1, 180, 271, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 181, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 181, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 182, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 182, -1 },
			{ -1, 183, 183, -1, 183, -1, -1, -1, -1, -1, -1, 183, -1, -1, -1, -1, -1, -1, 183, -1, -1, 183, 183, 183, 183, 183, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 183, 183, 183, 183, 183, 183, 183, 183, 183, -1, -1, -1, 183, -1, -1, -1, 183, 183, -1, 183, -1, -1, -1, -1, -1, 183, -1, -1, 183, -1 },
			{ -1, -1, -1, 184, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 185, 185, 185, 185, 185, 185, -1, 185, 185, 185, 185, 185, 185, -1, 185, -1, -1, -1, -1, -1, -1, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 185, 185, -1, -1, 185, 185, -1, -1, 185, -1, 185, 185, -1, -1, -1, -1, -1, 185, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, 187, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, -1, -1, 187, 187, -1, -1, 187, -1, 187, 187, -1, -1, -1, -1, -1, 187, -1, -1 },
			{ -1, 191, 191, 191, 191, 192, 192, 192, 192, 192, 192, 191, 192, 192, 192, 192, 192, 192, 191, 192, 191, 191, 191, 191, 191, 191, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, 191, 191, 192, 191, 191, 191, 191, 191, 191, 191, 191, 191, 191, 192, 191, 191, 191, 191, 191, 191, 191, 191, 191, 191, 191, 193, 191, 191, 191, -1, 191, 191, 191 },
			{ -1, 194, 194, 194, 194, 194, 195, 196, 194, 194, 196, 194, 194, 194, 196, 194, 197, 194, 194, 194, 198, 194, 194, 194, 194, 194, 276, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 199, 199, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 196, 194, 194, 194, 196, 199, 199, 194, 194, 194, 194, 199, 199, 200, 194, 194, 194, -1, 199, 194, 194 },
			{ -1, -1, -1, -1, -1, 192, 192, 192, 192, 192, 192, -1, 192, 192, 192, 192, 192, 192, -1, 192, -1, -1, -1, -1, -1, -1, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 192, 192, -1, -1, 192, 192, -1, -1, 192, -1, 192, 192, -1, -1, -1, -1, -1, 192, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 569, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 895, -1, -1, -1, -1, -1, -1, 895, -1, -1, -1, 895, -1, -1, -1, -1, -1, -1, -1, -1, -1, 895, 895, -1, -1, -1, 895, -1, -1, -1, -1, -1, 895, 895, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 895, -1, -1, 895, 895, -1, -1, 895, -1, 895, 895, -1, -1, -1, -1, -1, 895, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 592, 592, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 592, 592, -1, -1, -1, -1, 592, 592, -1, -1, -1, -1, -1, 592, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 280, -1, -1, -1, 280, -1, -1, -1, -1, -1, 280, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, 280, 280, -1, -1, 280, -1, 280, 280, -1, -1, -1, -1, -1, 280, -1, -1 },
			{ -1, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, -1, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 572, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, -1, 203, 203, 203 },
			{ -1, 205, 205, -1, 205, -1, -1, -1, -1, -1, -1, 205, -1, -1, -1, -1, -1, -1, 205, -1, -1, 205, 205, 205, 205, 205, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 205, 205, 205, 205, 205, 205, 205, 205, 205, -1, -1, -1, 205, -1, -1, -1, 205, 205, -1, 205, -1, -1, -1, -1, -1, 205, -1, -1, 205, -1 },
			{ -1, -1, -1, -1, -1, 207, 207, 207, 207, 207, 207, -1, 207, 207, 207, 207, 207, 207, -1, 207, -1, -1, -1, -1, -1, -1, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 207, 207, -1, -1, 207, 207, -1, -1, 207, -1, 207, 207, -1, -1, -1, -1, -1, 207, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 208, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 209, 209, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 209, -1, -1, 209, 209, -1, -1, 209, -1, 209, 209, -1, -1, -1, -1, -1, 209, -1, -1 },
			{ -1, 213, 213, 213, 213, 214, 214, 214, 214, 214, 214, 213, 214, 214, 214, 214, 214, 214, 213, 214, 213, 213, 213, 213, 213, 213, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, 213, 213, 214, 213, 213, 213, 213, 213, 213, 213, 213, 213, 213, 214, 213, 213, 213, 213, 213, 213, 213, 213, 213, 213, 213, 215, 213, 213, 213, -1, 213, 213, 213 },
			{ -1, 216, 216, 217, 216, 216, 218, 219, 216, 216, 219, 216, 216, 216, 219, 216, 220, 216, 216, 216, 216, 216, 216, 216, 216, 216, 285, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 221, 221, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 219, 216, 216, 216, 219, 221, 221, 216, 216, 216, 216, 221, 221, 222, 216, 216, 216, -1, 221, 216, 216 },
			{ -1, -1, -1, -1, -1, 214, 214, 214, 214, 214, 214, -1, 214, 214, 214, 214, 214, 214, -1, 214, -1, -1, -1, -1, -1, -1, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 214, 214, -1, -1, 214, 214, -1, -1, 214, -1, 214, 214, -1, -1, -1, -1, -1, 214, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 575, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 896, -1, -1, -1, -1, -1, -1, 896, -1, -1, -1, 896, -1, -1, -1, -1, -1, -1, -1, -1, -1, 896, 896, -1, -1, -1, 896, -1, -1, -1, -1, -1, 896, 896, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 896, -1, -1, 896, 896, -1, -1, 896, -1, 896, 896, -1, -1, -1, -1, -1, 896, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 594, 594, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 594, 594, -1, -1, -1, -1, 594, 594, -1, -1, -1, -1, -1, 594, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 289, -1, -1, -1, -1, -1, -1, 289, -1, -1, -1, 289, -1, -1, -1, -1, -1, -1, -1, -1, -1, 289, 289, -1, -1, -1, 289, -1, -1, -1, -1, -1, 289, 289, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 289, -1, -1, 289, 289, -1, -1, 289, -1, 289, 289, -1, -1, -1, -1, -1, 289, -1, -1 },
			{ -1, 225, 225, -1, 225, -1, -1, -1, -1, -1, -1, 225, -1, -1, -1, -1, -1, -1, 225, -1, -1, 225, 225, 225, 225, 225, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 225, 225, 225, 225, 225, 225, 225, 225, 225, -1, -1, -1, 225, -1, -1, -1, 225, 225, -1, 225, -1, -1, -1, -1, -1, 225, -1, -1, 225, -1 },
			{ -1, -1, -1, 226, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 226, 226, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 227, 227, 227, 227, 227, 227, -1, 227, 227, 227, 227, 227, 227, -1, 227, -1, -1, -1, -1, -1, -1, 227, 227, 227, 227, 227, 227, 227, 227, 227, 227, 227, 227, 227, 227, 227, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 227, 227, -1, -1, 227, 227, -1, -1, 227, -1, 227, 227, -1, -1, -1, -1, -1, 227, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 228, 228, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 228, -1, -1, 228, 228, -1, -1, 228, -1, 228, 228, -1, -1, -1, -1, -1, 228, -1, -1 },
			{ -1, 232, 232, 232, 232, 233, 233, 233, 233, 233, 233, 232, 233, 233, 233, 233, 233, 233, 232, 233, 232, 232, 232, 232, 232, 232, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 232, 232, 233, 232, 232, 232, 232, 232, 232, 232, 232, 232, 232, 233, 232, 232, 232, 232, 232, 232, 232, 232, 232, 232, 232, 234, 232, 232, 232, -1, 232, 232, 232 },
			{ -1, 235, 235, 235, 235, 235, 236, 237, 235, 235, 237, 235, 235, 235, 237, 235, 238, 235, 235, 235, 235, 235, 235, 235, 235, 235, 295, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 239, 239, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 237, 235, 235, 235, 237, 239, 239, 235, 235, 235, 235, 239, 239, 240, 235, 235, 235, -1, 239, 235, 235 },
			{ -1, -1, -1, -1, -1, 233, 233, 233, 233, 233, 233, -1, 233, 233, 233, 233, 233, 233, -1, 233, -1, -1, -1, -1, -1, -1, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 233, 233, -1, -1, 233, 233, -1, -1, 233, -1, 233, 233, -1, -1, -1, -1, -1, 233, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 580, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 897, -1, -1, -1, -1, -1, -1, 897, -1, -1, -1, 897, -1, -1, -1, -1, -1, -1, -1, -1, -1, 897, 897, -1, -1, -1, 897, -1, -1, -1, -1, -1, 897, 897, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 897, -1, -1, 897, 897, -1, -1, 897, -1, 897, 897, -1, -1, -1, -1, -1, 897, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 596, 596, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 596, 596, -1, -1, -1, -1, 596, 596, -1, -1, -1, -1, -1, 596, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 299, -1, -1, -1, -1, -1, -1, 299, -1, -1, -1, 299, -1, -1, -1, -1, -1, -1, -1, -1, -1, 299, 299, -1, -1, -1, 299, -1, -1, -1, -1, -1, 299, 299, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 299, -1, -1, 299, 299, -1, -1, 299, -1, 299, 299, -1, -1, -1, -1, -1, 299, -1, -1 },
			{ -1, -1, -1, -1, -1, 245, 245, 245, 245, 245, 245, -1, 245, 245, 245, 245, 245, 245, -1, 245, -1, -1, -1, -1, -1, -1, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 245, 245, -1, -1, 245, 245, -1, -1, 245, -1, 245, 245, -1, -1, -1, -1, -1, 245, -1, -1 },
			{ -1, -1, -1, -1, -1, 247, 247, 247, 247, 247, 247, -1, 247, 247, 247, 247, 247, 247, -1, 247, -1, -1, -1, -1, -1, -1, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 247, 247, -1, -1, 247, 247, -1, -1, 247, -1, 247, 247, -1, -1, -1, -1, -1, 247, -1, -1 },
			{ -1, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, -1, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, -1, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 250, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, -1, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, -1, 251, 251, 251 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 253, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 257, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 255, 255, -1, 255, 255, 255, 255, 255, 255, -1, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, -1, 255, -1, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, -1, 255, 255, 255 },
			{ -1, -1, -1, 2, -1, -1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, 579, 579, 579, 579, 579, 579, -1, 579, 579, 579, 579, 579, 579, -1, 579, -1, -1, -1, -1, -1, -1, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, -1, -1, 579, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 579, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 3, -1, 2, 4, 5, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, 894, -1, 894, 894, 894, 894, 894, 894, 894, 894, 320, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 320, 320, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, -1, 894, 320, 894 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 22, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 23, -1, -1, -1, 24, -1, -1, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 25, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 629, 872, 872, 872, 872, 872, -1, 872, 872, 325, 872, 872, 872, -1, 872, -1, 371, -1, -1, -1, -1, 872, 872, 27, 872, 872, 872, 872, 872, 872, 872, 872, 630, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 383, -1, -1, -1, -1, -1, -1, -1, -1, 385, -1, -1, -1, -1, -1, 387, -1, -1, -1, -1, -1, 14, 14, -1, -1, -1, -1, -1, -1, 40, -1, -1, -1, -1, -1, 14, -1, -1, 14, 14, -1, -1, 14, -1, 14, 14, -1, -1, -1, -1, -1, 14, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 86, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 271, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 271, -1 },
			{ -1, 183, 183, -1, 183, -1, -1, -1, -1, -1, -1, 183, -1, -1, -1, -1, -1, -1, 183, -1, -1, 183, 190, 183, 183, 183, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 183, 183, 183, 183, 183, 183, 183, 183, 183, -1, -1, -1, 183, -1, -1, -1, 183, 183, -1, 183, -1, -1, -1, -1, -1, 183, -1, -1, 183, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 568, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, 187, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, -1, -1, 187, 187, -1, -1, 187, -1, 187, 187, -1, -1, -1, -1, -1, 187, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 201, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 202, -1, -1, -1, -1, -1, -1, 202, -1, -1, -1, 202, -1, -1, -1, -1, -1, -1, -1, -1, -1, 202, 202, -1, -1, -1, 202, -1, -1, -1, -1, -1, 202, 202, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 202, -1, -1, 202, 202, -1, -1, 202, -1, 202, 202, -1, -1, -1, -1, -1, 202, -1, -1 },
			{ -1, 205, 205, -1, 205, -1, -1, -1, -1, -1, -1, 205, -1, -1, -1, -1, -1, -1, 205, -1, -1, 205, 212, 205, 205, 205, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 205, 205, 205, 205, 205, 205, 205, 205, 205, -1, -1, -1, 205, -1, -1, -1, 205, 205, -1, 205, -1, -1, -1, -1, -1, 205, -1, -1, 205, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 574, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 209, 209, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 209, -1, -1, 209, 209, -1, -1, 209, -1, 209, 209, -1, -1, -1, -1, -1, 209, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 223, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 224, -1, -1, -1, -1, -1, -1, 224, -1, -1, -1, 224, -1, -1, -1, -1, -1, -1, -1, -1, -1, 224, 224, -1, -1, -1, 224, -1, -1, -1, -1, -1, 224, 224, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 224, -1, -1, 224, 224, -1, -1, 224, -1, 224, 224, -1, -1, -1, -1, -1, 224, -1, -1 },
			{ -1, 225, 225, -1, 225, -1, -1, -1, -1, -1, -1, 225, -1, -1, -1, -1, -1, -1, 225, -1, -1, 225, 231, 225, 225, 225, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 225, 225, 225, 225, 225, 225, 225, 225, 225, -1, -1, -1, 225, -1, -1, -1, 225, 225, -1, 225, -1, -1, -1, -1, -1, 225, -1, -1, 225, -1 },
			{ -1, 225, 225, 226, 225, -1, -1, -1, -1, -1, -1, 225, -1, -1, -1, -1, -1, -1, 225, -1, 226, 291, 225, 225, 225, 225, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 225, 225, 225, 225, 225, 225, 225, 225, 225, -1, -1, -1, 225, -1, -1, -1, 225, 225, -1, 225, -1, -1, -1, -1, -1, 225, -1, -1, 225, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 578, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 228, 228, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 228, -1, -1, 228, 228, -1, -1, 228, -1, 228, 228, -1, -1, -1, -1, -1, 228, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 241, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 242, -1, -1, -1, -1, -1, -1, 242, -1, -1, -1, 242, -1, -1, -1, -1, -1, -1, -1, -1, -1, 242, 242, -1, -1, -1, 242, -1, -1, -1, -1, -1, 242, 242, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 242, -1, -1, 242, 242, -1, -1, 242, -1, 242, 242, -1, -1, -1, -1, -1, 242, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 243, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 256, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 314, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 26, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 28, 872, 826, 872, 872, 872, -1, 872, 872, 331, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 787, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 267, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 269, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 308, -1, -1, -1, -1, -1, -1, 308, -1, -1, -1, 308, -1, -1, -1, -1, -1, -1, -1, -1, -1, 308, 308, -1, -1, -1, 308, -1, -1, -1, -1, -1, 308, 308, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 308, -1, -1, 308, 308, -1, -1, 308, -1, 308, 308, -1, -1, -1, -1, -1, 308, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 310, -1, -1, -1, -1, -1, -1, 310, -1, -1, -1, 310, -1, -1, -1, -1, -1, -1, -1, -1, -1, 310, 310, -1, -1, -1, 310, -1, -1, -1, -1, -1, 310, 310, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 310, -1, -1, 310, 310, -1, -1, 310, -1, 310, 310, -1, -1, -1, -1, -1, 310, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 312, -1, -1, -1, 312, -1, -1, -1, -1, -1, 312, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, 312, 312, -1, -1, 312, -1, 312, 312, -1, -1, -1, -1, -1, 312, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 317, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 29, -1, -1, -1, 30, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 343, 872, 872, -1, 872, 872, 872, 872, 872, 644, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 38, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 7, 263, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 7, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 33, -1, -1, -1, 34, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 39, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 646, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 323, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 35, -1, -1, -1, -1, -1, -1, 36, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 37, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 381, -1, -1, 872, 872, 792, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, 12, 13, -1, -1, -1, -1, 872, 872, 872, 648, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 326, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 42, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 848, 827, 872, 872, 872, 57, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 329, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 43, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 44, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 660, 872, 872, 358, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 59, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 332, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 45, -1, -1, -1, 46, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 47, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 60, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 335, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 268, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 61, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 338, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 48, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 62, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 341, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 49, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 40, 40, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 40, -1, -1, 40, 40, -1, -1, 40, -1, 40, 40, -1, -1, -1, -1, -1, 40, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 65, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 344, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 50, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 51, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 66, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 344, -1, -1, -1, -1, -1, -1, 347, -1, -1, -1, -1, 344, 344, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 344, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 52, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 53, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 67, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 350, -1, 347, -1, -1, -1, -1, -1, -1, -1, -1, 353, 881, -1, 347, 347, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 347, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 54, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 68, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 597, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 55, 55, 55, 55, 55, 55, -1, 55, 55, 55, 55, 55, 55, -1, 55, -1, -1, -1, -1, -1, -1, 55, 55, 55, 55, 55, 55, 55, 55, 55, 55, 55, 55, -1, -1, 55, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 55, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 73, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 356, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 74, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 361, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 391, -1, 393, 395, -1, -1, -1, -1, 600, -1, -1, 397, -1, -1, -1, -1, -1, -1, 399, -1, -1, -1, 401, 403, 405, -1, -1, 407, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 399, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 76, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 359, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 261, 359, 359, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 359, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 77, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 363, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 78, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 359, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 79, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 359, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 80, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ 1, 8, 264, 9, 304, 10, 779, 822, 265, 845, 590, 11, 859, 305, 617, 867, 619, 871, 315, 872, 12, 13, 318, 11, 11, 321, 620, 316, 621, 319, 873, 874, 322, 872, 622, 875, 872, 872, 14, 14, 872, 324, 327, 330, 333, 336, 339, 342, 345, 348, 351, 872, 14, 354, 15, 266, 14, 16, 357, 14, 354, 14, 14, 17, 18, 19, 354, 1, 14, 11, 354 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 81, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 411, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 82, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, 413, 415, 415, 413, 413, 413, 413, 413, 413, 413, 415, 413, 413, 413, 413, 413, 413, 413, 413, 413, 58, 415, 413, 415, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 417, 413, 413, 415, 413, 413, 413, 413, 413, 413, 413, 413, 413, -1, 413, 413, 413 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 83, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 85, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, 373, -1, 373, 373, 373 },
			{ -1, -1, -1, -1, -1, 87, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 88, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, 377, -1, 377, 377, 377 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 89, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, 421, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 90, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 69, 69, -1, -1, 423, 423, -1, -1, -1, -1, -1, -1, -1, -1, 69, -1, -1, 69, 69, -1, -1, 69, -1, 69, 69, -1, -1, -1, -1, -1, 69, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 91, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 92, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 93, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 425, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 94, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 427, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 95, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 601, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 96, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 429, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 97, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 431, -1, -1, -1, -1, -1, 433, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 98, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 99, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 435, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 100, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 437, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 101, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 439, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 102, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 603, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 604, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 103, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 441, 441, 441, 441, 441, 441, -1, 441, 441, 441, 441, 441, 441, -1, 441, 443, 605, -1, 409, -1, -1, 441, 441, 441, 441, 441, 441, 441, 441, 441, 441, 441, 441, -1, -1, 441, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 441, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 409, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 104, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 784, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 105, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, 413, 415, 415, 413, 413, 413, 413, 413, 413, 413, 415, 413, 413, 413, 413, 413, 413, 413, 413, 413, 75, 415, 413, 415, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 417, 413, 413, 415, 413, 413, 413, 413, 413, 413, 413, 413, 413, -1, 413, 413, 413 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 106, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 58, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 445, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, -1, 415, 415, 415 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 108, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, 413, 599, 599, 413, 413, 413, 413, 413, 413, 413, 599, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 599, 413, 447, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 413, 599, 413, 413, 413, 413, 413, 413, 413, 413, 413, -1, 413, 413, 413 },
			{ -1, -1, -1, -1, -1, 872, 109, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 110, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, 409, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 111, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 112, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 449, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 113, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 451, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 114, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 455, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 115, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 459, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 116, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 461, -1, -1, 463, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 117, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 465, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 118, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 608, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 119, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 609, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 120, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 441, 441, 441, 441, 441, 441, 86, 441, 441, 441, 441, 441, 441, -1, 441, -1, -1, -1, -1, 270, -1, 441, 441, 441, 441, 441, 441, 441, 441, 441, 441, 441, 441, 441, 441, 441, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 441, 441, -1, -1, 441, 441, -1, -1, 441, -1, 441, 441, -1, -1, -1, -1, -1, 441, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 125, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 467, 467, 467, 467, 467, 467, -1, 467, 467, 467, 467, 467, 467, -1, 467, -1, -1, -1, -1, -1, -1, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, -1, -1, 467, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 467, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 126, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 447, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, 599, -1, 599, 599, 599 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 127, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 599, 415, 415, 415, 415, 415, 415, 415, 415, 415, 58, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 445, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, 415, -1, 415, 415, 415 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 128, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 471, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 129, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 473, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 130, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 475, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 131, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 477, -1, -1, -1, -1, -1, 479, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 481, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 483, -1, -1, 485, 107, 487, -1, -1, -1, -1, -1, -1, -1, 479, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 132, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 489, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 133, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 491, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 134, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 135, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 495, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 142, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 497, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 143, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 467, 467, 467, 467, 467, 467, -1, 467, 467, 467, 467, 467, 467, -1, 467, 505, -1, -1, -1, -1, -1, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 467, 467, -1, -1, 467, 467, -1, -1, 467, -1, 467, 467, -1, -1, -1, -1, -1, 467, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 144, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 469, 469, 469, 469, 469, 469, -1, 469, 469, 469, 469, 469, 469, -1, 469, -1, 505, -1, -1, -1, -1, 469, 469, 469, 469, 469, 469, 469, 469, 469, 469, 469, 469, 469, 469, 469, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 469, 469, -1, -1, 469, 469, -1, -1, 469, -1, 469, 469, -1, -1, -1, -1, -1, 469, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 145, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 509, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 146, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 610, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 147, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 475, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 121, -1, -1, -1, -1, -1, -1, -1, -1, 475, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 148, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 511, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 149, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 479, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, 479, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 150, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 513, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 156, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 515, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 157, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 485, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 122, -1, -1, -1, -1, -1, -1, -1, -1, 485, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 158, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 479, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 159, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 517, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 160, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 519, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 521, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 523, -1, -1, 525, 123, 526, -1, -1, -1, -1, -1, -1, -1, 519, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 161, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 527, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 162, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 528, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 163, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 529, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 165, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 531, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 166, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 532, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 167, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 612, -1, -1, -1, -1, -1, 533, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 124, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 168, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 86, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 270, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 169, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 613, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 170, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 534, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 171, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 536, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 172, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 513, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 136, -1, -1, -1, -1, -1, -1, -1, -1, 513, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 173, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 515, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 137, -1, -1, -1, -1, -1, -1, -1, -1, 515, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 174, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 517, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 138, -1, -1, -1, -1, -1, -1, -1, -1, 517, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 175, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 519, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 123, -1, -1, -1, -1, -1, -1, -1, -1, 519, -1 },
			{ -1, -1, -1, -1, -1, 872, 176, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 177, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 538, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 178, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 525, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 139, -1, -1, -1, -1, -1, -1, -1, -1, 525, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 519, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 527, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 140, -1, -1, -1, -1, -1, -1, -1, -1, 527, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 539, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 475, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 530, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 141, -1, -1, -1, -1, -1, -1, -1, -1, 530, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 540, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 124, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 544, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 544, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 151, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 479, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 152, -1, -1, -1, -1, -1, -1, -1, -1, 537, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 538, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 153, -1, -1, -1, -1, -1, -1, -1, -1, 538, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 545, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 540, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 154, -1, -1, -1, -1, -1, -1, -1, -1, 540, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 155, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 543, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 26, 543, 543, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 543, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 546, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 544, -1, -1, -1, 547, 614, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 544, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 545, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 164, -1, -1, -1, -1, -1, -1, -1, -1, 545, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 549, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 551, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 552, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 615, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 553, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 554, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 557, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 180, 180, -1, -1, -1, 558, -1, -1, -1, -1, -1, -1, -1, -1, 180, -1, -1, 180, 180, -1, -1, 180, -1, 180, 180, -1, -1, -1, -1, -1, 180, 556, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 559, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 180, 180, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 180, -1, -1, 180, 180, -1, -1, 180, -1, 180, 180, -1, -1, -1, -1, -1, 180, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 560, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 561, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 561, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 562, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 561, -1, -1, -1, -1, 616, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 561, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 563, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 565, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 566, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 181, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 182, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ 1, 183, 183, 184, 183, 185, 185, 185, 185, 185, 185, 183, 185, 185, 185, 185, 185, 185, 183, 185, 186, 183, 183, 183, 183, 183, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 187, 187, 185, 183, 183, 272, 183, 183, 183, 183, 183, 183, 188, 185, 187, 183, 189, 273, 187, 183, 183, 187, 183, 187, 187, 274, 275, 309, 183, 1, 187, 183, 309 },
			{ -1, -1, -1, -1, -1, 570, 570, 570, 570, 570, 570, -1, 570, 570, 570, 570, 570, 570, -1, 570, -1, -1, -1, -1, -1, -1, 570, 570, 570, 570, 570, 570, 570, 570, 570, -1, 570, 570, 570, 570, 570, -1, -1, 570, -1, -1, -1, -1, -1, -1, -1, 570, 570, -1, -1, 570, 570, -1, -1, 570, -1, 570, 570, -1, -1, -1, -1, -1, 570, 570, -1 },
			{ -1, -1, -1, -1, -1, 570, 570, 570, 570, 570, 570, -1, 570, 570, 570, 570, 570, 570, -1, 570, -1, -1, -1, -1, -1, -1, 570, 570, 570, 570, 570, 570, 570, 570, 570, -1, 570, 570, 570, 570, 570, -1, -1, 570, -1, -1, -1, -1, -1, -1, -1, 570, 570, -1, -1, 570, 570, -1, -1, 570, -1, 570, 570, -1, -1, 277, -1, -1, 570, 570, -1 },
			{ 1, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 204, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 572, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 1, 203, 203, 203 },
			{ -1, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, -1, 203, 203, 203 },
			{ 1, 205, 205, 206, 205, 207, 207, 207, 207, 207, 207, 205, 207, 207, 207, 207, 207, 207, 205, 207, 208, 205, 205, 205, 205, 205, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 209, 209, 207, 205, 205, 281, 205, 205, 205, 205, 205, 205, 210, 207, 209, 205, 211, 282, 209, 205, 205, 209, 205, 209, 209, 283, 284, 311, 205, 1, 209, 205, 311 },
			{ -1, -1, -1, -1, -1, 576, 576, 576, 576, 576, 576, -1, 576, 576, 576, 576, 576, 576, -1, 576, -1, -1, -1, -1, -1, -1, 576, 576, 576, 576, 576, 576, 576, 576, 576, -1, 576, 576, 576, 576, 576, -1, -1, 576, -1, -1, -1, -1, -1, -1, -1, 576, 576, -1, -1, 576, 576, -1, -1, 576, -1, 576, 576, -1, -1, -1, -1, -1, 576, 576, -1 },
			{ -1, -1, -1, -1, -1, 576, 576, 576, 576, 576, 576, -1, 576, 576, 576, 576, 576, 576, -1, 576, -1, -1, -1, -1, -1, -1, 576, 576, 576, 576, 576, 576, 576, 576, 576, -1, 576, 576, 576, 576, 576, -1, -1, 576, -1, -1, -1, -1, -1, -1, -1, 576, 576, -1, -1, 576, 576, -1, -1, 576, -1, 576, 576, -1, -1, 286, -1, -1, 576, 576, -1 },
			{ 1, 225, 225, 226, 225, 227, 227, 227, 227, 227, 227, 225, 227, 227, 227, 227, 227, 227, 225, 227, 226, 291, 225, 225, 225, 225, 227, 227, 227, 227, 227, 227, 227, 227, 227, 227, 227, 227, 228, 228, 227, 225, 225, 290, 225, 225, 225, 225, 225, 225, 229, 227, 228, 225, 230, 292, 228, 225, 225, 228, 225, 228, 228, 293, 294, 313, 225, 259, 228, 225, 313 },
			{ -1, -1, -1, -1, -1, 579, 579, 579, 579, 579, 579, 243, 579, 579, 579, 579, 579, 579, -1, 579, -1, -1, -1, -1, 300, -1, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 579, 579, -1, -1, 579, 579, -1, -1, 579, -1, 579, 579, -1, -1, -1, 581, -1, 579, -1, -1 },
			{ -1, -1, -1, -1, -1, 582, 582, 582, 582, 582, 582, -1, 582, 582, 582, 582, 582, 582, -1, 582, -1, -1, -1, -1, -1, -1, 582, 582, 582, 582, 582, 582, 582, 582, 582, -1, 582, 582, 582, 582, 582, -1, -1, 582, -1, -1, -1, -1, -1, -1, -1, 582, 582, -1, -1, 582, 582, -1, -1, 582, -1, 582, 582, -1, -1, -1, -1, -1, 582, 582, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 243, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 300, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 582, 582, 582, 582, 582, 582, -1, 582, 582, 582, 582, 582, 582, -1, 582, -1, -1, -1, -1, -1, -1, 582, 582, 582, 582, 582, 582, 582, 582, 582, -1, 582, 582, 582, 582, 582, -1, -1, 582, -1, -1, -1, -1, -1, -1, -1, 582, 582, -1, -1, 582, 582, -1, -1, 582, -1, 582, 582, -1, -1, 296, -1, -1, 582, 582, -1 },
			{ 1, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 294, 259, 294, 294, 294 },
			{ 1, 244, 244, 244, 244, 245, 245, 245, 245, 245, 245, 244, 245, 245, 245, 245, 245, 245, 244, 245, 244, 244, 244, 244, 244, 244, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 244, 244, 245, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 245, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 1, 244, 244, 244 },
			{ 1, 246, 246, 246, 246, 247, 247, 247, 247, 247, 247, 246, 247, 247, 247, 247, 247, 247, 246, 247, 246, 246, 246, 246, 246, 246, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 246, 246, 247, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 247, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 1, 246, 246, 246 },
			{ 1, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 249, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 1, 248, 248, 248 },
			{ 1, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 252, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 1, 251, 251, 251 },
			{ 1, 254, 255, 255, 254, 255, 255, 255, 255, 255, 255, 256, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 301, 255, 302, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 1, 255, 255, 255 },
			{ -1, 894, -1, 894, 894, 894, 894, 894, 894, 894, 262, -1, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, -1, -1, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, -1, 894, -1, 894 },
			{ -1, -1, -1, -1, -1, 872, 872, 328, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 789, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 278, -1, -1, -1, -1, -1, -1, 278, -1, -1, -1, 278, -1, -1, -1, -1, -1, -1, -1, -1, -1, 278, 278, -1, -1, -1, 278, -1, -1, -1, -1, -1, 278, 278, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 278, -1, -1, 278, 278, -1, -1, 278, -1, 278, 278, -1, -1, -1, -1, -1, 278, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 279, 279, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 279, 279, -1, -1, -1, -1, 279, 279, -1, -1, -1, -1, -1, 279, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 287, -1, -1, -1, -1, -1, -1, 287, -1, -1, -1, 287, -1, -1, -1, -1, -1, -1, -1, -1, -1, 287, 287, -1, -1, -1, 287, -1, -1, -1, -1, -1, 287, 287, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 287, -1, -1, 287, 287, -1, -1, 287, -1, 287, 287, -1, -1, -1, -1, -1, 287, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 288, 288, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 288, 288, -1, -1, -1, -1, 288, 288, -1, -1, -1, -1, -1, 288, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 297, -1, -1, -1, -1, -1, -1, 297, -1, -1, -1, 297, -1, -1, -1, -1, -1, -1, -1, -1, -1, 297, 297, -1, -1, -1, 297, -1, -1, -1, -1, -1, 297, 297, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 297, -1, -1, 297, 297, -1, -1, 297, -1, 297, 297, -1, -1, -1, -1, -1, 297, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 298, 298, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 298, 298, -1, -1, -1, -1, 298, 298, -1, -1, -1, -1, -1, 298, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 359, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 365, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 602, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 453, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 457, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 618, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 607, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 469, 469, 469, 469, 469, 469, -1, 469, 469, 469, 469, 469, 469, -1, 469, -1, -1, -1, -1, -1, -1, 469, 469, 469, 469, 469, 469, 469, 469, 469, 469, 469, 469, -1, -1, 469, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 469, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 507, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 503, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 611, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 499, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 530, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 542, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 543, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 550, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 555, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 564, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 635, 872, 872, 636, 334, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 501, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 337, 872, 872, 872, 872, 872, -1, 872, 872, 825, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 340, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 786, 872, 872, -1, 872, 645, 872, 872, 824, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 346, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 349, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 352, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 793, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 829, 872, 872, 872, -1, 872, 651, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 652, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 355, 872, 872, 872, 872, 653, -1, 791, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 654, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 790, 872, 872, 872, 872, 655, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 794, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 656, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 657, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 658, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 861, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 659, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 846, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 360, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 828, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 362, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 663, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 364, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 366, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 368, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 665, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 370, 872, 847, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 372, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 886, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 667, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 669, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 860, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 671, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 374, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 674, 675, 872, 872, 872, 676, -1, 677, 830, 796, 678, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 679, 680, 872, 872, 831, 872, 872, 872, 872, 872, 681, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 683, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 376, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 378, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 380, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 382, 872, 872, 872, 872, 882, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 686, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 384, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 798, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 690, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 388, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 862, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 832, -1, 872, 872, 872, 872, 872, 691, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 390, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 392, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 394, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 694, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 396, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 695, 872, 872, 398, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 851, 696, 872, 872, 697, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 400, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 799, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 402, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 404, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 700, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 406, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 408, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 410, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 863, 872, 872, 872, 872, 412, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 804, 703, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 802, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 837, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 801, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 835, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 414, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 834, 872, 872, -1, 872, 872, 872, 872, 872, 887, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 705, 872, 872, -1, 872, 872, 872, 872, 853, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 836, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 416, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 418, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 420, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 707, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 422, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 870, 872, 872, 872, 854, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 710, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 711, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 424, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 426, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 428, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 806, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 430, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 715, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 434, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 808, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 716, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 436, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 839, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 719, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 438, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 440, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 725, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 811, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 842, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 858, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 728, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 442, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 444, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 732, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 446, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 810, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 448, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 841, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 450, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 452, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 454, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 458, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 460, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 840, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 737, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 738, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 813, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 856, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 462, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 742, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 464, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 466, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 468, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 745, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 746, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 470, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 751, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 472, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 474, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 754, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 476, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 478, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 818, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 480, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 757, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 482, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 759, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 484, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 486, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 760, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 488, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 490, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 492, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 761, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 494, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 496, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 764, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 820, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 766, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 767, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 768, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 498, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 500, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 502, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 504, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 769, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 506, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 508, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 510, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 512, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 772, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 774, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 514, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 775, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 516, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 518, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 520, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 777, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 522, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 524, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, 894, -1, 894, 894, 894, 894, 894, 894, 589, 894, -1, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, -1, -1, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, -1, 894, -1, 894 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 625, 626, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 627, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 591, -1, -1, -1, -1, -1, -1, 591, -1, -1, -1, 591, -1, -1, -1, -1, -1, -1, -1, -1, -1, 591, 591, -1, -1, -1, 591, -1, -1, -1, -1, -1, 591, 591, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 591, -1, -1, 591, 591, -1, -1, 591, -1, 591, 591, -1, -1, -1, -1, -1, 591, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 593, -1, -1, -1, -1, -1, -1, 593, -1, -1, -1, 593, -1, -1, -1, -1, -1, -1, -1, -1, -1, 593, 593, -1, -1, -1, 593, -1, -1, -1, -1, -1, 593, 593, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 593, -1, -1, 593, 593, -1, -1, 593, -1, 593, 593, -1, -1, -1, -1, -1, 593, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 595, -1, -1, -1, -1, -1, -1, 595, -1, -1, -1, 595, -1, -1, -1, -1, -1, -1, -1, -1, -1, 595, 595, -1, -1, -1, 595, -1, -1, -1, -1, -1, 595, 595, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 595, -1, -1, 595, 595, -1, -1, 595, -1, 595, 595, -1, -1, -1, -1, -1, 595, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 598, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 606, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 672, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 668, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 868, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 666, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 661, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 849, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 850, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 673, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 684, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 797, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 699, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 704, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 709, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 805, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 718, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 865, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 722, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 809, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 724, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 721, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 866, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 812, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 734, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 735, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 888, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 748, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 739, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 749, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 816, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 756, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 763, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 765, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 762, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 889, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 770, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 771, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 773, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 628, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 682, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 670, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 664, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 662, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 688, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 693, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 685, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 803, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 706, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 714, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 713, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 726, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 723, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 727, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 883, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 740, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 736, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 752, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 750, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 741, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 819, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 776, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 631, 872, 872, 872, -1, 872, 632, 872, 872, 633, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 692, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 698, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 687, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 852, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 708, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 807, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 730, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 838, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 731, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 744, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 755, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 753, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 743, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 634, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 702, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 833, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 712, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 720, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 729, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 733, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 747, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 637, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 638, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 800, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 717, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 855, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 639, 872, 872, 872, 872, -1, 640, 872, 641, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 642, 872, 872, 872, 872, 872, 872, 872, 643, 872, 872, 788, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 647, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 785, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 649, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 823, 872, 872, 872, -1, 872, 650, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, 894, -1, 894, 894, 894, 894, 894, 778, 894, 894, -1, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, -1, -1, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, -1, 894, -1, 894 },
			{ -1, -1, -1, -1, -1, -1, 780, -1, -1, -1, -1, -1, -1, 780, -1, -1, -1, 780, -1, -1, -1, -1, -1, -1, -1, -1, -1, 780, 780, -1, -1, -1, 780, -1, -1, -1, -1, -1, 780, 780, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 780, -1, -1, 780, 780, -1, -1, 780, -1, 780, 780, -1, -1, -1, -1, -1, 780, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 781, -1, -1, -1, -1, -1, -1, 781, -1, -1, -1, 781, -1, -1, -1, -1, -1, -1, -1, -1, -1, 781, 781, -1, -1, -1, 781, -1, -1, -1, -1, -1, 781, 781, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 781, -1, -1, 781, 781, -1, -1, 781, -1, 781, 781, -1, -1, -1, -1, -1, 781, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 782, -1, -1, -1, -1, -1, -1, 782, -1, -1, -1, 782, -1, -1, -1, -1, -1, -1, -1, -1, -1, 782, 782, -1, -1, -1, 782, -1, -1, -1, -1, -1, 782, 782, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 782, -1, -1, 782, 782, -1, -1, 782, -1, 782, 782, -1, -1, -1, -1, -1, 782, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 783, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 864, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 857, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 814, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 821, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 869, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 884, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 843, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, -1, 872, 872, 872, 872, 872, 872, -1, 872, -1, -1, -1, -1, -1, -1, 872, 872, 872, 844, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, -1, -1, 872, 872, -1, -1, 872, -1, 872, 872, -1, -1, -1, -1, -1, 872, -1, -1 },
			{ -1, 894, -1, 894, 894, 894, 894, 877, 894, 894, 894, -1, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, -1, -1, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, -1, 894, -1, 894 },
			{ -1, -1, -1, -1, -1, -1, 878, -1, -1, -1, -1, -1, -1, 878, -1, -1, -1, 878, -1, -1, -1, -1, -1, -1, -1, -1, -1, 878, 878, -1, -1, -1, 878, -1, -1, -1, -1, -1, 878, 878, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 878, -1, -1, 878, 878, -1, -1, 878, -1, 878, 878, -1, -1, -1, -1, -1, 878, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 879, -1, -1, -1, -1, -1, -1, 879, -1, -1, -1, 879, -1, -1, -1, -1, -1, -1, -1, -1, -1, 879, 879, -1, -1, -1, 879, -1, -1, -1, -1, -1, 879, 879, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 879, -1, -1, 879, 879, -1, -1, 879, -1, 879, 879, -1, -1, -1, -1, -1, 879, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 880, -1, -1, -1, -1, -1, -1, 880, -1, -1, -1, 880, -1, -1, -1, -1, -1, -1, -1, -1, -1, 880, 880, -1, -1, -1, 880, -1, -1, -1, -1, -1, 880, 880, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 880, -1, -1, 880, 880, -1, -1, 880, -1, 880, 880, -1, -1, -1, -1, -1, 880, -1, -1 },
			{ -1, 894, -1, 894, 894, 894, 894, 894, 894, 894, 894, -1, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, -1, -1, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, 894, -1, 894, -1, 894 },
			{ -1, -1, -1, -1, -1, -1, 891, -1, -1, -1, -1, -1, -1, 891, -1, -1, -1, 891, -1, -1, -1, -1, -1, -1, -1, -1, -1, 891, 891, -1, -1, -1, 891, -1, -1, -1, -1, -1, 891, 891, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 891, -1, -1, 891, 891, -1, -1, 891, -1, 891, 891, -1, -1, -1, -1, -1, 891, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 892, -1, -1, -1, -1, -1, -1, 892, -1, -1, -1, 892, -1, -1, -1, -1, -1, -1, -1, -1, -1, 892, 892, -1, -1, -1, 892, -1, -1, -1, -1, -1, 892, 892, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 892, -1, -1, 892, 892, -1, -1, 892, -1, 892, 892, -1, -1, -1, -1, -1, 892, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 893, -1, -1, -1, -1, -1, -1, 893, -1, -1, -1, 893, -1, -1, -1, -1, -1, -1, -1, -1, -1, 893, 893, -1, -1, -1, 893, -1, -1, -1, -1, -1, 893, 893, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 893, -1, -1, 893, 893, -1, -1, 893, -1, 893, 893, -1, -1, -1, -1, -1, 893, -1, -1 }
		};
		
		
		private static int[] yy_state_dtrans = new int[]
		{
			  0,
			  367,
			  567,
			  571,
			  573,
			  577,
			  583,
			  584,
			  585,
			  586,
			  587,
			  588
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
							System.Diagnostics.Debug.Assert(last_accept_state >= 898);
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

