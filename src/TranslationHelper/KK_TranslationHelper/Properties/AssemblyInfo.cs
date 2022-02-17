using System.Reflection;
using System.Runtime.InteropServices;
using GeBoCommon;
using TranslationHelperPlugin;
using static TranslationHelperPlugin.TranslationHelper;

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("6b60025d-20de-4df7-aeb2-be76490a7386")]

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
[assembly: AssemblyTitle(Constants.Prefix + "_" + nameof(TranslationHelper))]
[assembly: AssemblyProduct(Constants.Prefix + "_" + nameof(TranslationHelper))]
[assembly: AssemblyCompany(Constants.RepoUrl)]
