namespace MySql.Data.MySqlClient
{
    using Microsoft.Win32;
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Configuration.Install;
    using System.IO;
    using System.Reflection;
    using System.Security.Permissions;
    using System.Xml;

    [RunInstaller(true), PermissionSet(SecurityAction.LinkDemand, Name="FullTrust"), PermissionSet(SecurityAction.InheritanceDemand, Name="FullTrust")]
    public class CustomInstaller : Installer
    {
        private static void AddProviderToMachineConfig()
        {
            object obj2 = Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\.NETFramework\", "InstallRoot", null);
            if (obj2 == null)
            {
                throw new Exception("Unable to retrieve install root for .NET framework");
            }
            UpdateMachineConfigs(obj2.ToString(), true);
            string str = obj2.ToString();
            str = str.Substring(0, str.Length - 1);
            str = string.Format("{0}64{1}", str, Path.DirectorySeparatorChar);
            if (Directory.Exists(str))
            {
                UpdateMachineConfigs(str, true);
            }
        }

        private static void AddProviderToMachineConfigInDir(string path)
        {
            string str = string.Format(@"{0}\machine.config", path);
            if (File.Exists(str))
            {
                StreamReader reader = new StreamReader(str);
                string xml = reader.ReadToEnd();
                reader.Close();
                XmlDocument document = new XmlDocument();
                document.LoadXml(xml);
                XmlElement newChild = (XmlElement) document.CreateNode(XmlNodeType.Element, "add", "");
                newChild.SetAttribute("name", "MySQL Data Provider");
                newChild.SetAttribute("invariant", "MySql.Data.MySqlClient");
                newChild.SetAttribute("description", ".Net Framework Data Provider for MySQL");
                Assembly executingAssembly = Assembly.GetExecutingAssembly();
                string str3 = string.Format("MySql.Data.MySqlClient.MySqlClientFactory, {0}", executingAssembly.FullName.Replace("Installers", "Data"));
                newChild.SetAttribute("type", str3);
                XmlNodeList elementsByTagName = document.GetElementsByTagName("DbProviderFactories");
                foreach (XmlNode node in elementsByTagName[0].ChildNodes)
                {
                    if (node.Attributes != null)
                    {
                        foreach (XmlAttribute attribute in node.Attributes)
                        {
                            if ((attribute.Name == "invariant") && (attribute.Value == "MySql.Data.MySqlClient"))
                            {
                                elementsByTagName[0].RemoveChild(node);
                                break;
                            }
                        }
                        continue;
                    }
                }
                elementsByTagName[0].AppendChild(newChild);
                XmlTextWriter w = new XmlTextWriter(str, null);
                w.Formatting = Formatting.Indented;
                document.Save(w);
                w.Flush();
                w.Close();
            }
        }

        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);
            AddProviderToMachineConfig();
        }

        private static void RemoveProviderFromMachineConfig()
        {
            object obj2 = Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\.NETFramework\", "InstallRoot", null);
            if (obj2 == null)
            {
                throw new Exception("Unable to retrieve install root for .NET framework");
            }
            UpdateMachineConfigs(obj2.ToString(), false);
            string str = obj2.ToString();
            str = str.Substring(0, str.Length - 1);
            str = string.Format("{0}64{1}", str, Path.DirectorySeparatorChar);
            if (Directory.Exists(str))
            {
                UpdateMachineConfigs(str, false);
            }
        }

        private static void RemoveProviderFromMachineConfigInDir(string path)
        {
            string str = string.Format(@"{0}\machine.config", path);
            if (File.Exists(str))
            {
                StreamReader reader = new StreamReader(str);
                string xml = reader.ReadToEnd();
                reader.Close();
                XmlDocument document = new XmlDocument();
                document.LoadXml(xml);
                XmlNodeList elementsByTagName = document.GetElementsByTagName("DbProviderFactories");
                foreach (XmlNode node in elementsByTagName[0].ChildNodes)
                {
                    if ((node.Attributes != null) && (node.Attributes["name"].Value == "MySQL Data Provider"))
                    {
                        elementsByTagName[0].RemoveChild(node);
                        break;
                    }
                }
                XmlTextWriter w = new XmlTextWriter(str, null);
                w.Formatting = Formatting.Indented;
                document.Save(w);
                w.Flush();
                w.Close();
            }
        }

        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);
            RemoveProviderFromMachineConfig();
        }

        private static void UpdateMachineConfigs(string rootPath, bool add)
        {
            string[] strArray = new string[] { "v2.0.50727", "v4.0.30319" };
            foreach (string str in strArray)
            {
                string str2 = rootPath + str;
                string path = string.Format(@"{0}\CONFIG", str2);
                if (Directory.Exists(path))
                {
                    if (add)
                    {
                        AddProviderToMachineConfigInDir(path);
                    }
                    else
                    {
                        RemoveProviderFromMachineConfigInDir(path);
                    }
                }
            }
        }
    }
}

