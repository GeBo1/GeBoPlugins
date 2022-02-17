using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using GeBoCommon;
using GeBoCommon.Studio;
using GeBoCommon.Utilities;
using HarmonyLib;
using JetBrains.Annotations;
using KKAPI;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using Studio;
using UnityEngine;
using UnityEngine.UI;

namespace StudioSceneNavigationPlugin
{
    [BepInDependency(GeBoAPI.GUID, GeBoAPI.Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(ExtendedSave.GUID)]
    [BepInDependency("marco.FolderBrowser", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class StudioSceneNavigation : BaseUnityPlugin
    {
        [PublicAPI]
        public const string GUID = Constants.PluginGUIDPrefix + "." + nameof(StudioSceneNavigation);

        public const string PluginName = "Studio Scene Navigation";
        public const string Version = "1.0.3.0";

        private const float SaveDelay = 5f;
        private static readonly object SaveFileLock = new object();

        internal static readonly char[] TrackingFileEntrySplit = { '\0' };
        internal static new ManualLogSource Logger;
        private static List<string> _normalizedScenePaths;
        private static string _currentSceneFolder = string.Empty;
        private static readonly object SavePendingLock = new object();
        private static bool _setPage;
        private static KeyValuePair<int, string>? LastPageSet;

        private static SceneLoadScene _sceneLoadScene;

        private static bool _externalLoadInProgress;


        private static readonly ExpiringSimpleCache<string, string> SceneRelativePathCache =
            new ExpiringSimpleCache<string, string>(CalculateSceneRelativePath, TimeSpan.FromMinutes(15),
                $"{typeof(StudioSceneNavigation).PrettyTypeFullName()}.{nameof(SceneRelativePathCache)}");

        private static readonly string[] TrackLastLoadedSceneFiles =
        {
            PathUtils.NormalizePath(PathUtils.CombinePaths(UserData.Path, "Studio",
                StringUtils.JoinStrings(".", nameof(StudioSceneNavigation), "LastLoadedScene", "data"))),
            PathUtils.NormalizePath(PathUtils.CombinePaths(
                Paths.ConfigPath, StringUtils.JoinStrings(".", GUID, "LastLoadedScene", "data")))
        };

        private static StudioSceneNavigation _instance;

        private readonly SimpleLazy<Func<string, bool>> _isSceneValid;

        private readonly SimpleLazy<Dictionary<string, string>> _lastLoadedScenes;
        //private readonly IEnumerator _saveTrackingFileDelay;

        private string _currentScenePath = string.Empty;
        private string _currentScenePathCandidate = string.Empty;

        private bool _navigationInProgress;
        private bool _savePending;
        private float _saveReady;
        private bool _scrollPending;

        private Coroutine _scrollToLastLoadedScene;

        public StudioSceneNavigation()
        {
            _lastLoadedScenes = new SimpleLazy<Dictionary<string, string>>(() =>
                TrackLastLoadedSceneEnabled.Value
                    ? LoadTrackingFile()
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

            _isSceneValid = new SimpleLazy<Func<string, bool>>(() =>
            {
                var pluginInfo = Chainloader.PluginInfos.Where(pi => pi.Key.EndsWith("InvalidSceneFileProtection"))
                    .Select(pi => pi.Value).FirstOrDefault();
                if (pluginInfo == null) return _ => true;

                var pluginType = pluginInfo.Instance.GetType();
                var method = AccessTools.Method(pluginType, "IsFileValid");
                if (method == null) return _ => true;

                Logger.LogDebug(
                    $"Will use {pluginType.Name}.{method.Name} to pre-check images during navigation");
                return (Func<string, bool>)Delegate.CreateDelegate(typeof(Func<string, bool>), method);
            });

            //_saveTrackingFileDelay = new WaitUntil(IsSaveReady);
        }

        private static string TrackLastLoadedSceneFile => TrackLastLoadedSceneFiles[0];

        public static StudioSceneNavigation Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<StudioSceneNavigation>();
                }

                return _instance;
            }
        }

        private static List<string> ScenePaths { get; set; } = new List<string>(0);

        [Obsolete("This is slow, used ScenePaths and NormalizedPathComparer")]
        private static List<string> NormalizedScenePaths => _normalizedScenePaths ??
                                                            (_normalizedScenePaths =
                                                                new List<string>(
                                                                    ScenePaths.Select(PathUtils.NormalizePath)));

        private Func<string, bool> IsSceneValid => _isSceneValid.Value;

        private bool SavePending
        {
            get
            {
                lock (SavePendingLock) return _savePending;
            }
            set
            {
                lock (SavePendingLock) _savePending = value;
                if (value) UpdateSaveReady();
            }
        }

        private Dictionary<string, string> LastLoadedScenes => _lastLoadedScenes.Value;

        public static ConfigEntry<KeyboardShortcut> NavigateNextSceneShortcut { get; private set; }
        public static ConfigEntry<KeyboardShortcut> NavigatePrevSceneShortcut { get; private set; }
        public static ConfigEntry<KeyboardShortcut> ReloadCurrentSceneShortcut { get; private set; }
        public static ConfigEntry<bool> NotificationSoundsEnabled { get; private set; }
        public static ConfigEntry<bool> TrackLastLoadedSceneEnabled { get; private set; }
        public static ConfigEntry<bool> RestoreLoaderPage { get; private set; }

        internal void Awake()
        {
            _instance = this;
            Logger = base.Logger;
            if (_currentSceneFolder.IsNullOrEmpty()) _currentSceneFolder = SceneUtils.StudioSceneRootFolder;
            Harmony.CreateAndPatchAll(typeof(Hooks));
            ExtendedSave.SceneBeingLoaded += ExtendedSave_SceneBeingLoaded;
            StudioSaveLoadApi.SceneLoad += StudioSaveLoadApi_SceneLoad;
            KoikatuAPI.Quitting += KoikatuAPI_Quitting;
        }

        internal void Update()
        {
            if (_navigationInProgress) return;

            if (NavigateNextSceneShortcut.Value.IsDown())
            {
                NavigateScene(1);
            }
            else if (NavigatePrevSceneShortcut.Value.IsDown())
            {
                NavigateScene(-1);
            }
            else if (ReloadCurrentSceneShortcut.Value.IsDown())
            {
                NavigateScene(0);
            }
        }

        internal void OnDestroy()
        {
            StopAllCoroutines();
            if (TrackLastLoadedSceneEnabled.Value && SavePending)
            {
                SaveTrackingFile();
            }
        }

        private void KoikatuAPI_Quitting(object sender, EventArgs args)
        {
            StopAllCoroutines();
            if (TrackLastLoadedSceneEnabled.Value && SavePending)
            {
                SaveTrackingFile();
            }
        }

        private static string FastCombineNormalizedPaths(params string[] parts)
        {
            return PathUtils.NormalizePath(StringUtils.JoinStrings(
                Path.DirectorySeparatorChar, parts));
        }


        private static string CalculateSceneRelativePath(string scenePath)
        {
            return PathUtils.GetRelativePath(SceneUtils.StudioSceneRootFolder, scenePath);
        }

        private bool IsSaveReady()
        {
            return Time.realtimeSinceStartup > _saveReady;
        }

        internal void Main()
        {
            _instance = this;
            Logger = base.Logger;
            NavigateNextSceneShortcut = Config.Bind("Keyboard Shortcuts", "Navigate Next",
                new KeyboardShortcut(KeyCode.F3, KeyCode.LeftShift), "Navigate to the next (newer) scene");
            NavigatePrevSceneShortcut = Config.Bind("Keyboard Shortcuts", "Navigate Previous",
                new KeyboardShortcut(KeyCode.F4, KeyCode.LeftShift), "Navigate to the previous (older) scene");
            ReloadCurrentSceneShortcut = Config.Bind("Keyboard Shortcuts", "Reload Current",
                new KeyboardShortcut(KeyCode.F5, KeyCode.LeftShift), "Reload the currently loaded scene");
            NotificationSoundsEnabled = Config.Bind("Config", "Notification Sounds", true,
                "When enabled, notification sounds will play when scene loading is complete, or navigation fails");
            TrackLastLoadedSceneEnabled = Config.Bind("Config", "Track Last Loaded Scene", true,
                "When enabled, the last loaded scene will be tracked externally and can be reloaded upon return");
            RestoreLoaderPage = Config.Bind("Config", "Restore Loader Page", true,
                "When opening the scene browser, scroll to the last loaded scene");
            GeBoAPI.Instance.SetupNotificationSoundConfig(GUID, NotificationSoundsEnabled);
        }

        private static void PlayNotificationSound(NotificationSound notificationSound)
        {
            GeBoAPI.Instance.PlayNotificationSound(notificationSound, GUID);
        }

        private void StudioSaveLoadApi_SceneLoad(object sender, SceneLoadEventArgs e)
        {
            // don't update navigation history on external load/import/clear
            if (_externalLoadInProgress) return;

            UpdateSaveReady();

            if (e.Operation == SceneOperationKind.Clear)
            {
                _currentScenePath = _currentScenePathCandidate = string.Empty;
                return;
            }

            if (e.Operation == SceneOperationKind.Import)
            {
                _currentScenePathCandidate = string.Empty;
                return;
            }

            var coroutines = GeBoCommon.Utilities.ListPool<IEnumerator>.Get();
            try
            {
                if (!string.IsNullOrEmpty(_currentScenePathCandidate) && e.Operation == SceneOperationKind.Load)
                {
                    _currentScenePath = _currentScenePathCandidate;
                    _currentScenePathCandidate = string.Empty;
                    if (_navigationInProgress)
                    {
                        coroutines.Add(
                            CoroutineUtils.CreateCoroutine(() => PlayNotificationSound(NotificationSound.Success)));
                        coroutines.Add(SetPageCoroutine(_currentScenePath));
                    }
                }

                coroutines.Add(CoroutineUtils.CreateCoroutine(() =>
                {
                    _navigationInProgress = false;
                    TrackLastLoadedScene();
                }));
                coroutines.Add(SaveTrackingFileCoroutine());
                StartCoroutine(CoroutineUtils.ComposeCoroutine(coroutines.ToArray()));
            }
            finally
            {
                GeBoCommon.Utilities.ListPool<IEnumerator>.Release(coroutines);
            }
        }


        private void UpdateSaveReady()
        {
            _saveReady = Time.realtimeSinceStartup + SaveDelay;
        }

        private void ExtendedSave_SceneBeingLoaded(string path)
        {
            if (_externalLoadInProgress) return;
            _currentScenePathCandidate = PathUtils.NormalizePath(path);
        }

        private void SaveTrackingFile()
        {
            Logger.DebugLogDebug($"{nameof(SaveTrackingFile)} fired");
            if (!SavePending) return;
            if (TrackLastLoadedSceneFile == null)
            {
                throw new NullReferenceException($"{nameof(TrackLastLoadedSceneFile)} should not be null");
            }

            var prefix = PathUtils.CombinePaths(Paths.CachePath, Path.GetFileName(TrackLastLoadedSceneFile));
            var newFile = string.Concat(prefix, Path.GetRandomFileName());
            var oldFile = string.Concat(prefix, Path.GetRandomFileName());

            var relativeScenes = DictionaryPool<string, string>.Get();
            try
            {
                foreach (var entry in LastLoadedScenes)
                {
                    relativeScenes[SceneRelativePathCache.Get(entry.Key)] =
                        Path.GetFileName(entry.Value);
                }

                lock (SavePendingLock)
                {
                    if (!SavePending) return;

                    try
                    {
                        using (var fileStream = new FileStream(newFile, FileMode.Create))
                        using (var streamWriter = new StreamWriter(fileStream, Encoding.UTF8))
                        {
                            streamWriter.Write(GUID);
                            streamWriter.Write(TrackingFileEntrySplit[0]);
                            streamWriter.Write(Version);
                            streamWriter.Write(TrackingFileEntrySplit[0]);
                            streamWriter.Write(relativeScenes.Count);
                            streamWriter.Write(TrackingFileEntrySplit[0]);
                            streamWriter.Write('\n');
                            foreach (var entry in relativeScenes)
                            {
                                streamWriter.Write(entry.Key);
                                streamWriter.Write(TrackingFileEntrySplit[0]);
                                streamWriter.Write(entry.Value);
                                streamWriter.Write('\n');
                            }
                        }

                        File.Move(TrackLastLoadedSceneFile, oldFile);
                        File.Move(newFile, TrackLastLoadedSceneFile);
                        File.Delete(oldFile);
                        SavePending = false;
                        Logger.DebugLogDebug($"Updated {TrackLastLoadedSceneFile}");
                    }
                    catch (Exception err)
                    {
                        if (!File.Exists(oldFile)) throw;
                        Logger.LogException(err, this,
                            $"{nameof(SaveTrackingFile)}: Error encountered, restoring {TrackLastLoadedSceneFile}");

                        File.Copy(oldFile, TrackLastLoadedSceneFile);

                        throw;
                    }
                    finally
                    {
                        if (File.Exists(oldFile)) File.Delete(oldFile);
                        if (File.Exists(newFile)) File.Delete(newFile);
                    }
                }
            }
            finally
            {
                DictionaryPool<string, string>.Release(relativeScenes);
            }
        }

        private static bool TryReadTrackingHeader(StreamReader reader, out Version version, out int expectedCount)
        {
            version = new Version(0, 0, 0, 0);
            expectedCount = -1;


            var line = reader.ReadLine();
            if (string.IsNullOrEmpty(line))
            {
                return false;
            }

            var entry = line.Split(TrackingFileEntrySplit);
            if (entry[0] != GUID)
            {
                return false;
            }

            try
            {
                version = new Version(entry[1]);
            }
            catch (ArgumentException) { }
            catch (FormatException) { }
            catch (OverflowException) { }

            if (!int.TryParse(entry[2], out expectedCount))
            {
                expectedCount = -1;
            }

            return true;
        }

        private void MigrateTrackingFile()
        {
            var trackingFile = TrackLastLoadedSceneFiles.Where(File.Exists).FirstOrDefault();
            if (trackingFile == null || trackingFile == TrackLastLoadedSceneFile) return;
            Logger.LogInfoMessage(
                $"Tracking file in outdated location '{trackingFile}', new version will be saved to '{TrackLastLoadedSceneFile}'");
            File.Copy(trackingFile, TrackLastLoadedSceneFile);
        }

        private Dictionary<string, string> LoadTrackingFile()
        {
            var lastLoadedScenes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var count = 0;

            MigrateTrackingFile();
            using (var fileStream = new FileStream(TrackLastLoadedSceneFile, FileMode.OpenOrCreate))
            {
                int expectedCount;
                Version version;
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true))
                {
                    if (!TryReadTrackingHeader(streamReader, out version, out expectedCount))
                    {
                        fileStream.Position = 0;
                    }

                    string line;

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        if (line.IsNullOrEmpty())
                        {
                            continue;
                        }

                        try
                        {
                            count++;
                            var entry = line.Split(TrackingFileEntrySplit, 2);
                            // Normalize on load to take care of any changes between versions

                            if (!Path.IsPathRooted(entry[0]))
                            {
                                if (entry[0] == ".")
                                {
                                    // already normalized
                                    entry[0] = SceneUtils.StudioSceneRootFolder;
                                }
                                else
                                {
                                    entry[0] = FastCombineNormalizedPaths(SceneUtils.StudioSceneRootFolder,
                                        entry[0]);
                                }
                            }


                            if (lastLoadedScenes.ContainsKey(entry[0]))
                            {
                                Logger.LogWarning(
                                    $"{nameof(LoadTrackingFile)}: line {count}: duplicate key {entry[0]}");
                            }

                            lastLoadedScenes[entry[0]] = Path.GetFileName(entry[1]);
                        }
                        catch (Exception err)
                        {
                            Logger.LogException(err,
                                $"{nameof(LoadTrackingFile)}: line {count}: {line.TrimEnd()}");
                        }
                    }
                }

                if (expectedCount >= 0 && version >= new Version(0, 8, 7) && expectedCount != count)
                {
                    Logger.LogWarningMessage(
                        $"{nameof(LoadTrackingFile)}: {TrackLastLoadedSceneFile} may be corrupted. It contains {count} entries, expected {expectedCount}.");
                }
            }

            Logger.DebugLogDebug($"{nameof(LoadTrackingFile)}: {TrackLastLoadedSceneFile} loaded ({count} entries)");
            return lastLoadedScenes;
        }

        private int GetCurrentPathIndex()
        {
            if (_currentScenePath.IsNullOrEmpty() || ScenePaths.IsNullOrEmpty()) return -1;
            return ScenePaths.FindPathIndex(_currentScenePath);
        }

        private void NavigateScene(int offset)
        {
            if (_navigationInProgress) return;
            _navigationInProgress = true;
            var navigated = false;
            Logger.LogDebug($"Attempting navigate to scene: {offset:+#;-#;0}");
            try
            {
                var paths = ScenePaths;
                var index = GetCurrentPathIndex();
                string nextImage = null;

                if (index == -1)
                {
                    if (!paths.IsNullOrEmpty())
                    {
                        Logger.LogDebug(
                            $"Folder changed, resuming navigation for: {PathUtils.GetRelativePath(SceneUtils.StudioSceneRootFolder, _currentSceneFolder)}");
                        if (TryGetLastLoadedScene(out nextImage))
                        {
                            _currentScenePath = nextImage;
                            index = GetCurrentPathIndex();
                        }
                    }
                }
                else
                {
                    while (nextImage is null)
                    {
                        index -= offset;
                        if (index < 0 || index >= paths.Count)
                        {
                            Logger.LogInfoMessage("No further scenes to navigate to.");
                            return;
                        }

                        nextImage = paths[index];
                        if (IsSceneValid(nextImage)) continue;

                        Logger.LogWarningMessage($"Skipping invalid scene file: {nextImage}");
                        nextImage = null;
                    }
                }

                if (index >= 0 && !nextImage.IsNullOrEmpty())
                {
                    var coroutines = GeBoCommon.Utilities.ListPool<IEnumerator>.Get();
                    try
                    {
                        coroutines.Add(CoroutineUtils.CreateCoroutine(
                            () => Logger.LogInfoMessage(
                                $"Loading scene {paths.Count - index}/{paths.Count} ('{PathUtils.GetRelativePath(SceneUtils.StudioSceneRootFolder, nextImage)}')")));
                        coroutines.Add(LoadScene(nextImage));
                        coroutines.Add(CleanupAfterLoad());
                        StartCoroutine(CoroutineUtils.ComposeCoroutine(coroutines.ToArray()));
                    }
                    finally
                    {
                        GeBoCommon.Utilities.ListPool<IEnumerator>.Release(coroutines);
                    }

                    navigated = true;
                }
            }
            catch (Exception err)
            {
                Logger.LogException(err, this, nameof(NavigateScene));
            }
            finally
            {
                // error encountered during navigation
                if (!navigated)
                {
                    _navigationInProgress = false;
                    PlayNotificationSound(NotificationSound.Error);
                }
            }

            if (navigated) _setPage = true;
        }

        private static IEnumerator CleanupAfterLoad()
        {
            // wait 2 frames
            yield return null;
            yield return null;
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }


        private IEnumerator LoadScene(string nextImage)
        {
            if (_sceneLoadScene != null)
            {
                // try to mimic clicking scene from scene browser
                try
                {
                    return _sceneLoadScene.LoadScene(nextImage);
                }
                catch (Exception err)
                {
                    Logger.LogException(err, this,
                        $"{nameof(LoadScene)}: unexpected error loading scene via {_sceneLoadScene.GetPrettyTypeName()}, will try fallback");
                }
            }

            return Singleton<Studio.Studio>.Instance.LoadSceneCoroutine(nextImage);
        }

        private static IEnumerator SetPageCoroutine(string scenePath)
        {
            if (_externalLoadInProgress || !_setPage) yield break;

            _setPage = false;
            yield return null;
            var page = GetLoaderPageForImage(scenePath);
            if (page < 0) page = 0;
            SceneLoadScene.page = page;
        }

        private Action SaveTrackingFileWorker()
        {
            while (SavePending && TrackLastLoadedSceneEnabled.Value && !IsSaveReady())
            {
                Thread.Sleep(100);
            }

            if (SavePending && TrackLastLoadedSceneEnabled.Value) SaveTrackingFile();
            return null;
        }

        private IEnumerator SaveTrackingFileCoroutine()
        {
            yield return null;
            ThreadingHelper.Instance.StartAsyncInvoke(SaveTrackingFileWorker);
            /*
            if (!SavePending || !TrackLastLoadedSceneEnabled.Value) yield break;
            yield return _saveTrackingFileDelay;
            if (SavePending && TrackLastLoadedSceneEnabled.Value) SaveTrackingFile();
            */
        }

        private void OnInitInfo(SceneLoadScene sceneLoadScene)
        {
            _sceneLoadScene = sceneLoadScene;
            _currentSceneFolder = string.Empty;
            ScenePaths = SceneUtils.GetSceneLoaderPaths(sceneLoadScene);
            _normalizedScenePaths = null;
            ScenePaths.SafeProc(0,
                p => _currentSceneFolder = PathUtils.NormalizePath(Path.GetDirectoryName(p)));

            if (!RestoreLoaderPage.Value) return;
            // any time InitInfo fires force scroll (bar will jump to top otherwise)
            _scrollPending = true;
            if (!LastPageSet.HasValue ||
                !PathUtils.NormalizedPathComparer.Equals(LastPageSet.Value.Value, _currentSceneFolder))
            {
                // if folder changed restore page of last loaded image,
                // otherwise keep current page when loader is dismissed and re-opened
                var lastLoadedScene = GetLastLoadedScene();
                var page = Math.Max(0, GetLoaderPageForImage(lastLoadedScene));
                SceneLoadScene.page = page;
                // _sceneLoadScene.SetPage(SceneLoadScene.page) should fire after OnInit
                // if not it'll be caught when trying to scroll and handled
            }
        }


        public static int GetLoaderPageForImage(string sceneFile)
        {
            if (sceneFile.IsNullOrEmpty() || ScenePaths.Count < 1) return -2;
            var index = ScenePaths.FindPathIndex(sceneFile);
            if (index < 0) return index;

            return index / ImagesPerPage;
        }


        private void OnSetPage(SceneLoadScene sceneLoadScene, int page)
        {
            try
            {
                LastPageSet = new KeyValuePair<int, string>(page, _currentSceneFolder);
            }
            catch
            {
                LastPageSet = null;
            }

            ScrollToCurrentPage(sceneLoadScene);
        }

        internal void ScrollToCurrentPage(SceneLoadScene sceneLoadScene)
        {
            if (!RestoreLoaderPage.Value || sceneLoadScene == null) return;
            if (_scrollToLastLoadedScene != null) StopCoroutine(_scrollToLastLoadedScene);
            _scrollToLastLoadedScene = StartCoroutine(ScrollToCurrentPageCoroutine(sceneLoadScene)
                .AppendCo(() => _scrollToLastLoadedScene = null));
        }

        internal IEnumerator ScrollToCurrentPageCoroutine(SceneLoadScene sceneLoadScene)
        {
            yield return null;
            if (sceneLoadScene == null || sceneLoadScene != _sceneLoadScene) yield break;

            // check if page selection is out of sync
            if (!LastPageSet.HasValue || SceneLoadScene.page != LastPageSet.Value.Key ||
                !PathUtils.NormalizedPathComparer.Equals(_currentSceneFolder, LastPageSet?.Value))
            {
                sceneLoadScene.SetPage(SceneLoadScene.page);
                yield return null;
                if (sceneLoadScene == null || sceneLoadScene != _sceneLoadScene) yield break;
                _scrollPending = true;
            }

            if (!_scrollPending) yield break;
            _scrollPending = false;

            sceneLoadScene.GetComponentsInChildren<ScrollRect>().SafeProc(0, r =>
            {
                var scrollPos = 1.0f - Mathf.Clamp(
                    Mathf.Lerp(-0.01f, 1.01f, SceneLoadScene.page / (sceneLoadScene.pageNum - 1f)),
                    0f,
                    1f);
                r.verticalScrollbar.SafeProc(sb => sb.value = scrollPos);
            });
        }

        private void TrackLastLoadedScene()
        {
            if (_currentScenePath.IsNullOrEmpty() || _externalLoadInProgress) return;

            var key = PathUtils.NormalizePath(Path.GetDirectoryName(_currentScenePath));
            if (!LastLoadedScenes.TryGetValue(key, out var current)) current = string.Empty;
            var currentValue = Path.GetFileName(_currentScenePath);
            // these are raw paths, so just simple compare
            if (StringComparer.OrdinalIgnoreCase.Equals(currentValue, current)) return;
            LastLoadedScenes[key] = currentValue;
            SavePending = true;
            UpdateSaveReady();
        }

        /// <summary>Gets the last loaded scene for the current folder</summary>
        /// <returns>`true` if scene found, otherwise `false`</returns>
        private bool TryGetLastLoadedScene(out string lastLoadedScenePath)
        {
            lastLoadedScenePath = GetLastLoadedScene(ScenePaths.LastOrDefault());

            if (lastLoadedScenePath != default)
            {
                lastLoadedScenePath = PathUtils.NormalizePath(Path.IsPathRooted(lastLoadedScenePath)
                    ? lastLoadedScenePath
                    : PathUtils.CombinePaths(_currentSceneFolder, Path.GetFileName(lastLoadedScenePath)));

                if (File.Exists(lastLoadedScenePath))
                {
                    return true;
                }
            }

            return false;
        }

        private string GetLastLoadedScene(string defaultImage = null)
        {
            if (!LastLoadedScenes.TryGetValue(_currentSceneFolder, out var nextImage))
            {
                nextImage = defaultImage.IsNullOrEmpty() ? ScenePaths.FirstOrDefault() : defaultImage;
            }

            return string.IsNullOrEmpty(nextImage)
                ? nextImage
                : PathUtils.NormalizePath(FastCombineNormalizedPaths(_currentSceneFolder, nextImage));
        }
    }
}
