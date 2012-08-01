using System;
using System.IO;
using System.Text;
namespace gpcc
{
	internal class Scanner
	{
		public class ParseException : Exception
		{
			public int line;
			public int column;
			public ParseException(int line, int column, string message) : base(message)
			{
				this.line = line;
				this.column = column;
			}
		}
		public string yylval;
		private char next;
		private string line;
		private int section;
		private string filename;
		private int linenr;
		private int pos;
		private StreamReader reader;
		private StringBuilder builder;
		public int CurrentLine
		{
			get
			{
				return this.linenr;
			}
		}
		public int CurrentColumn
		{
			get
			{
				return this.pos;
			}
		}
		public Scanner(string path)
		{
			this.section = 0;
			this.filename = path;
			this.reader = new StreamReader(path);
			this.builder = new StringBuilder();
			this.pos = 0;
			this.linenr = 0;
			this.line = "";
			this.Advance();
		}
		public GrammarToken Next()
		{
			this.yylval = null;
			if (this.next == '\0')
			{
				return GrammarToken.Eof;
			}
			if (this.section == 3)
			{
				this.builder.Length = 0;
				if (GPCG.LINES)
				{
					this.builder.AppendFormat("#line {0} \"{1}\"", this.linenr, this.filename);
					this.builder.AppendLine();
				}
				while (this.next != '\0')
				{
					this.builder.Append(this.next);
					this.Advance();
				}
				this.yylval = this.builder.ToString();
				return GrammarToken.Epilog;
			}
			if (this.pos == 0 && this.line.StartsWith("%%"))
			{
				this.Advance();
				this.Advance();
				this.section++;
				return GrammarToken.EndOfSection;
			}
			if (this.section == 0)
			{
				this.builder.Length = 0;
				if (GPCG.LINES)
				{
					this.builder.AppendFormat("#line {0} \"{1}\"", this.linenr, this.filename);
					this.builder.AppendLine();
				}
				while (this.next != '\0' && !this.line.StartsWith("%%"))
				{
					while (this.next != '\0' && this.next != '\n')
					{
						this.builder.Append(this.next);
						this.Advance();
					}
					if (this.next != '\0')
					{
						this.builder.AppendLine();
						this.Advance();
					}
				}
				this.yylval = this.builder.ToString();
				return GrammarToken.Prelude;
			}
			char c = this.next;
			if (c <= '\'')
			{
				switch (c)
				{
				case '\t':
				case '\n':
					break;
				default:
					if (c != ' ')
					{
						switch (c)
						{
						case '%':
							this.Advance();
							if (this.next == '{')
							{
								this.Advance();
								this.builder.Length = 0;
								if (GPCG.LINES)
								{
									this.builder.AppendFormat("#line {0} \"{1}\"", this.linenr, this.filename);
									this.builder.AppendLine();
								}
								while (true)
								{
									if (this.next == '%')
									{
										this.Advance();
										if (this.next == '}')
										{
											break;
										}
									}
									else
									{
										this.builder.Append(this.next);
										this.Advance();
									}
								}
								this.Advance();
								this.yylval = this.builder.ToString();
								return GrammarToken.Prolog;
							}
							if (char.IsLetter(this.next))
							{
								this.builder.Length = 0;
								while (char.IsLetter(this.next))
								{
									this.builder.Append(this.next);
									this.Advance();
								}
								string text = this.builder.ToString();
								string key;
								switch (key = text)
								{
								case "union":
									this.yylval = this.ScanUnion();
									return GrammarToken.Union;
								case "prec":
									return GrammarToken.Prec;
								case "token":
									return GrammarToken.Token;
								case "type":
									return GrammarToken.Type;
								case "nonassoc":
									return GrammarToken.NonAssoc;
								case "left":
									return GrammarToken.Left;
								case "right":
									return GrammarToken.Right;
								case "start":
									return GrammarToken.Start;
								case "namespace":
									return GrammarToken.Namespace;
								case "visibility":
									return GrammarToken.Visibility;
								case "attributes":
									return GrammarToken.Attributes;
								case "parsertype":
									return GrammarToken.ParserName;
								case "tokens":
									return GrammarToken.GenerateTokens;
								case "tokentype":
									return GrammarToken.TokenName;
								case "valuetype":
									return GrammarToken.ValueTypeName;
								case "positiontype":
									return GrammarToken.PositionType;
								}
								this.ReportError("Unexpected keyword {0}", new object[]
								{
									text
								});
								return this.Next();
							}
							this.ReportError("Unexpected keyword {0}", new object[]
							{
								this.next
							});
							return this.Next();
						case '&':
							goto IL_642;
						case '\'':
						{
							this.Advance();
							bool flag = this.next == '\\';
							if (flag)
							{
								this.Advance();
							}
							this.yylval = new string(this.Escape(flag, this.next), 1);
							this.Advance();
							if (this.next != '\'')
							{
								this.ReportError("Expected closing character quote", new object[0]);
							}
							else
							{
								this.Advance();
							}
							return GrammarToken.Literal;
						}
						default:
							goto IL_642;
						}
					}
					break;
				}
				this.Advance();
				return this.Next();
			}
			if (c != '/')
			{
				switch (c)
				{
				case ':':
					this.Advance();
					return GrammarToken.Colon;
				case ';':
					this.Advance();
					return GrammarToken.SemiColon;
				case '<':
					this.Advance();
					this.builder.Length = 0;
					while (this.next != '>' && this.next != '\0')
					{
						this.builder.Append(this.next);
						this.Advance();
					}
					this.Advance();
					this.yylval = this.builder.ToString();
					return GrammarToken.Kind;
				default:
					switch (c)
					{
					case '{':
						if (this.section == 1)
						{
							this.Advance();
							return GrammarToken.LeftCurly;
						}
						this.yylval = this.ScanCodeBlock();
						return GrammarToken.Action;
					case '|':
						this.Advance();
						return GrammarToken.Divider;
					case '}':
						this.Advance();
						return GrammarToken.RightCurly;
					}
					break;
				}
			}
			else
			{
				this.Advance();
				if (this.next == '/')
				{
					while (this.next != '\n')
					{
						this.Advance();
					}
					return this.Next();
				}
				if (this.next == '*')
				{
					this.Advance();
					while (true)
					{
						if (this.next == '*')
						{
							this.Advance();
							if (this.next == '/')
							{
								break;
							}
						}
						else
						{
							this.Advance();
						}
					}
					this.Advance();
					return this.Next();
				}
				this.ReportError("unexpected / character, not in comment", new object[0]);
				return this.Next();
			}
			IL_642:
			if (char.IsLetter(this.next))
			{
				this.builder.Length = 0;
				while (char.IsLetterOrDigit(this.next) || this.next == '_' || this.next == '.')
				{
					this.builder.Append(this.next);
					this.Advance();
				}
				this.yylval = this.builder.ToString();
				return GrammarToken.Symbol;
			}
			this.ReportError("Unexpected character '{0}'", new object[]
			{
				this.next
			});
			this.Advance();
			return this.Next();
		}
		private void Advance()
		{
			if (this.pos + 1 < this.line.Length)
			{
				this.pos++;
				this.next = this.line[this.pos];
				return;
			}
			if (this.reader.EndOfStream)
			{
				this.next = '\0';
				return;
			}
			this.line = this.reader.ReadLine() + "\n";
			this.linenr++;
			this.pos = 0;
			this.next = this.line[this.pos];
		}
		private string ScanCodeBlock()
		{
			this.builder.Length = 0;
			if (GPCG.LINES)
			{
				this.builder.AppendFormat("#line {0} \"{1}\"\n", this.linenr, this.filename);
				this.builder.Append("\t\t\t");
			}
			this.builder.Append(this.next);
			this.Advance();
			int num = 1;
			while (true)
			{
				char c = this.next;
				if (c <= '\'')
				{
					if (c != '"')
					{
						if (c == '\'')
						{
							this.builder.Append(this.next);
							this.Advance();
							while (this.next != '\0' && this.next != '\'')
							{
								if (this.next == '\\')
								{
									this.builder.Append(this.next);
									this.Advance();
								}
								if (this.next != '\0')
								{
									this.builder.Append(this.next);
									this.Advance();
								}
							}
							if (this.next != '\0')
							{
								this.builder.Append(this.next);
								this.Advance();
								continue;
							}
							continue;
						}
					}
					else
					{
						this.builder.Append(this.next);
						this.Advance();
						while (this.next != '\0' && this.next != '"')
						{
							if (this.next == '\\')
							{
								this.builder.Append(this.next);
								this.Advance();
							}
							if (this.next != '\0')
							{
								this.builder.Append(this.next);
								this.Advance();
							}
						}
						if (this.next != '\0')
						{
							this.builder.Append(this.next);
							this.Advance();
							continue;
						}
						continue;
					}
				}
				else
				{
					if (c != '/')
					{
						if (c != '@')
						{
							switch (c)
							{
							case '{':
								num++;
								this.builder.Append(this.next);
								this.Advance();
								continue;
							case '}':
								this.builder.Append(this.next);
								this.Advance();
								if (--num == 0)
								{
									goto Block_8;
								}
								continue;
							}
						}
						else
						{
							this.builder.Append(this.next);
							this.Advance();
							if (this.next != '"')
							{
								continue;
							}
							this.builder.Append(this.next);
							this.Advance();
							while (this.next != '\0' && this.next != '"')
							{
								this.builder.Append(this.next);
								this.Advance();
							}
							if (this.next != '\0')
							{
								this.builder.Append(this.next);
								this.Advance();
								continue;
							}
							continue;
						}
					}
					else
					{
						this.builder.Append(this.next);
						this.Advance();
						if (this.next == '/')
						{
							while (this.next != '\0')
							{
								if (this.next == '\n')
								{
									break;
								}
								this.builder.Append(this.next);
								this.Advance();
							}
							continue;
						}
						if (this.next != '*')
						{
							this.builder.Append(this.next);
							this.Advance();
							continue;
						}
						this.builder.Append(this.next);
						this.Advance();
						while (true)
						{
							if (this.next == '\0' || this.next == '*')
							{
								if (this.next != '\0')
								{
									this.builder.Append(this.next);
									this.Advance();
								}
								if (this.next == '\0' || this.next == '/')
								{
									break;
								}
							}
							else
							{
								this.builder.Append(this.next);
								this.Advance();
							}
						}
						if (this.next != '\0')
						{
							this.builder.Append(this.next);
							this.Advance();
							continue;
						}
						continue;
					}
				}
				this.builder.Append(this.next);
				this.Advance();
			}
			Block_8:
			return this.builder.ToString();
		}
		private string ScanUnion()
		{
			while (this.next != '{')
			{
				this.Advance();
			}
			return this.ScanCodeBlock();
		}
		private char Escape(bool backslash, char ch)
		{
			if (!backslash)
			{
				return ch;
			}
			if (ch <= 'b')
			{
				if (ch == '\'')
				{
					return '\'';
				}
				if (ch == '0')
				{
					return '\0';
				}
				switch (ch)
				{
				case 'a':
					return '\a';
				case 'b':
					return '\b';
				}
			}
			else
			{
				if (ch == 'f')
				{
					return '\f';
				}
				if (ch == 'n')
				{
					return '\n';
				}
				switch (ch)
				{
				case 'r':
					return '\r';
				case 't':
					return '\t';
				case 'v':
					return '\v';
				}
			}
			this.ReportError("Unexpected escape character '\\{0}'", new object[]
			{
				ch
			});
			return ch;
		}
		public void ReportError(string format, params object[] args)
		{
			throw new Scanner.ParseException(this.linenr, this.pos, string.Format(format, args));
		}
	}
}
