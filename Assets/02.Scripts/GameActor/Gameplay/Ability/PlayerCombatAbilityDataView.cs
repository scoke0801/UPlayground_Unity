using System.Collections.Generic;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Actor.Animation;

namespace UPlayGround.Gameplay.Ability
{
    /// <summary>
    /// AbilitySet의 전투 Ability/Payload를 기존 PlayerCombat 실행기가 소비할 읽기 전용 형태로 해석한다.
    /// 직렬화 데이터의 진실 소스는 AbilitySetSO이며 별도 플레이어 공격 SO를 보관하지 않는다.
    /// </summary>
    public sealed class PlayerCombatAbilityDataView
    {
        public readonly List<AbilityAttackInfo> liteComboAttackList = new();
        public readonly List<AbilityAttackInfo> heavyComboAttackList = new();
        public readonly List<AbilityAttackInfo> jumpAttackList = new();
        public readonly List<AbilityAttackInfo> dashAttackList = new();
        public readonly List<AbilityAttackInfo> skillAttackList = new();
        public readonly List<ChargeStageData> chargeStages = new();
        public readonly List<float> chargeStageThresholds = new();
        public readonly List<ComboRouteEntry> comboRoutes = new();

        public AbilityAttackInfo counterAttack;
        public AbilityAttackInfo parryCounterAttack;
        public AbilityAttackInfo entryAttack;
        public bool useEntryAttackVsGroggy;
        public AbilityAttackInfo entryAttackVsGroggy;
        public bool useEntryAttackVsAirborne;
        public AbilityAttackInfo entryAttackVsAirborne;
        public AbilityAttackInfo swapEvadeCounterAttack;
        public AbilityAttackInfo swapSpecialAttack;
        public AbilityMotionKey chargeMotionKey;
        public PlayerInterruptAction chargeInterruptActions;
        public string fullChargeVfxKey;
        public ActorSocketType fullChargeVfxSocket;
        public UnityEngine.Vector3 fullChargeVfxOffset;
        public float comboLinkWindow = 1f;

        public static PlayerCombatAbilityDataView Build(AbilitySetSO set)
        {
            if (set == null) return null;
            var view = new PlayerCombatAbilityDataView
            {
                comboLinkWindow = set.GetEffectiveComboLinkWindow(),
            };
            AddSequence(set, PlayerCombatAbilitySlot.LightCombo, view.liteComboAttackList);
            AddSequence(set, PlayerCombatAbilitySlot.HeavyCombo, view.heavyComboAttackList);
            AddSequence(set, PlayerCombatAbilitySlot.JumpCombo, view.jumpAttackList);
            AddSequence(set, PlayerCombatAbilitySlot.DashAttack, view.dashAttackList);
            AddPlayerSlot(set, PlayerSkillSlot.Ability, view.skillAttackList);
            AddPlayerSlot(set, PlayerSkillSlot.Ultimate, view.skillAttackList);

            view.counterAttack = Resolve(
                set.GetCombatAbility(PlayerCombatAbilitySlot.CounterAttack));
            view.parryCounterAttack = Resolve(
                set.GetCombatAbility(PlayerCombatAbilitySlot.ParryCounterAttack));
            view.entryAttack = Resolve(
                set.GetCombatAbility(PlayerCombatAbilitySlot.EntryAttack));
            view.entryAttackVsGroggy = Resolve(
                set.GetCombatAbility(PlayerCombatAbilitySlot.EntryAttackVsGroggy));
            view.useEntryAttackVsGroggy = view.entryAttackVsGroggy?.baseInfo != null;
            view.entryAttackVsAirborne = Resolve(
                set.GetCombatAbility(PlayerCombatAbilitySlot.EntryAttackVsAirborne));
            view.useEntryAttackVsAirborne = view.entryAttackVsAirborne?.baseInfo != null;
            view.swapEvadeCounterAttack = Resolve(
                set.GetCombatAbility(PlayerCombatAbilitySlot.SwapEvadeCounterAttack));
            view.swapSpecialAttack = Resolve(
                set.GetCombatAbility(PlayerCombatAbilitySlot.SwapSpecialAttack));

            PlayerChargeAbilitySettings charge = set.GetEffectiveCharge();
            if (charge != null)
            {
                view.chargeStageThresholds.AddRange(charge.stageThresholds);
                view.chargeInterruptActions = charge.interruptActions;
                view.fullChargeVfxKey = charge.fullChargeVfxKey;
                view.fullChargeVfxSocket = charge.fullChargeVfxSocket;
                view.fullChargeVfxOffset = charge.fullChargeVfxOffset;
                for (int i = 0; i < charge.stages.Count; i++)
                {
                    AbilityAttackInfo attack = Resolve(
                        set.ResolveEffectiveChargeAbility(charge.stages[i]));
                    if (attack?.baseInfo == null) continue;
                    if (!view.chargeMotionKey.IsValid)
                        view.chargeMotionKey = attack.baseInfo.motionKey;
                    view.chargeStages.Add(new ChargeStageData
                    {
                        hitPhases = attack.baseInfo.hitPhases,
                        interruptActions = attack.interruptActions,
                    });
                }
            }

            IReadOnlyList<AbilityComboRouteDefinition> routes =
                set.GetEffectiveComboRoutes();
            for (int i = 0; i < routes.Count; i++)
            {
                AbilityComboRouteDefinition source = routes[i];
                if (source == null) continue;
                view.comboRoutes.Add(new ComboRouteEntry
                {
                    routeName = source.routeId,
                    displayName = source.displayName,
                    inputPattern = source.inputPattern,
                    matchMode = source.matchMode,
                    requiredTagIds = source.requiredTagIds,
                    blockedTagIds = source.blockedTagIds,
                    groundCondition = source.groundCondition,
                    skillGaugeIndex = source.skillGaugeIndex,
                    attackInfo = Resolve(
                        set.ResolveEffectiveComboRouteAbility(source.ability)),
                    priority = source.priority,
                    perfectWindow = source.perfectWindow,
                    enhancedAttackInfo = Resolve(
                        set.ResolveEffectiveComboRouteAbility(
                            source.enhancedAbility)),
                    enhancedDamageMultiplier = source.enhancedDamageMultiplier,
                    enhancedPoiseMultiplier = source.enhancedPoiseMultiplier,
                    enhancedGrantTagId = source.enhancedGrantTagId,
                });
            }
            return view;
        }

        private static void AddSequence(
            AbilitySetSO set,
            PlayerCombatAbilitySlot slot,
            List<AbilityAttackInfo> destination)
        {
            IReadOnlyList<GameplayAbilitySO> abilities = set.GetCombatSequence(slot);
            for (int i = 0; i < abilities.Count; i++)
            {
                AbilityAttackInfo attack = Resolve(abilities[i]);
                if (attack?.baseInfo != null) destination.Add(attack);
            }
        }

        private static void AddPlayerSlot(
            AbilitySetSO set,
            PlayerSkillSlot slot,
            List<AbilityAttackInfo> destination)
        {
            AbilityAttackInfo attack = Resolve(set.GetPlayerAbility(slot));
            if (attack?.baseInfo != null) destination.Add(attack);
        }

        private static AbilityAttackInfo Resolve(GameplayAbilitySO ability)
        {
            if (ability?.variants == null) return null;
            AbilityVariantDefinition best = null;
            for (int i = 0; i < ability.variants.Count; i++)
            {
                AbilityVariantDefinition candidate = ability.variants[i];
                if (!UPlayGroundAbilityPayloadResolver.IsExecutable(candidate))
                    continue;
                if (best == null || candidate.priority > best.priority)
                    best = candidate;
            }
            return UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(
                best, out AbilityAttackInfo attack)
                ? attack
                : null;
        }
    }
}
