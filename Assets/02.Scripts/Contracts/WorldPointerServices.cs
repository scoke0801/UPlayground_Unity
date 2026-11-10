using UnityEngine;

namespace UPlayGround.Manager
{
    /// <summary>월드 클릭을 전투 선입력보다 먼저 소비하는 UI 소유 계약.</summary>
    public interface IWorldPointerInputService : IGameService
    {
        bool TryConsume(Vector2 position, bool performed);
        bool TryConsumeInteraction(bool performed, bool preferNearestItem = false);
        bool TryConsumeDirection(Vector2 direction, bool performed);
    }
}
