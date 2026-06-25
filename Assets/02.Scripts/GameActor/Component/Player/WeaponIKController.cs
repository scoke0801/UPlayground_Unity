using KINEMATION.MotionWarping.Runtime.Core;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Component
{
    /// <summary>
    /// 무기 IK — Phase 0 프로토타입 (보조손 그립).
    ///
    /// [목적] 설계서(docs/WEAPON_IK_SYSTEM_DESIGN.md §3, §10 Phase 0)의 타이밍 전제를 실증한다:
    ///   (a) 그립을 "주손 본 + 캐시 오프셋"으로 역산하면 현재 프레임에 정확한가
    ///   (b) MagicaCloth 손 콜라이더가 같은 프레임 IK 결과를 따라가는가 (FootIK와 동일 OnAnimatorIK 공존)
    ///   (c) TwoBoneIk 본 직접 솔브가 다음 Animator 평가에 깔끔히 리셋되는가 (drift 없음)
    ///
    /// [동작]
    ///   - OnAnimatorIK 패스에서 보조손(gripHand) 팔에 2본 IK를 적용해 그립 포인트에 밀착.
    ///   - 무기는 ParentConstraint로 주손 본에 강체 부착 → 그립의 주손-기준 로컬 오프셋을 캐시해
    ///     콘스트레인트 해석을 기다리지 않고 현재 프레임 본 포즈에서 그립 월드 좌표를 역산.
    ///   - FootIKController와 동일한 Relay 모델로 Animator GO에 OnAnimatorIK 콜백을 전달.
    ///
    /// Phase 0 한정: 겨냥/상태페이드/모델교체 자동연동/SO 정책은 제외. 그립 1개 검증에 집중.
    /// </summary>
    public class WeaponIKController : MonoBehaviour
    {
        [Header("Weight")]
        [SerializeField, Range(0f, 1f)] private float _maxWeight = 1f;
        [SerializeField, Tooltip("weight 변화 속도 (켜고 끌 때 스냅 방지).")]
        private float _weightSpeed = 12f;

        [Header("Elbow Hint")]
        [SerializeField, Range(0f, 1f), Tooltip("팔꿈치 pole 보정 강도. hint는 현재 굽힘 평면을 유지(뒤집힘 방지). 0이면 hint 무시됨.")]
        private float _elbowHintWeight = 0.5f;

        [Header("Prototype Debug")]
        [SerializeField, Tooltip("PlayerEquipment 배선 없이 에디터에서 직접 그립을 지정해 테스트.")]
        private WeaponGripPoint _debugGripOverride;
        [SerializeField] private bool _drawGizmos = true;

        private Animator _animator;

        // 보조손(그립을 잡는 손) 팔 체인
        private Transform _offUpperArm, _offLowerArm, _offHand;
        // 주손(무기를 쥔 손) — 그립 오프셋의 기준
        private Transform _mainHand;
        private bool _bonesBound;

        private WeaponGripPoint _grip;
        private float _gripWeight;          // 외부 지정 목표 weight (0~1)
        private bool _hasGrip;

        // 그립의 주손-기준 강체 로컬 오프셋.
        // 그립 transform은 ParentConstraint 자식이라 OnAnimatorIK 시점엔 "직전 프레임" 좌표다.
        // 주손이 그 사이 움직였으면 오프셋이 오염되므로, 주손이 정지한 프레임에서만 락한다.
        private Vector3 _gripLocalPos;
        private Quaternion _gripLocalRot = Quaternion.identity;
        private bool _offsetLocked;
        private Vector3 _prevMainHandPos;
        private bool _hasPrevMainHand;
        private float _gripPendingTime;          // 그립 주입 후 락 대기 시간 (폴백용)
        private const float StationaryEps = 0.0008f;   // 프레임간 주손 이동 임계 (정지 판정)
        private const float LockFallbackTime = 0.6f;   // 정지 못 잡아도 강제 락하는 시간

        private Transform _elbowHint;            // 팔꿈치 pole (collinear 뒤집힘 방지)

        private float _currentWeight;            // 보간된 적용 weight
        private bool _forceDisabled;

        // no-op/미발화 감지 워치독
        private bool _ikEverProcessed;
        private float _gripSetTime = -1f;
        private bool _warnedNoFire;

        // 디버그
        private Vector3 _dbgGripWorld, _dbgHandWorld;
        private float _dbgDistance;

        /// <summary>외부(상태머신)에서 IK 강제 비활성화. Phase 0에선 단순 게이트.</summary>
        public bool ForceDisabled
        {
            get => _forceDisabled;
            set => _forceDisabled = value;
        }

        /// <summary>현재 그립 손과 그립 포인트의 실제 거리(m). Phase 0 정확도 검증용.</summary>
        public float DebugGripDistance => _dbgDistance;

#if UNITY_EDITOR
        // ── 에디터 상태 패널 전용 게터 (WeaponIKControllerEditor) ──
        // 에디터 어셈블리에서 접근하므로 internal이 아닌 public. 빌드엔 포함되지 않음.
        public bool EditorIkEverProcessed => _ikEverProcessed;
        public bool EditorHasGrip => _hasGrip;
        public bool EditorOffsetLocked => _offsetLocked;
        public float EditorCurrentWeight => _currentWeight;
#endif

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null)
            {
                Debug.LogError("[WeaponIK] Animator를 찾을 수 없습니다.", this);
                enabled = false;
                return;
            }

            BindBones();
            SetupRelay(_animator);

            if (_debugGripOverride != null)
                SetGrip(_debugGripOverride, _debugGripOverride.defaultWeight);
        }

        /// <summary>모델 교체 시 호출. 새 Animator에 재바인딩하고 그립 오프셋을 무효화한다.</summary>
        public void Refresh(Animator newAnimator)
        {
            if (newAnimator == null)
            {
                enabled = false;
                return;
            }

            if (_animator != null && _animator.gameObject != gameObject)
            {
                var old = _animator.gameObject.GetComponent<WeaponIKRelay>();
                if (old != null) old.Owner = null;
            }

            _animator = newAnimator;
            _bonesBound = false;
            _offsetLocked = false;       // 새 스켈레톤은 본 비율이 달라 이전 오프셋 무효
            _hasPrevMainHand = false;

            // 그립이 모델과 함께 파괴됐으면(fake-null) stale 상태 정리.
            // 무기에 붙어 살아있는 그립은 유지 → BindBones가 새 스켈레톤에 재바인딩하고
            // 다음 OnAnimatorIK에서 오프셋을 재락한다.
            if (_grip == null && _hasGrip)
                ClearGrip();

            BindBones();
            SetupRelay(_animator);
            enabled = true;
        }

        /// <summary>
        /// 보조손 그립을 주입한다. PlayerEquipment가 발도/교체 완료 시 호출(예정).
        /// 오프셋은 다음 OnAnimatorIK에서 1회 캐시한다.
        /// </summary>
        public void SetGrip(WeaponGripPoint grip, float weight)
        {
            _grip = grip;
            _hasGrip = grip != null;
            _gripWeight = Mathf.Clamp01(weight);
            _offsetLocked = false;
            _hasPrevMainHand = false;
            _gripPendingTime = 0f;
            _gripSetTime = Time.time;
            _warnedNoFire = false;

            if (_hasGrip)
                BindOffHand(grip.gripHand);
        }

        public void ClearGrip()
        {
            _grip = null;
            _hasGrip = false;
            _gripWeight = 0f;
            _offsetLocked = false;
            _gripSetTime = -1f;
        }

        private void SetupRelay(Animator animator)
        {
            if (animator.gameObject == gameObject) return;

            var relay = animator.gameObject.GetComponent<WeaponIKRelay>();
            if (relay == null) relay = animator.gameObject.AddComponent<WeaponIKRelay>();
            relay.Owner = this;
        }

        private void BindBones()
        {
            if (_animator == null || !_animator.isHuman) return;

            // 그립이 이미 지정돼 있으면 해당 손, 아니면 기본 보조손(왼손) 기준으로 바인딩.
            EquipPosition offHand = _grip != null ? _grip.gripHand : EquipPosition.LeftHand;
            BindOffHand(offHand);
        }

        private void BindOffHand(EquipPosition gripHand)
        {
            if (_animator == null || !_animator.isHuman) return;

            bool left = gripHand != EquipPosition.RightHand; // 기본 LeftHand
            _offUpperArm = _animator.GetBoneTransform(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
            _offLowerArm = _animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
            _offHand     = _animator.GetBoneTransform(left ? HumanBodyBones.LeftHand     : HumanBodyBones.RightHand);
            // 주손 = 보조손의 반대 (무기를 쥔 손)
            _mainHand    = _animator.GetBoneTransform(left ? HumanBodyBones.RightHand    : HumanBodyBones.LeftHand);

            _bonesBound = _offUpperArm != null && _offLowerArm != null && _offHand != null && _mainHand != null;
            if (!_bonesBound)
                Debug.LogWarning("[WeaponIK] 팔 본 바인딩 실패 (휴머노이드 아님?).", this);
        }

        private void Update()
        {
            // 프로토타입 편의: 런타임에 _debugGripOverride를 인스펙터로 끌어다 넣으면 즉시 반영.
            // (PlayerEquipment.SetGrip 배선 전까지 발도된 무기를 빠르게 테스트하기 위함)
            if (_debugGripOverride != null && _grip != _debugGripOverride)
                SetGrip(_debugGripOverride, _debugGripOverride.defaultWeight);

            // 워치독: 그립을 줬는데 OnAnimatorIK가 안 불리면(ApplyAnimatorIK=false 등) 조용한 무동작 → 1회 경고.
            if (_hasGrip && !_warnedNoFire && _gripSetTime >= 0f && Time.time - _gripSetTime > 0.5f && !_ikEverProcessed)
            {
                Debug.LogWarning("[WeaponIK] 그립이 주입됐지만 OnAnimatorIK가 발화하지 않습니다. " +
                                 "Animancer Layer0 ApplyAnimatorIK가 켜졌는지(ActorAnimator), Animator IK Pass를 확인하세요.", this);
                _warnedNoFire = true;
            }
        }

        // FootIK와 동일하게 Animator GO에서 직접 호출되거나(Awake 부착) Relay가 전달.
        private void OnAnimatorIK(int layerIndex) => ProcessWeaponIK();

        internal void ProcessWeaponIK()
        {
            _ikEverProcessed = true;   // OnAnimatorIK 발화 확인 (워치독용) — 본 바인딩 여부와 무관
            if (!_bonesBound || _animator == null) return;

            float targetWeight = (_forceDisabled || !_hasGrip || _grip == null) ? 0f : _gripWeight * _maxWeight;
            _currentWeight = Mathf.MoveTowards(_currentWeight, targetWeight, _weightSpeed * Time.deltaTime);

            // ── 그립 오프셋 락 (정지 프레임 한정) ──
            // 그립 transform은 ParentConstraint 자식이라 OnAnimatorIK 시점엔 직전 프레임 좌표.
            // 주손이 프레임간 정지일 때만 (지연 grip == 현재 grip) 오프셋이 정확하므로 그 순간에만 락한다.
            // weight가 아직 0인 발도 직후 정지 프레임에 보통 락된다.
            if (_grip != null && _mainHand != null && !_offsetLocked)
                TryLockGripOffset();

            if (_currentWeight <= 0.001f || !_offsetLocked || _mainHand == null)
                return;

            // ── 현재 프레임 손 포즈에서 그립 월드 좌표 역산 (콘스트레인트 해석 불필요) ──
            Vector3 gripWorldPos = _mainHand.TransformPoint(_gripLocalPos);
            Quaternion gripWorldRot = _mainHand.rotation * _gripLocalRot;

            // ── 팔꿈치 hint: 현재 굽힘을 유지해 collinear 폴백(Vector3.up) 뒤집힘 방지 ──
            Transform hint = GetElbowHint();

            // ── 보조손 2본 IK ──
            TwoBoneIk.SolveTwoBoneIK(
                _offUpperArm, _offLowerArm, _offHand,
                (gripWorldPos, gripWorldRot),
                hint: hint,
                targetWeight: _currentWeight,
                hintWeight: _elbowHintWeight);

#if UNITY_EDITOR
            _dbgGripWorld = gripWorldPos;
            _dbgHandWorld = _offHand.position;
            _dbgDistance = Vector3.Distance(_dbgGripWorld, _dbgHandWorld);
#endif
        }

        private void TryLockGripOffset()
        {
            if (_grip == null) return;   // 파괴/해제 경합 방어 (Refresh와 그립 파괴 사이 프레임)

            Vector3 mhPos = _mainHand.position;
            if (_hasPrevMainHand)
            {
                _gripPendingTime += Time.deltaTime;
                float moved = Vector3.Distance(mhPos, _prevMainHandPos);
                bool stationary = moved <= StationaryEps;
                bool fallback = _gripPendingTime >= LockFallbackTime;
                if (stationary || fallback)
                {
                    _gripLocalPos = _mainHand.InverseTransformPoint(_grip.transform.position);
                    _gripLocalRot = Quaternion.Inverse(_mainHand.rotation) * _grip.transform.rotation;
                    _offsetLocked = true;
                    if (fallback && !stationary)
                        Debug.LogWarning("[WeaponIK] 정지 프레임을 못 잡아 그립 오프셋을 강제 락. " +
                                         "constraint 1프레임 지연만큼 정확도 저하 가능.", this);
                }
            }
            _prevMainHandPos = mhPos;
            _hasPrevMainHand = true;
        }

        private Transform GetElbowHint()
        {
            if (_elbowHint == null)
            {
                _elbowHint = new GameObject("WeaponIK_ElbowHint").transform;
                _elbowHint.SetParent(transform, false);
                _elbowHint.hideFlags = HideFlags.HideInHierarchy;
            }
            // 솔브 직전 현재 팔꿈치 위치를 pole로 → 기존 굽힘 평면 유지.
            _elbowHint.position = _offLowerArm.position;
            return _elbowHint;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_drawGizmos || !Application.isPlaying || _currentWeight <= 0.001f) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_dbgGripWorld, 0.03f);   // 그립 목표
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_dbgHandWorld, 0.025f);  // 실제 손
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_dbgGripWorld, _dbgHandWorld); // 오차(짧을수록 정확)
        }
#endif
    }

    /// <summary>
    /// Animator가 WeaponIKController와 다른 GameObject에 있을 때
    /// OnAnimatorIK 콜백을 WeaponIKController로 전달하는 릴레이. (FootIKRelay와 동일 모델)
    /// </summary>
    internal class WeaponIKRelay : MonoBehaviour
    {
        internal WeaponIKController Owner;

        private void OnAnimatorIK(int layerIndex)
        {
            if (Owner != null && Owner.enabled)
                Owner.ProcessWeaponIK();
        }
    }
}
