using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Manager;

/// <summary>
/// 파티 편성 화면의 전투 슬롯 엔트리 — BattleOrder에 배치된 캐릭터만 표시.
/// </summary>
public class UIPartyBattleEntry : MonoBehaviour
{
    [SerializeField] private Image              _characterIcon;
    [SerializeField] private TextMeshProUGUI    _characterNameText;
    [SerializeField] private TextMeshProUGUI    _characterLevelText;
    [SerializeField] private GameObject         _partyOrderRoot;
    [SerializeField] private TextMeshProUGUI    _partyOrderText;

    [SerializeField] private Image      _weaponIcon;
    [SerializeField] private GameObject _selectedImage;
    [SerializeField] private Button     _slotButton;

    public event Action<CharacterActorType> OnRemoveRequested;

    private CharacterActorType _boundType = CharacterActorType.None;

    public CharacterActorType BoundType => _boundType;

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

        bool isActive = PartyManager.Instance != null &&
                        PartyManager.Instance.ActiveCharacterType == type;
        if (_selectedImage != null) _selectedImage.SetActive(isActive);

        if (_slotButton != null) _slotButton.interactable = canRemove;

        gameObject.SetActive(true);
    }

    public void Unbind()
    {
        _boundType = CharacterActorType.None;
        gameObject.SetActive(false);
    }

    private void RefreshLevelText()
    {
        if (_characterLevelText == null) return;

        int level = PartyManager.Instance?.GetLevel(_boundType) ?? 1;
        _characterLevelText.text = $"Lv. {Mathf.Max(1, level)}";
    }

    private void OnSlotButtonClicked()
    {
        if (_boundType != CharacterActorType.None)
            OnRemoveRequested?.Invoke(_boundType);
    }
}
