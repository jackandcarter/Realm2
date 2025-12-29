using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MapPinEditorWindow provides a lightweight editor utility that links world map pins
/// with their zone mini map counterparts. Designers can click directly on preview
/// textures to capture normalized coordinates (0-1 range from bottom-left) while keeping
/// data editable through JSON or CSV instead of binary assets.
/// </summary>
public class MapPinEditorWindow : EditorWindow
{
    [SerializeField] private Texture2D worldMapTexture;
    [SerializeField] private List<ZoneMiniMap> zoneMiniMaps = new List<ZoneMiniMap>();
    [SerializeField] private List<PinDefinition> pins = new List<PinDefinition>();
    [SerializeField] private int selectedPinIndex = -1;
    [SerializeField] private int selectedZoneIndex = -1;

    private Vector2 pinListScroll;

    [MenuItem("Tools/Map Tools/Map Pin Editor")]
    private static void OpenWindow()
    {
        GetWindow<MapPinEditorWindow>("Map Pin Editor");
    }

    private void OnEnable()
    {
        wantsMouseMove = true;

        if (pins == null)
        {
            pins = new List<PinDefinition>();
        }

        if (zoneMiniMaps == null)
        {
            zoneMiniMaps = new List<ZoneMiniMap>();
        }
    }

    private void OnGUI()
    {
        DrawWorldMapSection();
        EditorGUILayout.Space();
        DrawPinListSection();
        EditorGUILayout.Space();
        DrawZoneSection();
        EditorGUILayout.Space();
        DrawImportExportSection();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(this);
        }
    }

    private void DrawWorldMapSection()
    {
        EditorGUILayout.LabelField("World Map", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Assign a world map texture, then click on it to capture normalized coordinates for pins.", MessageType.Info);
        Texture2D newTexture = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("Texture", "World map texture used for pin placement preview."), worldMapTexture, typeof(Texture2D), false);

        if (newTexture != worldMapTexture)
        {
            RecordUndo("Assign world map texture");
            worldMapTexture = newTexture;
        }

        if (worldMapTexture == null)
        {
            EditorGUILayout.HelpBox("Assign a world map texture to preview and author pin locations.", MessageType.Info);
            return;
        }

        float aspect = Mathf.Max(0.01f, (float)worldMapTexture.width / worldMapTexture.height);
        Rect mapRect = GUILayoutUtility.GetAspectRect(aspect, GUILayout.ExpandWidth(true));
        GUI.Box(mapRect, GUIContent.none);
        GUI.DrawTexture(mapRect, worldMapTexture, ScaleMode.StretchToFill);

        DrawWorldPinOverlay(mapRect);
        HandleWorldMapInput(mapRect);
    }

    private void DrawPinListSection()
    {
        EditorGUILayout.LabelField("Pin Definitions", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Pins define the id and normalized coordinates for world map and zone mini map locations.", MessageType.Info);

        using (var scroll = new EditorGUILayout.ScrollViewScope(pinListScroll, GUILayout.Height(160)))
        {
            pinListScroll = scroll.scrollPosition;

            for (int i = 0; i < pins.Count; i++)
            {
                var pin = pins[i];
                bool isSelected = i == selectedPinIndex;

                EditorGUILayout.BeginHorizontal();
                string label = string.IsNullOrEmpty(pin.id) ? $"Pin {i}" : pin.id;
                label += $"  ({pin.worldNormalizedPosition.x:F2}, {pin.worldNormalizedPosition.y:F2})";

                if (GUILayout.Toggle(isSelected, new GUIContent(label, "Select this pin to edit its identifiers and coordinates."), EditorStyles.miniButtonLeft))
                {
                    selectedPinIndex = i;
                }

                if (GUILayout.Button(new GUIContent("✕", "Remove this pin from the list."), EditorStyles.miniButtonRight, GUILayout.Width(24)))
                {
                    RecordUndo("Remove pin");
                    pins.RemoveAt(i);
                    if (selectedPinIndex == i)
                    {
                        selectedPinIndex = Mathf.Clamp(selectedPinIndex - 1, -1, pins.Count - 1);
                    }
                    GUI.FocusControl(null);
                    return;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(new GUIContent("Add Pin", "Create a new pin entry with a default identifier."), GUILayout.Width(100)))
        {
            RecordUndo("Add pin");
            pins.Add(new PinDefinition { id = $"Pin {pins.Count + 1}" });
            selectedPinIndex = pins.Count - 1;
        }
        EditorGUILayout.EndHorizontal();

        if (selectedPinIndex >= 0 && selectedPinIndex < pins.Count)
        {
            EditorGUILayout.Space();
            DrawSelectedPinDetails(pins[selectedPinIndex]);
        }
        else
        {
            EditorGUILayout.HelpBox("Select a pin to edit its identifiers and normalized coordinates.", MessageType.Info);
        }
    }

    private void DrawSelectedPinDetails(PinDefinition pin)
    {
        EditorGUILayout.LabelField("Selected Pin", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        string newId = EditorGUILayout.TextField(new GUIContent("Identifier", "Unique id used to reference this pin in data exports."), pin.id);
        if (EditorGUI.EndChangeCheck())
        {
            RecordUndo("Rename pin");
            pin.id = newId;
        }

        EditorGUI.BeginChangeCheck();
        Vector2 newWorld = Clamp01(EditorGUILayout.Vector2Field(new GUIContent("World Normalized", "Normalized (0-1) coordinate on the world map texture."), pin.worldNormalizedPosition));
        if (EditorGUI.EndChangeCheck())
        {
            RecordUndo("Edit world normalized coordinate");
            pin.worldNormalizedPosition = newWorld;
        }

        if (selectedZoneIndex >= 0 && selectedZoneIndex < zoneMiniMaps.Count)
        {
            var zone = zoneMiniMaps[selectedZoneIndex];
            if (!string.IsNullOrEmpty(zone.zoneName))
            {
                ZoneCoordinate zoneCoord = pin.GetOrCreateZoneCoordinate(zone.zoneName);
                EditorGUI.BeginChangeCheck();
                Vector2 newZone = Clamp01(EditorGUILayout.Vector2Field(new GUIContent($"{zone.zoneName} Normalized", "Normalized (0-1) coordinate on the selected zone mini map texture."), zoneCoord.normalizedPosition));
                if (EditorGUI.EndChangeCheck())
                {
                    RecordUndo("Edit zone normalized coordinate");
                    zoneCoord.normalizedPosition = newZone;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Give the selected zone a name to author its pin offsets.", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Select a zone to author zone-specific normalized offsets.", MessageType.Info);
        }
    }

    private void DrawZoneSection()
    {
        EditorGUILayout.LabelField("Zone Mini Maps", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Define per-zone mini map masks and coordinates to translate world pins into local maps.", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < zoneMiniMaps.Count; i++)
        {
            var zone = zoneMiniMaps[i];
            string label = string.IsNullOrEmpty(zone.zoneName) ? $"Zone {i + 1}" : zone.zoneName;
            bool isSelected = i == selectedZoneIndex;

            if (GUILayout.Toggle(isSelected, new GUIContent(label, "Select this zone mini map to edit its mask and coordinates."), "Button"))
            {
                selectedZoneIndex = i;
            }
        }

        if (GUILayout.Button(new GUIContent("+", "Add a new zone mini map entry."), GUILayout.Width(28)))
        {
            RecordUndo("Add zone mini map");
            zoneMiniMaps.Add(new ZoneMiniMap { zoneName = $"Zone {zoneMiniMaps.Count + 1}" });
            selectedZoneIndex = zoneMiniMaps.Count - 1;
        }
        EditorGUILayout.EndHorizontal();

        if (zoneMiniMaps.Count == 0)
        {
            EditorGUILayout.HelpBox("Add zone mini maps to preview local pin placement masks.", MessageType.Info);
            return;
        }

        if (selectedZoneIndex < 0 || selectedZoneIndex >= zoneMiniMaps.Count)
        {
            selectedZoneIndex = Mathf.Clamp(selectedZoneIndex, 0, zoneMiniMaps.Count - 1);
        }

        ZoneMiniMap selectedZone = zoneMiniMaps[selectedZoneIndex];

        EditorGUI.BeginChangeCheck();
        string zoneName = EditorGUILayout.TextField(new GUIContent("Zone Name", "Unique name used to match zone-specific coordinates."), selectedZone.zoneName);
        if (EditorGUI.EndChangeCheck())
        {
            RecordUndo("Rename zone");
            selectedZone.zoneName = zoneName;
        }

        EditorGUI.BeginChangeCheck();
        Texture2D newTexture = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("Mini Map Mask", "Texture used to preview the zone mini map."), selectedZone.miniMapMask, typeof(Texture2D), false);
        if (EditorGUI.EndChangeCheck())
        {
            RecordUndo("Assign zone mini map mask");
            selectedZone.miniMapMask = newTexture;
        }

        if (GUILayout.Button(new GUIContent("Remove Selected Zone", "Delete the currently selected zone mini map entry.")))
        {
            RecordUndo("Remove zone mini map");
            zoneMiniMaps.RemoveAt(selectedZoneIndex);
            selectedZoneIndex = zoneMiniMaps.Count == 0 ? -1 : Mathf.Clamp(selectedZoneIndex - 1, 0, zoneMiniMaps.Count - 1);
            return;
        }

        if (selectedZone.miniMapMask == null)
        {
            EditorGUILayout.HelpBox("Assign a mini map mask texture to preview zone-specific pin placement.", MessageType.Info);
            return;
        }

        float aspect = Mathf.Max(0.01f, (float)selectedZone.miniMapMask.width / selectedZone.miniMapMask.height);
        Rect zoneRect = GUILayoutUtility.GetAspectRect(aspect, GUILayout.ExpandWidth(true));
        GUI.Box(zoneRect, GUIContent.none);
        GUI.DrawTexture(zoneRect, selectedZone.miniMapMask, ScaleMode.StretchToFill);

        DrawZonePinOverlay(zoneRect, selectedZone.zoneName);
        HandleZoneMapInput(zoneRect, selectedZone.zoneName);
    }

    private void DrawWorldPinOverlay(Rect mapRect)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        Handles.BeginGUI();
        for (int i = 0; i < pins.Count; i++)
        {
            Vector2 guiPosition = NormalizedToGui(mapRect, pins[i].worldNormalizedPosition);
            float radius = Mathf.Clamp(mapRect.width, 64f, 256f) * 0.01f;
            Handles.color = i == selectedPinIndex ? Color.cyan : Color.white;
            Handles.DrawSolidDisc(guiPosition, Vector3.forward, radius);
        }
        Handles.EndGUI();
    }

    private void DrawZonePinOverlay(Rect zoneRect, string zoneName)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        Handles.BeginGUI();
        for (int i = 0; i < pins.Count; i++)
        {
            var pin = pins[i];
            ZoneCoordinate coord = pin.zoneCoordinates.FirstOrDefault(c => c.zoneName == zoneName);
            if (coord == null)
            {
                continue;
            }

            Vector2 guiPosition = NormalizedToGui(zoneRect, coord.normalizedPosition);
            float radius = Mathf.Clamp(zoneRect.width, 64f, 256f) * 0.01f;
            Handles.color = i == selectedPinIndex ? Color.cyan : new Color(1f, 1f, 1f, 0.8f);
            Handles.DrawSolidDisc(guiPosition, Vector3.forward, radius);
        }
        Handles.EndGUI();
    }

    private void HandleWorldMapInput(Rect mapRect)
    {
        Event evt = Event.current;
        if (evt.type == EventType.MouseDown && evt.button == 0 && mapRect.Contains(evt.mousePosition))
        {
            if (selectedPinIndex < 0 || selectedPinIndex >= pins.Count)
            {
                ShowNotification(new GUIContent("Select a pin before placing it on the world map."));
                return;
            }

            Vector2 normalized = GuiToNormalized(mapRect, evt.mousePosition);
            RecordUndo("Set world pin position");
            pins[selectedPinIndex].worldNormalizedPosition = normalized;
            evt.Use();
        }
    }

    private void HandleZoneMapInput(Rect zoneRect, string zoneName)
    {
        if (string.IsNullOrEmpty(zoneName))
        {
            return;
        }

        Event evt = Event.current;
        if (evt.type == EventType.MouseDown && evt.button == 0 && zoneRect.Contains(evt.mousePosition))
        {
            if (selectedPinIndex < 0 || selectedPinIndex >= pins.Count)
            {
                ShowNotification(new GUIContent("Select a pin before placing it on the zone map."));
                return;
            }

            Vector2 normalized = GuiToNormalized(zoneRect, evt.mousePosition);
            RecordUndo("Set zone pin position");
            ZoneCoordinate coord = pins[selectedPinIndex].GetOrCreateZoneCoordinate(zoneName);
            coord.normalizedPosition = normalized;
            evt.Use();
        }
    }

    private void DrawImportExportSection()
    {
        EditorGUILayout.LabelField("Import / Export", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Export to JSON for long-term storage or CSV for spreadsheet adjustments. Import replaces the in-memory pin list.", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("Export JSON", "Save the current pins and zones to a JSON file.")))
        {
            string path = EditorUtility.SaveFilePanel("Export Pins (JSON)", Application.dataPath, "MapPins", "json");
            if (!string.IsNullOrEmpty(path))
            {
                ExportJson(path);
            }
        }

        if (GUILayout.Button(new GUIContent("Import JSON", "Load pins and zones from a JSON file (overwrites current list).")))
        {
            string path = EditorUtility.OpenFilePanel("Import Pins (JSON)", Application.dataPath, "json");
            if (!string.IsNullOrEmpty(path))
            {
                ImportJson(path);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("Export CSV", "Save pin coordinates to a CSV file for spreadsheet editing.")))
        {
            string path = EditorUtility.SaveFilePanel("Export Pins (CSV)", Application.dataPath, "MapPins", "csv");
            if (!string.IsNullOrEmpty(path))
            {
                ExportCsv(path);
            }
        }

        if (GUILayout.Button(new GUIContent("Import CSV", "Load pin coordinates from a CSV file (overwrites current list).")))
        {
            string path = EditorUtility.OpenFilePanel("Import Pins (CSV)", Application.dataPath, "csv");
            if (!string.IsNullOrEmpty(path))
            {
                ImportCsv(path);
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void ExportJson(string path)
    {
        var container = new PinCollection { pins = pins.Select(pin => pin.Clone()).ToList() };
        string json = JsonUtility.ToJson(container, true);
        File.WriteAllText(path, json, Encoding.UTF8);
        AssetDatabase.Refresh();
    }

    private void ImportJson(string path)
    {
        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            var container = JsonUtility.FromJson<PinCollection>(json);
            if (container?.pins == null)
            {
                Debug.LogWarning("No pins found in JSON file.");
                return;
            }

            RecordUndo("Import pins from JSON");
            pins = container.pins.Select(pin => pin.Clone()).ToList();
            selectedPinIndex = Mathf.Clamp(selectedPinIndex, 0, pins.Count - 1);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to import pins from JSON: {ex.Message}");
        }
    }

    private void ExportCsv(string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("PinId,WorldX,WorldY,ZoneName,ZoneX,ZoneY");
        foreach (var pin in pins)
        {
            if (pin.zoneCoordinates.Count == 0)
            {
                sb.AppendLine(FormatCsvRow(pin.id, pin.worldNormalizedPosition, string.Empty, Vector2.zero));
            }
            else
            {
                foreach (var coord in pin.zoneCoordinates)
                {
                    sb.AppendLine(FormatCsvRow(pin.id, pin.worldNormalizedPosition, coord.zoneName, coord.normalizedPosition));
                }
            }
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
    }

    private string FormatCsvRow(string pinId, Vector2 worldPos, string zoneName, Vector2 zonePos)
    {
        return string.Join(",", new[]
        {
            EscapeCsv(pinId),
            worldPos.x.ToString(CultureInfo.InvariantCulture),
            worldPos.y.ToString(CultureInfo.InvariantCulture),
            EscapeCsv(zoneName),
            zonePos.x.ToString(CultureInfo.InvariantCulture),
            zonePos.y.ToString(CultureInfo.InvariantCulture)
        });
    }

    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Contains(",") || value.Contains("\""))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private void ImportCsv(string path)
    {
        try
        {
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length <= 1)
            {
                Debug.LogWarning("CSV file does not contain any rows.");
                return;
            }

            RecordUndo("Import pins from CSV");

            var importedPins = new Dictionary<string, PinDefinition>();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] cells = ParseCsvLine(line).ToArray();
                if (cells.Length < 3)
                {
                    Debug.LogWarning($"Skipping malformed CSV row: {line}");
                    continue;
                }

                string pinId = cells[0];
                float worldX = ParseFloat(cells[1]);
                float worldY = ParseFloat(cells[2]);
                string zoneName = cells.Length > 3 ? cells[3] : string.Empty;
                float zoneX = cells.Length > 4 ? ParseFloat(cells[4]) : 0f;
                float zoneY = cells.Length > 5 ? ParseFloat(cells[5]) : 0f;

                if (!importedPins.TryGetValue(pinId, out PinDefinition pin))
                {
                    pin = new PinDefinition { id = pinId, worldNormalizedPosition = new Vector2(worldX, worldY) };
                    importedPins.Add(pinId, pin);
                }
                else
                {
                    pin.worldNormalizedPosition = new Vector2(worldX, worldY);
                }

                if (!string.IsNullOrEmpty(zoneName))
                {
                    ZoneCoordinate coord = pin.GetOrCreateZoneCoordinate(zoneName);
                    coord.normalizedPosition = new Vector2(zoneX, zoneY);

                    if (!zoneMiniMaps.Any(z => z.zoneName == zoneName))
                    {
                        zoneMiniMaps.Add(new ZoneMiniMap { zoneName = zoneName });
                    }
                }
            }

            pins = importedPins.Values.ToList();
            selectedPinIndex = Mathf.Clamp(selectedPinIndex, 0, pins.Count - 1);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to import pins from CSV: {ex.Message}");
        }
    }

    private IEnumerable<string> ParseCsvLine(string line)
    {
        var cell = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                yield return cell.ToString();
                cell.Clear();
            }
            else
            {
                cell.Append(c);
            }
        }

        yield return cell.ToString();
    }

    private float ParseFloat(string value)
    {
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
        {
            return Mathf.Clamp01(result);
        }

        return 0f;
    }

    private static Vector2 Clamp01(Vector2 value)
    {
        return new Vector2(Mathf.Clamp01(value.x), Mathf.Clamp01(value.y));
    }

    private static Vector2 GuiToNormalized(Rect rect, Vector2 guiPosition)
    {
        float x = Mathf.InverseLerp(rect.xMin, rect.xMax, guiPosition.x);
        float y = Mathf.InverseLerp(rect.yMax, rect.yMin, guiPosition.y);
        return Clamp01(new Vector2(x, y));
    }

    private static Vector2 NormalizedToGui(Rect rect, Vector2 normalized)
    {
        float x = Mathf.Lerp(rect.xMin, rect.xMax, normalized.x);
        float y = Mathf.Lerp(rect.yMax, rect.yMin, normalized.y);
        return new Vector2(x, y);
    }

    private void RecordUndo(string label)
    {
        Undo.RecordObject(this, label);
    }

    [Serializable]
    private class PinCollection
    {
        public List<PinDefinition> pins = new List<PinDefinition>();
    }

    [Serializable]
    private class PinDefinition
    {
        public string id = "Pin";
        public Vector2 worldNormalizedPosition = new Vector2(0.5f, 0.5f);
        public List<ZoneCoordinate> zoneCoordinates = new List<ZoneCoordinate>();

        public ZoneCoordinate GetOrCreateZoneCoordinate(string zoneName)
        {
            var coord = zoneCoordinates.FirstOrDefault(c => c.zoneName == zoneName);
            if (coord == null)
            {
                coord = new ZoneCoordinate { zoneName = zoneName };
                zoneCoordinates.Add(coord);
            }

            return coord;
        }

        public PinDefinition Clone()
        {
            var clone = new PinDefinition
            {
                id = id,
                worldNormalizedPosition = worldNormalizedPosition,
                zoneCoordinates = zoneCoordinates.Select(coord => new ZoneCoordinate
                {
                    zoneName = coord.zoneName,
                    normalizedPosition = coord.normalizedPosition
                }).ToList()
            };

            return clone;
        }
    }

    [Serializable]
    private class ZoneCoordinate
    {
        public string zoneName;
        public Vector2 normalizedPosition = new Vector2(0.5f, 0.5f);
    }

    [Serializable]
    private class ZoneMiniMap
    {
        public string zoneName;
        public Texture2D miniMapMask;
    }
}
