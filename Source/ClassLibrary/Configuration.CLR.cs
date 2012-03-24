/*

 Copyright (c) 2005-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/


using System;
using System.IO;
using System.Web;
using System.Xml;
using System.Collections;
using System.Configuration;
using System.Runtime.Serialization;

using PHP;
using PHP.Core;
using System.Net.Mail;

namespace PHP.Library
{
	[Serializable]
	public sealed partial class LibraryConfiguration : IPhpConfiguration
	{
		#region Highlighting

		/// <summary>
		/// Highlighting functions options.
		/// </summary>
		[Serializable]
		public partial class HighlightingSection : IPhpConfigurationSection
		{
			public bool Parse(string name, string value, XmlNode node)
			{
				switch (name)
				{
					case "String": String = value; break;
					case "Comment": Comment = value; break;
					case "Keyword": Keyword = value; break;
					case "Background": Background = value; break;
					case "Default": Default = value; break;
					case "Html": Html = value; break;
					case "ScriptTags": ScriptTags = value; break;
					default:
						return false;
				}
				return true;
			}
		}

		#endregion

		#region Mailer

		/// <summary>
		/// Mailign funtions options.
		/// </summary>
		[Serializable]
		public class MailerSection : IPhpConfigurationSection
		{
			/// <summary>
			/// SMTP server name used for sending e-mails.
			/// </summary>
			public string SmtpServer = null;

			/// <summary>
			/// SMTP server port used for sending e-mails.
			/// </summary>
			public int SmtpPort = 25;

			/// <summary>
			/// The default value of "From" header.
			/// </summary>
			public string DefaultFromHeader = null;

            public MailerSection()
            {
                
            }

			internal MailerSection DeepCopy()
			{
				return (MailerSection)this.MemberwiseClone();
			}

			public bool Parse(string name, string value, XmlNode node)
			{
				switch (name)
				{
					case "DefaultFromHeader":
                        try
                        {
                            // check the mail address:
                            MailAddress address = new MailAddress(value);

                            // remember the value only if the address is valid:
                            DefaultFromHeader = value;
                        }
                        catch
                        {
                            // an invalid mail address provided:
                            throw new ConfigUtils.InvalidAttributeValueException(node, "value");
                        }
						break;

					case "SmtpServer":
						SmtpServer = value;
						break;

					case "SmtpPort":
						SmtpPort = ConfigUtils.ParseInteger(value, 0, UInt16.MaxValue, node);
						break;

					default:
						return false;
				}
				return true;
			}
		}

		#endregion

		#region Session

		/// <summary>
		/// Serialization options.
		/// </summary>
		[Serializable]
		public sealed class SessionSection : IPhpConfigurationSection
		{
            static SessionSection()
            {
                // load configuration into the context every request
                RequestContext.RequestBegin += () =>
                {
                    var config = LibraryConfiguration.Local;
                    if (config == null) return;

                    RequestContext context = RequestContext.CurrentContext;
                    Debug.Assert(context != null);
                    HttpCookie cookie = AspNetSessionHandler.GetCookie(context.HttpContext);

                    if (config.Session.CacheExpire >= 0)
                        context.HttpContext.Session.Timeout = config.Session.CacheExpire;

                    if (config.Session.CacheLimiter != null)
                        PhpSession.CacheLimiter(config.Session.CacheLimiter);

                    if (cookie != null)
                    {
                        if (config.Session.CookieLifetime >= 0)
                            context.SessionCookieLifetime = config.Session.CookieLifetime;

                        if (config.Session.CookiePath != null)
                            cookie.Path = config.Session.CookiePath;

                        if (config.Session.CookieDomain != null)
                            cookie.Domain = config.Session.CookieDomain;

                        cookie.Secure = config.Session.CookieSecure;
                    }
                };
            }
         
            /// <summary>
			/// A serializer used for serializing session data. Can't contain a <B>null</B> reference.
			/// Setting <B>null</B> to the propety will set the default PHP serializer.
			/// </summary>
			public Serializer Serializer
			{
				get
				{
					return serializer;
				}
				set
				{
					serializer = (value != null) ? value : PhpSerializer.Default;
				}
			}
			private Serializer serializer = PhpSerializer.Default;

			/// <summary>
			/// A probability factor of invocation of old sessions collection. To get the probability, 
			/// the factor is divided by <see cref="GcDivisor"/>.
			/// </summary>
			public int GcProbability = 1;

			/// <summary>
			/// The divisor of probability factor.
			/// </summary>
			public int GcDivisor = 100;

			/// <summary>
			/// A maximal session lifetime in seconds.
			/// </summary>
			public int GcMaxLifetime = 1440;

			/// <summary>
			/// A directory path relative to the current directory where the session files will be stored in.
			/// </summary>
			public string SavePath = Path.GetTempPath();

            /// <summary>
            /// HttpContext.Session.Timeout if not -1.
            /// </summary>
            public int CacheExpire = -1;

            /// <summary>
            /// CacheLimiter if not null.
            /// </summary>
            public string CacheLimiter = null;

            /// <summary>
            /// HttpContext.SessionCookieLifetime if not -1.
            /// </summary>
            public int CookieLifetime = -1;

            /// <summary>
            /// cookie.Path if not null.
            /// </summary>
            public string CookiePath = null;

            /// <summary>
            /// cookie.Domain if not null.
            /// </summary>
            public string CookieDomain = null;

            /// <summary>
            /// cookie.Secure.
            /// </summary>
            public bool CookieSecure = false;

			/// <summary>
			/// Copies values to the target structure.
			/// </summary>
			internal SessionSection DeepCopy()
			{
				return (SessionSection)this.MemberwiseClone();
			}

			public bool Parse(string name, string value, XmlNode node)
			{
				switch (name)
				{
					case "Serializer":
						{
							Serializer serializer = Serializers.GetSerializer(value);

							if (serializer == null)
								throw new ConfigurationErrorsException(LibResources.GetString("unknown_serializer", value) + ".", node);

							this.serializer = serializer;
							break;
						}

					case "GcProbability":
						GcProbability = ConfigUtils.ParseInteger(value, 0, Int32.MaxValue, node);
						break;

					case "GcDivisor":
						GcDivisor = ConfigUtils.ParseInteger(value, 1, Int32.MaxValue, node);
						break;

					case "GcMaxLifetime":
						GcMaxLifetime = ConfigUtils.ParseInteger(value, Int32.MinValue, Int32.MaxValue, node);
						break;

					case "SavePath":
						if (value != "")
							SavePath = value;
						break;

					case "CacheExpire":
						CacheExpire = ConfigUtils.ParseInteger(value, 0, Int32.MaxValue, node);
						break;

					case "CacheLimiter":
                        CacheLimiter = value;
                        break;

					case "CookieLifetime":
						CookieLifetime = ConfigUtils.ParseInteger(value, 0, Int32.MaxValue, node);
						break;

					case "CookiePath":
                        CookiePath = value;
						break;

					case "CookieDomain":
                        CookieDomain = value;
						break;

					case "CookieSecure":
                        CookieSecure = value == "true";
						break;

					default:
						return false;
				}
				return true;
			}

			internal void Validate()
			{
				// sets default value if the path is null: 
				if (SavePath == null)
					SavePath = Path.GetTempPath();
			}
		}

		#endregion

		#region Date

		/// <summary>
		/// Date funtions options.
		/// </summary>
		[Serializable]
		public partial class DateSection : IPhpConfigurationSection
		{
			public bool Parse(string name, string value, XmlNode node)
			{
				switch (name)
				{
					case "Latitude":
						Latitude = ConfigUtils.ParseDouble(value, node);
						break;

					case "Longitude":
						Longitude = ConfigUtils.ParseDouble(value, node);
						break;

					case "SunsetZenith":
						SunsetZenith = ConfigUtils.ParseDouble(value, node);
						break;

					case "SunriseZenith":
						SunriseZenith = ConfigUtils.ParseDouble(value, node);
						break;

					case "TimeZone":
						if (!string.IsNullOrEmpty(value))
						{
							TimeZone = PhpTimeZone.GetTimeZone(value);
							if (TimeZone == null)
								throw new ConfigurationErrorsException(LibResources.GetString("unknown_timezone", value) + ".", node);
						}
						break;

					default:
						return false;
				}
				return true;
			}
		}

		#endregion

        #region Serialization

        /// <summary>
        /// Serialization functions options.
        /// </summary>
        [Serializable]
        public sealed class SerializationSection : IPhpConfigurationSection
        {
            /// <summary>
            /// A serializer used for serializing data. Can't contain a <B>null</B> reference.
            /// Setting <B>null</B> to the property will set the default PHP serializer.
            /// </summary>
            public Serializer DefaultSerializer
            {
                get
                {
                    return defaultSerializer;
                }
                set
                {
                    defaultSerializer = (value != null) ? value : PhpSerializer.Default;
                }
            }

            private Serializer defaultSerializer = PhpSerializer.Default;

            public bool Parse(string name, string value, XmlNode node)
            {
                switch (name)
                {
                    case "DefaultSerializer":
                        {
                            Serializer serializer = Serializers.GetSerializer(value);

                            if (serializer == null)
                                throw new ConfigurationErrorsException(LibResources.GetString("unknown_serializer", value) + ".", node);

                            this.defaultSerializer = serializer;
                            break;
                        }
                    default:
                        return false;
                }
                return true;
            }

            internal SerializationSection DeepCopy()
            {
                return (SerializationSection)this.MemberwiseClone();
            }
        }

        #endregion

        #region Legacy Configuration

        /// <summary>
		/// Gets, sets, or restores a value of a legacy configuration option.
		/// </summary>
		private static object GetSetRestore(LocalConfiguration config, string option, object value, IniAction action)
		{
			LibraryConfiguration local = (LibraryConfiguration)config.GetLibraryConfig(LibraryDescriptor.Singleton);
			LibraryConfiguration @default = DefaultLocal;

			switch (option)
			{
				case "sendmail_from": return PhpIni.GSR(ref local.Mailer.DefaultFromHeader, @default.Mailer.DefaultFromHeader, value, action);
				case "SMTP": return PhpIni.GSR(ref local.Mailer.SmtpServer, @default.Mailer.SmtpServer, value, action);
				case "smtp_port": return PhpIni.GSR(ref local.Mailer.SmtpPort, @default.Mailer.SmtpPort, value, action);

				case "highlight.bg": return PhpIni.GSR(ref local.Highlighting.Background, @default.Highlighting.Background, value, action);
				case "highlight.comment": return PhpIni.GSR(ref local.Highlighting.Comment, @default.Highlighting.Comment, value, action);
				case "highlight.default": return PhpIni.GSR(ref local.Highlighting.Default, @default.Highlighting.Default, value, action);
				case "highlight.html": return PhpIni.GSR(ref local.Highlighting.Html, @default.Highlighting.Html, value, action);
				case "highlight.keyword": return PhpIni.GSR(ref local.Highlighting.Keyword, @default.Highlighting.Keyword, value, action);
				case "highlight.string": return PhpIni.GSR(ref local.Highlighting.String, @default.Highlighting.String, value, action);

				case "session.serialize_handler": return PhpSession.GsrSerializer(local, @default, value, action);
				case "session.cache_expire": return PhpSession.GsrCacheExpire(value, action);
				case "session.cache_limiter": return PhpSession.GsrCacheLimiter(value, action);
				case "session.save_path": return PhpIni.GSR(ref local.Session.SavePath, @default.Session.SavePath, value, action);
				case "session.gc_maxlifetime": return PhpIni.GSR(ref local.Session.GcMaxLifetime, @default.Session.GcMaxLifetime, value, action);
				case "session.gc_probability": return PhpIni.GSR(ref local.Session.GcProbability, @default.Session.GcProbability, value, action);
				case "session.gc_divisor": return PhpIni.GSR(ref local.Session.GcDivisor, @default.Session.GcDivisor, value, action);

				case "session.cookie_lifetime": return PhpSession.GsrCookieLifetime(value, action);
				case "session.cookie_path": return PhpSession.GsrCookiePath(value, action);
				case "session.cookie_domain": return PhpSession.GsrCookieDomain(value, action);
				case "session.cookie_secure": return PhpSession.GsrCookieSecure(value, action);

				case "date.default_latitude": return PhpIni.GSR(ref local.Date.Latitude, @default.Date.Latitude, value, action);
				case "date.default_longitude": return PhpIni.GSR(ref local.Date.Longitude, @default.Date.Longitude, value, action);
				case "date.sunrise_zenith": return PhpIni.GSR(ref local.Date.SunriseZenith, @default.Date.SunriseZenith, value, action);
				case "date.sunset_zenith": return PhpIni.GSR(ref local.Date.SunsetZenith, @default.Date.SunsetZenith, value, action);
				case "date.timezone": return PhpTimeZone.GsrTimeZone(local, @default, value, action);
			}

			Debug.Fail("Option '" + option + "' is supported but not implemented.");
			return null;
		}

		/// <summary>
		/// Writes Phalanger BCL legacy options and their values to XML text stream.
		/// Skips options whose values are the same as default values of Phalanger.
		/// </summary>
		/// <param name="writer">XML writer.</param>
		/// <param name="options">A hashtable containing PHP names and option values. Consumed options are removed from the table.</param>
		/// <param name="writePhpNames">Whether to add "phpName" attribute to option nodes.</param>
		public static void LegacyOptionsToXml(XmlTextWriter writer, Hashtable options, bool writePhpNames) // GENERICS:<string,string>
		{
			if (writer == null)
				throw new ArgumentNullException("writer");
			if (options == null)
				throw new ArgumentNullException("options");

			LibraryConfiguration local = new LibraryConfiguration();
			PhpIniXmlWriter ow = new PhpIniXmlWriter(writer, options, writePhpNames);

			ow.StartSection("session");
			ow.WriteOption("session.cache_limiter", "CacheLimiter", "no-cache", PhpSession.DefaultCacheLimiter);
			ow.WriteOption("session.cache_expire", "CacheExpire", 180, PhpSession.DefaultCacheExpire);
			ow.WriteOption("session.serialize_handler", "Serializer", "php", local.Session.Serializer.Name);
			ow.WriteOption("session.gc_probability", "GcProbability", 1, local.Session.GcProbability);
			ow.WriteOption("session.gc_divisor", "GcDivisor", 100, local.Session.GcDivisor);
			ow.WriteOption("session.gc_maxlifetime", "GcMaxLifetime", 1440, local.Session.GcMaxLifetime);
			ow.WriteOption("session.save_path", "SavePath", "", local.Session.SavePath);
			ow.WriteOption("session.cookie_lifetime", "CookieLifetime", 0, PhpSession.DefaultCookieLifetime);
			ow.WriteOption("session.cookie_path", "CookiePath", "/", PhpSession.DefaultCookiePath);
			ow.WriteOption("session.cookie_domain", "CookieDomain", "", PhpSession.DefaultCookieDomain);
			ow.WriteOption("session.cookie_secure", "CookieSecure", false, PhpSession.DefaultCookieSecure);

			ow.StartSection("mailer");
			ow.WriteOption("SMTP", "SmtpServer", "localhost", local.Mailer.SmtpServer);
			ow.WriteOption("smtp_port", "SmtpPort", 25, local.Mailer.SmtpPort);
			ow.WriteOption("sendmail_from", "DefaultFromHeader", null, local.Mailer.DefaultFromHeader);

			ow.StartSection("highlighting");
			ow.WriteOption("highlight.bg", "Background", "#FFFFFF", local.Highlighting.Background);
			ow.WriteOption("highlight.string", "String", "#DD0000", local.Highlighting.String);
			ow.WriteOption("highlight.comment", "Comment", "#FF8000", local.Highlighting.Comment);
			ow.WriteOption("highlight.keyword", "Keyword", "#007700", local.Highlighting.Keyword);
			ow.WriteOption("highlight.html", "Html", "#000000", local.Highlighting.Html);
			ow.WriteOption("highlight.default", "Default", "#0000BB", local.Highlighting.Default);

			ow.StartSection("date");
			ow.WriteOption("date.default_latitude", "Latitude", 31.7667, local.Date.Latitude);
			ow.WriteOption("date.default_longitude", "Longitude", 35.2333, local.Date.Longitude);
			ow.WriteOption("date.sunrise_zenith", "SunriseZenith", 90.83, local.Date.SunriseZenith);
			ow.WriteOption("date.sunset_zenith", "SunsetZenith", 90.83, local.Date.SunsetZenith);
			ow.WriteOption("date.timezone", "TimeZone", null, local.Date.TimeZone.StandardName);

			ow.WriteEnd();
		}

		/// <summary>
		/// Registers legacy ini-options.
		/// </summary>
		internal static void RegisterLegacyOptions()
		{
			const string s = "standard";
			GetSetRestoreDelegate d = new GetSetRestoreDelegate(GetSetRestore);

			// highlighting:
			IniOptions.Register("highlight.bg", IniFlags.Supported | IniFlags.Local, d, s);
			IniOptions.Register("highlight.comment", IniFlags.Supported | IniFlags.Local, d, s);
			IniOptions.Register("highlight.default", IniFlags.Supported | IniFlags.Local, d, s);
			IniOptions.Register("highlight.html", IniFlags.Supported | IniFlags.Local, d, s);
			IniOptions.Register("highlight.keyword", IniFlags.Supported | IniFlags.Local, d, s);
			IniOptions.Register("highlight.string", IniFlags.Supported | IniFlags.Local, d, s);

			// e-mail:
			IniOptions.Register("SMTP", IniFlags.Supported | IniFlags.Local, d, s);
			IniOptions.Register("smtp_port", IniFlags.Supported | IniFlags.Local, d, s);
			IniOptions.Register("sendmail_from", IniFlags.Supported | IniFlags.Local, d, s);
			IniOptions.Register("sendmail_path", IniFlags.Unsupported | IniFlags.Global, d, s);

			// session:
			IniOptions.Register("session.cache_expire", IniFlags.Supported | IniFlags.Local | IniFlags.Http, d, s);
			IniOptions.Register("session.cache_limiter", IniFlags.Supported | IniFlags.Local | IniFlags.Http, d, s);
			IniOptions.Register("session.serialize_handler", IniFlags.Supported | IniFlags.Local | IniFlags.Http, d, s);
			IniOptions.Register("session.save_path", IniFlags.Supported | IniFlags.Local | IniFlags.Http, d, s);
			IniOptions.Register("session.gc_maxlifetime", IniFlags.Supported | IniFlags.Local | IniFlags.Http, d, s);
			IniOptions.Register("session.gc_probability", IniFlags.Supported | IniFlags.Local | IniFlags.Http, d, s);
			IniOptions.Register("session.gc_divisor", IniFlags.Supported | IniFlags.Local | IniFlags.Http, d, s);
			IniOptions.Register("session.cookie_domain", IniFlags.Supported | IniFlags.Local | IniFlags.Http, d, s);
			IniOptions.Register("session.cookie_lifetime", IniFlags.Supported | IniFlags.Local | IniFlags.Http, d, s);
			IniOptions.Register("session.cookie_path", IniFlags.Supported | IniFlags.Local | IniFlags.Http, d, s);
			IniOptions.Register("session.cookie_secure", IniFlags.Supported | IniFlags.Local | IniFlags.Http, d, s);

			IniOptions.Register("session.use_cookies", IniFlags.Unsupported | IniFlags.Local | IniFlags.Http, d, s);
			IniOptions.Register("session.use_only_cookies", IniFlags.Unsupported | IniFlags.Local | IniFlags.Http, d, s);
			IniOptions.Register("session.encode_sources", IniFlags.Unsupported | IniFlags.Local | IniFlags.Http, d, s);
			IniOptions.Register("session.entropy_file", IniFlags.Unsupported | IniFlags.Local | IniFlags.Http, d, s);
			IniOptions.Register("session.entropy_length", IniFlags.Unsupported | IniFlags.Local | IniFlags.Http, d, s);
			IniOptions.Register("session.referer_check", IniFlags.Unsupported | IniFlags.Local | IniFlags.Http, d, s);
			IniOptions.Register("session.use_trans_sid", IniFlags.Unsupported | IniFlags.Global | IniFlags.Http, d, s);

			// date:
			IniOptions.Register("date.default_latitude", IniFlags.Supported | IniFlags.Local, d, s);
			IniOptions.Register("date.default_longitude", IniFlags.Supported | IniFlags.Local, d, s);
			IniOptions.Register("date.sunrise_zenith", IniFlags.Supported | IniFlags.Local, d, s);
			IniOptions.Register("date.sunset_zenith", IniFlags.Supported | IniFlags.Local, d, s);
			IniOptions.Register("date.timezone", IniFlags.Supported | IniFlags.Local, d, s);
		}

		#endregion
	}
}
