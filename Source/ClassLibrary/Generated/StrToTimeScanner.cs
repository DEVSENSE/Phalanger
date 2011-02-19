namespace PHP.Library.StrToTime
{
	#region User Code
	
	using System;
using System.Collections.Generic;
/*
 Copyright (c) 2005-2006 Tomas Matousek. Based on PHP5 implementation by Derick Rethans <derick@derickrethans.nl>. 
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 You must not remove this notice from this software.
*/
#endregion
	
	
	internal class Scanner
	{
		public enum LexicalStates
		{
			YYINITIAL = 0,
		}
		
		[Flags]
		private enum AcceptConditions : byte
		{
			NotAccept = 0,
			AcceptOnStart = 1,
			AcceptOnEnd = 2,
			Accept = 4
		}
		
		private const int NoState = -1;
		private const char BOL = (char)128;
		private const char EOF = (char)129;
		
		private Tokens yyreturn;
		internal DateInfo Time { get { return time; } }
		private DateInfo time = new DateInfo();
		internal int Errors { get { return errors; } } 
		private int errors = 0;
		internal int Position { get { return pos; } }
		private int pos = 0;
		private string str;
		void INIT()
		{
			str = new string(buffer, token_start, token_end - token_start);
			pos = 0;
		}
		void DEINIT()
		{
		}
		
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
		
		private bool yy_at_bol = false;
		
		public LexicalStates CurrentLexicalState { get { return current_lexical_state; } set { current_lexical_state = value; } } 
		private LexicalStates current_lexical_state;
		
		public Scanner(System.IO.TextReader reader)
		{
			Initialize(reader, LexicalStates.YYINITIAL);
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
				case 1:
					// #line 698
					{
					  break;
					}
					break;
					
				case 2:
					// #line 702
					{
					  return Tokens.ERROR;
					}
					break;
					
				case 3:
					// #line 644
					{
						INIT();
						errors += time.SetTimeZone(str, ref pos) ? 0 : 1;
						DEINIT();
						return Tokens.TIMEZONE;
					}
					break;
					
				case 4:
					// #line 435
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						DateInfo.SkipDaySuffix(str, ref pos);
						time.m = DateInfo.ParseMonth(str, ref pos);
						DEINIT();
						return Tokens.DATE_TEXT;
					}
					break;
					
				case 5:
					// #line 198
					{
						INIT();
						time.HAVE_RELATIVE();
						time.UNHAVE_DATE();
						time.UNHAVE_TIME();
						int i = DateInfo.ParseSignedInt(str, ref pos, 24);
						time.y = 1970;
						time.m = 1;
						time.d = 1;
						time.h = time.i = time.s = 0;
						time.f = 0.0;
						time.relative.s += i;
						time.z = 0;
						time.HAVE_TZ();
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 6:
					// #line 421
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.DATE_TEXT;
					}
					break;
					
				case 7:
					// #line 243
					{
						INIT();
						if (time.have_time!=0) { return Tokens.ERROR; }
						time.HAVE_TIME();
						time.h = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.i = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						if (pos < str.Length && (str[pos] == ':' || str[pos] == '.')) 
						{
							time.s = DateInfo.ParseUnsignedInt(str, ref pos, 2);
							if (pos < str.Length && str[pos] == '.') 
								time.f = DateInfo.ParseFraction(str, ref pos, 8);
						}
						if (pos < str.Length) 
							errors += time.SetTimeZone(str, ref pos) ? 0 : 1;
						DEINIT();
						return Tokens.TIME24_WITH_ZONE;
					}
					break;
					
				case 8:
					// #line 306
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						if (pos < str.Length && str[pos] == '/') 
						{
							time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
							time.y = DateInfo.ProcessYear(time.y);
					  }
						DEINIT();
						return Tokens.AMERICAN;
					}
					break;
					
				case 9:
					// #line 336
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR;} 
						time.HAVE_DATE();
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						DateInfo.SkipDaySuffix(str, ref pos);
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.DATE_FULL;
					}
					break;
					
				case 10:
					// #line 219
					{
						INIT();
						if (time.have_time!=0) { return Tokens.ERROR; }
						time.HAVE_TIME();
						time.h = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						if (pos < str.Length && (str[pos] == ':' || str[pos] == '.')) 
						{
						  time.i = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						  if (pos < str.Length && (str[pos] == ':' || str[pos] == '.')) 
						  {
							  time.s = DateInfo.ParseUnsignedInt(str, ref pos, 2);
							}  
						}
						if (!time.SetMeridian(str, ref pos))
						{
							return Tokens.ERROR; 
						}	
						DEINIT();
						return Tokens.TIME12;
					}
					break;
					
				case 11:
					// #line 597
					{
						INIT();
						time.relative.y = -time.relative.y;
						time.relative.m = -time.relative.m;
						time.relative.d = -time.relative.d;
						time.relative.h = -time.relative.h;
						time.relative.i = -time.relative.i;
						time.relative.s = -time.relative.s;
						time.relative.weekday = -time.relative.weekday;
						DEINIT();
						return Tokens.AGO;
					}
					break;
					
				case 12:
					// #line 630
					{
						INIT();
						time.HAVE_RELATIVE();
						time.HAVE_WEEKDAY_RELATIVE();
						time.UNHAVE_TIME();
						time.SetWeekDay(DateInfo.ReadToSpace(str,ref pos));
					  time.relative.weekday_behavior = 1;
						DEINIT();
						return Tokens.WEEKDAY;
					}
					break;
					
				case 13:
					// #line 163
					{
						INIT();
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 14:
					// #line 266
					{
						INIT();
						switch (time.have_time) 
						{
							case 0:
								time.h = DateInfo.ParseUnsignedInt(str, ref pos, 2);
								time.i = DateInfo.ParseUnsignedInt(str, ref pos, 2);
								time.s = 0;
								break;
							case 1:
								time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
								break;
							default:
								DEINIT();
								return Tokens.ERROR;
					  }
						time.have_time++;
						DEINIT();
						return Tokens.GNU_NOCOLON;
					}
					break;
					
				case 15:
					// #line 588
					{
						INIT();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						DEINIT();
						return Tokens.CLF;
					}
					break;
					
				case 16:
					// #line 682
					{
						INIT();
						time.HAVE_RELATIVE();
						while(pos < str.Length) 
						{
							int amount = DateInfo.ParseSignedInt(str, ref pos, 24);
							while (pos < str.Length && str[pos] == ' ') pos++;
							time.SetRelative(DateInfo.ReadToSpace(str, ref pos), amount, 0);
						}
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 17:
					// #line 170
					{
						INIT();
						time.UNHAVE_TIME();
						time.HAVE_TIME();
						time.h = 12;
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 18:
					// #line 351
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.ISO_DATE;
					}
					break;
					
				case 19:
					// #line 406
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.d = 1;
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.DATE_NO_DAY;
					}
					break;
					
				case 20:
					// #line 392
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.d = 1;
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.DATE_NO_DAY;
					}
					break;
					
				case 21:
					// #line 653
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						if (time.have_time!=0) { return Tokens.ERROR; }
						time.HAVE_TIME();
						time.h = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.i = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						if (pos < str.Length && str[pos] == ':') 
						{
							time.s = DateInfo.ParseUnsignedInt(str, ref pos, 2);
							if (pos < str.Length && str[pos] == '.') 
								time.f = DateInfo.ParseFraction(str, ref pos, 8);
					  }
						if (pos < str.Length) 
							errors += time.SetTimeZone(str, ref pos) ? 0 : 1;
						DEINIT();
						return Tokens.SHORTDATE_WITH_TIME;
					}
					break;
					
				case 22:
					// #line 180
					{
						INIT();
						time.UNHAVE_TIME();
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 23:
					// #line 378
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR;} 
						time.HAVE_DATE();
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.DATE_FULL_POINTED;
					}
					break;
					
				case 24:
					// #line 486
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 3);
						time.m = 1;
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.PG_YEARDAY;
					}
					break;
					
				case 25:
					// #line 290
					{
						INIT();
						if (time.have_time!=0) { return Tokens.ERROR; }
						time.HAVE_TIME();
						time.h = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.i = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.s = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						if (pos < str.Length) 
							errors += time.SetTimeZone(str, ref pos) ? 0 : 1;
						DEINIT();
						return Tokens.ISO_NOCOLON;
					}
					break;
					
				case 26:
					// #line 519
					{
						{
							int w, d;
							INIT();
							if (time.have_date!=0) { return Tokens.ERROR; } 
							time.HAVE_DATE();
							time.HAVE_RELATIVE();
							time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
							w = DateInfo.ParseUnsignedInt(str, ref pos, 2);
							d = 1;
							time.m = 1;
							time.d = 1;
							time.relative.d = DateInfo.WeekToDay(time.y, w, d);
							DEINIT();
							return Tokens.ISO_WEEK;
						}	
					}
					break;
					
				case 27:
					// #line 612
					{
						INIT();
						time.HAVE_RELATIVE();
						while (pos < str.Length) 
						{
						  int behavior;
							int amount = DateInfo.ParseRelativeText(str, ref pos, out behavior);
							while (pos < str.Length && str[pos] == ' ') pos++;
							time.SetRelative(DateInfo.ReadToSpace(str,ref pos), amount, behavior);
					  }
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 28:
					// #line 365
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR;} 
						time.HAVE_DATE();
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						DEINIT();
						return Tokens.DATE_FULL_POINTED;
					}
					break;
					
				case 29:
					// #line 448
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						DEINIT();
						return Tokens.DATE_NOCOLON;
					}
					break;
					
				case 30:
					// #line 500
					{
						int week, day;
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.HAVE_RELATIVE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						week = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						day = DateInfo.ParseUnsignedInt(str, ref pos, 1);
						time.m = 1;
						time.d = 1;
						time.relative.d = DateInfo.WeekToDay(time.y, week, day);
						DEINIT();
						return Tokens.ISO_WEEK;
					}
					break;
					
				case 31:
					// #line 540
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.PG_TEXT;
					}
					break;
					
				case 32:
					// #line 188
					{
						INIT();
						time.HAVE_RELATIVE();
						time.UNHAVE_TIME();
						time.relative.d = 1;
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 33:
					// #line 554
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.PG_TEXT;
					}
					break;
					
				case 34:
					// #line 153
					{
						INIT();
						time.HAVE_RELATIVE();
						time.UNHAVE_TIME();
						time.relative.d = -1;
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 35:
					// #line 323
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						DEINIT();
						return Tokens.ISO_DATE;
					}
					break;
					
				case 36:
					// #line 461
					{
						INIT();
						if (time.have_time!=0) { return Tokens.ERROR; }
						time.HAVE_TIME();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.h = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.i = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.s = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						if (pos < str.Length && str[pos] == '.') 
						{
							time.f = DateInfo.ParseFraction(str, ref pos, 9);
							if (pos < str.Length)
							  errors += time.SetTimeZone(str, ref pos) ? 0 : 1;
						}
						DEINIT();
						return Tokens.XMLRPC_SOAP;
					}
					break;
					
				case 37:
					// #line 568
					{
						INIT();
						if (time.have_time!=0) { return Tokens.ERROR; }
						time.HAVE_TIME();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.h = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.i = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.s = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						errors += time.SetTimeZone(str, ref pos) ? 0 : 1;
						DEINIT();
						return Tokens.CLF;
					}
					break;
					
				case 39: goto case 1;
				case 40: goto case 2;
				case 41: goto case 3;
				case 42: goto case 4;
				case 43: goto case 6;
				case 44: goto case 7;
				case 45: goto case 8;
				case 46: goto case 9;
				case 47: goto case 10;
				case 48: goto case 12;
				case 49: goto case 16;
				case 50: goto case 18;
				case 51: goto case 19;
				case 52: goto case 20;
				case 53: goto case 21;
				case 54: goto case 24;
				case 55: goto case 27;
				case 56: goto case 31;
				case 57: goto case 35;
				case 58: goto case 36;
				case 59: goto case 37;
				case 61: goto case 2;
				case 62: goto case 3;
				case 63: goto case 4;
				case 64: goto case 6;
				case 65: goto case 7;
				case 66: goto case 8;
				case 67: goto case 10;
				case 68: goto case 16;
				case 69: goto case 18;
				case 70: goto case 19;
				case 71: goto case 21;
				case 72: goto case 24;
				case 73: goto case 27;
				case 74: goto case 35;
				case 75: goto case 36;
				case 76: goto case 37;
				case 78: goto case 2;
				case 79: goto case 3;
				case 80: goto case 4;
				case 81: goto case 6;
				case 82: goto case 7;
				case 83: goto case 8;
				case 84: goto case 10;
				case 85: goto case 16;
				case 86: goto case 18;
				case 87: goto case 19;
				case 88: goto case 21;
				case 89: goto case 24;
				case 90: goto case 27;
				case 91: goto case 36;
				case 93: goto case 2;
				case 94: goto case 3;
				case 95: goto case 4;
				case 96: goto case 6;
				case 97: goto case 7;
				case 98: goto case 8;
				case 99: goto case 16;
				case 100: goto case 18;
				case 101: goto case 19;
				case 102: goto case 21;
				case 103: goto case 24;
				case 104: goto case 27;
				case 105: goto case 36;
				case 107: goto case 2;
				case 108: goto case 3;
				case 109: goto case 4;
				case 110: goto case 6;
				case 111: goto case 7;
				case 112: goto case 8;
				case 113: goto case 16;
				case 114: goto case 18;
				case 115: goto case 19;
				case 116: goto case 21;
				case 117: goto case 27;
				case 118: goto case 36;
				case 120: goto case 3;
				case 121: goto case 4;
				case 122: goto case 6;
				case 123: goto case 7;
				case 124: goto case 16;
				case 125: goto case 18;
				case 126: goto case 19;
				case 127: goto case 21;
				case 128: goto case 27;
				case 129: goto case 36;
				case 131: goto case 3;
				case 132: goto case 4;
				case 133: goto case 6;
				case 134: goto case 7;
				case 135: goto case 16;
				case 136: goto case 18;
				case 137: goto case 19;
				case 138: goto case 21;
				case 139: goto case 27;
				case 140: goto case 36;
				case 142: goto case 3;
				case 143: goto case 4;
				case 144: goto case 6;
				case 145: goto case 7;
				case 146: goto case 16;
				case 147: goto case 18;
				case 148: goto case 19;
				case 149: goto case 21;
				case 150: goto case 27;
				case 151: goto case 36;
				case 153: goto case 3;
				case 154: goto case 4;
				case 155: goto case 6;
				case 156: goto case 7;
				case 157: goto case 16;
				case 158: goto case 18;
				case 159: goto case 19;
				case 160: goto case 21;
				case 161: goto case 27;
				case 162: goto case 36;
				case 164: goto case 3;
				case 165: goto case 4;
				case 166: goto case 6;
				case 167: goto case 7;
				case 168: goto case 18;
				case 169: goto case 19;
				case 170: goto case 21;
				case 171: goto case 27;
				case 172: goto case 36;
				case 174: goto case 3;
				case 175: goto case 4;
				case 176: goto case 6;
				case 177: goto case 7;
				case 178: goto case 18;
				case 179: goto case 19;
				case 180: goto case 21;
				case 181: goto case 27;
				case 182: goto case 36;
				case 184: goto case 3;
				case 185: goto case 6;
				case 186: goto case 7;
				case 187: goto case 18;
				case 188: goto case 19;
				case 189: goto case 21;
				case 191: goto case 3;
				case 192: goto case 6;
				case 193: goto case 7;
				case 194: goto case 19;
				case 195: goto case 21;
				case 197: goto case 3;
				case 198: goto case 6;
				case 199: goto case 7;
				case 200: goto case 19;
				case 201: goto case 21;
				case 203: goto case 3;
				case 204: goto case 6;
				case 205: goto case 7;
				case 206: goto case 19;
				case 207: goto case 21;
				case 209: goto case 3;
				case 210: goto case 6;
				case 211: goto case 7;
				case 212: goto case 19;
				case 213: goto case 21;
				case 215: goto case 3;
				case 216: goto case 6;
				case 217: goto case 7;
				case 218: goto case 19;
				case 219: goto case 21;
				case 221: goto case 3;
				case 222: goto case 6;
				case 223: goto case 7;
				case 224: goto case 19;
				case 225: goto case 21;
				case 227: goto case 3;
				case 228: goto case 6;
				case 229: goto case 7;
				case 230: goto case 19;
				case 231: goto case 21;
				case 233: goto case 3;
				case 234: goto case 6;
				case 235: goto case 7;
				case 236: goto case 19;
				case 237: goto case 21;
				case 239: goto case 3;
				case 240: goto case 7;
				case 241: goto case 19;
				case 242: goto case 21;
				case 244: goto case 3;
				case 245: goto case 7;
				case 246: goto case 21;
				case 248: goto case 3;
				case 249: goto case 7;
				case 250: goto case 21;
				case 252: goto case 3;
				case 253: goto case 7;
				case 254: goto case 21;
				case 256: goto case 3;
				case 257: goto case 7;
				case 258: goto case 21;
				case 260: goto case 3;
				case 261: goto case 7;
				case 263: goto case 3;
				case 264: goto case 7;
				case 266: goto case 7;
				case 268: goto case 7;
				case 270: goto case 7;
				case 272: goto case 7;
				case 274: goto case 7;
				case 276: goto case 7;
				case 278: goto case 7;
				case 280: goto case 7;
				case 618: goto case 3;
				case 619: goto case 6;
				case 620: goto case 9;
				case 621: goto case 31;
				case 622: goto case 37;
				case 623: goto case 3;
				case 624: goto case 4;
				case 625: goto case 7;
				case 626: goto case 19;
				case 627: goto case 20;
				case 628: goto case 27;
				case 629: goto case 37;
				case 631: goto case 3;
				case 632: goto case 10;
				case 633: goto case 18;
				case 634: goto case 21;
				case 635: goto case 7;
				case 636: goto case 8;
				case 637: goto case 2;
				case 638: goto case 3;
				case 639: goto case 4;
				case 640: goto case 7;
				case 641: goto case 19;
				case 642: goto case 4;
				case 643: goto case 6;
				case 644: goto case 7;
				case 645: goto case 18;
				case 646: goto case 19;
				case 647: goto case 21;
				case 648: goto case 7;
				case 649: goto case 21;
				case 650: goto case 7;
				case 651: goto case 21;
				case 652: goto case 6;
				case 653: goto case 7;
				case 655: goto case 7;
				case 656: goto case 21;
				case 658: goto case 7;
				case 659: goto case 19;
				case 660: goto case 21;
				case 662: goto case 7;
				case 663: goto case 19;
				case 664: goto case 3;
				case 665: goto case 6;
				case 666: goto case 21;
				case 668: goto case 7;
				case 669: goto case 6;
				case 670: goto case 3;
				case 671: goto case 21;
				case 674: goto case 3;
				case 675: goto case 3;
				case 676: goto case 21;
				case 677: goto case 7;
				case 678: goto case 7;
				case 726: goto case 3;
				case 727: goto case 6;
				case 728: goto case 9;
				case 729: goto case 37;
				case 730: goto case 7;
				case 732: goto case 3;
				case 733: goto case 2;
				case 734: goto case 3;
				case 735: goto case 6;
				case 736: goto case 7;
				case 737: goto case 21;
				case 738: goto case 21;
				case 739: goto case 7;
				case 741: goto case 7;
				case 742: goto case 21;
				case 743: goto case 7;
				case 744: goto case 6;
				case 745: goto case 3;
				case 747: goto case 3;
				case 760: goto case 3;
				case 761: goto case 6;
				case 763: goto case 2;
				case 764: goto case 3;
				case 765: goto case 6;
				case 766: goto case 7;
				case 767: goto case 21;
				case 768: goto case 21;
				case 769: goto case 7;
				case 770: goto case 7;
				case 771: goto case 21;
				case 772: goto case 7;
				case 773: goto case 3;
				case 775: goto case 3;
				case 780: goto case 3;
				case 782: goto case 3;
				case 783: goto case 6;
				case 784: goto case 21;
				case 785: goto case 7;
				case 786: goto case 7;
				case 787: goto case 3;
				case 789: goto case 3;
				case 793: goto case 3;
				case 795: goto case 3;
				case 796: goto case 6;
				case 797: goto case 7;
				case 798: goto case 3;
				case 800: goto case 3;
				case 804: goto case 3;
				case 806: goto case 3;
				case 807: goto case 7;
				case 809: goto case 3;
				case 812: goto case 3;
				case 814: goto case 3;
				case 818: goto case 3;
				case 820: goto case 3;
				case 823: goto case 3;
				case 825: goto case 3;
				case 828: goto case 3;
				case 830: goto case 3;
				case 833: goto case 3;
				case 834: goto case 3;
				case 837: goto case 3;
				case 840: goto case 3;
				case 842: goto case 3;
				case 844: goto case 3;
				case 846: goto case 3;
				case 854: goto case 7;
				case 855: goto case 9;
				case 856: goto case 12;
				case 857: goto case 27;
				case 858: goto case 31;
				case 859: goto case 4;
				case 860: goto case 7;
				case 861: goto case 19;
				case 862: goto case 27;
				case 863: goto case 7;
				case 864: goto case 8;
				case 865: goto case 3;
				case 866: goto case 3;
				case 867: goto case 7;
				case 868: goto case 21;
				case 869: goto case 21;
				case 870: goto case 7;
				case 871: goto case 6;
				case 872: goto case 7;
				case 873: goto case 3;
				case 874: goto case 3;
				case 880: goto case 3;
				case 881: goto case 3;
				case 882: goto case 4;
				case 883: goto case 4;
				case 884: goto case 3;
				case 885: goto case 7;
				case 886: goto case 9;
				case 887: goto case 12;
				case 888: goto case 27;
				case 889: goto case 27;
				case 890: goto case 7;
				case 891: goto case 3;
				case 892: goto case 6;
				case 893: goto case 3;
				case 894: goto case 3;
				case 897: goto case 3;
				case 898: goto case 3;
				case 899: goto case 9;
				case 900: goto case 3;
				case 901: goto case 3;
				case 902: goto case 3;
				case 905: goto case 9;
				case 906: goto case 3;
				case 907: goto case 3;
				case 909: goto case 3;
				case 911: goto case 3;
				case 913: goto case 3;
				case 920: goto case 4;
				case 921: goto case 4;
				case 922: goto case 3;
				case 923: goto case 4;
				case 924: goto case 4;
				case 925: goto case 4;
				case 926: goto case 4;
				case 927: goto case 4;
				case 928: goto case 4;
				case 929: goto case 4;
				case 930: goto case 4;
				case 931: goto case 3;
				case 948: goto case 3;
				case 949: goto case 12;
				case 950: goto case 27;
				case 951: goto case 3;
				case 953: goto case 3;
				case 955: goto case 3;
				case 956: goto case 12;
				case 957: goto case 27;
				case 958: goto case 3;
				case 959: goto case 3;
				case 961: goto case 3;
				case 962: goto case 3;
				case 964: goto case 3;
				case 965: goto case 3;
				case 967: goto case 3;
				case 969: goto case 3;
				case 971: goto case 3;
				case 974: goto case 3;
				case 978: goto case 3;
				case 980: goto case 3;
				case 981: goto case 3;
				case 982: goto case 27;
				case 983: goto case 3;
				case 984: goto case 3;
				case 986: goto case 3;
				case 987: goto case 3;
				case 988: goto case 3;
				case 990: goto case 3;
				case 992: goto case 3;
				case 993: goto case 3;
				case 994: goto case 3;
				case 996: goto case 3;
				case 997: goto case 3;
				case 998: goto case 3;
				case 999: goto case 3;
				case 1000: goto case 3;
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
			
			return buffer[lookahead_index++];
		}
		
		private char[] ResizeBuffer(char[] buf)
		{
			char[] result = new char[buf.Length << 1];
			System.Buffer.BlockCopy(buf, 0, result, 0, buf.Length << 1);
			return result;
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
			AcceptConditions.NotAccept, // 38
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
			AcceptConditions.NotAccept, // 60
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
			AcceptConditions.NotAccept, // 77
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
			AcceptConditions.NotAccept, // 92
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
			AcceptConditions.NotAccept, // 106
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
			AcceptConditions.NotAccept, // 119
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
			AcceptConditions.NotAccept, // 130
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
			AcceptConditions.NotAccept, // 141
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
			AcceptConditions.NotAccept, // 152
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
			AcceptConditions.NotAccept, // 163
			AcceptConditions.Accept, // 164
			AcceptConditions.Accept, // 165
			AcceptConditions.Accept, // 166
			AcceptConditions.Accept, // 167
			AcceptConditions.Accept, // 168
			AcceptConditions.Accept, // 169
			AcceptConditions.Accept, // 170
			AcceptConditions.Accept, // 171
			AcceptConditions.Accept, // 172
			AcceptConditions.NotAccept, // 173
			AcceptConditions.Accept, // 174
			AcceptConditions.Accept, // 175
			AcceptConditions.Accept, // 176
			AcceptConditions.Accept, // 177
			AcceptConditions.Accept, // 178
			AcceptConditions.Accept, // 179
			AcceptConditions.Accept, // 180
			AcceptConditions.Accept, // 181
			AcceptConditions.Accept, // 182
			AcceptConditions.NotAccept, // 183
			AcceptConditions.Accept, // 184
			AcceptConditions.Accept, // 185
			AcceptConditions.Accept, // 186
			AcceptConditions.Accept, // 187
			AcceptConditions.Accept, // 188
			AcceptConditions.Accept, // 189
			AcceptConditions.NotAccept, // 190
			AcceptConditions.Accept, // 191
			AcceptConditions.Accept, // 192
			AcceptConditions.Accept, // 193
			AcceptConditions.Accept, // 194
			AcceptConditions.Accept, // 195
			AcceptConditions.NotAccept, // 196
			AcceptConditions.Accept, // 197
			AcceptConditions.Accept, // 198
			AcceptConditions.Accept, // 199
			AcceptConditions.Accept, // 200
			AcceptConditions.Accept, // 201
			AcceptConditions.NotAccept, // 202
			AcceptConditions.Accept, // 203
			AcceptConditions.Accept, // 204
			AcceptConditions.Accept, // 205
			AcceptConditions.Accept, // 206
			AcceptConditions.Accept, // 207
			AcceptConditions.NotAccept, // 208
			AcceptConditions.Accept, // 209
			AcceptConditions.Accept, // 210
			AcceptConditions.Accept, // 211
			AcceptConditions.Accept, // 212
			AcceptConditions.Accept, // 213
			AcceptConditions.NotAccept, // 214
			AcceptConditions.Accept, // 215
			AcceptConditions.Accept, // 216
			AcceptConditions.Accept, // 217
			AcceptConditions.Accept, // 218
			AcceptConditions.Accept, // 219
			AcceptConditions.NotAccept, // 220
			AcceptConditions.Accept, // 221
			AcceptConditions.Accept, // 222
			AcceptConditions.Accept, // 223
			AcceptConditions.Accept, // 224
			AcceptConditions.Accept, // 225
			AcceptConditions.NotAccept, // 226
			AcceptConditions.Accept, // 227
			AcceptConditions.Accept, // 228
			AcceptConditions.Accept, // 229
			AcceptConditions.Accept, // 230
			AcceptConditions.Accept, // 231
			AcceptConditions.NotAccept, // 232
			AcceptConditions.Accept, // 233
			AcceptConditions.Accept, // 234
			AcceptConditions.Accept, // 235
			AcceptConditions.Accept, // 236
			AcceptConditions.Accept, // 237
			AcceptConditions.NotAccept, // 238
			AcceptConditions.Accept, // 239
			AcceptConditions.Accept, // 240
			AcceptConditions.Accept, // 241
			AcceptConditions.Accept, // 242
			AcceptConditions.NotAccept, // 243
			AcceptConditions.Accept, // 244
			AcceptConditions.Accept, // 245
			AcceptConditions.Accept, // 246
			AcceptConditions.NotAccept, // 247
			AcceptConditions.Accept, // 248
			AcceptConditions.Accept, // 249
			AcceptConditions.Accept, // 250
			AcceptConditions.NotAccept, // 251
			AcceptConditions.Accept, // 252
			AcceptConditions.Accept, // 253
			AcceptConditions.Accept, // 254
			AcceptConditions.NotAccept, // 255
			AcceptConditions.Accept, // 256
			AcceptConditions.Accept, // 257
			AcceptConditions.Accept, // 258
			AcceptConditions.NotAccept, // 259
			AcceptConditions.Accept, // 260
			AcceptConditions.Accept, // 261
			AcceptConditions.NotAccept, // 262
			AcceptConditions.Accept, // 263
			AcceptConditions.Accept, // 264
			AcceptConditions.NotAccept, // 265
			AcceptConditions.Accept, // 266
			AcceptConditions.NotAccept, // 267
			AcceptConditions.Accept, // 268
			AcceptConditions.NotAccept, // 269
			AcceptConditions.Accept, // 270
			AcceptConditions.NotAccept, // 271
			AcceptConditions.Accept, // 272
			AcceptConditions.NotAccept, // 273
			AcceptConditions.Accept, // 274
			AcceptConditions.NotAccept, // 275
			AcceptConditions.Accept, // 276
			AcceptConditions.NotAccept, // 277
			AcceptConditions.Accept, // 278
			AcceptConditions.NotAccept, // 279
			AcceptConditions.Accept, // 280
			AcceptConditions.NotAccept, // 281
			AcceptConditions.NotAccept, // 282
			AcceptConditions.NotAccept, // 283
			AcceptConditions.NotAccept, // 284
			AcceptConditions.NotAccept, // 285
			AcceptConditions.NotAccept, // 286
			AcceptConditions.NotAccept, // 287
			AcceptConditions.NotAccept, // 288
			AcceptConditions.NotAccept, // 289
			AcceptConditions.NotAccept, // 290
			AcceptConditions.NotAccept, // 291
			AcceptConditions.NotAccept, // 292
			AcceptConditions.NotAccept, // 293
			AcceptConditions.NotAccept, // 294
			AcceptConditions.NotAccept, // 295
			AcceptConditions.NotAccept, // 296
			AcceptConditions.NotAccept, // 297
			AcceptConditions.NotAccept, // 298
			AcceptConditions.NotAccept, // 299
			AcceptConditions.NotAccept, // 300
			AcceptConditions.NotAccept, // 301
			AcceptConditions.NotAccept, // 302
			AcceptConditions.NotAccept, // 303
			AcceptConditions.NotAccept, // 304
			AcceptConditions.NotAccept, // 305
			AcceptConditions.NotAccept, // 306
			AcceptConditions.NotAccept, // 307
			AcceptConditions.NotAccept, // 308
			AcceptConditions.NotAccept, // 309
			AcceptConditions.NotAccept, // 310
			AcceptConditions.NotAccept, // 311
			AcceptConditions.NotAccept, // 312
			AcceptConditions.NotAccept, // 313
			AcceptConditions.NotAccept, // 314
			AcceptConditions.NotAccept, // 315
			AcceptConditions.NotAccept, // 316
			AcceptConditions.NotAccept, // 317
			AcceptConditions.NotAccept, // 318
			AcceptConditions.NotAccept, // 319
			AcceptConditions.NotAccept, // 320
			AcceptConditions.NotAccept, // 321
			AcceptConditions.NotAccept, // 322
			AcceptConditions.NotAccept, // 323
			AcceptConditions.NotAccept, // 324
			AcceptConditions.NotAccept, // 325
			AcceptConditions.NotAccept, // 326
			AcceptConditions.NotAccept, // 327
			AcceptConditions.NotAccept, // 328
			AcceptConditions.NotAccept, // 329
			AcceptConditions.NotAccept, // 330
			AcceptConditions.NotAccept, // 331
			AcceptConditions.NotAccept, // 332
			AcceptConditions.NotAccept, // 333
			AcceptConditions.NotAccept, // 334
			AcceptConditions.NotAccept, // 335
			AcceptConditions.NotAccept, // 336
			AcceptConditions.NotAccept, // 337
			AcceptConditions.NotAccept, // 338
			AcceptConditions.NotAccept, // 339
			AcceptConditions.NotAccept, // 340
			AcceptConditions.NotAccept, // 341
			AcceptConditions.NotAccept, // 342
			AcceptConditions.NotAccept, // 343
			AcceptConditions.NotAccept, // 344
			AcceptConditions.NotAccept, // 345
			AcceptConditions.NotAccept, // 346
			AcceptConditions.NotAccept, // 347
			AcceptConditions.NotAccept, // 348
			AcceptConditions.NotAccept, // 349
			AcceptConditions.NotAccept, // 350
			AcceptConditions.NotAccept, // 351
			AcceptConditions.NotAccept, // 352
			AcceptConditions.NotAccept, // 353
			AcceptConditions.NotAccept, // 354
			AcceptConditions.NotAccept, // 355
			AcceptConditions.NotAccept, // 356
			AcceptConditions.NotAccept, // 357
			AcceptConditions.NotAccept, // 358
			AcceptConditions.NotAccept, // 359
			AcceptConditions.NotAccept, // 360
			AcceptConditions.NotAccept, // 361
			AcceptConditions.NotAccept, // 362
			AcceptConditions.NotAccept, // 363
			AcceptConditions.NotAccept, // 364
			AcceptConditions.NotAccept, // 365
			AcceptConditions.NotAccept, // 366
			AcceptConditions.NotAccept, // 367
			AcceptConditions.NotAccept, // 368
			AcceptConditions.NotAccept, // 369
			AcceptConditions.NotAccept, // 370
			AcceptConditions.NotAccept, // 371
			AcceptConditions.NotAccept, // 372
			AcceptConditions.NotAccept, // 373
			AcceptConditions.NotAccept, // 374
			AcceptConditions.NotAccept, // 375
			AcceptConditions.NotAccept, // 376
			AcceptConditions.NotAccept, // 377
			AcceptConditions.NotAccept, // 378
			AcceptConditions.NotAccept, // 379
			AcceptConditions.NotAccept, // 380
			AcceptConditions.NotAccept, // 381
			AcceptConditions.NotAccept, // 382
			AcceptConditions.NotAccept, // 383
			AcceptConditions.NotAccept, // 384
			AcceptConditions.NotAccept, // 385
			AcceptConditions.NotAccept, // 386
			AcceptConditions.NotAccept, // 387
			AcceptConditions.NotAccept, // 388
			AcceptConditions.NotAccept, // 389
			AcceptConditions.NotAccept, // 390
			AcceptConditions.NotAccept, // 391
			AcceptConditions.NotAccept, // 392
			AcceptConditions.NotAccept, // 393
			AcceptConditions.NotAccept, // 394
			AcceptConditions.NotAccept, // 395
			AcceptConditions.NotAccept, // 396
			AcceptConditions.NotAccept, // 397
			AcceptConditions.NotAccept, // 398
			AcceptConditions.NotAccept, // 399
			AcceptConditions.NotAccept, // 400
			AcceptConditions.NotAccept, // 401
			AcceptConditions.NotAccept, // 402
			AcceptConditions.NotAccept, // 403
			AcceptConditions.NotAccept, // 404
			AcceptConditions.NotAccept, // 405
			AcceptConditions.NotAccept, // 406
			AcceptConditions.NotAccept, // 407
			AcceptConditions.NotAccept, // 408
			AcceptConditions.NotAccept, // 409
			AcceptConditions.NotAccept, // 410
			AcceptConditions.NotAccept, // 411
			AcceptConditions.NotAccept, // 412
			AcceptConditions.NotAccept, // 413
			AcceptConditions.NotAccept, // 414
			AcceptConditions.NotAccept, // 415
			AcceptConditions.NotAccept, // 416
			AcceptConditions.NotAccept, // 417
			AcceptConditions.NotAccept, // 418
			AcceptConditions.NotAccept, // 419
			AcceptConditions.NotAccept, // 420
			AcceptConditions.NotAccept, // 421
			AcceptConditions.NotAccept, // 422
			AcceptConditions.NotAccept, // 423
			AcceptConditions.NotAccept, // 424
			AcceptConditions.NotAccept, // 425
			AcceptConditions.NotAccept, // 426
			AcceptConditions.NotAccept, // 427
			AcceptConditions.NotAccept, // 428
			AcceptConditions.NotAccept, // 429
			AcceptConditions.NotAccept, // 430
			AcceptConditions.NotAccept, // 431
			AcceptConditions.NotAccept, // 432
			AcceptConditions.NotAccept, // 433
			AcceptConditions.NotAccept, // 434
			AcceptConditions.NotAccept, // 435
			AcceptConditions.NotAccept, // 436
			AcceptConditions.NotAccept, // 437
			AcceptConditions.NotAccept, // 438
			AcceptConditions.NotAccept, // 439
			AcceptConditions.NotAccept, // 440
			AcceptConditions.NotAccept, // 441
			AcceptConditions.NotAccept, // 442
			AcceptConditions.NotAccept, // 443
			AcceptConditions.NotAccept, // 444
			AcceptConditions.NotAccept, // 445
			AcceptConditions.NotAccept, // 446
			AcceptConditions.NotAccept, // 447
			AcceptConditions.NotAccept, // 448
			AcceptConditions.NotAccept, // 449
			AcceptConditions.NotAccept, // 450
			AcceptConditions.NotAccept, // 451
			AcceptConditions.NotAccept, // 452
			AcceptConditions.NotAccept, // 453
			AcceptConditions.NotAccept, // 454
			AcceptConditions.NotAccept, // 455
			AcceptConditions.NotAccept, // 456
			AcceptConditions.NotAccept, // 457
			AcceptConditions.NotAccept, // 458
			AcceptConditions.NotAccept, // 459
			AcceptConditions.NotAccept, // 460
			AcceptConditions.NotAccept, // 461
			AcceptConditions.NotAccept, // 462
			AcceptConditions.NotAccept, // 463
			AcceptConditions.NotAccept, // 464
			AcceptConditions.NotAccept, // 465
			AcceptConditions.NotAccept, // 466
			AcceptConditions.NotAccept, // 467
			AcceptConditions.NotAccept, // 468
			AcceptConditions.NotAccept, // 469
			AcceptConditions.NotAccept, // 470
			AcceptConditions.NotAccept, // 471
			AcceptConditions.NotAccept, // 472
			AcceptConditions.NotAccept, // 473
			AcceptConditions.NotAccept, // 474
			AcceptConditions.NotAccept, // 475
			AcceptConditions.NotAccept, // 476
			AcceptConditions.NotAccept, // 477
			AcceptConditions.NotAccept, // 478
			AcceptConditions.NotAccept, // 479
			AcceptConditions.NotAccept, // 480
			AcceptConditions.NotAccept, // 481
			AcceptConditions.NotAccept, // 482
			AcceptConditions.NotAccept, // 483
			AcceptConditions.NotAccept, // 484
			AcceptConditions.NotAccept, // 485
			AcceptConditions.NotAccept, // 486
			AcceptConditions.NotAccept, // 487
			AcceptConditions.NotAccept, // 488
			AcceptConditions.NotAccept, // 489
			AcceptConditions.NotAccept, // 490
			AcceptConditions.NotAccept, // 491
			AcceptConditions.NotAccept, // 492
			AcceptConditions.NotAccept, // 493
			AcceptConditions.NotAccept, // 494
			AcceptConditions.NotAccept, // 495
			AcceptConditions.NotAccept, // 496
			AcceptConditions.NotAccept, // 497
			AcceptConditions.NotAccept, // 498
			AcceptConditions.NotAccept, // 499
			AcceptConditions.NotAccept, // 500
			AcceptConditions.NotAccept, // 501
			AcceptConditions.NotAccept, // 502
			AcceptConditions.NotAccept, // 503
			AcceptConditions.NotAccept, // 504
			AcceptConditions.NotAccept, // 505
			AcceptConditions.NotAccept, // 506
			AcceptConditions.NotAccept, // 507
			AcceptConditions.NotAccept, // 508
			AcceptConditions.NotAccept, // 509
			AcceptConditions.NotAccept, // 510
			AcceptConditions.NotAccept, // 511
			AcceptConditions.NotAccept, // 512
			AcceptConditions.NotAccept, // 513
			AcceptConditions.NotAccept, // 514
			AcceptConditions.NotAccept, // 515
			AcceptConditions.NotAccept, // 516
			AcceptConditions.NotAccept, // 517
			AcceptConditions.NotAccept, // 518
			AcceptConditions.NotAccept, // 519
			AcceptConditions.NotAccept, // 520
			AcceptConditions.NotAccept, // 521
			AcceptConditions.NotAccept, // 522
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
			AcceptConditions.NotAccept, // 617
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
			AcceptConditions.NotAccept, // 654
			AcceptConditions.Accept, // 655
			AcceptConditions.Accept, // 656
			AcceptConditions.NotAccept, // 657
			AcceptConditions.Accept, // 658
			AcceptConditions.Accept, // 659
			AcceptConditions.Accept, // 660
			AcceptConditions.NotAccept, // 661
			AcceptConditions.Accept, // 662
			AcceptConditions.Accept, // 663
			AcceptConditions.Accept, // 664
			AcceptConditions.Accept, // 665
			AcceptConditions.Accept, // 666
			AcceptConditions.NotAccept, // 667
			AcceptConditions.Accept, // 668
			AcceptConditions.Accept, // 669
			AcceptConditions.Accept, // 670
			AcceptConditions.Accept, // 671
			AcceptConditions.NotAccept, // 672
			AcceptConditions.NotAccept, // 673
			AcceptConditions.Accept, // 674
			AcceptConditions.Accept, // 675
			AcceptConditions.Accept, // 676
			AcceptConditions.Accept, // 677
			AcceptConditions.Accept, // 678
			AcceptConditions.NotAccept, // 679
			AcceptConditions.NotAccept, // 680
			AcceptConditions.NotAccept, // 681
			AcceptConditions.NotAccept, // 682
			AcceptConditions.NotAccept, // 683
			AcceptConditions.NotAccept, // 684
			AcceptConditions.NotAccept, // 685
			AcceptConditions.NotAccept, // 686
			AcceptConditions.NotAccept, // 687
			AcceptConditions.NotAccept, // 688
			AcceptConditions.NotAccept, // 689
			AcceptConditions.NotAccept, // 690
			AcceptConditions.NotAccept, // 691
			AcceptConditions.NotAccept, // 692
			AcceptConditions.NotAccept, // 693
			AcceptConditions.NotAccept, // 694
			AcceptConditions.NotAccept, // 695
			AcceptConditions.NotAccept, // 696
			AcceptConditions.NotAccept, // 697
			AcceptConditions.NotAccept, // 698
			AcceptConditions.NotAccept, // 699
			AcceptConditions.NotAccept, // 700
			AcceptConditions.NotAccept, // 701
			AcceptConditions.NotAccept, // 702
			AcceptConditions.NotAccept, // 703
			AcceptConditions.NotAccept, // 704
			AcceptConditions.NotAccept, // 705
			AcceptConditions.NotAccept, // 706
			AcceptConditions.NotAccept, // 707
			AcceptConditions.NotAccept, // 708
			AcceptConditions.NotAccept, // 709
			AcceptConditions.NotAccept, // 710
			AcceptConditions.NotAccept, // 711
			AcceptConditions.NotAccept, // 712
			AcceptConditions.NotAccept, // 713
			AcceptConditions.NotAccept, // 714
			AcceptConditions.NotAccept, // 715
			AcceptConditions.NotAccept, // 716
			AcceptConditions.NotAccept, // 717
			AcceptConditions.NotAccept, // 718
			AcceptConditions.NotAccept, // 719
			AcceptConditions.NotAccept, // 720
			AcceptConditions.NotAccept, // 721
			AcceptConditions.NotAccept, // 722
			AcceptConditions.NotAccept, // 723
			AcceptConditions.NotAccept, // 724
			AcceptConditions.NotAccept, // 725
			AcceptConditions.Accept, // 726
			AcceptConditions.Accept, // 727
			AcceptConditions.Accept, // 728
			AcceptConditions.Accept, // 729
			AcceptConditions.Accept, // 730
			AcceptConditions.NotAccept, // 731
			AcceptConditions.Accept, // 732
			AcceptConditions.Accept, // 733
			AcceptConditions.Accept, // 734
			AcceptConditions.Accept, // 735
			AcceptConditions.Accept, // 736
			AcceptConditions.Accept, // 737
			AcceptConditions.Accept, // 738
			AcceptConditions.Accept, // 739
			AcceptConditions.NotAccept, // 740
			AcceptConditions.Accept, // 741
			AcceptConditions.Accept, // 742
			AcceptConditions.Accept, // 743
			AcceptConditions.Accept, // 744
			AcceptConditions.Accept, // 745
			AcceptConditions.NotAccept, // 746
			AcceptConditions.Accept, // 747
			AcceptConditions.NotAccept, // 748
			AcceptConditions.NotAccept, // 749
			AcceptConditions.NotAccept, // 750
			AcceptConditions.NotAccept, // 751
			AcceptConditions.NotAccept, // 752
			AcceptConditions.NotAccept, // 753
			AcceptConditions.NotAccept, // 754
			AcceptConditions.NotAccept, // 755
			AcceptConditions.NotAccept, // 756
			AcceptConditions.NotAccept, // 757
			AcceptConditions.NotAccept, // 758
			AcceptConditions.NotAccept, // 759
			AcceptConditions.Accept, // 760
			AcceptConditions.Accept, // 761
			AcceptConditions.NotAccept, // 762
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
			AcceptConditions.NotAccept, // 774
			AcceptConditions.Accept, // 775
			AcceptConditions.NotAccept, // 776
			AcceptConditions.NotAccept, // 777
			AcceptConditions.NotAccept, // 778
			AcceptConditions.NotAccept, // 779
			AcceptConditions.Accept, // 780
			AcceptConditions.NotAccept, // 781
			AcceptConditions.Accept, // 782
			AcceptConditions.Accept, // 783
			AcceptConditions.Accept, // 784
			AcceptConditions.Accept, // 785
			AcceptConditions.Accept, // 786
			AcceptConditions.Accept, // 787
			AcceptConditions.NotAccept, // 788
			AcceptConditions.Accept, // 789
			AcceptConditions.NotAccept, // 790
			AcceptConditions.NotAccept, // 791
			AcceptConditions.NotAccept, // 792
			AcceptConditions.Accept, // 793
			AcceptConditions.NotAccept, // 794
			AcceptConditions.Accept, // 795
			AcceptConditions.Accept, // 796
			AcceptConditions.Accept, // 797
			AcceptConditions.Accept, // 798
			AcceptConditions.NotAccept, // 799
			AcceptConditions.Accept, // 800
			AcceptConditions.NotAccept, // 801
			AcceptConditions.NotAccept, // 802
			AcceptConditions.NotAccept, // 803
			AcceptConditions.Accept, // 804
			AcceptConditions.NotAccept, // 805
			AcceptConditions.Accept, // 806
			AcceptConditions.Accept, // 807
			AcceptConditions.NotAccept, // 808
			AcceptConditions.Accept, // 809
			AcceptConditions.NotAccept, // 810
			AcceptConditions.NotAccept, // 811
			AcceptConditions.Accept, // 812
			AcceptConditions.NotAccept, // 813
			AcceptConditions.Accept, // 814
			AcceptConditions.NotAccept, // 815
			AcceptConditions.NotAccept, // 816
			AcceptConditions.NotAccept, // 817
			AcceptConditions.Accept, // 818
			AcceptConditions.NotAccept, // 819
			AcceptConditions.Accept, // 820
			AcceptConditions.NotAccept, // 821
			AcceptConditions.NotAccept, // 822
			AcceptConditions.Accept, // 823
			AcceptConditions.NotAccept, // 824
			AcceptConditions.Accept, // 825
			AcceptConditions.NotAccept, // 826
			AcceptConditions.NotAccept, // 827
			AcceptConditions.Accept, // 828
			AcceptConditions.NotAccept, // 829
			AcceptConditions.Accept, // 830
			AcceptConditions.NotAccept, // 831
			AcceptConditions.NotAccept, // 832
			AcceptConditions.Accept, // 833
			AcceptConditions.Accept, // 834
			AcceptConditions.NotAccept, // 835
			AcceptConditions.NotAccept, // 836
			AcceptConditions.Accept, // 837
			AcceptConditions.NotAccept, // 838
			AcceptConditions.NotAccept, // 839
			AcceptConditions.Accept, // 840
			AcceptConditions.NotAccept, // 841
			AcceptConditions.Accept, // 842
			AcceptConditions.NotAccept, // 843
			AcceptConditions.Accept, // 844
			AcceptConditions.NotAccept, // 845
			AcceptConditions.Accept, // 846
			AcceptConditions.NotAccept, // 847
			AcceptConditions.NotAccept, // 848
			AcceptConditions.NotAccept, // 849
			AcceptConditions.NotAccept, // 850
			AcceptConditions.NotAccept, // 851
			AcceptConditions.NotAccept, // 852
			AcceptConditions.NotAccept, // 853
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
			AcceptConditions.NotAccept, // 875
			AcceptConditions.NotAccept, // 876
			AcceptConditions.NotAccept, // 877
			AcceptConditions.NotAccept, // 878
			AcceptConditions.NotAccept, // 879
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
			AcceptConditions.NotAccept, // 895
			AcceptConditions.NotAccept, // 896
			AcceptConditions.Accept, // 897
			AcceptConditions.Accept, // 898
			AcceptConditions.Accept, // 899
			AcceptConditions.Accept, // 900
			AcceptConditions.Accept, // 901
			AcceptConditions.Accept, // 902
			AcceptConditions.NotAccept, // 903
			AcceptConditions.NotAccept, // 904
			AcceptConditions.Accept, // 905
			AcceptConditions.Accept, // 906
			AcceptConditions.Accept, // 907
			AcceptConditions.NotAccept, // 908
			AcceptConditions.Accept, // 909
			AcceptConditions.NotAccept, // 910
			AcceptConditions.Accept, // 911
			AcceptConditions.NotAccept, // 912
			AcceptConditions.Accept, // 913
			AcceptConditions.NotAccept, // 914
			AcceptConditions.NotAccept, // 915
			AcceptConditions.NotAccept, // 916
			AcceptConditions.NotAccept, // 917
			AcceptConditions.NotAccept, // 918
			AcceptConditions.NotAccept, // 919
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
			AcceptConditions.NotAccept, // 932
			AcceptConditions.NotAccept, // 933
			AcceptConditions.NotAccept, // 934
			AcceptConditions.NotAccept, // 935
			AcceptConditions.NotAccept, // 936
			AcceptConditions.NotAccept, // 937
			AcceptConditions.NotAccept, // 938
			AcceptConditions.NotAccept, // 939
			AcceptConditions.NotAccept, // 940
			AcceptConditions.NotAccept, // 941
			AcceptConditions.NotAccept, // 942
			AcceptConditions.NotAccept, // 943
			AcceptConditions.NotAccept, // 944
			AcceptConditions.NotAccept, // 945
			AcceptConditions.NotAccept, // 946
			AcceptConditions.NotAccept, // 947
			AcceptConditions.Accept, // 948
			AcceptConditions.Accept, // 949
			AcceptConditions.Accept, // 950
			AcceptConditions.Accept, // 951
			AcceptConditions.NotAccept, // 952
			AcceptConditions.Accept, // 953
			AcceptConditions.NotAccept, // 954
			AcceptConditions.Accept, // 955
			AcceptConditions.Accept, // 956
			AcceptConditions.Accept, // 957
			AcceptConditions.Accept, // 958
			AcceptConditions.Accept, // 959
			AcceptConditions.NotAccept, // 960
			AcceptConditions.Accept, // 961
			AcceptConditions.Accept, // 962
			AcceptConditions.NotAccept, // 963
			AcceptConditions.Accept, // 964
			AcceptConditions.Accept, // 965
			AcceptConditions.NotAccept, // 966
			AcceptConditions.Accept, // 967
			AcceptConditions.NotAccept, // 968
			AcceptConditions.Accept, // 969
			AcceptConditions.NotAccept, // 970
			AcceptConditions.Accept, // 971
			AcceptConditions.NotAccept, // 972
			AcceptConditions.NotAccept, // 973
			AcceptConditions.Accept, // 974
			AcceptConditions.NotAccept, // 975
			AcceptConditions.NotAccept, // 976
			AcceptConditions.NotAccept, // 977
			AcceptConditions.Accept, // 978
			AcceptConditions.NotAccept, // 979
			AcceptConditions.Accept, // 980
			AcceptConditions.Accept, // 981
			AcceptConditions.Accept, // 982
			AcceptConditions.Accept, // 983
			AcceptConditions.Accept, // 984
			AcceptConditions.NotAccept, // 985
			AcceptConditions.Accept, // 986
			AcceptConditions.Accept, // 987
			AcceptConditions.Accept, // 988
			AcceptConditions.NotAccept, // 989
			AcceptConditions.Accept, // 990
			AcceptConditions.NotAccept, // 991
			AcceptConditions.Accept, // 992
			AcceptConditions.Accept, // 993
			AcceptConditions.Accept, // 994
			AcceptConditions.NotAccept, // 995
			AcceptConditions.Accept, // 996
			AcceptConditions.Accept, // 997
			AcceptConditions.Accept, // 998
			AcceptConditions.Accept, // 999
			AcceptConditions.Accept, // 1000
			AcceptConditions.NotAccept, // 1001
		};
		
		private static int[] colMap = new int[]
		{
			4, 5, 5, 5, 5, 5, 5, 5, 5, 4, 4, 5, 5, 4, 5, 5, 
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 
			1, 5, 5, 5, 5, 5, 5, 5, 6, 7, 5, 8, 3, 9, 2, 10, 
			11, 12, 13, 14, 15, 16, 17, 18, 19, 19, 20, 21, 5, 5, 5, 5, 
			22, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 23, 
			5, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 
			39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 40, 5, 5, 5, 5, 5, 
			5, 5
		};
		
		private static int[] rowMap = new int[]
		{
			0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 11, 13, 14, 
			15, 16, 17, 18, 19, 20, 21, 22, 23, 2, 24, 25, 2, 26, 2, 27, 
			21, 2, 21, 28, 29, 30, 1, 2, 31, 32, 33, 34, 35, 36, 2, 2, 
			21, 37, 38, 39, 40, 41, 42, 43, 2, 44, 45, 46, 23, 47, 48, 49, 
			50, 51, 52, 53, 54, 55, 2, 56, 57, 58, 2, 2, 2, 59, 60, 2, 
			61, 62, 63, 64, 10, 65, 66, 67, 68, 69, 70, 29, 71, 72, 73, 74, 
			75, 76, 77, 78, 79, 80, 81, 2, 82, 83, 84, 85, 86, 87, 88, 89, 
			2, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 101, 2, 102, 103, 
			104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 2, 114, 115, 116, 117, 118, 
			119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 2, 132, 133, 
			134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 
			150, 151, 152, 153, 154, 2, 155, 156, 157, 158, 2, 159, 160, 161, 162, 163, 
			164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 19, 176, 177, 178, 
			179, 180, 40, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 23, 191, 192, 
			193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 2, 203, 204, 173, 205, 206, 
			22, 207, 208, 209, 16, 210, 189, 211, 212, 213, 194, 214, 215, 216, 217, 218, 
			219, 220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 230, 231, 232, 192, 233, 
			234, 235, 236, 237, 238, 239, 224, 240, 227, 241, 242, 243, 244, 245, 246, 247, 
			248, 249, 250, 251, 252, 253, 254, 255, 49, 256, 257, 258, 259, 260, 261, 262, 
			263, 264, 265, 266, 267, 268, 269, 270, 271, 272, 273, 274, 275, 276, 277, 278, 
			279, 280, 281, 282, 283, 284, 285, 286, 287, 288, 289, 290, 291, 292, 293, 294, 
			295, 296, 297, 298, 299, 300, 301, 302, 303, 304, 305, 306, 307, 308, 309, 310, 
			311, 312, 313, 314, 315, 316, 317, 318, 319, 320, 321, 322, 323, 324, 325, 326, 
			327, 328, 329, 330, 331, 332, 333, 334, 335, 336, 337, 338, 339, 340, 341, 342, 
			343, 344, 345, 346, 347, 21, 348, 349, 350, 351, 352, 353, 354, 355, 356, 357, 
			358, 359, 360, 361, 362, 363, 364, 365, 366, 367, 368, 369, 370, 371, 372, 373, 
			374, 375, 376, 377, 378, 379, 380, 381, 382, 383, 384, 385, 386, 387, 388, 389, 
			390, 391, 392, 393, 394, 395, 396, 397, 398, 399, 400, 401, 402, 403, 404, 405, 
			406, 407, 408, 409, 410, 411, 412, 413, 414, 415, 416, 417, 418, 419, 420, 421, 
			422, 423, 229, 424, 425, 426, 427, 428, 429, 430, 431, 432, 433, 434, 435, 436, 
			437, 438, 439, 440, 441, 442, 443, 444, 445, 446, 447, 448, 449, 450, 451, 452, 
			453, 454, 455, 456, 457, 458, 459, 460, 461, 462, 463, 464, 465, 466, 467, 468, 
			469, 470, 471, 472, 473, 474, 475, 476, 477, 399, 478, 479, 480, 481, 482, 483, 
			484, 154, 485, 486, 487, 488, 489, 490, 491, 492, 493, 494, 495, 496, 497, 498, 
			499, 500, 501, 502, 503, 504, 505, 506, 507, 508, 509, 510, 511, 135, 512, 513, 
			514, 515, 516, 517, 518, 519, 520, 521, 522, 523, 113, 524, 525, 526, 527, 528, 
			529, 530, 531, 532, 533, 534, 535, 536, 537, 538, 501, 539, 512, 540, 541, 542, 
			543, 544, 545, 546, 547, 548, 549, 550, 551, 552, 553, 29, 45, 554, 555, 545, 
			556, 557, 558, 559, 560, 561, 562, 563, 564, 565, 566, 567, 568, 569, 570, 571, 
			572, 573, 574, 575, 576, 577, 578, 579, 580, 581, 582, 583, 584, 585, 586, 587, 
			588, 589, 590, 591, 592, 593, 594, 595, 596, 597, 598, 599, 600, 601, 602, 603, 
			506, 604, 605, 606, 607, 608, 609, 610, 611, 575, 208, 612, 613, 614, 615, 616, 
			617, 618, 619, 620, 621, 622, 623, 624, 625, 626, 627, 628, 629, 630, 631, 632, 
			633, 199, 634, 635, 636, 446, 637, 638, 639, 640, 641, 642, 643, 448, 644, 645, 
			646, 647, 199, 648, 446, 649, 650, 651, 652, 498, 653, 654, 499, 655, 656, 532, 
			657, 658, 659, 660, 661, 662, 663, 664, 665, 666, 667, 668, 669, 670, 671, 672, 
			673, 674, 675, 676, 677, 678, 679, 187, 680, 681, 682, 683, 684, 304, 22, 685, 
			686, 687, 442, 688, 689, 690, 691, 692, 693, 140, 694, 695, 696, 697, 698, 194, 
			699, 700, 701, 167, 702, 703, 704, 705, 706, 707, 708, 709, 710, 711, 712, 713, 
			675, 714, 678, 715, 716, 717, 718, 719, 720, 721, 722, 723, 724, 725, 726, 727, 
			728, 729, 730, 731, 732, 733, 734, 227, 735, 736, 737, 738, 739, 740, 741, 742, 
			743, 744, 745, 746, 747, 748, 749, 750, 751, 752, 753, 754, 755, 756, 11, 757, 
			758, 759, 760, 761, 762, 763, 764, 765, 766, 767, 768, 769, 770, 771, 772, 773, 
			774, 775, 776, 777, 778, 779, 780, 665, 781, 782, 783, 784, 785, 786, 787, 788, 
			789, 790, 791, 792, 793, 679, 794, 795, 702, 796, 797, 798, 799, 800, 801, 802, 
			803, 804, 805, 806, 807, 808, 809, 810, 811, 812, 813, 814, 815, 816, 817, 818, 
			819, 820, 821, 822, 823, 824, 825, 826, 827, 828, 829, 830, 831, 832, 833, 834, 
			835, 836, 837, 838, 839, 840, 43, 841, 842, 843, 844, 845, 846, 847, 848, 849, 
			850, 851, 852, 853, 854, 855, 856, 857, 858, 859, 860, 861, 862, 863, 864, 865, 
			866, 867, 868, 869, 870, 871, 872, 873, 874, 875, 876, 877, 878, 879, 880, 881, 
			882, 883, 884, 885, 886, 887, 888, 889, 890, 891, 892, 893, 812, 894, 895, 896, 
			897, 898, 899, 900, 901, 902, 21, 903, 904, 905, 906, 907, 908, 909, 910, 911, 
			912, 913, 914, 915, 916, 917, 918, 919, 920, 921
		};
		
		private static int[,] nextState = new int[,]
		{
			{ -1, 1, 39, 39, 39, 2, 40, 2, 61, 61, 2, 78, 93, 637, 733, 763, 763, 763, 763, 763, 2, 2, 107, 2, 3, 618, 618, 726, 880, 760, 618, 618, 41, 780, 618, 897, 793, 804, 812, 996, 618, 618, 818, 823, 618, 623, 828, 881, 999 },
			{ -1, 38, -1, -1, -1, -1, -1, -1, -1, -1, -1, 60, 60, 60, 60, 60, 60, 60, 60, 60, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 94, 94, 94, 94, 94, 94, 108, 94, 94, 94, 94, 94, 94, 94, 94, 638, 94, 94, 94, 94, 734, 94, 94, 94, 94 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 296, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 624, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 63, -1, 63, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 5, 5, 5, 5, 5, 5, 5, 5, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 761, 761, 761, -1, -1, -1, -1, -1, -1, -1, 43, 43, 43, 43, 43, 43, 43, 43, 43, -1, -1, -1, -1, -1, -1, -1, 761, -1, -1, -1, 761, -1, -1, -1, -1, -1, 64, -1, -1, -1, 64, 81, 96, -1, -1, -1, -1, -1 },
			{ -1, 319, 320, -1, -1, -1, 321, -1, 322, 876, -1, 44, 44, 44, 44, 44, 44, 44, 44, 44, 323, -1, -1, -1, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 327, 66, 66, 66, 66, 66, 66, 66, 66, 66, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 328, -1, -1, -1, 328, 329, 330, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 855, 855, 855, 855, 855, 855, 855, 855, 855, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 47, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 789, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, 359, 360, -1, -1, -1, -1, -1, -1, 361, 362, 630, 731, 762, 781, 794, 794, 805, 60, 60, -1, -1, -1, -1, 690, -1, -1, 363, -1, 364, -1, 190, 19, 692, -1, -1, 365, 694, 366, -1, -1, -1, 367, 672, -1, 51, 368, 861, 673 },
			{ -1, 359, 360, -1, -1, -1, -1, -1, -1, 361, 362, 813, 819, 824, 829, 60, 60, 60, 60, 60, -1, -1, -1, -1, 690, -1, -1, 363, -1, 364, -1, 190, 19, 692, -1, -1, 365, 694, 366, -1, -1, -1, 367, 672, -1, 51, 368, 861, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 157, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 633, 633, 633, 633, 633, 633, 633, 633, 633, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 399, -1, -1, -1, 399, 400, 401, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 626, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, -1, 70, -1 },
			{ -1, -1, 356, -1, -1, -1, -1, -1, -1, -1, -1, 665, 665, 665, 665, 665, 665, 665, 665, 665, 356, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 448, 452, -1, -1, -1, 450, -1, 451, 451, -1, 53, 53, 53, 53, 53, 53, 53, 53, 53, 452, -1, -1, -1, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 466, 466, 466, 466, 466, 466, 466, 466, 466, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 60, 60, 60, 60, 60, 60, 60, 60, 60, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 30, 30, 30, 30, 30, 30, 30, 30, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 982, 389, 389, 389, 389, 389, 389 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 60, 60, 60, 60, 60, 60, 60, 60, 60, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 541, -1, -1, 238, -1, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 858, 858, 858, 858, 858, 858, 858, 858, 858, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1, -1, -1, 535, 536, 583, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 75, 75, 75, 75, 75, 75, 75, 75, 75, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 729, 729, 729, 729, 729, 729, 729, 729, 729, 59, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 833, 833, 833, 833, 833, 833, 833, 833, 833, 833, 833, 833, 833, 833, 833, 833, 833, 833, 833, 833, 833, 833, 833, 833, 833 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 271, -1, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, -1, 94, 94, 94, 94, 94, 94, 94, 94, 131, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 142, 94, 142, 94 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 296, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 859, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 761, 122, 761, -1, -1, -1, -1, -1, -1, -1, 133, 133, 144, 155, 155, 155, 155, 155, 155, 356, -1, -1, -1, -1, -1, -1, 761, -1, -1, -1, 761, -1, -1, -1, -1, -1, 64, -1, -1, -1, 64, 81, 96, -1, -1, -1, -1, -1 },
			{ -1, 319, 320, -1, -1, -1, 321, -1, 322, 876, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 323, -1, -1, -1, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 327, 66, 66, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 328, -1, -1, -1, 328, 329, 330, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 693, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 633, 633, 750, 750, 750, 750, 750, 750, 750, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 399, -1, -1, -1, 399, 400, 401, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 861, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 665, 665, 665, 665, 665, 665, 665, 665, 665, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 448, 452, -1, -1, -1, 450, -1, 451, 451, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 452, -1, -1, -1, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 60, 29, 29, 29, 29, 29, 29, 29, 29, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 850, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 74, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 75, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 629, 629, 629, 629, 629, 629, 76, 76, 76, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 38, -1, -1, -1, -1, -1, -1, -1, -1, -1, 62, 62, 631, 732, 732, 732, 732, 732, 732, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 732, 732, 732, 732, 732, 732, 732, 732, 732, 209, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 296, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 761, 761, 761, -1, -1, -1, -1, -1, -1, -1, 765, 765, 783, 796, 796, 796, 796, 796, 796, -1, -1, -1, -1, -1, -1, -1, 166, -1, -1, -1, 761, -1, -1, -1, -1, -1, 761, -1, -1, -1, 761, 761, 761, -1, -1, -1, -1, -1 },
			{ -1, 319, 323, -1, -1, -1, 321, -1, 322, 322, -1, 625, 625, 625, 625, 625, 625, 625, 625, 625, 323, -1, -1, -1, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 327, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 328, -1, -1, -1, 328, 329, 330, -1, -1, -1, -1, -1 },
			{ -1, -1, 47, -1, -1, -1, -1, 186, -1, -1, 398, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 398, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 157, -1, 375, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 750, 750, 750, 750, 750, 750, 750, 750, 750, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 399, -1, -1, -1, 399, 400, 401, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 138, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 149, 149, 149, 149, 149, 149, 149, 149, 149, 149, 149, 149, 149, 149, 149, 149, 149, 149, 149, 149, 149, 149, 149, 149, 149 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 29, 29, 29, 29, 29, 29, 29, 29, 29, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 181, -1, -1, -1, -1, -1, -1 },
			{ -1, 77, 282, -1, -1, -1, -1, -1, -1, 282, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 163, -1, -1, 173, -1, 183, -1, 190, 4, 196, -1, -1, 202, 657, 214, -1, -1, -1, 680, 672, -1, 42, 238, 859, 673 },
			{ -1, 77, 92, -1, -1, -1, -1, -1, -1, 106, 119, 130, 141, 141, 141, 141, 141, 141, 141, 141, 152, -1, -1, -1, 163, -1, -1, 173, -1, 183, -1, 190, 4, 196, -1, -1, 202, 208, 214, -1, -1, 220, 226, 232, -1, 42, 238, 859, 673 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 296, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 761, 761, 761, -1, -1, -1, -1, -1, -1, -1, 765, 765, 783, 796, 796, 796, 796, 796, 796, -1, -1, -1, -1, -1, -1, -1, 761, -1, -1, -1, 761, -1, -1, -1, -1, -1, 761, -1, -1, -1, 761, 761, 166, -1, -1, -1, -1, -1 },
			{ -1, 319, 683, -1, -1, -1, 321, -1, 322, 876, -1, 111, 111, 111, 111, 111, 111, 111, 111, 111, 684, -1, -1, -1, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 864, 864, 864, 864, 864, 864, 864, 864, 864, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 693, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 376, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 114, 114, 114, 114, 114, 114, 114, 114, 114, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 399, -1, -1, -1, 399, 400, 401, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 516, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 525, 452, -1, -1, -1, 526, -1, 527, 527, -1, 647, 647, 647, 647, 647, 647, 647, 647, 647, 452, -1, -1, -1, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 29, 29, 60, 60, 60, 60, 60, 60, 60, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 719, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 282, 282, -1, -1, -1, -1, -1, -1, 282, -1, 7, 854, 885, 885, 885, 885, 44, 44, 44, -1, -1, -1, -1, 163, -1, -1, 746, -1, 774, -1, -1, 4, 196, -1, -1, 679, 657, 214, -1, -1, -1, 788, -1, -1, 42, -1, 859, -1 },
			{ -1, 243, 247, -1, -1, -1, -1, -1, -1, 106, 119, 141, 141, 141, 251, 251, 251, 251, 251, 251, 255, -1, -1, -1, 259, -1, -1, 173, -1, 183, -1, 190, 4, 196, -1, -1, 202, 208, 214, 262, -1, 220, 226, 232, -1, 42, 238, 859, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 296, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 370, -1, -1, -1, -1 },
			{ -1, 761, 761, 761, -1, -1, -1, -1, -1, -1, -1, 765, 765, 783, 796, 796, 796, 796, 796, 796, -1, -1, -1, -1, -1, -1, -1, 761, -1, -1, -1, 166, -1, -1, -1, -1, -1, 761, -1, -1, -1, 761, 761, 761, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 327, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 379, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 114, 114, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 399, -1, -1, -1, 399, 400, 401, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 517, -1, -1, -1, -1 },
			{ -1, 525, 452, -1, -1, -1, 526, -1, 527, 527, -1, 656, -1, -1, -1, -1, -1, -1, -1, -1, 452, -1, -1, -1, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 181, -1, 554, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 129, 129, 129, 129, 129, 129, 129, 129, 129, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 282, 282, -1, -1, -1, -1, -1, -1, 282, -1, 283, 284, 285, 285, 285, 285, 285, 285, 285, -1, -1, -1, -1, 975, -1, -1, 945, -1, 946, -1, -1, 4, 976, -1, -1, 944, 943, 977, -1, -1, -1, 947, -1, -1, 42, -1, 859, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 269, -1, -1, 5, 5, 5, 5, 5, 5, 5, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 11, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 296, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, 371, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 356, -1, -1, -1, -1, -1, -1, -1, -1, 652, 652, 652, 652, 652, 176, 176, 176, 176, 356, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 382, 683, -1, -1, -1, 321, -1, 322, 876, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 684, -1, -1, -1, 199, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 199, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 380, -1, -1, -1, 157, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 399, -1, -1, -1, 399, 400, 401, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 518, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 525, 452, -1, -1, -1, 526, -1, 527, 527, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 452, -1, -1, -1, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 719, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 555, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 129, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 8, 8, 8, 45, 66, 66, 66, 66, 66, -1, -1, -1, -1, 654, -1, -1, 799, -1, 808, -1, -1, -1, 661, -1, -1, 748, 740, 667, -1, -1, -1, 815, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 12, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 296, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 372, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 761, 761, 761, -1, -1, -1, -1, -1, -1, -1, 216, 216, 871, 892, 892, 892, 222, 222, 222, -1, -1, -1, -1, -1, -1, -1, 761, -1, -1, -1, 761, -1, -1, -1, -1, -1, 761, -1, -1, -1, 761, 761, 761, -1, -1, -1, -1, -1 },
			{ -1, 394, 395, -1, -1, -1, 396, -1, 397, 397, -1, 205, 205, 205, 205, 205, 205, 205, 205, 205, -1, -1, -1, -1, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 696, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 715, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 737, 737, 737, 737, 737, 737, 737, 737, 737, 201, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 720, -1, -1, -1, -1 },
			{ -1, -1, 615, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 77, 92, -1, -1, -1, -1, -1, -1, 106, 119, 286, 286, 286, 286, 286, 286, 287, 287, 287, 152, -1, -1, -1, 163, -1, -1, 173, -1, 183, -1, 190, 4, 196, -1, -1, 202, 208, 214, -1, -1, 220, 226, 232, -1, 42, 238, 859, 673 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 271, 316, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 670, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 296, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 63 },
			{ -1, -1, 356, -1, -1, -1, -1, -1, -1, -1, -1, 20, 20, 20, 20, 20, 20, 20, 20, 20, 356, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 394, 395, -1, -1, -1, 396, -1, 397, 397, -1, 205, 23, 23, 23, 23, 23, 23, 23, 23, -1, -1, -1, -1, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 381, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 645, 645, 645, 645, 645, 645, 645, 645, 645, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1, -1, -1, 535, 536, 537, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1, -1, -1, 181, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 617, 617, -1, 140, 140, 140, 140, 140, 140, 140, 140, 140, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 243, 247, -1, -1, -1, -1, -1, -1, 106, 119, 286, 286, 286, 286, 286, 286, 287, 287, 287, 255, -1, -1, -1, 259, -1, -1, 173, -1, 183, -1, 190, 4, 196, -1, -1, 202, 208, 214, 262, -1, 220, 226, 232, -1, 42, 238, 859, 673 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 271, 316, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 296, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, 63, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 356, -1, -1, -1, -1, -1, -1, -1, -1, 20, 20, 20, 20, 20, 52, 52, 52, 52, 356, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 394, 395, -1, -1, -1, 396, -1, 397, 397, -1, 23, 23, 23, 23, 23, 23, 23, 23, 23, -1, -1, -1, -1, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 777, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 645, 645, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1, -1, -1, 535, 536, 537, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 138, -1, -1, 528, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 528, 651, 651, 651, 651, 651, 651, 651, 651, 651, 651, 651, 651, 651, 651, 651, 651, 651, 651, 651, 651, 651, 651, 651, 651, 651 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 557, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 172, 172, 172, 172, 172, 172, 172, 172, 172, 182, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 65, 65, 65, 65, 65, 65, 625, 625, 625, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 12, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 296, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, 374, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 356, -1, -1, -1, -1, -1, -1, -1, -1, 52, 52, 52, 52, 52, 52, 52, 52, 52, 356, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 739, 739, 739, 739, 739, 739, 739, 739, 739, 223, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 645, 35, 35, 35, 35, 35, 35, 35, 35, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1, -1, -1, 535, 536, 537, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 519, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 529, -1, -1, -1, -1, 717, -1, 718, 718, -1, 656, 656, 656, 656, 656, 656, 656, 656, 656, -1, -1, -1, -1, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 172, 172, 172, 172, 172, 91, 75, 75, 75, 182, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 288, -1, -1, -1, -1, 289, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 233, 830, 830, 830, 830, 830, 830, 893, 13, 830, 830 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 296, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 378, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 761, 761, 761, -1, -1, -1, -1, -1, -1, -1, 765, 765, 783, 796, 796, 796, 796, 796, 796, -1, -1, -1, -1, -1, -1, -1, 761, -1, -1, -1, 761, -1, -1, -1, -1, -1, 761, -1, -1, -1, 761, 761, 761, -1, -1, -1, -1, -1 },
			{ -1, 394, 395, -1, -1, -1, 396, -1, 397, 397, -1, 177, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 35, 35, 35, 35, 35, 35, 35, 35, 35, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1, -1, -1, 535, 536, 537, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 713, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 529, -1, -1, -1, -1, 717, -1, 718, 718, -1, 656, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 721, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 91, 91, 91, 91, 91, 91, 75, 75, 75, 182, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 290, -1, -1, -1, 291, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 856, 830, 830, 830, 830, 830 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 296, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 642, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 627, 627, 627, 627, 627, 627, 627, 627, 627, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 394, 395, -1, -1, -1, 396, -1, 397, 397, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 35, 35, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1, -1, -1, 535, 536, 537, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 646, -1, -1, -1, -1, -1 },
			{ -1, 529, -1, -1, -1, -1, 717, -1, 718, 718, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 91, 91, 91, 91, 91, 91, 75, 75, 75, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 292, -1, -1, -1, -1, -1, -1, -1, -1, -1, 293, -1, -1, 294, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 844, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 949, 830, 830, 830, 830 },
			{ -1, 761, 761, 761, -1, -1, -1, -1, -1, -1, -1, 43, 228, 228, 228, 228, 228, 228, 228, 228, -1, -1, -1, -1, -1, -1, -1, 761, -1, -1, -1, 761, -1, -1, -1, -1, -1, 64, -1, -1, -1, 64, 81, 96, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 582, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 516, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 138, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 738, 738, 738, 738, 738, 738, 738, 738, 738, 738, 738, 738, 738, 738, 738, 738, 738, 738, 738, 738, 738, 738, 738, 738, 738 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 295, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 887, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, 761, 761, 761, -1, -1, -1, -1, -1, -1, -1, 228, 228, 228, 228, 228, 228, 228, 228, 228, -1, -1, -1, -1, -1, -1, -1, 761, -1, -1, -1, 761, -1, -1, -1, -1, -1, 64, -1, -1, -1, 64, 81, 96, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, 398, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 398, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658, 658 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 517, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 138, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 660, 660, 660, 660, 660, 660, 660, 660, 660, 660, 660, 660, 660, 660, 660, 660, 660, 660, 660, 660, 660, 660, 660, 660, 660 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 297, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 298, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 956, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, 761, 761, 761, -1, -1, -1, -1, -1, -1, -1, 228, 228, 110, 643, 643, 643, 643, 643, 643, -1, -1, -1, -1, -1, -1, -1, 761, -1, -1, -1, 761, -1, -1, -1, -1, -1, 64, -1, -1, -1, 64, 81, 96, -1, -1, -1, -1, -1 },
			{ -1, -1, 445, -1, -1, -1, -1, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 67, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 518, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 207, 207, 207, 207, 207, 207, 138, 138, 138, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 299, -1, -1, -1, -1, -1, -1, -1, 300, -1, -1, -1, -1, -1, 301, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 664, 664, 664, 664, 664, 664, 664, 664, 664, 664, 664, 664, 664, 664, 664, 664, 664, 664, 664, 664, 664, 664, 664, 664, 664 },
			{ -1, 394, 395, -1, -1, -1, 396, -1, 397, 397, -1, 466, 466, 466, 466, 466, 466, 466, 466, 466, -1, -1, -1, -1, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 715, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 138, 138, 138, 138, 138, 138, 138, 138, 138, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 302, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 303, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 239, 239, 239, 239, 239, 239, 79, 79, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 225, 225, 225, 225, 225, 225, 225, 225, 225, 237, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 304, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 221, 221, 221, 221, 221, 221, 221, 221, 221, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, 448, 449, -1, -1, -1, 450, -1, 451, 451, -1, 669, 669, 669, 669, 669, 669, 669, 669, 669, 449, -1, -1, -1, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71 },
			{ -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 668, 668, 668, 668, 668, 668, 668, 668, 668, 668, 668, 668, 668, 668, 668, 668, 668, 668, 668, 668, 668, 668, 668, 668, 668 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 767, 767, 767, 767, 767, 207, 138, 138, 138, 237, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 302, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 448, 449, -1, -1, -1, 450, -1, 451, 451, -1, 210, 210, 210, 210, 210, 210, 210, 210, 210, 449, -1, -1, -1, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 229, 229, 229, 229, 229, 229, 186, 186, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 519, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 207, 207, 207, 207, 207, 207, 138, 138, 138, 237, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 305, -1, -1, -1, 306, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 302, 307, -1, -1, -1, -1 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 355, 316, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 674, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, 761, 122, 761, -1, -1, -1, -1, -1, 494, -1, 133, 133, 144, 155, 155, 155, 155, 155, 155, 356, -1, -1, -1, -1, -1, -1, 761, -1, -1, -1, 761, -1, -1, -1, -1, -1, 64, -1, -1, -1, 64, 81, 96, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 186, 186, 186, 186, 186, 186, 186, 186, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 138, -1, -1, 528, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 528, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 308, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 309, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 17, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 266, 266, 266, 266, 266, 266, 266, 266, 266, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 713, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 310, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 79, 79, 79, 79, 79, 79, 79, 79, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 663, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 250, 250, 250, 250, 250, 250, 250, 250, 250, 237, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 243, 282, -1, -1, -1, -1, -1, -1, 282, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 259, -1, -1, 173, -1, 183, -1, 190, 4, 196, -1, -1, 202, 657, 214, 262, -1, -1, 680, 672, -1, 42, 238, 859, 673 },
			{ -1, 492, 395, -1, -1, -1, 396, -1, 397, 397, -1, 466, 466, 466, 466, 466, 466, 466, 466, 466, -1, -1, -1, -1, 662, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 662, 211, 211, 211, 211, 211, 211, 211, 211, 211 },
			{ -1, 282, 282, -1, -1, -1, -1, -1, -1, 282, -1, 82, 863, 890, 890, 890, 890, 860, 860, 860, -1, -1, -1, -1, 163, -1, -1, 746, -1, 774, -1, -1, 4, 196, -1, -1, 679, 657, 214, -1, -1, -1, 788, -1, -1, 42, -1, 859, -1 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 271, 316, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, 492, 395, -1, -1, -1, 396, -1, 397, 397, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 662, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 662, 211, 211, 211, 211, 211, 211, 211, 211, 211 },
			{ -1, 77, 92, -1, -1, -1, -1, -1, -1, 106, 312, 286, 286, 286, 286, 286, 286, 287, 287, 287, 152, -1, -1, -1, 163, -1, -1, 173, -1, 183, -1, 190, 4, 196, -1, -1, 202, 208, 214, -1, -1, 220, 226, 232, -1, 42, 238, 859, 673 },
			{ -1, 393, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 778, 389, 791, 389, 896, 389, 389, 389, 389, 802, 389, 389, 389, 389, 389, 811, 817, 389, 389, 822, 389, 904 },
			{ -1, 511, -1, -1, -1, -1, 703, -1, 512, 512, -1, 253, 253, 253, 253, 253, 253, 253, 253, 253, -1, -1, -1, -1, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870 },
			{ -1, -1, -1, -1, -1, -1, -1, 138, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 635, 635, 635, 635, 635, 635, 730, 730, 730, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 22 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 264, 264, 264, 264, 264, 264, 264, 264, 264, 270, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 528, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 528, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258 },
			{ -1, -1, 313, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, -1, -1, 288, -1, -1, -1, -1, 289, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 807, 807, 807, 807, 807, 229, 186, 186, 186, 270, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 313, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 229, 229, 229, 229, 229, 229, 186, 186, 186, 270, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 77, 314, -1, -1, -1, -1, -1, -1, 106, 312, 287, 287, 287, 287, 287, 287, 287, 287, 287, -1, -1, -1, -1, 163, -1, -1, 173, -1, 183, -1, 190, 4, 196, -1, -1, 202, 208, 214, -1, -1, 220, 226, 232, -1, 42, 238, 859, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 28, 28, 28, 28, 28, 28, 28, 28, 28, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, 315, -1, 287, 287, 287, 287, 287, 287, 287, 287, 287, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, 398, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 398, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 5, 5, 5, 5, 5, 5, 5, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 271, 271, -1, -1, -1, -1, -1, -1, 271, -1, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 398, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 398, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 290, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 280, 280, 280, 280, 280, 280, 280, 280, 270, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 293, -1, -1, 294, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 300, -1, -1, -1, -1, -1, 301, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 305, -1, -1, -1, 317, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 307, -1, -1, -1, -1 },
			{ -1, 282, 282, -1, -1, -1, -1, -1, -1, 282, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 163, -1, -1, 746, -1, 774, -1, -1, 4, 196, -1, -1, 679, 657, 214, -1, -1, -1, 788, -1, -1, 42, -1, 859, -1 },
			{ -1, -1, 325, -1, -1, -1, -1, -1, -1, 326, -1, 285, 285, 285, 285, 285, 285, 285, 285, 285, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 325, -1, -1, -1, -1, -1, -1, 326, -1, 285, 285, 285, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 325, -1, -1, -1, -1, -1, -1, 326, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, 315, -1, 14, 14, 14, 14, 14, 14, 14, 14, 14, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, 315, -1, 15, 15, 15, 15, 15, 15, 15, 15, 15, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 80, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 95, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 16 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 109, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 121, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 687, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 49, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 340, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 639, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 132, -1, 143, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 154, -1, -1, -1, -1, -1, -1, 63 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 68, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 85, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 282, 314, -1, -1, -1, -1, -1, -1, 341, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 163, -1, -1, 746, -1, 774, -1, -1, 4, 196, -1, -1, 679, 657, 214, -1, -1, -1, 788, -1, -1, 42, -1, 859, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 109, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 165, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 99, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 113, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 175, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 49, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 282, 314, -1, -1, -1, -1, -1, -1, 341, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 163, -1, -1, 746, -1, 774, -1, -1, 4, 196, -1, -1, 679, 657, 214, -1, -1, -1, 788, -1, 124, 42, -1, 859, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 135, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 146, 342, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 340, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 654, -1, -1, 799, -1, 808, -1, -1, -1, 661, -1, -1, 748, 740, 667, -1, -1, -1, 815, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 282, 282, -1, -1, -1, -1, -1, -1, 282, -1, 343, 344, 345, 345, 345, 345, 345, 345, 345, -1, -1, -1, -1, 163, -1, -1, 746, -1, 774, -1, -1, 4, 196, -1, -1, 679, 657, 214, -1, -1, -1, 788, -1, -1, 42, -1, 859, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 346, 347, 348, 348, 348, 348, 348, 348, 348, -1, -1, -1, -1, 349, -1, -1, 350, -1, 691, -1, -1, -1, 351, -1, -1, 352, 353, 686, -1, -1, -1, 751, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 354, 354, 354, 354, 354, 354, 354, 354, 354, 354, 354, 354, 354, 354, 354, 354, 354, 354, 354, 354, 354, 354, 354, 354, 354 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 113, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 124, -1, -1, -1, -1 },
			{ -1, 319, -1, -1, -1, -1, 321, -1, 322, 322, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 123, 123, 123, 123, 123, 123, 134, 145, 145, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 640, 640, 640, 640, 640, 640, 640, 640, 640, 640, 640, 640, 640, 640, 640, 640, 640, 640, 640, 640, 640, 640, 640, 640, 640 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 156, 156, 653, 739, 739, 739, 739, 739, 739, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 648, 648, 648, 648, 648, 648, 167, 177, 177, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 175, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 877, 877, 877, 877, 877, 877, 877, 877, 877, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 18, 18, 18, 50, 69, 69, 69, 69, 69, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 83, 83, 83, 83, 83, 83, 83, 83, 83, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 98, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 98, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 98, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 357, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 357, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 357, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 357, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 357, -1, 357, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 357, -1, -1, -1, -1, -1, -1, 357 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 357, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 357, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 358, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 16, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 282, 282, -1, -1, -1, -1, -1, -1, 282, -1, 377, 688, 681, 681, 681, 681, 681, 681, 681, -1, -1, -1, -1, 163, -1, -1, 746, -1, 774, -1, -1, 4, 196, -1, -1, 679, 657, 214, -1, -1, -1, 788, -1, -1, 42, -1, 859, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 16, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 685, -1, -1, -1, -1, -1, -1, 325, -1, 345, 345, 345, 345, 345, 345, 345, 345, 345, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 685, -1, -1, -1, -1, -1, -1, 325, -1, 749, 749, 749, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 685, -1, -1, -1, -1, -1, -1, 325, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 383, -1, 348, 348, 348, 348, 348, 348, 348, 348, 348, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 383, -1, 348, 348, 348, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 383, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 699, -1, -1, -1, -1, 384, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 695, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 700, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 385, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 386, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 387, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263 },
			{ -1, 271, 271, -1, -1, -1, -1, -1, -1, 271, -1, 185, 192, 192, 198, 727, 727, 727, 727, 727, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 21, 21, 21, 21, 21, 21, 53, 53, 53, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 895, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 895, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 357, -1, -1, -1, -1, -1 },
			{ -1, 359, 402, -1, -1, -1, -1, -1, -1, 402, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 690, -1, -1, 363, -1, 364, -1, 190, 19, 692, -1, -1, 365, 694, 366, -1, -1, -1, 367, 672, -1, 51, 238, 861, 673 },
			{ -1, 402, 402, -1, -1, -1, -1, -1, -1, 402, -1, 776, 790, 790, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, 690, -1, -1, 404, -1, 704, -1, -1, 19, 692, -1, -1, 405, 694, 366, -1, -1, -1, 757, -1, -1, 51, -1, 861, -1 },
			{ -1, 402, 402, -1, -1, -1, -1, -1, -1, 402, -1, 406, 407, 408, 408, 408, 408, 408, 408, 408, -1, -1, -1, -1, 409, -1, -1, 779, -1, 792, -1, -1, 19, 410, -1, -1, 705, 411, 753, -1, -1, -1, 803, -1, -1, 51, -1, 861, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 412, 413, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 290, -1, -1, -1, 421, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 422, -1, -1, -1, -1, -1, -1, -1, -1, -1, 293, -1, -1, 294, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 425, -1, -1, -1, -1, -1, -1, -1, 300, -1, -1, -1, -1, -1, 301, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 427, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 305, -1, -1, -1, 428, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 307, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 429, 430, 430, 430, 430, 431, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 310, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 63, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 436, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 378, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 437, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 755, -1, -1, -1, -1, -1, 438, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 63, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 440, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 16, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 325, -1, -1, -1, -1, -1, -1, 325, -1, 681, 681, 681, 681, 681, 681, 681, 681, 681, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 441, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 701, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 442, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 708, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 382, -1, -1, -1, -1, 321, -1, 322, 322, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 199, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 199, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 86, 86, 86, 100, 114, 114, 114, 114, 114, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 706, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 706, -1, 706, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 706, -1, -1, -1, -1, -1, -1, 706 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 706, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 447, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 271, 271, -1, -1, -1, -1, -1, -1, 271, 316, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, 393, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 778, 389, 791, 389, 896, 389, 389, 389, 389, 802, 389, 389, 389, 389, 389, 811, 817, 389, 389, 822, 389, 904 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 48 },
			{ -1, 393, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 758, -1, 453, -1, 707, -1, -1, -1, -1, 454, -1, -1, -1, -1, -1, 455, 456, -1, -1, 709, -1, 759 },
			{ -1, 394, -1, -1, -1, -1, 396, -1, 397, 397, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 253, 253, 253, 253, 253, 253, 253, 253, 253, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872, 872 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 257, 257, 261, 264, 264, 264, 264, 264, 264, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467, 467 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 125, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 125, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 125, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 402, 402, -1, -1, -1, -1, -1, -1, 402, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 690, -1, -1, 404, -1, 704, -1, -1, 19, 692, -1, -1, 405, 694, 366, -1, -1, -1, 757, -1, -1, 51, -1, 861, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 469, 469, 469, 469, 469, 469, 470, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 421, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 425, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 472, -1, 408, 473, 473, 473, 473, 473, 473, 473, 473, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 472, -1, 473, 473, 473, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 472, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 474, -1, -1, -1, -1, 475, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 478, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 479, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 481, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 484, 484, 484, 484, 484, 484, 484, 484, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 484, 484, 484, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 60, 24, 24, 24, 24, 24, 24, 24, 24, -1, 25, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 54, 72, 72, 89, 24, 24, 24, 24, 24, -1, 25, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 24, 24, 24, 24, 24, 24, 24, 24, 24, -1, 25, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 24, 24, 24, 24, 24, 24, 24, 60, 60, -1, 25, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 60, 60, 60, 60, 60, 60, 60, 60, 60, -1, 25, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 87, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 101, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 115, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 126, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 641, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 137, -1, 148, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 159, -1, -1, -1, -1, -1, -1, 70 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 115, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 169, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 113, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 179, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 26, 26, 26, 26, 26, 26, 26, 26, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 26, 26, 26, 26, 26, 26, 26, 26, 26, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 26, 26, 26, 26, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 60, 24, 24, 24, 24, 24, 24, 24, 24, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 54, 72, 72, 89, 24, 24, 24, 24, 24, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 24, 24, 24, 24, 24, 24, 24, 24, 24, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 24, 24, 24, 24, 24, 24, 24, 60, 60, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 63, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 485, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 486, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 157 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 16, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 488, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 16, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 490, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 491, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 84, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 23, 23, 23, 23, 23, 23, 23, 23, 23, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 689, -1, -1, -1, -1, -1 },
			{ -1, 448, -1, -1, -1, -1, 450, -1, 451, 451, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 88, 88, 88, 88, 88, 88, 102, 116, 116, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 634, 634, 634, 634, 634, 634, 634, 634, 634, 634, 634, 634, 634, 634, 634, 634, 634, 634, 634, 634, 634, 634, 634, 634, 634 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 127, 127, 649, 737, 737, 737, 737, 737, 737, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 160, 160, 160, 160, 160, 160, 170, 180, 180, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 712, -1, -1, 496, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 498, -1, -1, -1, -1, -1, 499, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 500, -1, -1, -1, 501, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 502, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 503, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 504, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 27 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 55, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 857, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 628, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 862, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 888, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 950, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 889, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 957, 508, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274, 274 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 103, 103, 103, 103, 103, 103, 103, 103, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 103, 103, 103, 103, 103, 103, 103, 103, 103, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 103, 103, 103, 103, 103, 103, 103, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 179, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 136, 136, 136, 147, 645, 645, 645, 645, 645, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 515, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 188, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 194, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 200, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 206, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 659, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 212, -1, 218, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 224, -1, -1, -1, -1, -1, -1, 230 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 200, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 236, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 241, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 714, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 487, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 520, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 63 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 63, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 439, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 521, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 489, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 492, -1, -1, -1, -1, 396, -1, 397, 397, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 662, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 662, 211, 211, 211, 211, 211, 211, 211, 211, 211 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 522, 523, 523, 524, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 31, 31, 31, 31, 31, 31, 31, 31, 31, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 90, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 531, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 104, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 117, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 128, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 139, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 90, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 150, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 161, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 171, 532, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 531, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 27, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 27, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 22, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 32, 389, 389 },
			{ -1, 511, -1, -1, -1, -1, 703, -1, 512, 512, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870, 870 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 276, 276, 278, 280, 280, 280, 280, 280, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 711, 711, 711, 711, 711, 711, 711, 711, 711, 711, 711, 711, 711, 711, 711, 711, 711, 711, 711, 711, 711, 711, 711, 711, 711 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 398, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 398, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514, 514 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 158, 168, 168, 178, 645, 645, 645, 645, 645, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 542, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 543, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 33, 33, 33, 33, 33, 33, 33, 33, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 33, 33, 33, 33, 33, 33, 33, 33, 33, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 33, 33, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 525, -1, -1, -1, -1, 526, -1, 527, 527, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 213, 213, 219, 225, 225, 225, 225, 225, 225, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 552, 552, 552, 552, 552, 552, 552, 552, 552, 552, 552, 552, 552, 552, 552, 552, 552, 552, 552, 552, 552, 552, 552, 552, 552 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 553, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 27, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 34 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 564, 564, 565, 566, 566, 566, 566, 566, 566, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 57, 57, 57, 57, 57, 57, 57, 57, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 57, 57, 57, 57, 57, 57, 57, 57, 57, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 57, 57, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 567, 567, 568, 569, 569, 569, 569, 569, 569, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 309, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 546, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 545, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 570, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 571, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 572, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 16, -1, -1, -1, -1, -1 },
			{ -1, 549, -1, -1, -1, -1, 717, -1, 550, 550, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 242, 242, 246, 250, 250, 250, 250, 250, 250, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 551, -1, -1, -1, -1, 717, -1, 550, 550, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 723, -1, -1, -1, -1, -1, 575, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 577, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 578, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 579, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 982 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 27, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 27, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 513, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 513, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 581, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 566, 566, 566, 566, 566, 566, 566, 566, 566, 810, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 566, 566, 566, 566, 566, -1, -1, -1, -1, 810, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 810, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 584, 584, 584, 584, 584, 584, 569, 569, 569, 585, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 584, 584, 584, 584, 584, 816, -1, -1, -1, 585, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 816, 816, 816, 816, 816, 816, -1, -1, -1, 585, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 586, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 722, 722, 722, 722, 722, 722, 722, 722, 722, 722, 722, 722, 722, 722, 722, 722, 722, 722, 722, 722, 722, 722, 722, 722, 722 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 528, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 528, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574, 574 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 587, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 181 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 576, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 588, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 821, 821, 589, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 564, 564, 565, 566, 566, 566, 566, 566, 566, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 592, 592, 593, 566, 566, 566, 566, 566, 566, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 826, 826, 826, 826, 826, 826, 594, 594, 594, 585, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 831, 831, 831, 831, 831, 831, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 596, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 598, 598, 598, 598, 598, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 591, 591, 591, 591, 591, 591, 591, 591, 591, 599, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 599, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 600, 600, 600, 600, 600, 600, 600, 600, 600, 810, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 600, 600, 600, 600, 600, -1, -1, -1, -1, 810, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 603, 603, 603, 603, 603, 603, 604, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 573, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 573, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 605, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 27, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 879, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 36, 36, 36, 36, 36, 36, 58, 75, 75, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 606, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 91, 91, 91, 91, 91, 91, 58, 75, 75, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 36, 603, 603, 603, 603, 603, 604, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 608, 608, 608, 608, 608, 608, 591, 591, 591, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 725, 725, 725, 725, 725, 725, 725, 725, 725, 599, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 610, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 838, 838, 838, 838, 838, 838, 612, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 105, 105, 105, 105, 105, 105, 118, 75, 75, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 613, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 614, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 614, -1, -1, -1, -1, -1, -1, 616, 616, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 140, 140, 140, 140, 140, 140, 140, 140, 140, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 37, 37, 622, 729, 729, 729, 729, 729, 729, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 151, 151, 162, 172, 172, 172, 172, 172, 172, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94 },
			{ -1, 761, 761, 761, -1, -1, -1, -1, -1, -1, -1, 43, 43, 110, 643, 643, 643, 643, 643, 643, -1, -1, -1, -1, -1, -1, -1, 761, -1, -1, -1, 761, -1, -1, -1, -1, -1, 64, -1, -1, -1, 64, 81, 96, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 46, 46, 46, 46, 46, 46, 46, 46, 46, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 56, 56, 56, 56, 56, 56, 56, 56, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 729, 729, 729, 729, 729, 629, 76, 76, 76, 59, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 271, -1, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, -1, 94, 94, 94, 94, 94, 94, 94, 94, 866, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 296, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 63, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 319, 323, -1, -1, -1, 321, -1, 322, 322, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 323, -1, -1, -1, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 234, 234, 234, 234, 234, 234, 234, 234, 234, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 850, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 533, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 76, 76, 76, 76, 76, 76, 76, 76, 76, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 414, 415, 415, 415, 415, 415, 415, 415, 415, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 732, 732, 732, 732, 732, 215, 221, 221, 221, 209, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, -1, 47, -1, -1, -1, -1, 186, -1, -1, 513, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 513, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 466, 466, 466, 466, 466, 466, 466, 466, 466, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 399, -1, -1, -1, 399, 400, 401, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 138, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 195, 195, 195, 195, 195, 195, 195, 195, 195, 195, 195, 195, 195, 195, 195, 195, 195, 195, 195, 195, 195, 195, 195, 195, 195 },
			{ -1, 319, 684, -1, -1, -1, 321, -1, 322, 322, -1, 644, 644, 644, 644, 644, 644, 644, 644, 644, 684, -1, -1, -1, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 112, 112, 112, 112, 112, 112, 112, 112, 112, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 243, 247, -1, -1, -1, -1, -1, -1, 106, 119, 251, 251, 251, 251, 251, 265, 265, 265, 265, 255, -1, -1, -1, 259, -1, -1, 173, -1, 183, -1, 190, 4, 196, -1, -1, 202, 208, 214, 262, -1, 220, 226, 232, -1, 42, 238, 859, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 227, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 296, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 752, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 217, 217, 217, 217, 217, 217, 217, 217, 217, 217, 217, 217, 217, 217, 217, 217, 217, 217, 217, 217, 217, 217, 217, 217, 217 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 716, -1, -1, -1, -1 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 296, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, 443, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 356, -1, -1, -1, -1, -1, -1, -1, -1, 176, 176, 176, 176, 176, 176, 176, 176, 176, 356, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 382, 684, -1, -1, -1, 321, -1, 322, 322, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 684, -1, -1, -1, 199, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 199, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1, -1, -1, 535, 536, 537, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 547, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 549, 452, -1, -1, -1, 717, -1, 550, 550, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 452, -1, -1, -1, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189 },
			{ -1, 394, 395, -1, -1, -1, 396, -1, 397, 397, -1, 177, 177, 177, 177, 177, 177, 177, 177, 177, -1, -1, -1, -1, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 737, 737, 737, 737, 737, 207, 138, 138, 138, 201, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 394, 395, -1, -1, -1, 396, -1, 397, 397, -1, 245, 23, 23, 23, 23, 23, 23, 23, 23, -1, -1, -1, -1, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211 },
			{ -1, -1, -1, -1, -1, -1, -1, 138, -1, -1, 528, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 528, 231, 231, 231, 231, 231, 231, 231, 231, 231, 231, 231, 231, 231, 231, 231, 231, 231, 231, 231, 231, 231, 231, 231, 231, 231 },
			{ -1, -1, 356, -1, -1, -1, -1, -1, -1, -1, -1, 627, 627, 627, 627, 627, 627, 627, 627, 627, 356, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 739, 739, 739, 739, 739, 229, 186, 186, 186, 223, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 682, -1, -1, -1, -1, 331, -1, -1, -1, -1 },
			{ -1, 394, 395, -1, -1, -1, 396, -1, 397, 397, -1, 249, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 303, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, 398, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 398, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 716, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 138, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254, 254 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 334, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 335, -1, -1, -1, -1 },
			{ -1, -1, 445, -1, -1, -1, -1, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 632, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741, 741 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 547, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260, 260 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 338, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272 },
			{ -1, 448, 449, -1, -1, -1, 450, -1, 451, 451, -1, 665, 665, 665, 665, 665, 665, 665, 665, 665, 449, -1, -1, -1, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 271, 316, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, 138, -1, -1, 573, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 573, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595, 595 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 309, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 311, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 390, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 355, 316, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, 316, 389, 389, 389, 389, 979, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 573, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 573, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676 },
			{ -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, 513, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 513, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562, 562 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 513, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 513, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 299, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 305, -1, -1, -1, 306, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 307, -1, -1, -1, -1 },
			{ -1, -1, 325, -1, -1, -1, -1, -1, -1, 325, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 357, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 736, 736, 736, 736, 736, 736, 650, 145, 145, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 766, 766, 766, 766, 766, 766, 655, 177, 177, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 446, 446, 446, 446, 446, 446, 446, 446, 446, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 697, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 373, -1, -1, -1, -1, -1 },
			{ -1, -1, 325, -1, -1, -1, -1, -1, -1, 325, -1, 681, 681, 681, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 419, -1, -1, -1, -1, 420, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 698, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 423, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 424, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 426, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 706, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 701, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 706, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 706, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 706, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 706, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 390, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 772, 772, 772, 772, 772, 772, 772, 772, 772, 772, 772, 772, 772, 772, 772, 772, 772, 772, 772, 772, 772, 772, 772, 772, 772 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 422, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 480, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 497, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 505, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 55, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 530, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 538, 539, 539, 540, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 544, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 666, 666, 219, 225, 225, 225, 225, 225, 225, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 580, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 575, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 607, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 611, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 94, 94, 94, 94, 764, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94 },
			{ -1, 761, 761, 761, -1, -1, -1, -1, -1, -1, -1, 735, 735, 110, 643, 643, 643, 643, 643, 643, -1, -1, -1, -1, -1, -1, -1, 761, -1, -1, -1, 761, -1, -1, -1, -1, -1, 64, -1, -1, -1, 64, 81, 96, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 620, 620, 620, 620, 620, 620, 620, 620, 620, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 629, 629, 629, 629, 629, 629, 76, 76, 76, 59, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 319, 684, -1, -1, -1, 321, -1, 322, 322, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 684, -1, -1, -1, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 415, 415, 415, 416, 416, 416, 416, 416, 416, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 215, 215, 215, 215, 215, 215, 221, 221, 221, 209, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, 243, 247, -1, -1, -1, -1, -1, -1, 106, 119, 265, 265, 267, 267, 267, 267, 267, 267, 267, 255, -1, -1, -1, 259, -1, -1, 173, -1, 183, -1, 190, 4, 196, -1, -1, 202, 208, 214, 262, -1, 220, 226, 232, -1, 42, 238, 859, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 873, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, -1, 356, -1, -1, -1, -1, -1, -1, -1, -1, 652, 652, 652, 652, 652, 652, 652, 652, 652, 356, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 394, 395, -1, -1, -1, 396, -1, 397, 397, -1, 245, 245, 245, 245, 245, 245, 245, 245, 245, -1, -1, -1, -1, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 207, 207, 207, 207, 207, 207, 138, 138, 138, 201, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 138, -1, -1, 573, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 573, 768, 768, 768, 768, 768, 768, 768, 768, 768, 768, 768, 768, 768, 768, 768, 768, 768, 768, 768, 768, 768, 768, 768, 768, 768 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 229, 229, 229, 229, 229, 229, 186, 186, 186, 223, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 337, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, 513, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 513, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770, 770 },
			{ -1, -1, -1, -1, -1, -1, -1, 138, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 771, 771, 771, 771, 771, 771, 771, 771, 771, 771, 771, 771, 771, 771, 771, 771, 771, 771, 771, 771, 771, 771, 771, 771, 771 },
			{ -1, 448, 452, -1, -1, -1, 450, -1, 451, 451, -1, 665, 665, 665, 665, 665, 665, 665, 665, 665, 452, -1, -1, -1, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 355, 316, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 291, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 391, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 336, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 388, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 754, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 482, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 438, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 391, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 471, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 495, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 506, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 94, 94, 94, 94, 782, 94, 94, 94, 891, 94, 94, 94, 94, 94, 922, 94, 94, 120, 94, 94, 94, 94, 94, 94, 94 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 416, 416, 416, 416, 416, 416, 416, 416, 416, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, 243, 247, -1, -1, -1, -1, -1, -1, 106, 119, 267, 267, 267, 267, 267, 267, 267, 267, 267, 255, -1, -1, -1, 259, -1, -1, 173, -1, 183, -1, 190, 4, 196, -1, -1, 202, 208, 214, 262, -1, 220, 226, 232, -1, 42, 238, 859, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 893, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, -1, 356, -1, -1, -1, -1, -1, -1, -1, -1, 204, 204, 204, 204, 204, 204, 204, 204, 204, 356, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 394, 395, -1, -1, -1, 396, -1, 397, 397, -1, 249, 249, 249, 249, 249, 249, 249, 249, 249, -1, -1, -1, -1, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211, 211 },
			{ -1, -1, -1, -1, -1, -1, -1, 138, -1, -1, 573, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 573, 671, 671, 671, 671, 671, 671, 671, 671, 671, 671, 671, 671, 671, 671, 671, 671, 671, 671, 671, 671, 671, 671, 671, 671, 671 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 797, 797, 797, 797, 797, 235, 240, 240, 240, 223, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, 513, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 513, 677, 677, 677, 677, 677, 677, 677, 677, 677, 677, 677, 677, 677, 677, 677, 677, 677, 677, 677, 677, 677, 677, 677, 677, 677 },
			{ -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 355, 316, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, 316, 244, 244, 244, 244, 248, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 292, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 391, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 468, 469, 469, 469, 469, 469, 469, 469, 469, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 444, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 457, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 476, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 795, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 806, 94, 94, 94, 94 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 416, 416, 416, 416, 416, 416, 417, 418, 418, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 981, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, -1, 356, -1, -1, -1, -1, -1, -1, -1, -1, 204, 204, 204, 204, 204, 210, 210, 210, 210, 356, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 235, 235, 235, 235, 235, 235, 240, 240, 240, 223, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 355, 316, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 675, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 324, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 392, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 469, 469, 469, 469, 469, 469, 469, 469, 469, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 1001, 389, 389, 458, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 477, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 814, 94, 94, 94, 94, 94, 94, 94, 974, 94, 94, 94, 94, 94, 153, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 418, 418, 418, 418, 418, 418, 418, 418, 418, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 951, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, -1, 356, -1, -1, -1, -1, -1, -1, -1, -1, 210, 210, 210, 210, 210, 210, 210, 210, 210, 356, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 229, 229, 229, 229, 229, 229, 266, 266, 266, 223, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 271, 316, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 248, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 332, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 390, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 563, 563, 563, 563, 563, 563, 563, 563, 563, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 459, 389, 389, 389, 389, 389, 460, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 483, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 94, 94, 94, 94, 906, 94, 94, 94, 909, 94, 94, 94, 94, 94, 164, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 418, 60, 60, 60, 60, 60, 60, 60, 60, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 745, 830, 773, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 333, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 391, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 590, 590, 590, 590, 590, 590, 591, 591, 591, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 461, 389, 389, 389, 462, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 710, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 94, 94, 820, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 432, 433, 433, 433, 433, 433, 433, 433, 433, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 901, 830, 830, 830, 830, 830, 830, 987 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 339, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 594, 594, 594, 594, 594, 594, 594, 594, 594, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 463, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 464, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 174, 94, 94, 94, 825, 94, 94, 94, 911, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 153, 94, 94, 94, 94 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 433, 433, 433, 434, 434, 434, 434, 434, 434, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 958, 830, 830, 830, 830, 830 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 598, 598, 598, 598, 598, 598, 598, 598, 598, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 465, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 94, 94, 94, 94, 909, 94, 94, 184, 94, 94, 94, 94, 94, 94, 913, 94, 94, 94, 94, 94, 191, 94, 983, 94, 94 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 434, 434, 434, 434, 434, 434, 434, 434, 434, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 898, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 787, 830, 830, 830, 830, 830, 955, 830, 830, 830 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 601, 601, 601, 601, 601, 601, 602, 594, 594, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 391, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 94, 94, 94, 94, 197, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94 },
			{ -1, 273, -1, -1, -1, -1, -1, -1, -1, -1, -1, 434, 434, 434, 434, 434, 434, 435, 60, 60, -1, -1, -1, -1, -1, -1, -1, 275, -1, 277, -1, 190, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, 281, 672, -1, -1, 238, -1, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 724, 724, 724, 724, 724, 724, 724, 724, 724, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 392, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203, 203 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 747, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 609, 609, 609, 609, 609, 609, 609, 609, 609, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 390 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 775, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 613, 613, 613, 613, 613, 613, 613, 613, 613, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 507, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 747, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 507, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 252, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 509, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 809, 252, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 390, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 256, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 510, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 391, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 534, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 559, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 560, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 561, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 597, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, 319, 320, -1, -1, -1, 321, -1, 322, 876, -1, 44, 44, 44, 625, 625, 625, 625, 625, 625, 323, -1, -1, -1, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 962, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 982, 389, 851, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 621, 621, 621, 621, 621, 621, 621, 621, 621, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 296, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 624, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 319, 683, -1, -1, -1, 321, -1, 322, 876, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 684, -1, -1, -1, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 626, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 970, 389, 389, 389, 389 },
			{ -1, 319, 683, -1, -1, -1, 321, -1, 322, 876, -1, 111, 111, 111, 644, 644, 644, 644, 644, 644, 684, -1, -1, -1, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 636, 636, 636, 636, 636, 636, 636, 636, 636, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 834, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 271, 316, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 798, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 797, 797, 797, 797, 797, 797, 797, 797, 797, 223, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 138, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 784, 784, 784, 784, 784, 784, 784, 784, 784, 784, 784, 784, 784, 784, 784, 784, 784, 784, 784, 784, 784, 784, 784, 784, 784 },
			{ -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786 },
			{ -1, 448, 449, -1, -1, -1, 450, -1, 451, 451, -1, 669, 669, 669, 669, 669, 744, 744, 744, 744, 449, -1, -1, -1, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 355, 316, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 874, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 702, 389, 389, 389, 389, 389, 389 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 296, -1, 886, 899, 899, 905, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 867, 867, 769, 785, 785, 785, 785, 785, 785, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 750, 750, 750, 750, 750, 750, 750, 750, 750, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 836, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 835, 835, 835, 835, 835, 835, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 94, 94, 94, 94, 94, 94, 94, 94, 865, 94, 94, 994, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 271, -1, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, -1, 94, 94, 94, 94, 94, 94, 94, 94, 131, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 875, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 752, -1, -1, -1, -1 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 875, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, 443, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 894, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, 319, 320, -1, -1, -1, 321, -1, 322, 876, -1, 625, 625, 625, 625, 625, 625, 625, 625, 625, 323, -1, -1, -1, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 855, 728, 728, 728, 728, 728, 728, 728, 728, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 907, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 852, 389, 389, 389, 982, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 918, 389, 389, 389, 389, 389, 389 },
			{ -1, 319, 683, -1, -1, -1, 321, -1, 322, 876, -1, 644, 644, 644, 644, 644, 644, 644, 644, 644, 684, -1, -1, -1, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 837, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 840, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, 448, 449, -1, -1, -1, 450, -1, 451, 451, -1, 744, 744, 744, 744, 744, 744, 744, 744, 744, 449, -1, -1, -1, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71, 71 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 355, 316, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, 316, 244, 244, 244, 244, 978, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 756, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 952, 952, 952, 952, 952, 952, 952, 952, 952, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 839, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 900, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 902, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 728, 728, 728, 728, 728, 728, 728, 728, 728, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 842, 830, 830, 830, 830, 830, 830 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 355, 316, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, 316, 244, 244, 800, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 827, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 801, 801, 801, 801, 801, 801, 801, 801, 801, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 841, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 728, 728, 855, 855, 855, 855, 855, 855, 855, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 842, 830 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 832, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 843, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 837, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 845, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 837, 830 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 832, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 846, 830, 830, 830, 830, 830, 830, 830, 830, 961, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 756, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 847, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 848, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 849, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 853, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 875, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 875, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 884, 830, 830, 830, 830 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 875, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 370, -1, -1, -1, -1 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 875, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, 371, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 875, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 372, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 875, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 63 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 875, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, 63, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 875, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, 374, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 875, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 378, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 296, 296, -1, -1, -1, -1, -1, -1, 875, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 883, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 878, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 921, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 923, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 924, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 925, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 926, -1, 927, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 928, -1, -1, -1, -1, -1, -1, 920 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 924, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 929, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 930, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 910, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 939, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 938, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 934, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 935, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 941, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 953, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 967, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 972, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 355, 316, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 931, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 908, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 914, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 965, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 992, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 989, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 355, 316, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 959, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 910, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 878, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 969, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 912, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 916, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 971, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 914, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 912, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 912, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 917, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 915, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 918, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 914, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 919, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 948, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 932, -1, -1, -1, -1, 933, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 936, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 937, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 940, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 942, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 942, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 984, 244, 244, 244 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 355, 316, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 988, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 964, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 954, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 991, 389, 389, 389, 389, 389, 973, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 993, 244, 244, 244, 244, 244 },
			{ -1, 271, 271, -1, -1, -1, -1, 79, -1, 355, 316, 6, 6, 6, 619, 727, 727, 727, 727, 727, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 960, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 972, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 963, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 973, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 966, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 968, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 980, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 985, 389, 389, 389, 389, 389 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 998, 94, 94, 94, 94, 94, 94, 94 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 244, 990, 244, 244, 244 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 997, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 94, 94, 94, 94, 1000, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94 },
			{ -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 830, 986, 830, 830, 830, 830, 830, 830 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 389, 995, 389, 389, 389, 389, 389, 389, 389 }
		};
		
		
		private static int[] yy_state_dtrans = new int[]
		{
			  0
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
							System.Diagnostics.Debug.Assert(last_accept_state >= 1002);
						}
						else
						{
							bool accepted = false;
							yyreturn = Accept0(last_accept_state, out accepted);
							if (accepted)
							{
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

