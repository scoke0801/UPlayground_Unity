using System.Collections.Generic;

namespace UPlayGround.Combat
{
    /// <summary>
    /// 전투 결과(<see cref="CombatResult"/>)를 인메모리 링버퍼에 기록하는 최소 로그 (P1.5).
    /// 피드백과 "같은 결과 객체"를 읽는 소비처를 제공해 P1 완료 기준을 충족하고,
    /// P3 cutover(legacy/runner 결과 일치 검증)와 이후 회귀 확인의 관측 수단이 된다.
    ///
    /// damage-applied 경로에서만 호출할 것 — 가드/무적으로 막힌 히트를 피해로 기록하지 않는다.
    /// </summary>
    public static class CombatLogRecorder
    {
        private const int DefaultCapacity = 256;

        /// <summary>기록 on/off. 기본 off — 디버깅/밸런싱 세션에서만 켠다.</summary>
        public static bool Enabled = false;

        private static int _capacity = DefaultCapacity;
        private static int _nextSequence = 1;
        private static readonly Queue<CombatLogEntry> _entries = new(DefaultCapacity);

        public static IReadOnlyCollection<CombatLogEntry> Entries => _entries;
        public static int Count => _entries.Count;
        public static int Capacity => _capacity;

        public static void SetCapacity(int capacity)
        {
            _capacity = capacity > 0 ? capacity : DefaultCapacity;
            TrimToCapacity();
        }

        public static void Record(in CombatResult result)
        {
            if (!Enabled)
                return;

            if (_entries.Count >= _capacity)
                _entries.Dequeue();
            _entries.Enqueue(CombatLogEntry.Create(_nextSequence++, result));
        }

        public static void Clear()
        {
            _entries.Clear();
            _nextSequence = 1;
        }

        private static void TrimToCapacity()
        {
            while (_entries.Count > _capacity)
                _entries.Dequeue();
        }
    }
}
