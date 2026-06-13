using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Tool.Editor.Combat;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// 애니메이션 에디터 전투 오버레이 확장.
    /// 현재 모션(AnimKey)에 매칭되는 AttackDataSO를 찾아
    /// ① 타임라인에 판정/캔슬/콤보/선후딜 트랙을 표시하고
    /// ② 씬 뷰에 히트박스 기즈모(+편집 핸들)를 그린다.
    /// </summary>
    public partial class MotionSetEditorWindow
    {
        const string PREFS_COMBAT_SHOW = "UPlayground.MotionSetWindow.CombatOverlay.Show";
        const string PREFS_COMBAT_EDIT = "UPlayground.MotionSetWindow.CombatOverlay.Edit";

        static readonly Color COL_COMBAT_ACTIVE   = new Color(0.95f, 0.30f, 0.25f);
        static readonly Color COL_COMBAT_CANCEL   = new Color(0.35f, 0.60f, 0.95f);
        static readonly Color COL_COMBAT_MOVE     = new Color(0.45f, 0.80f, 0.85f);
        static readonly Color COL_COMBAT_COMBO    = new Color(0.35f, 0.80f, 0.40f);
        static readonly Color COL_COMBAT_PREPOST  = new Color(0.55f, 0.55f, 0.60f);

        AttackDataSO _combatAttackData;
        bool _combatPrefsLoaded;
        bool _showCombatOverlay = true;
        bool _combatEditHitbox;
        int  _combatGizmoPhase = -1;          // -1 = 커서 시간 기준 자동
        int  _combatAttackIndex;              // 같은 AnimKey를 쓰는 항목이 여럿일 때 선택
        AnimKey _combatManualKey = AnimKey.None; // 액터 세트 미사용 시 수동 키

        // 자동 페어링을 컨텍스트(세트)당 1회만 시도
        UnityEngine.Object _combatPairingTriedFor;

        List<CombatTimelineUtility.ResolvedAttack> _combatResolved = new();

        // ─────────────────────────────────────────────────────────────────
        //  컨트롤 바 (OnGUI에서 호출)
        // ─────────────────────────────────────────────────────────────────
        void DrawCombatOverlayControls()
        {
            LoadCombatPrefsOnce();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("전투 오버레이", EditorStyles.boldLabel, GUILayout.Width(90));

                    bool show = EditorGUILayout.ToggleLeft("표시", _showCombatOverlay, GUILayout.Width(50));
                    if (show != _showCombatOverlay)
                    {
                        _showCombatOverlay = show;
                        EditorPrefs.SetBool(PREFS_COMBAT_SHOW, show);
                        SceneView.RepaintAll();
                    }

                    EditorGUI.BeginChangeCheck();
                    var newData = (AttackDataSO)EditorGUILayout.ObjectField(
                        _combatAttackData, typeof(AttackDataSO), false, GUILayout.MinWidth(160));
                    if (EditorGUI.EndChangeCheck())
                    {
                        _combatAttackData = newData;
                        SaveCombatPairing();
                    }

                    if (GUILayout.Button("자동 연결", GUILayout.Width(64)))
                        AutoConnectCombatData();

                    bool edit = GUILayout.Toggle(_combatEditHitbox, "씬 핸들 편집", EditorStyles.miniButton, GUILayout.Width(90));
                    if (edit != _combatEditHitbox)
                    {
                        _combatEditHitbox = edit;
                        EditorPrefs.SetBool(PREFS_COMBAT_EDIT, edit);
                        SceneView.RepaintAll();
                    }

                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                if (_showCombatOverlay)
                    DrawCombatStatusRow();
            }
            EditorGUILayout.EndVertical();

            RefreshCombatOverlayTracks();
        }

        void LoadCombatPrefsOnce()
        {
            if (_combatPrefsLoaded) return;
            _combatPrefsLoaded = true;
            _showCombatOverlay = EditorPrefs.GetBool(PREFS_COMBAT_SHOW, true);
            _combatEditHitbox  = EditorPrefs.GetBool(PREFS_COMBAT_EDIT, false);
        }

        void DrawCombatStatusRow()
        {
            AnimKey key = GetCombatAnimKey();

            EditorGUILayout.BeginHorizontal();
            {
                if (_selectedActorMotionKey == AnimKey.None)
                {
                    EditorGUILayout.LabelField("AnimKey", GUILayout.Width(55));
                    _combatManualKey = (AnimKey)EditorGUILayout.EnumPopup(_combatManualKey, GUILayout.Width(170));
                }

                if (_combatAttackData == null)
                {
                    EditorGUILayout.LabelField("공격 데이터 없음 — 에셋을 지정하거나 [자동 연결] (몬스터 전용 탐색)", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                    return;
                }

                ResolveCombatAttacks(key);

                if (key == AnimKey.None)
                {
                    EditorGUILayout.LabelField("모션 키 미선택", EditorStyles.miniLabel);
                }
                else if (_combatResolved.Count == 0)
                {
                    EditorGUILayout.LabelField($"'{key}' 를 쓰는 공격 데이터가 없습니다.", EditorStyles.miniLabel);
                }
                else
                {
                    if (_combatResolved.Count > 1)
                    {
                        var names = new string[_combatResolved.Count];
                        for (int i = 0; i < names.Length; i++) names[i] = _combatResolved[i].SourceName;
                        _combatAttackIndex = EditorGUILayout.Popup(
                            Mathf.Clamp(_combatAttackIndex, 0, names.Length - 1), names, GUILayout.Width(190));
                    }
                    else
                    {
                        _combatAttackIndex = 0;
                        EditorGUILayout.LabelField(_combatResolved[0].SourceName, EditorStyles.miniBoldLabel, GUILayout.Width(190));
                    }

                    var atk = GetCurrentCombatAttack();
                    int phaseCount = atk?.HitPhases?.Count ?? 0;

                    // 기즈모 페이즈 선택 (자동 = 커서 시간 기준)
                    var phaseNames = new string[phaseCount + 1];
                    phaseNames[0] = "자동(커서)";
                    for (int i = 0; i < phaseCount; i++) phaseNames[i + 1] = $"P{i}";
                    int sel = EditorGUILayout.Popup(_combatGizmoPhase + 1, phaseNames, GUILayout.Width(86));
                    _combatGizmoPhase = sel - 1;

                    DrawCombatPhaseMismatchLabel(phaseCount);
                }

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawCombatPhaseMismatchLabel(int dataPhaseCount)
        {
            MotionSet set = GetCurrentMotionSet();
            if (set == null) return;

            var collisions = CombatTimelineUtility.CollectCollisionSpans(set);
            int maxIdx = -1;
            foreach (var span in collisions) maxIdx = Mathf.Max(maxIdx, span.PhaseIndex);
            int timelinePhases = maxIdx + 1;

            if (collisions.Count == 0)
            {
                EditorGUILayout.LabelField("Collision 이벤트 없음", EditorStyles.miniLabel, GUILayout.Width(120));
                return;
            }

            if (timelinePhases != dataPhaseCount)
            {
                var warnStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(1f, 0.65f, 0.2f) }
                };
                EditorGUILayout.LabelField(
                    $"⚠ 페이즈 불일치: 타임라인 {timelinePhases} ↔ 데이터 {dataPhaseCount}",
                    warnStyle, GUILayout.Width(190));
            }
            else
            {
                EditorGUILayout.LabelField($"페이즈 {dataPhaseCount}개 일치", EditorStyles.miniLabel, GUILayout.Width(110));
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  데이터 연결
        // ─────────────────────────────────────────────────────────────────
        AnimKey GetCombatAnimKey()
        {
            return _selectedActorMotionKey != AnimKey.None ? _selectedActorMotionKey : _combatManualKey;
        }

        UnityEngine.Object GetCombatPairingContext()
        {
            return _actorAnimationSet != null ? (UnityEngine.Object)_actorAnimationSet : _asset;
        }

        void ResolveCombatAttacks(AnimKey key)
        {
            _combatResolved = CombatTimelineUtility.ResolveAttacks(_combatAttackData, key);
        }

        CombatTimelineUtility.ResolvedAttack GetCurrentCombatAttack()
        {
            if (_combatResolved == null || _combatResolved.Count == 0) return null;
            return _combatResolved[Mathf.Clamp(_combatAttackIndex, 0, _combatResolved.Count - 1)];
        }

        void SaveCombatPairing()
        {
            var context = GetCombatPairingContext();
            if (context != null)
                CombatTimelineUtility.SaveAttackDataPairing(context, _combatAttackData);
        }

        /// <summary> 몬스터: ActorDefinitionSO 스캔. 실패 시 저장된 수동 페어링 복원. </summary>
        void AutoConnectCombatData()
        {
            var context = GetCombatPairingContext();

            if (_actorAnimationSet != null)
            {
                var found = CombatTimelineUtility.FindEnemyAttackDataForMotionSet(_actorAnimationSet, out var owner);
                if (found != null)
                {
                    _combatAttackData = found;
                    SaveCombatPairing();
                    ShowNotification(new GUIContent($"연결: {owner.name} → {found.name}"));
                    return;
                }
            }

            var cached = CombatTimelineUtility.LoadAttackDataPairing(context);
            if (cached != null)
            {
                _combatAttackData = cached;
                ShowNotification(new GUIContent($"저장된 페어링 복원: {cached.name}"));
                return;
            }

            ShowNotification(new GUIContent("연결 실패 — 몬스터 ActorDefinitionSO에서 찾지 못했습니다.\n플레이어는 수동 지정 후 자동 저장됩니다."));
        }

        /// <summary> 컨텍스트가 바뀌면 저장된 페어링을 자동 복원한다. </summary>
        void TryRestoreCombatPairing()
        {
            var context = GetCombatPairingContext();
            if (context == null || ReferenceEquals(_combatPairingTriedFor, context)) return;
            _combatPairingTriedFor = context;

            if (_combatAttackData != null) return;
            _combatAttackData = CombatTimelineUtility.LoadAttackDataPairing(context);
        }

        // ─────────────────────────────────────────────────────────────────
        //  타임라인 트랙 갱신
        // ─────────────────────────────────────────────────────────────────
        void RefreshCombatOverlayTracks()
        {
            if (_drawer == null) return;

            TryRestoreCombatPairing();

            MotionSet set = GetCurrentMotionSet();
            if (set == null || !_showCombatOverlay || _combatAttackData == null)
            {
                _drawer.overlayTracks = null;
                return;
            }

            ResolveCombatAttacks(GetCombatAnimKey());
            var atk = GetCurrentCombatAttack();
            if (atk == null)
            {
                _drawer.overlayTracks = null;
                return;
            }

            float total = set.TotalDuration;
            var collisions = CombatTimelineUtility.CollectCollisionSpans(set);
            var tracks = new List<MotionSetDrawer.OverlayTrack>();

            // ① 판정 액티브
            var activeTrack = new MotionSetDrawer.OverlayTrack { label = "⚔ 판정 액티브", color = COL_COMBAT_ACTIVE };
            foreach (var span in collisions)
            {
                HitPhaseData phase = atk.GetHitPhase(span.PhaseIndex);
                string text = phase != null
                    ? $"P{span.PhaseIndex} {phase.damage:0.#}dmg/{phase.poiseDamage:0.#}p"
                    : $"P{span.PhaseIndex}";
                activeTrack.spans.Add(new MotionSetDrawer.OverlaySpan { start = span.Start, end = span.End, label = text });
            }
            tracks.Add(activeTrack);

            // ② 캔슬 윈도우 (콜리전 비활성 구간, Move 제외 마스크)
            PlayerInterruptAction nonMove = atk.InterruptActions & ~PlayerInterruptAction.Move;
            if (nonMove != PlayerInterruptAction.None)
            {
                string maskText = CombatTimelineUtility.FormatInterruptMask(nonMove);
                var cancelTrack = new MotionSetDrawer.OverlayTrack { label = $"✂ 캔슬 ({maskText})", color = COL_COMBAT_CANCEL };
                foreach (var span in CombatTimelineUtility.ComputeComplementSpans(collisions, total))
                    cancelTrack.spans.Add(new MotionSetDrawer.OverlaySpan { start = span.Start, end = span.End, label = maskText });
                tracks.Add(cancelTrack);
            }

            // ③ 이동 캔슬 — 마지막 히트 페이즈 이후 후딜에서만 (PlayerAttackState 게이트와 동일)
            if ((atk.InterruptActions & PlayerInterruptAction.Move) != 0 && collisions.Count > 0)
            {
                float lastEnd = 0f;
                foreach (var span in collisions) lastEnd = Mathf.Max(lastEnd, span.End);
                if (total - lastEnd > 0.01f)
                {
                    var moveTrack = new MotionSetDrawer.OverlayTrack { label = "✂ 이동 캔슬 (후딜)", color = COL_COMBAT_MOVE };
                    moveTrack.spans.Add(new MotionSetDrawer.OverlaySpan { start = lastEnd, end = total, label = "이동" });
                    tracks.Add(moveTrack);
                }
            }

            // ④ 콤보 윈도우
            var combos = CombatTimelineUtility.CollectSpans<ComboWindowEvent>(set);
            if (combos.Count > 0)
            {
                var comboTrack = new MotionSetDrawer.OverlayTrack { label = "⛓ 콤보 윈도우", color = COL_COMBAT_COMBO };
                foreach (var span in combos)
                    comboTrack.spans.Add(new MotionSetDrawer.OverlaySpan { start = span.Start, end = span.End, label = "콤보" });
                tracks.Add(comboTrack);
            }

            // ⑤ 선딜 / 후딜
            if (collisions.Count > 0)
            {
                CombatTimelineUtility.ComputeFrameMetrics(collisions, total,
                    out float startup, out float activeTotal, out float recovery);

                var frameTrack = new MotionSetDrawer.OverlayTrack { label = "◔ 선딜·후딜", color = COL_COMBAT_PREPOST };
                if (startup > 0.01f)
                    frameTrack.spans.Add(new MotionSetDrawer.OverlaySpan { start = 0f, end = startup, label = $"선딜 {startup:0.00}s" });
                if (recovery > 0.01f)
                    frameTrack.spans.Add(new MotionSetDrawer.OverlaySpan { start = total - recovery, end = total, label = $"후딜 {recovery:0.00}s" });
                tracks.Add(frameTrack);
            }

            _drawer.overlayTracks = tracks;
        }

        // ─────────────────────────────────────────────────────────────────
        //  씬 뷰 히트박스 기즈모 (OnSceneGUI에서 호출)
        // ─────────────────────────────────────────────────────────────────
        void DrawCombatHitboxGizmo(SceneView sceneView)
        {
            if (!_showCombatOverlay || _targetActor == null) return;

            var atk = GetCurrentCombatAttack();
            if (atk == null) return;

            MotionSet set = GetCurrentMotionSet();
            if (set == null) return;

            // 표시할 페이즈 결정: 커서 시간이 Collision 구간 안이면 그 페이즈(액티브), 아니면 수동 선택
            float time = _drawer != null ? _drawer.cursorTime : 0f;
            int phaseIndex = -1;
            bool isActive = false;
            foreach (var span in CombatTimelineUtility.CollectCollisionSpans(set))
            {
                if (time < span.Start || time > span.End) continue;
                phaseIndex = span.PhaseIndex;
                isActive = true;
                break;
            }

            if (!isActive)
            {
                if (_combatGizmoPhase < 0) return; // 자동 모드 + 비액티브 → 미표시
                phaseIndex = _combatGizmoPhase;
            }

            HitPhaseData phase = atk.GetHitPhase(phaseIndex);
            if (phase == null) return;

            Transform actorTr = _targetActor.transform;
            // 런타임 MeleeHitShape와 동일: 원점 = 액터 위치 + up * attackOffset.y
            Vector3 origin = actorTr.position + Vector3.up * phase.attackOffset.y;
            Vector3 forward = actorTr.forward;
            float radius = phase.attackRadius;

            Color main = isActive ? COL_COMBAT_ACTIVE : new Color(1f, 0.6f, 0.2f, 0.8f);
            Handles.color = main;

            // 판정 반경 + 각도
            if (atk.HasHitAngle && atk.HitAngle < 180f)
            {
                Vector3 from = Quaternion.Euler(0f, -atk.HitAngle, 0f) * forward;
                Handles.DrawWireArc(origin, Vector3.up, from, atk.HitAngle * 2f, radius, 2.5f);
                Handles.DrawLine(origin, origin + Quaternion.Euler(0f, atk.HitAngle, 0f) * forward * radius, 2.5f);
                Handles.DrawLine(origin, origin + Quaternion.Euler(0f, -atk.HitAngle, 0f) * forward * radius, 2.5f);
                Handles.DrawLine(origin, origin + forward * radius, 1f);
            }
            else
            {
                Handles.DrawWireDisc(origin, Vector3.up, radius, 2.5f);
            }

            // 높이 클램프 범위
            if (phase.hitHeightRange > 0f)
            {
                Color faded = new Color(main.r, main.g, main.b, 0.35f);
                Handles.color = faded;
                Handles.DrawWireDisc(origin + Vector3.up * phase.hitHeightRange, Vector3.up, radius);
                Handles.DrawWireDisc(origin - Vector3.up * phase.hitHeightRange, Vector3.up, radius);
                Handles.DrawLine(origin + forward * radius + Vector3.up * phase.hitHeightRange,
                                 origin + forward * radius - Vector3.up * phase.hitHeightRange);
                Handles.color = main;
            }

            Handles.Label(origin + Vector3.up * 0.15f,
                $"{atk.SourceName}  P{phaseIndex}\n" +
                $"dmg {phase.damage:0.#} / poise {phase.poiseDamage:0.#} / break {phase.breakDamage:0.#}\n" +
                $"r {radius:0.##}  h±{phase.hitHeightRange:0.##}  {phase.reactionType}");

            if (_combatEditHitbox)
                DrawCombatHitboxHandles(atk, phase, actorTr, origin, radius);
        }

        void DrawCombatHitboxHandles(
            CombatTimelineUtility.ResolvedAttack atk, HitPhaseData phase,
            Transform actorTr, Vector3 origin, float radius)
        {
            // 반경 핸들
            EditorGUI.BeginChangeCheck();
            float newRadius = Handles.RadiusHandle(Quaternion.identity, origin, radius);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(atk.Owner, "히트박스 반경 수정");
                phase.attackRadius = Mathf.Max(0.05f, newRadius);
                EditorUtility.SetDirty(atk.Owner);
                Repaint();
            }

            // 높이 오프셋 핸들 (attackOffset.y)
            EditorGUI.BeginChangeCheck();
            Vector3 newOrigin = Handles.Slider(origin, Vector3.up,
                HandleUtility.GetHandleSize(origin) * 0.35f, Handles.ArrowHandleCap, 0f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(atk.Owner, "히트박스 높이 수정");
                phase.attackOffset.y = newOrigin.y - actorTr.position.y;
                EditorUtility.SetDirty(atk.Owner);
                Repaint();
            }
        }
    }
}
