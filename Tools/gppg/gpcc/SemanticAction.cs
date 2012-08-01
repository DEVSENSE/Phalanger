using System;
using System.Text;
namespace gpcc
{
	public class SemanticAction
	{
		private Production production;
		private int startLine;
		private int pos;
		private string commands;
		public SemanticAction(Production production, int startLine, int pos, string commands)
		{
			this.production = production;
			this.pos = pos;
			this.startLine = startLine;
			this.commands = commands;
		}
		public void GenerateCode(CodeGenerator codeGenerator)
		{
			int i = 0;
			int num = this.startLine;
			while (i < this.commands.Length)
			{
				char c = this.commands[i];
				if (c <= '$')
				{
					if (c == '\n')
					{
						num++;
						this.Output(codeGenerator, i++);
						continue;
					}
					switch (c)
					{
					case '"':
						this.Output(codeGenerator, i++);
						while (i < this.commands.Length && this.commands[i] != '"')
						{
							if (this.commands[i] == '\\')
							{
								this.Output(codeGenerator, i++);
							}
							if (i < this.commands.Length)
							{
								this.Output(codeGenerator, i++);
							}
						}
						if (i < this.commands.Length)
						{
							this.Output(codeGenerator, i++);
							continue;
						}
						continue;
					case '$':
					{
						i++;
						string kind = this.ParseKind(codeGenerator, num, ref i);
						this.ParseItemReference(codeGenerator, "yyval", kind, num, ref i);
						continue;
					}
					}
				}
				else
				{
					if (c != '\'')
					{
						if (c != '/')
						{
							if (c == '@')
							{
								if (i + 1 >= this.commands.Length || this.commands[i + 1] != '"')
								{
									i++;
									this.ParseItemReference(codeGenerator, "yypos", "", num, ref i);
									continue;
								}
								this.Output(codeGenerator, i++);
								this.Output(codeGenerator, i++);
								while (i < this.commands.Length && this.commands[i] != '"')
								{
									this.Output(codeGenerator, i++);
								}
								if (i < this.commands.Length)
								{
									this.Output(codeGenerator, i++);
									continue;
								}
								continue;
							}
						}
						else
						{
							this.Output(codeGenerator, i++);
							if (this.commands[i] == '/')
							{
								while (i < this.commands.Length && this.commands[i] != '\n')
								{
									this.Output(codeGenerator, i++);
								}
								if (i < this.commands.Length)
								{
									this.Output(codeGenerator, i++);
									continue;
								}
								continue;
							}
							else
							{
								if (this.commands[i] != '*')
								{
									continue;
								}
								this.Output(codeGenerator, i++);
								while (true)
								{
									if (i >= this.commands.Length || this.commands[i] == '*')
									{
										if (i < this.commands.Length)
										{
											this.Output(codeGenerator, i++);
										}
										if (i >= this.commands.Length || this.commands[i] == '/')
										{
											break;
										}
									}
									else
									{
										this.Output(codeGenerator, i++);
									}
								}
								if (i < this.commands.Length)
								{
									this.Output(codeGenerator, i++);
									continue;
								}
								continue;
							}
						}
					}
					else
					{
						this.Output(codeGenerator, i++);
						while (i < this.commands.Length && this.commands[i] != '\'')
						{
							if (this.commands[i] == '\\')
							{
								this.Output(codeGenerator, i++);
							}
							if (i < this.commands.Length)
							{
								this.Output(codeGenerator, i++);
							}
						}
						if (i < this.commands.Length)
						{
							this.Output(codeGenerator, i++);
							continue;
						}
						continue;
					}
				}
				this.Output(codeGenerator, i++);
			}
			codeGenerator.Output.WriteLine();
		}
		private string ParseKind(CodeGenerator codeGenerator, int line, ref int i)
		{
			if (this.commands[i] == '<')
			{
				i++;
				StringBuilder stringBuilder = new StringBuilder();
				while (i < this.commands.Length && this.commands[i] != '>')
				{
					stringBuilder.Append(this.commands[i]);
					i++;
				}
				if (i < this.commands.Length)
				{
					i++;
					return stringBuilder.ToString();
				}
				Console.Error.WriteLine("Expected '>' at ({0}:{1})", line, i);
			}
			return null;
		}
		private void ParseItemReference(CodeGenerator codeGenerator, string item, string kind, int line, ref int i)
		{
			if (this.commands[i] == '$')
			{
				i++;
				if (kind == null)
				{
					kind = this.production.lhs.kind;
				}
				codeGenerator.Output.Write(item);
				if (!string.IsNullOrEmpty(kind))
				{
					codeGenerator.Output.Write(".{0}", kind);
					return;
				}
			}
			else
			{
				if (char.IsDigit(this.commands[i]))
				{
					int num = (int)(this.commands[i] - '0');
					i++;
					while (i < this.commands.Length && this.commands[i] >= '0' && this.commands[i] <= '9')
					{
						num = 10 * num + (int)this.commands[i] - 48;
						i++;
					}
					if (num <= 0 || num > this.production.rhs.Count)
					{
						throw new Scanner.ParseException(line, this.pos, string.Format("Invalid production token number {0}.", num));
					}
					if (kind == null)
					{
						kind = this.production.rhs[num - 1].kind;
					}
					codeGenerator.Output.Write("value_stack.array[value_stack.top-{0}].{1}", this.pos - num + 1, item);
					if (!string.IsNullOrEmpty(kind))
					{
						codeGenerator.Output.Write(".{0}", kind);
						return;
					}
				}
				else
				{
					Console.Error.WriteLine("Unexpected '$' at ({0}:{1})", line, i);
				}
			}
		}
		private void Output(CodeGenerator codeGenerator, int i)
		{
			if (this.commands[i] == '\n')
			{
				codeGenerator.Output.WriteLine();
				return;
			}
			codeGenerator.Output.Write(this.commands[i]);
		}
	}
}
