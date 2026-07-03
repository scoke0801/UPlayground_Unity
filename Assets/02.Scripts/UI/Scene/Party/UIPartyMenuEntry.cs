using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

/// <summary>
/// 파티 편성 화면의 보유 캐릭터 목록 엔트리.
/// 클릭 시 OnToggleRequested 이벤트를 발행하고, UI_PartyMenu가 초안 상태를 관리한다.
/// </summary>
public class UIPartyMenuEntry : MonoBehaviour
{
    [SerializeField] private Image              _characterIcon;
    [SerializeField] private TextMeshProUGUI    _characterNameText;
    [SerializeField] private TextMeshProUGUI    _characterLevelText;
    [SerializeField] private GameObject         _dimmedImage;
    [SerializeField] private Button             _button;
    [SerializeField] private GameObject         _partyOrderRoot;
    [SerializeField] private TextMeshProUGUI    _partyOrderText;
    [SerializeField] private GameObject         _selectedImage;
    [SerializeField] private Image              _weaponIcon;

    public event Action<CharacterActorType> OnToggleRequested;

    private CharacterActorType _type = CharacterActorType.None;

    public CharacterActorType Type => _type;

    private void Awake()
    {
        _dimmedImage.SetActive(true);
        _partyOrderRoot.SetActive(false);
        _selectedImage.SetActive(false);

        _button.onClick.AddListener(OnClickedButton);
    }

    /// <summary>한 번만 호출 — 아이콘, 이름, 잠금 상태 세팅.</summary>
    public bool Init(CharacterActorType type)
    {
        _type = type;

        var pm   = PartyManager.Instance;
        var data = pm?.PartyMemberDataSO;
        if (data == null) return false;

        _characterIcon.sprite = data.GetHeadSprite(type);

        if (_weaponIcon != null)        _weaponIcon.sprite      = data.GetWeaponIcon(type);
        if (_characterNameText != null) _characterNameText.text = data.GetName(type);
        RefreshLevelText(pm.Roster.Contains(type));

        bool isUnlocked = pm.Roster.Contains(type);
        _dimmedImage.SetActive(!isUnlocked);

        return true;
    }

    /// <summary>초안 BattleOrder와 상세 선택 대상을 받아 뱃지·선택 상태를 갱신한다.</summary>
    public void RefreshBattleStatus(IReadOnlyList<CharacterActorType> pendingOrder, CharacterActorType selectedType)
    {
        var pm = PartyManager.Instance;
        bool isUnlocked = pm != null && pm.Roster.Contains(_type);
        RefreshLevelText(isUnlocked);
        _dimmedImage.SetActive(!isUnlocked);

        int battleIndex = -1;
        for (int i = 0; i < pendingOrder.Count; i++)
        {
            if (pendingOrder[i] == _type) { battleIndex = i; break; }
        }

        bool isInBattle = battleIndex >= 0;
        _partyOrderRoot.SetActive(isInBattle);
        if (isInBattle)
            _partyOrderText.text = (battleIndex + 1).ToString();

        _selectedImage.SetActive(selectedType == _type);
    }

    private void RefreshLevelText(bool isUnlocked)
    {
        if (_characterLevelText == null) return;

        if (!isUnlocked)
        {
            _characterLevelText.text = string.Empty;
            return;
        }

        int level = PartyManager.Instance?.GetLevel(_type) ?? 1;
        _characterLevelText.text = $"Lv. {Mathf.Max(1, level)}";
    }

    private void OnClickedButton()
    {
        // 미보유(잠금) 캐릭터는 클릭 무시
        var pm = PartyManager.Instance;
        if (pm == null || !pm.Roster.Contains(_type)) return;

        OnToggleRequested?.Invoke(_type);
    }
}
