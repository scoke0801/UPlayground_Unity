using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor
{
    /// <summary>
    /// 선택한 GameObject의 인스펙터를 그대로 보여주되, 검색어를 입력하면 일치하는 직렬화 필드만 남기는 창.
    /// 검색어가 없을 때는 실제 Editor를 그리므로 커스텀 인스펙터와 PropertyDrawer가 원본 그대로 동작한다.
    /// 메뉴: UPlayGround/유틸/인스펙터 필드 검색기
    /// </summary>
    public class InspectorFieldSearchWindow : EditorWindow
    {
        /// <summary>창에 표시되는 컴포넌트 하나의 상태.</summary>
        private sealed class ComponentEntry
        {
            public Component Component;
            public GameObject Owner;

            /// <summary>검색 모드에서만 사용한다. 전체 보기에서는 Editor가 자체 SerializedObject를 소유한다.</summary>
            public SerializedObject SerializedObject;

            /// <summary>전체 보기에서만 사용한다. 펼쳤을 때 지연 생성한다.</summary>
            public UnityEditor.Editor Editor;

            public readonly List<string> Paths = new();
            public bool IsExpanded;
            public bool IsRenderFailed;
        }

        // 한 번의 검색에서 훑을 직렬화 프로퍼티 총량 상한.
        // SerializeReference 그래프나 대형 배열, 자식 포함 검색에서 창이 멈추는 것을 막는다.
        private const int PropertyScanBudget = 20000;
        private const string ScriptPropertyPath = "m_Script";

        private const string BaseTitle = "Field Search";

        // IMGUI는 폭이 모자라면 스크롤 대신 필드를 눌러 잘라낸다.
        // 이 폭 아래로는 내용을 줄이지 않고 가로 스크롤로 넘겨 값이 통째로 사라지는 것을 막는다.
        private const float MinContentWidth = 330f;

        // 이 폭 미만에서는 wideMode를 꺼서 Vector·Quaternion 계열을 라벨 아래 줄로 내린다.
        private const float WideModeMinWidth = 380f;
        private const float VerticalScrollBarWidth = 16f;
        private const float LabelWidthRatio = 0.4f;
        private const float MinLabelWidth = 110f;
        private const float MaxLabelWidth = 220f;

        // 창을 여러 개 띄워 서로 다른 대상을 비교하므로, 각 창의 대상·질의 상태는
        // 도메인 리로드 후에도 창별로 유지되어야 한다.
        // ── 대상 ──────────────────────────────────────────────────────
        [SerializeField] private GameObject _lockedTarget;
        [SerializeField] private bool _isTargetLocked;

        // ── 질의 ──────────────────────────────────────────────────────
        [SerializeField] private string _query = "";
        [SerializeField] private bool _isIncludingChildren;
        [SerializeField] private bool _isSearchingValues;

        // ── 결과 ──────────────────────────────────────────────────────
        private readonly List<ComponentEntry> _entries = new();
        private readonly List<string> _missingScriptOwners = new();
        private readonly HashSet<string> _matchedPathSet = new();

        // 재수집으로 Entry가 새로 만들어져도 펼침 상태는 유지되어야 한다.
        private readonly Dictionary<int, bool> _expandedStateByInstanceId = new();

        private int _matchedFieldCount;
        private int _scannedPropertyCount;
        private bool _isBudgetExceeded;

        // ── UI 상태 ──────────────────────────────────────────────────
        private Vector2 _scroll;
        private bool _isDirty = true;
        private string[] _queryTokens = System.Array.Empty<string>();

        private bool IsSearching => _queryTokens.Length > 0;

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/유틸/인스펙터 필드 검색기", priority = 101)]
        public static void Open()
        {
            var window = GetWindow<InspectorFieldSearchWindow>();
            // 가로 스크롤 없이 값 필드가 온전히 보이는 최소 폭(내용 하한 + 세로 스크롤바)에 맞춘다.
            window.minSize = new Vector2(MinContentWidth + VerticalScrollBarWidth, 240f);
            window.UpdateTitle();
            window.Show();
        }

        /// <summary>
        /// 현재 창의 질의 상태를 물려받은 창을 하나 더 띄운다.
        /// 새 창은 현재 대상에 잠기므로, 원본 창으로 선택을 계속 옮기면서 둘을 나란히 비교할 수 있다.
        /// </summary>
        private void OpenDuplicate(GameObject target)
        {
            var window = CreateWindow<InspectorFieldSearchWindow>();
            window.minSize = minSize;
            window._query = _query;
            window._isIncludingChildren = _isIncludingChildren;
            window._isSearchingValues = _isSearchingValues;
            window._lockedTarget = target;
            window._isTargetLocked = target != null;
            window.UpdateTitle();
            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += MarkDirtyAndRepaint;
            UpdateTitle();
            _isDirty = true;
        }

        /// <summary>창이 여러 개일 때 탭만 보고 구분할 수 있도록 잠긴 대상 이름을 제목에 넣는다.</summary>
        private void UpdateTitle()
        {
            string suffix = _isTargetLocked && _lockedTarget != null ? $" : {_lockedTarget.name}" : "";
            titleContent = new GUIContent(BaseTitle + suffix);
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= MarkDirtyAndRepaint;
            ClearEntries();
        }

        private void OnSelectionChange() => MarkDirtyAndRepaint();

        private void OnHierarchyChange() => MarkDirtyAndRepaint();

        private void MarkDirtyAndRepaint()
        {
            _isDirty = true;
            Repaint();
        }

        // ─────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            GameObject target = ResolveTarget();

            DrawToolbar(target);

            // 레이아웃과 리페인트가 서로 다른 결과를 그리면 IMGUI가 깨지므로 Layout에서만 재수집한다.
            if (Event.current.type == EventType.Layout && _isDirty)
            {
                RebuildEntries(target);
                _isDirty = false;
            }

            if (target == null)
            {
                EditorGUILayout.HelpBox("Hierarchy 또는 Scene 뷰에서 오브젝트를 선택하세요.", MessageType.Info);
                return;
            }

            DrawSummary(target);
            DrawEntries();
        }

        private GameObject ResolveTarget()
        {
            if (_isTargetLocked && _lockedTarget != null)
                return _lockedTarget;

            return Selection.activeGameObject;
        }

        private void DrawToolbar(GameObject target)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                _query = EditorGUILayout.TextField(_query, EditorStyles.toolbarSearchField);
                bool isQueryChanged = EditorGUI.EndChangeCheck();

                EditorGUI.BeginChangeCheck();
                _isIncludingChildren = GUILayout.Toggle(_isIncludingChildren,
                    new GUIContent("자식 포함", "선택한 오브젝트의 하위 계층 컴포넌트까지 대상에 넣는다."),
                    EditorStyles.toolbarButton, GUILayout.Width(64f));
                _isSearchingValues = GUILayout.Toggle(_isSearchingValues,
                    new GUIContent("값 검색", "필드 이름뿐 아니라 현재 값 문자열도 검색 대상에 넣는다."),
                    EditorStyles.toolbarButton, GUILayout.Width(56f));
                bool isOptionChanged = EditorGUI.EndChangeCheck();

                bool isLockToggled = GUILayout.Toggle(_isTargetLocked,
                    new GUIContent(_isTargetLocked ? "잠김" : "잠금", "대상을 고정해 선택이 바뀌어도 유지한다."),
                    EditorStyles.toolbarButton, GUILayout.Width(44f));
                if (isLockToggled != _isTargetLocked)
                {
                    _isTargetLocked = isLockToggled;
                    _lockedTarget = isLockToggled ? target : null;
                    UpdateTitle();
                }

                if (GUILayout.Button(
                        new GUIContent("+", "지금 대상에 잠긴 창을 하나 더 연다. 두 오브젝트를 나란히 비교할 때 쓴다."),
                        EditorStyles.toolbarButton, GUILayout.Width(24f)))
                {
                    // 버튼 처리 도중 새 창을 만들면 이번 프레임의 IMGUI 레이아웃이 어긋나므로 다음 프레임으로 미룬다.
                    GameObject duplicateTarget = target;
                    EditorApplication.delayCall += () => OpenDuplicate(duplicateTarget);
                    GUIUtility.ExitGUI();
                }

                if (isQueryChanged || isOptionChanged)
                    _isDirty = true;
            }
        }

        private void DrawSummary(GameObject target)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string scope = _isIncludingChildren ? $"{target.name} (하위 포함)" : target.name;
                string detail = IsSearching
                    ? $"필드 {_matchedFieldCount}개 / 컴포넌트 {_entries.Count}개"
                    : $"컴포넌트 {_entries.Count}개";

                EditorGUILayout.LabelField($"{scope} — {detail}", EditorStyles.miniBoldLabel);

                if (!IsSearching && GUILayout.Button("모두 펼치기", EditorStyles.miniButtonLeft, GUILayout.Width(72f)))
                    SetAllExpanded(true);
                if (!IsSearching && GUILayout.Button("모두 접기", EditorStyles.miniButtonRight, GUILayout.Width(64f)))
                    SetAllExpanded(false);
            }

            if (_isBudgetExceeded)
            {
                EditorGUILayout.HelpBox(
                    "필드가 너무 많아 일부만 검색했습니다. '자식 포함'을 끄거나 검색어를 좁혀 주세요.",
                    MessageType.Warning);
            }

            foreach (string owner in _missingScriptOwners)
                EditorGUILayout.HelpBox($"'{owner}'에 Missing Script가 있어 건너뛰었습니다.", MessageType.Warning);

            if (_entries.Count == 0)
            {
                EditorGUILayout.HelpBox(IsSearching ? "일치하는 필드가 없습니다." : "표시할 컴포넌트가 없습니다.",
                    MessageType.None);
            }
        }

        private void SetAllExpanded(bool isExpanded)
        {
            foreach (ComponentEntry entry in _entries)
            {
                entry.IsExpanded = isExpanded;
                if (entry.Component != null)
                    _expandedStateByInstanceId[entry.Component.GetInstanceID()] = isExpanded;
            }
        }

        private void DrawEntries()
        {
            float viewWidth = position.width - VerticalScrollBarWidth;
            float contentWidth = Mathf.Max(viewWidth, MinContentWidth);

            using var scrollScope = new EditorGUILayout.ScrollViewScope(_scroll);
            _scroll = scrollScope.scrollPosition;

            // labelWidth/wideMode는 전역 상태라 다른 에디터 GUI에 새지 않도록 반드시 되돌린다.
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            bool previousWideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.labelWidth =
                Mathf.Clamp(contentWidth * LabelWidthRatio, MinLabelWidth, MaxLabelWidth);
            EditorGUIUtility.wideMode = contentWidth >= WideModeMinWidth;

            try
            {
                // 내용에 하한 폭을 주면 창이 더 좁을 때 잘리는 대신 가로 스크롤바가 생긴다.
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(contentWidth)))
                {
                    for (int i = 0; i < _entries.Count; i++)
                    {
                        ComponentEntry entry = _entries[i];

                        // 플레이 모드 전환·파괴로 대상이 사라지면 다음 프레임에 다시 수집한다.
                        if (entry.Component == null)
                        {
                            _isDirty = true;
                            continue;
                        }

                        DrawEntry(entry);
                    }
                }
            }
            finally
            {
                EditorGUIUtility.labelWidth = previousLabelWidth;
                EditorGUIUtility.wideMode = previousWideMode;
            }
        }

        private void DrawEntry(ComponentEntry entry)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawEntryHeader(entry);

                if (!entry.IsExpanded)
                    return;

                EditorGUI.indentLevel++;
                if (IsSearching)
                    DrawMatchedFields(entry);
                else
                    DrawFullInspector(entry);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawEntryHeader(ComponentEntry entry)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var icon = EditorGUIUtility.ObjectContent(entry.Component, entry.Component.GetType()).image;
                string title = entry.Component.GetType().Name;
                if (_isIncludingChildren && entry.Owner != null)
                    title += $"   ({entry.Owner.name})";

                bool isExpanded = EditorGUILayout.Foldout(entry.IsExpanded, new GUIContent(title, icon), true,
                    EditorStyles.foldoutHeader);
                if (isExpanded != entry.IsExpanded)
                {
                    entry.IsExpanded = isExpanded;
                    _expandedStateByInstanceId[entry.Component.GetInstanceID()] = isExpanded;
                }

                GUILayout.FlexibleSpace();
                DrawEnabledToggle(entry.Component);

                if (GUILayout.Button("선택", EditorStyles.miniButton, GUILayout.Width(40f)))
                {
                    Selection.activeGameObject = entry.Owner;
                    EditorGUIUtility.PingObject(entry.Owner);
                }
            }
        }

        /// <summary>Unity 기본 인스펙터 헤더처럼 Behaviour의 활성 상태를 편집한다.</summary>
        private void DrawEnabledToggle(Component component)
        {
            if (component is not Behaviour behaviour)
                return;

            using (new EditorGUI.DisabledScope((behaviour.hideFlags & HideFlags.NotEditable) != 0))
            {
                EditorGUI.BeginChangeCheck();
                bool isEnabled = GUILayout.Toggle(
                    behaviour.enabled,
                    new GUIContent("", "이 컴포넌트의 활성 상태를 전환한다."),
                    EditorStyles.toggle,
                    GUILayout.Width(18f));
                if (!EditorGUI.EndChangeCheck())
                    return;

                Undo.RecordObject(behaviour, isEnabled ? "컴포넌트 활성화" : "컴포넌트 비활성화");
                behaviour.enabled = isEnabled;

                if (PrefabUtility.IsPartOfPrefabInstance(behaviour))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);

                EditorUtility.SetDirty(behaviour);
                _isDirty = true;
            }
        }

        /// <summary>검색어가 없을 때는 실제 Editor를 그려 커스텀 인스펙터를 그대로 재현한다.</summary>
        private void DrawFullInspector(ComponentEntry entry)
        {
            if (entry.IsRenderFailed)
            {
                EditorGUILayout.HelpBox("이 컴포넌트의 인스펙터를 그리는 중 예외가 발생해 표시를 중단했습니다.",
                    MessageType.Warning);
                return;
            }

            if (entry.Editor == null)
                entry.Editor = UnityEditor.Editor.CreateEditor(entry.Component);

            if (entry.Editor == null)
            {
                EditorGUILayout.HelpBox("Editor를 생성하지 못했습니다.", MessageType.Warning);
                return;
            }

            try
            {
                entry.Editor.OnInspectorGUI();
            }
            catch (ExitGUIException)
            {
                // 오브젝트 피커 등이 정상적으로 GUI를 빠져나가는 경로이므로 삼키면 안 된다.
                throw;
            }
            catch (System.Exception exception)
            {
                // 서드파티 인스펙터 하나가 창 전체를 망가뜨리지 않도록 해당 항목만 차단한다.
                entry.IsRenderFailed = true;
                Debug.LogException(exception);
            }
        }

        private void DrawMatchedFields(ComponentEntry entry)
        {
            if (entry.SerializedObject == null)
            {
                _isDirty = true;
                return;
            }

            entry.SerializedObject.Update();

            for (int i = 0; i < entry.Paths.Count; i++)
            {
                string path = entry.Paths[i];
                SerializedProperty property = entry.SerializedObject.FindProperty(path);
                if (property == null)
                {
                    _isDirty = true;
                    continue;
                }

                DrawParentBreadcrumb(path);
                EditorGUILayout.PropertyField(property, true);
            }

            entry.SerializedObject.ApplyModifiedProperties();
        }

        /// <summary>중첩 필드는 어느 부모 아래에 있는지 보여야 값을 오해하지 않는다.</summary>
        private static void DrawParentBreadcrumb(string path)
        {
            int lastDot = path.LastIndexOf('.');
            if (lastDot < 0)
                return;

            string parent = path.Substring(0, lastDot).Replace(".Array.data", "");
            EditorGUILayout.LabelField(parent, EditorStyles.miniLabel);
        }

        // ── 수집 ──────────────────────────────────────────────────────
        private void RebuildEntries(GameObject target)
        {
            ClearEntries();
            _queryTokens = BuildQueryTokens(_query);

            if (target == null)
                return;

            foreach (GameObject owner in EnumerateTargets(target))
            {
                if (_isBudgetExceeded)
                    return;

                Component[] components = owner.GetComponents<Component>();
                foreach (Component component in components)
                {
                    if (_isBudgetExceeded)
                        return;

                    if (component == null)
                    {
                        // GetComponents는 스크립트가 유실된 슬롯을 null로 돌려준다.
                        if (!_missingScriptOwners.Contains(owner.name))
                            _missingScriptOwners.Add(owner.name);
                        continue;
                    }

                    if (IsSearching)
                        AddSearchedEntry(component, owner);
                    else
                        AddFullEntry(component, owner, isRootOwner: owner == target);
                }
            }
        }

        private IEnumerable<GameObject> EnumerateTargets(GameObject target)
        {
            if (!_isIncludingChildren)
            {
                yield return target;
                yield break;
            }

            Transform[] transforms = target.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in transforms)
                yield return transform.gameObject;
        }

        /// <summary>전체 보기 항목. 자식 컴포넌트는 접어 두어 Editor 생성 비용을 미룬다.</summary>
        private void AddFullEntry(Component component, GameObject owner, bool isRootOwner)
        {
            _entries.Add(new ComponentEntry
            {
                Component = component,
                Owner = owner,
                IsExpanded = ResolveExpandedState(component, isRootOwner),
            });
        }

        private bool ResolveExpandedState(Component component, bool isRootOwner)
        {
            if (_expandedStateByInstanceId.TryGetValue(component.GetInstanceID(), out bool isExpanded))
                return isExpanded;

            return isRootOwner;
        }

        private void AddSearchedEntry(Component component, GameObject owner)
        {
            var serializedObject = new SerializedObject(component);
            _matchedPathSet.Clear();

            var iterator = serializedObject.GetIterator();

            // NextVisible은 접힌 폴드아웃 내부를 건너뛰므로, 숨은 필드까지 찾으려면 Next로 전체를 훑어야 한다.
            while (iterator.Next(true))
            {
                if (++_scannedPropertyCount > PropertyScanBudget)
                {
                    _isBudgetExceeded = true;
                    break;
                }

                if (iterator.propertyPath == ScriptPropertyPath)
                    continue;

                if (IsMatch(iterator))
                    _matchedPathSet.Add(iterator.propertyPath);
            }

            if (_matchedPathSet.Count == 0)
            {
                serializedObject.Dispose();
                return;
            }

            var entry = new ComponentEntry
            {
                Component = component,
                Owner = owner,
                SerializedObject = serializedObject,
                IsExpanded = true,
            };

            // 부모가 이미 걸렸으면 자식은 부모 렌더링에 포함되므로 따로 그리지 않는다.
            foreach (string path in _matchedPathSet)
            {
                if (!HasMatchedAncestor(path))
                    entry.Paths.Add(path);
            }

            entry.Paths.Sort(System.StringComparer.Ordinal);
            _matchedFieldCount += entry.Paths.Count;
            _entries.Add(entry);
        }

        private bool HasMatchedAncestor(string path)
        {
            int cut = path.LastIndexOf('.');
            while (cut > 0)
            {
                string parent = path.Substring(0, cut);
                if (_matchedPathSet.Contains(parent))
                    return true;

                cut = parent.LastIndexOf('.');
            }

            return false;
        }

        private bool IsMatch(SerializedProperty property)
        {
            string displayName = property.displayName;
            string name = property.name;
            string value = _isSearchingValues ? PropertyValueToString(property) : null;

            foreach (string token in _queryTokens)
            {
                bool isTokenFound =
                    Contains(displayName, token) ||
                    Contains(name, token) ||
                    (value != null && Contains(value, token));

                if (!isTokenFound)
                    return false;
            }

            return true;
        }

        private static bool Contains(string source, string token)
        {
            return !string.IsNullOrEmpty(source) &&
                   source.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string[] BuildQueryTokens(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return System.Array.Empty<string>();

            return query.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
        }

        private static string PropertyValueToString(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.ArraySize:
                    return property.intValue.ToString();
                case SerializedPropertyType.Boolean:
                    return property.boolValue.ToString();
                case SerializedPropertyType.Float:
                    return property.doubleValue.ToString("R");
                case SerializedPropertyType.String:
                    return property.stringValue;
                case SerializedPropertyType.Color:
                    return property.colorValue.ToString();
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue != null ? property.objectReferenceValue.name : "None";
                case SerializedPropertyType.Enum:
                    return EnumValueToString(property);
                case SerializedPropertyType.Vector2:
                    return property.vector2Value.ToString();
                case SerializedPropertyType.Vector3:
                    return property.vector3Value.ToString();
                case SerializedPropertyType.Vector4:
                    return property.vector4Value.ToString();
                case SerializedPropertyType.Quaternion:
                    return property.quaternionValue.eulerAngles.ToString();
                case SerializedPropertyType.Rect:
                    return property.rectValue.ToString();
                case SerializedPropertyType.Bounds:
                    return property.boundsValue.ToString();
                case SerializedPropertyType.ManagedReference:
                    return property.managedReferenceFullTypename;
                default:
                    return null;
            }
        }

        /// <summary>enumValueIndex는 다중 값이나 유실된 항목에서 범위를 벗어날 수 있어 방어한다.</summary>
        private static string EnumValueToString(SerializedProperty property)
        {
            string[] names = property.enumDisplayNames;
            int index = property.enumValueIndex;
            return index >= 0 && index < names.Length ? names[index] : property.intValue.ToString();
        }

        private void ClearEntries()
        {
            foreach (ComponentEntry entry in _entries)
            {
                entry.SerializedObject?.Dispose();

                // Editor는 UnityEngine.Object라서 명시적으로 파괴하지 않으면 누수된다.
                if (entry.Editor != null)
                    UnityEngine.Object.DestroyImmediate(entry.Editor);
            }

            _entries.Clear();
            _missingScriptOwners.Clear();
            _matchedPathSet.Clear();
            _matchedFieldCount = 0;
            _scannedPropertyCount = 0;
            _isBudgetExceeded = false;
        }
    }
}
