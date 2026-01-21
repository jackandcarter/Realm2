using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Realm.Abilities;
using Realm.Data;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor.ContentSync
{
    public class ServerContentCatalogExporter : EditorWindow
    {
        private const string SchemaVersion = "1";
        private const string DefaultOutputFileName = "realm-content.json";
        private StatRegistry registry;
        private string outputPath;

        [MenuItem("Realm/Content/Export Server Catalog")]
        public static void ShowWindow()
        {
            var window = GetWindow<ServerContentCatalogExporter>("Server Content Export");
            window.minSize = new Vector2(480f, 240f);
            window.Show();
        }

        private void OnEnable()
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = GetDefaultOutputPath();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Server Content Catalog Export", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Exports stat, class, equipment, and ability metadata to a JSON file that the server can load on startup.",
                MessageType.Info);

            registry = (StatRegistry)EditorGUILayout.ObjectField(
                new GUIContent("Stat Registry", "Registry asset that aggregates content for export."),
                registry,
                typeof(StatRegistry),
                false);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Output Path", GUILayout.Width(90f));
                outputPath = EditorGUILayout.TextField(outputPath);
                if (GUILayout.Button("Browse", GUILayout.Width(80f)))
                {
                    var selected = EditorUtility.SaveFilePanel(
                        "Export Server Content Catalog",
                        Path.GetDirectoryName(outputPath),
                        DefaultOutputFileName,
                        "json");
                    if (!string.IsNullOrWhiteSpace(selected))
                    {
                        outputPath = selected;
                    }
                }
            }

            using (new EditorGUI.DisabledScope(registry == null || string.IsNullOrWhiteSpace(outputPath)))
            {
                if (GUILayout.Button("Export Catalog", GUILayout.Height(36f)))
                {
                    ExportCatalog();
                }
            }
        }

        private void ExportCatalog()
        {
            if (registry == null)
            {
                EditorUtility.DisplayDialog("Missing Registry", "Select a StatRegistry asset first.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                EditorUtility.DisplayDialog("Missing Path", "Select an output path for the catalog.", "OK");
                return;
            }

            var catalog = BuildCatalog(registry);
            var json = JsonUtility.ToJson(catalog, true);
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outputPath, json);
            Debug.Log($"Server content catalog exported to {outputPath}", this);
            AssetDatabase.Refresh();
        }

        private static ServerContentCatalog BuildCatalog(StatRegistry statRegistry)
        {
            return new ServerContentCatalog
            {
                meta = new ContentCatalogMeta
                {
                    generatedAt = DateTime.UtcNow.ToString("O"),
                    schemaVersion = SchemaVersion,
                    unityVersion = Application.unityVersion,
                },
                stats = statRegistry.StatDefinitions.Select(ExportStat).ToList(),
                statCategories = statRegistry.Categories.Select(ExportCategory).ToList(),
                statProfiles = FindAssets<StatProfileDefinition>().Select(ExportStatProfile).ToList(),
                classes = statRegistry.Classes.Select(ExportClass).ToList(),
                abilities = statRegistry.Abilities.Select(ExportAbility).ToList(),
                weaponTypes = statRegistry.WeaponTypes.Select(ExportWeaponType).ToList(),
                armorTypes = statRegistry.ArmorTypes.Select(ExportArmorType).ToList(),
                weapons = statRegistry.Weapons.Select(ExportWeapon).ToList(),
                armors = statRegistry.Armors.Select(ExportArmor).ToList(),
            };
        }

        private static StatDefinitionExport ExportStat(StatDefinition stat)
        {
            return new StatDefinitionExport
            {
                guid = stat.Guid,
                displayName = stat.DisplayName,
                description = stat.Description,
                ratios = stat.Ratios.Select(ratio => new StatRatioExport
                {
                    sourceStatGuid = ratio.SourceStat != null ? ratio.SourceStat.Guid : null,
                    ratio = ratio.Ratio
                }).ToList(),
            };
        }

        private static StatCategoryExport ExportCategory(StatCategory category)
        {
            return new StatCategoryExport
            {
                guid = category.Guid,
                displayName = category.DisplayName,
                description = category.Description,
                accentColor = ColorExport.FromColor(category.AccentColor),
                statGuids = category.Stats.Select(stat => stat != null ? stat.Guid : null)
                    .Where(guid => !string.IsNullOrWhiteSpace(guid))
                    .ToList(),
            };
        }

        private static StatProfileExport ExportStatProfile(StatProfileDefinition profile)
        {
            return new StatProfileExport
            {
                guid = profile.Guid,
                displayName = profile.DisplayName,
                description = profile.Description,
                statCurves = profile.StatCurves.Select(ExportCurve).ToList(),
            };
        }

        private static ClassDefinitionExport ExportClass(ClassDefinition definition)
        {
            return new ClassDefinitionExport
            {
                guid = definition.Guid,
                classId = definition.ClassId,
                displayName = definition.DisplayName,
                description = definition.Description,
                statProfileGuid = definition.StatProfile != null ? definition.StatProfile.Guid : null,
                statCategoryGuids = definition.StatCategories.Select(category => category != null ? category.Guid : null)
                    .Where(guid => !string.IsNullOrWhiteSpace(guid))
                    .ToList(),
                allowedWeaponTypeGuids = definition.AllowedWeaponTypes.Select(type => type != null ? type.Guid : null)
                    .Where(guid => !string.IsNullOrWhiteSpace(guid))
                    .ToList(),
                allowedArmorTypeGuids = definition.AllowedArmorTypes.Select(type => type != null ? type.Guid : null)
                    .Where(guid => !string.IsNullOrWhiteSpace(guid))
                    .ToList(),
                abilityUnlocks = definition.AbilityUnlocks.Select(ExportAbilityUnlock).ToList(),
                baseStatCurves = definition.BaseStatCurves.Select(ExportCurve).ToList(),
                growthModifiers = definition.GrowthModifiers.Select(ExportCurve).ToList(),
            };
        }

        private static ClassAbilityUnlockExport ExportAbilityUnlock(ClassAbilityUnlock unlock)
        {
            return new ClassAbilityUnlockExport
            {
                abilityGuid = unlock.Ability != null ? unlock.Ability.Guid : null,
                conditionType = unlock.ConditionType.ToString(),
                requiredLevel = unlock.RequiredLevel,
                questId = unlock.QuestId,
                itemId = unlock.ItemId,
                notes = unlock.Notes,
            };
        }

        private static StatCurveExport ExportCurve(ClassStatCurve curve)
        {
            return new StatCurveExport
            {
                statGuid = curve.Stat != null ? curve.Stat.Guid : null,
                baseCurve = CurveExport.FromCurve(curve.BaseValues),
                growthCurve = CurveExport.FromCurve(curve.GrowthValues),
                softCapCurve = CurveExport.FromCurve(curve.SoftCapCurve),
                jitterVariance = Vector2Export.FromVector(curve.JitterVariance),
                formulaTemplate = curve.FormulaTemplate.ToString(),
                formulaCoefficients = curve.FormulaCoefficients.Select(coefficient => new FormulaCoefficientExport
                {
                    key = coefficient.Key,
                    value = coefficient.Value
                }).ToList(),
            };
        }

        private static AbilityDefinitionExport ExportAbility(AbilityDefinition ability)
        {
            return new AbilityDefinitionExport
            {
                guid = ability.Guid,
                abilityName = ability.AbilityName,
                description = ability.Description,
            };
        }

        private static WeaponTypeExport ExportWeaponType(WeaponTypeDefinition type)
        {
            return new WeaponTypeExport
            {
                guid = type.Guid,
                displayName = type.DisplayName,
                description = type.Description,
            };
        }

        private static ArmorTypeExport ExportArmorType(ArmorTypeDefinition type)
        {
            return new ArmorTypeExport
            {
                guid = type.Guid,
                displayName = type.DisplayName,
                description = type.Description,
            };
        }

        private static WeaponDefinitionExport ExportWeapon(WeaponDefinition weapon)
        {
            return new WeaponDefinitionExport
            {
                guid = weapon.Guid,
                displayName = weapon.DisplayName,
                description = weapon.Description,
                slot = weapon.Slot.ToString().ToLowerInvariant(),
                requiredClassIds = weapon.RequiredClassIds.ToList(),
                weaponTypeGuid = weapon.WeaponType != null ? weapon.WeaponType.Guid : null,
                baseDamage = weapon.BaseDamage,
                specialAttackGuid = weapon.SpecialAttack != null ? weapon.SpecialAttack.Guid : null,
            };
        }

        private static ArmorDefinitionExport ExportArmor(ArmorDefinition armor)
        {
            return new ArmorDefinitionExport
            {
                guid = armor.Guid,
                displayName = armor.DisplayName,
                description = armor.Description,
                slot = armor.Slot.ToString().ToLowerInvariant(),
                requiredClassIds = armor.RequiredClassIds.ToList(),
                armorTypeGuid = armor.ArmorType != null ? armor.ArmorType.Guid : null,
            };
        }

        private static string GetDefaultOutputPath()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(projectRoot, "Server", "content", DefaultOutputFileName);
        }

        private static IEnumerable<T> FindAssets<T>() where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    yield return asset;
                }
            }
        }
    }

    [Serializable]
    public class ServerContentCatalog
    {
        public ContentCatalogMeta meta;
        public List<StatDefinitionExport> stats = new();
        public List<StatCategoryExport> statCategories = new();
        public List<StatProfileExport> statProfiles = new();
        public List<ClassDefinitionExport> classes = new();
        public List<AbilityDefinitionExport> abilities = new();
        public List<WeaponTypeExport> weaponTypes = new();
        public List<ArmorTypeExport> armorTypes = new();
        public List<WeaponDefinitionExport> weapons = new();
        public List<ArmorDefinitionExport> armors = new();
    }

    [Serializable]
    public class ContentCatalogMeta
    {
        public string generatedAt;
        public string schemaVersion;
        public string unityVersion;
    }

    [Serializable]
    public class StatDefinitionExport
    {
        public string guid;
        public string displayName;
        public string description;
        public List<StatRatioExport> ratios = new();
    }

    [Serializable]
    public class StatRatioExport
    {
        public string sourceStatGuid;
        public float ratio;
    }

    [Serializable]
    public class StatCategoryExport
    {
        public string guid;
        public string displayName;
        public string description;
        public ColorExport accentColor;
        public List<string> statGuids = new();
    }

    [Serializable]
    public class StatProfileExport
    {
        public string guid;
        public string displayName;
        public string description;
        public List<StatCurveExport> statCurves = new();
    }

    [Serializable]
    public class ClassDefinitionExport
    {
        public string guid;
        public string classId;
        public string displayName;
        public string description;
        public string statProfileGuid;
        public List<string> statCategoryGuids = new();
        public List<string> allowedWeaponTypeGuids = new();
        public List<string> allowedArmorTypeGuids = new();
        public List<ClassAbilityUnlockExport> abilityUnlocks = new();
        public List<StatCurveExport> baseStatCurves = new();
        public List<StatCurveExport> growthModifiers = new();
    }

    [Serializable]
    public class ClassAbilityUnlockExport
    {
        public string abilityGuid;
        public string conditionType;
        public int requiredLevel;
        public string questId;
        public string itemId;
        public string notes;
    }

    [Serializable]
    public class StatCurveExport
    {
        public string statGuid;
        public CurveExport baseCurve;
        public CurveExport growthCurve;
        public CurveExport softCapCurve;
        public Vector2Export jitterVariance;
        public string formulaTemplate;
        public List<FormulaCoefficientExport> formulaCoefficients = new();
    }

    [Serializable]
    public class FormulaCoefficientExport
    {
        public string key;
        public float value;
    }

    [Serializable]
    public class AbilityDefinitionExport
    {
        public string guid;
        public string abilityName;
        public string description;
    }

    [Serializable]
    public class WeaponTypeExport
    {
        public string guid;
        public string displayName;
        public string description;
    }

    [Serializable]
    public class ArmorTypeExport
    {
        public string guid;
        public string displayName;
        public string description;
    }

    [Serializable]
    public class WeaponDefinitionExport
    {
        public string guid;
        public string displayName;
        public string description;
        public string slot;
        public List<string> requiredClassIds = new();
        public string weaponTypeGuid;
        public float baseDamage;
        public string specialAttackGuid;
    }

    [Serializable]
    public class ArmorDefinitionExport
    {
        public string guid;
        public string displayName;
        public string description;
        public string slot;
        public List<string> requiredClassIds = new();
        public string armorTypeGuid;
    }

    [Serializable]
    public class CurveExport
    {
        public List<CurveKeyExport> keys = new();

        public static CurveExport FromCurve(AnimationCurve curve)
        {
            var export = new CurveExport();
            if (curve == null)
            {
                return export;
            }

            foreach (var key in curve.keys)
            {
                export.keys.Add(new CurveKeyExport
                {
                    time = key.time,
                    value = key.value,
                    inTangent = key.inTangent,
                    outTangent = key.outTangent
                });
            }

            return export;
        }
    }

    [Serializable]
    public class CurveKeyExport
    {
        public float time;
        public float value;
        public float inTangent;
        public float outTangent;
    }

    [Serializable]
    public class Vector2Export
    {
        public float x;
        public float y;

        public static Vector2Export FromVector(Vector2 vector)
        {
            return new Vector2Export { x = vector.x, y = vector.y };
        }
    }

    [Serializable]
    public class ColorExport
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public static ColorExport FromColor(Color color)
        {
            return new ColorExport
            {
                r = color.r,
                g = color.g,
                b = color.b,
                a = color.a
            };
        }
    }
}
