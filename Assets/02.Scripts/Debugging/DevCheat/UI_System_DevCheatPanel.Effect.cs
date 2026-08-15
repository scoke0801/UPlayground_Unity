#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Contracts.Ability;
using UPlayGround.Data.Ability;
using UPlayGround.Manager;

namespace UPlayGround.UI.DevCheat
{
    /// <summary>UI_System_DevCheatPanel — 버프·디버프 발급/제거 탭.</summary>
    public partial class UI_System_DevCheatPanel
    {
        private readonly List<GameplayEffectSO> _availableEffects = new();
        private readonly List<GameplayEffectViewState> _activeEffects = new();
        private TMP_InputField _effectSearch;
        private RectTransform _availableEffectContent;
        private RectTransform _activeEffectContent;
        private TextMeshProUGUI _activeEffectSummary;
        private PlayerActor _effectObservedPlayer;
        private bool _effectActiveListDirty;

        private void BuildEffectTab(RectTransform panel)
        {
            var layout = AddHLG(panel.gameObject, 12, 12);
            layout.childForceExpandWidth = false;

            RectTransform available = NewRect("AvailableEffects", panel);
            SetSize(available.gameObject, flexW: 1);
            AddImage(available.gameObject, PanelBg);
            var availableLayout = AddVLG(available.gameObject, 8, 10);
            availableLayout.childForceExpandHeight = false;

            TextMeshProUGUI title = MakeText(
                available,
                "발급 가능한 버프 / 디버프",
                21,
                Accent);
            SetSize(title.gameObject, minH: 34, prefH: 34);

            _effectSearch = MakeInput(
                available,
                "Effect ID 또는 표시 이름 검색",
                _ => RefreshAvailableEffectList());
            SetSize(_effectSearch.gameObject, minH: 40, prefH: 40);

            _availableEffectContent = MakeScroll(available, out _);
            SetSize(
                ((RectTransform)_availableEffectContent.parent.parent).gameObject,
                flexH: 1);

            RectTransform active = NewRect("ActiveEffects", panel);
            SetSize(active.gameObject, minW: 470, prefW: 470);
            AddImage(active.gameObject, PanelBg);
            var activeLayout = AddVLG(active.gameObject, 8, 10);
            activeLayout.childForceExpandHeight = false;

            RectTransform activeHeader = NewRect("ActiveHeader", active);
            SetSize(activeHeader.gameObject, minH: 42, prefH: 42);
            var headerLayout = AddHLG(activeHeader.gameObject, 8, 0);
            headerLayout.childForceExpandWidth = false;

            _activeEffectSummary = MakeText(
                activeHeader,
                "활성 Effect 0개",
                20,
                Accent);
            SetSize(_activeEffectSummary.gameObject, flexW: 1);

            Button refresh = MakeButton(
                activeHeader,
                "새로고침",
                BtnBg,
                RefreshEffectTab,
                14);
            SetSize(refresh.gameObject, minW: 88, prefW: 88);

            Button removeAll = MakeButton(
                activeHeader,
                "전체 제거",
                DangerBtn,
                RemoveAllEffects,
                14);
            SetSize(removeAll.gameObject, minW: 88, prefW: 88);

            _activeEffectContent = MakeScroll(active, out _);
            SetSize(
                ((RectTransform)_activeEffectContent.parent.parent).gameObject,
                flexH: 1);
        }

        private void RefreshEffectTab()
        {
            RefreshAvailableEffectList();
            RefreshActiveEffectList();
        }

        private void RefreshAvailableEffectList()
        {
            if (_availableEffectContent == null)
                return;

            ClearChildren(_availableEffectContent);
            CheatManager cheat = CheatManager.Instance;
            if (cheat == null)
            {
                MakeText(
                    _availableEffectContent,
                    "CheatManager 준비 대기 중…",
                    16,
                    TextSub);
                return;
            }

            cheat.CopyAvailableGameplayEffects(_availableEffects);
            string search = _effectSearch != null
                ? _effectSearch.text?.Trim()
                : string.Empty;
            int shown = 0;
            for (int i = 0; i < _availableEffects.Count; i++)
            {
                GameplayEffectSO effect = _availableEffects[i];
                if (!MatchesEffectSearch(effect, search))
                    continue;

                BuildAvailableEffectRow(effect, shown++);
            }

            if (shown == 0)
            {
                MakeText(
                    _availableEffectContent,
                    "표시할 Duration/Infinite Effect가 없습니다.",
                    16,
                    TextSub);
            }
        }

        private void BuildAvailableEffectRow(GameplayEffectSO effect, int index)
        {
            RectTransform row = NewRect(
                "Effect_" + effect.effectId,
                _availableEffectContent);
            SetSize(row.gameObject, minH: 82, prefH: 82);
            AddImage(row.gameObject, index % 2 == 0 ? RowBg : RowBgAlt);
            var layout = AddHLG(row.gameObject, 8, 8);
            layout.childForceExpandWidth = false;

            string name = GetEffectDisplayName(effect);
            string polarity = GetPolarityLabel(effect.polarity);
            string duration = effect.durationType
                == GameplayEffectDurationType.Infinite
                    ? "무한"
                    : $"{effect.durationSeconds:0.##}초";
            TextMeshProUGUI label = MakeText(
                row,
                $"<b>{name}</b>\n" +
                $"<color=#8FA6B5>{effect.effectId}</color>\n" +
                $"[{polarity}] [{duration}] [HUD " +
                $"{((effect.presentation?.showInHud ?? true) ? "표시" : "숨김")}]",
                14,
                TextMain);
            SetSize(label.gameObject, flexW: 1);

            GameplayEffectSO captured = effect;
            Button grant = MakeButton(
                row,
                "발급",
                effect.polarity == GameplayEffectPolarity.Harmful
                    ? DangerBtn
                    : AccentBtn,
                () => GrantEffect(captured),
                15);
            SetSize(grant.gameObject, minW: 76, prefW: 76);
        }

        private void RefreshActiveEffectList()
        {
            _effectActiveListDirty = false;
            if (_activeEffectContent == null)
                return;

            ClearChildren(_activeEffectContent);
            PlayerActor player = PartyManager.Instance?.ActiveCharacter;
            _activeEffects.Clear();
            player?.Effects?.CopyActiveEffects(_activeEffects);
            _activeEffects.Sort(CompareActiveEffects);

            if (_activeEffectSummary != null)
            {
                _activeEffectSummary.text =
                    $"활성 Effect {_activeEffects.Count}개";
            }

            for (int i = 0; i < _activeEffects.Count; i++)
                BuildActiveEffectRow(_activeEffects[i], i);

            if (_activeEffects.Count == 0)
            {
                MakeText(
                    _activeEffectContent,
                    "활성 버프·디버프가 없습니다.",
                    16,
                    TextSub);
            }
        }

        private void BuildActiveEffectRow(
            GameplayEffectViewState state,
            int index)
        {
            RectTransform row = NewRect(
                "ActiveEffect_" + state.RuntimeId,
                _activeEffectContent);
            SetSize(row.gameObject, minH: 82, prefH: 82);
            AddImage(row.gameObject, index % 2 == 0 ? RowBg : RowBgAlt);
            var layout = AddHLG(row.gameObject, 8, 8);
            layout.childForceExpandWidth = false;

            string remaining = state.IsInfinite
                ? "무한"
                : $"{Mathf.Max(0f, state.RemainingSeconds):0.0}초";
            TextMeshProUGUI label = MakeText(
                row,
                $"<b>{state.DisplayName}</b>\n" +
                $"<color=#8FA6B5>{state.EffectId}</color>\n" +
                $"[{GetPolarityLabel(state.Polarity)}] " +
                $"[{remaining}] [스택 {state.StackCount}]",
                14,
                TextMain);
            SetSize(label.gameObject, flexW: 1);

            ulong runtimeId = state.RuntimeId;
            Button remove = MakeButton(
                row,
                "제거",
                DangerBtn,
                () => RemoveEffect(runtimeId),
                15);
            SetSize(remove.gameObject, minW: 72, prefW: 72);
        }

        private void GrantEffect(GameplayEffectSO effect)
        {
            CheatManager.Instance?.GrantGameplayEffect(effect);
        }

        private void RemoveEffect(ulong runtimeId)
        {
            CheatManager.Instance?.RemoveGameplayEffect(runtimeId);
        }

        private void RemoveAllEffects()
        {
            CheatManager.Instance?.RemoveAllGameplayEffects();
        }

        private void BindEffectCheatEvents()
        {
            UnbindEffectCheatEvents();
            if (PartyManager.Instance != null)
                PartyManager.Instance.OnSwapCompleted += OnEffectCheatPlayerSwapped;
            BindObservedPlayer(PartyManager.Instance?.ActiveCharacter);
        }

        private void UnbindEffectCheatEvents()
        {
            if (PartyManager.Instance != null)
                PartyManager.Instance.OnSwapCompleted -= OnEffectCheatPlayerSwapped;
            BindObservedPlayer(null);
        }

        private void OnEffectCheatPlayerSwapped(PlayerActor current)
        {
            BindObservedPlayer(current);
            if (_currentTab == CheatTab.Effect)
                RefreshEffectTab();
        }

        private void BindObservedPlayer(PlayerActor player)
        {
            if (_effectObservedPlayer?.Effects != null)
                _effectObservedPlayer.Effects.StateChanged -= OnObservedEffectChanged;
            _effectObservedPlayer = player;
            if (_effectObservedPlayer?.Effects != null)
                _effectObservedPlayer.Effects.StateChanged += OnObservedEffectChanged;
        }

        private void OnObservedEffectChanged()
        {
            if (_currentTab == CheatTab.Effect)
                _effectActiveListDirty = true;
        }

        private void LateUpdate()
        {
            if (_effectActiveListDirty && _currentTab == CheatTab.Effect)
                RefreshActiveEffectList();
        }

        private static bool MatchesEffectSearch(
            GameplayEffectSO effect,
            string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return true;

            return (!string.IsNullOrWhiteSpace(effect.effectId)
                    && effect.effectId.IndexOf(
                        search,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                   || GetEffectDisplayName(effect).IndexOf(
                       search,
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetEffectDisplayName(GameplayEffectSO effect) =>
            HasMeaningfulDisplayName(effect?.presentation?.displayName)
                ? effect.presentation.displayName
                : effect?.name ?? "Effect";

        private static bool HasMeaningfulDisplayName(string displayName) =>
            !string.IsNullOrWhiteSpace(displayName)
            && !string.Equals(
                displayName.Trim(),
                "새 Effect",
                StringComparison.Ordinal);

        private static string GetPolarityLabel(
            GameplayEffectPolarity polarity) =>
            polarity switch
            {
                GameplayEffectPolarity.Beneficial => "버프",
                GameplayEffectPolarity.Harmful => "디버프",
                _ => "중립",
            };

        private static int CompareActiveEffects(
            GameplayEffectViewState left,
            GameplayEffectViewState right)
        {
            int polarity = left.Polarity.CompareTo(right.Polarity);
            return polarity != 0
                ? polarity
                : string.CompareOrdinal(left.EffectId, right.EffectId);
        }
    }
}
#endif
