using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PHP.Library.GetText.GetTextSharp
{
    public class PoParser
    {
        public void Parse(TextReader reader, IGettextParserRequestor requestor)
        {
            int num = 1;
            StringBuilder stringBuilder = null;
            StringBuilder stringBuilder2 = null;
            while (true)
            {
                string text = reader.ReadLine();
                text = ((text == null) ? null : text.Trim());
                if (text == null || text.Length == 0)
                {
                    if (num == 3 && stringBuilder != null && stringBuilder2 != null)
                    {
                        requestor.Handle(stringBuilder.ToString().Replace("\\n", "\n"), stringBuilder2.ToString().Replace("\\n", "\n"));
                        stringBuilder = null;
                        stringBuilder2 = null;
                    }
                    if (text == null)
                    {
                        break;
                    }
                    num = 1;
                }
                else
                {
                    if (text[0] != '#')
                    {
                        bool flag = text.StartsWith("msgid ");
                        bool flag2 = !flag && text.StartsWith("msgstr ");
                        if (flag || flag2)
                        {
                            num = (flag ? 2 : 3);
                            int num2 = text.IndexOf('"');
                            if (num2 != -1)
                            {
                                int num3 = text.IndexOf('"', num2 + 1);
                                if (num3 != -1)
                                {
                                    string value = text.Substring(num2 + 1, num3 - num2 - 1);
                                    if (flag)
                                    {
                                        stringBuilder = new StringBuilder();
                                        stringBuilder.Append(value);
                                    }
                                    else
                                    {
                                        stringBuilder2 = new StringBuilder();
                                        stringBuilder2.Append(value);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (text[0] == '"')
                            {
                                if (text[text.Length - 1] == '"')
                                {
                                    text = text.Substring(1, text.Length - 2);
                                }
                                else
                                {
                                    text = text.Substring(1, text.Length - 1);
                                }
                                switch (num)
                                {
                                    case 2:
                                        {
                                            stringBuilder.Append(text);
                                            break;
                                        }
                                    case 3:
                                        {
                                            stringBuilder2.Append(text);
                                            break;
                                        }
                                }
                            }
                        }
                    }
                }
            }
        }
        public void Parse(string text, IGettextParserRequestor requestor)
        {
            this.Parse(new StringReader(text), requestor);
        }
        public Dictionary<string, string> ParseIntoDictionary(TextReader reader)
        {
            DictionaryGettextParserRequestor dictionaryGettextParserRequestor = new DictionaryGettextParserRequestor();
            this.Parse(reader, dictionaryGettextParserRequestor);
            return dictionaryGettextParserRequestor;
        }
    }
}