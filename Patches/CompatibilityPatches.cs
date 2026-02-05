using HarmonyLib;
using Iris.Settings;

namespace Iris.Patches
{
    public static class CompatibilityPatches
    {
        [HarmonyPatch(typeof(scnGame))] 
        public static class GamePatch {
            [HarmonyPatch(nameof(scnGame.Play)),HarmonyPrefix]
            public static void Prefix()
            {
                FilterManager.SetPlayState(true);
            }
            
            [HarmonyPatch(nameof(scnGame.ResetScene)),HarmonyPostfix]
            public static void Postfix()
            {
                FilterManager.SetPlayState(false);
            }
        }
        /*
        [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.Play))]
        public static class PlayPatch
        {
            public static void Prefix()
            {
                FilterManager.SetPlayState(true);
            }
        }
        
        [HarmonyPatch(typeof(scnGame), nameof(scnGame.ResetScene))]
        public static class StopPatch
        {
            public static void Postfix()
            {
                FilterManager.SetPlayState(false);
            }
        }
        */
        [HarmonyPatch(typeof(scrController))]
        public static class GameControllerPatch
        {
            [HarmonyPatch("Awake"),HarmonyPostfix]
            public static void Postfix()
            {
                if (ADOBase.controller.gameworld) FilterManager.SetPlayState(true);
            }
            
            [HarmonyPatch(nameof(scrController.OnLandOnPortal)),HarmonyPostfix]
            public static void PostfixLand() 
            {
                FilterManager.SetPlayState(false);
            }
        }
    }
}