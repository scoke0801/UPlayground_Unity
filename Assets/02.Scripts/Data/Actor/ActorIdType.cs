// 자동 생성 파일입니다. 직접 수정하지 마세요.
// UPlayGround/Actor/Actor Database Editor → [Enum 생성] 버튼으로 재생성하세요.
// Generated: 2026-04-10 15:08
namespace UPlayGround.Data.Actor
{
    /// <summary>
    /// ActorDatabase에 등록된 모든 Actor의 타입 열거형.
    /// ActorSpawnManager.SpawnActor(ActorIdType, ...) 호출에 사용한다.
    /// </summary>
    public enum ActorIdType
    {
        None = 0,
        SkeletonCommon = 1,
        Skeleton_Bow = 2,
        Skeleton_Sword = 3,
        Golem_Normal = 4,
        Golem_Inferno = 5,
        Golem_Black = 6,
        Lich_Normal = 7,
        Lich_Elite = 8,
        Ent_Normal = 9,
        Ent_Elite = 10,
        Griffin_Normal = 11,
        Griffin_Elite = 12,
        MonsterHonoka = 13,
        MonsterLianLian = 14,
        MonsterBokusei = 15,
        SpiderQueen_1 = 16,
        SpiderQueen_2 = 17,
        SpiderQueen_3 = 18,
        SpiderMinion_1 = 19,
        SpiderMinion_2 = 20,
        SpiderMinion_3 = 21,
        ChildPlant_1 = 22,
        ChildPlant_2 = 23,
        ChildPlant_3 = 24,
        Plant_1 = 25,
        Plant_2 = 26,
        Plant_3 = 27,
        RootPlant_1 = 28,
        RootPlant_2 = 29,
        RootPlant_3 = 30,
        Dryad = 31,
        Training_Dummy = 32,
    }

    public static class ActorIdTypeExtensions
    {
        /// <summary>enum 값을 ActorDatabase의 actorId 문자열로 변환한다.</summary>
        public static string ToActorId(this ActorIdType type) => type switch
        {
            ActorIdType.SkeletonCommon => "SkeletonCommon",
            ActorIdType.Skeleton_Bow => "Skeleton_Bow",
            ActorIdType.Skeleton_Sword => "Skeleton_Sword",
            ActorIdType.Golem_Normal => "Golem_Normal",
            ActorIdType.Golem_Inferno => "Golem_Inferno",
            ActorIdType.Golem_Black => "Golem_Black",
            ActorIdType.Lich_Normal => "Lich_Normal",
            ActorIdType.Lich_Elite => "Lich_Elite",
            ActorIdType.Ent_Normal => "Ent_Normal",
            ActorIdType.Ent_Elite => "Ent_Elite",
            ActorIdType.Griffin_Normal => "Griffin_Normal",
            ActorIdType.Griffin_Elite => "Griffin_Elite",
            ActorIdType.MonsterHonoka => "MonsterHonoka",
            ActorIdType.MonsterLianLian => "MonsterLianLian",
            ActorIdType.MonsterBokusei => "MonsterBokusei",
            ActorIdType.SpiderQueen_1 => "SpiderQueen_1",
            ActorIdType.SpiderQueen_2 => "SpiderQueen_2",
            ActorIdType.SpiderQueen_3 => "SpiderQueen_3",
            ActorIdType.SpiderMinion_1 => "SpiderMinion_1",
            ActorIdType.SpiderMinion_2 => "SpiderMinion_2",
            ActorIdType.SpiderMinion_3 => "SpiderMinion_3",
            ActorIdType.ChildPlant_1 => "ChildPlant_1",
            ActorIdType.ChildPlant_2 => "ChildPlant_2",
            ActorIdType.ChildPlant_3 => "ChildPlant_3",
            ActorIdType.Plant_1 => "Plant_1",
            ActorIdType.Plant_2 => "Plant_2",
            ActorIdType.Plant_3 => "Plant_3",
            ActorIdType.RootPlant_1 => "RootPlant_1",
            ActorIdType.RootPlant_2 => "RootPlant_2",
            ActorIdType.RootPlant_3 => "RootPlant_3",
            ActorIdType.Dryad => "Dryad",
            ActorIdType.Training_Dummy => "Training_Dummy",
            _ => string.Empty,
        };
    }
}
