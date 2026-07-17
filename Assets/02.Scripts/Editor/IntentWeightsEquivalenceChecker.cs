#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.AI.CombatDecision;

namespace UPlayGround.Data.Enemy.EditorTools
{
    /// <summary>
    /// IW_Default_Melee SO와 LegacyIntentScoring의 결과가 일치하는지 확인하는 회귀 검증.
    /// 합성 IntentEvaluationContext 케이스를 다양하게 생성해 두 경로의 9개 점수를 비교한다.
    /// 차이가 ε(0.0001) 이상이면 실패로 간주하고 Console에 로그.
    /// </summary>
    public static class IntentWeightsEquivalenceChecker
    {
        private const float Epsilon = 0.0001f;
        private const string DefaultAssetPath = "Assets/10.Datas/AI/IntentWeights/IW_Default_Melee.asset";

        [MenuItem("UPlayGround/적/의도 가중치/레거시 동등성 검사 (IW_Default_Melee)")]
        public static void Run()
        {
            var so = AssetDatabase.LoadAssetAtPath<EnemyIntentWeightsSO>(DefaultAssetPath);
            if (so == null)
            {
                Debug.LogError($"[IntentWeightsEquivalenceChecker] {DefaultAssetPath} 가 없습니다. 먼저 메뉴 'Generate All Default Profiles'를 실행하세요.");
                return;
            }

            var cases = BuildTestCases();
            int passed = 0, failed = 0;
            var failures = new StringBuilder();

            foreach (var (label, ctx) in cases)
            {
                var legacy = LegacyIntentScoring.Compute(in ctx);
                var soScores = ComputeFromSO(so, in ctx);

                if (Compare(legacy, soScores, out var diffDetail))
                    passed++;
                else
                {
                    failed++;
                    failures.AppendLine($"❌ [{label}] {diffDetail}");
                }
            }

            if (failed == 0)
                Debug.Log($"[IntentWeightsEquivalenceChecker] ✅ {passed}/{cases.Count} 케이스 모두 동등. IW_Default_Melee는 레거시와 일치합니다.");
            else
                Debug.LogError($"[IntentWeightsEquivalenceChecker] ❌ {failed}/{cases.Count} 케이스 불일치:\n{failures}");
        }

        private static LegacyIntentScoring.Scores ComputeFromSO(EnemyIntentWeightsSO so, in IntentEvaluationContext ctx)
        {
            return new LegacyIntentScoring.Scores
            {
                attack       = IntentScoreComputer.Compute(so.attack,       in ctx),
                punish       = IntentScoreComputer.Compute(so.punish,       in ctx),
                counter      = IntentScoreComputer.Compute(so.counter,      in ctx),
                pressure     = IntentScoreComputer.Compute(so.pressure,     in ctx),
                chase        = IntentScoreComputer.Compute(so.chase,        in ctx),
                retreat      = IntentScoreComputer.Compute(so.retreat,      in ctx),
                keepDistance = IntentScoreComputer.Compute(so.keepDistance, in ctx),
                defend       = IntentScoreComputer.Compute(so.defend,       in ctx),
                recover      = IntentScoreComputer.Compute(so.recover,      in ctx)
            };
        }

        private static bool Compare(LegacyIntentScoring.Scores a, LegacyIntentScoring.Scores b, out string detail)
        {
            detail = null;
            var sb = new StringBuilder();
            void Check(string n, float x, float y) {
                if (Mathf.Abs(x - y) > Epsilon)
                    sb.AppendLine($"  {n}: legacy={x:0.0000} so={y:0.0000} diff={Mathf.Abs(x-y):0.0000}");
            }
            Check("attack",       a.attack,       b.attack);
            Check("punish",       a.punish,       b.punish);
            Check("counter",      a.counter,      b.counter);
            Check("pressure",     a.pressure,     b.pressure);
            Check("chase",        a.chase,        b.chase);
            Check("retreat",      a.retreat,      b.retreat);
            Check("keepDistance", a.keepDistance, b.keepDistance);
            Check("defend",       a.defend,       b.defend);
            Check("recover",      a.recover,      b.recover);
            if (sb.Length == 0) return true;
            detail = sb.ToString();
            return false;
        }

        private static List<(string label, IntentEvaluationContext ctx)> BuildTestCases()
        {
            var list = new List<(string, IntentEvaluationContext)>();

            // 거리 6 × 행동 가능성 4 × 플레이어 상태 5 = 120 케이스
            float[] distances = { 0.5f, 1.4f, 2.4f, 3.0f, 4.5f, 7.0f };
            (bool delay, bool atk, bool skill, bool guard)[] capabilities =
            {
                (true,  true,  true,  true),
                (true,  true,  false, false),
                (false, true,  false, true),
                (true,  false, false, true)
            };

            foreach (var d in distances)
            foreach (var cap in capabilities)
            foreach (var playerMode in new[] { 0, 1, 2, 3, 4 })
            {
                var ctx = MakeBaseContext(d);
                ctx.ActionDelayElapsed = cap.delay;
                ctx.HasAvailableAttack = cap.atk;
                ctx.CanUseSkill = cap.skill;
                ctx.HasGuardMotion = cap.guard;
                string pState = "idle";
                switch (playerMode)
                {
                    case 1: ctx.IsPlayerAttacking = true;  ctx.IsPlayerAttackingFrequently = true; pState = "atk";        break;
                    case 2: ctx.IsPlayerStaggered = true;                                          pState = "stagger";    break;
                    case 3: ctx.IsPlayerRecovering = true; ctx.IsPlayerRecoveringFrequently = true; pState = "recover";   break;
                    case 4: ctx.IsPlayerGuarding = true;   ctx.IsPlayerGuardingFrequently = true; ctx.IsPlayerDodgingFrequently = true; pState = "guard";     break;
                }

                list.Add(($"d={d} delay={cap.delay} atk={cap.atk} skill={cap.skill} guard={cap.guard} player={pState}", ctx));
            }

            // 자기 상태(HP/피격/Poise) × 거리 sweep — Retreat/Recover/Defend 보너스 커버
            float[] hpLevels = { 0.20f, 0.50f, 1.00f };
            bool[] hits     = { false, true };
            bool[] poises   = { false, true };
            float[] selfDistances = { 0.6f, 1.5f, 3.0f, 5.0f };

            foreach (var d in selfDistances)
            foreach (var hp in hpLevels)
            foreach (var hit in hits)
            foreach (var poise in poises)
            {
                var ctx = MakeBaseContext(d);
                ctx.HealthPercent = hp;
                ctx.WasHitRecently = hit;
                ctx.IsPoiseBroken = poise;
                list.Add(($"self d={d} hp={hp} hit={hit} poise={poise}", ctx));
            }

            // 후퇴 쿨다운 활성 + 다양한 거리
            foreach (var d in new[] { 0.6f, 1.5f, 3.0f })
            {
                var ctx = MakeBaseContext(d);
                ctx.TimeSinceRetreat = 0.5f;
                ctx.MinRetreatCooldown = 1.5f;
                ctx.WasHitRecently = true;
                list.Add(($"retreat_cd d={d}", ctx));
            }

            // 플레이어 빈번 공격 × TooClose (Retreat의 IsPlayerAttackingFrequently&&TooClose 커버)
            foreach (var d in new[] { 0.4f, 0.8f, 1.5f })
            {
                var ctx = MakeBaseContext(d);
                ctx.IsPlayerAttacking = true;
                ctx.IsPlayerAttackingFrequently = true;
                ctx.WasHitRecently = true;
                list.Add(($"player_atk_freq d={d}", ctx));
            }

            return list;
        }

        /// <summary> 기본 컨텍스트: optimal=2.5, min=1.5, personalSpace=0.8, preferred=2.5, HP=1.0, default chances </summary>
        private static IntentEvaluationContext MakeBaseContext(float distance)
        {
            return new IntentEvaluationContext
            {
                Distance = distance,
                OptimalDistance = 2.5f,
                MinDistance = 1.5f,
                PersonalSpace = 0.8f,
                PreferredRange = 2.5f,
                HealthPercent = 1.0f,
                Aggression = 0.5f,
                ReactionChance = 0.35f,
                PunishChance = 0.35f,
                CounterChance = 0.2f,
                RetreatChance = 0.2f,
                GuardChance = 0.25f,
                CircleWeight = 0.35f,
                MinRetreatCooldown = 1.5f,
                TimeSinceRetreat = 999f
            };
        }
    }
}
#endif
