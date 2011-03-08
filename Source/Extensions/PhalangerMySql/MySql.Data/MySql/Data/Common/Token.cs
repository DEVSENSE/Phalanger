namespace MySql.Data.Common
{
    using System;

    internal class Token
    {
        public bool Output;
        public string Text;
        public MySql.Data.Common.TokenType Type;

        public Token(MySql.Data.Common.TokenType type, string text)
        {
            this.Type = type;
            this.Text = text;
            this.Output = true;
        }

        public bool IsRealToken
        {
            get
            {
                return ((((this.Type != MySql.Data.Common.TokenType.Comment) && (this.Type != MySql.Data.Common.TokenType.CommandComment)) && (this.Type != MySql.Data.Common.TokenType.Whitespace)) && this.Output);
            }
        }
    }
}

