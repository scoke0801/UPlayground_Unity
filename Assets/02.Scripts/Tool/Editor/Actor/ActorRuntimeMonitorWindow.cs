using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Manager;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Tag;
using UPlayGround.MovementController;
using UPlayGround.Animation;

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
        private bool    _showDetails     = true;
        private double  _lastRefreshTime;
        private const double RefreshInterval = 0.25;

        // ── 캐시 ─────────────────────────────────────────────────────
        private List<ActorRow> _rows = new();
        private int _selectedActorInstanceId;
        private readonly Dictionary<int, StateTrackInfo> _stateTrackMap = new();

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
        private const float ColAnim      = 260f;
        private const float ColAnimTime  = 90f;
        private const float ColTags      = 220f;
        private const float ColGroup     = 90f;
        private const float ColSpawnTime = 65f;
        private const float ColWarp      = 220f;
        private const float RowH         = 22f;

        private static readonly Color ColorWarpActive = new(0.55f, 0.85f, 1.00f);
        private static readonly Color ColorWarpIdle   = new(0.55f, 0.55f, 0.55f);

        // ── 메뉴 ─────────────────────────────────────────────────────
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/캐릭터/액터/액터 런타임 모니터", priority = 101)]
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
            _showDetails = GUILayout.Toggle(_showDetails, "상세", EditorStyles.toolbarButton, GUILayout.Width(45));

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
            DrawHeaderCell(ref x, y, ColAnim,      "애니메이션");
            DrawHeaderCell(ref x, y, ColAnimTime,  "재생");
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
            DrawCell(ref x, y, ColState, row.stateSummary, EditorStyles.miniLabel);

            // 애니메이션
            DrawCell(ref x, y, ColAnim, row.animationSummary, EditorStyles.miniLabel);
            DrawProgressCell(ref x, y, ColAnimTime, row.animationNormalizedTime, row.animationTimeSummary);

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

            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
            {
                _selectedActorInstanceId = row.instanceId;
                GUI.FocusControl(null);
                Repaint();
            }
        }

        private void DrawProgressCell(ref float x, float y, float width, float ratio, string label)
        {
            var rect = new Rect(x + 2, y + 2, width - 6, RowH - 8);
            EditorGUI.DrawRect(rect, ColorHpBg);
            if (ratio > 0f)
            {
                var fill = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(ratio), rect.height);
                EditorGUI.DrawRect(fill, new Color(0.30f, 0.55f, 0.90f));
            }

            GUI.Label(rect, label, EditorStyles.miniLabel);
            x += width;
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
            if (_showDetails)
                DrawSelectedActorDetails();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(
                $"Play Time: {Time.time:F1}s  |  Actor 합계: {_rows.Count}",
                EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSelectedActorDetails()
        {
            ActorRow selected = null;
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].instanceId == _selectedActorInstanceId)
                {
                    selected = _rows[i];
                    break;
                }
            }

            if (selected == null && _rows.Count > 0)
            {
                selected = _rows[0];
                _selectedActorInstanceId = selected.instanceId;
            }

            if (selected == null)
                return;

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinHeight(92f));
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{selected.name}  ({selected.actorId})", EditorStyles.boldLabel, GUILayout.Width(260f));
            GUILayout.Label($"상태: {selected.stateName}  {selected.stateAge:F1}s", EditorStyles.miniLabel, GUILayout.Width(180f));
            GUILayout.Label($"이전: {selected.previousStateName}", EditorStyles.miniLabel, GUILayout.Width(160f));
            GUILayout.Label($"전환: {selected.lastTransitionSummary}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"MotionSet: {selected.motionSetName}", EditorStyles.miniLabel, GUILayout.Width(260f));
            GUILayout.Label($"Key: {selected.animationKey}", EditorStyles.miniLabel, GUILayout.Width(150f));
            GUILayout.Label($"Motion: {selected.motionIndexSummary} {selected.motionName}", EditorStyles.miniLabel, GUILayout.Width(260f));
            GUILayout.Label($"Clip: {selected.clipName}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"시간: {selected.animationTimeSummary}  local={selected.localTimeSummary}", EditorStyles.miniLabel, GUILayout.Width(260f));
            GUILayout.Label($"Layer: {selected.layerIndex}", EditorStyles.miniLabel, GUILayout.Width(80f));
            GUILayout.Label($"Speed: state {selected.stateSpeed:F2} / graph {selected.graphSpeed:F2}", EditorStyles.miniLabel, GUILayout.Width(180f));
            GUILayout.Label($"Loop/Freeze: {selected.loopSummary}", EditorStyles.miniLabel, GUILayout.Width(220f));
            GUILayout.Label($"Events: {selected.activeEvents}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(selected.warpInfo) && selected.warpInfo != "-")
                GUILayout.Label($"MotionWarp: {selected.warpInfo}", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
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
                int instanceId = actor.GetInstanceID();
                StateTrackInfo stateTrack = UpdateStateTrack(instanceId, stateName);

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

                ActorAnimator.AnimationDebugSnapshot animSnapshot = actor.Animator != null
                    ? actor.Animator.CaptureDebugSnapshot()
                    : ActorAnimator.AnimationDebugSnapshot.Empty;
                string animationSummary = BuildAnimationSummary(animSnapshot);
                string animationTimeSummary = BuildAnimationTimeSummary(animSnapshot);

                _rows.Add(new ActorRow
                {
                    instanceId  = instanceId,
                    actor      = actor,
                    actorId    = string.IsNullOrEmpty(actor.ActorId) ? "(없음)" : actor.ActorId,
                    name       = actor.name,
                    typeName   = actor.ActorType.ToString(),
                    hpCurrent  = hpCurrent,
                    hpMax      = hpMax,
                    stateName  = stateName,
                    stateSummary = $"{stateName} {stateTrack.StateAge:F1}s",
                    previousStateName = stateTrack.PreviousStateName,
                    stateAge = stateTrack.StateAge,
                    lastTransitionSummary = stateTrack.LastTransitionSummary,
                    tags       = tagsText,
                    groupName  = groupName,
                    spawnTime  = info != null ? info.spawnTime : -1f,
                    warpActive = warpActive,
                    warpInfo   = warpInfo,
                    animationSummary = animationSummary,
                    animationTimeSummary = animationTimeSummary,
                    animationNormalizedTime = animSnapshot.NormalizedTime,
                    animationKey = animSnapshot.DisplayKey,
                    motionSetName = animSnapshot.MotionSetName,
                    motionName = animSnapshot.MotionName,
                    clipName = animSnapshot.ClipName,
                    motionIndexSummary = animSnapshot.MotionIndex >= 0
                        ? $"{animSnapshot.MotionIndex + 1}/{animSnapshot.MotionCount}"
                        : "-",
                    localTimeSummary = animSnapshot.MotionDuration > 0f
                        ? $"{animSnapshot.LocalTime:F2}/{animSnapshot.MotionDuration:F2}s"
                        : "-",
                    layerIndex = animSnapshot.LayerIndex,
                    stateSpeed = animSnapshot.StateSpeed,
                    graphSpeed = animSnapshot.GraphSpeed,
                    loopSummary = BuildLoopSummary(animSnapshot),
                    activeEvents = animSnapshot.ActiveEvents,
                });
            }

            // actorId 기준 정렬
            _rows.Sort((a, b) => string.Compare(a.actorId, b.actorId, System.StringComparison.OrdinalIgnoreCase));

            PruneDeadStateTracks();
        }

        /// <summary>
        /// 파괴된(디스폰된) 액터의 상태 추적 엔트리를 제거한다.
        /// 필터로 숨겨졌을 뿐 살아 있는 액터의 전환 이력은 보존해야 하므로,
        /// _rows(표시 대상)가 아니라 InstanceID의 실제 생존 여부로 판정한다.
        /// </summary>
        private void PruneDeadStateTracks()
        {
            if (_stateTrackMap.Count == 0)
                return;

            List<int> dead = null;
            foreach (int id in _stateTrackMap.Keys)
            {
                if (EditorUtility.InstanceIDToObject(id) == null)
                    (dead ??= new List<int>()).Add(id);
            }

            if (dead == null)
                return;

            for (int i = 0; i < dead.Count; i++)
                _stateTrackMap.Remove(dead[i]);
        }

        private StateTrackInfo UpdateStateTrack(int instanceId, string stateName)
        {
            if (!_stateTrackMap.TryGetValue(instanceId, out StateTrackInfo info))
            {
                info = new StateTrackInfo
                {
                    CurrentStateName = stateName,
                    PreviousStateName = "-",
                    EnterTime = Time.time,
                    LastTransitionSummary = "-",
                };
                _stateTrackMap[instanceId] = info;
                return info;
            }

            if (info.CurrentStateName != stateName)
            {
                info.PreviousStateName = info.CurrentStateName;
                info.CurrentStateName = stateName;
                info.EnterTime = Time.time;
                info.LastTransitionSummary = $"{info.PreviousStateName} → {stateName} @{Time.time:F1}s";
            }

            return info;
        }

        private static string BuildAnimationSummary(ActorAnimator.AnimationDebugSnapshot snapshot)
        {
            if (!snapshot.IsValid)
                return "-";

            if (snapshot.IsPlayingMotionSet)
                return $"{snapshot.DisplayKey} / {snapshot.MotionName} / {snapshot.ClipName}";

            return $"Clip / {snapshot.ClipName}";
        }

        private static string BuildAnimationTimeSummary(ActorAnimator.AnimationDebugSnapshot snapshot)
        {
            if (!snapshot.IsValid)
                return "-";

            if (snapshot.TotalDuration > 0f)
                return $"{snapshot.GlobalTime:F2}/{snapshot.TotalDuration:F2}s";

            return $"{snapshot.GlobalTime:F2}s";
        }

        private static string BuildLoopSummary(ActorAnimator.AnimationDebugSnapshot snapshot)
        {
            if (!snapshot.IsValid)
                return "-";

            string freeze = snapshot.IsFrozen ? "Freeze" : "Run";
            string loop = snapshot.IsInfiniteLooping
                ? $"InfiniteLoop stage {snapshot.InfiniteLoopStageIndex}"
                : "Loop -";
            return $"{freeze}, {loop}";
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
            public int instanceId;
            public GameActor actor;
            public string actorId;
            public string name;
            public string typeName;
            public float  hpCurrent;
            public float  hpMax;
            public string stateName;
            public string stateSummary;
            public string previousStateName;
            public float  stateAge;
            public string lastTransitionSummary;
            public string tags;      // GameplayTag 목록 (쉼표 구분)
            public string groupName;
            public float  spawnTime; // -1이면 스폰 기록 없음
            public bool   warpActive;
            public string warpInfo;
            public string animationSummary;
            public string animationTimeSummary;
            public float animationNormalizedTime;
            public string animationKey;
            public string motionSetName;
            public string motionName;
            public string clipName;
            public string motionIndexSummary;
            public string localTimeSummary;
            public int layerIndex;
            public float stateSpeed;
            public float graphSpeed;
            public string loopSummary;
            public string activeEvents;
        }

        private class StateTrackInfo
        {
            public string CurrentStateName;
            public string PreviousStateName;
            public float EnterTime;
            public string LastTransitionSummary;

            public float StateAge => Mathf.Max(0f, Time.time - EnterTime);
        }
    }
}
