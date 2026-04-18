using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Combat;

namespace UPlayGround.Editor
{
    /// <summary>
    /// PlayerAttackDataSO 편집 UI 로직.
    /// CustomEditor 와 EditorWindow 가 공유합니다.
    /// </summary>
    public class PlayerAttackDataSODrawer
    {
        // ─── 탭 ─────────────────────────────────────────────────────────
        private static readonly string[] TabLabels = { "약공격", "강공격", "점프", "대쉬", "스킬", "카운터", "차지" };
        internal static readonly Color[] TabAccents =
        {
            new Color(0.35f, 0.55f, 1.00f),
            new Color(1.00f, 0.35f, 0.35f),
            new Color(0.30f, 0.90f, 0.55f),
            new Color(1.00f, 0.65f, 0.20f),
            new Color(0.75f, 0.35f, 1.00f),
            new Color(1.00f, 0.85f, 0.00f),
            new Color(1.00f, 0.50f, 0.15f),
        };
        private int _tab;

        // ─── SerializedObject / Property ────────────────────────────────
        private readonly SerializedObject _so;
        private SerializedProperty _liteList, _heavyList, _jumpList, _dashList, _skillList;
        private SerializedProperty _counter, _parryCounter;
        private SerializedProperty _chargeAnimKey, _chargeStages, _chargeThresholds;
        private SerializedProperty _vfxKey, _vfxSocket, _vfxOffset;

        // ─── 폴드아웃 상태 ───────────────────────────────────────────────
        private readonly Dictionary<string, List<bool>>       _cardFold  = new();
        private readonly Dictionary<string, List<List<bool>>> _phaseFold = new();

        // ─── 스타일 (지연 초기화) ────────────────────────────────────────
        private static GUIStyle _cardStyle;
        private static GUIStyle _phaseStyle;
        private static GUIStyle _tabStyleActive;
        private static GUIStyle _tabStyleNormal;

        private static void EnsureStyles()
        {
            if (_cardStyle != null) return;
            _cardStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 4, 6),
                margin  = new RectOffset(0, 0, 2, 2),
            };
            _phaseStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 4, 4),
                margin  = new RectOffset(4, 4, 2, 2),
            };
            _tabStyleActive = new GUIStyle(EditorStyles.miniButtonMid) { fontStyle = FontStyle.Bold, fontSize = 11, fixedHeight = 26 };
            _tabStyleNormal = new GUIStyle(EditorStyles.miniButtonMid) { fontSize = 11, fixedHeight = 26 };
        }

        // ─── 생성자 ──────────────────────────────────────────────────────
        public PlayerAttackDataSODrawer(SerializedObject so)
        {
            _so = so;
            _liteList         = so.FindProperty("liteComboAttackList");
            _heavyList        = so.FindProperty("heavyComboAttackList");
            _jumpList         = so.FindProperty("jumpAttackList");
            _dashList         = so.FindProperty("dashAttackList");
            _skillList        = so.FindProperty("skillAttackList");
            _counter          = so.FindProperty("counterAttack");
            _parryCounter     = so.FindProperty("parryCounterAttack");
            _chargeAnimKey    = so.FindProperty("chargeAnimKey");
            _chargeStages     = so.FindProperty("chargeStages");
            _chargeThresholds = so.FindProperty("chargeStageThresholds");
            _vfxKey           = so.FindProperty("fullChargeVfxKey");
            _vfxSocket        = so.FindProperty("fullChargeVfxSocket");
            _vfxOffset        = so.FindProperty("fullChargeVfxOffset");
        }

        // ═══════════════════════════════════════════════════════════════
        //  진입점
        // ═══════════════════════════════════════════════════════════════

        public void DrawGUI()
        {
            EnsureStyles();
            DrawTabBar();
            EditorGUILayout.Space(6);

            Color accent = TabAccents[_tab];
            switch (_tab)
            {
                case 0: DrawAttackList(_liteList,  "약공격 콤보", "lite",  accent); break;
                case 1: DrawAttackList(_heavyList, "강공격 콤보", "heavy", accent); break;
                case 2: DrawAttackList(_jumpList,  "점프 공격",   "jump",  accent); break;
                case 3: DrawAttackList(_dashList,  "대쉬 공격",   "dash",  accent); break;
                case 4: DrawAttackList(_skillList, "스킬 공격",   "skill", accent); break;
                case 5: DrawCounterAttack(accent); break;
                case 6: DrawChargeSection(accent); break;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  ① 탭 바 — 공격 수 배지 + 빈 탭 흐리게
        // ═══════════════════════════════════════════════════════════════

        private int GetTabCount(int tabIndex) => tabIndex switch
        {
            0 => _liteList.arraySize,
            1 => _heavyList.arraySize,
            2 => _jumpList.arraySize,
            3 => _dashList.arraySize,
            4 => _skillList.arraySize,
            5 => 1,                        // 카운터는 단일 구조
            6 => _chargeStages.arraySize,
            _ => 0,
        };

        private void DrawTabBar()
        {
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < TabLabels.Length; i++)
            {
                bool active = i == _tab;
                int  count  = GetTabCount(i);
                bool empty  = count == 0 && i != 5; // 카운터는 항상 표시

                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = active
                    ? new Color(TabAccents[i].r, TabAccents[i].g, TabAccents[i].b, 0.9f)
                    : empty
                        ? new Color(0.18f, 0.18f, 0.18f, 0.8f)
                        : new Color(0.25f, 0.25f, 0.25f, 0.8f);

                var style = active ? _tabStyleActive : _tabStyleNormal;

                // 카운터(5)·차지(6)는 단일 구조라 숫자 불필요
                string label = (i == 5 || i == 6) ? TabLabels[i] : $"{TabLabels[i]} ({count})";

                Color prevContent = GUI.contentColor;
                if (empty && !active) GUI.contentColor = new Color(1f, 1f, 1f, 0.4f);

                if (GUILayout.Button(label, style))
                    _tab = i;

                GUI.contentColor    = prevContent;
                GUI.backgroundColor = prev;
            }
            EditorGUILayout.EndHorizontal();

            Rect bar = GUILayoutUtility.GetRect(0, 3, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(bar, TabAccents[_tab]);
        }

        // ═══════════════════════════════════════════════════════════════
        //  공격 리스트
        // ═══════════════════════════════════════════════════════════════

        private void DrawAttackList(SerializedProperty list, string title, string key, Color accent)
        {
            EnsureFoldLists(key, list.arraySize);

            DrawSectionHeader(title, accent);

            // ④ 전체 펼치기 / 접기
            if (list.arraySize > 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("▼ 전체 펼치기", EditorStyles.miniButton, GUILayout.Width(92)))
                    for (int i = 0; i < _cardFold[key].Count; i++) _cardFold[key][i] = true;
                if (GUILayout.Button("▶ 전체 접기", EditorStyles.miniButton, GUILayout.Width(78)))
                    for (int i = 0; i < _cardFold[key].Count; i++) _cardFold[key][i] = false;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);

            if (list.arraySize == 0)
                EditorGUILayout.HelpBox("공격 데이터가 없습니다. 아래 버튼으로 추가하세요.", MessageType.Info);

            int removeAt    = -1;
            int duplicateAt = -1;

            for (int i = 0; i < list.arraySize; i++)
            {
                var (del, dup) = DrawAttackCard(list.GetArrayElementAtIndex(i), i, list.arraySize, key, accent);
                if (del) removeAt    = i;
                if (dup) duplicateAt = i;
            }

            if (removeAt >= 0)
            {
                list.DeleteArrayElementAtIndex(removeAt);
                _cardFold[key].RemoveAt(removeAt);
                _phaseFold[key].RemoveAt(removeAt);
            }
            else if (duplicateAt >= 0)
            {
                // InsertArrayElementAtIndex: 해당 인덱스에 복사본 삽입 (원본은 +1로 밀림)
                // MoveArrayElement로 원본/복사본 위치를 교환 → 원본 유지, 복사본이 바로 뒤에
                list.InsertArrayElementAtIndex(duplicateAt);
                list.MoveArrayElement(duplicateAt, duplicateAt + 1);
                _cardFold[key].Insert(duplicateAt + 1, true);
                _phaseFold[key].Insert(duplicateAt + 1, new List<bool>());
            }

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(accent.r * 0.6f, accent.g * 0.6f, accent.b * 0.6f, 1f);
            if (GUILayout.Button("+ 공격 추가", GUILayout.Height(28)))
            {
                list.arraySize++;
                _cardFold[key].Add(true);
                _phaseFold[key].Add(new List<bool>());
                SerializedProperty np = list.GetArrayElementAtIndex(list.arraySize - 1);
                SerializedProperty hp = np.FindPropertyRelative("baseInfo.hitPhases");
                if (hp.arraySize == 0) hp.arraySize = 1;
            }
            GUI.backgroundColor = Color.white;
        }

        // ─── 공격 카드 ─────────────────────────────────────────────────

        private (bool deleted, bool duplicated) DrawAttackCard(
            SerializedProperty prop, int index, int total, string key, Color accent)
        {
            SerializedProperty baseInfo   = prop.FindPropertyRelative("baseInfo");
            SerializedProperty animKeyP   = baseInfo.FindPropertyRelative("animKey");
            SerializedProperty typeP      = baseInfo.FindPropertyRelative("attackType");
            SerializedProperty phasesP    = baseInfo.FindPropertyRelative("hitPhases");
            SerializedProperty interruptP = prop.FindPropertyRelative("canBeInterrupted");
            SerializedProperty angleP     = prop.FindPropertyRelative("hitAngle");

            // ② 헤더에 Phase 0 데미지 표시
            float phase0Dmg = phasesP.arraySize > 0
                ? phasesP.GetArrayElementAtIndex(0).FindPropertyRelative("damage").floatValue
                : 0f;

            bool fold      = _cardFold[key][index];
            bool deleted   = false;
            bool duplicated = false;

            EditorGUILayout.BeginVertical(_cardStyle);

            string animLabel = animKeyP.enumDisplayNames[animKeyP.enumValueIndex];
            string summary   = $"  [{index + 1}]  {animLabel}   |   Phase {phasesP.arraySize}   |   DMG {phase0Dmg:F0}   |   각도 {angleP.floatValue:F0}°   |   {(interruptP.boolValue ? "캔슬 O" : "캔슬 X")}";
            Color  bgColor   = new Color(accent.r, accent.g, accent.b, 0.18f);

            bool newFold = DrawCardHeaderRow(summary, bgColor, fold, index > 0, index < total - 1,
                                             out bool clickedUp, out bool clickedDown,
                                             out bool clickedDel, out bool clickedDup);
            _cardFold[key][index] = newFold;

            if (clickedUp)   MoveElement(key, _liteList, _heavyList, _jumpList, _dashList, _skillList, prop, index, -1);
            if (clickedDown) MoveElement(key, _liteList, _heavyList, _jumpList, _dashList, _skillList, prop, index, +1);
            if (clickedDel)  deleted    = true;
            if (clickedDup)  duplicated = true;

            if (newFold)
            {
                EditorGUILayout.Space(4);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("기본 정보", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(animKeyP,   new GUIContent("AnimKey"));
                    EditorGUILayout.PropertyField(typeP,      new GUIContent("공격 타입"));
                    EditorGUILayout.PropertyField(interruptP, new GUIContent("캔슬 가능"));
                    EditorGUILayout.PropertyField(angleP,     new GUIContent("판정 각도 (°)"));
                }

                EditorGUILayout.Space(4);
                DrawHitPhaseList(phasesP, key, index, accent);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
            return (deleted, duplicated);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Hit Phase 리스트
        // ═══════════════════════════════════════════════════════════════

        private void DrawHitPhaseList(SerializedProperty phases, string key, int cardIdx, Color accent)
        {
            var folds = _phaseFold[key][cardIdx];
            while (folds.Count < phases.arraySize) folds.Add(true);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                Rect headerRow = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
                EditorGUI.LabelField(
                    new Rect(headerRow.x + 4, headerRow.y, headerRow.width - 70, headerRow.height),
                    $"Hit Phases  ({phases.arraySize}개)", EditorStyles.miniBoldLabel);

                Rect addBtn = new Rect(headerRow.xMax - 66, headerRow.y + 1, 62, 18);
                GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f, 1f);
                if (GUI.Button(addBtn, "+ Phase", EditorStyles.miniButton))
                {
                    phases.arraySize++;
                    folds.Add(true);
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.Space(2);

                int removePhase = -1;
                for (int p = 0; p < phases.arraySize; p++)
                {
                    bool del = DrawHitPhaseCard(phases.GetArrayElementAtIndex(p), p, folds, accent);
                    if (del) removePhase = p;
                }

                if (removePhase >= 0)
                {
                    phases.DeleteArrayElementAtIndex(removePhase);
                    folds.RemoveAt(removePhase);
                }
            }
        }

        // ─── Hit Phase 카드 ───────────────────────────────────────────

        private bool DrawHitPhaseCard(SerializedProperty phase, int index, List<bool> folds, Color accent)
        {
            SerializedProperty damageP   = phase.FindPropertyRelative("damage");
            SerializedProperty poiseP    = phase.FindPropertyRelative("poiseDamage");
            SerializedProperty reactionP = phase.FindPropertyRelative("reactionType");

            bool fold    = folds[index];
            bool deleted = false;

            EditorGUILayout.BeginVertical(_phaseStyle);

            string reactionLabel = reactionP.enumDisplayNames[reactionP.enumValueIndex];
            string summary = $"  Phase {index}  |  데미지 {damageP.floatValue:F0}  |  포이즈 {poiseP.floatValue:F0}  |  {reactionLabel}";
            Color  phBg    = new Color(accent.r * 0.5f, accent.g * 0.5f, accent.b * 0.5f, 0.20f);

            bool newFold = DrawCardHeaderRow(summary, phBg, fold, false, false,
                                             out _, out _, out bool clickedDel, out _,
                                             showMoveButtons: false, showDuplicateButton: false,
                                             showDeleteButton: index > 0);
            folds[index] = newFold;
            if (clickedDel) deleted = true;

            if (newFold)
            {
                EditorGUILayout.Space(3);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("데미지", EditorStyles.miniBoldLabel);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(damageP, new GUIContent("데미지"));
                    EditorGUILayout.PropertyField(poiseP,  new GUIContent("포이즈 데미지"));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.PropertyField(reactionP, new GUIContent("반응 타입"));
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("히트박스", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(phase.FindPropertyRelative("attackOffset"),   new GUIContent("오프셋"));
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(phase.FindPropertyRelative("attackRadius"),   new GUIContent("반경"));
                    EditorGUILayout.PropertyField(phase.FindPropertyRelative("hitHeightRange"), new GUIContent("높이 범위 (-1=무제한)"));
                    EditorGUILayout.EndHorizontal();
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("이펙트", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(phase.FindPropertyRelative("hitParticleName"), new GUIContent("히트 파티클"));
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("반응 힘", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(phase.FindPropertyRelative("pullForce"),      new GUIContent("끌어당기기 힘"));
                    EditorGUILayout.PropertyField(phase.FindPropertyRelative("airborneForce"),  new GUIContent("공중 띄우기 힘"));
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(phase.FindPropertyRelative("knockBackForce"), new GUIContent("넉백 힘"));
                    EditorGUILayout.PropertyField(phase.FindPropertyRelative("knockBackDrag"),  new GUIContent("넉백 감속"));
                    EditorGUILayout.EndHorizontal();
                }

                string rStr = reactionP.enumDisplayNames[reactionP.enumValueIndex];
                if (rStr.Contains("Grab"))
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField("그랩", EditorStyles.miniBoldLabel);
                        EditorGUILayout.PropertyField(phase.FindPropertyRelative("grabDuration"), new GUIContent("지속 시간 (초)"));
                    }
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(1);
            return deleted;
        }

        // ═══════════════════════════════════════════════════════════════
        //  카운터 공격 탭
        // ═══════════════════════════════════════════════════════════════

        private void DrawCounterAttack(Color accent)
        {
            EnsureFoldLists("counter", 1);
            EnsureFoldLists("parryCounter", 1);

            DrawSectionHeader("퍼펙트 가드 반격", accent);
            EditorGUILayout.HelpBox("비워두면 강공격 첫 번째 데이터로 대체됩니다.", MessageType.Info);
            EditorGUILayout.Space(4);

            DrawCounterAttackField(_counter, "counter", accent);

            EditorGUILayout.Space(12);
            DrawSectionHeader("패리 반격", accent);
            EditorGUILayout.HelpBox("비워두면 퍼펙트 가드 반격 데이터로 대체됩니다.", MessageType.Info);
            EditorGUILayout.Space(4);

            DrawCounterAttackField(_parryCounter, "parryCounter", accent);
        }

        private void DrawCounterAttackField(SerializedProperty prop, string key, Color accent)
        {
            SerializedProperty baseInfo   = prop.FindPropertyRelative("baseInfo");
            SerializedProperty animKeyP   = baseInfo.FindPropertyRelative("animKey");
            SerializedProperty typeP      = baseInfo.FindPropertyRelative("attackType");
            SerializedProperty phasesP    = baseInfo.FindPropertyRelative("hitPhases");
            SerializedProperty interruptP = prop.FindPropertyRelative("canBeInterrupted");
            SerializedProperty angleP     = prop.FindPropertyRelative("hitAngle");

            EditorGUILayout.BeginVertical(_cardStyle);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("기본 정보", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(animKeyP,   new GUIContent("AnimKey"));
                EditorGUILayout.PropertyField(typeP,      new GUIContent("공격 타입"));
                EditorGUILayout.PropertyField(interruptP, new GUIContent("캔슬 가능"));
                EditorGUILayout.PropertyField(angleP,     new GUIContent("판정 각도 (°)"));
            }

            EditorGUILayout.Space(4);
            DrawHitPhaseList(phasesP, key, 0, accent);

            EditorGUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════════════════
        //  차지 탭
        // ═══════════════════════════════════════════════════════════════

        private void DrawChargeSection(Color accent)
        {
            EnsureFoldLists("charge", _chargeStages.arraySize);

            DrawSectionHeader("차지 공격", accent);
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("공통 설정", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(_chargeAnimKey, new GUIContent("차지 AnimKey"));
                EditorGUILayout.HelpBox("MotionSet 안의 InfiniteLoop 수 = chargeStages 수와 일치시켜야 합니다.", MessageType.Info);
            }

            EditorGUILayout.Space(6);

            if (_chargeStages.arraySize > 0)
                DrawThresholdBar(accent, _chargeStages.arraySize);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("단계 임계값  (비워두면 균등 분배,  요소 수 = 단계 수 - 1)", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(_chargeThresholds, new GUIContent("Thresholds"), true);
            }

            EditorGUILayout.Space(6);
            DrawSectionHeader("차지 단계 데이터", accent);

            // ④ 전체 펼치기 / 접기
            if (_chargeStages.arraySize > 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("▼ 전체 펼치기", EditorStyles.miniButton, GUILayout.Width(92)))
                    for (int i = 0; i < _cardFold["charge"].Count; i++) _cardFold["charge"][i] = true;
                if (GUILayout.Button("▶ 전체 접기", EditorStyles.miniButton, GUILayout.Width(78)))
                    for (int i = 0; i < _cardFold["charge"].Count; i++) _cardFold["charge"][i] = false;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);

            int removeAt    = -1;
            int duplicateAt = -1;

            for (int i = 0; i < _chargeStages.arraySize; i++)
            {
                var (del, dup) = DrawChargeStageCard(_chargeStages.GetArrayElementAtIndex(i), i, _chargeStages.arraySize, accent);
                if (del) removeAt    = i;
                if (dup) duplicateAt = i;
            }

            if (removeAt >= 0)
            {
                _chargeStages.DeleteArrayElementAtIndex(removeAt);
                _cardFold["charge"].RemoveAt(removeAt);
                _phaseFold["charge"].RemoveAt(removeAt);
            }
            else if (duplicateAt >= 0)
            {
                _chargeStages.InsertArrayElementAtIndex(duplicateAt);
                _chargeStages.MoveArrayElement(duplicateAt, duplicateAt + 1);
                _cardFold["charge"].Insert(duplicateAt + 1, true);
                _phaseFold["charge"].Insert(duplicateAt + 1, new List<bool>());
            }

            GUI.backgroundColor = new Color(accent.r * 0.6f, accent.g * 0.6f, accent.b * 0.6f, 1f);
            if (GUILayout.Button("+ 단계 추가", GUILayout.Height(28)))
            {
                _chargeStages.arraySize++;
                _cardFold["charge"].Add(true);
                _phaseFold["charge"].Add(new List<bool>());
                SerializedProperty np = _chargeStages.GetArrayElementAtIndex(_chargeStages.arraySize - 1);
                SerializedProperty hp = np.FindPropertyRelative("hitPhases");
                if (hp.arraySize == 0) hp.arraySize = 1;
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(8);

            DrawSectionHeader("풀 차지 VFX", accent);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_vfxKey,    new GUIContent("VFX Key"));
                EditorGUILayout.PropertyField(_vfxSocket, new GUIContent("소켓 타입"));
                EditorGUILayout.PropertyField(_vfxOffset, new GUIContent("로컬 오프셋"));
            }
        }

        // ─── 임계값 바 시각화 ─────────────────────────────────────────

        private void DrawThresholdBar(Color accent, int stageCount)
        {
            EditorGUILayout.LabelField("단계 분포 미리보기", EditorStyles.miniBoldLabel);
            Rect bar = GUILayoutUtility.GetRect(0, 26, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(bar, new Color(0.1f, 0.1f, 0.1f, 0.6f));

            var thresholds = new List<float>();
            for (int i = 0; i < _chargeThresholds.arraySize; i++)
                thresholds.Add(_chargeThresholds.GetArrayElementAtIndex(i).floatValue);

            if (thresholds.Count < stageCount - 1)
            {
                thresholds.Clear();
                for (int i = 1; i < stageCount; i++)
                    thresholds.Add((float)i / stageCount);
            }

            float[] hues = { 0.33f, 0.55f, 0.05f, 0.10f, 0.80f };
            float   prev = 0f;
            for (int s = 0; s < stageCount; s++)
            {
                float next = s < thresholds.Count ? Mathf.Clamp01(thresholds[s]) : 1f;
                Rect  seg  = new Rect(bar.x + prev * bar.width + 1, bar.y + 2,
                                      (next - prev) * bar.width - 2, bar.height - 4);
                EditorGUI.DrawRect(seg, Color.HSVToRGB(hues[s % hues.Length], 0.7f, 0.75f) * new Color(1, 1, 1, 0.8f));
                EditorGUI.LabelField(new Rect(seg.x + 4, seg.y, seg.width, seg.height),
                                     $"{s + 1}단계", EditorStyles.whiteMiniLabel);
                prev = next;
            }
            EditorGUILayout.Space(4);
        }

        // ─── 차지 단계 카드 ───────────────────────────────────────────

        private (bool deleted, bool duplicated) DrawChargeStageCard(SerializedProperty stage, int index, int total, Color accent)
        {
            SerializedProperty phasesP    = stage.FindPropertyRelative("hitPhases");
            SerializedProperty interruptP = stage.FindPropertyRelative("canBeInterrupted");
            SerializedProperty angleP     = stage.FindPropertyRelative("hitAngle");

            bool fold      = _cardFold["charge"][index];
            bool deleted   = false;
            bool duplicated = false;

            EditorGUILayout.BeginVertical(_cardStyle);

            string summary = $"  [{index + 1}단계]  Phase {phasesP.arraySize}   |   각도 {angleP.floatValue:F0}°   |   {(interruptP.boolValue ? "캔슬 O" : "캔슬 X")}";
            Color  bgColor = new Color(accent.r, accent.g, accent.b, 0.18f);

            bool newFold = DrawCardHeaderRow(summary, bgColor, fold, index > 0, index < total - 1,
                                             out bool cu, out bool cd, out bool cDel, out bool cDup);
            _cardFold["charge"][index] = newFold;

            if (cu)   SwapChargeStages(index, index - 1);
            if (cd)   SwapChargeStages(index, index + 1);
            if (cDel) deleted    = true;
            if (cDup) duplicated = true;

            if (newFold)
            {
                EditorGUILayout.Space(4);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("단계 설정", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(interruptP, new GUIContent("캔슬 가능"));
                    EditorGUILayout.PropertyField(angleP,     new GUIContent("판정 각도 (°)"));
                }
                EditorGUILayout.Space(4);
                DrawHitPhaseList(phasesP, "charge", index, accent);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
            return (deleted, duplicated);
        }

        private void SwapChargeStages(int a, int b)
        {
            _chargeStages.MoveArrayElement(a, b);
            SwapList(_cardFold["charge"], a, b);
            SwapList(_phaseFold["charge"], a, b);
        }

        // ═══════════════════════════════════════════════════════════════
        //  공통 헬퍼
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 전체 폭을 하나의 Rect로 차지하는 카드 헤더 행.
        /// 버튼 순서 (우→좌): 삭제[✕] — 복사[⧉] — 아래[↓] — 위[↑]
        /// </summary>
        private static bool DrawCardHeaderRow(
            string text, Color bgColor, bool fold,
            bool canUp, bool canDown,
            out bool clickedUp, out bool clickedDown,
            out bool clickedDelete, out bool clickedDuplicate,
            bool showMoveButtons      = true,
            bool showDuplicateButton  = true,
            bool showDeleteButton     = true)
        {
            clickedUp = clickedDown = clickedDelete = clickedDuplicate = false;

            const float btnW = 22f, btnH = 22f, gap = 2f, margin = 4f;

            Rect row  = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(row, bgColor);

            float rx   = row.xMax - margin;
            float btnY = row.y + (row.height - btnH) * 0.5f;

            Rect delRect = Rect.zero, dupRect = Rect.zero, downRect = Rect.zero, upRect = Rect.zero;

            if (showDeleteButton)
            {
                rx -= btnW;
                delRect = new Rect(rx, btnY, btnW, btnH);
                rx -= gap;
            }
            if (showDuplicateButton)
            {
                rx -= btnW;
                dupRect = new Rect(rx, btnY, btnW, btnH);
                rx -= gap;
            }
            if (showMoveButtons)
            {
                rx -= btnW;
                downRect = new Rect(rx, btnY, btnW, btnH);
                rx -= gap + btnW;
                upRect = new Rect(rx, btnY, btnW, btnH);
                rx -= gap;
            }

            Rect   foldRect = new Rect(row.x + margin, row.y, rx - row.x - margin, row.height);
            string icon     = fold ? "▼" : "▶";
            if (GUI.Button(foldRect, icon + text, EditorStyles.boldLabel))
                fold = !fold;

            if (showMoveButtons)
            {
                EditorGUI.BeginDisabledGroup(!canUp);
                if (GUI.Button(upRect, "↑", EditorStyles.miniButton)) clickedUp = true;
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(!canDown);
                if (GUI.Button(downRect, "↓", EditorStyles.miniButton)) clickedDown = true;
                EditorGUI.EndDisabledGroup();
            }

            if (showDuplicateButton)
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.3f, 0.55f, 0.9f, 1f);
                if (GUI.Button(dupRect, "⧉", EditorStyles.miniButton)) clickedDuplicate = true;
                GUI.backgroundColor = prev;
            }

            if (showDeleteButton)
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                if (GUI.Button(delRect, "✕", EditorStyles.miniButton)) clickedDelete = true;
                GUI.backgroundColor = prev;
            }

            return fold;
        }

        internal static void DrawSectionHeader(string title, Color accent)
        {
            Rect rect = GUILayoutUtility.GetRect(0, 26, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(accent.r, accent.g, accent.b, 0.18f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2, rect.width, 2),
                               new Color(accent.r, accent.g, accent.b, 0.7f));
            var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            EditorGUI.LabelField(new Rect(rect.x + 8, rect.y, rect.width, rect.height), title, style);
        }

        private void EnsureFoldLists(string key, int size)
        {
            if (!_cardFold.ContainsKey(key))  _cardFold[key]  = new List<bool>();
            if (!_phaseFold.ContainsKey(key)) _phaseFold[key] = new List<List<bool>>();
            while (_cardFold[key].Count < size)  _cardFold[key].Add(false);
            while (_phaseFold[key].Count < size) _phaseFold[key].Add(new List<bool>());
        }

        private static void SwapList<T>(List<T> list, int a, int b)
        {
            if (a < 0 || b < 0 || a >= list.Count || b >= list.Count) return;
            (list[a], list[b]) = (list[b], list[a]);
        }

        private void MoveElement(string key,
            SerializedProperty lite, SerializedProperty heavy,
            SerializedProperty jump, SerializedProperty dash, SerializedProperty skill,
            SerializedProperty elem, int index, int dir)
        {
            SerializedProperty list = key switch
            {
                "lite"  => lite,
                "heavy" => heavy,
                "jump"  => jump,
                "dash"  => dash,
                "skill" => skill,
                _       => null,
            };
            if (list == null) return;
            int target = index + dir;
            if (target < 0 || target >= list.arraySize) return;
            list.MoveArrayElement(index, target);
            SwapList(_cardFold[key],  index, target);
            SwapList(_phaseFold[key], index, target);
        }
    }
}
