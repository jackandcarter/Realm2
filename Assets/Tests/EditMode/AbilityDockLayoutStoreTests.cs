using System.Collections.Generic;
using Client.UI.HUD.Dock;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class AbilityDockLayoutStoreTests
    {
        private const string TestClass = "unit-test-class";

        [SetUp]
        public void SetUp()
        {
            AbilityDockLayoutStore.Clear(TestClass);
        }

        [TearDown]
        public void TearDown()
        {
            AbilityDockLayoutStore.Clear(TestClass);
        }

        [Test]
        public void ReturnsDefaultOrderWhenNoLayoutStored()
        {
            var defaultOrder = new List<string> { "a", "b", "c" };
            var result = AbilityDockLayoutStore.GetLayout(TestClass, defaultOrder);

            CollectionAssert.AreEqual(defaultOrder, result);
        }

        [Test]
        public void PersistsCustomLayout()
        {
            var customOrder = new List<string> { "c", "a", "b" };
            AbilityDockLayoutStore.SaveLayout(TestClass, customOrder);

            var retrieved = AbilityDockLayoutStore.GetLayout(TestClass, null);
            CollectionAssert.AreEqual(customOrder, retrieved);
        }

        [Test]
        public void ClearRemovesStoredLayout()
        {
            var customOrder = new List<string> { "x", "y" };
            AbilityDockLayoutStore.SaveLayout(TestClass, customOrder);
            AbilityDockLayoutStore.Clear(TestClass);

            var fallback = new List<string> { "fallback" };
            var retrieved = AbilityDockLayoutStore.GetLayout(TestClass, fallback);
            CollectionAssert.AreEqual(fallback, retrieved);
        }

        [Test]
        public void ClearAllRemovesLayouts()
        {
            var customOrder = new List<string> { "x", "y" };
            AbilityDockLayoutStore.SaveLayout(TestClass, customOrder);
            AbilityDockLayoutStore.ClearAll();

            var fallback = new List<string> { "fallback" };
            var retrieved = AbilityDockLayoutStore.GetLayout(TestClass, fallback);
            CollectionAssert.AreEqual(fallback, retrieved);
        }
    }
}
