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

        private CombatHitboxShape _previousShape;
        private bool _hasPreviousShape;

        // 활성 윈도우 동안 누적되는 궤적 샘플. 가장 오래된 것이 앞쪽(인덱스 0)에 위치한다.
        private readonly List<TrailSample> _swingTrail = new(64);

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
        }

        private void RecordTrail(in CombatHitboxShape shape)
        {
            if (!_drawSwingTrail || _swingTrailDuration <= 0f)
                return;

            float now = Time.time;
            _swingTrail.Add(new TrailSample(shape, now));
            PruneTrail(now);
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

        private void OnDrawGizmosSelected()
        {
            if (!TryGetWorldShape(out CombatHitboxShape shape))
                return;

            DrawShapeWire(shape, _debugColor);
        }

        // 활성 윈도우 동안 누적된 궤적을 N초간 잔상으로 표시한다.
        // 선택 여부와 무관하게 보여야 하므로 OnDrawGizmosSelected 가 아닌 OnDrawGizmos 에서 그린다.
        private void OnDrawGizmos()
        {
            if (!_drawSwingTrail || _swingTrailDuration <= 0f || _swingTrail.Count == 0)
                return;

            if (!DebugGizmoManager.IsLocalContentEnabled(
                    DebugGizmoCategory.Combat,
                    DebugGizmoContentType.HitboxSwingTrail))
                return;

            float now = Time.time;
            PruneTrail(now);

            CombatHitboxShape? prev = null;
            for (int i = 0; i < _swingTrail.Count; i++)
            {
                TrailSample sample = _swingTrail[i];
                float life = Mathf.Clamp01(1f - (now - sample.Time) / _swingTrailDuration);
                if (life <= 0f)
                    continue;

                Color color = _swingTrailColor;
                color.a *= life;
                DrawShapeWire(sample.Shape, color);

                // 연속 샘플의 중심을 이어 스윙 경로를 강조한다.
                if (prev.HasValue)
                {
                    Gizmos.color = color;
                    Gizmos.DrawLine(prev.Value.Center, sample.Shape.Center);
                }
                prev = sample.Shape;
            }
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
