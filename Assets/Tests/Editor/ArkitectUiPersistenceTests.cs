#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using Building;
using Client.UI;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public class ArkitectUiPersistenceTests
    {
        [SetUp]
        public void SetUp()
        {
            ArkitectRegistry.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            ArkitectRegistry.ResetForTests();
        }

        [Test]
        public void ArkitectUiManager_ReusesRegisteredPanels()
        {
            var root = new GameObject("ArkitectCanvas");
            try
            {
                var manager = root.AddComponent<ArkitectUIManager>();
                InvokeInitialize(manager);
                var firstSnapshot = CapturePanelSnapshot(manager);

                InvokeInitialize(manager);
                var secondSnapshot = CapturePanelSnapshot(manager);

                CollectionAssert.AreEquivalent(firstSnapshot.Keys, secondSnapshot.Keys);
                foreach (var key in firstSnapshot.Keys)
                {
                    Assert.AreSame(firstSnapshot[key], secondSnapshot[key],
                        $"Panel '{key}' should be reused between initializations.");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ArkitectUiManager_RetainsPanelsAfterComponentRecreation()
        {
            var root = new GameObject("ArkitectCanvas");
            try
            {
                var manager = root.AddComponent<ArkitectUIManager>();
                InvokeInitialize(manager);
                var firstSnapshot = CapturePanelSnapshot(manager);

                Object.DestroyImmediate(manager);
                manager = root.AddComponent<ArkitectUIManager>();

                InvokeInitialize(manager);
                var secondSnapshot = CapturePanelSnapshot(manager);

                CollectionAssert.AreEquivalent(firstSnapshot.Keys, secondSnapshot.Keys);
                foreach (var key in firstSnapshot.Keys)
                {
                    Assert.AreSame(firstSnapshot[key], secondSnapshot[key],
                        $"Panel '{key}' should persist after recreating the manager.");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void InvokeInitialize(ArkitectUIManager manager)
        {
            var method = typeof(ArkitectUIManager).GetMethod("InitializeUi", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "InitializeUi method could not be located via reflection.");
            method.Invoke(manager, null);
        }

        private static Dictionary<string, GameObject> CapturePanelSnapshot(ArkitectUIManager manager)
        {
            var snapshot = new Dictionary<string, GameObject>
            {
                { "Panels", manager.transform.Find("Panels")?.gameObject },
                { "TabBar", manager.transform.Find("TabBar")?.gameObject },
                { "PlotsPanel", manager.transform.Find("Panels/PlotsPanel")?.gameObject },
                { "MaterialsPanel", manager.transform.Find("Panels/MaterialsPanel")?.gameObject },
                { "BlueprintsPanel", manager.transform.Find("Panels/BlueprintsPanel")?.gameObject },
                { "CommissionsPanel", manager.transform.Find("Panels/CommissionsPanel")?.gameObject }
            };

            foreach (var pair in snapshot)
            {
                Assert.IsNotNull(pair.Value, $"Expected '{pair.Key}' to exist after initialization.");
            }

            return snapshot;
        }
    }
}
#endif
