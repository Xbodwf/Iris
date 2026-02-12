using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

namespace Iris.Internal
{
    /// <summary>
    /// 全局输入 Hook 管理器
    /// 优先拦截 Unity 层的输入，提供比 Input.GetKey 更稳定的检测
    /// </summary>
    public static class InputHookManager
    {
        private static readonly HashSet<KeyCode> _pressedKeys = new();
        private static readonly KeyCode[] _allKeyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));
        public static event Action<KeyCode, bool>? OnKeyStateChanged;

        public static bool GetKey(KeyCode keyCode) => _pressedKeys.Contains(keyCode);

        public static void SetKeyState(KeyCode keyCode, bool isPressed)
        {
            bool changed = isPressed ? _pressedKeys.Add(keyCode) : _pressedKeys.Remove(keyCode);
            if (changed)
            {
                OnKeyStateChanged?.Invoke(keyCode, isPressed);
            }
        }

        // 用于在配置界面快速监听
        public static KeyCode LastKeyDown { get; private set; } = KeyCode.None;
        public static void ClearLastKey() => LastKeyDown = KeyCode.None;

        [HarmonyPatch(typeof(Input), "GetKeyDown", typeof(KeyCode))]
        [HarmonyPostfix]
        private static void Postfix_GetKeyDown(KeyCode key, bool __result)
        {
            if (__result)
            {
                LastKeyDown = key;
                SetKeyState(key, true);
            }
        }

        [HarmonyPatch(typeof(Input), "GetKeyUp", typeof(KeyCode))]
        [HarmonyPostfix]
        private static void Postfix_GetKeyUp(KeyCode key, bool __result)
        {
            if (__result) SetKeyState(key, false);
        }

        // 某些按键可能通过 GetKey 连续触发逻辑，这里做一个全局状态同步
        public static void Update()
        {
            // 实时检测按下，用于配置界面监听
            if (Input.anyKeyDown)
            {
                foreach (KeyCode kcode in _allKeyCodes)
                {
                    if (kcode >= KeyCode.JoystickButton0) break; // 优化：跳过手柄按键
                    if (Input.GetKeyDown(kcode))
                    {
                        LastKeyDown = kcode;
                        SetKeyState(kcode, true);
                        break;
                    }
                }
            }

            // 对于没有触发 GetKeyUp 的异常情况，可以通过 Input.anyKey 辅助清理（可选）
            if (!Input.anyKey && _pressedKeys.Count > 0)
            {
                var keys = new List<KeyCode>(_pressedKeys);
                foreach (var k in keys) SetKeyState(k, false);
            }
        }
    }
}
