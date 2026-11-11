using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.Pet;
using UPlayGround.Data.Save;
using System.Reflection;

namespace UPlayGround.Content.Tests
{
    public sealed class PetV2RulesTests
    {
        [Test]
        public void 지형_베이크_높이차가_있어도_평면_경로_지점을_통과한다()
        {
            var profile = ScriptableObject.CreateInstance<PetFollowProfileSO>();
            try
            {
                profile.navGroundTolerance = .6f;
                Assert.That(profile.IsGroundProjectionValid(51.827f, 52.257f), Is.True);
                Assert.That(profile.IsGroundProjectionValid(0f, .61f), Is.False);
                Assert.That(profile.IsPathCornerReached(Vector3.zero, new Vector3(.1f, .49f, .1f), Vector3.up), Is.True);
                Assert.That(profile.IsPathCornerReached(Vector3.zero, new Vector3(.31f, .49f, 0f), Vector3.up), Is.False);
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [Test]
        public void 축소_캡슐은_NavMesh_베이크_높이_허용치를_줄이지_않는다()
        {
            var profile = ScriptableObject.CreateInstance<PetFollowProfileSO>();
            try
            {
                profile.capsuleRadius = .055f;
                Assert.That(profile.IsGroundProjectionValid(0f, 1f / 6f), Is.True);
                Assert.That(profile.IsGroundProjectionValid(0f, .3f), Is.False);
                profile.maxGroundDifference = .1f;
                Assert.That(profile.IsGroundProjectionValid(0f, 1f / 6f), Is.False);
            }
            finally { Object.DestroyImmediate(profile); }
        }
        [Test]
        public void 중복입력_최대레벨_오래된수치로는_강화하지_않는다()
        {
            var skill = new PetSkillDefinition { skillId = PetSkillCatalogSO.Pickup };
            for (int i = 0; i < 5; i++) skill.levels.Add(new PetSkillLevel { upgradeCost = 100, explorationValue = 1f });
            var request = new PetUpgradeRequest(skill.skillId, 1, 100, skill.GetRevision());
            Assert.That(PetUpgradeRules.CanUpgrade(skill, 1, request), Is.True);
            Assert.That(PetUpgradeRules.CanUpgrade(skill, 2, request), Is.False);
            Assert.That(PetUpgradeRules.CanUpgrade(skill, 5,
                new PetUpgradeRequest(skill.skillId, 5, 100, skill.GetRevision())), Is.False);
            skill.levels[1].explorationValue = .5f;
            Assert.That(PetUpgradeRules.CanUpgrade(skill, 1, request), Is.False);
        }
        [Test]
        public void 근거리와_중단은_복귀를_허용하지_않는다()
        {
            var profile = ScriptableObject.CreateInstance<PetFollowProfileSO>();
            try
            {
                var gate = new PetRecallGate();
                for (int i = 0; i < 1200; i++) Assert.That(gate.Tick(19f, true, .25f, profile), Is.False);
                Assert.That(gate.Tick(30f, false, 30f, profile), Is.False);
                Assert.That(gate.Tick(30f, true, .5f, profile), Is.False);
                Assert.That(gate.Tick(19f, true, .1f, profile), Is.False);
                Assert.That(gate.Tick(30f, true, .5f, profile), Is.False);
            }
            finally { Object.DestroyImmediate(profile); }
        }
        [Test]
        public void 원거리_연속이탈과_복귀유예를_모두_요구한다()
        {
            var profile = ScriptableObject.CreateInstance<PetFollowProfileSO>();
            try
            {
                var gate = new PetRecallGate();
                Assert.That(gate.Tick(30f, true, .5f, profile), Is.False);
                Assert.That(gate.Tick(30f, true, .5f, profile), Is.True);
                gate.MarkRecalled(profile);
                Assert.That(gate.Tick(30f, true, 1f, profile), Is.False);
                Assert.That(gate.Tick(30f, true, 1f, profile), Is.False);
                Assert.That(gate.Tick(30f, true, 1f, profile), Is.True);
            }
            finally { Object.DestroyImmediate(profile); }
        }
        [Test]
        public void 버전35_펫은_공용성장을_보정하고_무펫은_지급하지_않는다()
        {
            var data = new GameSaveData { saveVersion = "3.5", pet = new PetSaveData
                { owned = new() { new() { petId = "Pet.Monkey" } }, selectedPetId = "Pet.Monkey" } };
            var type = System.Type.GetType("UPlayGround.Manager.SaveManager, Assembly-CSharp", true);
            type.GetMethod("MigrateToCurrentVersion", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { data });
            Assert.That(data.saveVersion, Is.EqualTo("3.6"));
            Assert.That(data.pet.skills.Count, Is.EqualTo(5));
            Assert.That(data.pet.skills.TrueForAll(skill => skill.level == 1), Is.True);
            Assert.That(data.pet.isAutoPickupEnabled, Is.True);
            Assert.That(data.pet.selectedSupportId, Is.EqualTo(PetSkillCatalogSO.Heal));
            Assert.That(PetSaveRules.Normalize(null).owned, Is.Empty);
        }
        [Test]
        public void 알려지지_않은_ID와_레벨은_보존하고_알려진_레벨만_보정한다()
        {
            var source = new PetSaveData { progressionVersion = 2, modelId = "Future.Model",
                selectedSupportId = "Future.Skill", isAutoPickupEnabled = false,
                skills = new() { new() { skillId = "Future.Skill", level = 91 },
                    new() { skillId = PetSkillCatalogSO.Heal, level = -3 } } };
            var result = PetSaveRules.Normalize(source);
            Assert.That(result.modelId, Is.EqualTo("Future.Model"));
            Assert.That(result.selectedSupportId, Is.EqualTo("Future.Skill"));
            Assert.That(result.isAutoPickupEnabled, Is.False);
            Assert.That(result.skills.Find(skill => skill.skillId == "Future.Skill").level, Is.EqualTo(91));
            Assert.That(result.skills.Find(skill => skill.skillId == PetSkillCatalogSO.Heal).level, Is.EqualTo(1));
            result.skills[0].level = 7;
            Assert.That(source.skills[0].level, Is.EqualTo(91));
        }
    }
}
