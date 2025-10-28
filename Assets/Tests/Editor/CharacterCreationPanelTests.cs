using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Client.Tests
{
    public class CharacterCreationPanelTests
    {
        [Test]
        public void PopulateClassButtons_UsesStarterRulesAndMarksLockedClasses()
        {
            var root = new GameObject("CharacterCreationPanel_TestRoot");
            try
            {
                var panel = root.AddComponent<Client.CharacterCreation.CharacterCreationPanel>();
                var listRoot = new GameObject("ClassListRoot", typeof(RectTransform)).transform;
                listRoot.SetParent(root.transform);

                var templateGo = new GameObject("ClassButtonTemplate", typeof(RectTransform));
                templateGo.transform.SetParent(root.transform);
                var button = templateGo.AddComponent<Button>();
                templateGo.SetActive(false);

                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(templateGo.transform, false);
                var label = labelGo.AddComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

                SetPrivateField(panel, "classListRoot", listRoot);
                SetPrivateField(panel, "classButtonTemplate", button);

                InvokePrivateMethod(panel, "ApplyClassStates", new object[]
                {
                    new[]
                    {
                        new Client.CharacterCreation.ClassUnlockState
                        {
                            ClassId = "time-mage",
                            Unlocked = false
                        }
                    }
                });

                var race = new Client.CharacterCreation.RaceDefinition
                {
                    Id = "felarian",
                    DisplayName = "Felarian"
                };

                InvokePrivateMethod(panel, "PopulateClassButtons", new object[] { race });

                var expectedStarterIds = Client.CharacterCreation.ClassRulesCatalog
                    .GetStarterClassIdsForRace(race.Id)
                    .Select(id => id.Trim())
                    .ToArray();

                var buttons = new List<Button>();
                foreach (Transform child in listRoot)
                {
                    var spawnedButton = child.GetComponent<Button>();
                    if (spawnedButton != null)
                    {
                        buttons.Add(spawnedButton);
                    }
                }

                Assert.AreEqual(expectedStarterIds.Length, buttons.Count, "Starter class count mismatch.");

                foreach (var starterId in expectedStarterIds)
                {
                    Assert.IsTrue(buttons.Any(b => string.Equals(b.name, $"Class_{starterId}", StringComparison.Ordinal)),
                        $"Missing button for starter class '{starterId}'.");
                }

                var timeMageButton = buttons.FirstOrDefault(b => b.name == "Class_time-mage");
                Assert.IsNotNull(timeMageButton, "Expected Time Mage button to be present.");
                Assert.IsFalse(timeMageButton.interactable, "Locked classes should not be interactable.");

                var timeMageLabel = timeMageButton.GetComponentInChildren<Text>();
                Assert.IsNotNull(timeMageLabel, "Time Mage button missing label.");
                StringAssert.Contains("Locked", timeMageLabel.text, "Locked label should explain unavailable classes.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                Assert.Fail($"Field '{fieldName}' not found on {instance.GetType().Name}.");
            }

            field.SetValue(instance, value);
        }

        private static object InvokePrivateMethod(object instance, string methodName, object[] parameters)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                Assert.Fail($"Method '{methodName}' not found on {instance.GetType().Name}.");
            }

            return method.Invoke(instance, parameters);
        }
    }
}
