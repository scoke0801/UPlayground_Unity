using UnityEngine;

namespace UPlayGround.Components
{
    /// <summary>접촉당 오브젝트를 만들지 않고 고정 크기 큐로 수면 파문을 전달한다.</summary>
    public static class WaterContactStamps
    {
        public const int Capacity = 32;

        public struct Stamp
        {
            public FootstepWaterSurface Surface;
            public Vector3 Position;
            public float Time, Strength;
        }

        private static readonly Stamp[] Stamps = new Stamp[Capacity];
        private static int _next;
        public static int OverwrittenCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            System.Array.Clear(Stamps, 0, Stamps.Length);
            _next = 0;
            OverwrittenCount = 0;
        }

        /// <summary>조회한 수역의 표면에만 입수·수영·발·낙하물 파문을 기록한다.</summary>
        public static bool TryEmit(Vector3 position, float strength)
        {
            if (strength <= 0f || !FootstepWaterSurface.TryResolveSurface(position, out var surface, out var water)
                || surface.dynamics == null) return false;
            if (Stamps[_next].Surface != null && FootstepWaterSurface.WaterTime - Stamps[_next].Time < surface.dynamics.rippleDuration)
                OverwrittenCount++;
            Stamps[_next] = new Stamp { Surface = surface, Position = water, Time = FootstepWaterSurface.WaterTime, Strength = strength };
            _next = (_next + 1) % Capacity;
            return true;
        }

        /// <summary>렌더 카메라는 같은 접촉 스냅샷을 읽으며 이벤트를 소비하거나 재생성하지 않는다.</summary>
        public static Stamp GetStamp(int index) => Stamps[index];
    }
}
