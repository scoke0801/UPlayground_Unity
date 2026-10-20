using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Manager;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Group
{
    public partial class MonsterGroupController
    {
        private MonsterActor _comboVictim;
        private MonsterActor _comboResponder;
        private float _comboStartedAt;
        private float _lastComboHit;

        private void OnEnable() => CombatLogRecorder.ResultObserved += ObserveContinuousAttack;
        private void OnDisable()
        {
            CombatLogRecorder.ResultObserved -= ObserveContinuousAttack;
            _comboVictim = _comboResponder = null;
        }

        private bool ShouldSuppressForDuel(MonsterActor member)
        {
            if (member == null || member.Grade == MonsterActorGrade.Boss
                || !Services.TryGet<ICombatEncounterService>(out var encounter)
                || encounter.CurrentEncounter != CombatEncounterKind.Duel) return false;
            if (encounter.IsBossAttacking) return true;
            return _priorities.TryGetValue(member, out var priority) && priority == MemberPriority.Summon
                && !IsVisibleFromGameplayCamera(member.transform.position);
        }

        /// <summary>공유 슬롯 경쟁이므로 그룹 한 곳에서 대응자 한 명만 고른다.</summary>
        private void ObserveContinuousAttack(CombatResult result)
        {
            if (result.Attacker is not PlayerActor player || result.Victim is not MonsterActor victim
                || !result.DamageApplied || result.FinalDamage <= 0f || !_priorities.ContainsKey(victim)
                || !Services.TryGet<ICombatEncounterService>(out var encounter) || encounter.EncounterProfile == null) return;
            var profile = encounter.EncounterProfile;
            float now = Time.time;
            if (_comboVictim != victim || now - _lastComboHit > profile.continuousHitGap)
            { _comboVictim = victim; _comboStartedAt = now; _comboResponder = null; }
            _lastComboHit = now;
            if (now - _comboStartedAt < profile.longComboSeconds || _comboResponder != null) return;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < _members.Count; i++)
            {
                var member = _members[i];
                if (member == null || member == victim || !member.IsAlive() || ShouldSuppressForDuel(member)) continue;
                Vector3 offset = Vector3.ProjectOnPlane(member.transform.position - player.transform.position, Vector3.up);
                if (!CanRespondToLongCombo(Vector3.Dot(player.transform.forward, offset.normalized),
                    IsVisibleFromGameplayCamera(member.transform.position))) continue;
                if (offset.sqrMagnitude >= nearest) continue;
                nearest = offset.sqrMagnitude;
                _comboResponder = member;
            }
        }

        public static bool CanRespondToLongCombo(float forwardDot, bool isVisible) => isVisible && forwardDot <= 0.5f;

        private MemberPriority GetEffectivePriority(MonsterActor member, MemberPriority fallback)
        {
            var priority = _priorities.TryGetValue(member, out var registered) ? registered : fallback;
            return GetLongComboPriority(member) > 0f && priority < MemberPriority.Summoner
                ? MemberPriority.Summoner : priority;
        }

        private float GetLongComboPriority(MonsterActor member)
        {
            if (member != _comboResponder || !Services.TryGet<ICombatEncounterService>(out var encounter)
                || encounter.CurrentEncounter != CombatEncounterKind.Sweep || encounter.EncounterProfile == null
                || Time.time - _lastComboHit > encounter.EncounterProfile.continuousHitGap
                || !IsVisibleFromGameplayCamera(member.transform.position)) return 0f;
            return 2f;
        }
    }
}
