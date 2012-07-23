using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace Lex
{
	public sealed class Gen
	{
		private const bool ERROR = false;
		private const bool NOT_ERROR = true;
		private const int BUFFER_SIZE = 1024;
		private const int CLASS_CODE = 0;
		private const int INIT_CODE = 1;
		private const int EOF_CODE = 2;
		private const int EOF_VALUE_CODE = 3;
		private TextReader instream;
		private IndentedTextWriter outstream;
		private Input ibuf;
		private Spec spec;
		private bool init_flag;
		private MakeNfa makeNfa;
		private Nfa2Dfa nfa2dfa;
		private Minimize minimize;
		private Emit emit;
		private string usercode;
		private readonly string inputFilePath;
		private BitSet all_states;
		private bool advance_stop;
		public string InputFilePath
		{
			get
			{
				return this.inputFilePath;
			}
		}
		private static Tokens CharToToken(char c)
		{
			if (c <= '?')
			{
				switch (c)
				{
				case '$':
				case '(':
				case ')':
				case '*':
				case '+':
				case '-':
				case '.':
					break;
				case '%':
				case '&':
				case '\'':
				case ',':
					return Tokens.LETTER;
				default:
					if (c != '?')
					{
						return Tokens.LETTER;
					}
					break;
				}
			}
			else
			{
				switch (c)
				{
				case '[':
				case ']':
				case '^':
					break;
				case '\\':
					return Tokens.LETTER;
				default:
					switch (c)
					{
					case '{':
					case '|':
					case '}':
						break;
					default:
						return Tokens.LETTER;
					}
					break;
				}
			}
			return (Tokens)c;
		}
		public Gen(string filename, string outfile, int version)
		{
			this.init_flag = false;
			this.inputFilePath = Path.GetFullPath(filename);
			this.instream = File.OpenText(this.inputFilePath);
			this.outstream = new IndentedTextWriter(File.CreateText(outfile), "\t");
			this.outstream.Indent = 2;
			this.ibuf = new Input(this.instream);
			this.spec = new Spec();
			this.spec.Version = version;
			this.nfa2dfa = new Nfa2Dfa();
			this.minimize = new Minimize();
			this.makeNfa = new MakeNfa();
			this.emit = new Emit();
			this.init_flag = true;
		}
		public void Generate()
		{
			if (!this.init_flag)
			{
				Error.ParseError(Errors.INIT, this.inputFilePath, 0);
			}
			if (this.spec.verbose)
			{
				Console.WriteLine("Processing first section -- user code.");
			}
			this.userCode();
			if (this.ibuf.eof_reached)
			{
				Error.ParseError(Errors.EOF, this.inputFilePath, this.ibuf.line_number);
			}
			if (this.spec.verbose)
			{
				Console.WriteLine("Processing second section -- Lex declarations.");
			}
			this.userDeclare();
			if (this.ibuf.eof_reached)
			{
				Error.ParseError(Errors.EOF, this.inputFilePath, this.ibuf.line_number);
			}
			if (this.spec.verbose)
			{
				Console.WriteLine("Processing third section -- lexical rules.");
			}
			this.userRules();
			if (this.spec.verbose)
			{
				Console.WriteLine("Outputting lexical analyzer code.");
			}
			this.outstream.Indent = 0;
			this.outstream.WriteLine("namespace " + this.spec.Namespace);
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("#region User Code");
			this.outstream.WriteLine();
			this.outstream.Write(this.usercode);
			this.outstream.WriteLine("#endregion");
			this.outstream.WriteLine();
			this.emit.Write(this.spec, this.outstream, this.inputFilePath);
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			this.outstream.Close();
		}
		private void userCode()
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (!this.init_flag)
			{
				Error.ParseError(Errors.INIT, this.inputFilePath, 0);
			}
			if (this.ibuf.eof_reached)
			{
				Error.ParseError(Errors.EOF, this.inputFilePath, 0);
			}
			while (true)
			{
				if (this.ibuf.GetLine())
				{
					Error.ParseError(Errors.EOF, this.inputFilePath, 0);
				}
				if (this.ibuf.line_read >= 2 && this.ibuf.line[0] == '%' && this.ibuf.line[1] == '%')
				{
					break;
				}
				stringBuilder.Append(new string(this.ibuf.line, 0, this.ibuf.line_read));
			}
			this.usercode = stringBuilder.ToString();
			this.ibuf.line_index = this.ibuf.line_read;
		}
		private string getName(bool optional)
		{
			while (this.ibuf.line_index < this.ibuf.line_read && char.IsWhiteSpace(this.ibuf.line[this.ibuf.line_index]))
			{
				this.ibuf.line_index++;
			}
			if (this.ibuf.line_index >= this.ibuf.line_read)
			{
				if (optional)
				{
					return "";
				}
				Error.ParseError(Errors.DIRECT, this.inputFilePath, 0);
			}
			int num = this.ibuf.line_index;
			while (num < this.ibuf.line_read && !Utility.IsNewline(this.ibuf.line[num]))
			{
				num++;
			}
			StringBuilder stringBuilder = new StringBuilder(num - this.ibuf.line_index);
			while (this.ibuf.line_index < this.ibuf.line_read && !Utility.IsNewline(this.ibuf.line[this.ibuf.line_index]))
			{
				stringBuilder.Append(this.ibuf.line[this.ibuf.line_index]);
				this.ibuf.line_index++;
			}
			return stringBuilder.ToString();
		}
		private void packCode(string st_dir, string end_dir, List<string> result, int code)
		{
			this.ibuf.line_index = st_dir.Length;
			while (true)
			{
				if (this.ibuf.line_index < this.ibuf.line_read)
				{
					int num = this.ibuf.line_read - this.ibuf.line_index - 1;
					if (num >= 0)
					{
						if (num > 0)
						{
							result.Add(new string(this.ibuf.line, this.ibuf.line_index, num));
						}
						this.ibuf.line_index = this.ibuf.line_read;
					}
				}
				else
				{
					if (this.ibuf.GetLine())
					{
						Error.ParseError(Errors.EOF, this.inputFilePath, this.ibuf.line_number);
					}
					if (Utility.Compare(this.ibuf.line, end_dir) == 0)
					{
						break;
					}
				}
			}
			this.ibuf.line_index = end_dir.Length - 1;
		}
		private void userDeclare()
		{
			if (this.ibuf.eof_reached)
			{
				Error.ParseError(Errors.EOF, this.inputFilePath, this.ibuf.line_number);
			}
			while (!this.ibuf.GetLine())
			{
				if (this.ibuf.line_read >= 2 && this.ibuf.line[0] == '%' && this.ibuf.line[1] == '%')
				{
					for (int i = 0; i < this.ibuf.line.Length - 2; i++)
					{
						this.ibuf.line[i] = this.ibuf.line[i + 2];
					}
					this.ibuf.line_read = this.ibuf.line_read - 2;
					this.ibuf.pushback_line = true;
					if (this.ibuf.line_read == 0 || this.ibuf.line[0] == '\r' || this.ibuf.line[0] == '\n')
					{
						this.ibuf.pushback_line = false;
					}
					return;
				}
				if (this.ibuf.line_read != 0)
				{
					if (this.ibuf.line[0] == '%')
					{
						if (this.ibuf.line_read <= 1)
						{
							Error.ParseError(Errors.DIRECT, this.inputFilePath, this.ibuf.line_number);
						}
						else
						{
							string text = this.ibuf.ReadDirective();
							this.ibuf.line_index = text.Length;
							string key;
                            if ((key = text) != null)
                            {
                                switch (key)
                                {
                                    case "%{":
                                        this.packCode(text, "%}", this.spec.ClassCode, 0);
                                        continue;
                                    case "%eof{":
                                        this.packCode(text, "%eof}", this.spec.EofCode, 2);
                                        continue;
                                    case "%ctor{":
                                        this.packCode(text, "%ctor}", this.spec.CtorCode, 1);
                                        continue;
                                    case "%init{":
                                        this.packCode(text, "%init}", this.spec.InitCode, 1);
                                        continue;
                                    case "%char":
                                        this.spec.CountChars = true;
                                        continue;
                                    case "%line":
                                        this.spec.CountLines = true;
                                        continue;
                                    case "%column":
                                        this.spec.CountColumns = true;
                                        continue;
                                    case "%class":
                                        this.spec.LexerName = this.getName(false);
                                        continue;
                                    case "%eofval":
                                        this.spec.EofTokenName = this.getName(false);
                                        continue;
                                    case "%errorval":
                                        this.spec.ErrorTokenName = this.getName(false);
                                        continue;
                                    case "%function":
                                        this.spec.FunctionName = this.getName(false);
                                        continue;
                                    case "%charmap":
                                        this.spec.CharMapMethod = this.getName(false);
                                        continue;
                                    case "%full":
                                        this.spec.dtrans_ncols = 256;
                                        continue;
                                    case "%unicode":
                                        this.spec.dtrans_ncols = 65536;
                                        continue;
                                    case "%integer":
                                        this.spec.integer_type = true;
                                        continue;
                                    case "%implements":
                                        this.spec.ImplementsName = this.getName(false);
                                        continue;
                                    case "%ignorecase":
                                        this.spec.IgnoreCase = true;
                                        continue;
                                    case "%attributes":
                                        this.spec.ClassAttributes = this.getName(false);
                                        continue;
                                    case "%x":
                                    case "%s":
                                    case "%state":
                                        this.saveStates();
                                        continue;
                                    case "%type":
                                        this.spec.TokenTypeName = this.getName(false);
                                        continue;
                                    case "%version":
                                        try
                                        {
                                            this.spec.Version = int.Parse(this.getName(false));
                                            continue;
                                        }
                                        catch
                                        {
                                            Error.ParseError(Errors.DIRECT, this.inputFilePath, this.ibuf.line_number);
                                            continue;
                                        }
                                    case "%variants":
                                        break;
                                    case "%yyeof":
                                        this.spec.yyeof = true;
                                        continue;
                                    case "%namespace":
                                        this.spec.Namespace = this.getName(false);
                                        continue;
                                    case "%valuetype":
                                        this.spec.SemanticValueType = this.getName(false);
                                        continue;
                                    default:
                                        goto IL_5ED;
                                }
                                try
                                {
                                    this.spec.VariantCount = int.Parse(this.getName(false));
                                }
                                catch
                                {
                                    Error.ParseError(Errors.DIRECT, this.inputFilePath, this.ibuf.line_number);
                                }
                                if (this.spec.VariantCount < 1)
                                {
                                    Error.ParseError(Errors.DIRECT, this.inputFilePath, this.ibuf.line_number);
                                    continue;
                                }
                                continue;
                            }
							IL_5ED:
							Error.ParseError(Errors.DIRECT, this.inputFilePath, this.ibuf.line_number);
						}
					}
					else
					{
						this.ibuf.line_index = 0;
						this.saveMacro();
					}
				}
			}
		}
		private void userRules()
		{
			if (!this.init_flag)
			{
				Error.ParseError(Errors.INIT, this.inputFilePath, 0);
			}
			if (this.spec.verbose)
			{
				Console.WriteLine("Creating NFA machine representation.");
			}
			MakeNfa.Allocate_BOL_EOF(this.spec);
			MakeNfa.CreateMachine(this, this.spec, this.ibuf);
			SimplifyNfa.simplify(this.spec);
			if (this.spec.verbose)
			{
				Console.WriteLine("Creating DFA transition table.");
			}
			Nfa2Dfa.MakeDFA(this.spec);
			if (this.spec.verbose)
			{
				Console.WriteLine("Minimizing DFA transition table.");
			}
			this.minimize.min_dfa(this.spec);
		}
		private void printccl(CharSet cset)
		{
			Console.Write(" [");
			for (int i = 0; i < this.spec.dtrans_ncols; i++)
			{
				if (cset.contains(i))
				{
					Console.Write(this.interp_int(i));
				}
			}
			Console.Write(']');
		}
		private string plab(Nfa state)
		{
			if (state == null)
			{
				return "--";
			}
			return this.spec.nfa_states.IndexOf(state, 0, this.spec.nfa_states.Count).ToString();
		}
		private string interp_int(int i)
		{
			switch (i)
			{
			case 8:
				return "\\b";
			case 9:
				return "\\t";
			case 10:
				return "\\n";
			case 11:
				break;
			case 12:
				return "\\f";
			case 13:
				return "\\r";
			default:
				if (i == 32)
				{
					return "\\ ";
				}
				break;
			}
			return char.ToString((char)i);
		}
		public void print_nfa()
		{
			Console.WriteLine("--------------------- NFA -----------------------");
			for (int i = 0; i < this.spec.nfa_states.Count; i++)
			{
				Nfa nfa = this.spec.nfa_states[i];
				Console.Write("Nfa state " + this.plab(nfa) + ": ");
				if (nfa.Next == null)
				{
					Console.Write("(TERMINAL)");
				}
				else
				{
					Console.Write("--> " + this.plab(nfa.Next));
					Console.Write("--> " + this.plab(nfa.Sibling));
					switch (nfa.Edge)
					{
					case '￼':
						Console.Write(" EPSILON ");
						goto IL_E3;
					case '￾':
						this.printccl(nfa.GetCharSet());
						goto IL_E3;
					}
					Console.Write(" " + this.interp_int((int)nfa.Edge));
				}
				IL_E3:
				if (i == 0)
				{
					Console.Write(" (START STATE)");
				}
				if (nfa.GetAccept() != null)
				{
					Console.Write(" accepting " + (((nfa.GetAnchor() & 1) != 0) ? "^" : "") + "<");
					nfa.GetAccept().Dump();
					Console.Write(">" + (((nfa.GetAnchor() & 2) != 0) ? "$" : ""));
				}
				Console.WriteLine("");
			}
			foreach (string current in this.spec.States.Keys)
			{
				int num = this.spec.States[current];
				int count = this.spec.state_rules[num].Count;
				for (int j = 0; j < count; j++)
				{
					Nfa nfa = this.spec.state_rules[num][j];
					Console.Write(this.spec.nfa_states.IndexOf(nfa) + " ");
				}
				Console.WriteLine("");
			}
			Console.WriteLine("-------------------- NFA ----------------------");
		}
		public BitSet GetStates()
		{
			BitSet bitSet = null;
			while (char.IsWhiteSpace(this.ibuf.line[this.ibuf.line_index]))
			{
				this.ibuf.line_index++;
				while (this.ibuf.line_index >= this.ibuf.line_read)
				{
					if (this.ibuf.GetLine())
					{
						return null;
					}
				}
			}
			if ('<' == this.ibuf.line[this.ibuf.line_index])
			{
				this.ibuf.line_index++;
				bitSet = new BitSet();
				while (true)
				{
					if (this.ibuf.line_index < this.ibuf.line_read)
					{
						while (true)
						{
							if (!char.IsWhiteSpace(this.ibuf.line[this.ibuf.line_index]))
							{
								if (',' != this.ibuf.line[this.ibuf.line_index])
								{
									break;
								}
								this.ibuf.line_index++;
							}
							else
							{
								this.ibuf.line_index++;
								while (this.ibuf.line_index >= this.ibuf.line_read)
								{
									if (this.ibuf.GetLine())
									{
										goto Block_6;
									}
								}
							}
						}
						if ('>' == this.ibuf.line[this.ibuf.line_index])
						{
							goto Block_9;
						}
						int line_index = this.ibuf.line_index;
						while (!char.IsWhiteSpace(this.ibuf.line[this.ibuf.line_index]) && this.ibuf.line[this.ibuf.line_index] != ',' && this.ibuf.line[this.ibuf.line_index] != '>')
						{
							this.ibuf.line_index++;
							if (this.ibuf.line_index >= this.ibuf.line_read)
							{
								break;
							}
						}
						int length = this.ibuf.line_index - line_index;
						string text = new string(this.ibuf.line, line_index, length);
						if (!this.spec.States.ContainsKey(text))
						{
							Console.WriteLine("Uninitialized State Name: [" + text + "]");
							Error.ParseError(Errors.STATE, this.inputFilePath, this.ibuf.line_number);
						}
						int bit = this.spec.States[text];
						bitSet.Set(bit, true);
					}
					else
					{
						if (this.ibuf.GetLine())
						{
							break;
						}
					}
				}
				Error.ParseError(Errors.EOF, this.inputFilePath, this.ibuf.line_number);
				return bitSet;
				Block_6:
				Error.ParseError(Errors.EOF, this.inputFilePath, this.ibuf.line_number);
				return bitSet;
				Block_9:
				this.ibuf.line_index++;
				if (this.ibuf.line_index < this.ibuf.line_read)
				{
					this.advance_stop = true;
				}
				return bitSet;
			}
			if (this.all_states == null)
			{
				this.all_states = new BitSet((bitSet != null) ? bitSet.Count : 0, true);
			}
			if (this.ibuf.line_index < this.ibuf.line_read)
			{
				this.advance_stop = true;
			}
			return this.all_states;
		}
		private bool expandMacro()
		{
			int line_index = this.ibuf.line_index;
			int i = this.ibuf.line_index + 1;
			if (i >= this.ibuf.line_read)
			{
				Error.impos("Unfinished macro name");
				return false;
			}
			int num = i;
			while (this.ibuf.line[i] != '}')
			{
				i++;
				if (i >= this.ibuf.line_read)
				{
					Error.impos("Unfinished macro name at line " + this.ibuf.line_number);
					return false;
				}
			}
			int num2 = i - num;
			int num3 = i;
			if (num2 == 0)
			{
				Error.impos("Nonexistent macro name");
				return false;
			}
			string text = new string(this.ibuf.line, num, num2);
			string text2;
			if (!this.spec.macros.TryGetValue(text, out text2))
			{
				Console.WriteLine("Error: Undefined macro \"" + text + "\".");
				Error.ParseError(Errors.NOMAC, this.inputFilePath, this.ibuf.line_number);
				return false;
			}
			char[] array = new char[this.ibuf.line.Length];
			int j;
			for (j = 0; j < line_index; j++)
			{
				array[j] = this.ibuf.line[j];
			}
			if (j >= array.Length)
			{
				array = Utility.doubleSize(array);
			}
			for (int k = 0; k < text2.Length; k++)
			{
				array[j] = text2[k];
				j++;
				if (j >= array.Length)
				{
					array = Utility.doubleSize(array);
				}
			}
			if (j >= array.Length)
			{
				array = Utility.doubleSize(array);
			}
			for (i = num3 + 1; i < this.ibuf.line_read; i++)
			{
				array[j] = this.ibuf.line[i];
				j++;
				if (j >= array.Length)
				{
					array = Utility.doubleSize(array);
				}
			}
			this.ibuf.line = array;
			this.ibuf.line_read = j;
			return true;
		}
		private void saveMacro()
		{
			int num = 0;
			while (char.IsWhiteSpace(this.ibuf.line[num]))
			{
				num++;
				if (num >= this.ibuf.line_read)
				{
					return;
				}
			}
			int num2 = num;
			while (!char.IsWhiteSpace(this.ibuf.line[num]) && '=' != this.ibuf.line[num])
			{
				num++;
				if (num >= this.ibuf.line_read)
				{
					Error.ParseError(Errors.MACDEF, this.inputFilePath, this.ibuf.line_number);
				}
			}
			int num3 = num - num2;
			if (num3 == 0)
			{
				Error.ParseError(Errors.MACDEF, this.inputFilePath, this.ibuf.line_number);
			}
			while (char.IsWhiteSpace(this.ibuf.line[num]))
			{
				num++;
				if (num >= this.ibuf.line_read)
				{
					Error.ParseError(Errors.MACDEF, this.inputFilePath, this.ibuf.line_number);
				}
			}
			if ('=' == this.ibuf.line[num])
			{
				num++;
				if (num >= this.ibuf.line_read)
				{
					Error.ParseError(Errors.MACDEF, this.inputFilePath, this.ibuf.line_number);
				}
			}
			while (char.IsWhiteSpace(this.ibuf.line[num]))
			{
				num++;
				if (num >= this.ibuf.line_read)
				{
					Error.ParseError(Errors.MACDEF, this.inputFilePath, this.ibuf.line_number);
				}
			}
			int num4 = num;
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			while (!char.IsWhiteSpace(this.ibuf.line[num]) || flag || flag2 || flag3)
			{
				if (!flag3 && !flag2 && this.ibuf.line[num] == '"')
				{
					flag = !flag;
				}
				flag3 = (!flag3 && this.ibuf.line[num] == '\\');
				if (!flag3 && !flag)
				{
					if ('[' == this.ibuf.line[num] && !flag2)
					{
						flag2 = true;
					}
					else
					{
						if (']' == this.ibuf.line[num] && flag2)
						{
							flag2 = false;
						}
					}
				}
				num++;
				if (num >= this.ibuf.line_read)
				{
					break;
				}
			}
			int num5 = num - num4;
			if (num5 == 0)
			{
				Error.ParseError(Errors.MACDEF, this.inputFilePath, this.ibuf.line_number);
			}
			string key = new string(this.ibuf.line, num2, num3);
			string value = "(" + new string(this.ibuf.line, num4, num5) + ")";
			if (this.spec.macros.ContainsKey(key))
			{
				Error.ParseError(Errors.DuplicatedMacro, this.inputFilePath, this.ibuf.line_number);
			}
			this.spec.macros.Add(key, value);
		}
		private void saveStates()
		{
			if (this.ibuf.eof_reached)
			{
				return;
			}
			if (this.ibuf.line_index >= this.ibuf.line_read)
			{
				return;
			}
			while (this.ibuf.line_index < this.ibuf.line_read)
			{
				while (char.IsWhiteSpace(this.ibuf.line[this.ibuf.line_index]))
				{
					this.ibuf.line_index++;
					if (this.ibuf.line_index >= this.ibuf.line_read)
					{
						return;
					}
				}
				int line_index = this.ibuf.line_index;
				while (!char.IsWhiteSpace(this.ibuf.line[this.ibuf.line_index]) && this.ibuf.line[this.ibuf.line_index] != ',')
				{
					this.ibuf.line_index++;
					if (this.ibuf.line_index >= this.ibuf.line_read)
					{
						break;
					}
				}
				int length = this.ibuf.line_index - line_index;
				this.spec.AddState(new string(this.ibuf.line, line_index, length));
				if (this.ibuf.line[this.ibuf.line_index] == ',')
				{
					this.ibuf.line_index++;
					if (this.ibuf.line_index >= this.ibuf.line_read)
					{
						return;
					}
				}
			}
		}
		private char expandEscape()
		{
			char c = char.ToLower(this.ibuf.line[this.ibuf.line_index]);
			char c2;
			if (c <= 'f')
			{
				switch (c)
				{
				case 'a':
					this.ibuf.line_index++;
					return '\a';
				case 'b':
					this.ibuf.line_index++;
					return '\b';
				default:
					if (c == 'f')
					{
						this.ibuf.line_index++;
						return '\f';
					}
					break;
				}
			}
			else
			{
				if (c == 'n')
				{
					this.ibuf.line_index++;
					return '\n';
				}
				switch (c)
				{
				case 'r':
					this.ibuf.line_index++;
					return '\r';
				case 't':
					this.ibuf.line_index++;
					return '\t';
				case 'v':
					this.ibuf.line_index++;
					return '\v';
				case 'x':
					this.ibuf.line_index++;
					c2 = '\0';
					if (Utility.ishexdigit(this.ibuf.line[this.ibuf.line_index]))
					{
						c2 = Utility.hex2bin(this.ibuf.line[this.ibuf.line_index]);
						this.ibuf.line_index++;
					}
					if (Utility.ishexdigit(this.ibuf.line[this.ibuf.line_index]))
					{
						c2 <<= 4;
						c2 |= Utility.hex2bin(this.ibuf.line[this.ibuf.line_index]);
						this.ibuf.line_index++;
					}
					if (Utility.ishexdigit(this.ibuf.line[this.ibuf.line_index]))
					{
						c2 <<= 4;
						c2 |= Utility.hex2bin(this.ibuf.line[this.ibuf.line_index]);
						this.ibuf.line_index++;
					}
					return c2;
				}
			}
			if (!Utility.isoctdigit(this.ibuf.line[this.ibuf.line_index]))
			{
				c2 = this.ibuf.line[this.ibuf.line_index];
				this.ibuf.line_index++;
			}
			else
			{
				c2 = Utility.oct2bin(this.ibuf.line[this.ibuf.line_index]);
				this.ibuf.line_index++;
				if (Utility.isoctdigit(this.ibuf.line[this.ibuf.line_index]))
				{
					c2 <<= 3;
					c2 |= Utility.oct2bin(this.ibuf.line[this.ibuf.line_index]);
					this.ibuf.line_index++;
				}
				if (Utility.isoctdigit(this.ibuf.line[this.ibuf.line_index]))
				{
					c2 <<= 3;
					c2 |= Utility.oct2bin(this.ibuf.line[this.ibuf.line_index]);
					this.ibuf.line_index++;
				}
			}
			return c2;
		}
		public Accept packAccept()
		{
			while (this.ibuf.line_index >= this.ibuf.line_read)
			{
				if (this.ibuf.GetLine())
				{
					Error.ParseError(Errors.EOF, this.inputFilePath, this.ibuf.line_number);
					return null;
				}
			}
			int i = 0;
			List<CodeBlock> list = new List<CodeBlock>(this.spec.VariantCount);
			while (i < this.spec.VariantCount)
			{
				while (char.IsWhiteSpace(this.ibuf.line[this.ibuf.line_index]))
				{
					this.ibuf.line_index++;
					while (this.ibuf.line_index >= this.ibuf.line_read)
					{
						if (this.ibuf.GetLine())
						{
							if (i <= 0)
							{
								Error.ParseError(Errors.EOF, this.inputFilePath, this.ibuf.line_number);
								return null;
							}
							goto IL_3F6;
						}
					}
				}
				if (this.ibuf.line[this.ibuf.line_index] != '{')
				{
					if (i > 0)
					{
						break;
					}
					Error.ParseError(Errors.BRACE, this.inputFilePath, this.ibuf.line_number);
				}
				StringBuilder stringBuilder = new StringBuilder(1024);
				List<string> list2 = new List<string>();
				int line_number = this.ibuf.line_number;
				int num = 0;
				bool flag4;
				bool flag3;
				bool flag2;
				bool flag = flag2 = (flag3 = (flag4 = false));
				bool flag6;
				bool flag5 = flag6 = false;
				while (true)
				{
					if (this.ibuf.line[this.ibuf.line_index] != '\n')
					{
						stringBuilder.Append(this.ibuf.line[this.ibuf.line_index]);
					}
					else
					{
						list2.Add(stringBuilder.ToString());
						stringBuilder.Length = 0;
					}
					if ((flag2 || flag) && flag6)
					{
						flag6 = false;
					}
					else
					{
						if ((flag2 || flag) && '\\' == this.ibuf.line[this.ibuf.line_index])
						{
							flag6 = true;
						}
						else
						{
							if (this.ibuf.line[this.ibuf.line_index] == '"' && !flag)
							{
								flag2 = !flag2;
							}
							else
							{
								if (this.ibuf.line[this.ibuf.line_index] == '\'' && !flag2)
								{
									flag = !flag;
								}
							}
						}
					}
					if (flag4)
					{
						if (flag5 && '/' == this.ibuf.line[this.ibuf.line_index])
						{
							flag5 = (flag4 = false);
						}
						else
						{
							flag5 = ('*' == this.ibuf.line[this.ibuf.line_index]);
						}
					}
					else
					{
						if (!flag3)
						{
							flag3 = (flag5 && '/' == this.ibuf.line[this.ibuf.line_index]);
							flag4 = (flag5 && '*' == this.ibuf.line[this.ibuf.line_index]);
							flag5 = ('/' == this.ibuf.line[this.ibuf.line_index]);
						}
					}
					if (!flag2 && !flag && !flag4 && !flag3)
					{
						if ('{' == this.ibuf.line[this.ibuf.line_index])
						{
							num++;
						}
						else
						{
							if ('}' == this.ibuf.line[this.ibuf.line_index])
							{
								num--;
								if (num == 0)
								{
									break;
								}
							}
						}
					}
					this.ibuf.line_index++;
					while (this.ibuf.line_index >= this.ibuf.line_read)
					{
						flag5 = (flag3 = false);
						if (flag2 || flag)
						{
							Error.ParseError(Errors.NEWLINE, this.inputFilePath, this.ibuf.line_number);
							flag2 = false;
						}
						if (this.ibuf.GetLine())
						{
							goto Block_31;
						}
					}
				}
				this.ibuf.line_index++;
				if (stringBuilder.Length > 0)
				{
					list2.Add(stringBuilder.ToString());
				}
				list.Add(new CodeBlock(list2, line_number, this.ibuf.line_number));
				i++;
				continue;
				Block_31:
				Error.ParseError(Errors.SYNTAX, this.inputFilePath, this.ibuf.line_number);
				return null;
			}
			IL_3F6:
			return new Accept(list);
		}
		public Tokens Advance()
		{
			this.ActualAdvance();
			return this.spec.current_token;
		}
		public void ActualAdvance()
		{
			if (this.ibuf.eof_reached)
			{
				this.spec.current_token = Tokens.END_OF_INPUT;
				this.spec.current_token_value = '\0';
				return;
			}
			if (Tokens.EOS == this.spec.current_token || this.ibuf.line_index >= this.ibuf.line_read)
			{
				if (this.spec.in_quote)
				{
					Error.ParseError(Errors.SYNTAX, this.inputFilePath, this.ibuf.line_number);
				}
				while (true)
				{
					if (!this.advance_stop || this.ibuf.line_index >= this.ibuf.line_read)
					{
						if (this.ibuf.GetLine())
						{
							break;
						}
						this.ibuf.line_index = 0;
					}
					else
					{
						this.advance_stop = false;
					}
					while (this.ibuf.line_index < this.ibuf.line_read && char.IsWhiteSpace(this.ibuf.line[this.ibuf.line_index]))
					{
						this.ibuf.line_index++;
					}
					if (this.ibuf.line_index < this.ibuf.line_read)
					{
						goto IL_137;
					}
				}
				this.spec.current_token = Tokens.END_OF_INPUT;
				this.spec.current_token_value = '\0';
				return;
			}
			IL_137:
			while (this.ibuf.line_index < this.ibuf.line_read)
			{
				char c = this.ibuf.line[this.ibuf.line_index++];
				if (!this.spec.in_quote && !this.spec.in_ccl && c == '{')
				{
					this.ibuf.line_index--;
					this.expandMacro();
				}
				else
				{
					if (c == '\\')
					{
						this.spec.current_token_value = this.expandEscape();
						this.spec.current_token = Tokens.LETTER;
						return;
					}
					if (this.spec.in_quote)
					{
						if (c != '"')
						{
							this.spec.current_token_value = c;
							this.spec.current_token = Tokens.LETTER;
							return;
						}
						this.spec.in_quote = false;
					}
					else
					{
						if (this.spec.in_ccl)
						{
							this.spec.current_token_value = c;
							char c2 = c;
							if (c2 != '-')
							{
								switch (c2)
								{
								case '[':
									if (this.ibuf.line_index < this.ibuf.line_read && this.ibuf.line[this.ibuf.line_index] == ':')
									{
										this.ibuf.line_index++;
										int line_index = this.ibuf.line_index;
										while (this.ibuf.line_index < this.ibuf.line_read && this.ibuf.line[this.ibuf.line_index] != ':')
										{
											this.ibuf.line_index++;
										}
										if (this.ibuf.line_index + 1 >= this.ibuf.line_read || this.ibuf.line[this.ibuf.line_index + 1] != ']')
										{
											Error.ParseError(Errors.SYNTAX, this.inputFilePath, this.ibuf.line_number);
										}
										this.spec.class_name = new string(this.ibuf.line, line_index, this.ibuf.line_index - line_index);
										this.spec.current_token = Tokens.CHAR_CLASS;
										this.ibuf.line_index += 2;
										return;
									}
									return;
								case ']':
									this.spec.current_token = Tokens.CCL_END;
									this.spec.in_ccl = false;
									return;
								case '^':
									this.spec.current_token = Tokens.AT_BOL;
									return;
								}
								this.spec.current_token = Tokens.LETTER;
								return;
							}
							this.spec.current_token = Tokens.DASH;
							return;
						}
						else
						{
							if (c == '"')
							{
								this.spec.in_quote = true;
							}
							else
							{
								if (char.IsWhiteSpace(c))
								{
									this.spec.current_token = Tokens.EOS;
									this.spec.current_token_value = '\0';
									return;
								}
								this.spec.current_token_value = c;
								this.spec.current_token = Gen.CharToToken(this.spec.current_token_value);
								if (this.spec.current_token == Tokens.CCL_START)
								{
									this.spec.in_ccl = true;
								}
								return;
							}
						}
					}
				}
			}
			this.spec.current_token = Tokens.EOS;
			this.spec.current_token_value = '\0';
		}
		private void details()
		{
			Console.WriteLine("\n\t** Macros **");
			foreach (string current in this.spec.macros.Keys)
			{
				string text = this.spec.macros[current];
				Console.WriteLine(string.Concat(new string[]
				{
					"Macro name \"",
					current,
					"\" has definition \"",
					text,
					"\"."
				}));
			}
			Console.WriteLine("\n\t** States **");
			foreach (string current2 in this.spec.States.Keys)
			{
				int num = this.spec.States[current2];
				Console.WriteLine(string.Concat(new object[]
				{
					"State \"",
					current2,
					"\" has identifying index ",
					num,
					"."
				}));
			}
			if (this.spec.dtrans_list != null)
			{
				Console.WriteLine("\n\t** DFA transition table **");
			}
		}
		private void print_header()
		{
			int num = 0;
			Console.WriteLine("/*---------------------- DFA -----------------------");
			foreach (string current in this.spec.States.Keys)
			{
				int num2 = this.spec.States[current];
				Console.WriteLine(string.Concat(new object[]
				{
					"State \"",
					current,
					"\" has identifying index ",
					num2,
					"."
				}));
				if (-1 != this.spec.state_dtrans[num2])
				{
					Console.WriteLine("\tStart index in transition table: " + this.spec.state_dtrans[num2]);
				}
				else
				{
					Console.WriteLine("\tNo associated transition states.");
				}
			}
			for (int i = 0; i < this.spec.dtrans_list.Count; i++)
			{
				DTrans dTrans = this.spec.dtrans_list[i];
				if (this.spec.accept_list == null && this.spec.anchor_array == null)
				{
					if (dTrans.GetAccept() == null)
					{
						Console.Write(" * State " + i + " [nonaccepting]");
					}
					else
					{
						Console.Write(" * State " + i + " [accepting, ");
						dTrans.GetAccept().Dump();
						Console.Write(">]");
						if (dTrans.GetAnchor() != 0)
						{
							Console.Write(" Anchor: " + (((dTrans.GetAnchor() & 1) != 0) ? "start " : "") + (((dTrans.GetAnchor() & 2) != 0) ? "end " : ""));
						}
					}
				}
				else
				{
					if (this.spec.accept_list[i] == null)
					{
						Console.Write(" * State " + i + " [nonaccepting]");
					}
					else
					{
						Console.Write(" * State " + i + " [accepting, ");
						dTrans.GetAccept().Dump();
						Console.Write(">]");
						if (this.spec.anchor_array[i] != 0)
						{
							Console.Write(" Anchor: " + (((this.spec.anchor_array[i] & 1) != 0) ? "start " : "") + (((this.spec.anchor_array[i] & 2) != 0) ? "end " : ""));
						}
					}
				}
				int num3 = -1;
				for (int j = 0; j < this.spec.dtrans_ncols; j++)
				{
					if (-1 != dTrans.GetDTrans(j))
					{
						if (num3 != dTrans.GetDTrans(j))
						{
							Console.Write("\n *    goto " + dTrans.GetDTrans(j) + " on ");
							num = 0;
						}
						string text = this.interp_int(j);
						Console.Write(text);
						num += text.Length;
						if (56 < num)
						{
							Console.Write("\n *             ");
							num = 0;
						}
						num3 = dTrans.GetDTrans(j);
					}
				}
				Console.WriteLine("");
			}
			Console.WriteLine(" */\n");
		}
	}
}
