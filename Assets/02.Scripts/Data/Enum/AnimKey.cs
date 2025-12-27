public enum AnimKey
{
    None = 0,
    Idle, 
    Move, 
    Attack, 
    Die,
    Dodge,
    
    Mining,
    Fishing,
    WoodCut,
    
    Jump,
    Fall,
    Land,
    
    // 정지 (Stop)
    Move_Stop_Walking,
    Move_Stop_Running,
    Move_Stop_Sprinting,

    // 제자리 회전 (Turn In Place)
    Move_Turn_L90,
    Move_Turn_R90,
    Move_Turn_L180,
    Move_Turn_R180,
    
    Mixer_Locomotion,
}