using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static GameDialogHelperPlugin.GameDialogHelper;

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d9c58c8c-1d67-455f-8251-8c9d8a4734be")]

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
[assembly: AssemblyDescription(PluginName + " for Koikatsu")]
[assembly: AssemblyTitle("KK_" + nameof(GameDialogHelperPlugin.GameDialogHelper))]
[assembly: AssemblyProduct("KK_" + nameof(GameDialogHelperPlugin.GameDialogHelper))]