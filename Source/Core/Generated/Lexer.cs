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
					// #line 284
					{ return (Tokens)GetTokenChar(0); }
					break;
					
				case 9:
					// #line 360
					{ 
						BEGIN(LexicalStates.ST_BACKQUOTE); 
						return Tokens.T_BACKQUOTE; 
					}
					break;
					
				case 10:
					// #line 285
					{ return Tokens.T_STRING; }
					break;
					
				case 11:
					// #line 287
					{ return Tokens.T_WHITESPACE; }
					break;
					
				case 12:
					// #line 344
					{ 
						BEGIN(LexicalStates.ST_DOUBLE_QUOTES); 
						return (GetTokenChar(0) != '"') ? Tokens.T_BINARY_DOUBLE : Tokens.T_DOUBLE_QUOTES; 
					}
					break;
					
				case 13:
					// #line 350
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
					// #line 288
					{ return Tokens.ParseDecimalNumber; }
					break;
					
				case 15:
					// #line 286
					{ return Tokens.T_NS_SEPARATOR; }
					break;
					
				case 16:
					// #line 299
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); yymore(); break; }
					break;
					
				case 17:
					// #line 322
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_LBRACE; }
					break;
					
				case 18:
					// #line 378
					{ return Tokens.ERROR; }
					break;
					
				case 19:
					// #line 323
					{ if (!yy_pop_state()) return Tokens.ERROR; return Tokens.T_RBRACE; }
					break;
					
				case 20:
					// #line 269
					{ return Tokens.T_MOD_EQUAL; }
					break;
					
				case 21:
					// #line 325
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
					// #line 277
					{ return Tokens.T_SL; }
					break;
					
				case 23:
					// #line 262
					{ return Tokens.T_IS_SMALLER_OR_EQUAL; }
					break;
					
				case 24:
					// #line 261
					{ return Tokens.T_IS_NOT_EQUAL; }
					break;
					
				case 25:
					// #line 235
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
					// #line 230
					{ return (InLinq) ? Tokens.T_LINQ_IN : Tokens.T_STRING; }
					break;
					
				case 28:
					// #line 144
					{ return Tokens.T_IF; }
					break;
					
				case 29:
					// #line 155
					{ return Tokens.T_AS; }
					break;
					
				case 30:
					// #line 260
					{ return Tokens.T_IS_EQUAL; }
					break;
					
				case 31:
					// #line 255
					{ return Tokens.T_DOUBLE_ARROW; }
					break;
					
				case 32:
					// #line 338
					{ return Tokens.DoubleQuotedString; }
					break;
					
				case 33:
					// #line 339
					{ return Tokens.SingleQuotedString; }
					break;
					
				case 34:
					// #line 263
					{ return Tokens.T_IS_GREATER_OR_EQUAL; }
					break;
					
				case 35:
					// #line 278
					{ return Tokens.T_SR; }
					break;
					
				case 36:
					// #line 267
					{ return Tokens.T_DIV_EQUAL; }
					break;
					
				case 37:
					// #line 300
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); yymore(); break; }
					break;
					
				case 38:
					// #line 302
					{ BEGIN(LexicalStates.ST_COMMENT); yymore(); break; }
					break;
					
				case 39:
					// #line 150
					{ return Tokens.T_DO; }
					break;
					
				case 40:
					// #line 180
					{ return Tokens.T_LOGICAL_OR; }
					break;
					
				case 41:
					// #line 231
					{ return (InLinq) ? Tokens.T_LINQ_BY : Tokens.T_STRING; }
					break;
					
				case 42:
					// #line 290
					{ return Tokens.ParseDouble; }
					break;
					
				case 43:
					// #line 236
					{ return Tokens.T_RGENERIC; }
					break;
					
				case 44:
					// #line 279
					{ return Tokens.T_DOUBLE_COLON; }
					break;
					
				case 45:
					// #line 264
					{ return Tokens.T_PLUS_EQUAL; }
					break;
					
				case 46:
					// #line 256
					{ return Tokens.T_INC; }
					break;
					
				case 47:
					// #line 265
					{ return Tokens.T_MINUS_EQUAL; }
					break;
					
				case 48:
					// #line 281
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 49:
					// #line 257
					{ return Tokens.T_DEC; }
					break;
					
				case 50:
					// #line 266
					{ return Tokens.T_MUL_EQUAL; }
					break;
					
				case 51:
					// #line 268
					{ return Tokens.T_CONCAT_EQUAL; }
					break;
					
				case 52:
					// #line 272
					{ return Tokens.T_AND_EQUAL; }
					break;
					
				case 53:
					// #line 276
					{ return Tokens.T_BOOLEAN_AND; }
					break;
					
				case 54:
					// #line 273
					{ return Tokens.T_OR_EQUAL; }
					break;
					
				case 55:
					// #line 275
					{ return Tokens.T_BOOLEAN_OR; }
					break;
					
				case 56:
					// #line 274
					{ return Tokens.T_XOR_EQUAL; }
					break;
					
				case 57:
					// #line 282
					{ return Tokens.T_VARIABLE; }
					break;
					
				case 58:
					// #line 270
					{ return Tokens.T_SL_EQUAL; }
					break;
					
				case 59:
					// #line 215
					{ return Tokens.T_INT_TYPE; }
					break;
					
				case 60:
					// #line 341
					{ return Tokens.ErrorInvalidIdentifier; }
					break;
					
				case 61:
					// #line 194
					{ return Tokens.T_TRY; }
					break;
					
				case 62:
					// #line 181
					{ return Tokens.T_LOGICAL_AND; }
					break;
					
				case 63:
					// #line 168
					{ return Tokens.T_NEW; }
					break;
					
				case 64:
					// #line 210
					{ return Tokens.T_USE; }
					break;
					
				case 65:
					// #line 258
					{ return Tokens.T_IS_IDENTICAL; }
					break;
					
				case 66:
					// #line 271
					{ return Tokens.T_SR_EQUAL; }
					break;
					
				case 67:
					// #line 139
					{ return Tokens.T_EXIT; }
					break;
					
				case 68:
					// #line 182
					{ return Tokens.T_LOGICAL_XOR; }
					break;
					
				case 69:
					// #line 151
					{ return Tokens.T_FOR; }
					break;
					
				case 70:
					// #line 169
					{ return Tokens.T_VAR; }
					break;
					
				case 71:
					// #line 291
					{ return Tokens.ParseDouble; }
					break;
					
				case 72:
					// #line 259
					{ return Tokens.T_IS_NOT_IDENTICAL; }
					break;
					
				case 73:
					// #line 289
					{ return Tokens.ParseHexadecimalNumber; }
					break;
					
				case 74:
					// #line 292
					{ return Tokens.ParseBinaryNumber; }
					break;
					
				case 75:
					// #line 249
					{ return Tokens.T_SELF; }
					break;
					
				case 76:
					// #line 158
					{ return Tokens.T_CASE; }
					break;
					
				case 77:
					// #line 340
					{ return Tokens.SingleQuotedIdentifier; }
					break;
					
				case 78:
					// #line 251
					{ return Tokens.T_TRUE; }
					break;
					
				case 79:
					// #line 183
					{ return Tokens.T_LIST; }
					break;
					
				case 80:
					// #line 253
					{ return Tokens.T_NULL; }
					break;
					
				case 81:
					// #line 212
					{ return Tokens.T_GOTO; }
					break;
					
				case 82:
					// #line 162
					{ return Tokens.T_ECHO; }
					break;
					
				case 83:
					// #line 147
					{ return Tokens.T_ELSE; }
					break;
					
				case 84:
					// #line 138
					{ return Tokens.T_EXIT; }
					break;
					
				case 85:
					// #line 170
					{ return Tokens.T_EVAL; }
					break;
					
				case 86:
					// #line 301
					{ BEGIN(LexicalStates.ST_DOC_COMMENT); yymore(); break; }
					break;
					
				case 87:
					// #line 223
					{ return Tokens.T_LINQ_FROM; }
					break;
					
				case 88:
					// #line 214
					{ return Tokens.T_BOOL_TYPE; }
					break;
					
				case 89:
					// #line 365
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
					// #line 164
					{ return Tokens.T_CLASS; }
					break;
					
				case 91:
					// #line 200
					{ return Tokens.T_CLONE; }
					break;
					
				case 92:
					// #line 195
					{ return Tokens.T_CATCH; }
					break;
					
				case 93:
					// #line 141
					{ return Tokens.T_CONST; }
					break;
					
				case 94:
					// #line 176
					{ return Tokens.T_ISSET; }
					break;
					
				case 95:
					// #line 216
					{ return Tokens.T_INT64_TYPE; }
					break;
					
				case 96:
					// #line 163
					{ return Tokens.T_PRINT; }
					break;
					
				case 97:
					// #line 165
					{ return Tokens.T_TRAIT; }
					break;
					
				case 98:
					// #line 197
					{ return Tokens.T_THROW; }
					break;
					
				case 99:
					// #line 184
					{ return Tokens.T_ARRAY; }
					break;
					
				case 100:
					// #line 229
					{ return (InLinq) ? Tokens.T_LINQ_GROUP : Tokens.T_STRING; }
					break;
					
				case 101:
					// #line 179
					{ return Tokens.T_UNSET; }
					break;
					
				case 102:
					// #line 146
					{ return Tokens.T_ENDIF; }
					break;
					
				case 103:
					// #line 177
					{ return Tokens.T_EMPTY; }
					break;
					
				case 104:
					// #line 202
					{ return Tokens.T_FINAL; }
					break;
					
				case 105:
					// #line 252
					{ return Tokens.T_FALSE; }
					break;
					
				case 106:
					// #line 143
					{ return Tokens.T_YIELD; }
					break;
					
				case 107:
					// #line 148
					{ return Tokens.T_WHILE; }
					break;
					
				case 108:
					// #line 224
					{ return (InLinq) ? Tokens.T_LINQ_WHERE : Tokens.T_STRING; }
					break;
					
				case 109:
					// #line 160
					{ return Tokens.T_BREAK; }
					break;
					
				case 110:
					// #line 240
					{ return Tokens.T_SET; }
					break;
					
				case 111:
					// #line 239
					{ return Tokens.T_GET; }
					break;
					
				case 112:
					// #line 306
					{ return Tokens.T_INT32_CAST; }
					break;
					
				case 113:
					// #line 218
					{ return Tokens.T_STRING_TYPE; }
					break;
					
				case 114:
					// #line 178
					{ return Tokens.T_STATIC; }
					break;
					
				case 115:
					// #line 228
					{ return (InLinq) ? Tokens.T_LINQ_SELECT : Tokens.T_STRING; }
					break;
					
				case 116:
					// #line 156
					{ return Tokens.T_SWITCH; }
					break;
					
				case 117:
					// #line 142
					{ return Tokens.T_RETURN; }
					break;
					
				case 118:
					// #line 211
					{ return Tokens.T_IMPORT; }
					break;
					
				case 119:
					// #line 248
					{ return Tokens.T_PARENT; }
					break;
					
				case 120:
					// #line 205
					{ return Tokens.T_PUBLIC; }
					break;
					
				case 121:
					// #line 238
					{ return Tokens.T_ASSERT; }
					break;
					
				case 122:
					// #line 175
					{ return Tokens.T_GLOBAL; }
					break;
					
				case 123:
					// #line 145
					{ return Tokens.T_ELSEIF; }
					break;
					
				case 124:
					// #line 152
					{ return Tokens.T_ENDFOR; }
					break;
					
				case 125:
					// #line 217
					{ return Tokens.T_DOUBLE_TYPE; }
					break;
					
				case 126:
					// #line 220
					{ return Tokens.T_OBJECT_TYPE; }
					break;
					
				case 127:
					// #line 241
					{ return Tokens.T_CALL; }
					break;
					
				case 128:
					// #line 312
					{ return Tokens.T_DOUBLE_CAST; }
					break;
					
				case 129:
					// #line 304
					{ return Tokens.T_INT8_CAST; }
					break;
					
				case 130:
					// #line 310
					{ return Tokens.T_UINT32_CAST; }
					break;
					
				case 131:
					// #line 319
					{ return Tokens.T_BOOL_CAST; }
					break;
					
				case 132:
					// #line 173
					{ return Tokens.T_REQUIRE; }
					break;
					
				case 133:
					// #line 171
					{ return Tokens.T_INCLUDE; }
					break;
					
				case 134:
					// #line 203
					{ return Tokens.T_PRIVATE; }
					break;
					
				case 135:
					// #line 233
					{ return Tokens.T_PARTIAL; }
					break;
					
				case 136:
					// #line 167
					{ return Tokens.T_EXTENDS; }
					break;
					
				case 137:
					// #line 134
					{
					  return Tokens.ErrorNotSupported; 
					}
					break;
					
				case 138:
					// #line 159
					{ return Tokens.T_DEFAULT; }
					break;
					
				case 139:
					// #line 196
					{ return Tokens.T_FINALLY; }
					break;
					
				case 140:
					// #line 153
					{ return Tokens.T_FOREACH; }
					break;
					
				case 141:
					// #line 225
					{ return (InLinq) ? Tokens.T_LINQ_ORDERBY : Tokens.T_STRING; }
					break;
					
				case 142:
					// #line 247
					{ return Tokens.T_SLEEP; }
					break;
					
				case 143:
					// #line 192
					{ return Tokens.T_DIR; }
					break;
					
				case 144:
					// #line 307
					{ return Tokens.T_INT64_CAST; }
					break;
					
				case 145:
					// #line 305
					{ return Tokens.T_INT16_CAST; }
					break;
					
				case 146:
					// #line 317
					{ return Tokens.T_ARRAY_CAST; }
					break;
					
				case 147:
					// #line 308
					{ return Tokens.T_UINT8_CAST; }
					break;
					
				case 148:
					// #line 320
					{ return Tokens.T_UNSET_CAST; }
					break;
					
				case 149:
					// #line 313
					{ return Tokens.T_FLOAT_CAST; }
					break;
					
				case 150:
					// #line 185
					{ return Tokens.T_CALLABLE; }
					break;
					
				case 151:
					// #line 161
					{ return Tokens.T_CONTINUE; }
					break;
					
				case 152:
					// #line 219
					{ return Tokens.T_RESOURCE_TYPE; }
					break;
					
				case 153:
					// #line 201
					{ return Tokens.T_ABSTRACT; }
					break;
					
				case 154:
					// #line 149
					{ return Tokens.T_ENDWHILE; }
					break;
					
				case 155:
					// #line 140
					{ return Tokens.T_FUNCTION; }
					break;
					
				case 156:
					// #line 190
					{ return Tokens.T_LINE; }
					break;
					
				case 157:
					// #line 191
					{ return Tokens.T_FILE; }
					break;
					
				case 158:
					// #line 246
					{ return Tokens.T_WAKEUP; }
					break;
					
				case 159:
					// #line 314
					{ return Tokens.T_STRING_CAST; }
					break;
					
				case 160:
					// #line 311
					{ return Tokens.T_UINT64_CAST; }
					break;
					
				case 161:
					// #line 309
					{ return Tokens.T_UINT16_CAST; }
					break;
					
				case 162:
					// #line 318
					{ return Tokens.T_OBJECT_CAST; }
					break;
					
				case 163:
					// #line 315
					{ return Tokens.T_BINARY_CAST; }
					break;
					
				case 164:
					// #line 221
					{ return Tokens.T_TYPEOF; }
					break;
					
				case 165:
					// #line 166
					{ return Tokens.T_INSTEADOF; }
					break;
					
				case 166:
					// #line 198
					{ return Tokens.T_INTERFACE; }
					break;
					
				case 167:
					// #line 204
					{ return Tokens.T_PROTECTED; }
					break;
					
				case 168:
					// #line 227
					{ return (InLinq) ? Tokens.T_LINQ_ASCENDING : Tokens.T_STRING; }
					break;
					
				case 169:
					// #line 209
					{ return Tokens.T_NAMESPACE; }
					break;
					
				case 170:
					// #line 157
					{ return Tokens.T_ENDSWITCH; }
					break;
					
				case 171:
					// #line 186
					{ return Tokens.T_CLASS_C; }
					break;
					
				case 172:
					// #line 187
					{ return Tokens.T_TRAIT_C; }
					break;
					
				case 173:
					// #line 316
					{ return Tokens.T_UNICODE_CAST; }
					break;
					
				case 174:
					// #line 206
					{ return Tokens.T_INSTANCEOF; }
					break;
					
				case 175:
					// #line 199
					{ return Tokens.T_IMPLEMENTS; }
					break;
					
				case 176:
					// #line 154
					{ return Tokens.T_ENDFOREACH; }
					break;
					
				case 177:
					// #line 226
					{ return (InLinq) ? Tokens.T_LINQ_DESCENDING : Tokens.T_STRING; }
					break;
					
				case 178:
					// #line 243
					{ return Tokens.T_TOSTRING; }
					break;
					
				case 179:
					// #line 250
					{ return Tokens.T_AUTOLOAD; }
					break;
					
				case 180:
					// #line 245
					{ return Tokens.T_DESTRUCT; }
					break;
					
				case 181:
					// #line 189
					{ return Tokens.T_METHOD_C; }
					break;
					
				case 182:
					// #line 244
					{ return Tokens.T_CONSTRUCT; }
					break;
					
				case 183:
					// #line 174
					{ return Tokens.T_REQUIRE_ONCE; }
					break;
					
				case 184:
					// #line 172
					{ return Tokens.T_INCLUDE_ONCE; }
					break;
					
				case 185:
					// #line 242
					{ return Tokens.T_CALLSTATIC; }
					break;
					
				case 186:
					// #line 188
					{ return Tokens.T_FUNC_C; }
					break;
					
				case 187:
					// #line 208
					{ return Tokens.T_NAMESPACE_C; }
					break;
					
				case 188:
					// #line 295
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_FILE; }
					break;
					
				case 189:
					// #line 294
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_LINE; }
					break;
					
				case 190:
					// #line 296
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_DEFAULT_LINE; }
					break;
					
				case 191:
					// #line 297
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_DEFAULT_FILE; }
					break;
					
				case 192:
					// #line 504
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 193:
					// #line 496
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 194:
					// #line 487
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 195:
					// #line 497
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_DOUBLE_QUOTES; }
					break;
					
				case 196:
					// #line 486
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 197:
					// #line 503
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 198:
					// #line 505
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 199:
					// #line 501
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 200:
					// #line 500
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 201:
					// #line 498
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 202:
					// #line 499
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 203:
					// #line 495
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 204:
					// #line 491
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 205:
					// #line 493
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 206:
					// #line 490
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 207:
					// #line 492
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 208:
					// #line 488
					{ return Tokens.OctalCharCode; }
					break;
					
				case 209:
					// #line 494
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 210:
					// #line 502
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 211:
					// #line 489
					{ return Tokens.HexCharCode; }
					break;
					
				case 212:
					// #line 446
					{ yymore(); break; }
					break;
					
				case 213:
					// #line 447
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.SingleQuotedString; }
					break;
					
				case 214:
					// #line 527
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 215:
					// #line 520
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_BACKQUOTE; }
					break;
					
				case 216:
					// #line 510
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 217:
					// #line 519
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 218:
					// #line 509
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 219:
					// #line 525
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 220:
					// #line 528
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 221:
					// #line 524
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 222:
					// #line 523
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 223:
					// #line 521
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 224:
					// #line 522
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 225:
					// #line 518
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 226:
					// #line 515
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 227:
					// #line 514
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 228:
					// #line 516
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 229:
					// #line 513
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 230:
					// #line 511
					{ return Tokens.OctalCharCode; }
					break;
					
				case 231:
					// #line 517
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 232:
					// #line 526
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 233:
					// #line 512
					{ return Tokens.HexCharCode; }
					break;
					
				case 234:
					// #line 482
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 235:
					// #line 475
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 236:
					// #line 467
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 237:
					// #line 466
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 238:
					// #line 480
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 239:
					// #line 483
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 240:
					// #line 479
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 241:
					// #line 478
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 242:
					// #line 476
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 243:
					// #line 477
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 244:
					// #line 474
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 245:
					// #line 471
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 246:
					// #line 472
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 247:
					// #line 470
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 248:
					// #line 468
					{ return Tokens.OctalCharCode; }
					break;
					
				case 249:
					// #line 473
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 250:
					// #line 481
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 251:
					// #line 469
					{ return Tokens.HexCharCode; }
					break;
					
				case 252:
					// #line 451
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
					
				case 253:
					// #line 391
					{
						yyless(0);
						if (!yy_pop_state()) return Tokens.ERROR;
						break;
					}
					break;
					
				case 254:
					// #line 384
					{
						if (!yy_pop_state()) return Tokens.ERROR;
						inString = (CurrentLexicalState != LexicalStates.ST_IN_SCRIPTING); 
						isCode = true;
						return Tokens.T_STRING;
					}
					break;
					
				case 255:
					// #line 405
					{
						yyless(0);
						if (!yy_pop_state()) return Tokens.ERROR;
						yy_push_state(LexicalStates.ST_IN_SCRIPTING);
						break;
					}
					break;
					
				case 256:
					// #line 399
					{
						if (!yy_pop_state()) return Tokens.ERROR;
						yy_push_state(LexicalStates.ST_IN_SCRIPTING);
						return Tokens.T_STRING_VARNAME;
					}
					break;
					
				case 257:
					// #line 440
					{ yymore(); break; }
					break;
					
				case 258:
					// #line 442
					{ yymore(); break; }
					break;
					
				case 259:
					// #line 441
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_DOC_COMMENT; }
					break;
					
				case 260:
					// #line 434
					{ yymore(); break; }
					break;
					
				case 261:
					// #line 436
					{ yymore(); break; }
					break;
					
				case 262:
					// #line 435
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_COMMENT; }
					break;
					
				case 263:
					// #line 414
					{ yymore(); break; }
					break;
					
				case 264:
					// #line 415
					{ yymore(); break; }
					break;
					
				case 265:
					// #line 416
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_LINE_COMMENT; }
					break;
					
				case 266:
					// #line 418
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
					
				case 269: goto case 2;
				case 270: goto case 4;
				case 271: goto case 5;
				case 272: goto case 7;
				case 273: goto case 8;
				case 274: goto case 10;
				case 275: goto case 14;
				case 276: goto case 21;
				case 277: goto case 24;
				case 278: goto case 26;
				case 279: goto case 89;
				case 280: goto case 189;
				case 281: goto case 192;
				case 282: goto case 196;
				case 283: goto case 197;
				case 284: goto case 198;
				case 285: goto case 203;
				case 286: goto case 204;
				case 287: goto case 206;
				case 288: goto case 208;
				case 289: goto case 211;
				case 290: goto case 214;
				case 291: goto case 218;
				case 292: goto case 219;
				case 293: goto case 220;
				case 294: goto case 225;
				case 295: goto case 227;
				case 296: goto case 229;
				case 297: goto case 230;
				case 298: goto case 233;
				case 299: goto case 234;
				case 300: goto case 235;
				case 301: goto case 237;
				case 302: goto case 238;
				case 303: goto case 239;
				case 304: goto case 244;
				case 305: goto case 245;
				case 306: goto case 247;
				case 307: goto case 248;
				case 308: goto case 251;
				case 309: goto case 252;
				case 310: goto case 263;
				case 311: goto case 265;
				case 313: goto case 8;
				case 314: goto case 10;
				case 315: goto case 21;
				case 316: goto case 26;
				case 317: goto case 196;
				case 318: goto case 197;
				case 319: goto case 218;
				case 320: goto case 219;
				case 321: goto case 237;
				case 322: goto case 238;
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
				case 360: goto case 8;
				case 361: goto case 10;
				case 363: goto case 8;
				case 364: goto case 10;
				case 366: goto case 8;
				case 367: goto case 10;
				case 369: goto case 10;
				case 371: goto case 10;
				case 373: goto case 10;
				case 375: goto case 10;
				case 377: goto case 10;
				case 379: goto case 10;
				case 381: goto case 10;
				case 383: goto case 10;
				case 385: goto case 10;
				case 387: goto case 10;
				case 389: goto case 10;
				case 391: goto case 10;
				case 393: goto case 10;
				case 395: goto case 10;
				case 397: goto case 10;
				case 399: goto case 10;
				case 401: goto case 10;
				case 403: goto case 10;
				case 405: goto case 10;
				case 407: goto case 10;
				case 409: goto case 10;
				case 411: goto case 10;
				case 413: goto case 10;
				case 415: goto case 10;
				case 417: goto case 10;
				case 419: goto case 10;
				case 421: goto case 10;
				case 423: goto case 10;
				case 425: goto case 10;
				case 427: goto case 10;
				case 429: goto case 10;
				case 431: goto case 10;
				case 433: goto case 10;
				case 435: goto case 10;
				case 437: goto case 10;
				case 439: goto case 10;
				case 441: goto case 10;
				case 443: goto case 10;
				case 445: goto case 10;
				case 447: goto case 10;
				case 449: goto case 10;
				case 451: goto case 10;
				case 453: goto case 10;
				case 455: goto case 10;
				case 457: goto case 10;
				case 459: goto case 10;
				case 461: goto case 10;
				case 463: goto case 10;
				case 465: goto case 10;
				case 467: goto case 10;
				case 469: goto case 10;
				case 471: goto case 10;
				case 473: goto case 10;
				case 475: goto case 10;
				case 477: goto case 10;
				case 479: goto case 10;
				case 481: goto case 10;
				case 483: goto case 10;
				case 485: goto case 10;
				case 487: goto case 10;
				case 489: goto case 10;
				case 491: goto case 10;
				case 493: goto case 10;
				case 495: goto case 10;
				case 497: goto case 10;
				case 499: goto case 10;
				case 501: goto case 10;
				case 503: goto case 10;
				case 505: goto case 10;
				case 507: goto case 10;
				case 509: goto case 10;
				case 511: goto case 10;
				case 513: goto case 10;
				case 515: goto case 10;
				case 517: goto case 10;
				case 519: goto case 10;
				case 521: goto case 10;
				case 523: goto case 10;
				case 525: goto case 10;
				case 527: goto case 10;
				case 529: goto case 10;
				case 531: goto case 10;
				case 533: goto case 10;
				case 535: goto case 10;
				case 537: goto case 10;
				case 539: goto case 10;
				case 541: goto case 10;
				case 543: goto case 10;
				case 545: goto case 10;
				case 604: goto case 5;
				case 605: goto case 10;
				case 606: goto case 206;
				case 607: goto case 208;
				case 608: goto case 229;
				case 609: goto case 230;
				case 610: goto case 247;
				case 611: goto case 248;
				case 632: goto case 10;
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
				case 801: goto case 10;
				case 802: goto case 10;
				case 803: goto case 10;
				case 804: goto case 5;
				case 805: goto case 10;
				case 806: goto case 206;
				case 807: goto case 229;
				case 808: goto case 247;
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
				case 907: goto case 10;
				case 908: goto case 10;
				case 909: goto case 10;
				case 910: goto case 10;
				case 911: goto case 10;
				case 912: goto case 5;
				case 913: goto case 206;
				case 914: goto case 229;
				case 915: goto case 247;
				case 917: goto case 10;
				case 918: goto case 10;
				case 919: goto case 10;
				case 920: goto case 10;
				case 921: goto case 10;
				case 922: goto case 10;
				case 923: goto case 10;
				case 924: goto case 10;
				case 925: goto case 5;
				case 926: goto case 206;
				case 927: goto case 229;
				case 928: goto case 247;
				case 929: goto case 5;
				case 930: goto case 206;
				case 931: goto case 229;
				case 932: goto case 247;
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
			AcceptConditions.Accept, // 251
			AcceptConditions.AcceptOnStart, // 252
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
			AcceptConditions.Accept, // 266
			AcceptConditions.NotAccept, // 267
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
			AcceptConditions.Accept, // 308
			AcceptConditions.AcceptOnStart, // 309
			AcceptConditions.Accept, // 310
			AcceptConditions.Accept, // 311
			AcceptConditions.NotAccept, // 312
			AcceptConditions.Accept, // 313
			AcceptConditions.Accept, // 314
			AcceptConditions.Accept, // 315
			AcceptConditions.Accept, // 316
			AcceptConditions.Accept, // 317
			AcceptConditions.Accept, // 318
			AcceptConditions.Accept, // 319
			AcceptConditions.Accept, // 320
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
			AcceptConditions.Accept, // 537
			AcceptConditions.NotAccept, // 538
			AcceptConditions.Accept, // 539
			AcceptConditions.NotAccept, // 540
			AcceptConditions.Accept, // 541
			AcceptConditions.NotAccept, // 542
			AcceptConditions.Accept, // 543
			AcceptConditions.NotAccept, // 544
			AcceptConditions.Accept, // 545
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
			AcceptConditions.NotAccept, // 603
			AcceptConditions.Accept, // 604
			AcceptConditions.Accept, // 605
			AcceptConditions.Accept, // 606
			AcceptConditions.Accept, // 607
			AcceptConditions.Accept, // 608
			AcceptConditions.Accept, // 609
			AcceptConditions.Accept, // 610
			AcceptConditions.Accept, // 611
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
			AcceptConditions.NotAccept, // 631
			AcceptConditions.Accept, // 632
			AcceptConditions.NotAccept, // 633
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
			AcceptConditions.Accept, // 806
			AcceptConditions.Accept, // 807
			AcceptConditions.Accept, // 808
			AcceptConditions.NotAccept, // 809
			AcceptConditions.NotAccept, // 810
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
			AcceptConditions.Accept, // 911
			AcceptConditions.Accept, // 912
			AcceptConditions.Accept, // 913
			AcceptConditions.Accept, // 914
			AcceptConditions.Accept, // 915
			AcceptConditions.NotAccept, // 916
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
			AcceptConditions.Accept, // 928
			AcceptConditions.Accept, // 929
			AcceptConditions.Accept, // 930
			AcceptConditions.Accept, // 931
			AcceptConditions.Accept, // 932
		};
		
		private static int[] colMap = new int[]
		{
			64, 64, 64, 64, 64, 64, 64, 64, 64, 23, 11, 64, 64, 24, 64, 64, 
			64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 
			69, 44, 20, 57, 50, 1, 47, 21, 58, 60, 45, 42, 53, 43, 46, 25, 
			55, 56, 62, 61, 39, 68, 38, 68, 59, 52, 41, 66, 2, 18, 22, 4, 
			53, 13, 32, 6, 26, 17, 28, 15, 19, 8, 40, 33, 12, 37, 14, 29, 
			9, 36, 7, 5, 10, 16, 34, 31, 27, 30, 51, 70, 54, 70, 49, 35, 
			3, 13, 32, 6, 26, 17, 28, 15, 19, 8, 40, 33, 12, 37, 14, 29, 
			9, 36, 7, 5, 10, 16, 34, 31, 27, 30, 51, 63, 48, 65, 53, 64, 
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
			17, 17, 17, 17, 17, 17, 17, 17, 33, 17, 17, 17, 17, 17, 17, 17, 
			1, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 34, 17, 17, 35, 
			1, 1, 1, 1, 36, 37, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 
			1, 1, 1, 1, 1, 1, 17, 17, 17, 17, 17, 17, 17, 17, 17, 1, 
			1, 1, 1, 1, 17, 17, 17, 17, 17, 17, 17, 17, 17, 1, 17, 17, 
			17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 38, 39, 40, 41, 
			42, 43, 44, 1, 45, 46, 47, 42, 1, 48, 1, 1, 49, 1, 50, 1, 
			51, 1, 1, 52, 53, 1, 54, 1, 55, 56, 57, 58, 59, 54, 1, 60, 
			1, 1, 1, 61, 1, 62, 63, 1, 1, 64, 65, 66, 67, 68, 69, 70, 
			65, 1, 71, 1, 1, 72, 1, 73, 74, 1, 1, 75, 1, 1, 76, 1, 
			77, 78, 79, 1, 80, 81, 1, 82, 83, 1, 1, 84, 85, 86, 1, 87, 
			88, 89, 90, 91, 1, 92, 1, 93, 94, 95, 96, 97, 1, 98, 1, 1, 
			1, 1, 99, 100, 101, 1, 102, 1, 1, 1, 1, 103, 104, 105, 106, 1, 
			107, 1, 1, 1, 1, 108, 1, 109, 110, 111, 112, 113, 114, 115, 1, 116, 
			1, 117, 1, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 
			131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 
			147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 1, 158, 159, 160, 161, 
			162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 9, 176, 
			177, 178, 10, 179, 180, 181, 182, 183, 184, 185, 29, 186, 30, 187, 188, 189, 
			190, 191, 192, 193, 194, 195, 196, 197, 160, 198, 199, 200, 201, 202, 203, 204, 
			205, 206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 216, 32, 217, 218, 219, 
			28, 220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 
			235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 
			251, 252, 253, 254, 255, 256, 257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 
			267, 268, 269, 270, 271, 272, 273, 274, 275, 276, 277, 278, 279, 280, 281, 282, 
			283, 284, 285, 286, 287, 288, 289, 290, 291, 292, 293, 294, 295, 296, 297, 298, 
			299, 300, 301, 302, 303, 304, 305, 306, 307, 308, 309, 310, 311, 312, 313, 314, 
			315, 316, 317, 318, 319, 320, 321, 322, 323, 324, 325, 326, 327, 328, 329, 330, 
			331, 332, 333, 334, 335, 336, 337, 338, 339, 340, 341, 342, 343, 344, 345, 346, 
			347, 348, 349, 350, 351, 352, 353, 354, 355, 356, 38, 357, 358, 359, 360, 361, 
			362, 363, 364, 365, 366, 367, 368, 115, 369, 370, 371, 372, 373, 116, 374, 375, 
			376, 117, 377, 378, 379, 380, 381, 382, 383, 384, 385, 386, 387, 388, 389, 390, 
			391, 392, 393, 394, 395, 396, 213, 397, 398, 399, 400, 401, 402, 403, 404, 405, 
			406, 407, 408, 409, 410, 411, 412, 413, 414, 415, 416, 417, 418, 419, 420, 421, 
			422, 423, 424, 425, 426, 427, 428, 429, 430, 431, 432, 433, 434, 435, 436, 437, 
			438, 439, 440, 441, 442, 443, 444, 445, 446, 447, 448, 449, 450, 451, 452, 453, 
			454, 455, 456, 457, 458, 459, 460, 461, 462, 463, 464, 465, 466, 467, 468, 469, 
			470, 471, 472, 473, 474, 475, 476, 477, 478, 479, 480, 481, 482, 483, 484, 485, 
			486, 487, 488, 489, 490, 491, 492, 493, 494, 495, 496, 497, 498, 499, 500, 501, 
			502, 503, 504, 505, 506, 507, 508, 509, 510, 511, 512, 513, 514, 515, 516, 517, 
			518, 519, 520, 521, 522, 523, 524, 525, 526, 527, 528, 529, 530, 531, 532, 533, 
			534, 535, 536, 537, 538, 539, 540, 541, 542, 543, 544, 545, 546, 547, 548, 549, 
			550, 551, 552, 553, 554, 555, 556, 557, 558, 559, 560, 561, 562, 563, 564, 565, 
			566, 567, 568, 569, 570, 571, 572, 573, 574, 575, 576, 577, 578, 579, 580, 581, 
			582, 583, 584, 585, 586, 587, 588, 589, 590, 591, 592, 593, 594, 595, 596, 597, 
			598, 599, 600, 601, 602, 603, 604, 605, 606, 607, 608, 609, 610, 611, 612, 613, 
			614, 615, 616, 617, 618, 619, 620, 621, 622, 623, 624, 625, 626, 627, 628, 629, 
			630, 631, 632, 633, 634, 635, 636, 637, 638, 639, 640, 641, 642, 643, 644, 645, 
			646, 647, 648, 649, 650, 651, 652, 653, 654, 655, 656, 657, 658, 659, 660, 661, 
			662, 663, 664, 665, 666, 667, 668, 669, 670, 671, 672, 673, 674, 675, 676, 677, 
			678, 679, 680, 681, 558, 682, 683, 684, 685, 686, 17, 687, 688, 689, 690, 691, 
			692, 693, 694, 695, 696, 697, 698, 699, 700, 701, 702, 703, 704, 705, 706, 707, 
			708, 709, 710, 711, 712
		};
		
		private static int[,] nextState = new int[,]
		{
			{ 1, 2, 269, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 2, 267, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 929, -1, 929, 929, 929, 925, 929, 929, 929, 929, -1, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, -1, -1, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, -1, 929, -1, 929 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 20, -1, -1, -1, 21, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 911, -1, 906, 906, 906, 906, 906, 638, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 639, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 11, -1 },
			{ -1, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 32, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, -1, 382, 382, 382, 384, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, -1, 382, 382, 382 },
			{ -1, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 33, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 388, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, -1, 386, 386, 386 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 392, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 14, 14, -1, -1, -1, -1, -1, -1, 42, -1, -1, -1, -1, -1, 14, -1, -1, 14, 14, -1, -1, 14, -1, 14, 14, -1, -1, -1, -1, -1, 14, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 398, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 276, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 315, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 418, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 58, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 278, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 876, 854, 906, 906, 906, 59, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 855, 875, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 65, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 66, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 428, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 821, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 904, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 392, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 42, 42, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 42, -1, -1, 42, 42, -1, -1, 42, -1, 42, 42, -1, -1, -1, -1, -1, 42, -1, -1 },
			{ -1, -1, -1, -1, -1, 57, 57, 57, 57, 57, 57, -1, 57, 57, 57, 57, 57, 57, -1, 57, -1, -1, -1, -1, -1, -1, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 57, 57, -1, -1, 57, 57, -1, -1, 57, -1, 57, 57, -1, -1, -1, -1, -1, 57, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 709, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 395, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 722, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, 71, 71, -1, -1, 71, -1, 71, 71, -1, -1, -1, -1, -1, 71, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 73, -1, -1, -1, -1, -1, -1, 73, -1, -1, -1, 73, -1, -1, -1, -1, -1, -1, -1, -1, 73, -1, 73, -1, -1, -1, 73, -1, -1, -1, -1, -1, 73, 73, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, -1, -1, 73, 73, -1, -1, 73, -1, 73, 73, -1, -1, -1, -1, -1, 73, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 74, 74, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 449, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 86, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 86, 86, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 86, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 473, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 777, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 843, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 784, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 920, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, -1, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, 188, -1, 188, 188, 188 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 189, 189, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 189, -1, -1, 189, 189, -1, -1, 189, -1, 189, 189, -1, -1, -1, -1, -1, 189, 280, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 190, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 190, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 191, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 191, -1 },
			{ -1, 192, 192, -1, 192, -1, -1, -1, -1, -1, -1, 192, -1, -1, -1, -1, -1, -1, 192, -1, -1, 192, 192, 192, 192, 192, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 192, 192, 192, 192, 192, 192, 192, 192, 192, -1, -1, -1, 192, -1, -1, -1, 192, 192, -1, 192, -1, -1, -1, -1, -1, 192, -1, -1, 192, -1 },
			{ -1, -1, -1, 193, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 194, 194, 194, 194, 194, 194, -1, 194, 194, 194, 194, 194, 194, -1, 194, -1, -1, -1, -1, -1, -1, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 194, 194, -1, -1, 194, 194, -1, -1, 194, -1, 194, 194, -1, -1, -1, -1, -1, 194, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 196, 196, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 196, -1, -1, 196, 196, -1, -1, 196, -1, 196, 196, -1, -1, -1, -1, -1, 196, -1, -1 },
			{ -1, 200, 200, 200, 200, 201, 201, 201, 201, 201, 201, 200, 201, 201, 201, 201, 201, 201, 200, 201, 200, 200, 200, 200, 200, 200, 201, 201, 201, 201, 201, 201, 201, 201, 201, 201, 201, 201, 200, 200, 201, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 201, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 202, 200, 200, 200, -1, 200, 200, 200 },
			{ -1, 203, 203, 203, 203, 203, 204, 205, 203, 203, 205, 203, 203, 203, 205, 203, 206, 203, 203, 203, 207, 203, 203, 203, 203, 203, 203, 285, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 208, 208, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 205, 203, 203, 203, 205, 208, 208, 203, 203, 203, 203, 208, 208, 209, 203, 203, 203, -1, 208, 203, 203 },
			{ -1, -1, -1, -1, -1, 201, 201, 201, 201, 201, 201, -1, 201, 201, 201, 201, 201, 201, -1, 201, -1, -1, -1, -1, -1, -1, 201, 201, 201, 201, 201, 201, 201, 201, 201, 201, 201, 201, 201, 201, 201, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 201, 201, -1, -1, 201, 201, -1, -1, 201, -1, 201, 201, -1, -1, -1, -1, -1, 201, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 584, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 930, -1, -1, -1, -1, -1, -1, 930, -1, -1, -1, 930, -1, -1, -1, -1, -1, -1, -1, -1, 930, -1, 930, -1, -1, -1, 930, -1, -1, -1, -1, -1, 930, 930, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 930, -1, -1, 930, 930, -1, -1, 930, -1, 930, 930, -1, -1, -1, -1, -1, 930, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 607, 607, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 607, 607, -1, -1, -1, -1, 607, 607, -1, -1, -1, -1, -1, 607, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 289, -1, -1, -1, -1, -1, -1, 289, -1, -1, -1, 289, -1, -1, -1, -1, -1, -1, -1, -1, 289, -1, 289, -1, -1, -1, 289, -1, -1, -1, -1, -1, 289, 289, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 289, -1, -1, 289, 289, -1, -1, 289, -1, 289, 289, -1, -1, -1, -1, -1, 289, -1, -1 },
			{ -1, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, -1, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 587, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, -1, 212, 212, 212 },
			{ -1, 214, 214, -1, 214, -1, -1, -1, -1, -1, -1, 214, -1, -1, -1, -1, -1, -1, 214, -1, -1, 214, 214, 214, 214, 214, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 214, 214, 214, 214, 214, 214, 214, 214, 214, -1, -1, -1, 214, -1, -1, -1, 214, 214, -1, 214, -1, -1, -1, -1, -1, 214, -1, -1, 214, -1 },
			{ -1, -1, -1, -1, -1, 216, 216, 216, 216, 216, 216, -1, 216, 216, 216, 216, 216, 216, -1, 216, -1, -1, -1, -1, -1, -1, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 216, 216, -1, -1, 216, 216, -1, -1, 216, -1, 216, 216, -1, -1, -1, -1, -1, 216, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 217, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 218, 218, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 218, -1, -1, 218, 218, -1, -1, 218, -1, 218, 218, -1, -1, -1, -1, -1, 218, -1, -1 },
			{ -1, 222, 222, 222, 222, 223, 223, 223, 223, 223, 223, 222, 223, 223, 223, 223, 223, 223, 222, 223, 222, 222, 222, 222, 222, 222, 223, 223, 223, 223, 223, 223, 223, 223, 223, 223, 223, 223, 222, 222, 223, 222, 222, 222, 222, 222, 222, 222, 222, 222, 222, 223, 222, 222, 222, 222, 222, 222, 222, 222, 222, 222, 222, 224, 222, 222, 222, -1, 222, 222, 222 },
			{ -1, 225, 225, 226, 225, 225, 227, 228, 225, 225, 228, 225, 225, 225, 228, 225, 229, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 294, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 230, 230, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 228, 225, 225, 225, 228, 230, 230, 225, 225, 225, 225, 230, 230, 231, 225, 225, 225, -1, 230, 225, 225 },
			{ -1, -1, -1, -1, -1, 223, 223, 223, 223, 223, 223, -1, 223, 223, 223, 223, 223, 223, -1, 223, -1, -1, -1, -1, -1, -1, 223, 223, 223, 223, 223, 223, 223, 223, 223, 223, 223, 223, 223, 223, 223, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 223, 223, -1, -1, 223, 223, -1, -1, 223, -1, 223, 223, -1, -1, -1, -1, -1, 223, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 590, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 931, -1, -1, -1, -1, -1, -1, 931, -1, -1, -1, 931, -1, -1, -1, -1, -1, -1, -1, -1, 931, -1, 931, -1, -1, -1, 931, -1, -1, -1, -1, -1, 931, 931, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 931, -1, -1, 931, 931, -1, -1, 931, -1, 931, 931, -1, -1, -1, -1, -1, 931, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 609, 609, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 609, 609, -1, -1, -1, -1, 609, 609, -1, -1, -1, -1, -1, 609, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 298, -1, -1, -1, -1, -1, -1, 298, -1, -1, -1, 298, -1, -1, -1, -1, -1, -1, -1, -1, 298, -1, 298, -1, -1, -1, 298, -1, -1, -1, -1, -1, 298, 298, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 298, -1, -1, 298, 298, -1, -1, 298, -1, 298, 298, -1, -1, -1, -1, -1, 298, -1, -1 },
			{ -1, 234, 234, -1, 234, -1, -1, -1, -1, -1, -1, 234, -1, -1, -1, -1, -1, -1, 234, -1, -1, 234, 234, 234, 234, 234, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 234, 234, 234, 234, 234, 234, 234, 234, 234, -1, -1, -1, 234, -1, -1, -1, 234, 234, -1, 234, -1, -1, -1, -1, -1, 234, -1, -1, 234, -1 },
			{ -1, -1, -1, 235, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 235, 235, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 236, 236, 236, 236, 236, 236, -1, 236, 236, 236, 236, 236, 236, -1, 236, -1, -1, -1, -1, -1, -1, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 236, 236, -1, -1, 236, 236, -1, -1, 236, -1, 236, 236, -1, -1, -1, -1, -1, 236, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 237, 237, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 237, -1, -1, 237, 237, -1, -1, 237, -1, 237, 237, -1, -1, -1, -1, -1, 237, -1, -1 },
			{ -1, 241, 241, 241, 241, 242, 242, 242, 242, 242, 242, 241, 242, 242, 242, 242, 242, 242, 241, 242, 241, 241, 241, 241, 241, 241, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 241, 241, 242, 241, 241, 241, 241, 241, 241, 241, 241, 241, 241, 242, 241, 241, 241, 241, 241, 241, 241, 241, 241, 241, 241, 243, 241, 241, 241, -1, 241, 241, 241 },
			{ -1, 244, 244, 244, 244, 244, 245, 246, 244, 244, 246, 244, 244, 244, 246, 244, 247, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 304, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 248, 248, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 246, 244, 244, 244, 246, 248, 248, 244, 244, 244, 244, 248, 248, 249, 244, 244, 244, -1, 248, 244, 244 },
			{ -1, -1, -1, -1, -1, 242, 242, 242, 242, 242, 242, -1, 242, 242, 242, 242, 242, 242, -1, 242, -1, -1, -1, -1, -1, -1, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 242, 242, -1, -1, 242, 242, -1, -1, 242, -1, 242, 242, -1, -1, -1, -1, -1, 242, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 595, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 932, -1, -1, -1, -1, -1, -1, 932, -1, -1, -1, 932, -1, -1, -1, -1, -1, -1, -1, -1, 932, -1, 932, -1, -1, -1, 932, -1, -1, -1, -1, -1, 932, 932, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 932, -1, -1, 932, 932, -1, -1, 932, -1, 932, 932, -1, -1, -1, -1, -1, 932, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 611, 611, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 611, 611, -1, -1, -1, -1, 611, 611, -1, -1, -1, -1, -1, 611, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 308, -1, -1, -1, -1, -1, -1, 308, -1, -1, -1, 308, -1, -1, -1, -1, -1, -1, -1, -1, 308, -1, 308, -1, -1, -1, 308, -1, -1, -1, -1, -1, 308, 308, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 308, -1, -1, 308, 308, -1, -1, 308, -1, 308, 308, -1, -1, -1, -1, -1, 308, -1, -1 },
			{ -1, -1, -1, -1, -1, 254, 254, 254, 254, 254, 254, -1, 254, 254, 254, 254, 254, 254, -1, 254, -1, -1, -1, -1, -1, -1, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 254, 254, -1, -1, 254, 254, -1, -1, 254, -1, 254, 254, -1, -1, -1, -1, -1, 254, -1, -1 },
			{ -1, -1, -1, -1, -1, 256, 256, 256, 256, 256, 256, -1, 256, 256, 256, 256, 256, 256, -1, 256, -1, -1, -1, -1, -1, -1, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 256, 256, -1, -1, 256, 256, -1, -1, 256, -1, 256, 256, -1, -1, -1, -1, -1, 256, -1, -1 },
			{ -1, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, -1, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, -1, 257, 257, 257 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 259, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, -1, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, -1, 260, 260, 260 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 262, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 266, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 264, 264, -1, 264, 264, 264, 264, 264, 264, -1, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, -1, 264, -1, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, -1, 264, 264, 264 },
			{ -1, -1, -1, 2, -1, -1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, 594, 594, 594, 594, 594, 594, -1, 594, 594, 594, 594, 594, 594, -1, 594, -1, -1, -1, -1, -1, -1, 594, 594, 594, 594, 594, 594, 594, 594, 594, 594, 594, 594, -1, -1, 594, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 594, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 3, -1, 2, 4, 5, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, 929, -1, 929, 929, 929, 929, 929, 929, 929, 929, 329, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 329, 329, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, -1, 929, 329, 929 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 22, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 23, -1, -1, -1, 24, -1, -1, 378, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 25, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 644, 906, 906, 906, 906, 906, -1, 906, 906, 27, 906, 906, 906, -1, 906, -1, 380, -1, -1, -1, -1, 906, 906, 28, 906, 906, 906, 906, 906, 906, 906, 906, 645, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 392, -1, -1, -1, -1, -1, -1, -1, -1, -1, 394, -1, -1, -1, -1, 396, -1, -1, -1, -1, -1, 14, 14, -1, -1, -1, -1, -1, -1, 42, -1, -1, -1, -1, -1, 14, -1, -1, 14, 14, -1, -1, 14, -1, 14, 14, -1, -1, -1, -1, -1, 14, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 72, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 89, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1 },
			{ -1, 192, 192, -1, 192, -1, -1, -1, -1, -1, -1, 192, -1, -1, -1, -1, -1, -1, 192, -1, -1, 192, 199, 192, 192, 192, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 192, 192, 192, 192, 192, 192, 192, 192, 192, -1, -1, -1, 192, -1, -1, -1, 192, 192, -1, 192, -1, -1, -1, -1, -1, 192, -1, -1, 192, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 583, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 196, 196, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 196, -1, -1, 196, 196, -1, -1, 196, -1, 196, 196, -1, -1, -1, -1, -1, 196, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 210, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 211, -1, -1, -1, -1, -1, -1, 211, -1, -1, -1, 211, -1, -1, -1, -1, -1, -1, -1, -1, 211, -1, 211, -1, -1, -1, 211, -1, -1, -1, -1, -1, 211, 211, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 211, -1, -1, 211, 211, -1, -1, 211, -1, 211, 211, -1, -1, -1, -1, -1, 211, -1, -1 },
			{ -1, 214, 214, -1, 214, -1, -1, -1, -1, -1, -1, 214, -1, -1, -1, -1, -1, -1, 214, -1, -1, 214, 221, 214, 214, 214, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 214, 214, 214, 214, 214, 214, 214, 214, 214, -1, -1, -1, 214, -1, -1, -1, 214, 214, -1, 214, -1, -1, -1, -1, -1, 214, -1, -1, 214, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 589, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 218, 218, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 218, -1, -1, 218, 218, -1, -1, 218, -1, 218, 218, -1, -1, -1, -1, -1, 218, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 232, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 233, -1, -1, -1, -1, -1, -1, 233, -1, -1, -1, 233, -1, -1, -1, -1, -1, -1, -1, -1, 233, -1, 233, -1, -1, -1, 233, -1, -1, -1, -1, -1, 233, 233, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 233, -1, -1, 233, 233, -1, -1, 233, -1, 233, 233, -1, -1, -1, -1, -1, 233, -1, -1 },
			{ -1, 234, 234, -1, 234, -1, -1, -1, -1, -1, -1, 234, -1, -1, -1, -1, -1, -1, 234, -1, -1, 234, 240, 234, 234, 234, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 234, 234, 234, 234, 234, 234, 234, 234, 234, -1, -1, -1, 234, -1, -1, -1, 234, 234, -1, 234, -1, -1, -1, -1, -1, 234, -1, -1, 234, -1 },
			{ -1, 234, 234, 235, 234, -1, -1, -1, -1, -1, -1, 234, -1, -1, -1, -1, -1, -1, 234, -1, 235, 300, 234, 234, 234, 234, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 234, 234, 234, 234, 234, 234, 234, 234, 234, -1, -1, -1, 234, -1, -1, -1, 234, 234, -1, 234, -1, -1, -1, -1, -1, 234, -1, -1, 234, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 593, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 237, 237, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 237, -1, -1, 237, 237, -1, -1, 237, -1, 237, 237, -1, -1, -1, -1, -1, 237, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 250, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 251, -1, -1, -1, -1, -1, -1, 251, -1, -1, -1, 251, -1, -1, -1, -1, -1, -1, -1, -1, 251, -1, 251, -1, -1, -1, 251, -1, -1, -1, -1, -1, 251, 251, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 251, -1, -1, 251, 251, -1, -1, 251, -1, 251, 251, -1, -1, -1, -1, -1, 251, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 252, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 265, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 26, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 29, 906, 852, 906, 906, 906, -1, 906, 906, 337, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 813, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 276, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 278, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 317, -1, -1, -1, -1, -1, -1, 317, -1, -1, -1, 317, -1, -1, -1, -1, -1, -1, -1, -1, 317, -1, 317, -1, -1, -1, 317, -1, -1, -1, -1, -1, 317, 317, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 317, -1, -1, 317, 317, -1, -1, 317, -1, 317, 317, -1, -1, -1, -1, -1, 317, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 319, -1, -1, -1, -1, -1, -1, 319, -1, -1, -1, 319, -1, -1, -1, -1, -1, -1, -1, -1, 319, -1, 319, -1, -1, -1, 319, -1, -1, -1, -1, -1, 319, 319, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 319, -1, -1, 319, 319, -1, -1, 319, -1, 319, 319, -1, -1, -1, -1, -1, 319, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 321, -1, -1, -1, -1, -1, -1, 321, -1, -1, -1, 321, -1, -1, -1, -1, -1, -1, -1, -1, 321, -1, 321, -1, -1, -1, 321, -1, -1, -1, -1, -1, 321, 321, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 321, -1, -1, 321, 321, -1, -1, 321, -1, 321, 321, -1, -1, -1, -1, -1, 321, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 326, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 30, -1, -1, -1, 31, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 346, 906, 906, -1, 906, 906, 906, 906, 906, 659, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 39, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 7, 272, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 7, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 34, -1, -1, -1, 35, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 40, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 662, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 329, 332, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 329, 329, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 329, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 36, -1, -1, -1, -1, -1, -1, 37, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 38, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 390, -1, -1, 906, 906, 818, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, 12, 13, -1, -1, -1, -1, 906, 906, 906, 665, 41, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 335, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 43, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 44, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 677, 906, 906, 364, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 61, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 338, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 45, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 46, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 62, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 341, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 47, -1, -1, -1, 48, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 49, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 63, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 344, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 64, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 347, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 50, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 67, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 350, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 51, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 42, 42, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 42, -1, -1, 42, 42, -1, -1, 42, -1, 42, 42, -1, -1, -1, -1, -1, 42, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 68, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 353, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 52, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 53, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 69, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 353, -1, -1, -1, -1, -1, -1, 356, -1, -1, -1, -1, 353, 353, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 353, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 54, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 55, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 70, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 359, -1, 356, -1, -1, -1, -1, -1, -1, -1, -1, 362, 916, -1, 356, 356, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 356, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 703, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 75, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 612, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 57, 57, 57, 57, 57, 57, -1, 57, 57, 57, 57, 57, 57, -1, 57, -1, -1, -1, -1, -1, -1, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, -1, -1, 57, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 57, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 76, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 365, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 78, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 400, -1, 402, 404, -1, -1, -1, -1, 615, -1, -1, 406, -1, -1, -1, -1, -1, -1, 408, -1, -1, 410, -1, 412, 414, -1, -1, 416, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 79, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 368, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 270, 368, 368, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 368, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 80, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 372, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 81, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 368, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 82, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 368, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 83, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ 1, 8, 273, 9, 313, 10, 805, 849, 274, 873, 605, 11, 888, 314, 632, 897, 634, 903, 324, 906, 12, 13, 327, 11, 11, 330, 325, 635, 636, 328, 908, 909, 331, 906, 637, 910, 906, 906, 14, 14, 906, 333, 336, 339, 342, 345, 348, 351, 354, 357, 360, 906, 14, 363, 15, 275, 14, 16, 366, 14, 363, 14, 14, 17, 18, 19, 363, 1, 14, 11, 363 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 84, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 420, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 85, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, 422, 424, 424, 422, 422, 422, 422, 422, 422, 422, 424, 422, 422, 422, 422, 422, 422, 422, 422, 422, 60, 424, 422, 424, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 426, 422, 422, 424, 422, 422, 422, 422, 422, 422, 422, 422, 422, -1, 422, 422, 422 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 87, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 88, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, 382, -1, 382, 382, 382 },
			{ -1, -1, -1, -1, -1, 90, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 91, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, 386, -1, 386, 386, 386 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 92, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, 430, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 93, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, 71, -1, -1, 432, 432, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, 71, 71, -1, -1, 71, -1, 71, 71, -1, -1, -1, -1, -1, 71, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 94, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 95, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 96, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 434, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 97, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 436, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 98, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 616, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 99, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 438, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 100, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 440, -1, -1, -1, -1, -1, 442, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 101, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 102, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 444, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 103, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 446, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 104, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 448, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 105, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 618, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 619, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 106, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 450, 450, 450, 450, 450, 450, -1, 450, 450, 450, 450, 450, 450, -1, 450, 452, 620, -1, 418, -1, -1, 450, 450, 450, 450, 450, 450, 450, 450, 450, 450, 450, 450, -1, -1, 450, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 450, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 418, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 107, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 810, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 108, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, 422, 424, 424, 422, 422, 422, 422, 422, 422, 422, 424, 422, 422, 422, 422, 422, 422, 422, 422, 422, 77, 424, 422, 424, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 426, 422, 422, 424, 422, 422, 422, 422, 422, 422, 422, 422, 422, -1, 422, 422, 422 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 109, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 60, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 454, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, -1, 424, 424, 424 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 110, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, 422, 614, 614, 422, 422, 422, 422, 422, 422, 422, 614, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 614, 422, 456, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 422, 614, 422, 422, 422, 422, 422, 422, 422, 422, 422, -1, 422, 422, 422 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 111, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 113, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, 418, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 114, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 115, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 458, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 116, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 460, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 117, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 464, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 118, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 468, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 119, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 470, -1, -1, 472, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 120, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 474, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 121, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 623, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 122, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 624, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 123, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 450, 450, 450, 450, 450, 450, 89, 450, 450, 450, 450, 450, 450, -1, 450, -1, -1, -1, -1, 279, -1, 450, 450, 450, 450, 450, 450, 450, 450, 450, 450, 450, 450, 450, 450, 450, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 450, 450, -1, -1, 450, 450, -1, -1, 450, -1, 450, 450, -1, -1, -1, -1, -1, 450, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 124, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 476, 476, 476, 476, 476, 476, -1, 476, 476, 476, 476, 476, 476, -1, 476, -1, -1, -1, -1, -1, -1, 476, 476, 476, 476, 476, 476, 476, 476, 476, 476, 476, 476, -1, -1, 476, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 476, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 125, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 456, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, 614, -1, 614, 614, 614 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 126, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 614, 424, 424, 424, 424, 424, 424, 424, 424, 424, 60, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 454, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, 424, -1, 424, 424, 424 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 127, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 480, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 132, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 482, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 133, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 484, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 134, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 486, -1, -1, -1, -1, -1, 488, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 490, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 492, -1, -1, 494, 112, 496, -1, -1, -1, -1, -1, -1, -1, 488, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 135, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 498, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 136, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 500, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 137, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 502, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 138, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 504, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 139, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 506, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 140, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 476, 476, 476, 476, 476, 476, -1, 476, 476, 476, 476, 476, 476, -1, 476, 514, -1, -1, -1, -1, -1, 476, 476, 476, 476, 476, 476, 476, 476, 476, 476, 476, 476, 476, 476, 476, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 476, 476, -1, -1, 476, 476, -1, -1, 476, -1, 476, 476, -1, -1, -1, -1, -1, 476, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 141, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 478, 478, 478, 478, 478, 478, -1, 478, 478, 478, 478, 478, 478, -1, 478, -1, 514, -1, -1, -1, -1, 478, 478, 478, 478, 478, 478, 478, 478, 478, 478, 478, 478, 478, 478, 478, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 478, 478, -1, -1, 478, 478, -1, -1, 478, -1, 478, 478, -1, -1, -1, -1, -1, 478, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 142, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 518, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 143, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 625, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 150, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 484, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 128, -1, -1, -1, -1, -1, -1, -1, -1, 484, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 151, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 520, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 152, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 488, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 112, -1, -1, -1, -1, -1, -1, -1, -1, 488, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 153, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 522, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 154, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 524, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 155, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 494, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 129, -1, -1, -1, -1, -1, -1, -1, -1, 494, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 156, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 488, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 157, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 526, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 158, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 528, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 530, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 532, -1, -1, 534, 130, 536, -1, -1, -1, -1, -1, -1, -1, 528, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 164, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 538, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 165, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 540, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 166, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 542, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 167, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 546, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 168, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 547, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 169, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 627, -1, -1, -1, -1, -1, 548, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 131, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 170, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 89, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 171, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 628, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 172, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 549, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 174, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 551, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 175, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 522, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 144, -1, -1, -1, -1, -1, -1, -1, -1, 522, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 176, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 524, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 145, -1, -1, -1, -1, -1, -1, -1, -1, 524, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 177, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 526, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 146, -1, -1, -1, -1, -1, -1, -1, -1, 526, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 178, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 528, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 130, -1, -1, -1, -1, -1, -1, -1, -1, 528, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 179, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 552, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 180, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 553, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 181, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 534, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 147, -1, -1, -1, -1, -1, -1, -1, -1, 534, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 182, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 528, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 183, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 538, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 148, -1, -1, -1, -1, -1, -1, -1, -1, 538, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 184, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 554, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 185, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 484, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 186, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 544, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 149, -1, -1, -1, -1, -1, -1, -1, -1, 544, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 187, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 555, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 131, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 559, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 559, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 550, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 159, -1, -1, -1, -1, -1, -1, -1, -1, 550, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 488, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 552, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 160, -1, -1, -1, -1, -1, -1, -1, -1, 552, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 553, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 161, -1, -1, -1, -1, -1, -1, -1, -1, 553, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 560, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 555, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 162, -1, -1, -1, -1, -1, -1, -1, -1, 555, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 163, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 26, 558, 558, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 561, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 559, -1, -1, 562, -1, 629, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 559, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 560, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 173, -1, -1, -1, -1, -1, -1, -1, -1, 560, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 563, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 564, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 566, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 567, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 630, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 568, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 569, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 571, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 571, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 572, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 571, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 189, 189, -1, -1, -1, 573, -1, -1, -1, -1, -1, -1, -1, -1, 189, -1, -1, 189, 189, -1, -1, 189, -1, 189, 189, -1, -1, -1, -1, -1, 189, 571, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 574, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 189, 189, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 189, -1, -1, 189, 189, -1, -1, 189, -1, 189, 189, -1, -1, -1, -1, -1, 189, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 575, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 576, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 576, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 577, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 576, -1, -1, -1, -1, 631, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 576, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 578, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 580, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 581, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 190, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 191, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ 1, 192, 192, 193, 192, 194, 194, 194, 194, 194, 194, 192, 194, 194, 194, 194, 194, 194, 192, 194, 195, 192, 192, 192, 192, 192, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 196, 196, 194, 192, 192, 281, 192, 192, 192, 192, 192, 192, 197, 194, 196, 192, 198, 282, 196, 192, 192, 196, 192, 196, 196, 283, 284, 318, 192, 1, 196, 192, 318 },
			{ -1, -1, -1, -1, -1, 585, 585, 585, 585, 585, 585, -1, 585, 585, 585, 585, 585, 585, -1, 585, -1, -1, -1, -1, -1, -1, 585, 585, 585, 585, 585, 585, 585, 585, 585, -1, 585, 585, 585, 585, 585, -1, -1, 585, -1, -1, -1, -1, -1, -1, -1, 585, 585, -1, -1, 585, 585, -1, -1, 585, -1, 585, 585, -1, -1, -1, -1, -1, 585, 585, -1 },
			{ -1, -1, -1, -1, -1, 585, 585, 585, 585, 585, 585, -1, 585, 585, 585, 585, 585, 585, -1, 585, -1, -1, -1, -1, -1, -1, 585, 585, 585, 585, 585, 585, 585, 585, 585, -1, 585, 585, 585, 585, 585, -1, -1, 585, -1, -1, -1, -1, -1, -1, -1, 585, 585, -1, -1, 585, 585, -1, -1, 585, -1, 585, 585, -1, -1, 286, -1, -1, 585, 585, -1 },
			{ 1, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 213, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 587, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 1, 212, 212, 212 },
			{ -1, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, 212, -1, 212, 212, 212 },
			{ 1, 214, 214, 215, 214, 216, 216, 216, 216, 216, 216, 214, 216, 216, 216, 216, 216, 216, 214, 216, 217, 214, 214, 214, 214, 214, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 218, 218, 216, 214, 214, 290, 214, 214, 214, 214, 214, 214, 219, 216, 218, 214, 220, 291, 218, 214, 214, 218, 214, 218, 218, 292, 293, 320, 214, 1, 218, 214, 320 },
			{ -1, -1, -1, -1, -1, 591, 591, 591, 591, 591, 591, -1, 591, 591, 591, 591, 591, 591, -1, 591, -1, -1, -1, -1, -1, -1, 591, 591, 591, 591, 591, 591, 591, 591, 591, -1, 591, 591, 591, 591, 591, -1, -1, 591, -1, -1, -1, -1, -1, -1, -1, 591, 591, -1, -1, 591, 591, -1, -1, 591, -1, 591, 591, -1, -1, -1, -1, -1, 591, 591, -1 },
			{ -1, -1, -1, -1, -1, 591, 591, 591, 591, 591, 591, -1, 591, 591, 591, 591, 591, 591, -1, 591, -1, -1, -1, -1, -1, -1, 591, 591, 591, 591, 591, 591, 591, 591, 591, -1, 591, 591, 591, 591, 591, -1, -1, 591, -1, -1, -1, -1, -1, -1, -1, 591, 591, -1, -1, 591, 591, -1, -1, 591, -1, 591, 591, -1, -1, 295, -1, -1, 591, 591, -1 },
			{ 1, 234, 234, 235, 234, 236, 236, 236, 236, 236, 236, 234, 236, 236, 236, 236, 236, 236, 234, 236, 235, 300, 234, 234, 234, 234, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 237, 237, 236, 234, 234, 299, 234, 234, 234, 234, 234, 234, 238, 236, 237, 234, 239, 301, 237, 234, 234, 237, 234, 237, 237, 302, 303, 322, 234, 268, 237, 234, 322 },
			{ -1, -1, -1, -1, -1, 594, 594, 594, 594, 594, 594, 252, 594, 594, 594, 594, 594, 594, -1, 594, -1, -1, -1, -1, 309, -1, 594, 594, 594, 594, 594, 594, 594, 594, 594, 594, 594, 594, 594, 594, 594, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 594, 594, -1, -1, 594, 594, -1, -1, 594, -1, 594, 594, -1, -1, -1, 596, -1, 594, -1, -1 },
			{ -1, -1, -1, -1, -1, 597, 597, 597, 597, 597, 597, -1, 597, 597, 597, 597, 597, 597, -1, 597, -1, -1, -1, -1, -1, -1, 597, 597, 597, 597, 597, 597, 597, 597, 597, -1, 597, 597, 597, 597, 597, -1, -1, 597, -1, -1, -1, -1, -1, -1, -1, 597, 597, -1, -1, 597, 597, -1, -1, 597, -1, 597, 597, -1, -1, -1, -1, -1, 597, 597, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 252, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 309, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 597, 597, 597, 597, 597, 597, -1, 597, 597, 597, 597, 597, 597, -1, 597, -1, -1, -1, -1, -1, -1, 597, 597, 597, 597, 597, 597, 597, 597, 597, -1, 597, 597, 597, 597, 597, -1, -1, 597, -1, -1, -1, -1, -1, -1, -1, 597, 597, -1, -1, 597, 597, -1, -1, 597, -1, 597, 597, -1, -1, 305, -1, -1, 597, 597, -1 },
			{ 1, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 303, 268, 303, 303, 303 },
			{ 1, 253, 253, 253, 253, 254, 254, 254, 254, 254, 254, 253, 254, 254, 254, 254, 254, 254, 253, 254, 253, 253, 253, 253, 253, 253, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 253, 253, 254, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 254, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 1, 253, 253, 253 },
			{ 1, 255, 255, 255, 255, 256, 256, 256, 256, 256, 256, 255, 256, 256, 256, 256, 256, 256, 255, 256, 255, 255, 255, 255, 255, 255, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 255, 255, 256, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 256, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 1, 255, 255, 255 },
			{ 1, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 258, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 1, 257, 257, 257 },
			{ 1, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 261, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 1, 260, 260, 260 },
			{ 1, 263, 264, 264, 263, 264, 264, 264, 264, 264, 264, 265, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 310, 264, 311, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 1, 264, 264, 264 },
			{ -1, 929, -1, 929, 929, 929, 929, 929, 929, 929, 271, -1, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, -1, -1, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, -1, 929, -1, 929 },
			{ -1, -1, -1, -1, -1, 906, 906, 334, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 815, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 287, -1, -1, -1, -1, -1, -1, 287, -1, -1, -1, 287, -1, -1, -1, -1, -1, -1, -1, -1, 287, -1, 287, -1, -1, -1, 287, -1, -1, -1, -1, -1, 287, 287, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 287, -1, -1, 287, 287, -1, -1, 287, -1, 287, 287, -1, -1, -1, -1, -1, 287, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 288, 288, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 288, 288, -1, -1, -1, -1, 288, 288, -1, -1, -1, -1, -1, 288, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 296, -1, -1, -1, -1, -1, -1, 296, -1, -1, -1, 296, -1, -1, -1, -1, -1, -1, -1, -1, 296, -1, 296, -1, -1, -1, 296, -1, -1, -1, -1, -1, 296, 296, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 296, -1, -1, 296, 296, -1, -1, 296, -1, 296, 296, -1, -1, -1, -1, -1, 296, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 297, 297, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 297, 297, -1, -1, -1, -1, 297, 297, -1, -1, -1, -1, -1, 297, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 306, -1, -1, -1, -1, -1, -1, 306, -1, -1, -1, 306, -1, -1, -1, -1, -1, -1, -1, -1, 306, -1, 306, -1, -1, -1, 306, -1, -1, -1, -1, -1, 306, 306, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 306, -1, -1, 306, 306, -1, -1, 306, -1, 306, 306, -1, -1, -1, -1, -1, 306, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 307, 307, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 307, 307, -1, -1, -1, -1, 307, 307, -1, -1, -1, -1, -1, 307, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 368, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 374, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 617, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 462, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 466, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 633, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 622, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 478, 478, 478, 478, 478, 478, -1, 478, 478, 478, 478, 478, 478, -1, 478, -1, -1, -1, -1, -1, -1, 478, 478, 478, 478, 478, 478, 478, 478, 478, 478, 478, 478, -1, -1, 478, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 478, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 516, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 512, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 626, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 508, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 550, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 544, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 557, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 565, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 570, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 579, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 650, 906, 906, 651, 340, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 510, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 343, 906, 906, 906, 906, 906, -1, 906, 906, 851, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 349, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 660, 812, 906, 906, -1, 906, 661, 906, 906, 850, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 352, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 355, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 358, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 819, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 856, 906, 906, 906, -1, 906, 668, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 669, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 361, 906, 906, 906, 906, 670, -1, 817, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 671, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 853, 906, 906, 906, 906, 672, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 820, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 673, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 674, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 675, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 890, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 676, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 874, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 367, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 889, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 369, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 680, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 371, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 373, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 375, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 683, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 377, 906, 898, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 379, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 685, 921, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 686, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 381, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 688, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 907, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 689, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 690, 906, 906, -1, 906, 906, 906, 906, 906, 691, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 383, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 693, 694, 906, 906, 906, 695, -1, 696, 857, 823, 697, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 698, 906, 699, 906, 906, 858, 906, 906, 906, 906, 906, 700, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 702, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 385, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 387, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 389, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 391, 906, 906, 906, 906, 917, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 706, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 393, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 825, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 710, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 397, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 891, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 859, -1, 906, 906, 906, 906, 906, 711, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 399, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 401, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 403, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 405, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 716, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 407, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 717, 906, 906, 409, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 879, 906, 718, 906, 906, 719, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 411, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 892, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 826, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 413, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 415, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 417, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 419, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 421, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 423, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 901, 906, 906, 906, 906, 425, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 831, 724, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 862, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 864, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 829, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 881, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 427, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 880, 906, 906, -1, 906, 906, 906, 906, 906, 922, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 726, 906, 906, -1, 906, 906, 906, 906, 894, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 863, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 429, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 431, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 433, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 435, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 728, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 437, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 905, 906, 906, 906, 882, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 731, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 732, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 439, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 441, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 443, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 445, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 736, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 833, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 447, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 737, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 451, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 836, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 453, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 866, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 742, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 455, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 457, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 749, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 839, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 869, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 887, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 752, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 459, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 461, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 755, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 463, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 838, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 465, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 884, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 868, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 467, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 759, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 469, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 471, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 475, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 477, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 479, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 867, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 761, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 762, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 841, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 885, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 481, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 766, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 483, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 485, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 487, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 770, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 489, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 776, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 491, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 778, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 493, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 845, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 495, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 497, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 871, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 499, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 783, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 501, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 785, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 503, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 505, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 786, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 507, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 509, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 511, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 513, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 740, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 787, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 788, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 515, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 517, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 847, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 792, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 793, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 794, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 519, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 521, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 523, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 525, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 795, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 527, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 529, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 531, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 533, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 798, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 800, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 535, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 801, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 537, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 539, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 541, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 803, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 543, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 545, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, 929, -1, 929, 929, 929, 929, 929, 929, 604, 929, -1, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, -1, -1, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, -1, 929, -1, 929 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 640, 641, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 642, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 606, -1, -1, -1, -1, -1, -1, 606, -1, -1, -1, 606, -1, -1, -1, -1, -1, -1, -1, -1, 606, -1, 606, -1, -1, -1, 606, -1, -1, -1, -1, -1, 606, 606, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 606, -1, -1, 606, 606, -1, -1, 606, -1, 606, 606, -1, -1, -1, -1, -1, 606, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 608, -1, -1, -1, -1, -1, -1, 608, -1, -1, -1, 608, -1, -1, -1, -1, -1, -1, -1, -1, 608, -1, 608, -1, -1, -1, 608, -1, -1, -1, -1, -1, 608, 608, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 608, -1, -1, 608, 608, -1, -1, 608, -1, 608, 608, -1, -1, -1, -1, -1, 608, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 610, -1, -1, -1, -1, -1, -1, 610, -1, -1, -1, 610, -1, -1, -1, -1, -1, -1, -1, -1, 610, -1, 610, -1, -1, -1, 610, -1, -1, -1, -1, -1, 610, 610, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 610, -1, -1, 610, 610, -1, -1, 610, -1, 610, 610, -1, -1, -1, -1, -1, 610, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 613, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 621, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 701, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 687, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 899, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 684, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 678, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 681, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 878, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 692, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 704, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 824, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 720, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 721, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 725, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 730, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 832, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 741, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 895, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 738, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 746, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 748, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 745, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 896, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 840, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 757, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 769, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 758, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 923, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 772, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 763, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 774, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 844, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 782, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 789, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 791, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 790, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 796, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 797, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 799, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 643, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 822, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 682, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 679, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 877, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 708, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 713, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 705, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 830, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 727, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 735, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 734, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 743, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 837, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 751, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 918, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 764, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 760, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 779, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 775, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 765, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 846, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 924, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 802, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 646, 906, 906, 906, -1, 906, 647, 906, 906, 648, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 712, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 714, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 707, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 893, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 729, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 834, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 750, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 747, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 835, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 768, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 773, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 781, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 780, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 767, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 649, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 715, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 860, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 733, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 739, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 754, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 865, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 756, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 771, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 652, 906, 906, 906, -1, 816, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 653, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 828, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 827, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 744, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 753, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 654, 906, 906, 906, 906, -1, 655, 906, 656, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 657, 906, 906, 906, 906, 906, 906, 658, 906, 906, 814, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 861, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 883, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 723, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 663, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 664, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 666, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 811, 906, 906, 906, -1, 906, 667, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, 929, -1, 929, 929, 929, 929, 929, 804, 929, 929, -1, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, -1, -1, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, -1, 929, -1, 929 },
			{ -1, -1, -1, -1, -1, -1, 806, -1, -1, -1, -1, -1, -1, 806, -1, -1, -1, 806, -1, -1, -1, -1, -1, -1, -1, -1, 806, -1, 806, -1, -1, -1, 806, -1, -1, -1, -1, -1, 806, 806, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 806, -1, -1, 806, 806, -1, -1, 806, -1, 806, 806, -1, -1, -1, -1, -1, 806, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 807, -1, -1, -1, -1, -1, -1, 807, -1, -1, -1, 807, -1, -1, -1, -1, -1, -1, -1, -1, 807, -1, 807, -1, -1, -1, 807, -1, -1, -1, -1, -1, 807, 807, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 807, -1, -1, 807, 807, -1, -1, 807, -1, 807, 807, -1, -1, -1, -1, -1, 807, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 808, -1, -1, -1, -1, -1, -1, 808, -1, -1, -1, 808, -1, -1, -1, -1, -1, -1, -1, -1, 808, -1, 808, -1, -1, -1, 808, -1, -1, -1, -1, -1, 808, 808, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 808, -1, -1, 808, 808, -1, -1, 808, -1, 808, 808, -1, -1, -1, -1, -1, 808, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 809, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 902, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 886, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 842, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 848, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 900, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 919, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 870, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, -1, -1, -1, -1, 906, 906, 906, 906, 906, 906, -1, 906, 906, 906, 906, 906, 906, -1, 906, -1, -1, -1, -1, -1, -1, 906, 906, 906, 872, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, 906, -1, -1, 906, 906, -1, -1, 906, -1, 906, 906, -1, -1, -1, -1, -1, 906, -1, -1 },
			{ -1, 929, -1, 929, 929, 929, 929, 912, 929, 929, 929, -1, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, -1, -1, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, -1, 929, -1, 929 },
			{ -1, -1, -1, -1, -1, -1, 913, -1, -1, -1, -1, -1, -1, 913, -1, -1, -1, 913, -1, -1, -1, -1, -1, -1, -1, -1, 913, -1, 913, -1, -1, -1, 913, -1, -1, -1, -1, -1, 913, 913, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 913, -1, -1, 913, 913, -1, -1, 913, -1, 913, 913, -1, -1, -1, -1, -1, 913, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 914, -1, -1, -1, -1, -1, -1, 914, -1, -1, -1, 914, -1, -1, -1, -1, -1, -1, -1, -1, 914, -1, 914, -1, -1, -1, 914, -1, -1, -1, -1, -1, 914, 914, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 914, -1, -1, 914, 914, -1, -1, 914, -1, 914, 914, -1, -1, -1, -1, -1, 914, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 915, -1, -1, -1, -1, -1, -1, 915, -1, -1, -1, 915, -1, -1, -1, -1, -1, -1, -1, -1, 915, -1, 915, -1, -1, -1, 915, -1, -1, -1, -1, -1, 915, 915, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 915, -1, -1, 915, 915, -1, -1, 915, -1, 915, 915, -1, -1, -1, -1, -1, 915, -1, -1 },
			{ -1, 929, -1, 929, 929, 929, 929, 929, 929, 929, 929, -1, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, -1, -1, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, 929, -1, 929, -1, 929 },
			{ -1, -1, -1, -1, -1, -1, 926, -1, -1, -1, -1, -1, -1, 926, -1, -1, -1, 926, -1, -1, -1, -1, -1, -1, -1, -1, 926, -1, 926, -1, -1, -1, 926, -1, -1, -1, -1, -1, 926, 926, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 926, -1, -1, 926, 926, -1, -1, 926, -1, 926, 926, -1, -1, -1, -1, -1, 926, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 927, -1, -1, -1, -1, -1, -1, 927, -1, -1, -1, 927, -1, -1, -1, -1, -1, -1, -1, -1, 927, -1, 927, -1, -1, -1, 927, -1, -1, -1, -1, -1, 927, 927, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 927, -1, -1, 927, 927, -1, -1, 927, -1, 927, 927, -1, -1, -1, -1, -1, 927, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 928, -1, -1, -1, -1, -1, -1, 928, -1, -1, -1, 928, -1, -1, -1, -1, -1, -1, -1, -1, 928, -1, 928, -1, -1, -1, 928, -1, -1, -1, -1, -1, 928, 928, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 928, -1, -1, 928, 928, -1, -1, 928, -1, 928, 928, -1, -1, -1, -1, -1, 928, -1, -1 }
		};
		
		
		private static int[] yy_state_dtrans = new int[]
		{
			  0,
			  376,
			  582,
			  586,
			  588,
			  592,
			  598,
			  599,
			  600,
			  601,
			  602,
			  603
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
							System.Diagnostics.Debug.Assert(last_accept_state >= 933);
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

