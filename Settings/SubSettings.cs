using System.Collections.Generic;

namespace Iris.Settings
{
    public class IrisSettings
    {
        public bool enableFilters = true;
        // 已启用的 Shader 列表
        public List<string> enabledFilters = new();
        
        public bool enableTestMode = false;
        // 参数配置（预留给不同 Shader 的滑动条等）
        public Dictionary<string, Dictionary<string, float>> filterConfigs = new();
    }

    public class UISettings
    {
        // 保持 M3 UI 逻辑所需的最小化配置
        public bool showWatermark = true;
    }
}