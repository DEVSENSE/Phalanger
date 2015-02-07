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
					// #line 277
					{ return (Tokens)GetTokenChar(0); }
					break;
					
				case 8:
					// #line 353
					{ 
						BEGIN(LexicalStates.ST_BACKQUOTE); 
						return Tokens.T_BACKQUOTE; 
					}
					break;
					
				case 9:
					// #line 278
					{ return Tokens.T_STRING; }
					break;
					
				case 10:
					// #line 280
					{ return Tokens.T_WHITESPACE; }
					break;
					
				case 11:
					// #line 337
					{ 
						BEGIN(LexicalStates.ST_DOUBLE_QUOTES); 
						return (GetTokenChar(0) != '"') ? Tokens.T_BINARY_DOUBLE : Tokens.T_DOUBLE_QUOTES; 
					}
					break;
					
				case 12:
					// #line 343
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
					// #line 281
					{ return Tokens.ParseDecimalNumber; }
					break;
					
				case 14:
					// #line 279
					{ return Tokens.T_NS_SEPARATOR; }
					break;
					
				case 15:
					// #line 292
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); yymore(); break; }
					break;
					
				case 16:
					// #line 315
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_LBRACE; }
					break;
					
				case 17:
					// #line 371
					{ return Tokens.ERROR; }
					break;
					
				case 18:
					// #line 316
					{ if (!yy_pop_state()) return Tokens.ERROR; return Tokens.T_RBRACE; }
					break;
					
				case 19:
					// #line 262
					{ return Tokens.T_MOD_EQUAL; }
					break;
					
				case 20:
					// #line 318
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
					// #line 270
					{ return Tokens.T_SL; }
					break;
					
				case 22:
					// #line 255
					{ return Tokens.T_IS_SMALLER_OR_EQUAL; }
					break;
					
				case 23:
					// #line 254
					{ return Tokens.T_IS_NOT_EQUAL; }
					break;
					
				case 24:
					// #line 228
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
					// #line 223
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
					// #line 253
					{ return Tokens.T_IS_EQUAL; }
					break;
					
				case 30:
					// #line 248
					{ return Tokens.T_DOUBLE_ARROW; }
					break;
					
				case 31:
					// #line 331
					{ return Tokens.DoubleQuotedString; }
					break;
					
				case 32:
					// #line 332
					{ return Tokens.SingleQuotedString; }
					break;
					
				case 33:
					// #line 256
					{ return Tokens.T_IS_GREATER_OR_EQUAL; }
					break;
					
				case 34:
					// #line 271
					{ return Tokens.T_SR; }
					break;
					
				case 35:
					// #line 260
					{ return Tokens.T_DIV_EQUAL; }
					break;
					
				case 36:
					// #line 293
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); yymore(); break; }
					break;
					
				case 37:
					// #line 295
					{ BEGIN(LexicalStates.ST_COMMENT); yymore(); break; }
					break;
					
				case 38:
					// #line 145
					{ return Tokens.T_DO; }
					break;
					
				case 39:
					// #line 174
					{ return Tokens.T_LOGICAL_OR; }
					break;
					
				case 40:
					// #line 224
					{ return (InLinq) ? Tokens.T_LINQ_BY : Tokens.T_STRING; }
					break;
					
				case 41:
					// #line 283
					{ return Tokens.ParseDouble; }
					break;
					
				case 42:
					// #line 229
					{ return Tokens.T_RGENERIC; }
					break;
					
				case 43:
					// #line 272
					{ return Tokens.T_DOUBLE_COLON; }
					break;
					
				case 44:
					// #line 257
					{ return Tokens.T_PLUS_EQUAL; }
					break;
					
				case 45:
					// #line 249
					{ return Tokens.T_INC; }
					break;
					
				case 46:
					// #line 258
					{ return Tokens.T_MINUS_EQUAL; }
					break;
					
				case 47:
					// #line 274
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 48:
					// #line 250
					{ return Tokens.T_DEC; }
					break;
					
				case 49:
					// #line 259
					{ return Tokens.T_MUL_EQUAL; }
					break;
					
				case 50:
					// #line 261
					{ return Tokens.T_CONCAT_EQUAL; }
					break;
					
				case 51:
					// #line 265
					{ return Tokens.T_AND_EQUAL; }
					break;
					
				case 52:
					// #line 269
					{ return Tokens.T_BOOLEAN_AND; }
					break;
					
				case 53:
					// #line 266
					{ return Tokens.T_OR_EQUAL; }
					break;
					
				case 54:
					// #line 268
					{ return Tokens.T_BOOLEAN_OR; }
					break;
					
				case 55:
					// #line 267
					{ return Tokens.T_XOR_EQUAL; }
					break;
					
				case 56:
					// #line 275
					{ return Tokens.T_VARIABLE; }
					break;
					
				case 57:
					// #line 263
					{ return Tokens.T_SL_EQUAL; }
					break;
					
				case 58:
					// #line 208
					{ return Tokens.T_INT_TYPE; }
					break;
					
				case 59:
					// #line 334
					{ return Tokens.ErrorInvalidIdentifier; }
					break;
					
				case 60:
					// #line 188
					{ return Tokens.T_TRY; }
					break;
					
				case 61:
					// #line 175
					{ return Tokens.T_LOGICAL_AND; }
					break;
					
				case 62:
					// #line 162
					{ return Tokens.T_NEW; }
					break;
					
				case 63:
					// #line 203
					{ return Tokens.T_USE; }
					break;
					
				case 64:
					// #line 251
					{ return Tokens.T_IS_IDENTICAL; }
					break;
					
				case 65:
					// #line 264
					{ return Tokens.T_SR_EQUAL; }
					break;
					
				case 66:
					// #line 135
					{ return Tokens.T_EXIT; }
					break;
					
				case 67:
					// #line 176
					{ return Tokens.T_LOGICAL_XOR; }
					break;
					
				case 68:
					// #line 146
					{ return Tokens.T_FOR; }
					break;
					
				case 69:
					// #line 163
					{ return Tokens.T_VAR; }
					break;
					
				case 70:
					// #line 284
					{ return Tokens.ParseDouble; }
					break;
					
				case 71:
					// #line 252
					{ return Tokens.T_IS_NOT_IDENTICAL; }
					break;
					
				case 72:
					// #line 282
					{ return Tokens.ParseHexadecimalNumber; }
					break;
					
				case 73:
					// #line 285
					{ return Tokens.ParseBinaryNumber; }
					break;
					
				case 74:
					// #line 242
					{ return Tokens.T_SELF; }
					break;
					
				case 75:
					// #line 153
					{ return Tokens.T_CASE; }
					break;
					
				case 76:
					// #line 333
					{ return Tokens.SingleQuotedIdentifier; }
					break;
					
				case 77:
					// #line 244
					{ return Tokens.T_TRUE; }
					break;
					
				case 78:
					// #line 177
					{ return Tokens.T_LIST; }
					break;
					
				case 79:
					// #line 246
					{ return Tokens.T_NULL; }
					break;
					
				case 80:
					// #line 205
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
					// #line 164
					{ return Tokens.T_EVAL; }
					break;
					
				case 85:
					// #line 294
					{ BEGIN(LexicalStates.ST_DOC_COMMENT); yymore(); break; }
					break;
					
				case 86:
					// #line 216
					{ return Tokens.T_LINQ_FROM; }
					break;
					
				case 87:
					// #line 207
					{ return Tokens.T_BOOL_TYPE; }
					break;
					
				case 88:
					// #line 358
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
					// #line 193
					{ return Tokens.T_CLONE; }
					break;
					
				case 91:
					// #line 189
					{ return Tokens.T_CATCH; }
					break;
					
				case 92:
					// #line 137
					{ return Tokens.T_CONST; }
					break;
					
				case 93:
					// #line 170
					{ return Tokens.T_ISSET; }
					break;
					
				case 94:
					// #line 209
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
					// #line 190
					{ return Tokens.T_THROW; }
					break;
					
				case 98:
					// #line 178
					{ return Tokens.T_ARRAY; }
					break;
					
				case 99:
					// #line 222
					{ return (InLinq) ? Tokens.T_LINQ_GROUP : Tokens.T_STRING; }
					break;
					
				case 100:
					// #line 173
					{ return Tokens.T_UNSET; }
					break;
					
				case 101:
					// #line 141
					{ return Tokens.T_ENDIF; }
					break;
					
				case 102:
					// #line 171
					{ return Tokens.T_EMPTY; }
					break;
					
				case 103:
					// #line 195
					{ return Tokens.T_FINAL; }
					break;
					
				case 104:
					// #line 245
					{ return Tokens.T_FALSE; }
					break;
					
				case 105:
					// #line 143
					{ return Tokens.T_WHILE; }
					break;
					
				case 106:
					// #line 217
					{ return (InLinq) ? Tokens.T_LINQ_WHERE : Tokens.T_STRING; }
					break;
					
				case 107:
					// #line 155
					{ return Tokens.T_BREAK; }
					break;
					
				case 108:
					// #line 233
					{ return Tokens.T_SET; }
					break;
					
				case 109:
					// #line 232
					{ return Tokens.T_GET; }
					break;
					
				case 110:
					// #line 299
					{ return Tokens.T_INT32_CAST; }
					break;
					
				case 111:
					// #line 211
					{ return Tokens.T_STRING_TYPE; }
					break;
					
				case 112:
					// #line 172
					{ return Tokens.T_STATIC; }
					break;
					
				case 113:
					// #line 221
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
					// #line 204
					{ return Tokens.T_IMPORT; }
					break;
					
				case 117:
					// #line 241
					{ return Tokens.T_PARENT; }
					break;
					
				case 118:
					// #line 198
					{ return Tokens.T_PUBLIC; }
					break;
					
				case 119:
					// #line 231
					{ return Tokens.T_ASSERT; }
					break;
					
				case 120:
					// #line 169
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
					// #line 210
					{ return Tokens.T_DOUBLE_TYPE; }
					break;
					
				case 124:
					// #line 213
					{ return Tokens.T_OBJECT_TYPE; }
					break;
					
				case 125:
					// #line 234
					{ return Tokens.T_CALL; }
					break;
					
				case 126:
					// #line 305
					{ return Tokens.T_DOUBLE_CAST; }
					break;
					
				case 127:
					// #line 297
					{ return Tokens.T_INT8_CAST; }
					break;
					
				case 128:
					// #line 303
					{ return Tokens.T_UINT32_CAST; }
					break;
					
				case 129:
					// #line 312
					{ return Tokens.T_BOOL_CAST; }
					break;
					
				case 130:
					// #line 167
					{ return Tokens.T_REQUIRE; }
					break;
					
				case 131:
					// #line 165
					{ return Tokens.T_INCLUDE; }
					break;
					
				case 132:
					// #line 196
					{ return Tokens.T_PRIVATE; }
					break;
					
				case 133:
					// #line 226
					{ return Tokens.T_PARTIAL; }
					break;
					
				case 134:
					// #line 161
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
					// #line 218
					{ return (InLinq) ? Tokens.T_LINQ_ORDERBY : Tokens.T_STRING; }
					break;
					
				case 139:
					// #line 240
					{ return Tokens.T_SLEEP; }
					break;
					
				case 140:
					// #line 186
					{ return Tokens.T_DIR; }
					break;
					
				case 141:
					// #line 300
					{ return Tokens.T_INT64_CAST; }
					break;
					
				case 142:
					// #line 298
					{ return Tokens.T_INT16_CAST; }
					break;
					
				case 143:
					// #line 310
					{ return Tokens.T_ARRAY_CAST; }
					break;
					
				case 144:
					// #line 301
					{ return Tokens.T_UINT8_CAST; }
					break;
					
				case 145:
					// #line 313
					{ return Tokens.T_UNSET_CAST; }
					break;
					
				case 146:
					// #line 306
					{ return Tokens.T_FLOAT_CAST; }
					break;
					
				case 147:
					// #line 179
					{ return Tokens.T_CALLABLE; }
					break;
					
				case 148:
					// #line 156
					{ return Tokens.T_CONTINUE; }
					break;
					
				case 149:
					// #line 212
					{ return Tokens.T_RESOURCE_TYPE; }
					break;
					
				case 150:
					// #line 194
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
					// #line 184
					{ return Tokens.T_LINE; }
					break;
					
				case 154:
					// #line 185
					{ return Tokens.T_FILE; }
					break;
					
				case 155:
					// #line 239
					{ return Tokens.T_WAKEUP; }
					break;
					
				case 156:
					// #line 307
					{ return Tokens.T_STRING_CAST; }
					break;
					
				case 157:
					// #line 304
					{ return Tokens.T_UINT64_CAST; }
					break;
					
				case 158:
					// #line 302
					{ return Tokens.T_UINT16_CAST; }
					break;
					
				case 159:
					// #line 311
					{ return Tokens.T_OBJECT_CAST; }
					break;
					
				case 160:
					// #line 308
					{ return Tokens.T_BINARY_CAST; }
					break;
					
				case 161:
					// #line 214
					{ return Tokens.T_TYPEOF; }
					break;
					
				case 162:
					// #line 191
					{ return Tokens.T_INTERFACE; }
					break;
					
				case 163:
					// #line 197
					{ return Tokens.T_PROTECTED; }
					break;
					
				case 164:
					// #line 220
					{ return (InLinq) ? Tokens.T_LINQ_ASCENDING : Tokens.T_STRING; }
					break;
					
				case 165:
					// #line 202
					{ return Tokens.T_NAMESPACE; }
					break;
					
				case 166:
					// #line 152
					{ return Tokens.T_ENDSWITCH; }
					break;
					
				case 167:
					// #line 180
					{ return Tokens.T_CLASS_C; }
					break;
					
				case 168:
					// #line 181
					{ return Tokens.T_TRAIT_C; }
					break;
					
				case 169:
					// #line 309
					{ return Tokens.T_UNICODE_CAST; }
					break;
					
				case 170:
					// #line 199
					{ return Tokens.T_INSTANCEOF; }
					break;
					
				case 171:
					// #line 192
					{ return Tokens.T_IMPLEMENTS; }
					break;
					
				case 172:
					// #line 149
					{ return Tokens.T_ENDFOREACH; }
					break;
					
				case 173:
					// #line 219
					{ return (InLinq) ? Tokens.T_LINQ_DESCENDING : Tokens.T_STRING; }
					break;
					
				case 174:
					// #line 236
					{ return Tokens.T_TOSTRING; }
					break;
					
				case 175:
					// #line 243
					{ return Tokens.T_AUTOLOAD; }
					break;
					
				case 176:
					// #line 238
					{ return Tokens.T_DESTRUCT; }
					break;
					
				case 177:
					// #line 183
					{ return Tokens.T_METHOD_C; }
					break;
					
				case 178:
					// #line 237
					{ return Tokens.T_CONSTRUCT; }
					break;
					
				case 179:
					// #line 168
					{ return Tokens.T_REQUIRE_ONCE; }
					break;
					
				case 180:
					// #line 166
					{ return Tokens.T_INCLUDE_ONCE; }
					break;
					
				case 181:
					// #line 235
					{ return Tokens.T_CALLSTATIC; }
					break;
					
				case 182:
					// #line 182
					{ return Tokens.T_FUNC_C; }
					break;
					
				case 183:
					// #line 201
					{ return Tokens.T_NAMESPACE_C; }
					break;
					
				case 184:
					// #line 288
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_FILE; }
					break;
					
				case 185:
					// #line 287
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_LINE; }
					break;
					
				case 186:
					// #line 289
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_DEFAULT_LINE; }
					break;
					
				case 187:
					// #line 290
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_DEFAULT_FILE; }
					break;
					
				case 188:
					// #line 497
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 189:
					// #line 489
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 190:
					// #line 480
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 191:
					// #line 490
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_DOUBLE_QUOTES; }
					break;
					
				case 192:
					// #line 479
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 193:
					// #line 496
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 194:
					// #line 498
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 195:
					// #line 494
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 196:
					// #line 493
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 197:
					// #line 491
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 198:
					// #line 492
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 199:
					// #line 488
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 200:
					// #line 484
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 201:
					// #line 486
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 202:
					// #line 483
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 203:
					// #line 485
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 204:
					// #line 481
					{ return Tokens.OctalCharCode; }
					break;
					
				case 205:
					// #line 487
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 206:
					// #line 495
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 207:
					// #line 482
					{ return Tokens.HexCharCode; }
					break;
					
				case 208:
					// #line 439
					{ yymore(); break; }
					break;
					
				case 209:
					// #line 440
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.SingleQuotedString; }
					break;
					
				case 210:
					// #line 520
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 211:
					// #line 513
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_BACKQUOTE; }
					break;
					
				case 212:
					// #line 503
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 213:
					// #line 512
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 214:
					// #line 502
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 215:
					// #line 518
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 216:
					// #line 521
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 217:
					// #line 517
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 218:
					// #line 516
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 219:
					// #line 514
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 220:
					// #line 515
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 221:
					// #line 511
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 222:
					// #line 508
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 223:
					// #line 507
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 224:
					// #line 509
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 225:
					// #line 506
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 226:
					// #line 504
					{ return Tokens.OctalCharCode; }
					break;
					
				case 227:
					// #line 510
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 228:
					// #line 519
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 229:
					// #line 505
					{ return Tokens.HexCharCode; }
					break;
					
				case 230:
					// #line 475
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 231:
					// #line 468
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 232:
					// #line 460
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 233:
					// #line 459
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 234:
					// #line 473
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 235:
					// #line 476
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 236:
					// #line 472
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 237:
					// #line 471
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 238:
					// #line 469
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 239:
					// #line 470
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 240:
					// #line 467
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 241:
					// #line 464
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 242:
					// #line 465
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 243:
					// #line 463
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 244:
					// #line 461
					{ return Tokens.OctalCharCode; }
					break;
					
				case 245:
					// #line 466
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 246:
					// #line 474
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 247:
					// #line 462
					{ return Tokens.HexCharCode; }
					break;
					
				case 248:
					// #line 444
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
					
				case 249:
					// #line 384
					{
						yyless(0);
						if (!yy_pop_state()) return Tokens.ERROR;
						break;
					}
					break;
					
				case 250:
					// #line 377
					{
						if (!yy_pop_state()) return Tokens.ERROR;
						inString = (CurrentLexicalState != LexicalStates.ST_IN_SCRIPTING); 
						isCode = true;
						return Tokens.T_STRING;
					}
					break;
					
				case 251:
					// #line 398
					{
						yyless(0);
						if (!yy_pop_state()) return Tokens.ERROR;
						yy_push_state(LexicalStates.ST_IN_SCRIPTING);
						break;
					}
					break;
					
				case 252:
					// #line 392
					{
						if (!yy_pop_state()) return Tokens.ERROR;
						yy_push_state(LexicalStates.ST_IN_SCRIPTING);
						return Tokens.T_STRING_VARNAME;
					}
					break;
					
				case 253:
					// #line 433
					{ yymore(); break; }
					break;
					
				case 254:
					// #line 435
					{ yymore(); break; }
					break;
					
				case 255:
					// #line 434
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_DOC_COMMENT; }
					break;
					
				case 256:
					// #line 427
					{ yymore(); break; }
					break;
					
				case 257:
					// #line 429
					{ yymore(); break; }
					break;
					
				case 258:
					// #line 428
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_COMMENT; }
					break;
					
				case 259:
					// #line 407
					{ yymore(); break; }
					break;
					
				case 260:
					// #line 408
					{ yymore(); break; }
					break;
					
				case 261:
					// #line 409
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_LINE_COMMENT; }
					break;
					
				case 262:
					// #line 411
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
					
				case 265: goto case 2;
				case 266: goto case 4;
				case 267: goto case 6;
				case 268: goto case 7;
				case 269: goto case 9;
				case 270: goto case 13;
				case 271: goto case 20;
				case 272: goto case 23;
				case 273: goto case 25;
				case 274: goto case 88;
				case 275: goto case 185;
				case 276: goto case 188;
				case 277: goto case 192;
				case 278: goto case 193;
				case 279: goto case 194;
				case 280: goto case 199;
				case 281: goto case 200;
				case 282: goto case 202;
				case 283: goto case 204;
				case 284: goto case 207;
				case 285: goto case 210;
				case 286: goto case 214;
				case 287: goto case 215;
				case 288: goto case 216;
				case 289: goto case 221;
				case 290: goto case 223;
				case 291: goto case 225;
				case 292: goto case 226;
				case 293: goto case 229;
				case 294: goto case 230;
				case 295: goto case 231;
				case 296: goto case 233;
				case 297: goto case 234;
				case 298: goto case 235;
				case 299: goto case 240;
				case 300: goto case 241;
				case 301: goto case 243;
				case 302: goto case 244;
				case 303: goto case 247;
				case 304: goto case 248;
				case 305: goto case 259;
				case 306: goto case 261;
				case 308: goto case 2;
				case 309: goto case 7;
				case 310: goto case 9;
				case 311: goto case 20;
				case 312: goto case 25;
				case 313: goto case 192;
				case 314: goto case 193;
				case 315: goto case 214;
				case 316: goto case 215;
				case 317: goto case 233;
				case 318: goto case 234;
				case 320: goto case 7;
				case 321: goto case 9;
				case 323: goto case 7;
				case 324: goto case 9;
				case 326: goto case 7;
				case 327: goto case 9;
				case 329: goto case 7;
				case 330: goto case 9;
				case 332: goto case 7;
				case 333: goto case 9;
				case 335: goto case 7;
				case 336: goto case 9;
				case 338: goto case 7;
				case 339: goto case 9;
				case 341: goto case 7;
				case 342: goto case 9;
				case 344: goto case 7;
				case 345: goto case 9;
				case 347: goto case 7;
				case 348: goto case 9;
				case 350: goto case 7;
				case 351: goto case 9;
				case 353: goto case 7;
				case 354: goto case 9;
				case 356: goto case 7;
				case 357: goto case 9;
				case 359: goto case 7;
				case 360: goto case 9;
				case 362: goto case 7;
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
				case 527: goto case 9;
				case 529: goto case 9;
				case 531: goto case 9;
				case 533: goto case 9;
				case 535: goto case 9;
				case 601: goto case 9;
				case 602: goto case 202;
				case 603: goto case 204;
				case 604: goto case 225;
				case 605: goto case 226;
				case 606: goto case 243;
				case 607: goto case 244;
				case 629: goto case 9;
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
				case 799: goto case 202;
				case 800: goto case 225;
				case 801: goto case 243;
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
				case 902: goto case 202;
				case 903: goto case 225;
				case 904: goto case 243;
				case 906: goto case 9;
				case 907: goto case 9;
				case 908: goto case 9;
				case 909: goto case 9;
				case 910: goto case 9;
				case 911: goto case 9;
				case 912: goto case 9;
				case 913: goto case 9;
				case 914: goto case 202;
				case 915: goto case 225;
				case 916: goto case 243;
				case 917: goto case 202;
				case 918: goto case 225;
				case 919: goto case 243;
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
			AcceptConditions.AcceptOnStart, // 248
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
			AcceptConditions.Accept, // 259
			AcceptConditions.Accept, // 260
			AcceptConditions.Accept, // 261
			AcceptConditions.Accept, // 262
			AcceptConditions.NotAccept, // 263
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
			AcceptConditions.Accept, // 300
			AcceptConditions.Accept, // 301
			AcceptConditions.Accept, // 302
			AcceptConditions.Accept, // 303
			AcceptConditions.AcceptOnStart, // 304
			AcceptConditions.Accept, // 305
			AcceptConditions.Accept, // 306
			AcceptConditions.NotAccept, // 307
			AcceptConditions.Accept, // 308
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
			AcceptConditions.NotAccept, // 319
			AcceptConditions.Accept, // 320
			AcceptConditions.Accept, // 321
			AcceptConditions.NotAccept, // 322
			AcceptConditions.Accept, // 323
			AcceptConditions.Accept, // 324
			AcceptConditions.NotAccept, // 325
			AcceptConditions.Accept, // 326
			AcceptConditions.Accept, // 327
			AcceptConditions.NotAccept, // 328
			AcceptConditions.Accept, // 329
			AcceptConditions.Accept, // 330
			AcceptConditions.NotAccept, // 331
			AcceptConditions.Accept, // 332
			AcceptConditions.Accept, // 333
			AcceptConditions.NotAccept, // 334
			AcceptConditions.Accept, // 335
			AcceptConditions.Accept, // 336
			AcceptConditions.NotAccept, // 337
			AcceptConditions.Accept, // 338
			AcceptConditions.Accept, // 339
			AcceptConditions.NotAccept, // 340
			AcceptConditions.Accept, // 341
			AcceptConditions.Accept, // 342
			AcceptConditions.NotAccept, // 343
			AcceptConditions.Accept, // 344
			AcceptConditions.Accept, // 345
			AcceptConditions.NotAccept, // 346
			AcceptConditions.Accept, // 347
			AcceptConditions.Accept, // 348
			AcceptConditions.NotAccept, // 349
			AcceptConditions.Accept, // 350
			AcceptConditions.Accept, // 351
			AcceptConditions.NotAccept, // 352
			AcceptConditions.Accept, // 353
			AcceptConditions.Accept, // 354
			AcceptConditions.NotAccept, // 355
			AcceptConditions.Accept, // 356
			AcceptConditions.Accept, // 357
			AcceptConditions.NotAccept, // 358
			AcceptConditions.Accept, // 359
			AcceptConditions.Accept, // 360
			AcceptConditions.NotAccept, // 361
			AcceptConditions.Accept, // 362
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
			AcceptConditions.Accept, // 527
			AcceptConditions.NotAccept, // 528
			AcceptConditions.Accept, // 529
			AcceptConditions.NotAccept, // 530
			AcceptConditions.Accept, // 531
			AcceptConditions.NotAccept, // 532
			AcceptConditions.Accept, // 533
			AcceptConditions.NotAccept, // 534
			AcceptConditions.Accept, // 535
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
			AcceptConditions.Accept, // 601
			AcceptConditions.Accept, // 602
			AcceptConditions.Accept, // 603
			AcceptConditions.Accept, // 604
			AcceptConditions.Accept, // 605
			AcceptConditions.Accept, // 606
			AcceptConditions.Accept, // 607
			AcceptConditions.NotAccept, // 608
			AcceptConditions.NotAccept, // 609
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
			AcceptConditions.Accept, // 629
			AcceptConditions.NotAccept, // 630
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
			AcceptConditions.NotAccept, // 802
			AcceptConditions.NotAccept, // 803
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
			AcceptConditions.NotAccept, // 905
			AcceptConditions.Accept, // 906
			AcceptConditions.Accept, // 907
			AcceptConditions.Accept, // 908
			AcceptConditions.Accept, // 909
			AcceptConditions.Accept, // 910
			AcceptConditions.Accept, // 911
			AcceptConditions.Accept, // 912
			AcceptConditions.Accept, // 913
			AcceptConditions.Accept, // 914
			AcceptConditions.Accept, // 915
			AcceptConditions.Accept, // 916
			AcceptConditions.Accept, // 917
			AcceptConditions.Accept, // 918
			AcceptConditions.Accept, // 919
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
			1, 16, 16, 16, 16, 16, 16, 16, 16, 1, 16, 16, 16, 16, 16, 16, 
			16, 16, 16, 16, 16, 16, 16, 16, 36, 37, 38, 39, 40, 41, 42, 1, 
			43, 44, 45, 40, 1, 46, 1, 1, 47, 1, 48, 1, 49, 1, 1, 50, 
			51, 1, 52, 1, 53, 54, 55, 56, 57, 52, 1, 58, 1, 1, 1, 59, 
			1, 60, 61, 1, 1, 62, 63, 64, 65, 66, 67, 68, 63, 1, 69, 1, 
			1, 70, 1, 71, 72, 1, 1, 73, 1, 1, 74, 1, 75, 76, 77, 1, 
			78, 79, 1, 80, 81, 1, 1, 82, 83, 84, 1, 85, 86, 87, 88, 1, 
			89, 1, 90, 91, 92, 93, 94, 1, 95, 1, 1, 1, 1, 96, 97, 98, 
			1, 99, 1, 1, 1, 1, 100, 101, 102, 103, 1, 104, 1, 1, 1, 1, 
			105, 1, 106, 107, 108, 109, 110, 111, 112, 113, 1, 114, 1, 115, 1, 116, 
			117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 
			133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 
			149, 150, 151, 152, 153, 154, 155, 1, 156, 157, 158, 159, 160, 161, 162, 163, 
			164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 
			180, 181, 8, 182, 183, 184, 9, 185, 186, 187, 188, 189, 190, 191, 28, 192, 
			29, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 158, 204, 205, 206, 
			207, 208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 
			31, 223, 224, 225, 27, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 
			237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 
			253, 254, 255, 256, 257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 268, 
			269, 270, 271, 272, 273, 274, 275, 276, 277, 278, 279, 280, 281, 282, 283, 284, 
			285, 286, 287, 288, 289, 290, 291, 292, 293, 294, 295, 296, 297, 298, 299, 300, 
			301, 302, 303, 304, 305, 306, 307, 308, 309, 310, 311, 312, 313, 314, 315, 316, 
			317, 318, 319, 320, 321, 322, 323, 324, 325, 326, 327, 328, 329, 330, 331, 332, 
			333, 334, 335, 336, 337, 338, 339, 340, 341, 342, 343, 344, 345, 346, 347, 348, 
			349, 350, 351, 352, 353, 354, 355, 36, 356, 357, 358, 359, 360, 361, 362, 363, 
			364, 365, 366, 367, 113, 368, 369, 370, 371, 372, 114, 373, 374, 375, 115, 376, 
			377, 378, 379, 380, 381, 382, 383, 384, 385, 386, 387, 388, 389, 390, 391, 392, 
			393, 394, 395, 219, 396, 397, 398, 399, 400, 401, 402, 403, 404, 405, 406, 407, 
			408, 409, 410, 411, 412, 413, 414, 415, 416, 417, 418, 419, 420, 421, 422, 423, 
			424, 425, 426, 427, 428, 429, 430, 431, 432, 433, 434, 435, 436, 437, 438, 439, 
			440, 441, 442, 443, 444, 445, 446, 447, 448, 449, 450, 451, 452, 453, 454, 455, 
			456, 457, 458, 459, 460, 461, 462, 463, 464, 465, 466, 467, 468, 469, 470, 471, 
			472, 473, 474, 475, 476, 477, 478, 479, 480, 481, 482, 483, 484, 485, 486, 487, 
			488, 489, 490, 491, 492, 493, 494, 495, 496, 497, 498, 499, 500, 501, 502, 503, 
			504, 505, 506, 507, 508, 509, 510, 511, 512, 513, 514, 515, 516, 517, 518, 519, 
			520, 521, 522, 523, 524, 525, 526, 527, 528, 529, 530, 531, 532, 533, 534, 535, 
			536, 537, 538, 539, 540, 541, 542, 543, 544, 545, 546, 547, 548, 549, 550, 551, 
			552, 553, 554, 555, 556, 557, 558, 559, 560, 561, 562, 563, 564, 565, 566, 567, 
			568, 569, 570, 571, 572, 573, 574, 575, 576, 577, 578, 579, 580, 581, 582, 583, 
			584, 585, 586, 587, 588, 589, 590, 591, 592, 593, 594, 595, 596, 597, 598, 599, 
			600, 601, 602, 603, 604, 605, 606, 607, 608, 609, 610, 611, 612, 613, 614, 615, 
			616, 617, 618, 619, 620, 621, 622, 623, 624, 625, 626, 627, 628, 629, 630, 631, 
			632, 633, 634, 635, 636, 637, 638, 639, 640, 641, 642, 643, 644, 645, 646, 647, 
			648, 649, 650, 651, 652, 653, 654, 655, 656, 657, 658, 659, 660, 661, 662, 663, 
			664, 665, 666, 667, 668, 669, 670, 671, 672, 673, 674, 675, 676, 677, 678, 679, 
			554, 680, 16, 681, 682, 683, 684, 685, 686, 687, 688, 689, 690, 691, 692, 693, 
			694, 695, 696, 697, 698, 699, 700, 701
		};
		
		private static int[,] nextState = new int[,]
		{
			{ 1, 2, 265, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 2, 263, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 319, -1, -1, -1, -1, -1, -1, -1, -1, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 19, -1, -1, -1, 20, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 901, -1, 898, 898, 898, 898, 898, 635, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 636, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, -1 },
			{ -1, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 31, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, -1, 386, 386, 386, 388, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, -1, 386, 386, 386 },
			{ -1, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 32, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 392, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, -1, 390, 390, 390 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 396, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 13, 13, -1, -1, -1, -1, -1, -1, 41, -1, -1, -1, -1, -1, 13, -1, -1, 13, 13, -1, -1, 13, -1, 13, 13, -1, -1, -1, -1, -1, 13, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 402, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 271, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 311, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 422, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 57, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 868, -1, 846, 898, 898, 898, 58, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 811, -1, 847, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 65, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 432, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 814, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 889, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 396, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, 41, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, -1, -1, 41, 41, -1, -1, 41, -1, 41, 41, -1, -1, -1, -1, -1, 41, -1, -1 },
			{ -1, -1, -1, -1, 56, -1, 56, 56, 56, 56, 56, -1, 56, 56, 56, 56, 56, 56, -1, 56, -1, -1, -1, -1, -1, -1, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 56, 56, -1, -1, 56, 56, -1, -1, 56, -1, 56, 56, -1, -1, -1, -1, -1, 56, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 704, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 391, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 717, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, 70, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, -1, -1, 70, 70, -1, -1, 70, -1, 70, 70, -1, -1, -1, -1, -1, 70, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 72, -1, -1, -1, -1, -1, -1, 72, -1, -1, -1, 72, -1, -1, -1, -1, -1, -1, -1, -1, 72, -1, 72, -1, -1, 72, -1, -1, -1, -1, -1, -1, 72, 72, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 72, -1, -1, 72, 72, -1, -1, 72, -1, 72, 72, -1, -1, -1, -1, -1, 72, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, 73, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 443, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 85, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 85, 85, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 85, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 771, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 835, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 778, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 909, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, -1, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, 184, -1, 184, 184, 184 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 275, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 185, 185, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 185, -1, -1, 185, 185, -1, -1, 185, -1, 185, 185, -1, -1, -1, -1, -1, 185, 275, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 186, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, -1 },
			{ -1, 188, 188, -1, -1, 188, -1, -1, -1, -1, -1, 188, -1, -1, -1, -1, -1, -1, 188, -1, -1, 188, 188, 188, 188, 188, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 188, 188, 188, 188, 188, 188, 188, 188, 188, -1, -1, -1, 188, -1, -1, -1, 188, 188, -1, 188, -1, -1, -1, -1, -1, 188, -1, -1, 188, -1 },
			{ -1, -1, -1, 189, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 190, -1, 190, 190, 190, 190, 190, -1, 190, 190, 190, 190, 190, 190, -1, 190, -1, -1, -1, -1, -1, -1, 190, 190, 190, 190, 190, 190, 190, 190, 190, 190, 190, 190, 190, 190, 190, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 190, 190, -1, -1, 190, 190, -1, -1, 190, -1, 190, 190, -1, -1, -1, -1, -1, 190, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 192, 192, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 192, -1, -1, 192, 192, -1, -1, 192, -1, 192, 192, -1, -1, -1, -1, -1, 192, -1, -1 },
			{ -1, 196, 196, 196, 197, 196, 197, 197, 197, 197, 197, 196, 197, 197, 197, 197, 197, 197, 196, 197, 196, 196, 196, 196, 196, 196, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 196, 196, 197, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 197, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 198, 196, 196, 196, -1, 196, 196, 196 },
			{ -1, 199, 199, 199, 199, 199, 200, 201, 199, 199, 201, 199, 199, 199, 201, 199, 202, 199, 199, 199, 203, 199, 199, 199, 199, 199, 199, 280, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 204, 204, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 201, 199, 199, 199, 201, 204, 204, 199, 199, 199, 199, 204, 204, 205, 199, 199, 199, -1, 204, 199, 199 },
			{ -1, -1, -1, -1, 197, -1, 197, 197, 197, 197, 197, -1, 197, 197, 197, 197, 197, 197, -1, 197, -1, -1, -1, -1, -1, -1, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 197, 197, -1, -1, 197, 197, -1, -1, 197, -1, 197, 197, -1, -1, -1, -1, -1, 197, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 581, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 917, -1, -1, -1, -1, -1, -1, 917, -1, -1, -1, 917, -1, -1, -1, -1, -1, -1, -1, -1, 917, -1, 917, -1, -1, 917, -1, -1, -1, -1, -1, -1, 917, 917, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 917, -1, -1, 917, 917, -1, -1, 917, -1, 917, 917, -1, -1, -1, -1, -1, 917, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 603, 603, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 603, 603, -1, -1, -1, -1, 603, 603, -1, -1, -1, -1, -1, 603, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 284, -1, -1, -1, -1, -1, -1, 284, -1, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, -1, 284, -1, 284, -1, -1, 284, -1, -1, -1, -1, -1, -1, 284, 284, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 284, -1, -1, 284, 284, -1, -1, 284, -1, 284, 284, -1, -1, -1, -1, -1, 284, -1, -1 },
			{ -1, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, -1, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 584, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, -1, 208, 208, 208 },
			{ -1, 210, 210, -1, -1, 210, -1, -1, -1, -1, -1, 210, -1, -1, -1, -1, -1, -1, 210, -1, -1, 210, 210, 210, 210, 210, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 210, 210, 210, 210, 210, 210, 210, 210, 210, -1, -1, -1, 210, -1, -1, -1, 210, 210, -1, 210, -1, -1, -1, -1, -1, 210, -1, -1, 210, -1 },
			{ -1, -1, -1, -1, 212, -1, 212, 212, 212, 212, 212, -1, 212, 212, 212, 212, 212, 212, -1, 212, -1, -1, -1, -1, -1, -1, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 212, 212, -1, -1, 212, 212, -1, -1, 212, -1, 212, 212, -1, -1, -1, -1, -1, 212, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 213, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 214, 214, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 214, -1, -1, 214, 214, -1, -1, 214, -1, 214, 214, -1, -1, -1, -1, -1, 214, -1, -1 },
			{ -1, 218, 218, 218, 219, 218, 219, 219, 219, 219, 219, 218, 219, 219, 219, 219, 219, 219, 218, 219, 218, 218, 218, 218, 218, 218, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 218, 218, 219, 218, 218, 218, 218, 218, 218, 218, 218, 218, 218, 219, 218, 218, 218, 218, 218, 218, 218, 218, 218, 218, 218, 220, 218, 218, 218, -1, 218, 218, 218 },
			{ -1, 221, 221, 222, 221, 221, 223, 224, 221, 221, 224, 221, 221, 221, 224, 221, 225, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 289, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 226, 226, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 224, 221, 221, 221, 224, 226, 226, 221, 221, 221, 221, 226, 226, 227, 221, 221, 221, -1, 226, 221, 221 },
			{ -1, -1, -1, -1, 219, -1, 219, 219, 219, 219, 219, -1, 219, 219, 219, 219, 219, 219, -1, 219, -1, -1, -1, -1, -1, -1, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 219, 219, -1, -1, 219, 219, -1, -1, 219, -1, 219, 219, -1, -1, -1, -1, -1, 219, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 587, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 918, -1, -1, -1, -1, -1, -1, 918, -1, -1, -1, 918, -1, -1, -1, -1, -1, -1, -1, -1, 918, -1, 918, -1, -1, 918, -1, -1, -1, -1, -1, -1, 918, 918, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 918, -1, -1, 918, 918, -1, -1, 918, -1, 918, 918, -1, -1, -1, -1, -1, 918, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 605, 605, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 605, 605, -1, -1, -1, -1, 605, 605, -1, -1, -1, -1, -1, 605, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 293, -1, -1, -1, -1, -1, -1, 293, -1, -1, -1, 293, -1, -1, -1, -1, -1, -1, -1, -1, 293, -1, 293, -1, -1, 293, -1, -1, -1, -1, -1, -1, 293, 293, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 293, -1, -1, 293, 293, -1, -1, 293, -1, 293, 293, -1, -1, -1, -1, -1, 293, -1, -1 },
			{ -1, 230, 230, -1, -1, 230, -1, -1, -1, -1, -1, 230, -1, -1, -1, -1, -1, -1, 230, -1, -1, 230, 230, 230, 230, 230, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 230, 230, 230, 230, 230, 230, 230, 230, 230, -1, -1, -1, 230, -1, -1, -1, 230, 230, -1, 230, -1, -1, -1, -1, -1, 230, -1, -1, 230, -1 },
			{ -1, -1, -1, 231, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 231, 231, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 232, -1, 232, 232, 232, 232, 232, -1, 232, 232, 232, 232, 232, 232, -1, 232, -1, -1, -1, -1, -1, -1, 232, 232, 232, 232, 232, 232, 232, 232, 232, 232, 232, 232, 232, 232, 232, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 232, 232, -1, -1, 232, 232, -1, -1, 232, -1, 232, 232, -1, -1, -1, -1, -1, 232, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 233, 233, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 233, -1, -1, 233, 233, -1, -1, 233, -1, 233, 233, -1, -1, -1, -1, -1, 233, -1, -1 },
			{ -1, 237, 237, 237, 238, 237, 238, 238, 238, 238, 238, 237, 238, 238, 238, 238, 238, 238, 237, 238, 237, 237, 237, 237, 237, 237, 238, 238, 238, 238, 238, 238, 238, 238, 238, 238, 238, 238, 237, 237, 238, 237, 237, 237, 237, 237, 237, 237, 237, 237, 237, 238, 237, 237, 237, 237, 237, 237, 237, 237, 237, 237, 237, 239, 237, 237, 237, -1, 237, 237, 237 },
			{ -1, 240, 240, 240, 240, 240, 241, 242, 240, 240, 242, 240, 240, 240, 242, 240, 243, 240, 240, 240, 240, 240, 240, 240, 240, 240, 240, 299, 240, 240, 240, 240, 240, 240, 240, 240, 240, 240, 244, 244, 240, 240, 240, 240, 240, 240, 240, 240, 240, 240, 242, 240, 240, 240, 242, 244, 244, 240, 240, 240, 240, 244, 244, 245, 240, 240, 240, -1, 244, 240, 240 },
			{ -1, -1, -1, -1, 238, -1, 238, 238, 238, 238, 238, -1, 238, 238, 238, 238, 238, 238, -1, 238, -1, -1, -1, -1, -1, -1, 238, 238, 238, 238, 238, 238, 238, 238, 238, 238, 238, 238, 238, 238, 238, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 238, 238, -1, -1, 238, 238, -1, -1, 238, -1, 238, 238, -1, -1, -1, -1, -1, 238, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 592, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 919, -1, -1, -1, -1, -1, -1, 919, -1, -1, -1, 919, -1, -1, -1, -1, -1, -1, -1, -1, 919, -1, 919, -1, -1, 919, -1, -1, -1, -1, -1, -1, 919, 919, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 919, -1, -1, 919, 919, -1, -1, 919, -1, 919, 919, -1, -1, -1, -1, -1, 919, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 607, 607, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 607, 607, -1, -1, -1, -1, 607, 607, -1, -1, -1, -1, -1, 607, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 303, -1, -1, -1, -1, -1, -1, 303, -1, -1, -1, 303, -1, -1, -1, -1, -1, -1, -1, -1, 303, -1, 303, -1, -1, 303, -1, -1, -1, -1, -1, -1, 303, 303, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 303, -1, -1, 303, 303, -1, -1, 303, -1, 303, 303, -1, -1, -1, -1, -1, 303, -1, -1 },
			{ -1, -1, -1, -1, 250, -1, 250, 250, 250, 250, 250, -1, 250, 250, 250, 250, 250, 250, -1, 250, -1, -1, -1, -1, -1, -1, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 250, 250, -1, -1, 250, 250, -1, -1, 250, -1, 250, 250, -1, -1, -1, -1, -1, 250, -1, -1 },
			{ -1, -1, -1, -1, 252, -1, 252, 252, 252, 252, 252, -1, 252, 252, 252, 252, 252, 252, -1, 252, -1, -1, -1, -1, -1, -1, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 252, 252, -1, -1, 252, 252, -1, -1, 252, -1, 252, 252, -1, -1, -1, -1, -1, 252, -1, -1 },
			{ -1, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, -1, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, -1, 253, 253, 253 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 255, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, -1, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, -1, 256, 256, 256 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 258, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 262, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 260, 260, 260, -1, 260, 260, 260, 260, 260, -1, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, -1, 260, -1, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, -1, 260, 260, 260 },
			{ -1, -1, -1, 2, -1, -1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, 591, -1, 591, 591, 591, 591, 591, -1, 591, 591, 591, 591, 591, 591, -1, 591, -1, -1, -1, -1, -1, -1, 591, 591, 591, 591, 591, 591, 591, 591, 591, 591, 591, 591, -1, -1, 591, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 591, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 3, -1, 2, 308, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 21, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 22, -1, -1, -1, 23, -1, -1, 382, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 24, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 641, -1, 898, 898, 898, 898, 898, -1, 898, 898, 26, 898, 898, 898, -1, 898, -1, 384, -1, -1, -1, -1, 898, 898, 27, 898, 898, 898, 898, 898, 898, 898, 642, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 396, -1, -1, -1, -1, -1, -1, -1, -1, -1, 398, -1, -1, -1, 400, -1, -1, -1, -1, -1, -1, 13, 13, -1, -1, -1, -1, -1, -1, 41, -1, -1, -1, -1, -1, 13, -1, -1, 13, 13, -1, -1, 13, -1, 13, 13, -1, -1, -1, -1, -1, 13, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 88, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 275, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 275, -1 },
			{ -1, 188, 188, -1, -1, 188, -1, -1, -1, -1, -1, 188, -1, -1, -1, -1, -1, -1, 188, -1, -1, 188, 195, 188, 188, 188, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 188, 188, 188, 188, 188, 188, 188, 188, 188, -1, -1, -1, 188, -1, -1, -1, 188, 188, -1, 188, -1, -1, -1, -1, -1, 188, -1, -1, 188, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 580, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 192, 192, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 192, -1, -1, 192, 192, -1, -1, 192, -1, 192, 192, -1, -1, -1, -1, -1, 192, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 206, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 207, -1, -1, -1, -1, -1, -1, 207, -1, -1, -1, 207, -1, -1, -1, -1, -1, -1, -1, -1, 207, -1, 207, -1, -1, 207, -1, -1, -1, -1, -1, -1, 207, 207, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 207, -1, -1, 207, 207, -1, -1, 207, -1, 207, 207, -1, -1, -1, -1, -1, 207, -1, -1 },
			{ -1, 210, 210, -1, -1, 210, -1, -1, -1, -1, -1, 210, -1, -1, -1, -1, -1, -1, 210, -1, -1, 210, 217, 210, 210, 210, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 210, 210, 210, 210, 210, 210, 210, 210, 210, -1, -1, -1, 210, -1, -1, -1, 210, 210, -1, 210, -1, -1, -1, -1, -1, 210, -1, -1, 210, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 586, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 214, 214, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 214, -1, -1, 214, 214, -1, -1, 214, -1, 214, 214, -1, -1, -1, -1, -1, 214, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 228, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 229, -1, -1, -1, -1, -1, -1, 229, -1, -1, -1, 229, -1, -1, -1, -1, -1, -1, -1, -1, 229, -1, 229, -1, -1, 229, -1, -1, -1, -1, -1, -1, 229, 229, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 229, -1, -1, 229, 229, -1, -1, 229, -1, 229, 229, -1, -1, -1, -1, -1, 229, -1, -1 },
			{ -1, 230, 230, -1, -1, 230, -1, -1, -1, -1, -1, 230, -1, -1, -1, -1, -1, -1, 230, -1, -1, 230, 236, 230, 230, 230, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 230, 230, 230, 230, 230, 230, 230, 230, 230, -1, -1, -1, 230, -1, -1, -1, 230, 230, -1, 230, -1, -1, -1, -1, -1, 230, -1, -1, 230, -1 },
			{ -1, 230, 230, 231, -1, 230, -1, -1, -1, -1, -1, 230, -1, -1, -1, -1, -1, -1, 230, -1, 231, 295, 230, 230, 230, 230, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 230, 230, 230, 230, 230, 230, 230, 230, 230, -1, -1, -1, 230, -1, -1, -1, 230, 230, -1, 230, -1, -1, -1, -1, -1, 230, -1, -1, 230, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 590, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 233, 233, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 233, -1, -1, 233, 233, -1, -1, 233, -1, 233, 233, -1, -1, -1, -1, -1, 233, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 246, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 247, -1, -1, -1, -1, -1, -1, 247, -1, -1, -1, 247, -1, -1, -1, -1, -1, -1, -1, -1, 247, -1, 247, -1, -1, 247, -1, -1, -1, -1, -1, -1, 247, 247, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 247, -1, -1, 247, 247, -1, -1, 247, -1, 247, 247, -1, -1, -1, -1, -1, 247, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 248, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 261, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 322, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 307, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 25, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 28, -1, 898, 844, 898, 898, 898, -1, 898, 898, 333, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 806, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 271, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 313, -1, -1, -1, -1, -1, -1, 313, -1, -1, -1, 313, -1, -1, -1, -1, -1, -1, -1, -1, 313, -1, 313, -1, -1, 313, -1, -1, -1, -1, -1, -1, 313, 313, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 313, -1, -1, 313, 313, -1, -1, 313, -1, 313, 313, -1, -1, -1, -1, -1, 313, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 315, -1, -1, -1, -1, -1, -1, 315, -1, -1, -1, 315, -1, -1, -1, -1, -1, -1, -1, -1, 315, -1, 315, -1, -1, 315, -1, -1, -1, -1, -1, -1, 315, 315, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 315, -1, -1, 315, 315, -1, -1, 315, -1, 315, 315, -1, -1, -1, -1, -1, 315, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 317, -1, -1, -1, -1, -1, -1, 317, -1, -1, -1, 317, -1, -1, -1, -1, -1, -1, -1, -1, 317, -1, 317, -1, -1, 317, -1, -1, -1, -1, -1, -1, 317, 317, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 317, -1, -1, 317, 317, -1, -1, 317, -1, 317, 317, -1, -1, -1, -1, -1, 317, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 325, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 29, -1, -1, -1, 30, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 342, 898, 898, -1, 898, 898, 898, 898, 898, 656, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 38, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 608, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 33, -1, -1, -1, 34, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 39, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 659, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 328, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 35, -1, -1, -1, -1, -1, -1, 36, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 37, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 394, -1, 898, -1, 898, 661, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, 11, 12, -1, -1, -1, -1, 898, 898, 898, 662, 898, 898, 898, 898, 898, 898, 898, 40, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, 267, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 42, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 43, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 674, 898, 898, 360, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 60, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 334, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 44, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 45, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 61, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 337, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 337, 337, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 337, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 46, -1, -1, -1, 47, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 48, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 62, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 337, 340, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 337, 337, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 337, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 272, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 63, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 343, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 49, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 66, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 346, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 50, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, 41, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, -1, -1, 41, 41, -1, -1, 41, -1, 41, 41, -1, -1, -1, -1, -1, 41, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 67, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 349, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 51, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 52, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 68, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 352, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 53, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 54, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 69, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 355, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 55, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 699, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 74, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 358, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 56, -1, 56, 56, 56, 56, 56, -1, 56, 56, 56, 56, 56, 56, -1, 56, -1, -1, -1, -1, -1, -1, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, -1, -1, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 75, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 361, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 77, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 361, -1, -1, -1, -1, -1, -1, 364, -1, -1, -1, -1, 361, 361, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 361, -1 },
			{ -1, -1, -1, -1, 404, -1, -1, 406, 408, -1, -1, -1, -1, 612, -1, -1, 410, -1, -1, -1, -1, -1, -1, 412, -1, -1, 414, -1, 416, 418, -1, 420, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 412, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 78, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 366, -1, 364, -1, -1, -1, -1, -1, -1, -1, -1, 368, 905, -1, 364, 364, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 364, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 79, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 609, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 80, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 81, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 374, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 82, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 372, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 266, 372, 372, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 372, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 83, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 376, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 84, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 372, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 86, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 372, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 87, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ 1, 7, 268, 8, 9, 309, 798, 841, 269, 865, 601, 10, 880, 310, 629, 888, 631, 894, 320, 898, 11, 12, 323, 10, 10, 326, 321, 632, 633, 324, 899, 327, 898, 634, 900, 898, 898, 898, 13, 13, 898, 329, 332, 335, 338, 341, 344, 347, 350, 353, 356, 898, 13, 359, 14, 270, 13, 15, 362, 13, 359, 13, 13, 16, 17, 18, 359, 1, 13, 10, 359 },
			{ -1, -1, -1, -1, 89, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 424, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 90, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, 426, 428, 428, 426, 426, 426, 426, 426, 426, 426, 428, 426, 426, 426, 426, 426, 426, 426, 426, 426, 59, 428, 426, 428, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 430, 426, 426, 428, 426, 426, 426, 426, 426, 426, 426, 426, 426, -1, 426, 426, 426 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 91, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 92, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, -1, 386, 386, 386 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 93, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 94, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, 390, -1, 390, 390, 390 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 95, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, 434, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 96, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, 70, -1, -1, 436, 436, -1, -1, -1, -1, -1, -1, -1, -1, 70, -1, -1, 70, 70, -1, -1, 70, -1, 70, 70, -1, -1, -1, -1, -1, 70, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 97, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 98, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 99, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 438, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 100, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 440, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 101, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 613, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 102, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 442, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 103, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 444, -1, -1, -1, -1, -1, 446, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 104, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 105, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 448, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 106, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 450, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 107, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 452, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 108, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 615, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 616, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 109, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 454, -1, 454, 454, 454, 454, 454, -1, 454, 454, 454, 454, 454, 454, -1, 454, 456, 617, -1, 422, -1, -1, 454, 454, 454, 454, 454, 454, 454, 454, 454, 454, 454, 454, -1, -1, 454, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 454, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 422, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 111, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 803, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 112, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, 426, 428, 428, 426, 426, 426, 426, 426, 426, 426, 428, 426, 426, 426, 426, 426, 426, 426, 426, 426, 76, 428, 426, 428, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 430, 426, 426, 428, 426, 426, 426, 426, 426, 426, 426, 426, 426, -1, 426, 426, 426 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 113, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 59, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 458, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, -1, 428, 428, 428 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 114, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, 426, 611, 611, 426, 426, 426, 426, 426, 426, 426, 611, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 611, 426, 460, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 426, 611, 426, 426, 426, 426, 426, 426, 426, 426, 426, -1, 426, 426, 426 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 115, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 116, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, 422, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 117, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 118, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 462, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 119, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 464, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 120, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 468, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 121, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 472, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 122, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 474, -1, -1, -1, 476, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 123, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 478, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 124, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 620, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 125, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 621, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 130, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 454, -1, 454, 454, 454, 454, 454, 88, 454, 454, 454, 454, 454, 454, -1, 454, -1, -1, -1, -1, 274, -1, 454, 454, 454, 454, 454, 454, 454, 454, 454, 454, 454, 454, 454, 454, 454, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 454, 454, -1, -1, 454, 454, -1, -1, 454, -1, 454, 454, -1, -1, -1, -1, -1, 454, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 131, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 480, -1, 480, 480, 480, 480, 480, -1, 480, 480, 480, 480, 480, 480, -1, 480, -1, -1, -1, -1, -1, -1, 480, 480, 480, 480, 480, 480, 480, 480, 480, 480, 480, 480, -1, -1, 480, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 480, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 132, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 460, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, 611, -1, 611, 611, 611 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 133, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 611, 428, 428, 428, 428, 428, 428, 428, 428, 428, 59, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 458, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, 428, -1, 428, 428, 428 },
			{ -1, -1, -1, -1, 134, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 484, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 135, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 486, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 136, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 488, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 137, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 490, -1, -1, -1, -1, -1, 492, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 494, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 496, -1, -1, 498, 110, 500, -1, -1, -1, -1, -1, -1, -1, 492, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 138, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 502, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 139, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 504, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 140, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 506, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 147, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 508, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 148, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 510, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 149, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 480, -1, 480, 480, 480, 480, 480, -1, 480, 480, 480, 480, 480, 480, -1, 480, 518, -1, -1, -1, -1, -1, 480, 480, 480, 480, 480, 480, 480, 480, 480, 480, 480, 480, 480, 480, 480, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 480, 480, -1, -1, 480, 480, -1, -1, 480, -1, 480, 480, -1, -1, -1, -1, -1, 480, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 150, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 482, -1, 482, 482, 482, 482, 482, -1, 482, 482, 482, 482, 482, 482, -1, 482, -1, 518, -1, -1, -1, -1, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 482, 482, -1, -1, 482, 482, -1, -1, 482, -1, 482, 482, -1, -1, -1, -1, -1, 482, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 151, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 522, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 152, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 622, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 153, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 488, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 126, -1, -1, -1, -1, -1, -1, -1, -1, 488, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 154, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 524, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 155, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 492, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 110, -1, -1, -1, -1, -1, -1, -1, -1, 492, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 161, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 526, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 162, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 528, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 163, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 498, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 127, -1, -1, -1, -1, -1, -1, -1, -1, 498, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 164, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 492, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 165, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 530, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 166, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 532, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 534, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 536, -1, -1, 537, 128, 538, -1, -1, -1, -1, -1, -1, -1, 532, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 167, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 539, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 168, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 540, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 170, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 171, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 543, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 172, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 544, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 173, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 624, -1, -1, -1, -1, -1, 545, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 129, -1, -1, -1, -1, -1, -1, -1, -1, 545, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 174, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 88, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 274, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 175, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 625, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 176, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 546, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 177, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 178, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 526, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 141, -1, -1, -1, -1, -1, -1, -1, -1, 526, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 179, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 528, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 142, -1, -1, -1, -1, -1, -1, -1, -1, 528, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 180, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 530, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 143, -1, -1, -1, -1, -1, -1, -1, -1, 530, -1 },
			{ -1, -1, -1, -1, 898, -1, 181, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 532, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 128, -1, -1, -1, -1, -1, -1, -1, -1, 532, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 182, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 549, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 183, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 550, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 144, -1, -1, -1, -1, -1, -1, -1, -1, 537, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 532, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 539, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 145, -1, -1, -1, -1, -1, -1, -1, -1, 539, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 551, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 488, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 542, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 146, -1, -1, -1, -1, -1, -1, -1, -1, 542, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 552, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 553, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 545, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 129, -1, -1, -1, -1, -1, -1, -1, -1, 545, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 547, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 156, -1, -1, -1, -1, -1, -1, -1, -1, 547, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 492, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 549, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 157, -1, -1, -1, -1, -1, -1, -1, -1, 549, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 550, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 158, -1, -1, -1, -1, -1, -1, -1, -1, 550, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 557, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 552, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 159, -1, -1, -1, -1, -1, -1, -1, -1, 552, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 553, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 160, -1, -1, -1, -1, -1, -1, -1, -1, 553, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 545, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 555, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 25, 555, 555, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 555, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1, -1, 559, -1, 626, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 557, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 169, -1, -1, -1, -1, -1, -1, -1, -1, 557, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 560, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 561, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 563, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 564, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 627, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 565, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 566, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 568, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 568, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 569, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 568, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 185, 185, -1, -1, -1, 570, -1, -1, -1, -1, -1, -1, -1, -1, 185, -1, -1, 185, 185, -1, -1, 185, -1, 185, 185, -1, -1, -1, -1, -1, 185, 568, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 571, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 185, 185, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 185, -1, -1, 185, 185, -1, -1, 185, -1, 185, 185, -1, -1, -1, -1, -1, 185, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 572, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 573, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 573, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 574, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 573, -1, -1, -1, -1, 628, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 573, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 575, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 577, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 578, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ 1, 188, 188, 189, 190, 188, 190, 190, 190, 190, 190, 188, 190, 190, 190, 190, 190, 190, 188, 190, 191, 188, 188, 188, 188, 188, 190, 190, 190, 190, 190, 190, 190, 190, 190, 190, 190, 190, 192, 192, 190, 188, 188, 276, 188, 188, 188, 188, 188, 188, 193, 190, 192, 188, 194, 277, 192, 188, 188, 192, 188, 192, 192, 278, 279, 314, 188, 1, 192, 188, 314 },
			{ -1, -1, -1, -1, 582, -1, 582, 582, 582, 582, 582, -1, 582, 582, 582, 582, 582, 582, -1, 582, -1, -1, -1, -1, -1, -1, 582, 582, 582, 582, 582, 582, 582, 582, -1, 582, 582, 582, 582, 582, 582, -1, -1, 582, -1, -1, -1, -1, -1, -1, -1, 582, 582, -1, -1, 582, 582, -1, -1, 582, -1, 582, 582, -1, -1, -1, -1, -1, 582, 582, -1 },
			{ -1, -1, -1, -1, 582, -1, 582, 582, 582, 582, 582, -1, 582, 582, 582, 582, 582, 582, -1, 582, -1, -1, -1, -1, -1, -1, 582, 582, 582, 582, 582, 582, 582, 582, -1, 582, 582, 582, 582, 582, 582, -1, -1, 582, -1, -1, -1, -1, -1, -1, -1, 582, 582, -1, -1, 582, 582, -1, -1, 582, -1, 582, 582, -1, -1, 281, -1, -1, 582, 582, -1 },
			{ 1, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 209, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 584, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 1, 208, 208, 208 },
			{ -1, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, 208, -1, 208, 208, 208 },
			{ 1, 210, 210, 211, 212, 210, 212, 212, 212, 212, 212, 210, 212, 212, 212, 212, 212, 212, 210, 212, 213, 210, 210, 210, 210, 210, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 214, 214, 212, 210, 210, 285, 210, 210, 210, 210, 210, 210, 215, 212, 214, 210, 216, 286, 214, 210, 210, 214, 210, 214, 214, 287, 288, 316, 210, 1, 214, 210, 316 },
			{ -1, -1, -1, -1, 588, -1, 588, 588, 588, 588, 588, -1, 588, 588, 588, 588, 588, 588, -1, 588, -1, -1, -1, -1, -1, -1, 588, 588, 588, 588, 588, 588, 588, 588, -1, 588, 588, 588, 588, 588, 588, -1, -1, 588, -1, -1, -1, -1, -1, -1, -1, 588, 588, -1, -1, 588, 588, -1, -1, 588, -1, 588, 588, -1, -1, -1, -1, -1, 588, 588, -1 },
			{ -1, -1, -1, -1, 588, -1, 588, 588, 588, 588, 588, -1, 588, 588, 588, 588, 588, 588, -1, 588, -1, -1, -1, -1, -1, -1, 588, 588, 588, 588, 588, 588, 588, 588, -1, 588, 588, 588, 588, 588, 588, -1, -1, 588, -1, -1, -1, -1, -1, -1, -1, 588, 588, -1, -1, 588, 588, -1, -1, 588, -1, 588, 588, -1, -1, 290, -1, -1, 588, 588, -1 },
			{ 1, 230, 230, 231, 232, 230, 232, 232, 232, 232, 232, 230, 232, 232, 232, 232, 232, 232, 230, 232, 231, 295, 230, 230, 230, 230, 232, 232, 232, 232, 232, 232, 232, 232, 232, 232, 232, 232, 233, 233, 232, 230, 230, 294, 230, 230, 230, 230, 230, 230, 234, 232, 233, 230, 235, 296, 233, 230, 230, 233, 230, 233, 233, 297, 298, 318, 230, 264, 233, 230, 318 },
			{ -1, -1, -1, -1, 591, -1, 591, 591, 591, 591, 591, 248, 591, 591, 591, 591, 591, 591, -1, 591, -1, -1, -1, -1, 304, -1, 591, 591, 591, 591, 591, 591, 591, 591, 591, 591, 591, 591, 591, 591, 591, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 591, 591, -1, -1, 591, 591, -1, -1, 591, -1, 591, 591, -1, -1, -1, 593, -1, 591, -1, -1 },
			{ -1, -1, -1, -1, 594, -1, 594, 594, 594, 594, 594, -1, 594, 594, 594, 594, 594, 594, -1, 594, -1, -1, -1, -1, -1, -1, 594, 594, 594, 594, 594, 594, 594, 594, -1, 594, 594, 594, 594, 594, 594, -1, -1, 594, -1, -1, -1, -1, -1, -1, -1, 594, 594, -1, -1, 594, 594, -1, -1, 594, -1, 594, 594, -1, -1, -1, -1, -1, 594, 594, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 248, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 304, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 594, -1, 594, 594, 594, 594, 594, -1, 594, 594, 594, 594, 594, 594, -1, 594, -1, -1, -1, -1, -1, -1, 594, 594, 594, 594, 594, 594, 594, 594, -1, 594, 594, 594, 594, 594, 594, -1, -1, 594, -1, -1, -1, -1, -1, -1, -1, 594, 594, -1, -1, 594, 594, -1, -1, 594, -1, 594, 594, -1, -1, 300, -1, -1, 594, 594, -1 },
			{ 1, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 264, 298, 298, 298 },
			{ 1, 249, 249, 249, 250, 249, 250, 250, 250, 250, 250, 249, 250, 250, 250, 250, 250, 250, 249, 250, 249, 249, 249, 249, 249, 249, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 249, 249, 250, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 250, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 1, 249, 249, 249 },
			{ 1, 251, 251, 251, 252, 251, 252, 252, 252, 252, 252, 251, 252, 252, 252, 252, 252, 252, 251, 252, 251, 251, 251, 251, 251, 251, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 251, 251, 252, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 252, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 251, 1, 251, 251, 251 },
			{ 1, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 254, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 1, 253, 253, 253 },
			{ 1, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 257, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 1, 256, 256, 256 },
			{ 1, 259, 260, 260, 260, 259, 260, 260, 260, 260, 260, 261, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 305, 260, 306, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 1, 260, 260, 260 },
			{ -1, -1, -1, -1, 898, -1, 898, 330, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 808, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 282, -1, -1, -1, -1, -1, -1, 282, -1, -1, -1, 282, -1, -1, -1, -1, -1, -1, -1, -1, 282, -1, 282, -1, -1, 282, -1, -1, -1, -1, -1, -1, 282, 282, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 282, -1, -1, 282, 282, -1, -1, 282, -1, 282, 282, -1, -1, -1, -1, -1, 282, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 283, 283, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 283, 283, -1, -1, -1, -1, 283, 283, -1, -1, -1, -1, -1, 283, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 291, -1, -1, -1, -1, -1, -1, 291, -1, -1, -1, 291, -1, -1, -1, -1, -1, -1, -1, -1, 291, -1, 291, -1, -1, 291, -1, -1, -1, -1, -1, -1, 291, 291, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 291, -1, -1, 291, 291, -1, -1, 291, -1, 291, 291, -1, -1, -1, -1, -1, 291, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 292, 292, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 292, 292, -1, -1, -1, -1, 292, 292, -1, -1, -1, -1, -1, 292, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 301, -1, -1, -1, -1, -1, -1, 301, -1, -1, -1, 301, -1, -1, -1, -1, -1, -1, -1, -1, 301, -1, 301, -1, -1, 301, -1, -1, -1, -1, -1, -1, 301, 301, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 301, -1, -1, 301, 301, -1, -1, 301, -1, 301, 301, -1, -1, -1, -1, -1, 301, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 302, 302, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 302, 302, -1, -1, -1, -1, 302, 302, -1, -1, -1, -1, -1, 302, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 331, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 372, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 378, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 614, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 466, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 470, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 630, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 619, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 482, -1, 482, 482, 482, 482, 482, -1, 482, 482, 482, 482, 482, 482, -1, 482, -1, -1, -1, -1, -1, -1, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, -1, -1, 482, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 482, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 520, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 516, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 623, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 512, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 547, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 542, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 554, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 555, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 562, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 567, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 576, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 647, 898, 898, 648, 336, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 514, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 339, -1, 898, 898, 898, 898, 898, -1, 898, 898, 843, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 345, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 657, 805, 898, 898, -1, 898, 658, 898, 898, 842, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 348, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 351, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 354, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 812, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 848, 898, 898, 898, -1, 898, 665, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 666, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 357, -1, 898, 898, 898, 898, 667, -1, 810, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 668, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 845, -1, 898, 898, 898, 898, 669, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 813, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 670, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 671, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 672, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 882, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 673, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 866, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 363, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 867, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 365, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 677, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 367, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 369, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 371, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 680, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 373, 898, 881, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 375, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 682, -1, 910, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 683, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 377, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 685, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 895, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 686, 898, 898, -1, 898, 898, 898, 898, 898, 687, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 688, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 379, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 689, -1, 690, 898, 898, 898, 691, -1, 692, 849, 816, 693, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 694, 898, 695, 898, 850, 898, 898, 898, 898, 898, 696, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 698, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 381, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 383, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 385, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 387, -1, 898, 898, 898, 898, 906, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 702, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 389, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 818, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 705, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 393, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 891, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 851, -1, 898, 898, 898, 898, 898, 706, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 395, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 397, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 399, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 401, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 711, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 403, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 712, -1, 898, 898, 405, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 871, 898, 713, 898, 714, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 407, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 884, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 819, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 409, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 411, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 413, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 415, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 417, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 892, 898, 898, 898, 898, 419, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 824, 719, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 854, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 856, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 822, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 873, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 421, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 872, 898, 898, -1, 898, 898, 898, 898, 898, 911, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 721, 898, 898, -1, 898, 898, 898, 898, 886, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 855, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 423, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 425, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 427, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 429, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 723, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 431, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 726, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 727, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 433, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 435, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 437, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 439, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 731, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 826, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 441, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 732, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 445, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 828, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 447, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 858, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 737, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 449, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 451, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 744, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 831, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 861, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 879, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 747, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 453, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 455, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 750, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 457, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 830, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 459, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 876, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 860, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 461, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 754, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 463, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 465, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 467, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 469, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 471, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 859, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 756, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 757, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 833, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 877, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 473, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 761, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 475, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 477, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 479, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 764, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 481, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 770, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 483, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 772, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 485, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 837, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 487, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 489, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 863, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 491, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 777, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 493, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 779, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 495, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 780, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 497, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 499, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 501, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 503, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 735, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 781, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 782, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 505, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 507, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 839, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 786, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 787, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 788, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 509, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 511, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 513, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 515, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 789, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 517, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 519, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 521, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 523, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 792, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 794, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 525, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 795, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 527, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 529, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 531, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 797, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 533, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 535, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 637, 638, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 639, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 602, -1, -1, -1, -1, -1, -1, 602, -1, -1, -1, 602, -1, -1, -1, -1, -1, -1, -1, -1, 602, -1, 602, -1, -1, 602, -1, -1, -1, -1, -1, -1, 602, 602, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 602, -1, -1, 602, 602, -1, -1, 602, -1, 602, 602, -1, -1, -1, -1, -1, 602, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 604, -1, -1, -1, -1, -1, -1, 604, -1, -1, -1, 604, -1, -1, -1, -1, -1, -1, -1, -1, 604, -1, 604, -1, -1, 604, -1, -1, -1, -1, -1, -1, 604, 604, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 604, -1, -1, 604, 604, -1, -1, 604, -1, 604, 604, -1, -1, -1, -1, -1, 604, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 606, -1, -1, -1, -1, -1, -1, 606, -1, -1, -1, 606, -1, -1, -1, -1, -1, -1, -1, -1, 606, -1, 606, -1, -1, 606, -1, -1, -1, -1, -1, -1, 606, 606, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 606, -1, -1, 606, 606, -1, -1, 606, -1, 606, 606, -1, -1, -1, -1, -1, 606, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 610, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 618, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 697, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 684, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 890, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 681, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 675, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 678, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 870, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 708, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 700, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 817, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 715, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 716, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 720, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 725, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 825, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 736, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 874, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 733, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 741, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 743, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 740, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 887, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 832, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 752, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 753, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 912, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 766, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 758, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 768, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 836, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 776, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 783, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 785, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 784, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 790, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 791, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 793, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 640, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 815, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 679, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 676, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 869, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 703, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 709, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 701, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 823, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 722, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 730, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 729, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 738, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 829, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 746, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 907, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 759, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 755, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 773, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 769, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 760, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 838, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 913, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 796, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 643, 898, 898, 898, -1, 898, 644, 898, 898, 645, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 707, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 710, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 883, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 885, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 724, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 827, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 745, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 742, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 751, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 763, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 767, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 775, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 774, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 762, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 646, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 821, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 852, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 897, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 734, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 749, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 857, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 765, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 649, 898, 898, 898, -1, 809, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 650, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 853, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 820, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 728, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 739, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 748, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 651, 898, 898, 898, 898, -1, 652, 898, 653, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 654, 898, 898, 898, 898, 898, 655, 898, 898, 807, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 718, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 875, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 660, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 663, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 804, 898, 898, 898, -1, 898, 664, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 799, -1, -1, -1, -1, -1, -1, 799, -1, -1, -1, 799, -1, -1, -1, -1, -1, -1, -1, -1, 799, -1, 799, -1, -1, 799, -1, -1, -1, -1, -1, -1, 799, 799, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 799, -1, -1, 799, 799, -1, -1, 799, -1, 799, 799, -1, -1, -1, -1, -1, 799, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 800, -1, -1, -1, -1, -1, -1, 800, -1, -1, -1, 800, -1, -1, -1, -1, -1, -1, -1, -1, 800, -1, 800, -1, -1, 800, -1, -1, -1, -1, -1, -1, 800, 800, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 800, -1, -1, 800, 800, -1, -1, 800, -1, 800, 800, -1, -1, -1, -1, -1, 800, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 801, -1, -1, -1, -1, -1, -1, 801, -1, -1, -1, 801, -1, -1, -1, -1, -1, -1, -1, -1, 801, -1, 801, -1, -1, 801, -1, -1, -1, -1, -1, -1, 801, 801, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 801, -1, -1, 801, 801, -1, -1, 801, -1, 801, 801, -1, -1, -1, -1, -1, 801, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 802, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 893, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 878, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 834, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 840, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 896, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 908, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 862, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, 898, -1, 898, 898, 898, 898, 898, -1, 898, 898, 898, 898, 898, 898, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, 898, 864, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, 898, 898, -1, -1, 898, -1, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 902, -1, -1, -1, -1, -1, -1, 902, -1, -1, -1, 902, -1, -1, -1, -1, -1, -1, -1, -1, 902, -1, 902, -1, -1, 902, -1, -1, -1, -1, -1, -1, 902, 902, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 902, -1, -1, 902, 902, -1, -1, 902, -1, 902, 902, -1, -1, -1, -1, -1, 902, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 903, -1, -1, -1, -1, -1, -1, 903, -1, -1, -1, 903, -1, -1, -1, -1, -1, -1, -1, -1, 903, -1, 903, -1, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 904, -1, -1, -1, -1, -1, -1, 904, -1, -1, -1, 904, -1, -1, -1, -1, -1, -1, -1, -1, 904, -1, 904, -1, -1, 904, -1, -1, -1, -1, -1, -1, 904, 904, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 904, -1, -1, 904, 904, -1, -1, 904, -1, 904, 904, -1, -1, -1, -1, -1, 904, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 914, -1, -1, -1, -1, -1, -1, 914, -1, -1, -1, 914, -1, -1, -1, -1, -1, -1, -1, -1, 914, -1, 914, -1, -1, 914, -1, -1, -1, -1, -1, -1, 914, 914, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 914, -1, -1, 914, 914, -1, -1, 914, -1, 914, 914, -1, -1, -1, -1, -1, 914, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 915, -1, -1, -1, -1, -1, -1, 915, -1, -1, -1, 915, -1, -1, -1, -1, -1, -1, -1, -1, 915, -1, 915, -1, -1, 915, -1, -1, -1, -1, -1, -1, 915, 915, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 915, -1, -1, 915, 915, -1, -1, 915, -1, 915, 915, -1, -1, -1, -1, -1, 915, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 916, -1, -1, -1, -1, -1, -1, 916, -1, -1, -1, 916, -1, -1, -1, -1, -1, -1, -1, -1, 916, -1, 916, -1, -1, 916, -1, -1, -1, -1, -1, -1, 916, 916, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 916, -1, -1, 916, 916, -1, -1, 916, -1, 916, 916, -1, -1, -1, -1, -1, 916, -1, -1 }
		};
		
		
		private static int[] yy_state_dtrans = new int[]
		{
			  0,
			  380,
			  579,
			  583,
			  585,
			  589,
			  595,
			  596,
			  597,
			  598,
			  599,
			  600
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
							System.Diagnostics.Debug.Assert(last_accept_state >= 920);
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

