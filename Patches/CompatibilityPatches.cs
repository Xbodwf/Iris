using HarmonyLib;
using Iris.Settings;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Iris.Patches
{
    public static class CompatibilityPatches
    {
        [HarmonyPatch(typeof(scrController), "Update")]
        public static class ControllerPlayStatePatch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                
                // 在 Update 方法的最开始插入我们的检测逻辑
                var injection = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0), // 加载 scrController 实例 (this)
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ControllerPlayStatePatch), nameof(CheckState)))
                };
                
                codes.InsertRange(0, injection);
                return codes;
            }

            public static void CheckState(scrController __instance)
            {
                if (__instance.gameworld)
                {
                        FilterManager.SetPlayState(true);
                }
                else
                {
                    FilterManager.SetPlayState(false);
                }
            }
        }
    }
}