namespace MySql.Data.Common
{
    using System;

    internal enum TokenType
    {
        Keyword,
        String,
        Number,
        Symbol,
        Identifier,
        Comment,
        CommandComment,
        Whitespace
    }
}

