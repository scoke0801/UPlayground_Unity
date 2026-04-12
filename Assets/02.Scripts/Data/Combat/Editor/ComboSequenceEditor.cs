using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Gameplay.Tag.Editor;

namespace UPlayGround.Data.Editor
{
    /// <summary>
    /// PlayerAttackDataSO의 comboSequences 필드를 시각적으로 편집하는 커스텀 에디터.
    /// 태그 조건은 GameplayTagId enum 드롭다운으로 편집한다.
    /// 메뉴: UPlayGround/Combat/Combo Sequence Editor
    /// </summary>
    public class ComboSequenceEditor : EditorWindow
    {
        // ── 색상 ─────────────────────────────────────────────────────
        private static readonly Color ColorLight    = new(0.25f, 0.55f, 1.00f);
        private static readonly Color ColorHeavy    = new(1.00f, 0.45f, 0.20f);
        private static readonly Color ColorHeader   = new(0.13f, 0.13f, 0.18f);
        private static readonly Color ColorRowEven  = new(0.20f, 0.20f, 0.22f);
        private static readonly Color ColorRowOdd   = new(0.24f, 0.24f, 0.26f);
        private static readonly Color ColorSelected = new(0.18f, 0.36f, 0.58f);
        private static readonly Color ColorRequired = new(0.25f, 0.70f, 0.30f);
        private static readonly Color ColorBlocked  = new(0.80f, 0.25f, 0.25f);

        private const float StepBtnW = 36f;
        private const float StepBtnH = 28f;
        private const float RowH     = 28f;

        // ── 상태 ─────────────────────────────────────────────────────
        private PlayerAttackDataSO _target;
        private SerializedObject   _serializedObj;
        private SerializedProperty _sequencesProp;

        private int     _selectedIndex = -1;
        private Vector2 _listScroll;
        private Vector2 _detailScroll;

        private ReorderableList _rl;

        // ── 메뉴 ─────────────────────────────────────────────────────
        [MenuItem("UPlayGround/Combat/Combo Sequence Editor")]
        public static void Open()
        {
            var w = GetWindow<ComboSequenceEditor>();
            w.titleContent = new GUIContent("Combo Sequence Editor",
                EditorGUIUtility.IconContent("d_UnityEditor.AnimationWindow").image);
            w.minSize = new Vector2(900f, 480f);
            w.Show();
        }

        // ── 라이프사이클 ──────────────────────────────────────────────
        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            _serializedObj?.ApplyModifiedProperties();
        }

        private void OnSelectionChanged()
        {
            var so = Selection.activeObject as PlayerAttackDataSO;
            if (so != null && so != _target)
                SetTarget(so);
        }

        private void SetTarget(PlayerAttackDataSO so)
        {
            _target        = so;
            _serializedObj = new SerializedObject(so);
            _sequencesProp = _serializedObj.FindProperty("comboSequences");
            _selectedIndex = -1;
            BuildReorderableList();
            Repaint();
        }

        private void BuildReorderableList()
        {
            _rl = new ReorderableList(_serializedObj, _sequencesProp,
                draggable: true, displayHeader: true,
                displayAddButton: true, displayRemoveButton: true);

            _rl.drawHeaderCallback = rect =>
                GUI.Label(rect, "콤보 시퀀스 목록", EditorStyles.boldLabel);

            _rl.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (_sequencesProp.arraySize <= index) return;
                DrawListRow(rect, _sequencesProp.GetArrayElementAtIndex(index), index, isActive);
            };

            _rl.elementHeight    = RowH + 4f;
            _rl.onSelectCallback = list => _selectedIndex = list.index;
            _rl.onAddCallback    = list =>
            {
                _sequencesProp.arraySize++;
                int ni      = _sequencesProp.arraySize - 1;
                var newElem = _sequencesProp.GetArrayElementAtIndex(ni);
                newElem.FindPropertyRelative("sequenceName").stringValue = $"New Combo {ni + 1}";
                newElem.FindPropertyRelative("priority").intValue        = 0;
                _selectedIndex = ni;
                _serializedObj.ApplyModifiedProperties();
            };
        }

        // ── OnGUI ─────────────────────────────────────────────────────
        private void OnGUI()
        {
            _serializedObj?.Update();

            DrawToolbar();

            if (_target == null)
            {
                DrawNoTargetMessage();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();

            _serializedObj.ApplyModifiedProperties();
        }

        // ── 툴바 ─────────────────────────────────────────────────────
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("대상 SO:", EditorStyles.toolbarButton, GUILayout.Width(55));

            var newTarget = (PlayerAttackDataSO)EditorGUILayout.ObjectField(
                _target, typeof(PlayerAttackDataSO), false, GUILayout.Width(220));
            if (newTarget != _target && newTarget != null)
                SetTarget(newTarget);

            GUILayout.FlexibleSpace();

            if (_target != null)
                GUILayout.Label($"총 {_sequencesProp.arraySize}개",
                    EditorStyles.toolbarButton, GUILayout.Width(50));

            // Tag Registry Editor 바로가기
            if (GUILayout.Button("🏷 Tag Registry Editor", EditorStyles.toolbarButton, GUILayout.Width(140)))
                GameplayTagRegistryEditorWindow.Open();

            if (GUILayout.Button("저장", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                _serializedObj?.ApplyModifiedProperties();
                EditorUtility.SetDirty(_target);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.EndHorizontal();
        }

        // ── 왼쪽 패널 (목록) ─────────────────────────────────────────
        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(280), GUILayout.ExpandHeight(true));
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
            _rl?.DoLayoutList();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawListRow(Rect rect, SerializedProperty elem, int index, bool isActive)
        {
            Color bg = isActive ? ColorSelected : (index % 2 == 0 ? ColorRowEven : ColorRowOdd);
            EditorGUI.DrawRect(rect, bg);

            float x = rect.x + 4;
            float y = rect.y + (rect.height - 18f) * 0.5f;

            // 입력 패턴 미니 미리보기
            DrawMiniSequence(ref x, y, elem.FindPropertyRelative("inputSequence"));

            // 이름
            GUI.Label(new Rect(x, y, 110f, 18f),
                elem.FindPropertyRelative("sequenceName").stringValue, EditorStyles.miniLabel);
            x += 112f;

            // 우선순위
            GUI.Label(new Rect(x, y, 40f, 18f),
                $"P:{elem.FindPropertyRelative("priority").intValue}", EditorStyles.miniLabel);
            x += 42f;

            // 스킬 게이지 슬롯
            int gaugeIdx = elem.FindPropertyRelative("skillGaugeIndex").intValue;
            if (gaugeIdx >= 0)
            {
                var prevColor = GUI.color;
                GUI.color = new Color(1f, 0.85f, 0.2f);
                GUI.Label(new Rect(x, y, 50f, 18f), $"G:{gaugeIdx + 1}", EditorStyles.miniLabel);
                GUI.color = prevColor;
            }
        }

        private void DrawMiniSequence(ref float x, float y, SerializedProperty seqProp)
        {
            int count = Mathf.Min(seqProp.arraySize, 5);
            for (int i = 0; i < count; i++)
            {
                var step      = seqProp.GetArrayElementAtIndex(i);
                var inputType = (ComboInputType)step.FindPropertyRelative("inputType").enumValueIndex;
                Color  c     = inputType == ComboInputType.LightAttack ? ColorLight : ColorHeavy;
                string label = inputType == ComboInputType.LightAttack ? "L" : "H";

                var old = GUI.backgroundColor;
                GUI.backgroundColor = c;
                GUI.Button(new Rect(x, y, 20f, 18f), label, EditorStyles.miniButton);
                GUI.backgroundColor = old;
                x += 20f;
            }
            if (seqProp.arraySize > 5)
            {
                GUI.Label(new Rect(x, y, 16f, 18f), "…", EditorStyles.miniLabel);
                x += 16f;
            }
            x += 4f;
        }

        // ── 오른쪽 패널 (디테일) ─────────────────────────────────────
        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (_selectedIndex < 0 || _selectedIndex >= _sequencesProp.arraySize)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("← 왼쪽에서 시퀀스를 선택하세요.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            var entry = _sequencesProp.GetArrayElementAtIndex(_selectedIndex);
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

            DrawDetailHeader(entry);
            GUILayout.Space(6);
            DrawInputChain(entry);
            GUILayout.Space(6);
            DrawTagConditions(entry);
            GUILayout.Space(6);
            DrawAttackInfoSection(entry);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawDetailHeader(SerializedProperty entry)
        {
            Rect hdr = GUILayoutUtility.GetRect(0, 28f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(hdr, ColorHeader);

            var nameProp = entry.FindPropertyRelative("sequenceName");
            nameProp.stringValue = GUI.TextField(
                new Rect(hdr.x + 8, hdr.y + 5, hdr.width - 160f, 18f),
                nameProp.stringValue, EditorStyles.toolbarTextField);

            GUI.Label(new Rect(hdr.xMax - 150f, hdr.y + 5, 50f, 18f),
                "우선순위:", EditorStyles.miniLabel);
            var priProp = entry.FindPropertyRelative("priority");
            priProp.intValue = EditorGUI.IntField(
                new Rect(hdr.xMax - 100f, hdr.y + 5, 50f, 18f),
                priProp.intValue);
        }

        // ── 입력 체인 시각화 ─────────────────────────────────────────
        private void DrawInputChain(SerializedProperty entry)
        {
            var seqProp = entry.FindPropertyRelative("inputSequence");

            EditorGUILayout.LabelField("입력 패턴", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "L = 좌클릭(약공격)   H = 우클릭(강공격)\n" +
                "버튼 클릭 = L↔H 토글   [×] = 삭제   [+L] / [+H] = 스텝 추가",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < seqProp.arraySize; i++)
            {
                var step      = seqProp.GetArrayElementAtIndex(i);
                var inputProp = step.FindPropertyRelative("inputType");
                var current   = (ComboInputType)inputProp.enumValueIndex;
                bool isLight  = current == ComboInputType.LightAttack;

                // 토글 버튼
                var oldBg = GUI.backgroundColor;
                GUI.backgroundColor = isLight ? ColorLight : ColorHeavy;
                if (GUILayout.Button(
                    new GUIContent(isLight ? "L" : "H",
                                   isLight ? "좌클릭 (약공격)" : "우클릭 (강공격)"),
                    GUILayout.Width(StepBtnW), GUILayout.Height(StepBtnH)))
                {
                    inputProp.enumValueIndex = isLight
                        ? (int)ComboInputType.HeavyAttack
                        : (int)ComboInputType.LightAttack;
                }

                // 삭제 버튼
                GUI.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
                if (GUILayout.Button("×", GUILayout.Width(16), GUILayout.Height(StepBtnH)))
                {
                    seqProp.DeleteArrayElementAtIndex(i);
                    GUI.backgroundColor = oldBg;
                    break;
                }
                GUI.backgroundColor = oldBg;

                if (i < seqProp.arraySize - 1)
                    GUILayout.Label("→", GUILayout.Width(16), GUILayout.Height(StepBtnH));
            }

            GUILayout.Space(8);

            var addBg = GUI.backgroundColor;
            GUI.backgroundColor = ColorLight;
            if (GUILayout.Button("+L", GUILayout.Width(StepBtnW), GUILayout.Height(StepBtnH)))
                AddStep(seqProp, ComboInputType.LightAttack);
            GUI.backgroundColor = ColorHeavy;
            if (GUILayout.Button("+H", GUILayout.Width(StepBtnW), GUILayout.Height(StepBtnH)))
                AddStep(seqProp, ComboInputType.HeavyAttack);
            GUI.backgroundColor = addBg;

            EditorGUILayout.EndHorizontal();

            if (seqProp.arraySize == 0)
                EditorGUILayout.HelpBox("입력 스텝이 없습니다. [+L] 또는 [+H]로 추가하세요.", MessageType.Warning);
        }

        private void AddStep(SerializedProperty seqProp, ComboInputType type)
        {
            seqProp.arraySize++;
            seqProp.GetArrayElementAtIndex(seqProp.arraySize - 1)
                   .FindPropertyRelative("inputType").enumValueIndex = (int)type;
        }

        // ── 태그 조건 (GameplayTagId enum 기반) ──────────────────────
        private void DrawTagConditions(SerializedProperty entry)
        {
            // 섹션 헤더 + Tag Registry Editor 바로가기
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("태그 조건", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("🏷 Tag Registry Editor", EditorStyles.miniButton, GUILayout.Width(150)))
                GameplayTagRegistryEditorWindow.Open();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "Required Tags : Actor가 이 태그를 모두 보유해야 시퀀스 발동 (AND 조건)\n" +
                "Blocked Tags  : 이 중 하나라도 보유하면 시퀀스 차단\n" +
                "태그를 새로 추가하려면 Tag Registry Editor에서 정의 후 코드 생성을 실행하세요.",
                MessageType.None);

            var requiredProp = entry.FindPropertyRelative("requiredTagIds");
            var blockedProp  = entry.FindPropertyRelative("blockedTagIds");

            DrawTagIdList("Required Tags", requiredProp, ColorRequired);
            GUILayout.Space(4);
            DrawTagIdList("Blocked Tags",  blockedProp,  ColorBlocked);
        }

        /// <summary>
        /// GameplayTagId enum 드롭다운 목록을 그린다.
        /// </summary>
        private void DrawTagIdList(string label, SerializedProperty listProp, Color labelColor)
        {
            // 레이블
            var oldContent = GUI.contentColor;
            GUI.contentColor = labelColor;
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            GUI.contentColor = oldContent;

            int removeAt = -1;

            for (int i = 0; i < listProp.arraySize; i++)
            {
                var elem    = listProp.GetArrayElementAtIndex(i);
                var current = (GameplayTagId)elem.intValue;

                EditorGUILayout.BeginHorizontal();

                // enum 드롭다운
                var selected = (GameplayTagId)EditorGUILayout.EnumPopup(
                    current, GUILayout.MinWidth(200), GUILayout.ExpandWidth(true));

                if (selected != current)
                    elem.intValue = (int)selected;

                // 태그 이름 문자열 힌트 (None이 아닐 때)
                if (selected != GameplayTagId.None)
                {
                    var oldColor = GUI.contentColor;
                    GUI.contentColor = new Color(0.65f, 0.65f, 0.65f);
                    GUILayout.Label(selected.TagName(), EditorStyles.miniLabel, GUILayout.Width(180));
                    GUI.contentColor = oldColor;
                }
                else
                {
                    GUILayout.Label("(태그 없음)", EditorStyles.miniLabel, GUILayout.Width(80));
                }

                // 삭제 버튼
                var oldBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
                if (GUILayout.Button("×", GUILayout.Width(22)))
                    removeAt = i;
                GUI.backgroundColor = oldBg;

                EditorGUILayout.EndHorizontal();
            }

            if (removeAt >= 0)
                listProp.DeleteArrayElementAtIndex(removeAt);

            // 추가 버튼
            var addBg = GUI.backgroundColor;
            GUI.backgroundColor = labelColor * 0.7f;
            if (GUILayout.Button($"+ {label} 추가", GUILayout.Width(150)))
            {
                listProp.arraySize++;
                listProp.GetArrayElementAtIndex(listProp.arraySize - 1).intValue =
                    (int)GameplayTagId.None;
            }
            GUI.backgroundColor = addBg;
        }

        // ── 공격 데이터 섹션 ─────────────────────────────────────────
        private void DrawAttackInfoSection(SerializedProperty entry)
        {
            // ── 스킬 게이지 ───────────────────────────────────────────
            EditorGUILayout.LabelField("스킬 게이지 (Skill Gauge)", EditorStyles.boldLabel);

            var gaugeProp = entry.FindPropertyRelative("skillGaugeIndex");
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    new GUIContent("게이지 슬롯",
                        "소비할 스킬 게이지 슬롯 인덱스 (1-based로 표시, 내부는 0-based).\n" +
                        "-1이면 게이지 비용 없음.\n게이지 부족 시 이 시퀀스는 기본 콤보로 폴백됩니다."),
                    GUILayout.Width(80f));

                int displayVal = gaugeProp.intValue + 1; // 0-based → 1-based
                int newDisplay = EditorGUILayout.IntField(displayVal, GUILayout.Width(50f));
                gaugeProp.intValue = newDisplay - 1;     // 1-based → 0-based

                string hint = gaugeProp.intValue < 0
                    ? "게이지 없음"
                    : $"슬롯 {gaugeProp.intValue + 1} 소비";
                EditorGUILayout.LabelField(hint, EditorStyles.miniLabel);
            }

            GUILayout.Space(6);

            // ── 공격 정보 ─────────────────────────────────────────────
            EditorGUILayout.LabelField("공격 데이터 (Attack Info)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                entry.FindPropertyRelative("attackInfo"), includeChildren: true);
        }

        // ── 대상 없음 안내 ───────────────────────────────────────────
        private void DrawNoTargetMessage()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                "Project 창에서 PlayerAttackDataSO를 선택하거나\n툴바의 드롭다운으로 SO를 지정하세요.",
                EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
        }
    }
}
