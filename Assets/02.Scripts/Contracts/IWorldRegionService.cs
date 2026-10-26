using UnityEngine.SceneManagement;
using UPlayGround.Manager;

namespace UPlayGround
{
    /// <summary>현장 러너가 전역 사건을 발화할 지역인지 판단하는 소비자 계약.</summary>
    public interface IWorldRegionService : IGameService
    {
        bool CanRunRegionEvents(Scene ownerScene);
    }
}
