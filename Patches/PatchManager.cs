using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Iris.Settings;

namespace Iris.Patches
{
    public static class PatchManager
    {
        private static Harmony _harmony => Main.Harmony!;
        
        // Status
        private static readonly Dictionary<Type, bool> _activePatches = new();
        // Optimization: Cache exact patch bindings for each patch class to speed up and isolate unpatching
        private static readonly Dictionary<Type, List<(MethodBase Original, MethodInfo PatchMethod)>> _patchedBindings = new();
        
        // Patch Declaration
        private class PatchDef
        {
            public Type Type;
            public Func<bool> Condition;
            public Type? Parent;
            public string Name;

            public PatchDef(Type type, Func<bool> condition, Type? parent = null)
            {
                Type = type;
                Condition = condition;
                Parent = parent;
                Name = type.Name;
            }
        }

        private static readonly List<PatchDef> _definitions = new();

        static PatchManager()
        {
            RegisterPatches();
        }

        private static void RegisterPatches()
        {
            _definitions.Clear();

            // --- Appearance ---
            var appCond = () => Main.settings.appearance.enableMenuSkin || Main.settings.appearance.enableTrackCustomization;
            _definitions.Add(new PatchDef(typeof(AppearancePatches), appCond));
            _definitions.Add(new PatchDef(typeof(AppearancePatches.FloorStartPatch), appCond, typeof(AppearancePatches)));
            _definitions.Add(new PatchDef(typeof(AppearancePatches.FloorRefreshColorPatch), appCond, typeof(AppearancePatches)));
            _definitions.Add(new PatchDef(typeof(AppearancePatches.EditorFloorUpdatePatch), appCond, typeof(AppearancePatches)));

            // --- Filter Runtime ---
            _definitions.Add(new PatchDef(typeof(CompatibilityPatches), () => true));
        }

        /// <summary>
        /// Apply all registered patches regardless of conditions.
        /// Used for "Load All, Unload as Needed" strategy.
        /// </summary>
        public static void ApplyAllPatches()
        {
            if (_harmony == null) return;
            
            Main.Logger?.Log("[PatchManager] Loading all patches (Initial phase)...");
            foreach (var def in _definitions)
            {
                _activePatches.TryGetValue(def.Type, out bool currentActive);
                if (!currentActive)
                {
                    ApplyPatch(def.Type);
                }
            }
        }

        public static void UpdateAllPatches()
        {
            if (_harmony == null) return;

            bool changed = true;
            int iterations = 0;
            while (changed && iterations < 10) // Safety limit
            {
                changed = false;
                iterations++;
                foreach (var def in _definitions)
                {
                    bool shouldBeActive = CalculateEffectiveStatus(def);
                    bool trackedActive = _activePatches.TryGetValue(def.Type, out bool currentActive) && currentActive;
                    bool actualActive = IsActuallyPatched(def.Type);
                    bool effectiveCurrent = trackedActive || actualActive;

                    if (effectiveCurrent != shouldBeActive)
                    {
                        if (shouldBeActive) ApplyPatch(def.Type);
                        else RemovePatch(def.Type);
                        changed = true; // Continue Loop
                    }

                    _activePatches[def.Type] = shouldBeActive;
                }
            }
        }

        private static bool CalculateEffectiveStatus(PatchDef def)
        {
            // Condition
            if (!def.Condition()) return false;
            
            // Check Parent
            if (def.Parent != null)
            {
                _activePatches.TryGetValue(def.Parent, out bool parentActive);
                if (!parentActive) return false;
            }

            return true;
        }

        private static bool IsActuallyPatched(Type patchClass)
        {
            var allPatchedMethods = _harmony.GetPatchedMethods();
            foreach (var original in allPatchedMethods)
            {
                var info = Harmony.GetPatchInfo(original);
                if (info == null) continue;

                bool matched = info.Prefixes.Any(p => p.owner == _harmony.Id && p.PatchMethod.DeclaringType == patchClass) ||
                               info.Postfixes.Any(p => p.owner == _harmony.Id && p.PatchMethod.DeclaringType == patchClass) ||
                               info.Transpilers.Any(p => p.owner == _harmony.Id && p.PatchMethod.DeclaringType == patchClass) ||
                               info.Finalizers.Any(p => p.owner == _harmony.Id && p.PatchMethod.DeclaringType == patchClass);
                if (matched)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ApplyPatch(Type type)
        {
            try
            {
                var processor = _harmony.CreateClassProcessor(type);
                var originals = processor.Patch();

                if (originals != null && originals.Count > 0)
                {
                    var bindings = new List<(MethodBase Original, MethodInfo PatchMethod)>();
                    foreach (var original in originals)
                    {
                        var info = Harmony.GetPatchInfo(original);
                        if (info == null) continue;

                        foreach (var p in info.Prefixes)
                        {
                            if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == type)
                                bindings.Add((original, p.PatchMethod));
                        }
                        foreach (var p in info.Postfixes)
                        {
                            if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == type)
                                bindings.Add((original, p.PatchMethod));
                        }
                        foreach (var p in info.Transpilers)
                        {
                            if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == type)
                                bindings.Add((original, p.PatchMethod));
                        }
                        foreach (var p in info.Finalizers)
                        {
                            if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == type)
                                bindings.Add((original, p.PatchMethod));
                        }
                    }

                    _patchedBindings[type] = bindings;
                    _activePatches[type] = true;
                    Main.Logger?.Log($"[PatchManager] Applied {type.Name} ({bindings.Count} patch bindings)");
                }
            }
            catch (Exception e)
            {
                Main.Logger?.Error($"[PatchManager] Failed to apply {type.Name}: {e}");
            }
        }

        private static void RemovePatch(Type type)
        {
            try
            {
                if (_patchedBindings.TryGetValue(type, out var bindings) && bindings.Count > 0)
                {
                    foreach (var (original, patchMethod) in bindings)
                    {
                        _harmony.Unpatch(original, patchMethod);
                    }
                    _patchedBindings.Remove(type);
                }
                else
                {
                    // Fallback to slow method if cache is missing or empty
                    UnpatchMethod(type);
                    _patchedBindings.Remove(type);
                }
                
                _activePatches[type] = false;
                Main.Logger?.Log($"[PatchManager] Removed {type.Name}");
            }
            catch (Exception e)
            {
                Main.Logger?.Error($"[PatchManager] Failed to remove {type.Name}: {e}");
            }
        }

        private static void UnpatchMethod(Type patchClass)
        {
            // Slow fallback: search all patched methods in the game
            var allPatchedMethods = _harmony.GetPatchedMethods();
            foreach (var original in allPatchedMethods)
            {
                var info = Harmony.GetPatchInfo(original);
                if (info == null) continue;

                foreach (var p in info.Prefixes)
                {
                    if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == patchClass)
                        _harmony.Unpatch(original, p.PatchMethod);
                }
                foreach (var p in info.Postfixes)
                {
                    if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == patchClass)
                        _harmony.Unpatch(original, p.PatchMethod);
                }
                foreach (var p in info.Transpilers)
                {
                    if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == patchClass)
                        _harmony.Unpatch(original, p.PatchMethod);
                }
                foreach (var p in info.Finalizers)
                {
                    if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == patchClass)
                        _harmony.Unpatch(original, p.PatchMethod);
                }
            }
        }

        public static void UnpatchAll()
        {
            _harmony?.UnpatchAll(_harmony.Id);
            _activePatches.Clear();
            _patchedBindings.Clear();
            Main.Logger?.Log("[PatchManager] Unpatched all");
        }
    }
}