using System.Reflection;
using System.Runtime.InteropServices;
using GeBoCommon;
using StudioSceneInitialCameraPlugin;
using static StudioSceneInitialCameraPlugin.StudioSceneInitialCamera;

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("760d13d7-2729-4e5c-9746-549c32bf5c1f")]


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
[assembly: AssemblyVersion(Version)]
[assembly: AssemblyFileVersion(Version)]
[assembly: AssemblyDescription(PluginName + " for " + Constants.GameName)]
[assembly: AssemblyTitle(Constants.Prefix + "_" + nameof(StudioSceneInitialCamera))]
[assembly: AssemblyProduct(Constants.Prefix + "_" + nameof(StudioSceneInitialCamera))]
[assembly: AssemblyCompany(GeBoCommon.Constants.RepoUrl)]
