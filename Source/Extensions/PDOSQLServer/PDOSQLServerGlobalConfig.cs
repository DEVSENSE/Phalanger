using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Xml;

namespace PHP.Library.Data
{
    public sealed class PDOSQLServerGlobalConfig : IPhpConfiguration, IPhpConfigurationSection
    {
        /// <summary>
        /// Parses XML configuration file.
        /// </summary>
        public bool Parse(string name, string value, XmlNode node)
        {
            return false;
        }

        /// <summary>
        /// Creates a deep copy of the configuration record.
        /// </summary>
        /// <returns>The copy.</returns>
        public IPhpConfiguration DeepCopy()
        {
            return (PDOSQLServerGlobalConfig)this.MemberwiseClone();
        }
    }
}
