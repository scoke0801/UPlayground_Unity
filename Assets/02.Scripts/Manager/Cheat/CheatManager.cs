using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Manager
{
    /// <summary>치트 액션 분류(실행 로그 표시/필터용).</summary>
    public enum CheatCategory
    {
        Gizmo,
        Item,
        Quest,
        Stat,
        Party,
        Combat,
        Time,
    }

    /// <summary>치트 실행 로그 1건.</summary>
    public readonly struct CheatLogEntry
    {
        public readonly DateTime      Time;
        public readonly CheatCategory Category;
        public readonly string        Message;

        public CheatLogEntry(DateTime time, CheatCategory category, string message)
        {
            Time     = time;
            Category = category;
            Message  = message;
        }
    }

    /// <summary>
    /// 치트 옵션 관리 매니저.
    /// GameManager에 등록되며 개발/테스트용 옵션을 중앙 관리한다.
    ///
    /// 실제 치트 조작(아이템/퀘스트/스탯/파티/기즈모)은 partial 파일에 분리되어 있으며
    /// <c>#if UNITY_EDITOR || DEVELOPMENT_BUILD</c> 로 감싸져 릴리스 빌드에서는 스트립된다.
    /// 이 코어 파일은 항상 컴파일된다(GameManager가 무조건 등록하고, IsAlwaysParryEnabled를
    /// 릴리스 전투 코드가 참조하기 때문).
    /// </summary>
    public partial class CheatManager : BaseManager<CheatManager>, IManager, ICheatStateService
    {
        [Header("전투 치트")]
        [Tooltip("활성화 시 어떤 상태에서도 적의 공격을 패리할 수 있다")]
        [SerializeField] private bool _alwaysParry = false;

        /// <summary> 항상 패리 가능 여부 </summary>
        public bool IsAlwaysParryEnabled => _alwaysParry;

        public void SetAlwaysParry(bool value)
        {
            _alwaysParry = value;
            Debug.Log($"[CheatManager] 항상 패리: {(_alwaysParry ? "ON" : "OFF")}");
            Log(CheatCategory.Combat, $"항상 패리 {(_alwaysParry ? "ON" : "OFF")}");
        }

        public void ToggleAlwaysParry() => SetAlwaysParry(!_alwaysParry);

        #region 실행 로그

        private const int MaxLogEntries = 50;

        // 최신 항목이 앞(index 0)에 오도록 유지한다(UI는 위에서부터 최신 순으로 표시).
        private readonly List<CheatLogEntry> _log = new(MaxLogEntries);

        /// <summary> 최근 실행 로그(최신순). </summary>
        public IReadOnlyList<CheatLogEntry> RecentLogs => _log;

        /// <summary> 로그가 갱신될 때 발생. 치트 패널이 구독해 목록을 다시 그린다. </summary>
        public event Action OnLogChanged;

        /// <summary> 치트 실행 1건을 로그에 남긴다. </summary>
        public void Log(CheatCategory category, string message)
        {
            _log.Insert(0, new CheatLogEntry(DateTime.Now, category, message));
            if (_log.Count > MaxLogEntries)
                _log.RemoveRange(MaxLogEntries, _log.Count - MaxLogEntries);

            OnLogChanged?.Invoke();
        }

        public void ClearLog()
        {
            _log.Clear();
            OnLogChanged?.Invoke();
        }

        #endregion

        #region IManager

        public void Init()                          => Debug.Log("[CheatManager] 초기화");
        public void AfterInit()                     { }
        public void Dispose()                       { _log.Clear(); OnLogChanged = null; }
        public void OnUpdate()                      { }
        public void OnFixedUpdate()                 { }
        public void OnLateUpdate()                  { }
        public void OnSceneChanged(string sceneType){ }

        #endregion
    }
}
