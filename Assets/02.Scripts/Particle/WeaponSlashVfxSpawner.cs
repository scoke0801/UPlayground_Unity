using UnityEngine;
using UnityEngine.Animations;

namespace FX
{
    public sealed class WeaponSlashVfxSpawner : MonoBehaviour
    {
        [Header("Blade")]
        [SerializeField] private Transform bladeBase;
        [SerializeField] private Transform bladeTip;

        [Header("VFX")]
        [SerializeField] private GameObject slashVfxPrefab;
        [SerializeField] private float scale = 1f;
        [SerializeField] private float destroyDelay = 2f;

        [Header("Offset")]
        [SerializeField] private Vector3 positionOffset;
        [SerializeField] private Vector3 rotationOffsetEuler;

        public Transform BladeBase => bladeBase;
        public Transform BladeTip => bladeTip;
        public GameObject SlashVfxPrefab => slashVfxPrefab;
        public float Scale => scale;
        public float DestroyDelay => destroyDelay;
        public Vector3 PositionOffset => positionOffset;
        public Vector3 RotationOffsetEuler => rotationOffsetEuler;

        /// <summary>
        /// SlashVFX 파티클의 기본 결정적 시드. 프리팹이 autoRandomSeed면 스폰 위치가 같아도
        /// 파티클 분포가 매 플레이 달라지므로, 모든 스폰 경로가 이 시드를 공유해 재현성을 고정한다.
        /// </summary>
        public const uint DefaultSlashParticleSeed = 0x51A5u;

        /// <summary>Begin/End 보간 시 원복할 임시 변경 전 월드 포즈.</summary>
        public struct BladePoseSnapshot
        {
            public bool valid;

            // 콘스트레인트 모드: 무기 루트(콘스트레인트 대상)를 원복.
            public bool constraintMode;
            public Transform constrainedRoot;
            public Vector3 rootPos;
            public Quaternion rootRot;

            // 폴백 모드: 블레이드 Base/Tip를 원복.
            public Vector3 basePos;
            public Quaternion baseRot;
            public Vector3 tipPos;
            public Quaternion tipRot;
        }

        // 서브프레임 보간 전역 토글. false면 보간 없이 현재 프레임 포즈로 스폰(원래 동작).
        public static bool EnableSubFrameInterpolation = true;

        // 콘스트레인트 대상의 월드 포즈를 소스(주손 본) 포즈 + 콘스트레인트 오프셋으로 즉석 계산해 배치한다.
        // 에디터 프리뷰(SyncParentConstraintsToSources)와 런타임이 이 동일한 공식을 공유해 결과가 일치한다.
        public static void PlaceConstrainedFromSourcePose(ParentConstraint pc, Vector3 srcPos, Quaternion srcRot)
        {
            if (pc == null || pc.sourceCount == 0) return;
            Vector3 tOff = pc.GetTranslationOffset(0);
            Vector3 rOff = pc.GetRotationOffset(0);
            pc.transform.SetPositionAndRotation(srcPos + srcRot * tOff, srcRot * Quaternion.Euler(rOff));
        }

        // 프레임 간 스냅샷 — 공간 이벤트(SlashVFX)의 서브프레임 보간용. prev=직전 프레임, cur=현재 프레임.
        int _curPoseFrame = -1;
        int _prevPoseFrame = -2;
        bool _hasPrevPose;

        // 콘스트레인트(무기=주손 부착) 참조. 블레이드는 한 프레임 지연된 콘스트레인트 결과이므로,
        // 라이브 블레이드 대신 "소스(주손 본) 포즈를 보간 → 동일 오프셋으로 무기 루트 배치"로 통일한다.
        bool _constraintResolved;
        int _constraintResolveAttempts;
        // 초기화 순서(블레이드 부착/소스 바인딩이 첫 프레임보다 늦는 경우)를 흡수할 소수 프레임만 재시도하고,
        // 그 뒤엔 폴백 모드로 영구 확정한다. 콘스트레인트 없는 무기는 폴백이 정상 동작이므로 매 프레임 탐색을 막는다.
        const int MaxConstraintResolveAttempts = 8;
        ParentConstraint _weaponConstraint;
        Transform _constraintSource;   // 주손 본 (콘스트레인트 소스, 애니메이션 직접 구동 = 지연 없음)
        Transform _constrainedRoot;    // 무기 루트 (콘스트레인트 대상; 블레이드의 부모)

        // 소스(주손 본) 월드 포즈 스냅샷 (콘스트레인트 모드)
        Vector3 _prevSrcPos, _curSrcPos;
        Quaternion _prevSrcRot, _curSrcRot;
        // 블레이드 월드 포즈 스냅샷 (폴백 모드: 콘스트레인트 없는 경우)
        Vector3 _prevBasePos, _curBasePos, _prevTipPos, _curTipPos;
        Quaternion _prevBaseRot, _curBaseRot, _prevTipRot, _curTipRot;

        void LateUpdate()
        {
            // 호출 순서와 무관하게 prev/cur가 항상 직전/현재 프레임 포즈가 되도록 매 프레임 굴린다.
            RollPoseSnapshot();
        }

        void ResolveConstraintOnce()
        {
            if (_constraintResolved) return;

            if (bladeBase != null)
            {
                ParentConstraint pc = bladeBase.GetComponentInParent<ParentConstraint>();
                if (pc != null && pc.sourceCount > 0)
                {
                    ConstraintSource src = pc.GetSource(0);
                    if (src.sourceTransform != null)
                    {
                        _weaponConstraint = pc;
                        _constraintSource = src.sourceTransform;
                        _constrainedRoot = pc.transform;
                        _constraintResolved = true;
                        // 모드 확정 직후 prev/cur 스냅샷을 재시드한다. 폴백 모드로 몇 프레임 굴린 뒤
                        // 콘스트레인트 모드로 전환되면 _prevSrc*가 비어 있어(0) 첫 보간이 원점에서 슬러프되는
                        // garbage 스폰이 나므로, _hasPrevPose를 내려 다음 RollPoseSnapshot이 prev=cur로 시드하게 한다.
                        _hasPrevPose = false;
                        return;
                    }
                }
            }

            // 성공할 때만 래치한다. 소수 프레임 재시도 후에도 못 찾으면 폴백 모드로 영구 확정.
            if (++_constraintResolveAttempts >= MaxConstraintResolveAttempts)
                _constraintResolved = true;
        }

        bool UseConstraintMode => _weaponConstraint != null && _constraintSource != null && _constrainedRoot != null;

        // 매 프레임 1회만 굴린다(Time.frameCount 가드 → 호출 순서·중복에 안전).
        void RollPoseSnapshot()
        {
            ResolveConstraintOnce();

            int frame = Time.frameCount;
            if (_curPoseFrame == frame) return;

            _prevPoseFrame = _curPoseFrame;

            if (UseConstraintMode)
            {
                _prevSrcPos = _curSrcPos; _prevSrcRot = _curSrcRot;
                _curSrcPos = _constraintSource.position; _curSrcRot = _constraintSource.rotation;
                if (!_hasPrevPose)
                {
                    _prevSrcPos = _curSrcPos; _prevSrcRot = _curSrcRot;
                }
            }
            else
            {
                if (bladeBase == null || bladeTip == null) return;
                _prevBasePos = _curBasePos; _prevBaseRot = _curBaseRot;
                _prevTipPos  = _curTipPos;  _prevTipRot  = _curTipRot;
                _curBasePos = bladeBase.position; _curBaseRot = bladeBase.rotation;
                _curTipPos  = bladeTip.position;  _curTipRot  = bladeTip.rotation;
                if (!_hasPrevPose)
                {
                    _prevBasePos = _curBasePos; _prevBaseRot = _curBaseRot;
                    _prevTipPos  = _curTipPos;  _prevTipRot  = _curTipRot;
                }
            }

            _hasPrevPose = true;
            _curPoseFrame = frame;
        }

        /// <summary>
        /// 블레이드를 발화 시각(eventStart)의 포즈로 임시 이동시킨다. 스폰 직후 반드시 <see cref="EndInterpolatedBladePose"/>로 원복.
        /// 콘스트레인트 모드: 소스(주손 본)를 직전↔현재 프레임 사이 <paramref name="t"/>로 보간해 eventStart 손 포즈를 구하고,
        /// 에디터 프리뷰와 동일한 오프셋 공식으로 무기 루트를 배치 → 블레이드(자식)가 따라온다(지연 제거, 양 경로 일치).
        /// </summary>
        public bool BeginInterpolatedBladePose(float t, out BladePoseSnapshot saved)
        {
            saved = default;
            if (!EnableSubFrameInterpolation) return false;
            if (bladeBase == null || bladeTip == null) return false;

            RollPoseSnapshot();

            // 안전 가드: prev 스냅샷이 "직전 프레임"이 아니면(무기 비활성/스왑 등으로 roll 누락) 보간하지 않는다.
            bool prevIsAdjacent = _hasPrevPose && (_curPoseFrame - _prevPoseFrame == 1);
            if (!prevIsAdjacent)
                return false;

            if (UseConstraintMode)
            {
                Vector3 srcPos = Vector3.Lerp(_prevSrcPos, _curSrcPos, t);
                Quaternion srcRot = Quaternion.Slerp(_prevSrcRot, _curSrcRot, t);

                saved.constraintMode = true;
                saved.constrainedRoot = _constrainedRoot;
                saved.rootPos = _constrainedRoot.position;
                saved.rootRot = _constrainedRoot.rotation;
                saved.valid = true;

                PlaceConstrainedFromSourcePose(_weaponConstraint, srcPos, srcRot);
                return true;
            }

            // 폴백(콘스트레인트 없음): 블레이드 월드 포즈를 직접 보간.
            saved.basePos = bladeBase.position; saved.baseRot = bladeBase.rotation;
            saved.tipPos  = bladeTip.position;  saved.tipRot  = bladeTip.rotation;
            saved.valid = true;

            bladeBase.SetPositionAndRotation(Vector3.Lerp(_prevBasePos, _curBasePos, t), Quaternion.Slerp(_prevBaseRot, _curBaseRot, t));
            bladeTip.SetPositionAndRotation(Vector3.Lerp(_prevTipPos, _curTipPos, t), Quaternion.Slerp(_prevTipRot, _curTipRot, t));
            return true;
        }

        /// <summary><see cref="BeginInterpolatedBladePose"/>가 임시 변경한 트랜스폼을 원복한다.</summary>
        public void EndInterpolatedBladePose(in BladePoseSnapshot saved)
        {
            if (!saved.valid) return;

            if (saved.constraintMode)
            {
                if (saved.constrainedRoot != null)
                    saved.constrainedRoot.SetPositionAndRotation(saved.rootPos, saved.rootRot);
                return;
            }

            if (bladeBase != null) bladeBase.SetPositionAndRotation(saved.basePos, saved.baseRot);
            if (bladeTip != null) bladeTip.SetPositionAndRotation(saved.tipPos, saved.tipRot);
        }

        public void SpawnSlash()
        {
            if (!TryGetSpawnPose(out Vector3 spawnPosition, out Quaternion rotation))
                return;

            GameObject instance = Instantiate(slashVfxPrefab, spawnPosition, rotation);
            ApplyDeterministicParticleSeed(instance, DefaultSlashParticleSeed);
            instance.transform.localScale *= scale;

            if (destroyDelay > 0f)
                Destroy(instance, destroyDelay);
        }

        public bool TryGetSpawnPose(out Vector3 spawnPosition, out Quaternion rotation)
        {
            return TryGetSpawnPose(slashVfxPrefab, positionOffset, rotationOffsetEuler, out spawnPosition, out rotation);
        }

        public bool TryGetSpawnPose(GameObject prefab, Vector3 localPositionOffset, Vector3 localRotationOffsetEuler, out Vector3 spawnPosition, out Quaternion rotation)
        {
            return TryGetSpawnPose(prefab, localPositionOffset, localRotationOffsetEuler, false, out spawnPosition, out rotation);
        }

        public bool TryGetSpawnPose(GameObject prefab, Vector3 positionOffsetValue, Vector3 rotationOffsetEulerValue, bool useWorldRotation, out Vector3 spawnPosition, out Quaternion rotation)
        {
            return TryGetSpawnPose(prefab, positionOffsetValue, false, rotationOffsetEulerValue, useWorldRotation, out spawnPosition, out rotation);
        }

        public bool TryGetSpawnPose(GameObject prefab, Vector3 positionOffsetValue, bool useWorldPosition, Vector3 rotationOffsetEulerValue, bool useWorldRotation, out Vector3 spawnPosition, out Quaternion rotation)
        {
            spawnPosition = default;
            rotation = default;

            if (bladeBase == null || bladeTip == null || prefab == null)
            {
                Debug.LogWarning($"{nameof(WeaponSlashVfxSpawner)}: Missing reference.", this);
                return false;
            }

            // 인스턴스 경로는 액터 컨텍스트가 없으므로 기준 회전을 identity로 두어 기존 절대 월드 동작을 유지한다.
            if (!TryGetSpawnPose(bladeBase, bladeTip, transform, positionOffsetValue, useWorldPosition, rotationOffsetEulerValue, useWorldRotation, Quaternion.identity, out spawnPosition, out rotation))
            {
                Debug.LogWarning($"{nameof(WeaponSlashVfxSpawner)}: Invalid blade direction.", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Blade Base/Tip 자세로부터 Slash VFX의 생성 위치/회전을 계산하는 공용 로직.
        /// Spawner 인스턴스와 SlashVFXEvent의 폴백 경로가 동일한 수식을 공유하기 위한 단일 소스다.
        /// </summary>
        /// <param name="upFallback">bladeBase.up이 칼날 방향과 평행할 때 사용할 보조 up 기준(없으면 World up).</param>
        /// <param name="referenceRotation">
        /// useWorldRotation 모드에서 회전의 기준이 되는 회전. 액터(캐릭터) 루트 회전을 넘기면
        /// 칼날 방향과 무관하게 캐릭터가 바라보는 방향을 따라간다. identity를 넘기면 절대 월드 기준이 된다.
        /// World 모드는 원래 칼날 방향을 무시(고정 오일러)하므로, 이 값을 액터 회전으로 주면 기존 World 튜닝값을
        /// 그대로 보존한 채(캐릭터 정면=identity일 때 동일) 캐릭터 회전만 추가로 반영된다.
        /// </param>
        public static bool TryGetSpawnPose(Transform bladeBase, Transform bladeTip, Transform upFallback, Vector3 positionOffsetValue, Vector3 rotationOffsetEulerValue, bool useWorldRotation, Quaternion referenceRotation, out Vector3 spawnPosition, out Quaternion rotation)
        {
            return TryGetSpawnPose(bladeBase, bladeTip, upFallback, positionOffsetValue, false, rotationOffsetEulerValue, useWorldRotation, referenceRotation, out spawnPosition, out rotation);
        }

        public static bool TryGetSpawnPose(Transform bladeBase, Transform bladeTip, Transform upFallback, Vector3 positionOffsetValue, bool useWorldPosition, Vector3 rotationOffsetEulerValue, bool useWorldRotation, Quaternion referenceRotation, out Vector3 spawnPosition, out Quaternion rotation)
        {
            spawnPosition = default;
            rotation = default;

            if (bladeBase == null || bladeTip == null)
                return false;

            Vector3 bladeDirection = bladeTip.position - bladeBase.position;

            if (bladeDirection.sqrMagnitude < 0.0001f)
                return false;

            bladeDirection.Normalize();

            Vector3 upDirection = Vector3.ProjectOnPlane(bladeBase.up, bladeDirection);

            if (upDirection.sqrMagnitude < 0.0001f && upFallback != null)
                upDirection = Vector3.ProjectOnPlane(upFallback.up, bladeDirection);

            if (upDirection.sqrMagnitude < 0.0001f)
                upDirection = Vector3.up;

            upDirection.Normalize();

            Vector3 center = Vector3.Lerp(bladeBase.position, bladeTip.position, 0.5f);

            Quaternion bladeRotation = Quaternion.LookRotation(bladeDirection, upDirection);
            // World 모드는 칼날 방향 대신 referenceRotation(액터 루트 회전 등)을 기준으로 삼는다.
            // identity면 절대 월드, 액터 회전이면 캐릭터가 바라보는 방향을 따른다.
            rotation = useWorldRotation
                ? referenceRotation * Quaternion.Euler(rotationOffsetEulerValue)
                : bladeRotation * Quaternion.Euler(rotationOffsetEulerValue);

            // 위치 World 모드는 액터 루트 회전 기준, Blade 모드는 칼날 기준으로 적용한다.
            // referenceRotation은 월드/액터 기준 튜닝값이 캐릭터 방향을 따라가도록 하는 기준이다.
            Vector3 worldOffset = useWorldPosition
                ? referenceRotation * positionOffsetValue
                : bladeRotation * positionOffsetValue;
            spawnPosition = center + worldOffset;
            return true;
        }

        /// <summary>
        /// 스폰된 VFX 인스턴스의 모든 ParticleSystem 랜덤 시드를 결정적으로 고정한다.
        /// 프리팹이 autoRandomSeed(=재생마다 새 시드)이면 스폰 트랜스폼이 동일해도 파티클 분포가
        /// 매 플레이 달라져 "위치가 어긋나 보이는" 변동이 생긴다. 시드를 고정하면 매번 동일하게 재현된다.
        /// 시스템마다 index만큼 어긋난 시드를 줘서, 한 프리팹 내 다중 시스템은 서로 다르되 플레이 간에는 결정적이다.
        /// </summary>
        public static void ApplyDeterministicParticleSeed(GameObject instance, uint seed)
        {
            if (instance == null)
                return;

            ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (ps == null)
                    continue;

                bool shouldPlay = ps.isPlaying || ps.main.playOnAwake;

                // randomSeed는 정지 상태에서만 설정 가능하므로 정지→시드→재생 순으로 처리한다.
                ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.useAutoRandomSeed = false;
                ps.randomSeed = seed + (uint)i;

                if (shouldPlay)
                    ps.Play(false);
            }
        }

        public void SetBladePoints(Transform newBladeBase, Transform newBladeTip)
        {
            bladeBase = newBladeBase;
            bladeTip = newBladeTip;

            // 블레이드가 바뀌면 해석된 콘스트레인트 참조가 stale일 수 있으므로 재해석을 강제한다.
            _constraintResolved = false;
            _constraintResolveAttempts = 0;
            _weaponConstraint = null;
            _constraintSource = null;
            _constrainedRoot = null;
            _hasPrevPose = false;
        }

        public void ApplySettings(GameObject prefab, float newScale, float newDestroyDelay, Vector3 newPositionOffset, Vector3 newRotationOffsetEuler)
        {
            slashVfxPrefab = prefab;
            scale = newScale;
            destroyDelay = newDestroyDelay;
            positionOffset = newPositionOffset;
            rotationOffsetEuler = newRotationOffsetEuler;
        }
    }
}
