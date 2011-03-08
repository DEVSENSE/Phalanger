namespace MySql.Data.Common
{
    using System;
    using System.Collections;
    using System.Text;

    internal class ContextString
    {
        private string contextMarkers;
        private bool escapeBackslash;

        public ContextString(string contextMarkers, bool escapeBackslash)
        {
            this.contextMarkers = contextMarkers;
            this.escapeBackslash = escapeBackslash;
        }

        private bool IndexInQuotes(string src, int index, int startIndex)
        {
            char ch = '\0';
            bool flag = false;
            for (int i = startIndex; i < index; i++)
            {
                char ch2 = src[i];
                int num2 = this.contextMarkers.IndexOf(ch2);
                if (((num2 > -1) && (ch == this.contextMarkers[num2])) && !flag)
                {
                    ch = '\0';
                }
                else if (((ch == '\0') && (num2 > -1)) && !flag)
                {
                    ch = ch2;
                }
                else if ((ch2 == '\\') && this.escapeBackslash)
                {
                    flag = !flag;
                }
            }
            if (ch == '\0')
            {
                return flag;
            }
            return true;
        }

        public int IndexOf(string src, char target)
        {
            char ch = '\0';
            bool flag = false;
            int num = 0;
            foreach (char ch2 in src)
            {
                int index = this.contextMarkers.IndexOf(ch2);
                if (((index > -1) && (ch == this.contextMarkers[index])) && !flag)
                {
                    ch = '\0';
                }
                else if (((ch == '\0') && (index > -1)) && !flag)
                {
                    ch = ch2;
                }
                else
                {
                    if ((ch == '\0') && (ch2 == target))
                    {
                        return num;
                    }
                    if ((ch2 == '\\') && this.escapeBackslash)
                    {
                        flag = !flag;
                    }
                }
                num++;
            }
            return -1;
        }

        public int IndexOf(string src, string target)
        {
            return this.IndexOf(src, target, 0);
        }

        public int IndexOf(string src, string target, int startIndex)
        {
            int index = src.IndexOf(target, startIndex);
            while (index != -1)
            {
                if (!this.IndexInQuotes(src, index, startIndex))
                {
                    return index;
                }
                index = src.IndexOf(target, (int) (index + 1));
            }
            return index;
        }

        public string[] Split(string src, string delimiters)
        {
            ArrayList list = new ArrayList();
            StringBuilder builder = new StringBuilder();
            bool flag = false;
            char ch = '\0';
            foreach (char ch2 in src)
            {
                if ((delimiters.IndexOf(ch2) != -1) && !flag)
                {
                    if (ch != '\0')
                    {
                        builder.Append(ch2);
                    }
                    else if (builder.Length > 0)
                    {
                        list.Add(builder.ToString());
                        builder.Remove(0, builder.Length);
                    }
                }
                else if ((ch2 == '\\') && this.escapeBackslash)
                {
                    flag = !flag;
                }
                else
                {
                    int index = this.contextMarkers.IndexOf(ch2);
                    if (!flag && (index != -1))
                    {
                        if ((index % 2) == 1)
                        {
                            if (ch == this.contextMarkers[index - 1])
                            {
                                ch = '\0';
                            }
                        }
                        else if (ch == this.contextMarkers[index + 1])
                        {
                            ch = '\0';
                        }
                        else if (ch == '\0')
                        {
                            ch = ch2;
                        }
                    }
                    builder.Append(ch2);
                }
            }
            if (builder.Length > 0)
            {
                list.Add(builder.ToString());
            }
            return (string[]) list.ToArray(typeof(string));
        }

        public string ContextMarkers
        {
            get
            {
                return this.contextMarkers;
            }
            set
            {
                this.contextMarkers = value;
            }
        }
    }
}

