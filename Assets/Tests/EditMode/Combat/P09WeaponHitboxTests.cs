using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Combat;

namespace UPlayGround.Combat.Tests
{
    /// <summary>P09 장비 교체 후 활성 무기의 판정이 이전 비활성 메시 쪽에만 남는 회귀를 검사한다.</summary>
    public sealed class P09WeaponHitboxTests
    {
        private static readonly Dictionary<string, string> WeaponGroups = new()
        {
            { "Sword", "MainWeapon" }, { "SubSword", "SubWeapon" },
            { "GreatSword", "MainWeapon" }, { "DualAxe", "MainWeapon" },
            { "DoubleAxe", "MainWeapon" }, { "Shield", "SubWeapon" },
            { "Staff", "MainWeapon" }
        };

        private static IEnumerable<string> PrefabPaths => AssetDatabase
            .FindAssets("t:Prefab", new[] { "Assets/03.Prefabs/Characters/Enemy" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => System.IO.Path.GetFileName(path).StartsWith("Enemy_P09_")
                           || System.IO.Path.GetFileName(path).StartsWith("Enemy_Random_"));

        [TestCaseSource(nameof(PrefabPaths))]
        public void 활성_근접_무기마다_올바른_그룹의_판정이_있다(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var weapon = prefab.transform.Find("Weapon");
            Assert.That(weapon, Is.Not.Null, path);
            foreach (Transform slot in weapon)
            {
                if (!WeaponGroups.TryGetValue(slot.name, out string group)) continue;
                foreach (var renderer in slot.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!renderer.enabled || !IsActiveBelowRoot(renderer.transform, prefab.transform)) continue;
                    var hitboxes = renderer.GetComponentsInChildren<CombatHitbox>(true);
                    Assert.That(hitboxes.Any(hitbox => hitbox.IsSupported
                        && hitbox.GroupId == group
                        && IsActiveBelowRoot(hitbox.transform, prefab.transform)), Is.True,
                        $"{path}: {slot.name}/{renderer.name}의 활성 {group} 판정 누락");
                }
            }
        }

        [TestCaseSource(nameof(PrefabPaths))]
        public void 활을_장착한_적은_주무기_판정을_활성화할_수_있다(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var bow = prefab.transform.Find("Weapon/Bow");
            if (bow == null || !IsActiveBelowRoot(bow, prefab.transform)) return;
            if (!bow.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(renderer =>
                    renderer.enabled && renderer.name.StartsWith("Bow_")
                    && IsActiveBelowRoot(renderer.transform, prefab.transform))) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                var set = instance.GetComponent<CombatHitboxSet>() ?? instance.AddComponent<CombatHitboxSet>();
                set.Refresh();
                Assert.That(set.BeginGroup("MainWeapon"), Is.True, path);
                Assert.That(set.ActiveHitboxes.Any(hitbox => hitbox.transform.IsChildOf(instance.transform.Find("Weapon/Bow"))),
                    Is.True, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static bool IsActiveBelowRoot(Transform current, Transform root)
        {
            for (; current != root; current = current.parent)
                if (!current.gameObject.activeSelf) return false;
            return true;
        }
    }
}
