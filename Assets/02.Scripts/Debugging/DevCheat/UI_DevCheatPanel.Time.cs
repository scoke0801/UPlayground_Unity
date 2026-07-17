#if UNITY_EDITOR || DEVELOPMENT_BUILD
using TMPro;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.UI.DevCheat
{
    /// <summary>UI_DevCheatPanel — 시간 탭(스킵/시각 이동/배속).</summary>
    public partial class UI_DevCheatPanel
    {
        private TextMeshProUGUI _timeInfoText;
        private bool _timeEventSubscribed;

        private void BuildTimeTab(RectTransform panel)
        {
            AddImage(panel.gameObject, PanelBg);
            var v = AddVLG(panel.gameObject, 10, 12);
            v.childForceExpandHeight = false;

            var title = MakeText(panel, "시간", 22, Accent, TextAlignmentOptions.Left);
            SetSize(title.gameObject, minH: 34, prefH: 34);

            // 현재 시각 표시
            var info = NewRect("TimeInfo", panel);
            SetSize(info.gameObject, minH: 44, prefH: 44);
            AddImage(info.gameObject, RowBg);
            _timeInfoText = MakeText(info, "-", 20, TextMain, TextAlignmentOptions.Center);

            // ── 시간 스킵 ──
            var skipLabel = MakeText(panel, "시간 스킵", 16, TextSub, TextAlignmentOptions.Left);
            SetSize(skipLabel.gameObject, minH: 24, prefH: 24);

            var skipRow = NewRect("SkipRow", panel);
            SetSize(skipRow.gameObject, minH: 46, prefH: 46);
            AddHLG(skipRow.gameObject, 8, 0, forceExpandWidth: true);
            MakeTimeButton(skipRow, "+10분", () => CheatManager.Instance?.AdvanceGameMinutes(10f));
            MakeTimeButton(skipRow, "+1시간", () => CheatManager.Instance?.AdvanceGameMinutes(60f));
            MakeTimeButton(skipRow, "+6시간", () => CheatManager.Instance?.AdvanceGameMinutes(6f * 60f));
            MakeTimeButton(skipRow, "+1일", () => CheatManager.Instance?.AdvanceGameMinutes(24f * 60f));

            // ── 시각 이동 (항상 앞으로) ──
            var jumpLabel = MakeText(panel, "시각 이동 (다음 해당 시각으로)", 16, TextSub, TextAlignmentOptions.Left);
            SetSize(jumpLabel.gameObject, minH: 24, prefH: 24);

            var jumpRow = NewRect("JumpRow", panel);
            SetSize(jumpRow.gameObject, minH: 46, prefH: 46);
            AddHLG(jumpRow.gameObject, 8, 0, forceExpandWidth: true);
            MakeTimeButton(jumpRow, "아침 08:00", () => CheatManager.Instance?.SkipToMinuteOfDay(8f * 60f));
            MakeTimeButton(jumpRow, "정오 12:00", () => CheatManager.Instance?.SkipToMinuteOfDay(12f * 60f));
            MakeTimeButton(jumpRow, "황혼 19:00", () => CheatManager.Instance?.SkipToMinuteOfDay(19f * 60f));
            MakeTimeButton(jumpRow, "밤 22:00", () => CheatManager.Instance?.SkipToMinuteOfDay(22f * 60f));

            // ── 시계 배속 ──
            var speedLabel = MakeText(panel, "시계 배속 (저장 안 됨)", 16, TextSub, TextAlignmentOptions.Left);
            SetSize(speedLabel.gameObject, minH: 24, prefH: 24);

            var speedRow = NewRect("SpeedRow", panel);
            SetSize(speedRow.gameObject, minH: 46, prefH: 46);
            AddHLG(speedRow.gameObject, 8, 0, forceExpandWidth: true);
            MakeTimeButton(speedRow, "정지", () => CheatManager.Instance?.SetWorldClockMultiplier(0f));
            MakeTimeButton(speedRow, "x1", () => CheatManager.Instance?.SetWorldClockMultiplier(1f));
            MakeTimeButton(speedRow, "x10", () => CheatManager.Instance?.SetWorldClockMultiplier(10f));
            MakeTimeButton(speedRow, "x60", () => CheatManager.Instance?.SetWorldClockMultiplier(60f));
            MakeTimeButton(speedRow, "x240", () => CheatManager.Instance?.SetWorldClockMultiplier(240f));

            // 분 단위 자동 갱신 (패널이 비활성이어도 텍스트 세팅은 무해)
            if (!_timeEventSubscribed && GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnGameMinuteChanged += OnCheatTimeMinuteChanged;
                _timeEventSubscribed = true;
            }
        }

        private void MakeTimeButton(Transform parent, string label, System.Action onClick)
        {
            MakeButton(parent, label, BtnBg, () =>
            {
                onClick?.Invoke();
                RefreshTimeInfo();
            }, 15);
        }

        private void OnCheatTimeMinuteChanged(int day, float minuteOfDay) => RefreshTimeInfo();

        private void RefreshTimeInfo()
        {
            if (_timeInfoText == null) return;

            var time = GameTimeManager.Instance;
            if (time == null)
            {
                _timeInfoText.text = "GameTimeManager 준비 대기 중…";
                return;
            }

            _timeInfoText.text =
                $"{time.FormatGameTime()}   ·   {GetPeriodLabel(time)}   ·   배속 x{time.WorldClockMultiplier:0.##}";
        }

        private static string GetPeriodLabel(GameTimeManager time) => time.CurrentDayPeriod switch
        {
            UPlayGround.Data.EnumType.DayPeriod.Dawn => "새벽",
            UPlayGround.Data.EnumType.DayPeriod.Day => "낮",
            UPlayGround.Data.EnumType.DayPeriod.Dusk => "황혼",
            UPlayGround.Data.EnumType.DayPeriod.Night => "밤",
            _ => string.Empty,
        };

        protected override void OnDispose()
        {
            if (_timeEventSubscribed && GameTimeManager.Instance != null)
                GameTimeManager.Instance.OnGameMinuteChanged -= OnCheatTimeMinuteChanged;
            _timeEventSubscribed = false;

            base.OnDispose();
        }
    }
}
#endif
