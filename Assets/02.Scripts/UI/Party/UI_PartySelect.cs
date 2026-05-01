using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

/// <summary>
/// 파티원 선택 / 편성 화면.
/// - 스왑 모드(기본): 출전 슬롯 클릭 시 즉시 활성 캐릭터 교체.
/// - 편성 모드: 출전 슬롯 / 후보 슬롯을 조작해 BattleOrder 를 변경. 즉시 반영.
/// 자세한 규칙: docs/party-formation-system.md
/// </summary>
public class UI_PartySelect : UI_Base
{
    [Header("Slot")]
    [SerializeField] private UI_PartyMemberSlot _slotPrefab;
    [SerializeField] private Transform _slotRoot;

    [Header("Candidate")]
    [SerializeField] private UI_PartyMemberSlot _candidatePrefab;
    [SerializeField] private Transform _candidateRoot;
    [SerializeField] private GameObject _candidatePanel;

    [Header("Current")]
    [SerializeField] private RawImage _characterPreview;
    [SerializeField] private UICharacterPreviewRenderer _previewRenderer;
    [SerializeField] private TextMeshProUGUI _currentNameText;
    [SerializeField] private TextMeshProUGUI _currentHpText;
    [SerializeField] private Image _currentHpFill;

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI _battleSizeText;
    [SerializeField] private Toggle _formationToggle;

    [Header("Button")]
    [SerializeField] private Button _closeButton;

    [Header("Option")]
    [SerializeField] private bool _pauseGameOnShow = true;
    [SerializeField] private bool _hideAfterSelect = true;

    private readonly List<UI_PartyMemberSlot> _slots = new();
    private readonly List<UI_PartyMemberSlot> _candidateSlots = new();
    private int _previewBattleIndex = -1;
    private int _selectedBattleIndex = -1;
    private bool _formationMode = false;

    protected override void Awake()
    {
        base.Awake();

        _layer = CanvasLayer.Scene;

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Hide);
        }

        if (_formationToggle != null)
        {
            _formationToggle.onValueChanged.AddListener(SetFormationMode);
        }

        if (_previewRenderer != null && _characterPreview != null)
        {
            _characterPreview.texture = _previewRenderer.GetRenderTexture();
        }
    }

    protected override void OnShow()
    {
        base.OnShow();

        if (_pauseGameOnShow)
        {
            GameTimeManager.Instance?.SetPause(true);
        }

        InputManager.Instance?.SetInputLayer(_layer.ToInputLayer());

        var partyManager = PartyManager.Instance;
        if (partyManager != null)
        {
            partyManager.OnSwapCompleted += OnSwapCompleted;
            partyManager.OnCharacterUnlocked += OnCharacterUnlocked;
            partyManager.OnRosterChanged += OnRosterChanged;
            partyManager.OnBattleOrderChanged += OnBattleOrderChanged;
        }

        _selectedBattleIndex = -1;
        Refresh();
        PreviewMember(partyManager?.ActiveIndex ?? 0);
    }

    protected override void OnHide()
    {
        var partyManager = PartyManager.Instance;
        if (partyManager != null)
        {
            partyManager.OnSwapCompleted -= OnSwapCompleted;
            partyManager.OnCharacterUnlocked -= OnCharacterUnlocked;
            partyManager.OnRosterChanged -= OnRosterChanged;
            partyManager.OnBattleOrderChanged -= OnBattleOrderChanged;
        }

        if (_pauseGameOnShow)
        {
            GameTimeManager.Instance?.SetPause(false);
        }

        if (_previewRenderer != null)
        {
            _previewRenderer.HidePreview();
        }

        InputManager.Instance?.SetInputLayer(InputLayer.None);

        base.OnHide();
    }

    public override bool PerformBackFunction()
    {
        Hide();
        return false;
    }

    public void SetFormationMode(bool on)
    {
        _formationMode = on;
        _selectedBattleIndex = -1;

        if (_candidatePanel != null)
        {
            _candidatePanel.SetActive(on);
        }

        Refresh();
    }

    public void Refresh()
    {
        var partyManager = PartyManager.Instance;
        PlayerActor player = partyManager?.ActiveCharacter;

        if (partyManager == null || player == null)
        {
            SetCurrentInfo(CharacterActorType.None, 0f, 0f);
            SetSlotCount(0);
            SetCandidateCount(0);
            UpdateBattleSizeText(0, 0);
            return;
        }

        CharacterActorType activeType = partyManager.ActiveCharacterType;
        SetCurrentInfo(activeType, player.GetHealthForCharacter(activeType), player.GetMaxHealthForCharacter(activeType));

        IReadOnlyList<CharacterActorType> battleOrder = partyManager.BattleOrder;
        int maxBattle = partyManager.MaxBattleSize;
        int slotCount = _formationMode ? maxBattle : battleOrder.Count;

        SetSlotCount(slotCount);
        UpdateBattleSizeText(battleOrder.Count, maxBattle);

        bool canSwap = partyManager.CanSwap();

        for (int i = 0; i < slotCount; ++i)
        {
            if (i < battleOrder.Count)
            {
                CharacterActorType type = battleOrder[i];
                float maxHp = player.GetMaxHealthForCharacter(type);
                float currentHp = player.HasHealthRecordForCharacter(type)
                    ? player.GetHealthForCharacter(type)
                    : maxHp;
                bool isActive = (i == partyManager.ActiveIndex);

                bool canSelect = _formationMode
                    ? true                     // 편성 모드: 슬롯 선택용
                    : canSwap && !isActive && currentHp > 0f;

                _slots[i].InitBattle(this, i, type, currentHp, maxHp, isActive, canSelect);
            }
            else
            {
                _slots[i].InitEmpty(this, i, _formationMode);
            }

            _slots[i].SetFocused(_formationMode
                ? i == _selectedBattleIndex
                : i == _previewBattleIndex);
        }

        RefreshCandidates(player);
    }

    private void RefreshCandidates(PlayerActor player)
    {
        var partyManager = PartyManager.Instance;
        if (partyManager == null)
        {
            SetCandidateCount(0);
            return;
        }

        if (_candidatePanel != null)
        {
            _candidatePanel.SetActive(_formationMode);
        }

        if (!_formationMode || _candidatePrefab == null || _candidateRoot == null)
        {
            SetCandidateCount(0);
            return;
        }

        IReadOnlyList<CharacterActorType> roster = partyManager.Roster;
        IReadOnlyList<CharacterActorType> battleOrder = partyManager.BattleOrder;

        var candidateTypes = new List<CharacterActorType>(roster.Count);
        for (int i = 0; i < roster.Count; ++i)
        {
            if (!battleOrder.Contains(roster[i]))
            {
                candidateTypes.Add(roster[i]);
            }
        }

        SetCandidateCount(candidateTypes.Count);

        bool battleHasRoom = battleOrder.Count < partyManager.MaxBattleSize || _selectedBattleIndex >= 0;

        for (int i = 0; i < candidateTypes.Count; ++i)
        {
            CharacterActorType type = candidateTypes[i];
            float maxHp = player.GetMaxHealthForCharacter(type);
            float currentHp = player.HasHealthRecordForCharacter(type)
                ? player.GetHealthForCharacter(type)
                : maxHp;

            _candidateSlots[i].InitCandidate(this, i, type, currentHp, maxHp, battleHasRoom);
        }
    }

    public void PreviewMember(int index)
    {
        var partyManager = PartyManager.Instance;
        IReadOnlyList<CharacterActorType> battleOrder = partyManager?.BattleOrder;

        if (battleOrder == null || index < 0 || index >= battleOrder.Count)
        {
            return;
        }

        _previewBattleIndex = index;

        CharacterActorType type = battleOrder[index];
        ShowPreviewFor(type);

        if (!_formationMode)
        {
            for (int i = 0; i < _slots.Count; ++i)
            {
                _slots[i].SetFocused(i == _previewBattleIndex);
            }
        }
    }

    public void PreviewCandidate(int candidateIndex)
    {
        var partyManager = PartyManager.Instance;
        if (partyManager == null) return;

        var candidates = GetCurrentCandidates();
        if (candidateIndex < 0 || candidateIndex >= candidates.Count) return;

        ShowPreviewFor(candidates[candidateIndex]);
    }

    /// <summary>
    /// 출전 슬롯 클릭. 모드에 따라 분기.
    /// </summary>
    public void OnBattleSlotClicked(int slotIndex)
    {
        if (!_formationMode)
        {
            // 스왑 모드: 즉시 교체
            SelectMember(slotIndex);
            return;
        }

        // 편성 모드: 슬롯 선택 토글 (다시 클릭하면 해제)
        _selectedBattleIndex = (_selectedBattleIndex == slotIndex) ? -1 : slotIndex;
        Refresh();
    }

    /// <summary>
    /// 후보 슬롯 클릭. 편성 모드 전용.
    /// 선택된 출전 슬롯이 있으면 그 자리와 교체, 없으면 빈 슬롯에 추가.
    /// </summary>
    public void OnCandidateClicked(int candidateIndex)
    {
        if (!_formationMode) return;

        var partyManager = PartyManager.Instance;
        if (partyManager == null) return;

        var candidates = GetCurrentCandidates();
        if (candidateIndex < 0 || candidateIndex >= candidates.Count) return;

        CharacterActorType type = candidates[candidateIndex];

        if (_selectedBattleIndex >= 0)
        {
            partyManager.ReplaceBattleSlot(_selectedBattleIndex, type);
            _selectedBattleIndex = -1;
        }
        else
        {
            partyManager.AddToBattle(type);
        }
        // OnBattleOrderChanged 이벤트로 Refresh 됨
    }

    public void SelectMember(int index)
    {
        var partyManager = PartyManager.Instance;
        if (partyManager == null) return;

        if (partyManager.RequestSwapTo(index))
        {
            _previewBattleIndex = partyManager.ActiveIndex;

            if (_hideAfterSelect)
            {
                Hide();
            }
            else
            {
                Refresh();
            }
        }
        else
        {
            Refresh();
        }
    }

    private List<CharacterActorType> GetCurrentCandidates()
    {
        var partyManager = PartyManager.Instance;
        var result = new List<CharacterActorType>();
        if (partyManager == null) return result;

        var roster = partyManager.Roster;
        var battleOrder = partyManager.BattleOrder;
        for (int i = 0; i < roster.Count; ++i)
        {
            if (!battleOrder.Contains(roster[i])) result.Add(roster[i]);
        }
        return result;
    }

    private void ShowPreviewFor(CharacterActorType type)
    {
        var partyManager = PartyManager.Instance;
        PlayerActor player = partyManager?.ActiveCharacter;
        if (player == null) return;

        float maxHp = player.GetMaxHealthForCharacter(type);
        float currentHp = player.HasHealthRecordForCharacter(type)
            ? player.GetHealthForCharacter(type)
            : maxHp;
        SetCurrentInfo(type, currentHp, maxHp);

        if (_previewRenderer != null)
        {
            _previewRenderer.ShowPreview(type);
        }
    }

    private void SetCurrentInfo(CharacterActorType characterType, float currentHp, float maxHp)
    {
        if (_currentNameText != null)
        {
            _currentNameText.text = characterType == CharacterActorType.None ? string.Empty : characterType.ToString();
        }

        if (_currentHpText != null)
        {
            _currentHpText.text = maxHp > 0f
                ? $"{Mathf.CeilToInt(currentHp)}/{Mathf.CeilToInt(maxHp)}"
                : string.Empty;
        }

        if (_currentHpFill != null)
        {
            _currentHpFill.fillAmount = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
        }
    }

    private void UpdateBattleSizeText(int battleCount, int maxBattle)
    {
        if (_battleSizeText != null)
        {
            _battleSizeText.text = $"{battleCount} / {maxBattle}";
        }
    }

    private void SetSlotCount(int count)
    {
        if (_slotPrefab == null || _slotRoot == null) return;

        while (_slots.Count < count)
        {
            UI_PartyMemberSlot slot = Instantiate(_slotPrefab, _slotRoot);
            _slots.Add(slot);
        }

        for (int i = 0; i < _slots.Count; ++i)
        {
            _slots[i].gameObject.SetActive(i < count);
        }
    }

    private void SetCandidateCount(int count)
    {
        Transform root = _candidateRoot;
        UI_PartyMemberSlot prefab = _candidatePrefab != null ? _candidatePrefab : _slotPrefab;
        if (prefab == null || root == null) return;

        while (_candidateSlots.Count < count)
        {
            UI_PartyMemberSlot slot = Instantiate(prefab, root);
            _candidateSlots.Add(slot);
        }

        for (int i = 0; i < _candidateSlots.Count; ++i)
        {
            _candidateSlots[i].gameObject.SetActive(i < count);
        }
    }

    private void OnSwapCompleted(PlayerActor player)         => Refresh();
    private void OnCharacterUnlocked(CharacterActorType t)   => Refresh();
    private void OnRosterChanged()                           => Refresh();
    private void OnBattleOrderChanged()                      => Refresh();
}
