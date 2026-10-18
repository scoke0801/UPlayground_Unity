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

        [TestCase("valid", true)]
        [TestCase("empty", false)]
        [TestCase("duplicate", false)]
        [TestCase("unknown", false)]
        [TestCase("overflow", false)]
        public void 시작_파티는_빈_슬롯을_제외하고_중복과_미등록과_상한을_검사한다(string scenario, bool expected)
        {
            var window = ScriptableObject.CreateInstance(WindowType);
            var catalog = ScriptableObject.CreateInstance<UPlayGround.Data.Party.PlayerCharacterCatalogSO>();
            var config = ScriptableObject.CreateInstance<UPlayGround.Data.Party.PartyConfigSO>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                catalog.entries.Add(new() { characterType = CharacterActorType.Raon, definitionAddress = "Bokusei" });
                catalog.entries.Add(new() { characterType = CharacterActorType.Hwarin, definitionAddress = "Honoka" });
                config.maxBattleSize = scenario == "overflow" ? 1 : 4;
                var members = scenario switch
                {
                    "empty" => new[] { CharacterActorType.None },
                    "duplicate" => new[] { CharacterActorType.Raon, CharacterActorType.Raon },
                    "unknown" => new[] { (CharacterActorType)int.MaxValue },
                    _ => new[] { CharacterActorType.Raon, CharacterActorType.None, CharacterActorType.Hwarin }
                };
                WindowType.GetField("_partyCatalog", flags).SetValue(window, catalog);
                WindowType.GetField("_partyConfig", flags).SetValue(window, config);
                WindowType.GetField("_startingParty", flags).SetValue(window, members);
                object[] arguments = { null, null };
                bool actual = (bool)WindowType.GetMethod("TryGetStartingParty", flags).Invoke(window, arguments);
                Assert.That(actual, Is.EqualTo(expected));
                if (expected)
                {
                    Assert.That(arguments[0], Is.EqualTo(new[] { CharacterActorType.Raon, CharacterActorType.Hwarin }));
                    Assert.That(arguments[1], Is.Null);
                }
                else Assert.That(arguments[1], Is.Not.Null.And.Not.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

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

        [TestCase(0, 10)]
        [TestCase(1, 15)]
        [TestCase(2, 20)]
        [TestCase(3, 25)]
        [TestCase(4, 30)]
        [TestCase(5, 40)]
        [TestCase(6, 50)]
        public void 레벨_교체는_등록된_동일_레벨_프리팹_6종을_선택한다(int tier, int level)
        {
            var window = ScriptableObject.CreateInstance(WindowType);
            try
            {
                var apply = WindowType.GetMethod("ApplyProgressionTier", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That((bool)apply.Invoke(window, new object[] { tier }), Is.True);
                var members = (ActorDefinitionSO[])WindowType.GetField("_members",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
                var database = AssetDatabase.LoadAssetAtPath<ActorDatabase>(
                    "Assets/10.Datas/Actor/DataBase/ActorDatabase.asset");
                Assert.That(members, Has.Length.EqualTo(6));
                Assert.That(members, Is.Unique);
                foreach (var member in members)
                {
                    Assert.That(member.EffectiveLevel, Is.EqualTo(level));
                    Assert.That(member.prefab, Is.Not.Null);
                    Assert.That(member.EffectiveAbilitySet, Is.Not.Null);
                    Assert.That(database.All, Does.Contain(member));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(window); }
        }

        [Test]
        public void 레벨_교체_실패는_기존_그룹을_보존한다()
        {
            var window = ScriptableObject.CreateInstance(WindowType);
            try
            {
                var membersField = WindowType.GetField("_members", BindingFlags.Instance | BindingFlags.NonPublic);
                var previous = membersField.GetValue(window);
                var tiers = (Array)WindowType.GetField("_tiers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
                var tier = tiers.GetValue(0);
                tier.GetType().GetField("armor").SetValue(tier, -1);
                var apply = WindowType.GetMethod("ApplyProgressionTier", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That((bool)apply.Invoke(window, new object[] { 0 }), Is.False);
                Assert.That(membersField.GetValue(window), Is.SameAs(previous));
                Assert.That((bool)apply.Invoke(window, new object[] { -1 }), Is.False);
                Assert.That(membersField.GetValue(window), Is.SameAs(previous));
            }
            finally { UnityEngine.Object.DestroyImmediate(window); }
        }

        [Test]
        public void AI_시험_정지는_수동_틱에서도_트리를_시작하지_않는다()
        {
            var root = new GameObject("CombatTestPausedRunner");
            root.SetActive(false);
            try
            {
                var runnerType = Type.GetType("UPlayGround.AI.BehaviorTree.BehaviorTreeRunner, UPlayGround.Actor", true);
                var runner = root.AddComponent(runnerType);
                runnerType.GetProperty("IsCombatTestPaused").SetValue(runner, true);
                runnerType.GetMethod("TickOnce", Type.EmptyTypes).Invoke(runner, null);
                Assert.That((bool)runnerType.GetProperty("IsRunning").GetValue(runner), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void 공격_관찰을_종료하면_이전_제한을_복원하며_반복_해제도_안전하다()
        {
            var window = ScriptableObject.CreateInstance(WindowType);
            var root = new GameObject("CombatTestRestriction");
            root.SetActive(false);
            var original = ScriptableObject.CreateInstance<UPlayGround.Data.Ability.GameplayAbilitySO>();
            var selected = ScriptableObject.CreateInstance<UPlayGround.Data.Ability.GameplayAbilitySO>();
            try
            {
                var combatType = Type.GetType("UPlayGround.Components.EnemyCombat, UPlayGround.Actor", true);
                var combat = root.AddComponent(combatType);
                var restriction = combatType.GetProperty("CombatTestAbility");
                restriction.SetValue(combat, selected);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                WindowType.GetField("_restrictedCombat", flags).SetValue(window, combat);
                WindowType.GetField("_previousRestriction", flags).SetValue(window, original);
                WindowType.GetField("_practiceIndex", flags).SetValue(window, 1);
                var release = WindowType.GetMethod("ReleasePracticeRestriction", flags);
                release.Invoke(window, null);
                release.Invoke(window, null);
                Assert.That(restriction.GetValue(combat), Is.SameAs(original));
                Assert.That(WindowType.GetField("_practiceIndex", flags).GetValue(window), Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(original);
                UnityEngine.Object.DestroyImmediate(selected);
            }
        }

        [Test]
        public void 공격_관찰중에만_동일_기술_반복_억제를_우회한다()
        {
            var root = new GameObject("CombatTestRepetition");
            root.SetActive(false);
            var ability = ScriptableObject.CreateInstance<UPlayGround.Data.Ability.GameplayAbilitySO>();
            var strategy = ScriptableObject.CreateInstance<UPlayGround.Data.Enemy.EnemyCombatStrategySO>();
            try
            {
                var combatType = Type.GetType("UPlayGround.Components.EnemyCombat, UPlayGround.Actor", true);
                var combat = root.AddComponent(combatType);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                combatType.GetField("_lastSelectedAbility", flags).SetValue(combat, ability);
                combatType.GetField("_consecutiveAbilitySelections", flags).SetValue(combat, 2);
                strategy.maxConsecutiveSameAbility = 2;
                var evaluate = combatType.GetMethod("EvaluateStrategy", flags);
                var arguments = new object[] { ability, strategy, 1f };
                Assert.That(evaluate.Invoke(combat, arguments).ToString(), Is.EqualTo("RepetitionLimit"));
                combatType.GetProperty("CombatTestAbility").SetValue(combat, ability);
                Assert.That(evaluate.Invoke(combat, arguments).ToString(), Is.EqualTo("None"));
                combatType.GetProperty("CombatTestAbility").SetValue(combat, null);
                Assert.That(evaluate.Invoke(combat, arguments).ToString(), Is.EqualTo("RepetitionLimit"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(ability);
                UnityEngine.Object.DestroyImmediate(strategy);
            }
        }

        [Test]
        public void 경고_링은_풀_재사용시_방어_유형의_이미지를_다시_선택한다()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03.Prefabs/UI/WorldSpace/DangerRing/UIDangerRing.prefab");
            Assert.That(prefab, Is.Not.Null);
            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var ringType = Type.GetType("UPlayGround.UI.UIDangerRing, UPlayGround.UI", true);
                var ring = instance.GetComponent(ringType);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                ringType.GetMethod("Awake", flags).Invoke(ring, null);
                var image = ringType.GetField("_fillImage", flags).GetValue(ring);
                var spriteProperty = image.GetType().GetProperty("sprite");
                var init = ringType.GetMethod("Init");
                Sprite previous = null;
                foreach (var defense in new[] { AttackDefenseType.Unblockable, AttackDefenseType.Parryable, AttackDefenseType.GuardableOnly })
                {
                    init.Invoke(ring, new object[] { null, null, null, 1f, defense, null, null });
                    var sprite = (Sprite)spriteProperty.GetValue(image);
                    Assert.That(sprite, Is.Not.Null);
                    Assert.That(sprite, Is.Not.SameAs(previous));
                    previous = sprite;
                    ringType.GetMethod("Release").Invoke(ring, null);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
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
