using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 신규 게임 시작 시 조작할 캐릭터 하나를 선택하는 화면.
    /// - 카드 클릭 → 트윈으로 선택 강조 + 상세(무기·무기 효과) 표시.
    /// - 취소 → 이전(중립) 상태로 복귀. 선택이 없으면 화면을 닫는다.
    /// - 시작 → CharacterConfirmed 이벤트 발생. (PartyConfig 반영은 추후 이 이벤트에 연동.)
    /// </summary>
    public class UI_CharacterSelect : UI_Base
    {
        /// <summary>
        /// UIManager 등록 키. UIKeyType은 자동 생성 파일이라 항목이 없을 수 있으므로,
        /// 문자열 상수로 두어 ShowUI(string) 오버로드로 호출한다. (UIPrefabDatabase 등록 키와 동일)
        /// </summary>
        public const string UIKey = "CharacterSelect";

        [Header("Data")]
        [SerializeField] private CharacterSelectDatabaseSO _database;

        [Tooltip("초상화/무기 아이콘 재사용 소스(PartyConfig 의 PartyMemberData). " +
                 "entry.portrait 가 비어 있으면 캐릭터 타입으로 여기서 조회한다.")]
        [SerializeField] private PartyMemberDataSO _memberData;

        [Header("Cards")]
        [SerializeField] private UI_CharacterSelectCard _cardPrefab;
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
        [SerializeField] private Image _weaponIcon;
        [SerializeField] private TextMeshProUGUI _weaponNameText;
        [SerializeField] private TextMeshProUGUI _weaponDescText;

        [Header("Weapon Effects (고정 행)")]
        [SerializeField] private GameObject[] _effectRoots;
        [SerializeField] private Image[] _effectIcons;
        [SerializeField] private TextMeshProUGUI[] _effectTitles;
        [SerializeField] private TextMeshProUGUI[] _effectDescs;

        [Header("Buttons")]
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        [Header("Tween")]
        [SerializeField] private float _detailDuration = 0.28f;
        [SerializeField] private float _detailSlide = 60f;

        /// <summary>
        /// 캐릭터 확정 시 발생. PartyConfig 연동(선택 캐릭터로 신규 게임 시작)은 추후 이 이벤트에 연결한다.
        /// </summary>
        public event Action<CharacterActorType> CharacterConfirmed;

        /// <summary>
        /// 선택 없이 화면을 닫을 때(취소/뒤로) 발생. 호출 측이 이전 화면 복귀 처리를 한다.
        /// </summary>
        public event Action Cancelled;

        private readonly List<UI_CharacterSelectCard> _cards = new();
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

            CacheDetailBase();
            BuildCards();
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

            if (_cardPrefab == null || _cardRoot == null || _database == null)
                return;

            for (int i = 0; i < _database.entries.Count; i++)
            {
                var entry = _database.entries[i];
                if (entry == null) continue;

                var card = Instantiate(_cardPrefab, _cardRoot);
                card.Init(this, _cards.Count, entry.characterType, ResolveDisplayName(entry), ResolveCardSprite(entry), entry.locked);
                _cards.Add(card);
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
            if (_database == null || _selectedIndex < 0 || _selectedIndex >= _database.entries.Count)
                return;

            CharacterActorType type = _database.entries[_selectedIndex].characterType;
            CharacterConfirmed?.Invoke(type);

            // TODO: PartyConfig 연동 — 선택한 캐릭터로 신규 게임을 시작한다. 현재는 이벤트만 발생.
            Debug.Log($"[CharacterSelect] 캐릭터 확정: {type}");
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
            var entry = _database.entries[index];

            if (_detailNameText != null) _detailNameText.text = ResolveDisplayName(entry);
            if (_detailTaglineText != null) _detailTaglineText.text = entry.tagline;

            if (_portraitLarge != null)
            {
                Sprite large = ResolveLargeSprite(entry);
                _portraitLarge.sprite = large;
                _portraitLarge.enabled = large != null;
            }
            if (_previewRenderer != null) _previewRenderer.ShowPreview(entry.characterType);

            WeaponEffectSO effect = entry.weaponEffect;
            if (effect != null)
            {
                if (_weaponIcon != null)
                {
                    Sprite icon = effect.weaponIcon != null
                        ? effect.weaponIcon
                        : (_memberData != null ? _memberData.GetWeaponIcon(entry.characterType) : null);
                    _weaponIcon.sprite = icon;
                    _weaponIcon.enabled = icon != null;
                }
                if (_weaponNameText != null) _weaponNameText.text = effect.ResolveWeaponName();
                if (_weaponDescText != null) _weaponDescText.text = effect.weaponDescription;
                PopulateEffects(effect.effects);
            }
            else
            {
                if (_weaponIcon != null) _weaponIcon.enabled = false;
                if (_weaponNameText != null) _weaponNameText.text = "-";
                if (_weaponDescText != null) _weaponDescText.text = string.Empty;
                PopulateEffects(null);
            }
        }

        private void PopulateEffects(List<WeaponEffectSO.EffectEntry> effects)
        {
            int rows = _effectRoots != null ? _effectRoots.Length : 0;
            for (int i = 0; i < rows; i++)
            {
                bool has = effects != null && i < effects.Count;
                if (_effectRoots[i] != null) _effectRoots[i].SetActive(has);
                if (!has) continue;

                WeaponEffectSO.EffectEntry e = effects[i];
                if (_effectIcons != null && i < _effectIcons.Length && _effectIcons[i] != null)
                {
                    _effectIcons[i].sprite = e.icon;
                    _effectIcons[i].enabled = e.icon != null;
                }
                if (_effectTitles != null && i < _effectTitles.Length && _effectTitles[i] != null)
                    _effectTitles[i].text = e.title;
                if (_effectDescs != null && i < _effectDescs.Length && _effectDescs[i] != null)
                    _effectDescs[i].text = e.description;
            }
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
            seq.Join(_detailGroup.DOFade(show ? 1f : 0f, _detailDuration));
            if (_detailPanel != null)
            {
                if (show) _detailPanel.anchoredPosition = _detailBasePos + Vector2.right * _detailSlide;
                Vector2 to = show ? _detailBasePos : _detailBasePos + Vector2.right * _detailSlide;
                seq.Join(_detailPanel.DOAnchorPos(to, _detailDuration).SetEase(Ease.OutCubic));
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
