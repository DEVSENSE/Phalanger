namespace MySql.Data.MySqlClient
{
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Runtime.InteropServices;
    using System.Text;

    internal class PreparableStatement : Statement
    {
        private int dataPosition;
        private int executionCount;
        private BitArray nullMap;
        private int nullMapPosition;
        private MySqlPacket packet;
        private List<MySqlParameter> parametersToSend;
        private int statementId;

        public PreparableStatement(MySqlCommand command, string text) : base(command, text)
        {
            this.parametersToSend = new List<MySqlParameter>();
        }

        public virtual void CloseStatement()
        {
            if (this.IsPrepared)
            {
                base.Driver.CloseStatement(this.statementId);
                this.statementId = 0;
            }
        }

        public override void Execute()
        {
            if (!this.IsPrepared)
            {
                base.Execute();
            }
            else
            {
                this.packet.Position = this.dataPosition;
                for (int i = 0; i < this.parametersToSend.Count; i++)
                {
                    MySqlParameter parameter = this.parametersToSend[i];
                    this.nullMap[i] = ((parameter.Value == DBNull.Value) || (parameter.Value == null)) || (parameter.Direction == ParameterDirection.Output);
                    if (!this.nullMap[i])
                    {
                        this.packet.Encoding = parameter.Encoding;
                        parameter.Serialize(this.packet, true, base.Connection.Settings);
                    }
                }
                if (this.nullMap != null)
                {
                    this.nullMap.CopyTo(this.packet.Buffer, this.nullMapPosition);
                }
                this.executionCount++;
                base.Driver.ExecuteStatement(this.packet);
            }
        }

        public override bool ExecuteNext()
        {
            return (!this.IsPrepared && base.ExecuteNext());
        }

        public virtual void Prepare()
        {
            string str;
            List<string> list = this.PrepareCommandText(out str);
            MySqlField[] parameters = null;
            this.statementId = base.Driver.PrepareStatement(str, ref parameters);
            for (int i = 0; i < list.Count; i++)
            {
                string parameterName = list[i];
                int index = base.Parameters.IndexOf(parameterName);
                if (index == -1)
                {
                    throw new InvalidOperationException(string.Format(Resources.ParameterNotFoundDuringPrepare, parameterName));
                }
                MySqlParameter item = base.Parameters[index];
                item.Encoding = parameters[i].Encoding;
                this.parametersToSend.Add(item);
            }
            int num3 = 0;
            if ((parameters != null) && (parameters.Length > 0))
            {
                this.nullMap = new BitArray(parameters.Length);
                num3 = (this.nullMap.Count + 7) / 8;
            }
            this.packet = new MySqlPacket(base.Driver.Encoding);
            this.packet.WriteByte(0);
            this.packet.WriteInteger((long) this.statementId, 4);
            this.packet.WriteByte(0);
            this.packet.WriteInteger(1L, 4);
            this.nullMapPosition = this.packet.Position;
            this.packet.Position += num3;
            this.packet.WriteByte(1);
            foreach (MySqlParameter parameter2 in this.parametersToSend)
            {
                this.packet.WriteInteger((long) parameter2.GetPSType(), 2);
            }
            this.dataPosition = this.packet.Position;
        }

        private List<string> PrepareCommandText(out string stripped_sql)
        {
            StringBuilder builder = new StringBuilder();
            List<string> list = new List<string>();
            int startIndex = 0;
            string resolvedCommandText = this.ResolvedCommandText;
            MySqlTokenizer tokenizer = new MySqlTokenizer(resolvedCommandText);
            for (string str2 = tokenizer.NextParameter(); str2 != null; str2 = tokenizer.NextParameter())
            {
                builder.Append(resolvedCommandText.Substring(startIndex, tokenizer.StartIndex - startIndex));
                builder.Append("?");
                list.Add(str2);
                startIndex = tokenizer.StopIndex;
            }
            builder.Append(resolvedCommandText.Substring(startIndex));
            stripped_sql = builder.ToString();
            return list;
        }

        public int ExecutionCount
        {
            get
            {
                return this.executionCount;
            }
            set
            {
                this.executionCount = value;
            }
        }

        public bool IsPrepared
        {
            get
            {
                return (this.statementId > 0);
            }
        }

        public int StatementId
        {
            get
            {
                return this.statementId;
            }
        }
    }
}

