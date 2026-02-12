using HarmonyLib;
using Iris.Patches;
using Iris.Settings;
using UnityModManagerNet;

namespace Iris
{
    public static class Main
    {
        public static UnityModManager.ModEntry? Mod { get; private set; }
        public static Harmony? Harmony { get; private set; }
        public static Config settings { get; private set; } = null!;
        public static UnityModManager.ModEntry.ModLogger? Logger;
        private static int _mainThreadId;

        public static bool IsMainThread => System.Threading.Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            Mod = modEntry;
            Logger = Mod.Logger;
            settings = UnityModManager.ModSettings.Load<Config>(modEntry);

            FilterManager.ScanFilters();

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = settings.OnGUI;
            modEntry.OnSaveGUI = settings.Save;
            modEntry.OnUpdate = OnUpdate;

            Harmony = new Harmony(modEntry.Info.Id);

            modEntry.Logger.Log($"Iris loaded: {VersionManager.GetFullVersionString()}");
            return true;
        }

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float dt)
        {
            bool needAppearanceRuntime = settings.appearance.enableMenuSkin || settings.appearance.enableTrackCustomization;
            if (needAppearanceRuntime)
            {
                AppearancePatches.OnUpdate(dt);
            }
            else
            {
                AppearancePatches.Disable();
            }
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value)
            {
                modEntry.Logger.Log("Iris enabled");
                PatchManager.ApplyAllPatches();
                PatchManager.UpdateAllPatches();
            }
            else
            {
                modEntry.Logger.Log("Iris disabled");
                FilterManager.SetPlayState(false);
                AppearancePatches.Disable();
                PatchManager.UnpatchAll();
            }

            return true;
        }
    }
}