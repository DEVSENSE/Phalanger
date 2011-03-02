using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Windows.Forms;

namespace Installer.IISSamples
{
    [RunInstaller(true)]
    public partial class IISSamples : System.Configuration.Install.Installer
    {
        public IISSamples()
        {
            InitializeComponent();
        }

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

        #endregion

        /// <summary>
        /// Phalanger installation directory provided by the main installer. Does not end with '\' or '/'.
        /// </summary>
        private string InstallDir { get { return _installDir ?? (_installDir = Context.Parameters["InstallDir"].TrimEnd('/', '\\')); } }
        private string _installDir = null;

        /// <summary>
        /// AppPool name configured to be used on phalanger. Typically allows 32-bit, using integrated pipeline and .NET 4.0.
        /// </summary>
        public const string PhalangerAppPool = "PhalangerAppPool";

        /// <summary>
        /// AppPool .NET runtime version string.
        /// </summary>
        public const string ManagedRuntimeVersion = "v4.0";

        /// <summary>
        /// Default web site. If not found, Web Site on index 0 should be used.
        /// </summary>
        public const string DefaultWebSite = "Default Web Site";

        /// <summary>
        /// Absolute path to single web samples. Does not end with '\'.
        /// </summary>
        public string WebSamplesDir { get { return InstallDir + @"\WebRoot\Samples"; } }

        /// <summary>
        /// Samples subdirectories. 
        /// </summary>
        public string[]/*!*/Samples { get { return new string[] {"ASP.NET", "SimpleScripts", "Tests"}; } }

        public string ApplicationDir(string subname) { return string.Format("/PhalangerSample_{0}", subname); }

        #region Install, Uninstall

        /// <summary>
        /// Install the IIS samples.
        /// </summary>
        /// <param name="stateSaver"></param>
        /// <remarks>
        /// 1. check for Default Web Site
        /// 2. create AppPool named "PhalangerAppPool" (if not yet)
        /// 3. create application "Default Web Site"/"Phalanger..." using AppPool "PhalangerAppPool"
        /// </remarks>
        public override void Install(IDictionary stateSaver)
        {
            try
            {
                IIS.CreateApplicationPool(
                    PhalangerAppPool,
                    Microsoft.Web.Administration.ProcessModelIdentityType.ApplicationPoolIdentity, null, null,  // IIS defaults
                    ManagedRuntimeVersion, true,
                    true, // enable 32bit 
                    Microsoft.Web.Administration.ManagedPipelineMode.Integrated,
                    1000, new TimeSpan(0, 20, 0), 0, new TimeSpan(0, 0, 0)  // IIS defaults
                    );
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                ShowError("IIS 7 is not installed properly. The installation cannot continue.");
                throw;
            }
            catch (Exception ex)
            {
                ShowError("Unable to create '{0}' IIS application pool\n\n{1}\n{2}", PhalangerAppPool, ex.Message, ex.GetType());
            }

            foreach (var sample in Samples)
            {
                try
                {
                    IIS.AddApplication(DefaultWebSite, ApplicationDir(sample), PhalangerAppPool, WebSamplesDir + '\\' + sample, null, null);
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    ShowError("IIS 7 is not installed properly. The installation cannot continue.");
                    throw;
                }
                catch (Exception ex)
                {
                    ShowError("Unable to create '{0}' IIS application in '{1}'\n\n{2}", sample, DefaultWebSite, ex.Message);
                }
            }
        }

        public override void Uninstall(IDictionary savedState)
        {
            foreach (var sample in Samples)
            {
                try
                {
                    IIS.RemoveApplication(DefaultWebSite, ApplicationDir(sample));
                }
                catch (Exception ex)
                {
                    ShowError("Error while removing '{0}' IIS applications from '{1}'\n\n{2}", sample, DefaultWebSite, ex.Message);
                }
            }

            try
            {
                IIS.RemoveApplicationPool(PhalangerAppPool);
            }
            catch (Exception ex)
            {
                ShowError("Error while removing '{0}' IIS application pool\n\n{1}", PhalangerAppPool, ex.Message);
            }
        }

        #endregion
    }
}
