using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Manager;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Tag;
using UPlayGround.MovementController;

namespace UPlayGround.Actor.Editor
{
    /// <summary>
    /// 런타임에 씬에 존재하는 Actor들의 상태를 실시간으로 모니터링하는 에디터 창.
    /// Play 모드에서만 의미 있는 데이터를 표시한다.
    /// 메뉴: UPlayGround/Actor/Actor Runtime Monitor
    /// </summary>
    public class ActorRuntimeMonitorWindow : EditorWindow
    {
        // ── UI 상태 ───────────────────────────────────────────────────
        private Vector2 _scrollPos;
        private string  _filterActorId  = "";
        private ActorType _filterType   = ActorType.None;
        private bool    _showSpawnedOnly = false;
        private bool    _autoRefresh     = true;
        private double  _lastRefreshTime;
        private const double RefreshInterval = 0.25;

        // ── 캐시 ─────────────────────────────────────────────────────
        private List<ActorRow> _rows = new();

        // ── 스타일 캐시 ───────────────────────────────────────────────
        private GUIStyle _styleHeader;
        private bool     _stylesInitialized;

        // ── 색상 ─────────────────────────────────────────────────────
        private static readonly Color ColorHeader   = new(0.13f, 0.13f, 0.18f);
        private static readonly Color ColorRowEven  = new(0.20f, 0.20f, 0.22f);
        private static readonly Color ColorRowOdd   = new(0.23f, 0.23f, 0.25f);
        private static readonly Color ColorHpFull   = new(0.20f, 0.75f, 0.30f);
        private static readonly Color ColorHpMid    = new(0.85f, 0.70f, 0.10f);
        private static readonly Color ColorHpLow    = new(0.80f, 0.20f, 0.15f);
        private static readonly Color ColorHpBg     = new(0.12f, 0.12f, 0.14f);

        // ── 컬럼 너비 ────────────────────────────────────────────────
        private const float ColActorId   = 140f;
        private const float ColName      = 120f;
        private const float ColType      = 75f;
        private const float ColHp        = 110f;
        private const float ColState     = 130f;
        private const float ColTags      = 220f;
        private const float ColGroup     = 90f;
        private const float ColSpawnTime = 65f;
        private const float ColWarp      = 220f;
        private const float RowH         = 22f;

        private static readonly Color ColorWarpActive = new(0.55f, 0.85f, 1.00f);
        private static readonly Color ColorWarpIdle   = new(0.55f, 0.55f, 0.55f);

        // ── 메뉴 ─────────────────────────────────────────────────────
        [MenuItem("UPlayGround/Character/Actor/Actor Runtime Monitor")]
        public static void Open()
        {
            var window = GetWindow<ActorRuntimeMonitorWindow>();
            window.titleContent = new GUIContent("Actor Monitor", EditorGUIUtility.IconContent("d_UnityEditor.ProfilerWindow").image);
            window.minSize = new Vector2(1080f, 320f);
            window.Show();
        }

        // ── 라이프사이클 ──────────────────────────────────────────────
        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!_autoRefresh) return;
            if (EditorApplication.timeSinceStartup - _lastRefreshTime < RefreshInterval) return;
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            CollectRows();
            Repaint();
        }

        // ── OnGUI ─────────────────────────────────────────────────────
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
            DrawFooter();
        }

        // ── 툴바 ─────────────────────────────────────────────────────
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // ActorID 필터
            GUILayout.Label("ActorID", EditorStyles.toolbarButton, GUILayout.Width(50));
            _filterActorId = EditorGUILayout.TextField(_filterActorId, EditorStyles.toolbarSearchField, GUILayout.Width(130));
            if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
                _filterActorId = "";

            GUILayout.Space(8);

            // ActorType 필터 (Flags enum이므로 EnumFlagsField 사용)
            GUILayout.Label("타입", EditorStyles.toolbarButton, GUILayout.Width(30));
            _filterType = (ActorType)EditorGUILayout.EnumFlagsField(_filterType, EditorStyles.toolbarPopup, GUILayout.Width(90));

            GUILayout.Space(8);

            // 스폰된 것만 보기
            _showSpawnedOnly = GUILayout.Toggle(_showSpawnedOnly, "스폰된 것만", EditorStyles.toolbarButton, GUILayout.Width(80));

            GUILayout.FlexibleSpace();

            // 자동 갱신
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "자동 갱신", EditorStyles.toolbarButton, GUILayout.Width(70));
            if (GUILayout.Button("새로 고침", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                CollectRows();
                Repaint();
            }

            GUILayout.Label($"총 {_rows.Count}개", EditorStyles.toolbarButton, GUILayout.Width(55));
            EditorGUILayout.EndHorizontal();
        }

        // ── 컬럼 헤더 ────────────────────────────────────────────────
        private void DrawColumnHeader()
        {
            Rect headerRect = GUILayoutUtility.GetRect(0, RowH + 2, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(headerRect, ColorHeader);

            float x = headerRect.x + 4;
            float y = headerRect.y + 3;
            DrawHeaderCell(ref x, y, ColActorId,   "ActorID");
            DrawHeaderCell(ref x, y, ColName,      "이름");
            DrawHeaderCell(ref x, y, ColType,      "타입");
            DrawHeaderCell(ref x, y, ColHp,        "HP");
            DrawHeaderCell(ref x, y, ColState,     "현재 상태");
            DrawHeaderCell(ref x, y, ColTags,      "GameplayTags");
            DrawHeaderCell(ref x, y, ColGroup,     "그룹");
            DrawHeaderCell(ref x, y, ColSpawnTime, "스폰 경과");
            DrawHeaderCell(ref x, y, ColWarp,      "MotionWarp");
        }

        private void DrawHeaderCell(ref float x, float y, float width, string label)
        {
            GUI.Label(new Rect(x, y, width, RowH), label, EditorStyles.miniBoldLabel);
            x += width;
        }

        // ── 행 목록 ───────────────────────────────────────────────────
        private void DrawRows()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                DrawRow(row, i);
            }

            if (_rows.Count == 0)
            {
                EditorGUILayout.Space(12);
                GUILayout.Label("표시할 Actor가 없습니다.", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawRow(ActorRow row, int index)
        {
            bool isAlive = row.actor != null;
            Color bg     = (index % 2 == 0) ? ColorRowEven : ColorRowOdd;

            Rect rowRect = GUILayoutUtility.GetRect(0, RowH, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rowRect, bg);

            float x = rowRect.x + 4;
            float y = rowRect.y + 3;

            // ActorID
            DrawCell(ref x, y, ColActorId, row.actorId, EditorStyles.miniLabel);

            // 이름 (클릭 시 선택)
            var nameRect = new Rect(x, y, ColName, RowH - 4);
            if (GUI.Button(nameRect, row.name, EditorStyles.miniButton) && isAlive)
            {
                Selection.activeGameObject = row.actor.gameObject;
                SceneView.lastActiveSceneView?.FrameSelected();
            }
            x += ColName;

            // 타입
            DrawCell(ref x, y, ColType, row.typeName, EditorStyles.miniLabel);

            // HP 바
            DrawHpBar(ref x, y, row.hpCurrent, row.hpMax);

            // 현재 상태
            DrawCell(ref x, y, ColState, row.stateName, EditorStyles.miniLabel);

            // GameplayTags
            DrawTagsCell(ref x, y, row.tags);

            // 그룹
            DrawCell(ref x, y, ColGroup, row.groupName, EditorStyles.miniLabel);

            // 스폰 경과 시간
            string elapsed = row.spawnTime >= 0f
                ? $"{(Time.time - row.spawnTime):F1}s"
                : "-";
            DrawCell(ref x, y, ColSpawnTime, elapsed, EditorStyles.miniLabel);

            // MotionWarp 상태
            DrawWarpCell(ref x, y, row);
        }

        private void DrawWarpCell(ref float x, float y, ActorRow row)
        {
            var rect = new Rect(x + 2, y, ColWarp - 4, RowH - 4);
            var old = GUI.contentColor;
            GUI.contentColor = row.warpActive ? ColorWarpActive : ColorWarpIdle;
            string text = string.IsNullOrEmpty(row.warpInfo) ? "-" : row.warpInfo;
            GUI.Label(rect, text, EditorStyles.miniLabel);
            GUI.contentColor = old;
            x += ColWarp;
        }

        private void DrawCell(ref float x, float y, float width, string text, GUIStyle style)
        {
            GUI.Label(new Rect(x, y, width - 4, RowH - 4), text, style);
            x += width;
        }

        private void DrawTagsCell(ref float x, float y, string tagsText)
        {
            var rect = new Rect(x + 2, y, ColTags - 4, RowH - 4);
            if (!string.IsNullOrEmpty(tagsText))
            {
                // 태그가 있으면 살짝 강조
                var old = GUI.contentColor;
                GUI.contentColor = new Color(0.70f, 0.95f, 0.70f);
                GUI.Label(rect, tagsText, EditorStyles.miniLabel);
                GUI.contentColor = old;
            }
            else
            {
                GUI.Label(rect, "-", EditorStyles.miniLabel);
            }
            x += ColTags;
        }

        private void DrawHpBar(ref float x, float y, float current, float max)
        {
            var bgRect  = new Rect(x + 2, y + 2, ColHp - 6, RowH - 8);
            EditorGUI.DrawRect(bgRect, ColorHpBg);

            if (max > 0f)
            {
                float ratio   = Mathf.Clamp01(current / max);
                var fillRect  = new Rect(bgRect.x, bgRect.y, bgRect.width * ratio, bgRect.height);
                Color barColor = ratio > 0.5f ? ColorHpFull : (ratio > 0.25f ? ColorHpMid : ColorHpLow);
                EditorGUI.DrawRect(fillRect, barColor);

                GUI.Label(bgRect,
                    $"  {current:F0} / {max:F0}",
                    EditorStyles.miniLabel);
            }
            else
            {
                GUI.Label(bgRect, "  N/A", EditorStyles.miniLabel);
            }

            x += ColHp;
        }

        // ── 푸터 ─────────────────────────────────────────────────────
        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(
                $"Play Time: {Time.time:F1}s  |  Actor 합계: {_rows.Count}",
                EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // ── 데이터 수집 ───────────────────────────────────────────────
        private void CollectRows()
        {
            _rows.Clear();

            if (!Application.isPlaying) return;

            // ActorSpawnManager의 스폰 정보(시간, 그룹) 맵
            var spawnInfoMap = new Dictionary<int, ActorSpawnManager.SpawnedActorInfo>();
            if (ActorSpawnManager.Instance != null)
            {
                foreach (var kv in ActorSpawnManager.Instance.SpawnedActors)
                {
                    if (kv.Value?.actor != null)
                        spawnInfoMap[kv.Value.actor.GetInstanceID()] = kv.Value;
                }
            }

            // 씬의 모든 GameActor 수집
            var allActors = FindObjectsByType<GameActor>(FindObjectsSortMode.None);

            foreach (var actor in allActors)
            {
                if (actor == null) continue;

                // 스폰된 것만 보기 필터
                bool hasSpawnInfo = spawnInfoMap.ContainsKey(actor.GetInstanceID());
                if (_showSpawnedOnly && !hasSpawnInfo) continue;

                // ActorType 필터 (None=0 이면 필터 없음, 복합 Flags 조합도 지원)
                if (_filterType != ActorType.None && (actor.ActorType & _filterType) == 0) continue;

                // ActorID 필터
                if (!string.IsNullOrEmpty(_filterActorId) &&
                    actor.ActorId.IndexOf(_filterActorId, System.StringComparison.OrdinalIgnoreCase) < 0) continue;

                // 상태 이름
                string stateName = "-";
                if (actor.ActorController != null)
                    stateName = actor.ActorController.CurrentState?.StateName ?? "-";

                // HP 정보 (MonsterActor만)
                float hpCurrent = -1f, hpMax = -1f;
                if (actor is MonsterActor monster)
                {
                    hpCurrent = monster.CurrentHealth;
                    hpMax     = monster.MaxHealth;
                }
                else if (actor is PlayerActor)
                {
                    // PlayerActor는 별도 HP 시스템 — 확장 여지
                    hpCurrent = -1f;
                    hpMax     = -1f;
                }

                // 그룹 이름
                string groupName = "-";
                spawnInfoMap.TryGetValue(actor.GetInstanceID(), out var info);
                if (info?.group != null)
                    groupName = info.group.name;
                else if (actor is MonsterActor m && m.AIController?.Group != null)
                    groupName = m.AIController.Group.name;

                // 태그 정보 수집
                string tagsText = "-";
                if (actor.Tags != null && actor.Tags.AllTags.Count > 0)
                    tagsText = actor.Tags.ToString();

                // MotionWarp 상태 수집
                bool warpActive = false;
                string warpInfo = "-";
                var warp = actor.GetComponent<MotionWarpController>();
                if (warp != null)
                {
                    if (warp.IsMotionWarping)
                    {
                        warpActive = true;
                        var settings = warp.HasActiveWindow ? warp.ActiveWindowSettings : default;
                        float t = warp.WarpDuration > 0f ? 1f - (warp.WarpRemainingTime / warp.WarpDuration) : 0f;
                        string targetName = warp.ActiveTarget.IsValid && warp.ActiveTarget.anchor != null
                            ? warp.ActiveTarget.anchor.name
                            : "-";
                        warpInfo =
                            $"key={warp.ActiveKey} → {targetName}  " +
                            $"t={t:F2} blend={warp.BlendWeight:F2} OOR={warp.OutOfRangeAccumulator:F2}s\n" +
                            $"{settings.targetPolicy} {settings.modifierType}" +
                            (warp.IsApplicable ? "" : $" — {warp.LastFailureReason}");
                    }
                    else if (warp.HasTarget)
                    {
                        warpInfo = $"idle (target ready: {warp.ActiveKey})";
                    }
                }

                _rows.Add(new ActorRow
                {
                    actor      = actor,
                    actorId    = string.IsNullOrEmpty(actor.ActorId) ? "(없음)" : actor.ActorId,
                    name       = actor.name,
                    typeName   = actor.ActorType.ToString(),
                    hpCurrent  = hpCurrent,
                    hpMax      = hpMax,
                    stateName  = stateName,
                    tags       = tagsText,
                    groupName  = groupName,
                    spawnTime  = info != null ? info.spawnTime : -1f,
                    warpActive = warpActive,
                    warpInfo   = warpInfo,
                });
            }

            // actorId 기준 정렬
            _rows.Sort((a, b) => string.Compare(a.actorId, b.actorId, System.StringComparison.OrdinalIgnoreCase));
        }

        // ── 안내 메시지 ───────────────────────────────────────────────
        private void DrawNotPlayingMessage()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("Play 모드에서만 Actor 상태를 확인할 수 있습니다.", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
        }

        // ── 스타일 초기화 ─────────────────────────────────────────────
        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _styleHeader = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 12,
                alignment = TextAnchor.MiddleLeft,
                normal    = { textColor = Color.white },
            };
        }

        // ── 행 데이터 구조 ────────────────────────────────────────────
        private class ActorRow
        {
            public GameActor actor;
            public string actorId;
            public string name;
            public string typeName;
            public float  hpCurrent;
            public float  hpMax;
            public string stateName;
            public string tags;      // GameplayTag 목록 (쉼표 구분)
            public string groupName;
            public float  spawnTime; // -1이면 스폰 기록 없음
            public bool   warpActive;
            public string warpInfo;
        }
    }
}
