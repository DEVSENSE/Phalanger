using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Resources;

namespace PHP.Library.GetText.GetTextSharp
{
    public class DatabaseResourceReader : IResourceReader, IEnumerable, IDisposable
    {
        private string dsn;
        private string language;
        private string sp;
        public DatabaseResourceReader(string dsn, CultureInfo culture)
        {
            this.dsn = dsn;
            this.language = culture.Name;
        }
        public DatabaseResourceReader(string dsn, CultureInfo culture, string sp)
        {
            this.sp = sp;
            this.dsn = dsn;
            this.language = culture.Name;
        }
        public IDictionaryEnumerator GetEnumerator()
        {
            Hashtable hashtable = new Hashtable();
            SqlConnection sqlConnection = new SqlConnection(this.dsn);
            SqlCommand sqlCommand = sqlConnection.CreateCommand();
            if (this.language == "")
            {
                this.language = CultureInfo.InvariantCulture.Name;
            }
            if (this.sp == null)
            {
                sqlCommand.CommandText = string.Format("SELECT MessageKey, MessageValue FROM Message WHERE Culture = '{0}'", this.language);
            }
            else
            {
                sqlCommand.CommandText = this.sp;
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("@culture", this.language);
            }
            try
            {
                sqlConnection.Open();
                using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                {
                    while (sqlDataReader.Read())
                    {
                        if (sqlDataReader.GetValue(1) != DBNull.Value)
                        {
                            hashtable[sqlDataReader.GetString(0)] = sqlDataReader.GetString(1);
                        }
                    }
                }
            }
            catch
            {
                bool flag = false;
                if (bool.TryParse(ConfigurationManager.AppSettings["Gettext.Throw"], out flag) && flag)
                {
                    throw;
                }
            }
            finally
            {
                sqlConnection.Close();
            }
            return hashtable.GetEnumerator();
        }
        public void Close()
        {
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        void IDisposable.Dispose()
        {
        }
    }
}