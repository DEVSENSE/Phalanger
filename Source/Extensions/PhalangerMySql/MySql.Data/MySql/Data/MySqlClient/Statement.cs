namespace MySql.Data.MySqlClient
{
    using MySql.Data.MySqlClient.Properties;
    using System;
    using System.Collections;

    internal abstract class Statement
    {
        private ArrayList buffers;
        protected MySqlCommand command;
        protected string commandText;

        private Statement(MySqlCommand cmd)
        {
            this.command = cmd;
            this.buffers = new ArrayList();
        }

        public Statement(MySqlCommand cmd, string text) : this(cmd)
        {
            this.commandText = text;
        }

        protected virtual void BindParameters()
        {
            MySqlParameterCollection parameters = this.command.Parameters;
            int num = 0;
            do
            {
                this.InternalBindParameters(this.ResolvedCommandText, parameters, null);
                if (this.command.Batch != null)
                {
                    while (num < this.command.Batch.Count)
                    {
                        MySqlCommand command = this.command.Batch[num++];
                        MySqlPacket packet = (MySqlPacket) this.buffers[this.buffers.Count - 1];
                        long num2 = command.EstimatedSize();
                        if (((packet.Length - 4) + num2) > this.Connection.driver.MaxPacketSize)
                        {
                            parameters = command.Parameters;
                            break;
                        }
                        this.buffers.RemoveAt(this.buffers.Count - 1);
                        string batchableCommandText = command.BatchableCommandText;
                        if (batchableCommandText.StartsWith("("))
                        {
                            packet.WriteStringNoNull(", ");
                        }
                        else
                        {
                            packet.WriteStringNoNull("; ");
                        }
                        this.InternalBindParameters(batchableCommandText, command.Parameters, packet);
                        if ((packet.Length - 4) > this.Connection.driver.MaxPacketSize)
                        {
                            parameters = command.Parameters;
                            break;
                        }
                    }
                }
                else
                {
                    return;
                }
            }
            while (num != this.command.Batch.Count);
        }

        public virtual void Close(MySqlDataReader reader)
        {
        }

        public virtual void Execute()
        {
            this.BindParameters();
            this.ExecuteNext();
        }

        public virtual bool ExecuteNext()
        {
            if (this.buffers.Count == 0)
            {
                return false;
            }
            MySqlPacket p = (MySqlPacket) this.buffers[0];
            this.Driver.SendQuery(p);
            this.buffers.RemoveAt(0);
            return true;
        }

        private void InternalBindParameters(string sql, MySqlParameterCollection parameters, MySqlPacket packet)
        {
            bool sqlServerMode = this.command.Connection.Settings.SqlServerMode;
            if (packet == null)
            {
                packet = new MySqlPacket(this.Driver.Encoding);
                packet.Version = this.Driver.Version;
                packet.WriteByte(0);
            }
            MySqlTokenizer tokenizer = new MySqlTokenizer(sql);
            tokenizer.ReturnComments = true;
            tokenizer.SqlServerMode = sqlServerMode;
            int startIndex = 0;
            for (string str = tokenizer.NextToken(); str != null; str = tokenizer.NextToken())
            {
                packet.WriteStringNoNull(sql.Substring(startIndex, tokenizer.StartIndex - startIndex));
                startIndex = tokenizer.StopIndex;
                if (MySqlTokenizer.IsParameter(str) && this.SerializeParameter(parameters, packet, str))
                {
                    str = null;
                }
                if (str != null)
                {
                    if ((sqlServerMode && tokenizer.Quoted) && str.StartsWith("["))
                    {
                        str = string.Format("`{0}`", str.Substring(1, str.Length - 2));
                    }
                    packet.WriteStringNoNull(str);
                }
            }
            this.buffers.Add(packet);
        }

        public virtual void Resolve(bool preparing)
        {
        }

        private bool SerializeParameter(MySqlParameterCollection parameters, MySqlPacket packet, string parmName)
        {
            MySqlParameter parameterFlexible = parameters.GetParameterFlexible(parmName, false);
            if (parameterFlexible == null)
            {
                if (!parmName.StartsWith("@") || !this.ShouldIgnoreMissingParameter(parmName))
                {
                    throw new MySqlException(string.Format(Resources.ParameterMustBeDefined, parmName));
                }
                return false;
            }
            parameterFlexible.Serialize(packet, false, this.Connection.Settings);
            return true;
        }

        protected virtual bool ShouldIgnoreMissingParameter(string parameterName)
        {
            if (!this.Connection.Settings.AllowUserVariables)
            {
                if (parameterName.StartsWith("@_cnet_param_"))
                {
                    return true;
                }
                if ((parameterName.Length <= 1) || ((parameterName[1] != '`') && (parameterName[1] != '\'')))
                {
                    return false;
                }
            }
            return true;
        }

        protected MySqlConnection Connection
        {
            get
            {
                return this.command.Connection;
            }
        }

        protected MySql.Data.MySqlClient.Driver Driver
        {
            get
            {
                return this.command.Connection.driver;
            }
        }

        protected MySqlParameterCollection Parameters
        {
            get
            {
                return this.command.Parameters;
            }
        }

        public virtual string ResolvedCommandText
        {
            get
            {
                return this.commandText;
            }
        }
    }
}

