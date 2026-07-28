using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Projectile;
using UPlayGround.Tool.Editor.Combat;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// 애니메이션 에디터 전투 오버레이 확장.
    /// 현재 MotionSetAsset에 매칭되는 AbilitySetSO 공격 Payload를 찾아
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

        AbilitySetSO _combatAttackData;
        bool _combatPrefsLoaded;
        bool _showCombatOverlay = true;
        bool _combatEditHitbox;
        int  _combatGizmoPhase = -1;          // -1 = 커서 시간 기준 자동
        int  _combatAttackIndex;              // 같은 MotionSetAsset을 쓰는 항목이 여럿일 때 선택
        MotionSetAsset _combatManualMotion;   // 현재 에셋이 없을 때 수동 지정

        // 자동 페어링을 컨텍스트(세트)당 1회만 시도
        UnityEngine.Object _combatPairingTriedFor;

        List<CombatTimelineUtility.ResolvedAttack> _combatResolved = new();

        // ─────────────────────────────────────────────────────────────────
        //  컨트롤 바 (OnGUI에서 호출)
        // ─────────────────────────────────────────────────────────────────
        // 패널 GUI만 그린다. 트랙 갱신(RefreshCombatOverlayTracks)은 패널 접힘 여부와
        // 무관하게 주기적으로 실행돼야 하므로 RunControlPanelSideEffects()로 분리했다.
        void DrawCombatOverlayPanel()
        {
            LoadCombatPrefsOnce();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    bool show = EditorGUILayout.ToggleLeft("표시", _showCombatOverlay, GUILayout.Width(50));
                    if (show != _showCombatOverlay)
                    {
                        _showCombatOverlay = show;
                        EditorPrefs.SetBool(PREFS_COMBAT_SHOW, show);
                        SceneView.RepaintAll();
                    }

                    EditorGUI.BeginChangeCheck();
                    var newData = (AbilitySetSO)EditorGUILayout.ObjectField(
                        _combatAttackData, typeof(AbilitySetSO), false, GUILayout.MinWidth(160));
                    if (EditorGUI.EndChangeCheck())
                    {
                        _combatAttackData = newData;
                        SaveCombatPairing();
                        RefreshMotionListView();
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
            MotionSetAsset motionAsset = GetCombatMotionAsset();

            EditorGUILayout.BeginHorizontal();
            {
                if (_asset == null)
                {
                    EditorGUILayout.LabelField("모션", GUILayout.Width(40));
                    _combatManualMotion = (MotionSetAsset)EditorGUILayout.ObjectField(
                        _combatManualMotion, typeof(MotionSetAsset), false, GUILayout.Width(190));
                }

                if (_combatAttackData == null)
                {
                    EditorGUILayout.LabelField("공격 데이터 없음 — 에셋을 지정하거나 [자동 연결] (몬스터 전용 탐색)", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                    return;
                }

                ResolveCombatAttacks(motionAsset);

                if (motionAsset == null)
                {
                    EditorGUILayout.LabelField("모션 키 미선택", EditorStyles.miniLabel);
                }
                else if (_combatResolved.Count == 0)
                {
                    EditorGUILayout.LabelField($"'{motionAsset.name}'를 쓰는 공격 데이터가 없습니다.", EditorStyles.miniLabel);
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
        MotionSetAsset GetCombatMotionAsset()
        {
            return _asset != null ? _asset : _combatManualMotion;
        }

        UnityEngine.Object GetCombatPairingContext()
        {
            return _actorAnimationSet != null ? (UnityEngine.Object)_actorAnimationSet : _asset;
        }

        void ResolveCombatAttacks(MotionSetAsset motionAsset)
        {
            _combatResolved = CombatTimelineUtility.ResolveAttacks(_combatAttackData, motionAsset);
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
                var found = CombatTimelineUtility.FindAbilitySetForMotionSet(
                    _actorAnimationSet,
                    out var owner,
                    out bool ambiguous,
                    out string candidateSummary);
                if (found != null)
                {
                    _combatAttackData = found;
                    SaveCombatPairing();
                    RefreshMotionListView();
                    ShowNotification(new GUIContent($"연결: {owner.name} → {found.name}"));
                    return;
                }
                if (ambiguous)
                {
                    ShowNotification(
                        new GUIContent(
                            $"자동 연결 후보가 모호합니다.\n{candidateSummary}\n"
                            + "AbilitySet을 직접 선택해 주세요."));
                    return;
                }
            }

            var cached = CombatTimelineUtility.LoadAttackDataPairing(context);
            if (cached != null)
            {
                _combatAttackData = cached;
                RefreshMotionListView();
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

            ResolveCombatAttacks(GetCombatMotionAsset());
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

            // ② 캔슬 윈도우 (Move 제외 마스크)
            //   - 저작(CancelWindowEvent 타임라인 이벤트) 있음: 이벤트 구간을 실선으로, 스팬별 effective
            //     마스크를 라벨로. 런타임 PlayerCombat.ResolveCancelMask와 동일 기준(이벤트 활성=캔슬).
            //   - 없음(폴백): 콜리전 비활성 구간(complement)을 [자동] 점선으로 = "자동 추론(미저작)" 구분.
            PlayerInterruptAction nonMove = atk.InterruptActions & ~PlayerInterruptAction.Move;
            var cancelEvents = CombatTimelineUtility.CollectCancelWindowSpans(set);
            if (nonMove != PlayerInterruptAction.None)
            {
                if (cancelEvents.Count > 0)
                {
                    var cancelTrack = new MotionSetDrawer.OverlayTrack { label = "✂ 캔슬 (저작)", color = COL_COMBAT_CANCEL };
                    foreach (var ce in cancelEvents)
                    {
                        // 런타임 ResolveCancelMask와 동일: maskOverride 없으면 전역, 있으면 전역과 교집합.
                        PlayerInterruptAction eff = (ce.Mask == PlayerInterruptAction.None
                            ? atk.InterruptActions
                            : (atk.InterruptActions & ce.Mask)) & ~PlayerInterruptAction.Move;
                        bool dead = eff == PlayerInterruptAction.None;
                        // 저작 캔슬 구간이 콜리전(액티브 히트)과 겹치면 런타임 ResolveCancelMask가 액티브 히트
                        // 가드(IsPossibleCollide)를 우회한다 → 히트 도중 캔슬이 열린다. 저작 실수일 수 있어 경고 표기.
                        bool overlapsActive = !dead && OverlapsCollision(ce.Start, ce.End, collisions);
                        string label = dead
                            ? "⚠ 마스크없음"
                            : (overlapsActive
                                ? $"⚠ 히트중 {CombatTimelineUtility.FormatInterruptMask(eff)}"
                                : CombatTimelineUtility.FormatInterruptMask(eff));
                        cancelTrack.spans.Add(new MotionSetDrawer.OverlaySpan
                        {
                            start = ce.Start, end = ce.End,
                            label = label,
                            dashed = dead,
                        });
                    }
                    tracks.Add(cancelTrack);
                }
                else
                {
                    // 런타임 폴백과 일치: 콜리전 complement를 [자동] 점선으로.
                    string maskText = CombatTimelineUtility.FormatInterruptMask(nonMove);
                    var cancelTrack = new MotionSetDrawer.OverlayTrack { label = $"✂ 캔슬 ({maskText}) [자동]", color = COL_COMBAT_CANCEL };
                    foreach (var span in CombatTimelineUtility.ComputeComplementSpans(collisions, total))
                        cancelTrack.spans.Add(new MotionSetDrawer.OverlaySpan { start = span.Start, end = span.End, label = maskText, dashed = true });
                    tracks.Add(cancelTrack);
                }
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

        // 저작 캔슬 구간 [start, end)가 콜리전(액티브 히트) 구간 중 하나와 겹치는지.
        static bool OverlapsCollision(float start, float end, List<CombatTimelineUtility.TimedSpan> collisions)
        {
            if (collisions == null) return false;
            foreach (var c in collisions)
                if (start < c.End && c.Start < end)
                    return true;
            return false;
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
            string eventHitboxGroupId = null;
            List<string> eventAdditionalHitboxGroupIds = null;
            bool isActive = false;
            foreach (var span in CombatTimelineUtility.CollectCollisionSpans(set))
            {
                if (time < span.Start || time > span.End) continue;
                phaseIndex = span.PhaseIndex;
                eventHitboxGroupId = span.HitboxGroupId;
                eventAdditionalHitboxGroupIds = span.AdditionalHitboxGroupIds;
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
            DrawProjectileTrajectory(phase);

            string attachedGroupId = !string.IsNullOrWhiteSpace(eventHitboxGroupId)
                ? eventHitboxGroupId
                : phase.hitboxGroupId;
            DrawAttachedCombatHitboxes(
                atk,
                phase,
                phaseIndex,
                attachedGroupId,
                isActive);

            if (isActive && eventAdditionalHitboxGroupIds != null)
            {
                foreach (string additionalGroupId in eventAdditionalHitboxGroupIds)
                {
                    DrawAttachedCombatHitboxes(
                        atk,
                        phase,
                        phaseIndex,
                        additionalGroupId,
                        true);
                }
            }
        }

        void DrawProjectileTrajectory(HitPhaseData phase)
        {
            ProjectileDefinitionSO definition = phase?.projectileDefinition;
            if (definition == null || definition.motion == null || _targetActor == null)
                return;

            Vector3 origin = _targetActor.transform.position + Vector3.up;
            Vector3 forward = _targetActor.transform.forward.normalized;
            Handles.color = new Color(0.2f, 0.85f, 1f, 0.9f);

            switch (definition.motion)
            {
                case ArcProjectileMotion arc:
                    float distance = Mathf.Max(1f, arc.speed * definition.lifetime);
                    Vector3 previous = origin;
                    for (int i = 1; i <= 24; i++)
                    {
                        float t = i / 24f;
                        float curved = arc.progressCurve.Evaluate(t);
                        Vector3 point = origin + forward * (distance * curved)
                                        + Vector3.up * (4f * curved * (1f - curved) * arc.arcHeight);
                        Handles.DrawLine(previous, point);
                        previous = point;
                    }
                    break;

                case HitscanProjectileMotion hitscan:
                    Handles.DrawAAPolyLine(3f, origin, origin + forward * hitscan.range);
                    break;

                case StationaryProjectileMotion:
                    Handles.DrawWireDisc(origin, Vector3.up, definition.collisionRadius);
                    break;

                case OrbitProjectileMotion orbit:
                    Handles.DrawWireDisc(origin, Vector3.up, orbit.radius);
                    break;

                case HomingProjectileMotion homing:
                    Handles.DrawDottedLine(
                        origin,
                        origin + forward * (homing.speed * definition.lifetime),
                        4f);
                    break;

                case LinearProjectileMotion linear:
                    Handles.DrawLine(
                        origin,
                        origin + forward * (linear.speed * definition.lifetime));
                    break;
            }

            Handles.Label(origin + Vector3.up * 0.25f, $"Projectile: {definition.name}");
        }

        bool DrawAttachedCombatHitboxes(
            CombatTimelineUtility.ResolvedAttack atk,
            HitPhaseData phase,
            int phaseIndex,
            string groupId,
            bool isActive)
        {
            string resolvedGroup = string.IsNullOrWhiteSpace(groupId)
                ? CombatHitbox.DefaultGroupId
                : groupId.Trim();
            CombatHitbox[] all = _targetActor.GetComponentsInChildren<CombatHitbox>(true);
            var matched = new List<CombatHitbox>();
            foreach (CombatHitbox hitbox in all)
            {
                if (hitbox != null
                    && string.Equals(
                        hitbox.GroupId,
                        resolvedGroup,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    matched.Add(hitbox);
                }
            }

            Color main = isActive ? COL_COMBAT_ACTIVE : new Color(1f, 0.6f, 0.2f, 0.8f);
            if (matched.Count == 0)
            {
                Handles.color = new Color(1f, 0.45f, 0.1f);
                Handles.Label(
                    _targetActor.transform.position + Vector3.up * 1.5f,
                    $"⛔ 필수 HitBox 그룹 없음: {resolvedGroup}");
                return false;
            }

            Handles.color = main;
            foreach (CombatHitbox hitbox in matched)
            {
                if (!hitbox.TryGetWorldShape(out CombatHitboxShape shape))
                    continue;

                if (shape.Type == CombatHitboxShapeType.Box)
                    DrawAttachedBox(hitbox, shape, main);
                else
                    DrawAttachedCapsule(hitbox, shape, main);
            }

            CombatHitbox first = matched[0];
            Vector3 labelPosition = first.TryGetWorldShape(out CombatHitboxShape firstShape)
                ? firstShape.Center
                : _targetActor.transform.position + Vector3.up;
            Handles.Label(
                labelPosition + Vector3.up * 0.15f,
                $"{atk.SourceName}  P{phaseIndex} / {resolvedGroup} / {matched.Count}개\n" +
                $"dmg {phase.damage:0.#} / poise {phase.poiseDamage:0.#} / break {phase.breakDamage:0.#}");
            return true;
        }

        void DrawAttachedBox(CombatHitbox hitbox, CombatHitboxShape shape, Color color)
        {
            Matrix4x4 previous = Handles.matrix;
            Handles.matrix = Matrix4x4.TRS(shape.Center, shape.Rotation, Vector3.one);
            Handles.color = color;
            Handles.DrawWireCube(Vector3.zero, shape.HalfExtents * 2f);

            if (_combatEditHitbox && hitbox.ShapeCollider is BoxCollider box)
            {
                var handle = new BoxBoundsHandle
                {
                    center = Vector3.zero,
                    size = shape.HalfExtents * 2f,
                    wireframeColor = color,
                    handleColor = color,
                };
                EditorGUI.BeginChangeCheck();
                handle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(box, "부착형 Box HitBox 수정");
                    Vector3 scale = Abs(box.transform.lossyScale);
                    box.center += new Vector3(
                        handle.center.x / Mathf.Max(0.0001f, scale.x),
                        handle.center.y / Mathf.Max(0.0001f, scale.y),
                        handle.center.z / Mathf.Max(0.0001f, scale.z));
                    box.size = new Vector3(
                        handle.size.x / Mathf.Max(0.0001f, scale.x),
                        handle.size.y / Mathf.Max(0.0001f, scale.y),
                        handle.size.z / Mathf.Max(0.0001f, scale.z));
                    EditorUtility.SetDirty(box);
                }
            }

            Handles.matrix = previous;
        }

        void DrawAttachedCapsule(CombatHitbox hitbox, CombatHitboxShape shape, Color color)
        {
            Handles.color = color;
            Handles.DrawWireDisc(shape.Point0, (shape.Point1 - shape.Point0).normalized, shape.Radius);
            Handles.DrawWireDisc(shape.Point1, (shape.Point1 - shape.Point0).normalized, shape.Radius);
            Handles.DrawWireArc(shape.Center, Vector3.up, Vector3.forward, 360f, shape.Radius);
            Handles.DrawLine(shape.Point0, shape.Point1);

            if (!_combatEditHitbox || hitbox.ShapeCollider is not CapsuleCollider capsule)
                return;

            EditorGUI.BeginChangeCheck();
            float radius = Handles.RadiusHandle(capsule.transform.rotation, shape.Center, shape.Radius);
            Vector3 center = Handles.PositionHandle(shape.Center, capsule.transform.rotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(capsule, "부착형 Capsule HitBox 수정");
                Vector3 scale = Abs(capsule.transform.lossyScale);
                int direction = Mathf.Clamp(capsule.direction, 0, 2);
                float radialScale = direction == 0
                    ? Mathf.Max(scale.y, scale.z)
                    : direction == 1
                        ? Mathf.Max(scale.x, scale.z)
                        : Mathf.Max(scale.x, scale.y);
                capsule.radius = radius / Mathf.Max(0.0001f, radialScale);
                capsule.center = capsule.transform.InverseTransformPoint(center);
                EditorUtility.SetDirty(capsule);
            }
        }

        static Vector3 Abs(Vector3 value)
            => new(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));

    }
}
