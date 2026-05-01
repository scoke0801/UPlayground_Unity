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
        Bokusei,
        Honoka,
        Reine,
        LianLian,
        Nenmir,
        Sera,
        Inori,
        Hichi,
        
        H09,
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
    }
}