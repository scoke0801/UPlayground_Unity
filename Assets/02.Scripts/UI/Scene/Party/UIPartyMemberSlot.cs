using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;

namespace UPlayGround.UI
{
    /// <summary>
    /// 파티원 선택 화면의 슬롯.
    /// 출전 슬롯(BattleOrder) / 후보 슬롯(Roster - BattleOrder) / 빈 슬롯 표시 모두 지원.
    /// </summary>
    public class UIPartyMemberSlot : MonoBehaviour, IPointerEnterHandler
    {
        public enum SlotKind
        {
            Battle,     // 출전 슬롯 — 캐릭터 있음
            Empty,      // 출전 슬롯 — 비어있음 (편성 모드 전용)
            Candidate,  // 로스터 슬롯 — Roster
        }

        [SerializeField] private Button _button;
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _activeMark;
        [SerializeField] private Image _focusedMark;
        [SerializeField] private Image _deadOverlay;
        [SerializeField] private GameObject _emptyOverlay;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _stateText;
        [SerializeField] private TextMeshProUGUI _orderText;

        private UI_Scene_PartySelect _parent;
        private int _index;
        private SlotKind _kind;
        private CharacterActorType _characterType;

        public CharacterActorType CharacterType => _characterType;
        public SlotKind Kind => _kind;

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

        public void InitBattle(UI_Scene_PartySelect parent, int index, CharacterActorType characterType,
            float currentHp, float maxHp, bool isActive, bool canSelect)
        {
            _parent = parent;
            _index = index;
            _kind = SlotKind.Battle;
            _characterType = characterType;

            bool isDead = currentHp <= 0f;
            float hpRatio = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;

            SetEmptyOverlay(false);

            if (_hpFill != null)         _hpFill.fillAmount = hpRatio;
            if (_activeMark != null)     _activeMark.gameObject.SetActive(isActive);
            if (_deadOverlay != null)    _deadOverlay.gameObject.SetActive(isDead);
            if (_nameText != null)       _nameText.text = characterType.ToString();
            if (_hpText != null)         _hpText.text = $"{Mathf.CeilToInt(currentHp)}/{Mathf.CeilToInt(maxHp)}";
            if (_stateText != null)      _stateText.text = isDead ? "전투 불능" : (isActive ? "출전 중" : string.Empty);
            if (_orderText != null)      _orderText.text = $"{index + 1}P";
            if (_button != null)         _button.interactable = canSelect;
        }

        public void InitCandidate(UI_Scene_PartySelect parent, int index, CharacterActorType characterType,
            float currentHp, float maxHp, bool canSelect)
        {
            InitRoster(parent, index, characterType, currentHp, maxHp, false, canSelect);
        }

        public void InitRoster(UI_Scene_PartySelect parent, int index, CharacterActorType characterType,
            float currentHp, float maxHp, bool inBattle, bool canSelect)
        {
            _parent = parent;
            _index = index;
            _kind = SlotKind.Candidate;
            _characterType = characterType;

            bool isDead = currentHp <= 0f;
            float hpRatio = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;

            SetEmptyOverlay(false);

            if (_hpFill != null)         _hpFill.fillAmount = hpRatio;
            if (_activeMark != null)     _activeMark.gameObject.SetActive(inBattle);
            if (_deadOverlay != null)    _deadOverlay.gameObject.SetActive(isDead);
            if (_nameText != null)       _nameText.text = characterType.ToString();
            if (_hpText != null)         _hpText.text = $"{Mathf.CeilToInt(currentHp)}/{Mathf.CeilToInt(maxHp)}";
            if (_stateText != null)      _stateText.text = isDead ? "전투 불능" : (inBattle ? "출전 중" : "로스터");
            if (_orderText != null)      _orderText.text = string.Empty;
            if (_button != null)         _button.interactable = canSelect;
        }

        public void InitEmpty(UI_Scene_PartySelect parent, int index, bool canSelect)
        {
            _parent = parent;
            _index = index;
            _kind = SlotKind.Empty;
            _characterType = CharacterActorType.None;

            SetEmptyOverlay(true);

            if (_hpFill != null)         _hpFill.fillAmount = 0f;
            if (_activeMark != null)     _activeMark.gameObject.SetActive(false);
            if (_deadOverlay != null)    _deadOverlay.gameObject.SetActive(false);
            if (_nameText != null)       _nameText.text = string.Empty;
            if (_hpText != null)         _hpText.text = string.Empty;
            if (_stateText != null)      _stateText.text = "빈 슬롯";
            if (_orderText != null)      _orderText.text = $"{index + 1}P";
            if (_button != null)         _button.interactable = canSelect;
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
            if (_kind == SlotKind.Battle)
            {
                _parent?.PreviewMember(_index);
            }
            else if (_kind == SlotKind.Candidate)
            {
                _parent?.PreviewCandidate(_index);
            }
        }

        private void OnClicked()
        {
            switch (_kind)
            {
                case SlotKind.Battle: _parent?.OnBattleSlotClicked(_index); break;
                case SlotKind.Empty:  _parent?.OnBattleSlotClicked(_index); break;
                case SlotKind.Candidate: _parent?.OnCandidateClicked(_index); break;
            }
        }

        private void SetEmptyOverlay(bool show)
        {
            if (_emptyOverlay != null) _emptyOverlay.SetActive(show);
        }
    }
}
