using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using PHP.Core;

namespace PHP.Library.Data
{
    /// <summary>
    /// Script dependent SQLite configuration.
    /// </summary>
    [Serializable]
    public sealed class SQLiteGlobalConfig : IPhpConfiguration, IPhpConfigurationSection
    {
        internal SQLiteGlobalConfig() { }

        public int AssocCase = 0;

        /// <summary>
        /// Parses XML configuration file.
        /// </summary>
        public bool Parse(string name, string value, XmlNode node)
        {
            switch (name)
            {
                case "AssocCase":
                    this.AssocCase = ConfigUtils.ParseInteger(value, Int32.MinValue, Int32.MaxValue, node);
                    break;
                default:
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Creates a deep copy of the configuration record.
        /// </summary>
        /// <returns>The copy.</returns>
        public IPhpConfiguration DeepCopy()
        {
            return (SQLiteGlobalConfig)this.MemberwiseClone();
        }
    }
}
