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
            { "IsHitFromBehind", "후방 피격됨" },
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
