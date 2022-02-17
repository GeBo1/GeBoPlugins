using System.Reflection;
using System.Runtime.InteropServices;
using static TranslationCacheCleanerPlugin.TranslationCacheCleaner;

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d67b9aef-16c0-405f-8a89-7ebed4249d9e")]
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
[assembly: AssemblyTitle(GeBoCommon.Constants.Prefix + "_" + nameof(TranslationCacheCleanerPlugin.TranslationCacheCleaner))]
[assembly: AssemblyProduct(GeBoCommon.Constants.Prefix + "_" + nameof(TranslationCacheCleanerPlugin.TranslationCacheCleaner))]
[assembly: AssemblyCompany(GeBoCommon.Constants.RepoUrl)]
