using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using GeBoCommon;
using Manager;

namespace GameDressForSuccessPlugin
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(Constants.GameProcessName)]
#if KK
    [BepInProcess(Constants.AltGameProcessName)]
#endif
    public partial class GameDressForSuccess : BaseUnityPlugin
    {
        public const string GUID = "com.gebo.BepInEx.GameDressForSuccess";
        public const string PluginName = "Dress for Success";
        public const string Version = "1.0";

        internal static GameDressForSuccess Instance;

        private bool _monitoringChange;
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<PluginMode> Mode { get; private set; }

        internal void Main()
        {
            Instance = this;
            Enabled = Config.Bind("Settings", "Enabled", true, "Whether the plugin is enabled");
            Mode = Config.Bind("Settings", "Mode", PluginMode.AutomaticOnly,
                "When to apply change when traveling with a girl");
        }

        internal void Awake()
        {
            Instance = this;
            HarmonyWrapper.PatchAll(typeof(Hooks));
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

        private void TravelingStart()
        {
            _monitoringChange = true;
        }

        private void TravelingDone()
        {
            _monitoringChange = false;
        }
    }
}
