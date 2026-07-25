#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Data.Stat;
using UPlayGround.Manager;

namespace UPlayGround.UI.DevCheat
{
    /// <summary>UI_DevCheatPanel — 플레이어 스텟 탭(base 스탯 즉시 변경).</summary>
    public partial class UI_DevCheatPanel
    {
        private readonly Dictionary<AttributeId, TMP_InputField> _statInputs = new();
        private readonly Dictionary<AttributeId, TextMeshProUGUI> _growthRankTexts = new();
        private RectTransform _statContent;
        private TextMeshProUGUI _growthSummaryText;

        // static 필드 초기화로 두면 안 된다. static 생성자는 이 타입을 처음 건드리는 시점,
        // 즉 MonoBehaviour 역직렬화/생성 중에 실행되고 그 시점의 Resources.Load는
        // UnityException("Load is not allowed to be called from a MonoBehaviour constructor")로 막힌다.
        // (UPlayGroundAttributeDefaults.All → AttributeRegistry.Registry → Resources.Load)
        // 프로퍼티로 두면 첫 사용 시점(BuildStatTab, Awake 이후)에 평가된다.
        // 캐시하지 않는 이유: 탭을 열 때 1회만 쓰이고, 도메인 리로드를 끈 환경에서
        // static 캐시가 레지스트리 변경 뒤에도 낡은 값을 붙들고 있는 문제를 피한다.
        private static AttributeId[] EditableAttributes => UPlayGroundAttributeDefaults.All;

        private void BuildStatTab(RectTransform panel)
        {
            AddImage(panel.gameObject, PanelBg);
            var v = AddVLG(panel.gameObject, 8, 12);
            v.childForceExpandHeight = false;

            var title = MakeText(panel, "플레이어 스텟 (활성 캐릭터)", 22, Accent, TextAlignmentOptions.Left);
            SetSize(title.gameObject, minH: 34, prefH: 34);
            var hint = MakeText(panel, "값 입력 후 [적용]. MaxHealth 변경 시 HP가 최대치로 회복됩니다.", 14, TextSub, TextAlignmentOptions.Left);
            SetSize(hint.gameObject, minH: 26, prefH: 26);

            var listScroll = MakeScroll(panel, out _);
            SetSize(((RectTransform)listScroll.parent.parent).gameObject, flexH: 1);
            _statContent = listScroll;
            _statInputs.Clear();
            _growthRankTexts.Clear();

            BuildGrowthCheatSection();

            var rawTitle = MakeText(_statContent, "기본 스탯 직접 변경 (비영구)", 18, Accent, TextAlignmentOptions.Left);
            SetSize(rawTitle.gameObject, minH: 32, prefH: 32);

            foreach (AttributeId attributeId in EditableAttributes)
            {
                var row = NewRect("Row_" + attributeId.Value, _statContent);
                SetSize(row.gameObject, minH: 46, prefH: 46);
                AddImage(row.gameObject, RowBg);
                var rh = AddHLG(row.gameObject, 8, 8);
                rh.childForceExpandWidth = false;

                var label = MakeText(row, attributeId.Value, 16, TextMain);
                SetSize(label.gameObject, flexW: 1);

                var input = MakeInput(row, "0", null, TMP_InputField.ContentType.DecimalNumber);
                SetSize(input.gameObject, minW: 130, prefW: 130, minH: 36, prefH: 36);
                _statInputs[attributeId] = input;

                AttributeId captured = attributeId;
                var apply = MakeButton(
                    row, "적용", AccentBtn, () => ApplyAttribute(captured), 16);
                SetSize(apply.gameObject, minW: 76, prefW: 76);
            }
        }

        private void RefreshStatValues()
        {
            var cheat = CheatManager.Instance;
            if (cheat == null) return;

            foreach (var pair in _statInputs)
            {
                if (pair.Value != null)
                    pair.Value.SetTextWithoutNotify(
                        cheat.GetPlayerAttribute(pair.Key)
                            .ToString("0.###", CultureInfo.InvariantCulture));
            }

            RefreshGrowthCheatValues();
        }

        private void ApplyAttribute(AttributeId attributeId)
        {
            if (!_statInputs.TryGetValue(attributeId, out var input)
                || input == null)
                return;
            if (!float.TryParse(input.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                return;

            CheatManager.Instance?.SetPlayerAttribute(attributeId, value);
            RefreshStatValues();
        }

        private void BuildGrowthCheatSection()
        {
            var title = MakeText(_statContent, "휴식 성장 능력치 (저장 반영)", 18, Positive, TextAlignmentOptions.Left);
            SetSize(title.gameObject, minH: 32, prefH: 32);

            var pointRow = NewRect("GrowthPoints", _statContent);
            SetSize(pointRow.gameObject, minH: 46, prefH: 46);
            AddImage(pointRow.gameObject, RowBgAlt);
            var pointLayout = AddHLG(pointRow.gameObject, 8, 8);
            pointLayout.childForceExpandWidth = false;

            _growthSummaryText = MakeText(pointRow, "성장 포인트 -", 16, TextMain);
            SetSize(_growthSummaryText.gameObject, flexW: 1);
            MakeGrowthButton(pointRow, "+1", () => ChangeGrowthPoints(1));
            MakeGrowthButton(pointRow, "+10", () => ChangeGrowthPoints(10));
            MakeGrowthButton(pointRow, "초기화", ResetGrowthPoints);

            foreach (AttributeId attribute in GrowthAttributeCatalog.LegacyOrderedIds)
            {
                var row = NewRect("Growth_" + attribute, _statContent);
                SetSize(row.gameObject, minH: 48, prefH: 48);
                AddImage(row.gameObject, RowBg);
                var layout = AddHLG(row.gameObject, 8, 8);
                layout.childForceExpandWidth = false;

                var label = MakeText(row, GetGrowthAttributeName(attribute), 16, TextMain);
                SetSize(label.gameObject, minW: 100, prefW: 100);

                var rankText = MakeText(row, "Rank -", 15, Accent, TextAlignmentOptions.Center);
                SetSize(rankText.gameObject, flexW: 1);
                _growthRankTexts[attribute] = rankText;

                AttributeId captured = attribute;
                MakeGrowthButton(row, "-1", () => ChangeGrowthRank(captured, -1));
                MakeGrowthButton(row, "+1", () => ChangeGrowthRank(captured, 1));
                MakeGrowthButton(row, "MAX", () => MaxGrowthRank(captured));
            }

            var hint = MakeText(
                _statContent,
                "랭크 치트는 포인트를 소비하지 않습니다. 랭크를 올리면 콤보·스킬 마일스톤도 즉시 해금됩니다.",
                13,
                TextSub,
                TextAlignmentOptions.Left);
            SetSize(hint.gameObject, minH: 42, prefH: 42);
        }

        private void RefreshGrowthCheatValues()
        {
            PartyManager pm = PartyManager.Instance;
            if (pm == null) return;
            CharacterActorType type = pm.ActiveCharacterType;

            if (_growthSummaryText != null)
                _growthSummaryText.text = $"{type}  |  성장 포인트 {pm.GetGrowthPoints(type)}";

            PartyMemberGrowthSO growth = pm.GetGrowthData(type);
            foreach (var pair in _growthRankTexts)
            {
                if (pair.Value == null) continue;
                int rank = pm.GetGrowthRank(type, pair.Key);
                int maxRank = 0;
                float value = 0f;
                if (growth != null && growth.TryGetInvestmentRule(pair.Key, out GrowthInvestmentRule rule))
                {
                    maxRank = Mathf.Max(1, rule.maxRank);
                    value = rule.flatPerRank * rank;
                }
                pair.Value.text = $"Rank {rank}/{maxRank}  (+{value:0.###})";
            }
        }

        private void ChangeGrowthPoints(int amount)
        {
            PartyManager pm = PartyManager.Instance;
            if (pm == null) return;
            CheatManager.Instance?.AddGrowthPoints(pm.ActiveCharacterType, amount);
            RefreshStatValues();
        }

        private void ResetGrowthPoints()
        {
            PartyManager pm = PartyManager.Instance;
            if (pm == null) return;
            int points = pm.GetGrowthPoints(pm.ActiveCharacterType);
            if (points > 0)
                CheatManager.Instance?.AddGrowthPoints(pm.ActiveCharacterType, -points);
            RefreshStatValues();
        }

        private void ChangeGrowthRank(AttributeId attribute, int delta)
        {
            PartyManager pm = PartyManager.Instance;
            if (pm == null) return;
            CharacterActorType type = pm.ActiveCharacterType;
            CheatManager.Instance?.SetGrowthRank(type, attribute, pm.GetGrowthRank(type, attribute) + delta);
            RefreshStatValues();
        }

        private void MaxGrowthRank(AttributeId attribute)
        {
            PartyManager pm = PartyManager.Instance;
            if (pm == null) return;
            CharacterActorType type = pm.ActiveCharacterType;
            PartyMemberGrowthSO growth = pm.GetGrowthData(type);
            if (growth == null || !growth.TryGetInvestmentRule(attribute, out GrowthInvestmentRule rule)) return;
            CheatManager.Instance?.SetGrowthRank(type, attribute, Mathf.Max(1, rule.maxRank));
            RefreshStatValues();
        }

        private void MakeGrowthButton(Transform parent, string label, Action onClick)
        {
            var button = MakeButton(parent, label, BtnBg, onClick, 14);
            SetSize(button.gameObject, minW: 62, prefW: 72);
        }

        private static string GetGrowthAttributeName(AttributeId attribute) =>
            GrowthAttributeCatalog.GetDisplayName(attribute);
    }
}
#endif
