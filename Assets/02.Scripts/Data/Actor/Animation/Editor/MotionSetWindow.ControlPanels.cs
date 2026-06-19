using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    // 애니메이션 에디터 상단 보조 컨트롤 패널의 레이아웃을 담당한다.
    // Phase 2: 키 큰 보조 패널(루트모션/워프/디버그/전투오버레이)을 탭 스트립으로 묶어
    //          한 번에 하나만 표시한다(또는 전부 닫음). 선택 탭은 EditorPrefs로 영속화.
    public partial class MotionSetEditorWindow
    {
        const string PREFS_PANEL_TAB  = "MotionSetWindow_PanelTab";
        const string PREFS_PANEL_HELP = "MotionSetWindow_PanelHelp";

        // 보조 패널의 장문 안내(HelpBox) 노출 여부. 기본 OFF로 세로 공간을 아낀다.
        // 각 패널 메서드가 이 값을 보고 HelpBox를 조건부로 그린다.
        bool ShowPanelHelp => EditorPrefs.GetBool(PREFS_PANEL_HELP, false);

        static readonly string[] _panelTabTitles =
        {
            "루트 모션",
            "워프",
            "이벤트 디버그",
            "전투 오버레이",
            "촬영 연동",
        };

        // 탭 스트립 + 선택된 패널. 기본값 -1(전부 닫힘) → 평소엔 타임라인이 위로 올라온다.
        void DrawControlPanelTabs()
        {
            int cur = EditorPrefs.GetInt(PREFS_PANEL_TAB, -1);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                for (int i = 0; i < _panelTabTitles.Length; i++)
                {
                    bool on = cur == i;
                    bool now = GUILayout.Toggle(on, _panelTabTitles[i], EditorStyles.toolbarButton);
                    if (now != on)
                    {
                        cur = now ? i : -1; // 같은 탭을 다시 누르면 닫힘(전부 숨김)
                        EditorPrefs.SetInt(PREFS_PANEL_TAB, cur);
                    }
                }
                GUILayout.FlexibleSpace();

                // 장문 안내 토글 — 패널 내 HelpBox를 일괄 표시/숨김
                bool help = ShowPanelHelp;
                bool newHelp = GUILayout.Toggle(
                    help, new GUIContent("ⓘ 도움말", "패널 내 장문 설명(HelpBox)을 표시/숨김"),
                    EditorStyles.toolbarButton, GUILayout.Width(70));
                if (newHelp != help)
                    EditorPrefs.SetBool(PREFS_PANEL_HELP, newHelp);
            }

            switch (cur)
            {
                case 0:
                    DrawRootMotionControls();
                    break;
                case 1:
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    DrawWarpBakeControls();
                    EditorGUILayout.EndVertical();
                    DrawWarpTargetControls();
                    break;
                case 2:
                    DrawEventDebugControls();
                    break;
                case 3:
                    DrawCombatOverlayPanel();
                    break;
                case 4:
                    DrawCaptureBridgeControls();
                    break;
            }
        }

        // 패널 접힘 여부와 무관하게 매 프레임 실행돼야 하는 부작용을 모아둔다.
        // (전투 오버레이 패널을 닫아도 타임라인 전투 트랙은 계속 갱신돼야 한다.)
        void RunControlPanelSideEffects()
        {
            LoadCombatPrefsOnce();
            RefreshCombatOverlayTracks();
        }
    }
}
