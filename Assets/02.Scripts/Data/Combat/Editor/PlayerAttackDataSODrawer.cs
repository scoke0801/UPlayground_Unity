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
        private static readonly string[] TabLabels = { "약공격", "강공격", "점프", "대쉬", "스킬", "카운터", "차지", "등장", "특수" };
        internal static readonly Color[] TabAccents =
        {
            new Color(0.35f, 0.55f, 1.00f),
            new Color(1.00f, 0.35f, 0.35f),
            new Color(0.30f, 0.90f, 0.55f),
            new Color(1.00f, 0.65f, 0.20f),
            new Color(0.75f, 0.35f, 1.00f),
            new Color(1.00f, 0.85f, 0.00f),
            new Color(1.00f, 0.50f, 0.15f),
            new Color(0.20f, 0.85f, 0.95f),
            new Color(1.00f, 0.25f, 0.65f),
        };
        private int _tab;

        // Phase 색상 (미니맵용)
        private static readonly Color[] PhaseColors =
        {
            new Color(0.29f, 0.61f, 1.00f),
            new Color(0.63f, 0.42f, 1.00f),
            new Color(1.00f, 0.49f, 0.26f),
            new Color(1.00f, 0.79f, 0.26f),
            new Color(0.37f, 0.79f, 0.48f),
        };

        // ─── SerializedObject / Property ────────────────────────────────
        private readonly SerializedObject _so;
        private SerializedProperty _liteList, _heavyList, _jumpList, _dashList, _skillList;
        private SerializedProperty _counter, _parryCounter, _entry, _swapSpecial;
        private SerializedProperty _chargeAnimKey, _chargeStages, _chargeThresholds;
        private SerializedProperty _vfxKey, _vfxSocket, _vfxOffset;

        // ─── 폴드아웃 / 검색 상태 ───────────────────────────────────────
        private readonly Dictionary<string, List<bool>>       _cardFold    = new();
        private readonly Dictionary<string, List<List<bool>>> _phaseFold   = new();
        private readonly Dictionary<string, string>           _searchFilter = new();

        // ─── 스타일 (지연 초기화) ────────────────────────────────────────
        private static GUIStyle _cardStyle;
        private static GUIStyle _phaseStyle;
        private static GUIStyle _tabStyleActive;
        private static GUIStyle _tabStyleNormal;
        private static GUIStyle _minimapLabelStyle;  // 미니맵 텍스트용 (루프 밖 캐시)

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
            _tabStyleActive    = new GUIStyle(EditorStyles.miniButtonMid) { fontStyle = FontStyle.Bold, fontSize = 11, fixedHeight = 26 };
            _tabStyleNormal    = new GUIStyle(EditorStyles.miniButtonMid) { fontSize = 11, fixedHeight = 26 };
            _minimapLabelStyle = new GUIStyle(EditorStyles.whiteMiniLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 9 };
        }

        // ═══════════════════════════════════════════════════════════════
        //  Phase 클립보드 (static → 탭·에셋 교체 후에도 유지)
        // ═══════════════════════════════════════════════════════════════
        private static class PhaseClipboard
        {
            public static bool   HasData    { get; private set; }
            public static string DisplayStr { get; private set; } = "";

            private static float   _damage, _poiseDamage;
            private static float   _pullForce, _airborneForce, _knockBackForce, _knockBackDrag;
            private static float   _attackRadius, _hitHeightRange, _grabDuration;
            private static Vector3 _attackOffset;
            private static int     _reactionType, _victimForcedAnimKey;
            private static string  _hitParticleName;

            public static void Copy(SerializedProperty phase)
            {
                _damage              = phase.FindPropertyRelative("damage").floatValue;
                _poiseDamage         = phase.FindPropertyRelative("poiseDamage").floatValue;
                _reactionType        = phase.FindPropertyRelative("reactionType").enumValueIndex;
                _attackOffset        = phase.FindPropertyRelative("attackOffset").vector3Value;
                _attackRadius        = phase.FindPropertyRelative("attackRadius").floatValue;
                _hitHeightRange      = phase.FindPropertyRelative("hitHeightRange").floatValue;
                _hitParticleName     = phase.FindPropertyRelative("hitParticleName").stringValue;
                _pullForce           = phase.FindPropertyRelative("pullForce").floatValue;
                _airborneForce       = phase.FindPropertyRelative("airborneForce").floatValue;
                _knockBackForce      = phase.FindPropertyRelative("knockBackForce").floatValue;
                _knockBackDrag       = phase.FindPropertyRelative("knockBackDrag").floatValue;
                _grabDuration        = phase.FindPropertyRelative("grabDuration").floatValue;
                _victimForcedAnimKey = phase.FindPropertyRelative("victimForcedAnimKey").enumValueIndex;

                HasData    = true;
                DisplayStr = $"DMG {_damage:F0}  포이즈 {_poiseDamage:F0}  반경 {_attackRadius:F1}";
            }

            public static void Paste(SerializedProperty phase)
            {
                if (!HasData) return;
                phase.FindPropertyRelative("damage").floatValue              = _damage;
                phase.FindPropertyRelative("poiseDamage").floatValue         = _poiseDamage;
                phase.FindPropertyRelative("reactionType").enumValueIndex    = _reactionType;
                phase.FindPropertyRelative("attackOffset").vector3Value      = _attackOffset;
                phase.FindPropertyRelative("attackRadius").floatValue        = _attackRadius;
                phase.FindPropertyRelative("hitHeightRange").floatValue      = _hitHeightRange;
                phase.FindPropertyRelative("hitParticleName").stringValue    = _hitParticleName;
                phase.FindPropertyRelative("pullForce").floatValue           = _pullForce;
                phase.FindPropertyRelative("airborneForce").floatValue       = _airborneForce;
                phase.FindPropertyRelative("knockBackForce").floatValue      = _knockBackForce;
                phase.FindPropertyRelative("knockBackDrag").floatValue       = _knockBackDrag;
                phase.FindPropertyRelative("grabDuration").floatValue        = _grabDuration;
                phase.FindPropertyRelative("victimForcedAnimKey").enumValueIndex = _victimForcedAnimKey;
            }

            public static void Clear() => HasData = false;
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
            _entry            = so.FindProperty("entryAttack");
            _swapSpecial      = so.FindProperty("swapSpecialAttack");
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

            if (PhaseClipboard.HasData)
                DrawClipboardBanner();

            EditorGUILayout.Space(4);

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
                case 7: DrawEntryAttack(accent); break;
                case 8: DrawSwapSpecialAttack(accent); break;
            }
        }

        // ─── 클립보드 배너 ────────────────────────────────────────────
        private static void DrawClipboardBanner()
        {
            Rect rect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.08f, 0.22f, 0.10f, 1f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3, rect.height), new Color(0.37f, 0.79f, 0.48f));
            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
                { normal = { textColor = new Color(0.6f, 0.95f, 0.65f) } };
            EditorGUI.LabelField(
                new Rect(rect.x + 8, rect.y, rect.width - 68, rect.height),
                $"⎘ 클립보드 — {PhaseClipboard.DisplayStr}", labelStyle);
            if (GUI.Button(new Rect(rect.xMax - 62, rect.y + 2, 58, 18), "지우기", EditorStyles.miniButton))
                PhaseClipboard.Clear();
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
            5 => 1,
            6 => _chargeStages.arraySize,
            7 => 1,
            8 => 1,
            _ => 0,
        };

        private void DrawTabBar()
        {
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < TabLabels.Length; i++)
            {
                bool active = i == _tab;
                int  count  = GetTabCount(i);
                bool empty  = count == 0 && i != 5 && i != 7 && i != 8;

                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = active
                    ? new Color(TabAccents[i].r, TabAccents[i].g, TabAccents[i].b, 0.9f)
                    : empty
                        ? new Color(0.18f, 0.18f, 0.18f, 0.8f)
                        : new Color(0.25f, 0.25f, 0.25f, 0.8f);

                var style = active ? _tabStyleActive : _tabStyleNormal;
                string label = (i == 5 || i == 6 || i == 7 || i == 8) ? TabLabels[i] : $"{TabLabels[i]} ({count})";

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
        //  ② 검증 헬퍼
        // ═══════════════════════════════════════════════════════════════
        private struct ValidationResult
        {
            public List<string> Errors;
            public List<string> Warnings;
            public bool HasIssues => (Errors?.Count > 0) || (Warnings?.Count > 0);
        }

        // attackProp = PlayerAttackInfo 또는 ChargeStageData의 SerializedProperty
        private static ValidationResult ValidateAttack(SerializedProperty attackProp)
        {
            var result = new ValidationResult
            {
                Errors   = new List<string>(),
                Warnings = new List<string>(),
            };

            // PlayerAttackInfo → baseInfo.hitPhases / ChargeStageData → hitPhases 폴백
            SerializedProperty phasesP = attackProp.FindPropertyRelative("baseInfo.hitPhases")
                                      ?? attackProp.FindPropertyRelative("hitPhases");
            if (phasesP == null) return result;

            if (phasesP.arraySize == 0)
            {
                result.Errors.Add("HitPhase가 없습니다.");
                return result;
            }

            for (int i = 0; i < phasesP.arraySize; i++)
            {
                SerializedProperty ph     = phasesP.GetArrayElementAtIndex(i);
                float  damage             = ph.FindPropertyRelative("damage").floatValue;
                float  radius             = ph.FindPropertyRelative("attackRadius").floatValue;
                float  af                 = ph.FindPropertyRelative("airborneForce").floatValue;
                float  kf                 = ph.FindPropertyRelative("knockBackForce").floatValue;
                SerializedProperty rxProp = ph.FindPropertyRelative("reactionType");
                string rxName             = rxProp.enumDisplayNames[rxProp.enumValueIndex];

                if (damage == 0f)  result.Warnings.Add($"Phase {i}: 대미지 0");
                if (radius <= 0f)  result.Errors.Add($"Phase {i}: 히트박스 반경 0 이하");
                if ((rxName == "Airborne" || rxName == "KnockBack") && af == 0f && kf == 0f)
                    result.Warnings.Add($"Phase {i}: {rxName} 타입이지만 반응 힘이 모두 0");
            }
            return result;
        }

        private static void DrawValidationPanel(ValidationResult v)
        {
            EditorGUILayout.Space(2);
            foreach (string err in v.Errors)
            {
                Rect r = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(r, new Color(0.5f, 0f, 0f, 0.25f));
                EditorGUI.DrawRect(new Rect(r.x, r.y, 3, r.height), Color.red);
                EditorGUI.LabelField(new Rect(r.x + 7, r.y, r.width, r.height), "✕  " + err,
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1f, 0.45f, 0.45f) } });
            }
            foreach (string warn in v.Warnings)
            {
                Rect r = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(r, new Color(0.5f, 0.4f, 0f, 0.25f));
                EditorGUI.DrawRect(new Rect(r.x, r.y, 3, r.height), Color.yellow);
                EditorGUI.LabelField(new Rect(r.x + 7, r.y, r.width, r.height), "!  " + warn,
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1f, 0.85f, 0.3f) } });
            }
            EditorGUILayout.Space(2);
        }

        // ═══════════════════════════════════════════════════════════════
        //  ③ Phase 미니맵 (duration 없으므로 등폭 블록, 대미지 크기로 채우기)
        // ═══════════════════════════════════════════════════════════════
        private static void DrawPhasesMinimap(SerializedProperty phases, Color accent)
        {
            EnsureStyles();
            int count = phases.arraySize;
            if (count == 0) return;

            // 최대 대미지 파악
            float maxDmg = 0f;
            for (int i = 0; i < count; i++)
            {
                float d = phases.GetArrayElementAtIndex(i).FindPropertyRelative("damage").floatValue;
                if (d > maxDmg) maxDmg = d;
            }
            if (maxDmg <= 0f) maxDmg = 1f;

            Rect bar = GUILayoutUtility.GetRect(0, 32, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(bar, new Color(0.07f, 0.07f, 0.07f, 0.85f));

            float blockW = bar.width / count;
            for (int i = 0; i < count; i++)
            {
                SerializedProperty ph  = phases.GetArrayElementAtIndex(i);
                float dmg              = ph.FindPropertyRelative("damage").floatValue;
                Color col              = PhaseColors[i % PhaseColors.Length];

                Rect blockRect = new Rect(bar.x + i * blockW + 1, bar.y + 1, blockW - 2, bar.height - 2);

                // 배경 (반투명)
                EditorGUI.DrawRect(blockRect, col * new Color(1f, 1f, 1f, 0.35f));

                // 대미지 채우기 바 (하단부터)
                float fillH = (dmg / maxDmg) * (bar.height - 6f);
                if (fillH > 0f)
                    EditorGUI.DrawRect(new Rect(blockRect.x, blockRect.yMax - fillH,
                                                blockRect.width, fillH), col * new Color(1f, 1f, 1f, 0.8f));

                // 텍스트 라벨
                EditorGUI.LabelField(blockRect, $"P{i}  {dmg:F0}", _minimapLabelStyle);

                // 구분선
                if (i < count - 1)
                    EditorGUI.DrawRect(new Rect(blockRect.xMax, bar.y, 1, bar.height), new Color(0f, 0f, 0f, 0.5f));
            }
            EditorGUILayout.Space(2);
        }

        // ═══════════════════════════════════════════════════════════════
        //  공격 리스트
        // ═══════════════════════════════════════════════════════════════
        private void DrawAttackList(SerializedProperty list, string title, string key, Color accent)
        {
            EnsureFoldLists(key, list.arraySize);
            if (!_searchFilter.ContainsKey(key)) _searchFilter[key] = "";

            DrawSectionHeader(title, accent);

            // 검색 필드 + 전체 펼치기/접기
            EditorGUILayout.BeginHorizontal();
            _searchFilter[key] = EditorGUILayout.TextField(_searchFilter[key],
                EditorStyles.toolbarSearchField, GUILayout.MinWidth(80));
            if (list.arraySize > 0)
            {
                if (GUILayout.Button("▼ 전체 펼치기", EditorStyles.miniButton, GUILayout.Width(90)))
                    for (int i = 0; i < _cardFold[key].Count; i++) _cardFold[key][i] = true;
                if (GUILayout.Button("▶ 전체 접기", EditorStyles.miniButton, GUILayout.Width(76)))
                    for (int i = 0; i < _cardFold[key].Count; i++) _cardFold[key][i] = false;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);

            if (list.arraySize == 0)
                EditorGUILayout.HelpBox("공격 데이터가 없습니다. 아래 버튼으로 추가하세요.", MessageType.Info);

            string filter    = _searchFilter[key].ToLower();
            int    removeAt  = -1;
            int    dupAt     = -1;

            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty prop = list.GetArrayElementAtIndex(i);

                // 검색 필터 — 폴드 인덱스 접근은 실제 배열 인덱스 기준이라 안전
                if (!string.IsNullOrEmpty(filter))
                {
                    SerializedProperty animKeyP = prop.FindPropertyRelative("baseInfo.animKey");
                    string animLabel = animKeyP.enumDisplayNames[animKeyP.enumValueIndex].ToLower();
                    if (!animLabel.Contains(filter)) continue;
                }

                var (del, dup) = DrawAttackCard(prop, i, list.arraySize, key, accent);
                if (del) removeAt = i;
                if (dup) dupAt    = i;
            }

            if (removeAt >= 0)
            {
                list.DeleteArrayElementAtIndex(removeAt);
                _cardFold[key].RemoveAt(removeAt);
                _phaseFold[key].RemoveAt(removeAt);
            }
            else if (dupAt >= 0)
            {
                list.InsertArrayElementAtIndex(dupAt);
                list.MoveArrayElement(dupAt, dupAt + 1);
                _cardFold[key].Insert(dupAt + 1, true);
                _phaseFold[key].Insert(dupAt + 1, new List<bool>());
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

            // 총 대미지 (모든 Phase 합산)
            float totalDmg = 0f;
            for (int p = 0; p < phasesP.arraySize; p++)
                totalDmg += phasesP.GetArrayElementAtIndex(p).FindPropertyRelative("damage").floatValue;

            // 검증
            ValidationResult validation = ValidateAttack(prop);

            bool fold      = _cardFold[key][index];
            bool deleted   = false;
            bool duplicated = false;

            EditorGUILayout.BeginVertical(_cardStyle);

            string animLabel = animKeyP.enumDisplayNames[animKeyP.enumValueIndex];
            string summary   = $"  [{index + 1}]  {animLabel}   |   Phase {phasesP.arraySize}   |   총DMG {totalDmg:F0}   |   각도 {angleP.floatValue:F0}°   |   {(interruptP.boolValue ? "캔슬 O" : "캔슬 X")}";

            // 오류=붉은, 경고=노랑, 정상=accent 배경
            Color bgColor = validation.Errors.Count > 0
                ? new Color(0.80f, 0.15f, 0.15f, 0.25f)
                : validation.Warnings.Count > 0
                    ? new Color(0.90f, 0.75f, 0.00f, 0.20f)
                    : new Color(accent.r, accent.g, accent.b, 0.18f);

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
                // Phase 미니맵
                if (phasesP.arraySize > 0)
                    DrawPhasesMinimap(phasesP, accent);

                // 검증 패널
                if (validation.HasIssues)
                    DrawValidationPanel(validation);

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
            DrawHitPhaseListShared(phases, _phaseFold[key][cardIdx], accent);
        }

        // EnemyAttackDataSODrawer에서도 호출
        internal static void DrawHitPhaseListShared(SerializedProperty phases, List<bool> folds, Color accent)
        {
            EnsureStyles();
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
        internal static bool DrawHitPhaseCard(SerializedProperty phase, int index, List<bool> folds, Color accent)
        {
            EnsureStyles();

            SerializedProperty damageP   = phase.FindPropertyRelative("damage");
            SerializedProperty poiseP    = phase.FindPropertyRelative("poiseDamage");
            SerializedProperty reactionP = phase.FindPropertyRelative("reactionType");

            bool fold    = folds[index];
            bool deleted = false;

            EditorGUILayout.BeginVertical(_phaseStyle);

            string reactionLabel = reactionP.enumDisplayNames[reactionP.enumValueIndex];
            string summary = $"  Phase {index}  |  데미지 {damageP.floatValue:F0}  |  포이즈 {poiseP.floatValue:F0}  |  {reactionLabel}";
            Color  phBg    = new Color(accent.r * 0.5f, accent.g * 0.5f, accent.b * 0.5f, 0.20f);

            // 헤더 (복사/붙여넣기 버튼 포함)
            bool newFold = DrawPhaseCardHeaderRow(summary, phBg, fold, index > 0, phase, out bool clickedDel);
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

                // 대미지 시각화 바
                DrawDamageBar(damageP.floatValue, accent);

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
                        EditorGUILayout.LabelField("그랩 / Forced Motion", EditorStyles.miniBoldLabel);
                        EditorGUILayout.PropertyField(phase.FindPropertyRelative("grabDuration"),        new GUIContent("지속 시간 (초)"));
                        EditorGUILayout.PropertyField(phase.FindPropertyRelative("victimForcedAnimKey"), new GUIContent("피격자 강제 애니 (None = Grabbed 폴백)"));
                    }
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(1);
            return deleted;
        }

        // ─── 대미지 시각화 바 ─────────────────────────────────────────
        private static void DrawDamageBar(float damage, Color accent)
        {
            const float kMax = 200f;
            float t = Mathf.Clamp01(damage / kMax);
            Rect outer = GUILayoutUtility.GetRect(0, 5, GUILayout.ExpandWidth(true));
            outer = new Rect(outer.x + 2, outer.y, outer.width - 4, outer.height);
            EditorGUI.DrawRect(outer, new Color(0.12f, 0.12f, 0.12f, 0.8f));
            if (t > 0f)
                EditorGUI.DrawRect(new Rect(outer.x, outer.y, outer.width * t, outer.height),
                                   accent * new Color(1f, 1f, 1f, 0.85f));
            EditorGUILayout.Space(2);
        }

        // ─── Phase 카드 헤더 (복사/붙여넣기 버튼 포함) ───────────────
        private static bool DrawPhaseCardHeaderRow(
            string text, Color bgColor, bool fold, bool canDelete,
            SerializedProperty phase, out bool clickedDel)
        {
            clickedDel = false;

            const float btnW = 22f, btnH = 22f, gap = 2f, margin = 4f;
            Rect row  = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(row, bgColor);

            float rx   = row.xMax - margin;
            float btnY = row.y + (row.height - btnH) * 0.5f;

            // 삭제 버튼
            Rect delRect = Rect.zero;
            if (canDelete)
            {
                rx -= btnW;
                delRect = new Rect(rx, btnY, btnW, btnH);
                rx -= gap;
            }

            // 붙여넣기 버튼
            rx -= btnW;
            Rect pasteRect = new Rect(rx, btnY, btnW, btnH);
            rx -= gap;

            // 복사 버튼
            rx -= btnW;
            Rect copyRect = new Rect(rx, btnY, btnW, btnH);
            rx -= gap;

            // 폴드 버튼
            Rect foldRect = new Rect(row.x + margin, row.y, rx - row.x - margin, row.height);
            if (GUI.Button(foldRect, (fold ? "▼" : "▶") + text, EditorStyles.boldLabel))
                fold = !fold;

            // 복사
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.55f, 0.9f, 1f);
            if (GUI.Button(copyRect, "⎘", EditorStyles.miniButton))
                PhaseClipboard.Copy(phase);
            GUI.backgroundColor = prev;

            // 붙여넣기
            EditorGUI.BeginDisabledGroup(!PhaseClipboard.HasData);
            GUI.backgroundColor = PhaseClipboard.HasData
                ? new Color(0.3f, 0.7f, 0.3f, 1f)
                : new Color(0.3f, 0.3f, 0.3f, 1f);
            if (GUI.Button(pasteRect, "⬇", EditorStyles.miniButton) && PhaseClipboard.HasData)
                PhaseClipboard.Paste(phase);
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            // 삭제
            if (canDelete)
            {
                GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                if (GUI.Button(delRect, "✕", EditorStyles.miniButton)) clickedDel = true;
                GUI.backgroundColor = Color.white;
            }

            return fold;
        }

        // ═══════════════════════════════════════════════════════════════
        //  카운터 공격 탭
        // ═══════════════════════════════════════════════════════════════
        private void DrawCounterAttack(Color accent)
        {
            EnsureFoldLists("counter",      1);
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

        // ═══════════════════════════════════════════════════════════════
        //  등장 공격 탭 (교체 직후 범위 내 적 존재 시 자동 발동)
        // ═══════════════════════════════════════════════════════════════
        private void DrawEntryAttack(Color accent)
        {
            EnsureFoldLists("entry", 1);

            DrawSectionHeader("교체 등장 공격", accent);
            EditorGUILayout.HelpBox(
                "교체 직후 incoming 캐릭터의 검출 반경 내에 적이 있을 때 자동으로 발동됩니다.\n" +
                "비워두면 약 공격 첫 번째 데이터로 대체됩니다.\n" +
                "검출 반경/LOS는 CharacterModelData·PartyConfigSO 인스펙터에서 설정합니다.",
                MessageType.Info);
            EditorGUILayout.Space(4);
            DrawCounterAttackField(_entry, "entry", accent);
        }

        private void DrawSwapSpecialAttack(Color accent)
        {
            EnsureFoldLists("swapSpecial", 1);

            DrawSectionHeader("풀 게이지 교체 특수 공격", accent);
            EditorGUILayout.HelpBox(
                "스킬 게이지가 가득 찬 캐릭터로 교체할 때 자동으로 발동됩니다.\n" +
                "비워두면 스킬 공격 0번, 등장 공격 순으로 대체됩니다.",
                MessageType.Info);
            EditorGUILayout.Space(4);
            DrawCounterAttackField(_swapSpecial, "swapSpecial", accent);
        }

        private void DrawCounterAttackField(SerializedProperty prop, string key, Color accent)
        {
            SerializedProperty baseInfo   = prop.FindPropertyRelative("baseInfo");
            SerializedProperty animKeyP   = baseInfo.FindPropertyRelative("animKey");
            SerializedProperty typeP      = baseInfo.FindPropertyRelative("attackType");
            SerializedProperty phasesP    = baseInfo.FindPropertyRelative("hitPhases");
            SerializedProperty interruptP = prop.FindPropertyRelative("canBeInterrupted");
            SerializedProperty angleP     = prop.FindPropertyRelative("hitAngle");

            ValidationResult validation = ValidateAttack(prop);

            EditorGUILayout.BeginVertical(_cardStyle);

            if (phasesP.arraySize > 0)
                DrawPhasesMinimap(phasesP, accent);

            if (validation.HasIssues)
                DrawValidationPanel(validation);

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

            int removeAt = -1;
            int dupAt    = -1;

            for (int i = 0; i < _chargeStages.arraySize; i++)
            {
                var (del, dup) = DrawChargeStageCard(_chargeStages.GetArrayElementAtIndex(i), i, _chargeStages.arraySize, accent);
                if (del) removeAt = i;
                if (dup) dupAt    = i;
            }

            if (removeAt >= 0)
            {
                _chargeStages.DeleteArrayElementAtIndex(removeAt);
                _cardFold["charge"].RemoveAt(removeAt);
                _phaseFold["charge"].RemoveAt(removeAt);
            }
            else if (dupAt >= 0)
            {
                _chargeStages.InsertArrayElementAtIndex(dupAt);
                _chargeStages.MoveArrayElement(dupAt, dupAt + 1);
                _cardFold["charge"].Insert(dupAt + 1, true);
                _phaseFold["charge"].Insert(dupAt + 1, new List<bool>());
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
        private (bool deleted, bool duplicated) DrawChargeStageCard(
            SerializedProperty stage, int index, int total, Color accent)
        {
            SerializedProperty phasesP    = stage.FindPropertyRelative("hitPhases");
            SerializedProperty interruptP = stage.FindPropertyRelative("canBeInterrupted");
            SerializedProperty angleP     = stage.FindPropertyRelative("hitAngle");

            ValidationResult validation = ValidateAttack(stage);

            bool fold      = _cardFold["charge"][index];
            bool deleted   = false;
            bool duplicated = false;

            EditorGUILayout.BeginVertical(_cardStyle);

            string summary = $"  [{index + 1}단계]  Phase {phasesP.arraySize}   |   각도 {angleP.floatValue:F0}°   |   {(interruptP.boolValue ? "캔슬 O" : "캔슬 X")}";
            Color  bgColor = validation.Errors.Count > 0
                ? new Color(0.80f, 0.15f, 0.15f, 0.25f)
                : validation.Warnings.Count > 0
                    ? new Color(0.90f, 0.75f, 0.00f, 0.20f)
                    : new Color(accent.r, accent.g, accent.b, 0.18f);

            bool newFold = DrawCardHeaderRow(summary, bgColor, fold, index > 0, index < total - 1,
                                             out bool cu, out bool cd, out bool cDel, out bool cDup);
            _cardFold["charge"][index] = newFold;

            if (cu)   SwapChargeStages(index, index - 1);
            if (cd)   SwapChargeStages(index, index + 1);
            if (cDel) deleted    = true;
            if (cDup) duplicated = true;

            if (newFold)
            {
                if (phasesP.arraySize > 0) DrawPhasesMinimap(phasesP, accent);
                if (validation.HasIssues)  DrawValidationPanel(validation);

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
            SwapList(_cardFold["charge"],  a, b);
            SwapList(_phaseFold["charge"], a, b);
        }

        // ═══════════════════════════════════════════════════════════════
        //  공통 헬퍼
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 카드 헤더 행. 버튼 순서 (우→좌): 삭제[✕] — 복사[⧉] — 아래[↓] — 위[↑]
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
