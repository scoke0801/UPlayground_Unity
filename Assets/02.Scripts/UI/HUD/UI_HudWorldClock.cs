using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 인게임 시계 HUD. 경과 일차 / 하루 중 시각(HH:MM) / 시간대(새벽·낮·황혼·밤)를 표시한다.
    /// GameTimeManager.OnGameMinuteChanged(정수 분 단위)를 구독하므로 매 프레임 갱신하지 않는다.
    /// UI_GamePlay가 다른 HUD와 함께 표시/숨김을 관리한다.
    /// </summary>
    public class UI_HudWorldClock : UI_Base
    {
        [Header("표시")]
        [SerializeField] private TextMeshProUGUI _timeText;   // "08:24"
        [SerializeField] private TextMeshProUGUI _dayText;    // "1일차 · 낮"

        [Header("시간대 아이콘 색")]
        [SerializeField] private Color _dawnColor  = new Color(0.95f, 0.70f, 0.50f, 1f);
        [SerializeField] private Color _dayColor   = new Color(1.00f, 0.92f, 0.55f, 1f);
        [SerializeField] private Color _duskColor  = new Color(0.95f, 0.45f, 0.35f, 1f);
        [SerializeField] private Color _nightColor = new Color(0.45f, 0.55f, 0.95f, 1f);

        private bool _subscribed;

        #region UI_Base

        protected override void OnInit()
        {
            _canCloseWithEsc = false;
        }

        protected override void OnShow()
        {
            var time = GameTimeManager.Instance;
            if (time != null && !_subscribed)
            {
                time.OnGameMinuteChanged += OnGameMinuteChanged;
                _subscribed = true;
            }

            Refresh();
        }

        protected override void OnHide()
        {
            Unsubscribe();
        }

        protected override void OnClose()
        {
            Unsubscribe();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            base.OnDispose();
        }

        #endregion

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;

            var time = GameTimeManager.Instance;
            if (time != null)
                time.OnGameMinuteChanged -= OnGameMinuteChanged;
        }

        private void OnGameMinuteChanged(int day, float minuteOfDay) => Refresh();

        private void Refresh()
        {
            var time = GameTimeManager.Instance;
            if (time == null) return;

            int minuteOfDay = (int)time.MinuteOfDay;
            if (_timeText != null)
                _timeText.text = $"{minuteOfDay / 60:D2}:{minuteOfDay % 60:D2}";

            if (_dayText != null)
                _dayText.text = $"{time.CurrentDay + 1}일차 {GetPeriodLabel(time.CurrentDayPeriod)}";
        }

        private static string GetPeriodLabel(DayPeriod period) => period switch
        {
            DayPeriod.Dawn => "새벽",
            DayPeriod.Day => "낮",
            DayPeriod.Dusk => "황혼",
            DayPeriod.Night => "밤",
            _ => string.Empty,
        };

        private Color GetPeriodColor(DayPeriod period) => period switch
        {
            DayPeriod.Dawn => _dawnColor,
            DayPeriod.Dusk => _duskColor,
            DayPeriod.Night => _nightColor,
            _ => _dayColor,
        };
    }
}
