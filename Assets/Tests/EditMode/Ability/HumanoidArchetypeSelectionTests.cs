using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UPlayGround.Components;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data;

namespace UPlayGround.Ability.Tests
{
    /// <summary>
    /// Humanoid 일반 몬스터(Enemy_Random_*) 5 아키타입의 공격 선택 가능성을 실제 에셋으로 검증한다.
    ///
    /// 설계 근거: Assets/docs/TODO/HUMANOID_MONSTER_GAS_BT_DESIGN.md
    ///
    /// 여기서 잡으려는 실패는 둘 다 "조용한" 종류다 — 예외도 로그도 없이 몬스터가
    /// 아무 공격도 하지 않는다.
    ///  (1) 궁수의 attackType이 Melee면 EnemyAttackRangePolicy가 근접 접근 사거리로
    ///      클램프해 교전 거리에서 후보가 0이 된다.
    ///  (2) BT가 요청하는 (attackCategory, abilityRole) 쌍이 AbilitySet의 배정과
    ///      어긋나면 그 규칙은 영구히 실패한다.
    /// </summary>
    public sealed class HumanoidArchetypeSelectionTests
    {
        private const int TestLevel = 3;
        private const string AbilityRoot = "Assets/10.Datas/Ability/Actor";

        /// <summary>BehaviorData의 교전 거리·personalSpace와 맞춘 값. 설계서 §7.4.</summary>
        private static readonly Dictionary<string, (float Optimal, float PersonalSpace)> Engagement = new()
        {
            ["GreatSword"] = (2.8f, 1.0f),
            ["DoubleAxe"] = (2.3f, 0.8f),
            ["DualBlade"] = (2.0f, 0.75f),
            ["SwordShield"] = (2.4f, 0.85f),
            ["Bow"] = (8.0f, 2.0f),
        };

        /// <summary>
        /// 역할↔카테고리 계약. HumanoidAuthoringBatch.ContractCategory 및
        /// gen_bt.py의 role_category()와 반드시 일치한다.
        /// </summary>
        private static AbilityAttackCategory ContractCategory(string archetype, AbilityAIRole role) =>
            role switch
            {
                AbilityAIRole.Opener => AbilityAttackCategory.Basic,
                AbilityAIRole.Finisher => AbilityAttackCategory.Basic,
                AbilityAIRole.Punish => archetype == "Bow"
                    ? AbilityAttackCategory.Skill
                    : AbilityAttackCategory.Heavy,
                _ => AbilityAttackCategory.Skill,
            };

        private static readonly AbilityAIRole[] Roles =
        {
            AbilityAIRole.Opener, AbilityAIRole.Punish, AbilityAIRole.Counter,
            AbilityAIRole.GapCloser, AbilityAIRole.Signature, AbilityAIRole.Finisher,
        };

        private static AbilitySetSO LoadSet(string archetype)
        {
            string token = archetype == "Spear" ? "Speat" : archetype;
            string path = $"{AbilityRoot}/Humanoid_{token}AttackData/AbilitySet_Humanoid_{token}AttackData.asset";
            var set = AssetDatabase.LoadAssetAtPath<AbilitySetSO>(path);
            Assert.That(set, Is.Not.Null, $"AbilitySet을 찾지 못했습니다: {path}");
            return set;
        }

        public static IEnumerable<string> Archetypes => Engagement.Keys;

        [Test]
        public void 궁수는_교전거리에서_공격후보를_가진다()
        {
            // 회귀 지점. attackType이 Melee로 되돌아가면 유효 사거리가
            // min(20, max(targetingRange-0.15, personalSpace+0.5)) 로 클램프돼 여기서 0이 된다.
            (float optimal, float personalSpace) = Engagement["Bow"];
            bool hasAttack = EnemyAttackRangePolicy.HasAttackInRange(
                LoadSet("Bow"),
                optimal,
                TestLevel,
                useMeleeApproachRange: true,
                personalSpaceDistance: personalSpace);

            Assert.That(hasAttack, Is.True,
                $"궁수가 교전 거리 {optimal}m에서 쓸 수 있는 공격이 없습니다. "
                + "Bow Payload의 baseInfo.attackType이 Ranged인지 확인하세요.");
        }

        [Test]
        public void 근접_아키타입은_궁수_교전거리에서는_후보가_없다()
        {
            // 반대 방향 검증 — 사거리 필터가 실제로 걸리는지. 전부 통과하는 필터는
            // 궁수 결함을 못 잡는다.
            foreach (string archetype in Archetypes.Where(a => a != "Bow"))
            {
                (_, float personalSpace) = Engagement[archetype];
                bool hasAttack = EnemyAttackRangePolicy.HasAttackInRange(
                    LoadSet(archetype),
                    8.0f,
                    TestLevel,
                    useMeleeApproachRange: true,
                    personalSpaceDistance: personalSpace);

                Assert.That(hasAttack, Is.False,
                    $"{archetype}이 8m에서 공격 가능하다고 판정됩니다. 근접 사거리 필터가 무력합니다.");
            }
        }

        /// <summary>
        /// 근접에서 "교전 거리에서 공격 가능한가"는 런타임 불변식이 아니다.
        /// EnemyChaseState.ResolveStopDistance가 chaseStopDistance와
        /// EnemyCombat.GetPreferredMeleeApproachDistance(= 요청 역할의 실제 최대 사거리 - 0.1)
        /// 중 작은 쪽까지 파고들기 때문에, 교전 거리보다 사거리가 짧아도 추격이 메워 준다.
        ///
        /// 따라서 진짜 불변식은 "역할마다 0보다 큰 도달 거리가 있고, 그 거리에서 선택 가능하다"이다.
        /// 도달 거리가 0이면 추격이 붙을 목표 자체가 없어 그 역할은 영구히 발동하지 않는다.
        /// </summary>
        [Test]
        public void 모든_역할은_0보다_큰_도달거리를_가진다()
        {
            var failures = new List<string>();
            foreach (string archetype in Archetypes)
            {
                (_, float personalSpace) = Engagement[archetype];
                AbilitySetSO set = LoadSet(archetype);

                foreach (AbilityAIRole role in Roles)
                {
                    float reach = MaxReach(set, archetype, role, personalSpace);
                    if (reach <= 0f)
                        failures.Add($"{archetype}: {role} 도달 거리 0 — 추격이 접근할 목표가 없다");
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        /// <summary>
        /// 해당 (카테고리, 역할)로 실제 선택 가능한 최대 거리를 찾는다.
        ///
        /// 이분 탐색을 쓰면 안 된다 — 베이크된 Ability는 minDistance가 0이 아니라
        /// (예: 0.65~1.55) 사용 가능 구간이 원점에서 떨어진 밴드다. 저거리 몇 점만
        /// 찔러 보는 방식은 그 밴드를 통째로 놓친다.
        /// </summary>
        private static float MaxReach(
            AbilitySetSO set, string archetype, AbilityAIRole role, float personalSpace)
        {
            AbilityAttackCategory category = ContractCategory(archetype, role);
            const float step = 0.05f;
            const float limit = 25f;

            float best = 0f;
            for (float d = step; d <= limit; d += step)
            {
                if (EnemyAttackRangePolicy.HasAttackInRange(
                        set, d, TestLevel, category,
                        useMeleeApproachRange: true,
                        personalSpaceDistance: personalSpace,
                        abilityRole: role))
                    best = d;
            }
            return best;
        }

        [Test]
        public void BT가_요청하는_카테고리_역할_쌍마다_후보가_존재한다()
        {
            // (attackCategory, abilityRole)은 AND 조건이다. 어긋난 쌍은 예외 없이
            // 조용히 실패하므로 데이터 쪽에서 막는다.
            var failures = new List<string>();
            foreach (string archetype in Archetypes)
            {
                AbilitySetSO set = LoadSet(archetype);
                foreach (AbilityAIRole role in Roles)
                {
                    AbilityAttackCategory category = ContractCategory(archetype, role);
                    int count = set.GetRuntimeAbilities()
                        .Where(a => a != null && a.variants != null)
                        .SelectMany(a => a.variants)
                        .Count(v => UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(
                                        v, out AbilityAttackInfo info)
                                    && EnemyAbilitySelectionPolicy.IsAISelectableAttack(info)
                                    && EnemyAbilitySelectionPolicy.MatchesCategory(info, category)
                                    && EnemyAbilitySelectionPolicy.MatchesRole(info, role));

                    if (count == 0)
                        failures.Add($"{archetype}: {category}+{role} 후보 0 — 대응 BT 규칙이 영구 실패한다");
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        /// <summary>
        /// 궁수만은 교전 거리에서 직접 닿아야 한다. 원거리는 추격으로 메우는 구조가 아니라
        /// 거리를 유지하며 쏘는 것이 설계이기 때문이다(설계서 §5).
        /// attackType이 Melee로 되돌아가면 근접 접근 사거리 클램프가 걸려 여기서 무너진다.
        /// </summary>
        [Test]
        public void 궁수의_원거리_역할은_교전거리까지_닿는다()
        {
            const string archetype = "Bow";
            (float optimal, float personalSpace) = Engagement[archetype];
            AbilitySetSO set = LoadSet(archetype);

            var failures = new List<string>();
            foreach (AbilityAIRole role in Roles)
            {
                // Counter는 근접 반응이라 원거리 도달 대상이 아니다.
                if (role == AbilityAIRole.Counter)
                    continue;

                float reach = MaxReach(set, archetype, role, personalSpace);
                if (reach < optimal)
                    failures.Add($"Bow: {role} 도달 {reach:0.##}m < 교전 거리 {optimal}m");
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }
    }
}
