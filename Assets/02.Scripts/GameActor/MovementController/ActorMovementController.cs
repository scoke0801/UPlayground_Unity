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
        public float DashSpeed = 18f;
        
        [Header("Misc")]
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
        public MotionWarpController MotionWarp { get; private set; }

        protected void Awake()
        {
            EnsureReferences();
        }

        protected virtual void OnEnable()
        {
            EnsureReferences();
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

namespace UPlayGround.MovementController
{
    public enum MotionWarpModifierType
    {
        Additive,
        Scale,
        Skew
    }

    public enum MotionWarpTargetPolicy
    {
        Snapshot,
        Live
    }

    public enum MotionWarpPreset
    {
        Custom,
        LightAttack,
        HeavyAttack,
        FinishAttack,
        Grab
    }

    [Serializable]
    public struct MotionWarpWindowSettings
    {
        public float duration;
        public MotionWarpPreset preset;
        public MotionWarpModifierType modifierType;
        public MotionWarpTargetPolicy targetPolicy;
        public float translationWeight;
        public float rotationWeight;
        public bool ignoreY;
        public bool overrideDistance;
        public float minDistance;
        public float maxDistance;
        public float maxSpeed;
        public Vector3 targetOffset;

        public static MotionWarpWindowSettings Default(float duration)
        {
            return new MotionWarpWindowSettings
            {
                duration = duration,
                preset = MotionWarpPreset.Custom,
                modifierType = MotionWarpModifierType.Additive,
                targetPolicy = MotionWarpTargetPolicy.Snapshot,
                translationWeight = 1f,
                rotationWeight = 1f,
                ignoreY = true,
                overrideDistance = false,
                minDistance = 0.3f,
                maxDistance = 4f,
                maxSpeed = 18f,
                targetOffset = Vector3.zero
            };
        }
    }

    /// <summary>
    /// MotionSet 이벤트로 열린 워프 구간에서 루트모션 속도를 타겟 방향으로 보정한다.
    /// State는 타겟 선택과 Combat 타이머만 전달하고, 스냅샷/도달 가능성/블렌딩은 여기서 공통 처리한다.
    /// </summary>
    public class MotionWarpController : MonoBehaviour
    {
        private const float DefaultContactBuffer = 0.08f;
        private const float CloseRangeStopBuffer = 0.12f;
        private const float CloseRangeTangentRetention = 0f;

        private Transform _target;
        private Vector3 _targetPosition;
        private bool _useSnapshot = true;
        private bool _feasibilityChecked;
        private bool _isApplicable;
        private float _blendWeight;
        private MotionWarpWindowSettings _windowSettings = MotionWarpWindowSettings.Default(0f);
        private bool _hasWindowSettings;
        private string _lastFailureReason = string.Empty;
        private float _lastArrivalError;

        public bool HasTarget => _target != null;
        public Vector3 TargetPosition => _targetPosition;
        public bool IsApplicable => _isApplicable;
        public string LastFailureReason => _lastFailureReason;
        public float LastArrivalError => _lastArrivalError;

        public void BeginWarpWindow(MotionWarpWindowSettings settings)
        {
            _windowSettings = settings;
            _windowSettings.translationWeight = Mathf.Clamp01(_windowSettings.translationWeight);
            _windowSettings.rotationWeight = Mathf.Clamp01(_windowSettings.rotationWeight);
            _hasWindowSettings = true;
            _useSnapshot = settings.targetPolicy == MotionWarpTargetPolicy.Snapshot;

            if (_target != null && _useSnapshot)
                _targetPosition = _target.position + settings.targetOffset;

            _feasibilityChecked = false;
            _isApplicable = false;
            _lastFailureReason = string.Empty;
        }

        public void EndWarpWindow()
        {
            _hasWindowSettings = false;
            _windowSettings = MotionWarpWindowSettings.Default(0f);
            _feasibilityChecked = false;
            _isApplicable = false;
            _lastFailureReason = string.Empty;
        }

        public void SetTarget(Transform target, bool useSnapshot = true)
        {
            _target = target;
            _useSnapshot = useSnapshot;
            _targetPosition = target != null
                ? target.position + (_hasWindowSettings ? _windowSettings.targetOffset : Vector3.zero)
                : Vector3.zero;
            _feasibilityChecked = false;
            _isApplicable = false;
            _blendWeight = 0f;
            _lastFailureReason = string.Empty;
        }

        public void ClearTarget()
        {
            _target = null;
            _targetPosition = Vector3.zero;
            _feasibilityChecked = false;
            _isApplicable = false;
            _blendWeight = 0f;
            EndWarpWindow();
        }

        public Vector3 EvaluateVelocity(
            Vector3 rootVelocity,
            Vector3 currentPosition,
            bool isWarping,
            float remainingTime,
            float totalDuration,
            float minDistance,
            float maxDistance,
            float maxSpeed,
            float deltaTime,
            Action cancelWarp = null)
        {
            if (deltaTime <= 0f)
                return rootVelocity;

            if (_target == null || !isWarping)
            {
                _feasibilityChecked = false;
                _isApplicable = false;
                _blendWeight = Mathf.MoveTowards(_blendWeight, 0f, deltaTime * 12f);
                _lastFailureReason = _target == null ? "Target 없음" : "워프 비활성";
                return rootVelocity;
            }

            MotionWarpWindowSettings settings = _hasWindowSettings
                ? _windowSettings
                : MotionWarpWindowSettings.Default(totalDuration);

            if (settings.overrideDistance)
            {
                minDistance = settings.minDistance;
                maxDistance = settings.maxDistance;
                maxSpeed = settings.maxSpeed;
            }

            totalDuration = settings.duration > 0f ? settings.duration : totalDuration;

            if (!_useSnapshot)
                _targetPosition = _target.position + settings.targetOffset;

            Vector3 toTarget = _targetPosition - currentPosition;
            toTarget.y = 0f;

            float remainingDist = toTarget.magnitude;
            if (!_feasibilityChecked)
            {
                bool outOfRange = remainingDist < minDistance || remainingDist > maxDistance;
                bool unreachable = totalDuration > 0f && remainingDist > maxSpeed * totalDuration;

                if (outOfRange || unreachable)
                {
                    cancelWarp?.Invoke();
                    _isApplicable = false;
                    _lastFailureReason = outOfRange ? "거리 범위 이탈" : "최대 속도로 도달 불가";
                    _blendWeight = Mathf.MoveTowards(_blendWeight, 0f, deltaTime * 12f);
                    return rootVelocity;
                }

                _feasibilityChecked = true;
            }

            if (remainingDist < minDistance || remainingDist > maxDistance || toTarget.sqrMagnitude <= 0.0001f)
            {
                _isApplicable = false;
                _lastFailureReason = toTarget.sqrMagnitude <= 0.0001f ? "타겟 거리 0" : "이동 중 거리 범위 이탈";
                _blendWeight = Mathf.MoveTowards(_blendWeight, 0f, deltaTime * 12f);
                return rootVelocity;
            }

            _isApplicable = true;
            _lastFailureReason = string.Empty;
            _lastArrivalError = remainingDist;
            _blendWeight = Mathf.MoveTowards(_blendWeight, 1f, deltaTime * 15f);

            float t = totalDuration > 0f ? 1f - (remainingTime / totalDuration) : 1f;
            t = Mathf.Clamp01(t);
            float eased = 1f - (1f - t) * (1f - t);

            Vector3 targetVelocity = settings.modifierType switch
            {
                MotionWarpModifierType.Scale => EvaluateScaleVelocity(rootVelocity, toTarget, remainingDist, remainingTime, maxSpeed),
                MotionWarpModifierType.Skew => EvaluateSkewVelocity(rootVelocity, toTarget, remainingDist, remainingTime, deltaTime, maxSpeed, eased),
                _ => EvaluateAdditiveVelocity(rootVelocity, toTarget, remainingDist, remainingTime, deltaTime, maxSpeed, eased)
            };

            float translationWeight = settings.translationWeight;
            Vector3 blended = Vector3.Lerp(rootVelocity, targetVelocity, _blendWeight * translationWeight);
            if (settings.ignoreY)
                blended.y = rootVelocity.y;

            return blended;
        }

        /// <summary>
        /// 공격 루트모션이 타겟 캡슐 안쪽으로 계속 밀고 들어가면 KCC가 접선 방향으로 투영하며
        /// 타겟 주변을 미끄러지는 현상이 생긴다. 타겟 표면 앞에서 접근 성분만 제한한다.
        /// </summary>
        public Vector3 ClampApproachVelocity(Vector3 velocity, Vector3 currentPosition, float deltaTime)
        {
            if (_target == null || deltaTime <= 0f)
                return velocity;

            Vector3 selfPosition = GetSelfCapsuleCenterPosition(currentPosition);
            Vector3 toTarget = GetHorizontalTargetOffset(selfPosition, _target);
            float distance = toTarget.magnitude;
            if (distance <= 0.0001f)
                return velocity;

            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            if (horizontalVelocity.sqrMagnitude <= 0.0001f)
                return velocity;

            Vector3 targetDirection = toTarget / distance;
            float approachSpeed = Vector3.Dot(horizontalVelocity, targetDirection);
            if (approachSpeed <= 0f)
                return velocity;

            float desiredDistance = GetCombinedHorizontalRadius(_target) + DefaultContactBuffer;
            float allowedApproachSpeed = Mathf.Max(0f, (distance - desiredDistance) / deltaTime);
            if (approachSpeed <= allowedApproachSpeed)
                return velocity;

            Vector3 approach = targetDirection * allowedApproachSpeed;
            Vector3 tangent = horizontalVelocity - targetDirection * approachSpeed;

            if (distance <= desiredDistance + CloseRangeStopBuffer)
                tangent *= CloseRangeTangentRetention;

            Vector3 clampedHorizontal = approach + tangent;
            return new Vector3(clampedHorizontal.x, velocity.y, clampedHorizontal.z);
        }

        private Vector3 GetSelfCapsuleCenterPosition(Vector3 currentPosition)
        {
            CapsuleCollider selfCapsule = GetComponent<CapsuleCollider>();
            if (selfCapsule == null)
                return currentPosition;

            Vector3 centerOffset = selfCapsule.transform.TransformPoint(selfCapsule.center) - transform.position;
            return currentPosition + centerOffset;
        }

        private Vector3 GetHorizontalTargetOffset(Vector3 currentPosition, Transform target)
        {
            Vector3 targetPosition = _useSnapshot ? _targetPosition : target.position;

            CapsuleCollider targetCapsule = target.GetComponent<CapsuleCollider>()
                                            ?? target.GetComponentInParent<CapsuleCollider>();
            if (targetCapsule != null)
                targetPosition = targetCapsule.transform.TransformPoint(targetCapsule.center);

            Vector3 toTarget = targetPosition - currentPosition;
            toTarget.y = 0f;
            return toTarget;
        }

        private float GetCombinedHorizontalRadius(Transform target)
        {
            float selfRadius = GetHorizontalRadius(GetComponent<CapsuleCollider>());
            float targetRadius = GetHorizontalRadius(
                target.GetComponent<CapsuleCollider>() ?? target.GetComponentInParent<CapsuleCollider>());

            return selfRadius + targetRadius;
        }

        private static float GetHorizontalRadius(CapsuleCollider capsule)
        {
            if (capsule == null)
                return 0.35f;

            Vector3 scale = capsule.transform.lossyScale;
            return capsule.direction switch
            {
                0 => capsule.radius * Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)),
                1 => capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)),
                _ => capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y))
            };
        }

        private static Vector3 EvaluateAdditiveVelocity(
            Vector3 rootVelocity,
            Vector3 toTarget,
            float remainingDist,
            float remainingTime,
            float deltaTime,
            float maxSpeed,
            float eased)
        {
            float baseSpeed = remainingTime > 0.01f
                ? remainingDist / remainingTime
                : remainingDist / deltaTime;

            float warpSpeed = Mathf.Lerp(baseSpeed * 1.3f, baseSpeed * 0.7f, eased);
            warpSpeed = Mathf.Clamp(warpSpeed, 0f, maxSpeed);

            Vector3 warpVelocity = toTarget.normalized * warpSpeed;
            return new Vector3(warpVelocity.x, rootVelocity.y, warpVelocity.z);
        }

        private static Vector3 EvaluateScaleVelocity(
            Vector3 rootVelocity,
            Vector3 toTarget,
            float remainingDist,
            float remainingTime,
            float maxSpeed)
        {
            Vector3 rootHorizontal = new Vector3(rootVelocity.x, 0f, rootVelocity.z);
            float rootSpeed = rootHorizontal.magnitude;
            float desiredSpeed = remainingTime > 0.01f ? remainingDist / remainingTime : maxSpeed;
            desiredSpeed = Mathf.Clamp(desiredSpeed, 0f, maxSpeed);

            if (rootSpeed <= 0.01f)
            {
                Vector3 fallback = toTarget.normalized * desiredSpeed;
                return new Vector3(fallback.x, rootVelocity.y, fallback.z);
            }

            float scale = desiredSpeed / rootSpeed;
            Vector3 scaled = toTarget.normalized * rootSpeed * scale;
            return new Vector3(scaled.x, rootVelocity.y, scaled.z);
        }

        private static Vector3 EvaluateSkewVelocity(
            Vector3 rootVelocity,
            Vector3 toTarget,
            float remainingDist,
            float remainingTime,
            float deltaTime,
            float maxSpeed,
            float eased)
        {
            Vector3 rootHorizontal = new Vector3(rootVelocity.x, 0f, rootVelocity.z);
            Vector3 targetDir = toTarget.normalized;

            float desiredSpeed = remainingTime > 0.01f
                ? remainingDist / remainingTime
                : remainingDist / deltaTime;
            desiredSpeed = Mathf.Clamp(desiredSpeed, 0f, maxSpeed);

            float rootSpeed = rootHorizontal.magnitude;
            float preservedSpeed = Mathf.Clamp(rootSpeed, 0f, maxSpeed);
            float skewSpeed = Mathf.Lerp(preservedSpeed, desiredSpeed, Mathf.Lerp(0.55f, 0.95f, eased));
            skewSpeed = Mathf.Clamp(skewSpeed, 0f, maxSpeed);

            Vector3 skewVelocity = targetDir * skewSpeed;
            return new Vector3(skewVelocity.x, rootVelocity.y, skewVelocity.z);
        }

        public bool TryGetFacingDirection(
            Vector3 currentPosition,
            bool isWarping,
            float remainingTime,
            float minDistance,
            float maxDistance,
            float maxSpeed,
            out Vector3 direction)
        {
            direction = Vector3.zero;
            if (_target == null || !isWarping || !_isApplicable)
                return false;

            MotionWarpWindowSettings settings = _hasWindowSettings
                ? _windowSettings
                : MotionWarpWindowSettings.Default(0f);

            if (settings.rotationWeight <= 0f)
                return false;

            if (settings.overrideDistance)
            {
                minDistance = settings.minDistance;
                maxDistance = settings.maxDistance;
                maxSpeed = settings.maxSpeed;
            }

            Vector3 toTarget = _targetPosition - currentPosition;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            bool reachable = remainingTime <= 0.01f || dist <= maxSpeed * remainingTime;
            if (dist < minDistance || dist > maxDistance || !reachable || toTarget.sqrMagnitude <= 0.01f)
                return false;

            direction = toTarget.normalized;
            return true;
        }
    }
}
