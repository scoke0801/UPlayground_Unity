using UnityEngine;

namespace UPlayGround.Data.Config
{
    public static class CameraConfig
    {
        public static readonly string[] LockLayer = new string[]
        {
            "Enemy",
        };
        // 카메라 충돌은 정적 환경 지오메트리(벽·지형·대형 구조물)에만 반응한다.
        // 제외 방식은 새 레이어가 추가될 때마다 자동으로 카메라 차단체가 되어
        // 캐릭터·인터랙션 오브젝트가 카메라를 당기는 사고가 반복되므로 포함 방식으로 운용한다.
        // 카메라에 막혀야 하는 오브젝트는 반드시 아래 레이어 중 하나에 배치할 것.
        public static readonly string[] CollisionIncludeLayer = new string[]
        {
            "Default",
            "Ground",
        };

        public static readonly LayerMask LockOnOutlineLayerMask = LayerMask.NameToLayer("LockOnOutline");

        public static LayerMask GetLockOnLayerMask()
        {
            LayerMask mask = 0;
    
            foreach (string layerName in LockLayer)
            {
                int layer = LayerMask.NameToLayer(layerName);
                if (layer != -1)
                {
                    mask |= (1 << layer);  // 비트 OR로 레이어 추가
                }
            }
    
            return mask;
        }

        public static LayerMask GetCollisionLayerMask()
        {
            LayerMask mask = 0;

            foreach (string layerName in CollisionIncludeLayer)
            {
                int layer = LayerMask.NameToLayer(layerName);
                if (layer != -1) // 유효한 레이어인 경우
                {
                    mask |= (1 << layer); // 해당 레이어 비트를 1로 만들어 포함
                }
            }

            return mask;
        }
    }

}