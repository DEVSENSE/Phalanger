using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany("The Phalanger Project Team")]
[assembly: AssemblyProduct("Phalanger")]
[assembly: AssemblyCopyright("Copyright (c) 2004-2014 Tomas Matousek, Ladislav Prosek, Vaclav Novak, Pavel Novak, Jan Benda, Martin Maly, Tomas Petricek, Daniel Balas, Miloslav Beno, Jakub Misek")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: System.Resources.NeutralResourcesLanguage("en-US", System.Resources.UltimateResourceFallbackLocation.MainAssembly)]

[assembly: AssemblyVersion(AssemblyVersionInfo.StableVersion)]
[assembly: AssemblyFileVersion(AssemblyVersionInfo.FileVersion)]

class AssemblyVersionInfo
{
    // This version string (and the comments for StableVersion and Version)
    // should be updated manually between major releases.
    // Servicing branches should retain the value
    public const string ReleaseVersion = "4.0";

    // Replaced by changeset number using build script.
    public const string ChangesetNumber = "5598";

    public const string StableVersion = ReleaseVersion + ".0.0";

    public const string FileVersion = ReleaseVersion + ".0." + ChangesetNumber;
}
