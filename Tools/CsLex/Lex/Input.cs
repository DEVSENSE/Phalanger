using System;
using System.IO;
namespace Lex
{
	public class Input
	{
		private const int BUFFER_SIZE = 1024;
		private const bool EOF = true;
		private const bool NOT_EOF = false;
		private TextReader instream;
		public bool eof_reached;
		public bool pushback_line;
		public char[] line;
		public int line_read;
		public int line_index;
		public int line_number;
		public Input(TextReader ihandle)
		{
			this.instream = ihandle;
			this.line = new char[1024];
			this.line_read = 0;
			this.line_index = 0;
			this.eof_reached = false;
			this.line_number = 0;
			this.pushback_line = false;
		}
		public bool GetLine()
		{
			if (this.eof_reached)
			{
				return true;
			}
			if (this.pushback_line)
			{
				this.pushback_line = false;
				int num = 0;
				while (num < this.line_read && char.IsWhiteSpace(this.line[num]))
				{
					num++;
				}
				if (num < this.line_read)
				{
					this.line_index = 0;
					return false;
				}
			}
			while (true)
			{
				string text = this.instream.ReadLine();
				if (text == null)
				{
					break;
				}
				text += "\n";
				this.line = text.ToCharArray();
				this.line_read = this.line.Length;
				this.line_number++;
				int num = 0;
				while (char.IsWhiteSpace(this.line[num]))
				{
					num++;
					if (num == this.line_read)
					{
						break;
					}
				}
				if (num < this.line_read)
				{
					goto Block_6;
				}
			}
			this.eof_reached = true;
			this.line_index = 0;
			return true;
			Block_6:
			this.line_index = 0;
			return false;
		}
		public string ReadDirective()
		{
			int num = 1;
			while (num < this.line_read && char.IsLetter(this.line[num]))
			{
				num++;
			}
			if ((num < this.line_read && this.line[num] == '{') || this.line[num] == '}')
			{
				num++;
			}
			return new string(this.line, 0, num);
		}
	}
}
