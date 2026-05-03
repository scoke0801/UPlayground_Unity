using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

/// <summary>
/// 파티 편성 화면의 보유 캐릭터 목록 엔트리.
/// 클릭 시 BattleOrder 편입/제외를 토글한다.
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

    [SerializeField] private Image _weaponIcon;
    private CharacterActorType _type = CharacterActorType.None;

    private void Awake()
    {
        _dimmedImage.SetActive(true);
        _partyOrderRoot.SetActive(false);
        _selectedImage.SetActive(false);

        _button.onClick.AddListener(OnClickedButton);
    }

    /// <summary>한 번만 호출 — 아이콘, 잠금 상태 세팅.</summary>
    public bool Init(CharacterActorType type)
    {
        _type = type;

        var pm   = PartyManager.Instance;
        var data = pm?.PartyMemberDataSO;
        if (data == null) return false;

        _characterIcon.sprite = data.GetHeadSprite(type);

        if (_weaponIcon != null)        _weaponIcon.sprite      = data.GetWeaponIcon(type);
        if (_characterNameText != null) _characterNameText.text = data.GetName(type);

        bool isUnlocked = pm.Roster.Contains(type);
        _dimmedImage.SetActive(!isUnlocked);

        RefreshBattleStatus();
        return true;
    }

    /// <summary>BattleOrder 변경 시 뱃지·선택 상태를 갱신한다.</summary>
    public void RefreshBattleStatus()
    {
        var pm = PartyManager.Instance;
        if (pm == null) return;

        var battleOrder = pm.BattleOrder;
        int battleIndex = -1;
        for (int i = 0; i < battleOrder.Count; i++)
        {
            if (battleOrder[i] == _type) { battleIndex = i; break; }
        }

        bool isInBattle = battleIndex >= 0;
        _partyOrderRoot.SetActive(isInBattle);
        if (isInBattle)
            _partyOrderText.text = (battleIndex + 1).ToString();

        _selectedImage.SetActive(isInBattle);
    }

    private void OnClickedButton()
    {
        var pm = PartyManager.Instance;
        if (pm == null) return;

        // 미보유(잠금) 캐릭터는 클릭 무시
        if (!pm.Roster.Contains(_type)) return;

        if (pm.BattleOrder.Contains(_type))
            pm.RemoveFromBattle(_type);
        else
            pm.AddToBattle(_type);

        // OnBattleOrderChanged → UI_PartyMenu.Refresh() → RefreshBattleStatus()
    }
}
