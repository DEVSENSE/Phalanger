/*

 Copyright (c) 2005-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/


using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Configuration;
using System.Runtime.Serialization;

using PHP;
using PHP.Core;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Library
{
	public sealed partial class LibraryConfiguration : IPhpConfiguration
	{
		#region Highlighting

		/// <summary>
		/// Highlighting functions options.
		/// </summary>
		public partial class HighlightingSection : IPhpConfigurationSection
		{
			/// <summary>String color.</summary>
			public string String = "navy";

			/// <summary>Comment color.</summary>
			public string Comment = "green";

			/// <summary>Keyword color.</summary>
			public string Keyword = "blue";

			/// <summary>Background color.</summary>
			public string Background = "white";

			/// <summary>HTML color.</summary>
			public string Html = "gray";

			/// <summary>Script tags color (<c>&lt;?</c>, <c>?&gt;</c>, <c>&lt;%</c>, <c>?&gt;%</c>, ...).</summary>
			public string ScriptTags = "red";

			/// <summary>Default foreground color.</summary>
			public string Default = "black";

			internal HighlightingSection DeepCopy()
			{
				return (HighlightingSection)this.MemberwiseClone();
			}
		}

		#endregion

		#region Date

		/// <summary>
		/// Date funtions options.
		/// </summary>
		public partial class DateSection : IPhpConfigurationSection
		{
			/// <summary>
			/// Default latitude (used for calculating sunrise and sunset times).
			/// </summary>
			public double Latitude = 31.7667;

			/// <summary>
			/// Default longitude (used for calculating sunrise and sunset times).
			/// </summary>
			public double Longitude = 35.2333;

			/// <summary>
			/// Default longitude (used for calculating sunrise and sunset times).
			/// </summary>
			public double SunsetZenith = 90.83;

			/// <summary>
			/// Default longitude (used for calculating sunrise and sunset times).
			/// </summary>
			public double SunriseZenith = 90.83;

			/// <summary>
			/// Default timezone used by time-zone functions.
			/// </summary>
			public TimeZoneInfo TimeZone = null;

			internal DateSection DeepCopy()
			{
				return (DateSection)this.MemberwiseClone();
			}
		}

		#endregion

		#region Construction, Copying

        public readonly HighlightingSection Highlighting;
        public readonly DateSection Date;
#if !SILVERLIGHT
        public readonly MailerSection Mailer;
        public readonly SessionSection Session;
        public readonly SerializationSection Serialization;
#endif

        public LibraryConfiguration()
        {
            this.Highlighting = new HighlightingSection();
            this.Date = new DateSection();
#if !SILVERLIGHT
            this.Mailer = new MailerSection();
            this.Session = new SessionSection();
            this.Serialization = new SerializationSection();
#endif
        }

        private LibraryConfiguration(LibraryConfiguration source)
        {
            this.Highlighting = source.Highlighting.DeepCopy();
            this.Date = source.Date.DeepCopy();
#if !SILVERLIGHT
            this.Mailer = source.Mailer.DeepCopy();
            this.Session = source.Session.DeepCopy();
            this.Serialization = source.Serialization.DeepCopy();
#endif
        }

		/// <summary>
		/// Creates a deep copy of the configuration record.
		/// </summary>
		/// <returns>The copy.</returns>
		public IPhpConfiguration DeepCopy()
		{
			return new LibraryConfiguration(this);
		}

		#endregion

		#region Configuration Getters

		/// <summary>
		/// Gets the library configuration associated with the current script context.
		/// </summary>
		public static LibraryConfiguration Local
		{
			get
			{
				return (LibraryConfiguration)Core.Configuration.Local.GetLibraryConfig(LibraryDescriptor.Singleton);
			}
		}

		/// <summary>
		/// Gets the default library configuration.
		/// </summary>
		public static LibraryConfiguration DefaultLocal
		{
			get
			{
				return (LibraryConfiguration)Core.Configuration.DefaultLocal.GetLibraryConfig(LibraryDescriptor.Singleton);
			}
		}

		/// <summary>
		/// Gets local configuration associated with a specified script context.
		/// </summary>
		/// <param name="context">Scritp context.</param>
		/// <returns>Local library configuration.</returns>
		public static LibraryConfiguration GetLocal(ScriptContext/*!*/ context)
		{
			if (context == null)
				throw new ArgumentNullException("context");

			return (LibraryConfiguration)context.Config.GetLibraryConfig(LibraryDescriptor.Singleton);
		}

		#endregion
	}
}
