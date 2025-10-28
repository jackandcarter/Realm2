using Realm.Data;
using UnityEditor;
using UnityEngine;

namespace Realm.Editor
{
    [CustomPropertyDrawer(typeof(ClassStatCurve))]
    public class ClassStatCurveDrawer : PropertyDrawer
    {
        private const float CurveFieldHeight = 48f;
        private const float PreviewHeight = 70f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
            {
                return height;
            }

            var spacing = EditorGUIUtility.standardVerticalSpacing;
            height += spacing; // space after foldout

            height += EditorGUIUtility.singleLineHeight + spacing; // stat field
            height += CurveFieldHeight + spacing; // base curve
            height += CurveFieldHeight + spacing; // growth curve
            height += CurveFieldHeight + spacing; // soft cap curve
            height += EditorGUIUtility.singleLineHeight + spacing; // jitter controls
            height += EditorGUIUtility.singleLineHeight + spacing; // formula template

            var templateProp = property.FindPropertyRelative("formulaTemplate");
            var helpHeight = GetTemplateHelpHeight(templateProp);
            if (helpHeight > 0f)
            {
                height += helpHeight + spacing;
            }

            var coefficients = property.FindPropertyRelative("formulaCoefficients");
            if (coefficients != null)
            {
                var count = coefficients.arraySize;
                for (var i = 0; i < count; i++)
                {
                    height += EditorGUIUtility.singleLineHeight + spacing;
                }
            }

            height += PreviewHeight + spacing; // total preview
            height += PreviewHeight; // jitter preview

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var statProp = property.FindPropertyRelative("stat");
            var baseCurveProp = property.FindPropertyRelative("baseValues");
            var growthCurveProp = property.FindPropertyRelative("growthValues");
            var softCapProp = property.FindPropertyRelative("softCapCurve");
            var jitterProp = property.FindPropertyRelative("jitterVariance");
            var templateProp = property.FindPropertyRelative("formulaTemplate");
            var coefficientsProp = property.FindPropertyRelative("formulaCoefficients");

            var displayLabel = GetDisplayLabel(label, statProp);

            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, displayLabel, true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;

            var y = foldoutRect.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            var statRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(statRect, statProp);
            y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            DrawCurveField(ref y, position, baseCurveProp, "Base Values");
            DrawCurveField(ref y, position, growthCurveProp, "Growth Curve");
            DrawCurveField(ref y, position, softCapProp, "Soft Cap");

            DrawJitterControls(ref y, position, jitterProp);

            var templateRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(templateRect, templateProp);

            y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            var templateHelpHeight = GetTemplateHelpHeight(templateProp);
            if (templateHelpHeight > 0f)
            {
                var helpRect = new Rect(position.x, y, position.width, templateHelpHeight);
                helpRect = EditorGUI.IndentedRect(helpRect);
                var template = (JrpgFormulaTemplate)templateProp.enumValueIndex;
                var description = JrpgFormulaTemplateLibrary.GetTemplateDescription(template);
                EditorGUI.HelpBox(helpRect, description, MessageType.None);
                y += templateHelpHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            SynchronizeCoefficients(templateProp, coefficientsProp);
            DrawCoefficientFields(ref y, position, coefficientsProp);

            DrawPreviewGraphs(ref y, position, baseCurveProp, growthCurveProp, softCapProp, jitterProp);

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        private static GUIContent GetDisplayLabel(GUIContent original, SerializedProperty statProp)
        {
            if (statProp != null && statProp.objectReferenceValue is StatDefinition stat)
            {
                var label = stat.DisplayName;
                if (string.IsNullOrEmpty(label))
                {
                    label = stat.name;
                }

                if (!string.IsNullOrEmpty(label))
                {
                    return new GUIContent(label);
                }
            }

            return original;
        }

        private static void DrawCurveField(ref float y, Rect position, SerializedProperty property, string label)
        {
            var rect = new Rect(position.x, y, position.width, CurveFieldHeight);
            EditorGUI.PropertyField(rect, property, new GUIContent(label));
            y += CurveFieldHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        private static void DrawJitterControls(ref float y, Rect position, SerializedProperty jitterProp)
        {
            var jitterRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            var contentRect = EditorGUI.PrefixLabel(jitterRect, new GUIContent("Jitter Variance"));

            var fieldWidth = 60f;
            var spacing = 4f;
            var minRect = new Rect(contentRect.x, contentRect.y, fieldWidth, jitterRect.height);
            var maxRect = new Rect(contentRect.xMax - fieldWidth, contentRect.y, fieldWidth, jitterRect.height);
            var sliderRect = new Rect(minRect.xMax + spacing, contentRect.y, contentRect.width - fieldWidth * 2 - spacing * 2, jitterRect.height);

            var jitter = jitterProp.vector2Value;
            var minValue = jitter.x;
            var maxValue = jitter.y;

            EditorGUI.BeginChangeCheck();
            minValue = EditorGUI.FloatField(minRect, minValue);
            maxValue = EditorGUI.FloatField(maxRect, maxValue);
            EditorGUI.MinMaxSlider(sliderRect, ref minValue, ref maxValue, -999f, 999f);
            if (minValue > maxValue)
            {
                var temp = minValue;
                minValue = maxValue;
                maxValue = temp;
            }

            if (EditorGUI.EndChangeCheck())
            {
                jitterProp.vector2Value = new Vector2(minValue, maxValue);
            }

            y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        private static void SynchronizeCoefficients(SerializedProperty templateProp, SerializedProperty coefficientsProp)
        {
            if (templateProp == null || coefficientsProp == null || templateProp.hasMultipleDifferentValues)
            {
                return;
            }

            var template = (JrpgFormulaTemplate)templateProp.enumValueIndex;
            var expectedKeys = JrpgFormulaTemplateLibrary.GetCoefficientKeys(template);

            coefficientsProp.arraySize = expectedKeys.Count;
            for (var i = 0; i < expectedKeys.Count; i++)
            {
                var element = coefficientsProp.GetArrayElementAtIndex(i);
                var keyProp = element.FindPropertyRelative("key");
                if (keyProp != null)
                {
                    keyProp.stringValue = expectedKeys[i];
                }
            }
        }

        private static void DrawCoefficientFields(ref float y, Rect position, SerializedProperty coefficientsProp)
        {
            if (coefficientsProp == null)
            {
                return;
            }

            for (var i = 0; i < coefficientsProp.arraySize; i++)
            {
                var element = coefficientsProp.GetArrayElementAtIndex(i);
                var keyProp = element.FindPropertyRelative("key");
                var valueProp = element.FindPropertyRelative("value");

                if (valueProp == null)
                {
                    continue;
                }

                var rect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
                var label = keyProp != null ? ObjectNames.NicifyVariableName(keyProp.stringValue) : $"Coefficient {i + 1}";
                EditorGUI.PropertyField(rect, valueProp, new GUIContent(label));
                y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        private static float GetTemplateHelpHeight(SerializedProperty templateProp)
        {
            if (templateProp == null || templateProp.hasMultipleDifferentValues)
            {
                return 0f;
            }

            var template = (JrpgFormulaTemplate)templateProp.enumValueIndex;
            var description = JrpgFormulaTemplateLibrary.GetTemplateDescription(template);
            if (string.IsNullOrEmpty(description))
            {
                return 0f;
            }

            var width = EditorGUIUtility.currentViewWidth - 40f;
            var height = EditorStyles.helpBox.CalcHeight(new GUIContent(description), width);
            return Mathf.Max(height, EditorGUIUtility.singleLineHeight * 1.2f);
        }

        private static void DrawPreviewGraphs(ref float y, Rect position, SerializedProperty baseCurveProp, SerializedProperty growthCurveProp, SerializedProperty softCapProp, SerializedProperty jitterProp)
        {
            var totalCurve = BuildTotalCurve(baseCurveProp, growthCurveProp, softCapProp);
            var jitterCurve = BuildJitterCurve(totalCurve, jitterProp);

            var viewRect = CalculateViewRect(totalCurve, jitterCurve);

            EditorGUI.BeginDisabledGroup(true);
            var totalRect = new Rect(position.x, y, position.width, PreviewHeight);
            EditorGUI.CurveField(totalRect, new GUIContent("Total Progression"), totalCurve, Color.green, viewRect);
            y += PreviewHeight + EditorGUIUtility.standardVerticalSpacing;

            var jitterRect = new Rect(position.x, y, position.width, PreviewHeight);
            EditorGUI.CurveField(jitterRect, new GUIContent("Jitter Sample"), jitterCurve, new Color(1f, 0.6f, 0.2f), viewRect);
            y += PreviewHeight;
            EditorGUI.EndDisabledGroup();
        }

        private static AnimationCurve BuildTotalCurve(SerializedProperty baseProp, SerializedProperty growthProp, SerializedProperty softCapProp)
        {
            var baseCurve = baseProp?.animationCurveValue;
            var growthCurve = growthProp?.animationCurveValue;
            var softCapCurve = softCapProp?.animationCurveValue;

            var totalCurve = new AnimationCurve();
            const int samples = 24;

            float minLevel = 1f;
            float maxLevel = 100f;
            DetermineLevelRange(baseCurve, ref minLevel, ref maxLevel);
            DetermineLevelRange(growthCurve, ref minLevel, ref maxLevel);
            DetermineLevelRange(softCapCurve, ref minLevel, ref maxLevel);

            if (Mathf.Approximately(minLevel, maxLevel))
            {
                maxLevel = minLevel + 1f;
            }

            for (var i = 0; i < samples; i++)
            {
                var t = samples <= 1 ? 0f : i / (float)(samples - 1);
                var level = Mathf.Lerp(minLevel, maxLevel, t);

                var baseValue = baseCurve != null ? baseCurve.Evaluate(level) : 0f;
                var growthValue = growthCurve != null ? growthCurve.Evaluate(level) : 0f;
                var total = baseValue + growthValue;

                if (softCapCurve != null && softCapCurve.length > 0)
                {
                    var cap = softCapCurve.Evaluate(level);
                    if (cap > 0f)
                    {
                        total = Mathf.Min(total, cap);
                    }
                }

                totalCurve.AddKey(new Keyframe(level, total));
            }

            return totalCurve;
        }

        private static void DetermineLevelRange(AnimationCurve curve, ref float minLevel, ref float maxLevel)
        {
            if (curve == null || curve.length == 0)
            {
                return;
            }

            var keys = curve.keys;
            if (keys.Length <= 0)
            {
                return;
            }

            minLevel = Mathf.Min(minLevel, keys[0].time);
            maxLevel = Mathf.Max(maxLevel, keys[keys.Length - 1].time);
        }

        private static AnimationCurve BuildJitterCurve(AnimationCurve totalCurve, SerializedProperty jitterProp)
        {
            var jitterCurve = new AnimationCurve();
            if (totalCurve == null || totalCurve.length == 0)
            {
                return jitterCurve;
            }

            var jitter = jitterProp != null ? jitterProp.vector2Value : Vector2.zero;
            var min = Mathf.Min(jitter.x, jitter.y);
            var max = Mathf.Max(jitter.x, jitter.y);

            for (var i = 0; i < totalCurve.length; i++)
            {
                var key = totalCurve.keys[i];
                var noise = Mathf.PerlinNoise(key.time * 0.13f, 0.61f);
                var sample = Mathf.Lerp(min, max, noise);
                jitterCurve.AddKey(new Keyframe(key.time, key.value + sample));
            }

            return jitterCurve;
        }

        private static Rect CalculateViewRect(AnimationCurve totalCurve, AnimationCurve jitterCurve)
        {
            var minLevel = float.MaxValue;
            var maxLevel = float.MinValue;
            var minValue = float.MaxValue;
            var maxValue = float.MinValue;

            ExpandBounds(totalCurve, ref minLevel, ref maxLevel, ref minValue, ref maxValue);
            ExpandBounds(jitterCurve, ref minLevel, ref maxLevel, ref minValue, ref maxValue);

            if (!float.IsFinite(minLevel) || !float.IsFinite(maxLevel) || Mathf.Approximately(minLevel, maxLevel))
            {
                minLevel = 0f;
                maxLevel = 100f;
            }

            if (!float.IsFinite(minValue) || !float.IsFinite(maxValue) || Mathf.Approximately(minValue, maxValue))
            {
                minValue = 0f;
                maxValue = 1f;
            }

            var width = Mathf.Max(1f, maxLevel - minLevel);
            var height = Mathf.Max(1f, maxValue - minValue);
            return new Rect(minLevel, minValue, width, height);
        }

        private static void ExpandBounds(AnimationCurve curve, ref float minLevel, ref float maxLevel, ref float minValue, ref float maxValue)
        {
            if (curve == null || curve.length == 0)
            {
                return;
            }

            foreach (var key in curve.keys)
            {
                minLevel = Mathf.Min(minLevel, key.time);
                maxLevel = Mathf.Max(maxLevel, key.time);
                minValue = Mathf.Min(minValue, key.value);
                maxValue = Mathf.Max(maxValue, key.value);
            }
        }
    }
}
