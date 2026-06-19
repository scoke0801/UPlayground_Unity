using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 게임 시간 흐름 제어 + 플레이 시간 누적.
    ///
    /// timeScale 소유권 모델:
    ///   여러 시스템(HitStop, TimeScaleEvent, PlayerGuard 등)이 동시에
    ///   timeScale 감속을 요청할 수 있다. 각 요청자는 고유 id로 등록하고,
    ///   활성 요청 중 가장 낮은 scale(가장 강한 효과)이 실제로 적용된다.
    ///   마지막 요청자가 해제하면 자동으로 1.0으로 복구된다.
    /// </summary>
    public class GameTimeManager : BaseManager<GameTimeManager>, IManager, IUpdatableManager
    {
        public static event Action<bool> OnPauseChanged;

        public bool IsPaused          { get; private set; }
        public float TotalPlaySeconds { get; private set; }

        // key: 요청자 id, value: 요청한 scale
        private readonly Dictionary<int, float> _requests = new Dictionary<int, float>();
        private int  _nextId        = 0;
        private float _activeScale  = 1f; // 현재 적용된 scale (Pause 해제 시 복구용)

        #region IManager

        public void Init()   { }
        public void AfterInit() { }
        public void Dispose() => SetPause(false);
        public void OnFixedUpdate() { }
        public void OnLateUpdate()  { }
        public void OnSceneChanged(string sceneType) { }

        public void OnUpdate()
        {
            if (!IsPaused)
                TotalPlaySeconds += Time.unscaledDeltaTime;
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
