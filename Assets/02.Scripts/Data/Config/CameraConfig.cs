using UnityEngine;

namespace UPlayGround.Data.Config
{
    public static class CameraConfig
    {
        public static readonly string[] LockLayer = new string[]
        {
            "Enemy",
        };
        public static readonly string[] CollisionExcludeLayer = new string[]
        {
            "Player",
            "Enemy",
//             "Default",
            "Npc",
            "Projectile",
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
            LayerMask mask = ~0; // 모든 레이어를 포함 (비트 전체를 1로)

            foreach (string layerName in CollisionExcludeLayer)
            {
                int layer = LayerMask.NameToLayer(layerName);
                if (layer != -1) // 유효한 레이어인 경우
                {
                    mask &= ~(1 << layer); // 해당 레이어 비트를 0으로 만들어 제외
                }
            }
    
            return mask;
        }
    }

}