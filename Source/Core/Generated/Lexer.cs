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
					// #line 109
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
						return Tokens.T_INLINE_HTML; 
					}
					break;
					
				case 6:
					// #line 97
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
					// #line 121
					{
						BEGIN(LexicalStates.ST_IN_SCRIPTING);
						return Tokens.T_OPEN_TAG;
					}
					break;
					
				case 8:
					// #line 283
					{ return (Tokens)GetTokenChar(0); }
					break;
					
				case 9:
					// #line 359
					{ 
						BEGIN(LexicalStates.ST_BACKQUOTE); 
						return Tokens.T_BACKQUOTE; 
					}
					break;
					
				case 10:
					// #line 284
					{ return Tokens.T_STRING; }
					break;
					
				case 11:
					// #line 286
					{ return Tokens.T_WHITESPACE; }
					break;
					
				case 12:
					// #line 343
					{ 
						BEGIN(LexicalStates.ST_DOUBLE_QUOTES); 
						return (GetTokenChar(0) != '"') ? Tokens.T_BINARY_DOUBLE : Tokens.T_DOUBLE_QUOTES; 
					}
					break;
					
				case 13:
					// #line 349
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
					// #line 287
					{ return Tokens.ParseDecimalNumber; }
					break;
					
				case 15:
					// #line 285
					{ return Tokens.T_NS_SEPARATOR; }
					break;
					
				case 16:
					// #line 298
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); yymore(); break; }
					break;
					
				case 17:
					// #line 321
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_LBRACE; }
					break;
					
				case 18:
					// #line 377
					{ return Tokens.ERROR; }
					break;
					
				case 19:
					// #line 322
					{ if (!yy_pop_state()) return Tokens.ERROR; return Tokens.T_RBRACE; }
					break;
					
				case 20:
					// #line 268
					{ return Tokens.T_MOD_EQUAL; }
					break;
					
				case 21:
					// #line 324
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
					// #line 276
					{ return Tokens.T_SL; }
					break;
					
				case 23:
					// #line 261
					{ return Tokens.T_IS_SMALLER_OR_EQUAL; }
					break;
					
				case 24:
					// #line 260
					{ return Tokens.T_IS_NOT_EQUAL; }
					break;
					
				case 25:
					// #line 234
					{ return Tokens.T_LGENERIC; }
					break;
					
				case 26:
					// #line 129
					{ 
						BEGIN(LexicalStates.INITIAL); 
						return Tokens.T_CLOSE_TAG; 
					}
					break;
					
				case 27:
					// #line 229
					{ return (InLinq) ? Tokens.T_LINQ_IN : Tokens.T_STRING; }
					break;
					
				case 28:
					// #line 143
					{ return Tokens.T_IF; }
					break;
					
				case 29:
					// #line 154
					{ return Tokens.T_AS; }
					break;
					
				case 30:
					// #line 259
					{ return Tokens.T_IS_EQUAL; }
					break;
					
				case 31:
					// #line 254
					{ return Tokens.T_DOUBLE_ARROW; }
					break;
					
				case 32:
					// #line 337
					{ return Tokens.DoubleQuotedString; }
					break;
					
				case 33:
					// #line 338
					{ return Tokens.SingleQuotedString; }
					break;
					
				case 34:
					// #line 262
					{ return Tokens.T_IS_GREATER_OR_EQUAL; }
					break;
					
				case 35:
					// #line 277
					{ return Tokens.T_SR; }
					break;
					
				case 36:
					// #line 266
					{ return Tokens.T_DIV_EQUAL; }
					break;
					
				case 37:
					// #line 299
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); yymore(); break; }
					break;
					
				case 38:
					// #line 301
					{ BEGIN(LexicalStates.ST_COMMENT); yymore(); break; }
					break;
					
				case 39:
					// #line 149
					{ return Tokens.T_DO; }
					break;
					
				case 40:
					// #line 179
					{ return Tokens.T_LOGICAL_OR; }
					break;
					
				case 41:
					// #line 230
					{ return (InLinq) ? Tokens.T_LINQ_BY : Tokens.T_STRING; }
					break;
					
				case 42:
					// #line 289
					{ return Tokens.ParseDouble; }
					break;
					
				case 43:
					// #line 235
					{ return Tokens.T_RGENERIC; }
					break;
					
				case 44:
					// #line 278
					{ return Tokens.T_DOUBLE_COLON; }
					break;
					
				case 45:
					// #line 263
					{ return Tokens.T_PLUS_EQUAL; }
					break;
					
				case 46:
					// #line 255
					{ return Tokens.T_INC; }
					break;
					
				case 47:
					// #line 264
					{ return Tokens.T_MINUS_EQUAL; }
					break;
					
				case 48:
					// #line 280
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 49:
					// #line 256
					{ return Tokens.T_DEC; }
					break;
					
				case 50:
					// #line 265
					{ return Tokens.T_MUL_EQUAL; }
					break;
					
				case 51:
					// #line 267
					{ return Tokens.T_CONCAT_EQUAL; }
					break;
					
				case 52:
					// #line 271
					{ return Tokens.T_AND_EQUAL; }
					break;
					
				case 53:
					// #line 275
					{ return Tokens.T_BOOLEAN_AND; }
					break;
					
				case 54:
					// #line 272
					{ return Tokens.T_OR_EQUAL; }
					break;
					
				case 55:
					// #line 274
					{ return Tokens.T_BOOLEAN_OR; }
					break;
					
				case 56:
					// #line 273
					{ return Tokens.T_XOR_EQUAL; }
					break;
					
				case 57:
					// #line 281
					{ return Tokens.T_VARIABLE; }
					break;
					
				case 58:
					// #line 269
					{ return Tokens.T_SL_EQUAL; }
					break;
					
				case 59:
					// #line 214
					{ return Tokens.T_INT_TYPE; }
					break;
					
				case 60:
					// #line 340
					{ return Tokens.ErrorInvalidIdentifier; }
					break;
					
				case 61:
					// #line 193
					{ return Tokens.T_TRY; }
					break;
					
				case 62:
					// #line 180
					{ return Tokens.T_LOGICAL_AND; }
					break;
					
				case 63:
					// #line 167
					{ return Tokens.T_NEW; }
					break;
					
				case 64:
					// #line 209
					{ return Tokens.T_USE; }
					break;
					
				case 65:
					// #line 257
					{ return Tokens.T_IS_IDENTICAL; }
					break;
					
				case 66:
					// #line 270
					{ return Tokens.T_SR_EQUAL; }
					break;
					
				case 67:
					// #line 139
					{ return Tokens.T_EXIT; }
					break;
					
				case 68:
					// #line 181
					{ return Tokens.T_LOGICAL_XOR; }
					break;
					
				case 69:
					// #line 150
					{ return Tokens.T_FOR; }
					break;
					
				case 70:
					// #line 168
					{ return Tokens.T_VAR; }
					break;
					
				case 71:
					// #line 290
					{ return Tokens.ParseDouble; }
					break;
					
				case 72:
					// #line 258
					{ return Tokens.T_IS_NOT_IDENTICAL; }
					break;
					
				case 73:
					// #line 288
					{ return Tokens.ParseHexadecimalNumber; }
					break;
					
				case 74:
					// #line 291
					{ return Tokens.ParseBinaryNumber; }
					break;
					
				case 75:
					// #line 248
					{ return Tokens.T_SELF; }
					break;
					
				case 76:
					// #line 157
					{ return Tokens.T_CASE; }
					break;
					
				case 77:
					// #line 339
					{ return Tokens.SingleQuotedIdentifier; }
					break;
					
				case 78:
					// #line 250
					{ return Tokens.T_TRUE; }
					break;
					
				case 79:
					// #line 182
					{ return Tokens.T_LIST; }
					break;
					
				case 80:
					// #line 252
					{ return Tokens.T_NULL; }
					break;
					
				case 81:
					// #line 211
					{ return Tokens.T_GOTO; }
					break;
					
				case 82:
					// #line 161
					{ return Tokens.T_ECHO; }
					break;
					
				case 83:
					// #line 146
					{ return Tokens.T_ELSE; }
					break;
					
				case 84:
					// #line 138
					{ return Tokens.T_EXIT; }
					break;
					
				case 85:
					// #line 169
					{ return Tokens.T_EVAL; }
					break;
					
				case 86:
					// #line 300
					{ BEGIN(LexicalStates.ST_DOC_COMMENT); yymore(); break; }
					break;
					
				case 87:
					// #line 222
					{ return Tokens.T_LINQ_FROM; }
					break;
					
				case 88:
					// #line 213
					{ return Tokens.T_BOOL_TYPE; }
					break;
					
				case 89:
					// #line 364
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
					
				case 90:
					// #line 163
					{ return Tokens.T_CLASS; }
					break;
					
				case 91:
					// #line 199
					{ return Tokens.T_CLONE; }
					break;
					
				case 92:
					// #line 194
					{ return Tokens.T_CATCH; }
					break;
					
				case 93:
					// #line 141
					{ return Tokens.T_CONST; }
					break;
					
				case 94:
					// #line 175
					{ return Tokens.T_ISSET; }
					break;
					
				case 95:
					// #line 215
					{ return Tokens.T_INT64_TYPE; }
					break;
					
				case 96:
					// #line 162
					{ return Tokens.T_PRINT; }
					break;
					
				case 97:
					// #line 164
					{ return Tokens.T_TRAIT; }
					break;
					
				case 98:
					// #line 196
					{ return Tokens.T_THROW; }
					break;
					
				case 99:
					// #line 183
					{ return Tokens.T_ARRAY; }
					break;
					
				case 100:
					// #line 228
					{ return (InLinq) ? Tokens.T_LINQ_GROUP : Tokens.T_STRING; }
					break;
					
				case 101:
					// #line 178
					{ return Tokens.T_UNSET; }
					break;
					
				case 102:
					// #line 145
					{ return Tokens.T_ENDIF; }
					break;
					
				case 103:
					// #line 176
					{ return Tokens.T_EMPTY; }
					break;
					
				case 104:
					// #line 201
					{ return Tokens.T_FINAL; }
					break;
					
				case 105:
					// #line 251
					{ return Tokens.T_FALSE; }
					break;
					
				case 106:
					// #line 147
					{ return Tokens.T_WHILE; }
					break;
					
				case 107:
					// #line 223
					{ return (InLinq) ? Tokens.T_LINQ_WHERE : Tokens.T_STRING; }
					break;
					
				case 108:
					// #line 159
					{ return Tokens.T_BREAK; }
					break;
					
				case 109:
					// #line 239
					{ return Tokens.T_SET; }
					break;
					
				case 110:
					// #line 238
					{ return Tokens.T_GET; }
					break;
					
				case 111:
					// #line 305
					{ return Tokens.T_INT32_CAST; }
					break;
					
				case 112:
					// #line 217
					{ return Tokens.T_STRING_TYPE; }
					break;
					
				case 113:
					// #line 177
					{ return Tokens.T_STATIC; }
					break;
					
				case 114:
					// #line 227
					{ return (InLinq) ? Tokens.T_LINQ_SELECT : Tokens.T_STRING; }
					break;
					
				case 115:
					// #line 155
					{ return Tokens.T_SWITCH; }
					break;
					
				case 116:
					// #line 142
					{ return Tokens.T_RETURN; }
					break;
					
				case 117:
					// #line 210
					{ return Tokens.T_IMPORT; }
					break;
					
				case 118:
					// #line 247
					{ return Tokens.T_PARENT; }
					break;
					
				case 119:
					// #line 204
					{ return Tokens.T_PUBLIC; }
					break;
					
				case 120:
					// #line 237
					{ return Tokens.T_ASSERT; }
					break;
					
				case 121:
					// #line 174
					{ return Tokens.T_GLOBAL; }
					break;
					
				case 122:
					// #line 144
					{ return Tokens.T_ELSEIF; }
					break;
					
				case 123:
					// #line 151
					{ return Tokens.T_ENDFOR; }
					break;
					
				case 124:
					// #line 216
					{ return Tokens.T_DOUBLE_TYPE; }
					break;
					
				case 125:
					// #line 219
					{ return Tokens.T_OBJECT_TYPE; }
					break;
					
				case 126:
					// #line 240
					{ return Tokens.T_CALL; }
					break;
					
				case 127:
					// #line 311
					{ return Tokens.T_DOUBLE_CAST; }
					break;
					
				case 128:
					// #line 303
					{ return Tokens.T_INT8_CAST; }
					break;
					
				case 129:
					// #line 309
					{ return Tokens.T_UINT32_CAST; }
					break;
					
				case 130:
					// #line 318
					{ return Tokens.T_BOOL_CAST; }
					break;
					
				case 131:
					// #line 172
					{ return Tokens.T_REQUIRE; }
					break;
					
				case 132:
					// #line 170
					{ return Tokens.T_INCLUDE; }
					break;
					
				case 133:
					// #line 202
					{ return Tokens.T_PRIVATE; }
					break;
					
				case 134:
					// #line 232
					{ return Tokens.T_PARTIAL; }
					break;
					
				case 135:
					// #line 166
					{ return Tokens.T_EXTENDS; }
					break;
					
				case 136:
					// #line 134
					{
					  return Tokens.ErrorNotSupported; 
					}
					break;
					
				case 137:
					// #line 158
					{ return Tokens.T_DEFAULT; }
					break;
					
				case 138:
					// #line 195
					{ return Tokens.T_FINALLY; }
					break;
					
				case 139:
					// #line 152
					{ return Tokens.T_FOREACH; }
					break;
					
				case 140:
					// #line 224
					{ return (InLinq) ? Tokens.T_LINQ_ORDERBY : Tokens.T_STRING; }
					break;
					
				case 141:
					// #line 246
					{ return Tokens.T_SLEEP; }
					break;
					
				case 142:
					// #line 191
					{ return Tokens.T_DIR; }
					break;
					
				case 143:
					// #line 306
					{ return Tokens.T_INT64_CAST; }
					break;
					
				case 144:
					// #line 304
					{ return Tokens.T_INT16_CAST; }
					break;
					
				case 145:
					// #line 316
					{ return Tokens.T_ARRAY_CAST; }
					break;
					
				case 146:
					// #line 307
					{ return Tokens.T_UINT8_CAST; }
					break;
					
				case 147:
					// #line 319
					{ return Tokens.T_UNSET_CAST; }
					break;
					
				case 148:
					// #line 312
					{ return Tokens.T_FLOAT_CAST; }
					break;
					
				case 149:
					// #line 184
					{ return Tokens.T_CALLABLE; }
					break;
					
				case 150:
					// #line 160
					{ return Tokens.T_CONTINUE; }
					break;
					
				case 151:
					// #line 218
					{ return Tokens.T_RESOURCE_TYPE; }
					break;
					
				case 152:
					// #line 200
					{ return Tokens.T_ABSTRACT; }
					break;
					
				case 153:
					// #line 148
					{ return Tokens.T_ENDWHILE; }
					break;
					
				case 154:
					// #line 140
					{ return Tokens.T_FUNCTION; }
					break;
					
				case 155:
					// #line 189
					{ return Tokens.T_LINE; }
					break;
					
				case 156:
					// #line 190
					{ return Tokens.T_FILE; }
					break;
					
				case 157:
					// #line 245
					{ return Tokens.T_WAKEUP; }
					break;
					
				case 158:
					// #line 313
					{ return Tokens.T_STRING_CAST; }
					break;
					
				case 159:
					// #line 310
					{ return Tokens.T_UINT64_CAST; }
					break;
					
				case 160:
					// #line 308
					{ return Tokens.T_UINT16_CAST; }
					break;
					
				case 161:
					// #line 317
					{ return Tokens.T_OBJECT_CAST; }
					break;
					
				case 162:
					// #line 314
					{ return Tokens.T_BINARY_CAST; }
					break;
					
				case 163:
					// #line 220
					{ return Tokens.T_TYPEOF; }
					break;
					
				case 164:
					// #line 165
					{ return Tokens.T_INSTEADOF; }
					break;
					
				case 165:
					// #line 197
					{ return Tokens.T_INTERFACE; }
					break;
					
				case 166:
					// #line 203
					{ return Tokens.T_PROTECTED; }
					break;
					
				case 167:
					// #line 226
					{ return (InLinq) ? Tokens.T_LINQ_ASCENDING : Tokens.T_STRING; }
					break;
					
				case 168:
					// #line 208
					{ return Tokens.T_NAMESPACE; }
					break;
					
				case 169:
					// #line 156
					{ return Tokens.T_ENDSWITCH; }
					break;
					
				case 170:
					// #line 185
					{ return Tokens.T_CLASS_C; }
					break;
					
				case 171:
					// #line 186
					{ return Tokens.T_TRAIT_C; }
					break;
					
				case 172:
					// #line 315
					{ return Tokens.T_UNICODE_CAST; }
					break;
					
				case 173:
					// #line 205
					{ return Tokens.T_INSTANCEOF; }
					break;
					
				case 174:
					// #line 198
					{ return Tokens.T_IMPLEMENTS; }
					break;
					
				case 175:
					// #line 153
					{ return Tokens.T_ENDFOREACH; }
					break;
					
				case 176:
					// #line 225
					{ return (InLinq) ? Tokens.T_LINQ_DESCENDING : Tokens.T_STRING; }
					break;
					
				case 177:
					// #line 242
					{ return Tokens.T_TOSTRING; }
					break;
					
				case 178:
					// #line 249
					{ return Tokens.T_AUTOLOAD; }
					break;
					
				case 179:
					// #line 244
					{ return Tokens.T_DESTRUCT; }
					break;
					
				case 180:
					// #line 188
					{ return Tokens.T_METHOD_C; }
					break;
					
				case 181:
					// #line 243
					{ return Tokens.T_CONSTRUCT; }
					break;
					
				case 182:
					// #line 173
					{ return Tokens.T_REQUIRE_ONCE; }
					break;
					
				case 183:
					// #line 171
					{ return Tokens.T_INCLUDE_ONCE; }
					break;
					
				case 184:
					// #line 241
					{ return Tokens.T_CALLSTATIC; }
					break;
					
				case 185:
					// #line 187
					{ return Tokens.T_FUNC_C; }
					break;
					
				case 186:
					// #line 207
					{ return Tokens.T_NAMESPACE_C; }
					break;
					
				case 187:
					// #line 294
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_FILE; }
					break;
					
				case 188:
					// #line 293
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_LINE; }
					break;
					
				case 189:
					// #line 295
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_DEFAULT_LINE; }
					break;
					
				case 190:
					// #line 296
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_DEFAULT_FILE; }
					break;
					
				case 191:
					// #line 503
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 192:
					// #line 495
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 193:
					// #line 486
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 194:
					// #line 496
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_DOUBLE_QUOTES; }
					break;
					
				case 195:
					// #line 485
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 196:
					// #line 502
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 197:
					// #line 504
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 198:
					// #line 500
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 199:
					// #line 499
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 200:
					// #line 497
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 201:
					// #line 498
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 202:
					// #line 494
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 203:
					// #line 490
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 204:
					// #line 492
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 205:
					// #line 489
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 206:
					// #line 491
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 207:
					// #line 487
					{ return Tokens.OctalCharCode; }
					break;
					
				case 208:
					// #line 493
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 209:
					// #line 501
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 210:
					// #line 488
					{ return Tokens.HexCharCode; }
					break;
					
				case 211:
					// #line 445
					{ yymore(); break; }
					break;
					
				case 212:
					// #line 446
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.SingleQuotedString; }
					break;
					
				case 213:
					// #line 526
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 214:
					// #line 519
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_BACKQUOTE; }
					break;
					
				case 215:
					// #line 509
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 216:
					// #line 518
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 217:
					// #line 508
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 218:
					// #line 524
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 219:
					// #line 527
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 220:
					// #line 523
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 221:
					// #line 522
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 222:
					// #line 520
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 223:
					// #line 521
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 224:
					// #line 517
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 225:
					// #line 514
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 226:
					// #line 513
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 227:
					// #line 515
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 228:
					// #line 512
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 229:
					// #line 510
					{ return Tokens.OctalCharCode; }
					break;
					
				case 230:
					// #line 516
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 231:
					// #line 525
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 232:
					// #line 511
					{ return Tokens.HexCharCode; }
					break;
					
				case 233:
					// #line 481
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 234:
					// #line 474
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 235:
					// #line 466
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 236:
					// #line 465
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 237:
					// #line 479
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 238:
					// #line 482
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 239:
					// #line 478
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 240:
					// #line 477
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 241:
					// #line 475
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 242:
					// #line 476
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 243:
					// #line 473
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 244:
					// #line 470
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 245:
					// #line 471
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 246:
					// #line 469
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 247:
					// #line 467
					{ return Tokens.OctalCharCode; }
					break;
					
				case 248:
					// #line 472
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 249:
					// #line 480
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 250:
					// #line 468
					{ return Tokens.HexCharCode; }
					break;
					
				case 251:
					// #line 450
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
					
				case 252:
					// #line 390
					{
						yyless(0);
						if (!yy_pop_state()) return Tokens.ERROR;
						break;
					}
					break;
					
				case 253:
					// #line 383
					{
						if (!yy_pop_state()) return Tokens.ERROR;
						inString = (CurrentLexicalState != LexicalStates.ST_IN_SCRIPTING); 
						isCode = true;
						return Tokens.T_STRING;
					}
					break;
					
				case 254:
					// #line 404
					{
						yyless(0);
						if (!yy_pop_state()) return Tokens.ERROR;
						yy_push_state(LexicalStates.ST_IN_SCRIPTING);
						break;
					}
					break;
					
				case 255:
					// #line 398
					{
						if (!yy_pop_state()) return Tokens.ERROR;
						yy_push_state(LexicalStates.ST_IN_SCRIPTING);
						return Tokens.T_STRING_VARNAME;
					}
					break;
					
				case 256:
					// #line 439
					{ yymore(); break; }
					break;
					
				case 257:
					// #line 441
					{ yymore(); break; }
					break;
					
				case 258:
					// #line 440
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_DOC_COMMENT; }
					break;
					
				case 259:
					// #line 433
					{ yymore(); break; }
					break;
					
				case 260:
					// #line 435
					{ yymore(); break; }
					break;
					
				case 261:
					// #line 434
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_COMMENT; }
					break;
					
				case 262:
					// #line 413
					{ yymore(); break; }
					break;
					
				case 263:
					// #line 414
					{ yymore(); break; }
					break;
					
				case 264:
					// #line 415
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_LINE_COMMENT; }
					break;
					
				case 265:
					// #line 417
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
					
				case 268: goto case 2;
				case 269: goto case 4;
				case 270: goto case 5;
				case 271: goto case 7;
				case 272: goto case 8;
				case 273: goto case 10;
				case 274: goto case 14;
				case 275: goto case 21;
				case 276: goto case 24;
				case 277: goto case 26;
				case 278: goto case 89;
				case 279: goto case 188;
				case 280: goto case 191;
				case 281: goto case 195;
				case 282: goto case 196;
				case 283: goto case 197;
				case 284: goto case 202;
				case 285: goto case 203;
				case 286: goto case 205;
				case 287: goto case 207;
				case 288: goto case 210;
				case 289: goto case 213;
				case 290: goto case 217;
				case 291: goto case 218;
				case 292: goto case 219;
				case 293: goto case 224;
				case 294: goto case 226;
				case 295: goto case 228;
				case 296: goto case 229;
				case 297: goto case 232;
				case 298: goto case 233;
				case 299: goto case 234;
				case 300: goto case 236;
				case 301: goto case 237;
				case 302: goto case 238;
				case 303: goto case 243;
				case 304: goto case 244;
				case 305: goto case 246;
				case 306: goto case 247;
				case 307: goto case 250;
				case 308: goto case 251;
				case 309: goto case 262;
				case 310: goto case 264;
				case 312: goto case 8;
				case 313: goto case 10;
				case 314: goto case 21;
				case 315: goto case 26;
				case 316: goto case 195;
				case 317: goto case 196;
				case 318: goto case 217;
				case 319: goto case 218;
				case 320: goto case 236;
				case 321: goto case 237;
				case 323: goto case 8;
				case 324: goto case 10;
				case 326: goto case 8;
				case 327: goto case 10;
				case 329: goto case 8;
				case 330: goto case 10;
				case 332: goto case 8;
				case 333: goto case 10;
				case 335: goto case 8;
				case 336: goto case 10;
				case 338: goto case 8;
				case 339: goto case 10;
				case 341: goto case 8;
				case 342: goto case 10;
				case 344: goto case 8;
				case 345: goto case 10;
				case 347: goto case 8;
				case 348: goto case 10;
				case 350: goto case 8;
				case 351: goto case 10;
				case 353: goto case 8;
				case 354: goto case 10;
				case 356: goto case 8;
				case 357: goto case 10;
				case 359: goto case 8;
				case 360: goto case 10;
				case 362: goto case 8;
				case 363: goto case 10;
				case 365: goto case 8;
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
				case 526: goto case 10;
				case 528: goto case 10;
				case 530: goto case 10;
				case 532: goto case 10;
				case 534: goto case 10;
				case 536: goto case 10;
				case 538: goto case 10;
				case 540: goto case 10;
				case 542: goto case 10;
				case 602: goto case 5;
				case 603: goto case 10;
				case 604: goto case 205;
				case 605: goto case 207;
				case 606: goto case 228;
				case 607: goto case 229;
				case 608: goto case 246;
				case 609: goto case 247;
				case 630: goto case 10;
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
				case 778: goto case 10;
				case 779: goto case 10;
				case 780: goto case 10;
				case 781: goto case 10;
				case 782: goto case 10;
				case 783: goto case 10;
				case 784: goto case 10;
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
				case 801: goto case 5;
				case 802: goto case 10;
				case 803: goto case 205;
				case 804: goto case 228;
				case 805: goto case 246;
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
				case 877: goto case 10;
				case 878: goto case 10;
				case 879: goto case 10;
				case 880: goto case 10;
				case 881: goto case 10;
				case 882: goto case 10;
				case 883: goto case 10;
				case 884: goto case 10;
				case 885: goto case 10;
				case 886: goto case 10;
				case 887: goto case 10;
				case 888: goto case 10;
				case 889: goto case 10;
				case 890: goto case 10;
				case 891: goto case 10;
				case 892: goto case 10;
				case 893: goto case 10;
				case 894: goto case 10;
				case 895: goto case 10;
				case 896: goto case 10;
				case 897: goto case 10;
				case 898: goto case 10;
				case 899: goto case 10;
				case 900: goto case 10;
				case 901: goto case 10;
				case 902: goto case 10;
				case 903: goto case 10;
				case 904: goto case 10;
				case 905: goto case 10;
				case 906: goto case 10;
				case 907: goto case 5;
				case 908: goto case 205;
				case 909: goto case 228;
				case 910: goto case 246;
				case 912: goto case 10;
				case 913: goto case 10;
				case 914: goto case 10;
				case 915: goto case 10;
				case 916: goto case 10;
				case 917: goto case 10;
				case 918: goto case 10;
				case 919: goto case 10;
				case 920: goto case 5;
				case 921: goto case 205;
				case 922: goto case 228;
				case 923: goto case 246;
				case 924: goto case 5;
				case 925: goto case 205;
				case 926: goto case 228;
				case 927: goto case 246;
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
			AcceptConditions.Accept, // 249
			AcceptConditions.Accept, // 250
			AcceptConditions.AcceptOnStart, // 251
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
			AcceptConditions.Accept, // 264
			AcceptConditions.Accept, // 265
			AcceptConditions.NotAccept, // 266
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
			AcceptConditions.Accept, // 305
			AcceptConditions.Accept, // 306
			AcceptConditions.Accept, // 307
			AcceptConditions.AcceptOnStart, // 308
			AcceptConditions.Accept, // 309
			AcceptConditions.Accept, // 310
			AcceptConditions.NotAccept, // 311
			AcceptConditions.Accept, // 312
			AcceptConditions.Accept, // 313
			AcceptConditions.Accept, // 314
			AcceptConditions.Accept, // 315
			AcceptConditions.Accept, // 316
			AcceptConditions.Accept, // 317
			AcceptConditions.Accept, // 318
			AcceptConditions.Accept, // 319
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
			AcceptConditions.Accept, // 540
			AcceptConditions.NotAccept, // 541
			AcceptConditions.Accept, // 542
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
			AcceptConditions.Accept, // 602
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
			AcceptConditions.Accept, // 630
			AcceptConditions.NotAccept, // 631
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
			AcceptConditions.Accept, // 910
			AcceptConditions.NotAccept, // 911
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
			AcceptConditions.Accept, // 925
			AcceptConditions.Accept, // 926
			AcceptConditions.Accept, // 927
		};
		
		private static int[] colMap = new int[]
		{
			64, 64, 64, 64, 64, 64, 64, 64, 64, 23, 11, 64, 64, 24, 64, 64, 
			64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 
			69, 44, 20, 57, 50, 1, 47, 21, 58, 60, 45, 42, 53, 43, 46, 25, 
			55, 56, 62, 61, 39, 68, 38, 68, 59, 52, 41, 66, 2, 18, 22, 4, 
			53, 13, 31, 6, 26, 17, 28, 15, 19, 8, 40, 32, 12, 36, 14, 29, 
			9, 35, 7, 5, 10, 16, 33, 30, 27, 37, 51, 70, 54, 70, 49, 34, 
			3, 13, 31, 6, 26, 17, 28, 15, 19, 8, 40, 32, 12, 36, 14, 29, 
			9, 35, 7, 5, 10, 16, 33, 30, 27, 37, 51, 63, 48, 65, 53, 64, 
			67, 0
		};
		
		private static int[] rowMap = new int[]
		{
			0, 1, 2, 3, 4, 5, 1, 1, 6, 1, 7, 8, 9, 10, 11, 1, 
			12, 1, 1, 1, 1, 13, 14, 1, 1, 1, 15, 16, 17, 18, 19, 1, 
			1, 1, 1, 20, 1, 1, 21, 22, 23, 17, 24, 1, 1, 1, 1, 1, 
			1, 1, 1, 1, 1, 1, 1, 1, 1, 25, 1, 26, 1, 17, 17, 17, 
			17, 1, 1, 17, 17, 27, 17, 28, 1, 29, 30, 17, 17, 1, 17, 17, 
			17, 17, 17, 31, 17, 17, 32, 17, 17, 1, 17, 17, 17, 17, 17, 17, 
			17, 17, 17, 17, 17, 17, 17, 17, 33, 17, 17, 17, 17, 17, 17, 1, 
			17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 34, 17, 17, 35, 1, 
			1, 1, 1, 36, 37, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 1, 
			1, 1, 1, 1, 1, 17, 17, 17, 17, 17, 17, 17, 17, 17, 1, 1, 
			1, 1, 1, 17, 17, 17, 17, 17, 17, 17, 17, 17, 1, 17, 17, 17, 
			17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 38, 39, 40, 41, 42, 
			43, 44, 1, 45, 46, 47, 42, 1, 48, 1, 1, 49, 1, 50, 1, 51, 
			1, 1, 52, 53, 1, 54, 1, 55, 56, 57, 58, 59, 54, 1, 60, 1, 
			1, 1, 61, 1, 62, 63, 1, 1, 64, 65, 66, 67, 68, 69, 70, 65, 
			1, 71, 1, 1, 72, 1, 73, 74, 1, 1, 75, 1, 1, 76, 1, 77, 
			78, 79, 1, 80, 81, 1, 82, 83, 1, 1, 84, 85, 86, 1, 87, 88, 
			89, 90, 91, 1, 92, 1, 93, 94, 95, 96, 97, 1, 98, 1, 1, 1, 
			1, 99, 100, 101, 1, 102, 1, 1, 1, 1, 103, 104, 105, 106, 1, 107, 
			1, 1, 1, 1, 108, 1, 109, 110, 111, 112, 113, 114, 115, 1, 116, 1, 
			117, 1, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 
			132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 
			148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 1, 158, 159, 160, 161, 162, 
			163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 9, 176, 177, 
			178, 10, 179, 180, 181, 182, 183, 184, 185, 29, 186, 30, 187, 188, 189, 190, 
			191, 192, 193, 194, 195, 196, 197, 160, 198, 199, 200, 201, 202, 203, 204, 205, 
			206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 216, 32, 217, 218, 219, 28, 
			220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 
			236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 
			252, 253, 254, 255, 256, 257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 
			268, 269, 270, 271, 272, 273, 274, 275, 276, 277, 278, 279, 280, 281, 282, 283, 
			284, 285, 286, 287, 288, 289, 290, 291, 292, 293, 294, 295, 296, 297, 298, 299, 
			300, 301, 302, 303, 304, 305, 306, 307, 308, 309, 310, 311, 312, 313, 314, 315, 
			316, 317, 318, 319, 320, 321, 322, 323, 324, 325, 326, 327, 328, 329, 330, 331, 
			332, 333, 334, 335, 336, 337, 338, 339, 340, 341, 342, 343, 344, 345, 346, 347, 
			348, 349, 350, 351, 352, 353, 354, 355, 38, 356, 357, 358, 359, 360, 361, 362, 
			363, 364, 365, 366, 367, 115, 368, 369, 370, 371, 372, 116, 373, 374, 375, 117, 
			376, 377, 378, 379, 380, 381, 382, 383, 384, 385, 386, 387, 388, 389, 390, 391, 
			392, 393, 394, 395, 213, 396, 397, 398, 399, 400, 401, 402, 403, 404, 405, 406, 
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
			679, 556, 680, 681, 682, 683, 684, 17, 685, 686, 687, 688, 689, 690, 691, 692, 
			693, 694, 695, 696, 697, 698, 699, 700, 701, 702, 703, 704, 705, 706, 707, 708
		};
		
		private static int[,] nextState = new int[,]
		{
			{ 1, 2, 268, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 2, 266, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 311, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 924, -1, 924, 924, 924, 920, 924, 924, 924, 924, -1, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, -1, -1, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, -1, 924, -1, 924 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 20, -1, -1, -1, 21, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 906, -1, 903, 903, 903, 903, 903, 636, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 637, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 11, -1 },
			{ -1, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 32, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, -1, 381, 381, 381, 383, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, -1, 381, 381, 381 },
			{ -1, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 33, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 387, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, -1, 385, 385, 385 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 391, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 14, 14, -1, -1, -1, -1, -1, -1, 42, -1, -1, -1, -1, -1, 14, -1, -1, 14, 14, -1, -1, 14, -1, 14, 14, -1, -1, -1, -1, -1, 14, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 397, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 275, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 314, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 417, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 58, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 315, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 873, 851, 903, 903, 903, 59, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 815, 852, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 65, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 66, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 427, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 818, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 895, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 391, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 42, 42, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 42, -1, -1, 42, 42, -1, -1, 42, -1, 42, 42, -1, -1, -1, -1, -1, 42, -1, -1 },
			{ -1, -1, -1, -1, -1, 57, 57, 57, 57, 57, 57, -1, 57, 57, 57, 57, 57, 57, -1, 57, -1, -1, -1, -1, -1, -1, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 57, 57, -1, -1, 57, 57, -1, -1, 57, -1, 57, 57, -1, -1, -1, -1, -1, 57, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 706, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 394, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 719, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, 71, 71, -1, -1, 71, -1, 71, 71, -1, -1, -1, -1, -1, 71, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 73, -1, -1, -1, -1, -1, -1, 73, -1, -1, -1, 73, -1, -1, -1, -1, -1, -1, -1, -1, 73, -1, 73, -1, -1, 73, -1, -1, -1, -1, -1, -1, 73, 73, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, -1, -1, 73, 73, -1, -1, 73, -1, 73, 73, -1, -1, -1, -1, -1, 73, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 74, 74, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 446, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 86, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 86, 86, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 86, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 470, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 774, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 840, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 781, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 915, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, -1, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, -1, 187, 187, 187 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 188, 188, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 188, -1, -1, 188, 188, -1, -1, 188, -1, 188, 188, -1, -1, -1, -1, -1, 188, 279, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 189, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 189, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 190, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 190, -1 },
			{ -1, 191, 191, -1, 191, -1, -1, -1, -1, -1, -1, 191, -1, -1, -1, -1, -1, -1, 191, -1, -1, 191, 191, 191, 191, 191, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 191, 191, 191, 191, 191, 191, 191, 191, 191, -1, -1, -1, 191, -1, -1, -1, 191, 191, -1, 191, -1, -1, -1, -1, -1, 191, -1, -1, 191, -1 },
			{ -1, -1, -1, 192, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 193, 193, 193, 193, 193, 193, -1, 193, 193, 193, 193, 193, 193, -1, 193, -1, -1, -1, -1, -1, -1, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 193, 193, -1, -1, 193, 193, -1, -1, 193, -1, 193, 193, -1, -1, -1, -1, -1, 193, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 195, 195, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 195, -1, -1, 195, 195, -1, -1, 195, -1, 195, 195, -1, -1, -1, -1, -1, 195, -1, -1 },
			{ -1, 199, 199, 199, 199, 200, 200, 200, 200, 200, 200, 199, 200, 200, 200, 200, 200, 200, 199, 200, 199, 199, 199, 199, 199, 199, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 199, 199, 200, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 200, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 201, 199, 199, 199, -1, 199, 199, 199 },
			{ -1, 202, 202, 202, 202, 202, 203, 204, 202, 202, 204, 202, 202, 202, 204, 202, 205, 202, 202, 202, 206, 202, 202, 202, 202, 202, 202, 284, 202, 202, 202, 202, 202, 202, 202, 202, 202, 202, 207, 207, 202, 202, 202, 202, 202, 202, 202, 202, 202, 202, 204, 202, 202, 202, 204, 207, 207, 202, 202, 202, 202, 207, 207, 208, 202, 202, 202, -1, 207, 202, 202 },
			{ -1, -1, -1, -1, -1, 200, 200, 200, 200, 200, 200, -1, 200, 200, 200, 200, 200, 200, -1, 200, -1, -1, -1, -1, -1, -1, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 200, 200, -1, -1, 200, 200, -1, -1, 200, -1, 200, 200, -1, -1, -1, -1, -1, 200, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 582, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 925, -1, -1, -1, -1, -1, -1, 925, -1, -1, -1, 925, -1, -1, -1, -1, -1, -1, -1, -1, 925, -1, 925, -1, -1, 925, -1, -1, -1, -1, -1, -1, 925, 925, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 925, -1, -1, 925, 925, -1, -1, 925, -1, 925, 925, -1, -1, -1, -1, -1, 925, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 605, 605, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 605, 605, -1, -1, -1, -1, 605, 605, -1, -1, -1, -1, -1, 605, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, -1, 288, -1, -1, -1, 288, -1, -1, -1, -1, -1, -1, -1, -1, 288, -1, 288, -1, -1, 288, -1, -1, -1, -1, -1, -1, 288, 288, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 288, -1, -1, 288, 288, -1, -1, 288, -1, 288, 288, -1, -1, -1, -1, -1, 288, -1, -1 },
			{ -1, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, -1, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 585, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, -1, 211, 211, 211 },
			{ -1, 213, 213, -1, 213, -1, -1, -1, -1, -1, -1, 213, -1, -1, -1, -1, -1, -1, 213, -1, -1, 213, 213, 213, 213, 213, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 213, 213, 213, 213, 213, 213, 213, 213, 213, -1, -1, -1, 213, -1, -1, -1, 213, 213, -1, 213, -1, -1, -1, -1, -1, 213, -1, -1, 213, -1 },
			{ -1, -1, -1, -1, -1, 215, 215, 215, 215, 215, 215, -1, 215, 215, 215, 215, 215, 215, -1, 215, -1, -1, -1, -1, -1, -1, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 215, 215, -1, -1, 215, 215, -1, -1, 215, -1, 215, 215, -1, -1, -1, -1, -1, 215, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 216, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 217, 217, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 217, -1, -1, 217, 217, -1, -1, 217, -1, 217, 217, -1, -1, -1, -1, -1, 217, -1, -1 },
			{ -1, 221, 221, 221, 221, 222, 222, 222, 222, 222, 222, 221, 222, 222, 222, 222, 222, 222, 221, 222, 221, 221, 221, 221, 221, 221, 222, 222, 222, 222, 222, 222, 222, 222, 222, 222, 222, 222, 221, 221, 222, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 222, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 223, 221, 221, 221, -1, 221, 221, 221 },
			{ -1, 224, 224, 225, 224, 224, 226, 227, 224, 224, 227, 224, 224, 224, 227, 224, 228, 224, 224, 224, 224, 224, 224, 224, 224, 224, 224, 293, 224, 224, 224, 224, 224, 224, 224, 224, 224, 224, 229, 229, 224, 224, 224, 224, 224, 224, 224, 224, 224, 224, 227, 224, 224, 224, 227, 229, 229, 224, 224, 224, 224, 229, 229, 230, 224, 224, 224, -1, 229, 224, 224 },
			{ -1, -1, -1, -1, -1, 222, 222, 222, 222, 222, 222, -1, 222, 222, 222, 222, 222, 222, -1, 222, -1, -1, -1, -1, -1, -1, 222, 222, 222, 222, 222, 222, 222, 222, 222, 222, 222, 222, 222, 222, 222, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 222, 222, -1, -1, 222, 222, -1, -1, 222, -1, 222, 222, -1, -1, -1, -1, -1, 222, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 588, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 926, -1, -1, -1, -1, -1, -1, 926, -1, -1, -1, 926, -1, -1, -1, -1, -1, -1, -1, -1, 926, -1, 926, -1, -1, 926, -1, -1, -1, -1, -1, -1, 926, 926, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 926, -1, -1, 926, 926, -1, -1, 926, -1, 926, 926, -1, -1, -1, -1, -1, 926, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 607, 607, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 607, 607, -1, -1, -1, -1, 607, 607, -1, -1, -1, -1, -1, 607, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 297, -1, -1, -1, -1, -1, -1, 297, -1, -1, -1, 297, -1, -1, -1, -1, -1, -1, -1, -1, 297, -1, 297, -1, -1, 297, -1, -1, -1, -1, -1, -1, 297, 297, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 297, -1, -1, 297, 297, -1, -1, 297, -1, 297, 297, -1, -1, -1, -1, -1, 297, -1, -1 },
			{ -1, 233, 233, -1, 233, -1, -1, -1, -1, -1, -1, 233, -1, -1, -1, -1, -1, -1, 233, -1, -1, 233, 233, 233, 233, 233, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 233, 233, 233, 233, 233, 233, 233, 233, 233, -1, -1, -1, 233, -1, -1, -1, 233, 233, -1, 233, -1, -1, -1, -1, -1, 233, -1, -1, 233, -1 },
			{ -1, -1, -1, 234, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 234, 234, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 235, 235, 235, 235, 235, 235, -1, 235, 235, 235, 235, 235, 235, -1, 235, -1, -1, -1, -1, -1, -1, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 235, 235, -1, -1, 235, 235, -1, -1, 235, -1, 235, 235, -1, -1, -1, -1, -1, 235, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 236, 236, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 236, -1, -1, 236, 236, -1, -1, 236, -1, 236, 236, -1, -1, -1, -1, -1, 236, -1, -1 },
			{ -1, 240, 240, 240, 240, 241, 241, 241, 241, 241, 241, 240, 241, 241, 241, 241, 241, 241, 240, 241, 240, 240, 240, 240, 240, 240, 241, 241, 241, 241, 241, 241, 241, 241, 241, 241, 241, 241, 240, 240, 241, 240, 240, 240, 240, 240, 240, 240, 240, 240, 240, 241, 240, 240, 240, 240, 240, 240, 240, 240, 240, 240, 240, 242, 240, 240, 240, -1, 240, 240, 240 },
			{ -1, 243, 243, 243, 243, 243, 244, 245, 243, 243, 245, 243, 243, 243, 245, 243, 246, 243, 243, 243, 243, 243, 243, 243, 243, 243, 243, 303, 243, 243, 243, 243, 243, 243, 243, 243, 243, 243, 247, 247, 243, 243, 243, 243, 243, 243, 243, 243, 243, 243, 245, 243, 243, 243, 245, 247, 247, 243, 243, 243, 243, 247, 247, 248, 243, 243, 243, -1, 247, 243, 243 },
			{ -1, -1, -1, -1, -1, 241, 241, 241, 241, 241, 241, -1, 241, 241, 241, 241, 241, 241, -1, 241, -1, -1, -1, -1, -1, -1, 241, 241, 241, 241, 241, 241, 241, 241, 241, 241, 241, 241, 241, 241, 241, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 241, 241, -1, -1, 241, 241, -1, -1, 241, -1, 241, 241, -1, -1, -1, -1, -1, 241, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 593, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 927, -1, -1, -1, -1, -1, -1, 927, -1, -1, -1, 927, -1, -1, -1, -1, -1, -1, -1, -1, 927, -1, 927, -1, -1, 927, -1, -1, -1, -1, -1, -1, 927, 927, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 927, -1, -1, 927, 927, -1, -1, 927, -1, 927, 927, -1, -1, -1, -1, -1, 927, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 609, 609, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 609, 609, -1, -1, -1, -1, 609, 609, -1, -1, -1, -1, -1, 609, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 307, -1, -1, -1, -1, -1, -1, 307, -1, -1, -1, 307, -1, -1, -1, -1, -1, -1, -1, -1, 307, -1, 307, -1, -1, 307, -1, -1, -1, -1, -1, -1, 307, 307, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 307, -1, -1, 307, 307, -1, -1, 307, -1, 307, 307, -1, -1, -1, -1, -1, 307, -1, -1 },
			{ -1, -1, -1, -1, -1, 253, 253, 253, 253, 253, 253, -1, 253, 253, 253, 253, 253, 253, -1, 253, -1, -1, -1, -1, -1, -1, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 253, 253, -1, -1, 253, 253, -1, -1, 253, -1, 253, 253, -1, -1, -1, -1, -1, 253, -1, -1 },
			{ -1, -1, -1, -1, -1, 255, 255, 255, 255, 255, 255, -1, 255, 255, 255, 255, 255, 255, -1, 255, -1, -1, -1, -1, -1, -1, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 255, 255, -1, -1, 255, 255, -1, -1, 255, -1, 255, 255, -1, -1, -1, -1, -1, 255, -1, -1 },
			{ -1, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, -1, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, -1, 256, 256, 256 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 258, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, -1, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, -1, 259, 259, 259 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 261, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 265, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 263, 263, -1, 263, 263, 263, 263, 263, 263, -1, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, -1, 263, -1, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, -1, 263, 263, 263 },
			{ -1, -1, -1, 2, -1, -1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, 592, 592, 592, 592, 592, 592, -1, 592, 592, 592, 592, 592, 592, -1, 592, -1, -1, -1, -1, -1, -1, 592, 592, 592, 592, 592, 592, 592, 592, 592, 592, 592, 592, -1, -1, 592, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 592, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 3, -1, 2, 4, 5, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, 924, -1, 924, 924, 924, 924, 924, 924, 924, 924, 328, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 328, 328, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, -1, 924, 328, 924 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 22, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 23, -1, -1, -1, 24, -1, -1, 377, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 25, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 642, 903, 903, 903, 903, 903, -1, 903, 903, 27, 903, 903, 903, -1, 903, -1, 379, -1, -1, -1, -1, 903, 903, 28, 903, 903, 903, 903, 903, 903, 903, 643, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 391, -1, -1, -1, -1, -1, -1, -1, -1, -1, 393, -1, -1, -1, 395, -1, -1, -1, -1, -1, -1, 14, 14, -1, -1, -1, -1, -1, -1, 42, -1, -1, -1, -1, -1, 14, -1, -1, 14, 14, -1, -1, 14, -1, 14, 14, -1, -1, -1, -1, -1, 14, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 72, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 89, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 279, -1 },
			{ -1, 191, 191, -1, 191, -1, -1, -1, -1, -1, -1, 191, -1, -1, -1, -1, -1, -1, 191, -1, -1, 191, 198, 191, 191, 191, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 191, 191, 191, 191, 191, 191, 191, 191, 191, -1, -1, -1, 191, -1, -1, -1, 191, 191, -1, 191, -1, -1, -1, -1, -1, 191, -1, -1, 191, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 581, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 195, 195, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 195, -1, -1, 195, 195, -1, -1, 195, -1, 195, 195, -1, -1, -1, -1, -1, 195, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 209, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 210, -1, -1, -1, -1, -1, -1, 210, -1, -1, -1, 210, -1, -1, -1, -1, -1, -1, -1, -1, 210, -1, 210, -1, -1, 210, -1, -1, -1, -1, -1, -1, 210, 210, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 210, -1, -1, 210, 210, -1, -1, 210, -1, 210, 210, -1, -1, -1, -1, -1, 210, -1, -1 },
			{ -1, 213, 213, -1, 213, -1, -1, -1, -1, -1, -1, 213, -1, -1, -1, -1, -1, -1, 213, -1, -1, 213, 220, 213, 213, 213, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 213, 213, 213, 213, 213, 213, 213, 213, 213, -1, -1, -1, 213, -1, -1, -1, 213, 213, -1, 213, -1, -1, -1, -1, -1, 213, -1, -1, 213, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 587, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 217, 217, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 217, -1, -1, 217, 217, -1, -1, 217, -1, 217, 217, -1, -1, -1, -1, -1, 217, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 231, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 232, -1, -1, -1, -1, -1, -1, 232, -1, -1, -1, 232, -1, -1, -1, -1, -1, -1, -1, -1, 232, -1, 232, -1, -1, 232, -1, -1, -1, -1, -1, -1, 232, 232, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 232, -1, -1, 232, 232, -1, -1, 232, -1, 232, 232, -1, -1, -1, -1, -1, 232, -1, -1 },
			{ -1, 233, 233, -1, 233, -1, -1, -1, -1, -1, -1, 233, -1, -1, -1, -1, -1, -1, 233, -1, -1, 233, 239, 233, 233, 233, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 233, 233, 233, 233, 233, 233, 233, 233, 233, -1, -1, -1, 233, -1, -1, -1, 233, 233, -1, 233, -1, -1, -1, -1, -1, 233, -1, -1, 233, -1 },
			{ -1, 233, 233, 234, 233, -1, -1, -1, -1, -1, -1, 233, -1, -1, -1, -1, -1, -1, 233, -1, 234, 299, 233, 233, 233, 233, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 233, 233, 233, 233, 233, 233, 233, 233, 233, -1, -1, -1, 233, -1, -1, -1, 233, 233, -1, 233, -1, -1, -1, -1, -1, 233, -1, -1, 233, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 591, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 236, 236, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 236, -1, -1, 236, 236, -1, -1, 236, -1, 236, 236, -1, -1, -1, -1, -1, 236, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 249, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 250, -1, -1, -1, -1, -1, -1, 250, -1, -1, -1, 250, -1, -1, -1, -1, -1, -1, -1, -1, 250, -1, 250, -1, -1, 250, -1, -1, -1, -1, -1, -1, 250, 250, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 250, -1, -1, 250, 250, -1, -1, 250, -1, 250, 250, -1, -1, -1, -1, -1, 250, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 251, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 264, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 322, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 26, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 29, 903, 849, 903, 903, 903, -1, 903, 903, 336, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 810, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 275, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, 316, -1, -1, 316, -1, -1, -1, -1, -1, -1, 316, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, 316, 316, -1, -1, 316, -1, 316, 316, -1, -1, -1, -1, -1, 316, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, 318, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, -1, 318, -1, 318, -1, -1, 318, -1, -1, -1, -1, -1, -1, 318, 318, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 318, -1, -1, 318, 318, -1, -1, 318, -1, 318, 318, -1, -1, -1, -1, -1, 318, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, 320, -1, -1, 320, -1, -1, -1, -1, -1, -1, 320, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, 320, 320, -1, -1, 320, -1, 320, 320, -1, -1, -1, -1, -1, 320, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 325, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 30, -1, -1, -1, 31, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 345, 903, 903, -1, 903, 903, 903, 903, 903, 657, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 39, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 7, 271, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 7, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 34, -1, -1, -1, 35, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 40, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 660, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 328, 331, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 328, 328, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 328, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 36, -1, -1, -1, -1, -1, -1, 37, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 38, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 389, -1, -1, 903, 903, 662, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, 12, 13, -1, -1, -1, -1, 903, 903, 903, 663, 903, 903, 903, 903, 903, 903, 903, 41, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 334, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 43, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 44, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 675, 903, 903, 363, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 61, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 337, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 45, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 46, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 62, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 340, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 47, -1, -1, -1, 48, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 49, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 63, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 343, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 276, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 64, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 346, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 50, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 67, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 349, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 51, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 42, 42, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 42, -1, -1, 42, 42, -1, -1, 42, -1, 42, 42, -1, -1, -1, -1, -1, 42, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 68, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 352, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 52, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 53, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 69, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 352, -1, -1, -1, -1, -1, -1, 355, -1, -1, -1, -1, 352, 352, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 352, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 54, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 55, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 70, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 358, -1, 355, -1, -1, -1, -1, -1, -1, -1, -1, 361, 911, -1, 355, 355, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 355, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 700, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 75, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 610, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 57, 57, 57, 57, 57, 57, -1, 57, 57, 57, 57, 57, 57, -1, 57, -1, -1, -1, -1, -1, -1, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, -1, -1, 57, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 57, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 76, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 364, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 78, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 399, -1, 401, 403, -1, -1, -1, -1, 613, -1, -1, 405, -1, -1, -1, -1, -1, -1, 407, -1, -1, 409, -1, 411, 413, -1, 415, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 407, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 79, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 367, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 269, 367, 367, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 367, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 80, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 371, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 81, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 367, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 82, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 367, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 83, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ 1, 8, 272, 9, 312, 10, 802, 846, 273, 870, 603, 11, 885, 313, 630, 894, 632, 900, 323, 903, 12, 13, 326, 11, 11, 329, 324, 633, 634, 327, 904, 330, 903, 635, 905, 903, 903, 903, 14, 14, 903, 332, 335, 338, 341, 344, 347, 350, 353, 356, 359, 903, 14, 362, 15, 274, 14, 16, 365, 14, 362, 14, 14, 17, 18, 19, 362, 1, 14, 11, 362 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 84, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 419, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 85, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 421, 423, 423, 421, 421, 421, 421, 421, 421, 421, 423, 421, 421, 421, 421, 421, 421, 421, 421, 421, 60, 423, 421, 423, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 425, 421, 421, 423, 421, 421, 421, 421, 421, 421, 421, 421, 421, -1, 421, 421, 421 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 87, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 88, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, 381, -1, 381, 381, 381 },
			{ -1, -1, -1, -1, -1, 90, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 91, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, 385, -1, 385, 385, 385 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 92, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, 429, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 93, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, 71, -1, -1, 431, 431, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, 71, 71, -1, -1, 71, -1, 71, 71, -1, -1, -1, -1, -1, 71, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 94, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 95, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 96, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 433, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 97, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 435, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 98, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 614, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 99, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 437, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 100, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 439, -1, -1, -1, -1, -1, 441, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 101, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 102, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 443, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 103, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 445, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 104, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 447, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 105, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 616, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 617, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 106, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 449, 449, 449, 449, 449, 449, -1, 449, 449, 449, 449, 449, 449, -1, 449, 451, 618, -1, 417, -1, -1, 449, 449, 449, 449, 449, 449, 449, 449, 449, 449, 449, 449, -1, -1, 449, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 449, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 417, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 107, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 807, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 108, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 421, 423, 423, 421, 421, 421, 421, 421, 421, 421, 423, 421, 421, 421, 421, 421, 421, 421, 421, 421, 77, 423, 421, 423, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 425, 421, 421, 423, 421, 421, 421, 421, 421, 421, 421, 421, 421, -1, 421, 421, 421 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 109, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 60, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 453, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, -1, 423, 423, 423 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 110, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 421, 612, 612, 421, 421, 421, 421, 421, 421, 421, 612, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 612, 421, 455, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 612, 421, 421, 421, 421, 421, 421, 421, 421, 421, -1, 421, 421, 421 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 112, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 113, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, 417, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 114, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 115, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 457, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 116, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 459, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 117, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 463, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 118, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 467, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 119, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 469, -1, -1, 471, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 120, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 473, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 121, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 621, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 122, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 622, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 123, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 449, 449, 449, 449, 449, 449, 89, 449, 449, 449, 449, 449, 449, -1, 449, -1, -1, -1, -1, 278, -1, 449, 449, 449, 449, 449, 449, 449, 449, 449, 449, 449, 449, 449, 449, 449, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 449, 449, -1, -1, 449, 449, -1, -1, 449, -1, 449, 449, -1, -1, -1, -1, -1, 449, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 124, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 475, 475, 475, 475, 475, 475, -1, 475, 475, 475, 475, 475, 475, -1, 475, -1, -1, -1, -1, -1, -1, 475, 475, 475, 475, 475, 475, 475, 475, 475, 475, 475, 475, -1, -1, 475, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 475, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 125, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 455, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, 612, -1, 612, 612, 612 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 126, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 612, 423, 423, 423, 423, 423, 423, 423, 423, 423, 60, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 453, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, -1, 423, 423, 423 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 131, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 479, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 132, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 481, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 133, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 483, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 134, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 485, -1, -1, -1, -1, -1, 487, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 489, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 491, -1, -1, 493, 111, 495, -1, -1, -1, -1, -1, -1, -1, 487, -1 },
			{ -1, -1, -1, -1, -1, 135, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 497, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 136, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 499, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 137, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 501, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 138, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 503, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 139, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 505, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 140, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 475, 475, 475, 475, 475, 475, -1, 475, 475, 475, 475, 475, 475, -1, 475, 513, -1, -1, -1, -1, -1, 475, 475, 475, 475, 475, 475, 475, 475, 475, 475, 475, 475, 475, 475, 475, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 475, 475, -1, -1, 475, 475, -1, -1, 475, -1, 475, 475, -1, -1, -1, -1, -1, 475, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 141, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 477, 477, 477, 477, 477, 477, -1, 477, 477, 477, 477, 477, 477, -1, 477, -1, 513, -1, -1, -1, -1, 477, 477, 477, 477, 477, 477, 477, 477, 477, 477, 477, 477, 477, 477, 477, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 477, 477, -1, -1, 477, 477, -1, -1, 477, -1, 477, 477, -1, -1, -1, -1, -1, 477, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 142, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 517, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 149, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 623, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 150, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 483, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 127, -1, -1, -1, -1, -1, -1, -1, -1, 483, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 151, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 519, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 152, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 487, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 111, -1, -1, -1, -1, -1, -1, -1, -1, 487, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 153, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 521, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 154, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 523, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 155, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 128, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 156, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 487, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 157, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 525, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 163, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 527, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 529, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 531, -1, -1, 533, 129, 535, -1, -1, -1, -1, -1, -1, -1, 527, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 164, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 165, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 539, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 166, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 167, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 544, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 168, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 545, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 169, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 625, -1, -1, -1, -1, -1, 546, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 130, -1, -1, -1, -1, -1, -1, -1, -1, 546, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 170, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 89, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 278, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 171, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 626, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 173, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 547, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 174, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 549, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 175, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 521, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 143, -1, -1, -1, -1, -1, -1, -1, -1, 521, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 176, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 523, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 144, -1, -1, -1, -1, -1, -1, -1, -1, 523, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 177, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 525, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 145, -1, -1, -1, -1, -1, -1, -1, -1, 525, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 178, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 527, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 129, -1, -1, -1, -1, -1, -1, -1, -1, 527, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 179, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 550, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 180, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 551, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 181, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 146, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 182, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 527, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 183, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 147, -1, -1, -1, -1, -1, -1, -1, -1, 537, -1 },
			{ -1, -1, -1, -1, -1, 903, 184, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 552, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 185, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 483, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 186, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 543, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 148, -1, -1, -1, -1, -1, -1, -1, -1, 543, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 553, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 554, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 546, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 130, -1, -1, -1, -1, -1, -1, -1, -1, 546, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 557, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 557, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 158, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 487, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 550, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 159, -1, -1, -1, -1, -1, -1, -1, -1, 550, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 551, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 160, -1, -1, -1, -1, -1, -1, -1, -1, 551, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 553, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 161, -1, -1, -1, -1, -1, -1, -1, -1, 553, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 554, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 162, -1, -1, -1, -1, -1, -1, -1, -1, 554, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 546, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 26, 556, 556, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 559, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 557, -1, -1, 560, -1, 627, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 557, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 172, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 561, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 562, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 564, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 565, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 628, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 566, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 567, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 569, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 569, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 570, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 569, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 188, 188, -1, -1, -1, 571, -1, -1, -1, -1, -1, -1, -1, -1, 188, -1, -1, 188, 188, -1, -1, 188, -1, 188, 188, -1, -1, -1, -1, -1, 188, 569, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 572, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 188, 188, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 188, -1, -1, 188, 188, -1, -1, 188, -1, 188, 188, -1, -1, -1, -1, -1, 188, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 573, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 574, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 574, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 575, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 574, -1, -1, -1, -1, 629, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 574, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 576, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 578, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 579, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 189, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 190, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ 1, 191, 191, 192, 191, 193, 193, 193, 193, 193, 193, 191, 193, 193, 193, 193, 193, 193, 191, 193, 194, 191, 191, 191, 191, 191, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 195, 195, 193, 191, 191, 280, 191, 191, 191, 191, 191, 191, 196, 193, 195, 191, 197, 281, 195, 191, 191, 195, 191, 195, 195, 282, 283, 317, 191, 1, 195, 191, 317 },
			{ -1, -1, -1, -1, -1, 583, 583, 583, 583, 583, 583, -1, 583, 583, 583, 583, 583, 583, -1, 583, -1, -1, -1, -1, -1, -1, 583, 583, 583, 583, 583, 583, 583, 583, -1, 583, 583, 583, 583, 583, 583, -1, -1, 583, -1, -1, -1, -1, -1, -1, -1, 583, 583, -1, -1, 583, 583, -1, -1, 583, -1, 583, 583, -1, -1, -1, -1, -1, 583, 583, -1 },
			{ -1, -1, -1, -1, -1, 583, 583, 583, 583, 583, 583, -1, 583, 583, 583, 583, 583, 583, -1, 583, -1, -1, -1, -1, -1, -1, 583, 583, 583, 583, 583, 583, 583, 583, -1, 583, 583, 583, 583, 583, 583, -1, -1, 583, -1, -1, -1, -1, -1, -1, -1, 583, 583, -1, -1, 583, 583, -1, -1, 583, -1, 583, 583, -1, -1, 285, -1, -1, 583, 583, -1 },
			{ 1, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 212, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 585, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 1, 211, 211, 211 },
			{ -1, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, -1, 211, 211, 211 },
			{ 1, 213, 213, 214, 213, 215, 215, 215, 215, 215, 215, 213, 215, 215, 215, 215, 215, 215, 213, 215, 216, 213, 213, 213, 213, 213, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 217, 217, 215, 213, 213, 289, 213, 213, 213, 213, 213, 213, 218, 215, 217, 213, 219, 290, 217, 213, 213, 217, 213, 217, 217, 291, 292, 319, 213, 1, 217, 213, 319 },
			{ -1, -1, -1, -1, -1, 589, 589, 589, 589, 589, 589, -1, 589, 589, 589, 589, 589, 589, -1, 589, -1, -1, -1, -1, -1, -1, 589, 589, 589, 589, 589, 589, 589, 589, -1, 589, 589, 589, 589, 589, 589, -1, -1, 589, -1, -1, -1, -1, -1, -1, -1, 589, 589, -1, -1, 589, 589, -1, -1, 589, -1, 589, 589, -1, -1, -1, -1, -1, 589, 589, -1 },
			{ -1, -1, -1, -1, -1, 589, 589, 589, 589, 589, 589, -1, 589, 589, 589, 589, 589, 589, -1, 589, -1, -1, -1, -1, -1, -1, 589, 589, 589, 589, 589, 589, 589, 589, -1, 589, 589, 589, 589, 589, 589, -1, -1, 589, -1, -1, -1, -1, -1, -1, -1, 589, 589, -1, -1, 589, 589, -1, -1, 589, -1, 589, 589, -1, -1, 294, -1, -1, 589, 589, -1 },
			{ 1, 233, 233, 234, 233, 235, 235, 235, 235, 235, 235, 233, 235, 235, 235, 235, 235, 235, 233, 235, 234, 299, 233, 233, 233, 233, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 236, 236, 235, 233, 233, 298, 233, 233, 233, 233, 233, 233, 237, 235, 236, 233, 238, 300, 236, 233, 233, 236, 233, 236, 236, 301, 302, 321, 233, 267, 236, 233, 321 },
			{ -1, -1, -1, -1, -1, 592, 592, 592, 592, 592, 592, 251, 592, 592, 592, 592, 592, 592, -1, 592, -1, -1, -1, -1, 308, -1, 592, 592, 592, 592, 592, 592, 592, 592, 592, 592, 592, 592, 592, 592, 592, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 592, 592, -1, -1, 592, 592, -1, -1, 592, -1, 592, 592, -1, -1, -1, 594, -1, 592, -1, -1 },
			{ -1, -1, -1, -1, -1, 595, 595, 595, 595, 595, 595, -1, 595, 595, 595, 595, 595, 595, -1, 595, -1, -1, -1, -1, -1, -1, 595, 595, 595, 595, 595, 595, 595, 595, -1, 595, 595, 595, 595, 595, 595, -1, -1, 595, -1, -1, -1, -1, -1, -1, -1, 595, 595, -1, -1, 595, 595, -1, -1, 595, -1, 595, 595, -1, -1, -1, -1, -1, 595, 595, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 251, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 308, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 595, 595, 595, 595, 595, 595, -1, 595, 595, 595, 595, 595, 595, -1, 595, -1, -1, -1, -1, -1, -1, 595, 595, 595, 595, 595, 595, 595, 595, -1, 595, 595, 595, 595, 595, 595, -1, -1, 595, -1, -1, -1, -1, -1, -1, -1, 595, 595, -1, -1, 595, 595, -1, -1, 595, -1, 595, 595, -1, -1, 304, -1, -1, 595, 595, -1 },
			{ 1, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 302, 267, 302, 302, 302 },
			{ 1, 252, 252, 252, 252, 253, 253, 253, 253, 253, 253, 252, 253, 253, 253, 253, 253, 253, 252, 253, 252, 252, 252, 252, 252, 252, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 252, 252, 253, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 253, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 1, 252, 252, 252 },
			{ 1, 254, 254, 254, 254, 255, 255, 255, 255, 255, 255, 254, 255, 255, 255, 255, 255, 255, 254, 255, 254, 254, 254, 254, 254, 254, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 254, 254, 255, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 255, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 1, 254, 254, 254 },
			{ 1, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 257, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 1, 256, 256, 256 },
			{ 1, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 260, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 259, 1, 259, 259, 259 },
			{ 1, 262, 263, 263, 262, 263, 263, 263, 263, 263, 263, 264, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 309, 263, 310, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 1, 263, 263, 263 },
			{ -1, 924, -1, 924, 924, 924, 924, 924, 924, 924, 270, -1, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, -1, -1, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, -1, 924, -1, 924 },
			{ -1, -1, -1, -1, -1, 903, 903, 333, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 812, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 286, -1, -1, -1, -1, -1, -1, 286, -1, -1, -1, 286, -1, -1, -1, -1, -1, -1, -1, -1, 286, -1, 286, -1, -1, 286, -1, -1, -1, -1, -1, -1, 286, 286, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 286, -1, -1, 286, 286, -1, -1, 286, -1, 286, 286, -1, -1, -1, -1, -1, 286, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 287, 287, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 287, 287, -1, -1, -1, -1, 287, 287, -1, -1, -1, -1, -1, 287, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 295, -1, -1, -1, -1, -1, -1, 295, -1, -1, -1, 295, -1, -1, -1, -1, -1, -1, -1, -1, 295, -1, 295, -1, -1, 295, -1, -1, -1, -1, -1, -1, 295, 295, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 295, -1, -1, 295, 295, -1, -1, 295, -1, 295, 295, -1, -1, -1, -1, -1, 295, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 296, 296, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 296, 296, -1, -1, -1, -1, 296, 296, -1, -1, -1, -1, -1, 296, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 305, -1, -1, -1, -1, -1, -1, 305, -1, -1, -1, 305, -1, -1, -1, -1, -1, -1, -1, -1, 305, -1, 305, -1, -1, 305, -1, -1, -1, -1, -1, -1, 305, 305, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 305, -1, -1, 305, 305, -1, -1, 305, -1, 305, 305, -1, -1, -1, -1, -1, 305, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 306, 306, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 306, 306, -1, -1, -1, -1, 306, 306, -1, -1, -1, -1, -1, 306, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 367, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 373, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 615, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 461, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 465, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 631, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 620, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 477, 477, 477, 477, 477, 477, -1, 477, 477, 477, 477, 477, 477, -1, 477, -1, -1, -1, -1, -1, -1, 477, 477, 477, 477, 477, 477, 477, 477, 477, 477, 477, 477, -1, -1, 477, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 477, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 515, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 511, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 624, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 507, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 543, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 555, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 563, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 568, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 577, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 648, 903, 903, 649, 339, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 509, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 342, 903, 903, 903, 903, 903, -1, 903, 903, 848, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 348, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 658, 809, 903, 903, -1, 903, 659, 903, 903, 847, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 351, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 354, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 357, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 816, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 853, 903, 903, 903, -1, 903, 666, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 667, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 360, 903, 903, 903, 903, 668, -1, 814, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 669, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 850, 903, 903, 903, 903, 670, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 817, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 671, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 672, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 673, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 887, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 674, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 871, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 366, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 872, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 368, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 678, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 370, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 372, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 374, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 681, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 376, 903, 886, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 378, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 683, 916, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 684, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 380, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 686, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 901, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 687, 903, 903, -1, 903, 903, 903, 903, 903, 688, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 689, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 382, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 690, 691, 903, 903, 903, 692, -1, 693, 854, 820, 694, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 695, 903, 696, 903, 855, 903, 903, 903, 903, 903, 697, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 699, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 384, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 386, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 388, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 390, 903, 903, 903, 903, 912, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 703, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 392, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 822, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 707, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 396, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 888, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 856, -1, 903, 903, 903, 903, 903, 708, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 398, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 400, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 402, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 404, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 713, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 406, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 714, 903, 903, 408, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 876, 903, 715, 903, 716, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 410, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 889, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 823, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 412, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 414, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 416, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 418, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 420, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 898, 903, 903, 903, 903, 422, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 828, 721, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 859, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 861, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 826, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 878, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 424, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 877, 903, 903, -1, 903, 903, 903, 903, 903, 917, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 723, 903, 903, -1, 903, 903, 903, 903, 891, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 860, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 426, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 428, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 430, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 432, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 725, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 434, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 902, 903, 903, 903, 879, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 728, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 729, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 436, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 438, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 440, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 442, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 733, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 830, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 444, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 734, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 448, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 833, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 450, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 863, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 739, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 452, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 454, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 746, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 836, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 866, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 884, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 749, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 456, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 458, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 752, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 460, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 835, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 462, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 881, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 865, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 464, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 756, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 466, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 468, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 472, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 474, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 476, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 864, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 758, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 759, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 838, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 882, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 478, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 763, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 480, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 482, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 484, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 767, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 486, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 773, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 488, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 775, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 490, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 842, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 492, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 494, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 868, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 496, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 780, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 498, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 782, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 500, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 502, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 783, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 504, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 506, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 508, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 510, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 737, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 784, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 785, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 512, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 514, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 844, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 789, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 790, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 791, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 516, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 518, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 520, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 522, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 792, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 524, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 526, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 528, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 530, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 795, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 797, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 532, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 798, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 534, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 536, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 538, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 800, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 540, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 542, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 924, -1, 924, 924, 924, 924, 924, 924, 602, 924, -1, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, -1, -1, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, -1, 924, -1, 924 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 638, 639, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 640, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 604, -1, -1, -1, -1, -1, -1, 604, -1, -1, -1, 604, -1, -1, -1, -1, -1, -1, -1, -1, 604, -1, 604, -1, -1, 604, -1, -1, -1, -1, -1, -1, 604, 604, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 604, -1, -1, 604, 604, -1, -1, 604, -1, 604, 604, -1, -1, -1, -1, -1, 604, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 606, -1, -1, -1, -1, -1, -1, 606, -1, -1, -1, 606, -1, -1, -1, -1, -1, -1, -1, -1, 606, -1, 606, -1, -1, 606, -1, -1, -1, -1, -1, -1, 606, 606, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 606, -1, -1, 606, 606, -1, -1, 606, -1, 606, 606, -1, -1, -1, -1, -1, 606, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 608, -1, -1, -1, -1, -1, -1, 608, -1, -1, -1, 608, -1, -1, -1, -1, -1, -1, -1, -1, 608, -1, 608, -1, -1, 608, -1, -1, -1, -1, -1, -1, 608, 608, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 608, -1, -1, 608, 608, -1, -1, 608, -1, 608, 608, -1, -1, -1, -1, -1, 608, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 611, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 619, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 698, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 685, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 896, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 682, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 676, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 679, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 875, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 710, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 701, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 821, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 717, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 718, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 722, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 727, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 829, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 738, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 892, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 735, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 743, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 745, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 742, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 893, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 837, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 754, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 766, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 755, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 918, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 769, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 760, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 771, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 841, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 779, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 786, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 788, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 787, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 793, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 794, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 796, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 641, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 819, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 680, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 677, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 874, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 705, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 711, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 702, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 827, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 724, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 732, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 731, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 740, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 834, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 748, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 913, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 761, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 757, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 776, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 772, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 762, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 843, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 919, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 799, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 644, 903, 903, 903, -1, 903, 645, 903, 903, 646, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 709, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 712, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 704, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 890, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 726, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 831, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 747, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 744, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 832, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 765, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 770, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 778, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 777, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 764, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 647, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 825, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 857, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 730, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 736, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 751, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 862, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 753, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 768, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 650, 903, 903, 903, -1, 813, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 651, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 858, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 824, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 741, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 750, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 652, 903, 903, 903, 903, -1, 653, 903, 654, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 655, 903, 903, 903, 903, 903, 656, 903, 903, 811, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 720, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 880, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 661, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 664, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 808, 903, 903, 903, -1, 903, 665, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 924, -1, 924, 924, 924, 924, 924, 801, 924, 924, -1, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, -1, -1, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, -1, 924, -1, 924 },
			{ -1, -1, -1, -1, -1, -1, 803, -1, -1, -1, -1, -1, -1, 803, -1, -1, -1, 803, -1, -1, -1, -1, -1, -1, -1, -1, 803, -1, 803, -1, -1, 803, -1, -1, -1, -1, -1, -1, 803, 803, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 803, -1, -1, 803, 803, -1, -1, 803, -1, 803, 803, -1, -1, -1, -1, -1, 803, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 804, -1, -1, -1, -1, -1, -1, 804, -1, -1, -1, 804, -1, -1, -1, -1, -1, -1, -1, -1, 804, -1, 804, -1, -1, 804, -1, -1, -1, -1, -1, -1, 804, 804, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 804, -1, -1, 804, 804, -1, -1, 804, -1, 804, 804, -1, -1, -1, -1, -1, 804, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 805, -1, -1, -1, -1, -1, -1, 805, -1, -1, -1, 805, -1, -1, -1, -1, -1, -1, -1, -1, 805, -1, 805, -1, -1, 805, -1, -1, -1, -1, -1, -1, 805, 805, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 805, -1, -1, 805, 805, -1, -1, 805, -1, 805, 805, -1, -1, -1, -1, -1, 805, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 806, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 899, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 883, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 839, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 845, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 897, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 914, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 867, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, -1, 903, 903, 903, 903, 903, 903, -1, 903, -1, -1, -1, -1, -1, -1, 903, 903, 903, 869, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, -1, -1, 903, 903, -1, -1, 903, -1, 903, 903, -1, -1, -1, -1, -1, 903, -1, -1 },
			{ -1, 924, -1, 924, 924, 924, 924, 907, 924, 924, 924, -1, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, -1, -1, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, -1, 924, -1, 924 },
			{ -1, -1, -1, -1, -1, -1, 908, -1, -1, -1, -1, -1, -1, 908, -1, -1, -1, 908, -1, -1, -1, -1, -1, -1, -1, -1, 908, -1, 908, -1, -1, 908, -1, -1, -1, -1, -1, -1, 908, 908, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 908, -1, -1, 908, 908, -1, -1, 908, -1, 908, 908, -1, -1, -1, -1, -1, 908, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 909, -1, -1, -1, -1, -1, -1, 909, -1, -1, -1, 909, -1, -1, -1, -1, -1, -1, -1, -1, 909, -1, 909, -1, -1, 909, -1, -1, -1, -1, -1, -1, 909, 909, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 909, -1, -1, 909, 909, -1, -1, 909, -1, 909, 909, -1, -1, -1, -1, -1, 909, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 910, -1, -1, -1, -1, -1, -1, 910, -1, -1, -1, 910, -1, -1, -1, -1, -1, -1, -1, -1, 910, -1, 910, -1, -1, 910, -1, -1, -1, -1, -1, -1, 910, 910, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 910, -1, -1, 910, 910, -1, -1, 910, -1, 910, 910, -1, -1, -1, -1, -1, 910, -1, -1 },
			{ -1, 924, -1, 924, 924, 924, 924, 924, 924, 924, 924, -1, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, -1, -1, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, 924, -1, 924, -1, 924 },
			{ -1, -1, -1, -1, -1, -1, 921, -1, -1, -1, -1, -1, -1, 921, -1, -1, -1, 921, -1, -1, -1, -1, -1, -1, -1, -1, 921, -1, 921, -1, -1, 921, -1, -1, -1, -1, -1, -1, 921, 921, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 921, -1, -1, 921, 921, -1, -1, 921, -1, 921, 921, -1, -1, -1, -1, -1, 921, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 922, -1, -1, -1, -1, -1, -1, 922, -1, -1, -1, 922, -1, -1, -1, -1, -1, -1, -1, -1, 922, -1, 922, -1, -1, 922, -1, -1, -1, -1, -1, -1, 922, 922, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 922, -1, -1, 922, 922, -1, -1, 922, -1, 922, 922, -1, -1, -1, -1, -1, 922, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 923, -1, -1, -1, -1, -1, -1, 923, -1, -1, -1, 923, -1, -1, -1, -1, -1, -1, -1, -1, 923, -1, 923, -1, -1, 923, -1, -1, -1, -1, -1, -1, 923, 923, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 923, -1, -1, 923, 923, -1, -1, 923, -1, 923, 923, -1, -1, -1, -1, -1, 923, -1, -1 }
		};
		
		
		private static int[] yy_state_dtrans = new int[]
		{
			  0,
			  375,
			  580,
			  584,
			  586,
			  590,
			  596,
			  597,
			  598,
			  599,
			  600,
			  601
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
							System.Diagnostics.Debug.Assert(last_accept_state >= 928);
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

