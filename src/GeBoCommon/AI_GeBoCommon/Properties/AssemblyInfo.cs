using System.Reflection;
using System.Runtime.InteropServices;
using static GeBoCommon.GeBoAPI;

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("622df8ad-567c-4036-8e13-636a5bbebb43")]

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
[assembly: AssemblyTitle(GeBoCommon.Constants.Prefix + "_" + PluginName)]
[assembly: AssemblyProduct(GeBoCommon.Constants.Prefix + "_" + PluginName)]
[assembly: AssemblyCompany("https://github.com/GeBo1/GeBoPlugins")]
