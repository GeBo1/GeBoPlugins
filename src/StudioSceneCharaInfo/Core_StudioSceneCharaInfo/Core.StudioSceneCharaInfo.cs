using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.Studio;
using GeBoCommon.Utilities;
using HarmonyLib;
using Studio;
using TranslationHelperPlugin;
using UnityEngine;

namespace StudioSceneCharaInfoPlugin
{
    [BepInDependency(TranslationHelper.GUID)]
    [BepInDependency(GeBoAPI.GUID, GeBoAPI.Version)]
    [BepInDependency(HSPEGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(Constants.StudioProcessName)]
    public partial class StudioSceneCharaInfo : BaseUnityPlugin
    {
        public const string GUID = "com.gebo.BepInEx.studioscenecharainfo";
        public const string PluginName = "Studio Scene Chara Info";
        public const string Version = "0.2.0";

        private const char DQ = '"';

        private static Action ResetHSPEWrapper;

        private static SceneLoadScene _studioInitObject;

        internal static new ManualLogSource Logger;

        private static bool dumping;

        private readonly HashSet<string> ProcessedScenes = new HashSet<string>();

        public StudioSceneCharaInfo()
        {
            ResetHSPEWrapper = LazyResetHSPE;
        }

        public SceneLoadScene StudioInitObject => _studioInitObject;

        public static ConfigEntry<KeyboardShortcut> SceneCharaInfoDumpHotkey { get; private set; }

        public List<string> GetListPath()
        {
            return SceneUtils.GetSceneLoaderPaths(StudioInitObject);
        }

        internal void Awake()
        {
            Logger = base.Logger;

            SceneCharaInfoDumpHotkey = Config.Bind("Keyboard Shortcuts", "Dump Chara Info Hotkey",
                new KeyboardShortcut(KeyCode.Tab, KeyCode.LeftControl),
                "Pressing this will dump a spreadsheet containing info about characters in scenes.");
        }

        internal void Start()
        {
            HarmonyWrapper.PatchAll(typeof(StudioSceneCharaInfo));
        }

        internal static void HSPE_Prefix_Patch()
        {
            if (dumping)
            {
                ResetHSPE();
            }
        }

        [HarmonyPatch(typeof(SceneLoadScene), "InitInfo")]
        [HarmonyPostfix]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "HarmonyPatch")]
        private static void StudioInitInfoPost(SceneLoadScene __instance)
        {
            _studioInitObject = __instance;
        }

        internal void Update()
        {
            if (StudioInitObject != null && !dumping && SceneCharaInfoDumpHotkey.Value.IsDown())
            {
                dumping = true;
                try
                {
                    Logger.LogInfo("Start dump.");
                    ExecuteDump();
                }
                finally
                {
                    dumping = false;
                    Singleton<Studio.Studio>.Instance.colorPalette.visible = true;
                }
            }
        }

        private void CollectCharInfos(ObjectInfo oICharInfo, ref List<ObjectInfo> charInfos)
        {
            var children = new List<ObjectInfo>();
            if (oICharInfo is OICharInfo charInfo)
            {
                charInfos.Add(oICharInfo);
                foreach (var kids in charInfo.child.Values)
                {
                    children.AddRange(kids);
                }
            }
            else if (oICharInfo is OIItemInfo itemInfo)
            {
                children.AddRange(itemInfo.child);
            }
            else if (oICharInfo is OIFolderInfo folderInfo)
            {
                children.AddRange(folderInfo.child);
            }
            else if (oICharInfo is OIRouteInfo routeInfo)
            {
                children.AddRange(routeInfo.child);
            }

            foreach (var child in children)
            {
                CollectCharInfos(child, ref charInfos);
                child.DeleteKey();
            }
        }

        private void CollectNames(ObjectInfo oICharInfo, ref List<string> names)
        {
            var children = new List<ObjectInfo>();
            if (oICharInfo is OICharInfo charInfo)
            {
                var info = charInfo.charFile.parameter;

                names.Add(info.fullname);

                foreach (var kids in charInfo.child.Values)
                {
                    children.AddRange(kids);
                }
            }
            else if (oICharInfo is OIItemInfo itemInfo)
            {
                children.AddRange(itemInfo.child);
            }
            else if (oICharInfo is OIFolderInfo folderInfo)
            {
                children.AddRange(folderInfo.child);
            }
            else if (oICharInfo is OIRouteInfo routeInfo)
            {
                children.AddRange(routeInfo.child);
            }

            foreach (var child in children)
            {
                CollectNames(child, ref names);
                child.DeleteKey();
            }
        }

        public static string PrepPath(string path)
        {
            var pathUri = new Uri(Path.GetFullPath(path));
            var rootUri = new Uri(Path.GetFullPath(
                StringUtils.JoinStrings(Path.DirectorySeparatorChar.ToString(), "UserData", "Studio", "scene") +
                Path.DirectorySeparatorChar).ToLowerInvariant());
            var pathString = pathUri.ToString();
            var rootString = rootUri.ToString();
            if (pathString.ToLowerInvariant().StartsWith(rootString, StringComparison.InvariantCulture))
            {
                pathUri = new Uri(rootString + pathString.Substring(rootString.Length));
            }

            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString()
                .Replace('/', Path.DirectorySeparatorChar));
        }

        private static void LazyResetHSPE()
        {
            Action wrapper = null;
            if (Chainloader.PluginInfos.TryGetValue(HSPEGUID, out var kkpeInfo))
            {
                var assembly = kkpeInfo.Instance.GetType().Assembly;
                var SceneInfo_Import_Patches = assembly.GetType("SceneInfo_Import_Patches");
                if (SceneInfo_Import_Patches != null)
                {
                    var method = AccessTools.Method(SceneInfo_Import_Patches, "Prefix", new Type[0]);
                    if (method != null)
                    {
                        Logger.LogInfo(
                            $"Installing workaround for {kkpeInfo.Metadata.Name} {kkpeInfo.Metadata.Version}");
                        wrapper = () => method.Invoke(null, new object[0]);
                    }
                }
            }

            ResetHSPEWrapper = wrapper;
            wrapper?.Invoke();
        }

        private static void ResetHSPE()
        {
            ResetHSPEWrapper?.Invoke();
        }

        private void ExecuteDump()
        {
            if (StudioInitObject == null) return;
            var scenes = GetListPath();

            if (scenes.Count < 1) return;

            var dirName = Path.GetDirectoryName(scenes[0]);
            if (string.IsNullOrEmpty(dirName)) return;

            var outputFile = Path.GetFullPath(Path.Combine(dirName, "SceneCharaInfo.csv"));

            var append = false;
            ProcessedScenes.Clear();
            if (File.Exists(outputFile))
            {
                append = true;
                using (var reader = new StreamReader(outputFile, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var fPath = line.Split(',').FirstOrDefault()?.Trim();
                        if (string.IsNullOrEmpty(fPath)) continue;
                        if (fPath.StartsWith($"{DQ}", StringComparison.InvariantCulture) &&
                            fPath.EndsWith($"{DQ}", StringComparison.InvariantCulture))
                        {
                            fPath = fPath.Substring(1, fPath.Length - 2);
                        }

                        ProcessedScenes.Add(fPath);
                    }
                }
            }

            Logger.LogDebug($"ProcessedScenes: \n\t{string.Join("\n\t", ProcessedScenes.ToArray())}");

            Logger.LogInfoMessage($"Dumping {scenes.Count} scenes to {outputFile}");

            using (var writer = new StreamWriter(outputFile, append, Encoding.UTF8))
            {
                var line = new List<string>();
                var i = 0;
                foreach (var pth in scenes)
                {
                    ResetHSPE();
                    i++;
                    var displayPath = PrepPath(pth);
                    //writer.Write($"{q}{displayPath}{q}");
                    if (ProcessedScenes.Contains(displayPath))
                    {
                        continue;
                    }

                    line.Clear();
                    line.Add(displayPath);
                    try
                    {
                        var names = ProcessScene(pth);
                        line.AddRange(names);
                        /*
                            foreach (string name in names.Distinct().OrderBy(a => a))
                            {
                                writer.Write($",{q}{name}{q}");
                            }
                            */
                        Logger.LogDebug($"finished {displayPath} ({i}/{scenes.Count})");
                    }
                    catch (Exception err)
                    {
                        //writer.Write($",{q}ERROR PROCESSING FILE{q}");
                        line.Add("ERROR PROCESSING FILE");
                        line.Add($"{err}".Replace(DQ, '\''));
                        Logger.LogError($"error processing {displayPath}: {err}");
                    }

                    writer.Write(DQ);
                    try
                    {
                        writer.Write(string.Join($"{DQ},{DQ}", line.ToArray()));
                    }
                    finally
                    {
                        writer.WriteLine(DQ);
                    }

                    ProcessedScenes.Add(displayPath);
                }
            }

            Logger.LogInfo($"Completed dumping {scenes.Count} scenes to {outputFile}");
            if (ResetHSPEWrapper != null)
            {
                Logger.LogWarningMessage("Dump complete. Reset or load new scene before proceeding");
            }

            GeBoAPI.Instance.PlayNotificationSound(NotificationSound.Success);
        }

        private List<string> ProcessScene(string pth)
        {
            var names = new List<string>();
            using (var fileStream = new FileStream(pth, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new BinaryReader(fileStream))
                {
                    PngFile.SkipPng(reader);
                    var version = new Version(reader.ReadString());
                    var num = reader.ReadInt32();
                    var infos = new List<ObjectInfo>();

                    for (var i = 0; i < num; i++)
                    {
                        var dummy = reader.ReadInt32();
                        var num2 = reader.ReadInt32();
                        ObjectInfo oICharInfo = null;

                        switch (num2)
                        {
                            case 0:
                            {
                                oICharInfo = new OICharInfo(null, Studio.Studio.GetNewIndex());
                                break;
                            }
                            case 1:
                            {
                                oICharInfo = new OIItemInfo(-1, -1, -1, Studio.Studio.GetNewIndex());
                                break;
                            }
                            case 2:
                            {
                                oICharInfo = new OILightInfo(-1, Studio.Studio.GetNewIndex());
                                break;
                            }
                            case 3:
                            {
                                oICharInfo = new OIFolderInfo(Studio.Studio.GetNewIndex());
                                break;
                            }
                            case 4:
                            {
                                oICharInfo = new OIRouteInfo(Studio.Studio.GetNewIndex());
                                break;
                            }
                            case 5:
                            {
                                oICharInfo = new OICameraInfo(Studio.Studio.GetNewIndex());
                                break;
                            }
                        }

                        ResetHSPE();
                        oICharInfo.Load(reader, version, true);
                        infos.Add(oICharInfo);
                        CollectNames(oICharInfo, ref names);
                    }

                    while (infos.Count > 0)
                    {
                        var info = infos[0];
                        infos.RemoveAt(0);
                        info.DeleteKey();
                    }
                }
            }

            return names;
        }
    }
}
