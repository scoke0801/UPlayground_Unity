namespace Interaction.Enum
{
    public enum LifeInteractionType
    {
        NONE = 0,
        WOODCUTTING,    // 벌목
        MINING,         // 채광
        Fishing,        // 낚시
        Gathering,      // 수집
    }

    public enum InteractionObjectType
    {
        NONE = 0,
        TREE,       // 벌목
        STONE,      // 채광
        FISHING_ZONE,//낚시터
        GATERING_ZONE,//수집
        NPC,        // NPC 대화
        REST_POINT, // 파티 체력 회복 (모닥불/제단)
        DROP_ITEM, // 맵 배치/드랍 아이템 줍기
    }
}
