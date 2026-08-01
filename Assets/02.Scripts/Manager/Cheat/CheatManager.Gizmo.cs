#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UPlayGround.Debugging;

namespace UPlayGround.Manager
{
    /// <summary>
    /// CheatManager — 기즈모 토글. 개발 빌드 전용.
    ///
    /// 히트박스만 런타임(개발 빌드 포함) 화면 렌더링을 위해 <see cref="HitboxRuntimeDebugRenderer"/> 를
    /// 직접 구동한다. 나머지(감지범위/AI/Nav)는 기존 <see cref="DebugGizmoManager"/>(에디터 씬뷰 전용)에
    /// 위임하며, 개발 스탠드얼론 빌드에는 DebugGizmoManager가 등록되지 않으므로 no-op이 된다.
    /// </summary>
    public partial class CheatManager
    {
        public bool IsHitboxGizmoEnabled => HitboxRuntimeDebugRenderer.Enabled;

        /// <summary> 히트박스 기즈모 토글. 런타임 렌더러 + (에디터) 씬뷰 기즈모를 함께 제어. </summary>
        public void SetHitboxGizmo(bool value)
        {
            HitboxRuntimeDebugRenderer.Enabled = value;

#if UNITY_EDITOR
            var gizmo = DebugGizmoManager.Instance;
            if (gizmo != null)
            {
                gizmo.SetEnabled(true);
                gizmo.SetCategory(DebugGizmoCategory.Combat, true);
                gizmo.SetContentType(DebugGizmoContentType.HitboxSwingTrail, value);
            }
#endif
            Log(CheatCategory.Gizmo, $"Hitbox {(value ? "ON" : "OFF")}");
        }

        /// <summary> 감지 범위 기즈모(에디터 씬뷰 전용). </summary>
        public void SetDetectionRangeGizmo(bool value)
        {
#if UNITY_EDITOR
            var gizmo = DebugGizmoManager.Instance;
            if (gizmo != null)
            {
                gizmo.SetEnabled(true);
                gizmo.SetCategory(DebugGizmoCategory.AI, true);
                gizmo.SetContentType(DebugGizmoContentType.EnemyDetection, value);
            }
#endif
            Log(CheatCategory.Gizmo, $"Detection Range {(value ? "ON" : "OFF")}");
        }

        /// <summary> AI 디버그 기즈모(에디터 씬뷰 전용). </summary>
        public void SetAiDebugGizmo(bool value)
        {
#if UNITY_EDITOR
            var gizmo = DebugGizmoManager.Instance;
            if (gizmo != null)
            {
                // DebugGizmoManager는 실제 사용 전까지 비활성 상태로 유지된다.
                // 개별 치트 토글이 켜질 때 전역 기즈모도 함께 활성화해야 표시된다.
                if (value)
                    gizmo.SetEnabled(true);
                gizmo.SetCategory(DebugGizmoCategory.AI, value);
            }
#endif
            Log(CheatCategory.Gizmo, $"AI Debug {(value ? "ON" : "OFF")}");
        }

        /// <summary> Nav/Path(이동) 기즈모(에디터 씬뷰 전용). </summary>
        public void SetNavPathGizmo(bool value)
        {
#if UNITY_EDITOR
            var gizmo = DebugGizmoManager.Instance;
            if (gizmo != null)
            {
                // 다른 기즈모 토글과 동일하게 지연 초기화된 매니저를 먼저 활성화한다.
                if (value)
                    gizmo.SetEnabled(true);
                gizmo.SetCategory(DebugGizmoCategory.Movement, value);
            }
#endif
            Log(CheatCategory.Gizmo, $"Nav/Path {(value ? "ON" : "OFF")}");
        }
    }
}
#endif
