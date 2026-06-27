using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// MotionSet 타임라인 드로잉 + 프로퍼티 편집 공용 클래스.
    /// Inspector, EditorWindow 양쪽에서 동일하게 사용한다.
    /// </summary>
    public class MotionSetDrawer
    {
        // 색상 팔레트
        static readonly Color COL_BG              = new Color(0.13f, 0.14f, 0.15f);
        static readonly Color COL_PANEL_BG        = new Color(0.11f, 0.12f, 0.13f);
        static readonly Color COL_TRACK_BG        = new Color(0.16f, 0.17f, 0.18f);
        static readonly Color COL_GROUP_HEADER_BG = new Color(0.14f, 0.15f, 0.17f);
        static readonly Color COL_RULER           = new Color(0.10f, 0.11f, 0.12f);
        static readonly Color COL_RULER_LINE      = new Color(0.35f, 0.37f, 0.40f);
        static readonly Color COL_RULER_TEXT      = new Color(0.65f, 0.67f, 0.72f);
        static readonly Color COL_CURSOR          = new Color(1.00f, 0.25f, 0.25f);
        static readonly Color COL_LABEL_BG        = new Color(0.10f, 0.11f, 0.12f);
        static readonly Color COL_LABEL_BORDER    = new Color(0.22f, 0.24f, 0.28f);
        static readonly Color COL_SECTION_HEADER  = new Color(0.16f, 0.18f, 0.20f);
        static readonly Color COL_INSPECTOR_BG    = new Color(0.12f, 0.13f, 0.14f);
        static readonly Color COL_INSPECTOR_FIELD = new Color(0.17f, 0.19f, 0.21f);
        static readonly Color COL_DIVIDER         = new Color(0.22f, 0.24f, 0.27f);

        static readonly Color[] COL_MOTION_CLIPS =
        {
            new Color(0.35f, 0.55f, 0.35f),
            new Color(0.55f, 0.65f, 0.30f),
            new Color(0.30f, 0.50f, 0.60f),
            new Color(0.50f, 0.40f, 0.55f),
        };

        // 이벤트 타입별 색상은 MotionEventStyle에서 관리
        static readonly Color COL_EVENT_SELECTED_OVERLAY = new Color(1f, 1f, 1f, 0.25f);
        static readonly Color COL_EVENT_DIAMOND          = new Color(1f, 1f, 1f, 0.8f);
        static readonly Color COL_MARKER = new Color(0.85f, 0.25f, 0.25f);
        static readonly Color COL_MARKER_TEXT = Color.white;
        static readonly Color COL_RANGE_OVERLAY = new Color(0.3f, 1f, 0.3f, 0.12f); // ④ 재생 구간 오버레이
        static readonly Color COL_RANGE_BORDER = new Color(0.3f, 1f, 0.3f, 0.5f); // ④ 재생 구간 경계선
        static readonly Color COL_CLIP_HANDLE = new Color(1f, 0.85f, 0.2f, 0.9f); // ⑥ 클립 핸들 색상

        // 레이아웃 상수
        const float LABEL_WIDTH       = 160f;
        const float INSPECTOR_WIDTH   = 280f;
        const float RULER_HEIGHT      = 22f;
        const float TRACK_HEIGHT      = 24f;
        const float EVENT_HEIGHT      = 20f;
        const float MARKER_HEIGHT     = 18f;
        const float GROUP_HEADER_H    = 20f;
        const float TRACK_GAP         = 1f;
        const float SECTION_GAP       = 4f;
        const float BASE_PPS          = 80f;
        const float CLIP_HANDLE_W     = 6f;

        // 인스펙터 패널 스크롤
        Vector2 _inspectorScroll;

        // 상태
        public float cursorTime;
        public float scrollX;
        public float zoom = 1f;
        public bool isDraggingCursor;
        public int selectedMotionIndex = -1;

        // 사용자가 ruler/scrub으로 cursorTime을 변경한 직후 1프레임 동안 true.
        // 외부(Window)에서 이 플래그를 보고 Seek를 발화한 뒤 false로 리셋한다.
        public bool cursorScrubRequested;

        // fps 표시 관련
        public bool showFrames = false; // true = 프레임 단위, false = 초 단위
        public int fps = 30; // 기준 fps

        // 재생 구간 오버레이용 (Window에서 설정)
        public float playRangeStart = 0f;
        public float playRangeEnd = -1f; // -1 = 전체

        // 이벤트 선택 연동
        public int selectedEventMotionIndex = -1;
        public int selectedEventIndex = -1;
        public bool selectedEventIsSetEvent = false;

        // 드래그 상태
        int _dragEventMotionIndex = -1;
        int _dragEventIndex = -1;
        bool _dragSetEvent;
        bool _isDraggingStart;
        bool _isDraggingEnd;
        bool _isDraggingBody;
        float _dragStartOffset;
        float _dragBodyStartTime;

        // 클립 핸들 드래그 상태
        int _clipHandleMotionIndex = -1;
        bool _clipHandleDraggingStart = false;
        bool _clipHandleDraggingEnd = false;

        // 이벤트 복사 버퍼
        MotionEventBase _copiedEvent = null;
        string _eventFilterText = string.Empty;

        // 섹션 접힘 상태
        public bool foldMotions = true;
        public bool foldTimeline = true;
        public bool foldEvents = true;

        // ====================================================================
        //  외부 주입 오버레이 트랙 (전투 데이터 등 읽기 전용 시각화)
        // ====================================================================
        public sealed class OverlaySpan
        {
            public float start;
            public float end;
            public string label;
            /// <summary> true면 점선/흐림으로 그려 "저작이 아니라 자동 추론된 구간"임을 시각 구분한다. </summary>
            public bool dashed;
        }

        public sealed class OverlayTrack
        {
            public string label;
            public Color color;
            public readonly List<OverlaySpan> spans = new List<OverlaySpan>();
        }

        /// <summary> null 또는 빈 리스트면 오버레이 그룹을 그리지 않는다. 매 프레임 외부에서 갱신해도 무방. </summary>
        public List<OverlayTrack> overlayTracks;
        public string overlayGroupTitle = "전투 데이터";

        // 이벤트 항목별 접힘 상태: key = "motionIdx_eventIdx" 또는 "set_eventIdx"
        readonly Dictionary<string, bool> _eventFoldouts = new Dictionary<string, bool>();

        bool GetEventFold(string key)
        {
            _eventFoldouts.TryGetValue(key, out bool open);
            return open; // 기본값 false = 접힌 상태
        }

        void SetEventFold(string key, bool open)
        {
            _eventFoldouts[key] = open;
        }

        static string EventKey(int motionIdx, int eventIdx) => $"m{motionIdx}_{eventIdx}";
        static string SetEventKey(int eventIdx) => $"set_{eventIdx}";

        // Undo/Dirty 처리용 콜백
        readonly Func<UnityEngine.Object> _getTarget;
        readonly Action _repaint;
        readonly Action<int, int> _onSelectedMotionChanged;

        /// <param name="getTarget">Undo/Dirty 대상 오브젝트 반환</param>
        /// <param name="repaint">Repaint 요청 콜백</param>
        public MotionSetDrawer(Func<UnityEngine.Object> getTarget, Action repaint, Action<int, int> onSelectedMotionChanged = null)
        {
            _getTarget = getTarget;
            _repaint = repaint;
            _onSelectedMotionChanged = onSelectedMotionChanged;
        }

        void RecordUndo(string name)
        {
            var obj = _getTarget?.Invoke();
            if (obj != null) Undo.RecordObject(obj, name);
        }

        void MarkDirty()
        {
            var obj = _getTarget?.Invoke();
            if (obj != null) EditorUtility.SetDirty(obj);
        }

        void Repaint() => _repaint?.Invoke();

        // ====================================================================
        //  전체 GUI (Inspector / Window 공용 진입점)
        // ====================================================================
        public void DrawFullGUI(MotionSet set)
        {
            HandleGlobalDragTermination();
            if (set == null) return;

            EditorGUILayout.Space(4);
            DrawHeader(set);
            EditorGUILayout.Space(2);

            foldMotions = EditorGUILayout.Foldout(foldMotions, "애니메이션 리스트", true, EditorStyles.foldoutHeader);
            if (foldMotions) DrawMotionList(set);

            EditorGUILayout.Space(4);

            // ── 2단 레이아웃: 인스펙터(좌) | 타임라인(우) ──
            foldTimeline = EditorGUILayout.Foldout(foldTimeline, "타임라인", true, EditorStyles.foldoutHeader);
            if (foldTimeline && set.IsValid())
            {
                Rect splitterRect = GUILayoutUtility.GetRect(0, CalcTimelineAndInspectorHeight(set));
                splitterRect.x    += 2;
                splitterRect.width -= 4;

                // 인스펙터 패널
                Rect inspRect     = new Rect(splitterRect.x, splitterRect.y, INSPECTOR_WIDTH, splitterRect.height);
                DrawInspectorPanel(inspRect, set);

                // 타임라인 패널
                Rect tlRect = new Rect(inspRect.xMax + 2, splitterRect.y,
                    splitterRect.width - INSPECTOR_WIDTH - 2, splitterRect.height);
                DrawTimeline(tlRect, set);
            }

            EditorGUILayout.Space(4);

            foldEvents = EditorGUILayout.Foldout(foldEvents, "애니메이션 이벤트", true, EditorStyles.foldoutHeader);
            if (foldEvents) DrawMotionSetEvents(set);

            if (isDraggingCursor || _isDraggingStart || _isDraggingEnd || _isDraggingBody
                || _clipHandleDraggingStart || _clipHandleDraggingEnd) Repaint();
        }

        /// <summary>
        /// 개별 이벤트 바의 가시성이나 호출 여부와 무관하게 드래그 상태를 종료한다.
        /// IMGUI에서는 창 밖 MouseUp 또는 포커스 손실 시 대상 컨트롤이 종료 이벤트를 받지 못할 수 있다.
        /// </summary>
        void HandleGlobalDragTermination()
        {
            Event e = Event.current;
            if (e == null) return;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                // 이전 MouseUp을 놓친 상태가 새 클릭으로 이어지지 않도록 먼저 정리한다.
                ResetEventDragState();
                return;
            }

            bool shiftReleased = e.type == EventType.KeyUp &&
                                 (e.keyCode == KeyCode.LeftShift || e.keyCode == KeyCode.RightShift);
            bool bodyDragLostShift = _isDraggingBody && e.type == EventType.MouseDrag && !e.shift;
            bool pointerReleased = e.type == EventType.MouseUp;
            bool pointerLeftWindow = e.type == EventType.MouseLeaveWindow;
            bool inputInterrupted = e.type == EventType.Ignore;

            if (shiftReleased || bodyDragLostShift || pointerReleased || pointerLeftWindow || inputInterrupted)
            {
                ResetEventDragState();

                if (pointerReleased || pointerLeftWindow || inputInterrupted)
                    CancelDragState();
            }
        }

        public void CancelDragState()
        {
            ResetEventDragState();
            isDraggingCursor = false;
            _clipHandleDraggingStart = false;
            _clipHandleDraggingEnd = false;
            _clipHandleMotionIndex = -1;
        }

        void ResetEventDragState()
        {
            _isDraggingStart = false;
            _isDraggingEnd = false;
            _isDraggingBody = false;
            _dragEventMotionIndex = -1;
            _dragEventIndex = -1;
        }

        float CalcTimelineAndInspectorHeight(MotionSet set)
        {
            int motionCount     = set.motions?.Count ?? 0;
            int eventGroupCount = CountEventGroupCount(set);
            int totalEventTracks = CountEventTracks(set);

            float timelineH = 20f           // zoom bar
                + RULER_HEIGHT + TRACK_GAP
                + GROUP_HEADER_H + TRACK_GAP
                + (TRACK_HEIGHT + TRACK_GAP) * Mathf.Max(motionCount, 1) + SECTION_GAP
                + GROUP_HEADER_H + TRACK_GAP
                + MARKER_HEIGHT + TRACK_GAP + SECTION_GAP
                + GROUP_HEADER_H + TRACK_GAP
                + (EVENT_HEIGHT + TRACK_GAP) * Mathf.Max(totalEventTracks, 1) + 8f;

            int overlayCount = overlayTracks?.Count ?? 0;
            if (overlayCount > 0)
            {
                timelineH += SECTION_GAP + GROUP_HEADER_H + TRACK_GAP
                    + (EVENT_HEIGHT + TRACK_GAP) * overlayCount;
            }

            return Mathf.Max(timelineH, 200f);
        }

        // ====================================================================
        //  좌측 인스펙터 패널
        // ====================================================================
        void DrawInspectorPanel(Rect rect, MotionSet set)
        {
            // 패널 배경
            EditorGUI.DrawRect(rect, COL_INSPECTOR_BG);
            EditorGUI.DrawRect(new Rect(rect.xMax, rect.y, 1, rect.height), COL_DIVIDER);

            // 타이틀 바
            Rect titleRect = new Rect(rect.x, rect.y, rect.width, 20f);
            EditorGUI.DrawRect(titleRect, COL_SECTION_HEADER);
            DrawPanelTitle(titleRect, "⚙  인스펙터");

            // 선택된 이벤트가 없으면 안내 메시지
            if (selectedEventIndex < 0)
            {
                Rect hintRect = new Rect(rect.x, titleRect.yMax + 20f, rect.width, 60f);
                var hintStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal    = { textColor = new Color(0.45f, 0.47f, 0.52f) },
                    wordWrap  = true
                };
                GUI.Label(hintRect, "타임라인에서 이벤트를\n클릭하여 선택하세요.", hintStyle);
                return;
            }

            // 선택된 이벤트 가져오기
            MotionEventBase selEvt = GetSelectedEvent(set);
            if (selEvt == null) return;

            var visual = MotionEventStyle.Get(selEvt);

            // 이벤트 타이틀 배지
            Rect badgeRect = new Rect(rect.x, titleRect.yMax, rect.width, 26f);
            EditorGUI.DrawRect(badgeRect, new Color(visual.color.r * 0.2f, visual.color.g * 0.2f, visual.color.b * 0.2f, 1f));
            EditorGUI.DrawRect(new Rect(badgeRect.x, badgeRect.y, 3, badgeRect.height), visual.color);

            var badgeStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal    = { textColor = visual.color },
                fontSize  = 11,
                padding   = new RectOffset(10, 4, 4, 0),
                alignment = TextAnchor.MiddleLeft
            };
            GUI.Label(badgeRect, $"{visual.icon}  {selEvt.GetDisplayName()}", badgeStyle);

            float y = badgeRect.yMax + 2f;

            // 타이밍 섹션
            Rect timingHeaderRect = new Rect(rect.x, y, rect.width, 18f);
            EditorGUI.DrawRect(timingHeaderRect, COL_SECTION_HEADER);
            EditorGUI.DrawRect(new Rect(rect.x, y, 3, 18f), visual.color);
            GUI.Label(timingHeaderRect, "  TIMING", new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.75f, 0.78f, 0.85f) },
                padding   = new RectOffset(8, 0, 2, 0),
                fontSize  = 10
            });
            y += 19f;
            EditorGUI.BeginChangeCheck();
            y = DrawInspectorTimingRow(rect, y, "Start", ref selEvt.startTime);
            y = DrawInspectorTimingRow(rect, y, "End",   ref selEvt.endTime);
            if (EditorGUI.EndChangeCheck())
                MarkDirty();
            float dur = selEvt.endTime - selEvt.startTime;
            DrawInspectorReadOnly(rect, ref y, "Duration", $"{dur:F3}s");
            y += 2f;

            // 프로퍼티 섹션 헤더
            Rect propHeaderRect = new Rect(rect.x, y, rect.width, 18f);
            EditorGUI.DrawRect(propHeaderRect, COL_SECTION_HEADER);
            EditorGUI.DrawRect(new Rect(rect.x, y, 3, 18f), new Color(0.6f, 0.65f, 0.75f));
            GUI.Label(propHeaderRect, "  PROPERTIES", new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.75f, 0.78f, 0.85f) },
                padding   = new RectOffset(8, 0, 2, 0),
                fontSize  = 10
            });
            y += 19f;

            // 프로퍼티 스크롤 영역 (GUILayout 기반)
            float remainH = rect.yMax - y;
            if (remainH > 10f)
            {
                Rect scrollViewRect = new Rect(rect.x, y, rect.width, remainH);
                // 내부 콘텐츠 너비: 스크롤바(14px) 제외
                float innerW        = rect.width - 16f;
                Rect contentRect    = new Rect(0, 0, innerW, 3000f);

                _inspectorScroll = GUI.BeginScrollView(scrollViewRect, _inspectorScroll, contentRect,
                    false, true); // 수평 스크롤 비활성, 수직만 활성
                GUILayout.BeginArea(new Rect(0, 0, innerW, 3000f));

                EditorGUIUtility.labelWidth = innerW * 0.52f;
                EditorGUI.indentLevel       = 0;
                DrawObjectFieldsInspector(selEvt);

                EditorGUIUtility.labelWidth = 0; // 기본값 복원
                GUILayout.EndArea();
                GUI.EndScrollView();
            }
        }

        // 인스펙터용 섹션 헤더 — 콜백 내에서 y를 갱신하도록 Action 대신 내용을 직접 그림
        float DrawInspectorSection(Rect panel, float y, string title, Color accentColor, System.Action drawContent)
        {
            // 섹션 헤더
            Rect headerRect = new Rect(panel.x, y, panel.width, 18f);
            EditorGUI.DrawRect(headerRect, COL_SECTION_HEADER);
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, 3, headerRect.height), accentColor);

            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.75f, 0.78f, 0.85f) },
                padding   = new RectOffset(8, 0, 2, 0),
                fontSize  = 10
            };
            GUI.Label(new Rect(headerRect.x + 4, headerRect.y, headerRect.width, headerRect.height),
                title.ToUpper(), style);

            y += 18f + 1f;
            drawContent?.Invoke();
            return y;
        }

        float DrawInspectorTimingRow(Rect panel, float y, string label, ref float value)
        {
            Rect rowRect = new Rect(panel.x, y, panel.width, 22f);
            if ((int)(y / 22f) % 2 == 0)
                EditorGUI.DrawRect(rowRect, COL_INSPECTOR_FIELD);

            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal    = { textColor = new Color(0.55f, 0.58f, 0.65f) },
                alignment = TextAnchor.MiddleLeft,
                padding   = new RectOffset(10, 0, 2, 0)
            };
            var valueStyle = new GUIStyle(EditorStyles.miniTextField)
            {
                normal    = { textColor = new Color(0.30f, 0.75f, 1.00f) },
                alignment = TextAnchor.MiddleRight,
                fontSize  = 10
            };

            GUI.Label(new Rect(panel.x, y, 80f, 22f), label, labelStyle);
            value = EditorGUI.FloatField(new Rect(panel.x + 80f, y + 2f, panel.width - 90f, 18f), value);

            return y + 22f;
        }

        void DrawInspectorReadOnly(Rect panel, ref float y, string label, string valueStr)
        {
            Rect rowRect = new Rect(panel.x, y, panel.width, 20f);
            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal  = { textColor = new Color(0.45f, 0.47f, 0.52f) },
                padding = new RectOffset(10, 0, 2, 0)
            };
            var valStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal    = { textColor = new Color(0.85f, 0.87f, 0.92f) },
                alignment = TextAnchor.MiddleRight,
                padding   = new RectOffset(0, 8, 2, 0)
            };
            GUI.Label(new Rect(panel.x, y, 100f, 20f), label, labelStyle);
            GUI.Label(new Rect(panel.x + 100f, y, panel.width - 108f, 20f), valueStr, valStyle);
            y += 20f;
        }

        // 인스펙터 패널 전용 — Rect 기반으로 필드를 그려 잘림 방지
        void DrawObjectFieldsInspector(object obj)
        {
            if (obj == null) return;
            var fields = obj.GetType().GetFields(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            float rowH  = 20f;
            float yPos  = 2f;  // BeginArea 기준 로컬 y
            float w     = EditorGUIUtility.currentViewWidth; // BeginArea 내 너비는 innerW로 이미 고정됨

            foreach (var field in fields)
            {
                if (field.Name == "startTime" || field.Name == "endTime") continue;

                var value     = field.GetValue(obj);
                var fieldType = field.FieldType;

                if (typeof(IList).IsAssignableFrom(fieldType))
                {
                    // 리스트는 GUILayout 기반 유지 (복잡도 때문)
                    DrawListProperty(field.Name, (IList)value, fieldType, obj, field);
                    yPos += rowH; // 대략적 추정 (리스트는 동적)
                }
                else
                {
                    DrawSingleField(obj, field.Name, value, fieldType, newVal =>
                    {
                        RecordUndo($"Change {field.Name}");
                        field.SetValue(obj, newVal);
                        MarkDirty();
                    });
                }
            }
        }

        void DrawPanelTitle(Rect rect, string text)
        {
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.70f, 0.73f, 0.80f) },
                padding   = new RectOffset(8, 0, 2, 0),
                fontSize  = 11
            };
            GUI.Label(rect, text, style);
        }

        MotionEventBase GetSelectedEvent(MotionSet set)
        {
            if (selectedEventIsSetEvent)
            {
                var list = set.globalEvents;
                if (list != null && selectedEventIndex >= 0 && selectedEventIndex < list.Count)
                    return list[selectedEventIndex];
                return null;
            }

            if (selectedEventMotionIndex < 0 || set.motions == null) return null;
            if (selectedEventMotionIndex >= set.motions.Count) return null;

            var motion = set.motions[selectedEventMotionIndex];
            if (motion.events == null || selectedEventIndex >= motion.events.Count) return null;
            return motion.events[selectedEventIndex];
        }

        // ====================================================================
        //  헤더
        // ====================================================================
        void DrawHeader(MotionSet set)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("애니메이션 셋", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUI.BeginChangeCheck();
                set.motionSetName = EditorGUILayout.TextField(set.motionSetName);
                if (EditorGUI.EndChangeCheck())
                    MarkDirty();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"{set.TotalDuration:F2}s", EditorStyles.miniLabel, GUILayout.Width(50));
            }
            EditorGUILayout.EndHorizontal();
        }

        // ====================================================================
        //  모션 리스트
        // ====================================================================
        void DrawMotionList(MotionSet set)
        {
            set.motions ??= new List<Motion>();

            EditorGUI.indentLevel++;
            for (int i = 0; i < set.motions.Count; i++)
            {
                var motion = set.motions[i];
                EditorGUILayout.BeginVertical(GUI.skin.box);
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        bool selected = selectedMotionIndex == i;
                        if (selected) GUI.backgroundColor = new Color(0.4f, 0.6f, 0.9f);

                        if (GUILayout.Button($"#{i}", GUILayout.Width(28)))
                        {
                            int previousIndex = selectedMotionIndex;
                            selectedMotionIndex = selectedMotionIndex == i ? -1 : i;
                            if (previousIndex != selectedMotionIndex)
                                _onSelectedMotionChanged?.Invoke(previousIndex, selectedMotionIndex);
                        }

                        GUI.backgroundColor = Color.white;

                        EditorGUI.BeginChangeCheck();
                        motion.motionName = EditorGUILayout.TextField(motion.motionName);
                        motion.motionClip = (AnimationClip)EditorGUILayout.ObjectField(
                            motion.motionClip, typeof(AnimationClip), false, GUILayout.Width(180));
                        if (EditorGUI.EndChangeCheck())
                            MarkDirty();

                        if (motion.IsValid())
                            EditorGUILayout.LabelField($"{motion.Duration:F2}s",
                                EditorStyles.miniLabel, GUILayout.Width(45));

                        // ② 위/아래 이동 버튼
                        EditorGUI.BeginDisabledGroup(i == 0);
                        if (GUILayout.Button("▲", GUILayout.Width(22)))
                        {
                            RecordUndo("Reorder Motion Up");
                            var tmp = set.motions[i];
                            set.motions[i] = set.motions[i - 1];
                            set.motions[i - 1] = tmp;
                            if (selectedMotionIndex == i) selectedMotionIndex = i - 1;
                            else if (selectedMotionIndex == i - 1) selectedMotionIndex = i;
                            MarkDirty();
                            break;
                        }

                        EditorGUI.EndDisabledGroup();

                        EditorGUI.BeginDisabledGroup(i == set.motions.Count - 1);
                        if (GUILayout.Button("▼", GUILayout.Width(22)))
                        {
                            RecordUndo("Reorder Motion Down");
                            var tmp = set.motions[i];
                            set.motions[i] = set.motions[i + 1];
                            set.motions[i + 1] = tmp;
                            if (selectedMotionIndex == i) selectedMotionIndex = i + 1;
                            else if (selectedMotionIndex == i + 1) selectedMotionIndex = i;
                            MarkDirty();
                            break;
                        }

                        EditorGUI.EndDisabledGroup();

                        if (GUILayout.Button("×", GUILayout.Width(22)))
                        {
                            RecordUndo("Remove Motion");
                            set.motions.RemoveAt(i);
                            if (selectedMotionIndex >= set.motions.Count) selectedMotionIndex = -1;
                            MarkDirty();
                            break;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    // 재생 구간 & 속도 설정 행
                    if (motion.IsValid())
                    {
                        float clipLen = motion.motionClip != null ? motion.motionClip.length : 0f;

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(30);

                        // ── 시작 시간 ──
                        EditorGUILayout.LabelField("시작", GUILayout.Width(50));
                        float rawStart = motion.clipStartTime >= 0f ? motion.clipStartTime : 0f;
                        float newStart = EditorGUILayout.FloatField(rawStart, GUILayout.Width(55));
                        newStart = Mathf.Clamp(newStart, 0f, clipLen);

                        // ── 종료 시간 ──
                        EditorGUILayout.LabelField("종료", GUILayout.Width(50));
                        float rawEnd = motion.clipEndTime >= 0f ? motion.clipEndTime : clipLen;
                        float newEnd = EditorGUILayout.FloatField(rawEnd, GUILayout.Width(55));
                        newEnd = Mathf.Clamp(newEnd, newStart + 0.001f, clipLen);

                        if (!Mathf.Approximately(newStart, rawStart) || !Mathf.Approximately(newEnd, rawEnd))
                        {
                            RecordUndo("Change Motion Clip Range");
                            // 전체 범위면 -1로 저장해 기본값 취급
                            motion.clipStartTime = Mathf.Approximately(newStart, 0f) ? -1f : newStart;
                            motion.clipEndTime = Mathf.Approximately(newEnd, clipLen) ? -1f : newEnd;
                            MarkDirty();
                        }

                        // 초기화 버튼
                        if (GUILayout.Button("초기화", GUILayout.Width(50)))
                        {
                            RecordUndo("Reset Motion Clip Range");
                            motion.clipStartTime = -1f;
                            motion.clipEndTime = -1f;
                            MarkDirty();
                        }

                        GUILayout.Space(10);

                        // ── 재생 속도 ──
                        EditorGUILayout.LabelField("속도", GUILayout.Width(50));
                        float newSpd = EditorGUILayout.FloatField(motion.playbackSpeed, GUILayout.Width(45));
                        newSpd = Mathf.Max(0.01f, newSpd);
                        if (!Mathf.Approximately(newSpd, motion.playbackSpeed))
                        {
                            RecordUndo("Change Motion Playback Speed");
                            motion.playbackSpeed = newSpd;
                            MarkDirty();
                        }

                        // 클립 정보 표시
                        if (clipLen > 0f)
                        {
                            float shownStart = motion.clipStartTime >= 0f ? motion.clipStartTime : 0f;
                            float shownEnd = motion.clipEndTime >= 0f ? motion.clipEndTime : clipLen;
                            EditorGUILayout.LabelField(
                                $"[{shownStart:F2}~{shownEnd:F2}] / {clipLen:F2}s → {motion.Duration:F2}s",
                                EditorStyles.miniLabel, GUILayout.MinWidth(50));
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    // ⑦ 이벤트 리스트: 선택된 이벤트 강조
                    if (selectedMotionIndex == i) DrawMotionEvents(motion, i);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 클립 추가", GUILayout.Width(120)))
            {
                RecordUndo("Add Motion");
                set.motions.Add(new Motion
                {
                    motionName = $"Motion_{set.motions.Count}",
                    events = new List<MotionEventBase>()
                });
                MarkDirty();
            }

            EditorGUILayout.EndHorizontal();
        }

        // ====================================================================
        //  모션별 이벤트 (개별 접힘/펼침 + 색상 배지)
        // ====================================================================
        void DrawMotionEvents(Motion motion, int motionIdx)
        {
            motion.events ??= new List<MotionEventBase>();

            EditorGUI.indentLevel++;
            DrawAttachedEventFilterBar("이벤트", motion.events.Count, CountFilteredEvents(motion.events));

            for (int i = 0; i < motion.events.Count; i++)
            {
                var evt = motion.events[i];
                if (evt == null) continue;
                if (!MatchesEventFilter(evt)) continue;

                string foldKey = EventKey(motionIdx, i);
                bool isOpen = GetEventFold(foldKey);
                var visual = MotionEventStyle.Get(evt);

                EditorGUILayout.BeginVertical(GUI.skin.box);
                {
                    // ── 헤더 행 ──
                    EditorGUILayout.BeginHorizontal();
                    {
                        // 컬러 배지
                        Rect badgeRect = GUILayoutUtility.GetRect(4, 18, GUILayout.Width(4));
                        EditorGUI.DrawRect(badgeRect, visual.color);
                        GUILayout.Space(4);

                        // 접힘 토글 화살표 + 이름 (클릭 영역 전체)
                        string arrow = isOpen ? "▼" : "▶";
                        var labelStyle = new GUIStyle(EditorStyles.boldLabel)
                        {
                            normal = { textColor = visual.color }
                        };
                        if (GUILayout.Button($"{arrow} {visual.icon} {evt.GetDisplayName()}",
                            labelStyle, GUILayout.MinWidth(120)))
                        {
                            SetEventFold(foldKey, !isOpen);
                        }

                        GUILayout.FlexibleSpace();

                        GUILayout.Label("Start", GUILayout.Width(35));
                        EditorGUI.BeginChangeCheck();
                        evt.startTime = EditorGUILayout.FloatField(evt.startTime, GUILayout.Width(55));
                        GUILayout.Space(4);
                        GUILayout.Label("End", GUILayout.Width(30));
                        evt.endTime = EditorGUILayout.FloatField(evt.endTime, GUILayout.Width(55));
                        if (EditorGUI.EndChangeCheck())
                            MarkDirty();

                        if (GUILayout.Button("⋮", GUILayout.Width(22)))
                            ShowEventContextMenu(motion.events, i, false, motionIdx);

                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                        if (GUILayout.Button("×", GUILayout.Width(22)))
                        {
                            GUI.backgroundColor = Color.white;
                            RecordUndo("Remove Motion Event");
                            motion.events.RemoveAt(i);
                            _eventFoldouts.Remove(foldKey);
                            if (selectedEventMotionIndex == motionIdx && selectedEventIndex == i)
                                ClearEventSelection();
                            MarkDirty();
                            break;
                        }
                        GUI.backgroundColor = Color.white;
                    }
                    EditorGUILayout.EndHorizontal();

                    // ── 프로퍼티 (펼쳐진 경우만) ──
                    if (isOpen)
                    {
                        EditorGUI.indentLevel++;
                        DrawEventProperties(evt);
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUILayout.EndVertical();
            }

            if (motion.events.Count > 0 && CountFilteredEvents(motion.events) == 0)
                EditorGUILayout.HelpBox("필터 조건에 맞는 이벤트가 없습니다.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 이벤트", GUILayout.Width(80)))
            {
                MotionEventMenuHelper.ShowAddEventMenu(
                    motion.events,
                    0f,
                    () => RecordUndo("Add Motion Event"),
                    () =>
                    {
                        // 새로 추가된 이벤트는 바로 펼쳐진 상태로
                        SetEventFold(EventKey(motionIdx, motion.events.Count - 1), true);
                        MarkDirty();
                        Repaint();
                    });
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        // ====================================================================
        //  ⑧ 이벤트 컨텍스트 메뉴
        // ====================================================================
        void ShowEventContextMenu(List<MotionEventBase> eventList, int eventIdx, bool isSetEvent, int motionIdx)
        {
            var menu = new GenericMenu();
            var evt = eventList[eventIdx];

            // 복사
            menu.AddItem(new GUIContent("복사 (Copy)"), false, () => { _copiedEvent = CloneEvent(evt); });

            // 붙여넣기
            if (_copiedEvent != null)
            {
                menu.AddItem(new GUIContent("붙여넣기 (Paste)"), false, () =>
                {
                    RecordUndo("Paste Event");
                    var pasted = CloneEvent(_copiedEvent);
                    pasted.startTime = evt.startTime + 0.05f;
                    pasted.endTime = evt.endTime + 0.05f;
                    eventList.Insert(eventIdx + 1, pasted);
                    MarkDirty();
                    Repaint();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("붙여넣기 (Paste)"));
            }

            menu.AddSeparator("");

            // 복제
            menu.AddItem(new GUIContent("복제 (Duplicate)"), false, () =>
            {
                RecordUndo("Duplicate Event");
                var dup = CloneEvent(evt);
                dup.startTime = evt.startTime + 0.05f;
                dup.endTime = evt.endTime + 0.05f;
                eventList.Insert(eventIdx + 1, dup);
                MarkDirty();
                Repaint();
            });

            menu.AddSeparator("");

            // 삭제
            menu.AddItem(new GUIContent("삭제 (Delete)"), false, () =>
            {
                RecordUndo("Delete Event");
                eventList.RemoveAt(eventIdx);
                if (!isSetEvent && selectedEventMotionIndex == motionIdx && selectedEventIndex == eventIdx)
                    ClearEventSelection();
                MarkDirty();
                Repaint();
            });

            menu.ShowAsContext();
        }

        MotionEventBase CloneEvent(MotionEventBase src)
        {
            // JSON 직렬화를 통한 딥 클론
            string json = JsonUtility.ToJson(src);
            var clone = (MotionEventBase)Activator.CreateInstance(src.GetType());
            JsonUtility.FromJsonOverwrite(json, clone);
            return clone;
        }

        void ClearEventSelection()
        {
            selectedEventMotionIndex = -1;
            selectedEventIndex = -1;
            selectedEventIsSetEvent = false;
        }

        void DrawEventProperties(MotionEventBase evt)
        {
            // 리플렉션을 통해 모든 필드를 가져와 순회하며 그립니다.
            DrawObjectFields(evt);
        }

        void DrawAttachedEventFilterBar(string label, int totalCount, int filteredCount)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel, GUILayout.Width(80));

                GUILayout.Label("필터", GUILayout.Width(32));
                EditorGUI.BeginChangeCheck();
                _eventFilterText = EditorGUILayout.TextField(
                    _eventFilterText,
                    GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField);
                if (EditorGUI.EndChangeCheck())
                    Repaint();

                if (!string.IsNullOrWhiteSpace(_eventFilterText))
                {
                    if (GUILayout.Button("×", GUILayout.Width(22)))
                    {
                        _eventFilterText = string.Empty;
                        Repaint();
                    }
                }

                EditorGUILayout.LabelField($"{filteredCount}/{totalCount}", EditorStyles.miniLabel, GUILayout.Width(52));
            }
            EditorGUILayout.EndHorizontal();
        }

        int CountFilteredEvents(List<MotionEventBase> events)
        {
            if (events == null) return 0;

            int count = 0;
            for (int i = 0; i < events.Count; i++)
            {
                var evt = events[i];
                if (evt != null && MatchesEventFilter(evt))
                    count++;
            }
            return count;
        }

        bool MatchesEventFilter(MotionEventBase evt)
        {
            if (evt == null) return false;
            if (string.IsNullOrWhiteSpace(_eventFilterText)) return true;

            string query = _eventFilterText.Trim();
            return ContainsIgnoreCase(evt.GetDisplayName(), query) ||
                   ContainsIgnoreCase(evt.GetShortLabel(), query) ||
                   ContainsIgnoreCase(evt.GetType().Name, query);
        }

        static bool ContainsIgnoreCase(string text, string query)
        {
            return !string.IsNullOrEmpty(text) &&
                   text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 객체의 필드를 분석하여 UI로 그리는 범용 메서드 (재귀 지원)
        /// </summary>
        void DrawObjectFields(object obj)
        {
            if (obj == null) return;

            var type = obj.GetType();
            var fields =
                type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                // 이벤트 공통 필드는 제외
                if (field.Name == "startTime" || field.Name == "endTime") continue;

                var value = field.GetValue(obj);
                var fieldType = field.FieldType;

                // 1. 리스트 또는 배열인 경우 처리
                if (typeof(IList).IsAssignableFrom(fieldType))
                {
                    DrawListProperty(field.Name, (IList)value, fieldType, obj, field);
                }
                // 2. 일반 단일 필드인 경우 처리
                else
                {
                    DrawSingleField(obj, field.Name, value, fieldType, (newValue) =>
                    {
                        RecordUndo($"Change {field.Name}");
                        field.SetValue(obj, newValue);
                        MarkDirty();
                    });
                }
            }
        }

        /// <summary>
        /// 리스트(IList)를 위한 UI를 그립니다 (추가/삭제 기능 포함)
        /// </summary>
        void DrawListProperty(string label, IList list, Type listType, object owner, System.Reflection.FieldInfo field)
        {
            if (list == null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, "List is Null");
                if (GUILayout.Button("Create List", GUILayout.Width(100)))
                {
                    RecordUndo($"Create {label} List");
                    var newList = Activator.CreateInstance(listType);
                    field.SetValue(owner, newList);
                    MarkDirty();
                }

                EditorGUILayout.EndHorizontal();
                return;
            }

            // 리스트 헤더와 추가 버튼
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{label} (Count: {list.Count})", EditorStyles.boldLabel);

            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                RecordUndo($"Add element to {label}");
                Type elementType = listType.IsArray ? listType.GetElementType() : listType.GetGenericArguments()[0];

                // ScriptableObject/Component 등 UnityEngine.Object 파생 타입은
                // Activator로 생성 불가 → null(빈 슬롯)로 추가 후 인스펙터에서 드래그 할당
                object newItem;
                if (typeof(UnityEngine.Object).IsAssignableFrom(elementType))
                    newItem = null;
                else if (elementType == typeof(string))
                    newItem = "";
                else
                    newItem = Activator.CreateInstance(elementType);

                if (listType.IsArray)
                {
                    Array newArray = Array.CreateInstance(elementType, list.Count + 1);
                    list.CopyTo(newArray, 0);
                    newArray.SetValue(newItem, list.Count);
                    field.SetValue(owner, newArray);
                }
                else
                {
                    list.Add(newItem);
                }

                MarkDirty();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                // 요소 삭제 버튼
                if (GUILayout.Button("x", GUILayout.Width(20)))
                {
                    RecordUndo($"Remove element from {label}");
                    if (listType.IsArray)
                    {
                        Type elementType = listType.GetElementType();
                        Array newArray = Array.CreateInstance(elementType, list.Count - 1);
                        int destIdx = 0;
                        for (int srcIdx = 0; srcIdx < list.Count; srcIdx++)
                        {
                            if (srcIdx == i) continue;
                            newArray.SetValue(list[srcIdx], destIdx++);
                        }

                        field.SetValue(owner, newArray);
                    }
                    else
                    {
                        list.RemoveAt(i);
                    }

                    MarkDirty();
                    break;
                }

                // 요소 내용 그리기
                object item = list[i];
                Type elementType2 = listType.IsArray
                    ? listType.GetElementType()
                    : listType.GetGenericArguments()[0];

                if (item == null)
                {
                    // UnityEngine.Object 파생 타입 → ObjectField로 null 슬롯 표시
                    if (typeof(UnityEngine.Object).IsAssignableFrom(elementType2))
                    {
                        DrawSingleField(null, $"[{i}]", null, elementType2, (newVal) =>
                        {
                            list[i] = newVal;
                            MarkDirty();
                        });
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"[{i}]", "(null)");
                    }
                }
                else
                {
                    Type itemType = item.GetType();

                    // 프리미티브 타입이면 바로 그리고, 복합 객체면 재귀적으로 필드를 그립니다.
                    if (itemType.IsPrimitive || itemType == typeof(string) ||
                        typeof(UnityEngine.Object).IsAssignableFrom(itemType))
                    {
                        DrawSingleField(null, $"[{i}]", item, itemType, (newVal) =>
                        {
                            list[i] = newVal;
                            MarkDirty();
                        });
                    }
                    else
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        DrawObjectFields(item); // [재귀 호출] SpawnTargetData 내부 필드들을 그립니다.
                        EditorGUILayout.EndVertical();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 단일 필드 드로잉 로직
        /// </summary>
        void DrawSingleField(object owner, string label, object value, Type fieldType, Action<object> onValueChanged)
        {
            EditorGUI.BeginChangeCheck();
            object newValue = value;

            if (fieldType == typeof(float)) newValue = EditorGUILayout.FloatField(label, (float)value);
            else if (fieldType == typeof(int)) newValue = EditorGUILayout.IntField(label, (int)value);
            else if (fieldType == typeof(string)) newValue = EditorGUILayout.TextField(label, (string)value);
            else if (fieldType == typeof(bool)) newValue = EditorGUILayout.Toggle(label, (bool)value);
            else if (fieldType == typeof(Vector3))
            {
                if (MotionEventOffsetFieldUtil.IsLocalOffset(owner, label))
                    newValue = DrawLocalOffsetField(MotionEventOffsetFieldUtil.GetLocalOffsetSpaceLabel(owner), (Vector3)value);
                else if (MotionEventOffsetFieldUtil.IsRotationOffset(owner, label))
                    newValue = DrawRotationOffsetField(MotionEventOffsetFieldUtil.GetRotationOffsetSpaceLabel(owner), (Vector3)value);
                else
                    newValue = EditorGUILayout.Vector3Field(label, (Vector3)value);
            }
            else if (fieldType == typeof(AnimationCurve))
                newValue = EditorGUILayout.CurveField(label, (AnimationCurve)value);
            else if (fieldType == typeof(LayerMask))
            {
                newValue = (LayerMask)EditorGUILayout.MaskField(label, ((LayerMask)value).value,
                    UnityEditorInternal.InternalEditorUtility.layers);
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            {
                newValue = EditorGUILayout.ObjectField(label, (UnityEngine.Object)value, fieldType, false);
            }
            else if (fieldType.IsEnum)
            {
                newValue = EditorGUILayout.EnumPopup(label, (System.Enum)value);
            }

            if (EditorGUI.EndChangeCheck())
            {
                onValueChanged?.Invoke(newValue);
            }
        }

        static Vector3 DrawLocalOffsetField(string label, Vector3 value)
        {
            EditorGUILayout.LabelField($"Position Offset ({label})", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            value.x = EditorGUILayout.FloatField($"{label} Right / X", value.x);
            value.y = EditorGUILayout.FloatField($"{label} Up / Y", value.y);
            value.z = EditorGUILayout.FloatField($"{label} Forward / Z", value.z);
            EditorGUI.indentLevel--;

            if (GUILayout.Button("Reset Offset", GUILayout.Height(20)))
                value = Vector3.zero;

            return value;
        }

        static Vector3 DrawRotationOffsetField(string label, Vector3 value)
        {
            EditorGUILayout.LabelField($"Rotation ({label})", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            value.x = EditorGUILayout.FloatField("Pitch / X", value.x);
            value.y = EditorGUILayout.FloatField("Yaw / Y", value.y);
            value.z = EditorGUILayout.FloatField("Roll / Z", value.z);
            EditorGUI.indentLevel--;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Rotation", GUILayout.Height(20)))
                    value = Vector3.zero;

                if (GUILayout.Button("Flip Forward", GUILayout.Height(20)))
                    value.y = MotionEventOffsetFieldUtil.NormalizeAngle(value.y + 180f);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Roll +90", GUILayout.Height(20)))
                    value.z = MotionEventOffsetFieldUtil.NormalizeAngle(value.z + 90f);

                if (GUILayout.Button("Roll -90", GUILayout.Height(20)))
                    value.z = MotionEventOffsetFieldUtil.NormalizeAngle(value.z - 90f);
            }

            return value;
        }

        // ====================================================================
        //  MotionSet 글로벌 이벤트 (개별 접힘/펼침 + 색상 배지)
        // ====================================================================
        void DrawMotionSetEvents(MotionSet set)
        {
            set.globalEvents ??= new List<MotionEventBase>();

            EditorGUI.indentLevel++;
            DrawAttachedEventFilterBar("글로벌 이벤트", set.globalEvents.Count, CountFilteredEvents(set.globalEvents));

            for (int i = 0; i < set.globalEvents.Count; i++)
            {
                var evt = set.globalEvents[i];
                if (evt == null) continue;
                if (!MatchesEventFilter(evt)) continue;

                string foldKey = SetEventKey(i);
                bool isOpen = GetEventFold(foldKey);
                var visual = MotionEventStyle.Get(evt);

                EditorGUILayout.BeginVertical(GUI.skin.box);
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        Rect badgeRect = GUILayoutUtility.GetRect(4, 18, GUILayout.Width(4));
                        EditorGUI.DrawRect(badgeRect, visual.color);
                        GUILayout.Space(4);

                        string arrow = isOpen ? "▼" : "▶";
                        var labelStyle = new GUIStyle(EditorStyles.boldLabel)
                        {
                            normal = { textColor = visual.color }
                        };
                        if (GUILayout.Button($"{arrow} {visual.icon} {evt.GetDisplayName()}",
                            labelStyle, GUILayout.MinWidth(120)))
                        {
                            SetEventFold(foldKey, !isOpen);
                        }

                        GUILayout.FlexibleSpace();

                        GUILayout.Label("Start", GUILayout.Width(35));
                        EditorGUI.BeginChangeCheck();
                        evt.startTime = EditorGUILayout.FloatField(evt.startTime, GUILayout.Width(55));
                        GUILayout.Space(4);
                        GUILayout.Label("End", GUILayout.Width(30));
                        evt.endTime = EditorGUILayout.FloatField(evt.endTime, GUILayout.Width(55));
                        if (EditorGUI.EndChangeCheck())
                            MarkDirty();

                        if (GUILayout.Button("⋮", GUILayout.Width(22)))
                            ShowEventContextMenu(set.globalEvents, i, true, -1);

                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                        if (GUILayout.Button("×", GUILayout.Width(22)))
                        {
                            GUI.backgroundColor = Color.white;
                            RecordUndo("Remove MotionSet Event");
                            set.globalEvents.RemoveAt(i);
                            _eventFoldouts.Remove(foldKey);
                            if (selectedEventIsSetEvent && selectedEventIndex == i)
                                ClearEventSelection();
                            MarkDirty();
                            break;
                        }
                        GUI.backgroundColor = Color.white;
                    }
                    EditorGUILayout.EndHorizontal();

                    if (isOpen)
                    {
                        EditorGUI.indentLevel++;
                        DrawEventProperties(evt);
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUILayout.EndVertical();
            }

            if (set.globalEvents.Count > 0 && CountFilteredEvents(set.globalEvents) == 0)
                EditorGUILayout.HelpBox("필터 조건에 맞는 이벤트가 없습니다.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 이벤트", GUILayout.Width(80)))
            {
                MotionEventMenuHelper.ShowAddEventMenu(
                    set.globalEvents,
                    0f,
                    () => RecordUndo("Add MotionSet Event"),
                    () =>
                    {
                        SetEventFold(SetEventKey(set.globalEvents.Count - 1), true);
                        MarkDirty();
                        Repaint();
                    });
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        // ====================================================================
        //  타임라인 전체 (Rect 기반 — 2단 레이아웃에서 호출)
        // ====================================================================
        void DrawTimeline(Rect fullRect, MotionSet set)
        {
            float totalDur = set.TotalDuration;
            if (totalDur <= 0f) return;

            float pps = BASE_PPS * zoom;
            EditorGUI.DrawRect(fullRect, COL_BG);

            DrawZoomControl(new Rect(fullRect.x, fullRect.y, fullRect.width, 18f), totalDur);

            Rect content = new Rect(fullRect.x, fullRect.y + 20f, fullRect.width, fullRect.height - 20f);
            float labelW    = LABEL_WIDTH;
            float trackW    = content.width - labelW;
            float timelineW = totalDur * pps;
            scrollX = Mathf.Clamp(scrollX, 0, Mathf.Max(0, timelineW - trackW));

            float y = content.y;

            // ── 룰러 ──
            Rect rulerRect = new Rect(content.x + labelW, y, trackW, RULER_HEIGHT);
            DrawRuler(rulerRect, totalDur, pps);
            y += RULER_HEIGHT + TRACK_GAP;

            // ── 몽타주 그룹 ──
            y = DrawGroupHeader(content.x, y, content.width, "몽타주", new Color(0.30f, 0.55f, 0.35f));
            if (set.motions != null)
            {
                float tOff = 0f;
                for (int i = 0; i < set.motions.Count; i++)
                {
                    DrawTrackLabel(new Rect(content.x, y, labelW, TRACK_HEIGHT),
                        set.motions[i].motionName ?? $"Motion {i}",
                        COL_MOTION_CLIPS[i % COL_MOTION_CLIPS.Length]);
                    DrawMotionClipBar(new Rect(content.x + labelW, y, trackW, TRACK_HEIGHT),
                        set.motions[i], i, tOff, pps);
                    tOff += set.motions[i].Duration;
                    y    += TRACK_HEIGHT + TRACK_GAP;
                }
            }
            y += SECTION_GAP;

            // ── 타이밍 그룹 ──
            y = DrawGroupHeader(content.x, y, content.width, "타이밍", new Color(0.85f, 0.30f, 0.30f));
            DrawTrackLabel(new Rect(content.x, y, labelW, MARKER_HEIGHT), "전환점",
                new Color(0.85f, 0.30f, 0.30f));
            DrawTimingMarkers(new Rect(content.x + labelW, y, trackW, MARKER_HEIGHT), set, pps);
            y += MARKER_HEIGHT + TRACK_GAP + SECTION_GAP;

            // ── 노티파이 그룹 ──
            y = DrawGroupHeader(content.x, y, content.width, "노티파이", new Color(0.40f, 0.55f, 0.90f));
            DrawEventTracks(content.x, content.x + labelW, y, labelW, trackW, set, pps);
            int totalEventTracks = CountEventTracks(set);
            float eventsEndY = y + (EVENT_HEIGHT + TRACK_GAP) * Mathf.Max(totalEventTracks, 1);

            // ── 외부 주입 오버레이 그룹 (전투 데이터 등) ──
            float contentEndY = eventsEndY;
            if (overlayTracks != null && overlayTracks.Count > 0)
            {
                float oy = eventsEndY + SECTION_GAP;
                oy = DrawGroupHeader(content.x, oy, content.width, overlayGroupTitle, new Color(0.95f, 0.55f, 0.25f));
                foreach (OverlayTrack track in overlayTracks)
                {
                    if (track == null) continue;
                    DrawEventTrackLabel(new Rect(content.x, oy, labelW, EVENT_HEIGHT), track.label, track.color);
                    DrawOverlayTrackBar(new Rect(content.x + labelW, oy, trackW, EVENT_HEIGHT), track, pps);
                    oy += EVENT_HEIGHT + TRACK_GAP;
                }
                contentEndY = oy;
            }

            // ── 커서 & 오버레이 ──
            Rect cursorArea = new Rect(content.x + labelW, content.y + RULER_HEIGHT,
                trackW, contentEndY - content.y - RULER_HEIGHT);
            DrawCursor(cursorArea, totalDur, pps);
            HandleCursorInput(rulerRect, totalDur, pps);
            DrawPlayRangeOverlay(cursorArea, totalDur, pps);

            HandleScroll(content, timelineW, trackW);
        }

        // 그룹 헤더 (색상 악센트 + 제목) — y를 반환
        float DrawGroupHeader(float x, float y, float width, string title, Color accentColor)
        {
            Rect headerRect = new Rect(x, y, width, GROUP_HEADER_H);
            EditorGUI.DrawRect(headerRect, COL_GROUP_HEADER_BG);
            EditorGUI.DrawRect(new Rect(x, y, 3, GROUP_HEADER_H), accentColor);
            EditorGUI.DrawRect(new Rect(x, y, width, 1), COL_DIVIDER);
            EditorGUI.DrawRect(new Rect(x, y + GROUP_HEADER_H - 1, width, 1), COL_DIVIDER);

            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.75f, 0.78f, 0.85f) },
                padding   = new RectOffset(10, 0, 2, 0),
                fontSize  = 10
            };
            GUI.Label(new Rect(x, y, width, GROUP_HEADER_H), $"▼  {title.ToUpper()}", style);
            return y + GROUP_HEADER_H + TRACK_GAP;
        }

        int CountEventGroupCount(MotionSet set)
        {
            var types = new System.Collections.Generic.HashSet<System.Type>();
            if (set.globalEvents != null)
                foreach (var e in set.globalEvents) if (e != null) types.Add(e.GetType());
            if (set.motions != null)
                foreach (var m in set.motions)
                    if (m.events != null)
                        foreach (var e in m.events) if (e != null) types.Add(e.GetType());
            return types.Count;
        }

        // ====================================================================
        //  ④ 재생 구간 오버레이
        // ====================================================================
        void DrawPlayRangeOverlay(Rect area, float totalDur, float pps)
        {
            float rangeStart = playRangeStart;
            float rangeEnd   = playRangeEnd > 0f ? playRangeEnd : totalDur;

            if (Mathf.Approximately(rangeStart, 0f) && Mathf.Approximately(rangeEnd, totalDur))
                return; // 전체 구간이면 오버레이 생략

            float x0 = rangeStart * pps - scrollX + area.x;
            float x1 = rangeEnd   * pps - scrollX + area.x;

            x0 = Mathf.Clamp(x0, area.x, area.xMax);
            x1 = Mathf.Clamp(x1, area.x, area.xMax);

            // 반투명 초록 오버레이
            EditorGUI.DrawRect(new Rect(x0, area.y, x1 - x0, area.height), COL_RANGE_OVERLAY);
            // 시작/끝 경계선
            EditorGUI.DrawRect(new Rect(x0 - 1, area.y, 2, area.height), COL_RANGE_BORDER);
            EditorGUI.DrawRect(new Rect(x1 - 1, area.y, 2, area.height), COL_RANGE_BORDER);
        }

        // ====================================================================
        //  ① 줌 + fps 토글
        // ====================================================================
        void DrawZoomControl(Rect r, float totalDur)
        {
            EditorGUI.LabelField(new Rect(r.x, r.y, 40, r.height), "줌", EditorStyles.miniLabel);
            zoom = GUI.HorizontalSlider(new Rect(r.x + 35, r.y + 2, 120, r.height), zoom, 0.2f, 10f);
            EditorGUI.LabelField(new Rect(r.x + 160, r.y, 60, r.height),
                $"×{zoom:F1}", EditorStyles.miniLabel);

            // ① fps 단위 토글
            bool newShowFrames = GUI.Toggle(new Rect(r.x + 225, r.y, 55, r.height),
                showFrames, "F단위", EditorStyles.miniButton);
            if (newShowFrames != showFrames)
            {
                showFrames = newShowFrames;
                Repaint();
            }

            if (showFrames)
            {
                // fps 입력 필드 (라벨 + IntField)
                EditorGUI.LabelField(new Rect(r.x + 285, r.y, 25, r.height), "fps", EditorStyles.miniLabel);
                fps = EditorGUI.IntField(new Rect(r.x + 308, r.y, 35, r.height), fps);
                fps = Mathf.Clamp(fps, 1, 120);
            }

            float pct = totalDur > 0 ? cursorTime / totalDur * 100f : 0;
            // ① 커서 시간: 초 또는 프레임 단위
            string cursorStr = showFrames
                ? $"커서: F{Mathf.RoundToInt(cursorTime * fps)} ({pct:F1}%)"
                : $"커서: {cursorTime:F2}s ({pct:F1}%)";
            EditorGUI.LabelField(new Rect(r.x + 350, r.y, 180, r.height), cursorStr, EditorStyles.miniLabel);
        }

        // ====================================================================
        //  ① 룰러 (fps 지원)
        // ====================================================================
        void DrawRuler(Rect rect, float totalDur, float pps)
        {
            EditorGUI.DrawRect(rect, COL_RULER);
            GUI.BeginClip(rect);

            float step        = GetRulerStep(pps);
            float startTime   = scrollX / pps;
            float startSnap   = Mathf.Floor(startTime / step) * step;
            var   labelStyle  = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = COL_RULER_TEXT } };

            for (float t = startSnap; t <= totalDur + step * 0.5f; t += step)
            {
                float x = t * pps - scrollX;
                if (x < -20 || x > rect.width + 20) continue;
                EditorGUI.DrawRect(new Rect(x, rect.height - 8, 1, 8), COL_RULER_LINE);

                // ① 프레임 또는 초 단위 라벨
                string label = showFrames
                    ? $"F{Mathf.RoundToInt(t * fps)}"
                    : $"{t:F2}s";
                GUI.Label(new Rect(x + 2, 0, 55, rect.height), label, labelStyle);
            }

            float sub = step / 5f;
            for (float t = startSnap; t <= totalDur; t += sub)
            {
                float x = t * pps - scrollX;
                if (x >= 0 && x <= rect.width)
                    EditorGUI.DrawRect(new Rect(x, rect.height - 4, 1, 4), COL_RULER_LINE * 0.6f);
            }

            GUI.EndClip();
        }

        static float GetRulerStep(float pps)
        {
            float[] steps = { 0.1f, 0.2f, 0.5f, 1f, 2f, 5f, 10f };
            foreach (var s in steps)
                if (s * pps >= 50f) return s;
            return 10f;
        }

        // ====================================================================
        //  라벨
        // ====================================================================
        void DrawSectionLabel(Rect rect, string text)
        {
            var style = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                normal   = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                fontSize = 10
            };
            EditorGUI.LabelField(rect, $"▼ {text}", style);
        }

        void DrawTrackLabel(Rect rect, string text, Color accentColor = default)
        {
            EditorGUI.DrawRect(rect, COL_LABEL_BG);
            // 좌측 악센트 바 (컬러가 있을 때만)
            if (accentColor != default)
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2, rect.height), accentColor);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), COL_LABEL_BORDER);

            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding   = new RectOffset(accentColor != default ? 8 : 6, 4, 0, 0),
                normal    = { textColor = new Color(0.80f, 0.82f, 0.87f) },
                clipping  = TextClipping.Clip
            };
            GUI.Label(rect, text, style);
        }

        // ====================================================================
        //  ⑥ 모션 클립 바 (드래그 핸들 포함)
        // ====================================================================
        void DrawMotionClipBar(Rect trackRect, Motion motion, int idx, float tOff, float pps)
        {
            EditorGUI.DrawRect(trackRect, COL_TRACK_BG);
            GUI.BeginClip(trackRect);

            float x0 = tOff * pps - scrollX;
            float w  = motion.Duration * pps;

            if (x0 + w > 0 && x0 < trackRect.width)
            {
                Color barColor = COL_MOTION_CLIPS[idx % COL_MOTION_CLIPS.Length];
                Rect  bar      = new Rect(x0, 2, w, trackRect.height - 4);

                bool hasCustomRange = motion.motionClip != null &&
                                      (motion.clipStartTime >= 0f || motion.clipEndTime >= 0f);

                // 기본 바 채우기
                EditorGUI.DrawRect(bar, barColor);

                // 구간 잘림 표시 (노란 경계선)
                if (hasCustomRange && motion.motionClip != null)
                {
                    float clipLen    = motion.motionClip.length;
                    bool  hasCutStart = motion.clipStartTime > 0.001f;
                    bool  hasCutEnd   = motion.clipEndTime >= 0f &&
                                        motion.clipEndTime < clipLen - 0.001f;

                    Color borderColor = new Color(1f, 0.9f, 0.2f, 1f);
                    if (hasCutStart)
                        EditorGUI.DrawRect(new Rect(x0,         2, 2, trackRect.height - 4), borderColor);
                    if (hasCutEnd)
                        EditorGUI.DrawRect(new Rect(x0 + w - 2, 2, 2, trackRect.height - 4), borderColor);
                }

                // 클립 이름 라벨
                string name = motion.motionClip != null ? motion.motionClip.name : motion.motionName;
                var style = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal    = { textColor = Color.white },
                    fontStyle = FontStyle.Bold,
                    clipping  = TextClipping.Clip,
                    padding   = new RectOffset(6, 6, 0, 0)
                };
                GUI.Label(new Rect(bar.x + 4, bar.y, bar.width - 8, bar.height), name, style);

                // 속도 배율 표시
                if (!Mathf.Approximately(motion.playbackSpeed, 1f))
                {
                    var speedStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleRight,
                        normal    = { textColor = new Color(1f, 1f, 0.5f) },
                        fontSize  = 9,
                        padding   = new RectOffset(0, 4, 0, 0)
                    };
                    GUI.Label(new Rect(bar.x, bar.y, bar.width, bar.height),
                        $"×{motion.playbackSpeed:F1}", speedStyle);
                }

                // ⑥ 클립 시작/끝 핸들 (클립이 있을 때만)
                if (motion.motionClip != null)
                {
                    float clipLen = motion.motionClip.length;
                    DrawClipHandles(bar, trackRect, motion, idx, tOff, pps, clipLen);
                }
            }

            GUI.EndClip();
        }

        // ⑥ 클립 핸들 그리기 + 드래그 처리
        void DrawClipHandles(Rect bar, Rect trackRect, Motion motion, int idx, float tOff, float pps, float clipLen)
        {
            // 핸들 사각형: 바의 왼쪽/오른쪽 끝 CLIP_HANDLE_W 너비
            Rect leftHandle  = new Rect(bar.x,                  bar.y, CLIP_HANDLE_W, bar.height);
            Rect rightHandle = new Rect(bar.x + bar.width - CLIP_HANDLE_W, bar.y, CLIP_HANDLE_W, bar.height);

            // 핸들 시각화
            Color handleColor = (_clipHandleMotionIndex == idx && _clipHandleDraggingStart)
                ? new Color(1f, 1f, 0f, 1f)   // 드래그 중이면 밝은 노랑
                : COL_CLIP_HANDLE;
            EditorGUI.DrawRect(leftHandle, handleColor);

            handleColor = (_clipHandleMotionIndex == idx && _clipHandleDraggingEnd)
                ? new Color(1f, 1f, 0f, 1f)
                : COL_CLIP_HANDLE;
            EditorGUI.DrawRect(rightHandle, handleColor);

            // 핸들 아이콘 (작은 수직선 3개)
            DrawHandleLines(leftHandle);
            DrawHandleLines(rightHandle);

            // 드래그 이벤트 처리
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (leftHandle.Contains(e.mousePosition))
                {
                    _clipHandleMotionIndex  = idx;
                    _clipHandleDraggingStart = true;
                    _clipHandleDraggingEnd   = false;
                    RecordUndo("Drag Clip Start");
                    e.Use();
                }
                else if (rightHandle.Contains(e.mousePosition))
                {
                    _clipHandleMotionIndex  = idx;
                    _clipHandleDraggingStart = false;
                    _clipHandleDraggingEnd   = true;
                    RecordUndo("Drag Clip End");
                    e.Use();
                }
            }

            if ((_clipHandleDraggingStart || _clipHandleDraggingEnd) &&
                _clipHandleMotionIndex == idx && e.type == EventType.MouseDrag)
            {
                // 마우스 위치 → 클립 내 시간 (trackRect 좌표 기준)
                float mouseX      = e.mousePosition.x;
                float globalTime  = (mouseX + scrollX) / pps;          // 타임라인 절대 시간
                float clipTime    = (globalTime - tOff) * motion.playbackSpeed + motion.ClipStartTime; // 실제 클립 시간

                if (_clipHandleDraggingStart)
                {
                    float clamped = Mathf.Clamp(clipTime, 0f, motion.ClipEndTime - 0.01f);
                    motion.clipStartTime = Mathf.Approximately(clamped, 0f) ? -1f : clamped;
                }
                else
                {
                    float clamped = Mathf.Clamp(clipTime, motion.ClipStartTime + 0.01f, clipLen);
                    motion.clipEndTime = Mathf.Approximately(clamped, clipLen) ? -1f : clamped;
                }

                MarkDirty();
                e.Use();
                Repaint();
            }

            if (e.type == EventType.MouseUp && (_clipHandleDraggingStart || _clipHandleDraggingEnd))
            {
                if (_clipHandleMotionIndex == idx)
                {
                    _clipHandleDraggingStart = false;
                    _clipHandleDraggingEnd   = false;
                    _clipHandleMotionIndex   = -1;
                    e.Use();
                }
            }
        }

        static void DrawHandleLines(Rect r)
        {
            float cx = r.x + r.width * 0.5f;
            float cy = r.y + r.height * 0.5f;
            float h  = r.height * 0.4f;
            EditorGUI.DrawRect(new Rect(cx - 2, cy - h * 0.5f, 1, h), new Color(0f, 0f, 0f, 0.5f));
            EditorGUI.DrawRect(new Rect(cx,     cy - h * 0.5f, 1, h), new Color(0f, 0f, 0f, 0.5f));
            EditorGUI.DrawRect(new Rect(cx + 2, cy - h * 0.5f, 1, h), new Color(0f, 0f, 0f, 0.5f));
        }

        // ====================================================================
        //  타이밍 마커
        // ====================================================================
        void DrawTimingMarkers(Rect trackRect, MotionSet set, float pps)
        {
            EditorGUI.DrawRect(trackRect, COL_TRACK_BG);
            GUI.BeginClip(trackRect);

            if (set.motions != null)
            {
                float t = 0f;
                var style = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal    = { textColor = COL_MARKER_TEXT },
                    fontStyle = FontStyle.Bold,
                    fontSize  = 10
                };

                for (int i = 0; i < set.motions.Count - 1; i++)
                {
                    t += set.motions[i].Duration;
                    float x = t * pps - scrollX;
                    if (x < -10 || x > trackRect.width + 10) continue;

                    float mw = 18f;
                    Rect mr  = new Rect(x - mw / 2f, 1, mw, MARKER_HEIGHT - 2);
                    EditorGUI.DrawRect(mr, COL_MARKER);
                    GUI.Label(mr, $"{i + 1}", style);
                }
            }

            GUI.EndClip();
        }

        // ====================================================================
        //  이벤트 트랙 (⑦ 클릭 선택 연동 포함)
        // ====================================================================
        int CountEventTracks(MotionSet set)
        {
            int c = set.globalEvents?.Count ?? 0;
            if (set.motions != null)
            {
                foreach (var m in set.motions)
                    c += m.events?.Count ?? 0;
            }
            return Mathf.Max(c, 1);
        }

        void DrawEventTracks(float labelX, float trackX, float yPos,
            float labelW, float trackW, MotionSet set, float pps)
        {
            int idx = 0;

            // MotionSet 글로벌 이벤트
            if (set.globalEvents != null)
            {
                for (int i = 0; i < set.globalEvents.Count; i++)
                {
                    var evt = set.globalEvents[i];
                    if (evt == null) continue;

                    var visual = MotionEventStyle.Get(evt);
                    float y = yPos + idx * (EVENT_HEIGHT + TRACK_GAP);
                    DrawEventTrackLabel(new Rect(labelX, y, labelW, EVENT_HEIGHT),
                        $"{visual.icon} {evt.GetDisplayName()}", visual.color);
                    DrawEventBarWithOffset(new Rect(trackX, y, trackW, EVENT_HEIGHT),
                        evt, 0f, pps, -1, i, true);
                    idx++;
                }
            }

            // 모션별 이벤트
            if (set.motions != null)
            {
                float tOff = 0f;
                for (int mi = 0; mi < set.motions.Count; mi++)
                {
                    var motion = set.motions[mi];
                    if (motion.events != null)
                    {
                        for (int ei = 0; ei < motion.events.Count; ei++)
                        {
                            var evt = motion.events[ei];
                            if (evt == null) continue;

                            var visual = MotionEventStyle.Get(evt);
                            float y = yPos + idx * (EVENT_HEIGHT + TRACK_GAP);
                            string label = evt.GetShortLabel();
                            if (string.IsNullOrEmpty(label)) label = $"M{mi}[{ei}]";

                            DrawEventTrackLabel(new Rect(labelX, y, labelW, EVENT_HEIGHT),
                                $"{visual.icon} {label}", visual.color);
                            DrawEventBarWithOffset(new Rect(trackX, y, trackW, EVENT_HEIGHT),
                                evt, tOff, pps, mi, ei, false);
                            idx++;
                        }
                    }
                    tOff += motion.Duration;
                }
            }

            if (idx == 0)
            {
                DrawTrackLabel(new Rect(labelX, yPos, labelW, EVENT_HEIGHT), "(없음)");
                EditorGUI.DrawRect(new Rect(trackX, yPos, trackW, EVENT_HEIGHT), COL_TRACK_BG);
            }
        }

        // 외부 주입 오버레이 트랙 바 — 읽기 전용 (클릭/드래그 없음)
        void DrawOverlayTrackBar(Rect trackRect, OverlayTrack track, float pps)
        {
            EditorGUI.DrawRect(trackRect, COL_TRACK_BG);
            GUI.BeginClip(trackRect);

            var textStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding   = new RectOffset(4, 2, 0, 0),
                normal    = { textColor = new Color(0.95f, 0.95f, 0.95f) },
                clipping  = TextClipping.Clip,
                fontSize  = 9,
            };

            foreach (OverlaySpan span in track.spans)
            {
                if (span == null) continue;

                float x0 = span.start * pps - scrollX;
                float x1 = span.end   * pps - scrollX;
                float w  = Mathf.Max(x1 - x0, 3f);
                if (x0 + w < 0 || x0 > trackRect.width) continue;

                Color dim = new Color(track.color.r, track.color.g, track.color.b, span.dashed ? 0.15f : 0.35f);
                Rect bar = new Rect(x0, 2, w, trackRect.height - 4);
                EditorGUI.DrawRect(bar, dim);
                if (span.dashed)
                {
                    // 자동 추론 구간: 상단 경계를 점선으로 그려 저작 구간(실선)과 구분한다.
                    for (float dx = 0; dx < w; dx += 6f)
                        EditorGUI.DrawRect(new Rect(x0 + dx, 2, Mathf.Min(3f, w - dx), 1), track.color);
                }
                else
                {
                    EditorGUI.DrawRect(new Rect(x0, 2, w, 2), track.color);
                    EditorGUI.DrawRect(new Rect(x0, 2, 1, trackRect.height - 4), track.color);
                    EditorGUI.DrawRect(new Rect(x0 + w - 1, 2, 1, trackRect.height - 4), track.color);
                }

                if (!string.IsNullOrEmpty(span.label) && w > 24f)
                    GUI.Label(bar, span.label, textStyle);
            }

            GUI.EndClip();
        }

        // 이벤트 트랙 전용 레이블 — 타입 색상 세로 바 포함
        void DrawEventTrackLabel(Rect rect, string text, Color accentColor)
        {
            EditorGUI.DrawRect(rect, COL_LABEL_BG);
            // 좌측 컬러 악센트 바 (3px)
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3, rect.height), accentColor);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), COL_LABEL_BORDER);

            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding   = new RectOffset(8, 4, 0, 0),
                normal    = { textColor = new Color(0.88f, 0.88f, 0.88f) },
                clipping  = TextClipping.Clip,
            };
            GUI.Label(rect, text, style);
        }

        void DrawEventBarWithOffset(Rect trackRect, MotionEventBase evt, float tOff, float pps,
            int motionIndex, int eventIndex, bool isSetEvent)
        {
            EditorGUI.DrawRect(trackRect, COL_TRACK_BG);
            GUI.BeginClip(trackRect);

            float x0 = (tOff + evt.startTime) * pps - scrollX;
            float x1 = (tOff + evt.endTime)   * pps - scrollX;
            float w  = x1 - x0;

            if (x0 + w > 0 && x0 < trackRect.width)
            {
                bool isSelected = isSetEvent
                    ? selectedEventIsSetEvent && selectedEventIndex == eventIndex
                    : !selectedEventIsSetEvent && selectedEventMotionIndex == motionIndex && selectedEventIndex == eventIndex;

                var visual = MotionEventStyle.Get(evt);

                // 바 배경 (딤 컬러) + 상단 강조선
                Rect bar = new Rect(x0, 2, Mathf.Max(w, 4f), trackRect.height - 4);
                EditorGUI.DrawRect(bar, visual.dimmed);

                // 상단 1px 강조선 (타입 색상 solid)
                EditorGUI.DrawRect(new Rect(bar.x, bar.y, bar.width, 2), visual.color);

                // 선택 시 밝기 오버레이
                if (isSelected)
                    EditorGUI.DrawRect(bar, COL_EVENT_SELECTED_OVERLAY);

                // 시작/끝 다이아몬드
                DrawDiamond(x0, trackRect.height * 0.5f, 5f, visual.color);
                if (w > 6f)
                    DrawDiamond(x1, trackRect.height * 0.5f, 5f, visual.color);

                // 핸들 (드래그용 hit area — 시각화 X)
                float hitAreaSize = 8f;
                Rect startHandle = new Rect(x0 - hitAreaSize, trackRect.height * 0.5f - hitAreaSize, hitAreaSize * 2, hitAreaSize * 2);
                Rect endHandle   = new Rect(x1 - hitAreaSize, trackRect.height * 0.5f - hitAreaSize, hitAreaSize * 2, hitAreaSize * 2);

                HandleEventClick(bar, trackRect, motionIndex, eventIndex, isSetEvent);
                HandleEventDrag(bar, startHandle, endHandle, trackRect, evt, tOff, pps, motionIndex, eventIndex, isSetEvent);
                HandleEventRightClick(bar, evt, motionIndex, eventIndex, isSetEvent);

                // 라벨 (짧은 이름, 바 너비에 맞춤)
                if (w > 20f)
                {
                    string label = evt.GetShortLabel();
                    if (!string.IsNullOrEmpty(label))
                    {
                        var style = new GUIStyle(EditorStyles.miniLabel)
                        {
                            alignment = TextAnchor.MiddleLeft,
                            normal    = { textColor = visual.color },
                            fontSize  = 9,
                            clipping  = TextClipping.Clip,
                            padding   = new RectOffset(4, 2, 0, 0),
                        };
                        GUI.Label(new Rect(bar.x, bar.y + 2, bar.width, bar.height - 2), label, style);
                    }
                }
            }

            GUI.EndClip();
        }

        // ⑦ 이벤트 클릭 선택
        void HandleEventClick(Rect barRect, Rect trackRect, int motionIndex, int eventIndex, bool isSetEvent)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && barRect.Contains(e.mousePosition))
            {
                // Shift 없이 클릭 시 선택 (Shift는 드래그용이므로 선택에서 제외)
                if (!e.shift)
                {
                    selectedEventMotionIndex = motionIndex;
                    selectedEventIndex       = eventIndex;
                    selectedEventIsSetEvent  = isSetEvent;
                    Repaint();
                    // e.Use() 하지 않아 드래그 핸들러가 후속 처리 가능
                }
            }
        }

        // ⑧ 이벤트 우클릭 컨텍스트 메뉴 (타임라인)
        void HandleEventRightClick(Rect barRect, MotionEventBase evt, int motionIndex, int eventIndex, bool isSetEvent)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 1 && barRect.Contains(e.mousePosition))
            {
                e.Use();
                // 컨텍스트 메뉴는 클립보드가 필요하므로 MotionSet 참조가 없어서 간단 메뉴만
                var menu = new GenericMenu();

                menu.AddItem(new GUIContent("복사 (Copy)"), false, () =>
                {
                    _copiedEvent = CloneEvent(evt);
                });

                menu.AddSeparator("");

                menu.AddItem(new GUIContent("이벤트 선택"), false, () =>
                {
                    selectedEventMotionIndex = motionIndex;
                    selectedEventIndex       = eventIndex;
                    selectedEventIsSetEvent  = isSetEvent;
                    Repaint();
                });

                menu.ShowAsContext();
            }
        }

        void HandleEventDrag(Rect barRect, Rect startRect, Rect endRect, Rect trackRect, MotionEventBase evt,
            float tOff, float pps, int motionIndex, int eventIndex, bool isSetEvent)
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Vector2 localPos = e.mousePosition;

                if (startRect.Contains(localPos))
                {
                    _isDraggingStart = true;
                    _dragEventMotionIndex = motionIndex;
                    _dragEventIndex = eventIndex;
                    _dragSetEvent = isSetEvent;
                    _dragStartOffset = tOff;
                    e.Use();
                    RecordUndo("Drag Event Start");
                }
                else if (endRect.Contains(localPos))
                {
                    _isDraggingEnd = true;
                    _dragEventMotionIndex = motionIndex;
                    _dragEventIndex = eventIndex;
                    _dragSetEvent = isSetEvent;
                    _dragStartOffset = tOff;
                    e.Use();
                    RecordUndo("Drag Event End");
                }
                else if (barRect.Contains(localPos) && e.shift)
                {
                    _isDraggingBody = true;
                    _dragEventMotionIndex = motionIndex;
                    _dragEventIndex = eventIndex;
                    _dragSetEvent = isSetEvent;
                    _dragStartOffset = tOff;
                    float mouseTime = (localPos.x + scrollX) / pps;
                    _dragBodyStartTime = mouseTime - (tOff + evt.startTime);
                    e.Use();
                    RecordUndo("Move Event");
                }
            }

            if ((_isDraggingStart || _isDraggingEnd || _isDraggingBody) && e.type == EventType.MouseDrag)
            {
                if (_dragEventMotionIndex == motionIndex && _dragEventIndex == eventIndex &&
                    _dragSetEvent == isSetEvent)
                {
                    float mouseTime = (e.mousePosition.x + scrollX) / pps - _dragStartOffset;

                    if (_isDraggingStart)
                    {
                        evt.startTime = Mathf.Max(0, Mathf.Min(mouseTime, evt.endTime - 0.01f));
                    }
                    else if (_isDraggingEnd)
                    {
                        evt.endTime = Mathf.Max(evt.startTime + 0.01f, mouseTime);
                    }
                    else if (_isDraggingBody)
                    {
                        float duration = evt.endTime - evt.startTime;
                        float currentMouseTime = (e.mousePosition.x + scrollX) / pps;
                        float newStartTime = currentMouseTime - _dragBodyStartTime - tOff;
                        newStartTime = Mathf.Max(0, newStartTime);

                        evt.startTime = newStartTime;
                        evt.endTime = newStartTime + duration;
                    }

                    MarkDirty();
                    e.Use();
                    Repaint();
                }
            }

        }

        static void DrawDiamond(float cx, float cy, float size, Color color)
        {
            float s = size * 0.7f;
            EditorGUI.DrawRect(new Rect(cx - s, cy - 1, s * 2, 2), color);
            EditorGUI.DrawRect(new Rect(cx - 1, cy - s, 2, s * 2), color);
            EditorGUI.DrawRect(new Rect(cx - s * 0.5f, cy - s * 0.5f, s, s), color);
        }

        // ====================================================================
        //  커서
        // ====================================================================
        void DrawCursor(Rect area, float totalDur, float pps)
        {
            float cx = cursorTime * pps - scrollX + area.x;
            if (cx >= area.x && cx <= area.xMax)
            {
                EditorGUI.DrawRect(new Rect(cx - 1, area.y, 2, area.height), COL_CURSOR);
                EditorGUI.DrawRect(new Rect(cx - 5, area.y - 2, 10, 8), COL_CURSOR);
            }
        }

        void HandleCursorInput(Rect rulerRect, float totalDur, float pps)
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown && rulerRect.Contains(e.mousePosition))
            {
                isDraggingCursor = true;
                cursorTime = Mathf.Clamp((e.mousePosition.x - rulerRect.x + scrollX) / pps, 0, totalDur);
                cursorScrubRequested = true;
                e.Use();
                Repaint();
            }
            if (isDraggingCursor && e.type == EventType.MouseDrag)
            {
                cursorTime = Mathf.Clamp((e.mousePosition.x - rulerRect.x + scrollX) / pps, 0, totalDur);
                cursorScrubRequested = true;
                e.Use();
                Repaint();
            }
            if (e.type == EventType.MouseUp && isDraggingCursor)
            {
                isDraggingCursor = false;
                e.Use();
            }
        }

        // ====================================================================
        //  스크롤
        // ====================================================================
        void HandleScroll(Rect contentRect, float timelineW, float viewW)
        {
            Event e = Event.current;
            if (e.type == EventType.ScrollWheel && contentRect.Contains(e.mousePosition))
            {
                if (e.control || e.command)
                    zoom = Mathf.Clamp(zoom - e.delta.y * 0.05f, 0.2f, 3f);
                else
                    scrollX = Mathf.Clamp(scrollX + e.delta.y * 20f, 0, Mathf.Max(0, timelineW - viewW));
                e.Use();
                Repaint();
            }
        }
    }
}
