using System;
using System.Collections.Generic;
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

        [Tooltip("위치 또는 Actor처럼 문자열로 식별하는 목표 ID")]
        public string targetStringId;

        [Tooltip("이 목표를 가리킬 지도·월드 마커 위치 ID. 비우면 목표 타입의 기본 규칙을 쓴다. 대화·서사 이벤트처럼 위치를 스스로 알 수 없는 목표에 지정한다.")]
        public string markerLocationId;

        [Tooltip("달성에 필요한 수량 (0이면 1회 달성)")]
        [Min(1)] public int requiredCount = 1;

        [Tooltip("이 목표를 표시하기 전에 완료되어야 하는 같은 퀘스트의 목표 ID. 진행 판정은 표시 여부와 무관하게 계속 누적됩니다.")]
        public List<string> revealAfterObjectiveIds = new List<string>();
    }
}
