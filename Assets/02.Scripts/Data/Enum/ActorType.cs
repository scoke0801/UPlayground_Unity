namespace UPlayGround.Data.EnumType
{
    public enum ActorType
    {
        None = 0,

        Player = 1,

        Monster = 2,

        Obstacle = 3,

    }

    public enum CharacterActorType
    {
        None = 0,
        
        // 비싼 친구들 부터
        Bokusei,
        Honoka,
        Reine,
        
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
    }
}