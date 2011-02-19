/// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.IO;
using System.Reflection;
using EnvDTE;
using Microsoft.VisualStudio.Build.ComInteropWrapper;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VSSDK.Tools.VsIdeTesting;

namespace Microsoft.VisualStudio.Project.IntegrationTests
{
	/// <summary>
	/// Tests the projectsystem for security issue.
	/// </summary>
	[TestClass]
	public class TestProjectForSecurity
	{
		private const string msbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

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

		/// <summary>
		/// Test bad imports.
		/// </summary>
		[TestMethod()]
		public void TestBadImport()
		{
			UIThreadInvoker.Invoke((ThreadInvoker)delegate()
			{
				//Get the global service provider and the dte
				IServiceProvider sp = VsIdeTestHostContext.ServiceProvider;
				DTE dte = (DTE)sp.GetService(typeof(DTE));

				// Create temp files on disk for the main project file and the imported project files.
				string importedProjFilename3 = CreateTempFileOnDisk(@"

								<Project xmlns=`msbuildnamespace`>
									<PropertyGroup>
										<ReferencePath>c:\foobar</ReferencePath>
									</PropertyGroup>
								</Project>

							");

				string importedProjFilename2 = CreateTempFileOnDisk(@"
			                
								<Project xmlns=`msbuildnamespace`>
									<PropertyGroup>
										<ReferencePath>c:\foobar</ReferencePath>
									</PropertyGroup>
								</Project>

							");

				string importedProjFilename1 = CreateTempFileOnDisk(string.Format(@"
			                
								<Project xmlns=`msbuildnamespace`>
									<Import Project=`{0}`/>
									<PropertyGroup>
										<ReferencePath>c:\foobar</ReferencePath>
									</PropertyGroup>
								</Project>

							", importedProjFilename2));

				// Create temp files on disk for the main project file and the imported project files.
				string mainProjFilename = CreateTempFileOnDisk(string.Format(@"
			                
								<Project xmlns=`msbuildnamespace`>
									<Import Project=`{0}`/>
									<Import Project=`{1}`/>
								</Project>

							", importedProjFilename1, importedProjFilename3));

				try
				{
					ProjectSecurityChecker projectSecurityChecker = new ProjectSecurityChecker(sp, mainProjFilename);
					MethodInfo mi = projectSecurityChecker.GetType().GetMethod("IsProjectSafeWithImports", BindingFlags.Instance | BindingFlags.NonPublic);
					string[] message = new string[1] { String.Empty };
					bool result = (bool)mi.Invoke(projectSecurityChecker, message);

					Assert.IsTrue(!result && !String.IsNullOrEmpty(message[0]), "No message returned from a project with unsafe imports");
				}
				finally
				{
					File.Delete(mainProjFilename);
					File.Delete(importedProjFilename1);
					File.Delete(importedProjFilename2);
					File.Delete(importedProjFilename3);
				}

			});
		}

		/// <summary>
		/// Test dangereous properties
		/// </summary>
		[TestMethod()]
		public void TestDangereousProperties()
		{
			UIThreadInvoker.Invoke((ThreadInvoker)delegate()
			{
				//Get the global service provider and the dte
				IServiceProvider sp = VsIdeTestHostContext.ServiceProvider;
				DTE dte = (DTE)sp.GetService(typeof(DTE));

				string mainProjFilename = CreateTempFileOnDisk(@"
			                
								<Project DefaultTargets=`Build`  xmlns=`msbuildnamespace`>
								   <PropertyGroup>
									<BaseIntermediateOutputPath>obj\</BaseIntermediateOutputPath>
								   </PropertyGroup>
								   <Import Project=`$(MSBuildBinPath)\Microsoft.CSharp.targets`/>
							   </Project>

							");

				try
				{
					ProjectSecurityChecker projectSecurityChecker = new ProjectSecurityChecker(sp, mainProjFilename);
					MethodInfo mi = projectSecurityChecker.GetType().GetMethod("IsProjectSafeWithProperties", BindingFlags.Instance | BindingFlags.NonPublic);
					string[] message = new string[1] { String.Empty };
					bool result = (bool)mi.Invoke(projectSecurityChecker, message);

					Assert.IsTrue(!result && !String.IsNullOrEmpty(message[0]), "No message returned from a project with unsafe properties.");
				}
				finally
				{
					File.Delete(mainProjFilename);
				}
			});
		}

		/// <summary>
		/// Test dangereous targets
		/// </summary>
		[TestMethod()]
		public void TestDangereousTargets()
		{
			UIThreadInvoker.Invoke((ThreadInvoker)delegate()
			{
				//Get the global service provider and the dte
				IServiceProvider sp = VsIdeTestHostContext.ServiceProvider;
				DTE dte = (DTE)sp.GetService(typeof(DTE));

				string mainProjFilename = CreateTempFileOnDisk(@"
			                
								<Project DefaultTargets=`Build`  xmlns=`msbuildnamespace`>
								  <Import Project=`$(MSBuildBinPath)\Microsoft.CSharp.targets`/>
								  <Target
										Name=`PrepareForBuild`>
										<PropertyGroup>
											<TargetDir>$(TargetDir)</TargetDir>
											<TargetPath>$(TargetPath)</TargetPath>
										</PropertyGroup>
								 </Target>                       
							   </Project>

							");

				try
				{
					ProjectSecurityChecker projectSecurityChecker = new ProjectSecurityChecker(sp, mainProjFilename);
					MethodInfo mi = projectSecurityChecker.GetType().GetMethod("IsProjectSafeWithTargets", BindingFlags.Instance | BindingFlags.NonPublic);
					string[] message = new string[1] { String.Empty };
					bool result = (bool)mi.Invoke(projectSecurityChecker, message);

					Assert.IsTrue(!result && !String.IsNullOrEmpty(message[0]), "No message returned from a project with redefined safe targets.");
				}
				finally
				{
					File.Delete(mainProjFilename);
				}
			});
		}

		/// <summary>
		/// Test dangereous items
		/// </summary>
		[TestMethod()]
		public void TestDangereousItems()
		{
			UIThreadInvoker.Invoke((ThreadInvoker)delegate()
			{
				//Get the global service provider and the dte
				IServiceProvider sp = VsIdeTestHostContext.ServiceProvider;
				DTE dte = (DTE)sp.GetService(typeof(DTE));

				string mainProjFilename = CreateTempFileOnDisk(@"
			                
								<Project DefaultTargets=`Build`  xmlns=`msbuildnamespace`>
								  <ItemGroup>        
									<AppConfigFileDestination Include=`$(OutDir)$(TargetFileName).config`/>
								  </ItemGroup>          
								  <Import Project=`$(MSBuildBinPath)\Microsoft.CSharp.targets`/>
							   </Project>

							");

				try
				{
					ProjectSecurityChecker projectSecurityChecker = new ProjectSecurityChecker(sp, mainProjFilename);
					MethodInfo mi = projectSecurityChecker.GetType().GetMethod("IsProjectSafeWithItems", BindingFlags.Instance | BindingFlags.NonPublic);
					string[] message = new string[1] { String.Empty };
					bool result = (bool)mi.Invoke(projectSecurityChecker, message);

					Assert.IsTrue(!result && !String.IsNullOrEmpty(message[0]), "No message returned from a project with not safe items.");
				}
				finally
				{
					File.Delete(mainProjFilename);
				}
			});
		}

		/// <summary>
		/// Test dangereous using tasks
		/// </summary>
		[TestMethod()]
		public void TestDangereousUsingTasks()
		{
			UIThreadInvoker.Invoke((ThreadInvoker)delegate()
			{
				//Get the global service provider and the dte
				IServiceProvider sp = VsIdeTestHostContext.ServiceProvider;
				DTE dte = (DTE)sp.GetService(typeof(DTE));

				string mainProjFilename = CreateTempFileOnDisk(@"                
			                   
								<Project xmlns=`msbuildnamespace`>

									<UsingTask 
										TaskName=`Microsoft.Build.Tasks.FormatUrl` 
										AssemblyName=`Microsoft.Build.Tasks, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a`/>

									<UsingTask 
										TaskName=`Microsoft.Build.Tasks.FormatVersion` 
										AssemblyName=`Microsoft.Build.Tasks, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a`/>
									<Import Project=`$(MSBuildBinPath)\Microsoft.CSharp.targets`/> 
								</Project>
							");

				try
				{
					ProjectSecurityChecker projectSecurityChecker = new ProjectSecurityChecker(sp, mainProjFilename);
					MethodInfo mi = projectSecurityChecker.GetType().GetMethod("IsProjectSafeWithUsingTasks", BindingFlags.Instance | BindingFlags.NonPublic);
					string[] message = new string[1] { String.Empty };
					bool result = (bool)mi.Invoke(projectSecurityChecker, message);

					Assert.IsTrue(!result && !String.IsNullOrEmpty(message[0]), "No message returned from a project with not safe taks.");
				}
				finally
				{
					File.Delete(mainProjFilename);
				}
			});
		}


		/// <summary>
		/// Test multiple failures.
		/// </summary>
		[TestMethod()]
		public void TestMultipleFailures()
		{
			UIThreadInvoker.Invoke((ThreadInvoker)delegate()
			{
				//Get the global service provider and the dte
				IServiceProvider sp = VsIdeTestHostContext.ServiceProvider;
				DTE dte = (DTE)sp.GetService(typeof(DTE));

				string mainProjFilename = CreateTempFileOnDisk(@"
			                
								<Project DefaultTargets=`Build`  xmlns=`msbuildnamespace`>
									<PropertyGroup>
										<BaseIntermediateOutputPath>obj\</BaseIntermediateOutputPath>
									</PropertyGroup>
									<ItemGroup>        
										<AppConfigFileDestination Include=`$(OutDir)$(TargetFileName).config`/>
									</ItemGroup>   
									<Import Project=`$(MSBuildBinPath)\Microsoft.CSharp.targets`/>
							   </Project>

							");
				try
				{
					ProjectSecurityChecker projectSecurityChecker = new ProjectSecurityChecker(sp, mainProjFilename);
					string errorMessage;
					bool result = projectSecurityChecker.IsProjectSafeAtLoadTime(out errorMessage);

					Assert.IsFalse(result, "A project was considered safe containing redefined safe properties and safe items!");

					Assert.IsTrue(errorMessage.Contains("1:") && errorMessage.Contains("2:"), "The error string returning from a project with multiple failures should contain the listed failures");
				}
				finally
				{
					File.Delete(mainProjFilename);
				}
			});
		}

		/// <summary>
		/// Tests imports on the user project.
		/// </summary>
		[TestMethod()]
		public void TestImportOnUserProject()
		{
			UIThreadInvoker.Invoke((ThreadInvoker)delegate()
			{
				//Get the global service provider and the dte
				IServiceProvider sp = VsIdeTestHostContext.ServiceProvider;
				DTE dte = (DTE)sp.GetService(typeof(DTE));

				string importedProjFilename = CreateTempFileOnDisk(@"
			                
								<Project xmlns=`msbuildnamespace`>
									<PropertyGroup>
										<ReferencePath>c:\foobar</ReferencePath>
									</PropertyGroup>
								</Project>

							");

				string mainProject = CreateTempFileOnDisk(@"                
			                   
								 <Project DefaultTargets=`Build`  xmlns=`msbuildnamespace`>
									<Import Project=`$(MSBuildBinPath)\Microsoft.CSharp.targets`/>
							   </Project>
							");

				ProjectShim mainProjectShim = null;
				UserProjectSecurityChecker userProject = null;
				try
				{
					mainProjectShim = CreateProjectShim(mainProject);
					userProject = CreateUserProjectSecurityChecker(sp, String.Format(@"
			                
								<Project DefaultTargets=`Build`  xmlns=`msbuildnamespace`>                      
								  <Import Project=`{0}`/>
							   </Project>
							", importedProjFilename), mainProjectShim);

					MethodInfo mi = userProject.GetType().GetMethod("IsProjectSafeWithImports", BindingFlags.Instance | BindingFlags.NonPublic);
					string[] message = new string[1] { String.Empty };
					bool result = (bool)mi.Invoke(userProject, message);

					Assert.IsTrue(!result && !String.IsNullOrEmpty(message[0]), "A user project contained imports and was not considered unsafe.");
				}
				finally
				{
					TestProjectForSecurity.CleanUp(userProject, mainProjectShim);
					File.Delete(importedProjFilename);
				}
			});
		}

		/// <summary>
		/// Tests properties on the user project.
		/// </summary>
		[TestMethod()]
		public void TestPropertiesOnUserProject()
		{
			UIThreadInvoker.Invoke((ThreadInvoker)delegate()
			{
				//Get the global service provider and the dte
				IServiceProvider sp = VsIdeTestHostContext.ServiceProvider;
				DTE dte = (DTE)sp.GetService(typeof(DTE));

				string mainProject = CreateTempFileOnDisk(@"                
			                   
								 <Project DefaultTargets=`Build`  xmlns=`msbuildnamespace`>
									<Import Project=`$(MSBuildBinPath)\Microsoft.CSharp.targets`/>
							   </Project>
							");

				ProjectShim mainProjectShim = null;
				UserProjectSecurityChecker userProject = null;
				try
				{
					mainProjectShim = CreateProjectShim(mainProject);
					userProject = CreateUserProjectSecurityChecker(sp, @"
			                
								<Project DefaultTargets=`Build`  xmlns=`msbuildnamespace`>                      
								  <PropertyGroup>
									<BaseIntermediateOutputPath>obj\</BaseIntermediateOutputPath>
								   </PropertyGroup>
							   </Project>
							", mainProjectShim);

					MethodInfo mi = userProject.GetType().GetMethod("IsProjectSafeWithProperties", BindingFlags.Instance | BindingFlags.NonPublic);
					string[] message = new string[1] { String.Empty };
					bool result = (bool)mi.Invoke(userProject, message);

					Assert.IsTrue(!result && !String.IsNullOrEmpty(message[0]), "A user project contained redefined properties and was not considered unsafe.");
				}
				finally
				{
					TestProjectForSecurity.CleanUp(userProject, mainProjectShim);
				}
			});
		}


		/// <summary>
		/// Tests targets on the user project.
		/// </summary>
		[TestMethod()]
		public void TestTargetsOnUserProject()
		{
			UIThreadInvoker.Invoke((ThreadInvoker)delegate()
			{
				//Get the global service provider and the dte
				IServiceProvider sp = VsIdeTestHostContext.ServiceProvider;
				DTE dte = (DTE)sp.GetService(typeof(DTE));

				string mainProject = CreateTempFileOnDisk(@"                
			                   
								 <Project DefaultTargets=`Build`  xmlns=`msbuildnamespace`>
									<Import Project=`$(MSBuildBinPath)\Microsoft.CSharp.targets`/>
							   </Project>
							");

				ProjectShim mainProjectShim = null;
				UserProjectSecurityChecker userProject = null;
				try
				{
					mainProjectShim = CreateProjectShim(mainProject);
					userProject = CreateUserProjectSecurityChecker(sp, @"
			                
								<Project DefaultTargets=`Build`  xmlns=`msbuildnamespace`>                      
								 <Target
										Name=`PrepareForBuild`>
										<PropertyGroup>
											<TargetDir>$(TargetDir)</TargetDir>
											<TargetPath>$(TargetPath)</TargetPath>
										</PropertyGroup>
								 </Target>           
							   </Project>
							", mainProjectShim);

					MethodInfo mi = userProject.GetType().GetMethod("IsProjectSafeWithTargets", BindingFlags.Instance | BindingFlags.NonPublic);
					string[] message = new string[1] { String.Empty };
					bool result = (bool)mi.Invoke(userProject, message);

					Assert.IsTrue(!result && !String.IsNullOrEmpty(message[0]), "A user project redefined targets and was not considered unsafe.");
				}
				finally
				{
					TestProjectForSecurity.CleanUp(userProject, mainProjectShim);
				}
			});
		}

		/// <summary>
		/// Tests multiple failures on the user project.
		/// </summary>
		[TestMethod()]
		public void TestMultipleFailuresOnUserProject()
		{
			UIThreadInvoker.Invoke((ThreadInvoker)delegate()
			{
				//Get the global service provider and the dte
				IServiceProvider sp = VsIdeTestHostContext.ServiceProvider;
				DTE dte = (DTE)sp.GetService(typeof(DTE));

				string importedProjFilename = CreateTempFileOnDisk(@"
			                
								<Project xmlns=`msbuildnamespace`>
									<PropertyGroup>
										<ReferencePath>c:\foobar</ReferencePath>
									</PropertyGroup>
								</Project>

							");

				string mainProject = CreateTempFileOnDisk(@"                
			                   
								 <Project DefaultTargets=`Build`  xmlns=`msbuildnamespace`>
									<Import Project=`$(MSBuildBinPath)\Microsoft.CSharp.targets`/>
							   </Project>
							");

				ProjectShim mainProjectShim = null;
				UserProjectSecurityChecker userProject = null;
				try
				{
					mainProjectShim = CreateProjectShim(mainProject);
					userProject = CreateUserProjectSecurityChecker(sp, String.Format(@"                
								<Project DefaultTargets=`Build`  xmlns=`msbuildnamespace`>                      
									<ItemGroup>        
										<AppConfigFileDestination Include=`$(OutDir)$(TargetFileName).config`/>
								  </ItemGroup> 
								<Import Project=`{0}`/> 
								</Project>
							", importedProjFilename), mainProjectShim);

					string errorMessage;
					userProject.IsProjectSafeAtLoadTime(out  errorMessage);
					Assert.IsTrue(errorMessage.Contains("1:") && errorMessage.Contains("2:"), "The error string returning from a project with multiple failures should contain the listed failures");
				}
				finally
				{
					TestProjectForSecurity.CleanUp(userProject, mainProjectShim);
					File.Delete(importedProjFilename);
				}
			});
		}

		/// <summary>
		/// Create an MSBuild project file on disk and return the full path to it.
		/// </summary>
		internal static string CreateTempFileOnDisk(string fileContents)
		{
			string projectFilePath = Path.GetTempFileName();

			File.WriteAllText(projectFilePath, CleanupProjectXml(fileContents));

			return projectFilePath;
		}

		/// <summary>
		/// Create a user security checker object that has its main project setup.
		/// </summary>
		internal static UserProjectSecurityChecker CreateUserProjectSecurityChecker(IServiceProvider sp, string fileContents, ProjectShim mainProjectShim)
		{
			string fileName = mainProjectShim.FullFileName + ".user";
			File.WriteAllText(fileName, CleanupProjectXml(fileContents));
			UserProjectSecurityChecker userProjectSecurityChecker = new UserProjectSecurityChecker(sp, fileName);

			PropertyInfo pi = userProjectSecurityChecker.GetType().GetProperty("MainProjectShim", BindingFlags.Instance | BindingFlags.NonPublic);
			pi.SetValue(userProjectSecurityChecker, mainProjectShim, new object[] { });
			return userProjectSecurityChecker;
		}

		/// <summary>
		/// Does certain replacements in a string representing the project file contents.
		/// This makes it easier to write unit tests because the author doesn't have
		/// to worry about escaping double-quotes, etc.
		/// </summary>
		internal static string CleanupProjectXml(string projectFileContents)
		{
			// Replace reverse-single-quotes with double-quotes.
			projectFileContents = projectFileContents.Replace("`", "\"");

			// Place the correct MSBuild namespace into the <Project> tag.
			projectFileContents = projectFileContents.Replace("msbuildnamespace", msbuildNamespace);

			return projectFileContents;
		}

		/// <summary>
		/// Creates a project shim from a project file.
		/// </summary>
		/// <param name="file">The path to the project file.</param>
		/// <returns>The project shim</returns>
		private static ProjectShim CreateProjectShim(string file)
		{
			EngineShim engine = new EngineShim();
			ProjectShim projectShim = engine.CreateNewProject();
			projectShim.Load(file);

			return projectShim;
		}

		/// <summary>
		/// Cleanups after a user project.
		/// </summary>
		private static void CleanUp(UserProjectSecurityChecker userProject, ProjectShim mainProjectShim)
		{
			String mainProject = String.Empty;
			if(userProject != null)
			{
				userProject.Dispose();
				userProject = null;
			}

			if(mainProjectShim != null)
			{
				mainProject = mainProjectShim.FullFileName;
				mainProjectShim.ParentEngine.UnloadProject(mainProjectShim);
				mainProjectShim = null;
			}

			if(!String.IsNullOrEmpty(mainProject))
			{
				try
				{
					File.Delete(mainProject);
					File.Delete(mainProject + ".user");
				}
				catch(IOException)
				{
				}
			}
		}

	}
}
