using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Codex;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>종별 기록과 상대 전용 보정을 표시하는 Scene 레이어 몬스터 도감.</summary>
    public sealed class UI_Scene_MonsterCodex : UI_SceneBase
    {
        [Header("필터")]
        [SerializeField] private TMP_Dropdown _gradeFilter;
        [SerializeField] private TMP_Dropdown _elementFilter;

        [Header("목록")]
        [SerializeField] private Transform _listContent;
        [SerializeField] private UIMonsterCodexSlot _slotPrefab;

        [Header("상세")]
        [SerializeField] private CanvasGroup _detailGroup;
        [SerializeField] private Image _portrait;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _description;
        [SerializeField] private TextMeshProUGUI _element;
        [SerializeField] private TextMeshProUGUI _progress;
        [SerializeField] private TextMeshProUGUI _bonuses;
        [SerializeField] private Button _closeButton;

        private readonly List<UIMonsterCodexSlot> _spawned = new();
        private IReadOnlyList<MonsterCodexEntryView> _entries = Array.Empty<MonsterCodexEntryView>();
        private string _selectedActorId;

        protected override bool BlocksLowerInput => true;

        protected override void Awake()
        {
            base.Awake();
            ConfigureMainPageShortcut(UIKeyType.MonsterCodex);
            NormalizeFullScreenLayout();
            _gradeFilter?.onValueChanged.AddListener(_ => RefreshList());
            _elementFilter?.onValueChanged.AddListener(_ => RefreshList());
            _closeButton?.onClick.AddListener(Hide);
            BuildFilterOptions();
        }

        protected override void OnShow()
        {
            NormalizeFullScreenLayout();
            base.OnShow();
            _entries = Svc.MonsterCodex?.GetAllEntries() ??
                       Array.Empty<MonsterCodexEntryView>();
            _selectedActorId = null;
            SetDetailVisible(false);
            RefreshList();
            RebuildLayout();
        }

        protected override void OnDispose()
        {
            _gradeFilter?.onValueChanged.RemoveAllListeners();
            _elementFilter?.onValueChanged.RemoveAllListeners();
            _closeButton?.onClick.RemoveListener(Hide);
            base.OnDispose();
        }

        public override bool PerformBackFunction()
        {
            Hide();
            return false;
        }

        private void BuildFilterOptions()
        {
            if (_gradeFilter != null)
            {
                _gradeFilter.ClearOptions();
                var labels = new List<string> { "전체 등급" };
                foreach (MonsterActorGrade grade in Enum.GetValues(typeof(MonsterActorGrade)))
                    labels.Add(grade.ToString());
                _gradeFilter.AddOptions(labels);
            }

            if (_elementFilter != null)
            {
                _elementFilter.ClearOptions();
                var labels = new List<string> { "전체 속성" };
                foreach (CombatElement element in Enum.GetValues(typeof(CombatElement)))
                    labels.Add(UICombatElementDisplay.Label(element));
                _elementFilter.AddOptions(labels);
            }
        }

        private void RefreshList()
        {
            ClearSlots();
            MonsterCodexEntryView first = null;

            foreach (MonsterCodexEntryView view in _entries)
            {
                if (view == null || !MatchesFilters(view))
                    continue;

                UIMonsterCodexSlot slot = Instantiate(_slotPrefab, _listContent);
                slot.Bind(view, Select);
                _spawned.Add(slot);
                first ??= view;
            }

            if (!string.IsNullOrEmpty(_selectedActorId))
            {
                foreach (MonsterCodexEntryView view in _entries)
                {
                    if (view?.actorId == _selectedActorId && MatchesFilters(view))
                    {
                        Select(view);
                        return;
                    }
                }
            }

            if (first != null)
                Select(first);
            else
                SetDetailVisible(false);

            RebuildNavigation();
            RebuildLayout();
        }

        private void RebuildNavigation()
        {
            var slots = new List<Selectable>();
            foreach (UIMonsterCodexSlot slot in _spawned)
            {
                if (slot != null && slot.Selectable != null)
                    slots.Add(slot.Selectable);
            }
            UIFocusNavigation.ConfigureVertical(slots);

            var filters = new Selectable[] { _gradeFilter, _elementFilter, _closeButton };
            UIFocusNavigation.ConfigureHorizontal(filters);
            Selectable firstFilter = UIFocusNavigation.FirstNavigable(filters);

            foreach (Selectable slot in slots)
            {
                Navigation navigation = slot.navigation;
                navigation.selectOnUp ??= firstFilter;
                slot.navigation = navigation;
            }

            Selectable initial = slots.Count > 0 ? slots[0] : firstFilter;
            SetDefaultFocus(initial, IsVisible);
        }

        private void RebuildLayout()
        {
            Canvas.ForceUpdateCanvases();
            if (_sceneContent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_sceneContent);
            if (_listContent is RectTransform listRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(listRect);
        }

        private void NormalizeFullScreenLayout()
        {
            if (_rectTransform != null)
            {
                _rectTransform.anchorMin = Vector2.zero;
                _rectTransform.anchorMax = Vector2.one;
                _rectTransform.pivot = new Vector2(0.5f, 0.5f);
                _rectTransform.anchoredPosition = Vector2.zero;
                _rectTransform.offsetMin = Vector2.zero;
                _rectTransform.offsetMax = Vector2.zero;
                _rectTransform.localScale = Vector3.one;
            }

            if (_sceneContent != null)
            {
                const float safeInset = 28f;
                _sceneContent.anchorMin = Vector2.zero;
                _sceneContent.anchorMax = Vector2.one;
                _sceneContent.pivot = new Vector2(0.5f, 0.5f);
                _sceneContent.anchoredPosition = Vector2.zero;
                _sceneContent.offsetMin = new Vector2(safeInset, safeInset);
                _sceneContent.offsetMax = new Vector2(-safeInset, -safeInset);
                _sceneContent.localScale = Vector3.one;
            }
        }

        private bool MatchesFilters(MonsterCodexEntryView view)
        {
            if (_gradeFilter != null && _gradeFilter.value > 0)
            {
                MonsterActorGrade selected =
                    (MonsterActorGrade)(_gradeFilter.value - 1);
                if (view.grade != selected)
                    return false;
            }

            if (_elementFilter != null && _elementFilter.value > 0)
            {
                CombatElement selected = (CombatElement)(_elementFilter.value - 1);
                if (view.element != selected)
                    return false;
            }

            return true;
        }

        private void Select(MonsterCodexEntryView view)
        {
            _selectedActorId = view.actorId;
            foreach (UIMonsterCodexSlot slot in _spawned)
                slot.SetSelected(slot.ActorId == view.actorId);

            SetDetailVisible(true);
            bool discovered = view.discovered;
            if (_portrait != null)
            {
                _portrait.sprite = view.portrait;
                _portrait.color = discovered ? Color.white : Color.black;
            }
            if (_name != null) _name.text = discovered ? view.displayName : "???";
            if (_description != null) _description.text = discovered ? view.description : "???";
            if (_progress != null)
            {
                _progress.text = discovered
                    ? $"기록 {view.recordRatio * 100f:0}% ({view.killCount:N0}/{view.fullRecordKillCount:N0})"
                    : "기록 0%";
            }
            if (_element != null)
            {
                _element.text = !discovered
                    ? "속성: ?"
                    : view.elementAssignmentMode == CombatElementAssignmentMode.RandomPerNewGame &&
                      view.element == CombatElement.None
                        ? "속성: ?"
                        : UICombatElementDisplay.RichLabel(view.element);
                _element.color = discovered && view.element != CombatElement.None
                    ? UICombatElementDisplay.Color(view.element)
                    : Color.white;
            }
            if (_bonuses != null)
            {
                _bonuses.text = discovered
                    ? $"경험치 +{(view.ExpMultiplier - 1f) * 100f:0.#}%\n" +
                      $"가하는 피해 +{(view.DamageDealtMultiplier - 1f) * 100f:0.#}%\n" +
                      $"입는 피해 {(view.DamageTakenMultiplier - 1f) * 100f:0.#}%"
                    : "???";
            }
        }

        private void SetDetailVisible(bool visible)
        {
            if (_detailGroup == null)
                return;
            _detailGroup.alpha = visible ? 1f : 0f;
            _detailGroup.interactable = visible;
            _detailGroup.blocksRaycasts = visible;
        }

        private void ClearSlots()
        {
            foreach (UIMonsterCodexSlot slot in _spawned)
            {
                if (slot == null) continue;
                slot.gameObject.SetActive(false);
                Destroy(slot.gameObject);
            }
            _spawned.Clear();
        }
    }
}
