using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Quest
{
    /// <summary>
    /// 퀘스트 정의 ScriptableObject.
    ///
    /// 사용 예시:
    ///   - QuestManager.Instance.AcceptQuest(questSO.questId)
    ///   - QuestManager.Instance.CompleteQuest(questSO.questId)
    ///
    /// 목표 타입별 호출 포인트:
    ///   ItemCollect  → QuestManager.Instance.NotifyItemCollected(itemId, count)
    ///   ItemDeliver  → QuestManager.Instance.NotifyItemDelivered(npcId, itemId, count)
    ///   ItemUse      → QuestManager.Instance.NotifyItemUsed(itemId, count)
    ///   MonsterKill  → QuestManager.Instance.NotifyMonsterKill(monsterId)
    ///   StoryProgress→ QuestManager.Instance.NotifyStoryProgress(progress)
    ///   ItemCraft    → QuestManager.Instance.NotifyItemCrafted(recipeId, quantity)
    ///   ItemEnhance  → QuestManager.Instance.NotifyItemEnhanced(itemId)
    ///   ReachLocation→ QuestManager.Instance.NotifyLocationReached(locationId)
    /// </summary>
    [CreateAssetMenu(fileName = "QuestSO", menuName = "UPlayGround/퀘스트/Quest")]
    public class QuestSO : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("퀘스트 고유 ID. 전체 DB에서 유일해야 함.")]
        public string questId;
        public string questName;
        [TextArea] public string questDescription;

        [Header("선행 조건")]
        [Tooltip("이 퀘스트를 수락하기 위해 완료해야 하는 퀘스트 ID 목록")]
        public List<string> requiredQuestIds = new List<string>();
        [Tooltip("이 퀘스트를 수락하기 위해 필요한 스토리 진행도 (0이면 조건 없음)")]
        public int requiredStoryProgress = 0;

        [Header("자동 연계")]
        [Tooltip("이 퀘스트 완료 직후 자동으로 수락할 후속 퀘스트 ID 목록")]
        public List<string> autoAcceptNextQuestIds = new List<string>();

        [Header("목표")]
        public List<QuestObjectiveData> objectives = new List<QuestObjectiveData>();

        [Header("보상")]
        public QuestRewardData reward = new QuestRewardData();

        [Header("설정")]
        [Tooltip("완료 후 다시 수락할 수 있는 반복 퀘스트")]
        public bool isRepeatable = false;
        [Tooltip("모든 목표 달성 즉시 자동으로 완료 처리")]
        public bool autoComplete = false;
    }
}
