using System.Reflection;
using System.Runtime.InteropServices;
using GeBoCommon;
using StudioCharaAnimFixPlugin;


// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("4a600a62-3caf-47f8-b1ec-be161313a5f5")]


// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion(StudioCharaAnimFix.Version)]
[assembly: AssemblyFileVersion(StudioCharaAnimFix.Version)]
[assembly: AssemblyDescription(StudioCharaAnimFix.PluginName + " for " + Constants.GameName)]
[assembly: AssemblyTitle(Constants.Prefix + "_" + nameof(StudioCharaAnimFix))]
[assembly: AssemblyProduct(Constants.Prefix + "_" + nameof(StudioCharaAnimFix))]
[assembly: AssemblyCompany(Constants.RepoUrl)]
