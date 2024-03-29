﻿using System.Reflection;
using System.Runtime.InteropServices;
using static TranslationHelperPlugin.TranslationHelper;

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("a412dd33-e3cf-4af0-9c17-9a918947b10b")]

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
[assembly: AssemblyTitle(GeBoCommon.Constants.Prefix + "_" + nameof(TranslationHelperPlugin.TranslationHelper))]
[assembly: AssemblyProduct(GeBoCommon.Constants.Prefix + "_" + nameof(TranslationHelperPlugin.TranslationHelper))]
[assembly: AssemblyCompany(GeBoCommon.Constants.RepoUrl)]
