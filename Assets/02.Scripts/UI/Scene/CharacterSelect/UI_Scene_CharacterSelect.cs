using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 신규 게임의 조작 캐릭터이자 서사 주인공을 선택하는 화면.
    /// 카드 선택은 프리뷰·상세 정보를 갱신하며, 확정 결과는 타이틀의 새 게임 시작 흐름으로 전달한다.
    /// </summary>
    public class UI_Scene_CharacterSelect : UI_SceneBase
    {
        /// <summary>
        /// UIManager 등록 키. UIKeyType은 자동 생성 파일이라 항목이 없을 수 있으므로,
        /// 문자열 상수로 두어 ShowUI(string) 오버로드로 호출한다. (UIPrefabDatabase 등록 키와 동일)
        /// </summary>
        public const string UIKey = "CharacterSelect";

        [Header("Data")]
        [SerializeField] private CharacterSelectDatabaseSO _database;
        [SerializeField] private CharacterPassiveDatabaseSO _passiveDatabase;

        [Tooltip("초상화/무기 아이콘 재사용 소스(PartyConfig 의 PartyMemberData). " +
                 "entry.portrait 가 비어 있으면 캐릭터 타입으로 여기서 조회한다.")]
        [SerializeField] private PartyMemberDataSO _memberData;

        [Header("Cards")]
        [SerializeField] private UICharacterSelectCard _cardPrefab;
        [SerializeField] private Transform _cardRoot;

        [Header("Preview")]
        [SerializeField] private Image _portraitLarge;                     // 선택 캐릭터 대형 초상화
        [SerializeField] private RawImage _characterPreview;               // 3D 프리뷰(선택적, 수동 배선)
        [SerializeField] private UICharacterPreviewRenderer _previewRenderer;

        [Header("Detail Panel")]
        [SerializeField] private CanvasGroup _detailGroup;
        [SerializeField] private RectTransform _detailPanel;
        [SerializeField] private TextMeshProUGUI _detailNameText;
        [SerializeField] private TextMeshProUGUI _detailTaglineText;
        [SerializeField] private TextMeshProUGUI _elementText;
        [SerializeField] private Image _weaponIcon;
        [SerializeField] private TextMeshProUGUI _weaponNameText;
        [SerializeField] private TextMeshProUGUI _weaponDescText;

        [Header("Weapon Effects (고정 행)")]
        [SerializeField] private GameObject[] _effectRoots;
        [SerializeField] private Image[] _effectIcons;
        [SerializeField] private TextMeshProUGUI[] _effectTitles;
        [SerializeField] private TextMeshProUGUI[] _effectDescs;

        [Header("Representative Passives")]
        [SerializeField] private UIPassiveAbilityRow[] _passiveRows;
        [SerializeField] private GameObject _passiveEmptyRoot;

        [Header("Buttons")]
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        [Header("Tween")]
        [SerializeField] private float _detailDuration = 0.28f;
        [SerializeField] private float _detailSlide = 60f;

        /// <summary>
        /// 캐릭터 확정 시 발생. 타이틀 화면이 새 게임 파티와 서사 주인공으로 예약한다.
        /// </summary>
        public event Action<CharacterActorType> CharacterConfirmed;

        /// <summary>
        /// 선택 없이 화면을 닫을 때(취소/뒤로) 발생. 호출 측이 이전 화면 복귀 처리를 한다.
        /// </summary>
        public event Action Cancelled;

        private readonly List<UICharacterSelectCard> _cards = new();
        private readonly List<CharacterSelectDatabaseSO.Entry> _cardEntries = new();
        private int _selectedIndex = -1;
        private Vector2 _detailBasePos;
        private bool _detailBaseCached;
        private Tween _detailTween;

        // 하위(게임플레이) 입력을 독점하는 모달.
        protected override bool BlocksLowerInput => true;

        protected override void OnInit()
        {
            _layer = CanvasLayer.Scene;

            if (_confirmButton != null) _confirmButton.onClick.AddListener(Confirm);
            if (_cancelButton != null) _cancelButton.onClick.AddListener(Cancel);

            if (_previewRenderer != null && _characterPreview != null)
                _characterPreview.texture = _previewRenderer.GetRenderTexture();

            HideLegacyWeaponSection();
            EnsureElementText();
            EnsurePassiveRows();
            CacheDetailBase();
            BuildCards();
        }

        private void HideLegacyWeaponSection()
        {
            if (_weaponIcon != null && _weaponIcon.transform.parent != null)
                _weaponIcon.transform.parent.gameObject.SetActive(false);
            if (_weaponDescText != null)
                _weaponDescText.gameObject.SetActive(false);
            if (_effectRoots != null)
                for (int i = 0; i < _effectRoots.Length; i++)
                    if (_effectRoots[i] != null)
                        _effectRoots[i].SetActive(false);

            Transform content = _weaponDescText != null
                ? _weaponDescText.transform.parent
                : _detailPanel;
            content?.Find("EffectsHeader")?.gameObject.SetActive(false);
        }

        private void EnsureElementText()
        {
            if (_elementText != null)
                return;

            Transform parent = _detailTaglineText != null
                ? _detailTaglineText.transform.parent
                : _detailPanel;
            if (parent == null)
                return;

            var elementObject = new GameObject(
                "Element_Runtime",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(TextMeshProUGUI));
            elementObject.transform.SetParent(parent, false);
            elementObject.transform.SetSiblingIndex(
                _detailTaglineText != null
                    ? _detailTaglineText.transform.GetSiblingIndex() + 1
                    : 0);
            elementObject.GetComponent<LayoutElement>().preferredHeight = 30f;
            _elementText = elementObject.GetComponent<TextMeshProUGUI>();
            _elementText.fontSize = 20f;
            _elementText.alignment = TextAlignmentOptions.Left;
        }

        private void EnsurePassiveRows()
        {
            if (_passiveRows != null && _passiveRows.Length > 0)
                return;
            Transform parent = _weaponDescText != null
                ? _weaponDescText.transform.parent
                : _detailPanel;
            if (parent == null)
                return;

            var header = new GameObject(
                "PassivesHeader_Runtime",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(TextMeshProUGUI));
            header.transform.SetParent(parent, false);
            header.GetComponent<LayoutElement>().preferredHeight = 32f;
            var headerText = header.GetComponent<TextMeshProUGUI>();
            headerText.text = "대표 패시브";
            headerText.fontSize = 22f;
            headerText.color = new Color(0.35f, 0.80f, 0.90f, 1f);
            headerText.alignment = TextAlignmentOptions.Left;

            _passiveRows =
                new UIPassiveAbilityRow[CharacterPassiveSetSO.MaxCharacterSelectRepresentatives];
            for (int i = 0; i < _passiveRows.Length; i++)
                _passiveRows[i] = UIPassiveAbilityRow.CreateRuntime(parent, i);

            _passiveEmptyRoot = new GameObject(
                "PassiveEmpty_Runtime",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(TextMeshProUGUI));
            _passiveEmptyRoot.transform.SetParent(parent, false);
            _passiveEmptyRoot.GetComponent<LayoutElement>().preferredHeight = 36f;
            var emptyText = _passiveEmptyRoot.GetComponent<TextMeshProUGUI>();
            emptyText.text = "대표 패시브 정보 없음";
            emptyText.fontSize = 17f;
            emptyText.color = new Color(0.62f, 0.68f, 0.74f, 1f);
            emptyText.alignment = TextAlignmentOptions.Left;

            if (_detailPanel != null)
                _detailPanel.sizeDelta = new Vector2(
                    _detailPanel.sizeDelta.x,
                    Mathf.Max(860f, _detailPanel.sizeDelta.y));
        }

        private void CacheDetailBase()
        {
            if (_detailBaseCached || _detailPanel == null) return;
            _detailBasePos = _detailPanel.anchoredPosition;
            _detailBaseCached = true;
        }

        private void BuildCards()
        {
            foreach (var c in _cards)
                if (c != null) Destroy(c.gameObject);
            _cards.Clear();
            _cardEntries.Clear();

            if (_cardPrefab == null || _cardRoot == null || _database == null)
                return;

            for (int i = 0; i < _database.entries.Count; i++)
            {
                var entry = _database.entries[i];
                if (entry == null) continue;

                var card = Instantiate(_cardPrefab, _cardRoot);
                card.Init(this, _cards.Count, entry.characterType, ResolveDisplayName(entry), ResolveCardSprite(entry), entry.locked);
                _cards.Add(card);
                _cardEntries.Add(entry);
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            SelectDefault();
        }

        /// <summary> 진입 시 첫 번째 선택 가능(비잠금) 카드를 기본 선택한다. </summary>
        private void SelectDefault()
        {
            _selectedIndex = -1;
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].ResetInstant();

            int first = -1;
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] != null && !_cards[i].IsLocked) { first = i; break; }
            }

            if (first < 0)
            {
                // 선택 가능한 카드가 없으면 중립 상태 유지.
                ShowDetail(false, false);
                if (_confirmButton != null) _confirmButton.interactable = false;
                return;
            }

            SelectIndex(first, false);
            FocusCard(first);
        }

        protected override void OnHide()
        {
            _detailTween?.Kill();
            if (_previewRenderer != null) _previewRenderer.HidePreview();
            base.OnHide();
        }

        public override bool PerformBackFunction()
        {
            Cancel();
            return false;
        }

        // ── 상호작용 ──────────────────────────────────────────────

        public void OnCardClicked(int index)
        {
            if (index < 0 || index >= _cards.Count) return;
            if (_cards[index] == null || _cards[index].IsLocked) return;
            if (index == _selectedIndex) return;

            SelectIndex(index, true);
        }

        /// <summary>
        /// 키보드/게임패드 내비게이션으로 카드에 포커스가 들어오면
        /// 클릭과 동일하게 프리뷰·상세·선택 연출을 갱신한다.
        /// </summary>
        public void OnCardFocused(int index)
        {
            if (index < 0 || index >= _cards.Count)
                return;
            if (_cards[index] == null || _cards[index].IsLocked || index == _selectedIndex)
                return;

            SelectIndex(index, true);
        }

        /// <summary> 카드를 선택 상태로 만들고 상세를 갱신한다. (기본 선택/클릭 공용) </summary>
        private void SelectIndex(int index, bool animate)
        {
            _selectedIndex = index;

            for (int i = 0; i < _cards.Count; i++)
            {
                bool sel = i == index;
                _cards[i].SetSelected(sel, animate);
                _cards[i].SetDimmed(!sel, animate);
            }

            PopulateDetail(index);
            ShowDetail(true, animate);

            if (_confirmButton != null) _confirmButton.interactable = true;

            RebuildCardNavigation(index);
        }

        private void FocusCard(int index)
        {
            if (index < 0 || index >= _cards.Count)
                return;

            Selectable selectable = _cards[index]?.Selectable;
            if (selectable == null || !selectable.IsInteractable() || EventSystem.current == null)
                return;

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }

        /// <summary>
        /// 동적으로 생성되는 카드 중 잠긴 항목을 제외하고 좌우 이웃을 명시한다.
        /// 카드 위쪽은 시작 버튼으로, 하단 버튼의 아래쪽은 현재 카드로 연결한다.
        /// </summary>
        private void RebuildCardNavigation(int selectedIndex)
        {
            var available = new List<Selectable>();
            for (int i = 0; i < _cards.Count; i++)
            {
                Selectable selectable = _cards[i]?.Selectable;
                if (_cards[i] != null && !_cards[i].IsLocked
                    && selectable != null && selectable.IsInteractable())
                {
                    available.Add(selectable);
                }
            }

            for (int i = 0; i < available.Count; i++)
            {
                Navigation navigation = available[i].navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnLeft = available.Count > 1
                    ? available[(i - 1 + available.Count) % available.Count]
                    : null;
                navigation.selectOnRight = available.Count > 1
                    ? available[(i + 1) % available.Count]
                    : null;
                navigation.selectOnUp = _confirmButton != null && _confirmButton.interactable
                    ? _confirmButton
                    : _cancelButton;
                navigation.selectOnDown = null;
                available[i].navigation = navigation;
            }

            Selectable selectedCard = selectedIndex >= 0 && selectedIndex < _cards.Count
                ? _cards[selectedIndex]?.Selectable
                : null;
            ConfigureFooterNavigation(selectedCard);
        }

        private void ConfigureFooterNavigation(Selectable selectedCard)
        {
            if (_cancelButton != null)
            {
                Navigation navigation = _cancelButton.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnLeft = null;
                navigation.selectOnRight = _confirmButton;
                navigation.selectOnUp = null;
                navigation.selectOnDown = selectedCard;
                _cancelButton.navigation = navigation;
            }

            if (_confirmButton != null)
            {
                Navigation navigation = _confirmButton.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnLeft = _cancelButton;
                navigation.selectOnRight = null;
                navigation.selectOnUp = null;
                navigation.selectOnDown = selectedCard;
                _confirmButton.navigation = navigation;
            }
        }

        public void Cancel()
        {
            // 캐릭터 선택은 항상 기본 선택이 존재하므로, 취소는 화면을 닫고
            // 호출 측(타이틀 등)에 복귀를 알린다.
            Cancelled?.Invoke();
            Hide();
        }

        private void Confirm()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _cardEntries.Count)
                return;

            CharacterActorType type = _cardEntries[_selectedIndex].characterType;
            CharacterConfirmed?.Invoke(type);
        }

        // ── 상태 갱신 ─────────────────────────────────────────────

        // entry 값이 있으면 우선, 없으면 PartyMemberData 에서 캐릭터 타입으로 조회.
        private string ResolveDisplayName(CharacterSelectDatabaseSO.Entry entry)
        {
            if (!string.IsNullOrEmpty(entry.displayName)) return entry.displayName;
            string fromMember = _memberData != null ? _memberData.GetName(entry.characterType) : null;
            return string.IsNullOrEmpty(fromMember) ? entry.characterType.ToString() : fromMember;
        }

        // 카드용 초상화: entry.portrait 우선, 없으면 PartyMemberData 의 머리 스프라이트.
        private Sprite ResolveCardSprite(CharacterSelectDatabaseSO.Entry entry)
            => entry.portrait != null ? entry.portrait
               : (_memberData != null ? _memberData.GetHeadSprite(entry.characterType) : null);

        // 대형 프리뷰용 초상화: entry.portrait 우선, 없으면 PartyMemberData 의 전신 스프라이트.
        private Sprite ResolveLargeSprite(CharacterSelectDatabaseSO.Entry entry)
            => entry.portrait != null ? entry.portrait
               : (_memberData != null ? _memberData.GetFullBodySprite(entry.characterType) : null);

        private void PopulateDetail(int index)
        {
            if (index < 0 || index >= _cardEntries.Count)
                return;
            var entry = _cardEntries[index];

            if (_detailNameText != null) _detailNameText.text = ResolveDisplayName(entry);
            if (_detailTaglineText != null) _detailTaglineText.text = entry.tagline;
            if (_elementText != null)
            {
                var element = _memberData != null
                    ? _memberData.GetCombatElement(entry.characterType)
                    : CombatElement.None;
                _elementText.text = UICombatElementDisplay.RichLabel(element);
                _elementText.color = UICombatElementDisplay.Color(element);
            }

            if (_portraitLarge != null)
            {
                Sprite large = ResolveLargeSprite(entry);
                _portraitLarge.sprite = large;
                _portraitLarge.enabled = large != null;
            }
            if (_previewRenderer != null) _previewRenderer.ShowPreview(entry.characterType);

            PopulatePassives(entry.characterType);
        }

        private void PopulatePassives(CharacterActorType characterType)
        {
            if (_passiveRows != null)
            {
                for (int i = 0; i < _passiveRows.Length; i++)
                    _passiveRows[i]?.Clear();
            }

            CharacterPassiveSetSO set =
                _passiveDatabase?.Get(characterType)
                ?? Svc.Passives?.GetPassiveSet(characterType);
            int rowIndex = 0;
            if (set != null && _passiveRows != null)
            {
                foreach (PassiveAbilitySO passive in set.EnumerateCharacterSelectRepresentatives())
                {
                    if (rowIndex >= _passiveRows.Length)
                        break;
                    _passiveRows[rowIndex]?.Bind(passive);
                    rowIndex++;
                }
            }

            if (_passiveEmptyRoot != null)
                _passiveEmptyRoot.SetActive(rowIndex == 0);
        }

        private void ShowDetail(bool show, bool animate)
        {
            if (_detailGroup == null) return;
            CacheDetailBase();
            _detailTween?.Kill();

            _detailGroup.interactable = show;
            _detailGroup.blocksRaycasts = show;
            if (show) _detailGroup.gameObject.SetActive(true);

            if (!animate)
            {
                _detailGroup.alpha = show ? 1f : 0f;
                if (_detailPanel != null) _detailPanel.anchoredPosition = _detailBasePos;
                if (!show) _detailGroup.gameObject.SetActive(false);
                return;
            }

            var seq = DOTween.Sequence().SetUpdate(true);
            seq.Join(DOTween.To(
                () => _detailGroup.alpha,
                value => _detailGroup.alpha = value,
                show ? 1f : 0f,
                _detailDuration));
            if (_detailPanel != null)
            {
                if (show) _detailPanel.anchoredPosition = _detailBasePos + Vector2.right * _detailSlide;
                Vector2 to = show ? _detailBasePos : _detailBasePos + Vector2.right * _detailSlide;
                seq.Join(DOTween.To(
                    () => _detailPanel.anchoredPosition,
                    value => _detailPanel.anchoredPosition = value,
                    to,
                    _detailDuration).SetEase(Ease.OutCubic));
            }
            if (!show)
                seq.OnComplete(() =>
                {
                    if (_detailGroup != null) _detailGroup.gameObject.SetActive(false);
                });

            _detailTween = seq;
        }
    }
}
