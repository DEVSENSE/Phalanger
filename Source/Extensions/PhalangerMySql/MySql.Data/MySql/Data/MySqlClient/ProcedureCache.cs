namespace MySql.Data.MySqlClient
{
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Text;

    internal class ProcedureCache
    {
        private Queue<int> hashQueue;
        private int maxSize;
        private Hashtable procHash;

        public ProcedureCache(int size)
        {
            this.maxSize = size;
            this.hashQueue = new Queue<int>(this.maxSize);
            this.procHash = new Hashtable(this.maxSize);
        }

        private DataSet AddNew(MySqlConnection connection, string spName)
        {
            DataSet procData = GetProcData(connection, spName);
            if (this.maxSize > 0)
            {
                int hashCode = this.GetCacheKey(spName, procData).GetHashCode();
                lock (this.procHash.SyncRoot)
                {
                    if (this.procHash.Keys.Count >= this.maxSize)
                    {
                        this.TrimHash();
                    }
                    if (!this.procHash.ContainsKey(hashCode))
                    {
                        this.procHash[hashCode] = procData;
                        this.hashQueue.Enqueue(hashCode);
                    }
                }
            }
            return procData;
        }

        internal string GetCacheKey(string spName, DataSet procData)
        {
            string str = string.Empty;
            StringBuilder builder = new StringBuilder(spName);
            builder.Append("(");
            string str2 = "";
            if (procData.Tables.Contains("Procedure Parameters"))
            {
                foreach (DataRow row in procData.Tables["Procedure Parameters"].Rows)
                {
                    if (row["ORDINAL_POSITION"].Equals(0))
                    {
                        str = "?=";
                    }
                    else
                    {
                        builder.AppendFormat(CultureInfo.InvariantCulture, "{0}?", new object[] { str2 });
                        str2 = ",";
                    }
                }
            }
            builder.Append(")");
            return (str + builder.ToString());
        }

        private static DataSet GetProcData(MySqlConnection connection, string spName)
        {
            string str = string.Empty;
            string str2 = spName;
            int index = spName.IndexOf(".");
            if (index != -1)
            {
                str = spName.Substring(0, index);
                str2 = spName.Substring(index + 1, (spName.Length - index) - 1);
            }
            string[] restrictionValues = new string[4];
            restrictionValues[1] = (str.Length > 0) ? str : connection.CurrentDatabase();
            restrictionValues[2] = str2;
            DataTable schema = connection.GetSchema("procedures", restrictionValues);
            if (schema.Rows.Count > 1)
            {
                throw new MySqlException(Resources.ProcAndFuncSameName);
            }
            if (schema.Rows.Count == 0)
            {
                throw new MySqlException(string.Format(Resources.InvalidProcName, str2, str));
            }
            DataSet set = new DataSet();
            set.Tables.Add(schema);
            ISSchemaProvider provider = new ISSchemaProvider(connection);
            string[] restrictions = provider.CleanRestrictions(restrictionValues);
            try
            {
                DataTable procedureParameters = provider.GetProcedureParameters(restrictions, schema);
                set.Tables.Add(procedureParameters);
            }
            catch (Exception)
            {
            }
            return set;
        }

        public DataSet GetProcedure(MySqlConnection conn, string spName, string cacheKey)
        {
            DataSet set = null;
            if (cacheKey != null)
            {
                int hashCode = cacheKey.GetHashCode();
                lock (this.procHash.SyncRoot)
                {
                    set = (DataSet) this.procHash[hashCode];
                }
            }
            if (set == null)
            {
                set = this.AddNew(conn, spName);
                conn.PerfMonitor.AddHardProcedureQuery();
                if (conn.Settings.Logging)
                {
                    MySqlTrace.LogInformation(conn.ServerThread, string.Format(Resources.HardProcQuery, spName));
                }
                return set;
            }
            conn.PerfMonitor.AddSoftProcedureQuery();
            if (conn.Settings.Logging)
            {
                MySqlTrace.LogInformation(conn.ServerThread, string.Format(Resources.SoftProcQuery, spName));
            }
            return set;
        }

        private void TrimHash()
        {
            int key = this.hashQueue.Dequeue();
            this.procHash.Remove(key);
        }
    }
}

