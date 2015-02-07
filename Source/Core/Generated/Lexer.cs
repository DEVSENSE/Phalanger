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
					// #line 198
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
					// #line 199
					{ return Tokens.T_IMPORT; }
					break;
					
				case 115:
					// #line 236
					{ return Tokens.T_PARENT; }
					break;
					
				case 116:
					// #line 193
					{ return Tokens.T_PUBLIC; }
					break;
					
				case 117:
					// #line 226
					{ return Tokens.T_ASSERT; }
					break;
					
				case 118:
					// #line 166
					{ return Tokens.T_GLOBAL; }
					break;
					
				case 119:
					// #line 138
					{ return Tokens.T_ELSEIF; }
					break;
					
				case 120:
					// #line 145
					{ return Tokens.T_ENDFOR; }
					break;
					
				case 121:
					// #line 205
					{ return Tokens.T_DOUBLE_TYPE; }
					break;
					
				case 122:
					// #line 208
					{ return Tokens.T_OBJECT_TYPE; }
					break;
					
				case 123:
					// #line 229
					{ return Tokens.T_CALL; }
					break;
					
				case 124:
					// #line 299
					{ return Tokens.T_DOUBLE_CAST; }
					break;
					
				case 125:
					// #line 291
					{ return Tokens.T_INT8_CAST; }
					break;
					
				case 126:
					// #line 297
					{ return Tokens.T_UINT32_CAST; }
					break;
					
				case 127:
					// #line 306
					{ return Tokens.T_BOOL_CAST; }
					break;
					
				case 128:
					// #line 164
					{ return Tokens.T_REQUIRE; }
					break;
					
				case 129:
					// #line 162
					{ return Tokens.T_INCLUDE; }
					break;
					
				case 130:
					// #line 191
					{ return Tokens.T_PRIVATE; }
					break;
					
				case 131:
					// #line 221
					{ return Tokens.T_PARTIAL; }
					break;
					
				case 132:
					// #line 158
					{ return Tokens.T_EXTENDS; }
					break;
					
				case 133:
					// #line 128
					{
					  return Tokens.ErrorNotSupported; 
					}
					break;
					
				case 134:
					// #line 152
					{ return Tokens.T_DEFAULT; }
					break;
					
				case 135:
					// #line 146
					{ return Tokens.T_FOREACH; }
					break;
					
				case 136:
					// #line 213
					{ return (InLinq) ? Tokens.T_LINQ_ORDERBY : Tokens.T_STRING; }
					break;
					
				case 137:
					// #line 235
					{ return Tokens.T_SLEEP; }
					break;
					
				case 138:
					// #line 181
					{ return Tokens.T_DIR; }
					break;
					
				case 139:
					// #line 294
					{ return Tokens.T_INT64_CAST; }
					break;
					
				case 140:
					// #line 292
					{ return Tokens.T_INT16_CAST; }
					break;
					
				case 141:
					// #line 304
					{ return Tokens.T_ARRAY_CAST; }
					break;
					
				case 142:
					// #line 295
					{ return Tokens.T_UINT8_CAST; }
					break;
					
				case 143:
					// #line 307
					{ return Tokens.T_UNSET_CAST; }
					break;
					
				case 144:
					// #line 300
					{ return Tokens.T_FLOAT_CAST; }
					break;
					
				case 145:
					// #line 154
					{ return Tokens.T_CONTINUE; }
					break;
					
				case 146:
					// #line 207
					{ return Tokens.T_RESOURCE_TYPE; }
					break;
					
				case 147:
					// #line 189
					{ return Tokens.T_ABSTRACT; }
					break;
					
				case 148:
					// #line 142
					{ return Tokens.T_ENDWHILE; }
					break;
					
				case 149:
					// #line 134
					{ return Tokens.T_FUNCTION; }
					break;
					
				case 150:
					// #line 179
					{ return Tokens.T_LINE; }
					break;
					
				case 151:
					// #line 180
					{ return Tokens.T_FILE; }
					break;
					
				case 152:
					// #line 234
					{ return Tokens.T_WAKEUP; }
					break;
					
				case 153:
					// #line 301
					{ return Tokens.T_STRING_CAST; }
					break;
					
				case 154:
					// #line 298
					{ return Tokens.T_UINT64_CAST; }
					break;
					
				case 155:
					// #line 296
					{ return Tokens.T_UINT16_CAST; }
					break;
					
				case 156:
					// #line 305
					{ return Tokens.T_OBJECT_CAST; }
					break;
					
				case 157:
					// #line 302
					{ return Tokens.T_BINARY_CAST; }
					break;
					
				case 158:
					// #line 209
					{ return Tokens.T_TYPEOF; }
					break;
					
				case 159:
					// #line 186
					{ return Tokens.T_INTERFACE; }
					break;
					
				case 160:
					// #line 192
					{ return Tokens.T_PROTECTED; }
					break;
					
				case 161:
					// #line 215
					{ return (InLinq) ? Tokens.T_LINQ_ASCENDING : Tokens.T_STRING; }
					break;
					
				case 162:
					// #line 197
					{ return Tokens.T_NAMESPACE; }
					break;
					
				case 163:
					// #line 150
					{ return Tokens.T_ENDSWITCH; }
					break;
					
				case 164:
					// #line 176
					{ return Tokens.T_CLASS_C; }
					break;
					
				case 165:
					// #line 303
					{ return Tokens.T_UNICODE_CAST; }
					break;
					
				case 166:
					// #line 194
					{ return Tokens.T_INSTANCEOF; }
					break;
					
				case 167:
					// #line 187
					{ return Tokens.T_IMPLEMENTS; }
					break;
					
				case 168:
					// #line 147
					{ return Tokens.T_ENDFOREACH; }
					break;
					
				case 169:
					// #line 214
					{ return (InLinq) ? Tokens.T_LINQ_DESCENDING : Tokens.T_STRING; }
					break;
					
				case 170:
					// #line 231
					{ return Tokens.T_TOSTRING; }
					break;
					
				case 171:
					// #line 238
					{ return Tokens.T_AUTOLOAD; }
					break;
					
				case 172:
					// #line 233
					{ return Tokens.T_DESTRUCT; }
					break;
					
				case 173:
					// #line 178
					{ return Tokens.T_METHOD_C; }
					break;
					
				case 174:
					// #line 232
					{ return Tokens.T_CONSTRUCT; }
					break;
					
				case 175:
					// #line 165
					{ return Tokens.T_REQUIRE_ONCE; }
					break;
					
				case 176:
					// #line 163
					{ return Tokens.T_INCLUDE_ONCE; }
					break;
					
				case 177:
					// #line 230
					{ return Tokens.T_CALLSTATIC; }
					break;
					
				case 178:
					// #line 177
					{ return Tokens.T_FUNC_C; }
					break;
					
				case 179:
					// #line 196
					{ return Tokens.T_NAMESPACE_C; }
					break;
					
				case 180:
					// #line 282
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_FILE; }
					break;
					
				case 181:
					// #line 281
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_LINE; }
					break;
					
				case 182:
					// #line 283
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_DEFAULT_LINE; }
					break;
					
				case 183:
					// #line 284
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_DEFAULT_FILE; }
					break;
					
				case 184:
					// #line 485
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 185:
					// #line 477
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 186:
					// #line 468
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 187:
					// #line 478
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_DOUBLE_QUOTES; }
					break;
					
				case 188:
					// #line 467
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 189:
					// #line 484
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 190:
					// #line 486
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 191:
					// #line 482
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 192:
					// #line 481
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 193:
					// #line 479
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 194:
					// #line 480
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 195:
					// #line 476
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 196:
					// #line 472
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 197:
					// #line 474
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 198:
					// #line 471
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 199:
					// #line 473
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 200:
					// #line 469
					{ return Tokens.OctalCharCode; }
					break;
					
				case 201:
					// #line 475
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 202:
					// #line 483
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 203:
					// #line 470
					{ return Tokens.HexCharCode; }
					break;
					
				case 204:
					// #line 427
					{ yymore(); break; }
					break;
					
				case 205:
					// #line 428
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.SingleQuotedString; }
					break;
					
				case 206:
					// #line 508
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 207:
					// #line 501
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_BACKQUOTE; }
					break;
					
				case 208:
					// #line 491
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 209:
					// #line 500
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 210:
					// #line 490
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 211:
					// #line 506
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 212:
					// #line 509
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 213:
					// #line 505
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 214:
					// #line 504
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 215:
					// #line 502
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 216:
					// #line 503
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 217:
					// #line 499
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 218:
					// #line 496
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 219:
					// #line 495
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 220:
					// #line 497
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 221:
					// #line 494
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 222:
					// #line 492
					{ return Tokens.OctalCharCode; }
					break;
					
				case 223:
					// #line 498
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 224:
					// #line 507
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 225:
					// #line 493
					{ return Tokens.HexCharCode; }
					break;
					
				case 226:
					// #line 463
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 227:
					// #line 456
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 228:
					// #line 448
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 229:
					// #line 447
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 230:
					// #line 461
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 231:
					// #line 464
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 232:
					// #line 460
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 233:
					// #line 459
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 234:
					// #line 457
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 235:
					// #line 458
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 236:
					// #line 455
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 237:
					// #line 452
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 238:
					// #line 453
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 239:
					// #line 451
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 240:
					// #line 449
					{ return Tokens.OctalCharCode; }
					break;
					
				case 241:
					// #line 454
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 242:
					// #line 462
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 243:
					// #line 450
					{ return Tokens.HexCharCode; }
					break;
					
				case 244:
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
					
				case 245:
					// #line 372
					{
						yyless(0);
						if (!yy_pop_state()) return Tokens.ERROR;
						break;
					}
					break;
					
				case 246:
					// #line 365
					{
						if (!yy_pop_state()) return Tokens.ERROR;
						inString = (CurrentLexicalState != LexicalStates.ST_IN_SCRIPTING); 
						isCode = true;
						return Tokens.T_STRING;
					}
					break;
					
				case 247:
					// #line 386
					{
						yyless(0);
						if (!yy_pop_state()) return Tokens.ERROR;
						yy_push_state(LexicalStates.ST_IN_SCRIPTING);
						break;
					}
					break;
					
				case 248:
					// #line 380
					{
						if (!yy_pop_state()) return Tokens.ERROR;
						yy_push_state(LexicalStates.ST_IN_SCRIPTING);
						return Tokens.T_STRING_VARNAME;
					}
					break;
					
				case 249:
					// #line 421
					{ yymore(); break; }
					break;
					
				case 250:
					// #line 423
					{ yymore(); break; }
					break;
					
				case 251:
					// #line 422
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_DOC_COMMENT; }
					break;
					
				case 252:
					// #line 415
					{ yymore(); break; }
					break;
					
				case 253:
					// #line 417
					{ yymore(); break; }
					break;
					
				case 254:
					// #line 416
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_COMMENT; }
					break;
					
				case 255:
					// #line 395
					{ yymore(); break; }
					break;
					
				case 256:
					// #line 396
					{ yymore(); break; }
					break;
					
				case 257:
					// #line 397
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_LINE_COMMENT; }
					break;
					
				case 258:
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
					
				case 261: goto case 2;
				case 262: goto case 4;
				case 263: goto case 6;
				case 264: goto case 7;
				case 265: goto case 9;
				case 266: goto case 13;
				case 267: goto case 20;
				case 268: goto case 23;
				case 269: goto case 25;
				case 270: goto case 87;
				case 271: goto case 181;
				case 272: goto case 184;
				case 273: goto case 188;
				case 274: goto case 189;
				case 275: goto case 190;
				case 276: goto case 195;
				case 277: goto case 196;
				case 278: goto case 198;
				case 279: goto case 200;
				case 280: goto case 203;
				case 281: goto case 206;
				case 282: goto case 210;
				case 283: goto case 211;
				case 284: goto case 212;
				case 285: goto case 217;
				case 286: goto case 219;
				case 287: goto case 221;
				case 288: goto case 222;
				case 289: goto case 225;
				case 290: goto case 226;
				case 291: goto case 227;
				case 292: goto case 229;
				case 293: goto case 230;
				case 294: goto case 231;
				case 295: goto case 236;
				case 296: goto case 237;
				case 297: goto case 239;
				case 298: goto case 240;
				case 299: goto case 243;
				case 300: goto case 244;
				case 301: goto case 255;
				case 302: goto case 257;
				case 304: goto case 2;
				case 305: goto case 7;
				case 306: goto case 9;
				case 307: goto case 20;
				case 308: goto case 25;
				case 309: goto case 188;
				case 310: goto case 189;
				case 311: goto case 210;
				case 312: goto case 211;
				case 313: goto case 229;
				case 314: goto case 230;
				case 316: goto case 7;
				case 317: goto case 9;
				case 319: goto case 7;
				case 320: goto case 9;
				case 322: goto case 7;
				case 323: goto case 9;
				case 325: goto case 7;
				case 326: goto case 9;
				case 328: goto case 7;
				case 329: goto case 9;
				case 331: goto case 7;
				case 332: goto case 9;
				case 334: goto case 7;
				case 335: goto case 9;
				case 337: goto case 7;
				case 338: goto case 9;
				case 340: goto case 7;
				case 341: goto case 9;
				case 343: goto case 7;
				case 344: goto case 9;
				case 346: goto case 7;
				case 347: goto case 9;
				case 349: goto case 7;
				case 350: goto case 9;
				case 352: goto case 7;
				case 353: goto case 9;
				case 355: goto case 7;
				case 356: goto case 9;
				case 358: goto case 7;
				case 359: goto case 9;
				case 361: goto case 9;
				case 363: goto case 9;
				case 365: goto case 9;
				case 367: goto case 9;
				case 369: goto case 9;
				case 371: goto case 9;
				case 373: goto case 9;
				case 375: goto case 9;
				case 377: goto case 9;
				case 379: goto case 9;
				case 381: goto case 9;
				case 383: goto case 9;
				case 385: goto case 9;
				case 387: goto case 9;
				case 389: goto case 9;
				case 391: goto case 9;
				case 393: goto case 9;
				case 395: goto case 9;
				case 397: goto case 9;
				case 399: goto case 9;
				case 401: goto case 9;
				case 403: goto case 9;
				case 405: goto case 9;
				case 407: goto case 9;
				case 409: goto case 9;
				case 411: goto case 9;
				case 413: goto case 9;
				case 415: goto case 9;
				case 417: goto case 9;
				case 419: goto case 9;
				case 421: goto case 9;
				case 423: goto case 9;
				case 425: goto case 9;
				case 427: goto case 9;
				case 429: goto case 9;
				case 431: goto case 9;
				case 433: goto case 9;
				case 435: goto case 9;
				case 437: goto case 9;
				case 439: goto case 9;
				case 441: goto case 9;
				case 443: goto case 9;
				case 445: goto case 9;
				case 447: goto case 9;
				case 449: goto case 9;
				case 451: goto case 9;
				case 453: goto case 9;
				case 455: goto case 9;
				case 457: goto case 9;
				case 459: goto case 9;
				case 461: goto case 9;
				case 463: goto case 9;
				case 465: goto case 9;
				case 467: goto case 9;
				case 469: goto case 9;
				case 471: goto case 9;
				case 473: goto case 9;
				case 475: goto case 9;
				case 477: goto case 9;
				case 479: goto case 9;
				case 481: goto case 9;
				case 483: goto case 9;
				case 485: goto case 9;
				case 487: goto case 9;
				case 489: goto case 9;
				case 491: goto case 9;
				case 493: goto case 9;
				case 495: goto case 9;
				case 497: goto case 9;
				case 499: goto case 9;
				case 501: goto case 9;
				case 503: goto case 9;
				case 505: goto case 9;
				case 507: goto case 9;
				case 509: goto case 9;
				case 511: goto case 9;
				case 513: goto case 9;
				case 515: goto case 9;
				case 517: goto case 9;
				case 519: goto case 9;
				case 521: goto case 9;
				case 523: goto case 9;
				case 525: goto case 9;
				case 588: goto case 9;
				case 589: goto case 198;
				case 590: goto case 200;
				case 591: goto case 221;
				case 592: goto case 222;
				case 593: goto case 239;
				case 594: goto case 240;
				case 615: goto case 9;
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
				case 777: goto case 9;
				case 778: goto case 9;
				case 779: goto case 9;
				case 780: goto case 9;
				case 781: goto case 198;
				case 782: goto case 221;
				case 783: goto case 239;
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
				case 878: goto case 9;
				case 879: goto case 9;
				case 880: goto case 9;
				case 881: goto case 198;
				case 882: goto case 221;
				case 883: goto case 239;
				case 885: goto case 9;
				case 886: goto case 9;
				case 887: goto case 9;
				case 888: goto case 9;
				case 889: goto case 9;
				case 890: goto case 9;
				case 891: goto case 198;
				case 892: goto case 221;
				case 893: goto case 239;
				case 894: goto case 198;
				case 895: goto case 221;
				case 896: goto case 239;
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
			AcceptConditions.Accept, // 243
			AcceptConditions.AcceptOnStart, // 244
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
			AcceptConditions.Accept, // 258
			AcceptConditions.NotAccept, // 259
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
			AcceptConditions.Accept, // 314
			AcceptConditions.NotAccept, // 315
			AcceptConditions.Accept, // 316
			AcceptConditions.Accept, // 317
			AcceptConditions.NotAccept, // 318
			AcceptConditions.Accept, // 319
			AcceptConditions.Accept, // 320
			AcceptConditions.NotAccept, // 321
			AcceptConditions.Accept, // 322
			AcceptConditions.Accept, // 323
			AcceptConditions.NotAccept, // 324
			AcceptConditions.Accept, // 325
			AcceptConditions.Accept, // 326
			AcceptConditions.NotAccept, // 327
			AcceptConditions.Accept, // 328
			AcceptConditions.Accept, // 329
			AcceptConditions.NotAccept, // 330
			AcceptConditions.Accept, // 331
			AcceptConditions.Accept, // 332
			AcceptConditions.NotAccept, // 333
			AcceptConditions.Accept, // 334
			AcceptConditions.Accept, // 335
			AcceptConditions.NotAccept, // 336
			AcceptConditions.Accept, // 337
			AcceptConditions.Accept, // 338
			AcceptConditions.NotAccept, // 339
			AcceptConditions.Accept, // 340
			AcceptConditions.Accept, // 341
			AcceptConditions.NotAccept, // 342
			AcceptConditions.Accept, // 343
			AcceptConditions.Accept, // 344
			AcceptConditions.NotAccept, // 345
			AcceptConditions.Accept, // 346
			AcceptConditions.Accept, // 347
			AcceptConditions.NotAccept, // 348
			AcceptConditions.Accept, // 349
			AcceptConditions.Accept, // 350
			AcceptConditions.NotAccept, // 351
			AcceptConditions.Accept, // 352
			AcceptConditions.Accept, // 353
			AcceptConditions.NotAccept, // 354
			AcceptConditions.Accept, // 355
			AcceptConditions.Accept, // 356
			AcceptConditions.NotAccept, // 357
			AcceptConditions.Accept, // 358
			AcceptConditions.Accept, // 359
			AcceptConditions.NotAccept, // 360
			AcceptConditions.Accept, // 361
			AcceptConditions.NotAccept, // 362
			AcceptConditions.Accept, // 363
			AcceptConditions.NotAccept, // 364
			AcceptConditions.Accept, // 365
			AcceptConditions.NotAccept, // 366
			AcceptConditions.Accept, // 367
			AcceptConditions.NotAccept, // 368
			AcceptConditions.Accept, // 369
			AcceptConditions.NotAccept, // 370
			AcceptConditions.Accept, // 371
			AcceptConditions.NotAccept, // 372
			AcceptConditions.Accept, // 373
			AcceptConditions.NotAccept, // 374
			AcceptConditions.Accept, // 375
			AcceptConditions.NotAccept, // 376
			AcceptConditions.Accept, // 377
			AcceptConditions.NotAccept, // 378
			AcceptConditions.Accept, // 379
			AcceptConditions.NotAccept, // 380
			AcceptConditions.Accept, // 381
			AcceptConditions.NotAccept, // 382
			AcceptConditions.Accept, // 383
			AcceptConditions.NotAccept, // 384
			AcceptConditions.Accept, // 385
			AcceptConditions.NotAccept, // 386
			AcceptConditions.Accept, // 387
			AcceptConditions.NotAccept, // 388
			AcceptConditions.Accept, // 389
			AcceptConditions.NotAccept, // 390
			AcceptConditions.Accept, // 391
			AcceptConditions.NotAccept, // 392
			AcceptConditions.Accept, // 393
			AcceptConditions.NotAccept, // 394
			AcceptConditions.Accept, // 395
			AcceptConditions.NotAccept, // 396
			AcceptConditions.Accept, // 397
			AcceptConditions.NotAccept, // 398
			AcceptConditions.Accept, // 399
			AcceptConditions.NotAccept, // 400
			AcceptConditions.Accept, // 401
			AcceptConditions.NotAccept, // 402
			AcceptConditions.Accept, // 403
			AcceptConditions.NotAccept, // 404
			AcceptConditions.Accept, // 405
			AcceptConditions.NotAccept, // 406
			AcceptConditions.Accept, // 407
			AcceptConditions.NotAccept, // 408
			AcceptConditions.Accept, // 409
			AcceptConditions.NotAccept, // 410
			AcceptConditions.Accept, // 411
			AcceptConditions.NotAccept, // 412
			AcceptConditions.Accept, // 413
			AcceptConditions.NotAccept, // 414
			AcceptConditions.Accept, // 415
			AcceptConditions.NotAccept, // 416
			AcceptConditions.Accept, // 417
			AcceptConditions.NotAccept, // 418
			AcceptConditions.Accept, // 419
			AcceptConditions.NotAccept, // 420
			AcceptConditions.Accept, // 421
			AcceptConditions.NotAccept, // 422
			AcceptConditions.Accept, // 423
			AcceptConditions.NotAccept, // 424
			AcceptConditions.Accept, // 425
			AcceptConditions.NotAccept, // 426
			AcceptConditions.Accept, // 427
			AcceptConditions.NotAccept, // 428
			AcceptConditions.Accept, // 429
			AcceptConditions.NotAccept, // 430
			AcceptConditions.Accept, // 431
			AcceptConditions.NotAccept, // 432
			AcceptConditions.Accept, // 433
			AcceptConditions.NotAccept, // 434
			AcceptConditions.Accept, // 435
			AcceptConditions.NotAccept, // 436
			AcceptConditions.Accept, // 437
			AcceptConditions.NotAccept, // 438
			AcceptConditions.Accept, // 439
			AcceptConditions.NotAccept, // 440
			AcceptConditions.Accept, // 441
			AcceptConditions.NotAccept, // 442
			AcceptConditions.Accept, // 443
			AcceptConditions.NotAccept, // 444
			AcceptConditions.Accept, // 445
			AcceptConditions.NotAccept, // 446
			AcceptConditions.Accept, // 447
			AcceptConditions.NotAccept, // 448
			AcceptConditions.Accept, // 449
			AcceptConditions.NotAccept, // 450
			AcceptConditions.Accept, // 451
			AcceptConditions.NotAccept, // 452
			AcceptConditions.Accept, // 453
			AcceptConditions.NotAccept, // 454
			AcceptConditions.Accept, // 455
			AcceptConditions.NotAccept, // 456
			AcceptConditions.Accept, // 457
			AcceptConditions.NotAccept, // 458
			AcceptConditions.Accept, // 459
			AcceptConditions.NotAccept, // 460
			AcceptConditions.Accept, // 461
			AcceptConditions.NotAccept, // 462
			AcceptConditions.Accept, // 463
			AcceptConditions.NotAccept, // 464
			AcceptConditions.Accept, // 465
			AcceptConditions.NotAccept, // 466
			AcceptConditions.Accept, // 467
			AcceptConditions.NotAccept, // 468
			AcceptConditions.Accept, // 469
			AcceptConditions.NotAccept, // 470
			AcceptConditions.Accept, // 471
			AcceptConditions.NotAccept, // 472
			AcceptConditions.Accept, // 473
			AcceptConditions.NotAccept, // 474
			AcceptConditions.Accept, // 475
			AcceptConditions.NotAccept, // 476
			AcceptConditions.Accept, // 477
			AcceptConditions.NotAccept, // 478
			AcceptConditions.Accept, // 479
			AcceptConditions.NotAccept, // 480
			AcceptConditions.Accept, // 481
			AcceptConditions.NotAccept, // 482
			AcceptConditions.Accept, // 483
			AcceptConditions.NotAccept, // 484
			AcceptConditions.Accept, // 485
			AcceptConditions.NotAccept, // 486
			AcceptConditions.Accept, // 487
			AcceptConditions.NotAccept, // 488
			AcceptConditions.Accept, // 489
			AcceptConditions.NotAccept, // 490
			AcceptConditions.Accept, // 491
			AcceptConditions.NotAccept, // 492
			AcceptConditions.Accept, // 493
			AcceptConditions.NotAccept, // 494
			AcceptConditions.Accept, // 495
			AcceptConditions.NotAccept, // 496
			AcceptConditions.Accept, // 497
			AcceptConditions.NotAccept, // 498
			AcceptConditions.Accept, // 499
			AcceptConditions.NotAccept, // 500
			AcceptConditions.Accept, // 501
			AcceptConditions.NotAccept, // 502
			AcceptConditions.Accept, // 503
			AcceptConditions.NotAccept, // 504
			AcceptConditions.Accept, // 505
			AcceptConditions.NotAccept, // 506
			AcceptConditions.Accept, // 507
			AcceptConditions.NotAccept, // 508
			AcceptConditions.Accept, // 509
			AcceptConditions.NotAccept, // 510
			AcceptConditions.Accept, // 511
			AcceptConditions.NotAccept, // 512
			AcceptConditions.Accept, // 513
			AcceptConditions.NotAccept, // 514
			AcceptConditions.Accept, // 515
			AcceptConditions.NotAccept, // 516
			AcceptConditions.Accept, // 517
			AcceptConditions.NotAccept, // 518
			AcceptConditions.Accept, // 519
			AcceptConditions.NotAccept, // 520
			AcceptConditions.Accept, // 521
			AcceptConditions.NotAccept, // 522
			AcceptConditions.Accept, // 523
			AcceptConditions.NotAccept, // 524
			AcceptConditions.Accept, // 525
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
			AcceptConditions.Accept, // 588
			AcceptConditions.Accept, // 589
			AcceptConditions.Accept, // 590
			AcceptConditions.Accept, // 591
			AcceptConditions.Accept, // 592
			AcceptConditions.Accept, // 593
			AcceptConditions.Accept, // 594
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
			AcceptConditions.NotAccept, // 613
			AcceptConditions.NotAccept, // 614
			AcceptConditions.Accept, // 615
			AcceptConditions.NotAccept, // 616
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
			AcceptConditions.Accept, // 780
			AcceptConditions.Accept, // 781
			AcceptConditions.Accept, // 782
			AcceptConditions.Accept, // 783
			AcceptConditions.NotAccept, // 784
			AcceptConditions.NotAccept, // 785
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
			AcceptConditions.Accept, // 881
			AcceptConditions.Accept, // 882
			AcceptConditions.Accept, // 883
			AcceptConditions.NotAccept, // 884
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
			16, 16, 16, 16, 16, 16, 16, 16, 31, 16, 16, 32, 1, 1, 1, 1, 
			33, 34, 16, 16, 16, 16, 16, 16, 16, 16, 16, 1, 1, 1, 1, 1, 
			1, 16, 16, 16, 16, 16, 16, 16, 16, 1, 1, 1, 1, 1, 16, 16, 
			16, 16, 16, 16, 16, 1, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 
			16, 16, 16, 16, 35, 36, 37, 38, 39, 40, 41, 1, 42, 43, 44, 39, 
			1, 45, 1, 1, 46, 1, 47, 1, 48, 1, 1, 49, 50, 1, 51, 1, 
			52, 53, 54, 55, 56, 51, 1, 57, 1, 1, 1, 58, 1, 59, 60, 1, 
			1, 61, 62, 63, 64, 65, 66, 67, 62, 1, 68, 1, 1, 69, 1, 70, 
			71, 1, 1, 72, 1, 1, 73, 1, 74, 75, 76, 1, 77, 78, 1, 79, 
			80, 1, 1, 81, 82, 83, 1, 84, 85, 86, 87, 1, 88, 1, 89, 90, 
			91, 92, 93, 1, 94, 1, 1, 1, 1, 95, 96, 97, 1, 98, 1, 1, 
			1, 1, 99, 100, 101, 102, 1, 103, 1, 1, 1, 1, 104, 1, 105, 106, 
			107, 108, 109, 110, 111, 112, 1, 113, 1, 114, 1, 115, 116, 117, 118, 119, 
			120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 
			136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 
			152, 153, 154, 1, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 
			167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 8, 181, 
			182, 183, 9, 184, 185, 186, 187, 188, 189, 190, 28, 191, 192, 193, 194, 195, 
			196, 197, 198, 199, 200, 201, 157, 202, 203, 204, 205, 206, 207, 208, 209, 210, 
			211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 30, 221, 222, 223, 27, 224, 
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
			381, 382, 383, 384, 385, 386, 217, 387, 388, 389, 390, 391, 392, 393, 394, 395, 
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
			652, 653, 654, 655, 656, 657, 658, 541, 659, 660, 661, 662, 663, 16, 664, 665, 
			666, 667, 668, 669, 670, 671, 672, 673, 674, 675, 676, 677, 678, 679, 680, 681, 
			682
		};
		
		private static int[,] nextState = new int[,]
		{
			{ 1, 2, 261, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 2, 259, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 315, -1, -1, -1, -1, -1, -1, -1, -1, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 19, -1, -1, -1, 20, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 880, -1, 877, 877, 877, 877, 877, 621, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 622, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, -1 },
			{ -1, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 31, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, -1, 382, 382, 382, 384, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, -1, 382, 382, 382 },
			{ -1, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 32, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 388, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, -1, 386, 386, 386 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 392, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 13, 13, -1, -1, -1, -1, -1, -1, 41, -1, -1, -1, -1, -1, 13, -1, -1, 13, -1, -1, 13, -1, 13, 13, 13, -1, -1, -1, -1, -1, 13, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 396, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 267, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 307, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 416, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 57, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 269, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 308, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 850, -1, 792, 877, 877, 877, 58, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 793, -1, 829, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 65, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 426, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 796, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 869, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 392, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, 41, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, -1, -1, 41, -1, -1, 41, -1, 41, 41, 41, -1, -1, -1, -1, -1, 41, -1, -1 },
			{ -1, -1, -1, -1, 56, -1, 56, 56, 56, 56, 56, -1, 56, 56, 56, 56, 56, 56, -1, 56, -1, -1, -1, -1, -1, -1, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 56, 56, -1, -1, 56, -1, -1, 56, -1, 56, 56, 56, -1, -1, -1, -1, -1, 56, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 689, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 387, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 702, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, 70, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, -1, -1, 70, -1, -1, 70, -1, 70, 70, 70, -1, -1, -1, -1, -1, 70, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 72, -1, -1, -1, -1, -1, -1, 72, -1, -1, -1, 72, -1, -1, -1, -1, -1, -1, -1, -1, 72, -1, 72, -1, -1, 72, -1, -1, -1, -1, -1, -1, 72, 72, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 72, -1, -1, 72, -1, -1, 72, -1, 72, 72, 72, -1, -1, -1, -1, -1, 72, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 437, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 84, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 84, 84, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 84, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 754, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 817, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 760, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 887, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, -1, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, -1, 180, 180, 180 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 271, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 181, 181, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 181, -1, -1, 181, -1, -1, 181, -1, 181, 181, 181, -1, -1, -1, -1, -1, 181, 271, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 182, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 182, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 183, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 183, -1 },
			{ -1, 184, 184, -1, -1, 184, -1, -1, -1, -1, -1, 184, -1, -1, -1, -1, -1, -1, 184, -1, -1, 184, 184, 184, 184, 184, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 184, 184, 184, 184, 184, 184, 184, 184, 184, -1, -1, -1, 184, -1, -1, 184, 184, -1, 184, -1, -1, -1, -1, -1, -1, 184, -1, -1, 184, -1 },
			{ -1, -1, -1, 185, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 186, -1, 186, 186, 186, 186, 186, -1, 186, 186, 186, 186, 186, 186, -1, 186, -1, -1, -1, -1, -1, -1, 186, 186, 186, 186, 186, 186, 186, 186, 186, 186, 186, 186, 186, 186, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 186, 186, -1, -1, 186, -1, -1, 186, -1, 186, 186, 186, -1, -1, -1, -1, -1, 186, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 188, 188, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 188, -1, -1, 188, -1, -1, 188, -1, 188, 188, 188, -1, -1, -1, -1, -1, 188, -1, -1 },
			{ -1, 192, 192, 192, 193, 192, 193, 193, 193, 193, 193, 192, 193, 193, 193, 193, 193, 193, 192, 193, 192, 192, 192, 192, 192, 192, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 192, 192, 193, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, 193, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, 192, 194, 192, 192, 192, -1, 192, 192, 192 },
			{ -1, 195, 195, 195, 195, 195, 196, 197, 195, 195, 197, 195, 195, 195, 197, 195, 198, 195, 195, 195, 199, 195, 195, 195, 195, 195, 195, 276, 195, 195, 195, 195, 195, 195, 195, 195, 195, 195, 200, 200, 195, 195, 195, 195, 195, 195, 195, 195, 195, 195, 197, 195, 195, 195, 197, 200, 195, 195, 195, 195, 200, 200, 200, 201, 195, 195, 195, -1, 200, 195, 195 },
			{ -1, -1, -1, -1, 193, -1, 193, 193, 193, 193, 193, -1, 193, 193, 193, 193, 193, 193, -1, 193, -1, -1, -1, -1, -1, -1, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 193, 193, -1, -1, 193, -1, -1, 193, -1, 193, 193, 193, -1, -1, -1, -1, -1, 193, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 569, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 894, -1, -1, -1, -1, -1, -1, 894, -1, -1, -1, 894, -1, -1, -1, -1, -1, -1, -1, -1, 894, -1, 894, -1, -1, 894, -1, -1, -1, -1, -1, -1, 894, 894, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 894, -1, -1, 894, -1, -1, 894, -1, 894, 894, 894, -1, -1, -1, -1, -1, 894, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 590, 590, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 590, -1, -1, -1, -1, 590, 590, 590, -1, -1, -1, -1, -1, 590, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, 280, -1, -1, 280, -1, -1, -1, -1, -1, -1, 280, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, 280, -1, -1, 280, -1, 280, 280, 280, -1, -1, -1, -1, -1, 280, -1, -1 },
			{ -1, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, -1, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 572, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, -1, 204, 204, 204 },
			{ -1, 206, 206, -1, -1, 206, -1, -1, -1, -1, -1, 206, -1, -1, -1, -1, -1, -1, 206, -1, -1, 206, 206, 206, 206, 206, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 206, 206, 206, 206, 206, 206, 206, 206, 206, -1, -1, -1, 206, -1, -1, 206, 206, -1, 206, -1, -1, -1, -1, -1, -1, 206, -1, -1, 206, -1 },
			{ -1, -1, -1, -1, 208, -1, 208, 208, 208, 208, 208, -1, 208, 208, 208, 208, 208, 208, -1, 208, -1, -1, -1, -1, -1, -1, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 208, 208, -1, -1, 208, -1, -1, 208, -1, 208, 208, 208, -1, -1, -1, -1, -1, 208, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 209, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 210, 210, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 210, -1, -1, 210, -1, -1, 210, -1, 210, 210, 210, -1, -1, -1, -1, -1, 210, -1, -1 },
			{ -1, 214, 214, 214, 215, 214, 215, 215, 215, 215, 215, 214, 215, 215, 215, 215, 215, 215, 214, 215, 214, 214, 214, 214, 214, 214, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 214, 214, 215, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, 215, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, 214, 216, 214, 214, 214, -1, 214, 214, 214 },
			{ -1, 217, 217, 218, 217, 217, 219, 220, 217, 217, 220, 217, 217, 217, 220, 217, 221, 217, 217, 217, 217, 217, 217, 217, 217, 217, 217, 285, 217, 217, 217, 217, 217, 217, 217, 217, 217, 217, 222, 222, 217, 217, 217, 217, 217, 217, 217, 217, 217, 217, 220, 217, 217, 217, 220, 222, 217, 217, 217, 217, 222, 222, 222, 223, 217, 217, 217, -1, 222, 217, 217 },
			{ -1, -1, -1, -1, 215, -1, 215, 215, 215, 215, 215, -1, 215, 215, 215, 215, 215, 215, -1, 215, -1, -1, -1, -1, -1, -1, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 215, 215, -1, -1, 215, -1, -1, 215, -1, 215, 215, 215, -1, -1, -1, -1, -1, 215, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 575, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 895, -1, -1, -1, -1, -1, -1, 895, -1, -1, -1, 895, -1, -1, -1, -1, -1, -1, -1, -1, 895, -1, 895, -1, -1, 895, -1, -1, -1, -1, -1, -1, 895, 895, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 895, -1, -1, 895, -1, -1, 895, -1, 895, 895, 895, -1, -1, -1, -1, -1, 895, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 592, 592, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 592, -1, -1, -1, -1, 592, 592, 592, -1, -1, -1, -1, -1, 592, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 289, -1, -1, -1, -1, -1, -1, 289, -1, -1, -1, 289, -1, -1, -1, -1, -1, -1, -1, -1, 289, -1, 289, -1, -1, 289, -1, -1, -1, -1, -1, -1, 289, 289, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 289, -1, -1, 289, -1, -1, 289, -1, 289, 289, 289, -1, -1, -1, -1, -1, 289, -1, -1 },
			{ -1, 226, 226, -1, -1, 226, -1, -1, -1, -1, -1, 226, -1, -1, -1, -1, -1, -1, 226, -1, -1, 226, 226, 226, 226, 226, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 226, 226, 226, 226, 226, 226, 226, 226, 226, -1, -1, -1, 226, -1, -1, 226, 226, -1, 226, -1, -1, -1, -1, -1, -1, 226, -1, -1, 226, -1 },
			{ -1, -1, -1, 227, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 227, 227, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 228, -1, 228, 228, 228, 228, 228, -1, 228, 228, 228, 228, 228, 228, -1, 228, -1, -1, -1, -1, -1, -1, 228, 228, 228, 228, 228, 228, 228, 228, 228, 228, 228, 228, 228, 228, 228, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 228, 228, -1, -1, 228, -1, -1, 228, -1, 228, 228, 228, -1, -1, -1, -1, -1, 228, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 229, 229, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 229, -1, -1, 229, -1, -1, 229, -1, 229, 229, 229, -1, -1, -1, -1, -1, 229, -1, -1 },
			{ -1, 233, 233, 233, 234, 233, 234, 234, 234, 234, 234, 233, 234, 234, 234, 234, 234, 234, 233, 234, 233, 233, 233, 233, 233, 233, 234, 234, 234, 234, 234, 234, 234, 234, 234, 234, 234, 234, 233, 233, 234, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 234, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 233, 235, 233, 233, 233, -1, 233, 233, 233 },
			{ -1, 236, 236, 236, 236, 236, 237, 238, 236, 236, 238, 236, 236, 236, 238, 236, 239, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 295, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 240, 240, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 238, 236, 236, 236, 238, 240, 236, 236, 236, 236, 240, 240, 240, 241, 236, 236, 236, -1, 240, 236, 236 },
			{ -1, -1, -1, -1, 234, -1, 234, 234, 234, 234, 234, -1, 234, 234, 234, 234, 234, 234, -1, 234, -1, -1, -1, -1, -1, -1, 234, 234, 234, 234, 234, 234, 234, 234, 234, 234, 234, 234, 234, 234, 234, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 234, 234, -1, -1, 234, -1, -1, 234, -1, 234, 234, 234, -1, -1, -1, -1, -1, 234, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 580, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 896, -1, -1, -1, -1, -1, -1, 896, -1, -1, -1, 896, -1, -1, -1, -1, -1, -1, -1, -1, 896, -1, 896, -1, -1, 896, -1, -1, -1, -1, -1, -1, 896, 896, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 896, -1, -1, 896, -1, -1, 896, -1, 896, 896, 896, -1, -1, -1, -1, -1, 896, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 594, 594, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 594, -1, -1, -1, -1, 594, 594, 594, -1, -1, -1, -1, -1, 594, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 299, -1, -1, -1, -1, -1, -1, 299, -1, -1, -1, 299, -1, -1, -1, -1, -1, -1, -1, -1, 299, -1, 299, -1, -1, 299, -1, -1, -1, -1, -1, -1, 299, 299, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 299, -1, -1, 299, -1, -1, 299, -1, 299, 299, 299, -1, -1, -1, -1, -1, 299, -1, -1 },
			{ -1, -1, -1, -1, 246, -1, 246, 246, 246, 246, 246, -1, 246, 246, 246, 246, 246, 246, -1, 246, -1, -1, -1, -1, -1, -1, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 246, 246, -1, -1, 246, -1, -1, 246, -1, 246, 246, 246, -1, -1, -1, -1, -1, 246, -1, -1 },
			{ -1, -1, -1, -1, 248, -1, 248, 248, 248, 248, 248, -1, 248, 248, 248, 248, 248, 248, -1, 248, -1, -1, -1, -1, -1, -1, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 248, 248, -1, -1, 248, -1, -1, 248, -1, 248, 248, 248, -1, -1, -1, -1, -1, 248, -1, -1 },
			{ -1, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, -1, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, -1, 249, 249, 249 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 251, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, -1, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, -1, 252, 252, 252 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 254, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 258, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 256, 256, 256, -1, 256, 256, 256, 256, 256, -1, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, -1, 256, -1, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, -1, 256, 256, 256 },
			{ -1, -1, -1, 2, -1, -1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, 579, -1, 579, 579, 579, 579, 579, -1, 579, 579, 579, 579, 579, 579, -1, 579, -1, -1, -1, -1, -1, -1, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, -1, -1, 579, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 579, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 3, -1, 2, 304, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 21, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 22, -1, -1, -1, 23, -1, -1, 378, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 24, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 627, -1, 877, 877, 877, 877, 877, -1, 877, 877, 26, 877, 877, 877, -1, 877, -1, 380, -1, -1, -1, -1, 877, 877, 27, 877, 877, 877, 877, 877, 877, 877, 628, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 392, -1, -1, -1, -1, -1, -1, -1, -1, -1, 394, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 13, 13, -1, -1, -1, -1, -1, -1, 41, -1, -1, -1, -1, -1, 13, -1, -1, 13, -1, -1, 13, -1, 13, 13, 13, -1, -1, -1, -1, -1, 13, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 87, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 271, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 271, -1 },
			{ -1, 184, 184, -1, -1, 184, -1, -1, -1, -1, -1, 184, -1, -1, -1, -1, -1, -1, 184, -1, -1, 184, 191, 184, 184, 184, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 184, 184, 184, 184, 184, 184, 184, 184, 184, -1, -1, -1, 184, -1, -1, 184, 184, -1, 184, -1, -1, -1, -1, -1, -1, 184, -1, -1, 184, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 568, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 188, 188, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 188, -1, -1, 188, -1, -1, 188, -1, 188, 188, 188, -1, -1, -1, -1, -1, 188, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 202, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 203, -1, -1, -1, -1, -1, -1, 203, -1, -1, -1, 203, -1, -1, -1, -1, -1, -1, -1, -1, 203, -1, 203, -1, -1, 203, -1, -1, -1, -1, -1, -1, 203, 203, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 203, -1, -1, 203, -1, -1, 203, -1, 203, 203, 203, -1, -1, -1, -1, -1, 203, -1, -1 },
			{ -1, 206, 206, -1, -1, 206, -1, -1, -1, -1, -1, 206, -1, -1, -1, -1, -1, -1, 206, -1, -1, 206, 213, 206, 206, 206, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 206, 206, 206, 206, 206, 206, 206, 206, 206, -1, -1, -1, 206, -1, -1, 206, 206, -1, 206, -1, -1, -1, -1, -1, -1, 206, -1, -1, 206, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 574, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 210, 210, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 210, -1, -1, 210, -1, -1, 210, -1, 210, 210, 210, -1, -1, -1, -1, -1, 210, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 224, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 225, -1, -1, -1, -1, -1, -1, 225, -1, -1, -1, 225, -1, -1, -1, -1, -1, -1, -1, -1, 225, -1, 225, -1, -1, 225, -1, -1, -1, -1, -1, -1, 225, 225, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 225, -1, -1, 225, -1, -1, 225, -1, 225, 225, 225, -1, -1, -1, -1, -1, 225, -1, -1 },
			{ -1, 226, 226, -1, -1, 226, -1, -1, -1, -1, -1, 226, -1, -1, -1, -1, -1, -1, 226, -1, -1, 226, 232, 226, 226, 226, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 226, 226, 226, 226, 226, 226, 226, 226, 226, -1, -1, -1, 226, -1, -1, 226, 226, -1, 226, -1, -1, -1, -1, -1, -1, 226, -1, -1, 226, -1 },
			{ -1, 226, 226, 227, -1, 226, -1, -1, -1, -1, -1, 226, -1, -1, -1, -1, -1, -1, 226, -1, 227, 291, 226, 226, 226, 226, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 226, 226, 226, 226, 226, 226, 226, 226, 226, -1, -1, -1, 226, -1, -1, 226, 226, -1, 226, -1, -1, -1, -1, -1, -1, 226, -1, -1, 226, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 578, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 229, 229, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 229, -1, -1, 229, -1, -1, 229, -1, 229, 229, 229, -1, -1, -1, -1, -1, 229, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 242, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 243, -1, -1, -1, -1, -1, -1, 243, -1, -1, -1, 243, -1, -1, -1, -1, -1, -1, -1, -1, 243, -1, 243, -1, -1, 243, -1, -1, -1, -1, -1, -1, 243, 243, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 243, -1, -1, 243, -1, -1, 243, -1, 243, 243, 243, -1, -1, -1, -1, -1, 243, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 244, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 257, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 303, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 25, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 28, -1, 877, 826, 877, 877, 877, -1, 877, 877, 329, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 788, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 267, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 269, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 309, -1, -1, -1, -1, -1, -1, 309, -1, -1, -1, 309, -1, -1, -1, -1, -1, -1, -1, -1, 309, -1, 309, -1, -1, 309, -1, -1, -1, -1, -1, -1, 309, 309, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 309, -1, -1, 309, -1, -1, 309, -1, 309, 309, 309, -1, -1, -1, -1, -1, 309, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 311, -1, -1, -1, -1, -1, -1, 311, -1, -1, -1, 311, -1, -1, -1, -1, -1, -1, -1, -1, 311, -1, 311, -1, -1, 311, -1, -1, -1, -1, -1, -1, 311, 311, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 311, -1, -1, 311, -1, -1, 311, -1, 311, 311, 311, -1, -1, -1, -1, -1, 311, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 313, -1, -1, -1, -1, -1, -1, 313, -1, -1, -1, 313, -1, -1, -1, -1, -1, -1, -1, -1, 313, -1, 313, -1, -1, 313, -1, -1, -1, -1, -1, -1, 313, 313, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 313, -1, -1, 313, -1, -1, 313, -1, 313, 313, 313, -1, -1, -1, -1, -1, 313, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 321, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 29, -1, -1, -1, 30, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 338, 877, 877, -1, 877, 877, 877, 877, 877, 642, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 38, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 595, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 33, -1, -1, -1, 34, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 39, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 645, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 324, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 35, -1, -1, -1, -1, -1, -1, 36, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 37, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 390, -1, 877, -1, 877, 647, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, 11, 12, -1, -1, -1, -1, 877, 877, 877, 648, 877, 877, 877, 877, 877, 877, 877, 40, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, 263, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 42, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 43, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 356, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 60, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 330, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 44, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 45, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 61, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 333, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 333, 333, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 333, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 46, -1, -1, -1, 47, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 48, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 62, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 333, 336, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 333, 333, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 333, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 268, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 63, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 339, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 49, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 66, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 342, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 50, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, 41, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, -1, -1, 41, -1, -1, 41, -1, 41, 41, 41, -1, -1, -1, -1, -1, 41, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 67, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 345, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 51, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 52, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 68, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 348, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 53, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 54, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 69, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 351, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 55, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 684, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 73, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 354, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 56, -1, 56, 56, 56, 56, 56, -1, 56, 56, 56, 56, 56, 56, -1, 56, -1, -1, -1, -1, -1, -1, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, -1, -1, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 74, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 357, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 76, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 357, -1, -1, -1, -1, -1, -1, 360, -1, -1, -1, -1, 357, 357, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 357, -1 },
			{ -1, -1, -1, -1, 398, -1, -1, 400, 402, -1, -1, -1, -1, 599, -1, -1, 404, -1, -1, -1, -1, -1, -1, 406, -1, -1, 408, -1, 410, 412, -1, 414, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 406, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 77, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 362, -1, 360, -1, -1, -1, -1, -1, -1, -1, -1, 364, 884, -1, 360, 360, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 360, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 78, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 596, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 79, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 366, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 80, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 81, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 368, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 262, 368, 368, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 368, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 82, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 372, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 83, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 368, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 85, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 368, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 86, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ 1, 7, 264, 8, 9, 305, 780, 823, 265, 847, 588, 10, 860, 306, 615, 868, 617, 874, 316, 877, 11, 12, 319, 10, 10, 322, 317, 618, 619, 320, 878, 323, 877, 620, 879, 877, 877, 877, 13, 13, 877, 325, 328, 331, 334, 337, 340, 343, 346, 349, 352, 877, 13, 355, 14, 266, 15, 358, 13, 355, 13, 13, 13, 16, 17, 18, 355, 1, 13, 10, 355 },
			{ -1, -1, -1, -1, 88, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 418, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 89, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, 420, 422, 422, 420, 420, 420, 420, 420, 420, 420, 422, 420, 420, 420, 420, 420, 420, 420, 420, 420, 59, 422, 420, 422, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 424, 420, 422, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, -1, 420, 420, 420 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 90, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 91, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, -1, 382, 382, 382 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 92, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 93, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, -1, 386, 386, 386 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 94, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, 428, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 95, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, 70, -1, -1, 430, 430, -1, -1, -1, -1, -1, -1, -1, -1, 70, -1, -1, 70, -1, -1, 70, -1, 70, 70, 70, -1, -1, -1, -1, -1, 70, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 96, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 97, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 432, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 98, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 434, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 99, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 600, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 100, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 436, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 101, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 438, -1, -1, -1, -1, -1, 440, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 102, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 103, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 442, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 104, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 444, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 105, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 446, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 106, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 602, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 603, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 107, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 448, -1, 448, 448, 448, 448, 448, -1, 448, 448, 448, 448, 448, 448, -1, 448, -1, -1, -1, 416, -1, -1, 448, 448, 448, 448, 448, 448, 448, 448, 448, 448, 448, 448, -1, -1, 448, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 448, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 416, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 109, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 785, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 110, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, 420, 422, 422, 420, 420, 420, 420, 420, 420, 420, 422, 420, 420, 420, 420, 420, 420, 420, 420, 420, 75, 422, 420, 422, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 424, 420, 422, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, -1, 420, 420, 420 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 111, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 59, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 450, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, -1, 422, 422, 422 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 112, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, 420, 598, 598, 420, 420, 420, 420, 420, 420, 420, 598, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 598, 420, 452, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, 598, 420, 420, 420, 420, 420, 420, 420, 420, 420, 420, -1, 420, 420, 420 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 113, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 114, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, 416, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 115, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 116, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 454, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 117, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 456, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 118, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 460, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 119, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 464, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 120, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 466, -1, -1, -1, 468, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 121, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 470, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 122, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 606, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 123, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 607, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 128, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 448, -1, 448, 448, 448, 448, 448, 87, 448, 448, 448, 448, 448, 448, -1, 448, -1, -1, -1, -1, 270, -1, 448, 448, 448, 448, 448, 448, 448, 448, 448, 448, 448, 448, 448, 448, 448, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 448, 448, -1, -1, 448, -1, -1, 448, -1, 448, 448, 448, -1, -1, -1, -1, -1, 448, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 129, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 452, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, 598, -1, 598, 598, 598 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 130, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 598, 422, 422, 422, 422, 422, 422, 422, 422, 422, 59, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 450, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, -1, 422, 422, 422 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 131, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 472, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 132, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 474, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 133, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 476, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 134, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 478, -1, -1, -1, -1, -1, 480, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 482, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 484, 108, 486, 488, -1, -1, -1, -1, -1, -1, -1, 480, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 135, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 490, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 136, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 492, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 137, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 494, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 138, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 496, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 145, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 498, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 146, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 508, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 147, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 608, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 148, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 476, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 124, -1, -1, -1, -1, -1, -1, -1, -1, -1, 476, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 149, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 510, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 150, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 480, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 108, -1, -1, -1, -1, -1, -1, -1, -1, -1, 480, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 151, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 512, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 152, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 484, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 125, -1, -1, -1, -1, -1, -1, -1, -1, -1, 484, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 158, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 514, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 159, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 480, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 160, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 516, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 161, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 518, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 520, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 522, 126, 524, 526, -1, -1, -1, -1, -1, -1, -1, 518, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 162, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 527, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 163, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 528, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 164, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 529, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 166, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 531, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 167, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 532, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 168, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 610, -1, -1, -1, -1, -1, 533, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 127, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 169, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 611, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 170, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 534, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 171, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 536, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 172, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 512, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 139, -1, -1, -1, -1, -1, -1, -1, -1, -1, 512, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 173, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 514, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 140, -1, -1, -1, -1, -1, -1, -1, -1, -1, 514, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 174, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 516, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 141, -1, -1, -1, -1, -1, -1, -1, -1, -1, 516, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 175, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 518, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 126, -1, -1, -1, -1, -1, -1, -1, -1, -1, 518, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 176, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 177, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 522, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 142, -1, -1, -1, -1, -1, -1, -1, -1, -1, 522, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 178, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 538, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 179, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 518, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 527, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 143, -1, -1, -1, -1, -1, -1, -1, -1, -1, 527, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 539, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 476, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 530, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 144, -1, -1, -1, -1, -1, -1, -1, -1, -1, 530, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 540, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 127, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 544, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 544, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 153, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 480, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 154, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 538, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 155, -1, -1, -1, -1, -1, -1, -1, -1, -1, 538, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 545, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 540, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 156, -1, -1, -1, -1, -1, -1, -1, -1, -1, 540, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 157, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 543, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 25, 543, 543, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 543, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 546, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 544, -1, -1, 547, -1, 612, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 544, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 545, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 165, -1, -1, -1, -1, -1, -1, -1, -1, -1, 545, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 549, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 551, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 552, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 613, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 553, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 554, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 557, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 181, 181, -1, -1, -1, 558, -1, -1, -1, -1, -1, -1, -1, -1, 181, -1, -1, 181, -1, -1, 181, -1, 181, 181, 181, -1, -1, -1, -1, -1, 181, 556, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 559, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 181, 181, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 181, -1, -1, 181, -1, -1, 181, -1, 181, 181, 181, -1, -1, -1, -1, -1, 181, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 560, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 561, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 561, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 562, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 561, -1, -1, -1, -1, 614, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 561, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 563, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 565, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 566, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 182, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 183, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ 1, 184, 184, 185, 186, 184, 186, 186, 186, 186, 186, 184, 186, 186, 186, 186, 186, 186, 184, 186, 187, 184, 184, 184, 184, 184, 186, 186, 186, 186, 186, 186, 186, 186, 186, 186, 186, 186, 188, 188, 186, 184, 184, 272, 184, 184, 184, 184, 184, 184, 189, 186, 188, 184, 190, 273, 184, 184, 188, 184, 188, 188, 188, 274, 275, 310, 184, 1, 188, 184, 310 },
			{ -1, -1, -1, -1, 570, -1, 570, 570, 570, 570, 570, -1, 570, 570, 570, 570, 570, 570, -1, 570, -1, -1, -1, -1, -1, -1, 570, 570, 570, 570, 570, 570, 570, 570, -1, 570, 570, 570, 570, 570, 570, -1, -1, 570, -1, -1, -1, -1, -1, -1, -1, 570, 570, -1, -1, 570, -1, -1, 570, -1, 570, 570, 570, -1, -1, -1, -1, -1, 570, 570, -1 },
			{ -1, -1, -1, -1, 570, -1, 570, 570, 570, 570, 570, -1, 570, 570, 570, 570, 570, 570, -1, 570, -1, -1, -1, -1, -1, -1, 570, 570, 570, 570, 570, 570, 570, 570, -1, 570, 570, 570, 570, 570, 570, -1, -1, 570, -1, -1, -1, -1, -1, -1, -1, 570, 570, -1, -1, 570, -1, -1, 570, -1, 570, 570, 570, -1, -1, 277, -1, -1, 570, 570, -1 },
			{ 1, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 205, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 572, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 1, 204, 204, 204 },
			{ -1, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, 204, -1, 204, 204, 204 },
			{ 1, 206, 206, 207, 208, 206, 208, 208, 208, 208, 208, 206, 208, 208, 208, 208, 208, 208, 206, 208, 209, 206, 206, 206, 206, 206, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 210, 210, 208, 206, 206, 281, 206, 206, 206, 206, 206, 206, 211, 208, 210, 206, 212, 282, 206, 206, 210, 206, 210, 210, 210, 283, 284, 312, 206, 1, 210, 206, 312 },
			{ -1, -1, -1, -1, 576, -1, 576, 576, 576, 576, 576, -1, 576, 576, 576, 576, 576, 576, -1, 576, -1, -1, -1, -1, -1, -1, 576, 576, 576, 576, 576, 576, 576, 576, -1, 576, 576, 576, 576, 576, 576, -1, -1, 576, -1, -1, -1, -1, -1, -1, -1, 576, 576, -1, -1, 576, -1, -1, 576, -1, 576, 576, 576, -1, -1, -1, -1, -1, 576, 576, -1 },
			{ -1, -1, -1, -1, 576, -1, 576, 576, 576, 576, 576, -1, 576, 576, 576, 576, 576, 576, -1, 576, -1, -1, -1, -1, -1, -1, 576, 576, 576, 576, 576, 576, 576, 576, -1, 576, 576, 576, 576, 576, 576, -1, -1, 576, -1, -1, -1, -1, -1, -1, -1, 576, 576, -1, -1, 576, -1, -1, 576, -1, 576, 576, 576, -1, -1, 286, -1, -1, 576, 576, -1 },
			{ 1, 226, 226, 227, 228, 226, 228, 228, 228, 228, 228, 226, 228, 228, 228, 228, 228, 228, 226, 228, 227, 291, 226, 226, 226, 226, 228, 228, 228, 228, 228, 228, 228, 228, 228, 228, 228, 228, 229, 229, 228, 226, 226, 290, 226, 226, 226, 226, 226, 226, 230, 228, 229, 226, 231, 292, 226, 226, 229, 226, 229, 229, 229, 293, 294, 314, 226, 260, 229, 226, 314 },
			{ -1, -1, -1, -1, 579, -1, 579, 579, 579, 579, 579, 244, 579, 579, 579, 579, 579, 579, -1, 579, -1, -1, -1, -1, 300, -1, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 579, 579, -1, -1, 579, -1, -1, 579, -1, 579, 579, 579, -1, -1, -1, 581, -1, 579, -1, -1 },
			{ -1, -1, -1, -1, 582, -1, 582, 582, 582, 582, 582, -1, 582, 582, 582, 582, 582, 582, -1, 582, -1, -1, -1, -1, -1, -1, 582, 582, 582, 582, 582, 582, 582, 582, -1, 582, 582, 582, 582, 582, 582, -1, -1, 582, -1, -1, -1, -1, -1, -1, -1, 582, 582, -1, -1, 582, -1, -1, 582, -1, 582, 582, 582, -1, -1, -1, -1, -1, 582, 582, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 244, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 300, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 582, -1, 582, 582, 582, 582, 582, -1, 582, 582, 582, 582, 582, 582, -1, 582, -1, -1, -1, -1, -1, -1, 582, 582, 582, 582, 582, 582, 582, 582, -1, 582, 582, 582, 582, 582, 582, -1, -1, 582, -1, -1, -1, -1, -1, -1, -1, 582, 582, -1, -1, 582, -1, -1, 582, -1, 582, 582, 582, -1, -1, 296, -1, -1, 582, 582, -1 },
			{ 1, 245, 245, 245, 246, 245, 246, 246, 246, 246, 246, 245, 246, 246, 246, 246, 246, 246, 245, 246, 245, 245, 245, 245, 245, 245, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 245, 245, 246, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 246, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 1, 245, 245, 245 },
			{ 1, 247, 247, 247, 248, 247, 248, 248, 248, 248, 248, 247, 248, 248, 248, 248, 248, 248, 247, 248, 247, 247, 247, 247, 247, 247, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 247, 247, 248, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 248, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 1, 247, 247, 247 },
			{ 1, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 250, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 1, 249, 249, 249 },
			{ 1, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 253, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 1, 252, 252, 252 },
			{ 1, 255, 256, 256, 256, 255, 256, 256, 256, 256, 256, 257, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 301, 256, 302, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 1, 256, 256, 256 },
			{ -1, -1, -1, -1, 877, -1, 877, 326, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 790, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 278, -1, -1, -1, -1, -1, -1, 278, -1, -1, -1, 278, -1, -1, -1, -1, -1, -1, -1, -1, 278, -1, 278, -1, -1, 278, -1, -1, -1, -1, -1, -1, 278, 278, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 278, -1, -1, 278, -1, -1, 278, -1, 278, 278, 278, -1, -1, -1, -1, -1, 278, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 279, 279, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 279, -1, -1, -1, -1, 279, 279, 279, -1, -1, -1, -1, -1, 279, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 287, -1, -1, -1, -1, -1, -1, 287, -1, -1, -1, 287, -1, -1, -1, -1, -1, -1, -1, -1, 287, -1, 287, -1, -1, 287, -1, -1, -1, -1, -1, -1, 287, 287, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 287, -1, -1, 287, -1, -1, 287, -1, 287, 287, 287, -1, -1, -1, -1, -1, 287, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 288, 288, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 288, -1, -1, -1, -1, 288, 288, 288, -1, -1, -1, -1, -1, 288, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 297, -1, -1, -1, -1, -1, -1, 297, -1, -1, -1, 297, -1, -1, -1, -1, -1, -1, -1, -1, 297, -1, 297, -1, -1, 297, -1, -1, -1, -1, -1, -1, 297, 297, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 297, -1, -1, 297, -1, -1, 297, -1, 297, 297, 297, -1, -1, -1, -1, -1, 297, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 298, 298, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 298, -1, -1, -1, -1, 298, 298, 298, -1, -1, -1, -1, -1, 298, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 327, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 368, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 374, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 601, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 458, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 462, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 616, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 605, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 506, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 504, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 609, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 500, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 530, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 542, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 543, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 550, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 555, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 564, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 633, 877, 877, 634, 332, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 502, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 335, -1, 877, 877, 877, 877, 877, -1, 877, 877, 825, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 341, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 643, 787, 877, 877, -1, 877, 644, 877, 877, 824, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 344, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 347, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 350, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 794, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 830, 877, 877, 877, -1, 877, 651, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 652, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 353, -1, 877, 877, 877, 877, 653, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 654, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 827, -1, 877, 877, 877, 877, 655, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 795, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 656, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 657, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 658, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 862, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 659, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 828, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 359, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 849, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 361, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 662, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 363, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 365, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 367, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 665, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 369, 877, 861, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 371, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 667, -1, 848, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 668, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 373, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 670, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 875, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 671, 877, 877, -1, 877, 877, 877, 877, 877, 672, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 673, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 375, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 674, -1, 675, 877, 877, 877, 676, -1, 677, 831, 798, 678, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 679, 877, 680, 877, 832, 877, 877, 877, 877, 877, 681, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 683, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 377, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 379, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 381, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 383, -1, 877, 877, 877, 877, 885, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 687, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 385, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 800, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 690, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 389, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 863, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 833, -1, 877, 877, 877, 877, 877, 691, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 391, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 393, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 395, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 696, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 397, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 697, -1, 877, 877, 399, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 853, 877, 698, 877, 699, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 401, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 864, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 801, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 403, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 405, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 407, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 409, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 411, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 872, 877, 877, 877, 877, 413, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 806, 704, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 836, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 804, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 855, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 415, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 854, 877, 877, -1, 877, 877, 877, 877, 877, 888, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 706, 877, 877, -1, 877, 877, 877, 877, 866, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 837, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 417, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 419, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 421, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 423, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 708, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 425, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 710, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 711, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 427, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 429, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 431, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 433, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 715, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 808, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 435, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 716, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 439, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 810, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 441, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 840, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 721, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 443, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 445, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 728, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 813, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 843, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 859, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 447, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 449, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 733, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 451, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 812, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 453, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 857, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 842, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 455, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 737, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 457, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 459, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 461, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 463, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 465, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 841, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 739, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 740, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 815, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 858, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 467, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 744, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 469, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 471, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 747, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 473, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 753, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 475, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 755, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 477, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 819, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 479, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 481, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 845, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 483, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 759, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 485, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 761, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 487, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 762, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 489, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 491, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 493, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 495, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 719, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 763, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 764, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 497, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 821, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 768, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 769, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 770, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 499, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 501, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 503, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 505, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 771, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 507, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 509, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 511, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 513, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 774, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 776, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 515, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 777, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 517, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 519, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 521, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 779, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 523, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 525, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 623, 624, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 625, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 589, -1, -1, -1, -1, -1, -1, 589, -1, -1, -1, 589, -1, -1, -1, -1, -1, -1, -1, -1, 589, -1, 589, -1, -1, 589, -1, -1, -1, -1, -1, -1, 589, 589, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 589, -1, -1, 589, -1, -1, 589, -1, 589, 589, 589, -1, -1, -1, -1, -1, 589, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 591, -1, -1, -1, -1, -1, -1, 591, -1, -1, -1, 591, -1, -1, -1, -1, -1, -1, -1, -1, 591, -1, 591, -1, -1, 591, -1, -1, -1, -1, -1, -1, 591, 591, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 591, -1, -1, 591, -1, -1, 591, -1, 591, 591, 591, -1, -1, -1, -1, -1, 591, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 593, -1, -1, -1, -1, -1, -1, 593, -1, -1, -1, 593, -1, -1, -1, -1, -1, -1, -1, -1, 593, -1, 593, -1, -1, 593, -1, -1, -1, -1, -1, -1, 593, 593, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 593, -1, -1, 593, -1, -1, 593, -1, 593, 593, 593, -1, -1, -1, -1, -1, 593, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 597, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 604, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 682, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 669, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 870, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 666, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 660, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 663, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 688, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 693, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 685, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 799, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 700, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 701, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 705, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 709, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 807, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 720, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 838, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 717, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 725, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 727, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 724, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 867, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 814, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 735, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 736, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 889, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 749, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 741, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 751, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 818, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 758, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 765, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 767, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 766, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 772, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 773, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 775, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 626, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 797, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 664, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 661, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 851, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 692, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 694, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 686, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 805, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 707, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 714, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 713, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 722, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 811, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 730, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 734, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 742, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 738, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 756, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 752, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 743, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 820, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 890, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 778, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 629, 877, 877, 877, -1, 877, 630, 877, 877, 631, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 871, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 695, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 852, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 865, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 876, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 809, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 729, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 726, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 746, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 750, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 757, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 745, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 632, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 803, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 834, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 712, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 718, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 732, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 839, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 748, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 635, 877, 877, 877, -1, 791, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 636, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 835, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 802, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 723, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 731, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 637, 877, 877, 877, 877, -1, 638, 877, 639, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 640, 877, 877, 877, 877, 877, 641, 877, 877, 789, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 703, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 856, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 646, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 649, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 786, 877, 877, 877, -1, 877, 650, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 781, -1, -1, -1, -1, -1, -1, 781, -1, -1, -1, 781, -1, -1, -1, -1, -1, -1, -1, -1, 781, -1, 781, -1, -1, 781, -1, -1, -1, -1, -1, -1, 781, 781, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 781, -1, -1, 781, -1, -1, 781, -1, 781, 781, 781, -1, -1, -1, -1, -1, 781, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 782, -1, -1, -1, -1, -1, -1, 782, -1, -1, -1, 782, -1, -1, -1, -1, -1, -1, -1, -1, 782, -1, 782, -1, -1, 782, -1, -1, -1, -1, -1, -1, 782, 782, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 782, -1, -1, 782, -1, -1, 782, -1, 782, 782, 782, -1, -1, -1, -1, -1, 782, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 783, -1, -1, -1, -1, -1, -1, 783, -1, -1, -1, 783, -1, -1, -1, -1, -1, -1, -1, -1, 783, -1, 783, -1, -1, 783, -1, -1, -1, -1, -1, -1, 783, 783, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 783, -1, -1, 783, -1, -1, 783, -1, 783, 783, 783, -1, -1, -1, -1, -1, 783, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 784, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 873, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 816, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 822, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 886, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 844, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, 877, -1, 877, 877, 877, 877, 877, -1, 877, 877, 877, 877, 877, 877, -1, 877, -1, -1, -1, -1, -1, -1, 877, 877, 877, 846, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, -1, -1, 877, -1, -1, 877, -1, 877, 877, 877, -1, -1, -1, -1, -1, 877, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 881, -1, -1, -1, -1, -1, -1, 881, -1, -1, -1, 881, -1, -1, -1, -1, -1, -1, -1, -1, 881, -1, 881, -1, -1, 881, -1, -1, -1, -1, -1, -1, 881, 881, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 881, -1, -1, 881, -1, -1, 881, -1, 881, 881, 881, -1, -1, -1, -1, -1, 881, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 882, -1, -1, -1, -1, -1, -1, 882, -1, -1, -1, 882, -1, -1, -1, -1, -1, -1, -1, -1, 882, -1, 882, -1, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 883, -1, -1, -1, -1, -1, -1, 883, -1, -1, -1, 883, -1, -1, -1, -1, -1, -1, -1, -1, 883, -1, 883, -1, -1, 883, -1, -1, -1, -1, -1, -1, 883, 883, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 883, -1, -1, 883, -1, -1, 883, -1, 883, 883, 883, -1, -1, -1, -1, -1, 883, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 891, -1, -1, -1, -1, -1, -1, 891, -1, -1, -1, 891, -1, -1, -1, -1, -1, -1, -1, -1, 891, -1, 891, -1, -1, 891, -1, -1, -1, -1, -1, -1, 891, 891, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 891, -1, -1, 891, -1, -1, 891, -1, 891, 891, 891, -1, -1, -1, -1, -1, 891, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 892, -1, -1, -1, -1, -1, -1, 892, -1, -1, -1, 892, -1, -1, -1, -1, -1, -1, -1, -1, 892, -1, 892, -1, -1, 892, -1, -1, -1, -1, -1, -1, 892, 892, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 892, -1, -1, 892, -1, -1, 892, -1, 892, 892, 892, -1, -1, -1, -1, -1, 892, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 893, -1, -1, -1, -1, -1, -1, 893, -1, -1, -1, 893, -1, -1, -1, -1, -1, -1, -1, -1, 893, -1, 893, -1, -1, 893, -1, -1, -1, -1, -1, -1, 893, 893, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 893, -1, -1, 893, -1, -1, 893, -1, 893, 893, 893, -1, -1, -1, -1, -1, 893, -1, -1 }
		};
		
		
		private static int[] yy_state_dtrans = new int[]
		{
			  0,
			  376,
			  567,
			  571,
			  573,
			  577,
			  583,
			  584,
			  585,
			  586,
			  587
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
							System.Diagnostics.Debug.Assert(last_accept_state >= 897);
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

