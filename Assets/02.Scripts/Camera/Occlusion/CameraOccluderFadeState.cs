using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 카메라 가림 페이드가 렌더링 쪽 RendererFeature에 넘기는 프레임 상태.
    /// 슬롯 하나는 흐려지는 오브젝트 덩어리 하나이며, 렌더러는 슬롯별 렌더링 레이어 비트로 식별된다.
    /// Camera 모듈이 URP에 의존하지 않도록 값만 전달하는 정적 경계로 둔다.
    /// </summary>
    public static class CameraOccluderFadeState
    {
        /// <summary>동시에 쓸 수 있는 최대 슬롯 수.</summary>
        public const int MaxSlots = 8;

        /// <summary>슬롯 0이 사용하는 렌더링 레이어 비트 번호. 이후 슬롯은 연속된다.</summary>
        public const int FirstRenderingLayerBit = 24;

        /// <summary>디더 단계 수. 가시율은 0~Levels 정수로 전달되며 Levels면 완전 표시다.</summary>
        public const int Levels = 16;

        private static int s_proxyLayerMask;
        private static int s_registeredFeatureCount;

        /// <summary>
        /// 활성 렌더러에 가림 페이드 RendererFeature가 생성돼 있는지 여부.
        /// 없으면 프록시를 아무도 디더하지 않으므로 페이더가 꺼지고 기존 카메라 충돌만 쓴다.
        /// </summary>
        public static bool IsFeatureRegistered => s_registeredFeatureCount > 0;

        /// <summary>RendererFeature가 생성될 때 호출한다.</summary>
        public static void RegisterFeature() => s_registeredFeatureCount++;

        /// <summary>RendererFeature가 해제될 때 호출한다.</summary>
        public static void UnregisterFeature() => s_registeredFeatureCount = Mathf.Max(0, s_registeredFeatureCount - 1);

        /// <summary>
        /// 페이드 프록시가 놓인 물리 레이어의 비트마스크. 렌더러 데이터가 이 레이어를 메인 패스에서 제외하고
        /// RendererFeature가 이 레이어만 골라 그린다.
        /// </summary>
        public static int ProxyLayerMask
        {
            get => s_proxyLayerMask;
            set => s_proxyLayerMask = value;
        }

        private static readonly int[] s_levels = new int[MaxSlots];
        private static int s_activeMask;
        private static int s_pixelScale = 1;

        /// <summary>활성 슬롯 비트마스크. 0이면 이번 프레임에 그릴 것이 없다.</summary>
        public static int ActiveSlotMask => s_activeMask;

        /// <summary>디더 점 크기(화면 픽셀).</summary>
        public static int PixelScale => s_pixelScale;

        /// <summary>슬롯의 렌더링 레이어 마스크를 반환한다.</summary>
        public static uint GetRenderingLayerMask(int slot) => 1u << (FirstRenderingLayerBit + slot);

        /// <summary>슬롯의 가시 단계(0~<see cref="Levels"/>)를 반환한다.</summary>
        public static int GetLevel(int slot) => s_levels[slot];

        /// <summary>슬롯 하나의 상태를 발행한다.</summary>
        public static void SetSlot(int slot, int level)
        {
            s_levels[slot] = Mathf.Clamp(level, 0, Levels);
            s_activeMask |= 1 << slot;
        }

        /// <summary>프레임 시작 시 모든 슬롯 발행을 비운다.</summary>
        public static void BeginPublish(int pixelScale)
        {
            s_activeMask = 0;
            s_pixelScale = Mathf.Clamp(pixelScale, 1, 4);
        }

        /// <summary>모든 상태를 지운다.</summary>
        public static void Clear()
        {
            s_activeMask = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            s_activeMask = 0;
            s_pixelScale = 1;
            s_proxyLayerMask = 0;
            // s_registeredFeatureCount는 일부러 리셋하지 않는다. 도메인 리로드 없이 Play Mode에 들어가면
            // 렌더러와 Feature가 재생성되지 않으므로, 리셋하면 등록이 영구히 사라져 페이드가 꺼진다.
        }
    }
}
