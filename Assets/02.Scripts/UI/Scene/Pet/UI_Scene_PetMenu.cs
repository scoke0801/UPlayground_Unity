using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UPlayGround.Data.Pet;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>파티 소유 펫의 동행 여부와 해금 외형을 선택한다.</summary>
    [RequireComponent(typeof(GraphicRaycaster))]
    public sealed partial class UI_Scene_PetMenu : UI_SceneBase
    {
        public const string UIKey = "PetMenu";
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _bond;
        [SerializeField] private TextMeshProUGUI _status;
        [SerializeField] private Button _toggle;
        [SerializeField] private Button _close;
        [SerializeField] private Button _appearanceTemplate;
        [SerializeField] private Transform _appearanceRoot;
        private readonly List<Button> _appearanceButtons = new();
        private string _displayedPetId;
        private IPetService _pets;
        private IUIInventoryService _inventory;
        [SerializeField] private UITabGroup _tabs;
        [SerializeField] private GameObject _modelPage;
        [SerializeField] private GameObject _skillPage;
        [SerializeField] private UICharacterPreviewRenderer _preview;
        [SerializeField] private Button _model;
        [SerializeField] private Button _apply;
        [SerializeField] private Button _skillTemplate;
        [SerializeField] private Transform _skillRoot;
        [SerializeField] private TextMeshProUGUI _skillDetail;
        [SerializeField] private Button _selectSkill;
        [SerializeField] private Button _upgrade;
        [SerializeField] private Button _retrySave;
        private readonly List<Button> _skillButtons = new();
        private string _selectedSkillId = PetSkillCatalogSO.Heal;
        private string _previewAppearanceId;
        private bool _inDetail;
        private int _shownLevel;
        private int _shownCost;
        private int _shownRevision;
        private string _feedback;
        private GridLayoutGroup _appearanceLayout;
        protected override bool BlocksLowerInput => true;

        protected override void OnInit()
        {
            base.OnInit();
            _appearanceLayout = _appearanceRoot.GetComponent<GridLayoutGroup>();
            _toggle.onClick.AddListener(ToggleAccompanying);
            _close.onClick.AddListener(Hide);
            _appearanceTemplate.gameObject.SetActive(false);
            InitializeV2();
        }
        protected override void OnShow()
        {
            base.OnShow();
            Services.TryGet(out _pets);
            Services.TryGet(out _inventory);
            if (_pets != null) _pets.Changed += Refresh;
            if (_inventory != null) _inventory.OnGoldChanged += Refresh;
            Refresh();
            var saved = _pets?.GetSnapshot();
            if (saved != null) _selectedSkillId = saved.SelectedSupportId;
            _previewAppearanceId = null;
            RefreshSkills();
            if (_tabs != null) _tabs.Select(_tabs.SelectedIndex < 0 ? 0 : _tabs.SelectedIndex);
            if (_tabs == null) RefreshPreview();
            FocusCurrentItem();
        }
        protected override void OnHide()
        {
            StopSkillFlow();
            if (_pets != null) _pets.Changed -= Refresh;
            if (_inventory != null) _inventory.OnGoldChanged -= Refresh;
            _preview?.HidePreview();
            base.OnHide();
        }
        protected override void OnDispose()
        {
            StopSkillFlow();
            if (_runtimeSkillFlow != null) Destroy(_runtimeSkillFlow);
            base.OnDispose();
        }
        public override bool PerformBackFunction()
        {
            if (_inDetail) { _inDetail = false; FocusCurrentItem(); return false; }
            Hide(); return false;
        }
        private void Refresh()
        {
            PetSaveData save = _pets?.GetSnapshot();
            PetOwnedEntry owned = null;
            if (save != null)
                foreach (var candidate in save.owned)
                    if (candidate.petId == save.selectedPetId) { owned = candidate; break; }
            if (owned == null && save?.owned.Count > 0) owned = save.owned[0];
            PetDefinitionSO definition = _pets?.Catalog?.Find(owned?.petId);
            _name.text = definition?.actor != null ? definition.actor.displayName : "아직 동행하는 친구가 없어요";
            _bond.text = owned != null ? $"외형 {definition?.appearances.Count ?? 0}종    /    {_inventory?.Gold ?? 0:N0} G" : string.Empty;
            _status.text = _pets?.State == PetRuntimeState.Failed ? "지금은 함께할 수 없어요. 잠시 후 다시 불러 주세요." : string.Empty;
            _toggle.interactable = definition != null;
            _toggle.GetComponentInChildren<TextMeshProUGUI>().text = save?.isAccompanying == true ? "쉬게 하기" : "함께 다니기";
            if (definition == null || owned == null) return;
            if (_displayedPetId != definition.petId)
            {
                foreach (var button in _appearanceButtons) Destroy(button.gameObject);
                _appearanceButtons.Clear(); _displayedPetId = definition.petId;
            }
            int visibleIndex = 0;
            for (int i = 0; i < definition.appearances.Count; i++)
            {
                var appearance = definition.appearances[i];
                if (appearance == null || !owned.unlockedAppearances.Contains(appearance.appearanceId)) continue;
                string id = appearance.appearanceId;
                Button button;
                if (visibleIndex < _appearanceButtons.Count) button = _appearanceButtons[visibleIndex];
                else
                {
                    button = Instantiate(_appearanceTemplate, _appearanceRoot);
                    button.onClick.AddListener(() =>
                    {
                        _previewAppearanceId = id; RefreshPreview();
                        _inDetail = true;
                        if (_apply.interactable) SetDefaultFocus(_apply, true);
                    });
                    _appearanceButtons.Add(button);
                }
                button.gameObject.SetActive(true);
                button.GetComponentInChildren<TextMeshProUGUI>().text = $"모습 {i + 1:00}";
                var portrait = button.transform.Find("Portrait")?.GetComponent<Image>();
                if (portrait != null) portrait.sprite = appearance.portrait;
                var equipped = button.transform.Find("Equipped");
                if (equipped != null) equipped.gameObject.SetActive(owned.appearanceId == id);
                visibleIndex++;
            }
            RefreshAppearanceLayout();
            RefreshSkills();
            if (_pets.CanEdit(out string reason)) _status.text = _feedback ?? string.Empty;
            else _status.text = reason;
            if (_pets.HasUnsavedChanges) _status.text += "  아직 저장되지 않았어요.";
            if (_retrySave != null) _retrySave.gameObject.SetActive(_pets.HasUnsavedChanges);
            if (_apply != null) _apply.interactable = _pets.CanEdit(out _);
            ConfigureFocusNavigation();
        }
        private void ToggleAccompanying()
        {
            if (_pets == null) return;
            var save = _pets.GetSnapshot();
            if (save.isAccompanying) _pets.SetAccompanying(false);
            else if (!string.IsNullOrEmpty(save.selectedPetId)) _pets.TrySelect(save.selectedPetId);
            else if (save.owned.Count > 0) _pets.TrySelect(save.owned[0].petId);
            SetDefaultFocus(_toggle, true);
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshAppearanceLayout();
        }

        private void RefreshAppearanceLayout()
        {
            if (_appearanceLayout == null || _appearanceButtons.Count == 0) return;
            var rect = (RectTransform)_appearanceRoot;
            int columns = _appearanceLayout.constraintCount;
            int rows = Mathf.CeilToInt((float)_appearanceButtons.Count / columns);
            float width = (rect.rect.width - _appearanceLayout.padding.horizontal - _appearanceLayout.spacing.x * (columns - 1)) / columns;
            float height = (rect.rect.height - _appearanceLayout.padding.vertical - _appearanceLayout.spacing.y * (rows - 1)) / rows;
            _appearanceLayout.cellSize = new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, height));
        }
    }
}
