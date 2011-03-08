/*

 Copyright (c) 2005-2006 Tomas Matousek.  

 This software is distributed under GNU General Public License version 2.
 The use and distribution terms for this software are contained in the file named LICENSE, 
 which can be found in the same directory as this file. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Web;
using System.Xml;
using System.Collections;
using System.Configuration;

using PHP.Core;

namespace PHP.Library.Data
{
  #region Local Configuration
  
  /// <summary>
  /// Script independent MySql configuration.
  /// </summary>
  [Serializable]
  public sealed class MySqlLocalConfig : IPhpConfiguration, IPhpConfigurationSection
  {
    internal MySqlLocalConfig() {}

    /// <summary>
    /// Request timeout in seconds. Non-positive value means no timeout.
    /// </summary>
    public int ConnectTimeout = 60;
    
    /// <summary>
    /// Default port.
    /// </summary>
    public int Port = 3306;
    
    /// <summary>
    /// Default server (host) name.
    /// </summary>
    public string Server = null;
    
    /// <summary>
    /// Default user name.
    /// </summary>
    public string User = null;
    
    /// <summary>
    /// Default password.
    /// </summary>
    public string Password = null;
    
    /// <summary>
		/// Creates a deep copy of the configuration record.
		/// </summary>
		/// <returns>The copy.</returns>
		public IPhpConfiguration DeepCopy()
		{
			return (MySqlLocalConfig)this.MemberwiseClone();
    }

    /// <summary>
    /// Parses XML configuration file.
    /// </summary>
    public bool Parse(string name, string value, XmlNode node)
    {
      switch (name)
      {
        case "ConnectTimeout":
				  ConnectTimeout = ConfigUtils.ParseInteger(value,Int32.MinValue,Int32.MaxValue,node);
					break; 
				
				case "Port":
				  Port = ConfigUtils.ParseInteger(value,0,UInt16.MaxValue,node);
					break;
				
				case "Server":
				  Server = value;
				  break;
				  
				case "User":
				  User = value;
				  break;
				  
				case "Password":
				  Password = value;
				  break;
					
        default: 
          return false;
      }
      return true;
    }
  }
  
  #endregion
  
  #region Global Configuration
  
  /// <summary>
  /// Script dependent MySql configuration.
  /// </summary>
  [Serializable]
  public sealed class MySqlGlobalConfig : IPhpConfiguration, IPhpConfigurationSection
  {
    internal MySqlGlobalConfig() {}

    /// <summary>
    /// Maximum number of connections per request. Negative value means no limit.
    /// </summary>
    public int MaxConnections = -1;
    
    /// <summary>
    /// Parses XML configuration file.
    /// </summary>
    public bool Parse(string name, string value, XmlNode node)
    {
      switch (name)
      {
        case "MaxConnections":
          MaxConnections = ConfigUtils.ParseInteger(value,Int32.MinValue,Int32.MaxValue,node);
					break; 
      
        default: 
          return false;
      }
      return true;
    }

    /// <summary>
		/// Creates a deep copy of the configuration record.
		/// </summary>
		/// <returns>The copy.</returns>
		public IPhpConfiguration DeepCopy()
		{
			return (MySqlGlobalConfig)this.MemberwiseClone();
    }
  }
  
  #endregion
  
  /// <summary>
  /// MSSQL extension configuration.
  /// </summary>
  public sealed class MySqlConfiguration
  {
    private MySqlConfiguration() {}
  
    #region Legacy Configuration

    /// <summary>
		/// Gets, sets, or restores a value of a legacy configuration option.
		/// </summary>
		private static object GetSetRestore(LocalConfiguration config,string option,object value,IniAction action)
		{
      MySqlLocalConfig local = (MySqlLocalConfig)config.GetLibraryConfig(MySqlLibraryDescriptor.Singleton);
      MySqlLocalConfig @default = DefaultLocal;
      MySqlGlobalConfig global = Global;
      
      switch (option)
      {
		    // local:
		    
		    case "mysql.connect_timeout": return PhpIni.GSR(ref local.ConnectTimeout,@default.ConnectTimeout,value,action);
        case "mysql.default_port": return PhpIni.GSR(ref local.Port,@default.Port,value,action);
        case "mysql.default_host": return PhpIni.GSR(ref local.Server,@default.Server,value,action);
        case "mysql.default_user": return PhpIni.GSR(ref local.User,@default.User,value,action);
        case "mysql.default_password": return PhpIni.GSR(ref local.Password,@default.Password,value,action);

		    // global:
		    
		    case "mysql.max_links": 
		      Debug.Assert(action == IniAction.Get);
		      return PhpIni.GSR(ref global.MaxConnections,-1,null,action);
      }
      
      Debug.Fail("Option '"+option+"' is supported but not implemented.");
      return null;
		}
		
		/// <summary>
		/// Writes MySql legacy options and their values to XML text stream.
		/// Skips options whose values are the same as default values of Phalanger.
		/// </summary>
		/// <param name="writer">XML writer.</param>
		/// <param name="options">A hashtable containing PHP names and option values. Consumed options are removed from the table.</param>
		/// <param name="writePhpNames">Whether to add "phpName" attribute to option nodes.</param>
		public static void LegacyOptionsToXml(XmlTextWriter writer,Hashtable options,bool writePhpNames) // GENERICS:<string,string>
		{
		  if (writer==null)
		    throw new ArgumentNullException("writer");
		  if (options==null)
		    throw new ArgumentNullException("options");
		    
	    MySqlLocalConfig local = new MySqlLocalConfig();
      MySqlGlobalConfig global = new MySqlGlobalConfig();
      PhpIniXmlWriter ow = new PhpIniXmlWriter(writer,options,writePhpNames);
		  
			ow.StartSection("mysql");
      
      // local:
      ow.WriteOption("mysql.default_port","Port",3306,local.Port);
			ow.WriteOption("mysql.default_host","Server",null,local.Server);
			ow.WriteOption("mysql.default_user","User",null,local.User);
			ow.WriteOption("mysql.default_password","Password",null,local.Password);
			ow.WriteOption("mysql.connect_timeout","ConnectTimeout",0,local.ConnectTimeout);
			
			// global:
      ow.WriteOption("mysql.max_links","MaxConnections",-1,global.MaxConnections);
			
			ow.WriteEnd();
		}
		
		/// <summary>
		/// Registers legacy ini-options.
		/// </summary>
		internal static void RegisterLegacyOptions()
		{
		  const string s = MySqlLibraryDescriptor.ExtensionName;
		  GetSetRestoreDelegate d = new GetSetRestoreDelegate(GetSetRestore);

		  // local:
      IniOptions.Register("mysql.trace_mode",      IniFlags.Unsupported | IniFlags.Local,d,s);
      IniOptions.Register("mysql.default_port",    IniFlags.Supported   | IniFlags.Local,d,s);
      IniOptions.Register("mysql.default_socket",  IniFlags.Unsupported | IniFlags.Local,d,s);
      IniOptions.Register("mysql.default_host",    IniFlags.Supported   | IniFlags.Local,d,s);
      IniOptions.Register("mysql.default_user",    IniFlags.Supported   | IniFlags.Local,d,s);
      IniOptions.Register("mysql.default_password",IniFlags.Supported   | IniFlags.Local,d,s);
      IniOptions.Register("mysql.connect_timeout", IniFlags.Supported   | IniFlags.Local,d,s);

		  // global:
      IniOptions.Register("mysql.allow_persistent",IniFlags.Unsupported | IniFlags.Global,d,s);
      IniOptions.Register("mysql.max_persistent",  IniFlags.Unsupported | IniFlags.Global,d,s);
      IniOptions.Register("mysql.max_links",       IniFlags.Supported   | IniFlags.Global,d,s);
		}
		    
    #endregion		
		
		#region Configuration Getters
		
		/// <summary>
		/// Gets the library configuration associated with the current script context.
		/// </summary>
		public static MySqlLocalConfig Local 
		{ 
		  get 
		  { 
		    return (MySqlLocalConfig)Configuration.Local.GetLibraryConfig(MySqlLibraryDescriptor.Singleton);
		  } 
		}
		
		/// <summary>
		/// Gets the default library configuration.
		/// </summary>
		public static MySqlLocalConfig DefaultLocal 
		{ 
		  get 
		  { 
		    return (MySqlLocalConfig)Configuration.DefaultLocal.GetLibraryConfig(MySqlLibraryDescriptor.Singleton);
		  } 
		}
		
		/// <summary>
		/// Gets the global library configuration.
		/// </summary>
		public static MySqlGlobalConfig Global 
		{ 
		  get 
		  { 
		    return (MySqlGlobalConfig)Configuration.Global.GetLibraryConfig(MySqlLibraryDescriptor.Singleton);
		  } 
		}
		
		#endregion
  }
}
