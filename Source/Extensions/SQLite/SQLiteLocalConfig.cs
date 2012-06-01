using System;
using System.Web;
using System.Xml;
using System.Collections;
using System.Configuration;

using PHP.Core;

namespace PHP.Library.Data
{
    [Serializable]
    public sealed class SQLiteLocalConfig : IPhpConfiguration, IPhpConfigurationSection
    {
        internal SQLiteLocalConfig()
        {
        }

        //public int AssocCase = 0;

        /// <summary>
        /// Creates a deep copy of the configuration record.
        /// </summary>
        /// <returns>The copy.</returns>
        public IPhpConfiguration DeepCopy()
        {
            return (SQLiteLocalConfig)this.MemberwiseClone();
        }

        /// <summary>
        /// Parses XML configuration file.
        /// </summary>
        public bool Parse(string name, string value, XmlNode node)
        {
            //switch (name)
            //{
            //    case "AssocCase":
            //        this.AssocCase = ConfigUtils.ParseInteger(value, Int32.MinValue, Int32.MaxValue, node);
            //        break;
            //    default:
            //        return false;
            //}
            //return true;
            return false;
        }
    }
}