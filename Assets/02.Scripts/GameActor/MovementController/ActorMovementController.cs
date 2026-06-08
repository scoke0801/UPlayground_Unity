using System;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround;
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
        public event Action<GameActorState, GameActorState> OnStateChanged;

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

            if (_currentState is PlayerFinishAttackState { IsTransitionLocked: true })
            {
                return;
            }

            if (_currentState is PlayerDeathState
                && Actor is PlayerActor playerActor
                && !playerActor.IsAlive()
                && newState is not PlayerDeathState)
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
            OnStateChanged?.Invoke(oldState, _currentState);
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
        Skew,
        // delta-warp: 원본 루트 델타를 재생하며 잔여 보정을 루트모션 비례 분배 → 커브 보존 + 정확 착지.
        // 신규 표준 경로. Additive/Scale/Skew 는 레거시(기존 .asset 호환)로 보존.
        DeltaWarp
    }

    public enum MotionWarpTargetPolicy
    {
        Snapshot,
        Live,
        // 매 프레임 anchor 위치를 갱신하면서, 추정된 타겟 속도 × predictionFactor × 남은 시간 만큼
        // 미래 위치를 예측해 보정. Live 단독 사용 시의 떨림을 줄이고 빠른 타겟 추적 정확도 개선.
        Predictive,
    }

    /// <summary>
    /// 워프 Y축 처리 정책. 기본은 IgnoreY 로 1차 동작 호환.
    /// </summary>
    public enum WarpYPolicy
    {
        IgnoreY = 0,         // 수평 보정만, Y 는 루트모션/중력에 위임 (현재 ignoreY=true 동등).
        MatchTargetY,        // 타겟 Y 도 적극 추적. 점프/공중 마무리 공격용.
        ProjectToTargetY,    // 워프 진행도에 따라 Y 도 점진 보간. 지면 높이 차 흡수용.
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
        public WarpYPolicy yPolicy;
        public bool overrideDistance;
        public float minDistance;
        public float maxDistance;
        public float maxSpeed;
        public Vector3 targetOffset;
        // 정규화 시간 t 를 회전 보간 알파로 매핑하는 곡선. null 이면 EaseOut(1-(1-t)^2) 폴백.
        public AnimationCurve rotationCurve;
        // Predictive 정책에서 타겟 속도를 어느 정도 가산할지 (0~1). 0 = Live 와 동일.
        public float predictionFactor;

        // ── 루트모션 속도 증폭 (직교 프리멀티플라이어) ──
        // 타겟 워프와 별개로, 루트모션 고유 속도 곡선에 게인을 곱해 증폭한다.
        // 타겟이 없어도 동작하며, 타겟이 있으면 증폭된 속도 위에서 워프가 합성된다.
        public bool amplifyEnabled;
        // 정규화 시간 t(0~1) → 게인 배율. null/빈 커브면 증폭 없음.
        // 접지 프레임 ≈1, 버스트 구간만 >1 로 두어 풋 슬라이딩을 최소화한다.
        public AnimationCurve amplifyGainCurve;
        // 증폭 결과 수평 속력의 자체 상한. 기존 워프 maxSpeed 와 분리.
        public float amplifyMaxSpeed;

        // ── delta-warp 캐시 키용 윈도우 절대 시간 (MotionEvent 의 start/endTime) ──
        // 액션 재생 간 동일 → 윈도우 총 루트모션 캐시 키의 일부.
        public float windowStartTime;
        public float windowEndTime;

        // ── 에디터 베이크 시드 (첫 시전부터 정확 delta-warp) ──
        // bakedValid 면 런타임 캐시 lookup 보다 우선해 BeginWarpWindow 가 _activeTotal 을 직접 시드한다.
        // 콤보/스킬처럼 세션 내 같은 단 재시전이 드물어 캐시가 못 데워지는 경우(=대부분의 실전 스윙)에도
        // 첫 시전부터 정확 모드로 진입한다. 베이크는 실제 액터 프리팹의 ActorAnimator.DeltaPosition 누적이라
        // 런타임 측정과 동일 소스·동일 스케일 → 변환 불필요.
        public bool    bakedValid;
        public Vector3 bakedLocalTotal;   // facing-불변 로컬 수평 총 변위 (런타임 _accumRootLocal 과 동일 정의)
        public float   bakedPathLen;      // 수평 경로 길이 (런타임 _accumRootPath 과 동일 정의)

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
                yPolicy = WarpYPolicy.IgnoreY,
                overrideDistance = false,
                minDistance = 0.3f,
                maxDistance = 4f,
                maxSpeed = 18f,
                targetOffset = Vector3.zero,
                rotationCurve = null,
                predictionFactor = 0.5f,
                amplifyEnabled = false,
                amplifyGainCurve = null,
                amplifyMaxSpeed = 25f,
                windowStartTime = 0f,
                windowEndTime = 0f,
                bakedValid = false,
                bakedLocalTotal = Vector3.zero,
                bakedPathLen = 0f,
            };
        }

        /// <summary>
        /// ignoreY bool 과 yPolicy enum 의 호환 매핑.
        /// 데이터 측에서 yPolicy 가 IgnoreY 기본값이면 ignoreY bool 을 우선 사용.
        /// </summary>
        public WarpYPolicy ResolveYPolicy()
        {
            if (yPolicy != WarpYPolicy.IgnoreY) return yPolicy;
            return ignoreY ? WarpYPolicy.IgnoreY : WarpYPolicy.MatchTargetY;
        }
    }

    /// <summary>
    /// MotionSet 이벤트로 열린 워프 구간에서 루트모션 속도를 타겟 방향으로 보정한다.
    /// State는 타겟 선택과 Combat 타이머만 전달하고, 스냅샷/도달 가능성/블렌딩은 여기서 공통 처리한다.
    /// </summary>
    /// <summary>
    /// 워프 캔슬 사유. OnWarpCancelled 이벤트와 함께 전달.
    /// </summary>
    public enum WarpCancelReason
    {
        ExternalEnd,        // EndMotionWarp 가 외부에서 조기 호출됨 (Hit/KnockBack/사망)
        OutOfRangeTimeout,  // OOR 누적 시간이 임계 초과
        TargetLost,         // 타겟이 파괴/사망
        ManualClear,        // ClearTarget 호출
    }

    public class MotionWarpController : MonoBehaviour
    {
        private const float DefaultContactBuffer = 0.08f;
        private const float CloseRangeStopBuffer = 0.12f;
        private const float CloseRangeTangentRetention = 0f;

        // OOR 누적 시간이 이 값을 초과하면 자동 캔슬.
        private const float OutOfRangeCancelThreshold = 0.1f;

        // ── 타겟 데이터 모델 (Phase 1 통합 + Phase 4 멀티 키) ──────────────
        // _targets: 현재 컨트롤러가 알고 있는 모든 키 → 타겟 매핑.
        // _activeKey: 현재 워프 윈도우/평가에 사용되는 키.
        // _activeTarget: _targets[_activeKey] 의 캐시 — 기존 코드 경로 호환용.
        public const string DefaultTargetKey = "primary";
        private readonly Dictionary<string, MotionWarpTarget> _targets = new();
        private string _activeKey = DefaultTargetKey;
        private MotionWarpTarget _activeTarget = MotionWarpTarget.None;
        private Vector3 _snapshotPosition;

        public string ActiveKey => _activeKey;
        public MotionWarpTarget GetTarget(string key)
            => key != null && _targets.TryGetValue(key, out var t) ? t : MotionWarpTarget.None;
        // ──────────────────────────────────────────────────────────────

        private bool _feasibilityChecked;
        private bool _isApplicable;
        private float _blendWeight;
        private MotionWarpWindowSettings _windowSettings = MotionWarpWindowSettings.Default(0f);
        private bool _hasWindowSettings;
        private string _lastFailureReason = string.Empty;
        private float _lastArrivalError;

        // ── 워프 타이머 (Combat 에서 이전) ──────────────────────────────
        // MotionEvent_MotionWarp.Execute 시 BeginMotionWarp 로 주입되고,
        // 매 프레임 deltaTime 만큼 소모하며 0 이하가 되면 워프 비활성.
        private float _warpRemainingTime;
        private float _warpTotalDuration;
        // OOR 누적 시간. 임계 초과 시 자동 캔슬.
        private float _outOfRangeAccumulator;
        // ──────────────────────────────────────────────────────────────

        // ── 회전 보간 시작점 (Phase 3) ───────────────────────────────────
        // 워프가 처음 applicable 한 프레임의 회전을 캡처해 곡선 보간의 기점으로 사용.
        private Quaternion _warpStartRotation = Quaternion.identity;
        private bool _warpStartCaptured;
        // ──────────────────────────────────────────────────────────────

        // ── delta-warp 모델 (C-exact 지연 캐싱) ──────────────────────────
        // 윈도우 동안 누적되는 "순수 애니메이션 루트 변위" — 매 프레임 raw DeltaPosition 을
        // 그 프레임의 액터 회전 역변환으로 애니메이션 로컬프레임에 투영해 합산(회전 불변).
        // 스티어링/월드 facing 과 무관하므로 액션 정체성만으로 캐시 가능.
        private Vector3 _accumRootLocal;
        private float   _accumRootPath;   // 누적 경로 길이(스칼라)
        // 활성 윈도우의 캐시 키/총량. 키는 윈도우 시작 시 액터 ActorAnimator 에서 조립.
        private WarpKey _activeWarpKey;
        private bool    _hasActiveWarpKey;
        private RootMotionTotal _activeTotal;
        private bool    _hasActiveTotal;  // true 면 캐시 히트(play-2+) → 정확 delta-warp.
        // 캐시 저장 허용 여부. BeginWarpWindow 에서 true, 인터럽트(Cancel/ClearTarget/조기 EndMotionWarp)
        // 에서 false. 부분 측정(중단된 첫 캐스트)이 캐시를 영구 오염시키는 것을 막는다.
        // 자연 완료(OnCompleteEvent→EndWarpWindow)는 EndWarpWindow 가 인터럽트보다 먼저 호출되어 true 유지.
        private bool    _warpStoreCommittable;
        // 정적 공유 캐시. 키에 lossyScale 버킷이 포함되므로 동일 스케일끼리만 공유돼 리그 스케일
        // 교차오염이 없다. 인스턴스 단위였을 때의 "스폰마다 첫 캐스트 재측정"을 "(액션,스케일)당
        // 세션 1회 측정"으로 축소한다.
        // 한계(stopgap): 각 (액션,스케일)의 세션 첫 시전은 여전히 play-1 feel 폴백(정확 착지 미보장).
        //                완전 제거하려면 BeginWarpWindow 시드(에디터 베이크) 필요 — 별도 작업.
        private static readonly Dictionary<WarpKey, RootMotionTotal> _rootTotalCache = new();

        // 도메인 리로드 비활성(Enter Play Mode Options) 시 정적 캐시가 세션 간 잔존해
        // 클립/윈도우 편집 후 stale 총량을 반환할 수 있다. 매 플레이 진입 시 비운다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRootTotalCache() => _rootTotalCache.Clear();
        // ──────────────────────────────────────────────────────────────

        // ── 타겟 속도 추적 (Phase 4 Predictive) ──────────────────────────
        // 활성 타겟의 이전 위치 / 추정 속도. Predictive 정책에서 미래 위치 가산용.
        private Vector3 _targetPreviousPosition;
        private Vector3 _targetVelocity;
        private bool _hasTargetVelocityHistory;
        // ──────────────────────────────────────────────────────────────

        // ── IDamageable 캐시 (Phase 6 perf) ──────────────────────────────
        // anchor 변경 시점에만 GetComponent 재실행. EvaluateVelocity 핫패스 보호.
        private IDamageable _cachedDamageable;
        private Transform _cachedDamageableAnchor;
        // ──────────────────────────────────────────────────────────────

        // 히트스톱 등 로컬 타임스케일 반영용. 없으면 Time.deltaTime 폴백.
        private GameActor _actor;

        // ── delta-warp 캐시 타입 ─────────────────────────────────────────
        // 윈도우 총 루트모션 캐시 키: 액션 정체성(motionSetName) + 모션 인덱스 + 윈도우(start,end)
        //                          + 양자화 lossyScale 버킷.
        // 재생 속도/타겟/히트스톱과 독립이라 액션 재생 간 안정적.
        // ScaleBucket: 캐시 총량은 월드 공간 raw 루트모션에서 측정돼 lossyScale 이 반영되므로,
        //              동일 스케일끼리만 공유해야 한다(다른 스케일 = 다른 버킷 → 교차오염 차단).
        //              0.01 단위로 양자화해 float 노이즈에 의한 키 미스를 막는다.
        private readonly struct WarpKey : IEquatable<WarpKey>
        {
            public readonly string     MotionSetName;
            public readonly int        MotionIndex;
            public readonly float      StartTime;
            public readonly float      EndTime;
            public readonly Vector3Int ScaleBucket;

            public WarpKey(string motionSetName, int motionIndex, float startTime, float endTime, Vector3Int scaleBucket)
            {
                MotionSetName = motionSetName;
                MotionIndex   = motionIndex;
                StartTime     = startTime;
                EndTime       = endTime;
                ScaleBucket   = scaleBucket;
            }

            public bool IsValid => !string.IsNullOrEmpty(MotionSetName);

            public bool Equals(WarpKey o) =>
                MotionIndex == o.MotionIndex &&
                StartTime.Equals(o.StartTime) &&
                EndTime.Equals(o.EndTime) &&
                ScaleBucket == o.ScaleBucket &&
                MotionSetName == o.MotionSetName;

            public override bool Equals(object o) => o is WarpKey k && Equals(k);
            public override int GetHashCode() =>
                System.HashCode.Combine(MotionSetName, MotionIndex, StartTime, EndTime, ScaleBucket);
        }

        // 윈도우의 "순수 애니메이션 루트 변위" 총량 — facing-불변 고유 로컬프레임 기준.
        // (rawHoriz = R·localRoot 를 매 프레임 Inverse(R) 로 투영·합산하므로 액터 회전 R 이 해석적으로
        //  소거된다. 따라서 시작 프레임 스냅샷이 아니라 클립 고유의 로컬 변위. 액터 스티어/회전과 무관.)
        private readonly struct RootMotionTotal
        {
            // 클립 고유 수평 총 변위(facing-불변). normalized 만 방향 추정에 사용 — 굽은 루트 클립이면
            // 방향이 "평균 로컬 헤딩" 으로 무뎌지나 PathLen 은 항상 정확.
            public readonly Vector3 LocalTotal;
            public readonly float   PathLen;    // 총 경로 길이(스칼라)
            public RootMotionTotal(Vector3 localTotal, float pathLen)
            {
                LocalTotal = localTotal;
                PathLen    = pathLen;
            }
            public bool IsValid => PathLen > 0.0001f;
        }
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 워프가 명시적으로 캔슬될 때 발화 (정상 종료에서는 미발화).
        /// 정책: 헛스윙 마무리 — 핸들러 없이도 잔여 루트모션이 자연스럽게 재생되도록 한다.
        /// 핸들러는 디버깅/통계/특수 후속 처리에만 사용.
        /// </summary>
        public event System.Action<WarpCancelReason> OnWarpCancelled;

        public bool HasTarget => _activeTarget.IsValid;
        public Vector3 TargetPosition => GetCurrentTargetPosition();
        public bool IsApplicable => _isApplicable;
        public string LastFailureReason => _lastFailureReason;
        public float LastArrivalError => _lastArrivalError;

        public bool  IsMotionWarping   => _warpRemainingTime > 0f;
        public float WarpRemainingTime => _warpRemainingTime;
        public float WarpDuration      => _warpTotalDuration;

        // 매 프레임 갱신되는 클립 재생 속도 배율. 외부(AttackState)에서 ActorAnimator.Speed 에 적용해 풋슬라이딩을 줄인다.
        // 피드백 루프 차단: DeltaPosition 에 이미 반영된 이전 K(_prevWarpK)로 역산해 Speed=1 기준 속도를 도출한 뒤
        // desiredSpeed / baseSpeed 로 새 K를 계산 → 캐시/사전 베이크 불필요, 첫 프레임부터 동작.
        public float WarpPlayRateScale { get; private set; } = 1f;
        private float _prevWarpK = 1f;
        private const float WarpPlayRateMin = 0.5f;
        private const float WarpPlayRateMax = 1.2f;

        private Vector3 GetCurrentTargetPosition()
        {
            if (!_activeTarget.IsValid) return Vector3.zero;
            return _activeTarget.follow ? _activeTarget.ResolveWorldPosition() : _snapshotPosition;
        }

        /// <summary>
        /// 타겟 anchor 가 파괴되었거나 IDamageable 이 사망 상태인지 판정.
        /// IDamageable 은 anchor 변경 시점에만 재조회 (매 프레임 GetComponent 회피).
        /// </summary>
        private bool IsTargetUnreachableLifecycle()
        {
            if (!_activeTarget.IsValid) return true; // anchor null
            Transform anchor = _activeTarget.anchor;
            if (anchor != _cachedDamageableAnchor)
            {
                _cachedDamageableAnchor = anchor;
                _cachedDamageable = anchor.GetComponent<IDamageable>()
                                 ?? anchor.GetComponentInParent<IDamageable>();
            }
            return _cachedDamageable != null && !_cachedDamageable.IsAlive();
        }

        private void Awake()
        {
            // GameActor 는 같은 root GameObject에 있다 (AMC.EnsureReferences 와 동일 가정).
            _actor = GetComponent<GameActor>();
        }

        private void Update()
        {
            // 히트스톱 로컬 타임스케일 반영. _actor 미존재 시(스탠드얼론 테스트 등) Time.deltaTime 폴백.
            float dt = _actor != null ? _actor.DeltaTime : Time.deltaTime;
            if (_warpRemainingTime > 0f)
                _warpRemainingTime -= dt;

            UpdateTargetVelocity(dt);
        }

        private void UpdateTargetVelocity(float dt)
        {
            // 워프 비활성이면 속도 추정 불필요. 히스토리 초기화 후 조기 종료.
            if (_warpRemainingTime <= 0f || !_activeTarget.IsValid || dt <= 0f)
            {
                _hasTargetVelocityHistory = false;
                _targetVelocity = Vector3.zero;
                return;
            }

            Vector3 currentPos = _activeTarget.anchor.position;
            if (_hasTargetVelocityHistory)
            {
                // 단일 프레임 차분. 노이즈가 큰 프로젝트에서는 EMA 로 후속 개선 가능.
                _targetVelocity = (currentPos - _targetPreviousPosition) / dt;
            }
            _targetPreviousPosition = currentPos;
            _hasTargetVelocityHistory = true;
        }

        /// <summary>
        /// MotionEvent_MotionWarp.Execute 에서 호출. warpDuration = endTime - startTime.
        /// </summary>
        public void BeginMotionWarp(float warpDuration)
        {
            _warpRemainingTime = warpDuration;
            _warpTotalDuration = warpDuration;
            _outOfRangeAccumulator = 0f;
            _warpStartCaptured = false;
            // 새 워프 윈도우 시작 — 속도 히스토리는 다음 프레임부터 다시 누적.
            _hasTargetVelocityHistory = false;
        }

        /// <summary>
        /// MotionEvent_MotionWarp.OnCompleteEvent (정상 종료) 또는 외부에서 조기 종료 시 호출.
        /// 정상 종료 / 조기 종료를 구분할 수 없으므로 캔슬 이벤트는 발화하지 않는다.
        /// 명시적 캔슬은 Cancel(reason) 또는 ClearTarget 경로를 사용할 것.
        /// </summary>
        public void EndMotionWarp()
        {
            _warpRemainingTime = 0f;
            _outOfRangeAccumulator = 0f;
            // 조기 종료일 수 있으므로 부분 측정 저장을 막는다. 자연 완료 경로는
            // EndWarpWindow 가 이 호출보다 먼저 실행되어 이미 캐시 저장을 마친 뒤다.
            _warpStoreCommittable = false;
        }

        /// <summary>
        /// 명시적 사유로 즉시 캔슬. 워프 중일 때만 OnWarpCancelled 발화.
        /// </summary>
        public void Cancel(WarpCancelReason reason)
        {
            bool wasWarping = _warpRemainingTime > 0f;
            _warpRemainingTime = 0f;
            _outOfRangeAccumulator = 0f;
            _warpStoreCommittable = false; // 중단된 윈도우의 부분 측정 저장 차단
            if (wasWarping)
                OnWarpCancelled?.Invoke(reason);
        }

        public void BeginWarpWindow(MotionWarpWindowSettings settings)
            => BeginWarpWindow(settings, DefaultTargetKey);

        public void BeginWarpWindow(MotionWarpWindowSettings settings, string key)
        {
            // 키가 바뀌면 활성 타겟 캐시 갱신 + 속도 히스토리 리셋.
            if (!string.IsNullOrEmpty(key) && key != _activeKey)
            {
                _activeKey = key;
                _activeTarget = _targets.TryGetValue(_activeKey, out var t) ? t : MotionWarpTarget.None;
                _hasTargetVelocityHistory = false;
            }

            _windowSettings = settings;
            _windowSettings.translationWeight = Mathf.Clamp01(_windowSettings.translationWeight);
            _windowSettings.rotationWeight = Mathf.Clamp01(_windowSettings.rotationWeight);
            _hasWindowSettings = true;

            // settings 의 정책을 _activeTarget 에 반영. World 공간 + offset 적용.
            bool useSnapshot = settings.targetPolicy == MotionWarpTargetPolicy.Snapshot;
            _activeTarget.follow = !useSnapshot;
            _activeTarget.offset = settings.targetOffset;
            _activeTarget.space  = WarpTargetSpace.World;
            _targets[_activeKey] = _activeTarget; // dict 와 캐시 동기화

            if (_activeTarget.IsValid && useSnapshot)
                _snapshotPosition = _activeTarget.ResolveWorldPosition();

            _feasibilityChecked = false;
            _isApplicable = false;
            _lastFailureReason = string.Empty;

            // ── delta-warp 윈도우 초기화: 캐시 키 조립 + 히트 조회 + 누적기 리셋 ──
            _warpStartCaptured = false;       // 시작 위치/회전을 첫 applicable 프레임에 다시 캡처
            _accumRootLocal = Vector3.zero;
            _accumRootPath  = 0f;
            _warpStoreCommittable = true;     // 인터럽트가 발생하면 false 로 내려가 부분 저장 차단

            _activeWarpKey = BuildWarpKey(settings);
            _hasActiveWarpKey = _activeWarpKey.IsValid;

            // 1순위: 에디터 베이크 시드. 콤보/스킬처럼 캐시가 못 데워지는 경우에도 첫 시전부터 정확 모드.
            //         베이크는 실제 액터 프리팹의 DeltaPosition 누적이라 런타임과 동일 정의·스케일(변환 불필요).
            if (settings.bakedValid && settings.bakedPathLen > 0.0001f)
            {
                _activeTotal = new RootMotionTotal(settings.bakedLocalTotal, settings.bakedPathLen);
                _hasActiveTotal = true;
            }
            // 2순위: 런타임 지연 캐시(play-2+). 베이크 없는 레거시 윈도우 폴백.
            else
            {
                _hasActiveTotal = _hasActiveWarpKey
                                  && _rootTotalCache.TryGetValue(_activeWarpKey, out _activeTotal)
                                  && _activeTotal.IsValid;
            }

            // K는 EvaluateVelocity 에서 매 프레임 갱신 — 여기서는 초기화만.
            WarpPlayRateScale = 1f;
            _prevWarpK = 1f;
        }

        // 현재 재생 중인 액션 정체성(ActorAnimator) + 윈도우 시간으로 캐시 키 조립.
        // 애니메이터/모션셋 미가용 시 무효 키 → 캐시 비활성(항상 play-1 feel 폴백).
        private WarpKey BuildWarpKey(in MotionWarpWindowSettings settings)
        {
            var anim = _actor != null ? _actor.Animator : null;
            if (anim == null || !anim.IsPlayingMotionSet) return default;
            string name = anim.CurrentMotionSetName;
            if (string.IsNullOrEmpty(name)) return default;

            // 캐시 총량은 월드 공간 raw 루트모션 기준이라 lossyScale 이 반영된다.
            // 0.01 단위 양자화 버킷으로 동일 스케일끼리만 공유한다(성분별 — 비균일 스케일 대응).
            Vector3 ls = transform.lossyScale;
            var scaleBucket = new Vector3Int(
                Mathf.RoundToInt(ls.x * 100f),
                Mathf.RoundToInt(ls.y * 100f),
                Mathf.RoundToInt(ls.z * 100f));

            return new WarpKey(name, anim.CurrentMotionIndex, settings.windowStartTime, settings.windowEndTime, scaleBucket);
        }

        public void EndWarpWindow()
        {
            // ── play-1 캐시 저장: 이번 윈도우에서 누적한 순수 루트 변위를 키에 저장 ──
            // 조건: 캐시 미스(첫 측정) + 유효 키 + 의미있는 누적 + "자연 완료"(_warpStoreCommittable).
            // 인터럽트(중단)된 윈도우는 부분 측정이라 저장하지 않는다 — 저장 안 하면 다음 재생이 다시 측정(안전).
            // play-2+ (_hasActiveTotal) 에서는 이미 캐시가 있으므로 재저장하지 않는다.
            if (_hasActiveWarpKey && !_hasActiveTotal && _warpStoreCommittable && _accumRootPath > 0.0001f)
                _rootTotalCache[_activeWarpKey] = new RootMotionTotal(_accumRootLocal, _accumRootPath);

            _hasActiveWarpKey = false;
            _hasActiveTotal = false;
            _warpStoreCommittable = false;
            _accumRootLocal = Vector3.zero;
            _accumRootPath = 0f;

            _hasWindowSettings = false;
            _windowSettings = MotionWarpWindowSettings.Default(0f);
            _feasibilityChecked = false;
            _isApplicable = false;
            _lastFailureReason = string.Empty;
            WarpPlayRateScale = 1f;
            _prevWarpK = 1f;
        }

        public void SetTarget(Transform target, bool useSnapshot = true)
            => SetTarget(DefaultTargetKey, target, useSnapshot);

        public void SetTarget(string key, Transform target, bool useSnapshot = true)
        {
            string useKey = string.IsNullOrEmpty(key) ? DefaultTargetKey : key;
            var t = new MotionWarpTarget
            {
                anchor = target,
                offset = _hasWindowSettings ? _windowSettings.targetOffset : Vector3.zero,
                space  = WarpTargetSpace.World,
                follow = !useSnapshot,
            };
            _targets[useKey] = t;
            // 활성 키와 같을 때만 캐시/스냅샷 갱신.
            if (useKey == _activeKey)
            {
                _activeTarget = t;
                _snapshotPosition = target != null ? t.ResolveWorldPosition() : Vector3.zero;
                _feasibilityChecked = false;
                _isApplicable = false;
                _blendWeight = 0f;
                _lastFailureReason = string.Empty;
                _hasTargetVelocityHistory = false;
            }
        }

        /// <summary>
        /// MotionWarpTarget 직접 주입. AnchorLocal/AnchorForward 같은 공간 옵션을 사용할 때.
        /// </summary>
        public void SetTarget(MotionWarpTarget target)
            => SetTarget(DefaultTargetKey, target);

        public void SetTarget(string key, MotionWarpTarget target)
        {
            string useKey = string.IsNullOrEmpty(key) ? DefaultTargetKey : key;
            _targets[useKey] = target;
            if (useKey == _activeKey)
            {
                _activeTarget = target;
                _snapshotPosition = target.IsValid && !target.follow
                    ? target.ResolveWorldPosition()
                    : Vector3.zero;
                _feasibilityChecked = false;
                _isApplicable = false;
                _blendWeight = 0f;
                _lastFailureReason = string.Empty;
                _hasTargetVelocityHistory = false;
            }
        }

        /// <summary>
        /// 모든 키의 타겟을 제거하고 워프 윈도우/타이머를 종료. (Hit/Death 등 전면 리셋용)
        /// </summary>
        public void ClearTarget()
        {
            bool wasWarping = _warpRemainingTime > 0f;

            _targets.Clear();
            _activeTarget = MotionWarpTarget.None;
            _snapshotPosition = Vector3.zero;
            _feasibilityChecked = false;
            _isApplicable = false;
            _blendWeight = 0f;
            _outOfRangeAccumulator = 0f;
            _warpStartCaptured = false;
            _hasTargetVelocityHistory = false;

            _warpStoreCommittable = false; // 전면 리셋(중단) — 부분 측정 저장 차단
            EndWarpWindow();
            _warpRemainingTime = 0f;
            if (wasWarping)
                OnWarpCancelled?.Invoke(WarpCancelReason.ManualClear);
        }

        /// <summary>
        /// 특정 키의 타겟만 제거. 활성 키와 다르면 워프 흐름은 영향 없음.
        /// </summary>
        public void ClearTarget(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            _targets.Remove(key);
            if (key == _activeKey)
            {
                bool wasWarping = _warpRemainingTime > 0f;
                _activeTarget = MotionWarpTarget.None;
                _snapshotPosition = Vector3.zero;
                _feasibilityChecked = false;
                _isApplicable = false;
                _blendWeight = 0f;
                _hasTargetVelocityHistory = false;
                _warpStoreCommittable = false; // 활성 키 타겟 제거(중단) — 부분 측정 저장 차단
                EndWarpWindow();
                _warpRemainingTime = 0f;
                if (wasWarping)
                    OnWarpCancelled?.Invoke(WarpCancelReason.ManualClear);
            }
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

            MotionWarpWindowSettings settings = _hasWindowSettings
                ? _windowSettings
                : MotionWarpWindowSettings.Default(totalDuration);

            totalDuration = settings.duration > 0f ? settings.duration : totalDuration;

            // 증폭 전 순수 루트 속도를 보존한다 — delta-warp 의 누적/캐시(윈도우 총 루트모션)와
            // 잔여 보정 추정은 "애니메이터가 만든 원본 루트모션" 을 기준으로 해야 하기 때문.
            Vector3 rawRootVelocity = rootVelocity;
            Vector3 rawHoriz = new Vector3(rawRootVelocity.x, 0f, rawRootVelocity.z);
            float rawFrameDist = rawHoriz.magnitude * deltaTime;

            // ── delta-warp 누적: 윈도우 전체에 걸쳐(타겟/적용성 게이트 "이전") 측정한다.
            //    minDistance 안쪽·OOR 프레임까지 포함해야 캐시 총량이 "순수 애니메이션 루트모션"(타겟 독립)이 된다.
            //    raw(증폭 전)를 액터 회전 역변환으로 애니메이션 로컬프레임에 투영 → facing 무관 누적.
            if (isWarping && rawFrameDist > 0f)
            {
                _accumRootPath  += rawFrameDist;
                _accumRootLocal += Quaternion.Inverse(transform.rotation) * (rawHoriz * deltaTime);
            }

            // 루트모션 속도 증폭: 타겟 게이트보다 "앞" 에서 적용한다.
            // 타겟 없는 단독 증폭이면 아래 early-return 으로 증폭된 rootVelocity 가 그대로 반환된다.
            // 타겟이 있으면(아래 delta-warp 경로) 증폭값이 gainHoriz(원본 재생 항)로 흡수되어
            // amplify 와 타겟 워프가 같은 파이프라인에서 합성된다. amplify off 면 gain=1.
            if (isWarping && settings.amplifyEnabled)
                rootVelocity = ApplyRootMotionAmplify(rootVelocity, settings, remainingTime, totalDuration);

            if (!_activeTarget.IsValid || !isWarping)
            {
                _feasibilityChecked = false;
                _isApplicable = false;
                _blendWeight = Mathf.MoveTowards(_blendWeight, 0f, deltaTime * 12f);
                _outOfRangeAccumulator = 0f;
                _lastFailureReason = !_activeTarget.IsValid ? "Target 없음" : "워프 비활성";
                return rootVelocity;
            }

            // 타겟 사망/파괴 감지 → 즉시 캔슬 (TargetLost).
            if (IsTargetUnreachableLifecycle())
            {
                _isApplicable = false;
                _lastFailureReason = "타겟 사망/파괴";
                Cancel(WarpCancelReason.TargetLost);
                cancelWarp?.Invoke();
                return rootVelocity;
            }

            // settings / totalDuration 는 함수 상단에서 이미 해석됨 (증폭 패스와 공유).
            if (settings.overrideDistance)
            {
                minDistance = settings.minDistance;
                maxDistance = settings.maxDistance;
                maxSpeed = settings.maxSpeed;
            }

            // Live(follow) 정책이면 매 프레임 갱신, Snapshot 이면 _snapshotPosition 사용.
            Vector3 targetWorld = _activeTarget.follow
                ? _activeTarget.ResolveWorldPosition()
                : _snapshotPosition;

            // Predictive: 추정 속도 × predictionFactor × 남은 시간 만큼 미래 위치를 미리 가산.
            if (settings.targetPolicy == MotionWarpTargetPolicy.Predictive
                && _hasTargetVelocityHistory
                && remainingTime > 0f)
            {
                float factor = Mathf.Clamp01(settings.predictionFactor);
                targetWorld += _targetVelocity * factor * remainingTime;
            }

            Vector3 toTarget = targetWorld - currentPosition;
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

                // OOR 누적 시간 임계 초과 시 명시 캔슬.
                _outOfRangeAccumulator += deltaTime;
                if (_outOfRangeAccumulator >= OutOfRangeCancelThreshold)
                {
                    Cancel(WarpCancelReason.OutOfRangeTimeout);
                    cancelWarp?.Invoke();
                }
                return rootVelocity;
            }

            // 정상 범위로 복귀 — 누적값 리셋.
            _outOfRangeAccumulator = 0f;

            _isApplicable = true;
            _lastFailureReason = string.Empty;
            _lastArrivalError = remainingDist;
            _blendWeight = Mathf.MoveTowards(_blendWeight, 1f, deltaTime * 15f);

            float t = totalDuration > 0f ? 1f - (remainingTime / totalDuration) : 1f;
            t = Mathf.Clamp01(t);
            float eased = 1f - (1f - t) * (1f - t);

            Vector3 targetVelocity = settings.modifierType switch
            {
                MotionWarpModifierType.DeltaWarp => EvaluateDeltaWarpVelocity(
                    rootVelocity, toTarget, remainingDist, rawFrameDist, deltaTime, eased, maxSpeed),
                MotionWarpModifierType.Scale => EvaluateScaleVelocity(rootVelocity, toTarget, remainingDist, remainingTime, maxSpeed),
                MotionWarpModifierType.Skew => EvaluateSkewVelocity(rootVelocity, toTarget, remainingDist, remainingTime, deltaTime, maxSpeed, eased),
                _ => EvaluateAdditiveVelocity(rootVelocity, toTarget, remainingDist, remainingTime, deltaTime, maxSpeed, eased)
            };

            float translationWeight = settings.translationWeight;
            Vector3 blended = Vector3.Lerp(rootVelocity, targetVelocity, _blendWeight * translationWeight);

            // Y축 정책: ignoreY bool 과 yPolicy enum 호환 매핑 후 분기.
            WarpYPolicy yPol = settings.ResolveYPolicy();
            if (yPol == WarpYPolicy.IgnoreY)
            {
                blended.y = rootVelocity.y;
            }
            else
            {
                float dy = targetWorld.y - currentPosition.y;
                float horizon = remainingTime > 0.01f ? remainingTime : deltaTime;
                float matchYSpeed = horizon > 0.0001f ? dy / horizon : 0f;
                if (yPol == WarpYPolicy.MatchTargetY)
                {
                    blended.y = matchYSpeed;
                }
                else // ProjectToTargetY: 진행도 t 기반 점진 보간
                {
                    blended.y = Mathf.Lerp(rootVelocity.y, matchYSpeed, _blendWeight * translationWeight * eased);
                }
            }

            // ── 재생 속도 배율 갱신 (캐시 불필요, 매 프레임) ────────────────────────────────
            // DeltaPosition 에는 이전 프레임에 설정한 Graph.Speed(= _prevWarpK)가 이미 곱해져 있다.
            // _prevWarpK 로 역산해 Speed=1 기준 기저 속도를 구한 뒤 desiredSpeed / baseSpeed 로 K 계산.
            // 1-프레임 래그(이전 K 역산)는 안정적이며 플레이어에게 보이지 않는다.
            float rawHorizSpeed = rawHoriz.magnitude;
            if (rawHorizSpeed > 0.001f && remainingTime > 0.01f)
            {
                float baseHorizSpeed = rawHorizSpeed / _prevWarpK;
                float desiredHorizSpeed = remainingDist / remainingTime;
                float newK = Mathf.Clamp(desiredHorizSpeed / baseHorizSpeed, WarpPlayRateMin, WarpPlayRateMax);
                WarpPlayRateScale = newK;
                _prevWarpK = newK;
            }
            // ──────────────────────────────────────────────────────────────────────────────

            return blended;
        }

        /// <summary>
        /// 공격 루트모션이 타겟 캡슐 안쪽으로 계속 밀고 들어가면 KCC가 접선 방향으로 투영하며
        /// 타겟 주변을 미끄러지는 현상이 생긴다. 타겟 표면 앞에서 접근 성분만 제한한다.
        /// </summary>
        public Vector3 ClampApproachVelocity(Vector3 velocity, Vector3 currentPosition, float deltaTime)
        {
            if (!_activeTarget.IsValid || deltaTime <= 0f)
                return velocity;

            Vector3 selfPosition = GetSelfCapsuleCenterPosition(currentPosition);
            Vector3 toTarget = GetHorizontalTargetOffset(selfPosition, _activeTarget.anchor);
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

            float desiredDistance = GetCombinedHorizontalRadius(_activeTarget.anchor) + DefaultContactBuffer;
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
            Vector3 targetPosition = _activeTarget.follow
                ? _activeTarget.ResolveWorldPosition()
                : _snapshotPosition;

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

        /// <summary>
        /// delta-warp: 원본 루트 델타를 재생(gainHoriz)하면서, 타겟까지의 잔여 보정을
        /// "이 프레임이 차지하는 루트모션 비율(share)" 만큼 분배해 더한다.
        /// 보정이 루트모션 크기에 비례 분배되므로 애니메이션의 가속–감속 커브가 워프를 구동하고,
        /// 누적 합이 타겟에 수렴해 정확 착지한다(잔여 기준 폐루프 → 스티어링/Live 타겟 드리프트 흡수).
        ///
        /// - 캐시 히트(play-2+, _hasActiveTotal): 위 정확 모드.
        /// - 캐시 미스(play-1): feel 폴백 — 속도 크기(gain×rawSpeed)는 보존하고 방향만 타겟으로
        ///   스티어. 정확 착지 보장은 없으나 곡선은 보존. 같은 프레임에 누적이 진행돼 다음 재생부터 정확.
        /// amplify 가 켜지면 gainHoriz 가 증폭돼 "더 빠르고 펀치감 있는 접근" 이 되지만 착지점은
        /// 여전히 타겟(캐시는 amplify 무관한 순수 애니메이션 총량 저장).
        /// 반환은 수평 속도(.y 는 호출부 Y 정책이 덮어씀). maxSpeed 로 수평 클램프(폐루프가 다음 프레임 보상).
        /// </summary>
        private Vector3 EvaluateDeltaWarpVelocity(
            Vector3 rootVelocity,
            Vector3 toTarget,
            float remainingDist,
            float rawFrameDist,
            float deltaTime,
            float eased,
            float maxSpeed)
        {
            Vector3 gainHoriz = new Vector3(rootVelocity.x, 0f, rootVelocity.z);
            Vector3 targetDir = remainingDist > 0.0001f ? toTarget / remainingDist : Vector3.zero;

            if (_hasActiveTotal && _activeTotal.PathLen > 0.0001f)
            {
                // 정확 모드 — 잔여 기준 폐루프.
                float remainingPath = Mathf.Max(rawFrameDist, _activeTotal.PathLen - _accumRootPath);
                Vector3 localDir = _activeTotal.LocalTotal.sqrMagnitude > 1e-6f
                    ? _activeTotal.LocalTotal.normalized
                    : Vector3.forward;
                // 남은 raw 변위를 현재 회전으로 추정(스티어링 흡수).
                Vector3 remainingRawWorld = transform.rotation * (localDir * remainingPath);
                remainingRawWorld.y = 0f;
                Vector3 correctionTotal = toTarget - remainingRawWorld; // 남은 구간서 메울 총 보정
                // remainingPath==0 (rawFrameDist==0 && accum>=PathLen, 예: settle 꼬리 + Live 타겟)이면
                // 0/0 → NaN 이 KCC 로 전파된다. 이 경우 share=1 로 디그레이드 —
                // remainingRawWorld≈0 → correctionTotal≈toTarget → 마지막 간격을 즉시 메운다(maxSpeed 클램프).
                float share = remainingPath > 1e-5f ? Mathf.Clamp01(rawFrameDist / remainingPath) : 1f;

                Vector3 frameWarped = gainHoriz * deltaTime + correctionTotal * share; // 프레임 변위
                // 주의: 큰 보정이 마지막 프레임에 집중되면 이 maxSpeed 클램프가 잔여 오프셋을 남길 수 있다
                // (다음 프레임이 없어 보상 불가). 폐루프가 평소 분산시키므로 드묾.
                return ClampHorizontal(frameWarped / deltaTime, rootVelocity.y, maxSpeed);
            }

            // feel 폴백 (play-1) — 크기 보존 + 타겟 스티어.
            float speed = gainHoriz.magnitude;
            if (speed <= 0.0001f)
                return new Vector3(0f, rootVelocity.y, 0f);
            Vector3 gainDir = gainHoriz / speed;
            Vector3 steerDir = targetDir.sqrMagnitude > 1e-6f
                ? Vector3.Slerp(gainDir, targetDir, eased).normalized
                : gainDir;
            return ClampHorizontal(steerDir * speed, rootVelocity.y, maxSpeed);
        }

        // 수평 성분만 maxSpeed 로 클램프하고 Y 는 전달값 유지.
        private static Vector3 ClampHorizontal(Vector3 velocity, float y, float maxSpeed)
        {
            Vector3 h = new Vector3(velocity.x, 0f, velocity.z);
            if (maxSpeed > 0f && h.magnitude > maxSpeed)
                h = h.normalized * maxSpeed;
            return new Vector3(h.x, y, h.z);
        }

        /// <summary>
        /// 루트모션 고유 속도 곡선을 게인으로 증폭한다 (타겟 무관 직교 패스).
        /// 게인 커브는 정규화 워프 진행도 t(0~1)를 배율로 매핑하며, 수평(XZ)에만 적용하고
        /// Y(중력/루트 수직)는 보존한다. 결과 수평 속력은 amplifyMaxSpeed 로 자체 클램프.
        /// </summary>
        private static Vector3 ApplyRootMotionAmplify(
            Vector3 rootVelocity,
            in MotionWarpWindowSettings settings,
            float remainingTime,
            float totalDuration)
        {
            var curve = settings.amplifyGainCurve;
            if (curve == null || curve.length == 0)
                return rootVelocity;

            // 워프 윈도우 진행도 — EvaluateVelocity 본문의 t 정의와 동일 공식.
            float t = totalDuration > 0f ? 1f - (remainingTime / totalDuration) : 1f;
            float gain = Mathf.Max(0f, curve.Evaluate(Mathf.Clamp01(t)));

            Vector3 horiz = new Vector3(rootVelocity.x, 0f, rootVelocity.z) * gain;
            float ceil = settings.amplifyMaxSpeed > 0f ? settings.amplifyMaxSpeed : float.MaxValue;
            if (horiz.magnitude > ceil)
                horiz = horiz.normalized * ceil;

            return new Vector3(horiz.x, rootVelocity.y, horiz.z);
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
            if (!_activeTarget.IsValid || !isWarping || !_isApplicable)
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

            Vector3 targetWorld = _activeTarget.follow
                ? _activeTarget.ResolveWorldPosition()
                : _snapshotPosition;
            Vector3 toTarget = targetWorld - currentPosition;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            bool reachable = remainingTime <= 0.01f || dist <= maxSpeed * remainingTime;
            if (dist < minDistance || dist > maxDistance || !reachable || toTarget.sqrMagnitude <= 0.01f)
                return false;

            direction = toTarget.normalized;
            return true;
        }

        /// <summary>
        /// 워프 회전 보간 결과를 한 번에 계산해 반환.
        /// rotationCurve 가 있으면 정규화 시간 t 의 곡선 알파로 Slerp(startRotation, targetRotation, alpha).
        /// 없으면 EaseOut(1-(1-t)^2) 폴백.
        /// startRotation 은 워프가 처음 applicable 해진 프레임에 캡처된다.
        /// </summary>
        public bool TryEvaluateRotation(
            Quaternion currentRotation,
            Vector3 currentPosition,
            bool isWarping,
            float remainingTime,
            float totalDuration,
            float minDistance,
            float maxDistance,
            float maxSpeed,
            out Quaternion newRotation)
        {
            newRotation = currentRotation;
            if (!TryGetFacingDirection(currentPosition, isWarping, remainingTime, minDistance, maxDistance, maxSpeed, out Vector3 dir))
                return false;

            // 첫 applicable 프레임에 startRotation 캡처.
            if (!_warpStartCaptured)
            {
                _warpStartRotation = currentRotation;
                _warpStartCaptured = true;
            }

            MotionWarpWindowSettings settings = _hasWindowSettings
                ? _windowSettings
                : MotionWarpWindowSettings.Default(0f);

            // 정규화 시간 t.
            float duration = settings.duration > 0f ? settings.duration : totalDuration;
            float t = duration > 0f ? 1f - (remainingTime / duration) : 1f;
            t = Mathf.Clamp01(t);

            // 곡선 알파 (없으면 EaseOut 폴백).
            float alpha = settings.rotationCurve != null && settings.rotationCurve.length > 0
                ? Mathf.Clamp01(settings.rotationCurve.Evaluate(t))
                : 1f - (1f - t) * (1f - t);
            alpha *= settings.rotationWeight;

            Quaternion target = Quaternion.LookRotation(dir);
            newRotation = Quaternion.Slerp(_warpStartRotation, target, alpha).normalized;
            return true;
        }

        // ── 디버그 / 모니터링 노출 (Phase 5) ──────────────────────────────
        public float BlendWeight => _blendWeight;
        public float OutOfRangeAccumulator => _outOfRangeAccumulator;
        public Vector3 TargetVelocity => _targetVelocity;
        public bool HasActiveWindow => _hasWindowSettings;
        public MotionWarpWindowSettings ActiveWindowSettings => _windowSettings;
        public MotionWarpTarget ActiveTarget => _activeTarget;
        public Vector3 SnapshotPosition => _snapshotPosition;

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;
        [SerializeField] private Color _gizmoTargetColor = new(0.20f, 0.85f, 0.30f);
        [SerializeField] private Color _gizmoMinMaxColor = new(0.85f, 0.70f, 0.10f);
        [SerializeField] private Color _gizmoReachColor  = new(0.30f, 0.55f, 0.95f);
        [SerializeField] private Color _gizmoPredictColor = new(0.95f, 0.30f, 0.65f);

        // 매 프레임 string interpolation 알로케이션 회피용 공유 빌더.
        private static readonly System.Text.StringBuilder _gizmoLabelSb = new(256);

        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos) return;
            if (!_activeTarget.IsValid) return;

            Vector3 selfPos = transform.position;
            Vector3 targetPos = _activeTarget.follow ? _activeTarget.ResolveWorldPosition() : _snapshotPosition;

            // 1) anchor 라인 — 자기 → 활성 타겟 위치.
            Gizmos.color = _gizmoTargetColor;
            Gizmos.DrawLine(selfPos, targetPos);
            Gizmos.DrawWireSphere(targetPos, 0.18f);

            // Snapshot 위치(Snapshot 정책일 때 라이브 anchor 위치와 차이를 시각화).
            if (!_activeTarget.follow && _activeTarget.anchor != null)
            {
                Gizmos.color = new Color(_gizmoTargetColor.r, _gizmoTargetColor.g, _gizmoTargetColor.b, 0.4f);
                Gizmos.DrawLine(_activeTarget.anchor.position, _snapshotPosition);
                Gizmos.DrawWireCube(_snapshotPosition, Vector3.one * 0.12f);
            }

            if (!_hasWindowSettings) return;

            // 2) min/max 디스크 — 자기 위치 기준.
            float minD = _windowSettings.minDistance;
            float maxD = _windowSettings.maxDistance;
            Gizmos.color = _gizmoMinMaxColor;
            DrawWireDisc(selfPos, minD);
            DrawWireDisc(selfPos, maxD);

            // 3) 도달 가능 영역 — maxSpeed × 남은 시간.
            float reach = _windowSettings.maxSpeed * Mathf.Max(_warpRemainingTime, 0f);
            if (reach > 0.01f)
            {
                Gizmos.color = _gizmoReachColor;
                DrawWireDisc(selfPos, reach);
            }

            // 4) Predictive 가산점.
            if (_windowSettings.targetPolicy == MotionWarpTargetPolicy.Predictive
                && _hasTargetVelocityHistory
                && _warpRemainingTime > 0f)
            {
                Vector3 predicted = targetPos + _targetVelocity * Mathf.Clamp01(_windowSettings.predictionFactor) * _warpRemainingTime;
                Gizmos.color = _gizmoPredictColor;
                Gizmos.DrawLine(targetPos, predicted);
                Gizmos.DrawWireSphere(predicted, 0.14f);
            }

            // 5) 디버그 텍스트 — 진행도/blend/OOR. StringBuilder 공유로 string interpolation 알로케이션 회피.
            float t = _warpTotalDuration > 0f ? 1f - (_warpRemainingTime / _warpTotalDuration) : 0f;
            _gizmoLabelSb.Clear();
            _gizmoLabelSb.Append("warp: t=").AppendFormat("{0:F2}", t)
                         .Append(" blend=").AppendFormat("{0:F2}", _blendWeight)
                         .Append(" OOR=").AppendFormat("{0:F2}", _outOfRangeAccumulator).Append("s\n")
                         .Append("key=").Append(_activeKey)
                         .Append(" policy=").Append(_windowSettings.targetPolicy)
                         .Append(" mod=").Append(_windowSettings.modifierType).Append('\n');
            if (_isApplicable)
                _gizmoLabelSb.Append("applicable");
            else
                _gizmoLabelSb.Append("not applicable: ").Append(_lastFailureReason);
            UnityEditor.Handles.Label(selfPos + Vector3.up * 2.2f, _gizmoLabelSb.ToString());
        }

        private static void DrawWireDisc(Vector3 center, float radius, int segments = 36)
        {
            if (radius <= 0f) return;
            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float ang = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 cur = center + new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
                Gizmos.DrawLine(prev, cur);
                prev = cur;
            }
        }
#endif
    }
}
