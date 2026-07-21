using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Save;
using UPlayGround.Data.World;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 게임 시간 흐름 제어 + 플레이 시간 누적 + 인게임 시계(낮밤/일수).
    ///
    /// timeScale 소유권 모델:
    ///   여러 시스템(HitStop, TimeScaleEvent, PlayerGuard 등)이 동시에
    ///   timeScale 감속을 요청할 수 있다. 각 요청자는 고유 id로 등록하고,
    ///   활성 요청 중 가장 낮은 scale(가장 강한 효과)이 실제로 적용된다.
    ///   마지막 요청자가 해제하면 자동으로 1.0으로 복구된다.
    ///
    /// 인게임 시계:
    ///   unscaledDeltaTime 기반이므로 히트스톱/슬로우에는 흐르고,
    ///   IsPaused(메뉴 등)일 때는 설정(pauseStopsWorldTime)에 따라 멈춘다.
    ///   재스폰/조명 시스템은 OnGameMinuteChanged(정수 분 단위)를 구독한다.
    /// </summary>
    public class GameTimeManager : BaseManager<GameTimeManager>, IManager, IUpdatableManager, ISaveable,
        IGameTimeService
    {
        private const string SettingsKey = "WorldTimeSettings";

        public static event Action<bool> OnPauseChanged;

        /// <summary> 정수 인게임 분이 바뀔 때만 발행. (경과 일수, 하루 중 분) </summary>
        public event Action<int, float> OnGameMinuteChanged;

        /// <summary> 낮밤 구간(Dawn/Day/Dusk/Night)이 바뀔 때 발행. </summary>
        public event Action<DayPeriod> OnDayPeriodChanged;

        public bool IsPaused          { get; private set; }
        public float TotalPlaySeconds { get; private set; }

        // ── 인게임 시계 ──
        /// <summary> 게임 시작부터 누적된 인게임 분. </summary>
        public float TotalGameMinutes { get; private set; }
        /// <summary> 경과 일수 (0부터). </summary>
        public int CurrentDay => (int)(TotalGameMinutes / WorldTimeSettingsSO.MinutesPerDay);
        /// <summary> 하루 중 시각 (0~1440분). </summary>
        public float MinuteOfDay => Mathf.Repeat(TotalGameMinutes, WorldTimeSettingsSO.MinutesPerDay);
        /// <summary> 하루 진행도 (0~1). 자정=0, 정오=0.5. </summary>
        public float DayProgress01 => MinuteOfDay / WorldTimeSettingsSO.MinutesPerDay;
        public DayPeriod CurrentDayPeriod { get; private set; } = DayPeriod.Day;
        public bool IsNight => CurrentDayPeriod == DayPeriod.Night;

        /// <summary> 로드된 시간 설정. 에셋이 없으면 null이며 코드 기본값으로 동작한다. </summary>
        public WorldTimeSettingsSO Settings { get; private set; }

        /// <summary>
        /// 인게임 시계 배속(치트/연출용). 설정 SO를 건드리지 않고 런타임에서만 곱해지며 저장되지 않는다.
        /// 0이면 시계 정지. 음수는 0으로 클램프.
        /// </summary>
        public float WorldClockMultiplier
        {
            get => _worldClockMultiplier;
            set => _worldClockMultiplier = Mathf.Max(0f, value);
        }
        private float _worldClockMultiplier = 1f;

        private int _lastWholeGameMinute = -1;

        // 새 게임/세이브 로드/치트가 시각을 확정했는지. 확정 전이라면 설정 로드 완료 시 시작 시각을 SO 값으로 재적용한다.
        private bool _clockExplicitlySet;

        // key: 요청자 id, value: 요청한 scale
        private readonly Dictionary<int, float> _requests = new Dictionary<int, float>();
        private int  _nextId        = 0;
        private float _activeScale  = 1f; // 현재 적용된 scale (Pause 해제 시 복구용)

        #region IManager

        public void Init()
        {
            SaveManager.Instance.RegisterSaveable(this);
            TotalGameMinutes = DefaultStartMinuteOfDay;
            LoadSettingsAsync().Forget();
        }

        public void AfterInit() { }
        public void Dispose() => SetPause(false);
        public void OnFixedUpdate() { }
        public void OnLateUpdate()  { }
        public void OnSceneChanged(string sceneType) { }

        public void OnUpdate()
        {
            if (!IsPaused)
                TotalPlaySeconds += Time.unscaledDeltaTime;

            // 시계가 멈춘 상태(일시정지)에서도 delta 0으로 호출해, SetTotalGameMinutes(치트/로드)가
            // 예약한 분/구간 이벤트 재발행이 유실되지 않게 한다.
            bool worldClockRuns = !IsPaused || !PauseStopsWorldTime;
            AdvanceGameClock(worldClockRuns ? Time.unscaledDeltaTime : 0f);
        }

        #endregion

        #region 인게임 시계

        private float GameMinutesPerRealSecond => Settings != null ? Settings.gameMinutesPerRealSecond : 1f;
        private float DefaultStartMinuteOfDay  => Settings != null ? Settings.startMinuteOfDay : 8f * 60f;
        private bool  PauseStopsWorldTime      => Settings == null || Settings.pauseStopsWorldTime;

        private void AdvanceGameClock(float unscaledDelta)
        {
            float rate = GameMinutesPerRealSecond * _worldClockMultiplier;
            if (unscaledDelta > 0f && rate > 0f)
                TotalGameMinutes += unscaledDelta * rate;

            // 배속 0(치트) 등으로 시계가 흐르지 않아도 외부에서 시각이 바뀌었을 수 있으므로 항상 점검한다.
            NotifyClockIfChanged();
        }

        /// <summary> 정수 분 경계를 넘었을 때만 분/구간 이벤트를 발행한다. </summary>
        private void NotifyClockIfChanged()
        {
            int wholeMinute = (int)TotalGameMinutes;
            if (wholeMinute == _lastWholeGameMinute) return;
            _lastWholeGameMinute = wholeMinute;

            OnGameMinuteChanged?.Invoke(CurrentDay, MinuteOfDay);

            DayPeriod period = GetPeriod(MinuteOfDay);
            if (period != CurrentDayPeriod)
            {
                CurrentDayPeriod = period;
                OnDayPeriodChanged?.Invoke(period);
            }
        }

        private DayPeriod GetPeriod(float minuteOfDay)
        {
            if (Settings != null) return Settings.GetPeriod(minuteOfDay);

            // 설정 에셋 미로드 시 기본 구간: Dawn 05-07 / Day 07-18 / Dusk 18-20 / Night 20-05
            if (minuteOfDay < 5f * 60f) return DayPeriod.Night;
            if (minuteOfDay < 7f * 60f) return DayPeriod.Dawn;
            if (minuteOfDay < 18f * 60f) return DayPeriod.Day;
            if (minuteOfDay < 20f * 60f) return DayPeriod.Dusk;
            return DayPeriod.Night;
        }

        /// <summary> 인게임 누적 분을 직접 설정한다(세이브 로드/치트). 이벤트는 다음 프레임 경계에서 발행된다. </summary>
        public void SetTotalGameMinutes(float minutes)
        {
            TotalGameMinutes = Mathf.Max(0f, minutes);
            _lastWholeGameMinute = -1; // 다음 갱신에서 무조건 분/구간 이벤트 재발행
            _clockExplicitlySet = true;
            CurrentDayPeriod = GetPeriod(MinuteOfDay);
        }

        /// <summary> "Day N HH:MM" 형태의 인게임 시각 문자열. </summary>
        public string FormatGameTime()
        {
            int minuteOfDay = (int)MinuteOfDay;
            return $"Day {CurrentDay + 1} {minuteOfDay / 60:D2}:{minuteOfDay % 60:D2}";
        }

        private async UniTask LoadSettingsAsync()
        {
            try
            {
                Settings = await AssetManager.Instance.TryLoadGlobalAsync<WorldTimeSettingsSO>(
                    SettingsKey, nameof(GameTimeManager));

                if (Settings == null)
                {
                    Debug.Log("[GameTimeManager] WorldTimeSettings 에셋이 없어 기본값으로 동작합니다.");
                    return;
                }

                // Init은 설정 로드 전에 코드 기본값(08:00)으로 시작 시각을 잡는다.
                // 아직 새 게임/로드가 시각을 확정하지 않았다면 SO의 시작 시각으로 재적용한다.
                if (!_clockExplicitlySet)
                    TotalGameMinutes = DefaultStartMinuteOfDay;
                CurrentDayPeriod = GetPeriod(MinuteOfDay);
            }
            catch (Exception)
            {
                // 설정 에셋이 Addressables에 없으면 코드 기본값으로 동작한다(에러 아님).
                Debug.Log("[GameTimeManager] WorldTimeSettings 에셋이 없어 기본값으로 동작합니다.");
            }
        }

        #endregion

        #region 저장 / 복원 (ISaveable)

        public void ExportSaveData(GameSaveData saveData)
        {
            if (saveData == null) return;
            var time = saveData.time ??= new TimeSaveData();
            time.totalPlaySeconds = TotalPlaySeconds;
            time.totalGameMinutes = TotalGameMinutes;
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            var time = saveData?.time;
            if (time == null) return;

            SetTotalPlaySeconds(time.totalPlaySeconds);

            // 구버전 세이브(totalGameMinutes 미기록)는 시작 시각으로 초기화한다.
            SetTotalGameMinutes(time.totalGameMinutes >= 0f ? time.totalGameMinutes : DefaultStartMinuteOfDay);
        }

        public void ResetForNewGame()
        {
            TotalPlaySeconds = 0f;
            SetTotalGameMinutes(DefaultStartMinuteOfDay);
        }

        #endregion

        #region Pause

        public void SetPause(bool pause)
        {
            if (IsPaused == pause) return;

            IsPaused = pause;
            // Pause가 최우선 — 재개 시엔 활성 요청 scale로 복구
            Time.timeScale = pause ? 0f : _activeScale;
            OnPauseChanged?.Invoke(IsPaused);
        }

        public void TogglePause() => SetPause(!IsPaused);

        #endregion

        #region TimeScale 요청 API

        /// <summary>
        /// timeScale 감속 요청을 등록한다.
        /// 반환된 id를 Release()에 전달해서 해제해야 한다.
        /// </summary>
        /// <param name="scale">목표 timeScale (낮을수록 강한 효과)</param>
        /// <returns>이 요청을 식별하는 고유 id</returns>
        public int Request(float scale)
        {
            int id = _nextId++;
            _requests[id] = Mathf.Clamp(scale, 0.001f, 1f);
            ApplyLowest();
            return id;
        }

        /// <summary>
        /// 기존 요청 id를 유지한 채 scale만 갱신한다.
        /// HitStop 복귀 램프처럼 같은 소유자가 시간에 따라 강도를 바꿀 때 사용한다.
        /// </summary>
        public void UpdateRequestScale(int id, float scale)
        {
            if (!_requests.ContainsKey(id)) return;

            _requests[id] = Mathf.Clamp(scale, 0.001f, 1f);
            ApplyLowest();
        }

        /// <summary>
        /// 등록된 요청을 해제한다.
        /// 남은 요청이 없으면 timeScale이 1.0으로 복구된다.
        /// </summary>
        public void Release(int id)
        {
            if (!_requests.Remove(id)) return;
            ApplyLowest();
        }

        /// <summary>
        /// 강제 전체 초기화 (씬 전환, OnSceneChanged 등)
        /// </summary>
        public void ReleaseAll()
        {
            _requests.Clear();
            ApplyLowest();
        }

        public bool IsSlowed => _activeScale < 1f;

        #endregion

        #region 내부

        private void ApplyLowest()
        {
            // 활성 요청 중 가장 낮은 scale 선택 → 없으면 1.0
            float lowest = 1f;
            foreach (var v in _requests.Values)
                if (v < lowest) lowest = v;

            _activeScale = lowest;

            if (!IsPaused)
                Time.timeScale = _activeScale;
        }

        #endregion

        #region 유틸

        public void SetTotalPlaySeconds(float seconds) => TotalPlaySeconds = Mathf.Max(0f, seconds);

        public string FormatPlayTime()
        {
            int total = (int)TotalPlaySeconds;
            return $"{total / 3600:D2}:{total % 3600 / 60:D2}:{total % 60:D2}";
        }

        #endregion
    }
}
