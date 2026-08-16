#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Stat;
using UPlayGround.Manager;

namespace UPlayGround.UI.DevCheat
{
    /// <summary>UI_System_DevCheatPanel — 플레이어 스텟 탭(base 스탯 즉시 변경).</summary>
    public partial class UI_System_DevCheatPanel
    {
        private readonly Dictionary<AttributeId, TMP_InputField> _statInputs = new();
        private RectTransform _statContent;

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

    }
}
#endif
