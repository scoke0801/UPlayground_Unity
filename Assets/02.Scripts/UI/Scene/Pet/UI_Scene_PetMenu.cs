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
    public sealed class UI_Scene_PetMenu : UI_SceneBase
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
        protected override bool BlocksLowerInput => true;

        protected override void OnInit()
        {
            base.OnInit();
            _toggle.onClick.AddListener(ToggleAccompanying);
            _close.onClick.AddListener(Hide);
            _appearanceTemplate.gameObject.SetActive(false);
        }
        protected override void OnShow()
        {
            base.OnShow();
            Services.TryGet(out _pets);
            if (_pets != null) _pets.Changed += Refresh;
            Refresh();
            SetDefaultFocus(_toggle.interactable ? _toggle : _close, true);
        }
        protected override void OnHide()
        {
            if (_pets != null) _pets.Changed -= Refresh;
            base.OnHide();
        }
        public override bool PerformBackFunction()
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(_appearanceRoot))
            { SetDefaultFocus(_toggle, true); return false; }
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
            _bond.text = owned != null ? $"친밀도 {owned.bondPoints}" : string.Empty;
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
                    button.onClick.AddListener(() => _pets.TrySetAppearance(id));
                    _appearanceButtons.Add(button);
                }
                button.gameObject.SetActive(true);
                button.GetComponentInChildren<TextMeshProUGUI>().text = (owned.appearanceId == id ? "● " : "") + $"모습 {i + 1}";
                visibleIndex++;
            }
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
    }
}
