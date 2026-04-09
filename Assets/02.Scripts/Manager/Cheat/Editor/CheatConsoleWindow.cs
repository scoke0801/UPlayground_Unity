using UnityEditor;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Cheat.Editor
{
    /// <summary>
    /// 치트 콘솔 에디터 윈도우.
    /// 플레이 모드에서만 CheatManager에 접근하여 치트를 토글한다.
    /// UPlayGround/치트 콘솔 메뉴에서 열 수 있다.
    /// </summary>
    public class CheatConsoleWindow : EditorWindow
    {
        // ── 스타일 캐시 ───────────────────────────────────────────────
        private GUIStyle _styleHeader;
        private GUIStyle _styleSectionLabel;
        private GUIStyle _styleStatusOn;
        private GUIStyle _styleStatusOff;
        private GUIStyle _styleWarningBox;
        private bool     _stylesInitialized;

        // ── 스크롤 ────────────────────────────────────────────────────
        private Vector2 _scrollPos;

        // ── 색상 팔레트 ───────────────────────────────────────────────
        private static readonly Color ColorOn      = new Color(0.20f, 0.80f, 0.35f);
        private static readonly Color ColorOff     = new Color(0.55f, 0.55f, 0.55f);
        private static readonly Color ColorHeader  = new Color(0.15f, 0.15f, 0.20f);
        private static readonly Color ColorSection = new Color(0.20f, 0.20f, 0.27f);
        private static readonly Color ColorWarning = new Color(0.80f, 0.40f, 0.10f);

        [MenuItem("UPlayGround/치트 콘솔 %&c")]
        public static void Open()
        {
            var window = GetWindow<CheatConsoleWindow>();
            window.titleContent = new GUIContent("치트 콘솔", EditorGUIUtility.IconContent("d_DebuggerEnabled").image);
            window.minSize = new Vector2(300f, 200f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            InitStyles();

            DrawHeader();

            if (!EditorApplication.isPlaying)
            {
                DrawNotPlayingWarning();
                return;
            }

            CheatManager cheat = CheatManager.Instance;
            if (cheat == null)
            {
                DrawManagerMissingWarning();
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            {
                EditorGUILayout.Space(6);
                DrawCombatSection(cheat);

                EditorGUILayout.Space(8);
                DrawFooter(cheat);
            }
            EditorGUILayout.EndScrollView();
        }

        // ── 헤더 ─────────────────────────────────────────────────────

        private void DrawHeader()
        {
            Rect headerRect = GUILayoutUtility.GetRect(0, 36, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(headerRect, ColorHeader);

            string playLabel = EditorApplication.isPlaying
                ? "<color=#55FF88>● 플레이 중</color>"
                : "<color=#AAAAAA>○ 정지</color>";

            GUI.Label(
                new Rect(headerRect.x + 10, headerRect.y + 4, headerRect.width - 20, 28),
                $"<b>치트 콘솔</b>     {playLabel}",
                _styleHeader);
        }

        // ── 경고 박스 ─────────────────────────────────────────────────

        private void DrawNotPlayingWarning()
        {
            EditorGUILayout.Space(10);
            Rect box = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            box.x     += 8; box.width -= 16;
            EditorGUI.DrawRect(box, ColorWarning * 0.35f);
            GUI.Label(box, "  ⚠  플레이 모드에서만 사용할 수 있습니다.", _styleWarningBox);
        }

        private void DrawManagerMissingWarning()
        {
            EditorGUILayout.Space(10);
            Rect box = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            box.x     += 8; box.width -= 16;
            EditorGUI.DrawRect(box, ColorWarning * 0.35f);
            GUI.Label(box, "  ⚠  CheatManager를 찾을 수 없습니다.", _styleWarningBox);
        }

        // ── 섹션: 전투 ────────────────────────────────────────────────

        private void DrawCombatSection(CheatManager cheat)
        {
            DrawSectionHeader("⚔  전투");

            EditorGUILayout.Space(4);

            // 항상 패리
            DrawCheatRow(
                label:       "항상 패리",
                description: "어떤 상태에서도 적의 공격을 패리합니다.",
                isOn:        cheat.IsAlwaysParryEnabled,
                onToggle:    v => cheat.SetAlwaysParry(v));

            EditorGUILayout.Space(4);
        }

        // ── 공통 레이아웃 헬퍼 ────────────────────────────────────────

        /// <summary> 섹션 제목 바 </summary>
        private void DrawSectionHeader(string title)
        {
            Rect r = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
            r.x += 6; r.width -= 12;
            EditorGUI.DrawRect(r, ColorSection);
            GUI.Label(new Rect(r.x + 8, r.y + 4, r.width, 18), title, _styleSectionLabel);
        }

        /// <summary>
        /// 치트 한 행: 상태 표시 + 레이블 + 설명 + 토글 버튼
        /// </summary>
        private void DrawCheatRow(string label, string description, bool isOn, System.Action<bool> onToggle)
        {
            EditorGUILayout.BeginHorizontal();
            {
                // 상태 인디케이터
                GUIStyle dot = isOn ? _styleStatusOn : _styleStatusOff;
                GUILayout.Label(isOn ? "●" : "○", dot, GUILayout.Width(20));

                // 레이블 + 설명
                EditorGUILayout.BeginVertical();
                {
                    GUILayout.Label(label, EditorStyles.boldLabel);
                    GUILayout.Label(description, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                // 토글 버튼
                GUI.backgroundColor = isOn ? ColorOn : Color.white;
                if (GUILayout.Button(isOn ? "ON" : "OFF", GUILayout.Width(50), GUILayout.Height(32)))
                    onToggle?.Invoke(!isOn);
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();
        }

        // ── 푸터 ─────────────────────────────────────────────────────

        private void DrawFooter(CheatManager cheat)
        {
            EditorGUI.DrawRect(
                GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true)),
                new Color(1, 1, 1, 0.08f));

            EditorGUILayout.Space(4);

            GUI.backgroundColor = new Color(0.8f, 0.25f, 0.25f);
            if (GUILayout.Button("모든 치트 끄기", GUILayout.Height(28)))
                ResetAll(cheat);
            GUI.backgroundColor = Color.white;
        }

        private void ResetAll(CheatManager cheat)
        {
            cheat.SetAlwaysParry(false);
            Debug.Log("[CheatConsole] 모든 치트 비활성화");
        }

        // ── 스타일 초기화 ─────────────────────────────────────────────

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _styleHeader = new GUIStyle(EditorStyles.label)
            {
                fontSize  = 13,
                fontStyle = FontStyle.Bold,
                richText  = true,
                alignment = TextAnchor.MiddleLeft,
                normal    = { textColor = Color.white },
            };

            _styleSectionLabel = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal   = { textColor = new Color(0.85f, 0.85f, 0.85f) },
            };

            _styleStatusOn = new GUIStyle(EditorStyles.label)
            {
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = ColorOn },
            };

            _styleStatusOff = new GUIStyle(EditorStyles.label)
            {
                fontSize  = 14,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = ColorOff },
            };

            _styleWarningBox = new GUIStyle(EditorStyles.label)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal    = { textColor = new Color(1f, 0.75f, 0.3f) },
            };

            _stylesInitialized = true;
        }
    }
}
