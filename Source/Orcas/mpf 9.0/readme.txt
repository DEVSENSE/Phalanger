Breaking changes in Visual Studio SDK 1.1
Posted by Vin on Oct-10-2008

There might be more, but these were the ones that came to light immediately.

    * ProjectBase.Files along with the parent “Project” folder does not belong to the Visual Studio SDK 1.1. It is now available as a separate download from Codeplex as MPF. You will need to update the base paths in your custom project files to point to the new location (wherever you place the MPF files) - The whole Project folder is no more part of the SDK, it has been moved to codeplex as a community project MPF

Visual Studio Managed Package Framework for Projects (MPFProj)

>>>>> http://www.codeplex.com/mpfproj/SourceControl/ListDownloadableCommits.aspx <<<<<

The ProjectBase.Files is the key file that needs to be added as an import to your VSX package project file
<Import Project=”$(ProjectBasePath)\ProjectBase.Files” />
ProjectBasePath is the location of the MPF Source

    * As a result of the above change, classes like ProjectFactory, ProjectNode, VsMenus are now in a different namespace called Microsoft.VisualStudio.Project.ProjectFactory (Old namespace was Microsoft.VisualStudio.Package)
