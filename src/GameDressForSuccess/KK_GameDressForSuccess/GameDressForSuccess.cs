using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChaCustom;
using GeBoCommon;
using HarmonyLib;
using KKAPI;
using KKAPI.MainGame;
using Manager;

namespace GameDressForSuccessPlugin
{
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInProcess(Constants.MainGameProcessNameSteam)]
    [BepInProcess(Constants.MainGameProcessNameVR)]
    [BepInProcess(Constants.MainGameProcessNameVRSteam)]
    public partial class GameDressForSuccess : BaseUnityPlugin
    {
        public const string GUID = "com.gebo.BepInEx.GameDressForSuccess";
        public const string PluginName = "Dress for Success";
        public const string Version = "1.1.1";

        internal static GameDressForSuccess Instance;
        private int _initialCoordinateType = -1;

        private bool _monitoringChange;
        internal new ManualLogSource Logger;

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<PluginMode> Mode { get; private set; }

        public static ConfigEntry<ResetToAutomaticMode> ResetToAutomatic { get; private set; }

        internal void Main()
        {
            Instance = this;
            Logger = base.Logger;
            Enabled = Config.Bind("Settings", "Enabled", true, "Whether the plugin is enabled");
            Mode = Config.Bind("Settings", "Mode", PluginMode.AutomaticOnly,
                "When to apply change when traveling with a girl");
            ResetToAutomatic = Config.Bind("Settings", "Reset to Automatic", ResetToAutomaticMode.PeriodChange,
                "When to reset the players dress state to 'Automatic'");
        }

        internal void Awake()
        {
            Instance = this;
            Logger = base.Logger;
            Harmony.CreateAndPatchAll(typeof(Hooks));
            GameAPI.RegisterExtraBehaviour<GameController>(GUID);
        }


        public static SaveData.Heroine GetTargetHeroine()
        {
            var advScene = Singleton<Game>.Instance?.actScene?.AdvScene;

            return advScene?.Scenario?.currentHeroine ??
                   (advScene?.nowScene as TalkScene)?.targetHeroine ??
                   FindObjectOfType<TalkScene>()?.targetHeroine;
        }

        internal void DressPlayer(ChaFileDefine.CoordinateType newCoordinateType)
        {
            if (!_monitoringChange) return;

            var player = Singleton<Game>.Instance?.Player;

            if (player == null) return;

            var mode = Mode.Value;

            var playerClothesIsAuto = player.changeClothesType < 0;

            if (mode != PluginMode.Always || (mode == PluginMode.AutomaticOnly && !playerClothesIsAuto)) return;

            if (player.chaCtrl.ChangeCoordinateTypeAndReload(newCoordinateType))
            {
                Logger.LogDebug($"Changed player clothes to {newCoordinateType}");
                if (!playerClothesIsAuto)
                {
                    // update selected clothes to match so you can change back
                    player.changeClothesType = (int)newCoordinateType;
                }
            }

            _monitoringChange = false;
        }

        private void TravelingStart(SaveData.Heroine heroine)
        {
            if (heroine == null) return;
            _monitoringChange = true;
            heroine.chaCtrl.SafeProc(
                cc => cc.chaFile.SafeProc(
                    cf => cf.status.SafeProc(
                        s => s.coordinateType.SafeProc(
                            c => _initialCoordinateType = c))));
        }

        private void TravelingDone(SaveData.Heroine heroine)
        {
            if (!_monitoringChange)
            {
                _initialCoordinateType = -1;
            }

            if (_initialCoordinateType != -1 && heroine != null)
            {
                var currentCoordinateType = _initialCoordinateType;
                heroine.chaCtrl.SafeProc(
                    cc => cc.chaFile.SafeProc(
                        cf => cf.status.SafeProc(
                            s => s.coordinateType.SafeProc(
                                c => currentCoordinateType = c))));
                if (currentCoordinateType != _initialCoordinateType)
                {
                    DressPlayer((ChaFileDefine.CoordinateType)currentCoordinateType);
                }
            }

            _initialCoordinateType = -1;
            _monitoringChange = false;
        }

        internal void SetPlayerClothesToAutomatic()
        {
            if (Game.IsInstance())
            {
                var player = Singleton<Game>.Instance.Player;
                if (player != null)
                {
                    player.changeClothesType = -1;
                }
            }

            if (CustomBase.IsInstance())
            {
                var customBase = Singleton<CustomBase>.Instance;
                if (customBase != null)
                {
                    customBase.autoClothesState = true;
                }
            }
        }
    }
}
