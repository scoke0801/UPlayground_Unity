using System;
using UnityEngine;
using UnityEngine.Rendering;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>중앙 시야를 비운 카메라 공간 바람 궤적을 재사용 메시로 그린다.</summary>
    public sealed class GlidingSpeedLines : IDisposable
    {
        private readonly Camera _camera;
        private readonly CameraSettings _settings;
        private readonly GameObject _root;
        private readonly Mesh _mesh;
        private readonly Vector3[] _vertices;
        private readonly Color[] _colors;
        private readonly int _count;
        private float _intensity;
        private float _time;

        public GlidingSpeedLines(Camera camera, CameraSettings settings)
        {
            _camera = camera;
            _settings = settings;
            _count = Mathf.Clamp(settings.glidingSpeedLineCount, 8, 96);
            _vertices = new Vector3[_count * 4];
            _colors = new Color[_vertices.Length];
            var indices = new int[_count * 6];
            for (int i = 0; i < _count; i++)
            {
                int vertex = i * 4, triangle = i * 6;
                indices[triangle] = vertex;
                indices[triangle + 1] = vertex + 1;
                indices[triangle + 2] = vertex + 2;
                indices[triangle + 3] = vertex + 2;
                indices[triangle + 4] = vertex + 1;
                indices[triangle + 5] = vertex + 3;
            }
            _mesh = new Mesh { name = "GlidingSpeedLines", hideFlags = HideFlags.DontSave };
            _mesh.MarkDynamic();
            _mesh.vertices = _vertices;
            _mesh.triangles = indices;
            _root = new GameObject("GlidingSpeedLines") { hideFlags = HideFlags.DontSave };
            _root.transform.SetParent(camera.transform, false);
            _root.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var renderer = _root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = settings.glidingSpeedLineMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _root.SetActive(false);
        }

        /// <summary>실제 이동의 카메라 방향 성분을 궤적에 반영하며 연출 모드에서는 즉시 숨긴다.</summary>
        public void Advance(float target, Vector3 velocity, float deltaTime, bool canShow)
        {
            if (_camera == null || _root == null) return;
            _intensity = canShow ? Mathf.Lerp(target, _intensity,
                Mathf.Exp(-deltaTime / Mathf.Max(0.01f, _settings.glidingFeedbackBlendTime))) : 0f;
            _root.SetActive(_intensity > 0.001f);
            if (_intensity <= 0.001f || deltaTime <= 0f) return;
            _time += deltaTime;
            float depth = Mathf.Max(_camera.nearClipPlane + 0.1f, 1f);
            float halfHeight = Mathf.Tan(_camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * depth;
            float halfWidth = halfHeight * _camera.aspect;
            Vector3 localVelocity = _camera.transform.InverseTransformDirection(velocity.normalized);
            float blurBlend = Mathf.Clamp01(SpeedRadialBlurState.Strength
                / Mathf.Max(0.0001f, _settings.glidingRadialBlurStrength));
            float blurScale = Mathf.Lerp(1f, _settings.radialBlurSpeedLineScale, blurBlend);
            for (int i = 0; i < _count; i++)
            {
                float phase = Mathf.Repeat(_time / Mathf.Max(0.01f, _settings.glidingSpeedLineLifetime)
                    + i * 0.618034f, 1f);
                float angle = i * 2.399963f;
                Vector2 radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float radius = Mathf.Lerp(_settings.glidingSpeedLineInnerRadius, 1.35f, phase);
                Vector2 center = radial * radius;
                Vector2 direction = radial * localVelocity.z - new Vector2(localVelocity.x, localVelocity.y);
                if (direction.sqrMagnitude < 0.001f) direction = radial;
                direction.Normalize();
                Vector2 side = new Vector2(-direction.y, direction.x) * _settings.glidingSpeedLineWidth;
                Vector2 tail = center - direction * (_settings.glidingSpeedLineLength * _intensity);
                int vertex = i * 4;
                _vertices[vertex] = ToPosition(tail - side, halfWidth, halfHeight, depth);
                _vertices[vertex + 1] = ToPosition(tail + side, halfWidth, halfHeight, depth);
                _vertices[vertex + 2] = ToPosition(center - side, halfWidth, halfHeight, depth);
                _vertices[vertex + 3] = ToPosition(center + side, halfWidth, halfHeight, depth);
                float alpha = Mathf.Sin(phase * Mathf.PI) * _intensity * _settings.glidingSpeedLineOpacity * blurScale;
                _colors[vertex] = _colors[vertex + 1] = new Color(1f, 1f, 1f, 0f);
                _colors[vertex + 2] = _colors[vertex + 3] = new Color(1f, 1f, 1f, alpha);
            }
            _mesh.vertices = _vertices;
            _mesh.colors = _colors;
            _mesh.RecalculateBounds();
        }

        private static Vector3 ToPosition(Vector2 point, float width, float height, float depth)
            => new Vector3(point.x * width, point.y * height, depth);

        /// <summary>씬 전환과 매니저 종료에서 런타임 메시까지 해제한다.</summary>
        public void Dispose()
        {
            if (Application.isPlaying)
            {
                if (_root != null) UnityEngine.Object.Destroy(_root);
                if (_mesh != null) UnityEngine.Object.Destroy(_mesh);
                return;
            }
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
            if (_mesh != null) UnityEngine.Object.DestroyImmediate(_mesh);
        }
    }
}
