using UnityEngine;

namespace UPlayGround.Data.Config
{
    public static class CameraConfig
    {
        public static readonly string[] LockLayer = new string[]
        {
            "Enemy",
        };
        
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
    }

}