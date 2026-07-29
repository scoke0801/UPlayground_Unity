using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UPlayGround.Animation.Editor.UIToolkit
{
    public sealed class MotionListView : VisualElement
    {
        public sealed class Item
        {
            public string Group;
            public string Title;
            public string Subtitle;
            public object UserData;
            public bool IsHeader;
            public bool IsSelected;
        }

        readonly List<Item> _source = new();
        readonly List<Item> _visible = new();
        readonly HashSet<string> _collapsedGroups = new(StringComparer.Ordinal);
        readonly Action<Item> _onSelected;
        readonly ToolbarSearchField _search;
        readonly ListView _list;
        bool _suppressSelection;
        bool _restoreKeyboardFocusAfterRebuild;
        bool _rebuildScheduled;

        public MotionListView(Action<Item> onSelected, Action onAdd)
        {
            _onSelected = onSelected;
            AddToClassList("up-motion-list-view");

            var header = new VisualElement();
            header.AddToClassList("up-panel-header");
            var kicker = new Label("MOTION SET");
            kicker.AddToClassList("up-panel-kicker");
            header.Add(kicker);
            var title = new Label("모션 목록");
            title.AddToClassList("up-panel-title");
            header.Add(title);
            Add(header);

            _search = new ToolbarSearchField();
            _search.AddToClassList("up-motion-list-search");
            _search.RegisterValueChangedCallback(_ => RebuildVisibleItems());
            Add(_search);

            _list = new ListView
            {
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                reorderable = false,
                showAlternatingRowBackgrounds = AlternatingRowBackground.None,
                makeItem = MakeItem,
                bindItem = BindItem,
            };
            _list.AddToClassList("up-motion-list");
            _list.focusable = true;
            _list.selectionChanged += HandleSelectionChanged;
            _list.RegisterCallback<KeyDownEvent>(HandleKeyDown, TrickleDown.TrickleDown);
            _list.RegisterCallback<NavigationMoveEvent>(
                HandleNavigationMove,
                TrickleDown.TrickleDown);
            _list.RegisterCallback<PointerDownEvent>(
                _ => _list.schedule.Execute(() => _list.Focus()),
                TrickleDown.TrickleDown);
            Add(_list);

            var addButton = new Button(() => onAdd?.Invoke()) { text = "+ 키/에셋 추가" };
            addButton.AddToClassList("up-motion-list-add");
            Add(addButton);
        }

        public void SetItems(IEnumerable<Item> items)
        {
            _source.Clear();
            if (items != null)
                _source.AddRange(items);
            RebuildVisibleItems();
        }

        VisualElement MakeItem()
        {
            var row = new VisualElement();
            row.AddToClassList("up-motion-list-item");

            var title = new Label { name = "title" };
            title.AddToClassList("up-motion-list-item-title");
            row.Add(title);

            var subtitle = new Label { name = "subtitle" };
            subtitle.AddToClassList("up-motion-list-item-subtitle");
            row.Add(subtitle);
            return row;
        }

        void BindItem(VisualElement element, int index)
        {
            Item item = _visible[index];
            element.userData = item;
            element.EnableInClassList("up-motion-list-group", item.IsHeader);
            element.EnableInClassList("up-motion-list-row", !item.IsHeader);
            element.EnableInClassList("up-motion-list-row-selected", item.IsSelected);

            var title = element.Q<Label>("title");
            var subtitle = element.Q<Label>("subtitle");
            title.text = item.IsHeader
                ? $"{(_collapsedGroups.Contains(item.Group) ? "▶" : "▼")} {item.Group}"
                : item.Title;
            subtitle.text = item.Subtitle ?? string.Empty;
            subtitle.EnableInClassList("up-hidden", item.IsHeader);
        }

        void HandleSelectionChanged(IEnumerable<object> selection)
        {
            if (_suppressSelection)
                return;

            Item item = selection.OfType<Item>().FirstOrDefault();
            if (item == null)
                return;

            if (item.IsHeader)
            {
                _restoreKeyboardFocusAfterRebuild = true;
                if (!_collapsedGroups.Add(item.Group))
                    _collapsedGroups.Remove(item.Group);
                RebuildVisibleItems();
                return;
            }

            foreach (Item sourceItem in _source)
                sourceItem.IsSelected = ReferenceEquals(sourceItem, item);
            _list.RefreshItems();
            _onSelected?.Invoke(item);
        }

        void HandleKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Home:
                    _list.panel?.focusController?.IgnoreEvent(evt);
                    SelectBoundaryItem(true);
                    evt.StopImmediatePropagation();
                    return;
                case KeyCode.End:
                    _list.panel?.focusController?.IgnoreEvent(evt);
                    SelectBoundaryItem(false);
                    evt.StopImmediatePropagation();
                    return;
                default:
                    return;
            }
        }

        void HandleNavigationMove(NavigationMoveEvent evt)
        {
            int direction;
            switch (evt.direction)
            {
                case NavigationMoveEvent.Direction.Up:
                    direction = -1;
                    break;
                case NavigationMoveEvent.Direction.Down:
                    direction = 1;
                    break;
                default:
                    return;
            }

            // Unity 6 ListView는 방향키를 NavigationMoveEvent로 변환한다.
            // 이 이벤트 하나만 처리하고 ListView 기본 탐색은 무시해야 중복 이동하지 않는다.
            _list.panel?.focusController?.IgnoreEvent(evt);
            MoveSelection(direction);
            evt.StopImmediatePropagation();
        }

        void MoveSelection(int direction)
        {
            int current = _list.selectedIndex;
            if (current < 0)
                current = _visible.FindIndex(item => !item.IsHeader && item.IsSelected);

            int next = FindSelectableIndex(current, direction);
            if (next < 0 && current < 0)
                next = FindSelectableIndex(direction > 0 ? -1 : _visible.Count, direction);
            if (next < 0)
                return;

            _restoreKeyboardFocusAfterRebuild = true;
            _list.SetSelection(next);
            _list.ScrollToItem(next);
        }

        void SelectBoundaryItem(bool first)
        {
            int index = first
                ? _visible.FindIndex(item => !item.IsHeader)
                : _visible.FindLastIndex(item => !item.IsHeader);
            if (index < 0)
                return;
            _restoreKeyboardFocusAfterRebuild = true;
            _list.SetSelection(index);
            _list.ScrollToItem(index);
        }

        int FindSelectableIndex(int current, int direction)
        {
            for (int index = current + direction;
                 index >= 0 && index < _visible.Count;
                 index += direction)
            {
                if (!_visible[index].IsHeader)
                    return index;
            }
            return -1;
        }

        void RebuildVisibleItems()
        {
            if (_rebuildScheduled)
                return;

            // 이 목록은 IMGUIContainer의 레이아웃 계산 중에도 갱신 요청을 받을 수 있다.
            // 그 시점에 ListView.Rebuild를 즉시 호출하면 VisualElement 계층 변경 예외가
            // 발생하므로 다음 UI 업데이트로 합친다.
            _rebuildScheduled = true;
            _list.schedule.Execute(() =>
            {
                _rebuildScheduled = false;
                RebuildVisibleItemsNow();
            });
        }

        void RebuildVisibleItemsNow()
        {
            bool restoreKeyboardFocus =
                _restoreKeyboardFocusAfterRebuild || HasListKeyboardFocus();
            _visible.Clear();
            string query = _search.value?.Trim() ?? string.Empty;

            foreach (IGrouping<string, Item> group in _source
                         .Where(item => !item.IsHeader)
                         .GroupBy(item => item.Group ?? "기타"))
            {
                List<Item> matches = group.Where(item => Matches(item, query)).ToList();
                if (matches.Count == 0)
                    continue;

                _visible.Add(new Item { Group = group.Key, Title = group.Key, IsHeader = true });
                if (!_collapsedGroups.Contains(group.Key))
                    _visible.AddRange(matches);
            }

            _suppressSelection = true;
            _list.itemsSource = _visible;
            _list.Rebuild();
            int selectedIndex = _visible.FindIndex(item => !item.IsHeader && item.IsSelected);
            if (selectedIndex >= 0)
                _list.SetSelectionWithoutNotify(new[] { selectedIndex });
            else
                _list.ClearSelection();
            _suppressSelection = false;

            _restoreKeyboardFocusAfterRebuild = false;
            if (restoreKeyboardFocus)
            {
                int indexToReveal = selectedIndex;
                _list.schedule.Execute(() =>
                {
                    _list.Focus();
                    if (indexToReveal >= 0)
                        _list.ScrollToItem(indexToReveal);
                });
            }
        }

        bool HasListKeyboardFocus()
        {
            if (_list.panel?.focusController?.focusedElement is not VisualElement focused)
                return false;
            return focused == _list || _list.Contains(focused);
        }

        static bool Matches(Item item, string query)
        {
            if (string.IsNullOrEmpty(query))
                return true;

            return Contains(item.Group, query) ||
                   Contains(item.Title, query) ||
                   Contains(item.Subtitle, query);
        }

        static bool Contains(string value, string query)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
