using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Diagnostics;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.Animation;

namespace UPlayGround.State
{
    /// <summary>
    /// 연계 라우트 해석·실행 오케스트레이션.
    ///
    /// 여러 공격 호스트 상태(PlayerAttackState=지상, PlayerJumpAttackState=공중)가
    /// pending 토큰 산출 → push → Resolve → (매칭 시) 트래커 Clear + ExecuteComboRoute의
    /// 동일 로직을 공유하기 위해 분리한다. 각 상태가 복사하면 peek/execute 드리프트가
    /// 재발하므로(설계 §5, advisor), 단일 지점으로 둔다.
    /// </summary>
    public static class ComboRouteRunner
    {
        /// <summary>연계 라우트 진단 로그 토글(문제 해결 후 false로). 기록(execute) 경로에서만 1회씩 출력.</summary>
        public static bool DebugLog = false;

        /// <summary>
        /// 이번 진입의 pending 토큰을 산출하고 라우트를 Resolve한다.
        /// recordToken:false=peek(가상 append, push 없음), true=execute(트래커에 push).
        /// </summary>
        public static ComboRouteEntry ResolveRoute(
            PlayerActor playerActor,
            PlayerMovementController controller,
            PlayerCombat combat,
            bool isHeavyAttack,
            PlayerInterruptAction forcedAttackAction,
            bool recordToken)
        {
            if (playerActor == null || combat == null || controller == null) return null;
            if (!TryComputePendingToken(controller, isHeavyAttack, forcedAttackAction, out var pending))
                return null;

            var tracker = playerActor.ComboInputTracker;
            var routes  = combat.ComboRoutes;

            // 라우트가 없어도 execute 경로에서는 토큰을 기록해 둬야 미래 입력이 매칭 가능하다.
            if (routes == null || routes.Count == 0)
            {
                if (recordToken)
                {
                    tracker.Push(pending);
                    if (DebugLog)
                        RuntimeLog.Trace(
                            RuntimeLogCategory.Combat | RuntimeLogCategory.Input,
                            $"[ComboRoute] +{ComboInputTrackerAbbrev(pending)} 윈도우=[{tracker.ToDebugString()}] 라우트수=0 → 활성 _attackData에 comboRoutes 없음! (편집한 SO와 다른 에셋이 캐릭터에 연결됐을 수 있음)");
                }
                return null;
            }

            System.Collections.Generic.IReadOnlyList<ComboInputToken> candidate;
            if (recordToken)
            {
                tracker.Push(pending);            // 단일 기록 지점
                candidate = tracker.GetWindow();
            }
            else
            {
                candidate = tracker.GetWindowWith(pending); // 가상 append (no push)
            }

            bool grounded = controller.Motor != null && controller.Motor.GroundingStatus.IsStableOnGround;
            // 실행 시점과 동일한 자원 조건으로 선택 불가능한 라우트를 제외한다.
            var result = ComboRouteResolver.Resolve(
                candidate, routes, playerActor.Tags, grounded,
                combat.CanAffordRoute);

            if (recordToken && DebugLog)
                RuntimeLog.Trace(
                    RuntimeLogCategory.Combat | RuntimeLogCategory.Input,
                    $"[ComboRoute] +{ComboInputTrackerAbbrev(pending)} 윈도우=[{tracker.ToDebugString()}] 라우트수={routes.Count} grounded={grounded} → {(result != null ? $"매칭 '{result.routeName}' motion={result.attackInfo?.motionKey}" : "매칭없음")}");

            return result;
        }

        private static string ComboInputTrackerAbbrev(ComboInputToken t)
            => UPlayGround.Input.ComboInputTracker.Abbrev(t);

        /// <summary>
        /// 라우트를 resolve → (매칭 시) 트래커 Clear + ExecuteComboRoute까지 수행한다.
        /// 매칭 라우트가 없으면 null을 반환하여 호출자가 기본 로직을 수행하게 한다.
        /// </summary>
        public static AttackData TryExecuteRoute(
            PlayerActor playerActor,
            PlayerMovementController controller,
            PlayerCombat combat,
            bool isHeavyAttack,
            PlayerInterruptAction forcedAttackAction,
            out MotionSetAsset motionAsset)
        {
            motionAsset = null;

            // 마무리 입력 간격은 ResolveRoute가 트래커에 push하기 '이전'에 캡처한다(push 후엔 0).
            float finishingInterval = playerActor != null
                ? playerActor.ComboInputTracker.TimeSinceLastToken()
                : float.PositiveInfinity;

            var route = ResolveRoute(playerActor, controller, combat,
                isHeavyAttack, forcedAttackAction, recordToken: true);
            if (route == null) return null;

            // 퍼펙트 타이밍: 마무리 입력이 직전 토큰으로부터 perfectWindow 안에 들어왔는가.
            bool isPerfect = route.HasPerfectWindow && finishingInterval <= route.perfectWindow;

            // 연계 발동 → 윈도우를 비워 stale 접두 토큰의 재매칭을 방지(설계 §8).
            playerActor.ComboInputTracker.Clear();
            var attack = combat.ExecuteComboRoute(route, isPerfect);
            motionAsset = attack?.motionAsset;
            return attack;
        }

        /// <summary>
        /// forced/normal 입력에서 이번 진입의 pending 토큰을 산출한다.
        /// 스킬3+ 처럼 라우트 토큰이 없는 입력은 false.
        /// </summary>
        private static bool TryComputePendingToken(
            PlayerMovementController controller,
            bool isHeavyAttack,
            PlayerInterruptAction forcedAttackAction,
            out ComboInputToken token)
        {
            token = ComboInputToken.LightAttack;

            if ((forcedAttackAction & PlayerInterruptAction.HeavyAttack) != 0) { token = ComboInputToken.HeavyAttack; return true; }
            if ((forcedAttackAction & PlayerInterruptAction.LightAttack) != 0) { token = ComboInputToken.LightAttack; return true; }

            int skill = FirstSkillInput(controller);
            if ((forcedAttackAction & PlayerInterruptAction.Skill) != 0)
            {
                if (skill == 0) { token = ComboInputToken.Skill1; return true; }
                if (skill == 1) { token = ComboInputToken.Skill2; return true; }
                return false; // 스킬3+ 는 라우트 토큰 없음
            }

            // 일반 진입: 스킬 입력 우선, 없으면 강/약
            if (skill == 0) { token = ComboInputToken.Skill1; return true; }
            if (skill == 1) { token = ComboInputToken.Skill2; return true; }
            if (skill >= 2) return false;

            token = isHeavyAttack ? ComboInputToken.HeavyAttack : ComboInputToken.LightAttack;
            return true;
        }

        private static int FirstSkillInput(PlayerMovementController controller)
        {
            for (int i = 0; i < PlayerAbilityResourceView.SkillSlotCount; i++)
                if (controller.HasSkillInput(i)) return i;
            return -1;
        }
    }
}
