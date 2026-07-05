#if UNITY_EDITOR || DEVELOPMENT_BUILD
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
        private void OnRenderObject()
        {
            if (!Enabled || CombatHitbox.Active.Count == 0)
                return;

            EnsureMaterial();

            _lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.Begin(GL.LINES);
            GL.Color(LineColor);

            foreach (var hitbox in CombatHitbox.Active)
            {
                if (hitbox == null)
                    continue;
                if (!hitbox.TryGetWorldShape(out CombatHitboxShape shape))
                    continue;

                if (shape.Type == CombatHitboxShapeType.Box)
                    DrawBox(shape);
                else
                    DrawCapsule(shape);
            }

            GL.End();
            GL.PopMatrix();
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

        // CombatHitbox.DrawCapsuleLineOutline 과 동일한 경량 윤곽(축 + 양옆 레일 + 양끝 캡).
        private static void DrawCapsule(in CombatHitboxShape shape)
        {
            Vector3 axis = shape.Point1 - shape.Point0;
            Vector3 perp = axis.sqrMagnitude > 0.0001f
                ? Vector3.Cross(axis, Vector3.up)
                : Vector3.right;
            if (perp.sqrMagnitude < 0.0001f)
                perp = Vector3.right;
            perp = perp.normalized * shape.Radius;

            Vector3 perp2 = axis.sqrMagnitude > 0.0001f
                ? Vector3.Cross(axis, perp).normalized * shape.Radius
                : Vector3.forward * shape.Radius;

            Line(shape.Point0, shape.Point1);
            Line(shape.Point0 + perp, shape.Point1 + perp);
            Line(shape.Point0 - perp, shape.Point1 - perp);
            Line(shape.Point0 + perp2, shape.Point1 + perp2);
            Line(shape.Point0 - perp2, shape.Point1 - perp2);
            // 양끝 캡(사각 링)
            Line(shape.Point0 + perp, shape.Point0 + perp2);
            Line(shape.Point0 + perp2, shape.Point0 - perp);
            Line(shape.Point0 - perp, shape.Point0 - perp2);
            Line(shape.Point0 - perp2, shape.Point0 + perp);
            Line(shape.Point1 + perp, shape.Point1 + perp2);
            Line(shape.Point1 + perp2, shape.Point1 - perp);
            Line(shape.Point1 - perp, shape.Point1 - perp2);
            Line(shape.Point1 - perp2, shape.Point1 + perp);
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
