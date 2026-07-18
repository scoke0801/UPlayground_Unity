using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Combat;

namespace UPlayGround.Editor
{
    /// <summary>
    /// EnemyAttackDataSO 편집 UI.
    /// EnemyAttackDataSO 전용 편집 UI.
    /// </summary>
    public class EnemyAttackDataSODrawer
    {
        private static readonly Color AccentColor = new Color(1.00f, 0.45f, 0.20f);

        private readonly SerializedObject   _so;
        private readonly SerializedProperty _skillList;
        private readonly SerializedProperty _globalCooldown;

        // 폴드 상태: 카드 / 페이즈
        private readonly List<bool>         _cardFold  = new();
        private readonly List<List<bool>>   _phaseFold = new();

        // 에디터 스타일
        private static GUIStyle _cardStyle;

        private static void EnsureStyles()
        {
            if (_cardStyle != null) return;
            _cardStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 4, 6),
                margin  = new RectOffset(0, 0, 2, 2),
            };
        }

        public EnemyAttackDataSODrawer(SerializedObject so)
        {
            _so             = so;
            _skillList      = so.FindProperty("skills");
            _globalCooldown = so.FindProperty("globalCooldown");
        }

        public void DrawGUI()
        {
            EnsureStyles();

            // 헤더 바
            DrawSectionHeader("전역 설정", AccentColor);
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                EditorGUILayout.PropertyField(_globalCooldown, new GUIContent("전역 쿨다운 (초)"));

            EditorGUILayout.Space(8);

            // 스킬 목록 헤더
            DrawSectionHeader($"스킬 풀  ({_skillList.arraySize}개)", AccentColor);
            EditorGUILayout.Space(4);

            // 전체 펼치기/접기
            if (_skillList.arraySize > 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("▼ 전체 펼치기", EditorStyles.miniButton, GUILayout.Width(92)))
                    for (int i = 0; i < _cardFold.Count; i++) _cardFold[i] = true;
                if (GUILayout.Button("▶ 전체 접기", EditorStyles.miniButton, GUILayout.Width(78)))
                    for (int i = 0; i < _cardFold.Count; i++) _cardFold[i] = false;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);

            // 목록이 비었을 때
            if (_skillList.arraySize == 0)
                EditorGUILayout.HelpBox("스킬이 없습니다. 아래 버튼으로 추가하세요.", MessageType.Info);

            EnsureFoldLists(_skillList.arraySize);

            int removeAt = -1;
            for (int i = 0; i < _skillList.arraySize; i++)
            {
                if (DrawSkillCard(_skillList.GetArrayElementAtIndex(i), i, _skillList.arraySize))
                    removeAt = i;
            }

            if (removeAt >= 0)
            {
                _skillList.DeleteArrayElementAtIndex(removeAt);
                _cardFold.RemoveAt(removeAt);
                _phaseFold.RemoveAt(removeAt);
            }

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(AccentColor.r * 0.6f, AccentColor.g * 0.6f, AccentColor.b * 0.6f, 1f);
            if (GUILayout.Button("+ 스킬 추가", GUILayout.Height(28)))
            {
                _skillList.arraySize++;
                _cardFold.Add(true);
                _phaseFold.Add(new List<bool>());
                // 기본 hitPhases 1개 보장
                var np = _skillList.GetArrayElementAtIndex(_skillList.arraySize - 1);
                var hp = np.FindPropertyRelative("baseInfo.hitPhases");
                if (hp.arraySize == 0) hp.arraySize = 1;
            }
            GUI.backgroundColor = Color.white;
        }

        // ─── 스킬 카드 ────────────────────────────────────────────────

        private bool DrawSkillCard(SerializedProperty prop, int index, int total)
        {
            EnsureStyles();

            var baseInfoP    = prop.FindPropertyRelative("baseInfo");
            var animKeyP     = baseInfoP.FindPropertyRelative("animKey");
            var typeP        = baseInfoP.FindPropertyRelative("attackType");
            var phasesP      = baseInfoP.FindPropertyRelative("hitPhases");
            var weightP      = prop.FindPropertyRelative("selectionWeight");
            var minRangeP    = prop.FindPropertyRelative("minRange");
            var maxRangeP    = prop.FindPropertyRelative("maxRange");
            var cooldownP    = prop.FindPropertyRelative("cooldown");
            var skillTypeP   = prop.FindPropertyRelative("skillType");
            var requiredLevelP = prop.FindPropertyRelative("requiredLevel");
            var useTelegraphP = prop.FindPropertyRelative("useTelegraph");
            var telegraphShapeP = prop.FindPropertyRelative("telegraphShape");
            var telegraphRadiusScaleP = prop.FindPropertyRelative("telegraphRadiusScale");
            var telegraphFXKeyP = prop.FindPropertyRelative("telegraphFXKey");
            var useMotionEventTelegraphP = prop.FindPropertyRelative("useMotionEventTelegraph");
            var telegraphAnchorTypeP = prop.FindPropertyRelative("telegraphAnchorType");
            var useTelegraphPositionForHitP = prop.FindPropertyRelative("useTelegraphPositionForHit");
            var useDangerRingP   = prop.FindPropertyRelative("useDangerRing");
            var dangerRingDurationP = prop.FindPropertyRelative("dangerRingDuration");
            var dangerRingPrefabKeyP = prop.FindPropertyRelative("dangerRingPrefabKey");
            var defenseTypeP = prop.FindPropertyRelative("defenseType");
            var aerialP      = prop.FindPropertyRelative("isAerialSkill");
            var diveP        = prop.FindPropertyRelative("isDiveAttack");
            var diveSpeedP   = prop.FindPropertyRelative("diveDescentSpeed");
            var aerialWgtP   = prop.FindPropertyRelative("aerialSkillWeight");
            var conditionP   = prop.FindPropertyRelative("conditionGroup");

            float dmg0 = phasesP.arraySize > 0
                ? phasesP.GetArrayElementAtIndex(0).FindPropertyRelative("damage").floatValue
                : 0f;

            bool fold    = _cardFold[index];
            bool deleted = false;

            EditorGUILayout.BeginVertical(_cardStyle);

            string animLabel = animKeyP.enumDisplayNames[animKeyP.enumValueIndex];
            string summary   = $"  [{index + 1}]  {animLabel}   |   Lv {requiredLevelP.intValue}+   |   Phase {phasesP.arraySize}   |   DMG {dmg0:F0}   |   가중치 {weightP.floatValue:F0}   |   사거리 {minRangeP.floatValue:F0}~{maxRangeP.floatValue:F0}";
            Color  bgColor   = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.18f);

            // 헤더 행 (이동 버튼 없이, 삭제만)
            fold = DrawSkillHeaderRow(summary, bgColor, fold, index > 0, index < total - 1,
                out bool clickedUp, out bool clickedDown, out bool clickedDel);
            _cardFold[index] = fold;

            if (clickedUp)   { _skillList.MoveArrayElement(index, index - 1); SwapFolds(index, index - 1); }
            if (clickedDown) { _skillList.MoveArrayElement(index, index + 1); SwapFolds(index, index + 1); }
            if (clickedDel)  deleted = true;

            if (fold)
            {
                EditorGUILayout.Space(4);

                // 기본 정보
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("기본 정보", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(animKeyP,   new GUIContent("AnimKey"));
                    EditorGUILayout.PropertyField(typeP,      new GUIContent("공격 타입"));
                    EditorGUILayout.PropertyField(skillTypeP, new GUIContent("스킬 타입"));
                    EditorGUILayout.PropertyField(requiredLevelP, new GUIContent("해금 레벨"));
                }

                // 선택 조건
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("선택 조건", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(weightP,   new GUIContent("선택 가중치"));
                    EditorGUILayout.PropertyField(cooldownP, new GUIContent("쿨다운 (초)"));
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(minRangeP, new GUIContent("최소 거리"));
                    EditorGUILayout.PropertyField(maxRangeP, new GUIContent("최대 거리"));
                    EditorGUILayout.EndHorizontal();
                }

                // 텔레그래프
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("텔레그래프", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(useTelegraphP, new GUIContent("텔레그래프 사용"));

                    if (useTelegraphP.boolValue)
                    {
                        EditorGUILayout.PropertyField(telegraphShapeP,       new GUIContent("형태"));
                        EditorGUILayout.PropertyField(telegraphRadiusScaleP, new GUIContent("반경 배율"));
                        EditorGUILayout.PropertyField(telegraphFXKeyP,       new GUIContent("FX 키"));
                        EditorGUILayout.PropertyField(useMotionEventTelegraphP, new GUIContent("MotionEvent 타이밍 사용"));
                        EditorGUILayout.PropertyField(telegraphAnchorTypeP, new GUIContent("위치 기준"));
                        EditorGUILayout.PropertyField(useTelegraphPositionForHitP, new GUIContent("텔레그래프 위치를 판정에 사용"));
                        EditorGUILayout.HelpBox("FX 키가 비어 있으면 형태별 기본 키를 사용합니다. 현재 Circle 기본 키는 EnemyHeavyAttackTelegraph_Circle입니다.", MessageType.Info);
                    }
                    else if (HasStrongReaction(phasesP))
                    {
                        EditorGUILayout.HelpBox("강한 리액션(Heavy/KnockBack/Airborne/Knockdown/Grab)이 포함되어 있습니다. 강공격이면 텔레그래프 사용 여부를 확인하세요.", MessageType.Warning);
                    }
                }

                // 방어 타입 (패링 가능/불가)
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("방어 타입", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(defenseTypeP, new GUIContent("방어 대응"));
                    // Unblockable = 2 (Parryable=0, GuardableOnly=1, Unblockable=2)
                    if (defenseTypeP.enumValueIndex == 2)
                        EditorGUILayout.HelpBox("Unblockable: 퍼펙트 가드해도 카운터가 열리지 않습니다. Danger Ring은 붉은색으로 표시됩니다.", MessageType.Info);
                }

                // Danger Ring (UI) — 텔레그래프와 독립
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Danger Ring (UI)", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(useDangerRingP, new GUIContent("Danger Ring 사용"));
                    if (useDangerRingP.boolValue)
                    {
                        EditorGUILayout.PropertyField(dangerRingDurationP, new GUIContent("수축 시간(초, 0=자동)"));
                        EditorGUILayout.PropertyField(dangerRingPrefabKeyP, new GUIContent("프리팹 키(선택)"));
                        EditorGUILayout.HelpBox("0→1 채움이 가득 차는 순간이 실제 타격과 맞도록 '채우는 시간'을 윈드업→타격 간격에 맞추세요. 텔레그래프 사용 여부와 무관하게 단독 표시됩니다. 프리팹 키가 비면 기본 'DangerRing' 프리팹을 사용합니다.", MessageType.Info);
                    }
                }

                // 공중 설정
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("공중 설정", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(aerialP,    new GUIContent("공중 전용 스킬"));
                    if (aerialP.boolValue)
                    {
                        EditorGUILayout.PropertyField(aerialWgtP, new GUIContent("공중 가중치"));
                        EditorGUILayout.PropertyField(diveP,      new GUIContent("Dive Attack"));
                        if (diveP.boolValue)
                            EditorGUILayout.PropertyField(diveSpeedP, new GUIContent("Dive 하강 속도"));
                    }
                }

                // 발동 조건 (SkillConditionGroup)
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("발동 조건 (Condition Group)", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(conditionP, new GUIContent("조건"), true);
                }

                EditorGUILayout.Space(4);

                EditorGUILayout.PropertyField(phasesP, new GUIContent("Hit Phases"), true);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
            return deleted;
        }

        // ─── 스킬 카드 헤더 행 (이동 + 삭제, 복사 없음) ──────────────

        private static bool DrawSkillHeaderRow(
            string text, Color bgColor, bool fold,
            bool canUp, bool canDown,
            out bool clickedUp, out bool clickedDown, out bool clickedDelete)
        {
            clickedUp = clickedDown = clickedDelete = false;

            const float btnW = 22f, btnH = 22f, gap = 2f, margin = 4f;

            Rect row  = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(row, bgColor);

            float rx   = row.xMax - margin;
            float btnY = row.y + (row.height - btnH) * 0.5f;

            rx -= btnW;
            Rect delRect  = new Rect(rx, btnY, btnW, btnH);
            rx -= gap + btnW;
            Rect downRect = new Rect(rx, btnY, btnW, btnH);
            rx -= gap + btnW;
            Rect upRect   = new Rect(rx, btnY, btnW, btnH);
            rx -= gap;

            Rect foldRect = new Rect(row.x + margin, row.y, rx - row.x - margin, row.height);
            if (GUI.Button(foldRect, (fold ? "▼" : "▶") + text, EditorStyles.boldLabel))
                fold = !fold;

            EditorGUI.BeginDisabledGroup(!canUp);
            if (GUI.Button(upRect, "↑", EditorStyles.miniButton)) clickedUp = true;
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!canDown);
            if (GUI.Button(downRect, "↓", EditorStyles.miniButton)) clickedDown = true;
            EditorGUI.EndDisabledGroup();

            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
            if (GUI.Button(delRect, "✕", EditorStyles.miniButton)) clickedDelete = true;
            GUI.backgroundColor = prev;

            return fold;
        }

        // ─── 헬퍼 ─────────────────────────────────────────────────────

        private void EnsureFoldLists(int size)
        {
            while (_cardFold.Count < size)  _cardFold.Add(false);
            while (_phaseFold.Count < size) _phaseFold.Add(new List<bool>());
        }

        private void SwapFolds(int a, int b)
        {
            if (a < 0 || b < 0 || a >= _cardFold.Count || b >= _cardFold.Count) return;
            (_cardFold[a],  _cardFold[b])  = (_cardFold[b],  _cardFold[a]);
            (_phaseFold[a], _phaseFold[b]) = (_phaseFold[b], _phaseFold[a]);
        }

        private static bool HasStrongReaction(SerializedProperty phasesP)
        {
            if (phasesP == null) return false;

            for (int i = 0; i < phasesP.arraySize; i++)
            {
                var reactionP = phasesP.GetArrayElementAtIndex(i).FindPropertyRelative("reactionType");
                if (reactionP == null || reactionP.enumValueIndex < 0) continue;

                string reactionName = reactionP.enumNames[reactionP.enumValueIndex];
                if (reactionName == "Heavy" ||
                    reactionName == "KnockBack" ||
                    reactionName == "Airborne" ||
                    reactionName == "Knockdown" ||
                    reactionName == "Grab")
                {
                    return true;
                }
            }

            return false;
        }

        private static void DrawSectionHeader(string title, Color accent)
        {
            Rect rect = GUILayoutUtility.GetRect(0f, 25f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(accent.r, accent.g, accent.b, 0.18f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), accent);
            EditorGUI.LabelField(
                new Rect(rect.x + 7f, rect.y, rect.width - 14f, rect.height),
                title,
                EditorStyles.boldLabel);
        }
    }
}
