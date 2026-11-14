using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Pet;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    public sealed partial class UI_Scene_PetMenu
    {
        private void InitializeV2()
        {
            if (_tabs != null)
            {
                ConfigureTabShortcuts(_tabs, _tabs);
                _tabs.SelectionChanged += SelectTab;
            }
            _model?.onClick.AddListener(() =>
            { RefreshPreview(); _inDetail = true; if (_apply.interactable) SetDefaultFocus(_apply, true); });
            _apply?.onClick.AddListener(ApplyModel);
            _selectSkill?.onClick.AddListener(SelectSkill);
            _upgrade?.onClick.AddListener(UpgradeSkill);
            _retrySave?.onClick.AddListener(() => { _pets?.RetrySave(); Refresh(); });
            if (_skillTemplate != null) _skillTemplate.gameObject.SetActive(false);
        }
        private void SelectTab(int index)
        {
            _inDetail = false;
            _modelPage?.SetActive(index == 0); _skillPage?.SetActive(index == 1);
            if (index == 1) StartSkillFlow(); else StopSkillFlow();
            if (index == 0) RefreshPreview(); else _preview?.HidePreview();
            RefreshSkills(); ConfigureFocusNavigation();
            FocusCurrentItem();
        }
        private void FocusCurrentItem()
        {
            if (_tabs?.SelectedIndex == 1 && _pets?.Catalog?.skills != null)
            {
                var skills = _pets.Catalog.skills.skills;
                for (int i = 0; i < skills.Count && i < _skillButtons.Count; i++)
                    if (skills[i].skillId == _selectedSkillId) { SetDefaultFocus(_skillButtons[i], true); return; }
            }
            if (_appearanceButtons.Count > 0)
            {
                var save = _pets?.GetSnapshot();
                var owned = save?.owned.Find(entry => entry.petId == save.selectedPetId);
                int index = _pets?.Catalog?.Find(owned?.petId)?.appearances.FindIndex(entry => entry.appearanceId == owned?.appearanceId) ?? 0;
                if (index >= 0 && index < _appearanceButtons.Count) { SetDefaultFocus(_appearanceButtons[index], true); return; }
            }
            SetDefaultFocus(_toggle.interactable ? _toggle : _close, true);
        }
        private void RefreshPreview()
        {
            var save = _pets?.GetSnapshot();
            var owned = save?.owned.Find(entry => entry.petId == save.selectedPetId);
            var definition = _pets?.Catalog?.Find(owned?.petId);
            if (_previewAppearanceId == null) _previewAppearanceId = owned?.appearanceId;
            if (_preview != null && !_preview.ShowPetPreview(definition, _previewAppearanceId))
                _status.text = "지금은 모습을 미리 볼 수 없어요.";
        }
        private void ApplyModel()
        {
            var save = _pets?.GetSnapshot();
            var definition = _pets?.Catalog?.Find(save?.selectedPetId);
            if (definition == null || !_pets.TrySetModel(definition.modelId)
                || !_pets.TrySetAppearance(_previewAppearanceId)) _feedback = "모습을 바꾸지 못했어요.";
            else
            {
                if (!save.isAccompanying) _pets.SetAccompanying(true);
                _feedback = "이 모습으로 함께 다녀요.";
            }
            Refresh(); SetDefaultFocus(_apply, true);
        }
        private void RefreshSkills()
        {
            var catalog = _pets?.Catalog?.skills;
            if (catalog == null || _skillTemplate == null) return;
            for (int i = 0; i < catalog.skills.Count; i++)
            {
                var skill = catalog.skills[i];
                if (i >= _skillButtons.Count)
                {
                    var button = Instantiate(_skillTemplate, _skillRoot);
                    string id = skill.skillId;
                    button.onClick.AddListener(() =>
                    {
                        _selectedSkillId = id; _inDetail = true; RefreshSkills();
                        if (_selectSkill.interactable) SetDefaultFocus(_selectSkill, true);
                        else if (_upgrade.interactable) SetDefaultFocus(_upgrade, true);
                        else FocusCurrentItem();
                    });
                    _skillButtons.Add(button);
                }
                var item = _skillButtons[i]; item.gameObject.SetActive(true);
                if (i < _skillSlots.Length) item.transform.SetParent(_skillSlots[i], false);
                ((RectTransform)item.transform).anchoredPosition = Vector2.zero;
                item.GetComponentInChildren<TextMeshProUGUI>().text =
                    (skill.skillId == _selectedSkillId ? "◆ " : "") + skill.displayName;
                var icon = item.transform.Find("SkillIcon")?.GetComponent<Image>();
                if (icon != null) icon.sprite = skill.icon;
                RefreshSkillNode(item, skill.skillId, _pets.GetSkillLevel(skill.skillId));
            }
            var selected = catalog.Find(_selectedSkillId);
            if (selected == null) return;
            _shownLevel = _pets.GetSkillLevel(_selectedSkillId);
            var current = selected.GetLevel(_shownLevel);
            var next = selected.GetLevel(_shownLevel + 1);
            _shownCost = current?.upgradeCost ?? 0;
            _shownRevision = selected.GetRevision();
            _skillDetail.text = $"<size=44><b>{selected.displayName}</b></size>   Lv.{_shownLevel}\n\n<color=#8DCDBE>현재 효과</color>\n"
                + "  " + Describe(_selectedSkillId, current)
                + (next != null ? "\n\n<color=#8DCDBE>다음 단계</color>\n" + Describe(_selectedSkillId, next) + $"\n\n강화 비용   {_shownCost:N0} G" : "\n\n최대 레벨이에요.");
            if (_selectedSkillId == PetSkillCatalogSO.PickupRange) _skillDetail.text += "\n\n아이템 줍기를 켜면 늘어난 범위를 사용해요.";
            if (selected.supportAbility != null && selected.triggerPolicy != null)
                _skillDetail.text += "\n\n" + DescribeTrigger(selected.triggerPolicy);
            bool canEdit = _pets.CanEdit(out _);
            _upgrade.interactable = canEdit && next != null && (_inventory?.Gold ?? 0) >= _shownCost;
            if (next != null && !_upgrade.interactable && canEdit) _skillDetail.text += "\n골드가 부족해요.";
            var state = _pets.GetSnapshot();
            bool passive = _selectedSkillId == PetSkillCatalogSO.PickupRange;
            _selectSkill.interactable = canEdit && !passive;
            _selectSkill.GetComponentInChildren<TextMeshProUGUI>().text = passive ? "항상 적용"
                : _selectedSkillId == PetSkillCatalogSO.Pickup ? (state.isAutoPickupEnabled ? "자동 줍기 끄기" : "자동 줍기 켜기")
                : state.SelectedSupportId == _selectedSkillId ? "선택한 도움" : "이 도움 받기";
        }
        private static string DescribeTrigger(PetSupportTriggerPolicy policy) => policy.kind switch
        {
            PetSupportTriggerKind.HealthBelow => $"체력이 {policy.threshold:P0} 이하일 때 도와줘요.",
            PetSupportTriggerKind.PerfectDefense => "완벽하게 피하거나 막아내면 도와줘요.",
            PetSupportTriggerKind.HeavyHitTaken => "큰 공격에 맞으면 도와줘요.",
            _ => ""
        };
        private static string Describe(string id, PetSkillLevel level)
        {
            if (level == null) return "정보가 없어요.";
            if (id == PetSkillCatalogSO.Pickup) return $"다음 아이템 탐색까지 {level.explorationValue:0.#}초";
            if (id == PetSkillCatalogSO.PickupRange) return $"주변 {level.explorationValue:0.#}m 탐색";
            var effect = level.Effect;
            if (id == PetSkillCatalogSO.Heal && effect != null) return $"최대 체력의 {effect.healMaxHealthRatio:P0} 회복";
            if (effect?.modifiers.Count != 1) return "정보가 없어요.";
            var modifier = effect.modifiers[0];
            float value = id == PetSkillCatalogSO.Heal ? modifier.coefficient : modifier.value;
            return id == PetSkillCatalogSO.Heal ? $"최대 체력의 {value:P0} 회복"
                : $"{(id == PetSkillCatalogSO.AttackBuff ? "공격력" : "방어력")} +{value:P0}, {effect.durationSeconds:0.#}초";
        }
        private void SelectSkill()
        {
            if (_selectedSkillId == PetSkillCatalogSO.Pickup) _pets.TrySetAutoPickup(!_pets.IsAutoPickupEnabled);
            else _pets.TrySetSupport(_selectedSkillId);
            Refresh(); SetDefaultFocus(_selectSkill, true);
        }
        private void UpgradeSkill()
        {
            var result = _pets.TryUpgrade(new PetUpgradeRequest(_selectedSkillId, _shownLevel, _shownCost, _shownRevision));
            _feedback = result switch
            {
                PetUpgradeResult.CommittedAndSaved => "한 단계 성장했어요.",
                PetUpgradeResult.CommittedUnsaved => "한 단계 성장했어요. 저장을 다시 시도해 주세요.",
                _ => "강화하지 못했어요. 레벨과 골드를 확인해 주세요."
            };
            Refresh(); FocusCurrentItem();
        }

        private void ConfigureFocusNavigation()
        {
            if (_tabs == null) return;
            UIFocusNavigation.ConfigureHorizontal(new[] { _toggle, _close }, false);
            var firstAction = _selectSkill != null && _selectSkill.interactable ? _selectSkill
                : _upgrade != null && _upgrade.interactable ? _upgrade : _close;
            if (_tabs.SelectedIndex == 1)
            {
                UIFocusNavigation.ConfigureVertical(_skillButtons, true);
                ConfigureSkillGraphNavigation(firstAction);
                var current = _skillButtons.Find(button => button.GetComponentInChildren<TextMeshProUGUI>().text.StartsWith("◆"));
                LinkAction(_selectSkill, current, _upgrade != null && _upgrade.interactable ? _upgrade : _toggle);
                LinkAction(_upgrade, current, _toggle);
                LinkBottom(_toggle, current); LinkBottom(_close, firstAction);
            }
            else
            {
                UIFocusNavigation.ConfigureGrid(_appearanceButtons, 2);
                for (int i = 0; i < _appearanceButtons.Count; i++)
                {
                    var item = _appearanceButtons[i]; var nav = item.navigation;
                    if (i % 2 == 0) nav.selectOnLeft = _model;
                    if (i + 2 >= _appearanceButtons.Count) nav.selectOnDown = _apply != null && _apply.interactable ? _apply : _toggle;
                    item.navigation = nav;
                }
                LinkAction(_model, null, _toggle);
                if (_appearanceButtons.Count > 0)
                {
                    var nav = _model.navigation; nav.selectOnRight = _appearanceButtons[0]; _model.navigation = nav;
                }
                LinkAction(_apply, _model, _toggle);
                LinkBottom(_toggle, _model); LinkBottom(_close, _apply != null && _apply.interactable ? _apply : _model);
            }
            if (_retrySave != null && _retrySave.gameObject.activeSelf)
            {
                var nav = _close.navigation; nav.selectOnUp = _retrySave; _close.navigation = nav;
                LinkAction(_retrySave, _toggle, _close);
            }
        }
        private static void LinkAction(Selectable item, Selectable left, Selectable down)
        {
            if (item == null) return;
            var navigation = item.navigation; navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnLeft = left; navigation.selectOnDown = down;
            navigation.selectOnUp = left; item.navigation = navigation;
        }
        private static void LinkBottom(Selectable item, Selectable up)
        {
            if (item == null) return;
            var navigation = item.navigation; navigation.selectOnUp = up; item.navigation = navigation;
        }
    }
}
