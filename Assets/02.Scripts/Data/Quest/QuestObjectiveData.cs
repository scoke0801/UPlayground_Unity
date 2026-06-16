using System;
using UnityEngine;

namespace UPlayGround.Data.Quest
{
    /// <summary>
    /// 퀘스트 단일 목표 데이터 (QuestSO에 List로 포함)
    /// </summary>
    [Serializable]
    public class QuestObjectiveData
    {
        [Tooltip("목표 고유 ID. QuestSO 내에서 유일해야 함.")]
        public string objectiveId;

        [Tooltip("플레이어에게 표시되는 목표 설명")]
        [TextArea] public string description;

        public QuestObjectiveType type;

        [Tooltip("아이템ID / 레시피ID / 스토리 진행도 값. MonsterKill의 숫자 ID는 레거시 호환용.")]
        public int targetId;

        [Tooltip("ItemDeliver 목표에서 아이템을 전달받는 NPC ID")]
        public int npcId;

        [Tooltip("ReachLocation 목표의 위치 ID 또는 MonsterKill 목표의 ActorId")]
        public string targetStringId;

        [Tooltip("달성에 필요한 수량 (0이면 1회 달성)")]
        [Min(1)] public int requiredCount = 1;
    }
}
