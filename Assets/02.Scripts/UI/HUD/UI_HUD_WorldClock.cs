using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 경과 일차·시각·시간대와 현재 환경의 날씨를 표시한다.
    /// </summary>
    public class UI_HUD_WorldClock : UI_Base
    {
        [Header("표시")]
        [SerializeField] private TextMeshProUGUI _timeText;   // "08:24"
        [SerializeField] private TextMeshProUGUI _dayText;    // "1일차 · 낮"

        [Header("날씨")]
        [SerializeField] private Image _weatherIcon;
        [SerializeField] private Sprite _clearDayIcon;
        [SerializeField] private Sprite _clearNightIcon;
        [SerializeField] private Sprite _cloudyIcon;
        [SerializeField] private Sprite _rainIcon;
        [SerializeField, Range(0.01f, 1f)] private float _rainThreshold = 0.1f;
        [SerializeField, Range(0.01f, 1f)] private float _cloudThreshold = 0.5f;
        [SerializeField, Min(0.05f)] private float _weatherRefreshSeconds = 0.25f;
        [SerializeField, Min(0f)] private float _weatherFadeSeconds = 0.15f;

        private IGameTimeService _timeService;
        private float _nextWeatherRefresh;
        private Tween _weatherTween;

        #region UI_Base

        protected override void OnInit()
        {
            _canCloseWithEsc = false;
        }

        protected override void OnShow()
        {
            if (_timeService == null && Services.TryGet<IGameTimeService>(out var time))
            {
                time.OnGameMinuteChanged += OnGameMinuteChanged;
                _timeService = time;
            }

            Refresh();
            RefreshWeather(animate: false);
            _nextWeatherRefresh = Time.unscaledTime + _weatherRefreshSeconds;
        }

        protected override void Update()
        {
            base.Update();
            if (!IsVisible || Time.unscaledTime < _nextWeatherRefresh) return;
            _nextWeatherRefresh = Time.unscaledTime + _weatherRefreshSeconds;
            // 씬 환경의 교체와 연속 보간을 따라가되 매 프레임 조회하지 않는다.
            RefreshWeather(animate: true);
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
            _weatherTween?.Kill();
            _weatherTween = null;
            if (_timeService != null)
                _timeService.OnGameMinuteChanged -= OnGameMinuteChanged;
            _timeService = null;
        }

        private void OnGameMinuteChanged(int day, float minuteOfDay) => Refresh();

        private void Refresh()
        {
            var time = Svc.GameTime;
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

        private void RefreshWeather(bool animate)
        {
            if (_weatherIcon == null) return;
            Sprite sprite = null;
            if (Services.TryGet<IUIWeatherService>(out var weather) && weather.IsReady
                && Services.TryGet<IGameTimeService>(out var time))
            {
                sprite = ResolveWeatherIcon(weather.RainIntensity, weather.CloudCoverage, time.CurrentDayPeriod);
            }
            if (animate && _weatherIcon.sprite == sprite && _weatherIcon.enabled == (sprite != null)) return;

            _weatherTween?.Kill();
            _weatherTween = null;
            _weatherIcon.sprite = sprite;
            _weatherIcon.enabled = sprite != null;
            if (sprite == null) return;

            Color color = _weatherIcon.color;
            color.a = animate && _weatherFadeSeconds > 0 ? 0 : 1;
            _weatherIcon.color = color;
            if (color.a == 1) return;
            _weatherTween = DOTween.To(() => _weatherIcon.color.a, alpha =>
            {
                Color current = _weatherIcon.color;
                current.a = alpha;
                _weatherIcon.color = current;
            }, 1f, _weatherFadeSeconds).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        private Sprite ResolveWeatherIcon(float rain, float cloud, DayPeriod period)
        {
            if (rain >= _rainThreshold) return _rainIcon;
            if (cloud >= _cloudThreshold) return _cloudyIcon;
            return period == DayPeriod.Night ? _clearNightIcon : _clearDayIcon;
        }
    }
}
