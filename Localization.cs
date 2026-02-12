using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Iris
{
    public static class Localization
    {
        private static readonly Dictionary<string, Dictionary<string, string>> languages = [];
        private static readonly Dictionary<string, string> languageDisplayNames = [];
        private static bool loaded = false;
        private static readonly List<string> _availableLanguages = [];
        public static List<string> AvailableLanguages
        {
            get
            {
                if (!loaded) Load();
                return _availableLanguages;
            }
        }

        public static string Get(string key)
        {
            if (!loaded) Load();
            string lang = Main.settings?.language ?? "en";
            if (languages.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out string val))
            {
                return val;
            }
            if (languages.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out string enVal))
            {
                return enVal;
            }
            return key;
        }

        public static string GetDisplayName(string langId)
        {
            if (!loaded) Load();
            return languageDisplayNames.TryGetValue(langId, out string name) ? name : langId;
        }

        public static void Load()
        {
            try
            {
                languages.Clear();
                languageDisplayNames.Clear();
                _availableLanguages.Clear();
                string langDir = Path.Combine(ResourceLoader.ResourcesPath, "lang");
                
                if (Directory.Exists(langDir))
                {
                    string[] files = Directory.GetFiles(langDir, "*.json");
                    foreach (string file in files)
                    {
                        try
                        {
                            string langId = Path.GetFileNameWithoutExtension(file);
                            string json = File.ReadAllText(file);
                            var dict = ParseJson(json);
                            if (dict.Count > 0)
                            {
                                languages[langId] = dict;
                                _availableLanguages.Add(langId);
                                
                                if (dict.TryGetValue("displayName", out string displayName))
                                {
                                    languageDisplayNames[langId] = displayName;
                                }
                                else
                                {
                                    languageDisplayNames[langId] = langId;
                                }

                                Main.Mod?.Logger.Log($"Loaded language: {langId} ({languageDisplayNames[langId]}) - {dict.Count} keys");
                            }
                        }
                        catch (Exception ex)
                        {
                            Main.Mod?.Logger.Error($"Failed to load language file {file}: {ex.Message}");
                        }
                    }
                }

                if (languages.Count == 0)
                {
                    Main.Mod?.Logger.Warning("No language files found, using empty 'en' fallback.");
                    languages["en"] = [];
                    languageDisplayNames["en"] = "English";
                    _availableLanguages.Add("en");
                }
            }
            catch (Exception ex)
            {
                Main.Mod?.Logger.Error($"Critical error in Localization.Load: {ex.Message}");
                if (languages.Count == 0) languages["en"] = [];
            }
            finally
            {
                loaded = true;
            }
        }

        private static Dictionary<string, string> ParseJson(string json)
        {
            var dict = new Dictionary<string, string>();
            var matches = Regex.Matches(json, "\"([^\"]+)\"\\s*:\\s*\"([^\"]+)\"");
            foreach (Match match in matches)
            {
                if (match.Groups.Count == 3)
                {
                    string key = match.Groups[1].Value;
                    string value = match.Groups[2].Value;
                    value = value.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t");
                    dict[key] = value;
                }
            }
            return dict;
        }

        public static string Get(string key, params object[] args)
        {
            try
            {
                string format = Get(key);
                return string.Format(format, args);
            }
            catch (Exception ex)
            {
                Main.Mod?.Logger.Error($"Localization.Get format error for key {key}: {ex.Message}");
                return key;
            }
        }
    }
}
