#if UNITY_EDITOR || DEVELOPMENT_BUILD
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.Data.Item;

namespace UPlayGround.UI.DevCheat
{
    /// <summary>UI_System_DevCheatPanel — 아이템 탭(검색/카테고리/생성·삭제·최대치).</summary>
    public partial class UI_System_DevCheatPanel
    {
        private TMP_InputField _itemSearch;
        private RectTransform  _itemListContent;
        private ItemType?      _itemCategoryFilter;   // null = 전체
        private int            _itemSelectedId = -1;
        private int            _itemQuantity   = 1;

        private Image            _itemIcon;
        private TextMeshProUGUI  _itemName, _itemIdText, _itemTypeText, _itemCountText, _itemDesc, _itemQtyText;

        private static readonly (string label, ItemType? type)[] ItemCategories =
        {
            ("전체", null),
            ("소비", ItemType.CONSUMABLE),
            ("장비", ItemType.EQUIPMENT),
            ("재료", ItemType.MATERIAL),
            ("퀘스트", ItemType.QUEST),
            ("중요", ItemType.IMPORTANT),
        };

        private void BuildItemTab(RectTransform panel)
        {
            var h = AddHLG(panel.gameObject, 12, 12);

            // ── 좌: 검색 + 카테고리 + 리스트 ──
            var center = NewRect("ItemCenter", panel);
            SetSize(center.gameObject, flexW: 1);
            AddImage(center.gameObject, PanelBg);
            var cv = AddVLG(center.gameObject, 8, 8);
            cv.childForceExpandHeight = false;

            _itemSearch = MakeInput(center, "아이템 ID 또는 이름 검색", _ => RefreshItemList());
            SetSize(_itemSearch.gameObject, minH: 40, prefH: 40);

            var catBar = NewRect("Categories", center);
            SetSize(catBar.gameObject, minH: 36, prefH: 36);
            var ch = AddHLG(catBar.gameObject, 4, 0, forceExpandWidth: true);
            foreach (var cat in ItemCategories)
            {
                ItemType? t = cat.type;
                MakeButton(catBar, cat.label, BtnBg, () => { _itemCategoryFilter = t; RefreshItemList(); }, 16);
            }

            var listScroll = MakeScroll(center, out _);
            SetSize(((RectTransform)listScroll.parent.parent).gameObject, flexH: 1);
            _itemListContent = listScroll;

            // ── 우: 상세 + 수량 + 액션 ──
            var right = NewRect("ItemDetail", panel);
            SetSize(right.gameObject, minW: 360, prefW: 360);
            AddImage(right.gameObject, PanelBg);
            var rv = AddVLG(right.gameObject, 8, 12);
            rv.childForceExpandHeight = false;

            _itemIcon = AddImage(NewRect("Icon", right).gameObject, new Color(0.2f, 0.2f, 0.25f, 1f));
            SetSize(_itemIcon.gameObject, minH: 120, prefH: 120);
            _itemIcon.preserveAspect = true;

            _itemName    = MakeText(right, "-", 24, TextMain, TextAlignmentOptions.Center);
            SetSize(_itemName.gameObject, minH: 34, prefH: 34);
            _itemIdText   = MakeText(right, "ItemId  -", 16, TextSub, TextAlignmentOptions.Center);
            _itemTypeText = MakeText(right, "타입  -", 16, TextSub, TextAlignmentOptions.Center);
            _itemCountText= MakeText(right, "보유 수량  0", 16, Accent, TextAlignmentOptions.Center);
            _itemDesc     = MakeText(right, "-", 16, TextSub, TextAlignmentOptions.Left);
            SetSize(_itemDesc.gameObject, minH: 60, prefH: 60);

            // 수량 스텝
            var qtyRow = NewRect("QtyRow", right);
            SetSize(qtyRow.gameObject, minH: 44, prefH: 44);
            var qh = AddHLG(qtyRow.gameObject, 4, 0, forceExpandWidth: true);
            MakeButton(qtyRow, "-10", BtnBg, () => AdjustQty(-10));
            MakeButton(qtyRow, "-1",  BtnBg, () => AdjustQty(-1));
            _itemQtyText = MakeText(qtyRow, "1", 20, TextMain, TextAlignmentOptions.Center);
            SetSize(_itemQtyText.gameObject, minW: 60, prefW: 60);
            MakeButton(qtyRow, "+1",  BtnBg, () => AdjustQty(1));
            MakeButton(qtyRow, "+10", BtnBg, () => AdjustQty(10));

            // 액션
            var grant = MakeButton(right, "생성", AccentBtn, OnGrantItem, 20);
            SetSize(grant.gameObject, minH: 48, prefH: 48);
            var del = MakeButton(right, "삭제", DangerBtn, OnDeleteItem, 20);
            SetSize(del.gameObject, minH: 48, prefH: 48);
            var max = MakeButton(right, "최대치 지급 (99)", BtnBg, OnGrantMax, 18);
            SetSize(max.gameObject, minH: 44, prefH: 44);

            var note = MakeText(right, "! 아이템 생성 및 삭제는 즉시 반영됩니다.", 14, new Color(0.85f, 0.5f, 0.4f), TextAlignmentOptions.Center);
            SetSize(note.gameObject, minH: 30, prefH: 30);
        }

        private void RefreshItemList()
        {
            if (_itemListContent == null) return;
            ClearChildren(_itemListContent);

            var itemManager = ItemManager.Instance;
            var inv = InventoryManager.Instance;
            if (itemManager == null || !itemManager.IsItemDBLoaded || itemManager.GetItemDB() == null)
            {
                MakeText(_itemListContent, "ItemDatabase 로드 대기 중…", 16, TextSub);
                return;
            }

            string search = _itemSearch != null ? _itemSearch.text : string.Empty;
            int rowIndex = 0;
            foreach (var item in itemManager.GetItemDB().AllItems)
            {
                if (item == null) continue;
                if (_itemCategoryFilter.HasValue && item.itemType != _itemCategoryFilter.Value) continue;
                if (!MatchesSearch(item, search)) continue;

                int id = item.itemId;
                int owned = inv != null ? inv.GetItemCount(id) : 0;

                var row = NewRect("Row", _itemListContent);
                SetSize(row.gameObject, minH: 44, prefH: 44);
                var bg = AddImage(row.gameObject, id == _itemSelectedId ? AccentBtn : (rowIndex++ % 2 == 0 ? RowBg : RowBgAlt));
                var btn = row.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.onClick.AddListener(() => SelectItem(id));

                var rh = AddHLG(row.gameObject, 8, 8);
                rh.childForceExpandWidth = false;
                var idT = MakeText(row, id.ToString(), 15, TextSub); SetSize(idT.gameObject, minW: 56, prefW: 56);
                var nameT = MakeText(row, item.itemName, 16, TextMain); SetSize(nameT.gameObject, flexW: 1);
                MakeText(row, $"보유 {owned}", 14, TextSub);
            }

            if (_itemListContent.childCount == 0)
                MakeText(_itemListContent, "검색 결과가 없습니다.", 16, TextSub);
        }

        private static bool MatchesSearch(ItemSO item, string search)
        {
            if (string.IsNullOrWhiteSpace(search)) return true;
            search = search.Trim();
            if (item.itemId.ToString().Contains(search)) return true;
            return !string.IsNullOrEmpty(item.itemName) &&
                   item.itemName.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SelectItem(int id)
        {
            _itemSelectedId = id;
            RefreshItemList();   // 선택 강조 갱신
            RefreshItemDetail();
        }

        private void RefreshItemDetail()
        {
            var itemManager = ItemManager.Instance;
            var inv = InventoryManager.Instance;
            ItemSO item = itemManager != null ? itemManager.GetItemData(_itemSelectedId) : null;

            if (item == null)
            {
                if (_itemName != null) _itemName.text = "-";
                if (_itemIdText != null) _itemIdText.text = "ItemId  -";
                if (_itemTypeText != null) _itemTypeText.text = "타입  -";
                if (_itemCountText != null) _itemCountText.text = "보유 수량  0";
                if (_itemDesc != null) _itemDesc.text = "-";
                if (_itemIcon != null) { _itemIcon.sprite = null; _itemIcon.color = new Color(0.2f, 0.2f, 0.25f, 1f); }
                return;
            }

            if (_itemName != null) _itemName.text = item.itemName;
            if (_itemIdText != null) _itemIdText.text = $"ItemId  {item.itemId}";
            if (_itemTypeText != null) _itemTypeText.text = $"타입  {item.itemType.ToDisplayString()}";
            if (_itemCountText != null) _itemCountText.text = $"보유 수량  {(inv != null ? inv.GetItemCount(item.itemId) : 0)}";
            if (_itemDesc != null) _itemDesc.text = string.IsNullOrEmpty(item.itemDescription) ? "설명 없음" : item.itemDescription;
            if (_itemIcon != null)
            {
                _itemIcon.sprite = item.icon;
                _itemIcon.color = item.icon != null ? Color.white : new Color(0.2f, 0.2f, 0.25f, 1f);
            }
        }

        private void AdjustQty(int delta)
        {
            _itemQuantity = Mathf.Max(1, _itemQuantity + delta);
            if (_itemQtyText != null) _itemQtyText.text = _itemQuantity.ToString();
        }

        private void OnGrantItem()
        {
            if (_itemSelectedId < 0) return;
            CheatManager.Instance?.GrantItem(_itemSelectedId, _itemQuantity);
            RefreshItemList();
            RefreshItemDetail();
        }

        private void OnDeleteItem()
        {
            if (_itemSelectedId < 0) return;
            CheatManager.Instance?.DeleteItem(_itemSelectedId, _itemQuantity);
            RefreshItemList();
            RefreshItemDetail();
        }

        private void OnGrantMax()
        {
            if (_itemSelectedId < 0) return;
            CheatManager.Instance?.GrantMax(_itemSelectedId);
            RefreshItemList();
            RefreshItemDetail();
        }
    }
}
#endif
