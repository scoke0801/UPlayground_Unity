#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public static class BehaviorTreeDisplayNameRegistry
    {
        private static readonly Dictionary<string, string> BlackboardLabels = new(StringComparer.Ordinal)
        {
            { EnemyBlackboardKeys.Aggression, "공격성" },
            { EnemyBlackboardKeys.ReactionChance, "반응 확률" },
            { EnemyBlackboardKeys.CounterChance, "반격 확률" },
            { EnemyBlackboardKeys.DodgeChance, "회피 확률" },
            { EnemyBlackboardKeys.PunishRecoveryChance, "후딜 응징 확률" },
            { EnemyBlackboardKeys.AntiGuardChance, "가드 대응 확률" },
            { EnemyBlackboardKeys.MinRetreatCooldown, "최소 후퇴 쿨다운" },
            { EnemyBlackboardKeys.MaxComboPressureCount, "최대 연속 압박 횟수" },
            { EnemyBlackboardKeys.PreferredRange, "선호 교전 거리" },
            { EnemyBlackboardKeys.RecentlyHitByPlayer, "최근 피격됨" },
            { EnemyBlackboardKeys.RecentHitCount, "최근 피격 횟수" },
            { EnemyBlackboardKeys.LastHitReactionType, "마지막 피격 반응 타입" },
            { EnemyBlackboardKeys.PoiseRatio, "강인도 비율" },
            { EnemyBlackboardKeys.IsPoiseBroken, "강인도 붕괴됨" },
            { EnemyBlackboardKeys.HitReactionLockTime, "피격 반응 잠금 시간" },
            { EnemyBlackboardKeys.RevengeChance, "보복 확률" },
            { EnemyBlackboardKeys.HasAttackSlot, "공격 슬롯 확보" },
            { EnemyBlackboardKeys.IsPlayerAttacking, "플레이어 공격 중" },
            { EnemyBlackboardKeys.IsPlayerGuarding, "플레이어 가드 중" },
            { EnemyBlackboardKeys.IsPlayerRecovering, "플레이어 회복/후딜 중" },
            { EnemyBlackboardKeys.IsPlayerDodgingFrequently, "플레이어 잦은 회피" },
            { EnemyBlackboardKeys.IsPlayerAttackingFrequently, "플레이어 잦은 공격" },
            { EnemyBlackboardKeys.IsPlayerGuardingFrequently, "플레이어 잦은 가드" },
            { EnemyBlackboardKeys.IsPlayerRecoveringFrequently, "플레이어 잦은 회복/대기" },
            { EnemyBlackboardKeys.PlayerDodgeCount, "플레이어 회피 횟수" },
            { EnemyBlackboardKeys.PlayerGuardCount, "플레이어 가드 횟수" },
            { EnemyBlackboardKeys.PlayerAttackCount, "플레이어 공격 횟수" },
            { EnemyBlackboardKeys.PlayerRecoverCount, "플레이어 회복/대기 횟수" },
            { EnemyBlackboardKeys.SelectedIntent, "선택된 전투 의도" },
            { EnemyBlackboardKeys.LastIntent, "마지막 전투 의도" },
            { EnemyBlackboardKeys.ConsecutiveIntentCount, "연속 의도 횟수" },
            { EnemyBlackboardKeys.IntentScoreAttack, "공격 의도 점수" },
            { EnemyBlackboardKeys.IntentScorePunish, "응징 의도 점수" },
            { EnemyBlackboardKeys.IntentScoreCounter, "반격 의도 점수" },
            { EnemyBlackboardKeys.IntentScorePressure, "압박 의도 점수" },
            { EnemyBlackboardKeys.IntentScoreChase, "추격 의도 점수" },
            { EnemyBlackboardKeys.IntentScoreRetreat, "후퇴 의도 점수" },
            { EnemyBlackboardKeys.IntentScoreKeepDistance, "거리 유지 의도 점수" },
            { EnemyBlackboardKeys.IntentScoreDefend, "방어 의도 점수" },
            { EnemyBlackboardKeys.IntentScoreRecover, "회복 의도 점수" },
            { EnemyBlackboardKeys.CombatRhythmPhase, "전투 리듬 단계" },
            { EnemyBlackboardKeys.EnemyAIRole, "몬스터 AI 역할" },
            { EnemyBlackboardKeys.IntentWeightAttack, "공격 의도 가중치" },
            { EnemyBlackboardKeys.IntentWeightPunish, "응징 의도 가중치" },
            { EnemyBlackboardKeys.IntentWeightCounter, "반격 의도 가중치" },
            { EnemyBlackboardKeys.IntentWeightPressure, "압박 의도 가중치" },
            { EnemyBlackboardKeys.IntentWeightChase, "추격 의도 가중치" },
            { EnemyBlackboardKeys.IntentWeightRetreat, "후퇴 의도 가중치" },
            { EnemyBlackboardKeys.IntentWeightKeepDistance, "거리 유지 의도 가중치" },
            { EnemyBlackboardKeys.IntentWeightDefend, "방어 의도 가중치" },
            { EnemyBlackboardKeys.IntentWeightRecover, "회복 의도 가중치" },
        };

        private static readonly Dictionary<string, string> NodeLabels = new(StringComparer.Ordinal)
        {
            { "IsPlayerLowHealth", "플레이어 체력 낮음" },
            { "IsSelfLowHealth", "자신 체력 낮음" },
            { "HasLineOfSight", "시야 확보" },
            { "IsTargetBehind", "타겟이 후방에 있음" },
            { "IsTargetCastingOrCharging", "타겟이 시전/차지 중" },
            { "RecentlyHitByPlayer", "최근 피격됨" },
            { "RecentlyGuardBroken", "최근 가드 깨짐" },
            { "AllyNearby", "근처 아군 있음" },
            { "AllyCountNearby", "근처 아군 수 조건" },
            { "HasAttackSlot", "공격 슬롯 확보" },
            { "CooldownReady", "쿨다운 준비됨" },
            { "WasLastHitHeavy", "마지막 피격이 강함" },
            { "IsPoiseBroken", "강인도 붕괴됨" },
            { "RecentHitCountGreaterOrEqual", "최근 피격 횟수 이상" },
            { "ConsecutiveAttackCountLessThan", "연속 공격 횟수 미만" },
            { "ConsecutiveAttackCountGreaterOrEqual", "연속 공격 횟수 이상" },
            { "CanIgnoreLightHit", "약경직 무시 가능" },
            { "CanRevengeAfterHit", "피격 후 보복 가능" },
            { "SelectedIntent", "선택 의도 조건" },
            { "IsPlayerAttackingFrequently", "플레이어 잦은 공격" },
            { "IsPlayerGuardingFrequently", "플레이어 잦은 가드" },
            { "IsPlayerRecoveringFrequently", "플레이어 잦은 회복/대기" },
            { "IsHitFromBehind", "후방 피격됨" },
            { nameof(BlackboardStringConditionNode), "문자열 블랙보드 조건" },
            { nameof(IsSelfLowHealthNode), "자신 체력 낮음" },
            { nameof(HasAttackSlotNode), "공격 슬롯 확보" },
            { nameof(CooldownReadyNode), "쿨다운 준비됨" },
            { nameof(IsPoiseBrokenNode), "강인도 붕괴됨" },
            { nameof(RecentHitCountGreaterOrEqualNode), "최근 피격 횟수 이상" },
            { nameof(ConsecutiveAttackCountNode), "연속 공격 횟수 조건" },
            { nameof(CanIgnoreLightHitNode), "약경직 무시 가능" },
            { nameof(CanRevengeAfterHitNode), "피격 후 보복 가능" },
            { nameof(WasLastHitHeavyNode), "마지막 피격이 강함" },
        };

        public static string GetBlackboardLabel(string key)
            => !string.IsNullOrWhiteSpace(key) && BlackboardLabels.TryGetValue(key, out var label) ? label : key;

        public static string GetNodeTypeLabel(Type type)
        {
            if (type == null)
                return string.Empty;

            return NodeLabels.TryGetValue(type.Name, out var label) ? label : TrimNodeSuffix(type.Name);
        }

        public static string GetNodeTitle(BTNode node)
        {
            if (node == null)
                return string.Empty;

            if (NodeLabels.TryGetValue(node.GetType().Name, out var typeLabel))
                return typeLabel;

            return NodeLabels.TryGetValue(node.DisplayName, out var displayLabel) ? displayLabel : node.DisplayName;
        }

        public static string FormatWithRawName(string displayName, string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName) || string.Equals(displayName, rawName, StringComparison.Ordinal))
                return displayName;

            return $"{displayName} ({rawName})";
        }

        private static string TrimNodeSuffix(string name)
            => name.EndsWith("Node", StringComparison.Ordinal) ? name[..^4] : name;
    }
}
#endif
