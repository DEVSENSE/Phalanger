using System;
using System.Text;
namespace gpcc
{
	public class Parser
	{
		private Grammar grammar;
		private int tokenStartLine;
		private int tokenStartColumn;
		private GrammarToken token;
		private Scanner scanner;
		public Grammar Parse(string filename)
		{
			this.scanner = new Scanner(filename);
			this.grammar = new Grammar();
			this.Advance();
			this.ParseHeader();
			this.ParseDeclarations();
			this.ParseProductions();
			this.ParseEpilog();
			return this.grammar;
		}
		private void ParseDeclarations()
		{
			int num = 0;
			while (this.token != GrammarToken.EndOfSection && this.token != GrammarToken.Eof)
			{
				switch (this.token)
				{
				case GrammarToken.Union:
					this.grammar.unionType = this.scanner.yylval;
					this.Advance();
					continue;
				case GrammarToken.Type:
				{
					this.Advance();
					string kind = null;
					if (this.token == GrammarToken.Kind)
					{
						kind = this.scanner.yylval;
						this.Advance();
					}
					while (this.token == GrammarToken.Symbol)
					{
						NonTerminal nonTerminal = this.grammar.LookupNonTerminal(this.scanner.yylval);
						nonTerminal.kind = kind;
						this.Advance();
					}
					continue;
				}
				case GrammarToken.Token:
				{
					this.Advance();
					string kind2 = null;
					if (this.token == GrammarToken.Kind)
					{
						kind2 = this.scanner.yylval;
						this.Advance();
					}
					while (this.token == GrammarToken.Symbol)
					{
						Terminal terminal = this.grammar.LookupTerminal(this.token, this.scanner.yylval);
						terminal.kind = kind2;
						this.Advance();
					}
					continue;
				}
				case GrammarToken.Left:
					this.Advance();
					num += 10;
					while (this.token == GrammarToken.Symbol || this.token == GrammarToken.Literal)
					{
						Terminal terminal2 = this.grammar.LookupTerminal(this.token, this.scanner.yylval);
						terminal2.prec = new Precedence(PrecType.left, num);
						this.Advance();
					}
					continue;
				case GrammarToken.Right:
					this.Advance();
					num += 10;
					while (this.token == GrammarToken.Symbol || this.token == GrammarToken.Literal)
					{
						Terminal terminal3 = this.grammar.LookupTerminal(this.token, this.scanner.yylval);
						terminal3.prec = new Precedence(PrecType.right, num);
						this.Advance();
					}
					continue;
				case GrammarToken.NonAssoc:
					this.Advance();
					num += 10;
					while (this.token == GrammarToken.Symbol || this.token == GrammarToken.Literal)
					{
						Terminal terminal4 = this.grammar.LookupTerminal(this.token, this.scanner.yylval);
						terminal4.prec = new Precedence(PrecType.nonassoc, num);
						this.Advance();
					}
					continue;
				case GrammarToken.Prolog:
				{
					Grammar expr_7B = this.grammar;
					expr_7B.prologCode += this.scanner.yylval;
					this.Advance();
					continue;
				}
				case GrammarToken.Start:
					this.Advance();
					if (this.token == GrammarToken.Symbol)
					{
						this.grammar.startSymbol = this.grammar.LookupNonTerminal(this.scanner.yylval);
						this.Advance();
						continue;
					}
					continue;
				case GrammarToken.Namespace:
					this.Advance();
					this.grammar.Namespace = this.scanner.yylval;
					this.Advance();
					while (this.scanner.yylval == ".")
					{
						this.Advance();
						Grammar expr_2FF = this.grammar;
						expr_2FF.Namespace = expr_2FF.Namespace + "." + this.scanner.yylval;
						this.Advance();
					}
					continue;
				case GrammarToken.Visibility:
					this.Advance();
					this.grammar.Visibility = this.scanner.yylval;
					this.Advance();
					continue;
				case GrammarToken.Attributes:
				{
					this.Advance();
					StringBuilder stringBuilder = new StringBuilder(this.scanner.yylval);
					while (this.Advance() == GrammarToken.Symbol)
					{
						stringBuilder.Append(' ');
						stringBuilder.Append(this.scanner.yylval);
					}
					this.grammar.Attributes = stringBuilder.ToString();
					continue;
				}
				case GrammarToken.ParserName:
					this.Advance();
					this.grammar.ParserName = this.scanner.yylval;
					this.Advance();
					continue;
				case GrammarToken.TokenName:
					this.Advance();
					this.grammar.TokenName = this.scanner.yylval;
					this.Advance();
					continue;
				case GrammarToken.ValueTypeName:
					this.Advance();
					this.grammar.ValueTypeName = this.scanner.yylval;
					this.Advance();
					continue;
				case GrammarToken.PositionType:
					this.Advance();
					this.grammar.PositionType = this.scanner.yylval;
					this.Advance();
					continue;
				}
				this.scanner.ReportError("Unexpected token {0} in declaration section", new object[]
				{
					this.token
				});
				this.Advance();
			}
			this.Advance();
		}
		private void ParseProductions()
		{
			while (this.token != GrammarToken.EndOfSection && this.token != GrammarToken.Eof)
			{
				while (this.token == GrammarToken.Symbol)
				{
					this.ParseProduction();
				}
			}
			this.Advance();
		}
		private void ParseProduction()
		{
			NonTerminal nonTerminal = null;
			if (this.token == GrammarToken.Symbol)
			{
				nonTerminal = this.grammar.LookupNonTerminal(this.scanner.yylval);
				if (this.grammar.startSymbol == null)
				{
					this.grammar.startSymbol = nonTerminal;
				}
				if (this.grammar.productions.Count == 0)
				{
					this.grammar.CreateSpecialProduction(this.grammar.startSymbol);
				}
			}
			else
			{
				this.scanner.ReportError("lhs symbol expected", new object[0]);
			}
			this.Advance();
			if (this.token != GrammarToken.Colon)
			{
				this.scanner.ReportError("Colon expected", new object[0]);
			}
			else
			{
				this.Advance();
			}
			this.ParseRhs(nonTerminal);
			while (this.token == GrammarToken.Divider)
			{
				this.Advance();
				this.ParseRhs(nonTerminal);
			}
			if (this.token != GrammarToken.SemiColon)
			{
				this.scanner.ReportError("Semicolon expected", new object[0]);
				return;
			}
			this.Advance();
		}
		private void ParseRhs(NonTerminal lhs)
		{
			Production production = new Production(lhs);
			int num = 0;
			while (this.token == GrammarToken.Symbol || this.token == GrammarToken.Literal || this.token == GrammarToken.Action || this.token == GrammarToken.Prec)
			{
				GrammarToken grammarToken = this.token;
				switch (grammarToken)
				{
				case GrammarToken.Symbol:
					if (this.grammar.terminals.ContainsKey(this.scanner.yylval))
					{
						production.rhs.Add(this.grammar.terminals[this.scanner.yylval]);
					}
					else
					{
						production.rhs.Add(this.grammar.LookupNonTerminal(this.scanner.yylval));
					}
					this.Advance();
					num++;
					break;
				case GrammarToken.Literal:
					production.rhs.Add(this.grammar.LookupTerminal(this.token, this.scanner.yylval));
					this.Advance();
					num++;
					break;
				case GrammarToken.Action:
				{
					SemanticAction semanticAction = new SemanticAction(production, this.tokenStartLine, num, this.scanner.yylval);
					this.Advance();
					if (this.token == GrammarToken.Divider || this.token == GrammarToken.SemiColon || this.token == GrammarToken.Prec)
					{
						production.semanticAction = semanticAction;
					}
					else
					{
						Grammar arg_1BA_0 = this.grammar;
						string arg_1B5_0 = "@";
						int num2 = ++this.grammar.NumActions;
						NonTerminal nonTerminal = arg_1BA_0.LookupNonTerminal(arg_1B5_0 + num2.ToString());
						Production production2 = new Production(nonTerminal);
						production2.semanticAction = semanticAction;
						this.grammar.AddProduction(production2);
						production.rhs.Add(nonTerminal);
					}
					num++;
					break;
				}
				default:
					if (grammarToken == GrammarToken.Prec)
					{
						this.Advance();
						if (this.token == GrammarToken.Symbol)
						{
							production.prec = this.grammar.LookupTerminal(this.token, this.scanner.yylval).prec;
							this.Advance();
						}
						else
						{
							this.scanner.ReportError("Expected symbol after %prec", new object[0]);
						}
					}
					break;
				}
			}
			this.grammar.AddProduction(production);
			Precedence.Calculate(production);
		}
		private void ParseHeader()
		{
			if (this.token == GrammarToken.Prelude)
			{
				this.grammar.headerCode = this.scanner.yylval;
				this.Advance();
			}
			if (this.token != GrammarToken.EndOfSection)
			{
				this.scanner.ReportError("Expected next section", new object[0]);
			}
			this.Advance();
		}
		private void ParseEpilog()
		{
			this.grammar.epilogCode = this.scanner.yylval;
			this.Advance();
			if (this.token != GrammarToken.Eof)
			{
				this.scanner.ReportError("Expected EOF", new object[0]);
			}
		}
		private GrammarToken Advance()
		{
			this.tokenStartLine = this.scanner.CurrentLine;
			this.tokenStartColumn = this.scanner.CurrentColumn;
			return this.token = this.scanner.Next();
		}
	}
}
