using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor
{
    /// <summary>
    /// 선택한 대상(GameObject·에셋·다중 선택)의 컴포넌트와 머티리얼을 한 창에서 훑고, 검색어로 필드를 좁혀 보는 창.
    /// 검색어가 없으면 실제 Editor를 그리므로 커스텀 인스펙터와 PropertyDrawer가 원본 그대로 동작한다.
    /// 메뉴: UPlayGround/유틸/인스펙터 필드 검색기
    /// </summary>
    public partial class InspectorFieldSearchWindow : EditorWindow
    {
        // 한 번의 검색에서 훑을 프로퍼티 총량 상한.
        // SerializeReference 그래프나 대형 배열, 자식 포함 검색에서 창이 멈추는 것을 막는다.
        private const int PropertyScanBudget = 20000;

        // 자식 포함에서 한 번에 다룰 오브젝트 상한. 대형 프리팹을 통째로 펼쳐도 창이 버티게 한다.
        private const int TargetObjectBudget = 300;

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

        // 화면 밖 항목은 그리지 않는다. 스크롤을 조금 움직였을 때 빈 칸이 보이지 않도록 한 화면만큼 여유를 둔다.
        private const float CullMargin = 400f;
        private const float EstimatedGroupHeaderHeight = 46f;
        private const float EstimatedEntryHeight = 24f;

        // 창을 여러 개 띄워 서로 다른 대상을 비교하므로, 각 창의 대상·질의 상태는
        // 도메인 리로드 후에도 창별로 유지되어야 한다.
        // ── 대상 ──────────────────────────────────────────────────────
        [SerializeField] private UnityEngine.Object[] _lockedTargets;
        [SerializeField] private bool _isTargetLocked;

        // ── 질의 ──────────────────────────────────────────────────────
        [SerializeField] private string _query = "";
        [SerializeField] private bool _isIncludingChildren;
        [SerializeField] private bool _isSearchingValues;
        [SerializeField] private bool _isIncludingMaterials = true;
        [SerializeField] private bool _isFollowingSelection = true;

        // ── 결과 ──────────────────────────────────────────────────────
        private readonly List<TargetGroup> _groups = new();
        private readonly List<string> _missingScriptOwners = new();
        private readonly Dictionary<string, int> _matchedScoreByPath = new();
        private readonly HashSet<int> _visitedMaterialIds = new();
        private readonly List<UnityEngine.Object> _resolvedTargets = new();

        // 재수집으로 항목이 새로 만들어져도 펼침 상태와 Editor 인스턴스는 유지되어야 한다.
        // Editor를 매번 파괴·재생성하면 Hierarchy가 바뀔 때마다 창 전체가 다시 굳는다.
        private readonly Dictionary<int, bool> _expandedStateByKey = new();
        private readonly Dictionary<int, bool> _groupExpandedById = new();
        private readonly Dictionary<int, InspectedEntry> _entryByKey = new();
        private readonly Dictionary<int, InspectedEntry> _reusableEntries = new();

        private int _matchedFieldCount;
        private int _componentEntryCount;
        private int _materialEntryCount;
        private int _scannedPropertyCount;
        private bool _isBudgetExceeded;
        private bool _isTargetBudgetExceeded;
        private bool _isMultiEditing;
        private string _targetDescription = "";

        // ── UI 상태 ──────────────────────────────────────────────────
        private Vector2 _scroll;
        private bool _isDirty = true;
        private string[] _queryTokens = System.Array.Empty<string>();

        // 같은 질의에서 필드 이름 매칭 결과는 바뀌지 않으므로, 서명이 같으면 재스캔을 건너뛴다.
        private string _scanSignature = "";
        private UnityEngine.Object _pendingScrollTarget;

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
        private void OpenDuplicate(UnityEngine.Object[] targets)
        {
            var window = CreateWindow<InspectorFieldSearchWindow>();
            window.minSize = minSize;
            window._query = _query;
            window._isIncludingChildren = _isIncludingChildren;
            window._isSearchingValues = _isSearchingValues;
            window._isIncludingMaterials = _isIncludingMaterials;
            window._isFollowingSelection = _isFollowingSelection;
            window._lockedTargets = targets;
            window._isTargetLocked = targets != null && targets.Length > 0;
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

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= MarkDirtyAndRepaint;
            DisposeAllEntries();
            DisposeInlineEditors();
        }

        /// <summary>창이 여러 개일 때 탭만 보고 구분할 수 있도록 잠긴 대상 이름을 제목에 넣는다.</summary>
        private void UpdateTitle()
        {
            string suffix = "";
            if (_isTargetLocked && _lockedTargets != null && _lockedTargets.Length > 0 && _lockedTargets[0] != null)
            {
                suffix = _lockedTargets.Length > 1
                    ? $" : {_lockedTargets[0].name} 외 {_lockedTargets.Length - 1}"
                    : $" : {_lockedTargets[0].name}";
            }

            titleContent = new GUIContent(BaseTitle + suffix);
        }

        private void OnSelectionChange()
        {
            // 자식 포함으로 수십 개 오브젝트를 늘어놓으면 어느 줄이 지금 선택한 오브젝트인지 찾기 어렵다.
            // Hierarchy에서 고른 오브젝트의 그룹으로 창을 자동으로 굴려 준다.
            if (_isFollowingSelection && _isIncludingChildren)
                _pendingScrollTarget = Selection.activeObject;

            MarkDirtyAndRepaint();
        }

        private void OnHierarchyChange() => MarkDirtyAndRepaint();

        private void MarkDirtyAndRepaint()
        {
            _isDirty = true;
            Repaint();
        }

        // ─────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            List<UnityEngine.Object> targets = ResolveTargets();

            DrawToolbar(targets);

            // 레이아웃과 리페인트가 서로 다른 결과를 그리면 IMGUI가 깨지므로 Layout에서만 재수집·컬링 판정을 한다.
            if (Event.current.type == EventType.Layout)
            {
                if (_isDirty)
                {
                    RebuildGroups(targets);
                    _isDirty = false;
                }

                UpdateCulling();

                if (TryApplyPendingScroll())
                    UpdateCulling();
            }

            if (targets.Count == 0)
            {
                EditorGUILayout.HelpBox("Hierarchy·Project 창에서 오브젝트나 에셋을 선택하세요. 여러 개를 골라 함께 편집할 수도 있습니다.",
                    MessageType.Info);
                return;
            }

            DrawSummary();
            DrawGroups();
        }

        /// <summary>잠긴 대상이 있으면 그것을, 아니면 지금 선택한 오브젝트·에셋 전부를 대상으로 삼는다.</summary>
        private List<UnityEngine.Object> ResolveTargets()
        {
            _resolvedTargets.Clear();

            UnityEngine.Object[] source = _isTargetLocked && _lockedTargets != null && _lockedTargets.Length > 0
                ? _lockedTargets
                : Selection.objects;

            if (source == null)
                return _resolvedTargets;

            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != null)
                    _resolvedTargets.Add(source[i]);
            }

            return _resolvedTargets;
        }

        private void DrawToolbar(List<UnityEngine.Object> targets)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                _query = EditorGUILayout.TextField(_query, EditorStyles.toolbarSearchField);
                bool isQueryChanged = EditorGUI.EndChangeCheck();

                EditorGUI.BeginChangeCheck();

                // 다중 편집은 공통 컴포넌트를 맞춰야 해서 하위 계층까지 섞으면 대상이 폭발한다.
                using (new EditorGUI.DisabledScope(_isMultiEditing))
                {
                    _isIncludingChildren = GUILayout.Toggle(_isIncludingChildren,
                        new GUIContent("자식", "선택한 오브젝트의 하위 계층 컴포넌트까지 대상에 넣는다. 다중 선택에서는 쓰지 않는다."),
                        EditorStyles.toolbarButton, GUILayout.Width(36f));
                }

                _isSearchingValues = GUILayout.Toggle(_isSearchingValues,
                    new GUIContent("값", "필드 이름뿐 아니라 현재 값 문자열도 검색 대상에 넣는다."),
                    EditorStyles.toolbarButton, GUILayout.Width(28f));
                _isIncludingMaterials = GUILayout.Toggle(_isIncludingMaterials,
                    new GUIContent("Mat", "Renderer가 쓰는 머티리얼의 셰이더 프로퍼티까지 대상에 넣는다."),
                    EditorStyles.toolbarButton, GUILayout.Width(34f));
                bool isOptionChanged = EditorGUI.EndChangeCheck();

                _isFollowingSelection = GUILayout.Toggle(_isFollowingSelection,
                    new GUIContent("추적", "Hierarchy에서 고른 오브젝트의 위치로 창을 자동으로 스크롤한다."),
                    EditorStyles.toolbarButton, GUILayout.Width(36f));

                bool isLockToggled = GUILayout.Toggle(_isTargetLocked,
                    new GUIContent(_isTargetLocked ? "잠김" : "잠금", "대상을 고정해 선택이 바뀌어도 유지한다."),
                    EditorStyles.toolbarButton, GUILayout.Width(38f));
                if (isLockToggled != _isTargetLocked)
                {
                    _isTargetLocked = isLockToggled;
                    _lockedTargets = isLockToggled ? targets.ToArray() : null;
                    UpdateTitle();
                    _isDirty = true;
                }

                if (GUILayout.Button(
                        new GUIContent("+", "지금 대상에 잠긴 창을 하나 더 연다. 두 대상을 나란히 비교할 때 쓴다."),
                        EditorStyles.toolbarButton, GUILayout.Width(22f)))
                {
                    // 버튼 처리 도중 새 창을 만들면 이번 프레임의 IMGUI 레이아웃이 어긋나므로 다음 프레임으로 미룬다.
                    UnityEngine.Object[] duplicateTargets = targets.ToArray();
                    EditorApplication.delayCall += () => OpenDuplicate(duplicateTargets);
                    GUIUtility.ExitGUI();
                }

                if (isQueryChanged || isOptionChanged)
                    _isDirty = true;
            }
        }

        private void DrawSummary()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string counts = _materialEntryCount > 0
                    ? $"컴포넌트 {_componentEntryCount} · 머티리얼 {_materialEntryCount}"
                    : $"컴포넌트 {_componentEntryCount}";
                string detail = IsSearching ? $"필드 {_matchedFieldCount} / {counts}" : counts;

                EditorGUILayout.LabelField($"{_targetDescription} — {detail}", EditorStyles.miniBoldLabel);

                if (GUILayout.Button("모두 펼치기", EditorStyles.miniButtonLeft, GUILayout.Width(72f)))
                    SetAllExpanded(true);
                if (GUILayout.Button("모두 접기", EditorStyles.miniButtonRight, GUILayout.Width(64f)))
                    SetAllExpanded(false);
            }

            if (_isTargetBudgetExceeded)
            {
                EditorGUILayout.HelpBox(
                    $"오브젝트가 {TargetObjectBudget}개를 넘어 일부만 표시했습니다. '자식'을 끄거나 더 아래 오브젝트를 선택하세요.",
                    MessageType.Warning);
            }

            if (_isBudgetExceeded)
            {
                EditorGUILayout.HelpBox(
                    "필드가 너무 많아 일부만 검색했습니다. '자식'을 끄거나 검색어를 좁혀 주세요.",
                    MessageType.Warning);
            }

            foreach (string owner in _missingScriptOwners)
                EditorGUILayout.HelpBox($"'{owner}'에 Missing Script가 있어 건너뛰었습니다.", MessageType.Warning);

            if (_groups.Count == 0)
            {
                EditorGUILayout.HelpBox(IsSearching ? "일치하는 필드가 없습니다." : "표시할 항목이 없습니다.",
                    MessageType.None);
            }
        }

        private void SetAllExpanded(bool isExpanded)
        {
            foreach (TargetGroup group in _groups)
            {
                group.IsExpanded = isExpanded;
                _groupExpandedById[group.Key] = isExpanded;

                foreach (InspectedEntry entry in group.Entries)
                {
                    entry.IsExpanded = isExpanded;
                    _expandedStateByKey[entry.Key] = isExpanded;
                }
            }
        }

        /// <summary>화면 밖 항목을 골라 둔다. 판정은 Layout에서만 하고, 같은 프레임의 Repaint는 그 결과를 그대로 쓴다.</summary>
        private void UpdateCulling()
        {
            float top = _scroll.y - CullMargin;
            float bottom = _scroll.y + position.height + CullMargin;
            float y = 0f;

            for (int g = 0; g < _groups.Count; g++)
            {
                TargetGroup group = _groups[g];
                group.ContentY = y;
                y += EstimatedGroupHeaderHeight;

                if (!group.IsExpanded)
                    continue;

                for (int i = 0; i < group.Entries.Count; i++)
                {
                    InspectedEntry entry = group.Entries[i];

                    // 아직 한 번도 그리지 않아 높이를 모르는 항목은 컬링하지 않는다.
                    // 첫 프레임에 한 번 그려 높이를 얻고 나면 그 다음부터 판정에 들어온다.
                    if (entry.CachedHeight <= 0f)
                    {
                        entry.IsCulled = false;
                        y += EstimatedEntryHeight;
                        continue;
                    }

                    entry.IsCulled = y + entry.CachedHeight < top || y > bottom;
                    y += entry.CachedHeight;
                }
            }
        }

        private bool TryApplyPendingScroll()
        {
            if (_pendingScrollTarget == null)
                return false;

            for (int i = 0; i < _groups.Count; i++)
            {
                if (_groups[i].Owner != _pendingScrollTarget)
                    continue;

                _scroll.y = Mathf.Max(0f, _groups[i].ContentY - 8f);
                _pendingScrollTarget = null;
                return true;
            }

            _pendingScrollTarget = null;
            return false;
        }
    }
}
