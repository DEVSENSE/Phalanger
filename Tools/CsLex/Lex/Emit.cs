using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
namespace Lex
{
	public class Emit
	{
		private const int START = 1;
		private const int END = 2;
		private const int NONE = 4;
		private const bool EDBG = true;
		private const bool NOT_EDBG = false;
		private Spec spec;
		private IndentedTextWriter outstream;
		private string inputFilePath;
		public Emit()
		{
			this.reset();
		}
		private void reset()
		{
			this.spec = null;
			this.outstream = null;
		}
		private void set(Spec spec, IndentedTextWriter outstream, string inputFilePath)
		{
			this.spec = spec;
			this.outstream = outstream;
			this.inputFilePath = inputFilePath;
		}
		private void print_details()
		{
			Console.WriteLine("---------------------- Transition Table ----------------------");
			for (int i = 0; i < this.spec.row_map.Length; i++)
			{
				Console.Write("State " + i);
				Accept accept = this.spec.accept_list[i];
				if (accept == null)
				{
					Console.WriteLine(" [nonaccepting]");
				}
				else
				{
					Console.Write(" [accepting, ");
					accept.Dump();
					Console.Write("]");
				}
				DTrans dTrans = this.spec.dtrans_list[this.spec.row_map[i]];
				bool flag = false;
				int num = dTrans.GetDTrans(this.spec.col_map[0]);
				if (-1 != num)
				{
					flag = true;
					Console.Write("\tgoto " + num.ToString() + " on [");
				}
				for (int j = 1; j < this.spec.dtrans_ncols; j++)
				{
					int dTrans2 = dTrans.GetDTrans(this.spec.col_map[j]);
					if (num == dTrans2)
					{
						if (-1 != num)
						{
							Console.Write((char)j);
						}
					}
					else
					{
						num = dTrans2;
						if (flag)
						{
							Console.WriteLine("]");
							flag = false;
						}
						if (-1 != num)
						{
							flag = true;
							Console.Write("\tgoto " + num.ToString() + " on [" + char.ToString((char)j));
						}
					}
				}
				if (flag)
				{
					Console.WriteLine("]");
				}
			}
			Console.WriteLine("---------------------- Transition Table ----------------------");
		}
		public void Write(Spec spec, IndentedTextWriter o, string inputFilePath)
		{
			this.set(spec, o, inputFilePath);
			this.Header();
			this.Construct();
			this.Helpers();
			this.Driver();
			this.Footer();
			this.reset();
		}
		private void Construct()
		{
			this.outstream.WriteLine("[Flags]");
			this.outstream.WriteLine("private enum AcceptConditions : byte");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("NotAccept = 0,");
			this.outstream.WriteLine("AcceptOnStart = 1,");
			this.outstream.WriteLine("AcceptOnEnd = 2,");
			this.outstream.WriteLine("Accept = 4");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			if (this.spec.CountColumns || this.spec.CountLines || this.spec.CountChars)
			{
				this.outstream.WriteLine("public struct Position");
				this.outstream.WriteLine("{");
				this.outstream.Indent++;
				if (this.spec.CountChars)
				{
					this.outstream.WriteLine("public int Char;");
				}
				if (this.spec.CountLines)
				{
					this.outstream.WriteLine("public int Line;");
				}
				if (this.spec.CountColumns)
				{
					this.outstream.WriteLine("public int Column;");
				}
				List<string> list = new List<string>();
				if (this.spec.CountChars)
				{
					list.Add("int ch");
				}
				if (this.spec.CountLines)
				{
					list.Add("int line");
				}
				if (this.spec.CountColumns)
				{
					list.Add("int column");
				}
				this.outstream.WriteLine("public Position({0})", string.Join(", ", list.ToArray()));
				this.outstream.WriteLine("{");
				this.outstream.Indent++;
				if (this.spec.CountChars)
				{
					this.outstream.WriteLine("this.Char = ch;");
				}
				if (this.spec.CountLines)
				{
					this.outstream.WriteLine("this.Line = line;");
				}
				if (this.spec.CountColumns)
				{
					this.outstream.WriteLine("this.Column = column;");
				}
				this.outstream.Indent--;
				this.outstream.WriteLine("}");
				this.outstream.Indent--;
				this.outstream.WriteLine("}");
			}
			this.outstream.WriteLine("private const int NoState = -1;");
			this.outstream.WriteLine("private const char BOL = (char){0};", (int)this.spec.BOL);
			this.outstream.WriteLine("private const char EOF = (char){0};", (int)this.spec.EOF);
			this.outstream.WriteLine();
			this.outstream.WriteLine("private {0} yyreturn;", this.spec.TokenTypeName);
			if (this.spec.SemanticValueType != null)
			{
				this.outstream.WriteLine("private {0} yylval;", this.spec.SemanticValueType);
			}
			if (this.spec.integer_type || this.spec.yyeof)
			{
				this.outstream.WriteLine("public const int YYEOF = -1;");
			}
			this.EmitUserCode(this.spec.ClassCode);
			this.outstream.WriteLine();
			this.outstream.WriteLine("private System.IO.TextReader reader;");
			this.outstream.WriteLine("private char[] buffer = new char[512];");
			this.outstream.WriteLine();
			this.outstream.WriteLine("// whether the currently parsed token is being expanded (yymore has been called):");
			this.outstream.WriteLine("private bool expanding_token;");
			this.outstream.WriteLine();
			this.outstream.WriteLine("// offset in buffer where the currently parsed token starts:");
			this.outstream.WriteLine("private int token_start;");
			this.outstream.WriteLine();
			this.outstream.WriteLine("// offset in buffer where the currently parsed token chunk starts:");
			this.outstream.WriteLine("private int token_chunk_start;");
			this.outstream.WriteLine();
			this.outstream.WriteLine("// offset in buffer one char behind the currently parsed token (chunk) ending character:");
			this.outstream.WriteLine("private int token_end;");
			this.outstream.WriteLine();
			this.outstream.WriteLine("// offset of the lookahead character (number of characters parsed):");
			this.outstream.WriteLine("private int lookahead_index;");
			this.outstream.WriteLine();
			this.outstream.WriteLine("// number of characters read into the buffer:");
			this.outstream.WriteLine("private int chars_read;");
			if (this.spec.CountColumns || this.spec.CountLines || this.spec.CountChars)
			{
				this.outstream.WriteLine();
				this.outstream.WriteLine("// parsed token start position (wrt beginning of the stream):");
				this.outstream.WriteLine("protected Position token_start_pos;");
				this.outstream.WriteLine();
				this.outstream.WriteLine("// parsed token end position (wrt beginning of the stream):");
				this.outstream.WriteLine("protected Position token_end_pos;");
			}
			this.outstream.WriteLine();
			this.outstream.WriteLine("private bool yy_at_bol = false;");
			this.outstream.WriteLine();
			if (this.spec.VariantCount > 1)
			{
				this.outstream.WriteLine("public int Variant { get { return variant; } set { variant = value; } }");
				this.outstream.WriteLine("private int variant = 0;");
				this.outstream.WriteLine();
			}
			this.outstream.WriteLine("public LexicalStates CurrentLexicalState { get { return current_lexical_state; } set { current_lexical_state = value; } } ");
			this.outstream.WriteLine("private LexicalStates current_lexical_state;");
			this.outstream.WriteLine();
			this.outstream.WriteLine("public {0}(System.IO.TextReader reader)", this.spec.LexerName);
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("Initialize(reader, LexicalStates.{0});", this.spec.InitialState);
			this.EmitUserCode(this.spec.CtorCode);
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			this.outstream.WriteLine("public void Initialize(System.IO.TextReader reader, LexicalStates lexicalState, bool atBol)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("this.expanding_token = false;");
			this.outstream.WriteLine("this.token_start = 0;");
			this.outstream.WriteLine("this.chars_read = 0;");
			this.outstream.WriteLine("this.lookahead_index = 0;");
			this.outstream.WriteLine("this.token_chunk_start = 0;");
			this.outstream.WriteLine("this.token_end = 0;");
			this.outstream.WriteLine("this.reader = reader;");
			this.outstream.WriteLine("this.yy_at_bol = atBol;");
			this.outstream.WriteLine("this.current_lexical_state = lexicalState;");
			this.EmitUserCode(this.spec.InitCode);
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			this.outstream.WriteLine("public void Initialize(System.IO.TextReader reader, LexicalStates lexicalState)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("Initialize(reader, lexicalState, false);");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			this.EmitAcceptMethods();
		}
		private void EmitUserCode(List<string> code)
		{
			foreach (string current in code)
			{
				this.outstream.WriteLine(current);
			}
		}
		private void States()
		{
			this.outstream.WriteLine();
			this.outstream.WriteLine("private static int[] yy_state_dtrans = new int[]");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			for (int i = 0; i < this.spec.state_dtrans.Length; i++)
			{
				this.outstream.Write("  ");
				this.outstream.Write(this.spec.state_dtrans[i]);
				if (i < this.spec.state_dtrans.Length - 1)
				{
					this.outstream.WriteLine(",");
				}
				else
				{
					this.outstream.WriteLine();
				}
			}
			this.outstream.Indent--;
			this.outstream.WriteLine("};");
		}
		private void Helpers()
		{
			if (this.spec.EofCode.Count > 0)
			{
				this.outstream.WriteLine("private bool yy_eof_done = false;");
				this.outstream.WriteLine("private void yy_do_eof()");
				this.outstream.WriteLine("{");
				this.outstream.Indent++;
				this.outstream.WriteLine("if (!yy_eof_done)");
				this.outstream.WriteLine("{");
				this.outstream.Indent++;
				this.EmitUserCode(this.spec.EofCode);
				this.outstream.Indent--;
				this.outstream.WriteLine("}");
				this.outstream.WriteLine("yy_eof_done = true;");
				this.outstream.Indent--;
				this.outstream.WriteLine("}");
				this.outstream.WriteLine();
			}
			this.outstream.WriteLine("private void BEGIN(LexicalStates state)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("current_lexical_state = state;");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			this.outstream.WriteLine("private char Advance()");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("if (lookahead_index >= chars_read)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("if (token_start > 0)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("// shift buffer left:");
			this.outstream.WriteLine("int length = chars_read - token_start;");
			this.outstream.WriteLine("System.Buffer.BlockCopy(buffer, token_start << 1, buffer, 0, length << 1);");
			this.outstream.WriteLine("token_end -= token_start;");
			this.outstream.WriteLine("token_chunk_start -= token_start;");
			this.outstream.WriteLine("token_start = 0;");
			this.outstream.WriteLine("chars_read = lookahead_index = length;");
			this.outstream.WriteLine();
			this.outstream.WriteLine("// populate the remaining bytes:");
			this.outstream.WriteLine("int count = reader.Read(buffer, chars_read, buffer.Length - chars_read);");
			this.outstream.WriteLine("if (count <= 0) return EOF;");
			this.outstream.WriteLine();
			this.outstream.WriteLine("chars_read += count;");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			this.outstream.WriteLine("while (lookahead_index >= chars_read)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("if (lookahead_index >= buffer.Length)");
			this.outstream.Indent++;
			this.outstream.WriteLine("buffer = ResizeBuffer(buffer);");
			this.outstream.Indent--;
			this.outstream.WriteLine();
			this.outstream.WriteLine("int count = reader.Read(buffer, chars_read, buffer.Length - chars_read);");
			this.outstream.WriteLine("if (count <= 0) return EOF;");
			this.outstream.WriteLine("chars_read += count;");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			if (this.spec.CharMapMethod != null)
			{
				this.outstream.WriteLine("return {0}(buffer[lookahead_index++]);", this.spec.CharMapMethod);
			}
			else
			{
				this.outstream.WriteLine("return buffer[lookahead_index++];");
			}
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			this.outstream.WriteLine("private char[] ResizeBuffer(char[] buf)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("char[] result = new char[buf.Length << 1];");
			this.outstream.WriteLine("System.Buffer.BlockCopy(buf, 0, result, 0, buf.Length << 1);");
			this.outstream.WriteLine("return result;");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			if (this.spec.CountLines || this.spec.CountColumns || this.spec.CountChars)
			{
                WriteCode(@"
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
");
			}
            WriteCode(
                @"
protected static bool IsNewLineCharacter(char ch)
{
    return ch == '\r' || ch == '\n' || ch == (char)0x2028 || ch == (char)0x2029;
}
");
 			this.outstream.WriteLine("private void TrimTokenEnd()");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("if (token_end > token_chunk_start && buffer[token_end - 1] == '\\n')");
			this.outstream.Indent++;
			this.outstream.WriteLine("token_end--;");
			this.outstream.Indent--;
			this.outstream.WriteLine("if (token_end > token_chunk_start && buffer[token_end - 1] == '\\r')");
			this.outstream.Indent++;
			this.outstream.WriteLine("token_end--;");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.Indent--;
			this.outstream.WriteLine();
			this.outstream.WriteLine("private void MarkTokenChunkStart()");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("token_chunk_start = lookahead_index;");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			this.outstream.WriteLine("private void MarkTokenEnd()");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("token_end = lookahead_index;");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			this.outstream.WriteLine("private void MoveToTokenEnd()");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("lookahead_index = token_end;");
			this.outstream.WriteLine("yy_at_bol = (token_end > token_chunk_start) && (buffer[token_end - 1] == '\\r' || buffer[token_end - 1] == '\\n');");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			this.outstream.WriteLine("public int TokenLength");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("get { return token_end - token_start; }");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			this.outstream.WriteLine("public int TokenChunkLength");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("get { return token_end - token_chunk_start; }");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			this.outstream.WriteLine("private void yymore()");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("if (!expanding_token)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("token_start = token_chunk_start;");
			this.outstream.WriteLine("expanding_token = true;");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			this.outstream.WriteLine("private void yyless(int count)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("lookahead_index = token_end = token_chunk_start + count;");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			if (this.spec.Version >= 2)
			{
				this.outstream.WriteLine("private Stack<LexicalStates> stateStack = new Stack<LexicalStates>(20);");
			}
			else
			{
				this.outstream.WriteLine("private Stack stateStack = new Stack(20);");
			}
			string arg = (this.spec.Version >= 2) ? "" : "(LexicalStates)";
			this.outstream.WriteLine();
			this.outstream.WriteLine("private void yy_push_state(LexicalStates state)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("stateStack.Push(current_lexical_state);");
			this.outstream.WriteLine("current_lexical_state = state;");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			this.outstream.WriteLine("private bool yy_pop_state()");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("if (stateStack.Count == 0) return false;");
			this.outstream.WriteLine("current_lexical_state = {0}stateStack.Pop();", arg);
			this.outstream.WriteLine("return true;");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			this.outstream.WriteLine("private LexicalStates yy_top_state()");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("return {0}stateStack.Peek();", arg);
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
		}
		private void Header()
		{
			this.outstream.WriteLine();
			this.outstream.Write("{0} class {1}", this.spec.ClassAttributes, this.spec.LexerName);
			if (this.spec.ImplementsName != null)
			{
				this.outstream.Write(" : ");
				this.outstream.Write(this.spec.ImplementsName);
			}
			this.outstream.WriteLine();
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("public enum LexicalStates");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			foreach (string current in this.spec.States.Keys)
			{
				this.outstream.WriteLine("{0} = {1},", current, this.spec.States[current]);
			}
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
		}
		private void EmitAcceptTable()
		{
			this.outstream.WriteLine("private static AcceptConditions[] acceptCondition = new AcceptConditions[]");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			for (int i = 0; i < this.spec.accept_list.Count; i++)
			{
				Accept accept = this.spec.accept_list[i];
				if (accept != null)
				{
					bool flag = (this.spec.anchor_array[i] & 1) != 0;
					bool flag2 = (this.spec.anchor_array[i] & 2) != 0;
					if (flag && flag2)
					{
						this.outstream.Write("AcceptConditions.AcceptOnStart | AcceptConditions.AcceptOnEnd");
					}
					else
					{
						if (flag)
						{
							this.outstream.Write("AcceptConditions.AcceptOnStart");
						}
						else
						{
							if (flag2)
							{
								this.outstream.Write("AcceptConditions.AcceptOnEnd");
							}
							else
							{
								this.outstream.Write("AcceptConditions.Accept");
							}
						}
					}
				}
				else
				{
					this.outstream.Write("AcceptConditions.NotAccept");
				}
				this.outstream.Write(", // ");
				this.outstream.WriteLine(i);
			}
			this.outstream.Indent--;
			this.outstream.WriteLine("};");
			this.outstream.WriteLine();
		}
		private void EmitTableCmap()
		{
			this.outstream.WriteLine("private static int[] colMap = new int[]");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			for (int i = 0; i < this.spec.ccls_map.Length; i++)
			{
				if (i > 0)
				{
					this.outstream.Write(", ");
					if (i % 16 == 0)
					{
						this.outstream.WriteLine();
					}
				}
				this.outstream.Write(this.spec.col_map[(int)this.spec.ccls_map[i]]);
			}
			this.outstream.WriteLine();
			this.outstream.Indent--;
			this.outstream.WriteLine("};");
			this.outstream.WriteLine();
		}
		private void EmitTableRmap()
		{
			this.outstream.WriteLine("private static int[] rowMap = new int[]");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			for (int i = 0; i < this.spec.row_map.Length; i++)
			{
				if (i > 0)
				{
					this.outstream.Write(", ");
					if (i % 16 == 0)
					{
						this.outstream.WriteLine();
					}
				}
				this.outstream.Write(this.spec.row_map[i]);
			}
			this.outstream.WriteLine();
			this.outstream.Indent--;
			this.outstream.WriteLine("};");
			this.outstream.WriteLine();
		}
		private void EmitTableNxt()
		{
			this.outstream.WriteLine("private static int[,] nextState = new int[,]");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			for (int i = 0; i < this.spec.dtrans_list.Count; i++)
			{
				DTrans dTrans = this.spec.dtrans_list[i];
				if (i > 0)
				{
					this.outstream.WriteLine(",");
				}
				this.outstream.Write("{ ");
				for (int j = 0; j < this.spec.dtrans_ncols; j++)
				{
					if (j > 0)
					{
						this.outstream.Write(", ");
					}
					this.outstream.Write(dTrans.GetDTrans(j));
				}
				this.outstream.Write(" }");
			}
			this.outstream.WriteLine();
			this.outstream.Indent--;
			this.outstream.WriteLine("};");
			this.outstream.WriteLine();
		}
		private void EmitTables()
		{
			this.outstream.WriteLine("#region Tables");
			this.outstream.WriteLine();
			this.EmitAcceptTable();
			this.EmitTableCmap();
			this.EmitTableRmap();
			this.EmitTableNxt();
			this.States();
			this.outstream.WriteLine();
			this.outstream.WriteLine("#endregion");
			this.outstream.WriteLine();
		}
		private void EmitEofTest()
		{
			if (this.spec.EofCode.Count > 0)
			{
				this.outstream.WriteLine("yy_do_eof();");
			}
			if (this.spec.SemanticValueType != null)
			{
				this.outstream.WriteLine("value = this.yylval;");
			}
			if (this.spec.integer_type)
			{
				this.outstream.WriteLine("return YYEOF;");
				return;
			}
			if (this.spec.EofTokenName != null)
			{
				this.outstream.WriteLine("return {0};", this.spec.EofTokenName);
				return;
			}
			this.outstream.WriteLine("return null;");
		}
		private void Driver()
		{
			this.EmitTables();
			string arg = this.spec.integer_type ? "int" : this.spec.TokenTypeName;
			if (this.spec.SemanticValueType != null)
			{
				this.outstream.WriteLine("public {0} {1}(out {2} value)", arg, this.spec.FunctionName, this.spec.SemanticValueType);
			}
			else
			{
				this.outstream.WriteLine("public {0} {1}()", arg, this.spec.FunctionName);
			}
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("int current_state = yy_state_dtrans[(int)current_lexical_state];");
			this.outstream.WriteLine("int last_accept_state = NoState;");
			this.outstream.WriteLine("bool is_initial_state = true;");
			if (this.spec.SemanticValueType != null)
			{
				this.outstream.WriteLine("yylval = new {0}();", this.spec.SemanticValueType);
			}
			this.outstream.WriteLine();
			this.outstream.WriteLine("MarkTokenChunkStart();");
			this.outstream.WriteLine("token_start = token_chunk_start;");
			this.outstream.WriteLine("expanding_token = false;");
			if (this.spec.CountChars || this.spec.CountLines || this.spec.CountColumns)
			{
				this.outstream.WriteLine("AdvanceEndPosition((token_end > 0) ? token_end - 1 : 0, token_start);");
				this.outstream.WriteLine();
				this.outstream.WriteLine("// capture token start position:");
				if (this.spec.CountChars)
				{
					this.outstream.WriteLine("token_start_pos.Char = token_end_pos.Char;");
				}
				if (this.spec.CountLines)
				{
					this.outstream.WriteLine("token_start_pos.Line = token_end_pos.Line;");
				}
				if (this.spec.CountColumns)
				{
					this.outstream.WriteLine("token_start_pos.Column = token_end_pos.Column;");
				}
			}
			this.outstream.WriteLine();
			this.outstream.WriteLine("if (acceptCondition[current_state] != AcceptConditions.NotAccept)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("last_accept_state = current_state;");
			this.outstream.WriteLine("MarkTokenEnd();");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			this.outstream.WriteLine("while (true)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("char lookahead = (is_initial_state && yy_at_bol) ? BOL : Advance();");
			this.outstream.WriteLine("int next_state = nextState[rowMap[current_state], colMap[lookahead]];");
			this.outstream.WriteLine();
			this.outstream.WriteLine("if (lookahead == EOF && is_initial_state)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.EmitEofTest();
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine("if (next_state != -1)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("current_state = next_state;");
			this.outstream.WriteLine("is_initial_state = false;");
			this.outstream.WriteLine();
			this.outstream.WriteLine("if (acceptCondition[current_state] != AcceptConditions.NotAccept)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("last_accept_state = current_state;");
			this.outstream.WriteLine("MarkTokenEnd();");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine("else");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("if (last_accept_state == NoState)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			if (this.spec.SemanticValueType != null)
			{
				this.outstream.WriteLine("value = this.yylval;");
			}
			if (this.spec.ErrorTokenName != null)
			{
				this.outstream.WriteLine("return {0};", this.spec.ErrorTokenName);
			}
			else
			{
				this.outstream.WriteLine("throw new System.ApplicationException(\"Lexical Error: Unmatched Input.\");");
			}
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine("else");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("if ((acceptCondition[last_accept_state] & AcceptConditions.AcceptOnEnd) != 0)");
			this.outstream.Indent++;
			this.outstream.WriteLine("TrimTokenEnd();");
			this.outstream.Indent--;
			this.outstream.WriteLine("MoveToTokenEnd();");
			this.outstream.WriteLine();
			this.outstream.WriteLine("if (last_accept_state < 0)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("System.Diagnostics.Debug.Assert(last_accept_state >= {0});", this.spec.accept_list.Count);
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine("else");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("bool accepted = false;");
			if (this.spec.VariantCount > 1)
			{
				this.outstream.WriteLine("switch (variant)");
				this.outstream.WriteLine("{");
				this.outstream.Indent++;
				for (int i = 0; i < this.spec.VariantCount; i++)
				{
					this.outstream.WriteLine("case {0}: yyreturn = Accept{0}(last_accept_state, out accepted); break;", i);
				}
				this.outstream.Indent--;
				this.outstream.WriteLine("}");
				this.outstream.WriteLine();
			}
			else
			{
				this.outstream.WriteLine("yyreturn = Accept0(last_accept_state, out accepted);");
			}
			this.outstream.WriteLine("if (accepted)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			if (this.spec.SemanticValueType != null)
			{
				this.outstream.WriteLine("value = yylval;");
			}
			if (this.spec.CountChars || this.spec.CountLines || this.spec.CountColumns)
			{
				this.outstream.WriteLine("AdvanceEndPosition(token_start, token_end - 1);");
			}
			this.outstream.WriteLine("return yyreturn;");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
			this.outstream.WriteLine("// token ignored:");
			this.outstream.WriteLine("is_initial_state = true;");
			this.outstream.WriteLine("current_state = yy_state_dtrans[(int)current_lexical_state];");
			this.outstream.WriteLine("last_accept_state = NoState;");
			this.outstream.WriteLine("MarkTokenChunkStart();");
			this.outstream.WriteLine("if (acceptCondition[current_state] != AcceptConditions.NotAccept)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("last_accept_state = current_state;");
			this.outstream.WriteLine("MarkTokenEnd();");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.Indent--;
			this.outstream.WriteLine("} // end of " + this.spec.FunctionName);
		}
		private void EmitAcceptMethods()
		{
			this.outstream.WriteLine("#region Accept");
			this.outstream.WriteLine();
			if (this.spec.Version >= 2)
			{
				this.outstream.WriteLine("#pragma warning disable 162");
				this.outstream.WriteLine();
			}
			for (int i = 0; i < this.spec.VariantCount; i++)
			{
				this.EmitAcceptVariant(i);
			}
			if (this.spec.Version >= 2)
			{
				this.outstream.WriteLine("#pragma warning restore 162");
				this.outstream.WriteLine();
			}
			this.outstream.WriteLine();
			this.outstream.WriteLine("#endregion");
		}
		private void EmitAcceptVariant(int variantIndex)
		{
			this.outstream.WriteLine();
			this.outstream.WriteLine("{0} Accept{1}(int state,out bool accepted)", this.spec.TokenTypeName, variantIndex);
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			this.outstream.WriteLine("accepted = true;");
			this.outstream.WriteLine();
			this.outstream.WriteLine("switch(state)");
			this.outstream.WriteLine("{");
			this.outstream.Indent++;
			Dictionary<int, int> dictionary = new Dictionary<int, int>();
			for (int i = 0; i < this.spec.accept_list.Count; i++)
			{
				if (this.spec.accept_list[i] != null)
				{
					List<CodeBlock> codeBlocks = this.spec.accept_list[i].CodeBlocks;
					if (codeBlocks != null)
					{
						CodeBlock codeBlock = codeBlocks[Math.Min(variantIndex, codeBlocks.Count - 1)];
						int firstLine = codeBlock.FirstLine;
						if (dictionary.ContainsKey(firstLine))
						{
							this.outstream.WriteLine("case {0}: goto case {1};", i, dictionary[firstLine]);
						}
						else
						{
							this.outstream.WriteLine("case {0}:", i);
							this.outstream.Indent++;
							dictionary.Add(firstLine, i);
							this.outstream.WriteLine("// #line {0}", firstLine);
							this.EmitUserCode(codeBlock.Code);
							if (this.spec.Version >= 2)
							{
								this.outstream.WriteLine("break;");
							}
							this.outstream.WriteLine();
							this.outstream.Indent--;
						}
					}
				}
			}
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine("accepted = false;");
			this.outstream.WriteLine("return yyreturn;");
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
			this.outstream.WriteLine();
		}
		private void Footer()
		{
			this.outstream.Indent--;
			this.outstream.WriteLine("}");
		}
        private void WriteCode(string/*!*/code)
        {
            var reader = new System.IO.StringReader(code);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                this.outstream.WriteLine(line);
            }
        }
	}
}
