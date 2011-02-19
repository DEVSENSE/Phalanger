VS.NET Deployment project is a very limited tool for building .msi files,
so a little post-build hacking is necessary to finalize the packages.

IMPORTANT: The procedures below are now performed by VB scripts
Installer-postbuild.vbs and Installer.VS-postbuild.vbs executed
as postbuild actions during normal project build in VS, so it is
no longer needed to do this manually.


Manual post-build hacking:

You'll need the Orca tool.
http://msdn.microsoft.com/library/default.asp?url=/library/en-us/msi/setup/orca_exe.asp


1) Phalanger

To remove the funny start menu icons, follow this procedure:
- open Phalanger.msi with Orca
- go to Shortcut table
- change Icon to empty string for all rows
- save the .msi


2) VS.NET Integration

To make devenv /setup work properly during uninstall, do this:
- open VSIntegration.msi with Orca
- go to InstallExecuteSequence table
- locate the two GUID-like actions with ProductState <> 1 in their condition
  (this is the uninstall custom action)
- change their Sequence to something around the Sequence of the other two GUID-like actions,
  for example 5996 and 5997.
- save the .msi

To make the nested ProjectAggregator2.msi installation work, do this:
- execute MsiDb -d VSIntegration.msi -r ProjectAggregator2.msi
  (MsiDb and ProjectAggregator2.msi can be found in Tools and Deployment respectively)
- open VSIntegration.msi with Orca
- go to the CustomAction table
- add a row with Action='InstallAggregator', Type='7', Source='ProjectAggregator2.msi'
- go to the InstallExecuteSequence table
- add a row with Action='InstallAggregator', Condition='NOT REMOVE~="ALL"',
  Sequence='5994' (or any other suitable sequence number, but it quite makes sense to
  do this before executing devenv /setup)

To make the nested WebApplicationProjectSetup.msi installation work, do this:
- execute MsiDb -d VSIntegration.msi -r WebApplicationProjectSetup.msi
  (MsiDb and WebApplicationProjectSetup.msi can be found in Tools and Deployment respectively)
- open VSIntegration.msi with Orca
- go to the CustomAction table
- add a row with Action='InstallWebApplicationProject', Type='7', Source='WebApplicationProjectSetup.msi'
- go to the InstallExecuteSequence table
- add a row with Action='InstallWebApplicationProject', Condition='LEGACYWEB AND NOT REMOVE~="ALL"',
  Sequence='5995' (or any other suitable sequence number, but it quite makes sense to
  do this before executing devenv /setup)

Note: Nested MSI (aka concurrent install) is a deprecated technique and we know it.
