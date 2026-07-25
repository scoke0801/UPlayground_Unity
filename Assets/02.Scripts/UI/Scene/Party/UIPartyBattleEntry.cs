using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 파티 편성 화면의 전투 슬롯 엔트리 — BattleOrder에 배치된 캐릭터만 표시.
    /// </summary>
    public class UIPartyBattleEntry : MonoBehaviour, ISelectHandler
    {
        [SerializeField] private Image              _characterIcon;
        [SerializeField] private TextMeshProUGUI    _characterNameText;
        [SerializeField] private TextMeshProUGUI    _characterLevelText;
        [SerializeField] private GameObject         _partyOrderRoot;
        [SerializeField] private TextMeshProUGUI    _partyOrderText;

        [SerializeField] private Image      _weaponIcon;
        [SerializeField] private GameObject _selectedImage;
        [SerializeField] private Button     _slotButton;

        [Header("HP")]
        [SerializeField] private Image           _hpFill;
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private GameObject      _deadText;   // "전투 불능"

        [Header("상태")]
        [SerializeField] private GameObject _contentRoot;   // 편성된 캐릭터 내용
        [SerializeField] private GameObject _emptyRoot;     // 빈 슬롯 플레이스홀더

        /// <summary> 슬롯 클릭 시 해당 캐릭터를 상세 표시 대상으로 선택 요청. </summary>
        public event Action<CharacterActorType> OnSelectRequested;

        private CharacterActorType _boundType = CharacterActorType.None;

        public CharacterActorType BoundType => _boundType;
        public Selectable Selectable => _slotButton;

        private void Awake()
        {
            _slotButton?.onClick.AddListener(OnSlotButtonClicked);
        }

        public void Bind(CharacterActorType type, PartyMemberDataSO memberData, int slotIndex, bool canRemove)
        {
            _boundType = type;

            if (_characterIcon != null && memberData != null)
                _characterIcon.sprite = memberData.GetFullBodySprite(type);

            if (_characterNameText != null && memberData != null)
                _characterNameText.text = memberData.GetName(type);

            RefreshLevelText();

            if (_weaponIcon != null && memberData != null)
                _weaponIcon.sprite = memberData.GetWeaponIcon(type);

            if (_partyOrderRoot != null) _partyOrderRoot.SetActive(true);
            if (_partyOrderText != null) _partyOrderText.text = (slotIndex + 1).ToString();

            bool isActive = UISvc.Party != null &&
                            UISvc.Party.ActiveCharacterType == type;
            if (_selectedImage != null) _selectedImage.SetActive(isActive);

            RefreshHp();

            if (_slotButton != null) _slotButton.interactable = true; // 클릭=선택(항상 가능)

            if (_contentRoot != null) _contentRoot.SetActive(true);
            if (_emptyRoot != null)   _emptyRoot.SetActive(false);

            gameObject.SetActive(true);
        }

        private void RefreshHp()
        {
            var player = UISvc.Actors?.Player;
            float cur = player != null ? player.GetHealthForCharacter(_boundType)    : 0f;
            float max = player != null ? player.GetMaxHealthForCharacter(_boundType) : 0f;

            if (_hpFill != null) _hpFill.fillAmount = max > 0f ? Mathf.Clamp01(cur / max) : 0f;
            if (_hpText != null) _hpText.text = $"{Mathf.RoundToInt(cur)} / {Mathf.RoundToInt(max)}";
            if (_deadText != null) _deadText.SetActive(cur <= 0f);
        }

        public void Unbind()
        {
            _boundType = CharacterActorType.None;

            // 슬롯 자체는 활성 상태로 두어 레이아웃 폭을 유지하고(편성 1명일 때 늘어짐 방지),
            // 캐릭터 내용 대신 "빈 슬롯" 플레이스홀더를 표시한다.
            if (_contentRoot != null) _contentRoot.SetActive(false);
            if (_emptyRoot != null)   _emptyRoot.SetActive(true);
            gameObject.SetActive(true);
        }

        private void RefreshLevelText()
        {
            if (_characterLevelText == null) return;

            int level = UISvc.Party?.GetLevel(_boundType) ?? 1;
            _characterLevelText.text = $"Lv. {Mathf.Max(1, level)}";
        }

        private void OnSlotButtonClicked()
        {
            if (_boundType != CharacterActorType.None)
                OnSelectRequested?.Invoke(_boundType);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (_boundType != CharacterActorType.None)
                OnSelectRequested?.Invoke(_boundType);
        }
    }
}
