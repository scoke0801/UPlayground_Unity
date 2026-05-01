using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;

/// <summary>
/// 파티원 선택 화면의 캐릭터 슬롯.
/// </summary>
public class UI_PartyMemberSlot : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _hpFill;
    [SerializeField] private Image _activeMark;
    [SerializeField] private Image _focusedMark;
    [SerializeField] private Image _deadOverlay;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _hpText;
    [SerializeField] private TextMeshProUGUI _stateText;

    private UI_PartySelect _parent;
    private int _index;

    private void Awake()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        if (_button != null)
        {
            _button.onClick.AddListener(OnClicked);
        }
    }

    public void Init(UI_PartySelect parent, int index, CharacterActorType characterType,
        float currentHp, float maxHp, bool isActive, bool canSelect)
    {
        _parent = parent;
        _index = index;

        bool isDead = currentHp <= 0f;
        float hpRatio = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;

        if (_hpFill != null)
        {
            _hpFill.fillAmount = hpRatio;
        }

        if (_activeMark != null)
        {
            _activeMark.gameObject.SetActive(isActive);
        }

        if (_deadOverlay != null)
        {
            _deadOverlay.gameObject.SetActive(isDead);
        }

        if (_nameText != null)
        {
            _nameText.text = characterType.ToString();
        }

        if (_hpText != null)
        {
            _hpText.text = $"{Mathf.CeilToInt(currentHp)}/{Mathf.CeilToInt(maxHp)}";
        }

        if (_stateText != null)
        {
            _stateText.text = isDead ? "전투 불능" : (isActive ? "출전 중" : string.Empty);
        }

        if (_button != null)
        {
            _button.interactable = canSelect && !isActive && !isDead;
        }
    }

    public void SetFocused(bool isFocused)
    {
        if (_focusedMark != null)
        {
            _focusedMark.gameObject.SetActive(isFocused);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _parent?.PreviewMember(_index);
    }

    private void OnClicked()
    {
        _parent?.SelectMember(_index);
    }
}
