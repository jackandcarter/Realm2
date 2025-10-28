using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Realm.Data;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor.Configuration
{
    public class ConfigurationAssetPersistenceProcessor : AssetModificationProcessor
    {
        private const string SnapshotSuffix = ".snapshot.json";
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static readonly HashSet<char> InvalidFileNameCharacters = new HashSet<char>(Path.GetInvalidFileNameChars());

        public static string[] OnWillSaveAssets(string[] paths)
        {
            if (paths == null || paths.Length == 0)
            {
                return paths;
            }

            foreach (var path in paths)
            {
                try
                {
                    if (!TryGetConfigurationAssets(path, out var configurationAssets))
                    {
                        continue;
                    }

                    GenerateSnapshots(path, configurationAssets);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to generate configuration snapshot for '{path}'.\n{ex}");
                }
            }

            return paths;
        }

        public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            try
            {
                if (TryGetConfigurationAssets(assetPath, out _))
                {
                    DeleteSnapshots(assetPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to clean configuration snapshots for '{assetPath}'.\n{ex}");
            }

            return AssetDeleteResult.DidNotDelete;
        }

        private static bool TryGetConfigurationAssets(string assetPath, out List<ConfigurationAsset> assets)
        {
            assets = null;

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            var loaded = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (loaded == null || loaded.Length == 0)
            {
                return false;
            }

            var configurations = new List<ConfigurationAsset>();
            for (var i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] is ConfigurationAsset configuration)
                {
                    configurations.Add(configuration);
                }
            }

            if (configurations.Count == 0)
            {
                return false;
            }

            assets = configurations;
            return true;
        }

        private static void GenerateSnapshots(string assetPath, List<ConfigurationAsset> assets)
        {
            var expectedSnapshots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var includeObjectName = assets.Count > 1;

            for (var i = 0; i < assets.Count; i++)
            {
                var asset = assets[i];
                if (asset == null)
                {
                    continue;
                }

                var snapshotRelativePath = BuildSnapshotRelativePath(assetPath, asset, includeObjectName, nameCounts);
                var snapshotFullPath = ToAbsolutePath(snapshotRelativePath);
                var directory = Path.GetDirectoryName(snapshotFullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonUtility.ToJson(asset, true) ?? "{}";
                File.WriteAllText(snapshotFullPath, json + Environment.NewLine, Encoding.UTF8);
                expectedSnapshots.Add(Path.GetFullPath(snapshotFullPath));
            }

            RemoveStaleSnapshots(assetPath, expectedSnapshots);
        }

        private static string BuildSnapshotRelativePath(string assetPath, ConfigurationAsset asset, bool includeObjectName, Dictionary<string, int> nameCounts)
        {
            var directory = Path.GetDirectoryName(assetPath) ?? string.Empty;
            var fileName = Path.GetFileName(assetPath);
            var suffix = string.Empty;

            if (includeObjectName || !AssetDatabase.IsMainAsset(asset))
            {
                var sanitizedName = SanitizeName(asset != null ? asset.name : string.Empty);
                var uniqueName = ReserveUniqueName(sanitizedName, nameCounts);
                suffix = $".{uniqueName}";
            }

            var snapshotFileName = $"{fileName}{suffix}{SnapshotSuffix}";
            var combined = string.IsNullOrEmpty(directory)
                ? snapshotFileName
                : Path.Combine(directory, snapshotFileName);

            return combined.Replace('\\', '/');
        }

        private static string ReserveUniqueName(string baseName, Dictionary<string, int> counts)
        {
            if (string.IsNullOrEmpty(baseName))
            {
                baseName = "Unnamed";
            }

            if (!counts.ContainsKey(baseName))
            {
                counts[baseName] = 1;
                return baseName;
            }

            var index = counts[baseName];
            counts[baseName] = index + 1;
            return $"{baseName}_{index}";
        }

        private static string SanitizeName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "Unnamed";
            }

            var builder = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                builder.Append(InvalidFileNameCharacters.Contains(ch) ? '_' : ch);
            }

            return builder.Length == 0 ? "Unnamed" : builder.ToString();
        }

        private static void RemoveStaleSnapshots(string assetPath, HashSet<string> expectedSnapshots)
        {
            var directory = Path.GetDirectoryName(assetPath) ?? string.Empty;
            var directoryFullPath = ToAbsolutePath(directory);
            if (string.IsNullOrEmpty(directoryFullPath) || !Directory.Exists(directoryFullPath))
            {
                return;
            }

            var filePattern = Path.GetFileName(assetPath) + "*" + SnapshotSuffix;
            foreach (var file in Directory.GetFiles(directoryFullPath, filePattern))
            {
                var fullPath = Path.GetFullPath(file);
                if (!expectedSnapshots.Contains(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
        }

        private static void DeleteSnapshots(string assetPath)
        {
            var directory = Path.GetDirectoryName(assetPath) ?? string.Empty;
            var directoryFullPath = ToAbsolutePath(directory);
            if (string.IsNullOrEmpty(directoryFullPath) || !Directory.Exists(directoryFullPath))
            {
                return;
            }

            var filePattern = Path.GetFileName(assetPath) + "*" + SnapshotSuffix;
            foreach (var file in Directory.GetFiles(directoryFullPath, filePattern))
            {
                File.Delete(file);
            }
        }

        private static string ToAbsolutePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return relativePath;
            }

            return Path.GetFullPath(Path.Combine(ProjectRoot, relativePath));
        }
    }
}
