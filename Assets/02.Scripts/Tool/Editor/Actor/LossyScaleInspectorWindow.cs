using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Actor.Editor
{
    /// <summary>
    /// KCC "lossy scale is not (1,1,1)" 경고의 범인을 찾는 에디터 창.
    /// 대상 액터를 지정하면 계층 전체를 탐색해 lossyScale이 (1,1,1)이 아닌 Transform을 열거한다.
    /// 메뉴: UPlayGround/Actor/Lossy Scale Inspector
    /// </summary>
    public class LossyScaleInspectorWindow : EditorWindow
    {
        // ── 대상 ──────────────────────────────────────────────────────
        private GameObject _target;

        // ── 결과 ──────────────────────────────────────────────────────
        private readonly List<Result> _results = new();
        private bool _scanned;

        // ── UI 상태 ──────────────────────────────────────────────────
        private Vector2 _scroll;
        private bool    _showOnlyBad = true;

        // ── 스타일 캐시 ───────────────────────────────────────────────
        private GUIStyle _styleHeader;
        private GUIStyle _styleBadRow;
        private GUIStyle _styleOkRow;
        private GUIStyle _stylePath;
        private bool     _stylesInitialized;

        // ── 색상 ─────────────────────────────────────────────────────
        private static readonly Color ColorHeader  = new(0.13f, 0.13f, 0.18f);
        private static readonly Color ColorBadRow  = new(0.45f, 0.10f, 0.10f);
        private static readonly Color ColorOkRow   = new(0.18f, 0.30f, 0.18f);
        private static readonly Color ColorBadText = new(1.0f, 0.55f, 0.45f);
        private static readonly Color ColorOkText  = new(0.6f, 0.9f, 0.6f);

        private const float Eps = 0.0001f;

        // ── 메뉴 ─────────────────────────────────────────────────────
        [MenuItem("UPlayGround/Character/Actor/Lossy Scale Inspector", priority =  101)]
        public static void Open()
        {
            var window = GetWindow<LossyScaleInspectorWindow>();
            window.titleContent = new GUIContent("Lossy Scale Inspector",
                EditorGUIUtility.IconContent("d_console.warnicon.sml").image);
            window.minSize = new Vector2(560f, 320f);
            window.Show();
        }

        // ─────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            InitStyles();
            DrawHeader();
            DrawToolbar();

            if (_target == null)
            {
                EditorGUILayout.HelpBox("대상 GameObject를 지정하세요.", MessageType.Info);
                return;
            }

            if (!_scanned)
            {
                EditorGUILayout.HelpBox("'스캔' 버튼을 눌러 계층을 검사하세요.", MessageType.None);
                return;
            }

            DrawSummary();
            DrawResultList();
        }

        // ── 헤더 ─────────────────────────────────────────────────────
        private void DrawHeader()
        {
            var headerRect = EditorGUILayout.GetControlRect(false, 28f);
            EditorGUI.DrawRect(headerRect, ColorHeader);
            var labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 13,
                alignment = TextAnchor.MiddleLeft,
                normal    = { textColor = new Color(0.85f, 0.85f, 0.95f) }
            };
            GUI.Label(new Rect(headerRect.x + 8, headerRect.y, headerRect.width - 8, headerRect.height),
                "Lossy Scale Inspector — KCC Scale 경고 범인 탐색기", labelStyle);
        }

        // ── 툴바 ─────────────────────────────────────────────────────
        private void DrawToolbar()
        {
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("대상 액터", GUILayout.Width(72));
                var prev = _target;
                _target = (GameObject)EditorGUILayout.ObjectField(_target, typeof(GameObject), true,
                    GUILayout.ExpandWidth(true));
                if (_target != prev)
                    _scanned = false;

                _showOnlyBad = GUILayout.Toggle(_showOnlyBad, "문제만 표시", GUILayout.Width(90));

                using (new EditorGUI.DisabledScope(_target == null))
                {
                    if (GUILayout.Button("스캔", GUILayout.Width(60)))
                        Scan();
                }

                if (GUILayout.Button("초기화", GUILayout.Width(60)))
                {
                    _target  = null;
                    _scanned = false;
                    _results.Clear();
                }
            }
            EditorGUILayout.Space(4);
        }

        // ── 요약 ─────────────────────────────────────────────────────
        private void DrawSummary()
        {
            int badCount = 0;
            foreach (var r in _results)
                if (r.IsBad) badCount++;

            var msg = badCount == 0
                ? $"이상 없음 — 전체 {_results.Count}개 Transform 모두 (1,1,1)"
                : $"경고: {badCount}개 Transform에서 lossyScale != (1,1,1) 검출 (전체 {_results.Count}개)";
            var type = badCount == 0 ? MessageType.Info : MessageType.Warning;
            EditorGUILayout.HelpBox(msg, type);
        }

        // ── 결과 목록 ────────────────────────────────────────────────
        private void DrawResultList()
        {
            // 컬럼 헤더
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Transform 경로",          GUILayout.Width(280));
                EditorGUILayout.LabelField("Local Scale",             GUILayout.Width(140));
                EditorGUILayout.LabelField("Lossy Scale",             GUILayout.Width(140));
                GUILayout.FlexibleSpace();
            }
            DrawSeparator();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (var r in _results)
            {
                if (_showOnlyBad && !r.IsBad) continue;
                DrawRow(r);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawRow(Result r)
        {
            var rowColor = r.IsBad ? ColorBadRow : ColorOkRow;
            var textColor = r.IsBad ? ColorBadText : ColorOkText;

            var rowRect = EditorGUILayout.GetControlRect(false, 20f);
            EditorGUI.DrawRect(rowRect, rowColor);

            var pathStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = textColor },
                clipping = TextClipping.Clip
            };
            var valStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.white }
            };

            float x = rowRect.x + 4;

            // 경로
            var pathRect = new Rect(x, rowRect.y + 1, 276, rowRect.height - 2);
            GUI.Label(pathRect, r.Path, pathStyle);
            x += 280;

            // local scale
            var localRect = new Rect(x, rowRect.y + 1, 136, rowRect.height - 2);
            GUI.Label(localRect, FormatScale(r.LocalScale), valStyle);
            x += 140;

            // lossy scale
            var lossyStyle = new GUIStyle(valStyle);
            if (r.IsBad) lossyStyle.normal.textColor = ColorBadText;
            var lossyRect = new Rect(x, rowRect.y + 1, 136, rowRect.height - 2);
            GUI.Label(lossyRect, FormatScale(r.LossyScale), lossyStyle);
            x += 140;

            // 선택 버튼
            float btnW = 52;
            var btnRect = new Rect(rowRect.xMax - btnW - 4, rowRect.y + 2, btnW, rowRect.height - 4);
            if (r.Transform != null && GUI.Button(btnRect, "선택", EditorStyles.miniButton))
            {
                Selection.activeTransform = r.Transform;
                EditorGUIUtility.PingObject(r.Transform.gameObject);
                SceneView.lastActiveSceneView?.FrameSelected();
            }
        }

        // ── 스캔 ─────────────────────────────────────────────────────
        private void Scan()
        {
            _results.Clear();

            if (_target == null) return;

            var all = _target.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                var ls = t.lossyScale;
                bool bad = Mathf.Abs(ls.x - 1f) > Eps
                        || Mathf.Abs(ls.y - 1f) > Eps
                        || Mathf.Abs(ls.z - 1f) > Eps;

                _results.Add(new Result
                {
                    Transform  = t,
                    Path       = GetRelativePath(t, _target.transform),
                    LocalScale = t.localScale,
                    LossyScale = ls,
                    IsBad      = bad
                });
            }

            // 문제 항목을 위로
            _results.Sort((a, b) =>
            {
                if (a.IsBad == b.IsBad) return string.Compare(a.Path, b.Path, System.StringComparison.Ordinal);
                return a.IsBad ? -1 : 1;
            });

            _scanned = true;
            Repaint();
        }

        // ── 유틸 ─────────────────────────────────────────────────────
        private static string GetRelativePath(Transform t, Transform root)
        {
            if (t == root) return root.name;
            var parts = new List<string>();
            var cur = t;
            while (cur != null && cur != root)
            {
                parts.Add(cur.name);
                cur = cur.parent;
            }
            parts.Add(root.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string FormatScale(Vector3 v) =>
            $"({v.x:F3}, {v.y:F3}, {v.z:F3})";

        private static void DrawSeparator()
        {
            var r = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(r, new Color(0.3f, 0.3f, 0.35f));
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _styleHeader = new GUIStyle(EditorStyles.boldLabel);
            _styleBadRow = new GUIStyle();
            _styleOkRow  = new GUIStyle();
            _stylePath   = new GUIStyle(EditorStyles.miniLabel);
            _stylesInitialized = true;
        }

        // ── 내부 데이터 ───────────────────────────────────────────────
        private class Result
        {
            public Transform Transform;
            public string    Path;
            public Vector3   LocalScale;
            public Vector3   LossyScale;
            public bool      IsBad;
        }
    }
}