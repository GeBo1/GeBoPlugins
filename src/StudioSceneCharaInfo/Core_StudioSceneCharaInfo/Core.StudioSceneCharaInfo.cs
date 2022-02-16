using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using HarmonyLib;
using JetBrains.Annotations;
using KKAPI.Utilities;
using Manager;
using Studio;
using StudioSceneNavigationPlugin;
using TranslationHelperPlugin.Chara;
using UnityEngine;
using BepInLogLevel = BepInEx.Logging.LogLevel;
#if AI || HS2
using AIChara;
#endif

using StudioCheckScene = Studio.CheckScene;
using TranslationHelper = TranslationHelperPlugin.TranslationHelper;
using UnityEngineResources = UnityEngine.Resources;

namespace StudioSceneCharaInfoPlugin
{
    [BepInDependency(TranslationHelper.GUID)]
    [BepInDependency(StudioSceneNavigation.GUID, StudioSceneNavigation.Version)]
    [BepInDependency(GeBoAPI.GUID, GeBoAPI.Version)]
    [BepInDependency(HspeGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(Constants.StudioProcessName)]
    public partial class StudioSceneCharaInfo : BaseUnityPlugin
    {
        [PublicAPI]
        public const string GUID = Constants.PluginGUIDPrefix + "." + nameof(StudioSceneCharaInfo);

        public const string PluginName = "Studio Scene Chara Info";
        public const string Version = "0.2.0.1";

        // ReSharper disable once InconsistentNaming
        private const char DQ = '"';

        private static readonly Regex CsvSplitter =
            new Regex("(?:^|,)(\"(?:[^\"])*\"|[^,]*)", Constants.SupportedRegexCompilationOption);

        private static Action _resetHspeWrapper;

        internal static new ManualLogSource Logger;

        private static bool _dumping;

        private readonly HashSet<string> _processedScenes = new HashSet<string>();

        private static readonly string StudioSceneRootPath = PathUtils.CombinePaths(UserData.Path, "Studio", "scene");

        private static readonly string[] DumpTags = { "TranslatedNames", "OriginalNames" };

#if deadcode
        public static SceneLoadScene StudioInitObject { get; private set; }
#endif
        public static ConfigEntry<KeyboardShortcut> SceneCharaInfoDumpHotkey { get; private set; }

        internal void Awake()
        {
            Logger = base.Logger;

            SceneCharaInfoDumpHotkey = Config.Bind("Keyboard Shortcuts", "Dump Chara Info Hotkey",
                new KeyboardShortcut(KeyCode.Tab, KeyCode.LeftControl),
                "Pressing this will dump a spreadsheet containing info about characters in scenes.");
        }

        internal void Start()
        {
            Harmony.CreateAndPatchAll(typeof(StudioSceneCharaInfo));
        }

        internal void Update()
        {
            if (_dumping || !SceneCharaInfoDumpHotkey.Value.IsDown()) return;
            _dumping = true;

            var scenes = GetListPath();
            if (scenes.Count < 1)
            {
                Logger.LogWarningMessage("No scenes present for the last folder viewed in the loader");
                _dumping = false;
                return;
            }

            var systemButtonCtrl = Singleton<Studio.Studio>.Instance.systemButtonCtrl;
            Singleton<Studio.Studio>.Instance.colorPalette.visible = false;
            StudioCheckScene.sprite = systemButtonCtrl.spriteInit;
            StudioCheckScene.unityActionYes = OnDumpYes;
            StudioCheckScene.unityActionNo = OnDumpNo;
            var datum = new Scene.Data { levelName = "StudioCheck", isAdd = true };
            Singleton<Scene>.Instance.LoadReserve(datum, false);
        }

        private void OnDumpNo()
        {
            _dumping = false;
            Singleton<Studio.Studio>.Instance.systemButtonCtrl.OnSelectIniteNo();
        }

        private void OnDumpYes()
        {
            Singleton<Studio.Studio>.Instance.systemButtonCtrl.OnSelectInitYes();
            ExecuteDump();
        }

        public static List<string> GetListPath()
        {
            return StudioSceneNavigation.Public.GetScenePaths().ToList();
        }


        [UsedImplicitly]
        private void CollectCharInfos(ObjectInfo oICharInfo, ref List<ObjectInfo> charInfos)
        {
            var children = ListPool<ObjectInfo>.Get();
            switch (oICharInfo)
            {
                case OICharInfo charInfo:
                {
                    charInfos.Add(oICharInfo);
                    foreach (var kids in charInfo.child.Values)
                    {
                        children.AddRange(kids);
                    }

                    break;
                }
                case OIItemInfo itemInfo:
                    children.AddRange(itemInfo.child);
                    break;
                case OIFolderInfo folderInfo:
                    children.AddRange(folderInfo.child);
                    break;
                case OIRouteInfo routeInfo:
                    children.AddRange(routeInfo.child);
                    break;
            }

            foreach (var child in children)
            {
                CollectCharInfos(child, ref charInfos);
                child.DeleteKey();
            }

            ListPool<ObjectInfo>.Release(children);
        }

#if deadcode
        private void CollectNames(ObjectInfo oICharInfo, ref List<string> names)
        {
            var children = ListPool<ObjectInfo>.Get();
            if (oICharInfo is OICharInfo charInfo)
            {
                var info = charInfo.charFile.parameter;

                if (TranslationHelper.TryFastTranslateFullName(TranslationHelperPlugin.Utils.CharaFileInfoWrapper.CreateWrapper(charInfo.charFile), out var translatedName))
                {
                    names.Add(translatedName);
                }
                else
                {
                    StartCoroutine(TranslationHelper.TranslateCardNames(charInfo.charFile));
                    names.Add(info.fullname);
                }

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

            ListPool<ObjectInfo>.Release(children);
        }
#endif

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

        private static void LazyResetHspe()
        {
            Action wrapper = null;
            if (Chainloader.PluginInfos.TryGetValue(HspeGuid, out var hspeInfo))
            {
                var assembly = hspeInfo.Instance.GetType().Assembly;
                var sceneInfoImportPatches = assembly.GetType("SceneInfo_Import_Patches");
                if (sceneInfoImportPatches != null)
                {
                    var method = AccessTools.Method(sceneInfoImportPatches, "Prefix",
                        ObjectUtils.GetEmptyArray<Type>());
                    if (method != null)
                    {
                        Logger.LogInfo(
                            $"Installing workaround for {hspeInfo.Metadata.Name} {hspeInfo.Metadata.Version}");
                        wrapper = () => method.Invoke(null, new object[0]);
                    }
                }
            }

            _resetHspeWrapper = wrapper;
            wrapper?.Invoke();
        }

        private static void ResetHspe()
        {
            _resetHspeWrapper?.Invoke();
        }

        private void ExecuteDump()
        {
            //if (StudioInitObject == null) return;
            var scenes = GetListPath();

            var coroutines = ListPool<IEnumerator>.Get();
            try
            {
                if (scenes.Count > 0)
                {
                    var dirName = Path.GetDirectoryName(scenes[0]);
                    if (!string.IsNullOrEmpty(dirName))
                    {
                        scenes.Reverse();
                        Logger.LogInfoMessage($"Start dump for {dirName}");


                        coroutines.Add(ProcessScenes(dirName, scenes));
                    }
                }

                coroutines.Add(PostDump());
                StartCoroutine(CoroutineUtils.ComposeCoroutine(coroutines.ToArray()));
            }
            finally
            {
                ListPool<IEnumerator>.Release(coroutines);
            }
#if deadcode
            var outputFile = Path.GetFullPath(Path.Combine(dirName, "SceneCharaInfo.csv"));

            var append = false;
            _processedScenes.Clear();
            try
            {
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

                            _processedScenes.Add(fPath);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Logger.LogException(err, this, $"Unable to load existing csv file");
                _processedScenes.Clear();
            }

            Logger.LogDebug($"ProcessedScenes: \n\t{string.Join("\n\t", _processedScenes.ToArray())}");

            Logger.LogInfoMessage($"Dumping {scenes.Count} scenes to {outputFile}");

            using (var writer = new StreamWriter(outputFile, append, Encoding.UTF8))
            {
                var line = new List<string>();
                var i = 0;
                foreach (var pth in scenes)
                {
                    ResetHspe();
                    i++;
                    var displayPath = PrepPath(pth);
                    //writer.Write($"{q}{displayPath}{q}");
                    if (_processedScenes.Contains(displayPath))
                    {
                        continue;
                    }

                    line.Clear();
                    line.Add(displayPath);
                    try
                    {
                        var names = ProcessScene(pth);
                        line.AddRange(names.Distinct().OrderBy(a => a));
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
                        Logger.LogException(err, $"{nameof(ExecuteDump)}: error processing {displayPath}");
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

                    _processedScenes.Add(displayPath);
                }
            }

            Logger.LogInfo($"Completed dumping {scenes.Count} scenes to {outputFile}");
            if (_resetHspeWrapper != null)
            {
                Logger.LogWarningMessage("Dump complete. Reset or load new scene before proceeding");
            }

            GeBoAPI.Instance.PlayNotificationSound(NotificationSound.Success);
#endif
        }

        private IEnumerator PostDump()
        {
            yield return null;
            ResetStudio();
            yield return null;
            _dumping = false;
        }

        private static void ResetStudio()
        {
            Singleton<Studio.Studio>.Instance.InitScene();
            ResetHspe();
        }

        public static string[] SplitProcessedSceneLine(string line)
        {
            var list = ListPool<string>.Get();
            try
            {
                foreach (Match match in CsvSplitter.Matches(line))
                {
                    var entry = match.Value;
                    if (0 == entry.Length)
                    {
                        list.Add("");
                    }

                    list.Add(TrimQuotes(entry.TrimStart(',')));
                }

                return list.ToArray();
            }
            finally
            {
                ListPool<string>.Release(list);
            }

            string TrimQuotes(string val)
            {
                return val[0] == '"' && val[val.Length - 1] == '"' ? val.Substring(1, val.Length - 2) : val;
            }
        }

        private Dictionary<string, HashSet<string>> LoadProcessedScenes(string resultFile)
        {
            var result = new Dictionary<string, HashSet<string>>();
            if (!File.Exists(resultFile)) return result;

            using (var reader = new StreamReader(resultFile, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var entry = SplitProcessedSceneLine(line);
                    if (entry.Length < 2) continue;
                    var key = entry[1].Trim();
                    if (!result.TryGetValue(key, out var hashSet))
                    {
                        result[key] = hashSet = new HashSet<string>();
                    }

                    hashSet.Add(entry[0].Trim());
                    Logger.LogWarning($"{nameof(LoadProcessedScenes)}: added {entry[1]} {entry[0]}");
                }
            }

            return result;
        }

        private IEnumerator ProcessScenes(string dirName, List<string> scenes)
        {
            var start = Time.realtimeSinceStartup;
            var sceneNum = 0;
            var sceneTotal = scenes.Count;
            var numFmt = "".PadLeft(sceneTotal.ToString().Length, '0');
            var msgFormat = "[{0:" + numFmt + "}/{1}] (ETA: {4}) {2}: {3}";
            string relativeScenePath;
            var scenesSkipped = 0;
            var resultTmp = new List<string>(25);

            var outputFile = Path.GetFullPath(Path.Combine(dirName, $"{nameof(StudioSceneCharaInfo)}.csv"));
            var tmpFile = PathUtils.GetTempFile(nameof(StudioSceneCharaInfo), "csv");
            Dictionary<string, HashSet<string>> processedScenes = null;

            var needsBackup = true;

            if (File.Exists(outputFile))
            {
                File.Copy(outputFile, tmpFile);
                processedScenes = LoadProcessedScenes(tmpFile);
            }

            Logger.LogFatal(
                $"processed scene loaded from {outputFile}: {processedScenes?.Values.Select(v => v.Count).Sum() / 2}");

            bool SceneHandled()
            {
                if (processedScenes == null) return false;
                var result = DumpTags.All(t =>
                    processedScenes.TryGetValue(t, out var h) && h.Contains(relativeScenePath));
                Logger.LogFatal($"{nameof(SceneHandled)}: {relativeScenePath} => {result}");
                return result;
            }

            void LogProgress(BepInLogLevel logLevel, string message, bool displayMessage = true)
            {
                var eta = "-";
                var scenesProcessed = sceneNum - scenesSkipped;
                if (scenesProcessed > 1)
                {
                    var tmpTotal = sceneTotal - scenesSkipped;
                    var elapsedMilliseconds = (Time.realtimeSinceStartup - start) * 1000f;
                    var avgMilliseconds = elapsedMilliseconds / scenesProcessed;
                    var estimatedRemainingMilliseconds = (tmpTotal + 1 - scenesProcessed) * avgMilliseconds;
                    if (estimatedRemainingMilliseconds > 0)
                    {
                        eta = TimeSpan.FromMilliseconds(estimatedRemainingMilliseconds).ToString();
                    }
                }

                if (displayMessage) logLevel |= BepInLogLevel.Message;
                Logger.Log(logLevel,
                    string.Format(msgFormat, sceneNum, sceneTotal, message,
                        relativeScenePath.IsNullOrEmpty() ? "-" : Path.GetFileName(relativeScenePath), eta));
            }

            try
            {
                ResetHspe();
                var append = File.Exists(tmpFile) && processedScenes != null && processedScenes.Count > 0 &&
                             processedScenes.Values.Any(h => h.Count > 0);
                using (var writer = new StreamWriter(tmpFile, append, Encoding.UTF8))
                {
                    var nextBackupTime = Time.realtimeSinceStartup + (60f * 3);
                    var nextNotifyTime = Time.realtimeSinceStartup + 60f;
                    foreach (var scene in scenes)
                    {
                        relativeScenePath = PathUtils.GetRelativePath(StudioSceneRootPath, scene);
                        sceneNum++;

                        if (SceneHandled())
                        {
                            scenesSkipped++;
                            continue;
                        }

                        if (!StudioSceneNavigation.Public.IsSceneMaybeValid(scene))
                        {
                            LogProgress(BepInLogLevel.Warning, "skipping invalid scene");
                            scenesSkipped++;
                            continue;
                        }

                        yield return StudioSceneNavigation.Public.LoadSceneExternal(scene);

                        if (StudioSceneNavigation.Public.LastLoadFailed)
                        {
                            LogProgress(BepInLogLevel.Error, "error loading scene");
                            yield return StartCoroutine(ResetStudioCoroutine());
                        }

                        // wait a frame or sometimes character list is empty
                        ChaControl[] chaControls;
                        var lastCount = -1;
                        var stable = 0;
                        while (true)
                        {
                            chaControls = FindObjectsOfType<ChaControl>();
                            if (chaControls.Length == lastCount)
                            {
                                stable++;
                                if ((lastCount == 0 && stable > 5) || stable >= 2) break;
                            }
                            else
                            {
                                lastCount = chaControls.Length;
                                stable = 0;
                            }

                            yield return null;
                        }

                        Logger.LogDebug($"{scene} contains {lastCount} characters");


                        yield return TranslationHelper.WaitOnCardTranslations();

                        AddResult(true, CollectNames(chaControls, true));

                        // should already be handled, in which case this will be pretty much a no-op
                        yield return TranslateCards(chaControls);

                        AddResult(false, CollectNames(chaControls));
                        var logMessage = Time.realtimeSinceStartup > nextNotifyTime;
                        LogProgress(BepInLogLevel.Info, "processed scene", logMessage);
                        if (logMessage) nextNotifyTime = Time.realtimeSinceStartup + 60f;
                        yield return StartCoroutine(ResetStudioCoroutine());
                        if (!(Time.realtimeSinceStartup > nextBackupTime)) continue;

                        // periodically update destination file
                        writer.Flush();
                        PathUtils.ReplaceFile(tmpFile, outputFile, needsBackup, true);
                        needsBackup = false;
                        nextBackupTime = Time.realtimeSinceStartup + (60f * 3);
                    }

                    void AddResult(bool originalNames, IEnumerable<string> names)
                    {
                        resultTmp.Clear();
                        resultTmp.Add(relativeScenePath);
                        resultTmp.Add(DumpTags[originalNames ? 1 : 0]);
                        resultTmp.AddRange(names);

                        writer.Write(DQ);
                        try
                        {
                            writer.Write(string.Join($"{DQ},{DQ}", resultTmp.ToArray()));
                        }
                        finally
                        {
                            writer.WriteLine(DQ);
                        }
                    }
                }
            }
            finally
            {
                PathUtils.ReplaceFile(tmpFile, outputFile, needsBackup);
            }


            Logger.LogInfoMessage(
                $"Processed {sceneNum} scenes in {TimeSpan.FromMilliseconds((Time.realtimeSinceStartup - start) * 1000f)}");
            yield return null;
            Logger.LogInfoMessage($"Results saved to {outputFile}");
        }


        private static IEnumerator ResetStudioCoroutine()
        {
            ResetStudio();
            yield return null;
            ResetHspe();
            UnityEngineResources.UnloadUnusedAssets();
            GC.Collect();
            yield return null;
        }

        private IEnumerator TranslateCards(ChaControl[] chaControls)
        {
            var jobs = ListPool<Coroutine>.Get();
            try
            {
                foreach (var chara in chaControls)
                {
                    jobs.Add(StartCoroutine(chara.chaFile.TranslateCardNamesCoroutine()));
                }

                foreach (var job in jobs) yield return job;
            }
            finally
            {
                ListPool<Coroutine>.Release(jobs);
            }
        }

        private IEnumerable<string> CollectNames(ChaControl[] chaControls, bool originalNames = false)
        {
            var names = HashSetPool<string>.Get();
            try
            {
                foreach (var chara in chaControls)
                {
                    names.Add(originalNames ? chara.chaFile.GetOriginalFullName() : chara.chaFile.GetFullName());
                }

                // yielding entries so results can be ordered and HashSet can be released when exhausted
                foreach (var chaName in names.OrderBy(a => a)) yield return chaName;
            }
            finally
            {
                HashSetPool<string>.Release(names);
            }
        }


#if deadcode
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
                    var infos = ListPool<ObjectInfo>.Get();

                    for (var i = 0; i < num; i++)
                    {
                        var dummy = reader.ReadInt32();
                        var num2 = reader.ReadInt32();
                        ObjectInfo oICharInfo;

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
                            default:
                                continue;
                        }

                        ResetHspe();
                        try
                        {
                            oICharInfo.Load(reader, version, true);
                            infos.Add(oICharInfo);
                            CollectNames(oICharInfo, ref names);
                        }
                        catch (Exception err)
                        {
                            Logger.LogException(err, $"{nameof(ProcessScene)}: error loading {oICharInfo} from {pth}");
                        }
                    }

                    while (infos.Count > 0)
                    {
                        var info = infos[0];
                        infos.RemoveAt(0);
                        info.DeleteKey();
                    }

                    ListPool<ObjectInfo>.Release(infos);
                }
            }

            return names;
        }
#endif
    }
}
