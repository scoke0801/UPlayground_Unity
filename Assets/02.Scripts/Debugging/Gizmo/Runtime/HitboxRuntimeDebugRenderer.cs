#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Combat;

namespace UPlayGround.Debugging
{
    /// <summary>
    /// 개발 빌드 전용 런타임 히트박스 렌더러.
    ///
    /// 기존 <see cref="DebugGizmoManager"/> 는 <c>OnDrawGizmos</c> 기반이라 스탠드얼론(개발) 빌드에서는
    /// 선이 그려지지 않는다. 개발 치트 패널의 "히트박스" 토글로 활성화되면, 이 렌더러가
    /// <see cref="CombatHitbox.Active"/> 레지스트리를 순회하며 각 히트박스의 현재 월드 형상을
    /// GL 즉시모드(무할당)로 그려 에디터/개발 빌드 모두에서 화면에 표시한다.
    ///
    /// 릴리스 빌드에서는 파일 전체가 컴파일되지 않는다.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class HitboxRuntimeDebugRenderer : MonoBehaviour
    {
        /// <summary> 치트 패널 "히트박스" 토글이 설정하는 활성 플래그. </summary>
        public static bool Enabled { get; set; }

        /// <summary> 히트박스 와이어 색. 치트 패널에서 변경 가능. </summary>
        public static Color LineColor { get; set; } = new(1f, 0.25f, 0.1f, 0.9f);

        private static HitboxRuntimeDebugRenderer s_instance;

        // 레지스트리를 순회하며 그리는 동안 만료 히트박스를 제거해도 안전하도록 매 프레임 복사해 두는 버퍼.
        private static readonly List<CombatHitbox> s_drawBuffer = new(64);

        private Material _lineMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (s_instance != null)
                return;

            var go = new GameObject(nameof(HitboxRuntimeDebugRenderer));
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            s_instance = go.AddComponent<HitboxRuntimeDebugRenderer>();
        }

        private void EnsureMaterial()
        {
            if (_lineMaterial != null)
                return;

            // Unity 내장 셰이더. 정점 컬러를 그대로 출력하며 GL 즉시모드에 적합하다.
            var shader = Shader.Find("Hidden/Internal-Colored");
            _lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _lineMaterial.SetInt("_ZWrite", 0);
            _lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always); // 벽 뒤에서도 보이게
        }

        // OnRenderObject 는 카메라마다 호출되므로 별도 카메라 훅 없이 씬을 렌더하는 모든 카메라에 그려진다.
        /// <summary> 명시적 범위 판정(Collision Event Explicit Shape) 와이어 색. 부착형과 구분한다. </summary>
        public static Color ExplicitShapeColor { get; set; } = new(0.3f, 0.6f, 1f, 0.9f);

        /// <summary> 실제 Physics 질의에 사용된 형상과 Anchor 스냅샷 표시 색. </summary>
        public static Color ExplicitAnchorColor { get; set; } = new(1f, 0.85f, 0.2f, 0.9f);

        private static readonly List<CombatCollisionSession> s_sessionBuffer = new(16);

        private void OnRenderObject()
        {
            if (!Enabled)
                return;

            int explicitCount = ExplicitCollisionDebugRegistry.Active.Count;
            if (CombatHitbox.Active.Count == 0 && explicitCount == 0)
                return;

            EnsureMaterial();

            // 그리는 도중 TryReleaseInactiveDebug 로 레지스트리를 수정해도 안전하도록 스냅샷을 뜬다.
            s_drawBuffer.Clear();
            foreach (var hitbox in CombatHitbox.Active)
                s_drawBuffer.Add(hitbox);

            s_sessionBuffer.Clear();
            foreach (var session in ExplicitCollisionDebugRegistry.Active)
                s_sessionBuffer.Add(session);

            _lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.Begin(GL.LINES);

            for (int i = 0; i < s_drawBuffer.Count; i++)
            {
                CombatHitbox hitbox = s_drawBuffer[i];
                if (hitbox != null)
                    DrawHitbox(hitbox);
            }

            for (int i = 0; i < s_sessionBuffer.Count; i++)
                DrawExplicitSession(s_sessionBuffer[i]);

            GL.End();
            GL.PopMatrix();

            s_sessionBuffer.Clear();

            // 판정이 끝났고 잔상도 만료된 히트박스를 레지스트리에서 정리한다(다음 프레임부터 순회 제외).
            for (int i = 0; i < s_drawBuffer.Count; i++)
                s_drawBuffer[i]?.TryReleaseInactiveDebug();
            s_drawBuffer.Clear();
        }

        // 에디터 CombatHitbox.OnDrawGizmos 와 동일한 구성으로 그린다:
        // (1) 시간에 따라 페이드되는 스윙 트레일(잔상 호), (2) 현재 판정 형상(감지 스윕 샘플 또는 현재 형상).
        private static void DrawHitbox(CombatHitbox hitbox)
        {
            DrawSwingTrail(hitbox);

            var detectionSamples = hitbox.LastDetectionSamples;
            if (detectionSamples != null && detectionSamples.Count > 0)
            {
                GL.Color(LineColor);
                CombatHitboxShape? previous = null;
                for (int i = 0; i < detectionSamples.Count; i++)
                {
                    CombatHitboxShape sample = detectionSamples[i];
                    DrawShape(sample);

                    if (previous.HasValue)
                        Line(previous.Value.Center, sample.Center);
                    previous = sample;
                }
                return;
            }

            // 감지 샘플이 아직 없는 활성 프레임에서만 현재 형상을 그린다. 판정이 끝난 뒤에는 잔상 트레일만 남긴다.
            if (hitbox.IsSampling && hitbox.TryGetWorldShape(out CombatHitboxShape current))
            {
                GL.Color(LineColor);
                DrawShape(current);
            }
        }

        // 에디터 OnDrawGizmos 의 스윙 트레일 로직을 그대로 옮긴다: 수명에 따라 알파 페이드, 연속 샘플 중심 연결,
        // 체인(채찍) 리더는 '첫 노드→끝 노드' 직선 + 말단 궤적만 그려 경량 유지.
        private static void DrawSwingTrail(CombatHitbox hitbox)
        {
            if (!hitbox.WantsSwingTrail)
                return;

            hitbox.PruneSwingTrailForDebug();
            int count = hitbox.SwingTrailSampleCount;
            if (count == 0)
                return;

            float now = Time.time;
            float duration = hitbox.SwingTrailDuration;
            bool chain = hitbox.IsChainTrail;
            Color baseColor = hitbox.SwingTrailColor;

            CombatHitboxShape? previous = null;
            for (int i = 0; i < count; i++)
            {
                hitbox.GetSwingTrailSample(i, out CombatHitboxShape shape, out float time);
                float life = Mathf.Clamp01(1f - (now - time) / duration);
                if (life <= 0f)
                    continue;

                Color color = baseColor;
                color.a *= life;
                GL.Color(color);

                if (chain)
                {
                    Line(shape.Point0, shape.Point1);
                    if (previous.HasValue)
                        Line(previous.Value.Point1, shape.Point1);
                }
                else
                {
                    DrawShape(shape);
                    if (previous.HasValue)
                        Line(previous.Value.Center, shape.Center);
                }
                previous = shape;
            }
        }

        // 명시적 판정 세션: (1) 현재 활성 Shape, (2) 마지막 실제 질의 Shape, (3) Anchor 스냅샷과 중심 연결선.
        private static void DrawExplicitSession(CombatCollisionSession session)
        {
            if (session == null || !session.IsActive)
                return;

            ResolvedCollisionShape resolved = session.Shape;
            if (!resolved.TryGetWorldShape(out CombatHitboxShape current))
                return;

            GL.Color(ExplicitShapeColor);
            DrawShape(current);

            // 실제 질의에 사용된 형상이 현재 형상과 다르면(Snapshot vs Follow) 함께 표시한다.
            if (session.HasLastQueriedShape)
            {
                CombatHitboxShape queried = session.LastQueriedShape;
                if ((queried.Center - current.Center).sqrMagnitude > 0.0001f)
                {
                    GL.Color(ExplicitAnchorColor);
                    DrawShape(queried);
                }
            }

            resolved.GetAnchorPose(out Vector3 anchorPosition, out _);
            GL.Color(ExplicitAnchorColor);
            Line(anchorPosition, current.Center);
            DrawWireSphere(anchorPosition, 0.15f);
        }

        private static void DrawShape(in CombatHitboxShape shape)
        {
            if (shape.Type == CombatHitboxShapeType.Box)
                DrawBox(shape);
            else if (shape.Type == CombatHitboxShapeType.Sphere)
                DrawWireSphere(shape.Center, shape.Radius);
            else
                DrawCapsule(shape);
        }

        private static void DrawBox(in CombatHitboxShape shape)
        {
            Vector3 e = shape.HalfExtents;
            // 로컬 8정점 → 월드 변환
            Vector3 c000 = ToWorld(shape, new Vector3(-e.x, -e.y, -e.z));
            Vector3 c001 = ToWorld(shape, new Vector3(-e.x, -e.y,  e.z));
            Vector3 c010 = ToWorld(shape, new Vector3(-e.x,  e.y, -e.z));
            Vector3 c011 = ToWorld(shape, new Vector3(-e.x,  e.y,  e.z));
            Vector3 c100 = ToWorld(shape, new Vector3( e.x, -e.y, -e.z));
            Vector3 c101 = ToWorld(shape, new Vector3( e.x, -e.y,  e.z));
            Vector3 c110 = ToWorld(shape, new Vector3( e.x,  e.y, -e.z));
            Vector3 c111 = ToWorld(shape, new Vector3( e.x,  e.y,  e.z));

            // 아래면
            Line(c000, c100); Line(c100, c101); Line(c101, c001); Line(c001, c000);
            // 윗면
            Line(c010, c110); Line(c110, c111); Line(c111, c011); Line(c011, c010);
            // 기둥
            Line(c000, c010); Line(c100, c110); Line(c101, c111); Line(c001, c011);
        }

        private static Vector3 ToWorld(in CombatHitboxShape shape, Vector3 local)
            => shape.Center + shape.Rotation * local;

        // 와이어 스피어의 원을 근사하는 세그먼트 수. 에디터 Gizmos.DrawWireSphere 와 비슷한 해상도.
        private const int CircleSegments = 24;

        // 에디터 기즈모(CombatHitbox.DrawShapeWire)와 동일한 구조로 그린다:
        // 양 끝점의 와이어 스피어 + 양옆 레일 2줄. GL 즉시모드라 DrawWireSphere 대신 원을 직접 근사한다.
        private static void DrawCapsule(in CombatHitboxShape shape)
        {
            DrawWireSphere(shape.Point0, shape.Radius);
            DrawWireSphere(shape.Point1, shape.Radius);

            Vector3 radial = shape.Point1 - shape.Point0;
            radial = radial.sqrMagnitude > 0.0001f
                ? Vector3.Cross(radial, Vector3.up).normalized
                : Vector3.right;
            Line(shape.Point0 + radial * shape.Radius, shape.Point1 + radial * shape.Radius);
            Line(shape.Point0 - radial * shape.Radius, shape.Point1 - radial * shape.Radius);
        }

        // Gizmos.DrawWireSphere 와 동일하게 월드 축에 정렬된 3개 원(XY/YZ/XZ 평면)으로 스피어 윤곽을 근사한다.
        private static void DrawWireSphere(Vector3 center, float radius)
        {
            if (radius <= 0f)
                return;

            DrawCircle(center, Vector3.right, Vector3.up, radius);
            DrawCircle(center, Vector3.up, Vector3.forward, radius);
            DrawCircle(center, Vector3.right, Vector3.forward, radius);
        }

        private static void DrawCircle(Vector3 center, Vector3 u, Vector3 v, float radius)
        {
            Vector3 previous = center + u * radius;
            for (int i = 1; i <= CircleSegments; i++)
            {
                float angle = (i / (float)CircleSegments) * Mathf.PI * 2f;
                Vector3 point = center + (u * Mathf.Cos(angle) + v * Mathf.Sin(angle)) * radius;
                Line(previous, point);
                previous = point;
            }
        }

        private static void Line(Vector3 a, Vector3 b)
        {
            GL.Vertex(a);
            GL.Vertex(b);
        }

        private void OnDestroy()
        {
            if (_lineMaterial != null)
                Destroy(_lineMaterial);
        }
    }
}
#endif
