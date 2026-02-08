using System.Collections.Generic;

namespace Iris.Settings
{
    public class IrisSettings
    {
        public bool enableFilters = true;
        
        public bool enablePosterize = false;
        public float posterizeDistortion = 64.0f;

        public bool enableVideoBloom = false;
        public float videoBloomAmount = 1.0f;
        public float videoBloomThreshold = 0.5f;

        // 启用的外部 Shader 列表（仅名称）
        public List<string> enabledFilters = new();

        // 外部 Shader 的配置列表
        // 当用户在 UI 中启用某个扫描到的 Shader 时，会在此列表中添加一项
        public List<FilterConfig> filterConfigs = new();
    }

    public class FilterConfig
    {
        public string name = "";
        public List<FilterParam> paramsList = new();
    }

    public class FilterParam
    {
        public string name = "";
        public float value = 0f;
    }

    public class UISettings
    {
        // 保持 M3 UI 逻辑所需的最小化配置
        public bool showWatermark = true;
    }
}