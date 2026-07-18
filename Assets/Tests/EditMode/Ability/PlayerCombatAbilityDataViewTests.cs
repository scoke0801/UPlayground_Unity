using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Ability;

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
            GameplayAbilitySO light0 = CreateAttack("Light0", AnimKey.Attack_1, 10f);
            GameplayAbilitySO light1 = CreateAttack("Light1", AnimKey.Attack_2, 20f);
            GameplayAbilitySO counter = CreateAttack("Counter", AnimKey.Counter_Attack_1, 30f);
            GameplayAbilitySO charge = CreateAttack("Charge", AnimKey.HeavyAttack_1, 40f);
            GameplayAbilitySO route = CreateAttack("Route", AnimKey.Skill_1, 50f);

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
            Assert.That(view.counterAttack.baseInfo.animKey,
                Is.EqualTo(AnimKey.Counter_Attack_1));
            Assert.That(view.chargeStages, Has.Count.EqualTo(1));
            Assert.That(view.chargeAnimKey, Is.EqualTo(AnimKey.HeavyAttack_1));
            Assert.That(view.comboRoutes, Has.Count.EqualTo(1));
            Assert.That(
                view.comboRoutes[0].attackInfo.baseInfo.hitPhases[0].damage,
                Is.EqualTo(50f));
        }

        private GameplayAbilitySO CreateAttack(
            string id,
            AnimKey animKey,
            float damage)
        {
            GameplayAbilitySO ability = Create<GameplayAbilitySO>();
            ability.abilityId = id;
            var attack = new AbilityAttackInfo
            {
                baseInfo = new AttackInfoBase(),
            };
            attack.baseInfo.animKey = animKey;
            attack.baseInfo.hitPhases[0].damage = damage;
            UPlayGroundMotionAbilityPayloadSO payload =
                Create<UPlayGroundMotionAbilityPayloadSO>();
            payload.executionId = id;
            payload.animKey = animKey;
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
