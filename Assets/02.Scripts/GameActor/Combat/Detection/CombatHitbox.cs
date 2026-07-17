using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Debugging;

namespace UPlayGround.Combat
{
    /// <summary>
    /// 무기 또는 신체 본에 부착하는 공격 판정 형상.
    /// Collider는 물리 이벤트가 아닌 명시적 Overlap 질의의 저작 데이터로만 사용한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatHitbox : MonoBehaviour
    {
        public const string DefaultGroupId = "Default";

        [SerializeField] private string _groupId = DefaultGroupId;
        [SerializeField] private Collider _shapeCollider;
        [SerializeField] private bool _useSweep = true;
        [SerializeField, Min(0.01f)] private float _sweepStepDistance = 0.15f;
        [SerializeField, Range(1, 32)] private int _maxSweepSteps = 8;
        [SerializeField] private Color _debugColor = new(1f, 0.25f, 0.1f, 0.45f);

        [Header("Swing Trail (Debug)")]
        [Tooltip("공격 활성 윈도우 동안의 HitBox 궤적을 N초간 잔상으로 표시한다.")]
        [SerializeField] private bool _drawSwingTrail = true;
        [Tooltip("궤적 잔상이 유지되는 시간(초). 0이면 비활성.")]
        [SerializeField, Min(0f)] private float _swingTrailDuration = 1f;
        [SerializeField] private Color _swingTrailColor = new(0.2f, 0.9f, 1f, 0.8f);
        [Tooltip("스윙 트레일 대신 현재 HitBox 형상만 상시 표시한다(누적 없음). 세그먼트가 많은 채찍 등에서 렉 없이 보이게 한다.")]
        [SerializeField] private bool _drawStaticShape;
        [Tooltip("체인(채찍) 트레일 리더. 설정 시 스윙 트레일을 세그먼트별이 아닌 'this(첫 노드)→끝 노드' 직선 하나로 기록한다. 세그먼트 수와 무관하게 트레일 비용이 일정.")]
        [SerializeField] private Transform _chainTrailEndpoint;
        [SerializeField, Min(0.005f)] private float _chainTrailRadius = 0.03f;

        // 스윙 트레일 누적 상한과 공간 데시메이션 간격.
        // 채찍처럼 HitBox가 많을 때 트레일 샘플 폭증으로 인한 기즈모 드로콜 렉을 막는다.
        private const int MaxTrailSamples = 96;
        private const float TrailMinSpacing = 0.015f;
        private const float TrailMinSpacingSqr = TrailMinSpacing * TrailMinSpacing;

        private CombatHitboxShape _previousShape;
        private bool _hasPreviousShape;

        // 활성 윈도우 동안 누적되는 궤적 샘플. 가장 오래된 것이 앞쪽(인덱스 0)에 위치한다.
        private readonly List<TrailSample> _swingTrail = new(MaxTrailSamples);

        private readonly struct TrailSample
        {
            public readonly CombatHitboxShape Shape;
            public readonly float Time;

            public TrailSample(in CombatHitboxShape shape, float time)
            {
                Shape = shape;
                Time = time;
            }
        }

        public string GroupId => string.IsNullOrWhiteSpace(_groupId) ? DefaultGroupId : _groupId.Trim();
        public Collider ShapeCollider => _shapeCollider;
        public bool UseSweep => _useSweep;
        public float SweepStepDistance => Mathf.Max(0.01f, _sweepStepDistance);
        public int MaxSweepSteps => Mathf.Clamp(_maxSweepSteps, 1, 32);
        public bool IsSupported => _shapeCollider is BoxCollider or CapsuleCollider;
        public bool HasPreviousShape => _hasPreviousShape;
        public CombatHitboxShape PreviousShape => _previousShape;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 개발 빌드 전용 런타임 히트박스 렌더러(HitboxRuntimeDebugRenderer)가 순회하는 판정 활성 레지스트리.
        // 릴리스 빌드에서는 아래 등록/해제 코드가 전부 스트립되어 0비용이 된다.
        private static readonly HashSet<CombatHitbox> s_active = new();
        private readonly List<CombatHitboxShape> _lastDetectionSamples = new(32);

        public static IReadOnlyCollection<CombatHitbox> Active => s_active;
        public IReadOnlyList<CombatHitboxShape> LastDetectionSamples => _lastDetectionSamples;

        // 현재 판정 윈도우가 진행 중인지. 렌더러는 이 값이 참일 때만 현재 형상을 실시간으로 그린다.
        public bool IsSampling { get; private set; }

        // 스윙 트레일(잔상)을 런타임 렌더러가 에디터 기즈모와 동일하게 그릴 수 있도록 노출한다.
        public bool WantsSwingTrail => _drawSwingTrail && _swingTrailDuration > 0f && _swingTrail.Count > 0;
        public bool IsChainTrail => _chainTrailEndpoint != null;
        public Color SwingTrailColor => _swingTrailColor;
        public float SwingTrailDuration => _swingTrailDuration;
        public int SwingTrailSampleCount => _swingTrail.Count;

        public void GetSwingTrailSample(int index, out CombatHitboxShape shape, out float time)
        {
            TrailSample sample = _swingTrail[index];
            shape = sample.Shape;
            time = sample.Time;
        }

        // 렌더러가 매 프레임 호출해 만료된 트레일 샘플을 제거한다. 남은 샘플이 있으면 true.
        public bool PruneSwingTrailForDebug()
        {
            if (_swingTrailDuration > 0f)
                PruneTrail(Time.time);
            return _swingTrail.Count > 0;
        }

        // 판정이 끝났고(!IsSampling) 잔상 트레일도 만료됐으면 레지스트리에서 스스로 제거한다. 제거 시 true.
        // 렌더러가 프레임 끝에서 호출하며, 그동안은 잔상을 계속 그릴 수 있도록 s_active에 남는다.
        public bool TryReleaseInactiveDebug()
        {
            if (IsSampling)
                return false;
            if (_drawSwingTrail && _swingTrailDuration > 0f)
            {
                PruneTrail(Time.time);
                if (_swingTrail.Count > 0)
                    return false;
            }
            s_active.Remove(this);
            return true;
        }

        public void BeginDebugDetectionSamples()
        {
            _lastDetectionSamples.Clear();
        }

        public void AddDebugDetectionSample(in CombatHitboxShape shape)
        {
            _lastDetectionSamples.Add(shape);
        }

        private void OnDisable()
        {
            s_active.Remove(this);
            _lastDetectionSamples.Clear();
            IsSampling = false;
        }
#endif

        private void Reset()
        {
            _shapeCollider = GetComponent<Collider>();
            NormalizeCollider();
        }

        private void OnValidate()
        {
            if (_shapeCollider == null)
                _shapeCollider = GetComponent<Collider>();
            _groupId = string.IsNullOrWhiteSpace(_groupId) ? DefaultGroupId : _groupId.Trim();
            _sweepStepDistance = Mathf.Max(0.01f, _sweepStepDistance);
            _maxSweepSteps = Mathf.Clamp(_maxSweepSteps, 1, 32);
            _swingTrailDuration = Mathf.Max(0f, _swingTrailDuration);
            _chainTrailRadius = Mathf.Max(0.005f, _chainTrailRadius);
            NormalizeCollider();
        }

        public void Configure(
            string groupId,
            Collider shapeCollider,
            bool useSweep,
            float sweepStepDistance,
            int maxSweepSteps)
        {
            _groupId = string.IsNullOrWhiteSpace(groupId) ? DefaultGroupId : groupId.Trim();
            _shapeCollider = shapeCollider;
            _useSweep = useSweep;
            _sweepStepDistance = Mathf.Max(0.01f, sweepStepDistance);
            _maxSweepSteps = Mathf.Clamp(maxSweepSteps, 1, 32);
            NormalizeCollider();
        }

        public void BeginSampling()
        {
            _hasPreviousShape = TryGetWorldShape(out _previousShape);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            s_active.Add(this);
            IsSampling = true;
#endif
            if (_hasPreviousShape)
                RecordTrail(_previousShape);
        }

        public void CommitShape(in CombatHitboxShape shape)
        {
            _previousShape = shape;
            _hasPreviousShape = true;
            RecordTrail(shape);
        }

        public void ClearSampling()
        {
            _hasPreviousShape = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            IsSampling = false;
            _lastDetectionSamples.Clear();
            // 스윙 트레일이 남아 있으면 렌더러가 잔상을 계속 그리도록 레지스트리에 유지한다(만료 시 렌더러가 정리).
            // 트레일이 없으면 즉시 제거해 종전과 동일하게 동작한다.
            if (!WantsSwingTrail)
                s_active.Remove(this);
#endif
        }

        private void RecordTrail(in CombatHitboxShape shape)
        {
            if (!_drawSwingTrail || _swingTrailDuration <= 0f)
                return;

            // 체인 리더면 개별 세그먼트 형상 대신 '첫 노드→끝 노드' 직선 하나만 기록한다.
            // 세그먼트가 11개여도 트레일은 1줄이라 드로콜이 세그먼트 수에 비례하지 않는다.
            CombatHitboxShape recordShape = shape;
            if (_chainTrailEndpoint != null && TryGetChainChordShape(out CombatHitboxShape chord))
                recordShape = chord;

            // 공간 데시메이션: 직전 샘플과 거의 같은 위치면 누적하지 않는다.
            // 느린 구간/제자리 샘플의 폭증을 막아 세그먼트가 많아도 드로콜이 선형으로 늘지 않게 한다.
            if (_swingTrail.Count > 0 &&
                (_swingTrail[_swingTrail.Count - 1].Shape.Center - recordShape.Center).sqrMagnitude < TrailMinSpacingSqr)
                return;

            float now = Time.time;
            _swingTrail.Add(new TrailSample(recordShape, now));
            // 하드 캡: 고프레임에서 빠른 스윙이어도 샘플 수를 상한으로 묶는다(오래된 것부터 제거).
            if (_swingTrail.Count > MaxTrailSamples)
                _swingTrail.RemoveRange(0, _swingTrail.Count - MaxTrailSamples);
            PruneTrail(now);
        }

        /// <summary>
        /// 세그먼트가 많은 무기(채찍 등)에서 per-세그먼트 스윙 트레일을 꺼 기즈모 드로콜 폭증을 줄인다.
        /// 에디터 생성 도구가 체인 HitBox를 만들 때 호출한다.
        /// </summary>
        public void SetSwingTrailEnabled(bool enabled)
        {
            _drawSwingTrail = enabled;
            if (!enabled)
                _swingTrail.Clear();
        }

        /// <summary>
        /// 누적 없는 상시 형상 표시를 켠다. 체인(채찍)처럼 트레일을 끈 HitBox가 선택 없이도 보이게 한다.
        /// </summary>
        public void SetStaticShapeEnabled(bool enabled)
        {
            _drawStaticShape = enabled;
        }

        /// <summary>
        /// 이 HitBox를 체인(채찍) 트레일 리더로 지정한다. 스윙 트레일을 세그먼트마다가 아니라
        /// 'this(첫 노드)→endpoint(끝 노드)' 직선 하나로만 기록해, 세그먼트 수와 무관하게 비용을 일정하게 유지한다.
        /// </summary>
        public void SetChainTrail(Transform endpoint, float radius)
        {
            _chainTrailEndpoint = endpoint;
            _chainTrailRadius = Mathf.Max(0.005f, radius);
            _drawSwingTrail = endpoint != null;
            if (endpoint == null)
                _swingTrail.Clear();
        }

        // 체인 첫 노드(this)→끝 노드(_chainTrailEndpoint)를 잇는 가는 캡슐(직선) 형상.
        private bool TryGetChainChordShape(out CombatHitboxShape shape)
        {
            if (_chainTrailEndpoint == null)
            {
                shape = default;
                return false;
            }

            Vector3 a = transform.position;
            Vector3 b = _chainTrailEndpoint.position;
            if ((b - a).sqrMagnitude <= 1e-6f)
            {
                shape = default;
                return false;
            }

            shape = CombatHitboxShape.Capsule((a + b) * 0.5f, a, b, Mathf.Max(0.005f, _chainTrailRadius));
            return true;
        }

        // 수명이 다한 궤적 샘플을 앞쪽부터 제거한다. 샘플은 시간순으로 누적되므로 앞에서부터 끊으면 된다.
        private void PruneTrail(float now)
        {
            float cutoff = now - _swingTrailDuration;
            int removeCount = 0;
            while (removeCount < _swingTrail.Count && _swingTrail[removeCount].Time < cutoff)
                removeCount++;
            if (removeCount > 0)
                _swingTrail.RemoveRange(0, removeCount);
        }

        public bool TryGetWorldShape(out CombatHitboxShape shape)
        {
            if (_shapeCollider is BoxCollider box)
            {
                Transform colliderTransform = box.transform;
                Vector3 scale = Abs(colliderTransform.lossyScale);
                Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, scale);
                shape = CombatHitboxShape.Box(
                    colliderTransform.TransformPoint(box.center),
                    colliderTransform.rotation,
                    halfExtents);
                return halfExtents.x > 0f && halfExtents.y > 0f && halfExtents.z > 0f;
            }

            if (_shapeCollider is CapsuleCollider capsule)
            {
                Transform colliderTransform = capsule.transform;
                Vector3 scale = Abs(colliderTransform.lossyScale);
                int direction = Mathf.Clamp(capsule.direction, 0, 2);
                float axisScale = direction == 0 ? scale.x : direction == 1 ? scale.y : scale.z;
                float radialScale = direction == 0
                    ? Mathf.Max(scale.y, scale.z)
                    : direction == 1
                        ? Mathf.Max(scale.x, scale.z)
                        : Mathf.Max(scale.x, scale.y);

                float radius = capsule.radius * radialScale;
                float height = Mathf.Max(capsule.height * axisScale, radius * 2f);
                float halfSegment = Mathf.Max(0f, height * 0.5f - radius);
                Vector3 localAxis = direction == 0 ? Vector3.right : direction == 1 ? Vector3.up : Vector3.forward;
                Vector3 worldAxis = colliderTransform.TransformDirection(localAxis).normalized;
                Vector3 center = colliderTransform.TransformPoint(capsule.center);

                shape = CombatHitboxShape.Capsule(
                    center,
                    center - worldAxis * halfSegment,
                    center + worldAxis * halfSegment,
                    radius);
                return radius > 0f;
            }

            shape = default;
            return false;
        }

        private void NormalizeCollider()
        {
            if (_shapeCollider == null)
                return;

            _shapeCollider.isTrigger = true;
            _shapeCollider.enabled = false;
        }

        private static Vector3 Abs(Vector3 value)
            => new(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));

        // 선택된 HitBox의 형상. 채찍처럼 세그먼트가 많으면 부모를 선택하는 것만으로 자식 HitBox 전부의
        // OnDrawGizmosSelected가 호출돼 무거워진다. (1) 다른 전투 기즈모와 동일하게 토글로 끌 수 있게 하고,
        // (2) 캡슐은 와이어 스피어 대신 선 윤곽으로 그려 선택 중에도 가볍게 유지한다.
        private void OnDrawGizmosSelected()
        {
            if (!DebugGizmoBridge.IsLocalContentEnabled(
                    DebugGizmoCategory.Combat,
                    DebugGizmoContentType.HitboxSwingTrail))
                return;
            if (!TryGetWorldShape(out CombatHitboxShape shape))
                return;

            if (shape.Type == CombatHitboxShapeType.Capsule)
                DrawCapsuleLineOutline(shape, _debugColor);
            else
                DrawShapeWire(shape, _debugColor);
        }

        // 선택 여부와 무관하게 보여야 하는 디버그 표시(스윙 트레일/상시 형상)를 OnDrawGizmosSelected 가 아닌
        // 여기서 그린다. 둘 다 Combat/HitboxSwingTrail 토글로 끌 수 있다.
        private void OnDrawGizmos()
        {
            bool wantTrail = _drawSwingTrail && _swingTrailDuration > 0f && _swingTrail.Count > 0;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            bool wantDetectionSamples = _lastDetectionSamples.Count > 0;
            if (!wantDetectionSamples && !wantTrail && !_drawStaticShape)
                return;
#else
            if (!wantTrail && !_drawStaticShape)
                return;
#endif

            if (!DebugGizmoBridge.IsLocalContentEnabled(
                    DebugGizmoCategory.Combat,
                    DebugGizmoContentType.HitboxSwingTrail))
                return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 실제 충돌 판정은 이전 프레임 형상과 현재 형상 사이를 sweep 샘플로 보간해 Overlap한다.
            // 에디터 기즈모도 현재 무기 콜라이더가 아니라 방금 판정에 사용한 샘플들을 우선 표시한다.
            if (wantDetectionSamples)
            {
                CombatHitboxShape? previous = null;
                Gizmos.color = _swingTrailColor;
                for (int i = 0; i < _lastDetectionSamples.Count; i++)
                {
                    CombatHitboxShape sample = _lastDetectionSamples[i];
                    DrawShapeWire(sample, _swingTrailColor);

                    if (previous.HasValue)
                    {
                        Gizmos.color = _swingTrailColor;
                        Gizmos.DrawLine(previous.Value.Center, sample.Center);
                    }
                    previous = sample;
                }
                return;
            }
#endif

            // 상시 형상: 누적 없이 현재 형상만 1회 그린다. 매 프레임 그려지므로 비용이 중요하다.
            // 캡슐은 와이어 스피어(호출당 메시 생성, 무거움) 대신 선 윤곽으로만 그린다 → 세그먼트가 많은
            // 채찍이 idle로 뭉쳐 있어도 가볍다. 정밀 형상은 해당 HitBox를 선택하면(OnDrawGizmosSelected) 보인다.
            if (_drawStaticShape && TryGetWorldShape(out CombatHitboxShape staticShape))
            {
                if (staticShape.Type == CombatHitboxShapeType.Capsule)
                    DrawCapsuleLineOutline(staticShape, _debugColor);
                else
                    DrawShapeWire(staticShape, _debugColor);
            }

            if (!wantTrail)
                return;

            float now = Time.time;
            PruneTrail(now);

            // 체인 리더는 샘플마다 '첫 노드→끝 노드' 직선 하나만 기록한다. 이 경우 캡슐 와이어(스피어 2개씩)를
            // 그리면 드로콜이 폭증해 렉이 생기므로, 가벼운 선(스윙 부채꼴 + 말단 궤적)만 그린다.
            bool chainTrail = _chainTrailEndpoint != null;

            CombatHitboxShape? prev = null;
            for (int i = 0; i < _swingTrail.Count; i++)
            {
                TrailSample sample = _swingTrail[i];
                float life = Mathf.Clamp01(1f - (now - sample.Time) / _swingTrailDuration);
                if (life <= 0f)
                    continue;

                Color color = _swingTrailColor;
                color.a *= life;
                Gizmos.color = color;

                if (chainTrail)
                {
                    // 직선(첫 노드→끝 노드) 자체를 그려 휘두름의 부채꼴을 보여준다.
                    Gizmos.DrawLine(sample.Shape.Point0, sample.Shape.Point1);
                    // 말단(Point1) 궤적을 이어 스윙 호를 강조한다.
                    if (prev.HasValue)
                        Gizmos.DrawLine(prev.Value.Point1, sample.Shape.Point1);
                }
                else
                {
                    DrawShapeWire(sample.Shape, color);
                    // 연속 샘플의 중심을 이어 스윙 경로를 강조한다.
                    if (prev.HasValue)
                        Gizmos.DrawLine(prev.Value.Center, sample.Shape.Center);
                }
                prev = sample.Shape;
            }
        }

        // 와이어 스피어 없이 선만으로 캡슐 윤곽(축 + 양옆 레일 + 양끝 캡)을 그린다.
        // DrawWireSphere가 호출당 메시를 생성해 무거운 것과 달리, 세그먼트당 5개 선이라 매우 가볍다.
        private void DrawCapsuleLineOutline(in CombatHitboxShape shape, Color color)
        {
            Gizmos.color = color;
            Vector3 axis = shape.Point1 - shape.Point0;
            Vector3 perp = axis.sqrMagnitude > 0.0001f
                ? Vector3.Cross(axis, Vector3.up)
                : transform.right;
            if (perp.sqrMagnitude < 0.0001f)
                perp = transform.right;
            perp = perp.normalized * shape.Radius;

            Gizmos.DrawLine(shape.Point0, shape.Point1);
            Gizmos.DrawLine(shape.Point0 + perp, shape.Point1 + perp);
            Gizmos.DrawLine(shape.Point0 - perp, shape.Point1 - perp);
            Gizmos.DrawLine(shape.Point0 + perp, shape.Point0 - perp);
            Gizmos.DrawLine(shape.Point1 + perp, shape.Point1 - perp);
        }

        private void DrawShapeWire(in CombatHitboxShape shape, Color color)
        {
            Gizmos.color = color;
            if (shape.Type == CombatHitboxShapeType.Box)
            {
                Matrix4x4 previous = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(shape.Center, shape.Rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, shape.HalfExtents * 2f);
                Gizmos.matrix = previous;
                return;
            }

            Vector3 radial = shape.Point1 - shape.Point0;
            radial = radial.sqrMagnitude > 0.0001f
                ? Vector3.Cross(radial, Vector3.up).normalized
                : transform.right;
            Gizmos.DrawWireSphere(shape.Point0, shape.Radius);
            Gizmos.DrawWireSphere(shape.Point1, shape.Radius);
            Gizmos.DrawLine(shape.Point0 + radial * shape.Radius, shape.Point1 + radial * shape.Radius);
            Gizmos.DrawLine(shape.Point0 - radial * shape.Radius, shape.Point1 - radial * shape.Radius);
        }
    }
}
