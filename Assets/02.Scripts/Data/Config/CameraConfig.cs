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

        // 가림 페이드 대상에서 항상 제외해 카메라 암으로 막는 레이어. 지형·바닥처럼 통과하면 안 되는 면이다.
        public static readonly string[] SolidObstacleLayer = new string[]
        {
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

        // 가림 페이드 프록시 전용 레이어. 렌더러 데이터가 이 레이어를 메인 패스(불투명·투명·프리패스)에서 제외하고
        // CameraOccluderFadeFeature만 그린다. 콜라이더가 없는 렌더링 전용 레이어이므로 물리에 영향이 없다.
        public const string FadeProxyLayerName = "CameraFadeProxy";

        /// <summary>프록시 레이어 인덱스. 레이어가 정의되지 않았으면 -1.</summary>
        public static int GetFadeProxyLayer() => LayerMask.NameToLayer(FadeProxyLayerName);

        public static LayerMask GetSolidObstacleLayerMask()
        {
            LayerMask mask = 0;
            foreach (string layerName in SolidObstacleLayer)
            {
                int layer = LayerMask.NameToLayer(layerName);
                if (layer != -1) mask |= 1 << layer;
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