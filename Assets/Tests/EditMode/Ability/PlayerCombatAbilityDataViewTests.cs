using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Ability;
using UPlayGround.Animation;
using UPlayGround.Data.Actor.Animation;

namespace UPlayGround.Ability.Tests
{
    public sealed class PlayerCombatAbilityDataViewTests
    {
        private readonly List<Object> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
                Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }

        [Test]
        public void AbilitySet의_일반공격_차지_연계라우트를_전투뷰로_해석한다()
        {
            AbilitySetSO set = Create<AbilitySetSO>();
            GameplayAbilitySO light0 = CreateAttack("Light0", 10f);
            GameplayAbilitySO light1 = CreateAttack("Light1", 20f);
            GameplayAbilitySO counter = CreateAttack("Counter", 30f);
            GameplayAbilitySO charge = CreateAttack("Charge", 40f);
            GameplayAbilitySO route = CreateAttack("Route", 50f);

            set.combatBindings.Add(new PlayerCombatAbilityBinding
            {
                slot = PlayerCombatAbilitySlot.LightCombo,
                abilities = new List<GameplayAbilitySO> { light0, light1 },
            });
            set.combatBindings.Add(new PlayerCombatAbilityBinding
            {
                slot = PlayerCombatAbilitySlot.CounterAttack,
                abilities = new List<GameplayAbilitySO> { counter },
            });
            set.charge.stages.Add(charge);
            set.charge.stageThresholds.Clear();
            set.comboRoutes.Add(new AbilityComboRouteDefinition
            {
                routeId = "LightRoute",
                ability = route,
            });

            PlayerCombatAbilityDataView view =
                PlayerCombatAbilityDataView.Build(set);

            Assert.That(view.liteComboAttackList, Has.Count.EqualTo(2));
            Assert.That(
                view.liteComboAttackList[1].baseInfo.hitPhases[0].damage,
                Is.EqualTo(20f));
            Assert.That(view.counterAttack.baseInfo.motionRef, Is.Not.Null);
            Assert.That(view.chargeStages, Has.Count.EqualTo(1));
            Assert.That(view.chargeMotionRef, Is.SameAs(charge.variants[0]
                    .executionPayload is UPlayGroundMotionAbilityPayloadSO payload
                        ? payload.attackInfo?.baseInfo?.motionRef
                        : null));
            Assert.That(view.comboRoutes, Has.Count.EqualTo(1));
            Assert.That(
                view.comboRoutes[0].attackInfo.baseInfo.hitPhases[0].damage,
                Is.EqualTo(50f));
        }

        [Test]
        public void 파생AbilitySet의_교체Ability를_전투뷰전체에적용한다()
        {
            AbilitySetSO common = Create<AbilitySetSO>();
            GameplayAbilitySO commonAttack = CreateAttack("Common", 10f);
            GameplayAbilitySO eliteAttack = CreateAttack("Elite", 99f);
            common.combatBindings.Add(new PlayerCombatAbilityBinding
            {
                slot = PlayerCombatAbilitySlot.LightCombo,
                abilities = new List<GameplayAbilitySO> { commonAttack },
            });
            common.charge.stages.Add(commonAttack);
            common.comboRoutes.Add(new AbilityComboRouteDefinition
            {
                routeId = "Route",
                ability = commonAttack,
            });
            AbilitySetSO derived = Create<AbilitySetSO>();
            derived.baseSet = common;
            derived.abilityOverrides.Add(
                new AbilitySetSO.AbilityOverrideEntry
                {
                    sourceAbility = commonAttack,
                    operation = AbilitySetOverrideOperation.Replace,
                    replacementAbility = eliteAttack,
                });

            PlayerCombatAbilityDataView view =
                PlayerCombatAbilityDataView.Build(derived);

            Assert.That(
                view.liteComboAttackList[0].baseInfo.hitPhases[0].damage,
                Is.EqualTo(99f));
            Assert.That(
                view.chargeStages[0].hitPhases[0].damage,
                Is.EqualTo(99f));
            Assert.That(
                view.comboRoutes[0].attackInfo.baseInfo.hitPhases[0].damage,
                Is.EqualTo(99f));
        }

        private GameplayAbilitySO CreateAttack(
            string id,
            float damage)
        {
            GameplayAbilitySO ability = Create<GameplayAbilitySO>();
            ability.abilityId = id;
            var attack = new AbilityAttackInfo
            {
                baseInfo = new AttackInfoBase(),
            };
            attack.baseInfo.hitPhases[0].damage = damage;
            MotionSetAsset motion = Create<MotionSetAsset>();
            motion.name = id + "Motion";
            MotionReferenceSO motionRef = Create<MotionReferenceSO>();
            motionRef.defaultMotion = motion;
            attack.baseInfo.motionRef = motionRef;
            UPlayGroundMotionAbilityPayloadSO payload =
                Create<UPlayGroundMotionAbilityPayloadSO>();
            payload.attackInfo = attack;
            ability.variants.Add(new AbilityVariantDefinition
            {
                variantId = "Default",
                executionPayload = payload,
            });
            return ability;
        }

        private T Create<T>() where T : ScriptableObject
        {
            T value = ScriptableObject.CreateInstance<T>();
            _objects.Add(value);
            return value;
        }
    }
}
