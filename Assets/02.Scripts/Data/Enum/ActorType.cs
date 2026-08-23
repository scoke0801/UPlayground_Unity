namespace UPlayGround.Data.EnumType
{
    [System.Flags]
    public enum ActorType
    {
        None      = 0,
        Player    = 1 << 0,  // 1
        Monster   = 1 << 1,  // 2
        Obstacle  = 1 << 2,  // 4
        NPC       = 1 << 3,  // 8  — 비전투 캐릭터 등 일반 NPC
        Combat    = 1 << 4,  // 16 — 전투 가능 여부
        Talkable  = 1 << 5,  // 32 — 대화 가능 여부
    }

    public enum MonsterActorGrade
    {
        Normal = 0,
        Elite,
        Boss,
        Weak,
    }
    
    public enum CharacterActorType
    {
        None = 0,
        Raon = 1,
        Hwarin = 2,
        Reine = 3,
        Lian = 4,
        SeolA = 5,
        Sera = 6,
        YeonHoa = 7,
        Yura = 8,
        MyoRyeong = 9,
        Myomyo = 10,
        Lili = 11,

        H09 = 12, // 안쓸거임
        Arin = 13,
    }

    /// <summary>세이브에 기록된 캐릭터 이름을 현재 enum 값으로 복원한다.</summary>
    public static class CharacterActorTypeUtility
    {
        public static bool TryParsePersistentName(
            string name,
            out CharacterActorType type)
        {
            if (!string.IsNullOrWhiteSpace(name)
                && System.Enum.TryParse(name, out type)
                && type != CharacterActorType.None)
            {
                return true;
            }

            type = name switch
            {
                "Bokusei" => CharacterActorType.Raon,
                "Honoka" => CharacterActorType.Hwarin,
                "LianLian" => CharacterActorType.Lian,
                "Nenmir" => CharacterActorType.SeolA,
                "Inori" => CharacterActorType.YeonHoa,
                "Hichi" => CharacterActorType.Yura,
                "Siuha" => CharacterActorType.MyoRyeong,
                "Komoe" => CharacterActorType.Myomyo,
                _ => CharacterActorType.None,
            };
            return type != CharacterActorType.None;
        }
    }

    public enum ActorSocketType
    {
        None = 0,
        
        LeftHand,
        RightHand,
        
        Center,
        
        Head,
        
        UI_HpBar,

        Weapon,

        GuardPosition,

        // 신규 항목은 반드시 끝에 추가한다 — 중간 삽입은 직렬화된 enum 정수값을 밀어내 기존 프리팹의 소켓 참조를 깨뜨린다.
        UIDangerRing,
    }
}
