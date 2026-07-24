#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Stat;
using UPlayGround.Data.Ability;
using UPlayGround.Gameplay.Ability;

namespace UPlayGround.Ability.Tests
{
    public sealed class AbilityDataIntegrityTests
    {
        [Test]
        public void 모든_AttributeProfile은_고유한_AttributeId와_필수값을_가진다()
        {
            string[] guids = AssetDatabase.FindAssets("t:AttributeProfileSO");
            var failures = new List<string>();
            Assert.That(guids, Is.Not.Empty, "Attribute Profile 데이터가 없습니다.");

            for (int assetIndex = 0; assetIndex < guids.Length; assetIndex++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[assetIndex]);
                AttributeProfileSO profile =
                    AssetDatabase.LoadAssetAtPath<AttributeProfileSO>(path);
                if (profile == null)
                {
                    failures.Add($"{path}: 로드 실패");
                    continue;
                }

                var values = new Dictionary<AttributeId, float>();
                if (!profile.TryCopyBaseValues(values, out string error))
                {
                    failures.Add($"{path}: {error}");
                    continue;
                }

                foreach (AttributeId attributeId in
                         UPlayGroundAttributeDefaults.All)
                    if (!values.ContainsKey(attributeId))
                        failures.Add($"{path}: 필수 Attribute 누락 {attributeId}");
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
                else if (ability.taskGraph.Root is not WaitMotionSetEndAbilityTask)
                    failures.Add($"{path}: 미지원 Root {ability.taskGraph.Root.GetType().Name}");
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }
    }
}
#endif
