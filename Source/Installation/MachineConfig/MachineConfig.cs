/*

 Copyright (c) 2004-2006 Ladislav Prosek.

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
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;

namespace MachineConfig
{
	/// <summary>
	/// Simple installer component that adds/removes Phalanger configuration entries to/from Machine.config.
	/// Executed during installation/uninstallation.
	/// </summary>
	[RunInstaller(true)]
	public class MachineConfig : Installer
	{
		#region Fields

		/// <summary>
		/// XPath of the Phalanger config section designation.
		/// </summary>
		private const string configSectionDesignation = "/configuration/configSections/section[@name='phpNet']";

		/// <summary>
		/// XPath of the Phalanger config section itself.
		/// </summary>
		private const string configSection = "/configuration/phpNet";

		/// <summary>
		/// XPath of the Phalanger HTTP handler entry.
		/// </summary>
		private const string httpHandler = "/configuration/system.web/httpHandlers/add[@path='*.php']";

		/// <summary>
		/// XPath of the Phalanger CodeDom provider entry.
		/// </summary>
		private const string codeDomProvider = "/configuration/system.codedom/compilers/compiler[@language='PHP']";

		/// <summary>
		/// The namespace URI (possibly) referenced by .NET configuration files.
		/// </summary>
		private const string configNamespaceUri = "http://schemas.microsoft.com/.NetConfiguration/v2.0";

		#endregion

		#region Helpers

        /// <summary>
        /// Get full directory path of existing directory that starts with given string.
        /// </summary>
        /// <param name="startsWith"></param>
        /// <returns></returns>
        private static string FindDir(string startsWith)
        {
            DirectoryInfo di = new DirectoryInfo(startsWith);

            startsWith = startsWith.ToLower();

            if (di.Exists) return di.FullName;
            if (!di.Parent.Exists) return null;

            foreach (var dir in di.Parent.GetDirectories())
            {
                if (dir.FullName.ToLower().StartsWith(startsWith))
                    return dir.FullName;
            }

            return null;
        }

        /// <summary>
		/// Shows a message box informing the user about a problem.
		/// </summary>
		/// <param name="format">A message containing zero or more format items.</param>
		/// <param name="args">Arguments to format items in <paramref name="format"/>.</param>
		private static void ShowError(string format, params object[] args)
		{
			MessageBox.Show(String.Format(format, args), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		
		/// <summary>
		/// Loads Machine.config and Web.config into DOM.
		/// </summary>
		/// <param name="machineConfig">DOM representation of Machine.config.</param>
		/// <param name="webConfig">DOM representation of Web.config.</param>
        private static void ReadConfigs(out XmlDocument machineConfig, out XmlDocument webConfig, string ConfigDirectory)
		{
            

            // machine.config
			machineConfig = new XmlDocument();

			try
			{
                //machineConfig.Load(Path.Combine(HttpRuntime.MachineConfigurationDirectory, "machine.config"));
                machineConfig.Load(ConfigDirectory + "machine.config");
			}
			catch (XmlException e)
			{
				ShowError("Could not read Machine.config ({0})", e.Message);
				throw;
			}
			catch (IOException e)
			{
				ShowError("Could not read Machine.config ({0})", e.Message);
				throw;
			}

            // web.config
			webConfig = new XmlDocument();

			try
			{
				//webConfig.Load(Path.Combine(HttpRuntime.MachineConfigurationDirectory, "web.config"));
                webConfig.Load(ConfigDirectory + "web.config");
			}
			catch (XmlException e)
			{
				ShowError("Could not read root Web.config ({0})", e.Message);
				throw;
			}
			catch (IOException e)
			{
				ShowError("Could not read root Web.config ({0})", e.Message);
				throw;
			}
		}

		/// <summary>
		/// Saves Machine.config and Web.config given their DOM representations.
		/// </summary>
		/// <param name="machineConfig">The DOM representation of Machine.config.</param>
		/// <param name="webConfig">The DOM representation of Web.config.</param>
		private static void WriteConfigs(XmlDocument machineConfig, XmlDocument webConfig, string ConfigDirectory)
		{
			try
			{
				//machineConfig.Save(Path.Combine(HttpRuntime.MachineConfigurationDirectory, "machine.config"));
                machineConfig.Save(ConfigDirectory + "machine.config");
			}
			catch (XmlException e)
			{
				ShowError("Error: Could not write Machine.config ({0})", e.Message);
				throw;
			}
			catch (IOException e)
			{
				ShowError("Could not write Machine.config ({0})", e.Message);
				throw;
			}

			try
			{
				//webConfig.Save(Path.Combine(HttpRuntime.MachineConfigurationDirectory, "web.config"));
			    webConfig.Save(ConfigDirectory + "web.config");
            }
			catch (XmlException e)
			{
				ShowError("Error: Could not write root Web.config ({0})", e.Message);
				throw;
			}
			catch (IOException e)
			{
				ShowError("Could not write root Web.config ({0})", e.Message);
				throw;
			}
		}

        /// <summary>
        /// // remove the Phalanger related nodes from given XML files.
        /// </summary>
        /// <param name="machineConfig"></param>
        /// <param name="webConfig"></param>
        private static void UninstallXmlNodes(XmlDocument machineConfig, XmlDocument webConfig)
        {
            // remove the Phalanger related nodes
            XmlRemove(machineConfig, configSectionDesignation);
            XmlRemove(machineConfig, codeDomProvider);
            XmlRemove(machineConfig, configSection);

            XmlRemove(webConfig, httpHandler);
        }

		/// <summary>
		/// Removes all nodes found by the specified XPath expression.
		/// </summary>
		/// <param name="node">The root node.</param>
		/// <param name="xpath">The XPath expression.</param>
		private static void XmlRemove(XmlNode node, string xpath)
		{
			XmlNodeList node_list = node.SelectNodes(xpath);
			for (int i = 0; i < node_list.Count; i++) node_list[i].ParentNode.RemoveChild(node_list[i]);

			// try to add the namespace
			XmlNamespaceManager namespace_manager = new XmlNamespaceManager(new NameTable());
			namespace_manager.AddNamespace("x", configNamespaceUri);

			xpath = xpath.Replace("/", "/x:");

			node_list = node.SelectNodes(xpath, namespace_manager);
			for (int i = 0; i < node_list.Count; i++) node_list[i].ParentNode.RemoveChild(node_list[i]);
		}

		/// <summary>
		/// Merges two XML subtrees.
		/// </summary>
		/// <param name="destination">The destination node.</param>
		/// <param name="source">The source node.</param>
		/// <param name="variables">Variables to be substituted in attribute values.</param>
		/// <param name="append"><B>True</B> to append <paramref name="source"/>'s children nodes to
		/// <paramref name="destination"/>'s children list, <B>false</B> to insert them as first children.</param>
		/// <returns><B>True</B> if successfully merged, <B>false</B> otherwise.</returns>
		private static bool XmlMerge(XmlNode destination, XmlNode source, Dictionary<string, string> variables, bool append/*, bool skipX86Extensions*/)
		{
			if (destination.NodeType != source.NodeType || destination.Name != source.Name) return false;

			// check attributes for equality
			bool equals = true;

			if (source.Attributes != null && destination.Attributes != null)
			{
				foreach (XmlAttribute src_attr in source.Attributes)
				{
					XmlAttribute dst_attr = destination.Attributes[src_attr.Name];
					if (dst_attr == null || dst_attr.Value != src_attr.Value)
					{
						equals = false;
						break;
					}
				}
			}

			if (!equals) return false;
			
			// iterate over children
			foreach (XmlNode src_child in source.ChildNodes)
			{
				if (src_child is XmlElement)
				{
                    /*if (skipX86Extensions)
                    {
                        var ass = src_child.Attributes["assembly"];

                        // skip assemblies with this key, because they are not compatible with x64 system
                        if (ass != null && ass.Value.Contains("PublicKeyToken=4ef6ed87c53048a3"))
                        {
                            continue;
                        }
                    }*/

					equals = false;

					foreach (XmlNode dst_child in destination.ChildNodes)
					{
                        if (XmlMerge(dst_child, src_child, variables, append/*, skipX86Extensions*/))
						{
							equals = true;
							break;
						}
					}

					if (!equals)
					{
						// we are adding the source subtree here
						if (append) destination.AppendChild(XmlAdopt(destination, src_child, variables));
						else destination.InsertBefore(XmlAdopt(destination, src_child, variables), destination.FirstChild);
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Imports a <paramref name="source"/> node to the document represented by a <paramref name="destination"/>
		/// node, substituting variable names for variable values.
		/// </summary>
		/// <param name="destination">The destination node.</param>
		/// <param name="source">The source node.</param>
		/// <param name="variables">The variable dictionary.</param>
		/// <returns>The source node imported to <paramref name="destination"/>'s document.</returns>
		private static XmlNode XmlAdopt(XmlNode destination, XmlNode source, Dictionary<string, string> variables)
		{
			if (destination.NamespaceURI != source.NamespaceURI)
			{
				// adjust namespace URIs
				source = XmlImport(destination, source);
			}
			else source = destination.OwnerDocument.ImportNode(source, true);

			// substitute variables
			foreach (XmlNode child in source.SelectNodes("//*"))
			{
				foreach (XmlAttribute attr in child.Attributes)
				{
					foreach (KeyValuePair<string, string> pair in variables)
					{
						attr.Value = attr.Value.Replace("{$" + pair.Key + "}", pair.Value);
					}
				}
			}

			return source;
		}

		/// <summary>
		/// Imports a <paramref name="source"/> node to the document represented by a <paramref name="destination"/>
		/// node, changing namespace URIs to prevent any <c>xmlns</c> attribute from being added.
		/// </summary>
		/// <param name="destination">The destination node.</param>
		/// <param name="source">The source node.</param>
		/// <returns>The source node with its entire subtree having the same namespace URI as destination.</returns>
		private static XmlNode XmlImport(XmlNode destination, XmlNode source)
		{
			// create a new node with the matching namespace URI
			XmlNode new_node = destination.OwnerDocument.CreateNode(
				source.NodeType,
				source.Name,
				destination.NamespaceURI);

			// copy attributes
			foreach (XmlAttribute attr in source.Attributes)
			{
				XmlAttribute new_attr = destination.OwnerDocument.CreateAttribute(
					attr.Name,
					attr.NamespaceURI);

				new_attr.Value = attr.Value;

				new_node.Attributes.Append(new_attr);
			}

			// copy child nodes
			foreach (XmlNode node in source.ChildNodes)
			{
				new_node.AppendChild(XmlImport(destination, node));
			}

			return new_node;
		}

		#endregion

        #region CONFIG directories

        /// <summary>
        /// List of directories (ending with \) containing machine.config and web.config files.
        /// </summary>
        internal string[] ConfigDirectories
        {
            get
            {
                if (_ConfigDirectories == null)
                {
                    string systemRoot = Environment.GetEnvironmentVariable("SystemRoot") + "\\";
                    List<string> dirs = new List<string>(2);

                    dirs.Add(FindDir(systemRoot + @"Microsoft.NET\Framework\v4.0") + @"\Config\");
                    dirs.Add(FindDir(systemRoot + @"Microsoft.NET\Framework64\v4.0") + @"\Config\");

                    _ConfigDirectories = dirs.ToArray();
                }

                return _ConfigDirectories;
            }
        }
        private string[] _ConfigDirectories = null;
        

        #endregion
        
        #region Uninstall, Install Overrides

        /// <summary>
		/// Removes the installation by deleting Phalanger entries from Machine.config.
		/// </summary>
		/// <param name="savedState">An <see cref="IDictionary"/> that contains the state of the computer after
		/// the installation was complete.</param>
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Uninstall(IDictionary savedState)
		{
			XmlDocument machine_config;
			XmlDocument web_config;

            foreach (string configDir in ConfigDirectories)
            {
                // the directory does not exists - unsupported environment, skip it
                if (!Directory.Exists(configDir))
                    continue;

                //
                ReadConfigs(out machine_config, out web_config, configDir);

                UninstallXmlNodes(machine_config, web_config);

                WriteConfigs(machine_config, web_config, configDir);
            }
		}

		/// <summary>
		/// Performs the installation by adding Phalanger entries to Machine.config.
		/// </summary>
		/// <param name="stateSaver">An <see cref="IDictionary"/> used to save information needed to perform a commit,
		/// rollback, or uninstall operation.</param>
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Install(IDictionary stateSaver)
		{
            // check the installation directory
			string install_dir = Context.Parameters["InstallDir"].TrimEnd('\\', '/', ' ');

			if (!Directory.Exists(install_dir))
			{
				ShowError("Directory '{0}' does not exist.", install_dir);
				return;
			}

            // modify .NET .config files
            XmlDocument machine_config;
			XmlDocument web_config;

            // install both - x86 and x64 configuration files
            foreach (string configDir in ConfigDirectories)
            {
                // if the directory does not exists - unsupported environment, skip it
                if (!Directory.Exists(configDir))
                    continue;

                //
                ReadConfigs(out machine_config, out web_config, configDir);

                // remove the Phalanger related nodes if already present
                UninstallXmlNodes(machine_config, web_config);

                // merge with config file templates stored in resources
                XmlDocument machine_config_template = new XmlDocument();
                XmlDocument web_config_template = new XmlDocument();

                machine_config_template.LoadXml(Resources.MachineConfig);
                web_config_template.LoadXml(Resources.WebConfig);

                Dictionary<string, string> variables = new Dictionary<string, string>();
                variables.Add("TARGETDIR", install_dir);

                XmlMerge(machine_config, machine_config_template, variables, true/*, configDir.Contains("\\Framework64\\")*/);
                XmlMerge(web_config, web_config_template, variables, false/*, false*/);

                // save configuration files
                WriteConfigs(machine_config, web_config, configDir);
            }
		}

		#endregion
	}
}
