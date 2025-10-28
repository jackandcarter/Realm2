using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor.DesignerTools
{
    [Serializable]
    public class StatCategory
    {
        public string id = Guid.NewGuid().ToString();
        public string displayName = "New Stat";
        public Color color = Color.white;
        public float defaultMultiplier = 1f;
        public float defaultBonus;
    }

    [Serializable]
    public class ClassStatAssignment
    {
        public string statId;
        public float multiplier = 1f;
        public float bonus;
    }

    [Serializable]
    public class LevelProgressionRow
    {
        public int level = 1;
        public AnimationCurve growthCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public float derivedStatPreview;
    }

    public enum LoadoutRole
    {
        None,
        Tank,
        Healer,
        DPS,
        Support
    }

    public enum LoadoutElement
    {
        None,
        Fire,
        Water,
        Earth,
        Air,
        Light,
        Shadow
    }

    [Serializable]
    public class LoadoutSlot
    {
        public string slotName = "New Slot";
        public LoadoutRole requiredRole;
        public LoadoutElement requiredElement;
        public bool allowTwoHanded;
    }

    [Serializable]
    public class CharacterClass
    {
        public string id = Guid.NewGuid().ToString();
        public string displayName = "New Class";
        public Texture2D emblem;
        public List<ClassStatAssignment> stats = new List<ClassStatAssignment>();
        public List<LevelProgressionRow> progression = new List<LevelProgressionRow>();
        public List<LoadoutSlot> loadoutSlots = new List<LoadoutSlot>();
        public bool allowShieldWithTwoHanded;
    }

    public class DesignerToolkitProfile : ScriptableObject
    {
        public List<StatCategory> statCategories = new List<StatCategory>();
        public List<CharacterClass> classes = new List<CharacterClass>();
        public float derivedStatBaseValue = 10f;
        public float derivedStatScale = 5f;
    }

    public class DesignerToolkitWindow : EditorWindow
    {
        private const string WindowTitle = "Designer Toolkit";
        private const string ProfileAssetPath = "Assets/Settings/DesignerToolkitProfile.asset";

        private DesignerToolkitProfile profile;
        private Vector2 statScroll;
        private Vector2 classScroll;
        private Vector2 progressionScroll;
        private CharacterClass selectedClass;

        [MenuItem("Tools/Designer/Toolkit")]        
        public static void ShowWindow()
        {
            var window = GetWindow<DesignerToolkitWindow>();
            window.titleContent = new GUIContent(WindowTitle, EditorGUIUtility.IconContent("d_UnityEditor.InspectorWindow").image);
            window.minSize = new Vector2(900f, 500f);
            window.LoadProfile();
            window.Show();
        }

        private void OnEnable()
        {
            LoadProfile();
        }

        private void LoadProfile()
        {
            if (profile != null)
            {
                return;
            }

            profile = AssetDatabase.LoadAssetAtPath<DesignerToolkitProfile>(ProfileAssetPath);
            if (profile == null)
            {
                profile = CreateInstance<DesignerToolkitProfile>();
                AssetDatabase.CreateAsset(profile, ProfileAssetPath);
                AssetDatabase.SaveAssets();
            }

            if (profile.statCategories.Count == 0)
            {
                SeedDefaultStats();
            }

            if (profile.classes.Count == 0)
            {
                SeedDefaultClasses();
            }
        }

        private void OnGUI()
        {
            if (profile == null)
            {
                LoadProfile();
                if (profile == null)
                {
                    EditorGUILayout.HelpBox("Failed to load toolkit profile.", MessageType.Error);
                    return;
                }
            }

            EditorGUILayout.Space();
            DrawToolbar();
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(260)))
                {
                    DrawStatPalette();
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    DrawClassPanels();
                }
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                {
                    EditorUtility.SetDirty(profile);
                    AssetDatabase.SaveAssets();
                }

                if (GUILayout.Button("Reset", EditorStyles.toolbarButton))
                {
                    if (EditorUtility.DisplayDialog("Reset Profile", "Are you sure you want to reset the designer toolkit profile?", "Reset", "Cancel"))
                    {
                        profile.statCategories.Clear();
                        profile.classes.Clear();
                        SeedDefaultStats();
                        SeedDefaultClasses();
                        selectedClass = null;
                    }
                }

                GUILayout.FlexibleSpace();

                profile.derivedStatBaseValue = EditorGUILayout.FloatField(new GUIContent("Derived Base"), profile.derivedStatBaseValue, GUILayout.Width(180));
                profile.derivedStatScale = EditorGUILayout.FloatField(new GUIContent("Derived Scale"), profile.derivedStatScale, GUILayout.Width(180));
            }
        }

        private void DrawStatPalette()
        {
            EditorGUILayout.LabelField("Stat Categories", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Drag stats into a class panel to assign them. Adjust default multipliers and bonuses to tweak baseline values.", MessageType.Info);

            using (var scroll = new EditorGUILayout.ScrollViewScope(statScroll))
            {
                statScroll = scroll.scrollPosition;
                for (int i = 0; i < profile.statCategories.Count; i++)
                {
                    var stat = profile.statCategories[i];
                    DrawStatPaletteItem(stat, i);
                }

                if (GUILayout.Button("Add Stat"))
                {
                    profile.statCategories.Add(new StatCategory());
                }
            }
        }

        private void DrawStatPaletteItem(StatCategory stat, int index)
        {
            var rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(stat.displayName, EditorStyles.boldLabel);
            stat.displayName = EditorGUILayout.TextField("Name", stat.displayName);
            stat.color = EditorGUILayout.ColorField("Color", stat.color);
            stat.defaultMultiplier = EditorGUILayout.FloatField("Default Multiplier", stat.defaultMultiplier);
            stat.defaultBonus = EditorGUILayout.FloatField("Default Bonus", stat.defaultBonus);

            if (GUILayout.Button("Remove"))
            {
                Undo.RecordObject(profile, "Remove Stat Category");
                profile.statCategories.RemoveAt(index);
                EditorGUILayout.EndVertical();
                return;
            }

            HandleStatDrag(stat, rect);
            EditorGUILayout.EndVertical();
        }

        private void HandleStatDrag(StatCategory stat, Rect rect)
        {
            var current = Event.current;
            switch (current.type)
            {
                case EventType.MouseDown:
                    if (rect.Contains(current.mousePosition))
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.SetGenericData("DesignerToolkitStat", stat.id);
                        DragAndDrop.objectReferences = Array.Empty<UnityEngine.Object>();
                        DragAndDrop.StartDrag(stat.displayName);
                        current.Use();
                    }
                    break;
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (DragAndDrop.GetGenericData("DesignerToolkitStat") is string statId && statId == stat.id)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        if (current.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                        }
                        current.Use();
                    }
                    break;
            }
        }

        private void DrawClassPanels()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Classes", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add Class", GUILayout.Width(100)))
                {
                    var newClass = new CharacterClass();
                    newClass.progression.Add(new LevelProgressionRow());
                    profile.classes.Add(newClass);
                    selectedClass = newClass;
                }
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(classScroll))
            {
                classScroll = scroll.scrollPosition;
                foreach (var characterClass in profile.classes)
                {
                    DrawClassPanel(characterClass);
                }
            }
        }

        private void DrawClassPanel(CharacterClass characterClass)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    characterClass.displayName = EditorGUILayout.TextField(characterClass.displayName, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Select", GUILayout.Width(70)))
                    {
                        selectedClass = characterClass;
                    }
                }

                characterClass.emblem = (Texture2D)EditorGUILayout.ObjectField("Emblem", characterClass.emblem, typeof(Texture2D), false);
                characterClass.allowShieldWithTwoHanded = EditorGUILayout.ToggleLeft("Allow shield with two-handed", characterClass.allowShieldWithTwoHanded);

                DrawStatAssignments(characterClass);
                DrawLevelProgression(characterClass);
                DrawLoadoutEditor(characterClass);
            }
        }

        private void DrawStatAssignments(CharacterClass characterClass)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stat Assignments", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Drag a stat from the palette to add it to this class. Override multipliers and bonuses to customize.", MessageType.None);

            var dropArea = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(60));
            GUI.Box(dropArea, "Drop Stat Here", EditorStyles.centeredGreyMiniLabel);

            var current = Event.current;
            if ((current.type == EventType.DragUpdated || current.type == EventType.DragPerform) && dropArea.Contains(current.mousePosition))
            {
                if (DragAndDrop.GetGenericData("DesignerToolkitStat") is string statId)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (current.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        AddStatToClass(characterClass, statId);
                    }
                    current.Use();
                }
            }

            for (int i = 0; i < characterClass.stats.Count; i++)
            {
                var assignment = characterClass.stats[i];
                var stat = profile.statCategories.FirstOrDefault(s => s.id == assignment.statId);
                if (stat == null)
                {
                    continue;
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(stat.displayName, EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        {
                            characterClass.stats.RemoveAt(i);
                            break;
                        }
                    }

                    assignment.multiplier = EditorGUILayout.FloatField("Multiplier", assignment.multiplier);
                    assignment.bonus = EditorGUILayout.FloatField("Bonus", assignment.bonus);

                    float preview = CalculateDerivedStatPreview(stat, assignment);
                    EditorGUILayout.LabelField("Derived Preview", preview.ToString("F2"));
                }
            }
        }

        private float CalculateDerivedStatPreview(StatCategory stat, ClassStatAssignment assignment)
        {
            return profile.derivedStatBaseValue + (stat.defaultMultiplier * assignment.multiplier) * profile.derivedStatScale + stat.defaultBonus + assignment.bonus;
        }

        private void AddStatToClass(CharacterClass characterClass, string statId)
        {
            if (characterClass.stats.Any(s => s.statId == statId))
            {
                ShowNotification(new GUIContent("Stat already assigned to this class."));
                return;
            }

            var stat = profile.statCategories.FirstOrDefault(s => s.id == statId);
            if (stat == null)
            {
                return;
            }

            Undo.RecordObject(profile, "Add Stat Assignment");
            characterClass.stats.Add(new ClassStatAssignment
            {
                statId = statId,
                multiplier = stat.defaultMultiplier,
                bonus = stat.defaultBonus
            });
        }

        private void DrawLevelProgression(CharacterClass characterClass)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Level Progression", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure each level's growth curve and preview the resulting derived stat.", MessageType.None);

            using (var scroll = new EditorGUILayout.ScrollViewScope(progressionScroll, GUILayout.Height(200)))
            {
                progressionScroll = scroll.scrollPosition;
                for (int i = 0; i < characterClass.progression.Count; i++)
                {
                    var row = characterClass.progression[i];
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            row.level = EditorGUILayout.IntField("Level", row.level);
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Remove", GUILayout.Width(70)))
                            {
                                characterClass.progression.RemoveAt(i);
                                break;
                            }
                        }

                        row.growthCurve = EditorGUILayout.CurveField("Growth Curve", row.growthCurve, Color.cyan, new Rect(0, 0, 1, 1));
                        row.derivedStatPreview = EditorGUILayout.FloatField("Derived Preview", row.derivedStatPreview);

                        if (selectedClass == characterClass)
                        {
                            var sample = row.growthCurve.Evaluate(1f);
                            row.derivedStatPreview = profile.derivedStatBaseValue + sample * profile.derivedStatScale;
                            EditorGUILayout.LabelField("Auto Preview", row.derivedStatPreview.ToString("F2"));
                        }
                    }
                }
            }

            if (GUILayout.Button("Add Level"))
            {
                var row = new LevelProgressionRow();
                if (characterClass.progression.Count > 0)
                {
                    row.level = characterClass.progression.Max(r => r.level) + 1;
                }

                characterClass.progression.Add(row);
            }
        }

        private void DrawLoadoutEditor(CharacterClass characterClass)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Loadout", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Define loadout slots, apply role/element filters, and validate conflicting gear.", MessageType.None);

            for (int i = 0; i < characterClass.loadoutSlots.Count; i++)
            {
                var slot = characterClass.loadoutSlots[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        slot.slotName = EditorGUILayout.TextField(slot.slotName);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        {
                            characterClass.loadoutSlots.RemoveAt(i);
                            break;
                        }
                    }

                    slot.requiredRole = (LoadoutRole)EditorGUILayout.EnumPopup("Role", slot.requiredRole);
                    slot.requiredElement = (LoadoutElement)EditorGUILayout.EnumPopup("Element", slot.requiredElement);
                    slot.allowTwoHanded = EditorGUILayout.ToggleLeft("Allow Two-Handed", slot.allowTwoHanded);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Slot"))
                {
                    characterClass.loadoutSlots.Add(new LoadoutSlot());
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Validate"))
                {
                    ValidateLoadout(characterClass);
                }
            }
        }

        private void ValidateLoadout(CharacterClass characterClass)
        {
            var issues = new List<string>();
            var roles = new HashSet<LoadoutRole>();
            foreach (var slot in characterClass.loadoutSlots)
            {
                if (slot.requiredRole != LoadoutRole.None && !roles.Add(slot.requiredRole))
                {
                    issues.Add($"Role {slot.requiredRole} assigned multiple times.");
                }

                if (!slot.allowTwoHanded && !characterClass.allowShieldWithTwoHanded)
                {
                    issues.Add($"{slot.slotName} disallows two-handed gear while shields with two-handed weapons are disabled.");
                }
            }

            if (characterClass.loadoutSlots.Count(s => s.requiredElement == LoadoutElement.None) == 0)
            {
                issues.Add("No neutral element slot available.");
            }

            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Loadout Validation", "No issues found!", "Ok");
            }
            else
            {
                EditorUtility.DisplayDialog("Loadout Validation", string.Join("\n", issues), "Ok");
            }
        }

        private void SeedDefaultStats()
        {
            profile.statCategories.AddRange(new[]
            {
                new StatCategory { displayName = "Health", color = new Color(0.7f, 0.2f, 0.2f), defaultMultiplier = 1.2f },
                new StatCategory { displayName = "Attack", color = new Color(0.8f, 0.6f, 0.2f), defaultMultiplier = 1.1f },
                new StatCategory { displayName = "Defense", color = new Color(0.2f, 0.6f, 0.8f), defaultMultiplier = 1.0f },
                new StatCategory { displayName = "Speed", color = new Color(0.3f, 0.8f, 0.4f), defaultMultiplier = 0.9f },
            });
        }

        private void SeedDefaultClasses()
        {
            var warrior = new CharacterClass { displayName = "Warrior", allowShieldWithTwoHanded = false };
            warrior.stats.Add(new ClassStatAssignment { statId = profile.statCategories[0].id, multiplier = 1.4f, bonus = 10 });
            warrior.progression.Add(new LevelProgressionRow { level = 1 });
            warrior.loadoutSlots.Add(new LoadoutSlot { slotName = "Weapon", requiredRole = LoadoutRole.DPS, requiredElement = LoadoutElement.None, allowTwoHanded = true });
            warrior.loadoutSlots.Add(new LoadoutSlot { slotName = "Shield", requiredRole = LoadoutRole.Tank, requiredElement = LoadoutElement.None, allowTwoHanded = false });

            var mage = new CharacterClass { displayName = "Mage", allowShieldWithTwoHanded = true };
            mage.stats.Add(new ClassStatAssignment { statId = profile.statCategories[1].id, multiplier = 1.6f, bonus = 15 });
            mage.progression.Add(new LevelProgressionRow { level = 1 });
            mage.loadoutSlots.Add(new LoadoutSlot { slotName = "Focus", requiredRole = LoadoutRole.Support, requiredElement = LoadoutElement.Light, allowTwoHanded = false });
            mage.loadoutSlots.Add(new LoadoutSlot { slotName = "Accessory", requiredRole = LoadoutRole.None, requiredElement = LoadoutElement.None, allowTwoHanded = true });

            profile.classes.Add(warrior);
            profile.classes.Add(mage);
        }
    }
}
