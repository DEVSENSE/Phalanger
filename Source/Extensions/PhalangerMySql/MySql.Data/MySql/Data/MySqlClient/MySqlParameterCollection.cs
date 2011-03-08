namespace MySql.Data.MySqlClient
{
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data.Common;
    using System.Drawing.Design;
    using System.Reflection;

    [ListBindable(true), Editor("MySql.Data.MySqlClient.Design.DBParametersEditor,MySql.Design", typeof(UITypeEditor))]
    public sealed class MySqlParameterCollection : DbParameterCollection
    {
        private Hashtable indexHashCI = new Hashtable(StringComparer.CurrentCultureIgnoreCase);
        private Hashtable indexHashCS = new Hashtable();
        private List<DbParameter> items = new List<DbParameter>();

        internal MySqlParameterCollection(MySqlCommand cmd)
        {
            this.Clear();
        }

        public MySqlParameter Add(MySqlParameter value)
        {
            return this.InternalAdd(value, -1);
        }

        public override int Add(object value)
        {
            MySqlParameter parameter = value as MySqlParameter;
            if (parameter == null)
            {
                throw new MySqlException("Only MySqlParameter objects may be stored");
            }
            if ((parameter.ParameterName == null) || (parameter.ParameterName == string.Empty))
            {
                throw new MySqlException("Parameters must be named");
            }
            parameter = this.Add(parameter);
            return this.IndexOf(parameter);
        }

        public MySqlParameter Add(string parameterName, MySqlDbType dbType)
        {
            return this.Add(new MySqlParameter(parameterName, dbType));
        }

        [Obsolete("Add(String parameterName, Object value) has been deprecated.  Use AddWithValue(String parameterName, Object value)")]
        public MySqlParameter Add(string parameterName, object value)
        {
            return this.Add(new MySqlParameter(parameterName, value));
        }

        public MySqlParameter Add(string parameterName, MySqlDbType dbType, int size)
        {
            return this.Add(new MySqlParameter(parameterName, dbType, size));
        }

        public MySqlParameter Add(string parameterName, MySqlDbType dbType, int size, string sourceColumn)
        {
            return this.Add(new MySqlParameter(parameterName, dbType, size, sourceColumn));
        }

        public override void AddRange(Array values)
        {
            foreach (DbParameter parameter in values)
            {
                this.Add(parameter);
            }
        }

        public MySqlParameter AddWithValue(string parameterName, object value)
        {
            return this.Add(new MySqlParameter(parameterName, value));
        }

        private static void AdjustHash(Hashtable hash, string parameterName, int keyIndex, bool addEntry)
        {
            if (hash.ContainsKey(parameterName))
            {
                int num = (int) hash[parameterName];
                if (num >= keyIndex)
                {
                    hash[parameterName] = addEntry ? ++num : --num;
                }
            }
        }

        private void AdjustHashes(int keyIndex, bool addEntry)
        {
            for (int i = 0; i < this.Count; i++)
            {
                string parameterName = (this.items[i] as MySqlParameter).ParameterName;
                AdjustHash(this.indexHashCI, parameterName, keyIndex, addEntry);
                AdjustHash(this.indexHashCS, parameterName, keyIndex, addEntry);
            }
        }

        public override void Clear()
        {
            foreach (MySqlParameter parameter in this.items)
            {
                parameter.Collection = null;
            }
            this.items.Clear();
            this.indexHashCS.Clear();
            this.indexHashCI.Clear();
        }

        public override bool Contains(object value)
        {
            DbParameter item = value as DbParameter;
            if (item == null)
            {
                throw new ArgumentException("Argument must be of type DbParameter", "value");
            }
            return this.items.Contains(item);
        }

        public override bool Contains(string parameterName)
        {
            return (this.IndexOf(parameterName) != -1);
        }

        public override void CopyTo(Array array, int index)
        {
            this.items.ToArray().CopyTo(array, index);
        }

        public override IEnumerator GetEnumerator()
        {
            return this.items.GetEnumerator();
        }

        protected override DbParameter GetParameter(int index)
        {
            this.CheckIndex(index);
            return this.items[index];
        }

        protected override DbParameter GetParameter(string parameterName)
        {
            int index = this.IndexOf(parameterName);
            if (index >= 0)
            {
                return this.items[index];
            }
            if (parameterName.StartsWith("@") || parameterName.StartsWith("?"))
            {
                string str = parameterName.Substring(1);
                index = this.IndexOf(str);
                if (index != -1)
                {
                    return this.items[index];
                }
            }
            throw new ArgumentException("Parameter '" + parameterName + "' not found in the collection.");
        }

        internal MySqlParameter GetParameterFlexible(string parameterName, bool throwOnNotFound)
        {
            int index = this.IndexOf(parameterName);
            if (-1 == index)
            {
                index = this.IndexOf("?" + parameterName);
            }
            if (-1 == index)
            {
                index = this.IndexOf("@" + parameterName);
            }
            if ((-1 == index) && (parameterName.StartsWith("@") || parameterName.StartsWith("?")))
            {
                index = this.IndexOf(parameterName.Substring(1));
            }
            if (-1 != index)
            {
                return this[index];
            }
            if (throwOnNotFound)
            {
                throw new ArgumentException("Parameter '" + parameterName + "' not found in the collection.");
            }
            return null;
        }

        private void CheckIndex(int index)
        {
            if ((index < 0) || (index >= this.Count))
            {
                throw new IndexOutOfRangeException("Parameter index is out of range.");
            }
        }

        public override int IndexOf(object value)
        {
            DbParameter item = value as DbParameter;
            if (item == null)
            {
                throw new ArgumentException("Argument must be of type DbParameter", "value");
            }
            return this.items.IndexOf(item);
        }

        public override int IndexOf(string parameterName)
        {
            object obj2 = this.indexHashCS[parameterName];
            if (obj2 == null)
            {
                obj2 = this.indexHashCI[parameterName];
            }
            if (obj2 == null)
            {
                return -1;
            }
            return (int) obj2;
        }

        public override void Insert(int index, object value)
        {
            MySqlParameter parameter = value as MySqlParameter;
            if (parameter == null)
            {
                throw new MySqlException("Only MySqlParameter objects may be stored");
            }
            this.InternalAdd(parameter, index);
        }

        private MySqlParameter InternalAdd(MySqlParameter value, int index)
        {
            if (value == null)
            {
                throw new ArgumentException("The MySqlParameterCollection only accepts non-null MySqlParameter type objects.", "value");
            }
            if (this.IndexOf(value.ParameterName) >= 0)
            {
                throw new MySqlException(string.Format(Resources.ParameterAlreadyDefined, value.ParameterName));
            }
            string parameterName = value.ParameterName;
            if ((parameterName[0] == '@') || (parameterName[0] == '?'))
            {
                parameterName = parameterName.Substring(1, parameterName.Length - 1);
            }
            if (this.IndexOf(parameterName) >= 0)
            {
                throw new MySqlException(string.Format(Resources.ParameterAlreadyDefined, value.ParameterName));
            }
            if (index == -1)
            {
                this.items.Add(value);
                index = this.items.IndexOf(value);
            }
            else
            {
                this.items.Insert(index, value);
                this.AdjustHashes(index, true);
            }
            this.indexHashCS.Add(value.ParameterName, index);
            this.indexHashCI.Add(value.ParameterName, index);
            value.Collection = this;
            return value;
        }

        internal void ParameterNameChanged(MySqlParameter p, string oldName, string newName)
        {
            int index = this.IndexOf(oldName);
            this.indexHashCS.Remove(oldName);
            this.indexHashCI.Remove(oldName);
            this.indexHashCS.Add(newName, index);
            this.indexHashCI.Add(newName, index);
        }

        public override void Remove(object value)
        {
            MySqlParameter parameter = value as MySqlParameter;
            parameter.Collection = null;
            int index = this.IndexOf(parameter);
            this.items.Remove(parameter);
            this.indexHashCS.Remove(parameter.ParameterName);
            this.indexHashCI.Remove(parameter.ParameterName);
            this.AdjustHashes(index, false);
        }

        public override void RemoveAt(int index)
        {
            object obj2 = this.items[index];
            this.Remove(obj2);
        }

        public override void RemoveAt(string parameterName)
        {
            DbParameter parameter = this.GetParameter(parameterName);
            this.Remove(parameter);
        }

        protected override void SetParameter(int index, DbParameter value)
        {
            this.CheckIndex(index);
            MySqlParameter parameter = (MySqlParameter) this.items[index];
            this.indexHashCS.Remove(parameter.ParameterName);
            this.indexHashCI.Remove(parameter.ParameterName);
            this.items[index] = value;
            this.indexHashCS.Add(value.ParameterName, index);
            this.indexHashCI.Add(value.ParameterName, index);
        }

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            int index = this.IndexOf(parameterName);
            if (index < 0)
            {
                throw new ArgumentException("Parameter '" + parameterName + "' not found in the collection.");
            }
            this.SetParameter(index, value);
        }

        public override int Count
        {
            get
            {
                return this.items.Count;
            }
        }

        public override bool IsFixedSize
        {
            get
            {
                return ((IList) this.items).IsFixedSize;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return ((IList) this.items).IsReadOnly;
            }
        }

        public override bool IsSynchronized
        {
            get
            {
                return ((ICollection) this.items).IsSynchronized;
            }
        }

        public MySqlParameter this[string name]
        {
            get
            {
                return (MySqlParameter) this.GetParameter(name);
            }
            set
            {
                this.SetParameter(name, value);
            }
        }

        public MySqlParameter this[int index]
        {
            get
            {
                return (MySqlParameter) this.GetParameter(index);
            }
            set
            {
                this.SetParameter(index, value);
            }
        }

        public override object SyncRoot
        {
            get
            {
                return ((ICollection) this.items).SyncRoot;
            }
        }
    }
}

