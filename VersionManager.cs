using System;

namespace Iris
{
    public enum VersionType
    {
        Hotfix,
        Release,
        Beta,
        Prerelease
    }

    public static class VersionManager
    {
        public static VersionType Type => VersionType.Beta;
        public const int MinorVersion = 2;

        public static string GetFullVersionString()
        {
            string baseVersion = Main.Mod?.Info.Version ?? "0.1.0";
            if (Type == VersionType.Release)
            {
                return baseVersion;
            }
            return $"{baseVersion}-{Type.ToString().ToLower()}{MinorVersion}";
        }
    }
}
