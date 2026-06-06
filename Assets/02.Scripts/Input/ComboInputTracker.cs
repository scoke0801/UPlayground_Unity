using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UPlayGround.Data.Combat;

namespace UPlayGround.Input
{
    /// <summary>
    /// 연계 라우트 판정을 위한 입력 토큰 스트림.
    ///
    /// 과거 InputSequenceTracker는 PlayerCombat이 소유 + ResetCombo에서 Clear라
    /// 대시/점프 등 비공격 상태를 넘기지 못했다(설계 §2.3). 본 트래커는
    /// PlayerActor 레벨에서 소유되어 상태 전환을 넘어 생존하며,
    /// 간격 기반 만료(마지막 토큰 이후 LinkWindow 초과 시 전체 폐기)로만 끊긴다.
    ///
    /// 토큰 push는 "발동 확정 시점"에만 수행한다(InputBuffer 선입력과의 이중 기록 방지).
    /// </summary>
    public class ComboInputTracker
    {
        private struct TokenEntry
        {
            public ComboInputToken token;
            public float           time;
        }

        private readonly List<TokenEntry>      _entries     = new();
        private readonly List<ComboInputToken> _windowCache = new();
        private float                          _lastTokenTime = -999f;

        /// <summary>
        /// 연속 입력 간 최대 허용 '간격'(초). 마지막 토큰 이후 이 시간을 넘기면 체인 전체를 폐기한다.
        /// 절대 나이가 아닌 '간격' 기준이라 약약약→강처럼 누적 시간이 긴 콤보도 끊기지 않는다(advisor 지적).
        /// </summary>
        public float LinkWindow = 1.0f;

        // ── 기록 ───────────────────────────────────────────────────────

        /// <summary>토큰을 스트림 끝에 추가한다(추가 전 간격 만료 정리).</summary>
        public void Push(ComboInputToken token)
        {
            Expire();
            _entries.Add(new TokenEntry { token = token, time = Time.time });
            _lastTokenTime = Time.time;
        }

        /// <summary>스트림 전체를 비운다(피격/전투이탈/캐릭터 교체 시).</summary>
        public void Clear() => _entries.Clear();

        public int Count
        {
            get { Expire(); return _entries.Count; }
        }

        // ── 조회 ───────────────────────────────────────────────────────

        /// <summary>
        /// 만료 제외 토큰 스트림(현재 윈도우). 반환 리스트는 내부 캐시 재사용분이므로
        /// 호출 직후 동기적으로만 사용한다(다음 조회 시 덮어써짐).
        /// </summary>
        public IReadOnlyList<ComboInputToken> GetWindow()
        {
            Expire();
            _windowCache.Clear();
            for (int i = 0; i < _entries.Count; i++)
                _windowCache.Add(_entries[i].token);
            return _windowCache;
        }

        /// <summary>
        /// 현재 윈도우 끝에 pending 토큰을 가상으로 덧붙인 후보 시퀀스를 반환한다.
        /// 실제 push 없이 매칭 판정(peek)에 사용한다.
        /// </summary>
        public IReadOnlyList<ComboInputToken> GetWindowWith(ComboInputToken pending)
        {
            Expire();
            _windowCache.Clear();
            for (int i = 0; i < _entries.Count; i++)
                _windowCache.Add(_entries[i].token);
            _windowCache.Add(pending);
            return _windowCache;
        }

        // ── 만료 ───────────────────────────────────────────────────────

        /// <summary>
        /// 마지막 토큰 이후 LinkWindow를 초과했으면 체인 전체를 폐기한다(간격 기반).
        /// 콤보가 끊김 없이 이어지는 동안에는 누적 시간과 무관하게 전체 스트림을 보존한다.
        /// </summary>
        private void Expire()
        {
            if (_entries.Count == 0) return;
            if (Time.time - _lastTokenTime > LinkWindow)
                _entries.Clear();
        }

        // ── 디버그 ─────────────────────────────────────────────────────

        /// <summary>현재 윈도우를 "L H D S1 J ..." 형식 문자열로 반환(런타임 모니터/로그용).</summary>
        public string ToDebugString()
        {
            Expire();
            if (_entries.Count == 0) return "(없음)";

            var sb = new StringBuilder();
            for (int i = 0; i < _entries.Count; i++)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(Abbrev(_entries[i].token));
            }
            return sb.ToString();
        }

        public static string Abbrev(ComboInputToken t) => t switch
        {
            ComboInputToken.LightAttack => "L",
            ComboInputToken.HeavyAttack => "H",
            ComboInputToken.Dodge       => "D",
            ComboInputToken.Dash        => "Da",
            ComboInputToken.Skill1      => "S1",
            ComboInputToken.Jump        => "J",
            ComboInputToken.Skill2      => "S2",
            ComboInputToken.Charge      => "C",
            _                           => "?",
        };
    }
}
