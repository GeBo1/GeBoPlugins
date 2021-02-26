using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChaCustom;
using GeBoCommon;
using GeBoCommon.Utilities;
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
        public const string Version = "1.2.0";

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
            GameAPI.RegisterExtraBehaviour<DressForSuccessController>(GUID);
        }

        internal void DressPlayer(ChaFileDefine.CoordinateType newCoordinateType)
        {
            if (!_monitoringChange || !Game.IsInstance() || Game.Instance == null) return;

            var player = Game.Instance.Player;
            if (player == null) return;


            var mode = Mode.Value;

            var playerClothesIsAuto = player.changeClothesType < 0;
            var playerShouldChange = playerClothesIsAuto || mode == PluginMode.Always;
            Logger.DebugLogDebug(
                $"{nameof(DressPlayer)}: mode={mode}, playerClothesIsAuto={playerClothesIsAuto}, playerShouldChange={playerShouldChange}");

            if (!playerShouldChange) return;

            player.chaCtrl.SafeProc(cc =>
            {
                if (!cc.ChangeCoordinateTypeAndReload(newCoordinateType)) return;
                Logger.LogDebug($"Changed player clothes to {newCoordinateType}");
                if (!playerClothesIsAuto)
                {
                    // update selected clothes to match so you can change back
                    player.changeClothesType = (int)newCoordinateType;
                }
            });

            _monitoringChange = false;
        }

        private void TravelingStart(SaveData.Heroine heroine)
        {
            // don't run on NPC chars            
            _initialCoordinateType = heroine.IsNullOrNpc() ? -1 : heroine.GetCoordinateType();
            _monitoringChange = _initialCoordinateType != -1;
        }

        private void TravelingDone(SaveData.Heroine heroine)
        {
            if (_monitoringChange && _initialCoordinateType != -1 && !heroine.IsNullOrNpc())
            {
                var currentCoordinateType = heroine.GetCoordinateType();
                if (currentCoordinateType != -1 && currentCoordinateType != _initialCoordinateType)
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
                Game.Instance.SafeProc(i => i.Player.SafeProc(p => p.changeClothesType = -1));
            }

            if (CustomBase.IsInstance())
            {
                CustomBase.Instance.SafeProc(i => i.autoClothesState = true);
            }
        }
    }
}
