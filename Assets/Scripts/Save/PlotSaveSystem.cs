using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Client.Terrain;
using UnityEngine;

namespace Client.Save
{
    public static class PlotSaveSystem
    {
        [Serializable]
        private class PlotSavePayload
        {
            public BuildPlotDefinition[] plots;
        }

        private const string FileName = "plots.json";

        public static BuildPlotDefinition[] LoadPlots(string realmId, string characterId)
        {
            try
            {
                var key = BuildPrefsKey(realmId, characterId);
                if (PlayerPrefs.HasKey(key))
                {
                    var payloadJson = PlayerPrefs.GetString(key);
                    if (!string.IsNullOrWhiteSpace(payloadJson))
                    {
                        var payload = JsonUtility.FromJson<PlotSavePayload>(payloadJson);
                        if (payload?.plots != null)
                        {
                            return payload.plots.Where(p => p != null).ToArray();
                        }
                    }
                }

                var filePath = BuildFilePath(realmId, characterId);
                if (!File.Exists(filePath))
                {
                    return Array.Empty<BuildPlotDefinition>();
                }

                var fileJson = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(fileJson))
                {
                    return Array.Empty<BuildPlotDefinition>();
                }

                var filePayload = JsonUtility.FromJson<PlotSavePayload>(fileJson);
                return filePayload?.plots?.Where(p => p != null).ToArray() ?? Array.Empty<BuildPlotDefinition>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load plot definitions: {ex}");
                return Array.Empty<BuildPlotDefinition>();
            }
        }

        public static void SavePlots(string realmId, string characterId, IEnumerable<BuildPlotDefinition> definitions)
        {
            try
            {
                if (definitions == null)
                {
                    return;
                }

                var payload = new PlotSavePayload
                {
                    plots = definitions.Where(d => d != null).Select(d => new BuildPlotDefinition(d)).ToArray()
                };

                var json = JsonUtility.ToJson(payload);
                var key = BuildPrefsKey(realmId, characterId);
                PlayerPrefs.SetString(key, json);
                PlayerPrefs.Save();

                var filePath = BuildFilePath(realmId, characterId);
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save plot definitions: {ex}");
            }
        }

        private static string BuildPrefsKey(string realmId, string characterId)
        {
            return $"PlotData::{Sanitize(realmId)}::{Sanitize(characterId)}";
        }

        private static string BuildFilePath(string realmId, string characterId)
        {
            var directory = Path.Combine(Application.persistentDataPath, "plots");
            var file = $"{Sanitize(realmId)}_{Sanitize(characterId)}_{FileName}";
            return Path.Combine(directory, file);
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "global";
            }

            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value.Trim();
        }
    }
}
