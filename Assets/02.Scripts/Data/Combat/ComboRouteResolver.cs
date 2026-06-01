using System;
using System.Collections.Generic;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Combat
{
    /// <summary>
    /// 입력 토큰 스트림과 등록된 연계 라우트를 매칭하는 순수 함수 모음.
    ///
    /// 런타임(PlayerAttackState)과 에디터(시뮬레이터/진단)가 동일 로직을 공유하기 위해
    /// MonoBehaviour/Unity 런타임 상태에 의존하지 않는 static으로 분리한다.
    /// (에디터 미리보기와 실제 게임 동작의 괴리 방지 — 설계 §7.4)
    /// </summary>
    public static class ComboRouteResolver
    {
        /// <summary>
        /// 입력 스트림 끝과 매칭되는 최적 라우트를 반환한다(없으면 null).
        /// 우선순위: 패턴 길이 큰 것 → priority 큰 것.
        /// </summary>
        /// <param name="stream">만료 제외 토큰 스트림(이번 입력 토큰까지 포함된 후보 시퀀스)</param>
        /// <param name="routes">후보 라우트 목록</param>
        /// <param name="tags">태그 조건 평가용 컨테이너(null이면 required 없는 라우트만 통과)</param>
        /// <param name="isGrounded">지상/공중 조건 평가용</param>
        /// <param name="resourceFilter">자원(게이지/쿨다운) 통과 여부. null이면 자원 게이팅 생략(에디터)</param>
        public static ComboRouteEntry Resolve(
            IReadOnlyList<ComboInputToken>  stream,
            IReadOnlyList<ComboRouteEntry>  routes,
            GameplayTagContainer            tags,
            bool                            isGrounded,
            Func<ComboRouteEntry, bool>     resourceFilter = null)
        {
            if (routes == null || routes.Count == 0) return null;
            if (stream == null || stream.Count == 0) return null;

            ComboRouteEntry best = null;
            foreach (var route in routes)
            {
                if (route == null || route.IsEmpty)          continue;
                if (!IsExecutable(route))                     continue; // 실행 불가 라우트는 기본 콤보를 가리지 않게 제외(advisor)
                if (!PatternMatches(stream, route))          continue;
                if (!route.CheckTagConditions(tags))         continue;
                if (!GroundOk(route.groundCondition, isGrounded)) continue;
                if (resourceFilter != null && !resourceFilter(route)) continue;

                if (IsBetter(route, best)) best = route;
            }
            return best;
        }

        /// <summary>
        /// 패턴이 스트림과 매칭되는지(태그/자원 무관, 순수 토큰 비교).
        /// Suffix: 스트림 끝 N개가 패턴과 일치 / Exact: 스트림 전체가 정확히 일치.
        /// 에디터 진단/시뮬레이터에서도 직접 사용한다.
        /// </summary>
        public static bool PatternMatches(IReadOnlyList<ComboInputToken> stream, ComboRouteEntry route)
        {
            if (stream == null || route == null || route.IsEmpty) return false;

            var pattern = route.inputPattern;
            int n = pattern.Count;
            if (stream.Count < n) return false;
            if (route.matchMode == ComboMatchMode.Exact && stream.Count != n) return false;

            int offset = stream.Count - n; // Suffix 시작 인덱스
            for (int i = 0; i < n; i++)
            {
                if (stream[offset + i] != pattern[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// 라우트가 실제 실행 가능한지(공격 정보·animKey 보유). 미설정 라우트가 매칭돼
        /// 입력이 먹통이 되는(dead input) 상황을 방지한다.
        /// </summary>
        public static bool IsExecutable(ComboRouteEntry route)
            => route != null
               && route.attackInfo?.baseInfo != null
               && route.attackInfo.baseInfo.animKey != AnimKey.None;

        private static bool GroundOk(RouteGroundCondition condition, bool isGrounded) => condition switch
        {
            RouteGroundCondition.Grounded => isGrounded,
            RouteGroundCondition.Airborne => !isGrounded,
            _                             => true,
        };

        /// <summary>경합 시 더 우선하는 후보인지: 패턴 길이 우선, 동률이면 priority 우선.</summary>
        private static bool IsBetter(ComboRouteEntry candidate, ComboRouteEntry best)
        {
            if (best == null) return true;

            int candLen = candidate.inputPattern.Count;
            int bestLen = best.inputPattern.Count;
            if (candLen != bestLen) return candLen > bestLen;

            return candidate.priority > best.priority;
        }
    }
}
