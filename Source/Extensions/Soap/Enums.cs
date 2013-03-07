using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Soap
{
    /// <summary>
    /// Type of caching for wsdl files
    /// </summary>
    public enum WsdlCache
    {
        /// <summary>
        /// No caching of wsdl files
        /// </summary>
        /// <remarks>
        /// Setting this option also purges all previsouly cached wsdl files.
        /// </remarks>
        [ImplementsConstant("WSDL_CACHE_NONE")]
        None = 0,

        /// <summary>
        /// Cache wsdl files just on disk
        /// </summary>
        [ImplementsConstant("WSDL_CACHE_DISK")]
        Disk = 1,//It's not possible to cache wsdl only on disk, because we are caching it in form of assemblies, which has to be always in memory

        /// <summary>
        /// Cache wsdl files just in memory
        /// </summary>
        [ImplementsConstant("WSDL_CACHE_MEMORY")]
        Memory = 2,

        /// <summary>
        /// Cache wsdl files in both memory and disk
        /// </summary>
        [ImplementsConstant("WSDL_CACHE_BOTH")]
        Both = 3
    }


    internal enum Protocol
    {
        HttpGet,
        HttpPost,
        HttpSoap
    }
}
