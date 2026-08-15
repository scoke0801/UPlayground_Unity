#if UNITY_EDITOR || DEVELOPMENT_BUILD
using TMPro;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.UI.DevCheat
{
    /// <summary>UI_System_DevCheatPanel — 기즈모 탭.</summary>
    public partial class UI_System_DevCheatPanel
    {
        private void BuildGizmoTab(RectTransform panel)
        {
            var v = AddVLG(panel.gameObject, 10, 16);
            v.childForceExpandHeight = false;
            v.childAlignment = TextAnchor.UpperLeft;

            var title = MakeText(panel, "기즈모 / 디버그 표시", 24, Accent, TextAlignmentOptions.Left);
            SetSize(title.gameObject, minH: 36, prefH: 36);

            var desc = MakeText(panel,
                "Hitbox는 개발 빌드에서도 화면에 렌더링됩니다. 나머지 항목은 에디터 씬뷰 전용입니다.",
                16, TextSub, TextAlignmentOptions.Left);
            SetSize(desc.gameObject, minH: 44, prefH: 44);

            var cheat = CheatManager.Instance;

            MakeToggleRow(panel, "Hitbox (런타임 렌더)", cheat != null && cheat.IsHitboxGizmoEnabled,
                v2 => CheatManager.Instance?.SetHitboxGizmo(v2));
            MakeToggleRow(panel, "Detection Range (에디터)", false,
                v2 => CheatManager.Instance?.SetDetectionRangeGizmo(v2));
            MakeToggleRow(panel, "AI Debug (에디터)", false,
                v2 => CheatManager.Instance?.SetAiDebugGizmo(v2));
            MakeToggleRow(panel, "Nav / Path (에디터)", false,
                v2 => CheatManager.Instance?.SetNavPathGizmo(v2));
        }

        private void MakeToggleRow(RectTransform parent, string label, bool isOn, System.Action<bool> onChanged)
        {
            var toggle = MakeToggle(parent, label, isOn, onChanged);
            SetSize(toggle.gameObject, minH: 46, prefH: 46);
        }
    }
}
#endif
