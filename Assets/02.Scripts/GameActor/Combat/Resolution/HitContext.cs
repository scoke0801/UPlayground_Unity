using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Combat
{
    /// <summary>
    /// 단일 히트 1회의 입력 정보를 표현하는 값 객체 (P1 도입).
    /// legacy <see cref="AttackData"/>를 점진적으로 대체하기 위한 기준점이며, 아직 AttackData를 삭제하지 않으므로
    /// 승격하지 않은 필드(반응 물리 파라미터 등)는 <see cref="Source"/>로 접근한다.
    /// </summary>
    public readonly struct HitContext
    {
        public readonly GameActor Attacker;
        public readonly GameActor Victim;
        public readonly AnimKey AnimKey;
        public readonly int HitPhaseIndex;
        public readonly AttackKind AttackKind;
        public readonly AttackReactionType ReactionType;
        public readonly AttackDefenseType DefenseType;

        /// <summary>적용 전 raw 입력 피해(attackData.damage). 최종 피해는 <see cref="DamageResult"/>가 담는다.</summary>
        public readonly float Damage;
        public readonly float PoiseDamage;
        public readonly float BreakDamage;
        public readonly bool IsCounterAttack;

        /// <summary>raw attackData.hitPoint. 피드백의 zero 폴백 판정이 동일하게 동작하도록 가공하지 않고 보관한다.</summary>
        public readonly Vector3 HitPoint;
        public readonly Vector3 AttackDirection;
        public readonly GameObject HitTarget;
        public readonly string HitParticleName;

        /// <summary>미승격 필드 접근용 back-reference. 전환 기간 한정이며 P3/P4 이후 제거 대상.</summary>
        public readonly AttackData Source;

        public HitContext(
            GameActor attacker,
            GameActor victim,
            AnimKey animKey,
            int hitPhaseIndex,
            AttackKind attackKind,
            AttackReactionType reactionType,
            AttackDefenseType defenseType,
            float damage,
            float poiseDamage,
            float breakDamage,
            bool isCounterAttack,
            Vector3 hitPoint,
            Vector3 attackDirection,
            GameObject hitTarget,
            string hitParticleName,
            AttackData source)
        {
            Attacker = attacker;
            Victim = victim;
            AnimKey = animKey;
            HitPhaseIndex = hitPhaseIndex;
            AttackKind = attackKind;
            ReactionType = reactionType;
            DefenseType = defenseType;
            Damage = damage;
            PoiseDamage = poiseDamage;
            BreakDamage = breakDamage;
            IsCounterAttack = isCounterAttack;
            HitPoint = hitPoint;
            AttackDirection = attackDirection;
            HitTarget = hitTarget;
            HitParticleName = hitParticleName;
            Source = source;
        }

        /// <summary>legacy AttackData를 HitContext로 변환한다. victim은 피격 대상 액터.</summary>
        public static HitContext FromAttackData(AttackData attackData, GameActor victim)
        {
            if (attackData == null)
                return default;

            return new HitContext(
                attackData.attacker,
                victim,
                attackData.animKey,
                attackData.hitPhaseIndex,
                attackData.attackKind,
                attackData.reactionType,
                attackData.defenseType,
                attackData.damage,
                attackData.poiseDamage,
                attackData.breakDamage,
                attackData.isCounterAttack,
                attackData.hitPoint,
                attackData.attackDirection,
                attackData.hitTarget,
                attackData.hitParticleName,
                attackData);
        }
    }
}
