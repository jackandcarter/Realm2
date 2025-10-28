using Client.CharacterCreation;
using NUnit.Framework;

namespace Client.Tests
{
    public class MainMenuCharacterClassStatusTests
    {
        [Test]
        public void Evaluate_ReturnsValidForUnlockedStarter()
        {
            var character = new CharacterInfo
            {
                name = "Aeloria",
                raceId = "felarian",
                classId = "ranger",
                classStates = new[]
                {
                    new ClassUnlockState { ClassId = "ranger", Unlocked = true }
                }
            };

            var result = CharacterClassStatusUtility.Evaluate(character);

            Assert.AreEqual(CharacterClassStatus.Valid, result.Status);
            Assert.IsTrue(result.CanPlay, "Unlocked starter class should be playable.");
            Assert.AreEqual("Unlocked", result.StatusLabel);
        }

        [Test]
        public void Evaluate_FlagsForbiddenClass()
        {
            var character = new CharacterInfo
            {
                name = "Shade",
                raceId = "revenant",
                classId = "ranger",
                classStates = new[]
                {
                    new ClassUnlockState { ClassId = "ranger", Unlocked = true }
                }
            };

            var result = CharacterClassStatusUtility.Evaluate(character);

            Assert.AreEqual(CharacterClassStatus.Forbidden, result.Status);
            Assert.IsFalse(result.CanPlay, "Forbidden classes must not be playable.");
            StringAssert.Contains("Forbidden", result.StatusLabel);
        }

        [Test]
        public void Evaluate_FlagsLockedQuestClass()
        {
            var character = new CharacterInfo
            {
                name = "Maker",
                raceId = "human",
                classId = ClassUnlockUtility.BuilderClassId,
                classStates = new[]
                {
                    new ClassUnlockState { ClassId = ClassUnlockUtility.BuilderClassId, Unlocked = false }
                }
            };

            var result = CharacterClassStatusUtility.Evaluate(character);

            Assert.AreEqual(CharacterClassStatus.Locked, result.Status);
            Assert.IsFalse(result.CanPlay, "Locked quest classes should not be playable.");
            Assert.AreEqual("Locked", result.StatusLabel);
        }

        [Test]
        public void Evaluate_FlagsStaleStarterData()
        {
            var character = new CharacterInfo
            {
                name = "Bram",
                raceId = "human",
                classId = "warrior",
                classStates = new[]
                {
                    new ClassUnlockState { ClassId = "warrior", Unlocked = false }
                }
            };

            var result = CharacterClassStatusUtility.Evaluate(character);

            Assert.AreEqual(CharacterClassStatus.Stale, result.Status);
            Assert.IsFalse(result.CanPlay, "Stale starter data should block play until resolved.");
            Assert.AreEqual("Starter locked", result.StatusLabel);
        }

        [Test]
        public void Evaluate_FlagsUnknownClassData()
        {
            var character = new CharacterInfo
            {
                name = "Mystery",
                raceId = "human",
                classId = "ancient-guardian",
                classStates = new[]
                {
                    new ClassUnlockState { ClassId = "ancient-guardian", Unlocked = true }
                }
            };

            var result = CharacterClassStatusUtility.Evaluate(character);

            Assert.AreEqual(CharacterClassStatus.Unavailable, result.Status);
            Assert.AreEqual("Unavailable", result.StatusLabel);
            Assert.IsFalse(result.CanPlay, "Unknown classes should not be playable.");
        }
    }
}
