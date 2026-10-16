using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Content.Tests
{
    /// <summary>시험장 기본 적의 스폰 연결과 재도전 세션 초기화를 검사한다.</summary>
    public sealed class CombatTestSessionTests
    {
        private static Type WindowType => Type.GetType(
            "UPlayGround.Tool.Editor.CombatTestSessionWindow, Assembly-CSharp-Editor", true);

        [TestCase("SwordShield")]
        [TestCase("GreatSword")]
        [TestCase("DualSword")]
        [TestCase("DualAxe")]
        [TestCase("Bow")]
        [TestCase("Staff_M")]
        public void 무기_판정_복구는_프리팹을_저장하지_않고_반복_가능하다(string weapon)
        {
            string id = "Enemy_P09_Armor06_" + weapon;
            string path = $"Assets/03.Prefabs/Characters/Enemy/Armor_06/{id}/{id}.prefab";
            var original = System.IO.File.ReadAllBytes(path);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var repair = WindowType.GetMethod("RepairInactiveWeaponHitboxes");
                Assert.That(repair, Is.Not.Null);
                Assert.That((int)repair.Invoke(null, new object[] { root }), Is.GreaterThanOrEqualTo(0));
                Assert.That((int)repair.Invoke(null, new object[] { root }), Is.Zero);
                if (weapon != "Bow")
                {
                    var setType = Type.GetType("UPlayGround.Combat.CombatHitboxSet, UPlayGround.Actor", true);
                    var set = root.GetComponent(setType) ?? root.AddComponent(setType);
                    setType.GetMethod("Refresh").Invoke(set, null);
                    Assert.That((bool)setType.GetMethod("BeginGroup").Invoke(set, new object[] { "MainWeapon" }), Is.True);
                    if (weapon == "SwordShield" || weapon == "DualSword")
                        Assert.That((bool)setType.GetMethod("BeginGroup").Invoke(set, new object[] { "SubWeapon" }), Is.True);
                    setType.GetMethod("EndGroup").Invoke(set, null);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Assert.That(System.IO.File.ReadAllBytes(path), Is.EqualTo(original));
        }

        [Test]
        public void 대검_시험_몬스터는_정의와_프로필과_프리팹에_대검_스킬을_쓴다()
        {
            var definition = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(
                "Assets/10.Datas/Actor/P09Progression/Generated/Enemy_P09_Armor06_GreatSword/Enemy_P09_Armor06_GreatSword_ActorDef.asset");
            var expected = AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Ability.AbilitySetSO>(
                "Assets/10.Datas/Ability/Actor/Humanoid_GreatSwordAttackData/AbilitySet_Humanoid_GreatSwordAttackData.asset");
            Assert.That(expected, Is.Not.Null);
            Assert.That(definition.abilitySet, Is.SameAs(expected));
            Assert.That(definition.monsterProfile.abilitySet, Is.SameAs(expected));
            Assert.That(definition.EffectiveAbilitySet, Is.SameAs(expected));
            var combatType = Type.GetType("UPlayGround.Components.EnemyCombat, UPlayGround.Actor", true);
            var combat = definition.prefab.GetComponent(combatType);
            Assert.That(combat, Is.Not.Null);
            Assert.That(new SerializedObject(combat).FindProperty("_abilitySet").objectReferenceValue, Is.SameAs(expected));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void 기본_적은_DB에_등록되고_전투_데이터를_가진다(int memberIndex)
        {
            var window = ScriptableObject.CreateInstance(WindowType);
            try
            {
                var members = (ActorDefinitionSO[])WindowType.GetField("_members",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
                Assert.That(members, Has.Length.EqualTo(6));
                Assert.That(members, Is.Unique);
                var definition = memberIndex < 0
                    ? (ActorDefinitionSO)WindowType.GetField("_boss",
                        BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window)
                    : members[memberIndex];
                Assert.That(definition, Is.Not.Null);
                var database = AssetDatabase.LoadAssetAtPath<ActorDatabase>(
                    "Assets/10.Datas/Actor/DataBase/ActorDatabase.asset");
                Assert.That(database, Is.Not.Null);
                Assert.That(database.All, Does.Contain(definition));
                Assert.That(definition.prefab, Is.Not.Null);
                Assert.That(definition.EffectiveAbilitySet, Is.Not.Null);
                Assert.That(definition.actorType.HasFlag(ActorType.Monster), Is.True);
                if (memberIndex < 0)
                {
                    Assert.That(definition.EffectiveGrade, Is.EqualTo(MonsterActorGrade.Boss));
                    Assert.That(definition.actorId, Is.EqualTo("MonsterBokusei"));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [TestCase(PlayModeStateChange.ExitingEditMode, false)]
        [TestCase(PlayModeStateChange.EnteredEditMode, false)]
        [TestCase(PlayModeStateChange.EnteredPlayMode, true)]
        public void 재도전은_세션을_초기화하고_플레이_진입은_소환_기록을_유지한다(
            PlayModeStateChange state, bool expected)
        {
            string key = (string)WindowType.GetField("StartedKey",
                BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
            bool previous = SessionState.GetBool(key, false);
            try
            {
                SessionState.SetBool(key, true);
                WindowType.GetMethod("ResetSession", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { state });
                Assert.That(SessionState.GetBool(key, false), Is.EqualTo(expected));
            }
            finally
            {
                SessionState.SetBool(key, previous);
            }
        }
    }
}
