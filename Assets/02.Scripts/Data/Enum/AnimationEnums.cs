namespace UPlayGround.Data.EnumType
{
    /// <summary>애니메이션 레이어 구분용.</summary>
    public enum AnimLayer
    {
        FullBody = 0,
        UpperBody = 1,
        LowerBody = 2,
        Head = 3,
        Eye = 4,
        LeftHand = 10,
        RightHand = 11,
        LeftFoot = 20,
        RightFoot = 21,
    }

    public enum InteractionAnimEvent
    {
        OnHit,
        CatchFish,
    }

    /// <summary>이동 애니메이션 유형.</summary>
    public enum BaseMoveAnimType
    {
        Walk = 0,
        Run,
        Sprint,
        Crouching,
    }
}
