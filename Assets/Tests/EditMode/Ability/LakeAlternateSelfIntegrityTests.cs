using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Editor.Ability;

namespace UPlayGround.Ability.Tests
{
    /// <summary>분신의 실제 외형과 액터별 공격 해석을 함께 검사한다.</summary>
    public sealed class LakeAlternateSelfIntegrityTests
    {
        [Test]
        public void 대화_배치는_다른_인물의_머리를_지면으로_사용하지_않는다()
        {
            var ground = new GameObject("검사 바닥");
            var actor = new GameObject("먼저 배치된 인물");
            Vector3 origin = new Vector3(7777f, 500f, 7777f);
            try
            {
                ground.transform.position = origin + Vector3.down * .5f;
                ground.AddComponent<BoxCollider>();
                actor.transform.position = origin + Vector3.up * .5f;
                actor.AddComponent<TestGameActor>();
                actor.AddComponent<BoxCollider>();
                Physics.SyncTransforms();
                Assert.IsTrue(ActorStagePlacement.TryProbeGround(origin, origin.y, 2f, out Vector3 position));
                Assert.AreEqual(origin.y, position.y, .001f);
            }
            finally
            {
                Object.DestroyImmediate(actor);
                Object.DestroyImmediate(ground);
            }
        }

        [TestCase("Raon", "Raon")]
        [TestCase("Arin", "Nenmir")]
        public void 분신은_현재_주인공_메시와_실행가능한_전투를_가진다(string character, string legacyId)
        {
            var definition = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(
                "Assets/10.Datas/Actor/DataBase/Boss/MonsterBossAlternateSelf" + legacyId + ".asset");
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03.Prefabs/Actor/Player/Models/PlayerModel_" + character + ".prefab");
            Assert.IsNotNull(definition?.prefab);
            Assert.IsNotNull(player);
            CollectionAssert.AreEquivalent(
                player.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(renderer => renderer.sharedMesh),
                definition.prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(renderer => renderer.sharedMesh));
            foreach (Transform child in definition.prefab.GetComponentsInChildren<Transform>(true))
                Assert.Zero(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), child.name);
            var animator = definition.prefab.GetComponent<ActorAnimator>();
            Assert.IsNotNull(animator?.MotionSet);
            int attacks = 0;
            foreach (var ability in definition.EffectiveAbilitySet.EnumerateAll().Distinct())
            {
                foreach (var variant in ability.variants)
                {
                    if (!UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(variant, out var info) || !info.aiSelectable)
                        continue;
                    Assert.IsTrue(info.motionKey.IsValid, ability.name);
                    Assert.IsNotNull(animator.MotionSet.GetAbilityMotionAsset(info.motionKey), ability.name);
                    Assert.IsNotEmpty(info.baseInfo.hitPhases, ability.name);
                    attacks++;
                }
            }
            Assert.Greater(attacks, 0);
        }

        [Test]
        public void 전수검증_결과를_보존하고_분신의_연결된_데이터에는_오류가_없다()
        {
            var definition = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(
                "Assets/10.Datas/Actor/DataBase/Boss/MonsterBossAlternateSelfRaon.asset");
            var scope = new HashSet<Object> { definition.EffectiveAbilitySet };
            foreach (var ability in definition.EffectiveAbilitySet.EnumerateAll())
            {
                scope.Add(ability);
                foreach (var variant in ability.variants) scope.Add(variant.executionPayload);
            }
            var issues = AbilityDataValidator.ValidateAll();
            Directory.CreateDirectory("tmp/narrative-overhaul");
            File.WriteAllLines("tmp/narrative-overhaul/ability-validation.txt",
                issues.Select(issue => $"{issue.Severity}: {issue.Context?.name}: {issue.Message}"));
            var failures = issues.Where(issue => issue.Severity == AbilityValidationSeverity.Error
                && scope.Contains(issue.Context)).Select(issue => issue.Message).ToArray();
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }
    }
}
