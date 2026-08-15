#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using TMPro;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.UI.DevCheat
{
    /// <summary>UI_System_DevCheatPanel — 파티원 탭(해금/레벨/경험치/회복/스왑 쿨).</summary>
    public partial class UI_System_DevCheatPanel
    {
        private RectTransform _partyContent;
        private const long ExpGrantStep = 500;

        private void BuildPartyTab(RectTransform panel)
        {
            AddImage(panel.gameObject, PanelBg);
            var v = AddVLG(panel.gameObject, 8, 12);
            v.childForceExpandHeight = false;

            var title = MakeText(panel, "파티원", 22, Accent, TextAlignmentOptions.Left);
            SetSize(title.gameObject, minH: 34, prefH: 34);

            // 상단 전역 버튼
            var top = NewRect("PartyTop", panel);
            SetSize(top.gameObject, minH: 46, prefH: 46);
            var th = AddHLG(top.gameObject, 8, 0, forceExpandWidth: true);
            MakeButton(top, "파티 전체 회복 + 부활", new Color(0.16f, 0.42f, 0.30f),
                () => { CheatManager.Instance?.HealParty(true); }, 16);
            MakeButton(top, "스왑 쿨타임 초기화", BtnBg,
                () => { CheatManager.Instance?.ResetSwapCooldowns(); }, 16);

            var listScroll = MakeScroll(panel, out _);
            SetSize(((RectTransform)listScroll.parent.parent).gameObject, flexH: 1);
            _partyContent = listScroll;
        }

        private void RefreshPartyList()
        {
            if (_partyContent == null) return;
            ClearChildren(_partyContent);

            var pm = PartyManager.Instance;
            if (pm == null)
            {
                MakeText(_partyContent, "PartyManager 준비 대기 중…", 16, TextSub);
                return;
            }

            int rowIndex = 0;
            foreach (CharacterActorType type in Enum.GetValues(typeof(CharacterActorType)))
            {
                if (type == CharacterActorType.None || type == CharacterActorType.H09) continue;

                bool unlocked = Contains(pm.Roster, type);
                int level = pm.GetLevel(type);

                var row = NewRect("Row_" + type, _partyContent);
                SetSize(row.gameObject, minH: 50, prefH: 50);
                AddImage(row.gameObject, rowIndex++ % 2 == 0 ? RowBg : RowBgAlt);
                var rh = AddHLG(row.gameObject, 6, 8);
                rh.childForceExpandWidth = false;

                var nameT = MakeText(row, type.ToString(), 17, unlocked ? TextMain : TextSub);
                SetSize(nameT.gameObject, flexW: 1);

                var lvT = MakeText(row, $"Lv.{level}", 16, Accent, TextAlignmentOptions.Center);
                SetSize(lvT.gameObject, minW: 64, prefW: 64);

                CharacterActorType captured = type;
                if (!unlocked)
                {
                    var unlockBtn = MakeButton(row, "해금", AccentBtn, () => { CheatManager.Instance?.UnlockCharacter(captured); RefreshPartyList(); }, 15);
                    SetSize(unlockBtn.gameObject, minW: 90, prefW: 120);
                }
                else
                {
                    MakeSmall(row, "Lv-", () => { CheatManager.Instance?.SetLevel(captured, Mathf.Max(1, pm.GetLevel(captured) - 1)); RefreshPartyList(); });
                    MakeSmall(row, "Lv+", () => { CheatManager.Instance?.SetLevel(captured, pm.GetLevel(captured) + 1); RefreshPartyList(); });
                    MakeSmall(row, $"EXP+{ExpGrantStep}", () => { CheatManager.Instance?.GrantExp(captured, ExpGrantStep); RefreshPartyList(); });
                }
            }
        }

        private void MakeSmall(Transform parent, string label, Action onClick)
        {
            var btn = MakeButton(parent, label, BtnBg, onClick, 14);
            SetSize(btn.gameObject, minW: 68, prefW: 92);
        }

        private static bool Contains(System.Collections.Generic.IReadOnlyList<CharacterActorType> list, CharacterActorType type)
        {
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++)
                if (list[i] == type) return true;
            return false;
        }
    }
}
#endif
