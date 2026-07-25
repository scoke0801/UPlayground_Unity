namespace UPlayGround.Gameplay.Tag
{
    /// <summary>
    /// 데이터 계층이 런타임 태그 컨테이너 구현에 의존하지 않도록 하는 읽기 전용 계약.
    /// </summary>
    public interface IGameplayTagReader
    {
        bool HasTag(GameplayTag tag);
    }
}
