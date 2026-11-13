namespace UPlayGround
{
    /// <summary>채집 애니메이션 입력만 전달하고 환경이 상태·보상·저장을 소유하게 한다.</summary>
    public interface IReactiveGatheringTarget
    {
        bool OwnsGathering { get; }
        bool CanGather { get; }
        float GatheringHealth { get; }
        float GatheringMaximumHealth { get; }
        bool TryGather(float damage);
    }
}
