using System.Collections.Generic;

namespace UPlayGround.Data.Quest
{
    /// <summary>
    /// 퀘스트 하나의 런타임 상태.
    /// QuestManager가 생성하고 보관한다.
    /// </summary>
    public class QuestRuntimeData
    {
        public QuestSO QuestSO { get; }
        public QuestStatus Status { get; set; }

        /// <summary> objectiveId → 현재 진행 카운트 </summary>
        public Dictionary<string, int> ObjectiveProgress { get; } = new Dictionary<string, int>();

        public QuestRuntimeData(QuestSO questSO)
        {
            QuestSO = questSO;
            Status  = QuestStatus.Available;
            foreach (var obj in questSO.objectives)
                ObjectiveProgress[obj.objectiveId] = 0;
        }

        /// <summary> 특정 목표가 달성됐는지 확인 </summary>
        public bool IsObjectiveComplete(QuestObjectiveData obj)
        {
            return ObjectiveProgress.TryGetValue(obj.objectiveId, out var count)
                   && count >= obj.requiredCount;
        }

        /// <summary> 모든 목표가 달성됐는지 확인 </summary>
        public bool AreAllObjectivesComplete()
        {
            foreach (var obj in QuestSO.objectives)
                if (!IsObjectiveComplete(obj))
                    return false;
            return true;
        }

        /// <summary> 진행 카운트를 value만큼 증가시키고 현재 값을 반환 </summary>
        public int AddProgress(string objectiveId, int value = 1)
        {
            if (!ObjectiveProgress.ContainsKey(objectiveId))
                ObjectiveProgress[objectiveId] = 0;

            ObjectiveProgress[objectiveId] += value;
            return ObjectiveProgress[objectiveId];
        }

        /// <summary> 진행 카운트를 value로 직접 설정 </summary>
        public void SetProgress(string objectiveId, int value)
        {
            ObjectiveProgress[objectiveId] = value;
        }
    }
}
