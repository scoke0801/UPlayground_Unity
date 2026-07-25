using System;
using System.Collections.Generic;
using UPlayGround.Data.Config;
using UPlayGround.Manager;

namespace UPlayGround.Dialogue
{
    /// <summary>
    /// 대화 재생 제어(정지·자동·스킵)의 전역 상태와 대화 이력을 소유합니다.
    /// DialogueManager가 소유하며, UI는 IUIDialogueService 계약을 통해서만 구독·명령합니다.
    ///
    /// 정지는 '대화 컨텍스트의 정지'이며 Time.timeScale을 건드리지 않습니다.
    /// 대화 카메라 녹화 재생이 timeScale에 의존할 수 있으므로 결합하지 않습니다.
    /// </summary>
    public sealed class DialoguePlaybackController
    {
        /// <summary>이력 링 버퍼 상한. 초과 시 오래된 항목부터 폐기합니다.</summary>
        public const int HistoryCapacity = 100;

        private readonly List<DialogueLogEntry> _history = new(HistoryCapacity);

        public event Action<bool> OnPauseChanged;
        public event Action<bool> OnAutoChanged;
        public event Action OnHistoryChanged;

        /// <summary>UI에게 현재 타이핑을 즉시 완성하라고 요청(타이핑 스킵/약).</summary>
        public event Action OnTypingCompleteRequested;

        public bool IsPaused { get; private set; }
        public bool IsAuto { get; private set; }

        public IReadOnlyList<DialogueLogEntry> History => _history;

        /// <summary>설정에서 조정하는 전역 자동 재생 대기 시간(초).</summary>
        public float AutoAdvanceDelay
        {
            get
            {
                var data = Svc.Settings?.Data;
                return data != null ? data.DialogueAutoAdvanceDelay : 1.4f;
            }
        }

        /// <summary>설정에서 조정하는 전역 타이핑 속도 배율(클수록 느림).</summary>
        public float TypingSpeedScale
        {
            get
            {
                var data = Svc.Settings?.Data;
                return data != null ? data.DialogueTypingSpeedScale : 1f;
            }
        }

        public void SetPaused(bool paused)
        {
            if (IsPaused == paused)
                return;

            IsPaused = paused;
            OnPauseChanged?.Invoke(paused);
        }

        public void SetAuto(bool auto)
        {
            if (IsAuto == auto)
                return;

            IsAuto = auto;
            OnAutoChanged?.Invoke(auto);
        }

        public void RequestTypingComplete() => OnTypingCompleteRequested?.Invoke();

        /// <summary>
        /// 대화 세션이 끝났을 때 재생 상태를 초기화합니다.
        /// 자동 재생 토글은 플레이어의 명시적 선택이므로 유지하고, 정지만 해제합니다.
        /// </summary>
        public void ResetForSessionEnd()
        {
            SetPaused(false);
        }

        // ── 이력 ────────────────────────────────────────────────────────

        public void RecordHistory(in DialogueLogEntry entry)
        {
            _history.Add(entry);

            // 상한 초과분은 오래된 쪽부터 버린다(런 단위 링 버퍼).
            while (_history.Count > HistoryCapacity)
                _history.RemoveAt(0);

            OnHistoryChanged?.Invoke();
        }

        public void ClearHistory()
        {
            if (_history.Count == 0)
                return;

            _history.Clear();
            OnHistoryChanged?.Invoke();
        }
    }
}
