using UnityEngine;

namespace UPlayGround.Dialogue
{
    /// <summary>주목 컷 진행 중 카메라가 실제로 해야 할 일.</summary>
    public enum DialogueFocusStep
    {
        /// <summary>할 일 없음.</summary>
        None = 0,

        /// <summary>주목 대상 구도로 넘어간다.</summary>
        EnterFocus = 1,

        /// <summary>원래 라인 구도로 되돌아간다.</summary>
        ReturnToLine = 2,
    }

    /// <summary>
    /// 대사 한 줄에 붙는 주목 컷("저기 있는 저 아이" 같은 지시 대사)의 시간 진행만 담당하는 상태 기계.
    ///
    /// 카메라 push와 분리한 이유는 두 가지다.
    /// 대기 → 주목 → 복귀 순서를 카메라 없이 검증할 수 있고,
    /// 대화 정지 중에는 호출측이 Tick을 건너뛰기만 하면 되기 때문이다.
    /// </summary>
    public sealed class DialogueFocusCutaway
    {
        private enum Phase
        {
            /// <summary>주목 컷이 걸려 있지 않다.</summary>
            Idle,

            /// <summary>라인 구도를 먼저 보여주는 대기 구간.</summary>
            Delay,

            /// <summary>주목 대상을 잡고 있는 구간.</summary>
            Hold,
        }

        private Phase _phase = Phase.Idle;
        private float _remaining;
        private float _holdSeconds;

        /// <summary>주목 컷이 진행 중인지(대기 구간 포함).</summary>
        public bool IsActive => _phase != Phase.Idle;

        /// <summary>이미 주목 대상을 잡고 있는지.</summary>
        public bool IsFocused => _phase == Phase.Hold;

        /// <summary>
        /// 주목 컷을 시작한다. 반환값이 <see cref="DialogueFocusStep.EnterFocus"/>면
        /// 대기 없이 곧바로 대상 구도로 넘어가야 한다.
        /// </summary>
        public DialogueFocusStep Begin(float delaySeconds, float holdSeconds)
        {
            _holdSeconds = Mathf.Max(0f, holdSeconds);
            if (_holdSeconds <= 0f)
            {
                Reset();
                return DialogueFocusStep.None;
            }

            float delay = Mathf.Max(0f, delaySeconds);
            if (delay > 0f)
            {
                _phase = Phase.Delay;
                _remaining = delay;
                return DialogueFocusStep.None;
            }

            _phase = Phase.Hold;
            _remaining = _holdSeconds;
            return DialogueFocusStep.EnterFocus;
        }

        /// <summary>시간을 진행시키고 이번 프레임에 필요한 카메라 전환을 반환한다.</summary>
        public DialogueFocusStep Tick(float deltaTime)
        {
            if (_phase == Phase.Idle || deltaTime <= 0f)
                return DialogueFocusStep.None;

            _remaining -= deltaTime;
            if (_remaining > 0f)
                return DialogueFocusStep.None;

            if (_phase == Phase.Delay)
            {
                // 넘친 시간은 주목 구간에서 차감한다. 프레임이 튀어도 총 연출 길이가 늘어나지 않는다.
                _remaining = Mathf.Max(0f, _holdSeconds + _remaining);
                _phase = Phase.Hold;
                return DialogueFocusStep.EnterFocus;
            }

            Reset();
            return DialogueFocusStep.ReturnToLine;
        }

        /// <summary>진행 중인 주목 컷을 버린다. 복귀 전환은 발생하지 않는다.</summary>
        public void Reset()
        {
            _phase = Phase.Idle;
            _remaining = 0f;
            _holdSeconds = 0f;
        }
    }
}
