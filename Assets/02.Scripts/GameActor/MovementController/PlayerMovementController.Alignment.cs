using UnityEngine;
using UPlayGround.Manager;
using UPlayGround.State;
using UPlayGround.Data.Combat;

namespace UPlayGround.MovementController
{
    public partial class PlayerMovementController
    {
        private float _actionAlignmentRemaining;
        private CombatEncounterProfileSO _alignmentProfile;

        /// <summary>공격의 접근 대상과 독립적으로 락온 방향 정렬 창을 시작한다.</summary>
        public void BeginActionAlignment()
        {
            if (_alignmentProfile == null)
                _alignmentProfile = Resources.Load<CombatEncounterProfileSO>(CombatEncounterProfileSO.ResourceKey);
            _actionAlignmentRemaining = _alignmentProfile != null ? _alignmentProfile.actionAlignmentSeconds : 0f;
        }

        /// <summary>거리 제한 없이 평면 방향을 해석하되 대상이 없으면 기존 방향을 유지한다.</summary>
        public static bool TryGetAlignmentDirection(Vector3 origin, Vector3 target, Vector3 up, out Vector3 direction)
        {
            direction = Vector3.ProjectOnPlane(target - origin, up);
            if (direction.sqrMagnitude < 0.0001f) return false;
            direction.Normalize();
            return true;
        }

        public static bool IsAlignedAction(ActorStateId state) => state is ActorStateId.Attack or ActorStateId.Charge
            or ActorStateId.DashAttack or ActorStateId.JumpAttack or ActorStateId.JumpDashAttack
            or ActorStateId.Guard or ActorStateId.Ultimate;

        /// <summary>선딜이 정렬 창보다 짧은 모션도 첫 판정 전에 방향을 확정한다.</summary>
        public void CompleteActionAlignmentBeforeHit()
        {
            if (_actionAlignmentRemaining <= 0f || Motor == null
                || !Services.TryGet<ILockOnTargetService>(out var targets)) return;
            Transform target = targets.GetLockOnTarget();
            if (target != null && TryGetAlignmentDirection(Motor.TransientPosition, target.position, Motor.CharacterUp, out var direction))
                Motor.SetRotation(Quaternion.LookRotation(direction, Motor.CharacterUp));
            _actionAlignmentRemaining = 0f;
        }

        /// <summary>KCC 회전의 단일 최종 경로에서 행동 정렬과 가드 추적을 적용한다.</summary>
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            Quaternion before = currentRotation;
            base.UpdateRotation(ref currentRotation, deltaTime);
            if (_alignmentProfile == null || !IsAlignedAction(CurrentState?.StateId ?? ActorStateId.None))
            { _actionAlignmentRemaining = 0f; return; }
            bool guard = CurrentState.StateId == ActorStateId.Guard;
            if (!guard && _actionAlignmentRemaining <= 0f) return;
            if (!Services.TryGet<ILockOnTargetService>(out var targets) || targets.GetLockOnTarget() == null)
            { _actionAlignmentRemaining = 0f; return; }
            Transform target = targets.GetLockOnTarget();
            if (!TryGetAlignmentDirection(Motor.TransientPosition, target.position, Motor.CharacterUp, out var direction)) return;
            float speed = _actionAlignmentRemaining > 0f ? _alignmentProfile.actionAlignmentSpeed : _alignmentProfile.guardTrackingSpeed;
            currentRotation = Quaternion.RotateTowards(before, Quaternion.LookRotation(direction, Motor.CharacterUp),
                Mathf.Max(0f, speed * deltaTime));
            _actionAlignmentRemaining = Mathf.Max(0f, _actionAlignmentRemaining - deltaTime);
        }
    }
}
