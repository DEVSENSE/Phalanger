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
			ST_LOOKING_FOR_PROPERTY = 6,
			ST_LOOKING_FOR_VARNAME = 7,
			ST_DOC_COMMENT = 8,
			ST_COMMENT = 9,
			ST_ONE_LINE_COMMENT = 10,
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
					// #line 75
					{ 
						return Tokens.T_INLINE_HTML; 
					}
					break;
					
				case 3:
					// #line 103
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
					// #line 115
					{
						BEGIN(LexicalStates.ST_IN_SCRIPTING);
						return Tokens.T_OPEN_TAG;
					}
					break;
					
				case 7:
					// #line 272
					{ return (Tokens)GetTokenChar(0); }
					break;
					
				case 8:
					// #line 347
					{ 
						BEGIN(LexicalStates.ST_BACKQUOTE); 
						return Tokens.T_BACKQUOTE; 
					}
					break;
					
				case 9:
					// #line 273
					{ return Tokens.T_STRING; }
					break;
					
				case 10:
					// #line 275
					{ return Tokens.T_WHITESPACE; }
					break;
					
				case 11:
					// #line 331
					{ 
						BEGIN(LexicalStates.ST_DOUBLE_QUOTES); 
						return (GetTokenChar(0) != '"') ? Tokens.T_BINARY_DOUBLE : Tokens.T_DOUBLE_QUOTES; 
					}
					break;
					
				case 12:
					// #line 337
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
					// #line 276
					{ return Tokens.ParseDecimalNumber; }
					break;
					
				case 14:
					// #line 274
					{ return Tokens.T_NS_SEPARATOR; }
					break;
					
				case 15:
					// #line 286
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); yymore(); break; }
					break;
					
				case 16:
					// #line 309
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_LBRACE; }
					break;
					
				case 17:
					// #line 359
					{ return Tokens.ERROR; }
					break;
					
				case 18:
					// #line 310
					{ if (!yy_pop_state()) return Tokens.ERROR; return Tokens.T_RBRACE; }
					break;
					
				case 19:
					// #line 257
					{ return Tokens.T_MOD_EQUAL; }
					break;
					
				case 20:
					// #line 312
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
					// #line 265
					{ return Tokens.T_SL; }
					break;
					
				case 22:
					// #line 250
					{ return Tokens.T_IS_SMALLER_OR_EQUAL; }
					break;
					
				case 23:
					// #line 249
					{ return Tokens.T_IS_NOT_EQUAL; }
					break;
					
				case 24:
					// #line 223
					{ return Tokens.T_LGENERIC; }
					break;
					
				case 25:
					// #line 123
					{ 
						BEGIN(LexicalStates.INITIAL); 
						return Tokens.T_CLOSE_TAG; 
					}
					break;
					
				case 26:
					// #line 218
					{ return (InLinq) ? Tokens.T_LINQ_IN : Tokens.T_STRING; }
					break;
					
				case 27:
					// #line 137
					{ return Tokens.T_IF; }
					break;
					
				case 28:
					// #line 148
					{ return Tokens.T_AS; }
					break;
					
				case 29:
					// #line 248
					{ return Tokens.T_IS_EQUAL; }
					break;
					
				case 30:
					// #line 243
					{ return Tokens.T_DOUBLE_ARROW; }
					break;
					
				case 31:
					// #line 325
					{ return Tokens.DoubleQuotedString; }
					break;
					
				case 32:
					// #line 326
					{ return Tokens.SingleQuotedString; }
					break;
					
				case 33:
					// #line 251
					{ return Tokens.T_IS_GREATER_OR_EQUAL; }
					break;
					
				case 34:
					// #line 266
					{ return Tokens.T_SR; }
					break;
					
				case 35:
					// #line 255
					{ return Tokens.T_DIV_EQUAL; }
					break;
					
				case 36:
					// #line 287
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); yymore(); break; }
					break;
					
				case 37:
					// #line 289
					{ BEGIN(LexicalStates.ST_COMMENT); yymore(); break; }
					break;
					
				case 38:
					// #line 143
					{ return Tokens.T_DO; }
					break;
					
				case 39:
					// #line 171
					{ return Tokens.T_LOGICAL_OR; }
					break;
					
				case 40:
					// #line 219
					{ return (InLinq) ? Tokens.T_LINQ_BY : Tokens.T_STRING; }
					break;
					
				case 41:
					// #line 278
					{ return Tokens.ParseDouble; }
					break;
					
				case 42:
					// #line 224
					{ return Tokens.T_RGENERIC; }
					break;
					
				case 43:
					// #line 267
					{ return Tokens.T_DOUBLE_COLON; }
					break;
					
				case 44:
					// #line 252
					{ return Tokens.T_PLUS_EQUAL; }
					break;
					
				case 45:
					// #line 244
					{ return Tokens.T_INC; }
					break;
					
				case 46:
					// #line 253
					{ return Tokens.T_MINUS_EQUAL; }
					break;
					
				case 47:
					// #line 269
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 48:
					// #line 245
					{ return Tokens.T_DEC; }
					break;
					
				case 49:
					// #line 254
					{ return Tokens.T_MUL_EQUAL; }
					break;
					
				case 50:
					// #line 256
					{ return Tokens.T_CONCAT_EQUAL; }
					break;
					
				case 51:
					// #line 260
					{ return Tokens.T_AND_EQUAL; }
					break;
					
				case 52:
					// #line 264
					{ return Tokens.T_BOOLEAN_AND; }
					break;
					
				case 53:
					// #line 261
					{ return Tokens.T_OR_EQUAL; }
					break;
					
				case 54:
					// #line 263
					{ return Tokens.T_BOOLEAN_OR; }
					break;
					
				case 55:
					// #line 262
					{ return Tokens.T_XOR_EQUAL; }
					break;
					
				case 56:
					// #line 270
					{ return Tokens.T_VARIABLE; }
					break;
					
				case 57:
					// #line 258
					{ return Tokens.T_SL_EQUAL; }
					break;
					
				case 58:
					// #line 203
					{ return Tokens.T_INT_TYPE; }
					break;
					
				case 59:
					// #line 328
					{ return Tokens.ErrorInvalidIdentifier; }
					break;
					
				case 60:
					// #line 183
					{ return Tokens.T_TRY; }
					break;
					
				case 61:
					// #line 172
					{ return Tokens.T_LOGICAL_AND; }
					break;
					
				case 62:
					// #line 159
					{ return Tokens.T_NEW; }
					break;
					
				case 63:
					// #line 199
					{ return Tokens.T_USE; }
					break;
					
				case 64:
					// #line 246
					{ return Tokens.T_IS_IDENTICAL; }
					break;
					
				case 65:
					// #line 259
					{ return Tokens.T_SR_EQUAL; }
					break;
					
				case 66:
					// #line 133
					{ return Tokens.T_EXIT; }
					break;
					
				case 67:
					// #line 173
					{ return Tokens.T_LOGICAL_XOR; }
					break;
					
				case 68:
					// #line 144
					{ return Tokens.T_FOR; }
					break;
					
				case 69:
					// #line 160
					{ return Tokens.T_VAR; }
					break;
					
				case 70:
					// #line 279
					{ return Tokens.ParseDouble; }
					break;
					
				case 71:
					// #line 247
					{ return Tokens.T_IS_NOT_IDENTICAL; }
					break;
					
				case 72:
					// #line 277
					{ return Tokens.ParseHexadecimalNumber; }
					break;
					
				case 73:
					// #line 237
					{ return Tokens.T_SELF; }
					break;
					
				case 74:
					// #line 151
					{ return Tokens.T_CASE; }
					break;
					
				case 75:
					// #line 327
					{ return Tokens.SingleQuotedIdentifier; }
					break;
					
				case 76:
					// #line 239
					{ return Tokens.T_TRUE; }
					break;
					
				case 77:
					// #line 174
					{ return Tokens.T_LIST; }
					break;
					
				case 78:
					// #line 241
					{ return Tokens.T_NULL; }
					break;
					
				case 79:
					// #line 200
					{ return Tokens.T_GOTO; }
					break;
					
				case 80:
					// #line 155
					{ return Tokens.T_ECHO; }
					break;
					
				case 81:
					// #line 140
					{ return Tokens.T_ELSE; }
					break;
					
				case 82:
					// #line 132
					{ return Tokens.T_EXIT; }
					break;
					
				case 83:
					// #line 161
					{ return Tokens.T_EVAL; }
					break;
					
				case 84:
					// #line 288
					{ BEGIN(LexicalStates.ST_DOC_COMMENT); yymore(); break; }
					break;
					
				case 85:
					// #line 211
					{ return Tokens.T_LINQ_FROM; }
					break;
					
				case 86:
					// #line 202
					{ return Tokens.T_BOOL_TYPE; }
					break;
					
				case 87:
					// #line 352
					{
						bool is_binary = GetTokenChar(0) != '<';
						hereDocLabel = GetTokenSubstring(is_binary ? 4 : 3).Trim();
						BEGIN(LexicalStates.ST_HEREDOC);
						return is_binary ? Tokens.T_BINARY_HEREDOC : Tokens.T_START_HEREDOC;
					}
					break;
					
				case 88:
					// #line 157
					{ return Tokens.T_CLASS; }
					break;
					
				case 89:
					// #line 188
					{ return Tokens.T_CLONE; }
					break;
					
				case 90:
					// #line 184
					{ return Tokens.T_CATCH; }
					break;
					
				case 91:
					// #line 135
					{ return Tokens.T_CONST; }
					break;
					
				case 92:
					// #line 167
					{ return Tokens.T_ISSET; }
					break;
					
				case 93:
					// #line 204
					{ return Tokens.T_INT64_TYPE; }
					break;
					
				case 94:
					// #line 156
					{ return Tokens.T_PRINT; }
					break;
					
				case 95:
					// #line 185
					{ return Tokens.T_THROW; }
					break;
					
				case 96:
					// #line 175
					{ return Tokens.T_ARRAY; }
					break;
					
				case 97:
					// #line 217
					{ return (InLinq) ? Tokens.T_LINQ_GROUP : Tokens.T_STRING; }
					break;
					
				case 98:
					// #line 170
					{ return Tokens.T_UNSET; }
					break;
					
				case 99:
					// #line 139
					{ return Tokens.T_ENDIF; }
					break;
					
				case 100:
					// #line 168
					{ return Tokens.T_EMPTY; }
					break;
					
				case 101:
					// #line 190
					{ return Tokens.T_FINAL; }
					break;
					
				case 102:
					// #line 240
					{ return Tokens.T_FALSE; }
					break;
					
				case 103:
					// #line 141
					{ return Tokens.T_WHILE; }
					break;
					
				case 104:
					// #line 212
					{ return (InLinq) ? Tokens.T_LINQ_WHERE : Tokens.T_STRING; }
					break;
					
				case 105:
					// #line 153
					{ return Tokens.T_BREAK; }
					break;
					
				case 106:
					// #line 228
					{ return Tokens.T_SET; }
					break;
					
				case 107:
					// #line 227
					{ return Tokens.T_GET; }
					break;
					
				case 108:
					// #line 293
					{ return Tokens.T_INT32_CAST; }
					break;
					
				case 109:
					// #line 206
					{ return Tokens.T_STRING_TYPE; }
					break;
					
				case 110:
					// #line 169
					{ return Tokens.T_STATIC; }
					break;
					
				case 111:
					// #line 216
					{ return (InLinq) ? Tokens.T_LINQ_SELECT : Tokens.T_STRING; }
					break;
					
				case 112:
					// #line 149
					{ return Tokens.T_SWITCH; }
					break;
					
				case 113:
					// #line 136
					{ return Tokens.T_RETURN; }
					break;
					
				case 114:
					// #line 236
					{ return Tokens.T_PARENT; }
					break;
					
				case 115:
					// #line 193
					{ return Tokens.T_PUBLIC; }
					break;
					
				case 116:
					// #line 226
					{ return Tokens.T_ASSERT; }
					break;
					
				case 117:
					// #line 166
					{ return Tokens.T_GLOBAL; }
					break;
					
				case 118:
					// #line 138
					{ return Tokens.T_ELSEIF; }
					break;
					
				case 119:
					// #line 145
					{ return Tokens.T_ENDFOR; }
					break;
					
				case 120:
					// #line 205
					{ return Tokens.T_DOUBLE_TYPE; }
					break;
					
				case 121:
					// #line 208
					{ return Tokens.T_OBJECT_TYPE; }
					break;
					
				case 122:
					// #line 229
					{ return Tokens.T_CALL; }
					break;
					
				case 123:
					// #line 299
					{ return Tokens.T_DOUBLE_CAST; }
					break;
					
				case 124:
					// #line 291
					{ return Tokens.T_INT8_CAST; }
					break;
					
				case 125:
					// #line 297
					{ return Tokens.T_UINT32_CAST; }
					break;
					
				case 126:
					// #line 306
					{ return Tokens.T_BOOL_CAST; }
					break;
					
				case 127:
					// #line 164
					{ return Tokens.T_REQUIRE; }
					break;
					
				case 128:
					// #line 162
					{ return Tokens.T_INCLUDE; }
					break;
					
				case 129:
					// #line 191
					{ return Tokens.T_PRIVATE; }
					break;
					
				case 130:
					// #line 221
					{ return Tokens.T_PARTIAL; }
					break;
					
				case 131:
					// #line 158
					{ return Tokens.T_EXTENDS; }
					break;
					
				case 132:
					// #line 128
					{
					  return Tokens.ErrorNotSupported; 
					}
					break;
					
				case 133:
					// #line 152
					{ return Tokens.T_DEFAULT; }
					break;
					
				case 134:
					// #line 146
					{ return Tokens.T_FOREACH; }
					break;
					
				case 135:
					// #line 213
					{ return (InLinq) ? Tokens.T_LINQ_ORDERBY : Tokens.T_STRING; }
					break;
					
				case 136:
					// #line 235
					{ return Tokens.T_SLEEP; }
					break;
					
				case 137:
					// #line 181
					{ return Tokens.T_DIR; }
					break;
					
				case 138:
					// #line 294
					{ return Tokens.T_INT64_CAST; }
					break;
					
				case 139:
					// #line 292
					{ return Tokens.T_INT16_CAST; }
					break;
					
				case 140:
					// #line 304
					{ return Tokens.T_ARRAY_CAST; }
					break;
					
				case 141:
					// #line 295
					{ return Tokens.T_UINT8_CAST; }
					break;
					
				case 142:
					// #line 307
					{ return Tokens.T_UNSET_CAST; }
					break;
					
				case 143:
					// #line 300
					{ return Tokens.T_FLOAT_CAST; }
					break;
					
				case 144:
					// #line 154
					{ return Tokens.T_CONTINUE; }
					break;
					
				case 145:
					// #line 207
					{ return Tokens.T_RESOURCE_TYPE; }
					break;
					
				case 146:
					// #line 189
					{ return Tokens.T_ABSTRACT; }
					break;
					
				case 147:
					// #line 142
					{ return Tokens.T_ENDWHILE; }
					break;
					
				case 148:
					// #line 134
					{ return Tokens.T_FUNCTION; }
					break;
					
				case 149:
					// #line 179
					{ return Tokens.T_LINE; }
					break;
					
				case 150:
					// #line 180
					{ return Tokens.T_FILE; }
					break;
					
				case 151:
					// #line 234
					{ return Tokens.T_WAKEUP; }
					break;
					
				case 152:
					// #line 301
					{ return Tokens.T_STRING_CAST; }
					break;
					
				case 153:
					// #line 298
					{ return Tokens.T_UINT64_CAST; }
					break;
					
				case 154:
					// #line 296
					{ return Tokens.T_UINT16_CAST; }
					break;
					
				case 155:
					// #line 305
					{ return Tokens.T_OBJECT_CAST; }
					break;
					
				case 156:
					// #line 302
					{ return Tokens.T_BINARY_CAST; }
					break;
					
				case 157:
					// #line 209
					{ return Tokens.T_TYPEOF; }
					break;
					
				case 158:
					// #line 186
					{ return Tokens.T_INTERFACE; }
					break;
					
				case 159:
					// #line 192
					{ return Tokens.T_PROTECTED; }
					break;
					
				case 160:
					// #line 215
					{ return (InLinq) ? Tokens.T_LINQ_ASCENDING : Tokens.T_STRING; }
					break;
					
				case 161:
					// #line 197
					{ return Tokens.T_NAMESPACE; }
					break;
					
				case 162:
					// #line 150
					{ return Tokens.T_ENDSWITCH; }
					break;
					
				case 163:
					// #line 176
					{ return Tokens.T_CLASS_C; }
					break;
					
				case 164:
					// #line 303
					{ return Tokens.T_UNICODE_CAST; }
					break;
					
				case 165:
					// #line 194
					{ return Tokens.T_INSTANCEOF; }
					break;
					
				case 166:
					// #line 187
					{ return Tokens.T_IMPLEMENTS; }
					break;
					
				case 167:
					// #line 147
					{ return Tokens.T_ENDFOREACH; }
					break;
					
				case 168:
					// #line 214
					{ return (InLinq) ? Tokens.T_LINQ_DESCENDING : Tokens.T_STRING; }
					break;
					
				case 169:
					// #line 231
					{ return Tokens.T_TOSTRING; }
					break;
					
				case 170:
					// #line 238
					{ return Tokens.T_AUTOLOAD; }
					break;
					
				case 171:
					// #line 233
					{ return Tokens.T_DESTRUCT; }
					break;
					
				case 172:
					// #line 178
					{ return Tokens.T_METHOD_C; }
					break;
					
				case 173:
					// #line 232
					{ return Tokens.T_CONSTRUCT; }
					break;
					
				case 174:
					// #line 165
					{ return Tokens.T_REQUIRE_ONCE; }
					break;
					
				case 175:
					// #line 163
					{ return Tokens.T_INCLUDE_ONCE; }
					break;
					
				case 176:
					// #line 230
					{ return Tokens.T_CALLSTATIC; }
					break;
					
				case 177:
					// #line 177
					{ return Tokens.T_FUNC_C; }
					break;
					
				case 178:
					// #line 196
					{ return Tokens.T_NAMESPACE_C; }
					break;
					
				case 179:
					// #line 282
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_FILE; }
					break;
					
				case 180:
					// #line 281
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_LINE; }
					break;
					
				case 181:
					// #line 283
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_DEFAULT_LINE; }
					break;
					
				case 182:
					// #line 284
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_DEFAULT_FILE; }
					break;
					
				case 183:
					// #line 485
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 184:
					// #line 477
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 185:
					// #line 468
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 186:
					// #line 478
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_DOUBLE_QUOTES; }
					break;
					
				case 187:
					// #line 467
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 188:
					// #line 484
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 189:
					// #line 486
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 190:
					// #line 482
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 191:
					// #line 481
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 192:
					// #line 479
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 193:
					// #line 480
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 194:
					// #line 476
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 195:
					// #line 472
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 196:
					// #line 474
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 197:
					// #line 471
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 198:
					// #line 473
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 199:
					// #line 469
					{ return Tokens.OctalCharCode; }
					break;
					
				case 200:
					// #line 475
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 201:
					// #line 483
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 202:
					// #line 470
					{ return Tokens.HexCharCode; }
					break;
					
				case 203:
					// #line 427
					{ yymore(); break; }
					break;
					
				case 204:
					// #line 428
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.SingleQuotedString; }
					break;
					
				case 205:
					// #line 508
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 206:
					// #line 501
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_BACKQUOTE; }
					break;
					
				case 207:
					// #line 491
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 208:
					// #line 500
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 209:
					// #line 490
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 210:
					// #line 506
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 211:
					// #line 509
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 212:
					// #line 505
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 213:
					// #line 504
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 214:
					// #line 502
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 215:
					// #line 503
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 216:
					// #line 499
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 217:
					// #line 496
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 218:
					// #line 495
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 219:
					// #line 497
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 220:
					// #line 494
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 221:
					// #line 492
					{ return Tokens.OctalCharCode; }
					break;
					
				case 222:
					// #line 498
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 223:
					// #line 507
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 224:
					// #line 493
					{ return Tokens.HexCharCode; }
					break;
					
				case 225:
					// #line 463
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 226:
					// #line 456
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 227:
					// #line 448
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 228:
					// #line 447
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 229:
					// #line 461
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 230:
					// #line 464
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 231:
					// #line 460
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 232:
					// #line 459
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 233:
					// #line 457
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 234:
					// #line 458
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 235:
					// #line 455
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 236:
					// #line 452
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 237:
					// #line 453
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 238:
					// #line 451
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 239:
					// #line 449
					{ return Tokens.OctalCharCode; }
					break;
					
				case 240:
					// #line 454
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 241:
					// #line 462
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 242:
					// #line 450
					{ return Tokens.HexCharCode; }
					break;
					
				case 243:
					// #line 432
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
					// #line 372
					{
						yyless(0);
						if (!yy_pop_state()) return Tokens.ERROR;
						break;
					}
					break;
					
				case 245:
					// #line 365
					{
						if (!yy_pop_state()) return Tokens.ERROR;
						inString = (CurrentLexicalState != LexicalStates.ST_IN_SCRIPTING); 
						isCode = true;
						return Tokens.T_STRING;
					}
					break;
					
				case 246:
					// #line 386
					{
						yyless(0);
						if (!yy_pop_state()) return Tokens.ERROR;
						yy_push_state(LexicalStates.ST_IN_SCRIPTING);
						break;
					}
					break;
					
				case 247:
					// #line 380
					{
						if (!yy_pop_state()) return Tokens.ERROR;
						yy_push_state(LexicalStates.ST_IN_SCRIPTING);
						return Tokens.T_STRING_VARNAME;
					}
					break;
					
				case 248:
					// #line 421
					{ yymore(); break; }
					break;
					
				case 249:
					// #line 423
					{ yymore(); break; }
					break;
					
				case 250:
					// #line 422
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_DOC_COMMENT; }
					break;
					
				case 251:
					// #line 415
					{ yymore(); break; }
					break;
					
				case 252:
					// #line 417
					{ yymore(); break; }
					break;
					
				case 253:
					// #line 416
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_COMMENT; }
					break;
					
				case 254:
					// #line 395
					{ yymore(); break; }
					break;
					
				case 255:
					// #line 396
					{ yymore(); break; }
					break;
					
				case 256:
					// #line 397
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_LINE_COMMENT; }
					break;
					
				case 257:
					// #line 399
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
				case 262: goto case 6;
				case 263: goto case 7;
				case 264: goto case 9;
				case 265: goto case 13;
				case 266: goto case 20;
				case 267: goto case 23;
				case 268: goto case 25;
				case 269: goto case 87;
				case 270: goto case 180;
				case 271: goto case 183;
				case 272: goto case 187;
				case 273: goto case 188;
				case 274: goto case 189;
				case 275: goto case 194;
				case 276: goto case 195;
				case 277: goto case 197;
				case 278: goto case 199;
				case 279: goto case 202;
				case 280: goto case 205;
				case 281: goto case 209;
				case 282: goto case 210;
				case 283: goto case 211;
				case 284: goto case 216;
				case 285: goto case 218;
				case 286: goto case 220;
				case 287: goto case 221;
				case 288: goto case 224;
				case 289: goto case 225;
				case 290: goto case 226;
				case 291: goto case 228;
				case 292: goto case 229;
				case 293: goto case 230;
				case 294: goto case 235;
				case 295: goto case 236;
				case 296: goto case 238;
				case 297: goto case 239;
				case 298: goto case 242;
				case 299: goto case 243;
				case 300: goto case 254;
				case 301: goto case 256;
				case 303: goto case 2;
				case 304: goto case 7;
				case 305: goto case 9;
				case 306: goto case 20;
				case 307: goto case 25;
				case 308: goto case 187;
				case 309: goto case 188;
				case 310: goto case 209;
				case 311: goto case 210;
				case 312: goto case 228;
				case 313: goto case 229;
				case 315: goto case 7;
				case 316: goto case 9;
				case 318: goto case 7;
				case 319: goto case 9;
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
				case 360: goto case 9;
				case 362: goto case 9;
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
				case 586: goto case 9;
				case 587: goto case 197;
				case 588: goto case 199;
				case 589: goto case 220;
				case 590: goto case 221;
				case 591: goto case 238;
				case 592: goto case 239;
				case 613: goto case 9;
				case 615: goto case 9;
				case 616: goto case 9;
				case 617: goto case 9;
				case 618: goto case 9;
				case 619: goto case 9;
				case 620: goto case 9;
				case 621: goto case 9;
				case 622: goto case 9;
				case 623: goto case 9;
				case 624: goto case 9;
				case 625: goto case 9;
				case 626: goto case 9;
				case 627: goto case 9;
				case 628: goto case 9;
				case 629: goto case 9;
				case 630: goto case 9;
				case 631: goto case 9;
				case 632: goto case 9;
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
				case 777: goto case 197;
				case 778: goto case 220;
				case 779: goto case 238;
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
				case 803: goto case 9;
				case 804: goto case 9;
				case 805: goto case 9;
				case 806: goto case 9;
				case 807: goto case 9;
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
				case 878: goto case 197;
				case 879: goto case 220;
				case 880: goto case 238;
				case 882: goto case 9;
				case 883: goto case 9;
				case 884: goto case 9;
				case 885: goto case 9;
				case 886: goto case 9;
				case 887: goto case 9;
				case 888: goto case 197;
				case 889: goto case 220;
				case 890: goto case 238;
				case 891: goto case 197;
				case 892: goto case 220;
				case 893: goto case 238;
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
				if (buffer[i] == '\n')
				{
					token_end_pos.Line++;
					last_eoln = i;
				}
			}
			token_end_pos.Char += to - from;
			token_end_pos.Column = to - last_eoln;
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
			AcceptConditions.AcceptOnStart, // 299
			AcceptConditions.Accept, // 300
			AcceptConditions.Accept, // 301
			AcceptConditions.NotAccept, // 302
			AcceptConditions.Accept, // 303
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
			AcceptConditions.NotAccept, // 524
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
			AcceptConditions.Accept, // 586
			AcceptConditions.Accept, // 587
			AcceptConditions.Accept, // 588
			AcceptConditions.Accept, // 589
			AcceptConditions.Accept, // 590
			AcceptConditions.Accept, // 591
			AcceptConditions.Accept, // 592
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
			AcceptConditions.Accept, // 613
			AcceptConditions.NotAccept, // 614
			AcceptConditions.Accept, // 615
			AcceptConditions.Accept, // 616
			AcceptConditions.Accept, // 617
			AcceptConditions.Accept, // 618
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
			AcceptConditions.NotAccept, // 780
			AcceptConditions.NotAccept, // 781
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
		};
		
		private static int[] colMap = new int[]
		{
			64, 64, 64, 64, 64, 64, 64, 64, 64, 23, 11, 64, 64, 24, 64, 64, 
			64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 
			69, 44, 20, 56, 50, 1, 47, 21, 57, 59, 45, 42, 53, 43, 46, 25, 
			55, 60, 62, 61, 39, 68, 38, 68, 58, 52, 41, 66, 2, 18, 22, 5, 
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
			1, 1, 16, 16, 26, 16, 27, 1, 28, 16, 16, 1, 16, 16, 16, 16, 
			16, 29, 16, 16, 30, 16, 16, 1, 16, 16, 16, 16, 16, 16, 16, 16, 
			16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 1, 16, 16, 16, 
			16, 16, 16, 16, 16, 16, 16, 31, 16, 16, 32, 1, 1, 1, 1, 33, 
			34, 16, 16, 16, 16, 16, 16, 16, 16, 16, 1, 1, 1, 1, 1, 1, 
			16, 16, 16, 16, 16, 16, 16, 16, 1, 1, 1, 1, 1, 16, 16, 16, 
			16, 16, 16, 16, 1, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 
			16, 16, 16, 35, 36, 37, 38, 39, 40, 41, 1, 42, 43, 44, 39, 1, 
			45, 1, 1, 46, 1, 47, 1, 48, 1, 1, 49, 50, 1, 51, 1, 52, 
			53, 54, 55, 56, 51, 1, 57, 1, 1, 1, 58, 1, 59, 60, 1, 1, 
			61, 62, 63, 64, 65, 66, 67, 62, 1, 68, 1, 1, 69, 1, 70, 71, 
			1, 1, 72, 1, 1, 73, 1, 74, 75, 76, 1, 77, 78, 1, 79, 80, 
			1, 1, 81, 82, 83, 1, 84, 85, 86, 87, 1, 88, 1, 89, 90, 91, 
			92, 93, 1, 94, 1, 1, 1, 1, 95, 96, 97, 1, 98, 1, 1, 1, 
			1, 99, 100, 101, 102, 1, 103, 1, 1, 1, 1, 104, 1, 105, 106, 107, 
			108, 109, 110, 111, 112, 1, 113, 1, 114, 1, 115, 116, 117, 118, 119, 120, 
			121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 
			137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 
			153, 154, 1, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167, 
			168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 8, 181, 182, 
			183, 9, 184, 185, 186, 187, 188, 189, 190, 28, 191, 192, 193, 194, 195, 196, 
			197, 198, 199, 200, 201, 157, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 
			212, 213, 214, 215, 216, 217, 218, 219, 220, 30, 221, 222, 223, 27, 224, 225, 
			226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 
			242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 256, 257, 
			258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 268, 269, 270, 271, 272, 273, 
			274, 275, 276, 277, 278, 279, 280, 281, 282, 283, 284, 285, 286, 287, 288, 289, 
			290, 291, 292, 293, 294, 295, 296, 297, 298, 299, 300, 301, 302, 303, 304, 305, 
			306, 307, 308, 309, 310, 311, 312, 313, 314, 315, 316, 317, 318, 319, 320, 321, 
			322, 323, 324, 325, 326, 327, 328, 329, 330, 331, 332, 333, 334, 335, 336, 337, 
			338, 339, 340, 341, 342, 343, 344, 345, 346, 35, 347, 348, 349, 350, 351, 352, 
			353, 354, 355, 356, 357, 358, 112, 359, 360, 361, 362, 363, 113, 364, 365, 366, 
			114, 367, 368, 369, 370, 371, 372, 373, 374, 375, 376, 377, 378, 379, 380, 381, 
			382, 383, 384, 385, 217, 386, 387, 388, 389, 390, 391, 392, 393, 394, 395, 396, 
			397, 398, 399, 400, 401, 402, 403, 404, 405, 406, 407, 408, 409, 410, 411, 412, 
			413, 414, 415, 416, 417, 418, 419, 420, 421, 422, 423, 424, 425, 426, 427, 428, 
			429, 430, 431, 432, 433, 434, 435, 436, 437, 438, 439, 440, 441, 442, 443, 444, 
			445, 446, 447, 448, 449, 450, 451, 452, 453, 454, 455, 456, 457, 458, 459, 460, 
			461, 462, 463, 464, 465, 466, 467, 468, 469, 470, 471, 472, 473, 474, 475, 476, 
			477, 478, 479, 480, 481, 482, 483, 484, 485, 486, 487, 488, 489, 490, 491, 492, 
			493, 494, 495, 496, 497, 498, 499, 500, 501, 502, 503, 504, 505, 506, 507, 508, 
			509, 510, 511, 512, 513, 514, 515, 516, 517, 518, 519, 520, 521, 522, 523, 524, 
			525, 526, 527, 528, 529, 530, 531, 532, 533, 534, 535, 536, 537, 538, 539, 540, 
			541, 542, 543, 544, 545, 546, 547, 548, 549, 550, 551, 552, 553, 554, 555, 556, 
			557, 558, 559, 560, 561, 562, 563, 564, 565, 566, 567, 568, 569, 570, 571, 572, 
			573, 574, 575, 576, 577, 578, 579, 580, 581, 582, 583, 584, 585, 586, 587, 588, 
			589, 590, 591, 592, 593, 594, 595, 596, 597, 598, 599, 600, 601, 602, 603, 604, 
			605, 606, 607, 608, 609, 610, 611, 612, 613, 614, 615, 616, 617, 618, 619, 620, 
			621, 622, 623, 624, 625, 626, 627, 628, 629, 630, 631, 632, 633, 634, 635, 636, 
			637, 638, 639, 640, 641, 642, 643, 644, 645, 646, 647, 648, 649, 650, 651, 652, 
			653, 654, 655, 656, 538, 657, 658, 659, 660, 661, 16, 662, 663, 664, 665, 666, 
			667, 668, 669, 670, 671, 672, 673, 674, 675, 676, 677, 678, 679, 680
		};
		
		private static int[,] nextState = new int[,]
		{
			{ 1, 2, 260, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 2, 258, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 314, -1, -1, -1, -1, -1, -1, -1, -1, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 19, -1, -1, -1, 20, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 877, -1, 874, 874, 874, 874, 874, 619, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 620, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, -1 },
			{ -1, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 31, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, -1, 381, 381, 381, 383, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, -1, 381, 381, 381 },
			{ -1, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 32, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 387, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, -1, 385, 385, 385 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 391, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 13, 13, -1, -1, -1, -1, -1, -1, 41, -1, -1, -1, -1, -1, 13, -1, -1, 13, -1, -1, 13, -1, 13, 13, 13, -1, -1, -1, -1, -1, 13, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 395, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 266, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 306, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 415, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 57, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 268, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 307, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 846, -1, 788, 874, 874, 874, 58, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 789, -1, 825, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 65, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 425, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 792, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 866, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 391, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, 41, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, -1, -1, 41, -1, -1, 41, -1, 41, 41, 41, -1, -1, -1, -1, -1, 41, -1, -1 },
			{ -1, -1, -1, -1, 56, -1, 56, 56, 56, 56, 56, -1, 56, 56, 56, 56, 56, 56, -1, 56, -1, -1, -1, -1, -1, -1, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 56, 56, -1, -1, 56, -1, -1, 56, -1, 56, 56, 56, -1, -1, -1, -1, -1, 56, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 686, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 386, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 698, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, 70, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, -1, -1, 70, -1, -1, 70, -1, 70, 70, 70, -1, -1, -1, -1, -1, 70, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 72, -1, -1, -1, -1, -1, -1, 72, -1, -1, -1, 72, -1, -1, -1, -1, -1, -1, -1, -1, 72, -1, 72, -1, -1, 72, -1, -1, -1, -1, -1, -1, 72, 72, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 72, -1, -1, 72, -1, -1, 72, -1, 72, 72, 72, -1, -1, -1, -1, -1, 72, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 434, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 84, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 84, 84, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 84, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 750, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 813, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 756, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 884, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, -1, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, 179, -1, 179, 179, 179 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 270, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 180, 180, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 180, -1, -1, 180, -1, -1, 180, -1, 180, 180, 180, -1, -1, -1, -1, -1, 180, 270, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 181, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 181, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 182, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 182, -1 },
			{ -1, 183, 183, -1, -1, 183, -1, -1, -1, -1, -1, 183, -1, -1, -1, -1, -1, -1, 183, -1, -1, 183, 183, 183, 183, 183, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 183, 183, 183, 183, 183, 183, 183, 183, 183, -1, -1, -1, 183, -1, -1, 183, 183, -1, 183, -1, -1, -1, -1, -1, -1, 183, -1, -1, 183, -1 },
			{ -1, -1, -1, 184, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 185, -1, 185, 185, 185, 185, 185, -1, 185, 185, 185, 185, 185, 185, -1, 185, -1, -1, -1, -1, -1, -1, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 185, 185, -1, -1, 185, -1, -1, 185, -1, 185, 185, 185, -1, -1, -1, -1, -1, 185, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, 187, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, -1, -1, 187, -1, -1, 187, -1, 187, 187, 187, -1, -1, -1, -1, -1, 187, -1, -1 },
			{ -1, 191, 191, 191, 192, 191, 192, 192, 192, 192, 192, 191, 192, 192, 192, 192, 192, 192, 191, 192, 191, 191, 191, 191, 191, 191, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, 191, 191, 192, 191, 191, 191, 191, 191, 191, 191, 191, 191, 191, 192, 191, 191, 191, 191, 191, 191, 191, 191, 191, 191, 191, 193, 191, 191, 191, -1, 191, 191, 191 },
			{ -1, 194, 194, 194, 194, 194, 195, 196, 194, 194, 196, 194, 194, 194, 196, 194, 197, 194, 194, 194, 198, 194, 194, 194, 194, 194, 194, 275, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 199, 199, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 196, 194, 194, 194, 196, 199, 194, 194, 194, 194, 199, 199, 199, 200, 194, 194, 194, -1, 199, 194, 194 },
			{ -1, -1, -1, -1, 192, -1, 192, 192, 192, 192, 192, -1, 192, 192, 192, 192, 192, 192, -1, 192, -1, -1, -1, -1, -1, -1, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 192, 192, -1, -1, 192, -1, -1, 192, -1, 192, 192, 192, -1, -1, -1, -1, -1, 192, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 567, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 891, -1, -1, -1, -1, -1, -1, 891, -1, -1, -1, 891, -1, -1, -1, -1, -1, -1, -1, -1, 891, -1, 891, -1, -1, 891, -1, -1, -1, -1, -1, -1, 891, 891, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 891, -1, -1, 891, -1, -1, 891, -1, 891, 891, 891, -1, -1, -1, -1, -1, 891, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 588, 588, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 588, -1, -1, -1, -1, 588, 588, 588, -1, -1, -1, -1, -1, 588, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, -1, 279, -1, -1, -1, 279, -1, -1, -1, -1, -1, -1, -1, -1, 279, -1, 279, -1, -1, 279, -1, -1, -1, -1, -1, -1, 279, 279, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 279, -1, -1, 279, -1, -1, 279, -1, 279, 279, 279, -1, -1, -1, -1, -1, 279, -1, -1 },
			{ -1, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, -1, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 570, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, -1, 203, 203, 203 },
			{ -1, 205, 205, -1, -1, 205, -1, -1, -1, -1, -1, 205, -1, -1, -1, -1, -1, -1, 205, -1, -1, 205, 205, 205, 205, 205, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 205, 205, 205, 205, 205, 205, 205, 205, 205, -1, -1, -1, 205, -1, -1, 205, 205, -1, 205, -1, -1, -1, -1, -1, -1, 205, -1, -1, 205, -1 },
			{ -1, -1, -1, -1, 207, -1, 207, 207, 207, 207, 207, -1, 207, 207, 207, 207, 207, 207, -1, 207, -1, -1, -1, -1, -1, -1, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 207, 207, -1, -1, 207, -1, -1, 207, -1, 207, 207, 207, -1, -1, -1, -1, -1, 207, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 208, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 209, 209, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 209, -1, -1, 209, -1, -1, 209, -1, 209, 209, 209, -1, -1, -1, -1, -1, 209, -1, -1 },
			{ -1, 213, 213, 213, 214, 213, 214, 214, 214, 214, 214, 213, 214, 214, 214, 214, 214, 214, 213, 214, 213, 213, 213, 213, 213, 213, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, 213, 213, 214, 213, 213, 213, 213, 213, 213, 213, 213, 213, 213, 214, 213, 213, 213, 213, 213, 213, 213, 213, 213, 213, 213, 215, 213, 213, 213, -1, 213, 213, 213 },
			{ -1, 216, 216, 217, 216, 216, 218, 219, 216, 216, 219, 216, 216, 216, 219, 216, 220, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 284, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 221, 221, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 219, 216, 216, 216, 219, 221, 216, 216, 216, 216, 221, 221, 221, 222, 216, 216, 216, -1, 221, 216, 216 },
			{ -1, -1, -1, -1, 214, -1, 214, 214, 214, 214, 214, -1, 214, 214, 214, 214, 214, 214, -1, 214, -1, -1, -1, -1, -1, -1, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 214, 214, -1, -1, 214, -1, -1, 214, -1, 214, 214, 214, -1, -1, -1, -1, -1, 214, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 573, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 892, -1, -1, -1, -1, -1, -1, 892, -1, -1, -1, 892, -1, -1, -1, -1, -1, -1, -1, -1, 892, -1, 892, -1, -1, 892, -1, -1, -1, -1, -1, -1, 892, 892, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 892, -1, -1, 892, -1, -1, 892, -1, 892, 892, 892, -1, -1, -1, -1, -1, 892, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 590, 590, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 590, -1, -1, -1, -1, 590, 590, 590, -1, -1, -1, -1, -1, 590, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, -1, 288, -1, -1, -1, 288, -1, -1, -1, -1, -1, -1, -1, -1, 288, -1, 288, -1, -1, 288, -1, -1, -1, -1, -1, -1, 288, 288, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 288, -1, -1, 288, -1, -1, 288, -1, 288, 288, 288, -1, -1, -1, -1, -1, 288, -1, -1 },
			{ -1, 225, 225, -1, -1, 225, -1, -1, -1, -1, -1, 225, -1, -1, -1, -1, -1, -1, 225, -1, -1, 225, 225, 225, 225, 225, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 225, 225, 225, 225, 225, 225, 225, 225, 225, -1, -1, -1, 225, -1, -1, 225, 225, -1, 225, -1, -1, -1, -1, -1, -1, 225, -1, -1, 225, -1 },
			{ -1, -1, -1, 226, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 226, 226, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 227, -1, 227, 227, 227, 227, 227, -1, 227, 227, 227, 227, 227, 227, -1, 227, -1, -1, -1, -1, -1, -1, 227, 227, 227, 227, 227, 227, 227, 227, 227, 227, 227, 227, 227, 227, 227, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 227, 227, -1, -1, 227, -1, -1, 227, -1, 227, 227, 227, -1, -1, -1, -1, -1, 227, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 228, 228, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 228, -1, -1, 228, -1, -1, 228, -1, 228, 228, 228, -1, -1, -1, -1, -1, 228, -1, -1 },
			{ -1, 232, 232, 232, 233, 232, 233, 233, 233, 233, 233, 232, 233, 233, 233, 233, 233, 233, 232, 233, 232, 232, 232, 232, 232, 232, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 232, 232, 233, 232, 232, 232, 232, 232, 232, 232, 232, 232, 232, 233, 232, 232, 232, 232, 232, 232, 232, 232, 232, 232, 232, 234, 232, 232, 232, -1, 232, 232, 232 },
			{ -1, 235, 235, 235, 235, 235, 236, 237, 235, 235, 237, 235, 235, 235, 237, 235, 238, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 294, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 239, 239, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 237, 235, 235, 235, 237, 239, 235, 235, 235, 235, 239, 239, 239, 240, 235, 235, 235, -1, 239, 235, 235 },
			{ -1, -1, -1, -1, 233, -1, 233, 233, 233, 233, 233, -1, 233, 233, 233, 233, 233, 233, -1, 233, -1, -1, -1, -1, -1, -1, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 233, 233, -1, -1, 233, -1, -1, 233, -1, 233, 233, 233, -1, -1, -1, -1, -1, 233, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 578, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 893, -1, -1, -1, -1, -1, -1, 893, -1, -1, -1, 893, -1, -1, -1, -1, -1, -1, -1, -1, 893, -1, 893, -1, -1, 893, -1, -1, -1, -1, -1, -1, 893, 893, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 893, -1, -1, 893, -1, -1, 893, -1, 893, 893, 893, -1, -1, -1, -1, -1, 893, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 592, 592, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 592, -1, -1, -1, -1, 592, 592, 592, -1, -1, -1, -1, -1, 592, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 298, -1, -1, -1, -1, -1, -1, 298, -1, -1, -1, 298, -1, -1, -1, -1, -1, -1, -1, -1, 298, -1, 298, -1, -1, 298, -1, -1, -1, -1, -1, -1, 298, 298, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 298, -1, -1, 298, -1, -1, 298, -1, 298, 298, 298, -1, -1, -1, -1, -1, 298, -1, -1 },
			{ -1, -1, -1, -1, 245, -1, 245, 245, 245, 245, 245, -1, 245, 245, 245, 245, 245, 245, -1, 245, -1, -1, -1, -1, -1, -1, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 245, 245, -1, -1, 245, -1, -1, 245, -1, 245, 245, 245, -1, -1, -1, -1, -1, 245, -1, -1 },
			{ -1, -1, -1, -1, 247, -1, 247, 247, 247, 247, 247, -1, 247, 247, 247, 247, 247, 247, -1, 247, -1, -1, -1, -1, -1, -1, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 247, 247, -1, -1, 247, -1, -1, 247, -1, 247, 247, 247, -1, -1, -1, -1, -1, 247, -1, -1 },
			{ -1, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, -1, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, -1, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 250, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, -1, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, -1, 251, 251, 251 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 253, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 257, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 255, 255, 255, -1, 255, 255, 255, 255, 255, -1, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, -1, 255, -1, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, -1, 255, 255, 255 },
			{ -1, -1, -1, 2, -1, -1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, 577, -1, 577, 577, 577, 577, 577, -1, 577, 577, 577, 577, 577, 577, -1, 577, -1, -1, -1, -1, -1, -1, 577, 577, 577, 577, 577, 577, 577, 577, 577, 577, 577, 577, -1, -1, 577, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 577, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 3, -1, 2, 303, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 21, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 22, -1, -1, -1, 23, -1, -1, 377, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 24, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 625, -1, 874, 874, 874, 874, 874, -1, 874, 874, 26, 874, 874, 874, -1, 874, -1, 379, -1, -1, -1, -1, 874, 874, 27, 874, 874, 874, 874, 874, 874, 874, 626, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 391, -1, -1, -1, -1, -1, -1, -1, -1, -1, 393, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 13, 13, -1, -1, -1, -1, -1, -1, 41, -1, -1, -1, -1, -1, 13, -1, -1, 13, -1, -1, 13, -1, 13, 13, 13, -1, -1, -1, -1, -1, 13, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 87, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 270, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 270, -1 },
			{ -1, 183, 183, -1, -1, 183, -1, -1, -1, -1, -1, 183, -1, -1, -1, -1, -1, -1, 183, -1, -1, 183, 190, 183, 183, 183, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 183, 183, 183, 183, 183, 183, 183, 183, 183, -1, -1, -1, 183, -1, -1, 183, 183, -1, 183, -1, -1, -1, -1, -1, -1, 183, -1, -1, 183, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 566, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, 187, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, -1, -1, 187, -1, -1, 187, -1, 187, 187, 187, -1, -1, -1, -1, -1, 187, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 201, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 202, -1, -1, -1, -1, -1, -1, 202, -1, -1, -1, 202, -1, -1, -1, -1, -1, -1, -1, -1, 202, -1, 202, -1, -1, 202, -1, -1, -1, -1, -1, -1, 202, 202, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 202, -1, -1, 202, -1, -1, 202, -1, 202, 202, 202, -1, -1, -1, -1, -1, 202, -1, -1 },
			{ -1, 205, 205, -1, -1, 205, -1, -1, -1, -1, -1, 205, -1, -1, -1, -1, -1, -1, 205, -1, -1, 205, 212, 205, 205, 205, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 205, 205, 205, 205, 205, 205, 205, 205, 205, -1, -1, -1, 205, -1, -1, 205, 205, -1, 205, -1, -1, -1, -1, -1, -1, 205, -1, -1, 205, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 572, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 209, 209, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 209, -1, -1, 209, -1, -1, 209, -1, 209, 209, 209, -1, -1, -1, -1, -1, 209, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 223, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 224, -1, -1, -1, -1, -1, -1, 224, -1, -1, -1, 224, -1, -1, -1, -1, -1, -1, -1, -1, 224, -1, 224, -1, -1, 224, -1, -1, -1, -1, -1, -1, 224, 224, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 224, -1, -1, 224, -1, -1, 224, -1, 224, 224, 224, -1, -1, -1, -1, -1, 224, -1, -1 },
			{ -1, 225, 225, -1, -1, 225, -1, -1, -1, -1, -1, 225, -1, -1, -1, -1, -1, -1, 225, -1, -1, 225, 231, 225, 225, 225, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 225, 225, 225, 225, 225, 225, 225, 225, 225, -1, -1, -1, 225, -1, -1, 225, 225, -1, 225, -1, -1, -1, -1, -1, -1, 225, -1, -1, 225, -1 },
			{ -1, 225, 225, 226, -1, 225, -1, -1, -1, -1, -1, 225, -1, -1, -1, -1, -1, -1, 225, -1, 226, 290, 225, 225, 225, 225, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 225, 225, 225, 225, 225, 225, 225, 225, 225, -1, -1, -1, 225, -1, -1, 225, 225, -1, 225, -1, -1, -1, -1, -1, -1, 225, -1, -1, 225, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 576, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 228, 228, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 228, -1, -1, 228, -1, -1, 228, -1, 228, 228, 228, -1, -1, -1, -1, -1, 228, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 241, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 242, -1, -1, -1, -1, -1, -1, 242, -1, -1, -1, 242, -1, -1, -1, -1, -1, -1, -1, -1, 242, -1, 242, -1, -1, 242, -1, -1, -1, -1, -1, -1, 242, 242, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 242, -1, -1, 242, -1, -1, 242, -1, 242, 242, 242, -1, -1, -1, -1, -1, 242, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 243, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 256, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 317, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 302, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 25, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 28, -1, 874, 822, 874, 874, 874, -1, 874, 874, 328, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 784, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 266, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 268, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 308, -1, -1, -1, -1, -1, -1, 308, -1, -1, -1, 308, -1, -1, -1, -1, -1, -1, -1, -1, 308, -1, 308, -1, -1, 308, -1, -1, -1, -1, -1, -1, 308, 308, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 308, -1, -1, 308, -1, -1, 308, -1, 308, 308, 308, -1, -1, -1, -1, -1, 308, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 310, -1, -1, -1, -1, -1, -1, 310, -1, -1, -1, 310, -1, -1, -1, -1, -1, -1, -1, -1, 310, -1, 310, -1, -1, 310, -1, -1, -1, -1, -1, -1, 310, 310, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 310, -1, -1, 310, -1, -1, 310, -1, 310, 310, 310, -1, -1, -1, -1, -1, 310, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, 312, -1, -1, 312, -1, -1, -1, -1, -1, -1, 312, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, 312, -1, -1, 312, -1, 312, 312, 312, -1, -1, -1, -1, -1, 312, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 29, -1, -1, -1, 30, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 337, 874, 874, -1, 874, 874, 874, 874, 874, 640, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 38, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 593, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 33, -1, -1, -1, 34, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 39, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 643, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 35, -1, -1, -1, -1, -1, -1, 36, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 37, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 389, -1, 874, -1, 874, 645, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, 11, 12, -1, -1, -1, -1, 874, 874, 874, 646, 874, 874, 874, 874, 874, 874, 874, 40, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, 262, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 42, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 43, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 355, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 60, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 329, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 44, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 45, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 61, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 332, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 332, 332, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 332, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 46, -1, -1, -1, 47, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 48, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 62, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 332, 335, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 332, 332, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 332, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 267, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 63, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 338, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 49, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 66, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 341, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 50, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, 41, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, -1, -1, 41, -1, -1, 41, -1, 41, 41, 41, -1, -1, -1, -1, -1, 41, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 67, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 344, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 51, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 52, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 68, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 347, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 53, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 54, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 69, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 350, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 55, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 681, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 73, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 353, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 56, -1, 56, 56, 56, 56, 56, -1, 56, 56, 56, 56, 56, 56, -1, 56, -1, -1, -1, -1, -1, -1, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, -1, -1, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 74, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 356, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 76, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 356, -1, -1, -1, -1, -1, -1, 359, -1, -1, -1, -1, 356, 356, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 356, -1 },
			{ -1, -1, -1, -1, 397, -1, -1, 399, 401, -1, -1, -1, -1, 597, -1, -1, 403, -1, -1, -1, -1, -1, -1, 405, -1, -1, 407, -1, 409, 411, -1, 413, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 405, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 77, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 361, -1, 359, -1, -1, -1, -1, -1, -1, -1, -1, 363, 881, -1, 359, 359, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 359, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 78, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 594, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 79, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 365, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 80, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 81, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 367, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 261, 367, 367, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 367, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 82, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 371, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 83, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 367, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 85, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 367, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 86, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ 1, 7, 263, 8, 9, 304, 776, 819, 264, 843, 586, 10, 856, 305, 613, 865, 615, 871, 315, 874, 11, 12, 318, 10, 10, 321, 316, 616, 617, 319, 875, 322, 874, 618, 876, 874, 874, 874, 13, 13, 874, 324, 327, 330, 333, 336, 339, 342, 345, 348, 351, 874, 13, 354, 14, 265, 15, 357, 13, 354, 13, 13, 13, 16, 17, 18, 354, 1, 13, 10, 354 },
			{ -1, -1, -1, -1, 88, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 417, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 89, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, 419, 421, 421, 419, 419, 419, 419, 419, 419, 419, 421, 419, 419, 419, 419, 419, 419, 419, 419, 419, 59, 421, 419, 421, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 423, 419, 421, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, -1, 419, 419, 419 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 90, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 91, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, -1, 381, 381, 381 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 92, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 93, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, -1, 385, 385, 385 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 94, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, 427, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 95, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, 70, -1, -1, 429, 429, -1, -1, -1, -1, -1, -1, -1, -1, 70, -1, -1, 70, -1, -1, 70, -1, 70, 70, 70, -1, -1, -1, -1, -1, 70, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 96, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 97, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 431, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 98, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 433, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 99, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 598, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 100, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 435, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 101, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 437, -1, -1, -1, -1, -1, 439, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 102, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 103, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 441, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 104, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 443, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 105, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 445, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 106, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 600, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 601, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 107, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 447, -1, 447, 447, 447, 447, 447, -1, 447, 447, 447, 447, 447, 447, -1, 447, -1, -1, -1, 415, -1, -1, 447, 447, 447, 447, 447, 447, 447, 447, 447, 447, 447, 447, -1, -1, 447, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 447, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 415, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 109, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 781, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 110, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, 419, 421, 421, 419, 419, 419, 419, 419, 419, 419, 421, 419, 419, 419, 419, 419, 419, 419, 419, 419, 75, 421, 419, 421, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 423, 419, 421, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, -1, 419, 419, 419 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 111, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 59, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 449, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, -1, 421, 421, 421 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 112, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, 419, 596, 596, 419, 419, 419, 419, 419, 419, 419, 596, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 596, 419, 451, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, 596, 419, 419, 419, 419, 419, 419, 419, 419, 419, 419, -1, 419, 419, 419 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 113, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 114, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, 415, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 115, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 116, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 453, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 117, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 455, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 118, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 459, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 119, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 463, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 120, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 465, -1, -1, -1, 467, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 121, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 469, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 122, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 604, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 127, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 605, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 128, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 447, -1, 447, 447, 447, 447, 447, 87, 447, 447, 447, 447, 447, 447, -1, 447, -1, -1, -1, -1, 269, -1, 447, 447, 447, 447, 447, 447, 447, 447, 447, 447, 447, 447, 447, 447, 447, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 447, 447, -1, -1, 447, -1, -1, 447, -1, 447, 447, 447, -1, -1, -1, -1, -1, 447, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 129, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 451, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, 596, -1, 596, 596, 596 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 130, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 596, 421, 421, 421, 421, 421, 421, 421, 421, 421, 59, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 449, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, -1, 421, 421, 421 },
			{ -1, -1, -1, -1, 131, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 471, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 132, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 473, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 133, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 475, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 134, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 477, -1, -1, -1, -1, -1, 479, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 481, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 483, 108, 485, 487, -1, -1, -1, -1, -1, -1, -1, 479, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 135, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 489, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 136, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 491, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 137, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 144, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 495, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 145, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 497, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 146, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 507, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 147, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 606, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 148, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 475, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 123, -1, -1, -1, -1, -1, -1, -1, -1, -1, 475, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 149, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 509, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 150, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 479, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 108, -1, -1, -1, -1, -1, -1, -1, -1, -1, 479, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 151, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 511, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 157, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 483, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 124, -1, -1, -1, -1, -1, -1, -1, -1, -1, 483, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 158, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 513, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 159, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 479, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 160, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 515, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 161, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 517, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 519, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 521, 125, 523, 524, -1, -1, -1, -1, -1, -1, -1, 517, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 162, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 525, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 163, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 526, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 165, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 527, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 166, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 529, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 167, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 530, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 168, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 608, -1, -1, -1, -1, -1, 531, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 126, -1, -1, -1, -1, -1, -1, -1, -1, -1, 531, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 169, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 609, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 170, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 532, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 171, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 534, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 172, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 511, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 138, -1, -1, -1, -1, -1, -1, -1, -1, -1, 511, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 173, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 513, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 139, -1, -1, -1, -1, -1, -1, -1, -1, -1, 513, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 174, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 515, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 140, -1, -1, -1, -1, -1, -1, -1, -1, -1, 515, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 175, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 517, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 125, -1, -1, -1, -1, -1, -1, -1, -1, -1, 517, -1 },
			{ -1, -1, -1, -1, 874, -1, 176, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 177, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 521, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 141, -1, -1, -1, -1, -1, -1, -1, -1, -1, 521, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 178, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 536, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 517, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 525, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 142, -1, -1, -1, -1, -1, -1, -1, -1, -1, 525, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 475, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 528, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 143, -1, -1, -1, -1, -1, -1, -1, -1, -1, 528, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 538, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 539, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 531, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 126, -1, -1, -1, -1, -1, -1, -1, -1, -1, 531, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 542, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 542, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 152, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 479, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 153, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 536, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 154, -1, -1, -1, -1, -1, -1, -1, -1, -1, 536, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 543, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 538, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 155, -1, -1, -1, -1, -1, -1, -1, -1, -1, 538, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 539, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 156, -1, -1, -1, -1, -1, -1, -1, -1, -1, 539, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 531, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 25, 541, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 544, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 542, -1, -1, 545, -1, 610, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 542, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 543, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 164, -1, -1, -1, -1, -1, -1, -1, -1, -1, 543, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 546, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 547, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 549, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 550, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 611, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 551, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 552, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 554, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 554, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 555, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 554, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 180, 180, -1, -1, -1, 556, -1, -1, -1, -1, -1, -1, -1, -1, 180, -1, -1, 180, -1, -1, 180, -1, 180, 180, 180, -1, -1, -1, -1, -1, 180, 554, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 557, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 180, 180, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 180, -1, -1, 180, -1, -1, 180, -1, 180, 180, 180, -1, -1, -1, -1, -1, 180, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 559, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 559, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 560, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 559, -1, -1, -1, -1, 612, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 559, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 561, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 563, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 564, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 181, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 182, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ 1, 183, 183, 184, 185, 183, 185, 185, 185, 185, 185, 183, 185, 185, 185, 185, 185, 185, 183, 185, 186, 183, 183, 183, 183, 183, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 185, 187, 187, 185, 183, 183, 271, 183, 183, 183, 183, 183, 183, 188, 185, 187, 183, 189, 272, 183, 183, 187, 183, 187, 187, 187, 273, 274, 309, 183, 1, 187, 183, 309 },
			{ -1, -1, -1, -1, 568, -1, 568, 568, 568, 568, 568, -1, 568, 568, 568, 568, 568, 568, -1, 568, -1, -1, -1, -1, -1, -1, 568, 568, 568, 568, 568, 568, 568, 568, -1, 568, 568, 568, 568, 568, 568, -1, -1, 568, -1, -1, -1, -1, -1, -1, -1, 568, 568, -1, -1, 568, -1, -1, 568, -1, 568, 568, 568, -1, -1, -1, -1, -1, 568, 568, -1 },
			{ -1, -1, -1, -1, 568, -1, 568, 568, 568, 568, 568, -1, 568, 568, 568, 568, 568, 568, -1, 568, -1, -1, -1, -1, -1, -1, 568, 568, 568, 568, 568, 568, 568, 568, -1, 568, 568, 568, 568, 568, 568, -1, -1, 568, -1, -1, -1, -1, -1, -1, -1, 568, 568, -1, -1, 568, -1, -1, 568, -1, 568, 568, 568, -1, -1, 276, -1, -1, 568, 568, -1 },
			{ 1, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 204, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 570, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 1, 203, 203, 203 },
			{ -1, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, -1, 203, 203, 203 },
			{ 1, 205, 205, 206, 207, 205, 207, 207, 207, 207, 207, 205, 207, 207, 207, 207, 207, 207, 205, 207, 208, 205, 205, 205, 205, 205, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 209, 209, 207, 205, 205, 280, 205, 205, 205, 205, 205, 205, 210, 207, 209, 205, 211, 281, 205, 205, 209, 205, 209, 209, 209, 282, 283, 311, 205, 1, 209, 205, 311 },
			{ -1, -1, -1, -1, 574, -1, 574, 574, 574, 574, 574, -1, 574, 574, 574, 574, 574, 574, -1, 574, -1, -1, -1, -1, -1, -1, 574, 574, 574, 574, 574, 574, 574, 574, -1, 574, 574, 574, 574, 574, 574, -1, -1, 574, -1, -1, -1, -1, -1, -1, -1, 574, 574, -1, -1, 574, -1, -1, 574, -1, 574, 574, 574, -1, -1, -1, -1, -1, 574, 574, -1 },
			{ -1, -1, -1, -1, 574, -1, 574, 574, 574, 574, 574, -1, 574, 574, 574, 574, 574, 574, -1, 574, -1, -1, -1, -1, -1, -1, 574, 574, 574, 574, 574, 574, 574, 574, -1, 574, 574, 574, 574, 574, 574, -1, -1, 574, -1, -1, -1, -1, -1, -1, -1, 574, 574, -1, -1, 574, -1, -1, 574, -1, 574, 574, 574, -1, -1, 285, -1, -1, 574, 574, -1 },
			{ 1, 225, 225, 226, 227, 225, 227, 227, 227, 227, 227, 225, 227, 227, 227, 227, 227, 227, 225, 227, 226, 290, 225, 225, 225, 225, 227, 227, 227, 227, 227, 227, 227, 227, 227, 227, 227, 227, 228, 228, 227, 225, 225, 289, 225, 225, 225, 225, 225, 225, 229, 227, 228, 225, 230, 291, 225, 225, 228, 225, 228, 228, 228, 292, 293, 313, 225, 259, 228, 225, 313 },
			{ -1, -1, -1, -1, 577, -1, 577, 577, 577, 577, 577, 243, 577, 577, 577, 577, 577, 577, -1, 577, -1, -1, -1, -1, 299, -1, 577, 577, 577, 577, 577, 577, 577, 577, 577, 577, 577, 577, 577, 577, 577, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 577, 577, -1, -1, 577, -1, -1, 577, -1, 577, 577, 577, -1, -1, -1, 579, -1, 577, -1, -1 },
			{ -1, -1, -1, -1, 580, -1, 580, 580, 580, 580, 580, -1, 580, 580, 580, 580, 580, 580, -1, 580, -1, -1, -1, -1, -1, -1, 580, 580, 580, 580, 580, 580, 580, 580, -1, 580, 580, 580, 580, 580, 580, -1, -1, 580, -1, -1, -1, -1, -1, -1, -1, 580, 580, -1, -1, 580, -1, -1, 580, -1, 580, 580, 580, -1, -1, -1, -1, -1, 580, 580, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 243, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 299, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 580, -1, 580, 580, 580, 580, 580, -1, 580, 580, 580, 580, 580, 580, -1, 580, -1, -1, -1, -1, -1, -1, 580, 580, 580, 580, 580, 580, 580, 580, -1, 580, 580, 580, 580, 580, 580, -1, -1, 580, -1, -1, -1, -1, -1, -1, -1, 580, 580, -1, -1, 580, -1, -1, 580, -1, 580, 580, 580, -1, -1, 295, -1, -1, 580, 580, -1 },
			{ 1, 244, 244, 244, 245, 244, 245, 245, 245, 245, 245, 244, 245, 245, 245, 245, 245, 245, 244, 245, 244, 244, 244, 244, 244, 244, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 244, 244, 245, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 245, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 1, 244, 244, 244 },
			{ 1, 246, 246, 246, 247, 246, 247, 247, 247, 247, 247, 246, 247, 247, 247, 247, 247, 247, 246, 247, 246, 246, 246, 246, 246, 246, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 246, 246, 247, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 247, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 1, 246, 246, 246 },
			{ 1, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 249, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 1, 248, 248, 248 },
			{ 1, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 252, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 1, 251, 251, 251 },
			{ 1, 254, 255, 255, 255, 254, 255, 255, 255, 255, 255, 256, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 300, 255, 301, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 1, 255, 255, 255 },
			{ -1, -1, -1, -1, 874, -1, 874, 325, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 786, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 277, -1, -1, -1, -1, -1, -1, 277, -1, -1, -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, 277, -1, 277, -1, -1, 277, -1, -1, -1, -1, -1, -1, 277, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 277, -1, -1, 277, -1, -1, 277, -1, 277, 277, 277, -1, -1, -1, -1, -1, 277, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 278, 278, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 278, -1, -1, -1, -1, 278, 278, 278, -1, -1, -1, -1, -1, 278, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 286, -1, -1, -1, -1, -1, -1, 286, -1, -1, -1, 286, -1, -1, -1, -1, -1, -1, -1, -1, 286, -1, 286, -1, -1, 286, -1, -1, -1, -1, -1, -1, 286, 286, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 286, -1, -1, 286, -1, -1, 286, -1, 286, 286, 286, -1, -1, -1, -1, -1, 286, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 287, 287, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 287, -1, -1, -1, -1, 287, 287, 287, -1, -1, -1, -1, -1, 287, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 296, -1, -1, -1, -1, -1, -1, 296, -1, -1, -1, 296, -1, -1, -1, -1, -1, -1, -1, -1, 296, -1, 296, -1, -1, 296, -1, -1, -1, -1, -1, -1, 296, 296, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 296, -1, -1, 296, -1, -1, 296, -1, 296, 296, 296, -1, -1, -1, -1, -1, 296, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 297, 297, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 297, -1, -1, -1, -1, 297, 297, 297, -1, -1, -1, -1, -1, 297, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 326, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 367, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 373, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 599, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 457, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 461, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 614, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 603, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 505, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 503, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 607, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 499, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 528, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 540, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 553, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 562, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 631, 874, 874, 632, 331, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 501, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 334, -1, 874, 874, 874, 874, 874, -1, 874, 874, 821, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 340, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 641, 783, 874, 874, -1, 874, 642, 874, 874, 820, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 343, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 346, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 349, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 790, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 826, 874, 874, 874, -1, 874, 649, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 650, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 352, -1, 874, 874, 874, 874, 651, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 652, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 823, -1, 874, 874, 874, 874, 653, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 791, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 654, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 824, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 655, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 859, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 656, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 844, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 358, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 845, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 360, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 659, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 362, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 364, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 366, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 662, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 368, 874, 858, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 370, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 664, -1, 857, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 665, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 372, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 667, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 872, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 668, 874, 874, -1, 874, 874, 874, 874, 874, 669, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 670, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 374, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 671, -1, 672, 874, 874, 874, 673, -1, 674, 827, 794, 675, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 676, 874, 677, 874, 828, 874, 874, 874, 874, 874, 678, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 680, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 376, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 378, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 380, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 382, -1, 874, 874, 874, 874, 882, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 684, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 384, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 388, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 860, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 829, -1, 874, 874, 874, 874, 874, 687, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 390, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 392, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 394, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 692, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 396, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 693, -1, 874, 874, 398, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 849, 874, 694, 874, 695, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 400, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 861, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 797, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 402, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 404, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 406, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 408, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 410, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 869, 874, 874, 874, 874, 412, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 802, 700, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 832, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 800, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 851, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 414, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 850, 874, 874, -1, 874, 874, 874, 874, 874, 885, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 702, 874, 874, -1, 874, 874, 874, 874, 863, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 833, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 416, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 418, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 420, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 422, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 704, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 424, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 706, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 707, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 426, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 428, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 430, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 711, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 804, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 432, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 712, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 436, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 806, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 438, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 836, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 717, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 440, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 442, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 724, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 809, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 839, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 855, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 444, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 446, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 729, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 448, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 808, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 450, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 853, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 838, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 452, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 733, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 454, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 456, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 458, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 460, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 462, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 837, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 735, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 736, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 811, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 854, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 464, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 740, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 466, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 468, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 743, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 470, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 749, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 472, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 751, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 474, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 815, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 476, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 478, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 841, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 480, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 755, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 482, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 757, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 484, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 758, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 486, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 488, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 490, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 492, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 715, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 759, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 760, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 494, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 817, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 764, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 765, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 766, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 496, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 498, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 500, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 502, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 767, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 504, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 506, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 508, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 510, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 770, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 772, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 512, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 773, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 514, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 516, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 518, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 775, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 520, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 522, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 621, 622, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 623, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 587, -1, -1, -1, -1, -1, -1, 587, -1, -1, -1, 587, -1, -1, -1, -1, -1, -1, -1, -1, 587, -1, 587, -1, -1, 587, -1, -1, -1, -1, -1, -1, 587, 587, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 587, -1, -1, 587, -1, -1, 587, -1, 587, 587, 587, -1, -1, -1, -1, -1, 587, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 589, -1, -1, -1, -1, -1, -1, 589, -1, -1, -1, 589, -1, -1, -1, -1, -1, -1, -1, -1, 589, -1, 589, -1, -1, 589, -1, -1, -1, -1, -1, -1, 589, 589, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 589, -1, -1, 589, -1, -1, 589, -1, 589, 589, 589, -1, -1, -1, -1, -1, 589, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 591, -1, -1, -1, -1, -1, -1, 591, -1, -1, -1, 591, -1, -1, -1, -1, -1, -1, -1, -1, 591, -1, 591, -1, -1, 591, -1, -1, -1, -1, -1, -1, 591, 591, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 591, -1, -1, 591, -1, -1, 591, -1, 591, 591, 591, -1, -1, -1, -1, -1, 591, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 595, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 602, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 679, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 666, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 867, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 663, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 657, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 660, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 685, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 689, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 682, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 795, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 696, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 697, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 701, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 705, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 803, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 716, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 834, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 713, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 721, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 723, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 720, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 864, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 810, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 731, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 732, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 886, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 745, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 737, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 747, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 814, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 754, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 761, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 763, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 762, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 768, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 769, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 771, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 624, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 793, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 661, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 658, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 847, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 796, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 690, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 683, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 801, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 703, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 710, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 709, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 718, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 807, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 726, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 730, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 738, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 734, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 752, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 748, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 739, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 816, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 887, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 774, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 627, 874, 874, 874, -1, 874, 628, 874, 874, 629, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 688, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 691, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 848, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 862, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 873, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 805, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 725, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 722, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 742, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 746, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 753, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 741, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 630, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 868, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 799, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 830, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 708, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 714, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 728, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 835, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 744, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 633, 874, 874, 874, -1, 787, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 634, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 831, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 798, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 719, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 727, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 635, 874, 874, 874, 874, -1, 636, 874, 637, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 638, 874, 874, 874, 874, 874, 639, 874, 874, 785, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 699, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 852, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 644, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 647, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 782, 874, 874, 874, -1, 874, 648, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 777, -1, -1, -1, -1, -1, -1, 777, -1, -1, -1, 777, -1, -1, -1, -1, -1, -1, -1, -1, 777, -1, 777, -1, -1, 777, -1, -1, -1, -1, -1, -1, 777, 777, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 777, -1, -1, 777, -1, -1, 777, -1, 777, 777, 777, -1, -1, -1, -1, -1, 777, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 778, -1, -1, -1, -1, -1, -1, 778, -1, -1, -1, 778, -1, -1, -1, -1, -1, -1, -1, -1, 778, -1, 778, -1, -1, 778, -1, -1, -1, -1, -1, -1, 778, 778, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 778, -1, -1, 778, -1, -1, 778, -1, 778, 778, 778, -1, -1, -1, -1, -1, 778, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 779, -1, -1, -1, -1, -1, -1, 779, -1, -1, -1, 779, -1, -1, -1, -1, -1, -1, -1, -1, 779, -1, 779, -1, -1, 779, -1, -1, -1, -1, -1, -1, 779, 779, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 779, -1, -1, 779, -1, -1, 779, -1, 779, 779, 779, -1, -1, -1, -1, -1, 779, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 780, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 870, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 812, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 818, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 883, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 840, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 874, 874, 874, 874, 874, -1, 874, 874, 874, 874, 874, 874, -1, 874, -1, -1, -1, -1, -1, -1, 874, 874, 874, 842, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, 874, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, -1, -1, 874, -1, -1, 874, -1, 874, 874, 874, -1, -1, -1, -1, -1, 874, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 878, -1, -1, -1, -1, -1, -1, 878, -1, -1, -1, 878, -1, -1, -1, -1, -1, -1, -1, -1, 878, -1, 878, -1, -1, 878, -1, -1, -1, -1, -1, -1, 878, 878, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 878, -1, -1, 878, -1, -1, 878, -1, 878, 878, 878, -1, -1, -1, -1, -1, 878, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 879, -1, -1, -1, -1, -1, -1, 879, -1, -1, -1, 879, -1, -1, -1, -1, -1, -1, -1, -1, 879, -1, 879, -1, -1, 879, -1, -1, -1, -1, -1, -1, 879, 879, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 879, -1, -1, 879, -1, -1, 879, -1, 879, 879, 879, -1, -1, -1, -1, -1, 879, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 880, -1, -1, -1, -1, -1, -1, 880, -1, -1, -1, 880, -1, -1, -1, -1, -1, -1, -1, -1, 880, -1, 880, -1, -1, 880, -1, -1, -1, -1, -1, -1, 880, 880, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 880, -1, -1, 880, -1, -1, 880, -1, 880, 880, 880, -1, -1, -1, -1, -1, 880, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 888, -1, -1, -1, -1, -1, -1, 888, -1, -1, -1, 888, -1, -1, -1, -1, -1, -1, -1, -1, 888, -1, 888, -1, -1, 888, -1, -1, -1, -1, -1, -1, 888, 888, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 888, -1, -1, 888, -1, -1, 888, -1, 888, 888, 888, -1, -1, -1, -1, -1, 888, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 889, -1, -1, -1, -1, -1, -1, 889, -1, -1, -1, 889, -1, -1, -1, -1, -1, -1, -1, -1, 889, -1, 889, -1, -1, 889, -1, -1, -1, -1, -1, -1, 889, 889, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 889, -1, -1, 889, -1, -1, 889, -1, 889, 889, 889, -1, -1, -1, -1, -1, 889, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 890, -1, -1, -1, -1, -1, -1, 890, -1, -1, -1, 890, -1, -1, -1, -1, -1, -1, -1, -1, 890, -1, 890, -1, -1, 890, -1, -1, -1, -1, -1, -1, 890, 890, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 890, -1, -1, 890, -1, -1, 890, -1, 890, 890, 890, -1, -1, -1, -1, -1, 890, -1, -1 }
		};
		
		
		private static int[] yy_state_dtrans = new int[]
		{
			  0,
			  375,
			  565,
			  569,
			  571,
			  575,
			  581,
			  582,
			  583,
			  584,
			  585
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
							System.Diagnostics.Debug.Assert(last_accept_state >= 894);
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

