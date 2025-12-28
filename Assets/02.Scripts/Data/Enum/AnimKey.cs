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

        #region Trun
        Walk_Turn_L45,
        Walk_Turn_R45,
        Walk_Turn_L90,
        Walk_Turn_R90,
        Walk_Turn_L180,
        Walk_Turn_R180,
        
        Run_Turn_L45,
        Run_Turn_R45,
        Run_Turn_L90,
        Run_Turn_R90,
        Run_Turn_L180,
        Run_Turn_R180,
        
        Sprint_Turn_L45,
        Sprint_Turn_R45,
        Sprint_Turn_L90,
        Sprint_Turn_R90,
        Sprint_Turn_L180,
        Sprint_Turn_R180,
        #endregion
        
        
        Mixer_Locomotion,
    }