using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using GeBoCommon;
using GeBoCommon.Studio;
using GeBoCommon.Utilities;
using HarmonyLib;
using Illusion.Extensions;
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
        public const string GUID = "com.gebo.bepinex.studioscenenavigation";
        public const string PluginName = "Studio Scene Navigation";
        public const string Version = "1.0.2.2";

        private const float SaveDelay = 5f;

        internal static readonly char[] TrackingFileEntrySplit = {'\0'};
        internal static new ManualLogSource Logger;
        private static List<string> _normalizedScenePaths;
        private static string _currentSceneFolder = string.Empty;
        private static readonly object SavePendingLock = new object();
        private static bool _setPage;

        private static SceneLoadScene _sceneLoadScene;

        private static readonly ExpiringSimpleCache<string, string> SceneRelativePathCache =
            new ExpiringSimpleCache<string, string>(CalculateSceneRelativePath, 360);

        private static readonly SimpleLazy<Action<int>> SceneLoadScenePageSetter = new SimpleLazy<Action<int>>(() =>
            Delegates.LazyReflectionSetter<SceneLoadScene, int>("page"));

        private static readonly SimpleLazy<Func<int>> SceneLoadScenePageGetter = new SimpleLazy<Func<int>>(() =>
            Delegates.LazyReflectionGetter<SceneLoadScene, int>("page"));

        private static readonly SimpleLazy<Func<SceneLoadScene, int>> SceneLoadScenePageNumGetter =
            new SimpleLazy<Func<SceneLoadScene, int>>(() =>
                Delegates.LazyReflectionInstanceGetter<SceneLoadScene, int>("pageNum"));


        private static readonly string TrackLastLoadedSceneFile = PathUtils.CombinePaths(
            "BepInEx", "config", StringUtils.JoinStrings(".", GUID, "LastLoadedScene", "data"));

        private static StudioSceneNavigation _instance;

        private readonly SimpleLazy<Func<string, bool>> _isSceneValid;
        private readonly SimpleLazy<Dictionary<string, string>> _lastLoadedScenes;
        private readonly IEnumerator _saveTrackingFileDelay;

        private string _currentScenePath = string.Empty;
        private string _currentScenePathCandidate = string.Empty;

        private bool _navigationInProgress;
        private bool _savePending;
        private float _saveReady;
        private Coroutine _scrollToLastLoadedScene;

        public StudioSceneNavigation()
        {
            _lastLoadedScenes = new SimpleLazy<Dictionary<string, string>>(() =>
                TrackLastLoadedSceneEnabled.Value ? LoadTrackingFile() : new Dictionary<string, string>());

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

            _saveTrackingFileDelay = new WaitUntil(IsSaveReady);
        }

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
            UpdateSaveReady();
            if (e.Operation == SceneOperationKind.Clear)
            {
                _currentScenePath = _currentScenePathCandidate = string.Empty;
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
                coroutines.Add(SaveTrackingFileCouroutine());
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
#if DEBUG
            //Logger.LogDebug( $"ExtendedSave_SceneBeingLoaded({path})");
#endif
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

            var prefix = Path.Combine(Paths.CachePath, Path.GetFileName(TrackLastLoadedSceneFile));
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

        private Dictionary<string, string> LoadTrackingFile()
        {
            var lastLoadedScenes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            var count = 0;
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
                                    entry[0] = PathUtils.NormalizePath(SceneUtils.StudioSceneRootFolder);
                                }
                                else
                                {
                                    entry[0] = PathUtils.NormalizePath(Path.Combine(SceneUtils.StudioSceneRootFolder,
                                        entry[0]));
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

            return lastLoadedScenes;
        }

        private void NavigateScene(int offset)
        {
            var navigated = false;
            if (_navigationInProgress) return;

            _navigationInProgress = true;
            Logger.LogDebug($"Attempting navigate to scene: {offset:+#;-#;0}");
            try
            {
                var paths = NormalizedScenePaths;
                var index = -1;
                if (!_currentScenePath.IsNullOrEmpty() && !paths.IsNullOrEmpty())
                {
                    index = paths.IndexOf(_currentScenePath);
                }

                if (index == -1)
                {
                    if (!paths.IsNullOrEmpty())
                    {
                        Logger.LogDebug(
                            $"Folder changed, resuming navigation for: {PathUtils.GetRelativePath(SceneUtils.StudioSceneRootFolder, _currentSceneFolder)}");
                        if (LoadLastLoadedScene())
                        {
                            navigated = true;
                        }
                    }
                }
                else
                {
                    string nextImage = null;
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

                    var coroutines = GeBoCommon.Utilities.ListPool<IEnumerator>.Get();
                    try
                    {
                        coroutines.Add(CoroutineUtils.CreateCoroutine(
                            () => Logger.LogInfoMessage(
                                $"Loading scene {paths.Count - index}/{paths.Count} ('{PathUtils.GetRelativePath(SceneUtils.StudioSceneRootFolder, nextImage)}')")));
                        coroutines.Add(LoadScene(nextImage));

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

        private static void SetPage(int pageNum, SceneLoadScene sceneLoadScene = null)
        {
            if (sceneLoadScene != null)
            {
                try
                {
                    sceneLoadScene.GetType().GetMethod("SetPage", AccessTools.all)
                        ?.Invoke(sceneLoadScene, new object[] {pageNum});
                    return;
                }
                catch
                {
                    // fall through
                }
            }

            SceneLoadScenePageSetter.Value(pageNum);
        }

        private static int GetPage()
        {
            return SceneLoadScenePageGetter.Value();
        }

        private static int GetNumPages(SceneLoadScene obj)
        {
            return SceneLoadScenePageNumGetter.Value(obj);
        }

        private static IEnumerator SetPageCoroutine(string scenePath)
        {
            if (!_setPage) yield break;

            _setPage = false;
            yield return null;
            var page = GetLoaderPageForImage(scenePath);
            if (page < 0) yield break;
            SetPage(page);
        }

        public IEnumerator SaveTrackingFileCouroutine()
        {
            if (!SavePending || !TrackLastLoadedSceneEnabled.Value) yield break;
            yield return _saveTrackingFileDelay;
            if (SavePending && TrackLastLoadedSceneEnabled.Value) SaveTrackingFile();
        }

        internal void ScrollToLastLoadedScene(SceneLoadScene sceneLoadScene)
        {
            if (!RestoreLoaderPage.Value || sceneLoadScene == null) return;
            if (_scrollToLastLoadedScene != null) StopCoroutine(_scrollToLastLoadedScene);
            _scrollToLastLoadedScene = StartCoroutine(ScrollToLastLoadedSceneCoroutine(sceneLoadScene)
                .AppendCo(() => _scrollToLastLoadedScene = null));
        }

        public static int GetLoaderPageForImage(string sceneFile)
        {
            if (sceneFile.IsNullOrEmpty() || NormalizedScenePaths.Count < 1) return -2;
            var index = NormalizedScenePaths.IndexOf(sceneFile);
            if (index < 0) return index;

            return index / ImagesPerPage;
        }

        internal IEnumerator ScrollToLastLoadedSceneCoroutine(SceneLoadScene sceneLoadScene)
        {
            yield return null;
            if (sceneLoadScene == null) yield break;
            var lastLoadedScene = GetLastLoadedScene();
            var page = Math.Max(0, GetLoaderPageForImage(lastLoadedScene));

            SetPage(page, sceneLoadScene);

            if (sceneLoadScene == null || page == 0) yield break;


            sceneLoadScene.SafeProc(sls => sls.GetComponentsInChildren<ScrollRect>().SafeProc(0, r =>
            {
                var buttonHeight = -1f;
                var scrollBarHeight = -1f;


                r.GetComponentsInChildren<Button>().SafeProc(0,
                    b => b.GetComponent<RectTransform>().SafeProc(rt => buttonHeight = rt.rect.height));
                r.verticalScrollbar.SafeProc(
                    sb => sb.GetComponent<RectTransform>().SafeProc(rt => scrollBarHeight = rt.rect.height));
                float scrollPos;
                if (buttonHeight > 0f && scrollBarHeight > 0f)
                {
                    scrollPos = 1.0f - Mathf.Max(0f, (buttonHeight * (page - 1)) - scrollBarHeight);
                }
                else
                {
                    scrollPos = 1.0f - ((GetPage() + 1f) / GetNumPages(sls));
                }

                r.verticalScrollbar.SafeProc(sb => sb.value = scrollPos);
            }));
        }

        private void TrackLastLoadedScene()
        {
            if (_currentScenePath.IsNullOrEmpty()) return;
            var key = PathUtils.NormalizePath(Path.GetDirectoryName(_currentScenePath));
            if (!LastLoadedScenes.TryGetValue(key, out var current)) current = string.Empty;
            var currentValue = Path.GetFileName(_currentScenePath);
            if (currentValue.Compare(current, StringComparison.InvariantCultureIgnoreCase)) return;
            LastLoadedScenes[key] = currentValue;
            SavePending = true;
            UpdateSaveReady();
        }

        /// <summary>Loads the last loaded scene from the currently active folder</summary>
        /// <returns>`true` if scene was loaded, otherwise `false`</returns>
        private bool LoadLastLoadedScene()
        {
            var navigated = false;
            var clearNavigation = !_navigationInProgress;
            _navigationInProgress = true;
            try
            {
                var nextImage = GetLastLoadedScene(ScenePaths.LastOrDefault());

                if (nextImage != default)
                {
                    nextImage = PathUtils.NormalizePath(Path.Combine(_currentSceneFolder, nextImage));

                    if (File.Exists(nextImage))
                    {
                        _currentScenePathCandidate = nextImage;
                        StartCoroutine(Singleton<Studio.Studio>.Instance.LoadSceneCoroutine(nextImage));
                        navigated = true;
                    }
                }
            }
            finally
            {
                if (!navigated)
                {
                    Logger.LogErrorMessage(
                        $"Error loading last scene from {_currentSceneFolder}");
                    if (clearNavigation)
                    {
                        _navigationInProgress = false;
                    }
                }
            }

            return navigated;
        }

        private string GetLastLoadedScene(string defaultImage = null)
        {
            if (!LastLoadedScenes.TryGetValue(_currentSceneFolder, out var nextImage))
            {
                nextImage = defaultImage.IsNullOrEmpty() ? ScenePaths.FirstOrDefault() : defaultImage;
            }

            return string.IsNullOrEmpty(nextImage)
                ? nextImage
                : PathUtils.NormalizePath(Path.Combine(_currentSceneFolder, nextImage));
        }
    }
}
