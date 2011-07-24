using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.DirectoryServices;
using System.Security;
using System.Security.AccessControl;
using Microsoft.Win32;
using Microsoft.Web.Administration;

namespace Installer.IISSamples
{
    public static class IIS
    {
        #region User Account Management
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static bool CreateLocalUserAccount(string userName, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(userName))
                    throw new ArgumentNullException("userName", "Invalid User Name.");
                if (string.IsNullOrEmpty(password))
                    throw new ArgumentNullException("password", "Invalid Password.");

                DirectoryEntry directoryEntry = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
                bool userFound = false;
                try
                {
                    if (directoryEntry.Children.Find(userName, "user") != null)
                        userFound = true;
                }
                catch
                {
                    userFound = false;
                }
                if (!userFound)
                {
                    DirectoryEntry newUser = directoryEntry.Children.Add(userName, "user");
                    newUser.Invoke("SetPassword", new object[] { password });
                    newUser.Invoke("Put", new object[] { "Description", "Application Pool User Account" });
                    newUser.CommitChanges();
                    newUser.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public static bool RemoveLocalUserAccount(string userName)
        {
            try
            {
                if (string.IsNullOrEmpty(userName))
                    throw new ArgumentNullException("userName", "Invalid User Name.");

                DirectoryEntry directoryEntry = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
                bool userFound = false;
                try
                {
                    if (directoryEntry.Children.Find(userName, "user") != null)
                        userFound = true;
                }
                catch
                {
                    userFound = false;
                }
                if (userFound)
                {
                    directoryEntry.Children.Remove(directoryEntry.Children.Find(userName, "user"));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            return true;
        }

        #endregion

        #region Content Storage Management
        /// <summary>
        /// 
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <param name="userAccount"></param>
        /// <param name="rights"></param>
        /// <param name="inheritanceFlags"></param>
        /// <param name="propagationFlags"></param>
        /// <param name="controlType"></param>
        /// <returns></returns>
        public static bool AddDirectorySecurity(string directoryPath, string userAccount, FileSystemRights rights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType controlType)
        {
            try
            {
                // Create a new DirectoryInfo object.
                DirectoryInfo dInfo = new DirectoryInfo(directoryPath);

                // Get a DirectorySecurity object that represents the 
                // current security settings.
                DirectorySecurity dSecurity = dInfo.GetAccessControl();

                // Add the FileSystemAccessRule to the security settings. 
                dSecurity.AddAccessRule(new FileSystemAccessRule(userAccount,
                                                                rights, inheritanceFlags, propagationFlags,
                                                                controlType));

                // Set the new access settings.
                dInfo.SetAccessControl(dSecurity);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            return true;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <param name="userAccount"></param>
        /// <param name="rights"></param>
        /// <param name="controlType"></param>
        /// <returns></returns>
        public static bool RemoveDirectorySecurity(string directoryPath, string userAccount, FileSystemRights rights, AccessControlType controlType)
        {
            try
            {
                // Create a new DirectoryInfo object.
                DirectoryInfo dInfo = new DirectoryInfo(directoryPath);

                // Get a DirectorySecurity object that represents the 
                // current security settings.
                DirectorySecurity dSecurity = dInfo.GetAccessControl();

                // Add the FileSystemAccessRule to the security settings. 
                dSecurity.RemoveAccessRule(new FileSystemAccessRule(userAccount,
                                                                rights,
                                                                controlType));

                // Set the new access settings.
                dInfo.SetAccessControl(dSecurity);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            return true;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userAccount"></param>
        /// <param name="rights"></param>
        /// <param name="controlType"></param>
        /// <returns></returns>
        public static bool AddFileSecurity(string filePath, string userAccount, FileSystemRights rights, AccessControlType controlType)
        {
            try
            {
                // Get a FileSecurity object that represents the 
                // current security settings.
                FileSecurity fSecurity = File.GetAccessControl(filePath);

                // Add the FileSystemAccessRule to the security settings. 
                fSecurity.AddAccessRule(new FileSystemAccessRule(userAccount,
                    rights, controlType));

                // Set the new access settings.
                File.SetAccessControl(filePath, fSecurity);


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userAccount"></param>
        /// <param name="rights"></param>
        /// <param name="controlType"></param>
        /// <returns></returns>
        public static bool RemoveFileSecurity(string filePath, string userAccount, FileSystemRights rights, AccessControlType controlType)
        {
            try
            {
                // Get a FileSecurity object that represents the 
                // current security settings.
                FileSecurity fSecurity = File.GetAccessControl(filePath);

                // Add the FileSystemAccessRule to the security settings. 
                fSecurity.RemoveAccessRule(new FileSystemAccessRule(userAccount,
                    rights, controlType));

                // Set the new access settings.
                File.SetAccessControl(filePath, fSecurity);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            return true;
        }

        #endregion

        #region Web Site Management
        /// <summary>
        /// 
        /// </summary>
        /// <param name="siteName"></param>
        /// <returns></returns>
        public static bool CreateWebSite(string siteName)
        {
            try
            {
                if (string.IsNullOrEmpty(siteName))
                {
                    throw new ArgumentNullException("siteName", "CreateWebSite: siteName is null or empty.");
                }
                //get the server manager instance
                using (ServerManager mgr = new ServerManager())
                {

                    Site newSite = mgr.Sites.CreateElement();
                    //get site id
                    newSite.Id = GenerateNewSiteID(mgr, siteName);
                    newSite.SetAttributeValue("name", siteName);
                    mgr.Sites.Add(newSite);
                    mgr.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationPoolName"></param>
        /// <param name="identityType"></param>
        /// <param name="applicationPoolIdentity"></param>
        /// <param name="password"></param>
        /// <param name="managedRuntimeVersion"></param>
        /// <param name="autoStart"></param>
        /// <param name="enable32BitAppOnWin64"></param>
        /// <param name="managedPipelineMode"></param>
        /// <param name="queueLength"></param>
        /// <param name="idleTimeout"></param>
        /// <param name="periodicRestartPrivateMemory"></param>
        /// <param name="periodicRestartTime"></param>
        /// <returns></returns>
        public static bool CreateApplicationPool(string applicationPoolName, ProcessModelIdentityType identityType, string applicationPoolIdentity, string password,
            string managedRuntimeVersion, bool autoStart, bool enable32BitAppOnWin64, ManagedPipelineMode managedPipelineMode, long queueLength, TimeSpan idleTimeout,
            long periodicRestartPrivateMemory, TimeSpan periodicRestartTime)
        {
            try
            {
                if (identityType == ProcessModelIdentityType.SpecificUser)
                {
                    if (string.IsNullOrEmpty(applicationPoolName))
                        throw new ArgumentNullException("applicationPoolName", "CreateApplicationPool: applicationPoolName is null or empty.");
                    if (string.IsNullOrEmpty(applicationPoolIdentity))
                        throw new ArgumentNullException("applicationPoolIdentity", "CreateApplicationPool: applicationPoolIdentity is null or empty.");
                    if (string.IsNullOrEmpty(password))
                        throw new ArgumentNullException("password", "CreateApplicationPool: password is null or empty.");
                }
                using (ServerManager mgr = new ServerManager())
                {
                    if (mgr.ApplicationPools[applicationPoolName] != null)
                        return true;   // already exists

                    ApplicationPool newAppPool = mgr.ApplicationPools.Add(applicationPoolName);
                    if (identityType == ProcessModelIdentityType.SpecificUser)
                    {
                        newAppPool.ProcessModel.IdentityType = ProcessModelIdentityType.SpecificUser;
                        newAppPool.ProcessModel.UserName = applicationPoolIdentity;
                        newAppPool.ProcessModel.Password = password;
                    }
                    else
                    {
                        newAppPool.ProcessModel.IdentityType = identityType;
                    }
                    if (!string.IsNullOrEmpty(managedRuntimeVersion))
                        newAppPool.ManagedRuntimeVersion = managedRuntimeVersion;
                    newAppPool.AutoStart = autoStart;
                    newAppPool.Enable32BitAppOnWin64 = enable32BitAppOnWin64;
                    newAppPool.ManagedPipelineMode = managedPipelineMode;
                    if (queueLength > 0)
                        newAppPool.QueueLength = queueLength;
                    if (idleTimeout != TimeSpan.MinValue)
                        newAppPool.ProcessModel.IdleTimeout = idleTimeout;
                    if (periodicRestartPrivateMemory > 0)
                        newAppPool.Recycling.PeriodicRestart.PrivateMemory = periodicRestartPrivateMemory;
                    if (periodicRestartTime != TimeSpan.MinValue)
                        newAppPool.Recycling.PeriodicRestart.Time = periodicRestartTime;
                    mgr.CommitChanges();
                }

            }
            catch// (Exception ex)
            {
                throw;
            }
            return true;
        }

        public static void RemoveApplicationPool(string applicationPoolName)
        {
            using (ServerManager mgr = new ServerManager())
            {
                var pool = mgr.ApplicationPools[applicationPoolName];

                if (pool != null)
                {
                    pool.Stop();
                    
                    mgr.ApplicationPools.Remove(pool);
                    mgr.CommitChanges();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="applicationPath"></param>
        /// <param name="applicationPool"></param>
        /// <param name="virtualDirectoryPath"></param>
        /// <param name="physicalPath"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static bool AddApplication(string siteName, string applicationPath, string applicationPool, string physicalPath, string userName, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(siteName))
                    throw new ArgumentNullException("siteName", "AddApplication: siteName is null or empty.");
                if (string.IsNullOrEmpty(applicationPath))
                    throw new ArgumentNullException("applicationPath", "AddApplication: application path is null or empty.");
                if (string.IsNullOrEmpty(physicalPath))
                    throw new ArgumentNullException("PhysicalPath", "AddApplication: Invalid physical path.");
                if (string.IsNullOrEmpty(applicationPool))
                    throw new ArgumentNullException("ApplicationPool", "AddApplication: application pool namespace is Nullable or empty.");
                using (ServerManager mgr = new ServerManager())
                {
                    ApplicationPool appPool = mgr.ApplicationPools[applicationPool];
                    if (appPool == null)
                        throw new Exception("Application Pool: " + applicationPool + " does not exist.");

                    Site site = mgr.Sites[siteName];
                    if (site != null)
                    {
                        Application app = site.Applications[applicationPath];
                        if (app != null)
                            throw new Exception("Application: " + applicationPath + " already exists.");
                        else
                        {
                            app = site.Applications.Add(applicationPath, physicalPath);
                            app.ApplicationPoolName = applicationPool;

                            //VirtualDirectory vDir = app.VirtualDirectories.CreateElement();
                            //vDir.Path = virtualDirectoryPath;
                            //vDir.PhysicalPath = physicalPath;
                            //if (!string.IsNullOrEmpty(userName))
                            //{
                            //    if (string.IsNullOrEmpty(password))
                            //        throw new Exception("Invalid Virtual Directory User Account Password.");
                            //    else
                            //    {
                            //        vDir.UserName = userName;
                            //        vDir.Password = password;
                            //    }
                            //}
                            //app.VirtualDirectories.Add(vDir);
                        }
                        //site.Applications.Add(app);
                        mgr.CommitChanges();
                        return true;
                    }
                    else
                        throw new Exception("Site: " + siteName + " does not exist.");
                }
            }
            catch// (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Remove all the IIS applications matching given path.
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="physicalPathRoot"></param>
        /// <returns></returns>
        public static bool RemoveApplication(string siteName, string path)
        {
            using (ServerManager mgr = new ServerManager())
            {
                Site site = mgr.Sites[siteName];
                if (site != null)
                {
                    List<Application> appsToRemove = new List<Application>();

                    foreach (var app in site.Applications)
                    {
                        if (app.Path.StartsWith(path))
                        {
                            appsToRemove.Add(app);
                        }
                    }

                    if (appsToRemove.Count > 0)
                    {
                        foreach (var app in appsToRemove)
                            site.Applications.Remove(app);

                        mgr.CommitChanges();

                        return true;
                    }
                }
                
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="application"></param>
        /// <param name="virtualDirectory"></param>
        /// <returns></returns>
        public static bool AddVirtualDirectory(string siteName, string application, string virtualDirectoryPath, string physicalPath, string userName, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(siteName))
                    throw new ArgumentNullException("siteName", "AddVirtualDirectory: siteName is null or empty.");
                if (string.IsNullOrEmpty(application))
                    throw new ArgumentNullException("application", "AddVirtualDirectory: application is null or empty.");
                if (string.IsNullOrEmpty(virtualDirectoryPath))
                    throw new ArgumentNullException("virtualDirectoryPath", "AddVirtualDirectory: virtualDirectoryPath is null or empty.");
                if (string.IsNullOrEmpty(physicalPath))
                    throw new ArgumentNullException("physicalPath", "AddVirtualDirectory: physicalPath is null or empty.");

                using (ServerManager mgr = new ServerManager())
                {
                    Site site = mgr.Sites[siteName];
                    if (site != null)
                    {
                        Application app = site.Applications[application];
                        if (app != null)
                        {
                            VirtualDirectory vDir = app.VirtualDirectories[virtualDirectoryPath];
                            if (vDir != null)
                            {
                                throw new Exception("Virtual Directory: " + virtualDirectoryPath + " already exists.");
                            }
                            else
                            {
                                vDir = app.VirtualDirectories.CreateElement();
                                vDir.Path = virtualDirectoryPath;
                                vDir.PhysicalPath = physicalPath;
                                if (!string.IsNullOrEmpty(userName))
                                {
                                    if (string.IsNullOrEmpty(password))
                                        throw new Exception("Invalid Virtual Directory User Account Password.");
                                    else
                                    {
                                        vDir.UserName = userName;
                                        vDir.Password = password;
                                    }
                                }
                                app.VirtualDirectories.Add(vDir);
                            }
                            mgr.CommitChanges();
                            return true;
                        }
                        else
                            throw new Exception("Application: " + application + " does not exist.");
                    }
                    else
                        throw new Exception("Site: " + siteName + " does not exist.");
                }
            }
            catch// (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="binding"></param>
        /// <returns></returns>
        public static bool AddSiteBinding(string siteName, string ipAddress, string tcpPort, string hostHeader, string protocol)
        {
            try
            {
                if (string.IsNullOrEmpty(siteName))
                {
                    throw new ArgumentNullException("siteName", "AddSiteBinding: siteName is null or empty.");
                }
                //get the server manager instance
                using (ServerManager mgr = new ServerManager())
                {
                    SiteCollection sites = mgr.Sites;
                    Site site = mgr.Sites[siteName];
                    if (site != null)
                    {
                        string bind = ipAddress + ":" + tcpPort + ":" + hostHeader;
                        //check the binding exists or not
                        foreach (Binding b in site.Bindings)
                        {
                            if (b.Protocol == protocol && b.BindingInformation == bind)
                            {
                                throw new Exception("A binding with the same ip, port and host header already exists.");
                            }
                        }
                        Binding newBinding = site.Bindings.CreateElement();
                        newBinding.Protocol = protocol;
                        newBinding.BindingInformation = bind;

                        site.Bindings.Add(newBinding);
                        mgr.CommitChanges();
                        return true;
                    }
                    else
                        throw new Exception("Site: " + siteName + " does not exist.");
                }
            }
            catch// (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="siteName"></param>
        /// <returns></returns>
        public static int GenerateNewSiteID(ServerManager manager, string siteName)
        {
            if (IsIncrementalSiteIDCreationSet())
            {
                return GenerateNewSiteIDIncremental(manager);
            }
            return GenerateNewSiteIDFromName(manager, siteName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="siteName"></param>
        /// <returns></returns>
        public static int GenerateNewSiteIDFromName(ServerManager manager, string siteName)
        {
            int siteID = Math.Abs(siteName.GetHashCode());
            while (ExistsSiteId(manager, siteID))
            {
                siteID++;
            }
            return siteID;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="siteID"></param>
        /// <returns></returns>
        public static bool ExistsSiteId(ServerManager manager, int siteID)
        {
            foreach (Site site in manager.Sites)
            {
                if (siteID == site.Id)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static int GenerateNewSiteIDIncremental(ServerManager manager)
        {
            int num = manager.Sites.Count + 1;
            long[] array = new long[num];
            int length = 1;
            for (int i = 1; i < num; i++)
            {
                long id = manager.Sites[i - 1].Id;
                if (id != 0)
                {
                    array[length++] = id;
                }
            }
            Array.Sort<long>(array, 0, length);
            for (int j = 1; j < num; j++)
            {
                if (array[j] != j)
                {
                    return j;
                }
            }
            return num;
        }

        public static bool IsIncrementalSiteIDCreationSet()
        {
            RegistryKey key = null;
            try
            {
                key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\InetMgr\Parameters", false);
                if (key == null)
                {
                    return false;
                }
                int num = (int)key.GetValue("IncrementalSiteIDCreation", 0);
                if (num == 1)
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                if (key != null)
                {
                    key.Close();
                    key = null;
                }
            }
            return false;
        }
        #endregion

        #region FTP Management
        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationPoolName"></param>
        /// <param name="siteName"></param>
        /// <param name="domainName"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="contentPath"></param>
        /// <param name="ipAddress"></param>
        /// <param name="tcpPort"></param>
        /// <param name="hostHeader"></param>
        /// <returns></returns>
        public static bool CreateFtpSite(string applicationPoolName, string siteName, string domainName, string userName, string password, string contentPath, string ipAddress, string tcpPort, string hostHeader)
        {
            try
            {
                //provision the application pool
                using (ServerManager mgr = new ServerManager())
                {
                    ApplicationPool appPool = mgr.ApplicationPools[applicationPoolName];

                    //per IIS7 team recommendation, we always create a new application pool
                    //create new application pool
                    if (appPool == null)
                    {
                        appPool = mgr.ApplicationPools.Add(applicationPoolName);

                        //set the application pool attribute
                        appPool.ProcessModel.IdentityType = ProcessModelIdentityType.SpecificUser;
                        appPool.ProcessModel.UserName = domainName + "\\" + userName;
                        appPool.ProcessModel.Password = password;
                    }

                    //if the appPool is null, we throw an exception. The appPool should be created or already exists.
                    if (appPool == null)
                        throw new Exception("Invalid Application Pool.");

                    //if the site already exists, throw an exception
                    if (mgr.Sites[siteName] != null)
                        throw new Exception("Site already exists.");

                    //create site
                    Site newSite = mgr.Sites.CreateElement();
                    newSite.Id = GenerateNewSiteID(mgr, siteName);
                    newSite.SetAttributeValue("name", siteName);
                    newSite.ServerAutoStart = true;
                    mgr.Sites.Add(newSite);

                    //create the default application for the site
                    Application newApp = newSite.Applications.CreateElement();
                    newApp.SetAttributeValue("path", "/"); //set to default root path
                    newApp.SetAttributeValue("applicationPool", applicationPoolName);
                    newSite.Applications.Add(newApp);

                    //create the default virtual directory
                    VirtualDirectory newVirtualDirectory = newApp.VirtualDirectories.CreateElement();
                    newVirtualDirectory.SetAttributeValue("path", "/");
                    newVirtualDirectory.SetAttributeValue("physicalPath", contentPath);
                    newApp.VirtualDirectories.Add(newVirtualDirectory);

                    //add the bindings 
                    Binding binding = newSite.Bindings.CreateElement();
                    binding.SetAttributeValue("protocol", "ftp");
                    binding.SetAttributeValue("bindingInformation", ipAddress + ":" + tcpPort + ":" + hostHeader);
                    newSite.Bindings.Add(binding);


                    //commit the changes
                    mgr.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            return true;
        }
        #endregion

    }
}
