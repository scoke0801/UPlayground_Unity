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
                if (actor.attackData == null)
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Error, "몬스터 ActorDefinitionSO인데 attackData가 비어 있습니다."));

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

                if (BalanceAttackAnalyzer.IsStrongEnemyAttack(skill) && !skill.useDangerRing)
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Warning, $"{label}은 강한 공격(Heavy/Skill)인데 Danger Ring이 꺼져 있습니다."));

                if (skill.useDangerRing && skill.dangerRingDuration <= 0f && BalanceAttackAnalyzer.CountHitPhases(skill.baseInfo) == 0)
                    messages.Add(new BalanceValidationMessage(BalanceValidationLevel.Warning, $"{label}은 Danger Ring 자동 수축 시간 계산에 필요한 공격 Phase가 없습니다."));

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
    }
}
#endif
