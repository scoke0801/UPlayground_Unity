using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Components;

namespace UPlayGround.Ability.Tests
{
    public sealed class CombatElementTests
    {
        [TestCase(CombatElement.Water, CombatElement.Fire)]
        [TestCase(CombatElement.Fire, CombatElement.Nature)]
        [TestCase(CombatElement.Nature, CombatElement.Water)]
        [TestCase(CombatElement.Light, CombatElement.Dark)]
        [TestCase(CombatElement.Dark, CombatElement.Light)]
        public void 유리한_속성은_추가_피해_배율을_반환한다(
            CombatElement attack,
            CombatElement defense)
        {
            Assert.That(
                CombatElementRules.ResolveDamageMultiplier(
                    attack, defense, 1.25f),
                Is.EqualTo(1.25f));
        }

        [TestCase(CombatElement.None, CombatElement.Fire)]
        [TestCase(CombatElement.Fire, CombatElement.Water)]
        [TestCase(CombatElement.Fire, CombatElement.Fire)]
        [TestCase(CombatElement.Light, CombatElement.Light)]
        public void 무속성이나_비상성은_추가_피해가_없다(
            CombatElement attack,
            CombatElement defense)
        {
            Assert.That(
                CombatElementRules.ResolveDamageMultiplier(
                    attack, defense, 1.25f),
                Is.EqualTo(1f));
        }

        [Test]
        public void Humanoid_랜덤속성은_같은_새게임_시드에서_결정적으로_유지된다()
        {
            CombatElement first =
                CombatElementRules.ResolveRandomElement(12345, "MonsterBokusei");
            CombatElement restored =
                CombatElementRules.ResolveRandomElement(12345, "MonsterBokusei");

            Assert.That(restored, Is.EqualTo(first));
            Assert.That(first, Is.Not.EqualTo(CombatElement.None));
        }

        [Test]
        public void 속성_Effect는_우선순위와_제거에_따라_현재_속성을_복원한다()
        {
            var go = new GameObject("ElementTestActor");
            var definition = ScriptableObject.CreateInstance<ActorDefinitionSO>();
            var fire = ScriptableObject.CreateInstance<GameplayEffectSO>();
            var light = ScriptableObject.CreateInstance<GameplayEffectSO>();
            try
            {
                definition.combatElement = CombatElement.Nature;
                TestGameActor actor = go.AddComponent<TestGameActor>();
                actor.InitializeForEditMode();
                actor.SetDefinition(definition);
                Assert.That(actor.HasElementOverride, Is.False);

                fire.effectId = "Effect.Test.Element.Fire";
                fire.durationType = GameplayEffectDurationType.Duration;
                fire.durationSeconds = 10f;
                fire.grantedElement = CombatElement.Fire;
                fire.elementPriority = 10;

                light.effectId = "Effect.Test.Element.Light";
                light.durationType = GameplayEffectDurationType.Duration;
                light.durationSeconds = 10f;
                light.grantedElement = CombatElement.Light;
                light.elementPriority = 20;

                var fireHandle = actor.Effects.ApplyEffect(fire, actor);
                Assert.That(actor.HasElementOverride, Is.True);
                var lightHandle = actor.Effects.ApplyEffect(light, actor);
                Assert.That(actor.CurrentElement, Is.EqualTo(CombatElement.Light));

                actor.Effects.RemoveEffect(lightHandle);
                Assert.That(actor.CurrentElement, Is.EqualTo(CombatElement.Fire));

                actor.Effects.RemoveEffect(fireHandle);
                Assert.That(actor.CurrentElement, Is.EqualTo(CombatElement.Nature));
                Assert.That(actor.HasElementOverride, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(fire);
                Object.DestroyImmediate(light);
            }
        }

        [Test]
        public void WeaponTrailController는_플레이어_모델_부모의_액터를_소유자로_연결한다()
        {
            var actorObject = new GameObject("PlayerActorRoot");
            var modelObject = new GameObject("ActiveCharacterModel");
            try
            {
                TestGameActor actor = actorObject.AddComponent<TestGameActor>();
                actor.InitializeForEditMode();
                modelObject.transform.SetParent(actorObject.transform);
                var controller =
                    modelObject.AddComponent<ActorWeaponTrailController>();

                MethodInfo bindOwner = typeof(ActorWeaponTrailController)
                    .GetMethod(
                        "BindOwner",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo ownerField = typeof(ActorWeaponTrailController)
                    .GetField(
                        "_owner",
                        BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(bindOwner, Is.Not.Null);
                Assert.That(ownerField, Is.Not.Null);
                bindOwner.Invoke(controller, null);
                Assert.That(ownerField.GetValue(controller), Is.SameAs(actor));
            }
            finally
            {
                Object.DestroyImmediate(modelObject);
                Object.DestroyImmediate(actorObject);
            }
        }

        [Test]
        public void 속성부여_Ability와_WeaponTrail_매핑_에셋은_모두_유효하다()
        {
            var library = Resources.Load<ElementalWeaponTrailLibrarySO>(
                ElementalWeaponTrailLibrarySO.ResourcesPath);
            Assert.That(library, Is.Not.Null);

            Assert.That(library.GetPrefab(CombatElement.None), Is.Null);

            CombatElement[] elements =
            {
                CombatElement.Fire,
                CombatElement.Water,
                CombatElement.Nature,
                CombatElement.Light,
                CombatElement.Dark,
            };
            for (int i = 0; i < elements.Length; i++)
                Assert.That(library.GetPrefab(elements[i]), Is.Not.Null, elements[i].ToString());

            string[] names = { "Fire", "Water", "Nature", "Light", "Dark" };
            var config = AssetDatabase.LoadAssetAtPath<PartyConfigSO>(
                "Assets/10.Datas/Party/PartyConfig.asset");
            Assert.That(config, Is.Not.Null);
            for (int i = 0; i < names.Length; i++)
            {
                string path =
                    $"Assets/10.Datas/Ability/ElementalImbue/GA_ElementalImbue_{names[i]}.asset";
                GameplayAbilitySO ability =
                    AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(path);
                Assert.That(ability, Is.Not.Null, path);
                Assert.That(
                    ability.activation.groundCondition,
                    Is.EqualTo(AbilityGroundCondition.Grounded));
                Assert.That(ability.cooldown.durationSeconds, Is.EqualTo(25f));
                Assert.That(
                    ability.cooldown.cooldownGroupId,
                    Is.EqualTo("Ability.ElementalImbue"));
                Assert.That(ability.presentation.icon, Is.Not.Null);
                Assert.That(
                    config.GetElementalImbueAbility(elements[i]),
                    Is.SameAs(ability));
                Assert.That(ability.variants, Has.Count.EqualTo(1));
                Assert.That(
                    UPlayGroundAbilityPayloadResolver.TryResolve(
                        ability.variants[0],
                        out MotionKey motionKey,
                        out _),
                    Is.True);
                Assert.That(
                    AssetDatabase
                        .FindAssets($"t:{nameof(ActorAnimationMotionSet)}")
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<
                            ActorAnimationMotionSet>)
                        .Any(x =>
                            x != null
                            && x.GetAbilityMotionAsset(motionKey) != null),
                    Is.True,
                    $"Motion Key '{motionKey}' 매핑 누락");
                Assert.That(ability.variants[0].ownerEffects, Has.Count.EqualTo(1));
                Assert.That(
                    ability.variants[0].ownerEffects[0].grantedElement,
                    Is.Not.EqualTo(CombatElement.None));
            }
        }

        [Test]
        public void 몬스터와_플레이어_속성_데이터가_요구사항대로_배정되어_있다()
        {
            AssertDefinitions(
                CombatElement.Dark,
                "Skeleton_Bow", "Skeleton_Common", "Skeleton_Sword",
                "Lich_Elite", "Lich_Normal",
                "Griffin_Elite",
                "SpiderQueen_1", "SpiderQueen_2", "SpiderQueen_3");
            AssertDefinitions(
                CombatElement.Nature,
                "ChildPlant", "ChildPlant_2", "ChildPlant_3", "Dryad",
                "Ent_Elite", "Ent_Normal",
                "Plant_1", "Plant_2", "Plant_3",
                "RootPlant_1", "RootPlant_2", "RootPlant_3");
            AssertDefinitions(CombatElement.Light, "Griffin_Normal");
            AssertDefinitions(
                CombatElement.None,
                "Golem_Black", "Golem_Inferno", "Golem_Normal",
                "SpiderMinion_1", "SpiderMinion_2", "SpiderMinion_3");

            string[] humanoidGuids =
                AssetDatabase.FindAssets(
                    "t:ActorDefinitionSO",
                    new[]
                    {
                        "Assets/10.Datas/Actor/DataBase/Humanoid",
                    });
            Assert.That(humanoidGuids, Has.Length.EqualTo(11));
            for (int i = 0; i < humanoidGuids.Length; i++)
            {
                var definition = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(
                    AssetDatabase.GUIDToAssetPath(humanoidGuids[i]));
                Assert.That(
                    definition.elementAssignmentMode,
                    Is.EqualTo(CombatElementAssignmentMode.RandomPerNewGame),
                    definition.name);
            }

            string[] allDefinitionGuids = AssetDatabase.FindAssets(
                "t:ActorDefinitionSO",
                new[] { "Assets/10.Datas/Actor/DataBase" });
            int randomEnemyCount = 0;
            for (int i = 0; i < allDefinitionGuids.Length; i++)
            {
                var definition = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(
                    AssetDatabase.GUIDToAssetPath(allDefinitionGuids[i]));
                if (definition == null
                    || !definition.actorId.StartsWith("Enemy_Random_"))
                {
                    continue;
                }
                randomEnemyCount++;
                Assert.That(
                    definition.elementAssignmentMode,
                    Is.EqualTo(CombatElementAssignmentMode.RandomPerNewGame),
                    definition.name);
            }
            Assert.That(randomEnemyCount, Is.EqualTo(10));

            var memberData = AssetDatabase.LoadAssetAtPath<PartyMemberDataSO>(
                "Assets/10.Datas/Party/PartyMemberData.asset");
            Assert.That(memberData, Is.Not.Null);
            Assert.That(memberData.sprites, Has.Count.EqualTo(12));
            for (int i = 0; i < memberData.sprites.Count; i++)
            {
                var entry = memberData.sprites[i];
                Assert.That(
                    entry.combatElement,
                    Is.Not.EqualTo(CombatElement.None),
                    entry.type.ToString());
                Assert.That(entry.weaponName, Is.Not.Empty, entry.type.ToString());
            }
        }

        private static void AssertDefinitions(
            CombatElement expected,
            params string[] assetNames)
        {
            for (int i = 0; i < assetNames.Length; i++)
            {
                string path =
                    $"Assets/10.Datas/Actor/DataBase/{assetNames[i]}.asset";
                ActorDefinitionSO definition =
                    AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(path);
                Assert.That(definition, Is.Not.Null, path);
                Assert.That(
                    definition.combatElement,
                    Is.EqualTo(expected),
                    assetNames[i]);
                Assert.That(
                    definition.elementAssignmentMode,
                    Is.EqualTo(CombatElementAssignmentMode.Fixed),
                    assetNames[i]);
            }
        }
    }
}
