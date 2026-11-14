using UnityEngine;
using UnityEngine.AI;
using UPlayGround.Components;
using UPlayGround.Data.Enemy;
using UPlayGround.State;

namespace UPlayGround.MovementController
{
    public partial class EnemyMovementController
    {
        private EnemyAIContext _combatAI;
        private EnemyFlyingAIContext _flyingAI;
        private EnemyDetection _combatDetection;
        private EnemyTacticalMemory _combatMemory;
        private EnemyDodgeBudget _dodgeBudget;
        private bool _resetDodgeBudget = true;
        private bool _returningHome;
        private readonly RaycastHit[] _combatCastHits = new RaycastHit[16];
        private NavMeshPath _returnPath;
        private readonly Vector3[] _returnCorners = new Vector3[32];
        private int _returnCornerCount;
        private int _returnCornerIndex;
        private float _nextReturnPathTime;

        public EnemyCombatMovementSettings CombatMovementSettings => _combatAI != null
            ? _combatAI.BehaviorData?.combatMovement : _flyingAI?.CombatMovementSettings;
        public Vector3 CombatHome => _combatDetection != null ? _combatDetection.AggroAnchorPosition : transform.position;
        public bool IsReturningHome => _returningHome;
        public bool IsCombatReturnBlocked { get; private set; }
        public bool IsFlyingCombatant => _flyingAI != null;
        public bool IsDodgeRecovering => Time.time < _dodgeBudget.RecoveryUntil;

        public void SetCombatMemory(EnemyTacticalMemory memory) => _combatMemory = memory;

        protected override void OnEnable()
        {
            base.OnEnable();
            _returnPath ??= new NavMeshPath();
            _combatAI = GetComponent<EnemyAIContext>();
            _flyingAI = GetComponent<EnemyFlyingAIContext>();
            _combatDetection = GetComponent<EnemyDetection>();
            _combatMemory = GetComponent<EnemyTacticalMemory>();
            if (_resetDodgeBudget)
            {
                _dodgeBudget.Reset();
                _returningHome = false;
                _returnCornerCount = 0;
                _nextReturnPathTime = 0f;
                _resetDodgeBudget = false;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (!gameObject.activeInHierarchy) _resetDodgeBudget = true;
        }

        protected override bool CanEnterState(GameActorState next, bool evaluateTransitionGuard)
        {
            if (!base.CanEnterState(next, evaluateTransitionGuard)) return false;
            var settings = CombatMovementSettings;
            if (settings == null) return true;
            if (next is EnemyDodgeState dodge)
                return CanStartCombatDodge() && dodge.TryPrepareExecution();
            // 회피·반격 모션의 정상 종료 전환까지 거절하면 보호 상태에 갇힌다.
            // 보행 상태로의 인계는 허용하고 다음 모터 틱에서 귀환이 우선한다.
            if (_returningHome && IsLocomotionState(next.StateId)
                && CurrentState != null && CurrentState.BlocksBehaviorTree
                && CurrentState.StateId != ActorStateId.ReturnHome) return true;
            if ((_returningHome || IsDodgeRecovering) && IsCombatAction(next.StateId)) return false;
            return true;
        }

        private static bool IsCombatAction(ActorStateId id)
            => id is ActorStateId.Dodge or ActorStateId.Dash or ActorStateId.JumpBack or ActorStateId.Retreat or ActorStateId.Flank
                or ActorStateId.Attack or ActorStateId.Counter or ActorStateId.Guard or ActorStateId.Charge
                or ActorStateId.Chase or ActorStateId.Circle or ActorStateId.Patrol
                or ActorStateId.Flying_Dive or ActorStateId.Flying_GroundAttack or ActorStateId.Flying_Retreat
                or ActorStateId.Flying_Chase or ActorStateId.Flying_Circle or ActorStateId.Flying_AirCircle
                or ActorStateId.Flying_Patrol or ActorStateId.Flying_TakeOff;

        private static bool IsLocomotionState(ActorStateId id)
            => id is ActorStateId.Chase or ActorStateId.Circle or ActorStateId.Retreat or ActorStateId.Flank or ActorStateId.Patrol
                or ActorStateId.Flying_Chase or ActorStateId.Flying_Circle or ActorStateId.Flying_Retreat
                or ActorStateId.Flying_AirCircle or ActorStateId.Flying_Patrol;

        private static bool IsForcedOrStaged(ActorStateId id)
            => id is ActorStateId.Death or ActorStateId.Hit or ActorStateId.Stun or ActorStateId.Knockdown
                or ActorStateId.Grabbed or ActorStateId.Airborne or ActorStateId.SpecialBreakVictim
                or ActorStateId.Incapacitated or ActorStateId.StageApproach;

        public override void BeforeCharacterUpdate(float deltaTime)
        {
            base.BeforeCharacterUpdate(deltaTime);
            var settings = CombatMovementSettings;
            if (settings == null || Motor == null || CurrentState == null || Actor is MonsterActor { IsDialogueStaged: true }) return;
            bool hasTarget = _combatDetection != null && _combatDetection.HasTarget;
            _returningHome = EnemyCombatMovementPolicy.ShouldReturn(Motor.TransientPosition, CombatHome, hasTarget,
                hasTarget ? _combatDetection.CurrentTarget.position : default, _returningHome, settings);
            if (_returningHome && CurrentState.StateId != ActorStateId.ReturnHome
                && !IsForcedOrStaged(CurrentState.StateId)
                && (!CurrentState.BlocksBehaviorTree || IsLocomotionState(CurrentState.StateId)))
                TryTransitionToState(ActorStateId.ReturnHome);
        }

        #if UNITY_EDITOR
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        #endif
        protected override void ConstrainStateVelocity(ref Vector3 velocity, float deltaTime)
        {
            var settings = CombatMovementSettings;
            if (settings == null || CurrentState == null || IsForcedOrStaged(CurrentState.StateId)
                || Actor is MonsterActor { IsDialogueStaged: true }) return;
            if (IsDodgeRecovering && CurrentState.StateId != ActorStateId.ReturnHome)
                velocity = new Vector3(0f, velocity.y, 0f);
            velocity = EnemyCombatMovementPolicy.ConstrainVelocity(Motor.TransientPosition, CombatHome, velocity,
                settings.territoryRadius, deltaTime);
        }

        #if UNITY_EDITOR
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        #endif
        public bool CanStartCombatDodge()
        {
            var settings = CombatMovementSettings;
            return !_returningHome && _dodgeBudget.CanStart(Time.time, settings)
                && _combatDetection != null && _combatDetection.HasTarget
                && _combatDetection.DistanceToTarget <= settings.dodgeThreatDistance
                && _combatMemory != null && (_combatMemory.IsPlayerAttacking() || _combatMemory.IsTargetCastingOrCharging);
        }

        public void NotifyCombatDodgeStarted() => _dodgeBudget.RecordStarted(Time.time, CombatMovementSettings);
        public void NotifyCombatDodgeEnded()
        {
            if (CombatMovementSettings != null) _dodgeBudget.RecordEnded(Time.time, CombatMovementSettings);
        }

        public void NotifyCombatReturnStarted()
        {
            _returnCornerCount = 0;
            _nextReturnPathTime = 0f;
            _combatAI?.ReleaseGroupSlot();
            _combatAI?.ReleaseFormationSlot();
            _flyingAI?.ReleaseGroupSlot();
        }

        #if UNITY_EDITOR
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        #endif
        public bool TryChooseCombatDodgeDirection(Vector3 proposed, out Vector3 direction)
        {
            direction = default;
            var settings = CombatMovementSettings;
            if (settings == null || Motor == null) return false;
            Vector3 inward = CombatHome - Motor.TransientPosition;
            inward.y = 0f;
            if (inward.sqrMagnitude <= Mathf.Epsilon) inward = -transform.forward;
            inward.Normalize();
            Vector3 tangent = Vector3.Cross(Vector3.up, inward);
            float travel = MaxRunMoveSpeed * settings.dodgeSpeedRatio * settings.dodgeMovementDuration * (2f / 3f);
            for (int i = 0; i < 4; i++)
            {
                Vector3 candidate = i switch { 0 => proposed, 1 => tangent, 2 => -tangent, _ => inward };
                candidate.y = 0f;
                if (candidate.sqrMagnitude <= Mathf.Epsilon) continue;
                candidate.Normalize();
                Vector3 end = Motor.TransientPosition + candidate * travel;
                float limit = Mathf.Max(0f, settings.territoryRadius - settings.boundaryMargin);
                if (settings.territoryRadius > 0f && EnemyCombatMovementPolicy.PlanarDistance(end, CombatHome) > limit) continue;
                if (!HasClearCombatPath(candidate, travel + settings.collisionClearance, settings)) continue;
                if (!HasCombatGroundSupport(candidate, travel, settings)) continue;
                direction = candidate;
                return true;
            }
            return false;
        }

        private bool HasCombatGroundSupport(Vector3 direction, float distance, EnemyCombatMovementSettings settings)
        {
            float radius = Motor.Capsule.radius;
            int steps = Mathf.CeilToInt(distance / Mathf.Max(radius, settings.collisionClearance));
            if (steps > _returnCorners.Length) return false;
            for (int step = 1; step <= steps; step++)
            {
                Vector3 origin = Motor.TransientPosition + direction * (distance * step / steps) + Vector3.up * radius;
                int count = Physics.RaycastNonAlloc(origin, Vector3.down, _combatCastHits,
                    settings.dodgeGroundProbeDepth + radius, settings.movementObstacleMask & Motor.CollidableLayers, QueryTriggerInteraction.Ignore);
                if (count == _combatCastHits.Length) return false;
                bool supported = false;
                for (int i = 0; i < count; i++)
                {
                    var hit = _combatCastHits[i];
                    if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
                    if (!IsColliderValidForCollisions(hit.collider) || Physics.GetIgnoreCollision(Motor.Capsule, hit.collider)) continue;
                    if (_combatDetection.HasTarget && hit.collider.transform.IsChildOf(_combatDetection.CurrentTarget)) continue;
                    if (Vector3.Dot(hit.normal, Vector3.up) >= Mathf.Cos(Motor.MaxStableSlopeAngle * Mathf.Deg2Rad))
                    { supported = true; break; }
                }
                if (!supported) return false;
            }
            return true;
        }

        private bool HasClearCombatPath(Vector3 direction, float distance, EnemyCombatMovementSettings settings)
        {
            var capsule = Motor.Capsule;
            float radius = Mathf.Max(0.01f, capsule.radius - settings.collisionClearance);
            Vector3 center = Motor.TransientPosition + Vector3.up * capsule.center.y;
            float half = Mathf.Max(0f, capsule.height * 0.5f - capsule.radius);
            int count = Physics.CapsuleCastNonAlloc(center + Vector3.up * half, center - Vector3.up * half,
                radius, direction, _combatCastHits, distance, settings.movementObstacleMask & Motor.CollidableLayers, QueryTriggerInteraction.Ignore);
            if (count == _combatCastHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var hit = _combatCastHits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
                if (!IsColliderValidForCollisions(hit.collider) || Physics.GetIgnoreCollision(Motor.Capsule, hit.collider)) continue;
                // 평지·완만한 경사 접촉은 진행 장애물이 아니다.
                if (Vector3.Dot(hit.normal, Vector3.up) >= Mathf.Cos(Motor.MaxStableSlopeAngle * Mathf.Deg2Rad)) continue;
                return false;
            }
            return true;
        }

        /// <summary>직선 통로가 막히면 기존 NavMesh 경로를 재사용하며, 유효 경로가 없으면 정지한다.</summary>
        #if UNITY_EDITOR
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        #endif
        public Vector3 GetCombatReturnDirection()
        {
            var settings = CombatMovementSettings;
            Vector3 delta = CombatHome - Motor.TransientPosition;
            delta.y = 0f;
            IsCombatReturnBlocked = false;
            if (delta.magnitude <= settings.returnArrivalDistance) return Vector3.zero;
            if (HasClearCombatPath(delta.normalized, delta.magnitude, settings)
                && (_flyingAI != null || HasCombatGroundSupport(delta.normalized, Mathf.Min(delta.magnitude, settings.returnArrivalDistance), settings)))
                return delta.normalized;
            if (_flyingAI == null && Time.time >= _nextReturnPathTime)
            {
                _nextReturnPathTime = Time.time + settings.returnRepathInterval;
                _returnCornerCount = 0;
                if (NavMesh.SamplePosition(Motor.TransientPosition, out var start, settings.returnArrivalDistance, NavMesh.AllAreas)
                    && NavMesh.SamplePosition(CombatHome, out var end, settings.returnArrivalDistance, NavMesh.AllAreas)
                    && NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, _returnPath)
                    && _returnPath.status == NavMeshPathStatus.PathComplete)
                {
                    _returnCornerCount = _returnPath.GetCornersNonAlloc(_returnCorners);
                    if (_returnCornerCount == _returnCorners.Length) _returnCornerCount = 0;
                    _returnCornerIndex = 0;
                }
            }
            while (_returnCornerIndex < _returnCornerCount)
            {
                Vector3 corner = _returnCorners[_returnCornerIndex] - Motor.TransientPosition;
                corner.y = 0f;
                if (corner.magnitude <= settings.returnArrivalDistance) { _returnCornerIndex++; continue; }
                float stepDistance = Mathf.Min(corner.magnitude, settings.returnArrivalDistance);
                if (HasClearCombatPath(corner.normalized, stepDistance, settings)
                    && HasCombatGroundSupport(corner.normalized, stepDistance, settings)) return corner.normalized;
                break;
            }
            // 플레이어·동료처럼 짧은 장애물은 NavMesh가 없어도 안쪽 사선으로 돌아간다.
            // 귀속 거리가 줄어드는 후보만 허용해 좌우 왕복과 구역 이탈을 막는다.
            Vector3 side = Vector3.Cross(Vector3.up, delta.normalized);
            float detourDistance = Mathf.Min(delta.magnitude, settings.returnArrivalDistance);
            for (int i = 0; i < 2; i++)
            {
                Vector3 detour = (delta.normalized + (i == 0 ? side : -side)).normalized;
                Vector3 next = Motor.TransientPosition + detour * detourDistance;
                if (EnemyCombatMovementPolicy.PlanarDistance(next, CombatHome) >= delta.magnitude) continue;
                if (HasClearCombatPath(detour, detourDistance, settings)
                    && (_flyingAI != null || HasCombatGroundSupport(detour, detourDistance, settings)))
                    return detour;
            }
            IsCombatReturnBlocked = true;
            return Vector3.zero;
        }
    }
}
