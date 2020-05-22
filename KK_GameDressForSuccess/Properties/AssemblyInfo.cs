using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static GameDressForSuccessPlugin.GameDressForSuccess;



// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("7193dfae-7481-4708-bcbb-81d01ff9f413")]

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
[assembly: AssemblyTitle(GeBoCommon.Constants.Prefix + "_" + nameof(GameDressForSuccessPlugin.GameDressForSuccess))]
[assembly: AssemblyProduct(GeBoCommon.Constants.Prefix + "_" + nameof(GameDressForSuccessPlugin.GameDressForSuccess))]
