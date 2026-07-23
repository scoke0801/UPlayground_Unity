#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.Stat;
using UPlayGround.Data.Ability;
using UPlayGround.Gameplay.Ability;

namespace UPlayGround.Ability.Tests
{
    public sealed class ActorStatAttributeMigrationTests
    {
        [Test]
        public void 모든_ActorStatSO는_고유한_AttributeId로_Shadow값이_일치한다()
        {
            string[] guids = AssetDatabase.FindAssets("t:ActorStatSO");
            var failures = new List<string>();
            Assert.That(guids, Is.Not.Empty, "ActorStatSO 기준 데이터가 없습니다.");

            for (int assetIndex = 0; assetIndex < guids.Length; assetIndex++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[assetIndex]);
                ActorStatSO source = AssetDatabase.LoadAssetAtPath<ActorStatSO>(path);
                if (source == null)
                {
                    failures.Add($"{path}: 로드 실패");
                    continue;
                }

                var runtime = new AttributeSetRuntime();
                var ids = new HashSet<AttributeId>();
                foreach (StatType statType in Enum.GetValues(typeof(StatType)))
                {
                    if (!UPlayGroundAttributeMapping.TryGetAttributeId(statType, out AttributeId id)
                        || !id.IsValid)
                    {
                        failures.Add($"{path}: {statType} 매핑 누락");
                        continue;
                    }
                    if (!ids.Add(id))
                    {
                        failures.Add($"{path}: {statType} 중복 ID {id.Value}");
                        continue;
                    }

                    float legacy = source.GetBase(statType);
                    if (!runtime.Register(new GameplayAttributeDefinition(id, legacy), legacy))
                    {
                        failures.Add($"{path}: {statType} 등록 실패");
                        continue;
                    }
                    float shadow = runtime.GetCurrent(id);
                    if (Math.Abs(legacy - shadow) > 0.0001f)
                        failures.Add($"{path}: {statType} Legacy {legacy} / Shadow {shadow}");
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [Test]
        public void 모든_GameplayAbility는_실행가능한_TaskGraph를_가진다()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:GameplayAbilitySO", new[] { "Assets/10.Datas/Ability" });
            var failures = new List<string>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameplayAbilitySO ability = AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(path);
                if (ability == null)
                    failures.Add($"{path}: Ability 로드 실패");
                else if (ability.taskGraph == null)
                    failures.Add($"{path}: TaskGraph 누락");
                else if (ability.taskGraph.Root == null)
                    failures.Add($"{path}: TaskGraph Root 누락");
                    else if (ability.taskGraph.Root is not LegacyMotionPayloadTask)
                    failures.Add($"{path}: 미지원 Root {ability.taskGraph.Root.GetType().Name}");
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }
    }
}
#endif
