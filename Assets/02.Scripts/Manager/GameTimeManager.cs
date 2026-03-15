using System;
using UnityEngine;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 게임 시간 흐름 제어 + 플레이 시간 누적
    /// - Pause 시 Time.timeScale = 0 으로 물리/애니메이션 등 전체 정지
    /// - 플레이 시간은 Pause 구간을 제외하고 누적 (unscaledDeltaTime 사용)
    /// </summary>
    public class GameTimeManager : BaseManager<GameTimeManager>, IManager
    {
        public static event Action<bool> OnPauseChanged; // true = 일시정지

        public bool IsPaused { get; private set; }

        // 누적 플레이 시간 (초). 저장/불러오기 시 외부에서 주입 가능.
        public float TotalPlaySeconds { get; private set; }

        // HitStop이 요청한 timeScale. Pause 중엔 적용을 보류하고, 재개 시 복구에 활용.
        private float _hitStopTimeScale = 1f;
        private bool _isHitStopping;

        // ─────────────────────────────────────────────
        #region IManager

        public void Init() { }
        public void AfterInit() { }
        public void Dispose() => SetPause(false); // 씬 전환 등에서 timeScale 복구 보장
        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }
        public void OnSceneChanged(string sceneType) { }

        public void OnUpdate()
        {
            // Pause 중엔 누적하지 않음
            if (!IsPaused)
                TotalPlaySeconds += Time.unscaledDeltaTime;
        }

        #endregion

        // ─────────────────────────────────────────────
        #region Public API

        public void SetPause(bool pause)
        {
            if (IsPaused == pause) return;

            IsPaused = pause;
            // HitStop 중이더라도 Pause가 우선. 재개 시엔 HitStop 스케일로 복구.
            Time.timeScale = pause ? 0f : _hitStopTimeScale;

            OnPauseChanged?.Invoke(IsPaused);
            Debug.Log($"[GameTimeManager] {(IsPaused ? "일시정지" : "재개")} | 누적 플레이 {FormatPlayTime()}");
        }

        public void TogglePause() => SetPause(!IsPaused);

        /// <summary>
        /// HitStopManager 전용. timeScale 소유권을 GameTimeManager에 위임.
        /// Pause 중엔 값만 저장하고 실제 적용은 보류.
        /// </summary>
        public void SetHitStopTimeScale(float scale)
        {
            _hitStopTimeScale = scale;
            _isHitStopping = scale < 1f;

            if (!IsPaused)
                Time.timeScale = _hitStopTimeScale;
        }

        /// <summary>
        /// HitStop 종료 시 호출. timeScale을 정상으로 복구.
        /// </summary>
        public void ResetHitStopTimeScale()
        {
            _hitStopTimeScale = 1f;
            _isHitStopping = false;

            if (!IsPaused)
                Time.timeScale = 1f;
        }

        /// <summary>
        /// 세이브 데이터 로드 시 누적 시간 주입
        /// </summary>
        public void SetTotalPlaySeconds(float seconds) => TotalPlaySeconds = Mathf.Max(0f, seconds);

        /// <summary>
        /// HH:MM:SS 포맷으로 반환
        /// </summary>
        public string FormatPlayTime()
        {
            int total = (int)TotalPlaySeconds;
            return $"{total / 3600:D2}:{total % 3600 / 60:D2}:{total % 60:D2}";
        }

        #endregion
    }
}
