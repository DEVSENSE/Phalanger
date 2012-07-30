/*

 Copyright (c) 2005-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Xml;
using System.IO;
using System.Configuration;
using System.Runtime.Serialization;

using PHP.Core;
#if !SILVERLIGHT
using System.Web;
using System.Runtime.Serialization.Formatters.Binary;
#endif

namespace PHP.Library
{
	/// <summary>
	/// Describes the class library assembly content and configuration.
	/// </summary>
	public sealed class LibraryDescriptor : PhpLibraryDescriptor
	{
        // default extension:
        public const string ExtStandard = "standard";

        // other extensions:
        //public const string ExtCalendar = "calendar";//There isn't managed calender in class library
        public const string ExtCore = "Core";//Class Library defines some functions/constants that in PHP belongs to Core
        public const string ExtSession = "session";
        public const string ExtCType = "ctype";
        public const string ExtTokenizer = "tokenizer";
        public const string ExtDate = "date";
        public const string ExtPcre = "pcre";
        public const string ExtEreg = "ereg";
        public const string ExtJson = "json";
        public const string ExtHash = "hash";
        public const string ExtSpl = "SPL";

		/// <summary>
		/// Stores one and only instance of the class that is created when the assembly is loaded.
		/// </summary>
		internal static LibraryDescriptor Singleton { get { return singleton; } }
		private static LibraryDescriptor singleton;

        ///// <summary>
        ///// Gets a list of implemented extensions.
        ///// </summary>
        //public override string[] ImplementedExtensions
        //{
        //    get { return new string[] { ExtStandard, ExtCore,/* ExtCalendar,*/ ExtCType, ExtSession, ExtTokenizer, ExtDate, ExtPcre, ExtEreg, ExtJson, ExtHash, ExtSpl }; }
        //}

		/// <summary>
		/// Called by the Core after the library is loaded.
		/// </summary>
		protected override void Loaded(PhpLibraryAttribute assemblyAttribute, LibraryConfigStore config)
		{
			base.Loaded(assemblyAttribute, config);
			singleton = this;

#if !SILVERLIGHT
			LibraryConfiguration.RegisterLegacyOptions();

			// registers session handlers:
			SessionHandlers.RegisterHandler(PhpSessionHandler.Default);
			SessionHandlers.RegisterHandler(PhpUserSessionHandler.Default);
            SessionHandlers.RegisterHandler(AspNetThruSessionHandler.Default);

			// registers serializers:
			Serializers.RegisterSerializer(PhpSerializer.Default);
            //Serializers.RegisterSerializer(PhalangerSerializer.Default);
			Serializers.RegisterSerializer(new ContextualSerializer("dotnet", delegate(PHP.Core.Reflection.DTypeDesc caller/*ignored*/)
			{
				return new BinaryFormatter(
					null,
					new StreamingContext(StreamingContextStates.Persistence, new SerializationContext()));
			}));
#endif
		}

#if !SILVERLIGHT
		/// <summary>
		/// Creates empty library configuration context.
		/// </summary>
		/// <returns>An initialized configuration context. Should not be a <B>null</B> reference.</returns>
		protected override ConfigContextBase CreateConfigContext()
		{
			return new ConfigContextBase(new LibraryConfiguration(), null);
		}

		/// <summary>
		/// Parses a configuration section belonging to the library. 
		/// </summary>
		/// <param name="result">A library configuration context.</param>
		/// <param name="context">The context of the configuration created by Phalanger Core.</param>
		/// <param name="section">A XML node containing the configuration or its part.</param>
		/// <returns>Updated library configuration context.</returns>
		protected override ConfigContextBase ParseConfig(ConfigContextBase result, PhpConfigurationContext context, XmlNode section)
		{
			LibraryConfiguration local = (LibraryConfiguration)result.Local;

			// parses XML tree:
			foreach (XmlNode node in section.ChildNodes)
			{
				if (node.NodeType == XmlNodeType.Element)
				{
					switch (node.Name)
					{
						case "mailer":
							ConfigUtils.ParseNameValueList(node, context, local.Mailer);
							break;

						case "highlighting":
							ConfigUtils.ParseNameValueList(node, context, local.Highlighting);
							break;

						case "session":

							// allowed only in web application configuration:
							if (HttpContext.Current == null)
								throw new ConfigurationErrorsException(CoreResources.GetString("web_only_option"), node);

							ConfigUtils.ParseNameValueList(node, context, local.Session);
							break;

						case "date":
							ConfigUtils.ParseNameValueList(node, context, local.Date);
							break;

                        case "serialization":
                            ConfigUtils.ParseNameValueList(node, context, local.Serialization);
                            break;

						default:
							throw new ConfigUtils.InvalidNodeException(node);
					}
				}
			}

			return result;
		}
#endif
	}
}  


