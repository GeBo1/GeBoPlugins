using System.Reflection;
using System.Runtime.InteropServices;
using static StudioSceneCharaInfoPlugin.StudioSceneCharaInfo;

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("4ad155c4-ca3e-4e34-8d6d-822cd860f27f")]

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
[assembly: AssemblyDescription(PluginName + " for " + GeBoCommon.Constants.GameName)]
[assembly: AssemblyTitle(GeBoCommon.Constants.Prefix + "_" + nameof(StudioSceneCharaInfoPlugin.StudioSceneCharaInfo))]
[assembly: AssemblyProduct(GeBoCommon.Constants.Prefix + "_" + nameof(StudioSceneCharaInfoPlugin.StudioSceneCharaInfo))]