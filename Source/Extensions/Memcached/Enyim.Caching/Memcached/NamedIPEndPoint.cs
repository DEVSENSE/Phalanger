using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Enyim.Caching.Memcached
{
    /// <summary>
    /// The IPEndPoint with the additional HostName property.
    /// </summary>
    public class NamedIPEndPoint : IPEndPoint
    {
        /// <summary>
        /// The host name.
        /// </summary>
        public readonly string HostName;

        /// <summary>
        /// The server weight hint.
        /// 
        /// The weight of the server relative to the total weight of all the servers in the pool.
        /// This controls the probability of the server being selected for operations.
        /// This is used only with consistent distribution option and usually corresponds to the amount of memory available to memcache on that server. 
        /// </summary>
        public readonly int Weight;

        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="host">Host name.</param>
        /// <param name="port"></param>
        /// <param name="weight">The server weight, used by locator if consistent distribution is used.</param>
        /// <param name="address">Host IP address.</param>
        public NamedIPEndPoint(string host, int port, int weight, IPAddress address)
            : base(address, port)
        {
            this.HostName = host;
            this.Weight = weight;
        }

        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="weight">The server weight, used by locator if consistent distribution is used.</param>
        /// <remarks>Can throw an exception if the host cannot be resolved.</remarks>
        public NamedIPEndPoint(string host, int port, int weight)
            : base(System.Net.Dns.GetHostAddresses(host)[0], port)
        {
            this.HostName = host;
            this.Weight = weight;
        }
    }
}
