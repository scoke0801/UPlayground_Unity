using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI
{
    /// <summary>성장 경로를 호 길이 UV를 가진 베지어 리본으로 그린다.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UISkillTreeCurve : MaskableGraphic
    {
        private Vector2 _start;
        private Vector2 _end;
        private Vector2 _arrivalDirection;
        private float _width;
        private float _bend;
        private int _segments;
        private bool _canFlow;

        /// <summary>노드 중심 좌표와 경로 표시 상태로 메시를 구성한다.</summary>
        public void Configure(Vector2 start, Vector2 end, float width, float bend,
            int segments, bool canFlow, Vector2 arrivalDirection)
        {
            _start = start;
            _end = end;
            _arrivalDirection = arrivalDirection.normalized;
            _width = Mathf.Max(1f, width);
            _bend = Mathf.Clamp(bend, 0.1f, 0.8f);
            _segments = Mathf.Clamp(segments, 8, 96);
            _canFlow = canFlow;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (_segments == 0 || (_end - _start).sqrMagnitude < 0.01f) return;
            float handleLength = Mathf.Abs(_end.y - _start.y) * _bend;
            Vector2 departure = Mathf.Abs(_end.x - _start.x) < 1f ? -_arrivalDirection : _arrivalDirection;
            Vector2 controlA = _start + departure * handleLength;
            Vector2 controlB = _end - _arrivalDirection * handleLength;
            Rect bounds = rectTransform.rect;
            controlA.x = Mathf.Clamp(controlA.x, bounds.xMin + _width, bounds.xMax - _width);
            controlB.x = Mathf.Clamp(controlB.x, bounds.xMin + _width, bounds.xMax - _width);
            Vector2 previous = _start;
            float distance = 0f;
            for (int i = 0; i <= _segments; i++)
            {
                float t = i / (float)_segments;
                float inverse = 1f - t;
                Vector2 point = inverse * inverse * inverse * _start
                    + 3f * inverse * inverse * t * controlA
                    + 3f * inverse * t * t * controlB + t * t * t * _end;
                Vector2 tangent = 3f * inverse * inverse * (controlA - _start)
                    + 6f * inverse * t * (controlB - controlA)
                    + 3f * t * t * (_end - controlB);
                if (tangent.sqrMagnitude < 0.001f) tangent = _end - _start;
                Vector2 normal = new Vector2(-tangent.y, tangent.x).normalized * (_width * 0.5f);
                distance += Vector2.Distance(previous, point);
                previous = point;
                AddVertex(mesh, point - normal, distance, 0f);
                AddVertex(mesh, point + normal, distance, 1f);
                if (i == 0) continue;
                int index = i * 2;
                mesh.AddTriangle(index - 2, index - 1, index);
                mesh.AddTriangle(index, index - 1, index + 1);
            }
        }

        private void AddVertex(VertexHelper mesh, Vector2 position, float distance, float side)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            // 픽셀 호 길이를 전달해 짧은 선과 긴 선에서 빛의 이동 속도를 맞춘다.
            vertex.uv0 = new Vector4(distance, side, _canFlow ? 1f : 0f, 0f);
            mesh.AddVert(vertex);
        }
    }
}
