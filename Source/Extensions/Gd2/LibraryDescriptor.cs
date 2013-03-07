/*

 Copyright (c) 2005-2011 Devsense.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Xml;

namespace PHP.Library.Gd2
{
    /// <summary>
    /// Describes the class library assembly content and configuration.
    /// </summary>
    public sealed class GdLibraryDescriptor : PhpLibraryDescriptor
    {
        /// <summary>
        /// Stores one and only instance of the class that is created when the assembly is loaded.
        /// </summary>
        internal static GdLibraryDescriptor Singleton { get { return singleton; } }
        private static GdLibraryDescriptor singleton;

        /// <summary>
        /// Called by the Core after the library is loaded.
        /// </summary>
        protected override void Loaded(PhpLibraryAttribute assemblyAttribute, LibraryConfigStore configStore)
        {
            base.Loaded(assemblyAttribute, configStore);
            singleton = this;
            GdConfiguration.RegisterLegacyOptions();
        }

        /// <summary>
        /// Parses a configuration section belonging to the MySql library. 
        /// </summary>
        /// <param name="result">A configuration context.</param>
        /// <param name="context">The context of the configuration created by Phalanger Core.</param>
        /// <param name="section">A XML node containing the configuration or its part.</param>
        /// <returns>Updated configuration context.</returns>
        protected override ConfigContextBase ParseConfig(ConfigContextBase result, PhpConfigurationContext context, XmlNode section)
        {
            // parses XML tree:
            ConfigUtils.ParseNameValueList(section, context, (GdLocalConfig)result.Local, (GdGlobalConfig)result.Global);

            return result;
        }

        /// <summary>
        /// Creates empty library configuration context.
        /// </summary>
        /// <returns>An initialized configuration context. Should not be a <B>null</B> reference.</returns>
        protected override ConfigContextBase CreateConfigContext()
        {
            return new ConfigContextBase(new GdLocalConfig(), new GdGlobalConfig());
        }

    }
}
