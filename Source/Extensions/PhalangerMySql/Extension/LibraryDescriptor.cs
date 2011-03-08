/*

 Copyright (c) 2005-2006 Tomas Matousek.  

 This software is distributed under GNU General Public License version 2.
 The use and distribution terms for this software are contained in the file named LICENSE, 
 which can be found in the same directory as this file. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Xml;
using System.IO;
using System.Configuration;

using PHP.Core;

namespace PHP.Library.Data
{
  /// <summary>
  /// Describes the class library assembly content and configuration.
  /// </summary>
  public sealed class MySqlLibraryDescriptor : PhpLibraryDescriptor
  {
    /// <summary>
    /// Stores one and only instance of the class that is created when the assembly is loaded.
    /// </summary>
    internal static MySqlLibraryDescriptor Singleton { get { return singleton; } }
    private static MySqlLibraryDescriptor singleton;
    
    ///// <summary>
    ///// Gets a list of implemented extensions.
    ///// </summary>
    //public override string[] ImplementedExtensions
    //{
    //  get { return new string[] { ExtensionName }; } 
    //}
    internal const string ExtensionName = "mysql";

    /// <summary>
    /// Called by the Core after the library is loaded.
    /// </summary>
    protected override void Loaded(PhpLibraryAttribute assemblyAttribute, LibraryConfigStore configStore)
    {
      base.Loaded(assemblyAttribute, configStore);
      singleton = this;
      MySqlConfiguration.RegisterLegacyOptions();
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
			ConfigUtils.ParseNameValueList(section,context,(MySqlLocalConfig)result.Local,(MySqlGlobalConfig)result.Global);
			
			return result;				
    }
     
     /// <summary>
    /// Creates empty library configuration context.
    /// </summary>
    /// <returns>An initialized configuration context. Should not be a <B>null</B> reference.</returns>
    protected override ConfigContextBase CreateConfigContext()
    {
      return new ConfigContextBase(new MySqlLocalConfig(),new MySqlGlobalConfig());
    }
        
  }
}  
