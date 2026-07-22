#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Item;

namespace UPlayGround.Data.Editor.Authoring
{
    /// <summary>
    /// 퀘스트·레시피·드랍 패널이 함께 사용하는 ItemSO 검색 피커입니다.
    /// </summary>
    public static class SharedItemPicker
    {
        public static void Show(VisualElement anchor, ItemSO current, Action<ItemSO> onSelected)
        {
            if (anchor == null)
                throw new ArgumentNullException(nameof(anchor));

            UnityEditor.PopupWindow.Show(anchor.worldBound, new ItemPickerPopup(current, onSelected));
        }

        public static void Show(Rect activatorRect, ItemSO current, Action<ItemSO> onSelected)
        {
            UnityEditor.PopupWindow.Show(activatorRect, new ItemPickerPopup(current, onSelected));
        }

        private sealed class ItemPickerPopup : PopupWindowContent
        {
            private readonly ItemSO _current;
            private readonly Action<ItemSO> _onSelected;
            private readonly List<ItemSO> _allItems = new List<ItemSO>();
            private readonly List<ItemSO> _filteredItems = new List<ItemSO>();
            private ListView _listView;
            private Label _countLabel;

            public ItemPickerPopup(ItemSO current, Action<ItemSO> onSelected)
            {
                _current = current;
                _onSelected = onSelected;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(360f, 430f);
            }

            public override void OnOpen()
            {
                _allItems.Clear();
                _allItems.AddRange(AssetDatabase.FindAssets("t:ItemSO")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<ItemSO>)
                    .Where(item => item != null)
                    .OrderBy(item => item.itemId));

                BuildGui(editorWindow.rootVisualElement);
            }

            public override void OnGUI(Rect rect) { }

            public override void OnClose()
            {
                _allItems.Clear();
                _filteredItems.Clear();
            }

            private void BuildGui(VisualElement host)
            {
                host.Clear();
                var root = new VisualElement();
                root.style.flexGrow = 1f;
                root.style.paddingLeft = 6f;
                root.style.paddingRight = 6f;
                root.style.paddingTop = 6f;
                root.style.paddingBottom = 6f;

                var search = new ToolbarSearchField { tooltip = "아이템 이름 또는 ID 검색" };
                search.RegisterValueChangedCallback(evt => RefreshList(evt.newValue));
                root.Add(search);

                _countLabel = new Label();
                _countLabel.style.fontSize = 10f;
                _countLabel.style.marginTop = 4f;
                _countLabel.style.marginBottom = 4f;
                root.Add(_countLabel);

                _listView = new ListView
                {
                    itemsSource = _filteredItems,
                    fixedItemHeight = 38f,
                    selectionType = SelectionType.Single,
                    makeItem = MakeItem,
                    bindItem = BindItem
                };
                _listView.style.flexGrow = 1f;
                _listView.selectionChanged += selection =>
                {
                    ItemSO item = selection.OfType<ItemSO>().FirstOrDefault();
                    if (item == null)
                        return;
                    _onSelected?.Invoke(item);
                    editorWindow.Close();
                };
                root.Add(_listView);

                var actions = new VisualElement();
                actions.style.flexDirection = FlexDirection.Row;
                actions.style.marginTop = 6f;

                var clearButton = new Button(() =>
                {
                    _onSelected?.Invoke(null);
                    editorWindow.Close();
                }) { text = "참조 비우기" };
                actions.Add(clearButton);

                var flexibleSpace = new VisualElement();
                flexibleSpace.style.flexGrow = 1f;
                actions.Add(flexibleSpace);

                var openHubButton = new Button(() =>
                {
                    DataAuthoringHubWindow.Open(ItemDomainPanel.DomainKey, _current);
                    editorWindow.Close();
                }) { text = "아이템 도메인 열기" };
                actions.Add(openHubButton);
                root.Add(actions);

                RefreshList(string.Empty);
                host.Add(root);
            }

            private static VisualElement MakeItem()
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;

                var icon = new Image { name = "icon", scaleMode = ScaleMode.ScaleToFit };
                icon.style.width = 32f;
                icon.style.height = 32f;
                icon.style.marginRight = 6f;
                row.Add(icon);

                var text = new VisualElement();
                text.style.flexGrow = 1f;
                var name = new Label { name = "name" };
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                text.Add(name);
                var detail = new Label { name = "detail" };
                detail.style.fontSize = 10f;
                text.Add(detail);
                row.Add(text);
                return row;
            }

            private void BindItem(VisualElement element, int index)
            {
                if (index < 0 || index >= _filteredItems.Count)
                    return;

                ItemSO item = _filteredItems[index];
                element.Q<Image>("icon").sprite = item.icon;
                element.Q<Label>("name").text = item.itemName;
                element.Q<Label>("detail").text = $"ID {item.itemId} · {item.itemType} · {item.itemRarity}";
                element.tooltip = AssetDatabase.GetAssetPath(item);
            }

            private void RefreshList(string query)
            {
                string normalized = query?.Trim() ?? string.Empty;
                _filteredItems.Clear();
                _filteredItems.AddRange(_allItems.Where(item =>
                    normalized.Length == 0
                    || (item.itemName ?? string.Empty).IndexOf(normalized, StringComparison.CurrentCultureIgnoreCase) >= 0
                    || item.itemId.ToString().Contains(normalized)));
                _listView?.Rebuild();
                if (_countLabel != null)
                    _countLabel.text = $"{_filteredItems.Count:N0} / {_allItems.Count:N0}개";

                int currentIndex = _current != null ? _filteredItems.IndexOf(_current) : -1;
                if (currentIndex >= 0)
                    _listView?.SetSelectionWithoutNotify(new[] { currentIndex });
            }
        }
    }
}
#endif
