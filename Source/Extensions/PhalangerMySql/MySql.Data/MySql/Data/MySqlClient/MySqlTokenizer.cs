namespace MySql.Data.MySqlClient
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal class MySqlTokenizer
    {
        private bool ansiQuotes;
        private bool backslashEscapes;
        private bool isComment;
        private bool multiLine;
        private int pos;
        private bool quoted;
        private bool returnComments;
        private string sql;
        private bool sqlServerMode;
        private int startIndex;
        private int stopIndex;

        public MySqlTokenizer()
        {
            this.backslashEscapes = true;
            this.multiLine = true;
            this.pos = 0;
        }

        public MySqlTokenizer(string input) : this()
        {
            this.sql = input;
        }

        private void CalculatePosition(int start, int stop)
        {
            this.startIndex = start;
            this.stopIndex = stop;
            bool multiLine = this.MultiLine;
        }

        public bool FindToken()
        {
            this.isComment = this.quoted = false;
            this.startIndex = this.stopIndex = -1;
            while (this.pos < this.sql.Length)
            {
                char c = this.sql[this.pos++];
                if (!char.IsWhiteSpace(c))
                {
                    if (((c == '`') || (c == '\'')) || ((c == '"') || ((c == '[') && this.SqlServerMode)))
                    {
                        this.ReadQuotedToken(c);
                    }
                    else if (((c == '#') || (c == '-')) || (c == '/'))
                    {
                        if (!this.ReadComment(c))
                        {
                            this.ReadSpecialToken();
                        }
                    }
                    else
                    {
                        this.ReadUnquotedToken();
                    }
                    if (this.startIndex != -1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public List<string> GetAllTokens()
        {
            List<string> list = new List<string>();
            for (string str = this.NextToken(); str != null; str = this.NextToken())
            {
                list.Add(str);
            }
            return list;
        }

        public static bool IsParameter(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }
            return ((s[0] == '?') || (((s.Length > 1) && (s[0] == '@')) && (s[1] != '@')));
        }

        private bool IsParameterMarker(char c)
        {
            if (c != '@')
            {
                return (c == '?');
            }
            return true;
        }

        private bool IsQuoteChar(char c)
        {
            if ((c != '`') && (c != '\''))
            {
                return (c == '"');
            }
            return true;
        }

        private bool IsSpecialCharacter(char c)
        {
            if ((char.IsLetterOrDigit(c) || (c == '$')) || ((c == '_') || (c == '.')))
            {
                return false;
            }
            if (this.IsParameterMarker(c))
            {
                return false;
            }
            return true;
        }

        public string NextParameter()
        {
            while (this.FindToken())
            {
                if ((this.stopIndex - this.startIndex) >= 2)
                {
                    this.sql.Substring(this.startIndex, this.stopIndex - this.startIndex).Trim();
                    char ch = this.sql[this.startIndex];
                    char ch2 = this.sql[this.startIndex + 1];
                    if ((ch == '?') || ((ch == '@') && (ch2 != '@')))
                    {
                        return this.sql.Substring(this.startIndex, this.stopIndex - this.startIndex);
                    }
                }
            }
            return null;
        }

        public string NextToken()
        {
            while (this.FindToken())
            {
                return this.sql.Substring(this.startIndex, this.stopIndex - this.startIndex);
            }
            return null;
        }

        private bool ReadComment(char c)
        {
            if ((c == '/') && ((this.pos >= this.sql.Length) || (this.sql[this.pos] != '*')))
            {
                return false;
            }
            if ((c == '-') && ((((this.pos + 1) >= this.sql.Length) || (this.sql[this.pos] != '-')) || (this.sql[this.pos + 1] != ' ')))
            {
                return false;
            }
            string str = "\n";
            if (this.sql[this.pos] == '*')
            {
                str = "*/";
            }
            int num = this.pos - 1;
            int index = this.sql.IndexOf(str, this.pos);
            if (str == "\n")
            {
                index = this.sql.IndexOf('\n', this.pos);
            }
            if (index == -1)
            {
                index = this.sql.Length - 1;
            }
            else
            {
                index += str.Length;
            }
            this.pos = index;
            if (this.ReturnComments)
            {
                this.startIndex = num;
                this.stopIndex = index;
                this.isComment = true;
            }
            return true;
        }

        public string ReadParenthesis()
        {
            StringBuilder builder = new StringBuilder("(");
            int startIndex = this.StartIndex;
            string str = this.NextToken();
            while (true)
            {
                if (str == null)
                {
                    throw new InvalidOperationException("Unable to parse SQL");
                }
                builder.Append(str);
                if ((str == ")") && !this.Quoted)
                {
                    return builder.ToString();
                }
                str = this.NextToken();
            }
        }

        private void ReadQuotedToken(char quoteChar)
        {
            if (quoteChar == '[')
            {
                quoteChar = ']';
            }
            this.startIndex = this.pos - 1;
            bool flag = false;
            bool flag2 = false;
            while (this.pos < this.sql.Length)
            {
                char ch = this.sql[this.pos];
                if ((ch == quoteChar) && !flag)
                {
                    flag2 = true;
                    break;
                }
                if (flag)
                {
                    flag = false;
                }
                else if ((ch == '\\') && this.BackslashEscapes)
                {
                    flag = true;
                }
                this.pos++;
            }
            if (flag2)
            {
                this.pos++;
            }
            this.Quoted = flag2;
            this.stopIndex = this.pos;
        }

        private void ReadSpecialToken()
        {
            this.startIndex = this.pos - 1;
            this.stopIndex = this.pos;
            this.Quoted = false;
        }

        private void ReadUnquotedToken()
        {
            this.startIndex = this.pos - 1;
            if (!this.IsSpecialCharacter(this.sql[this.startIndex]))
            {
                while (this.pos < this.sql.Length)
                {
                    char c = this.sql[this.pos];
                    if (char.IsWhiteSpace(c) || this.IsSpecialCharacter(c))
                    {
                        break;
                    }
                    this.pos++;
                }
            }
            this.Quoted = false;
            this.stopIndex = this.pos;
        }

        public bool AnsiQuotes
        {
            get
            {
                return this.ansiQuotes;
            }
            set
            {
                this.ansiQuotes = value;
            }
        }

        public bool BackslashEscapes
        {
            get
            {
                return this.backslashEscapes;
            }
            set
            {
                this.backslashEscapes = value;
            }
        }

        public bool IsComment
        {
            get
            {
                return this.isComment;
            }
        }

        public bool MultiLine
        {
            get
            {
                return this.multiLine;
            }
            set
            {
                this.multiLine = value;
            }
        }

        public int Position
        {
            get
            {
                return this.pos;
            }
            set
            {
                this.pos = value;
            }
        }

        public bool Quoted
        {
            get
            {
                return this.quoted;
            }
            private set
            {
                this.quoted = value;
            }
        }

        public bool ReturnComments
        {
            get
            {
                return this.returnComments;
            }
            set
            {
                this.returnComments = value;
            }
        }

        public bool SqlServerMode
        {
            get
            {
                return this.sqlServerMode;
            }
            set
            {
                this.sqlServerMode = value;
            }
        }

        public int StartIndex
        {
            get
            {
                return this.startIndex;
            }
            set
            {
                this.startIndex = value;
            }
        }

        public int StopIndex
        {
            get
            {
                return this.stopIndex;
            }
            set
            {
                this.stopIndex = value;
            }
        }

        public string Text
        {
            get
            {
                return this.sql;
            }
            set
            {
                this.sql = value;
                this.pos = 0;
            }
        }
    }
}

