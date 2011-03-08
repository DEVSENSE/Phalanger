namespace MySql.Data.Common
{
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    internal class QueryNormalizer
    {
        private string fullSql;
        private static List<string> keywords = new List<string>();
        private int pos;
        private string queryType;
        private List<Token> tokens = new List<Token>();

        static QueryNormalizer()
        {
            StringReader reader = new StringReader(Resources.keywords);
            for (string str = reader.ReadLine(); str != null; str = reader.ReadLine())
            {
                keywords.Add(str);
            }
        }

        private void CollapseInList(List<Token> tok, ref int pos)
        {
            if (this.GetNextRealToken(tok, ref pos) != null)
            {
                Token nextRealToken = this.GetNextRealToken(tok, ref pos);
                if ((nextRealToken != null) && (nextRealToken.Type != MySql.Data.Common.TokenType.Keyword))
                {
                    int num = pos;
                    while (++pos < tok.Count)
                    {
                        nextRealToken = tok[pos];
                        if (nextRealToken.Type == MySql.Data.Common.TokenType.CommandComment)
                        {
                            return;
                        }
                        if (nextRealToken.IsRealToken)
                        {
                            if (nextRealToken.Text == "(")
                            {
                                return;
                            }
                            if (nextRealToken.Text == ")")
                            {
                                break;
                            }
                        }
                    }
                    int num2 = pos;
                    for (int i = num2; i > num; i--)
                    {
                        tok.RemoveAt(i);
                    }
                    tok.Insert(++num, new Token(MySql.Data.Common.TokenType.Whitespace, " "));
                    tok.Insert(++num, new Token(MySql.Data.Common.TokenType.Comment, "/* , ... */"));
                    tok.Insert(++num, new Token(MySql.Data.Common.TokenType.Whitespace, " "));
                    tok.Insert(++num, new Token(MySql.Data.Common.TokenType.Symbol, ")"));
                }
            }
        }

        private void CollapseInLists(List<Token> tok)
        {
            int pos = -1;
            while (++pos < tok.Count)
            {
                Token token = tok[pos];
                if ((token.Type == MySql.Data.Common.TokenType.Keyword) && (token.Text == "IN"))
                {
                    this.CollapseInList(tok, ref pos);
                }
            }
        }

        private void CollapseValueList(List<Token> tok, ref int pos)
        {
            List<int> list = new List<int>();
        Label_003D:
            while (++pos < tok.Count)
            {
                if (((tok[pos].Type == MySql.Data.Common.TokenType.Symbol) && (tok[pos].Text == ")")) || (pos == (tok.Count - 1)))
                {
                    break;
                }
            }
            list.Add(pos);
            while (++pos < tok.Count)
            {
                if (tok[pos].IsRealToken)
                {
                    break;
                }
            }
            if (pos != tok.Count)
            {
                if (!(tok[pos].Text != ","))
                {
                    goto Label_003D;
                }
                pos--;
            }
            if (list.Count >= 2)
            {
                int num = list[0];
                tok[++num] = new Token(MySql.Data.Common.TokenType.Whitespace, " ");
                tok[++num] = new Token(MySql.Data.Common.TokenType.Comment, "/* , ... */");
                num++;
                while (num <= list[list.Count - 1])
                {
                    tok[num++].Output = false;
                }
            }
        }

        private void CollapseValueLists(List<Token> tok)
        {
            int pos = -1;
            while (++pos < tok.Count)
            {
                Token token = tok[pos];
                if ((token.Type == MySql.Data.Common.TokenType.Keyword) && token.Text.StartsWith("VALUE"))
                {
                    this.CollapseValueList(tok, ref pos);
                }
            }
        }

        private void CollapseWhitespace(List<Token> tok)
        {
            Token token = null;
            foreach (Token token2 in tok)
            {
                if ((token2.Output && (token2.Type == MySql.Data.Common.TokenType.Whitespace)) && ((token != null) && (token.Type == MySql.Data.Common.TokenType.Whitespace)))
                {
                    token2.Output = false;
                }
                if (token2.Output)
                {
                    token = token2;
                }
            }
        }

        private bool ConsumeComment()
        {
            char ch = this.fullSql[this.pos];
            if ((ch == '/') && (((this.pos + 1) >= this.fullSql.Length) || (this.fullSql[this.pos + 1] != '*')))
            {
                return false;
            }
            if ((ch == '-') && ((((this.pos + 2) >= this.fullSql.Length) || (this.fullSql[this.pos + 1] != '-')) || (this.fullSql[this.pos + 2] != ' ')))
            {
                return false;
            }
            string str = "\n";
            if (ch == '/')
            {
                str = "*/";
            }
            int index = this.fullSql.IndexOf(str, this.pos);
            if (index == -1)
            {
                index = this.fullSql.Length - 1;
            }
            else
            {
                index += str.Length;
            }
            string text = this.fullSql.Substring(this.pos, index - this.pos);
            if (text.StartsWith("/*!"))
            {
                this.tokens.Add(new Token(MySql.Data.Common.TokenType.CommandComment, text));
            }
            this.pos = index;
            return true;
        }

        private void ConsumeQuotedToken(char c)
        {
            bool flag = false;
            int pos = this.pos;
            this.pos++;
            while (this.pos < this.fullSql.Length)
            {
                char ch = this.fullSql[this.pos];
                if ((ch == c) && !flag)
                {
                    break;
                }
                if (flag)
                {
                    flag = false;
                }
                else if (ch == '\\')
                {
                    flag = true;
                }
                this.pos++;
            }
            this.pos++;
            if (c == '\'')
            {
                this.tokens.Add(new Token(MySql.Data.Common.TokenType.String, "?"));
            }
            else
            {
                this.tokens.Add(new Token(MySql.Data.Common.TokenType.Identifier, this.fullSql.Substring(pos, this.pos - pos)));
            }
        }

        private void ConsumeSymbol()
        {
            char ch = this.fullSql[this.pos++];
            this.tokens.Add(new Token(MySql.Data.Common.TokenType.Symbol, ch.ToString()));
        }

        private void ConsumeUnquotedToken()
        {
            double num2;
            int pos = this.pos;
            while ((this.pos < this.fullSql.Length) && !this.IsSpecialCharacter(this.fullSql[this.pos]))
            {
                this.pos++;
            }
            string s = this.fullSql.Substring(pos, this.pos - pos);
            if (double.TryParse(s, out num2))
            {
                this.tokens.Add(new Token(MySql.Data.Common.TokenType.Number, "?"));
            }
            else
            {
                Token item = new Token(MySql.Data.Common.TokenType.Identifier, s);
                if (this.IsKeyword(s))
                {
                    item.Type = MySql.Data.Common.TokenType.Keyword;
                    item.Text = item.Text.ToUpperInvariant();
                }
                this.tokens.Add(item);
            }
        }

        private void ConsumeWhitespace()
        {
            this.tokens.Add(new Token(MySql.Data.Common.TokenType.Whitespace, " "));
            while ((this.pos < this.fullSql.Length) && char.IsWhiteSpace(this.fullSql[this.pos]))
            {
                this.pos++;
            }
        }

        private void DetermineStatementType(List<Token> tok)
        {
            foreach (Token token in tok)
            {
                if (token.Type == MySql.Data.Common.TokenType.Keyword)
                {
                    this.queryType = token.Text.ToUpperInvariant();
                    break;
                }
            }
        }

        private Token GetNextRealToken(List<Token> tok, ref int pos)
        {
            while (++pos < tok.Count)
            {
                if (tok[pos].IsRealToken)
                {
                    return tok[pos];
                }
            }
            return null;
        }

        private bool IsKeyword(string word)
        {
            return keywords.Contains(word.ToUpperInvariant());
        }

        private bool IsSpecialCharacter(char c)
        {
            return ((!char.IsLetterOrDigit(c) && (c != '$')) && ((c != '_') && (c != '.')));
        }

        private bool LetterStartsComment(char c)
        {
            if ((c != '#') && (c != '/'))
            {
                return (c == '-');
            }
            return true;
        }

        public string Normalize(string sql)
        {
            this.tokens.Clear();
            StringBuilder builder = new StringBuilder();
            this.fullSql = sql;
            this.TokenizeSql(sql);
            this.DetermineStatementType(this.tokens);
            this.ProcessMathSymbols(this.tokens);
            this.CollapseValueLists(this.tokens);
            this.CollapseInLists(this.tokens);
            this.CollapseWhitespace(this.tokens);
            foreach (Token token in this.tokens)
            {
                if (token.Output)
                {
                    builder.Append(token.Text);
                }
            }
            return builder.ToString();
        }

        private void ProcessMathSymbols(List<Token> tok)
        {
            Token token = null;
            foreach (Token token2 in tok)
            {
                if (((token2.Type == MySql.Data.Common.TokenType.Symbol) && ((token2.Text == "-") || (token2.Text == "+"))) && ((((token != null) && (token.Type != MySql.Data.Common.TokenType.Number)) && (token.Type != MySql.Data.Common.TokenType.Identifier)) && ((token.Type != MySql.Data.Common.TokenType.Symbol) || (token.Text != ")"))))
                {
                    token2.Output = false;
                }
                if (token2.IsRealToken)
                {
                    token = token2;
                }
            }
        }

        private void TokenizeSql(string sql)
        {
            this.pos = 0;
            while (this.pos < sql.Length)
            {
                char c = sql[this.pos];
                if (!this.LetterStartsComment(c) || !this.ConsumeComment())
                {
                    if (char.IsWhiteSpace(c))
                    {
                        this.ConsumeWhitespace();
                    }
                    else
                    {
                        if (((c == '\'') || (c == '"')) || (c == '`'))
                        {
                            this.ConsumeQuotedToken(c);
                            continue;
                        }
                        if (!this.IsSpecialCharacter(c))
                        {
                            this.ConsumeUnquotedToken();
                        }
                        else
                        {
                            this.ConsumeSymbol();
                        }
                    }
                }
            }
        }

        public string QueryType
        {
            get
            {
                return this.queryType;
            }
        }
    }
}

