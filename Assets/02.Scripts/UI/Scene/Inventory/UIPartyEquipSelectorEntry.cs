using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;

namespace UPlayGround.UI
{
    /// <summary>
    /// 인벤토리 장비 화면의 파티원 선택 버튼. 초상/이름을 표시하고,
    /// 클릭 시 해당 캐릭터를 장비 편집 대상으로 선택하도록 부모에 알린다.
    /// </summary>
    public class UIPartyEquipSelectorEntry : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _portrait;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _indexText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private GameObject _activeBadge;
        [SerializeField] private GameObject _lockedOverlay;
        [SerializeField] private GameObject _selectedHighlight;

        private CharacterActorType _type = CharacterActorType.None;
        private Action<CharacterActorType> _onClick;

        public CharacterActorType Type => _type;

        public void Bind(CharacterActorType type, Sprite portrait, string displayName, Action<CharacterActorType> onClick)
        {
            _type = type;
            _onClick = onClick;

            if (_portrait != null)
            {
                _portrait.sprite  = portrait;
                _portrait.enabled = portrait != null;
            }
            if (_nameText != null)
                _nameText.text = displayName;

            SetSelected(false);
            SetLocked(false);
        }

        public void SetSelected(bool selected)
        {
            if (_selectedHighlight != null)
                _selectedHighlight.SetActive(selected);
        }

        public void SetMeta(int index, int level, bool isActive)
        {
            if (_indexText != null)
                _indexText.text = index.ToString();
            if (_levelText != null)
                _levelText.text = $"Lv.{Mathf.Max(1, level)}";
            if (_activeBadge != null)
                _activeBadge.SetActive(isActive);
        }

        public void SetLocked(bool locked)
        {
            if (_lockedOverlay != null)
                _lockedOverlay.SetActive(locked);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onClick?.Invoke(_type);
        }
    }
}
