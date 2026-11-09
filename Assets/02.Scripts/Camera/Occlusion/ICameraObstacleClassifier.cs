using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>카메라 충돌 계산에서 콜라이더가 암을 접게 하는 장애물인지 판정한다.</summary>
    public interface ICameraObstacleClassifier
    {
        /// <summary>true면 카메라가 통과할 수 없는 Solid 장애물이다.</summary>
        bool IsBlocking(Collider collider);
    }
}
