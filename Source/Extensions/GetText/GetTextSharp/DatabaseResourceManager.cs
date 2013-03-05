using System;
using System.Collections;
using System.Configuration;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace PHP.Library.GetText.GetTextSharp
{
    public class DatabaseResourceManager : ResourceManager
    {
        private string dsn;
        private string sp;
        public DatabaseResourceManager()
        {
            this.dsn = (ConfigurationManager.AppSettings["Gettext.ConnectionString"] ?? ConfigurationManager.ConnectionStrings["Gettext"].ConnectionString);
            this.ResourceSets = new Hashtable();
        }
        public DatabaseResourceManager(string storedProcedure)
            : this()
        {
            this.sp = storedProcedure;
        }
        public DatabaseResourceManager(string name, string path, string fileformat)
            : this()
        {
        }
        protected override ResourceSet InternalGetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
        {
            DatabaseResourceSet databaseResourceSet = null;
            if (this.ResourceSets.Contains(culture.Name))
            {
                databaseResourceSet = (this.ResourceSets[culture.Name] as DatabaseResourceSet);
            }
            else
            {
                Hashtable resourceSets;
                Monitor.Enter(resourceSets = this.ResourceSets);
                try
                {
                    if (this.ResourceSets.Contains(culture.Name))
                    {
                        databaseResourceSet = (this.ResourceSets[culture.Name] as DatabaseResourceSet);
                    }
                    else
                    {
                        databaseResourceSet = new DatabaseResourceSet(this.dsn, culture, this.sp);
                        this.ResourceSets.Add(culture.Name, databaseResourceSet);
                    }
                }
                finally
                {
                    Monitor.Exit(resourceSets);
                }
            }
            return databaseResourceSet;
        }
    }
}
