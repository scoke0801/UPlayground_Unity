#if UNITY_EDITOR
using System.Collections.Generic;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Tool.Editor.Balance
{
    public static class BalanceActorDataValidator
    {
        public static List<BalanceValidationMessage> Validate(
            ActorDefinitionSO actor,
            BalanceScenarioAsset scenario,
            float assumedDistance,
            int monsterLevel)
        {
            var messages = new List<BalanceValidationMessage>();
            if (actor == null)
            {
                messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Error, "ActorDefinitionSO가 선택되지 않았습니다."));
                return messages;
            }

            if (string.IsNullOrWhiteSpace(actor.actorId))
                messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Error, "actorId가 비어 있습니다."));

            if (actor.prefab == null)
                messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Warning, "prefab이 비어 있습니다. Play Mode 검증과 BT Runner 탐색이 제한됩니다."));

            if (actor.statData == null)
                messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Error, "statData가 비어 있습니다."));

            bool isMonster = (actor.actorType & ActorType.Monster) != 0;
            if (isMonster)
            {
                if (actor.monsterScaling == null)
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Warning, "monsterScaling이 비어 있어 몬스터 Growth 기준을 명시적으로 추적할 수 없습니다."));

                if (actor.attackData == null)
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Error, "몬스터 ActorDefinitionSO인데 attackData가 비어 있습니다."));

                if (actor.breakGaugeData == null)
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Warning, "breakGaugeData가 비어 있어 브레이크 시간/노출 보너스 분석을 할 수 없습니다."));

                if (actor.behaviorData == null)
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Warning, "behaviorData가 비어 있어 BT/Intent 분석을 할 수 없습니다."));
                else if (actor.behaviorData.behaviorTree == null)
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Warning, "behaviorData.behaviorTree가 비어 있습니다."));
            }

            if (actor.level < 1)
                messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Error, "level이 1보다 작습니다."));

            ValidateAttackData(actor.attackData, assumedDistance, monsterLevel, messages);

            if (scenario == null)
                messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Warning, "BalanceScenarioAsset이 없어 창의 임시 입력값으로 분석합니다."));
            else
            {
                if (scenario.playerStatData == null)
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Warning, "플레이어 statData가 비어 있어 기본 스탯으로 계산합니다."));
                if (scenario.playerAttackData == null && scenario.manualPlayerDps <= 0f)
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Error, "플레이어 공격 데이터가 없고 manualPlayerDps도 0 이하입니다."));
            }

            return messages;
        }

        private static void ValidateAttackData(
            EnemyAttackDataSO attackData,
            float assumedDistance,
            int monsterLevel,
            List<BalanceValidationMessage> messages)
        {
            if (attackData == null)
                return;

            if (attackData.skills == null || attackData.skills.Count == 0)
            {
                messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Error, "EnemyAttackDataSO.skills가 비어 있습니다."));
                return;
            }

            int usableCount = 0;
            int unlockedCount = 0;
            for (int i = 0; i < attackData.skills.Count; i++)
            {
                EnemyAttackInfo skill = attackData.skills[i];
                string label = $"skills[{i}]";
                if (skill == null)
                {
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Error, $"{label}이 null입니다."));
                    continue;
                }

                if (skill.baseInfo == null)
                {
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Error, $"{label}.baseInfo가 null입니다."));
                    continue;
                }

                if (skill.baseInfo.hitPhases == null || skill.baseInfo.hitPhases.Count == 0)
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Error, $"{label}.hitPhases가 비어 있습니다."));

                if (skill.selectionWeight <= 0f)
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Warning, $"{label}.selectionWeight가 0 이하입니다."));

                if (skill.cooldown <= 0f)
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Warning, $"{label}.cooldown이 0 이하입니다."));

                if (BalanceAttackAnalyzer.SumDamage(skill.baseInfo) <= 0f)
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Warning, $"{label}의 총 damage가 0 이하입니다."));

                bool isStrong = BalanceAttackAnalyzer.IsStrongEnemyAttack(skill);
                if (isStrong && !skill.useDangerRing)
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Warning, $"{label}은 강한 공격(Heavy/Skill)인데 Danger Ring이 꺼져 있습니다."));

                if (skill.useDangerRing && skill.dangerRingDuration <= 0f && BalanceAttackAnalyzer.CountHitPhases(skill.baseInfo) == 0)
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Warning, $"{label}은 Danger Ring 자동 수축 시간 계산에 필요한 공격 Phase가 없습니다."));

                // 넓은 범위/긴 사거리 강공격인데 바닥 텔레그래프가 없으면 경고 (Danger Ring과 별개)
                bool wideOrLongRange = skill.maxRange >= 4f || skill.telegraphRadiusScale >= 1.5f;
                if (isStrong && wideOrLongRange && !skill.useTelegraph)
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Warning, $"{label}은 넓은 범위/긴 사거리 강공격인데 useTelegraph가 꺼져 있습니다."));

                if (skill.useTelegraph && skill.useMotionEventTelegraph && skill.baseInfo.attackType == AttackType.Ranged)
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Info, $"{label}은 useMotionEventTelegraph 사용 — MotionSet에 TelegraphEvent가 있는지 확인하세요."));

                if (!skill.IsUnlockedForLevel(monsterLevel))
                    continue;

                unlockedCount++;
                if (skill.IsInRange(assumedDistance))
                    usableCount++;
            }

            if (unlockedCount == 0)
                messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Error, $"레벨 {monsterLevel}에서 해금된 공격이 없습니다."));
            else if (usableCount == 0)
                messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Warning, $"기준 거리 {assumedDistance:F1}에서 사용 가능한 공격이 없습니다."));
        }

        /// <summary>등급별 권장 범위(LEVEL_GRADE_COMBAT_BALANCE_POLICY)의 강한 공격 합산 확률 밴드.</summary>
        private static (float low, float high) GetStrongChanceBand(MonsterActorGrade grade)
        {
            return grade switch
            {
                MonsterActorGrade.Boss => (0.40f, 0.65f),
                MonsterActorGrade.Elite => (0.25f, 0.45f),
                _ => (0.10f, 0.25f),
            };
        }

        /// <summary>
        /// 정적 추정이 끝난 뒤, 계산된 결과를 기준으로 하는 검증을 결과 메시지에 덧붙인다.
        /// (Strong% 등급 밴드 이탈, 단일 공격 DPS 과점)
        /// </summary>
        public static void AppendPostAnalysisMessages(BalanceScenarioResult result)
        {
            if (result?.Actor == null || result.Status == BalanceCheckStatus.InvalidData)
                return;

            if (result.SkillBreakdowns.Count > 0)
            {
                (float low, float high) = GetStrongChanceBand(result.Actor.grade);
                float strong = result.StrongAttackChance;
                if (strong > high)
                {
                    // 상한 초과: 강한 공격을 너무 자주 던져 과하게 치명적일 수 있음 → 경고
                    result.Messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Warning,
                        $"강한 공격 합산 확률 {strong * 100f:F0}%가 {result.Actor.grade} 권장 상한 {high * 100f:F0}%를 초과합니다."));
                }
                else if (strong > 0f && strong < low)
                {
                    // 강한 공격이 있긴 하나 하한 미만: 의도일 수 있어 정보 수준으로만 안내.
                    // (강한 공격이 전혀 없는 순수 기본 공격 몬스터는 정상 설계이므로 경고하지 않는다.)
                    result.Messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Info,
                        $"강한 공격 합산 확률 {strong * 100f:F0}%가 {result.Actor.grade} 권장 하한 {low * 100f:F0}% 미만입니다."));
                }
            }

            if (result.TopAttackDpsShare > 0.35f && !string.IsNullOrEmpty(result.TopAttackName))
                result.Messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Warning,
                    $"'{result.TopAttackName}' 단일 공격이 전체 적 DPS의 {result.TopAttackDpsShare * 100f:F0}%를 차지합니다 (권장 35% 이하)."));

            if (result.MonsterBreakGauge > 0f)
            {
                if (result.PlayerExpectedBreakDps <= 0f)
                    result.Messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Warning,
                        "몬스터는 Break Gauge를 사용하지만 플레이어 공격 데이터의 breakDamage 합산이 0입니다."));
                else if (result.EstimatedTimeToBreak > result.TargetDuration)
                    result.Messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Info,
                        $"예상 브레이크 시간이 {result.EstimatedTimeToBreak:F1}s로 기준 시간 {result.TargetDuration:F1}s보다 깁니다."));
            }
        }
    }
}
#endif
