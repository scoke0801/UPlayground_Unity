using System;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.State;

namespace UPlayGround.MovementController
{
    public partial class ActorMovementController : MonoBehaviour, ICharacterController
    {
        [Header("Stable Movement")]
        public float MaxWalkMoveSpeed = 3f;
        public float MaxRunMoveSpeed = 6.5f;
        public float MaxSprintMoveSpeed = 10f;
        public float StableMovementSharpness = 15;
        public float OrientationSharpness = 10;
        
        [Header("Crouching Movement")]
        public float MaxCrouchingMoveSpeed = 3f;
        
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
        
        [Header("Dodge")] 
        public float DodgePower = 7.5f;
        
        [Header("Dash")]
        public float DashSpeed = 18f;
        public float DashDuration = 0.3f;
        public float DashCollisionSearchRadius = 5f; // 대시 시작 시 주변 몬스터 탐색 반경
        
        [Header("Misc")]
        public bool RotationObstruction;
        public Vector3 Gravity = new Vector3(0, -30f, 0);

        protected List<Collider> IgnoredColliders = new List<Collider>();
        protected Vector3 _internalVelocityAdd = Vector3.zero;

        // Impulse (넉백/Launch 전용)
        // _internalVelocityAdd 와 분리: Impulse는 매 프레임 drag로 감속
        private Vector3 _impulseVelocity  = Vector3.zero;
        private float   _impulseDrag      = 10f;   // 감속 강도 (높을수록 빨리 멈춤)
        private bool    _hasImpulse       = false;

        /// <summary>
        /// 물리 충격량 부여. 넉백/Launch 등 감속이 필요한 외부 힘에 사용.
        /// drag: 높을수록 빨리 감속 (권장: 넉백 6~10, Launch 3~5)
        /// </summary>
        public void AddImpulse(Vector3 velocity, float drag = 8f)
        {
            _impulseVelocity += velocity;
            _impulseDrag      = drag;
            _hasImpulse       = true;

            // 위쪽 성분이 있으면 KCC 지면 판정 강제 해제 (Launch 필수)
            if (velocity.y > 0f)
                Motor.ForceUnground();
        }

        public void ClearImpulse()
        {
            _impulseVelocity = Vector3.zero;
            _hasImpulse      = false;
        }

        /// <summary> 현재 Impulse가 활성화 중인지 (EnemyAirborneState tumble 판정용) </summary>
        public bool HasImpulse => _hasImpulse;

        public KinematicCharacterMotor Motor { get; private set; }
        public GameActor Actor { get; private set; }

        protected void Awake()
        {
            Motor = GetComponent<KinematicCharacterMotor>();
            Actor = GetComponent<GameActor>();
            
            // Assign to motor
            Motor.CharacterController = this;
        }

        protected virtual void Start()
        {
        }

        protected virtual void Update()
        {
            if (_currentState != null)
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

        /// <summary>
        /// 상태 전환
        /// </summary>
        public bool TryTransitionToState(GameActorState newState)
        {
            if (newState.CanTransitionState(CurrentState.StateName) == false)
            {
                return false;
            }

            TransitionToState(newState);
            return true;
        }
        
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
            
            // 같은 타입의 상태로 전환 방지
            if (_currentState != null && _currentState.GetType() == newState.GetType())
            {
                return;
            }
            
            GameActorState oldState = _currentState;
            
            // 이전 상태 종료
            _currentState?.OnExit(newState);
            
            // 새 상태 설정
            _currentState = newState;
            
            // 새 상태 진입
            _currentState.OnEnter(oldState);
        }
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
            // ── impulse 분리 ──────────────────────────────────
            // KCC가 넘겨주는 currentVelocity에는 이전 프레임의 impulse가 합산되어 있다.
            // State에게는 impulse를 제외한 순수 stateVelocity만 넘겨야
            // impulse 성분이 stateVelocity에 영구 주입되는 버그를 방지한다.
            Vector3 stateVelocity = currentVelocity - _impulseVelocity;
            _currentState?.UpdateVelocity(ref stateVelocity, deltaTime);

            if (!Motor.GroundingStatus.IsStableOnGround)
            {
                if (_currentState is { AdjustGravity: true })
                {
                    float verticalSpeed     = Vector3.Dot(stateVelocity, Motor.CharacterUp);
                    float gravityMultiplier = verticalSpeed < 0f ? FallGravityMultiplier : RiseGravityMultiplier;
                    stateVelocity += gravityMultiplier * deltaTime * Gravity;
                }
            }

            if (_internalVelocityAdd.sqrMagnitude > 0f)
            {
                if (_internalVelocityAdd.y > 0f)
                    Motor.ForceUnground();

                stateVelocity       += _internalVelocityAdd;
                _internalVelocityAdd  = Vector3.zero;
            }

            // ── impulse 감속 + 재합산 ──────────────────────────
            if (_hasImpulse)
            {
                _impulseVelocity = Vector3.Lerp(
                    _impulseVelocity,
                    Vector3.zero,
                    1f - Mathf.Exp(-_impulseDrag * deltaTime));

                if (_impulseVelocity.sqrMagnitude < 0.01f)
                    ClearImpulse();
            }

            currentVelocity = stateVelocity + _impulseVelocity;
        }

        public virtual void BeforeCharacterUpdate(float deltaTime)
        {
            _currentState?.BeforeCharacterUpdate(deltaTime);
        }

        public virtual void AfterCharacterUpdate(float deltaTime)
        {
            _currentState?.AfterCharacterUpdate(deltaTime);
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
            return true;
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