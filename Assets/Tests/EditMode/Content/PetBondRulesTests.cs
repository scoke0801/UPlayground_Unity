using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.Pet;

namespace UPlayGround.Content.Tests
{
    public sealed class PetBondRulesTests
    {
        private PetBondProgressionSO _data;
        private PetSaveData _save;
        [SetUp] public void SetUp() { _data = ScriptableObject.CreateInstance<PetBondProgressionSO>(); _save = PetSaveRules.Normalize(null); }
        [TearDown] public void TearDown() => Object.DestroyImmediate(_data);

        [TestCase(2)] [TestCase(3)]
        public void 기존세이브_유대는_합계가아닌_최댓값을_한번만_이전한다(int version)
        {
            var source = new PetSaveData { progressionVersion = version, sharedBondPoints = 10,
                owned = new() { new() { petId = "A", bondPoints = 120 }, new() { petId = "B", bondPoints = 80 } },
                supportSlots = new() { "Future.Support", PetSkillCatalogSO.AttackBuff } };
            var migrated = PetSaveRules.Normalize(source);
            Assert.That(migrated.progressionVersion, Is.EqualTo(3));
            Assert.That(migrated.sharedBondPoints, Is.EqualTo(120));
            Assert.That(migrated.bondMigrationVersion, Is.EqualTo(1));
            Assert.That(source.bondMigrationVersion, Is.Zero);
            migrated.sharedBondPoints = 50;
            var second = PetSaveRules.Normalize(migrated);
            Assert.That(second.sharedBondPoints, Is.EqualTo(50));
            Assert.That(second.supportSlots, Is.EqualTo(source.supportSlots));
            Assert.That(second.owned[0].bondPoints, Is.EqualTo(120));
        }
        [Test]
        public void 미래_마커와_유대원장은_사본으로_보존한다()
        {
            _save.bondMigrationVersion = 9; _save.sharedBondPoints = 400;
            _save.bondLedger.daily[0] = 15; _save.bondLedger.window[2] = 17;
            _save.bondLedger.lifestyleRemainder = 41; _save.bondLedger.windowActiveSeconds = 790;
            PetBondRules.Claim(_save.bondLedger, "Quest.A", "Goal.B");
            var copy = PetSaveRules.Normalize(_save);
            Assert.That(copy.bondMigrationVersion, Is.EqualTo(9));
            Assert.That(copy.bondLedger.windowActiveSeconds, Is.EqualTo(790));
            Assert.That(copy.bondLedger.lifestyleRemainder, Is.EqualTo(41));
            copy.bondLedger.daily[0] = 0; copy.bondLedger.claims[0].questId = "Changed";
            Assert.That(_save.bondLedger.daily[0], Is.EqualTo(15));
            Assert.That(_save.bondLedger.claims[0].questId, Is.EqualTo("Quest.A"));
        }
        [Test]
        public void 한도초과는_감쇠하되_한도들이_중복감쇠하지_않는다()
        {
            _data.sourceCaps = new[] { 10f, 10f, 10f }; _data.encounterCap = 10f;
            Assert.That(PetBondRules.Award(_save, _data, PetBondSource.Combat, 12f, 1, true), Is.EqualTo(10.4f).Within(.001f));
            Assert.That(PetBondRules.Award(_save, _data, PetBondSource.Combat, 5f, 1, true), Is.EqualTo(1f).Within(.001f));
            Assert.That(_save.sharedBondPoints, Is.EqualTo(11));
        }
        [Test]
        public void 잠으로_날이_바뀌어도_활동창_한도와_퀘스트청구는_남는다()
        {
            _data.sourceCaps = new[] { 10f, 10f, 10f };
            PetBondRules.Award(_save, _data, PetBondSource.Quest, 10f, 1);
            PetBondRules.Claim(_save.bondLedger, "Quest", "Goal");
            Assert.That(PetBondRules.Award(_save, _data, PetBondSource.Quest, 5f, 2), Is.EqualTo(1f));
            Assert.That(_save.bondLedger.daily[2], Is.EqualTo(5f));
            Assert.That(_save.bondLedger.window[2], Is.EqualTo(15f));
            Assert.That(PetBondRules.Claim(_save.bondLedger, "Quest", "Goal"), Is.False);
        }
        [Test]
        public void 목표청구는_식별자쌍으로_구별하고_퀘스트완료와_독립이다()
        {
            Assert.That(PetBondRules.Claim(_save.bondLedger, "A:B", "C"), Is.True);
            Assert.That(PetBondRules.Claim(_save.bondLedger, "A", "B:C"), Is.True);
            Assert.That(PetBondRules.Claim(_save.bondLedger, "A", null), Is.True);
            var restored = PetSaveRules.Normalize(_save);
            Assert.That(PetBondRules.Claim(restored.bondLedger, "A:B", "C"), Is.False);
        }
        [Test]
        public void 균형잡힌_세원천이_한원천_반복보다_빠르다()
        {
            _data.sourceCaps = new[] { 10f, 10f, 10f };
            var balanced = PetSaveRules.Normalize(null);
            PetBondRules.Award(_save, _data, PetBondSource.Combat, 30f, 1);
            foreach (PetBondSource source in System.Enum.GetValues(typeof(PetBondSource)))
                PetBondRules.Award(balanced, _data, source, 10f, 1);
            Assert.That(balanced.sharedBondPoints, Is.GreaterThan(_save.sharedBondPoints));
        }
        [TestCase(0, 1, 1)] [TestCase(99, 1, 1)] [TestCase(100, 2, 2)] [TestCase(399, 2, 2)] [TestCase(400, 3, 2)]
        public void 유대_경계에서만_두슬롯과_시그니처를_연다(long points, int stage, int slots)
        { Assert.That(_data.GetStage(points), Is.EqualTo(stage)); Assert.That(_data.GetSlotCount(points), Is.EqualTo(slots)); }

        [Test]
        public void 방치시간은_쌓지않고_전투활동이_생활적립으로_새지않는다()
        {
            Assert.That(PetBondRules.AdvanceActivity(_save.bondLedger, _data, 1800f, false, false), Is.Zero);
            Assert.That(_save.bondLedger.windowActiveSeconds, Is.Zero);
            PetBondRules.AdvanceActivity(_save.bondLedger, _data, 60f, false, true);
            Assert.That(_save.bondLedger.lifestyleRemainder, Is.Zero);
            Assert.That(_save.bondLedger.windowActiveSeconds, Is.EqualTo(60f));
            Assert.That(PetBondRules.AdvanceActivity(_save.bondLedger, _data, 60f, true, false), Is.EqualTo(1f));
        }
        [Test]
        public void 실제활동창이_차야_감쇠보호창을_갱신한다()
        {
            _save.bondLedger.window[0] = 30;
            _save.bondLedger.windowActiveSeconds = 1799f;
            PetBondRules.AdvanceActivity(_save.bondLedger, _data, .5f, true, false);
            Assert.That(_save.bondLedger.window[0], Is.EqualTo(30));
            PetBondRules.AdvanceActivity(_save.bondLedger, _data, .5f, true, false);
            Assert.That(_save.bondLedger.window[0], Is.Zero);
        }
        [Test]
        public void 응급회복과_방치중_자동버팀은_유대를_주지않는다()
        {
            Assert.That(_data.GetSupportPoints(PetSupportTriggerKind.HealthBelow, true), Is.Zero);
            Assert.That(_data.GetSupportPoints(PetSupportTriggerKind.HeavyHitTaken, false), Is.Zero);
            Assert.That(_data.GetSupportPoints(PetSupportTriggerKind.HeavyHitTaken, true), Is.EqualTo(2f));
        }
    }
}
