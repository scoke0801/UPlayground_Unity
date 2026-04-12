using System.Collections.Generic;
using System.Text;
using UPlayGround.Data.Combat;

namespace UPlayGround.Input
{
    /// <summary>
    /// 현재 콤보 체인에서 입력된 LightAttack / HeavyAttack 순서를 기록한다.
    /// PlayerCombat이 소유하며, 콤보 리셋 시 함께 초기화된다.
    /// </summary>
    public class InputSequenceTracker
    {
        private readonly List<ComboInputType> _history = new();

        // ── 기록 ───────────────────────────────────────────────────────

        /// <summary>새 입력을 히스토리 끝에 추가한다.</summary>
        public void Record(ComboInputType inputType) => _history.Add(inputType);

        public void Clear() => _history.Clear();

        /// <summary>마지막으로 추가된 입력을 취소한다 (매칭 예비 판정 후 롤백용).</summary>
        public void RemoveLast()
        {
            if (_history.Count > 0)
                _history.RemoveAt(_history.Count - 1);
        }

        public int Count => _history.Count;

        // ── 매칭 ───────────────────────────────────────────────────────

        /// <summary>
        /// 현재 히스토리가 sequence와 길이 및 내용이 정확히 일치하는지 확인한다.
        /// </summary>
        public bool Matches(List<ComboInputStep> sequence)
        {
            if (sequence == null || sequence.Count == 0) return false;
            if (_history.Count != sequence.Count) return false;

            for (int i = 0; i < sequence.Count; i++)
                if (_history[i] != sequence[i].inputType) return false;

            return true;
        }

        // ── 디버그 ─────────────────────────────────────────────────────

        /// <summary>현재 히스토리를 "LLHR..." 형식 문자열로 반환한다.</summary>
        public string ToDebugString()
        {
            var sb = new StringBuilder();
            foreach (var t in _history)
                sb.Append(t == ComboInputType.LightAttack ? 'L' : 'H');
            return sb.Length > 0 ? sb.ToString() : "(없음)";
        }
    }
}
