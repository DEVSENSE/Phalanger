/// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.IO;
using System.Reflection;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VSSDK.Tools.VsIdeTesting;

namespace Microsoft.VisualStudio.Project.IntegrationTests
{
	/// <summary>
	/// Tests global property support on MPF
	/// </summary>
	[TestClass]
	public class TestGlobalProperties
	{
		private delegate void ThreadInvoker();

		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
		{
			get
			{
				return testContextInstance;
			}
			set
			{
				testContextInstance = value;
			}
		}

		[TestCleanup()]
		public void MyTestCleanup()
		{
			IVsSolution solutionService = VsIdeTestHostContext.ServiceProvider.GetService(typeof(IVsSolution)) as IVsSolution;
			if(solutionService != null)
			{
				object isOpen;
				solutionService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out isOpen);
				if((bool)isOpen)
				{
					solutionService.CloseSolutionElement((uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_ForceSave, null, 0);
				}
			}
		}

		[TestMethod()]
		public void TestGlobalPropertyMatch()
		{
			UIThreadInvoker.Invoke((ThreadInvoker)delegate()
			{
				//Get the global service provider and the dte
				IServiceProvider sp = VsIdeTestHostContext.ServiceProvider;
				DTE dte = (DTE)sp.GetService(typeof(DTE));

				string destination = Path.Combine(TestContext.TestDir, TestContext.TestName);
				ProjectNode project = Utilities.CreateMyNestedProject(sp, dte, TestContext.TestName, destination, true);

				Microsoft.Build.BuildEngine.Project buildProject = typeof(ProjectNode).GetProperty("BuildProject", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(project, new object[] { }) as Microsoft.Build.BuildEngine.Project;
				IVsHierarchy nestedProject = Utilities.GetNestedHierarchy(project, "ANestedProject");

				IVsBuildPropertyStorage nestedProjectPropertyStorage = nestedProject as IVsBuildPropertyStorage;
				foreach(string property in Enum.GetNames(typeof(GlobalProperty)))
				{
					string nestedProjectGlobalProperty;

					// We will pass in the debug configuration since the GetPropertyValue will change the global property to whatever does not exist as configuration like empty or null.
					nestedProjectPropertyStorage.GetPropertyValue(property, "Debug", (uint)_PersistStorageType.PST_PROJECT_FILE, out nestedProjectGlobalProperty);

					string parentProjectGlobalProperty = buildProject.GlobalProperties[property].Value;

					bool result;
					bool isBoolean = (Boolean.TryParse(parentProjectGlobalProperty, out result) == true);
					Assert.IsTrue(String.Compare(nestedProjectGlobalProperty, parentProjectGlobalProperty, isBoolean ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) == 0, "The following global Properties do not match for Property: " + property + " Nested :" + nestedProjectGlobalProperty + " Parent:" + parentProjectGlobalProperty);
				}
			});
		}

		[TestMethod()]
		public void TestConfigChange()
		{
			UIThreadInvoker.Invoke((ThreadInvoker)delegate()
			{
				//Get the global service provider and the dte
				IServiceProvider sp = VsIdeTestHostContext.ServiceProvider;
				DTE dte = (DTE)sp.GetService(typeof(DTE));

				string destination = Path.Combine(TestContext.TestDir, TestContext.TestName);
				ProjectNode project = Utilities.CreateMyNestedProject(sp, dte, TestContext.TestName, destination, true);

				EnvDTE.Property property = dte.Solution.Properties.Item("ActiveConfig");

				// Now chnange the active config that should trigger a project config change event and the global property should be thus updated.
				property.Value = "Release|Any CPU";

				Microsoft.Build.BuildEngine.Project buildProject = typeof(ProjectNode).GetProperty("BuildProject", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(project, new object[] { }) as Microsoft.Build.BuildEngine.Project;

				string activeConfig = buildProject.GlobalProperties[GlobalProperty.Configuration.ToString()].Value;

				Assert.IsTrue(activeConfig == "Release");
			});
		}
	}
}
