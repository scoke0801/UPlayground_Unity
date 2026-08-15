using System.Collections.Generic;
using UnityEngine;

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

        /// <summary>
        /// 목표의 표시 선행 조건만 평가한다. 숨겨진 목표도 Notify 계열 진행 판정은 계속 받는다.
        /// </summary>
        public bool IsObjectiveVisible(QuestObjectiveData objective)
        {
            return QuestObjectiveVisibility.IsVisible(QuestSO, this, objective);
        }

        public IEnumerable<QuestObjectiveData> GetVisibleObjectives()
        {
            foreach (QuestObjectiveData objective in QuestSO.objectives)
            {
                if (QuestObjectiveVisibility.IsVisible(QuestSO, this, objective))
                    yield return objective;
            }
        }

        /// <summary> 진행 카운트를 value만큼 증가시키고 현재 값을 반환 </summary>
        public int AddProgress(string objectiveId, int value = 1)
        {
            if (!ObjectiveProgress.ContainsKey(objectiveId))
                ObjectiveProgress[objectiveId] = 0;

            ObjectiveProgress[objectiveId] += value;
            var objective = QuestSO.objectives.Find(obj => obj.objectiveId == objectiveId);
            if (objective != null)
                ObjectiveProgress[objectiveId] = Mathf.Clamp(ObjectiveProgress[objectiveId], 0, objective.requiredCount);
            return ObjectiveProgress[objectiveId];
        }

        /// <summary> 진행 카운트를 value로 직접 설정 </summary>
        public void SetProgress(string objectiveId, int value)
        {
            var objective = QuestSO.objectives.Find(obj => obj.objectiveId == objectiveId);
            ObjectiveProgress[objectiveId] = objective != null
                ? Mathf.Clamp(value, 0, objective.requiredCount)
                : Mathf.Max(0, value);
        }
    }

    /// <summary>
    /// HUD·퀘스트 메뉴·월드 마커가 공유하는 목표 표시 규칙.
    /// 표시 선행 조건은 안내 순서만 제어하며 실제 진행 순서를 강제하지 않는다.
    /// </summary>
    public static class QuestObjectiveVisibility
    {
        public static bool IsVisible(
            QuestSO quest,
            QuestRuntimeData runtime,
            QuestObjectiveData objective,
            bool revealAll = false)
        {
            if (quest == null || objective == null)
                return false;
            if (revealAll)
                return true;

            List<string> prerequisites = objective.revealAfterObjectiveIds;
            if (prerequisites == null || prerequisites.Count == 0)
                return true;
            if (runtime == null)
                return false;

            for (int i = 0; i < prerequisites.Count; i++)
            {
                string prerequisiteId = prerequisites[i];
                if (string.IsNullOrWhiteSpace(prerequisiteId))
                    return false;

                QuestObjectiveData prerequisite = quest.objectives.Find(
                    candidate => candidate != null && candidate.objectiveId == prerequisiteId);
                if (prerequisite == null || !runtime.IsObjectiveComplete(prerequisite))
                    return false;
            }

            return true;
        }
    }
}
