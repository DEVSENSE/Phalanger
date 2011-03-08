namespace MySql.Data.MySqlClient
{
    using MySql.Data.MySqlClient.Properties;
    using MySql.Data.Types;
    using System;
    using System.Collections;
    using System.Data;
    using System.IO;
    using System.Reflection;

    internal class ResultSet
    {
        private int affectedRows;
        private Driver driver;
        private Hashtable fieldHashCI;
        private Hashtable fieldHashCS;
        private MySqlField[] fields;
        private bool hasRows;
        private int insertedId;
        private bool isOutputParameters;
        private bool isSequential;
        private bool readDone;
        private int rowIndex;
        private int seqIndex;
        private int skippedRows;
        private int statementId;
        private int totalRows;
        private bool[] uaFieldsUsed;
        private IMySqlValue[] values;

        public ResultSet(int affectedRows, int insertedId)
        {
            this.affectedRows = affectedRows;
            this.insertedId = insertedId;
            this.readDone = true;
        }

        public ResultSet(Driver d, int statementId, int numCols)
        {
            this.affectedRows = -1;
            this.insertedId = -1;
            this.driver = d;
            this.statementId = statementId;
            this.rowIndex = -1;
            this.LoadColumns(numCols);
            this.isOutputParameters = this.driver.HasStatus(ServerStatusFlags.OutputParameters);
            this.hasRows = this.GetNextRow();
            this.readDone = !this.hasRows;
        }

        public void Close()
        {
            if (!this.readDone)
            {
                if (this.HasRows && (this.rowIndex == -1))
                {
                    this.skippedRows++;
                }
                try
                {
                    while (this.driver.IsOpen && this.driver.SkipDataRow())
                    {
                        this.totalRows++;
                        this.skippedRows++;
                    }
                }
                catch (IOException)
                {
                }
                this.readDone = true;
            }
        }

        public bool FieldRead(int index)
        {
            return this.uaFieldsUsed[index];
        }

        private bool GetNextRow()
        {
            bool flag = this.driver.FetchDataRow(this.statementId, this.Size);
            if (flag)
            {
                this.totalRows++;
            }
            return flag;
        }

        public int GetOrdinal(string name)
        {
            object obj2 = this.fieldHashCS[name];
            if (obj2 == null)
            {
                obj2 = this.fieldHashCI[name];
                if (obj2 == null)
                {
                    throw new IndexOutOfRangeException(string.Format(Resources.CouldNotFindColumnName, name));
                }
            }
            return (int) obj2;
        }

        private void LoadColumns(int numCols)
        {
            this.fields = this.driver.GetColumns(numCols);
            this.values = new IMySqlValue[numCols];
            this.uaFieldsUsed = new bool[numCols];
            this.fieldHashCS = new Hashtable();
            this.fieldHashCI = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
            for (int i = 0; i < this.fields.Length; i++)
            {
                string columnName = this.fields[i].ColumnName;
                if (!this.fieldHashCS.ContainsKey(columnName))
                {
                    this.fieldHashCS.Add(columnName, i);
                }
                if (!this.fieldHashCI.ContainsKey(columnName))
                {
                    this.fieldHashCI.Add(columnName, i);
                }
                this.values[i] = this.fields[i].GetValueObject();
            }
        }

        public bool NextRow(CommandBehavior behavior)
        {
            if (this.readDone)
            {
                return false;
            }
            if (((behavior & CommandBehavior.SingleRow) != CommandBehavior.Default) && (this.rowIndex == 0))
            {
                return false;
            }
            this.isSequential = (behavior & CommandBehavior.SequentialAccess) != CommandBehavior.Default;
            this.seqIndex = -1;
            if (this.rowIndex >= 0)
            {
                bool nextRow = false;
                try
                {
                    nextRow = this.GetNextRow();
                }
                catch (MySqlException exception)
                {
                    if (exception.IsQueryAborted)
                    {
                        this.readDone = true;
                    }
                    throw;
                }
                if (!nextRow)
                {
                    this.readDone = true;
                    return false;
                }
            }
            if (!this.isSequential)
            {
                this.ReadColumnData(false);
            }
            this.rowIndex++;
            return true;
        }

        private void ReadColumnData(bool outputParms)
        {
            for (int i = 0; i < this.Size; i++)
            {
                this.values[i] = this.driver.ReadColumnValue(i, this.fields[i], this.values[i]);
            }
            if (outputParms)
            {
                bool flag = this.driver.FetchDataRow(this.statementId, this.fields.Length);
                this.rowIndex = 0;
                if (flag)
                {
                    throw new MySqlException(Resources.MoreThanOneOPRow);
                }
            }
        }

        public void SetValueObject(int i, IMySqlValue valueObject)
        {
            this.values[i] = valueObject;
        }

        public int AffectedRows
        {
            get
            {
                return this.affectedRows;
            }
        }

        public MySqlField[] Fields
        {
            get
            {
                return this.fields;
            }
        }

        public bool HasRows
        {
            get
            {
                return this.hasRows;
            }
        }

        public int InsertedId
        {
            get
            {
                return this.insertedId;
            }
        }

        public bool IsOutputParameters
        {
            get
            {
                return this.isOutputParameters;
            }
            set
            {
                this.isOutputParameters = value;
            }
        }

        public IMySqlValue this[int index]
        {
            get
            {
                if (this.rowIndex < 0)
                {
                    throw new MySqlException(Resources.AttemptToAccessBeforeRead);
                }
                this.uaFieldsUsed[index] = true;
                if (this.isSequential && (index != this.seqIndex))
                {
                    if (index < this.seqIndex)
                    {
                        throw new MySqlException(Resources.ReadingPriorColumnUsingSeqAccess);
                    }
                    while (this.seqIndex < (index - 1))
                    {
                        this.driver.SkipColumnValue(this.values[++this.seqIndex]);
                    }
                    this.values[index] = this.driver.ReadColumnValue(index, this.fields[index], this.values[index]);
                    this.seqIndex = index;
                }
                return this.values[index];
            }
        }

        public int Size
        {
            get
            {
                if (this.fields != null)
                {
                    return this.fields.Length;
                }
                return 0;
            }
        }

        public int SkippedRows
        {
            get
            {
                return this.skippedRows;
            }
        }

        public int TotalRows
        {
            get
            {
                return this.totalRows;
            }
        }

        public IMySqlValue[] Values
        {
            get
            {
                return this.values;
            }
        }
    }
}

