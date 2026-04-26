#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.Stat;
using UPlayGround.Manager;

namespace UPlayGround.Tool.Editor.Stat
{
    /// <summary>
    /// Play 모드 전용. 씬의 모든 GameActor의 ActorStatContainer 상태를 실시간 모니터링한다.
    /// 메뉴: UPlayGround/Stat/Stat Runtime Monitor
    /// </summary>
    public class StatRuntimeMonitorWindow : EditorWindow
    {
        // ── UI 상태 ───────────────────────────────────────────────
        private string _filter = "";
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double RefreshInterval = 0.25;
        private Vector2 _scrollPos;
        private readonly HashSet<int> _expanded = new();

        // ── 스타일 ────────────────────────────────────────────────
        private GUIStyle _styleHeader;
        private GUIStyle _styleSubLabel;
        private bool _stylesInitialized;

        // ── 색상 ─────────────────────────────────────────────────
        private static readonly Color ColorHeader   = new(0.13f, 0.13f, 0.18f);
        private static readonly Color ColorRowEven  = new(0.20f, 0.20f, 0.22f);
        private static readonly Color ColorRowOdd   = new(0.23f, 0.23f, 0.25f);
        private static readonly Color ColorBarBg    = new(0.12f, 0.12f, 0.14f);
        private static readonly Color ColorHpFull   = new(0.20f, 0.75f, 0.30f);
        private static readonly Color ColorHpLow    = new(0.80f, 0.20f, 0.15f);
        private static readonly Color ColorPoise    = new(0.85f, 0.70f, 0.10f);
        private static readonly Color ColorModBuff  = new(0.30f, 0.70f, 0.95f);
        private static readonly Color ColorModDebuff= new(0.95f, 0.40f, 0.30f);

        // ── 컬럼 너비 ─────────────────────────────────────────────
        private const float ColExpand = 18f;
        private const float ColName   = 180f;
        private const float ColHp     = 130f;
        private const float ColAtk    = 50f;
        private const float ColDef    = 50f;
        private const float ColPoise  = 110f;
        private const float ColMods   = 50f;
        private const float RowH      = 22f;

        // ── 메뉴 ─────────────────────────────────────────────────
        [MenuItem("UPlayGround/Stat/Stat Runtime Monitor")]
        public static void Open()
        {
            var window = GetWindow<StatRuntimeMonitorWindow>();
            window.titleContent = new GUIContent("Stat Monitor", EditorGUIUtility.IconContent("d_UnityEditor.ProfilerWindow").image);
            window.minSize = new Vector2(720f, 360f);
            window.Show();
        }

        // ── 라이프사이클 ──────────────────────────────────────────
        private void OnEnable()  => EditorApplication.update += OnEditorUpdate;
        private void OnDisable() => EditorApplication.update -= OnEditorUpdate;

        private void OnEditorUpdate()
        {
            if (!_autoRefresh) return;
            if (EditorApplication.timeSinceStartup - _lastRefreshTime < RefreshInterval) return;
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            Repaint();
        }

        // ── OnGUI ────────────────────────────────────────────────
        private void OnGUI()
        {
            InitStyles();
            DrawToolbar();

            if (!Application.isPlaying)
            {
                DrawNotPlayingMessage();
                return;
            }

            DrawColumnHeader();
            DrawRows();
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _styleHeader = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(6, 0, 2, 0),
            };
            _styleSubLabel = new GUIStyle(EditorStyles.miniLabel)
            {
                richText = true,
            };
        }

        // ── 툴바 ─────────────────────────────────────────────────
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("필터", GUILayout.Width(30));
            _filter = EditorGUILayout.TextField(_filter, EditorStyles.toolbarSearchField, GUILayout.Width(180));
            if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
                _filter = "";

            GUILayout.FlexibleSpace();

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "자동 갱신", EditorStyles.toolbarButton, GUILayout.Width(70));

            if (GUILayout.Button("모두 펼치기", EditorStyles.toolbarButton, GUILayout.Width(80)))
                ExpandAll(true);
            if (GUILayout.Button("모두 접기", EditorStyles.toolbarButton, GUILayout.Width(70)))
                ExpandAll(false);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawNotPlayingMessage()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Play 모드에서만 동작합니다.", EditorStyles.centeredGreyMiniLabel);
        }

        // ── 컬럼 헤더 ────────────────────────────────────────────
        private void DrawColumnHeader()
        {
            var rect = GUILayoutUtility.GetRect(0, RowH, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, ColorHeader);

            float x = rect.x;
            DrawHeaderCell("", ref x, ColExpand, rect.y, rect.height);
            DrawHeaderCell("Actor", ref x, ColName,  rect.y, rect.height);
            DrawHeaderCell("HP",    ref x, ColHp,    rect.y, rect.height);
            DrawHeaderCell("ATK",   ref x, ColAtk,   rect.y, rect.height);
            DrawHeaderCell("DEF",   ref x, ColDef,   rect.y, rect.height);
            DrawHeaderCell("Poise", ref x, ColPoise, rect.y, rect.height);
            DrawHeaderCell("Mods",  ref x, ColMods,  rect.y, rect.height);
        }

        private void DrawHeaderCell(string label, ref float x, float w, float y, float h)
        {
            GUI.Label(new Rect(x, y, w, h), label, _styleHeader);
            x += w;
        }

        // ── 행 ───────────────────────────────────────────────────
        private void DrawRows()
        {
            var manager = GameObjectManager.Instance;
            if (manager == null)
            {
                EditorGUILayout.LabelField("GameObjectManager가 초기화되지 않았습니다.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            string lower = _filter.ToLower();
            int row = 0;

            foreach (var actor in manager.AllActors)
            {
                if (actor == null) continue;
                if (!string.IsNullOrEmpty(lower) && !actor.gameObject.name.ToLower().Contains(lower) && !actor.ActorId.ToLower().Contains(lower))
                    continue;

                DrawActorRow(actor, row++);

                int instanceId = actor.GetInstanceID();
                if (_expanded.Contains(instanceId))
                    DrawModifierList(actor.Stats);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawActorRow(GameActor actor, int rowIndex)
        {
            var rect = GUILayoutUtility.GetRect(0, RowH, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, rowIndex % 2 == 0 ? ColorRowEven : ColorRowOdd);

            var stats = actor.Stats;
            int instanceId = actor.GetInstanceID();
            bool isExpanded = _expanded.Contains(instanceId);

            float x = rect.x;

            // 펼치기 화살표
            if (GUI.Button(new Rect(x, rect.y, ColExpand, rect.height), isExpanded ? "▾" : "▸", EditorStyles.miniLabel))
            {
                if (isExpanded) _expanded.Remove(instanceId);
                else _expanded.Add(instanceId);
            }
            x += ColExpand;

            // 이름
            string label = string.IsNullOrEmpty(actor.ActorId) ? actor.gameObject.name : $"{actor.gameObject.name}  [{actor.ActorId}]";
            GUI.Label(new Rect(x, rect.y, ColName, rect.height), label);
            x += ColName;

            // HP 바
            DrawHpCell(actor, new Rect(x + 2, rect.y + 2, ColHp - 4, rect.height - 4));
            x += ColHp;

            // ATK / DEF
            GUI.Label(new Rect(x, rect.y, ColAtk, rect.height), stats.AttackPower.ToString("0.##"));
            x += ColAtk;

            GUI.Label(new Rect(x, rect.y, ColDef, rect.height), stats.Defense.ToString("0.##"));
            x += ColDef;

            // Poise (PoiseStat이 있다면 그 값을, 없으면 컨테이너 MaxPoise만 표시)
            DrawPoiseCell(actor, new Rect(x + 2, rect.y + 2, ColPoise - 4, rect.height - 4));
            x += ColPoise;

            // 수정자 개수
            int modCount = stats.ModifierCount;
            var prevColor = GUI.color;
            if (modCount > 0) GUI.color = ColorModBuff;
            GUI.Label(new Rect(x, rect.y, ColMods, rect.height), modCount.ToString());
            GUI.color = prevColor;
        }

        private void DrawHpCell(GameActor actor, Rect rect)
        {
            float max = actor.Stats.MaxHealth;
            float current = max;
            if (actor is IDamageable dmg)
            {
                current = dmg.GetCurrentHealth();
            }

            float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            EditorGUI.DrawRect(rect, ColorBarBg);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * ratio, rect.height),
                               Color.Lerp(ColorHpLow, ColorHpFull, ratio));
            GUI.Label(rect, $"  {current:0}/{max:0}", _styleSubLabel);
        }

        private void DrawPoiseCell(GameActor actor, Rect rect)
        {
            float max = actor.Stats.MaxPoise;
            // PoiseStat은 자체적으로 현재값을 보유하므로 이름 기반 폴백 표시만
            EditorGUI.DrawRect(rect, ColorBarBg);
            // 풀 바를 단색으로 채우고 최대치만 표시 (컨테이너에 현재 Poise 추적 미구현)
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, rect.height), new Color(ColorPoise.r, ColorPoise.g, ColorPoise.b, 0.4f));
            GUI.Label(rect, $"  max {max:0}", _styleSubLabel);
        }

        private void DrawModifierList(ActorStatContainer stats)
        {
            var modifiers = stats.EditorGetModifiers();
            if (modifiers == null || modifiers.Count == 0)
            {
                var rect = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.18f));
                GUI.Label(new Rect(rect.x + 28, rect.y, rect.width - 28, rect.height), "활성 수정자 없음", _styleSubLabel);
                return;
            }

            foreach (var tm in modifiers)
            {
                var m = tm.Modifier;
                var rect = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.18f));

                string srcName     = m.source?.ToString() ?? "(unknown)";
                string sign        = m.value >= 0f ? "+" : "";
                string modSymbol   = m.modifierType switch
                {
                    ModifierType.Flat     => "Flat",
                    ModifierType.Percent  => "%",
                    ModifierType.Multiply => "×",
                    _ => "?",
                };
                string durationStr = m.IsPermanent ? "<color=#9CA0A8>영구</color>" : $"<color=#FFC34D>{tm.RemainingTime:F1}s</color>";

                Color barColor = m.value >= 0 ? ColorModBuff : ColorModDebuff;
                EditorGUI.DrawRect(new Rect(rect.x + 22, rect.y + 4, 3, rect.height - 8), barColor);

                string text = $"<b>{srcName}</b>  ·  {m.statType}  {modSymbol} {sign}{m.value:0.##}  ·  {durationStr}";
                GUI.Label(new Rect(rect.x + 30, rect.y, rect.width - 30, rect.height), text, _styleSubLabel);
            }
        }

        private void ExpandAll(bool expand)
        {
            _expanded.Clear();
            if (!expand) return;
            var manager = GameObjectManager.Instance;
            if (manager == null) return;
            foreach (var actor in manager.AllActors)
                if (actor != null) _expanded.Add(actor.GetInstanceID());
        }
    }
}
#endif
