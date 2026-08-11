namespace UPlayGround.Data.Quest
{
    /// <summary>
    /// 퀘스트 목표 타입
    /// </summary>
    public enum QuestObjectiveType
    {
        ItemCollect  = 0,  // 아이템 수집 (인벤토리에 targetId 아이템 requiredCount개 보유)
        ItemDeliver  = 1,  // 아이템 NPC에게 전달 (npcId NPC에게 targetId 아이템 requiredCount개 전달)
        ItemUse      = 2,  // 아이템 사용 (targetId 아이템 requiredCount회 사용)
        MonsterKill  = 3,  // 몬스터 처치 (targetId 몬스터 requiredCount마리 처치)
        StoryProgress = 4, // 스토리 진행 (StoryManager 진행도가 targetId 이상)
        ItemCraft    = 5,  // 아이템 제작 (targetId 레시피 ID로 requiredCount회 제작)
        ItemEnhance  = 6,  // 아이템 강화 (targetId 아이템 ID requiredCount회 강화)
        ReachLocation = 7, // 목표 지점 도달 (targetStringId 위치 ID 도달)
        CycleOuterBoss = 8, // 현재 사이클의 외곽 수호자 처치 수
    }
}
