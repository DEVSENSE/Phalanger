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
using System.Security.AccessControl;

namespace MachineConfig
{
	/// <summary>
	/// 
	/// </summary>
	[RunInstaller(true)]
    public class Permissions : Installer
	{
        #region Helpers

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
        /// Set required permissions to the Phalanger install folder. To enable phalanger ASP.NET app i.e. to generate/modify dynamic wrappers.
        /// </summary>
        /// <param name="folder">Phalanger install folder.</param>
        private static void SetEveryonePermission(string folder)
        {
            var everyonesid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null);
            FileSystemAccessRule everyOne = new FileSystemAccessRule(everyonesid, FileSystemRights.FullControl | FileSystemRights.Write | FileSystemRights.Read, AccessControlType.Allow);
            DirectorySecurity dirSecurity = new DirectorySecurity(folder, AccessControlSections.Group);
            dirSecurity.AddAccessRule(everyOne);
            Directory.SetAccessControl(folder, dirSecurity);
        }

        #endregion

        #region Install, Commit

        /// <summary>
		/// 
		/// </summary>
		/// <param name="stateSaver">An <see cref="IDictionary"/> used to save information needed to perform a commit,
		/// rollback, or uninstall operation.</param>
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Install(IDictionary stateSaver)
		{
            base.Install(stateSaver);

            // check the installation directory
			string install_dir = Context.Parameters["InstallDir"];

			if (!Directory.Exists(install_dir))
			{
				ShowError("Directory '{0}' does not exist.", install_dir);
				return;
			}

            // install_dir must not end with \\
            if (install_dir.EndsWith("\\") || install_dir.EndsWith("/"))
            {
                install_dir = install_dir.Substring(0, install_dir.Length - 1);
            }

            // set the write/modify permission for the install_dir
            try
            {
                SetEveryonePermission(install_dir);
            }
            catch (Exception ex)
            {
                ShowError("Error while setting folder '{0}' permissions.\n{1}", install_dir, ex.Message);
            }
		}

		[System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);
            
            System.Diagnostics.Process.Start("http://www.php-compiler.net/Phalanger-Installed.html");
        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Rollback(IDictionary savedState)
        {
            base.Rollback(savedState);
        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);
        }

        #endregion
    }
}
