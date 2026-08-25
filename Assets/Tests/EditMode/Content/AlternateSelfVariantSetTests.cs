using NUnit.Framework;
using UnityEditor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Story;

namespace UPlayGround.Content.Tests
{
    public sealed class AlternateSelfVariantSetTests
    {
        private const string VariantSetPath =
            "Assets/10.Datas/Story/LakeOfLife/AlternateSelfVariantSet_LakeOfLife.asset";

        private AlternateSelfVariantSetSO _variantSet;

        [SetUp]
        public void SetUp()
        {
            _variantSet = AssetDatabase.LoadAssetAtPath<
                AlternateSelfVariantSetSO>(VariantSetPath);
            Assert.IsNotNull(
                _variantSet,
                $"최종 보스 Variant Set을 찾을 수 없습니다: {VariantSetPath}");
        }

        [Test]
        public void 라온은_기존_라온_보스로_연결된다()
        {
            Assert.IsTrue(_variantSet.TryGetVariant(
                CharacterActorType.Raon,
                out var bossActor));
            Assert.AreEqual("BossAlternateSelfRaon", bossActor.actorId);
            Assert.AreEqual(CharacterActorType.Raon, bossActor.characterType);
            Assert.IsNotNull(bossActor.prefab);
            Assert.IsNotNull(bossActor.EffectiveAbilitySet);
            Assert.IsNotNull(bossActor.EffectiveBehaviorData);
            Assert.AreEqual(MonsterActorGrade.Boss, bossActor.EffectiveGrade);
            Assert.AreEqual(CharacterActorType.None, bossActor.EffectiveRecruitableAs);
            Assert.IsNull(bossActor.EffectiveDropTable);
            Assert.Zero(bossActor.EffectiveExpReward);
            Assert.Zero(bossActor.EffectiveGoldReward);
        }

        [Test]
        public void 아린은_Nenmir_전투_데이터로_연결된다()
        {
            Assert.IsTrue(_variantSet.TryGetVariant(
                CharacterActorType.Arin,
                out var bossActor));
            Assert.AreEqual("BossAlternateSelfNenmir", bossActor.actorId);
            Assert.AreEqual(CharacterActorType.SeolA, bossActor.characterType);
            Assert.IsNotNull(bossActor.prefab);
            Assert.IsNotNull(bossActor.EffectiveAbilitySet);
            Assert.IsNotNull(bossActor.EffectiveBehaviorData);
            Assert.AreEqual(MonsterActorGrade.Boss, bossActor.EffectiveGrade);
            Assert.AreEqual(CharacterActorType.None, bossActor.EffectiveRecruitableAs);
            Assert.IsNull(bossActor.EffectiveDropTable);
            Assert.Zero(bossActor.EffectiveExpReward);
            Assert.Zero(bossActor.EffectiveGoldReward);
        }

        [Test]
        public void 미지원_주인공은_다른_보스로_폴백하지_않는다()
        {
            Assert.IsFalse(_variantSet.TryGetVariant(
                CharacterActorType.SeolA,
                out var bossActor));
            Assert.IsNull(bossActor);
        }
    }
}
