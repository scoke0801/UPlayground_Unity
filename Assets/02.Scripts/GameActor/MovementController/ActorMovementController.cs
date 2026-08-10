using System;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround;
using UPlayGround.Debugging;
using UPlayGround.State;
using UPlayGround.CameraSystem;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Simulation;
using UPlayGround.Diagnostics;

namespace UPlayGround.MovementController
{
    public partial class ActorMovementController : MonoBehaviour, ICharacterController, ICameraVelocityProvider
    {
        [Header("Stable Movement")]
        public float MaxWalkMoveSpeed = 3f;
        public float MaxRunMoveSpeed = 6.5f;
        public float MaxSprintMoveSpeed = 10f;
        public float StableMovementSharpness = 15;
        public float OrientationSharpness = 10;
        
        [Header("Air Movement")]
        public float MaxAirMoveSpeed = 3f;
        public float AirAccelerationSpeed = 5f;
        public float Drag = 0.1f;

        [Header("Jumping")]
        public float JumpSpeed = 10f;
        public float JumpPreGroundingGraceTime = 0.1f; // 땅에 닿기 직전 점프 입력 허용 시간
        public float JumpPostGroundingGraceTime = 0.05f; // 낭떠러지에서 떨어진 후 점프 허용 시간 (코요테 타임) — 유예 시간과 합산 고려하여 축소
        public float LandDrag = 1.5f;  // 착지 시점에 적용할 Drag은 별도로 사용한다.
         
        [Header("Jump Feel")]
        public float FallGravityMultiplier = 2.5f;   // 하강 시 중력 배율
        public float RiseGravityMultiplier = 1.5f;   // 상승 시 중력 배율 (감속 강화)

        [Header("External Velocity")]
        [Min(0f)]
        [Tooltip("AddForce MotionEvent가 요청할 수 있는 최대 상향 속도. 수평/하향 속도에는 적용하지 않습니다.")]
        public float MotionEventUpwardSpeedLimit = 12f;
        
        [Header("Dash")]
        public float DashSpeed = 12f;
        
        [Header("Misc")]
        public Vector3 Gravity = new Vector3(0, -30f, 0);

        protected List<Collider> IgnoredColliders = new List<Collider>();
        protected Vector3 _internalVelocityAdd = Vector3.zero;
        private Vector3 _pendingPlanarKnockbackVelocity;
        private PendingVerticalLaunch _pendingVerticalLaunch;
        private readonly List<DirectionalVelocityDamper> _impulseDampers = new();

        /// <summary>
        /// 다음 KCC 합성 마지막 단계에 1회성 속도 변화를 더한다.
        /// 상태 이동이나 명시적인 이동 이벤트처럼 감쇠가 필요 없는 변화에 사용한다.
        /// </summary>
        public void QueueVelocityChange(Vector3 deltaVelocity)
        {
            _internalVelocityAdd += deltaVelocity;
        }

        /// <summary>
        /// 캐릭터 Up 축을 제외한 수평 넉백을 등록한다.
        /// 잘못된 입력에 수직 성분이 있어도 접지 해제나 Launch로 승격하지 않는다.
        /// </summary>
        public void AddPlanarKnockback(Vector3 deltaVelocity, float directionalDrag = 8f)
        {
            Vector3 up = Motor != null ? Motor.CharacterUp : Vector3.up;
            Vector3 planarVelocity = Vector3.ProjectOnPlane(deltaVelocity, up);
            if (planarVelocity.sqrMagnitude <= 0.0001f)
                return;

            _pendingPlanarKnockbackVelocity += planarVelocity;
            if (directionalDrag > 0f)
            {
                _impulseDampers.Add(
                    new DirectionalVelocityDamper(planarVelocity, directionalDrag));
                _impulseDampers.Sort(CompareImpulseDampers);
            }
        }

        /// <summary>
        /// 수직 Launch와 선택적 수평 넉백을 별도 채널로 등록한다.
        /// Replace 정책은 기존 점프 속도와 같은 스텝의 여러 피격 Launch를 합산하지 않는다.
        /// </summary>
        public void AddLaunch(
            float upwardSpeed,
            Vector3 planarVelocity,
            float planarDrag = 0f,
            VerticalLaunchVelocityPolicy verticalPolicy =
                VerticalLaunchVelocityPolicy.Replace)
        {
            AddPlanarKnockback(planarVelocity, planarDrag);
            _pendingVerticalLaunch.Enqueue(upwardSpeed, verticalPolicy);
            if (_pendingVerticalLaunch.HasValue && Motor != null)
                Motor.ForceUnground();
        }

        /// <summary>
        /// MotionEvent의 로컬 이동 요청을 적용한다. 수평/하향 성분은 일반 속도 변화로,
        /// 상향 성분은 상태 허용 여부와 설정된 한계를 통과한 Launch로 처리한다.
        /// </summary>
        public void QueueMotionVelocityChange(Vector3 deltaVelocity)
        {
            Vector3 up = Motor != null ? Motor.CharacterUp : Vector3.up;
            float verticalSpeed = Vector3.Dot(deltaVelocity, up);
            Vector3 planarVelocity = deltaVelocity - up * verticalSpeed;
            if (planarVelocity.sqrMagnitude > 0.0001f)
                QueueVelocityChange(planarVelocity);

            if (verticalSpeed <= 0f)
            {
                if (verticalSpeed < 0f)
                    QueueVelocityChange(up * verticalSpeed);
                return;
            }

            float authoredUpwardSpeed =
                ExternalVelocityPolicy.ClampAuthoredUpwardSpeed(
                    verticalSpeed,
                    MotionEventUpwardSpeedLimit,
                    _currentState?.AllowsUpwardMotionVelocityChange != false);
            AddLaunch(
                authoredUpwardSpeed,
                Vector3.zero,
                verticalPolicy: VerticalLaunchVelocityPolicy.AtLeast);
        }

        /// <summary>아직 소비되지 않은 외부 속도 요청과 감쇠 modifier를 모두 제거한다.</summary>
        public void ClearExternalVelocityChanges()
        {
            _internalVelocityAdd = Vector3.zero;
            _pendingPlanarKnockbackVelocity = Vector3.zero;
            _pendingVerticalLaunch.Clear();
            _impulseDampers.Clear();
        }

        /// <summary>레거시 호출 호환. 신규 코드는 ClearExternalVelocityChanges를 사용한다.</summary>
        [Obsolete("ClearExternalVelocityChanges를 사용하세요.")]
        public void ClearImpulse() => ClearExternalVelocityChanges();

        /// <summary>레거시 호출 호환. 신규 코드는 분리된 외력 API를 사용한다.</summary>
        [Obsolete("QueueVelocityChange, AddPlanarKnockback 또는 AddLaunch를 사용하세요.")]
        public void AddImpulse(Vector3 velocity, float drag = 8f)
        {
            Vector3 up = Motor != null ? Motor.CharacterUp : Vector3.up;
            float upwardSpeed = Vector3.Dot(velocity, up);
            Vector3 planarVelocity = velocity - up * upwardSpeed;
            if (upwardSpeed > 0f)
            {
                AddLaunch(upwardSpeed, planarVelocity, drag);
                return;
            }

            AddPlanarKnockback(planarVelocity, drag);
            if (upwardSpeed < 0f)
                QueueVelocityChange(up * upwardSpeed);
        }

        /// <summary> 현재 Impulse가 활성화 중인지 (EnemyAirborneState tumble 판정용) </summary>
        public bool HasImpulse =>
            _pendingPlanarKnockbackVelocity.sqrMagnitude > 0.0001f
            || _pendingVerticalLaunch.HasValue
            || _impulseDampers.Count > 0;

        /// <summary>현재 KCC 권위 속도에 아직 소비되지 않은 1회성 delta-v를 합친 예측값.</summary>
        public Vector3 PredictedVelocity
        {
            get
            {
                Vector3 velocity =
                    (Motor != null ? Motor.Velocity : Vector3.zero)
                    + _internalVelocityAdd
                    + _pendingPlanarKnockbackVelocity;
                return ResolvePendingVerticalLaunch(velocity);
            }
        }

        private Vector3 ResolvePendingVerticalLaunch(Vector3 velocity)
        {
            if (!_pendingVerticalLaunch.HasValue)
                return velocity;

            Vector3 up = Motor != null ? Motor.CharacterUp : Vector3.up;
            float currentUpwardSpeed = Vector3.Dot(velocity, up);
            float resolvedUpwardSpeed =
                _pendingVerticalLaunch.Resolve(currentUpwardSpeed);
            return velocity + up * (resolvedUpwardSpeed - currentUpwardSpeed);
        }

        private static int CompareImpulseDampers(
            DirectionalVelocityDamper left,
            DirectionalVelocityDamper right)
        {
            int result = left.Direction.x.CompareTo(right.Direction.x);
            if (result != 0) return result;
            result = left.Direction.y.CompareTo(right.Direction.y);
            if (result != 0) return result;
            result = left.Direction.z.CompareTo(right.Direction.z);
            if (result != 0) return result;
            result = left.Drag.CompareTo(right.Drag);
            if (result != 0) return result;
            // List.Sort는 불안정 정렬이므로 여기서 0을 돌려주면 방향/Drag가 같고
            // 잔여 속도만 다른 damper의 적용 순서가 프레임마다 뒤집힐 수 있다.
            // Apply는 availableSpeed 클램프 때문에 순서에 의존하므로 완전 순서를 만든다.
            return left.RemainingSpeed.CompareTo(right.RemainingSpeed);
        }

        public KinematicCharacterMotor Motor { get; private set; }
        public Vector3 CameraVelocity => Motor != null ? Motor.Velocity : Vector3.zero;
        public GameActor Actor { get; private set; }
        public MotionWarpController MotionWarp { get; private set; }
        public ActorStateMachine StateMachine { get; private set; }
        private ActorSimulationParticipant _simulationParticipant;

        public void SetSimulationParticipant(ActorSimulationParticipant participant) =>
            _simulationParticipant = participant;

        protected void Awake()
        {
            EnsureReferences();
            EnsureStateMachine();
        }

        protected virtual void OnEnable()
        {
            EnsureReferences();
            EnsureStateMachine();
            // 비활성화 중 회수한 태그를 현재 상태 기준으로 복원한다.
            ApplySemanticStateTag(_currentState?.StateId);
        }

        protected virtual void OnDisable()
        {
            // 비활성화·풀 반환 경로에서는 OnExit이 돌지 않으므로
            // 여기서 시맨틱 상태 태그를 회수한다. 남겨 두면 재사용된 액터가
            // State.Hit/Death를 계속 보유해 피격 리액션이 영구히 차단된다.
            ClearSemanticStateTag();
            ClearExternalVelocityChanges();
        }

        private void EnsureStateMachine()
        {
            if (StateMachine != null)
                return;

            StateMachine = new ActorStateMachine(this);
            RegisterDefaultStates();
        }

        protected virtual void RegisterDefaultStates()
        {
        }

        private void EnsureReferences()
        {
            Motor = GetComponent<KinematicCharacterMotor>();
            Actor = GetComponent<GameActor>();
            _simulationParticipant = GetComponent<ActorSimulationParticipant>();
            MotionWarp = GetComponent<MotionWarpController>();
            if (MotionWarp == null)
                MotionWarp = gameObject.AddComponent<MotionWarpController>();
            
            if (Motor == null)
            {
                Debug.LogError("[ActorMovementController] KinematicCharacterMotor를 찾을 수 없습니다.", this);
                return;
            }

            // 스크립트 리컴파일 후 NonSerialized 필드가 초기화될 수 있으므로 OnEnable에서도 재할당한다.
            Motor.CharacterController = this;
        }

        protected virtual void Start()
        {
        }

        protected virtual void Update()
        {
            if (_simulationParticipant != null && _simulationParticipant.IsSuspended)
                return;

            // Motor가 비활성화된 경우(파티 대기 상태) 상태 머신을 멈춰 InputBuffer를 공유 소비하지 않도록 함
            if (_currentState != null && (Motor == null || Motor.enabled))
            {
                // 로컬 타임스케일이 적용된 시간을 사용
                float deltaTime = Actor.DeltaTime;
                _currentState.UpdateState(deltaTime);
            }
        }

        /// <summary>레거시 호출 호환. 신규 코드는 QueueVelocityChange를 사용한다.</summary>
        [Obsolete("QueueVelocityChange를 사용하세요.")]
        public void AddVelocity(Vector3 velocity) => QueueVelocityChange(velocity);

        public void AddIgnoreCollider(Collider inCollider)
        {
            IgnoredColliders.Add(inCollider);
        }

        public void RemoveIgnoreCollider(Collider inCollider)
        {
            IgnoredColliders.Remove(inCollider);
        }
    }

    public partial class ActorMovementController : MonoBehaviour, ICharacterController
    {
        // 상태 관리
        private GameActorState _currentState;
        public GameActorState CurrentState => _currentState;

        /// <summary>현재 부여 중인 시맨틱 상태 태그. ApplySemanticStateTag만 갱신한다.</summary>
        private GameplayTag _semanticStateTag;
        public event Action<GameActorState, GameActorState> OnStateChanged;

        /// <summary>
        /// 상태 전환
        /// </summary>
        public bool TryTransitionToState(GameActorState newState)
        {
            if (newState == null)
                return false;

            if (CurrentState != null && newState.CanTransitionState(CurrentState.StateId) == false)
            {
                return false;
            }

            return TransitionToState(newState);
        }

        public bool TryTransitionToState(ActorStateId stateId)
            => StateMachine.TryTransition(stateId);

        public bool TryTransitionToState<TContext>(ActorStateId stateId, in TContext context)
            => StateMachine.TryTransition(stateId, context);
        
        /// <summary>
        /// 상태 전환
        /// </summary>
        public bool TransitionToState(GameActorState newState)
        {
            if (newState == null)
            {
                Debug.LogError("Cannot transition to null state!");
                return false;
            }
            
            // 대부분의 상태는 같은 타입 중복 전환을 막는다.
            // 공격 캔슬처럼 새 실행 컨텍스트로 재진입해야 하는 상태는 명시적으로 허용한다.
            if (_currentState != null
                && _currentState.GetType() == newState.GetType()
                && (!newState.AllowsSameTypeReentry
                    || !newState.CanReenterFrom(_currentState)))
            {
                return false;
            }

            if (_currentState?.BlocksExitTo(newState) == true)
            {
                return false;
            }
            
            GameActorState oldState = _currentState;

            // 이전 상태 종료
            _currentState?.OnExit(newState);
            Actor?.Animator?.FlushRootMotion();

            // 새 상태 설정
            _currentState = newState;

            // 시맨틱 상태 태그(State.Hit/Stun/...)는 상태가 아니라 컨트롤러가 소유한다.
            // 상태 쪽 OnEnter/OnExit에 두면 파생 상태가 base 호출을 한 번만 빠뜨려도
            // 태그가 잔존하고, 그 액터는 이후 영구히 피격 리액션이 차단된다.
            // 전환 경로가 하나뿐인 이곳에서 처리해 그 사고 자체를 없앤다.
            ApplySemanticStateTag(newState.StateId);

            // 새 상태 진입
            _currentState.OnEnter(oldState);
            OnStateChanged?.Invoke(oldState, _currentState);

            if (this is PlayerMovementController)
            {
                RuntimeLog.Trace(
                    RuntimeLogCategory.Combat | RuntimeLogCategory.Player,
                    $"[PlayerState] {Actor?.name ?? name}: {oldState?.StateId.ToString() ?? "None"} -> {_currentState.StateId}",
                    Actor != null ? Actor : this);
            }

            return true;
        }

        /// <summary>
        /// 현재 시맨틱 상태 태그를 새 상태에 맞게 교체한다.
        /// <paramref name="stateId"/>가 null이면 태그를 제거하기만 한다(상태 머신 해제).
        /// </summary>
        private void ApplySemanticStateTag(ActorStateId? stateId)
        {
            GameplayTag next = stateId.HasValue
                ? GameActorState.ResolveSemanticStateTag(stateId.Value)
                : default;
            if (_semanticStateTag.Equals(next))
                return;

            if (_semanticStateTag.IsValid())
                Actor?.Tags?.RemoveTag(_semanticStateTag);
            _semanticStateTag = next;
            if (_semanticStateTag.IsValid())
                Actor?.Tags?.AddTag(_semanticStateTag);
        }

        /// <summary>
        /// 상태 머신을 놓을 때 시맨틱 상태 태그를 반드시 회수한다.
        /// 풀 반환·비활성화·씬 전환처럼 OnExit이 돌지 않는 경로를 위한 안전망.
        /// </summary>
        public void ClearSemanticStateTag() => ApplySemanticStateTag(null);

        public void TransitionToState(ActorStateId stateId)
            => StateMachine.Transition(stateId);

        public void TransitionToState<TContext>(ActorStateId stateId, in TContext context)
            => StateMachine.Transition(stateId, context);
    }
    
    public partial class ActorMovementController : MonoBehaviour, ICharacterController
    {
        public virtual void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // deltaTime은 KCCSimulator가 LocalTimeScale을 반영해서 전달
            _currentState?.UpdateRotation(ref currentRotation, deltaTime);
            currentRotation = currentRotation.normalized;
        }

        public virtual void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // currentVelocity는 KCC 충돌 해결을 거친 유일한 권위 값이다.
            // 이전 프레임 impulse를 역산하지 않고 상태, 감쇠, 신규 delta를 순서대로 합성한다.
            _currentState?.UpdateVelocity(ref currentVelocity, deltaTime);

            if (!Motor.GroundingStatus.IsStableOnGround)
            {
                if (_currentState is { GravityOwner: GravityOwnership.Controller })
                {
                    float verticalSpeed = Vector3.Dot(currentVelocity, Motor.CharacterUp);
                    float gravityMultiplier = _currentState.GetGravityMultiplier(verticalSpeed);
                    currentVelocity += gravityMultiplier * deltaTime * Gravity;
                }
            }

            _currentState?.ConstrainVelocityAfterGravity(ref currentVelocity, deltaTime);

            for (int i = _impulseDampers.Count - 1; i >= 0; i--)
            {
                var damper = _impulseDampers[i];
                damper.Apply(ref currentVelocity, deltaTime);
                if (damper.IsActive)
                    _impulseDampers[i] = damper;
                else
                    _impulseDampers.RemoveAt(i);
            }

            if (_internalVelocityAdd.sqrMagnitude > 0f)
            {
                if (Vector3.Dot(_internalVelocityAdd, Motor.CharacterUp) > 0f)
                    Motor.ForceUnground();

                currentVelocity += _internalVelocityAdd;
                _internalVelocityAdd  = Vector3.zero;
            }

            if (_pendingPlanarKnockbackVelocity.sqrMagnitude > 0f)
            {
                currentVelocity += _pendingPlanarKnockbackVelocity;
                _pendingPlanarKnockbackVelocity = Vector3.zero;
            }

            if (_pendingVerticalLaunch.HasValue)
            {
                currentVelocity = ResolvePendingVerticalLaunch(currentVelocity);
                _pendingVerticalLaunch.Clear();
            }
        }

        public virtual void BeforeCharacterUpdate(float deltaTime)
        {
            Actor?.Animator?.BeginRootMotionStep();
            _currentState?.BeforeCharacterUpdate(deltaTime);
        }

        public virtual void AfterCharacterUpdate(float deltaTime)
        {
            _currentState?.AfterCharacterUpdate(deltaTime);
            Actor?.Animator?.EndRootMotionStep();
        }

        public virtual void PostGroundingUpdate(float deltaTime)
        {
            _currentState?.PostGroundingUpdate(deltaTime);
        }

        /// <summary>
        /// 호출 시점: 모터가 주변의 물리적 장애물을 감지하고 충돌 계산을 시작하기 직전, 매 충돌 후보마다 호출됩니다.
        /// 역할: 특정 콜라이더와 충돌할지 말지를 결정하는 **'통행권 체크'**입니다.
        /// </summary>
        public virtual bool IsColliderValidForCollisions(Collider coll)
        {
            return coll != null && !IgnoredColliders.Contains(coll);
        }
        
        /// <summary>
        /// 호출 시점: 캐릭터의 아래쪽으로 발을 뻗어(Probing) 지면을 찾았을 때 호출됩니다.
        /// 역할: 캐릭터가 밟고 있는 지면에 대한 정보를 처리합니다.
        /// </summary>
        public virtual void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            _currentState?.OnGroundHit(hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
        }
        
        /// <summary>
        /// 호출 시점: 캐릭터가 실제로 Velocity에 의해 이동하다가 무언가에 '턱' 하고 걸렸을 때 호출됩니다.
        /// 역할: 이동 중 방해물을 만났을 때의 후처리를 담당합니다.
        /// </summary>
        public virtual void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
            _currentState?.OnMovementHit(hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
        }
        
        /// <summary>
        /// 호출 시점: OnGroundHit 직후, 혹은 벽에 부딪혔을 때 호출되어 해당 표면이 '안정적인지'를 최종 판정합니다.
        /// 역할: KCC의 핵심 로직 중 하나로, 부딪힌 지점의 법선(Normal) 등을 계산해서
        /// 캐릭터가 여기서 멈출지, 미끄러질지, 아니면 위로 걸어 올라갈지를 결정합니다.
        /// </summary>
        public virtual void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition,
            Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }
        
        /// <summary>
        /// 호출 시점: 일반적인 이동 계산 외에, 캐릭터가 물리 엔진의 오차 등으로 인해 물체 안으로 겹쳐졌을 때(Depenetration) 호출됩니다.
        /// 역할: 예기치 못한 충돌이나 텔레포트 등으로 인해 벽 속에 끼었을 때의 예외 처리를 담당합니다.
        /// </summary>
        public virtual void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
    }
}

namespace UPlayGround.MovementController
{
}
