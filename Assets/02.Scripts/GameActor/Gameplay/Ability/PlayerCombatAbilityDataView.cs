using System.Collections.Generic;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Gameplay.Ability
{
    /// <summary>
    /// AbilitySet의 전투 Ability/Payload를 기존 PlayerCombat 실행기가 소비할 읽기 전용 형태로 해석한다.
    /// 직렬화 데이터의 진실 소스는 AbilitySetSO이며 별도 플레이어 공격 SO를 보관하지 않는다.
    /// </summary>
    public sealed class PlayerCombatAbilityDataView
    {
        public readonly List<PlayerAttackInfo> liteComboAttackList = new();
        public readonly List<PlayerAttackInfo> heavyComboAttackList = new();
        public readonly List<PlayerAttackInfo> jumpAttackList = new();
        public readonly List<PlayerAttackInfo> dashAttackList = new();
        public readonly List<PlayerAttackInfo> skillAttackList = new();
        public readonly List<ChargeStageData> chargeStages = new();
        public readonly List<float> chargeStageThresholds = new();
        public readonly List<ComboRouteEntry> comboRoutes = new();

        public PlayerAttackInfo counterAttack;
        public PlayerAttackInfo parryCounterAttack;
        public PlayerAttackInfo entryAttack;
        public bool useEntryAttackVsGroggy;
        public PlayerAttackInfo entryAttackVsGroggy;
        public bool useEntryAttackVsAirborne;
        public PlayerAttackInfo entryAttackVsAirborne;
        public PlayerAttackInfo swapEvadeCounterAttack;
        public PlayerAttackInfo swapSpecialAttack;
        public AnimKey chargeAnimKey;
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
                comboLinkWindow = set.comboLinkWindow,
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

            if (set.charge != null)
            {
                view.chargeStageThresholds.AddRange(set.charge.stageThresholds);
                view.chargeInterruptActions = set.charge.interruptActions;
                view.fullChargeVfxKey = set.charge.fullChargeVfxKey;
                view.fullChargeVfxSocket = set.charge.fullChargeVfxSocket;
                view.fullChargeVfxOffset = set.charge.fullChargeVfxOffset;
                for (int i = 0; i < set.charge.stages.Count; i++)
                {
                    PlayerAttackInfo attack = Resolve(set.charge.stages[i]);
                    if (attack?.baseInfo == null) continue;
                    if (view.chargeAnimKey == AnimKey.None)
                        view.chargeAnimKey = attack.baseInfo.animKey;
                    view.chargeStages.Add(new ChargeStageData
                    {
                        hitPhases = attack.baseInfo.hitPhases,
                        interruptActions = attack.interruptActions,
                    });
                }
            }

            for (int i = 0; i < set.comboRoutes.Count; i++)
            {
                AbilityComboRouteDefinition source = set.comboRoutes[i];
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
                    attackInfo = Resolve(source.ability),
                    priority = source.priority,
                    perfectWindow = source.perfectWindow,
                    enhancedAttackInfo = Resolve(source.enhancedAbility),
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
            List<PlayerAttackInfo> destination)
        {
            IReadOnlyList<GameplayAbilitySO> abilities = set.GetCombatSequence(slot);
            for (int i = 0; i < abilities.Count; i++)
            {
                PlayerAttackInfo attack = Resolve(abilities[i]);
                if (attack?.baseInfo != null) destination.Add(attack);
            }
        }

        private static void AddPlayerSlot(
            AbilitySetSO set,
            PlayerSkillSlot slot,
            List<PlayerAttackInfo> destination)
        {
            PlayerAttackInfo attack = Resolve(set.GetPlayerAbility(slot));
            if (attack?.baseInfo != null) destination.Add(attack);
        }

        private static PlayerAttackInfo Resolve(GameplayAbilitySO ability)
        {
            if (ability?.variants == null) return null;
            AbilityVariantDefinition best = null;
            for (int i = 0; i < ability.variants.Count; i++)
            {
                AbilityVariantDefinition candidate = ability.variants[i];
                if (!UPlayGroundAbilityPayloadResolver.TryResolve(
                        candidate, out _, out _))
                    continue;
                if (best == null || candidate.priority > best.priority)
                    best = candidate;
            }
            return UPlayGroundAbilityPayloadResolver.TryResolve(
                best, out _, out PlayerAttackInfo attack)
                ? attack
                : null;
        }
    }
}
