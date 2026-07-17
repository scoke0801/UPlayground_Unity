#if UNITY_EDITOR || DEVELOPMENT_BUILD
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI.DevCheat
{
    /// <summary>UI_DevCheatPanel — 전투 탭(항상 패리/주변 몬스터 처치).</summary>
    public partial class UI_DevCheatPanel
    {
        private Toggle _alwaysParryToggle;
        private float  _killNearbyRadius = 30f;

        private void BuildCombatTab(RectTransform panel)
        {
            AddImage(panel.gameObject, PanelBg);
            var v = AddVLG(panel.gameObject, 10, 12);
            v.childForceExpandHeight = false;

            var title = MakeText(panel, "전투", 22, Accent, TextAlignmentOptions.Left);
            SetSize(title.gameObject, minH: 34, prefH: 34);

            // ── 항상 패리 ──
            _alwaysParryToggle = MakeToggle(
                panel,
                "항상 패리 (어떤 상태에서도 적의 공격을 패리)",
                CheatManager.Instance != null && CheatManager.Instance.IsAlwaysParryEnabled,
                on => CheatManager.Instance?.SetAlwaysParry(on));
            SetSize(_alwaysParryToggle.gameObject, minH: 48, prefH: 48);

            // ── 주변 몬스터 처치 ──
            var killLabel = MakeText(
                panel,
                "주변 몬스터 처치 — 활성 캐릭터 반경 내 몬스터를 즉시 처치 (드랍/경험치/퀘스트 정상 처리)",
                16, TextSub, TextAlignmentOptions.Left);
            SetSize(killLabel.gameObject, minH: 24, prefH: 24);

            var killRow = NewRect("KillRow", panel);
            SetSize(killRow.gameObject, minH: 46, prefH: 46);
            AddImage(killRow.gameObject, RowBg);
            var h = AddHLG(killRow.gameObject, 8, 6);
            h.childForceExpandWidth = false;

            var radiusLabel = MakeText(killRow, "반경(m)", 16, TextSub, TextAlignmentOptions.Center);
            SetSize(radiusLabel.gameObject, minW: 70, prefW: 70);

            var radiusInput = MakeInput(
                killRow,
                "30",
                text =>
                {
                    if (float.TryParse(text, out float radius) && radius > 0f)
                        _killNearbyRadius = radius;
                },
                TMP_InputField.ContentType.DecimalNumber);
            SetSize(radiusInput.gameObject, minW: 110, prefW: 110);
            radiusInput.SetTextWithoutNotify(_killNearbyRadius.ToString("0.#"));

            var killBtn = MakeButton(
                killRow,
                "처치",
                DangerBtn,
                () => CheatManager.Instance?.KillNearbyMonsters(_killNearbyRadius),
                16);
            SetSize(killBtn.gameObject, minW: 100, prefW: 100);
        }

        // 탭이 다시 표시될 때 외부(에디터 치트 콘솔 등)에서 바뀐 상태를 반영한다.
        private void RefreshCombatTab()
        {
            if (_alwaysParryToggle != null && CheatManager.Instance != null)
                _alwaysParryToggle.SetIsOnWithoutNotify(CheatManager.Instance.IsAlwaysParryEnabled);
        }
    }
}
#endif
