using System.Reflection;
using HarmonyLib;
using Iris.Patches;
using Iris.Settings;
using UnityEngine;
using UnityModManagerNet;

namespace Iris
{
    public static class Main
    {
        public static UnityModManager.ModEntry? Mod { get; private set; }
        public static Harmony? Harmony { get; private set; }
        public static Config config { get; private set; } = null!;
        public static UnityModManager.ModEntry.ModLogger? Logger;
        private static int _mainThreadId;

        public static bool IsMainThread => System.Threading.Thread.CurrentThread.ManagedThreadId == _mainThreadId;
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            Mod = modEntry;
            Logger = Mod.Logger;
            config = UnityModManager.ModSettings.Load<Config>(modEntry);
            Localization.Load();
            
            KeyviewerManager.LoadConfigs();
            // 同步保存的状态到 Manager
            foreach (var kv in KeyviewerManager.LoadedConfigs)
            {
                if (config.filters.kvEnabledStates.TryGetValue(kv.Name, out bool enabled))
                {
                    KeyviewerManager.SetEnabled(kv.Name, enabled);
                }
            }
            
            FilterManager.ScanFilters();
            
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = config.OnGUI;
            modEntry.OnSaveGUI = config.Save;
            
            Harmony = new Harmony(modEntry.Info.Id);
            
            modEntry.Logger.Log(Localization.Get("ModLoaded", config.language));
            return true;
        }

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float dt)
        {
            KeyviewerManager.UpdateListener();
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value)
            {
                modEntry.Logger.Log(Localization.Get("ModEnabled"));
                Harmony?.PatchAll(Assembly.GetExecutingAssembly());
                modEntry.OnUpdate = OnUpdate;
            }
            else
            {
                modEntry.Logger.Log(Localization.Get("ModDisabled"));
                FilterManager.SetPlayState(false);
                Harmony?.UnpatchAll(modEntry.Info.Id);
                modEntry.OnUpdate = null;
            }
            return true;
        }
    }
}
