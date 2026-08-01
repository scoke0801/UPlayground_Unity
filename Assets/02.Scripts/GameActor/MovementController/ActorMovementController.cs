using System;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround;
using UPlayGround.Debugging;
using UPlayGround.State;
using UPlayGround.CameraSystem;

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
        
        [Header("Dash")]
        public float DashSpeed = 12f;
        
        [Header("Misc")]
        public Vector3 Gravity = new Vector3(0, -30f, 0);

        protected List<Collider> IgnoredColliders = new List<Collider>();
        protected Vector3 _internalVelocityAdd = Vector3.zero;
        private Vector3 _pendingImpulseVelocity;
        private readonly List<DirectionalVelocityDamper> _impulseDampers = new();

        /// <summary>
        /// 물리 충격량 부여. 넉백/Launch 등 감속이 필요한 외부 힘에 사용.
        /// drag: 높을수록 빨리 감속 (권장: 넉백 6~10, Launch 3~5)
        /// </summary>
        public void AddImpulse(Vector3 velocity, float drag = 8f)
        {
            _pendingImpulseVelocity += velocity;

            Vector3 up = Motor != null ? Motor.CharacterUp : Vector3.up;
            float upwardSpeed = Vector3.Dot(velocity, up);
            Vector3 planarVelocity = velocity - up * upwardSpeed;
            if (planarVelocity.sqrMagnitude > 0.0001f && drag > 0f)
            {
                _impulseDampers.Add(new DirectionalVelocityDamper(planarVelocity, drag));
                _impulseDampers.Sort(CompareImpulseDampers);
            }

            if (upwardSpeed > 0f && Motor != null)
                Motor.ForceUnground();
        }

        public void ClearImpulse()
        {
            _pendingImpulseVelocity = Vector3.zero;
            _impulseDampers.Clear();
        }

        /// <summary> 현재 Impulse가 활성화 중인지 (EnemyAirborneState tumble 판정용) </summary>
        public bool HasImpulse =>
            _pendingImpulseVelocity.sqrMagnitude > 0.0001f || _impulseDampers.Count > 0;

        /// <summary>현재 KCC 권위 속도에 아직 소비되지 않은 1회성 delta-v를 합친 예측값.</summary>
        public Vector3 PredictedVelocity =>
            (Motor != null ? Motor.Velocity : Vector3.zero)
            + _internalVelocityAdd
            + _pendingImpulseVelocity;

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

        protected void Awake()
        {
            EnsureReferences();
            EnsureStateMachine();
        }

        protected virtual void OnEnable()
        {
            EnsureReferences();
            EnsureStateMachine();
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
            // Motor가 비활성화된 경우(파티 대기 상태) 상태 머신을 멈춰 InputBuffer를 공유 소비하지 않도록 함
            if (_currentState != null && (Motor == null || Motor.enabled))
            {
                // 로컬 타임스케일이 적용된 시간을 사용
                float deltaTime = Actor.DeltaTime;
                _currentState.UpdateState(deltaTime);
            }
        }

        public void AddVelocity(Vector3 velocity)
        {
            _internalVelocityAdd += velocity;
        }

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

            TransitionToState(newState);
            return true;
        }

        public bool TryTransitionToState(ActorStateId stateId)
            => StateMachine.TryTransition(stateId);

        public bool TryTransitionToState<TContext>(ActorStateId stateId, in TContext context)
            => StateMachine.TryTransition(stateId, context);
        
        /// <summary>
        /// 상태 전환
        /// </summary>
        public void TransitionToState(GameActorState newState)
        {
            if (newState == null)
            {
                Debug.LogError("Cannot transition to null state!");
                return;
            }
            
            // 대부분의 상태는 같은 타입 중복 전환을 막는다.
            // 공격 캔슬처럼 새 실행 컨텍스트로 재진입해야 하는 상태는 명시적으로 허용한다.
            if (_currentState != null
                && _currentState.GetType() == newState.GetType()
                && (!newState.AllowsSameTypeReentry
                    || !newState.CanReenterFrom(_currentState)))
            {
                return;
            }

            if (_currentState?.BlocksExitTo(newState) == true)
            {
                return;
            }
            
            GameActorState oldState = _currentState;
            
            // 이전 상태 종료
            _currentState?.OnExit(newState);
            Actor?.Animator?.FlushRootMotion();
            
            // 새 상태 설정
            _currentState = newState;
            
            // 새 상태 진입
            _currentState.OnEnter(oldState);
            OnStateChanged?.Invoke(oldState, _currentState);
        }

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

            if (_pendingImpulseVelocity.sqrMagnitude > 0f)
            {
                currentVelocity += _pendingImpulseVelocity;
                _pendingImpulseVelocity = Vector3.zero;
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
