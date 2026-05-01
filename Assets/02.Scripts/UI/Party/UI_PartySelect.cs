using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

/// <summary>
/// 파티원 선택 화면.
/// PartyManager의 현재 파티 순서를 보여주고 선택한 인덱스로 즉시 교체한다.
/// </summary>
public class UI_PartySelect : UI_Base
{
    [Header("Slot")]
    [SerializeField] private UI_PartyMemberSlot _slotPrefab;
    [SerializeField] private Transform _slotRoot;

    [Header("Current")]
    [SerializeField] private RawImage _characterPreview;
    [SerializeField] private UICharacterPreviewRenderer _previewRenderer;
    [SerializeField] private TextMeshProUGUI _currentNameText;
    [SerializeField] private TextMeshProUGUI _currentHpText;
    [SerializeField] private Image _currentHpFill;

    [Header("Button")]
    [SerializeField] private Button _closeButton;

    [Header("Option")]
    [SerializeField] private bool _pauseGameOnShow = true;
    [SerializeField] private bool _hideAfterSelect = true;

    private readonly List<UI_PartyMemberSlot> _slots = new();
    private int _previewIndex = -1;

    protected override void Awake()
    {
        base.Awake();

        _layer = CanvasLayer.Scene;

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Hide);
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

        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.OnSwapCompleted += OnSwapCompleted;
            PartyManager.Instance.OnCharacterUnlocked += OnCharacterUnlocked;
        }

        Refresh();
        PreviewMember(PartyManager.Instance?.ActiveIndex ?? 0);
    }

    protected override void OnHide()
    {
        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.OnSwapCompleted -= OnSwapCompleted;
            PartyManager.Instance.OnCharacterUnlocked -= OnCharacterUnlocked;
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

    public void Refresh()
    {
        PartyManager partyManager = PartyManager.Instance;
        PlayerActor player = partyManager?.ActiveCharacter;

        if (partyManager == null || player == null)
        {
            SetCurrentInfo(CharacterActorType.None, 0f, 0f);
            SetSlotCount(0);
            return;
        }

        CharacterActorType activeType = partyManager.ActiveCharacterType;
        float activeHp = player.GetHealthForCharacter(activeType);
        float activeMaxHp = player.GetMaxHealthForCharacter(activeType);
        SetCurrentInfo(activeType, activeHp, activeMaxHp);

        IReadOnlyList<CharacterActorType> partyOrder = partyManager.PartyOrder;
        SetSlotCount(partyOrder.Count);

        bool canSwap = partyManager.CanSwap();

        for (int i = 0; i < partyOrder.Count; ++i)
        {
            CharacterActorType type = partyOrder[i];

            float maxHp = player.GetMaxHealthForCharacter(type);
            float currentHp = player.HasHealthRecordForCharacter(type)
                ? player.GetHealthForCharacter(type)
                : maxHp;
            bool isActive = i == partyManager.ActiveIndex;

            _slots[i].Init(this, i, type, currentHp, maxHp, isActive, canSwap);
            _slots[i].SetFocused(i == _previewIndex);
        }
    }

    public void PreviewMember(int index)
    {
        PartyManager partyManager = PartyManager.Instance;
        IReadOnlyList<CharacterActorType> partyOrder = partyManager?.PartyOrder;

        if (partyOrder == null || index < 0 || index >= partyOrder.Count)
        {
            return;
        }

        _previewIndex = index;

        CharacterActorType type = partyOrder[index];
        PlayerActor player = partyManager.ActiveCharacter;
        float maxHp = player.GetMaxHealthForCharacter(type);
        float currentHp = player.HasHealthRecordForCharacter(type)
            ? player.GetHealthForCharacter(type)
            : maxHp;
        SetCurrentInfo(type, currentHp, maxHp);

        if (_previewRenderer != null)
        {
            _previewRenderer.ShowPreview(type);
        }

        for (int i = 0; i < _slots.Count; ++i)
        {
            _slots[i].SetFocused(i == _previewIndex);
        }
    }

    public void SelectMember(int index)
    {
        PartyManager partyManager = PartyManager.Instance;
        if (partyManager == null)
        {
            return;
        }

        if (partyManager.RequestSwapTo(index))
        {
            _previewIndex = partyManager.ActiveIndex;

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

    private void SetSlotCount(int count)
    {
        if (_slotPrefab == null || _slotRoot == null)
        {
            return;
        }

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

    private void OnSwapCompleted(PlayerActor player)
    {
        Refresh();
    }

    private void OnCharacterUnlocked(CharacterActorType type)
    {
        Refresh();
    }
}
