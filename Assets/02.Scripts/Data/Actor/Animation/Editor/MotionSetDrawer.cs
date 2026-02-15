using System;
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
        // ── 색상 팔레트 ──
        static readonly Color COL_BG           = new Color(0.18f, 0.18f, 0.18f);
        static readonly Color COL_TRACK_BG     = new Color(0.22f, 0.22f, 0.22f);
        static readonly Color COL_RULER        = new Color(0.15f, 0.15f, 0.15f);
        static readonly Color COL_RULER_LINE   = new Color(0.4f, 0.4f, 0.4f);
        static readonly Color COL_RULER_TEXT   = new Color(0.7f, 0.7f, 0.7f);
        static readonly Color COL_CURSOR       = new Color(0.9f, 0.2f, 0.2f);
        static readonly Color COL_LABEL_BG     = new Color(0.14f, 0.14f, 0.14f);
        static readonly Color COL_LABEL_BORDER = new Color(0.3f, 0.3f, 0.3f);

        static readonly Color[] COL_MOTION_CLIPS =
        {
            new Color(0.35f, 0.55f, 0.35f),
            new Color(0.55f, 0.65f, 0.30f),
            new Color(0.30f, 0.50f, 0.60f),
            new Color(0.50f, 0.40f, 0.55f),
        };

        static readonly Color COL_EVENT_BAR     = new Color(0.45f, 0.45f, 0.55f, 0.85f);
        static readonly Color COL_EVENT_DIAMOND = new Color(0.6f, 0.6f, 0.7f);
        static readonly Color COL_MARKER        = new Color(0.85f, 0.25f, 0.25f);
        static readonly Color COL_MARKER_TEXT   = Color.white;

        // ── 레이아웃 상수 ──
        const float LABEL_WIDTH   = 140f;
        const float RULER_HEIGHT  = 24f;
        const float TRACK_HEIGHT  = 28f;
        const float EVENT_HEIGHT  = 22f;
        const float MARKER_HEIGHT = 20f;
        const float TRACK_GAP     = 2f;
        const float SECTION_GAP   = 6f;
        const float BASE_PPS      = 80f;

        // ── 상태 ──
        public float cursorTime;
        public float scrollX;
        public float zoom = 1f;
        public bool  isDraggingCursor;
        public int   selectedMotionIndex = -1;

        // 드래그 상태
        int   _dragEventMotionIndex = -1;
        int   _dragEventIndex = -1;
        bool  _dragSetEvent;
        bool  _isDraggingStart;
        bool  _isDraggingEnd;
        bool  _isDraggingBody;
        float _dragStartOffset;
        float _dragBodyStartTime;

        // 접힘 상태
        public bool foldMotions  = true;
        public bool foldTimeline = true;
        public bool foldEvents   = true;

        // Undo/Dirty 처리용 콜백
        readonly Func<UnityEngine.Object> _getTarget;
        readonly Action _repaint;

        /// <param name="getTarget">Undo/Dirty 대상 오브젝트 반환</param>
        /// <param name="repaint">Repaint 요청 콜백</param>
        public MotionSetDrawer(Func<UnityEngine.Object> getTarget, Action repaint)
        {
            _getTarget = getTarget;
            _repaint   = repaint;
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
            if (set == null) return;

            EditorGUILayout.Space(4);
            DrawHeader(set);
            EditorGUILayout.Space(4);

            foldMotions = EditorGUILayout.Foldout(foldMotions, "모션 리스트", true, EditorStyles.foldoutHeader);
            if (foldMotions) DrawMotionList(set);

            EditorGUILayout.Space(4);

            foldTimeline = EditorGUILayout.Foldout(foldTimeline, "타임라인", true, EditorStyles.foldoutHeader);
            if (foldTimeline && set.IsValid()) DrawTimeline(set);

            EditorGUILayout.Space(4);

            foldEvents = EditorGUILayout.Foldout(foldEvents, "모션 셋 이벤트", true, EditorStyles.foldoutHeader);
            if (foldEvents) DrawMotionSetEvents(set);

            if (isDraggingCursor || _isDraggingStart || _isDraggingEnd || _isDraggingBody) Repaint();
        }

        // ====================================================================
        //  헤더
        // ====================================================================
        void DrawHeader(MotionSet set)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("모션 셋", EditorStyles.boldLabel, GUILayout.Width(60));
                set.motionSetName = EditorGUILayout.TextField(set.motionSetName);
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
                            selectedMotionIndex = selectedMotionIndex == i ? -1 : i;

                        GUI.backgroundColor = Color.white;

                        motion.motionName = EditorGUILayout.TextField(motion.motionName);
                        motion.motionClip = (AnimationClip)EditorGUILayout.ObjectField(
                            motion.motionClip, typeof(AnimationClip), false, GUILayout.Width(180));

                        if (motion.IsValid())
                            EditorGUILayout.LabelField($"{motion.Duration:F2}s",
                                EditorStyles.miniLabel, GUILayout.Width(45));

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

                    if (selectedMotionIndex == i) DrawMotionEvents(motion);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 모션 추가", GUILayout.Width(120)))
            {
                RecordUndo("Add Motion");
                set.motions.Add(new Motion
                {
                    motionName = $"Motion_{set.motions.Count}",
                    events  = new List<MotionEventBase>()
                });
                MarkDirty();
            }
            EditorGUILayout.EndHorizontal();
        }

        // ====================================================================
        //  모션별 이벤트
        // ====================================================================
        void DrawMotionEvents(Motion motion)
        {
            motion.events ??= new List<MotionEventBase>();

            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("이벤트", EditorStyles.miniBoldLabel);

            for (int i = 0; i < motion.events.Count; i++)
            {
                var evt = motion.events[i];
                if (evt == null) continue;

                EditorGUILayout.BeginVertical(GUI.skin.box);
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(evt.GetDisplayName(), EditorStyles.boldLabel, GUILayout.Width(100));
                        GUILayout.FlexibleSpace();
                        
                        // 레이블 없이 FloatField 사용
                        GUILayout.Label("Start", GUILayout.Width(40));
                        evt.startTime = EditorGUILayout.FloatField(evt.startTime, GUILayout.Width(100));

                        GUILayout.Space(10);
                        
                        GUILayout.Label("End", GUILayout.Width(40));
                        evt.endTime = EditorGUILayout.FloatField(evt.endTime, GUILayout.Width(100));

                        if (GUILayout.Button("×", GUILayout.Width(22)))
                        {
                            RecordUndo("Remove Motion Event");
                            motion.events.RemoveAt(i);
                            MarkDirty();
                            break;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    // 이벤트별 세부 프로퍼티
                    EditorGUI.indentLevel++;
                    DrawEventProperties(evt);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 이벤트", GUILayout.Width(80)))
            {
                MotionEventMenuHelper.ShowAddEventMenu(motion.events, 0f, () =>
                {
                    RecordUndo("Add Motion Event");
                    MarkDirty();
                    Repaint();
                });
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        void DrawEventProperties(MotionEventBase evt)
        {
            // 리플렉션으로 각 이벤트의 public 필드/프로퍼티 그리기
            var type = evt.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.Name == "startTime" || field.Name == "endTime") continue;

                var value = field.GetValue(evt);
                var fieldType = field.FieldType;

                EditorGUILayout.BeginHorizontal();
                {
                    if (fieldType == typeof(float))
                    {
                        var newValue = EditorGUILayout.FloatField(field.Name, (float)value);
                        if (!newValue.Equals(value))
                        {
                            RecordUndo($"Change {field.Name}");
                            field.SetValue(evt, newValue);
                            MarkDirty();
                        }
                    }
                    else if (fieldType == typeof(int))
                    {
                        var newValue = EditorGUILayout.IntField(field.Name, (int)value);
                        if (newValue != (int)value)
                        {
                            RecordUndo($"Change {field.Name}");
                            field.SetValue(evt, newValue);
                            MarkDirty();
                        }
                    }
                    else if (fieldType == typeof(string))
                    {
                        var newValue = EditorGUILayout.TextField(field.Name, (string)value);
                        if (newValue != (string)value)
                        {
                            RecordUndo($"Change {field.Name}");
                            field.SetValue(evt, newValue);
                            MarkDirty();
                        }
                    }
                    else if (fieldType == typeof(bool))
                    {
                        var newValue = EditorGUILayout.Toggle(field.Name, (bool)value);
                        if (newValue != (bool)value)
                        {
                            RecordUndo($"Change {field.Name}");
                            field.SetValue(evt, newValue);
                            MarkDirty();
                        }
                    }
                    else if (fieldType == typeof(Vector3))
                    {
                        var newValue = EditorGUILayout.Vector3Field(field.Name, (Vector3)value);
                        if (newValue != (Vector3)value)
                        {
                            RecordUndo($"Change {field.Name}");
                            field.SetValue(evt, newValue);
                            MarkDirty();
                        }
                    }
                    else if (fieldType == typeof(AnimationCurve))
                    {
                        var newValue = EditorGUILayout.CurveField(field.Name, (AnimationCurve)value);
                        if (newValue != (AnimationCurve)value)
                        {
                            RecordUndo($"Change {field.Name}");
                            field.SetValue(evt, newValue);
                            MarkDirty();
                        }
                    }
                    else if (fieldType == typeof(LayerMask))
                    {
                        // LayerMask를 처리하려면 MaskField를 사용해야 함
                        var layerMask = (LayerMask)value;
                        var newValue = EditorGUILayout.MaskField(field.Name, layerMask.value, 
                            UnityEditorInternal.InternalEditorUtility.layers);
    
                        if (newValue != layerMask.value)
                        {
                            RecordUndo($"Change {field.Name}");
                            field.SetValue(evt, (LayerMask)newValue);
                            MarkDirty();
                        }
                    }
                    else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                    {
                        var newValue = EditorGUILayout.ObjectField(field.Name, (UnityEngine.Object)value, fieldType, false);
                        if (newValue != (UnityEngine.Object)value)
                        {
                            RecordUndo($"Change {field.Name}");
                            field.SetValue(evt, newValue);
                            MarkDirty();
                        }
                    }
                    else if (fieldType.IsEnum)
                    {
                        var newValue = EditorGUILayout.EnumPopup(field.Name, (System.Enum)value);
                        if (!newValue.Equals(value))
                        {
                            RecordUndo($"Change {field.Name}");
                            field.SetValue(evt, newValue);
                            MarkDirty();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        // ====================================================================
        //  MotionSet 이벤트
        // ====================================================================
        void DrawMotionSetEvents(MotionSet set)
        {
            set.globalEvents ??= new List<MotionEventBase>();

            EditorGUI.indentLevel++;
            for (int i = 0; i < set.globalEvents.Count; i++)
            {
                var evt = set.globalEvents[i];
                if (evt == null) continue;

                EditorGUILayout.BeginVertical(GUI.skin.box);
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(evt.GetDisplayName(), EditorStyles.boldLabel, GUILayout.Width(100));
                        GUILayout.FlexibleSpace();
                            
                        // 레이블 없이 FloatField 사용
                        GUILayout.Label("Start", GUILayout.Width(40));
                        evt.startTime = EditorGUILayout.FloatField(evt.startTime, GUILayout.Width(60));
        
                        GUILayout.Space(10);
        
                        GUILayout.Label("End", GUILayout.Width(40));
                        evt.endTime = EditorGUILayout.FloatField(evt.endTime, GUILayout.Width(60));

                        if (GUILayout.Button("×", GUILayout.Width(22)))
                        {
                            RecordUndo("Remove MotionSet Event");
                            set.globalEvents.RemoveAt(i);
                            MarkDirty();
                            break;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    // 이벤트별 세부 프로퍼티
                    EditorGUI.indentLevel++;
                    DrawEventProperties(evt);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 이벤트", GUILayout.Width(80)))
            {
                MotionEventMenuHelper.ShowAddEventMenu(set.globalEvents, 0f, () =>
                {
                    RecordUndo("Add MotionSet Event");
                    MarkDirty();
                    Repaint();
                });
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        // ====================================================================
        //  타임라인 전체
        // ====================================================================
        void DrawTimeline(MotionSet set)
        {
            float totalDur = set.TotalDuration;
            if (totalDur <= 0f) return;

            float pps = BASE_PPS * zoom;

            // 높이 계산
            int motionCount     = set.motions?.Count ?? 0;
            int eventTrackCount = CountEventTracks(set);

            float timelineH = RULER_HEIGHT + TRACK_GAP
                + (TRACK_HEIGHT + TRACK_GAP) * Mathf.Max(motionCount, 1) + SECTION_GAP
                + MARKER_HEIGHT + TRACK_GAP + SECTION_GAP
                + (EVENT_HEIGHT + TRACK_GAP) * Mathf.Max(eventTrackCount, 1) + 8f;

            Rect fullRect = GUILayoutUtility.GetRect(0, timelineH + 30f);
            fullRect.x     += 4;
            fullRect.width -= 8;

            EditorGUI.DrawRect(fullRect, COL_BG);

            // 줌 컨트롤
            DrawZoomControl(new Rect(fullRect.x, fullRect.y, fullRect.width, 18f), totalDur);

            Rect content = new Rect(fullRect.x, fullRect.y + 20f, fullRect.width, fullRect.height - 20f);
            float labelW    = LABEL_WIDTH;
            float trackW    = content.width - labelW;
            float timelineW = totalDur * pps;

            scrollX = Mathf.Clamp(scrollX, 0, Mathf.Max(0, timelineW - trackW));

            float y = content.y;

            // 룰러
            Rect rulerRect = new Rect(content.x + labelW, y, trackW, RULER_HEIGHT);
            DrawRuler(rulerRect, totalDur, pps);
            y += RULER_HEIGHT + TRACK_GAP;

            // 모션 트랙
            DrawSectionLabel(new Rect(content.x, y - 2, labelW, 16f), "몽타주");
            if (set.motions != null)
            {
                float tOff = 0f;
                for (int i = 0; i < set.motions.Count; i++)
                {
                    DrawTrackLabel(new Rect(content.x, y, labelW, TRACK_HEIGHT),
                        set.motions[i].motionName ?? $"Motion {i}");
                    DrawMotionClipBar(new Rect(content.x + labelW, y, trackW, TRACK_HEIGHT),
                        set.motions[i], i, tOff, pps);
                    tOff += set.motions[i].Duration;
                    y    += TRACK_HEIGHT + TRACK_GAP;
                }
            }
            y += SECTION_GAP;

            // 타이밍 마커
            DrawSectionLabel(new Rect(content.x, y, labelW, MARKER_HEIGHT), "타이밍");
            DrawTimingMarkers(new Rect(content.x + labelW, y, trackW, MARKER_HEIGHT), set, pps);
            y += MARKER_HEIGHT + TRACK_GAP + SECTION_GAP;

            // 노티파이
            DrawSectionLabel(new Rect(content.x, y - 2, labelW, 16f), "노티파이");
            DrawEventTracks(content.x, content.x + labelW, y, labelW, trackW, set, pps);

            // 커서
            Rect cursorArea = new Rect(content.x + labelW, content.y, trackW,
                y - content.y + EVENT_HEIGHT * eventTrackCount);
            DrawCursor(cursorArea, totalDur, pps);
            HandleCursorInput(rulerRect, totalDur, pps);

            HandleScroll(content, timelineW, trackW);
        }

        // ====================================================================
        //  줌
        // ====================================================================
        void DrawZoomControl(Rect r, float totalDur)
        {
            EditorGUI.LabelField(new Rect(r.x, r.y, 40, r.height), "줌", EditorStyles.miniLabel);
            zoom = GUI.HorizontalSlider(new Rect(r.x + 35, r.y + 2, 120, r.height), zoom, 0.2f, 10f);
            EditorGUI.LabelField(new Rect(r.x + 160, r.y, 60, r.height),
                $"×{zoom:F1}", EditorStyles.miniLabel);

            float pct = totalDur > 0 ? cursorTime / totalDur * 100f : 0;
            EditorGUI.LabelField(new Rect(r.x + 220, r.y, 140, r.height),
                $"커서: {cursorTime:F2}s ({pct:F1}%)", EditorStyles.miniLabel);
        }

        // ====================================================================
        //  룰러
        // ====================================================================
        void DrawRuler(Rect rect, float totalDur, float pps)
        {
            EditorGUI.DrawRect(rect, COL_RULER);
            GUI.BeginClip(rect);

            float step        = GetRulerStep(pps);
            float startTime   = scrollX / pps;
            float startSnap   = Mathf.Floor(startTime / step) * step;
            var   labelStyle  = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = COL_RULER_TEXT } };

            for (float t = startSnap; t <= totalDur; t += step)
            {
                float x = t * pps - scrollX;
                if (x < -20 || x > rect.width + 20) continue;
                EditorGUI.DrawRect(new Rect(x, rect.height - 8, 1, 8), COL_RULER_LINE);
                GUI.Label(new Rect(x + 2, 0, 50, rect.height), $"{Mathf.RoundToInt(t * 30f)}", labelStyle);
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

        void DrawTrackLabel(Rect rect, string text)
        {
            EditorGUI.DrawRect(rect, COL_LABEL_BG);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), COL_LABEL_BORDER);

            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding   = new RectOffset(8, 4, 0, 0),
                normal    = { textColor = new Color(0.75f, 0.75f, 0.75f) }
            };
            GUI.Label(rect, text, style);
        }

        // ====================================================================
        //  모션 클립 바
        // ====================================================================
        void DrawMotionClipBar(Rect trackRect, Motion motion, int idx, float tOff, float pps)
        {
            EditorGUI.DrawRect(trackRect, COL_TRACK_BG);
            GUI.BeginClip(trackRect);

            float x0 = tOff * pps - scrollX;
            float w  = motion.Duration * pps;

            if (x0 + w > 0 && x0 < trackRect.width)
            {
                Rect bar = new Rect(x0, 2, w, trackRect.height - 4);
                EditorGUI.DrawRect(bar, COL_MOTION_CLIPS[idx % COL_MOTION_CLIPS.Length]);

                string name = motion.motionClip != null ? motion.motionClip.name : motion.motionName;
                
                // 텍스트가 잘리지 않도록 여백 추가
                var style = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal    = { textColor = Color.white },
                    fontStyle = FontStyle.Bold,
                    clipping  = TextClipping.Clip,
                    padding   = new RectOffset(6, 6, 0, 0)
                };
                
                // 텍스트 영역 확보
                Rect textRect = new Rect(bar.x + 4, bar.y, bar.width - 8, bar.height);
                GUI.Label(textRect, name, style);
            }

            GUI.EndClip();
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
        //  이벤트 트랙
        // ====================================================================
        int CountEventTracks(MotionSet set)
        {
            int c = set.globalEvents?.Count ?? 0;
            if (set.motions != null)
            {
                foreach (var m in set.motions)
                {
                    c += m.events?.Count ?? 0;
                }
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
                    
                    float y = yPos + idx * (EVENT_HEIGHT + TRACK_GAP);
                    DrawTrackLabel(new Rect(labelX, y, labelW, EVENT_HEIGHT), $"Set: {evt.GetDisplayName()}");
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
                            
                            float y = yPos + idx * (EVENT_HEIGHT + TRACK_GAP);
                            string label = evt.GetShortLabel();
                            if (string.IsNullOrEmpty(label)) label = $"M{mi}[{ei}]";

                            DrawTrackLabel(new Rect(labelX, y, labelW, EVENT_HEIGHT), label);
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
                Rect bar = new Rect(x0, 3, w, trackRect.height - 6);
                EditorGUI.DrawRect(bar, COL_EVENT_BAR);

                // 시작/종료 다이아몬드 (클릭 영역 확대)
                float diamondSize = 6f;
                float hitAreaSize = 8f; // 클릭 감지 영역
                Rect startDiamond = new Rect(x0 - hitAreaSize, trackRect.height / 2f - hitAreaSize, hitAreaSize * 2, hitAreaSize * 2);
                Rect endDiamond = new Rect(x1 - hitAreaSize, trackRect.height / 2f - hitAreaSize, hitAreaSize * 2, hitAreaSize * 2);
                
                DrawDiamond(x0, trackRect.height / 2f, diamondSize, COL_EVENT_DIAMOND);
                DrawDiamond(x1, trackRect.height / 2f, diamondSize, COL_EVENT_DIAMOND);

                // 드래그 처리
                HandleEventDrag(bar, startDiamond, endDiamond, trackRect, evt, tOff, pps, 
                    motionIndex, eventIndex, isSetEvent);

                // 이벤트 라벨 표시
                string label = evt.GetShortLabel();
                if (!string.IsNullOrEmpty(label))
                {
                    var style = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal    = { textColor = Color.white },
                        fontSize  = 9,
                        clipping  = TextClipping.Clip
                    };
                    GUI.Label(bar, label, style);
                }
            }

            GUI.EndClip();
        }

        void HandleEventDrag(Rect barRect, Rect startRect, Rect endRect, Rect trackRect, MotionEventBase evt, 
            float tOff, float pps, int motionIndex, int eventIndex, bool isSetEvent)
        {
            Event e = Event.current;
            
            // 마우스 다운 - 드래그 시작
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Vector2 localPos = e.mousePosition;
                
                // 다이아몬드 우선 체크
                if (startRect.Contains(localPos))
                {
                    _isDraggingStart = true;
                    _isDraggingEnd = false;
                    _isDraggingBody = false;
                    _dragEventMotionIndex = motionIndex;
                    _dragEventIndex = eventIndex;
                    _dragSetEvent = isSetEvent;
                    _dragStartOffset = tOff;
                    e.Use();
                    RecordUndo("Drag Event Start");
                }
                else if (endRect.Contains(localPos))
                {
                    _isDraggingStart = false;
                    _isDraggingEnd = true;
                    _isDraggingBody = false;
                    _dragEventMotionIndex = motionIndex;
                    _dragEventIndex = eventIndex;
                    _dragSetEvent = isSetEvent;
                    _dragStartOffset = tOff;
                    e.Use();
                    RecordUndo("Drag Event End");
                }
                // Shift 키를 누르고 있을 때만 몸통 드래그 허용
                else if (barRect.Contains(localPos) && e.shift)
                {
                    _isDraggingStart = false;
                    _isDraggingEnd = false;
                    _isDraggingBody = true;
                    _dragEventMotionIndex = motionIndex;
                    _dragEventIndex = eventIndex;
                    _dragSetEvent = isSetEvent;
                    _dragStartOffset = tOff;
                    // 마우스 클릭 위치와 이벤트 시작 시간의 차이 저장
                    float mouseTime = (localPos.x + scrollX) / pps;
                    _dragBodyStartTime = mouseTime - (tOff + evt.startTime);
                    e.Use();
                    RecordUndo("Move Event");
                }
                // Shift 없이 바 클릭 시 이벤트를 소비하지 않음 (타임라인 스크롤 허용)
            }
            
            // 드래그 중
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
                        // 마우스 위치에서 초기 오프셋을 빼서 새로운 시작 시간 계산
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
            
            // 마우스 업 - 드래그 종료
            if (e.type == EventType.MouseUp && (_isDraggingStart || _isDraggingEnd || _isDraggingBody))
            {
                _isDraggingStart = false;
                _isDraggingEnd = false;
                _isDraggingBody = false;
                _dragEventMotionIndex = -1;
                _dragEventIndex = -1;
                e.Use();
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
                e.Use();
                Repaint();
            }
            if (isDraggingCursor && e.type == EventType.MouseDrag)
            {
                cursorTime = Mathf.Clamp((e.mousePosition.x - rulerRect.x + scrollX) / pps, 0, totalDur);
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