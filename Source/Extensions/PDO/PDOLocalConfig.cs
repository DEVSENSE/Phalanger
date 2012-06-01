using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Xml;

namespace PHP.Library.Data
{
    [Serializable]
    public sealed class PDOLocalConfig : IPhpConfiguration, IPhpConfigurationSection
    {
        internal PDOLocalConfig()
        {
        }

        //public int AssocCase = 0;

        /// <summary>
        /// Creates a deep copy of the configuration record.
        /// </summary>
        /// <returns>The copy.</returns>
        public IPhpConfiguration DeepCopy()
        {
            return (PDOLocalConfig)this.MemberwiseClone();
        }

        /// <summary>
        /// Parses XML configuration file.
        /// </summary>
        public bool Parse(string name, string value, XmlNode node)
        {
            return false;
        }
    }
}
