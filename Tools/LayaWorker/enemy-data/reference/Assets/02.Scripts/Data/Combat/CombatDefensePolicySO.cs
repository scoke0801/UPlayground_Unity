using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Combat
{
    [CreateAssetMenu(fileName = "CombatDefensePolicy", menuName = "UPlayGround/전투/Defense Policy")]
    public class CombatDefensePolicySO : ScriptableObject
    {
        [Header("판정 규칙")]
        [Tooltip("Unblockable 공격을 가드 상태에서 막을 수 있는지. 꺼두면 Guarded보다 UnblockableHit가 우선한다.")]
        public bool allowGuardAgainstUnblockable;

        [Tooltip("Unblockable 공격을 공격 중 패리로 막을 수 있는지.")]
        public bool allowParryAgainstUnblockable;

        [Tooltip("Unblockable 공격도 퍼펙트 도지 성공으로 무효화할 수 있는지.")]
        public bool allowPerfectDodgeAgainstUnblockable = true;

        [Header("판정 시간 오버라이드")]
        [Tooltip("퍼펙트 가드 판정 창(초). 0이면 PlayerCombat의 기존 값을 사용한다.")]
        [Min(0f)] public float perfectGuardWindowSeconds;

        [Tooltip("퍼펙트 도지 판정 창(초). 0이면 PlayerCombat의 기존 값을 사용한다.")]
        [Min(0f)] public float perfectDodgeWindowSeconds;

        [Tooltip("가드 브레이크까지 허용할 피격 횟수. 0이면 PlayerCombat의 기존 값을 사용한다.")]
        [Min(0)] public int maxGuardCount;

        [Tooltip("가드 브레이크 후 재가드까지의 시간(초). 0이면 PlayerCombat의 기존 값을 사용한다.")]
        [Min(0f)] public float guardResetDelaySeconds;

        [Tooltip("어시스트 스왑 직후 패리 판정 창(초). 0이면 PlayerCombat의 기존 값을 사용한다.")]
        [Min(0f)] public float assistParryWindowSeconds;

        [Header("대시 회피 판정")]
        [Tooltip("대시 중 주변 위협을 스캔해 회피 성공을 판정할지. 끄면 '무적으로 실제 피격됐을 때'만 성립한다(구 동작).")]
        public bool enableDashEvadeThreatScan = true;

        [Tooltip("대시 회피 위협 탐색 반경(미터). 0이면 PlayerDashState의 기본값을 사용한다.")]
        [Min(0f)] public float dashEvadeThreatSearchRange;

        [Tooltip("적 히트박스가 켜지기 전 몇 초까지 위협으로 볼지. 0이면 기본값을 사용한다.")]
        [Min(0f)] public float dashEvadeWindowBeforeHit;

        [Tooltip("적 히트박스가 켜진 뒤 몇 초까지 위협으로 볼지. 0이면 기본값을 사용한다.")]
        [Min(0f)] public float dashEvadeGraceAfterHitStart;

        [Tooltip("위협 반경에 더할 여유(미터). 0이면 기본값을 사용한다.")]
        [Min(0f)] public float dashEvadeThreatRadiusPadding;

        [Header("성공 피드백 오버라이드")]
        [Tooltip("null이면 현재 코드 기본 프로필을 사용한다.")]
        public DefenseSuccessFeedbackProfile parryFeedback =
            DefenseSuccessFeedbackProfile.CreateDefault(DefenseSuccessType.Parry);

        [Tooltip("null이면 현재 코드 기본 프로필을 사용한다.")]
        public DefenseSuccessFeedbackProfile perfectGuardFeedback =
            DefenseSuccessFeedbackProfile.CreateDefault(DefenseSuccessType.PerfectGuard);

        [Tooltip("null이면 현재 코드 기본 프로필을 사용한다.")]
        public DefenseSuccessFeedbackProfile perfectDodgeFeedback =
            DefenseSuccessFeedbackProfile.CreateDefault(DefenseSuccessType.PerfectDodge);

        [Tooltip("null이면 현재 코드 기본 프로필을 사용한다.")]
        public DefenseSuccessFeedbackProfile dashEvadeFeedback =
            DefenseSuccessFeedbackProfile.CreateDashEvade();

        public bool CanGuard(AttackDefenseType defenseType)
            => defenseType != AttackDefenseType.Unblockable || allowGuardAgainstUnblockable;

        public bool CanParry(AttackDefenseType defenseType)
            => defenseType != AttackDefenseType.Unblockable || allowParryAgainstUnblockable;

        public bool CanPerfectDodge(AttackDefenseType defenseType)
            => defenseType != AttackDefenseType.Unblockable || allowPerfectDodgeAgainstUnblockable;

        public float ResolvePerfectGuardWindow(float fallback)
            => ResolvePositive(perfectGuardWindowSeconds, fallback);

        public float ResolvePerfectDodgeWindow(float fallback)
            => ResolvePositive(perfectDodgeWindowSeconds, fallback);

        public int ResolveMaxGuardCount(int fallback)
            => maxGuardCount > 0 ? maxGuardCount : Mathf.Max(1, fallback);

        public float ResolveGuardResetDelay(float fallback)
            => ResolvePositive(guardResetDelaySeconds, fallback);

        public float ResolveAssistParryWindow(float fallback)
            => ResolvePositive(assistParryWindowSeconds, fallback);

        public float ResolveDashEvadeSearchRange(float fallback)
            => ResolvePositive(dashEvadeThreatSearchRange, fallback);

        public float ResolveDashEvadeWindowBeforeHit(float fallback)
            => ResolvePositive(dashEvadeWindowBeforeHit, fallback);

        public float ResolveDashEvadeGraceAfterHitStart(float fallback)
            => ResolvePositive(dashEvadeGraceAfterHitStart, fallback);

        public float ResolveDashEvadeRadiusPadding(float fallback)
            => ResolvePositive(dashEvadeThreatRadiusPadding, fallback);

        public DefenseSuccessFeedbackProfile GetFeedbackProfile(DefenseSuccessType type)
        {
            return type switch
            {
                DefenseSuccessType.Parry => parryFeedback,
                DefenseSuccessType.PerfectGuard => perfectGuardFeedback,
                DefenseSuccessType.PerfectDodge => perfectDodgeFeedback,
                _ => null,
            };
        }

        private static float ResolvePositive(float value, float fallback)
            => value > 0f ? value : Mathf.Max(0f, fallback);
    }
}
