namespace MySql.Data.MySqlClient
{
    using MySql.Data.Common;
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal class CharSetMap
    {
        private static Dictionary<string, string> defaultCollations;
        private static object lockObject = new object();
        private static Dictionary<string, CharacterSet> mapping;
        private static Dictionary<string, int> maxLengths;

        static CharSetMap()
        {
            InitializeMapping();
        }

        internal static string GetDefaultCollation(string charset, MySqlConnection connection)
        {
            lock (lockObject)
            {
                if (defaultCollations == null)
                {
                    InitCollections(connection);
                }
            }
            if (!defaultCollations.ContainsKey(charset))
            {
                return null;
            }
            return defaultCollations[charset];
        }

        public static Encoding GetEncoding(DBVersion version, string CharSetName)
        {
            try
            {
                return Encoding.GetEncoding(GetCharacterSet(version, CharSetName).name);
            }
            catch (NotSupportedException)
            {
                return Encoding.GetEncoding(0);
            }
        }

        public static CharacterSet GetCharacterSet(DBVersion version, string CharSetName)
        {
            CharacterSet set = mapping[CharSetName];
            if (set == null)
            {
                throw new MySqlException("Character set '" + CharSetName + "' is not supported");
            }
            return set;
        }

        internal static int GetMaxLength(string charset, MySqlConnection connection)
        {
            lock (lockObject)
            {
                if (maxLengths == null)
                {
                    InitCollections(connection);
                }
            }
            if (!maxLengths.ContainsKey(charset))
            {
                return 1;
            }
            return maxLengths[charset];
        }

        internal static void InitCollections(MySqlConnection connection)
        {
            defaultCollations = new Dictionary<string, string>();
            maxLengths = new Dictionary<string, int>();
            MySqlCommand command = new MySqlCommand("SHOW CHARSET", connection);
            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    defaultCollations.Add(reader.GetString(0), reader.GetString(2));
                    maxLengths.Add(reader.GetString(0), Convert.ToInt32(reader.GetValue(3)));
                }
            }
        }

        private static void InitializeMapping()
        {
            LoadCharsetMap();
        }

        private static void LoadCharsetMap()
        {
            mapping = new Dictionary<string, CharacterSet>();
            mapping.Add("latin1", new CharacterSet("windows-1252", 1));
            mapping.Add("big5", new CharacterSet("big5", 2));
            mapping.Add("dec8", mapping["latin1"]);
            mapping.Add("cp850", new CharacterSet("ibm850", 1));
            mapping.Add("hp8", mapping["latin1"]);
            mapping.Add("koi8r", new CharacterSet("koi8-u", 1));
            mapping.Add("latin2", new CharacterSet("latin2", 1));
            mapping.Add("swe7", mapping["latin1"]);
            mapping.Add("ujis", new CharacterSet("EUC-JP", 3));
            mapping.Add("eucjpms", mapping["ujis"]);
            mapping.Add("sjis", new CharacterSet("sjis", 2));
            mapping.Add("cp932", mapping["sjis"]);
            mapping.Add("hebrew", new CharacterSet("hebrew", 1));
            mapping.Add("tis620", new CharacterSet("windows-874", 1));
            mapping.Add("euckr", new CharacterSet("euc-kr", 2));
            mapping.Add("euc_kr", mapping["euckr"]);
            mapping.Add("koi8u", new CharacterSet("koi8-u", 1));
            mapping.Add("koi8_ru", mapping["koi8u"]);
            mapping.Add("gb2312", new CharacterSet("gb2312", 2));
            mapping.Add("gbk", mapping["gb2312"]);
            mapping.Add("greek", new CharacterSet("greek", 1));
            mapping.Add("cp1250", new CharacterSet("windows-1250", 1));
            mapping.Add("win1250", mapping["cp1250"]);
            mapping.Add("latin5", new CharacterSet("latin5", 1));
            mapping.Add("armscii8", mapping["latin1"]);
            mapping.Add("utf8", new CharacterSet("utf-8", 3));
            mapping.Add("ucs2", new CharacterSet("UTF-16BE", 2));
            mapping.Add("cp866", new CharacterSet("cp866", 1));
            mapping.Add("keybcs2", mapping["latin1"]);
            mapping.Add("macce", new CharacterSet("x-mac-ce", 1));
            mapping.Add("macroman", new CharacterSet("x-mac-romanian", 1));
            mapping.Add("cp852", new CharacterSet("ibm852", 2));
            mapping.Add("latin7", new CharacterSet("iso-8859-7", 1));
            mapping.Add("cp1251", new CharacterSet("windows-1251", 1));
            mapping.Add("win1251ukr", mapping["cp1251"]);
            mapping.Add("cp1251csas", mapping["cp1251"]);
            mapping.Add("cp1251cias", mapping["cp1251"]);
            mapping.Add("win1251", mapping["cp1251"]);
            mapping.Add("cp1256", new CharacterSet("cp1256", 1));
            mapping.Add("cp1257", new CharacterSet("windows-1257", 1));
            mapping.Add("ascii", new CharacterSet("us-ascii", 1));
            mapping.Add("usa7", mapping["ascii"]);
            mapping.Add("binary", mapping["ascii"]);
            mapping.Add("latin3", new CharacterSet("latin3", 1));
            mapping.Add("latin4", new CharacterSet("latin4", 1));
            mapping.Add("latin1_de", new CharacterSet("iso-8859-1", 1));
            mapping.Add("german1", new CharacterSet("iso-8859-1", 1));
            mapping.Add("danish", new CharacterSet("iso-8859-1", 1));
            mapping.Add("czech", new CharacterSet("iso-8859-2", 1));
            mapping.Add("hungarian", new CharacterSet("iso-8859-2", 1));
            mapping.Add("croat", new CharacterSet("iso-8859-2", 1));
            mapping.Add("latvian", new CharacterSet("iso-8859-13", 1));
            mapping.Add("latvian1", new CharacterSet("iso-8859-13", 1));
            mapping.Add("estonia", new CharacterSet("iso-8859-13", 1));
            mapping.Add("dos", new CharacterSet("ibm437", 1));
        }
    }
}

