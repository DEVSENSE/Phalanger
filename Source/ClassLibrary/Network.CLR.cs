/*

 Copyright (c) 2004-2006 Tomas Matousek and Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

 TODO:
   Added: inet_pton,inet_ntop (PHP 5.1.0, UNIX only)
*/

using System;
using PHP.Core;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;

namespace PHP.Library
{
	#region Enumerations

	/// <summary>
	/// Types of the DNS record.
	/// </summary>
	public enum DnsRecordType
	{
		/// <summary>IPv4 Address Resource</summary>
		[ImplementsConstant("DNS_A")]
		Ip4Address,

		/// <summary>Mail Exchanger Resource</summary>
		[ImplementsConstant("DNS_MX")]
		Mail,

		/// <summary>Alias (Canonical Name) Resource</summary>
		[ImplementsConstant("DNS_CNAME")]
		Alias,

		/// <summary>Authoritative Name Server Resource.</summary>
		[ImplementsConstant("DNS_NS")]
		NameServer,

		/// <summary>Pointer Resource.</summary>
		[ImplementsConstant("DNS_PTR")]
		Pointer,

		/// <summary>Host Info Resource.</summary>
		[ImplementsConstant("DNS_HINFO")]
		HostInfo,

		/// <summary>Start of Authority Resource.</summary>
		[ImplementsConstant("DNS_SOA")]
		StartOfAuthority,

		/// <summary>Text Resource.</summary>
		[ImplementsConstant("DNS_TXT")]
		Text,

		/// <summary>Any Resource Record.</summary>
		[ImplementsConstant("DNS_ANY")]
		Any,

		/// <summary>IPv6 Address Resource</summary>
		[ImplementsConstant("DNS_AAAA")]
		Ip6Address,

		/// <summary>Iteratively query the name server for each available record type.</summary>
		[ImplementsConstant("DNS_ALL")]
		All
	}

	#endregion

	/// <summary>
	/// Socket functions.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class Sockets
	{
		#region pfsockopen

		[ImplementsFunction("pfsockopen")]
		[return: CastToFalse]
		public static PhpResource OpenPersistent(string target, int port)
		{
			int errno;
			string errstr;
			return Open(target, port, out errno, out errstr, ScriptContext.CurrentContext.Config.FileSystem.DefaultSocketTimeout, true);
		}

		[ImplementsFunction("pfsockopen")]
		[return: CastToFalse]
		public static PhpResource OpenPersistent(string target, int port, out int errno)
		{
			string errstr;
			return Open(target, port, out errno, out errstr, ScriptContext.CurrentContext.Config.FileSystem.DefaultSocketTimeout, true);
		}

		[ImplementsFunction("pfsockopen")]
		[return: CastToFalse]
		public static PhpResource OpenPersistent(string target, int port, out int errno, out string errstr)
		{
			return Open(target, port, out errno, out errstr, ScriptContext.CurrentContext.Config.FileSystem.DefaultSocketTimeout, true);
		}

		[ImplementsFunction("pfsockopen")]
		[return: CastToFalse]
		public static PhpResource OpenPersistent(string target, int port, out int errno, out string errstr, double timeout)
		{
			return Open(target, port, out errno, out errstr, timeout, true);
		}

		#endregion

		#region fsockopen

		/// <summary>
		/// Not supported.
		/// </summary>
		[ImplementsFunction("fsockopen")]
		public static PhpResource Open(string target, int port)
		{
			int errno;
			string errstr;
			return Open(target, port, out errno, out errstr, ScriptContext.CurrentContext.Config.FileSystem.DefaultSocketTimeout, false);
		}

		/// <summary>
		/// Not supported.
		/// </summary>
		[ImplementsFunction("fsockopen")]
		public static PhpResource Open(string target, int port, out int errno)
		{
			string errstr;
			return Open(target, port, out errno, out errstr, ScriptContext.CurrentContext.Config.FileSystem.DefaultSocketTimeout, false);
		}

		/// <summary>
		/// Not supported.
		/// </summary>
		[ImplementsFunction("fsockopen")]
		public static PhpResource Open(string target, int port, out int errno, out string errstr)
		{
			return Open(target, port, out errno, out errstr, ScriptContext.CurrentContext.Config.FileSystem.DefaultSocketTimeout, false);
		}

		/// <summary>
		/// Not supported.
		/// </summary>
		[ImplementsFunction("fsockopen")]
		public static PhpResource Open(string target, int port, out int errno, out string errstr, double timeout)
		{
			return Open(target, port, out errno, out errstr, timeout, false);
		}

		/// <summary>
		/// Not supported.
		/// </summary>
		[ImplementsFunction("fsockopen")]
		public static PhpResource Open(string target, int port, out int errno, out string errstr, double timeout, bool persistent)
		{
			return StreamSocket.Connect(target, port, out errno, out errstr, timeout,
			  persistent ? StreamSocket.SocketOptions.Persistent : StreamSocket.SocketOptions.None,
			  StreamContext.Default);
		}

		#endregion

		#region socket_get_status, socket_set_blocking, socket_set_timeout

		/// <summary>
		/// Gets status.
		/// </summary>
		/// <param name="stream">A stream.</param>
		/// <returns>The array containing status info.</returns>
		[ImplementsFunction("socket_get_status")]
		public static PhpArray GetStatus(PhpResource stream)
		{
			return PhpStreams.GetMetaData(stream);
		}

		/// <summary>
		/// Sets blocking mode.
		/// </summary>
		/// <param name="stream">A stream.</param>
		/// <param name="mode">A mode.</param>
		[ImplementsFunction("socket_set_blocking")]
		public static bool SetBlocking(PhpResource stream, int mode)
		{
			return PhpStreams.SetBlocking(stream, mode);
		}


		/// <summary>
		/// Sets a timeout.
		/// </summary>
		/// <param name="stream">A stream.</param>
		/// <param name="seconds">The timeout in seconds.</param>
		[ImplementsFunction("socket_set_timeout")]
		public static bool SetTimeout(PhpResource stream, int seconds)
		{
			return PhpStreams.SetTimeout(stream, seconds);
		}

		/// <summary>
		/// Sets a timeout.
		/// </summary>
		/// <param name="stream">A stream.</param>
		/// <param name="seconds">Seconds part of the timeout.</param>
		/// <param name="microseconds">Microseconds part of the timeout.</param>
		[ImplementsFunction("socket_set_timeout")]
		public static bool SetTimeout(PhpResource stream, int seconds, int microseconds)
		{
			return PhpStreams.SetTimeout(stream, seconds, microseconds);
		}

		#endregion
	}

	/// <summary>
	/// Functions working with DNS.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class Dns
	{
		#region NS: dns_check_record, checkdnsrr

		/// <summary>
		/// Not supported.
		/// </summary>
        [ImplementsFunction("checkdnsrr", FunctionImplOptions.NotSupported)]
		public static int CheckRecordRows(string host)
		{
			return CheckRecords(host, "MX");
		}

		/// <summary>
		/// Not supported.
		/// </summary>
        [ImplementsFunction("checkdnsrr", FunctionImplOptions.NotSupported)]
		public static int CheckRecordRows(string host, string type)
		{
			return CheckRecords(host, type);
		}

		/// <summary>
		/// Not supported.
		/// </summary>
        [ImplementsFunction("dns_check_record", FunctionImplOptions.NotSupported)]
		public static int CheckRecords(string host, string type)
		{
			PhpException.FunctionNotSupported();
			return 0;
		}


		#endregion

		#region NS: dns_get_record

		/// <summary>
		/// Not supported.
		/// </summary>
        [ImplementsFunction("dns_get_record", FunctionImplOptions.NotSupported)]
		public static PhpArray GetRecord(string host)
		{
			return GetRecord(host, DnsRecordType.All);
		}

		/// <summary>
		/// Not supported.
		/// </summary>
        [ImplementsFunction("dns_get_record", FunctionImplOptions.NotSupported)]
		public static PhpArray GetRecord(string host, DnsRecordType type)
		{
			PhpException.FunctionNotSupported();
			return null;
		}

		/// <summary>
		/// Not supported.
		/// </summary>
        [ImplementsFunction("dns_get_record", FunctionImplOptions.NotSupported)]
		public static PhpArray GetRecord(string host, DnsRecordType type, out PhpArray authNS, out PhpArray additional)
		{
			PhpException.FunctionNotSupported();
			authNS = null;
			additional = null;
			return null;
		}

		#endregion

		#region gethostbyaddr, gethostbyname, gethostbynamel

		/// <summary>
		/// Gets the Internet host name corresponding to a given IP address.
		/// </summary>
		/// <param name="ipAddress">The IP address.</param>
		/// <returns>The host name or unmodified <paramref name="ipAddress"/> on failure.</returns>
		[ImplementsFunction("gethostbyaddr")]
		public static string GetHostByAddress(string ipAddress)
		{
			try
			{
				return System.Net.Dns.GetHostEntry(ipAddress).HostName;
			}
			catch (System.Exception)
			{
				return ipAddress;
			}
		}

		/// <summary>
		/// Gets the IP address corresponding to a given Internet host name.
		/// </summary>
		/// <param name="hostName">The host name.</param>
		/// <returns>The IP address or unmodified <paramref name="hostName"/> on failure.</returns>
		[ImplementsFunction("gethostbyname")]
		public static string GetHostByName(string hostName)
		{
			try
			{
				IPAddress[] addresses = System.Net.Dns.GetHostEntry(hostName).AddressList;
				return (addresses.Length > 0) ? addresses[0].ToString() : hostName;
			}
			catch (System.Exception)
			{
				return hostName;
			}
		}

		/// <summary>
		/// Gets a list of IP addresses corresponding to a given Internet host name.
		/// </summary>
		/// <param name="hostName">The host name.</param>
		/// <returns>The list of IP addresses to which the Internet host specified by <paramref name="hostName"/> resolves.
		/// </returns>
		[ImplementsFunction("gethostbynamel")]
		public static PhpArray GetHostByNameList(string hostName)
		{
			try
			{
				IPAddress[] addresses = System.Net.Dns.GetHostEntry(hostName).AddressList;
				PhpArray result = new PhpArray(addresses.Length, 0);

				foreach (IPAddress address in addresses)
					result.Add(address.ToString());

				return result;
			}
			catch (System.Exception)
			{
				return null;
			}
		}

		#endregion

		#region NS: getmxrr, dns_get_mx

		/// <summary>
		/// Not supported.
		/// </summary>
		[ImplementsFunction("getmxrr")]
		public static bool GetMxRecordRow(string hostName, PhpArray mxHosts)
		{
			return GetMxRecord(hostName, mxHosts); ;
		}

		/// <summary>
		/// Not supported.
		/// </summary>
		[ImplementsFunction("getmxrr")]
		public static bool GetMxRecordRow(string hostName, PhpArray mxHosts, PhpArray weight)
		{
			return GetMxRecord(hostName, mxHosts, weight);
		}

		/// <summary>
		/// Not supported.
		/// </summary>
        [ImplementsFunction("dns_get_mx", FunctionImplOptions.NotSupported)]
		public static bool GetMxRecord(string hostName, PhpArray mxHosts)
		{
			PhpException.FunctionNotSupported();
			return false;
		}

		/// <summary>
		/// Not supported.
		/// </summary>
        [ImplementsFunction("dns_get_mx", FunctionImplOptions.NotSupported)]
		public static bool GetMxRecord(string hostName, PhpArray mxHosts, PhpArray weight)
		{
			PhpException.FunctionNotSupported();
			return false;
		}

		#endregion

		#region getprotobyname, getprotobynumber, getservbyname, getservbyport, ip2long, long2ip

		/// <summary>
		/// Returns protocol number associated with a given protocol name.
		/// </summary>
		/// <param name="name">The protocol name.</param>
		/// <returns>The protocol number or <c>-1</c> if <paramref name="name"/> is not found.</returns>
		[ImplementsFunction("getprotobyname")]
		[return: CastToFalse]
		public static int GetProtocolByName(string name)
		{
			if (string.IsNullOrEmpty(name)) return -1;

            NetworkUtils.ProtoEnt ent = NetworkUtils.GetProtocolByName(name);
			if (ent == null) return -1;
			return ent.p_proto;
		}

		/// <summary>
		/// Returns protocol name associated with a given protocol number.
		/// </summary>
		/// <param name="number">The protocol number.</param>
		/// <returns>The protocol name or <B>null</B> if <paramref name="number"/> is not found.</returns>
		[ImplementsFunction("getprotobynumber")]
		[return: CastToFalse]
		public static string GetProtocolByNumber(int number)
		{
			NetworkUtils.ProtoEnt ent = NetworkUtils.GetProtocolByNumber(number);
			if (ent == null) return null;
			return ent.p_name;
		}

		/// <summary>
		/// Returns port number associated with a given Internet service and protocol.
		/// </summary>
		/// <param name="service">The service.</param>
		/// <param name="protocol">The protocol.</param>
		/// <returns>The port number or <c>-1</c> if not found.</returns>
		[ImplementsFunction("getservbyname")]
		[return: CastToFalse]
		public static int GetServiceByName(string service, string protocol)
		{
			if (service == null) return -1;

			NetworkUtils.ServEnt ent = NetworkUtils.GetServiceByName(service, protocol);
			if (ent == null) return -1;
			return IPAddress.NetworkToHostOrder(ent.s_port);
		}

		/// <summary>
		/// Returns an Internet service that corresponds to a given port and protocol.
		/// </summary>
		/// <param name="port">The port.</param>
		/// <param name="protocol">The protocol.</param>
		/// <returns>The service name or <B>null</B> if not found.</returns>
		[ImplementsFunction("getservbyport")]
		[return: CastToFalse]
		public static string GetServiceByPort(int port, string protocol)
		{
			NetworkUtils.ServEnt ent = NetworkUtils.GetServiceByPort(IPAddress.HostToNetworkOrder(port), protocol);
			if (ent == null) return null;
			return ent.s_proto;
		}

		/// <summary>
		/// Converts a string containing an (IPv4) Internet Protocol dotted address into a proper address.
		/// </summary>
		/// <param name="ipAddress">The string representation of the address.</param>
		/// <returns>The integer representation of the address.</returns>
		[ImplementsFunction("ip2long")]
		[return: CastToFalse]
		public static int IPToInteger(string ipAddress)
		{
			if (string.IsNullOrEmpty(ipAddress)) return -1;
			IPAddress addr;
			try
			{
				addr = IPAddress.Parse(ipAddress);
			}
			catch (FormatException)
			{
				return -1;
			}

			if (addr.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return -1;
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(addr.GetAddressBytes(), 0));
		}

		/// <summary>
		/// Converts an (IPv4) Internet network address into a string in Internet standard dotted format.
		/// </summary>
		/// <param name="properAddress">The integer representation of the address.</param>
		/// <returns>The string representation of the address.</returns>
		[ImplementsFunction("long2ip")]
		public static string IntegerToIP(int properAddress)
		{
			IPAddress addr;
			unchecked
			{
				addr = new IPAddress((long)(uint)IPAddress.HostToNetworkOrder(properAddress));
			}
			return addr.ToString();
		}

		#endregion
	}
}
